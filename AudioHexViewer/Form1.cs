// ═══════════════════════════════════════════════════════════════════════════
// Form1.cs  —  UI pure : constructor, menus, VU-mètres, calibration, affichage
//
// La classe Form1 est découpée en fichiers partiels (Phase 3) :
//   Form1.AudioCapture.cs   — capture WASAPI, ProcessBuffer, enregistrement
//   Form1.FftPipeline.cs    — pipeline FFT, analyse, timer
//   Form1.FftDiagnostics.cs — log de diagnostic (F8)
//   Form1.TestSignals.cs    — sources de test (F9/F10)
//   Form1.FftTicker.cs      — ticker de progression FFT (UX)
// ═══════════════════════════════════════════════════════════════════════════
using AudioHexViewer.Core;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;// ...
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;



namespace AudioHexViewer
{
    public partial class Form1 : Form

    {
        // --- CHAMPS PRIVS ---
        private IWaveIn? capture;   // WasapiCapture (shared) ou ExclusiveWasapiCapture
        private MMDevice? selectedDevice; // priphrique audio choisi
        private WaveFormat? currentFormat;

        // Format demandé explicitement par l'utilisateur (null = auto / Windows Shared Mode)
        private WaveFormat? _requestedCaptureFormat = null;
        private WaveFormatExtensible? _requestedOutputFormat = null;   // SR générateur (null = Auto/Shared)

        /// <summary>
        /// Source pour le mode Loopback Test : le générateur alimente directement l'analyseur.
        /// null = mode normal (périphérique physique).
        /// </summary>
        private IWaveProvider? _loopbackSource = null;

        /// <summary>Appelé par GeneratorWindow pour activer/désactiver le loopback logiciel.</summary>
        private void HandleLoopbackRequest(IWaveProvider? source)
        {
            bool wasCapturing = capture != null;

            // Arrêter la capture actuelle (physique ou loopback précédent)
            if (wasCapturing) StopCapture();

            _loopbackSource = source;

            if (source != null)
            {
                // Démarrer la capture en mode loopback
                StartCapture();
                // Auto-démarrer le spectre et l'histogramme pour voir immédiatement le signal
                if (spectrumControl != null && !_spectrumRunning)
                {
                    spectrumControl.StartFromRun(false);
                    _spectrumRunning = true;
                }
                if (histogramControl != null && !_histogramRunning)
                {
                    histogramControl.StartFromRun(false);
                    _histogramRunning = true;
                }
            }
            else if (wasCapturing)
            {
                // Loopback désactivé → reprendre capture physique si possible
                StartCapture();
            }
        }




        // Suivi de stabilit des bits (24 bits) sur 1 seconde
        private readonly bool[] lastBitsLeft = new bool[24];
        private readonly bool[] lastBitsRight = new bool[24];
        private readonly DateTime[] lastChangeLeft = new DateTime[24];
        private readonly DateTime[] lastChangeRight = new DateTime[24];
        private readonly TimeSpan bitStabilityThreshold = TimeSpan.FromSeconds(0.4);

        private string _lastAllFormatsLeft = "";
        private string _lastAllFormatsRight = "";

        // Saturation par voie
        private DateTime? lastSatLeft = null;
        private DateTime? lastSatRight = null;
        private readonly TimeSpan satDisplayTime = TimeSpan.FromSeconds(1.5);   // dure totale daffichage SAT
        private readonly TimeSpan satBlinkPeriod = TimeSpan.FromMilliseconds(400); // priode du clignotement
        private bool isCurrentlySatLeft = false;
        private bool isCurrentlySatRight = false;

        private readonly Font levelFontRegular;
        private readonly Font levelFontBold;

        // Dernires valeurs calcules (pour laffichage principal)
        private double lastLeftFloat;
        private double lastRightFloat;

        private int lastLeftScaled;
        private int lastRightScaled;

        private int lastLeft24;
        private int lastRight24;

        private string lastLeftHex24 = "";
        private string lastRightHex24 = "";

        private string lastLeftBits = "";
        private string lastRightBits = "";

        private double lastLeftVolts;
        private double lastRightVolts;

        private bool captureOutput = false; // false = IN (capture), true = OUT (loopback)

        // Périphérique sélectionné (remplace l'ancien comboDevices.SelectedItem)
        private MMDevice? _selectedInputDevice;
        private MMDevice? _selectedOutputDevice;  // pour le futur générateur

        // Taille de la fentre principale (avec/sans graphes)
        //private int _heightWithGraphs;
        //private bool _hasHeightWithGraphs;
        // Hauteur minimale de la fentre
        private int _minWidth;
        private int _minHeightNoGraphs;
        // Dsactivation temporaire du redimensionnement
        //private bool _borderStyleSaved;

        private SessionSettings _session = SessionSettingsManager.LoadOrNull() ?? new SessionSettings();

        // --- Enregistrement 1 seconde ---
        private bool isRecordingOneSecond = false;
        private int recordSamplesNeeded = 0;
        private int recordSamplesCollected = 0;
        private List<RecordFrame> recordBuffer = new List<RecordFrame>();

        private readonly object _recordLock = new object();

        // =============================================================
        // Pipeline float stéréo [-1..1]
        // _audioRing et StereoRingBuffer (int24) supprimés : jamais lus hors ProcessBuffer.
        // =============================================================

        // ── Canal de découplage : thread de capture → ProcessBuffer ──────────
        // Le thread de capture (ExclusiveWasapi, période 10 ms) ne doit faire
        // QUE copier les bytes dans ce canal et revenir à WaitOne immédiatement.
        // ProcessBuffer, qui peut dépasser 10 ms, s'exécute sur _processTask.
        private Channel<(byte[] Buffer, int Count)>? _captureChannel;
        private Task? _processTask;
        private CancellationTokenSource? _processCts;
        // ─────────────────────────────────────────────────────────────────────

        // ── Diagnostic débit audio ─────────────────────────────────────
        private long _diagSamplesReceived;
        private long _diagSamplesExpected;
        private DateTime _diagLastTime = DateTime.MinValue;
        private double _diagDeficitMs;         // déficit cumulé en ms
        private long _diagDropCount; // paquets perdus (canal plein, DropNewest) — reset toutes les 2s
        // ─────────────────────────────────────────────────────────────
        // ── Pipeline FFT (Phase 2: ring buffer + worker + stats + queue) ─────
        private FftPipelineController? _pipeline;


        // ── Logger de diagnostic FFT (F8) ────────────────────────────────────
        // Une ligne CSV par FFT produite. Détecte les discontinuités via HopActuel
        // vs HopAttendu : tout écart > 1ms indique un gap dans les données audio.
        private volatile bool   _diagLogEnabled;
        private StreamWriter?   _diagLogWriter;
        private string?         _diagLogPath;
        private readonly object _diagLogLock    = new object();
        private long            _diagLogFftIndex;        // numéro séquentiel de FFT dans le log
        private long            _diagLogAbsEndPrev = -1; // AbsEnd de la FFT précédente
        private long            _diagDropForLog;         // drops depuis la dernière FFT loguée
        private string          _lastDebugBarText = "";  // dernier texte de la barre debug (pour le log F8)
        // ─────────────────────────────────────────────────────────────────────

        // --- Test source: sine 1 kHz -> écrit dans _pipeline?.Ring à la place de la carte son ---
        private bool _useSineTest = false;
        // --- Test source: signal de référence "mesures" (THD/SINAD/SNR/SFDR) ---
        private bool _useMetricsRefTest = false;
        private CancellationTokenSource? _sineCts;
        private Task? _sineTask;



        private SpectrumFrame? _lastFrameUi; // dernier frame affiché
        private long _uiFramesDrained = 0;
        private readonly System.Diagnostics.Stopwatch _uiSw = System.Diagnostics.Stopwatch.StartNew();
        private double _uiLastRateT = 0;

        // --- FFT Progress Ticker (UX) ---
        // Reset à chaque affichage de courbe, animation autonome sur une durée = hop/Fs
        private System.Windows.Forms.Label? _lblFftTicker;
        private double _fftTickerPeriodSec = 0.0;
        private double _fftTickerStartSec = 0.0;
        private bool _fftTickerEnabled = false;

        // --- Params "source de vérité" pour le ticker (mis à jour à chaque RestartSfftFromUi) ---
        private int _tickerFs = 0;
        private int _tickerN = 0;
        private int _tickerHop = 0;
        private double _tickerOverlap01 = 0.0;

        // Phase : tant qu'on n'a pas affiché la 1ère FFT, on est en priming (période = N/Fs si overlap>0)
        private bool _tickerPriming = true;

        // Throttle affichage UI (10 Hz => 0.10 s)
        private double _uiLastDisplayT = 0.0;
        private const double UiDisplayPeriodSec = 0.10; // 10 Hz

        // NOTE Phase 2: fenêtre d'analyse lue depuis FftPipelineController.Ring

        // \u2500\u2500 Buffers snapshot pré-alloués (supprime 32 MB LOH à chaque tick UI) \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500
        // \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500

        // Pour le cas N > Buffer : accumulation dans une “fenêtre FFT” logique (taille N)


        // --- Fenêtre d'analyse commune Histogram + Spectrum ---
        private int analysisSampleSize = 131072;  // défaut
        private double analysisOverlap = 0.0;     // 0..1
        private int analysisHopSamples = 131072;  // = N*(1-overlap)
        //private bool _updatingSampleSizeUi = false;
        //private long analysisCursorStart = 0;     // index absolu de début de la dernière fenêtre utilisée

        private System.Windows.Forms.Timer? analysisTimer;

        // --- UI -> paramètres SFFT (source unique de vérité) ---



        //     1048576 samples / 44100 Hz = 23,78 s 
        private const int MaxHistogramSeconds = 24; // on garde jusqu' 24 s d'historique

        // --- Mode "Once" pour l'histogramme ---
        //private bool histOnceActive = false;
        //private int histOnceTargetSamples = 0;
        //private int histOnceAccumSamples = 0;

        // Seuils dBFS pour le bargraphe
        private const double DbMin = -80.0;      // plancher d'affichage
        // ── VU-mètre entrée (dans DataViewForm) ───────────────────────────────
        private const int    VuLedCount    = 40;
        private const double VuDbMin       = -80.0;
        private const double VuBlueEnd     = -36.0;
        private const double VuGreenEnd    = -12.0;
        private const double VuOrangeEnd   = -6.0;
        private int    _vuInPctL, _vuInPctR;
        private string _vuInStrL = "-∞ dBFS", _vuInStrR = "-∞ dBFS";
        private System.Windows.Forms.Timer? _vuInTimer;

        private const double DbBlueToGreen = -36.0;
        private const double DbGreenToOrange = -12.0;
        private const double DbOrangeToRed = -6.0;
        private const int LedCount = 40; // nombre de segments "LED" par barre

        // --- Calibration tension ---
        // G et D spares
        private double calibGainLeft = 1.0;
        private double calibOffsetLeft = 0.0;
        private bool calibValidLeft = false;

        private double calibGainRight = 1.0;
        private double calibOffsetRight = 0.0;
        private bool calibValidRight = false;

        // Codes 24 bits mmoriss en continu
        private int lastCodeLeft;
        private int lastCodeRight;

        // Timer de calibration (moyenne)
        private readonly System.Windows.Forms.Timer calibTimer;
        private int calibTicks = 0;
        private int calibTicksTarget = 0;
        private long calibAccum = 0;

        private HistogramControl? histogramControl;
        private SpectrumControl?  spectrumControl;
        private OscilloscopeControl? oscilloscopeControl;
        private bool _oscilloscopeRunning;
        private ToolStripMenuItem? menuColors;
        private ToolStripMenuItem? menuBuffer = null; // removed from menustrip — kept for null-safe FftPipeline stubs

        // Fenêtres non-modales persistantes
        private GeneratorWindow? _generatorWindow;

        private bool _spectrumRunning;
        private bool _histogramRunning;

        // Limitation de frquence pour la mise  jour des textes (pour viter le scintillement)
        private DateTime lastTextDump = DateTime.MinValue;
        private readonly TimeSpan textDumpInterval = TimeSpan.FromMilliseconds(200); // ~2 fps

        private SessionSettings? _loadedSessionSettings;

        private Form? dataFormatsWindow;

        private DataViewForm? _dataViewForm;

        //private bool histogramVisible = false;

        // Cursors menu
        private ToolStripMenuItem? menuCursors;

        private ToolStripMenuItem? menuC1Cursor;
        private ToolStripMenuItem? menuC1Enabled;
        private ToolStripMenuItem? menuC1TrackAuto;
        private ToolStripMenuItem? menuC1TrackLeft;
        private ToolStripMenuItem? menuC1TrackRight;

        private ToolStripMenuItem? menuC2Cursor;
        private ToolStripMenuItem? menuC2Enabled;
        private ToolStripMenuItem? menuC2TrackAuto;
        private ToolStripMenuItem? menuC2TrackLeft;
        private ToolStripMenuItem? menuC2TrackRight;

        private ToolStripMenuItem? menuCursorsSetValue;



        private enum CalStage
        {
            None,
            Positive,
            Negative
        }

        private enum CalChannel
        {
            None,
            Left,
            Right
        }

        private CalStage calibStage = CalStage.None;
        private CalChannel calibChannel = CalChannel.None;

        // Flags pour savoir quoi calibrer
        private bool calibDoLeft = false;
        private bool calibDoRight = false;

        // Variables intermdiaires
        private double calibCodePos = 0.0;
        private double calibCodeNeg = 0.0;
        private double calibVPos = 0.0;
        private double calibVNeg = 0.0;

        // Rafrachissement tension max
        private readonly TimeSpan voltageUpdateInterval = TimeSpan.FromMilliseconds(250);
        private DateTime lastVoltageUpdate = DateTime.MinValue;
        private double displayVoltsLeft = 0.0;
        private double displayVoltsRight = 0.0;

        private double DbToPercent(double db)
        {
            // mappe [DbMin..0] vers [0..100]
            if (db <= DbMin) return 0.0;
            if (db >= 0.0) return 100.0;
            return (db - DbMin) / (0.0 - DbMin) * 100.0;
        }




        //
        // Formatage tension (évite la notation scientifique, garde une mantisse "stable")
        // - V si >= 1 V
        // - mV si < 1 V et >= 1 mV
        // - µV si < 1 mV
        // Mantisse affichée avec ~4 chiffres significatifs et zéros conservés.
        private static string FormatVoltageWithUnit(double volts)
        {
            double a = Math.Abs(volts);

            string unit;
            double v;
            if (a >= 1.0)
            {
                unit = "V";
                v = volts;
            }
            else if (a >= 1e-3)
            {
                unit = "mV";
                v = volts * 1e3;
            }
            else
            {
                unit = "µV";
                v = volts * 1e6;
            }

            double av = Math.Abs(v);

            // 4 chiffres significatifs avec zéros conservés
            string fmt = av >= 100 ? "F1"
                      : av >= 10 ? "F2"
                      : "F3";

            return $"{v.ToString(fmt, CultureInfo.InvariantCulture)} {unit}";
        }

        private class RecordFrame
        {
            public int Left;
            public int Right;
        }


        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            SaveSessionSettingsSilently();
        }
        //
        //
        private readonly int[] _sampleSizeOptions = new[] { 8192, 16384, 32768, 65536, 131072, 262144, 524288, 1048576, 2097152, 4194304 };

        // Sample rates proposés dans le menu Sample Rate
        private readonly int[] _sampleRateOptions = new[]
        {
            44100, 48000, 88200, 96000,
            176400, 192000, 352800, 384000,
            705600, 768000
        };

        // Options REW-style (en samples)
        private readonly int[] _bufferOptions = new[] { 16 * 1024, 32 * 1024, 64 * 1024, 128 * 1024, 256 * 1024, 512 * 1024 };

        private bool _updatingBufferMenuUi = false;

        // Valeur courante (on l’utilisera à l’étape suivante pour le ring-buffer)
        private int analysisBufferSize = 128 * 1024;



        // évite les boucles (menu -> combo -> menu)
        private bool _updatingSampleSizeMenuUi = false;
        //
        private readonly (string Label, double Value)[] _overlapOptions =
{
            ("0%", 0.0),
            ("50%", 0.5),
            ("75%", 0.75),
            ("87.5%", 0.875),
            ("93.75%", 0.9375),
        };

        private bool _updatingOverlapMenuUi = false;

        //


        //

        // Appelé sur le thread audio (à chaque sample)

        // Snapshot des N derniers samples (thread UI)

        //




        public Form1()
        {
            InitializeComponent();

            levelFontRegular = new Font(this.Font, FontStyle.Regular);
            levelFontBold = new Font(this.Font, FontStyle.Bold);

            // Libération propre
            this.FormClosed += (_, __) =>
            {
                levelFontRegular.Dispose();
                levelFontBold.Dispose();
            };


            _dataViewForm = new DataViewForm();
            _dataViewForm.Hide();

            // IMPORTANT : on rebranche l’event du combo (car il n’est plus sur Form1)
            _dataViewForm.comboDisplayMode.SelectedIndexChanged += comboDisplayMode_SelectedIndexChanged;


            InitDataView();

            InitAnalysisUi();
           
            InitColorsMenu();

            //this.WindowState = FormWindowState.Maximized; // Plein cran au dmarrage
            ShowHistogramArea();
            ShowSpectrumArea();
            ShowOscilloscopeArea();

            // Verrouille la largeur minimale  la largeur par dfaut
            this.MinimumSize = new Size(this.Width, this.MinimumSize.Height);

            _minWidth = this.Width;
            _minHeightNoGraphs = this.Height; // minimum = taille initiale

            this.MinimumSize = new Size(_minWidth, _minHeightNoGraphs);

            // Modes possibles pour la valeur mise en avant
            _dataViewForm!.comboDisplayMode.Items.AddRange(new object[]
            {
                "Decimal (24b)",
                "Hex (24 bits)",
                "Binary (24b)",
                "Float",
                "Voltage (Vrms)",
                "Voltage (Vpeak)",
                "Voltage (Vdc)"
            });
            _dataViewForm!.comboDisplayMode.SelectedIndex = 0;
            _dataViewForm!.comboDisplayMode.SelectedIndexChanged += comboDisplayMode_SelectedIndexChanged;

            // Init des horodatages de changements de bits
            var now = DateTime.Now;
            for (int i = 0; i < 24; i++)
            {
                lastChangeLeft[i] = now;
                lastChangeRight[i] = now;
                lastBitsLeft[i] = false;
                lastBitsRight[i] = false;
            }

            // Charger les périphériques d'entrée et sélectionner le premier disponible
            try
            {
                var enumerator = new MMDeviceEnumerator();
                var caps = enumerator.EnumerateAudioEndPoints(NAudio.CoreAudioApi.DataFlow.Capture, NAudio.CoreAudioApi.DeviceState.Active);
                if (caps.Count > 0) _selectedInputDevice = caps[0];
            }
            catch { }

            // Timer calibration
            calibTimer = new System.Windows.Forms.Timer();
            calibTimer.Interval = 50;
            calibTimer.Tick += CalibTimer_Tick;

            // Charger la calibration existante
            LoadCalibration();

            // dBFS markers supprimés (ancien vu-mètre déplacé dans DataViewForm)

            // ── VU-mètre entrée ───────────────────────────────────────────────
            InitInputVuMeter();

            // 
            InitSampleSizeMenu();
            InitOverlapMenu();
            UpdateSampleSizeMenuChecks();
            UpdateOverlapMenuChecks();
            InitBufferMenu();
            UpdateBufferMenuChecks();

            this.FormClosing += Form1_FormClosing;
        }
        //
        private void InitDataView()
        {
            _dataViewForm = new DataViewForm();

            _dataViewForm.StartPosition = FormStartPosition.Manual;

            // ✅ Déplacement des contrôles demandés
            // MainDataFormat = label4 + comboDisplayMode (dans ton Designer, label4 = "Main data format")
            _dataViewForm.AttachControls(
                _dataViewForm!.lblLeftMainTitle, _dataViewForm!.lblRightMainTitle,
                _dataViewForm!.lblLeftMainValue, _dataViewForm!.lblRightMainValue,
                _dataViewForm!.label2, _dataViewForm!.label3,
                _dataViewForm!.lblLeftLevelDV, _dataViewForm!.lblRightLevelDV,
                _dataViewForm!.label4, _dataViewForm!.comboDisplayMode
            );

            // Ne pas afficher au démarrage
            _dataViewForm.RunStopChanged += (_, running) =>
            {
                if (running && capture == null)
                    StartCapture();
                UpdateVuInTimer();
                if (!running)
                {
                    // Si aucun autre onglet n'utilise la capture, on l'arrête
                    // → capture devient null → UpdateTitleBar() efface le texte IN
                    if (!_histogramRunning && !_spectrumRunning && !_oscilloscopeRunning)
                        StopCapture();
                    // Clear DataView display
                    _dataViewForm.lblLeftMainValue.Text  = "—";
                    _dataViewForm.lblRightMainValue.Text = "—";
                    _dataViewForm.lblLeftLevelDV.Text    = "-∞ dBFS";
                    _dataViewForm.lblRightLevelDV.Text   = "-∞ dBFS";
                }
                UpdateTitleBar();
            };
            _dataViewForm.Hide();
        }
        //
        private void menuDataView_Click(object? sender, EventArgs e)
        {
            if (_dataViewForm == null)
                InitDataView();

            // Positionner à gauche de Form1, sous "All data formats"
            if (!_dataViewForm!.Visible)
            {
                var sc = System.Windows.Forms.Screen.FromControl(this).WorkingArea;
                int allFmtH = (dataFormatsWindow != null && !dataFormatsWindow.IsDisposed && dataFormatsWindow.Visible)
                    ? dataFormatsWindow.Height : 0;
                int allFmtX = (dataFormatsWindow != null && !dataFormatsWindow.IsDisposed && dataFormatsWindow.Visible)
                    ? dataFormatsWindow.Left : Math.Max(sc.X, this.Left - _dataViewForm.Width);
                int dvX = Math.Max(sc.X, this.Left - _dataViewForm.Width);
                int dvY = Math.Max(sc.Y, Math.Min(this.Top + allFmtH, sc.Bottom - _dataViewForm.Height));
                _dataViewForm.Location = new System.Drawing.Point(dvX, dvY);
            }
            _dataViewForm.Show();
            _dataViewForm.BringToFront();
        }

        /// <summary>
        /// View > Debug info : affiche/cache la barre de debug interne (sans troncature).
        /// </summary>
        private void menuDebugInfo_Click(object? sender, EventArgs e)
        {
            bool visible = menuDebugInfo.Checked;
            panelDebugInfo.Visible = visible;
            if (!visible)
                lblDebugInfo.Text = "";
            else
                lblDebugInfo.Text = _lastDebugBarText; // affiche immédiatement le dernier texte connu
        }

        //
        private void InitCursorsMenu()
        {
            menuCursors = new ToolStripMenuItem("Cursors")
            {
                Alignment = ToolStripItemAlignment.Right
            };

            // ---------- C1 ----------
            menuC1Cursor = new ToolStripMenuItem("C1 Cursor");

            menuC1Enabled = new ToolStripMenuItem("Enabled") { CheckOnClick = true };
            menuC1Enabled.CheckedChanged += (_, __) =>
            {
                if (spectrumControl != null)
                    spectrumControl.CursorLeftEnabled = menuC1Enabled.Checked;
            };

            menuC1TrackAuto = new ToolStripMenuItem("Track: Auto") { CheckOnClick = true };
            menuC1TrackLeft = new ToolStripMenuItem("Track: Left") { CheckOnClick = true };
            menuC1TrackRight = new ToolStripMenuItem("Track: Right") { CheckOnClick = true };

            void SetC1Track(SpectrumControl.CursorTrackMode mode)
            {
                // radio check
                menuC1TrackAuto.Checked = (mode == SpectrumControl.CursorTrackMode.Auto);
                menuC1TrackLeft.Checked = (mode == SpectrumControl.CursorTrackMode.Left);
                menuC1TrackRight.Checked = (mode == SpectrumControl.CursorTrackMode.Right);

                if (spectrumControl != null)
                    spectrumControl.C1TrackMode = mode;
            }

            menuC1TrackAuto.Click += (_, __) => SetC1Track(SpectrumControl.CursorTrackMode.Auto);
            menuC1TrackLeft.Click += (_, __) => SetC1Track(SpectrumControl.CursorTrackMode.Left);
            menuC1TrackRight.Click += (_, __) => SetC1Track(SpectrumControl.CursorTrackMode.Right);

            menuC1Cursor.DropDownItems.Add(menuC1Enabled);
            menuC1Cursor.DropDownItems.Add(new ToolStripSeparator());
            menuC1Cursor.DropDownItems.Add(menuC1TrackAuto);
            menuC1Cursor.DropDownItems.Add(menuC1TrackLeft);
            menuC1Cursor.DropDownItems.Add(menuC1TrackRight);

            // ---------- C2 ----------
            menuC2Cursor = new ToolStripMenuItem("C2 Cursor");

            menuC2Enabled = new ToolStripMenuItem("Enabled") { CheckOnClick = true };
            menuC2Enabled.CheckedChanged += (_, __) =>
            {
                if (spectrumControl != null)
                    spectrumControl.CursorRightEnabled = menuC2Enabled.Checked;
            };

            menuC2TrackAuto = new ToolStripMenuItem("Track: Auto") { CheckOnClick = true };
            menuC2TrackLeft = new ToolStripMenuItem("Track: Left") { CheckOnClick = true };
            menuC2TrackRight = new ToolStripMenuItem("Track: Right") { CheckOnClick = true };

            void SetC2Track(SpectrumControl.CursorTrackMode mode)
            {
                // radio check
                menuC2TrackAuto.Checked = (mode == SpectrumControl.CursorTrackMode.Auto);
                menuC2TrackLeft.Checked = (mode == SpectrumControl.CursorTrackMode.Left);
                menuC2TrackRight.Checked = (mode == SpectrumControl.CursorTrackMode.Right);

                if (spectrumControl != null)
                    spectrumControl.C2TrackMode = mode;
            }

            menuC2TrackAuto.Click += (_, __) => SetC2Track(SpectrumControl.CursorTrackMode.Auto);
            menuC2TrackLeft.Click += (_, __) => SetC2Track(SpectrumControl.CursorTrackMode.Left);
            menuC2TrackRight.Click += (_, __) => SetC2Track(SpectrumControl.CursorTrackMode.Right);

            menuC2Cursor.DropDownItems.Add(menuC2Enabled);
            menuC2Cursor.DropDownItems.Add(new ToolStripSeparator());
            menuC2Cursor.DropDownItems.Add(menuC2TrackAuto);
            menuC2Cursor.DropDownItems.Add(menuC2TrackLeft);
            menuC2Cursor.DropDownItems.Add(menuC2TrackRight);

            // ---------- Set Value (Étape 4) ----------
            menuCursorsSetValue = new ToolStripMenuItem("Set Value...");
            menuCursorsSetValue.Click += (_, __) =>
            {
                if (spectrumControl == null) return;

                // valeurs actuelles
                double? c1 = spectrumControl.TryGetCursor1FrequencyHz(out var f1) ? f1 : (double?)null;
                double? c2 = spectrumControl.TryGetCursor2FrequencyHz(out var f2) ? f2 : (double?)null;

                using var dlg = new CursorSetValueDialog(
                    c1Enabled: spectrumControl.CursorLeftEnabled,
                    c2Enabled: spectrumControl.CursorRightEnabled,
                    c1CurrentHz: c1,
                    c2CurrentHz: c2,
                    getPeakC1Hz: () =>
                    {
                        return spectrumControl.TryFindPeakFrequencyForCursor1(out var p) ? p : (double?)null;
                    },
                    getPeakC2Hz: () =>
                    {
                        return spectrumControl.TryFindPeakFrequencyForCursor2(out var p) ? p : (double?)null;
                    }
                );

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    if (dlg.C1Hz.HasValue)
                        spectrumControl.SetCursor1FrequencyHz(dlg.C1Hz.Value);

                    if (dlg.C2Hz.HasValue)
                        spectrumControl.SetCursor2FrequencyHz(dlg.C2Hz.Value);
                }
            };


            // Assemblage menu
            menuCursors.DropDownItems.Add(menuC1Cursor);
            menuCursors.DropDownItems.Add(menuC2Cursor);
            menuCursors.DropDownItems.Add(new ToolStripSeparator());
            menuCursors.DropDownItems.Add(menuCursorsSetValue);

            // Valeurs par défaut (radio)
            menuC1TrackAuto.Checked = true;
            menuC2TrackAuto.Checked = true;

            // Ajouter au menu principal (top-level)
            menuStrip1.Items.Add(menuCursors);
        }

        //

        // --------------------------------------------------------------------
        // THD : logique + persistance (UI lives in SpectrumControl)
        // --------------------------------------------------------------------

        private void ApplyThdEnabled(bool enabled)
        {
            _session.Spectrum.EnableThdMeasures = enabled;

            if (spectrumControl != null)
            {
                spectrumControl.SetThdMeasuresEnabled(enabled);
                spectrumControl.SetThdEnabledUi(enabled);
            }

            if (!SessionSettingsManager.Save(_session, out string? err) && !string.IsNullOrWhiteSpace(err))
                MessageBox.Show(this, err, "Save settings failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }


        private void ShowThdSettings()
        {
            try
            {
                int sr = currentFormat != null ? currentFormat.SampleRate : 0;

                using (var dlg = new ThdSettingsDialog(_session.Spectrum.Thd, sr))
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK)
                        return;

                    _session.Spectrum.Thd = dlg.Result;

                    if (spectrumControl != null)
                        spectrumControl.ApplyThdSettings(_session.Spectrum.Thd);

                    if (!SessionSettingsManager.Save(_session, out string? err) && !string.IsNullOrWhiteSpace(err))
                        MessageBox.Show(this, err, "Save settings failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Crash in THD setting", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //



        //
        // --- Colors menu (FFT/Overlay/PeakHold/Grid/Background) ---
        private void InitColorsMenu()
        {
            // ── Ordre final : Devices|Generator|Overlap|Sample size|Colors|View|Export|Tools|Help
            // Designer a chargé : [0]Devices [1]Overlap [2]SampleSize [3]View [4]Export [5]Help

            // Generator → index 1  (entre Devices et Overlap)
            var menuGenerator = new ToolStripMenuItem("Generator");
            menuGenerator.Click += menuGenerator_Click;
            menuStrip1.Items.Insert(1, menuGenerator);
            // Après insert : [0]Devices [1]Generator [2]Overlap [3]SampleSize [4]View [5]Export [6]Help

            // Colors → index 4  (entre SampleSize et View)
            menuColors = new ToolStripMenuItem("Colors");
            menuColors.Click += (_, __) => ShowColorsDialog();
            menuStrip1.Items.Insert(4, menuColors);
            // Après insert : [0]Devices [1]Generator [2]Overlap [3]SampleSize [4]Colors [5]View [6]Export [7]Help

            // Export CSV (sous-menu Export)
            var menuExportCsv = new ToolStripMenuItem("Export data to CSV…");
            menuExportCsv.Click += menuExportCsv_Click;
            exportToolStripMenuItem.DropDownItems.Add(menuExportCsv);

            // Tools → index 7  (entre Export et Help)
            var menuTools       = new ToolStripMenuItem("Tools");
            var menuDataRecord  = new ToolStripMenuItem("Data Record (1s)");
            var menuCalibrateIn = new ToolStripMenuItem("Calibrate Input Device");
            menuDataRecord.Click  += (_, _) => btnRecord_Click(menuDataRecord, EventArgs.Empty);
            menuCalibrateIn.Click += (_, _) => btnCalV_Click(menuCalibrateIn, EventArgs.Empty);
            menuTools.DropDownItems.AddRange(new ToolStripItem[] { menuDataRecord, menuCalibrateIn });
            menuStrip1.Items.Insert(7, menuTools);
            // Résultat final : [0]Devices [1]Generator [2]Overlap [3]SampleSize [4]Colors [5]View [6]Export [7]Tools [8]Help
        }

        private void ShowColorsDialog()
        {
            if (spectrumControl == null)
                return;

            // Current colors from the control
            var current = spectrumControl.GetSpectrumColorSettingsSnapshot();

            // Persisted defaults (used when clicking "Default" in the dialog)
            var persistedDefaults =
                _loadedSessionSettings?.Spectrum?.DefaultColors
                ?? _session?.Spectrum?.DefaultColors
                ?? SpectrumColorSettings.Defaults();

            using var dlg = new SpectrumColorsDialog(current, persistedDefaults);
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            spectrumControl.ApplySpectrumColorSettings(dlg.SelectedColors);

            // Propagation des couleurs à l'onglet Histogramme (fond, grilles, labels, barres L/R)
            histogramControl?.ApplySpectrumColorSettings(dlg.SelectedColors);

            // Propagation des couleurs à l'oscilloscope
            oscilloscopeControl?.ApplySpectrumColorSettings(dlg.SelectedColors);

            // Keep settings in sync (used when re-applying settings later + for persistence)
            if (_loadedSessionSettings != null)
            {
                _loadedSessionSettings.Spectrum.Colors = dlg.SelectedColors;

                if (dlg.DefaultsChanged)
                    _loadedSessionSettings.Spectrum.DefaultColors = dlg.SelectedDefaults;
            }

            // Also keep the always-present in-memory session in sync
            if (_session.Spectrum != null)
            {
                _session.Spectrum.Colors = dlg.SelectedColors;
                if (dlg.DefaultsChanged)
                    _session.Spectrum.DefaultColors = dlg.SelectedDefaults;
            }

            // Persist via existing snapshot mechanism
            SaveSessionSettingsSilently();
        }

        //        
        //
        //
        //
       
        //
        private void ApplyAnalysisSettings(SessionSettings s)
        {
            if (s == null) return;

            // IMPORTANT: ordre
            // 1) Sample size (source d'autorité pour FFT + histogram)
            SetAnalysisSampleSize(s.Analysis.SampleSize);

            // 2) Overlap (dépend du sample size car hop = sample*(1-overlap))
            SetAnalysisOverlap(s.Analysis.Overlap);

            // 3) Buffer (sans dépendance forte, on le fait après)
            SetAnalysisBufferSize(s.Analysis.BufferSize);

            // 4) Sample rate demandé (0 = Auto)
            int savedSr = s.Analysis.RequestedSampleRate;
            if (savedSr > 0)
                _requestedCaptureFormat = new NAudio.Wave.WaveFormatExtensible(savedSr, 32, 2);
            else
                _requestedCaptureFormat = null;   // Auto

            // Si tu as des fonctions de refresh du menu, appelle-les ici
            UpdateSampleSizeMenuChecks();
            UpdateOverlapMenuChecks();
            UpdateBufferMenuChecks();

            if (histogramControl != null)
            {
                histogramControl.SetGlobalSampleSize(analysisSampleSize);
                histogramControl.UpdateHopSamples(analysisHopSamples);
            }

            if (spectrumControl != null)
            {
                spectrumControl.SetGlobalSampleSize(analysisSampleSize);
                spectrumControl.UpdateHopSamples(analysisHopSamples);
            }

        }

        //
        private void LoadUiSettingsFromSettingsFiles()
        {

            // 1) Essai du nouveau fichier SessionSettings.json
            var session = SessionSettingsManager.LoadOrNull();
            if (session != null && session.Ui != null)
            {
                _loadedSessionSettings = session;
                ApplyUiSettings(session.Ui);
                return;
            }
        }

        private void ApplyUiSettings(UiSettings ui)
        {
            // 1) IN / OUT
            captureOutput = ui.CaptureOutput;

            // 2) Device ID — restaurer depuis la liste des périphériques d'entrée
            if (!string.IsNullOrEmpty(ui.DeviceId))
            {
                try
                {
                    var en = new MMDeviceEnumerator();
                    var flow = captureOutput
                        ? NAudio.CoreAudioApi.DataFlow.Render
                        : NAudio.CoreAudioApi.DataFlow.Capture;
                    foreach (var d in en.EnumerateAudioEndPoints(flow, NAudio.CoreAudioApi.DeviceState.Active))
                    {
                        if (d.ID == ui.DeviceId) { _selectedInputDevice = d; break; }
                    }
                }
                catch { }
            }
            // 3) Output device — restaurer ou auto-sélectionner le premier disponible
            try
            {
                var en = new MMDeviceEnumerator();
                var renders = en.EnumerateAudioEndPoints(
                    NAudio.CoreAudioApi.DataFlow.Render,
                    NAudio.CoreAudioApi.DeviceState.Active);
                if (!string.IsNullOrEmpty(ui.OutputDeviceId))
                {
                    foreach (var d in renders)
                        if (d.ID == ui.OutputDeviceId) { _selectedOutputDevice = d; break; }
                }
                // Si aucun périphérique restauré (premier démarrage ou ID disparu),
                // prendre le premier périphérique de rendu disponible
                if (_selectedOutputDevice == null && renders.Count > 0)
                    _selectedOutputDevice = renders[0];
                // Restaurer le format exclusif si SR était configuré
                if (_selectedOutputDevice != null && ui.RequestedOutputSR > 0)
                    _requestedOutputFormat = new WaveFormatExtensible(ui.RequestedOutputSR, 32, 2);
            }
            catch { }

            // 4) Main Data Format
            if (!string.IsNullOrEmpty(ui.MainDataFormat))
            {
                int idx = _dataViewForm!.comboDisplayMode.Items.IndexOf(ui.MainDataFormat);
                if (idx >= 0)
                    _dataViewForm!.comboDisplayMode.SelectedIndex = idx;
            }
        }
        //
        private void ApplyLoadedSessionSettingsToExistingControls()
        {
            if (_loadedSessionSettings == null)
                return;

            if (histogramControl != null)
                histogramControl.ApplyHistogramSettings(_loadedSessionSettings.Histogram);

            if (spectrumControl != null)
            {
                spectrumControl.ApplySpectrumSettings(_loadedSessionSettings.Spectrum);

                // Synchroniser les couleurs de l'histogramme avec celles du spectre
                var loadedColors = _loadedSessionSettings.Spectrum?.Colors ?? SpectrumColorSettings.Defaults();
                histogramControl?.ApplySpectrumColorSettings(loadedColors);
                oscilloscopeControl?.ApplySpectrumColorSettings(loadedColors);
            }

            oscilloscopeControl?.ApplySettings(_loadedSessionSettings.Oscilloscope);
        }

        //
        private SessionSettings BuildSessionSettingsSnapshot()
        {
            var s = new SessionSettings
            {
                Version = 1
            };

            // UI
            s.Ui.DeviceId = _selectedInputDevice?.ID;
            s.Ui.CaptureOutput = captureOutput;
            s.Ui.MainDataFormat = _dataViewForm!.comboDisplayMode.SelectedItem as string;
            // Périphérique de sortie générateur
            s.Ui.OutputDeviceId    = _selectedOutputDevice?.ID;
            s.Ui.RequestedOutputSR = _requestedOutputFormat?.SampleRate ?? 0;

            // Analysis (valeurs actuelles, même si on ne les applique pas encore)
            s.Analysis.BufferSize  = analysisBufferSize;
            s.Analysis.Overlap     = analysisOverlap;
            s.Analysis.SampleSize  = analysisSampleSize;
            s.Analysis.RequestedSampleRate = _requestedCaptureFormat?.SampleRate ?? 0;

            if (histogramControl != null)
                s.Histogram = histogramControl.GetHistogramSettingsSnapshot();

            if (spectrumControl != null)
            {
                s.Spectrum = spectrumControl.GetSpectrumSettingsSnapshot();

                // Preserve persisted spectrum default colors (used by SpectrumColorsDialog)
                var persistedDefaults = _loadedSessionSettings?.Spectrum?.DefaultColors ?? _session?.Spectrum?.DefaultColors;
                if (persistedDefaults != null)
                    s.Spectrum.DefaultColors = persistedDefaults;
            }

            // Histogram / Spectrum : on remplira à l'étape suivante (pour l'instant defaults)

            // ── Calibration de niveau de sortie du générateur (persistance) ──
            s.GenCalValidLeft           = _session.GenCalValidLeft;
            s.GenCalVrmsFullScaleLeft   = _session.GenCalVrmsFullScaleLeft;
            s.GenCalValidRight          = _session.GenCalValidRight;
            s.GenCalVrmsFullScaleRight  = _session.GenCalVrmsFullScaleRight;

            // ── Settings oscilloscope ──────────────────────────────────────────
            if (oscilloscopeControl != null)
                s.Oscilloscope = oscilloscopeControl.GetSettingsSnapshot();
            else
                s.Oscilloscope = _session.Oscilloscope ?? new OscilloscopeSettings();

            return s;
        }

        private void SaveSessionSettingsSilently()
        {
            var snap = BuildSessionSettingsSnapshot();
            SessionSettingsManager.Save(snap, out _);
            // volontairement silencieux : pas de MessageBox
        }

        //
        private string GroupBits(string bits, int groupSize = 4)
        {
            var sb = new StringBuilder(bits.Length + bits.Length / groupSize);

            for (int i = 0; i < bits.Length; i++)
            {
                if (i > 0 && i % groupSize == 0)
                    sb.Append(' ');

                sb.Append(bits[i]);
            }

            return sb.ToString();
        }

        private string FormatRow(string label, string value)
        {
            // 14 caractres pour la colonne "label",  ajuster si besoin
            const int labelWidth = 14;
            if (label.Length > labelWidth)
                label = label.Substring(0, labelWidth);

            string paddedLabel = label.PadRight(labelWidth);

            return $"{paddedLabel} : {value}";
        }
        //
        // ================================================================
        // NÉGOCIATION DU FORMAT EXCLUSIF (inspiré de SRatetest2)
        // ================================================================

        /// <summary>
        /// Teste, pour un SR donné, les formats courants (32/24/16 bits, plain et extensible)
        /// et retourne le premier accepté par le driver en mode Exclusif.
        /// Retourne null si aucun n'est supporté.
        /// </summary>
        private static WaveFormat? FindSupportedExclusiveFormat(MMDevice device, int sampleRate)
        {
            var ac = device.AudioClient;
            var mode = AudioClientShareMode.Exclusive;

            // Float32 stéréo en premier : certains drivers déclarent PCM Int32 mais
            // livrent des données Float32, causant saturation à ±0.5.
            // IMPORTANT : pas de format mono — ProcessBuffer exige 2 canaux.
            WaveFormat[] candidates = new WaveFormat[]
            {
                WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2), // Float32 stéréo (préféré)
                new WaveFormat(sampleRate, 32, 2),
                new WaveFormatExtensible(sampleRate, 32, 2),
                new WaveFormat(sampleRate, 24, 2),
                new WaveFormatExtensible(sampleRate, 24, 2),
                new WaveFormat(sampleRate, 16, 2),
                new WaveFormatExtensible(sampleRate, 16, 2),
            };

            foreach (var wf in candidates)
            {
                try
                {
                    if (ac.IsFormatSupported(mode, wf))
                        return wf;
                }
                catch { /* certains drivers lèvent une exception au lieu de retourner false */ }
            }

            return null;
        }

        // ================================================================
        // FIN NÉGOCIATION FORMAT
        // ================================================================
        //
        // ================================================================
        // SAMPLE RATE MENU (Exclusive Mode)
        // ================================================================

        /// <summary>
        /// Vérifie si le format demandé est supporté par le driver en mode Exclusif.
        /// Retourne false en cas d'erreur (driver non compatible, device absent, etc.)
        /// </summary>
        private static bool IsFormatSupportedExclusive(MMDevice device, WaveFormat format)
        {
            try
            {
                var client = device.AudioClient;
                return client.IsFormatSupported(AudioClientShareMode.Exclusive, format);
            }
            catch
            {
                return false;
            }
        }

        // ================================================================
        // FIN SAMPLE RATE MENU
        // ================================================================
        //
        private void UpdateDeviceSummary()
        {
            //if (lblDeviceSummary == null)
            //    return;

            if (_selectedInputDevice is not MMDevice dev)
                return;

            // Type : IN / OUT
            string typeText = captureOutput ? "Output" : "Input";

            // On essaye d'avoir un format pertinent :
            WaveFormat? fmt = currentFormat;
            if (fmt == null)
            {
                try
                {
                    fmt = dev.AudioClient.MixFormat;
                }
                catch
                {
                    fmt = null;
                }
            }

            string line1 = dev.FriendlyName; // ex: "Ligne (MKADC1)"
            string line2 = $"{typeText}";

            string line3 = "";
            string line4 = "";

            if (fmt != null)
            {
                double srKHz = fmt.SampleRate / 1000.0;
                line3 = $"Format: {srKHz:0.###} kHz {fmt.BitsPerSample} bits";

                if (lblFormatInfo != null)
                    lblFormatInfo.Text = $"Format: {srKHz:0.###} kHz {fmt.BitsPerSample} bits";

                string enc;
                if (fmt.Encoding == WaveFormatEncoding.IeeeFloat)
                    enc = "IEEE Float";
                else if (fmt.Encoding == WaveFormatEncoding.Pcm)
                    enc = "PCM";
                else
                    enc = fmt.Encoding.ToString();

                line4 = enc;
            }
            else
            {
                line3 = "(unknown format)";
            }

            //lblDeviceSummary.Text = string.Join(Environment.NewLine,
            //    line1,
            //    line2,
            //    line3,
            //    line4
            //);
        }

        //
        private void menuDevices_Click(object? sender, EventArgs e)
        {
            // Utilise les listes mises en cache lors du scan au démarrage → instantané
            var inputs  = SplashScreen.CachedInputs;
            var outputs = SplashScreen.CachedOutputs;
            if (inputs.Count == 0 && outputs.Count == 0)
            {
                MessageBox.Show(this, "No audio devices found.", "Devices",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var dlg = new DevicesDialog(
                inputDevices:        inputs,
                outputDevices:       outputs,
                currentInput:        _selectedInputDevice,
                currentOutput:       _selectedOutputDevice,
                sampleRateOptions:   _sampleRateOptions,
                currentInputFormat:  _requestedCaptureFormat as WaveFormatExtensible,
                currentOutputFormat: _requestedOutputFormat,
                onInfosClick:        ShowDeviceInfoDialog,
                currentBufferSize:   analysisBufferSize);

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                // Apply buffer size if changed
                if (dlg.SelectedBufferSize != analysisBufferSize)
                    SetAnalysisBufferSize(dlg.SelectedBufferSize);

                bool deviceChanged = dlg.SelectedInputDevice?.ID != _selectedInputDevice?.ID;
                bool srChanged     = (dlg.SelectedInputFormat?.SampleRate ?? 0) != (_requestedCaptureFormat?.SampleRate ?? 0);

                _selectedInputDevice    = dlg.SelectedInputDevice;
                _selectedOutputDevice   = dlg.SelectedOutputDevice;
                _requestedCaptureFormat = dlg.SelectedInputFormat;
                _requestedOutputFormat  = dlg.SelectedOutputFormat;

                if (deviceChanged || srChanged)
                {
                    currentFormat = null;
                    UpdateDeviceInfo();
                    UpdateDeviceSummary();

                    // Si la capture est active, la redémarrer avec les nouveaux paramètres
                if (capture != null)
                    {
                        StopCapture();
                        StartCapture();
                    }
                }
            }
        }

        private void menuGenerator_Click(object? sender, EventArgs e)
        {
            if (_generatorWindow == null || _generatorWindow.IsDisposed)
            {
                _generatorWindow = new GeneratorWindow(
                    getOutputDevice:    () => _selectedOutputDevice,
                    getInputDevice:     () => _selectedInputDevice,
                    getOutputFormat:    () => _requestedOutputFormat,
                    getAnalyzerSR:      () => currentFormat?.SampleRate ?? 0,
                    getAnalyzerN:       () => spectrumControl?.CurrentFftSize ?? 0,
                    onLoopbackRequest:  HandleLoopbackRequest,
                    settings:           _session,
                    saveSettings:       SaveSessionSettingsSilently,
                    onRunStateChanged:  UpdateTitleBar);

                // Positionner à côté de la fenêtre principale
                _generatorWindow.Location = new System.Drawing.Point(
                    Location.X + Width + 4, Location.Y);
            }

            if (_generatorWindow.WindowState == FormWindowState.Minimized)
                _generatorWindow.WindowState = FormWindowState.Normal;

            _generatorWindow.Show(this);
        }

        private void ShowDeviceInfoDialog()
        {
            try
            {
                using var dlg = new DeviceInfoDialog(
                    inputDevice:  _selectedInputDevice,
                    outputDevice: _selectedOutputDevice,
                    activeFormat: currentFormat,
                    isCapturing:  capture != null,
                    isLoopback:   captureOutput,
                    srOptions:    _sampleRateOptions);
                dlg.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Device info", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void menuShowAllFormats_Click(object? sender, EventArgs e)
        {
            if (dataFormatsWindow != null && !dataFormatsWindow.IsDisposed)
            {
                dataFormatsWindow.Show();
                dataFormatsWindow.BringToFront();
                RefreshAllFormatsWindow();
                return;
            }

            dataFormatsWindow = BuildAllFormatsWindow();
            // À gauche de Form1, aligné sur le haut de Form1
            var sc1 = System.Windows.Forms.Screen.FromControl(this).WorkingArea;
            int fmtX = Math.Max(sc1.X, this.Left - dataFormatsWindow.Width);
            int fmtY = Math.Max(sc1.Y, Math.Min(this.Top, sc1.Bottom - dataFormatsWindow.Height));
            dataFormatsWindow.Location = new System.Drawing.Point(fmtX, fmtY);
            dataFormatsWindow.Show(this);
        }

        private Form BuildAllFormatsWindow()
        {
            var dlg = new Form
            {
                Text            = "All data formats",
                StartPosition   = FormStartPosition.Manual,
                Size            = new Size(460, 330),
                FormBorderStyle = FormBorderStyle.FixedSingle,
                MinimizeBox     = false,
                MaximizeBox     = false,
                ShowIcon        = false,
                Font            = new Font("Segoe UI", 9f),
                BackColor       = SystemColors.Window
            };

            // ── Layout 2 colonnes ─────────────────────────────────────────────
            var table = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 1,
                Padding     = new Padding(10, 8, 10, 8)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            dlg.Controls.Add(table);

            var flowL = BuildChannelRows("LEFT",  out var valsL);
            var flowR = BuildChannelRows("RIGHT", out var valsR);
            _fmtValuesL = valsL;
            _fmtValuesR = valsR;
            table.Controls.Add(flowL, 0, 0);
            table.Controls.Add(flowR, 1, 0);

            RefreshAllFormatsWindow();

            // Refresh à chaque tick audio (throttlé via _lastAllFormatsDumpL change)
            dlg.FormClosing += (_, e2) =>
            {
                if (e2.CloseReason == CloseReason.UserClosing)
                { e2.Cancel = true; dlg.Hide(); }
            };
            return dlg;
        }

        private FlowLayoutPanel BuildChannelFlow(string title)
        {
            var flow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoScroll    = false,
                Padding       = new Padding(4)
            };

            // Title
            flow.Controls.Add(new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = SystemColors.HotTrack,
                AutoSize  = false, Width = 260, Height = 28,
                Margin    = new Padding(0, 0, 0, 4)
            });
            // Separator
            flow.Controls.Add(new Panel
            {
                Width = 260, Height = 1,
                BackColor = Color.FromArgb(180, 210, 240),
                Margin    = new Padding(0, 0, 0, 6)
            });
            return flow;
        }

        // ── All formats window — labels créés une fois, mis à jour par Text seulement ──
        private static readonly Font _fmtLabelFont   = new("Segoe UI", 8.5f);
        private static readonly Font _fmtValueFont   = new("Segoe UI", 8.5f);
        private static readonly Font _fmtBinFont     = new("Courier New", 8f);
        private static readonly Font _fmtSectionFont = new("Segoe UI Semibold", 8.5f);

        // Value labels — [0=Float,1=Dec,2=Hex,3=BinMSB,4=Vrms,5=Vpeak,6=Vdc,7=BinLSB]
        private Label[]? _fmtValuesL, _fmtValuesR;

        private void RefreshAllFormatsWindow()
        {
            if (_fmtValuesL == null || _fmtValuesR == null) return;

            // ── Gather values (no UI work) ────────────────────────────────
            string fltL = lastLeftFloat.ToString("G9"),   fltR = lastRightFloat.ToString("G9");
            string decL = lastLeft24.ToString(),           decR = lastRight24.ToString();
            string hexL = lastLeftHex24,                   hexR = lastRightHex24;
            string binL = lastLeftBits,                    binR = lastRightBits;

            double? vrmsL=null, vpkL=null, vdcL=null, vrmsR=null, vpkR=null, vdcR=null;
            bool calL=false, calR=false;
            if (spectrumControl != null)
            {
                calL = spectrumControl.VoltageCalibrationValidLeft;
                calR = spectrumControl.VoltageCalibrationValidRight;
                spectrumControl.GetLastVoltages(out vrmsL,out vpkL,out vdcL,out vrmsR,out vpkR,out vdcR);
            }
            string V(bool cal, double? v) => cal && v.HasValue ? FormatVoltageWithUnit(v.Value) : "Uncalibrated";
            Color  C(bool cal) => cal ? SystemColors.ControlText : SystemColors.GrayText;

            // ── Update LEFT labels (Text only — no layout) ────────────────
            // Split binary "xxxx xxxx xxxx xxxx xxxx xxxx" → MSB/LSB 12 bits each
            static (string msb, string lsb) SplitBin(string b)
            {
                // Format: "xxxx xxxx xxxx xxxx xxxx xxxx" (4+1+4+1+4+1+4+1+4+1+4 = 29 chars)
                // First 14 chars = bits 23-12 (MSB), last 14 = bits 11-0 (LSB)
                if (b.Length >= 29)
                    return (b[..14].TrimEnd(), b[15..].TrimEnd());
                return (b, "");
            }
            var (msbL, lsbL) = SplitBin(binL);
            var (msbR, lsbR) = SplitBin(binR);

            _fmtValuesL[0].Text = fltL;  _fmtValuesR[0].Text = fltR;
            _fmtValuesL[1].Text = decL;  _fmtValuesR[1].Text = decR;
            _fmtValuesL[2].Text = hexL;  _fmtValuesR[2].Text = hexR;
            _fmtValuesL[3].Text = msbL;  _fmtValuesR[3].Text = msbR;  // MSB bits 23-12
            _fmtValuesL[4].Text = lsbL;  _fmtValuesR[4].Text = lsbR;  // LSB bits 11-0
            _fmtValuesL[5].Text = V(calL, vrmsL); _fmtValuesR[5].Text = V(calR, vrmsR);
            _fmtValuesL[6].Text = V(calL, vpkL);  _fmtValuesR[6].Text = V(calR, vpkR);
            _fmtValuesL[7].Text = V(calL, vdcL);  _fmtValuesR[7].Text = V(calR, vdcR);
            // Voltage color
            for (int i = 5; i <= 7; i++)
            { _fmtValuesL[i].ForeColor = C(calL); _fmtValuesR[i].ForeColor = C(calR); }
        }

        private FlowLayoutPanel BuildChannelRows(string title, out Label[] valueLabels)
        {
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, AutoScroll = false, Padding = new Padding(4)
            };
            valueLabels = new Label[8];

            // Title + separator
            flow.Controls.Add(new Label {
                Text = title, Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = SystemColors.HotTrack,
                AutoSize = false, Width = 260, Height = 28, Margin = new Padding(0,0,0,4)
            });
            flow.Controls.Add(new Panel {
                Width = 260, Height = 1,
                BackColor = Color.FromArgb(180, 210, 240), Margin = new Padding(0,0,0,6)
            });

            // Digital section
            AddFmtSection(flow, "Digital");
            valueLabels[0] = AddFmtRow(flow, "Float",         mono: false);
            valueLabels[1] = AddFmtRow(flow, "Decimal (24b)", mono: false);
            valueLabels[2] = AddFmtRow(flow, "Hex (24 bits)", mono: false, color: Color.FromArgb(0,100,180));
            // Binary : 2 lignes MSB (bits 23-12) / LSB (bits 11-0)
            valueLabels[3] = AddFmtRow(flow, "Bin MSB",       mono: true,  color: Color.FromArgb(0,130,0));
            valueLabels[4] = AddFmtRow(flow, "Bin LSB",       mono: true,  color: Color.FromArgb(0,130,0));

            // Voltage section
            AddFmtSection(flow, "Voltage");
            valueLabels[5] = AddFmtRow(flow, "Vrms",  mono: false, color: SystemColors.GrayText);
            valueLabels[6] = AddFmtRow(flow, "Vpeak", mono: false, color: SystemColors.GrayText);
            valueLabels[7] = AddFmtRow(flow, "Vdc",   mono: false, color: SystemColors.GrayText);

            return flow;
        }

        private static void AddFmtSection(FlowLayoutPanel flow, string title)
        {
            flow.Controls.Add(new Panel { Width = 260, Height = 5, Margin = new Padding(0) });
            flow.Controls.Add(new Label {
                Text = title.ToUpperInvariant(), Font = _fmtSectionFont,
                ForeColor = Color.FromArgb(0, 90, 160),
                AutoSize = false, Width = 260, Height = 18, Margin = new Padding(0)
            });
            flow.Controls.Add(new Panel {
                Width = 260, Height = 1,
                BackColor = Color.FromArgb(180, 210, 240), Margin = new Padding(0, 0, 0, 3)
            });
        }

        // Binary split helpers ─────────────────────────────────────────────
        // Input format: "xxxx xxxx xxxx xxxx xxxx xxxx" (24 bits with spaces)
        private static string BinMsb(string bin)
        {
            // Take first 12 significant bits (chars 0..14 of "xxxx xxxx xxxx xxxx xxxx xxxx")
            // Format from FormatBitsWithStability: groups of 4 separated by spaces
            // 24 bits = "b23b22b21b20 b19b18b17b16 b15b14b13b12 b11b10b9b8 b7b6b5b4 b3b2b1b0"
            if (string.IsNullOrEmpty(bin)) return "—";
            var parts = bin.Split(' ');
            if (parts.Length < 6) return bin;
            return parts[0] + " " + parts[1] + " " + parts[2]; // MSB: bits 23-12
        }
        private static string BinLsb(string bin)
        {
            if (string.IsNullOrEmpty(bin)) return "—";
            var parts = bin.Split(' ');
            if (parts.Length < 6) return "";
            return parts[3] + " " + parts[4] + " " + parts[5]; // LSB: bits 11-0
        }

        // Binary row: two value labels stacked (MSB + LSB indented)
        private static Label AddFmtRowBin(FlowLayoutPanel flow, string label, out Label lsbLabel)
        {
            var row = new Panel { Width = 260, Height = 36, Margin = new Padding(0, 1, 0, 1) };
            row.Controls.Add(new Label {
                Text = "Binary (24b) :", Font = _fmtLabelFont,
                ForeColor = SystemColors.GrayText,
                Location = new Point(0, 2), Width = 90, Height = 16
            });
            var msbLabel = new Label {
                Text = "—", Font = _fmtBinFont,
                ForeColor = Color.FromArgb(0, 130, 0),
                Location = new Point(94, 1), Width = 162, Height = 16
            };
            lsbLabel = new Label {
                Text = "", Font = _fmtBinFont,
                ForeColor = Color.FromArgb(0, 130, 0),
                Location = new Point(94, 18), Width = 162, Height = 16
            };
            row.Controls.Add(msbLabel);
            row.Controls.Add(lsbLabel);
            flow.Controls.Add(row);
            return msbLabel;
        }

        // Returns the value Label (kept for update)
        private static Label AddFmtRow(FlowLayoutPanel flow, string label,
            bool mono = false, Color? color = null)
        {
            var row = new Panel { Width = 260, Height = 18, Margin = new Padding(0, 1, 0, 1) };
            row.Controls.Add(new Label {
                Text = label + " :", Font = _fmtLabelFont,
                ForeColor = SystemColors.GrayText,
                Location = new Point(0, 2), Width = 90, Height = 16
            });
            var valLbl = new Label {
                Text = "—", Font = mono ? _fmtBinFont : _fmtValueFont,
                ForeColor = color ?? SystemColors.ControlText,
                Location = new Point(94, 2), Width = 162, Height = 16,
                AutoEllipsis = true
            };
            row.Controls.Add(valLbl);
            flow.Controls.Add(row);
            return valLbl;
        }

        //
        //
        private void menuAbout_Click(object? sender, EventArgs e)
        {
            using var about = new SplashScreen(aboutMode: true);
            about.ShowDialog(this);
        }
        //
        private void comboDisplayMode_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateMainDisplayFromLastValues();
        }

        private void menuOpenCal_Click(object? sender, EventArgs e)
        {
            string path = GetCalibrationFilePath();
            if (!File.Exists(path))
            {
                MessageBox.Show("Aucun fichier de calibration trouv.",
                                "Open calibration file",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                return;
            }

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Impossible d'ouvrir le fichier de calibration : " + ex.Message,
                                "Open calibration file",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }
        //
        //
        private void menuOpenRecordsFolder_Click(object? sender, EventArgs e)
        {
            try
            {
                string folder = GetRecordsFolderPath();

                Process.Start(new ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Impossible d'ouvrir le dossier d'enregistrements : " + ex.Message,
                    "Open records folder",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        //
        //
        //
        /// <summary>
        /// Démarre la capture audio + pipeline d'analyse si elle n'est pas déjà active.
        /// Remplace l'ancien bouton READ.
        /// </summary>

        // ═══════════════════════════════════════════════════════════════════════
        // LOGGER DE DIAGNOSTIC FFT  (F8 pour activer/désactiver)
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Active ou désactive le logger CSV (F8). Une ligne par FFT produite.
        /// Fichier créé dans Mes Documents : AudioHexViewer_Diag_YYYYMMDD_HHmmss.csv
        /// </summary>

        /// <summary>
        /// Écrit une ligne CSV pour la FFT dont la frame est <paramref name="frame"/>.
        /// Appelé depuis le thread UI (UpdateAnalysisFrame) à chaque nouvelle FFT.
        /// </summary>

        /// <summary>Arrête proprement le log si actif (appelé par StopCapture).</summary>

        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Arrête la capture audio + pipeline d'analyse si active.
        /// Remplace l'ancien bouton STOP.
        /// </summary>

        //
        //
        //
        private void btnCalV_Click(object sender, EventArgs e)
        {
            // Unification calibration: le bouton CAL V ouvre désormais le même dialog
            // que la calibration Spectrum (niveau AC), afin de servir à la fois au mode dBV et aux mesures en volts.
            if (spectrumControl == null)
            {
                MessageBox.Show("Le contrôle Spectrum n'est pas initialisé.",
                                "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            spectrumControl.ShowLevelCalibrationDialog(this);

            // Rafraîchir l'affichage principal (si l'utilisateur est en mode Voltage*)
            UpdateMainDisplayFromLastValues();
        }
        //
        //
        private void StartChannelCalibration(CalChannel channel)
        {
            calibChannel = channel;

            string chanName = (channel == CalChannel.Left) ? "GAUCHE" : "DROITE";

            MessageBox.Show(
                $"Voie {chanName} :\r\n" +
                "Applique une tension POSITIVE (1  5 V),\r\n" +
                "puis clique sur OK pour dmarrer la mesure sur ~1 s.",
                "Calibration +V",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            calibStage = CalStage.Positive;
            calibAccum = 0;
            calibTicks = 0;
            double durationSeconds = 1.0;  // facilement modifiable
            calibTicksTarget = (int)(durationSeconds * 1000.0 / calibTimer.Interval);

            calibTimer.Start();
        }

        //
        //
        private void CalibTimer_Tick(object? sender, EventArgs e)
        {
            int code = 0;

            if (calibChannel == CalChannel.Left)
                code = lastCodeLeft;
            else if (calibChannel == CalChannel.Right)
                code = lastCodeRight;
            else
                return;

            calibAccum += code;
            calibTicks++;

            if (calibTicks >= calibTicksTarget)
            {
                calibTimer.Stop();

                double avgCode = calibTicks > 0 ? (double)calibAccum / calibTicks : 0.0;

                if (calibStage == CalStage.Positive)
                {
                    calibCodePos = avgCode;
                    calibStage = CalStage.None;
                    FinishPositiveCalibration();
                }
                else if (calibStage == CalStage.Negative)
                {
                    calibCodeNeg = avgCode;
                    calibStage = CalStage.None;
                    FinishNegativeCalibration();
                }
            }
        }

        //
        private void FinishPositiveCalibration()
        {
            string chanName = (calibChannel == CalChannel.Left) ? "GAUCHE" : "DROITE";

            string? input = Microsoft.VisualBasic.Interaction.InputBox(
                $"Voie {chanName} :\r\n" +
                "Mesure termine.\r\n" +
                "Saisis la valeur de la tension POSITIVE applique (en volts, ex: 2.500) :",
                "Calibration +V",
                "2.500");

            if (!double.TryParse(input.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out calibVPos))
            {
                MessageBox.Show("Valeur non valide, calibration annule.",
                                "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Error);
                calibStage = CalStage.None;
                calibChannel = CalChannel.None;
                return;
            }

            MessageBox.Show(
                $"Voie {chanName} :\r\n" +
                "Maintenant, applique une tension NGATIVE (1  5 V),\r\n" +
                "puis clique sur OK pour dmarrer la mesure sur ~1 s.",
                "Calibration -V",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            calibStage = CalStage.Negative;
            calibAccum = 0;
            calibTicks = 0;
            double durationSeconds = 1.0;
            calibTicksTarget = (int)(durationSeconds * 1000.0 / calibTimer.Interval);
            calibTimer.Start();
        }

        //
        private void FinishNegativeCalibration()
        {
            string chanName = (calibChannel == CalChannel.Left) ? "GAUCHE" : "DROITE";

            string? input = Microsoft.VisualBasic.Interaction.InputBox(
                $"Voie {chanName} :\r\n" +
                "Mesure termine.\r\n" +
                "Saisis la valeur de la tension NGATIVE applique (en volts, ex: -2.500) :",
                "Calibration -V",
                "-2.500");

            if (!double.TryParse(input.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out calibVNeg))
            {
                MessageBox.Show("Valeur non valide, calibration annule.",
                                "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Error);
                calibStage = CalStage.None;
                calibChannel = CalChannel.None;
                return;
            }

            if (Math.Abs(calibCodePos - calibCodeNeg) < 1.0)
            {
                MessageBox.Show("Codes de calibration trop proches, impossible de calculer gain/offset.",
                                "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Error);
                calibStage = CalStage.None;
                calibChannel = CalChannel.None;
                return;
            }

            double gain = (calibVPos - calibVNeg) / (calibCodePos - calibCodeNeg);
            double offset = calibVPos - gain * calibCodePos;

            if (calibChannel == CalChannel.Left)
            {
                calibGainLeft = gain;
                calibOffsetLeft = offset;
                calibValidLeft = true;
            }
            else if (calibChannel == CalChannel.Right)
            {
                calibGainRight = gain;
                calibOffsetRight = offset;
                calibValidRight = true;
            }

            MessageBox.Show(
                $"Voie {chanName} : calibration termine.\r\n\r\n" +
                $"Gain   = {gain:F8} V/code\r\n" +
                $"Offset = {offset:F8} V",
                "Calibration",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            // Passer  l'autre voie si demand
            if (calibChannel == CalChannel.Left && calibDoRight)
            {
                // On a fini gauche, on passe  droite
                StartChannelCalibration(CalChannel.Right);
            }
            else if (calibChannel == CalChannel.Right && calibDoLeft)
            {
                // (si un jour tu veux inverser l'ordre)
                StartChannelCalibration(CalChannel.Left);
            }
            else
            {
                // fin totale
                calibStage = CalStage.None;
                calibChannel = CalChannel.None;
                SaveCalibration();
            }
        }

        //
        //
        private string GetCalibrationFilePath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, "AudioHexViewer.cal");
        }
        //
        private string GetRecordsFolderPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string dir = Path.Combine(baseDir, "records");

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            return dir;
        }

        //
        private void SaveCalibration()
        {
            string path = GetCalibrationFilePath();

            try
            {
                using (var sw = new StreamWriter(path, false))
                {
                    sw.WriteLine("GainLeft=" + calibGainLeft.ToString("R", CultureInfo.InvariantCulture));
                    sw.WriteLine("OffsetLeft=" + calibOffsetLeft.ToString("R", CultureInfo.InvariantCulture));
                    sw.WriteLine("ValidLeft=" + calibValidLeft);

                    sw.WriteLine("GainRight=" + calibGainRight.ToString("R", CultureInfo.InvariantCulture));
                    sw.WriteLine("OffsetRight=" + calibOffsetRight.ToString("R", CultureInfo.InvariantCulture));
                    sw.WriteLine("ValidRight=" + calibValidRight);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de l'criture de la calibration : " + ex.Message,
                                "Calibration",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }
        //
        //
        private void LoadCalibration()
        {
            string path = GetCalibrationFilePath();
            if (!File.Exists(path))
            {
                calibValidLeft = false;
                calibValidRight = false;
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(path);

                double gL = 1.0, oL = 0.0;
                double gR = 1.0, oR = 0.0;
                bool vL = false, vR = false;
                bool gLOk = false, oLOk = false, gROk = false, oROk = false;

                foreach (var line in lines)
                {
                    var parts = line.Split('=', 2);
                    if (parts.Length != 2) continue;

                    string key = parts[0].Trim();
                    string val = parts[1].Trim();

                    if (string.Equals(key, "GainLeft", StringComparison.OrdinalIgnoreCase))
                    {
                        if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out gL))
                            gLOk = true;
                    }
                    else if (string.Equals(key, "OffsetLeft", StringComparison.OrdinalIgnoreCase))
                    {
                        if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out oL))
                            oLOk = true;
                    }
                    else if (string.Equals(key, "ValidLeft", StringComparison.OrdinalIgnoreCase))
                    {
                        bool.TryParse(val, out vL);
                    }
                    else if (string.Equals(key, "GainRight", StringComparison.OrdinalIgnoreCase))
                    {
                        if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out gR))
                            gROk = true;
                    }
                    else if (string.Equals(key, "OffsetRight", StringComparison.OrdinalIgnoreCase))
                    {
                        if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out oR))
                            oROk = true;
                    }
                    else if (string.Equals(key, "ValidRight", StringComparison.OrdinalIgnoreCase))
                    {
                        bool.TryParse(val, out vR);
                    }
                }

                if (gLOk && oLOk)
                {
                    calibGainLeft = gL;
                    calibOffsetLeft = oL;
                    calibValidLeft = vL;
                }
                else
                {
                    calibValidLeft = false;
                }

                if (gROk && oROk)
                {
                    calibGainRight = gR;
                    calibOffsetRight = oR;
                    calibValidRight = vR;
                }
                else
                {
                    calibValidRight = false;
                }
            }
            catch (Exception ex)
            {
                calibValidLeft = false;
                calibValidRight = false;
                MessageBox.Show("Erreur lors de la lecture de la calibration : " + ex.Message,
                                "Calibration",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }
        //
        //

        //dbut de ProcessBuffer

        //
        //private void ShowHistogramWindow()
        //{
        //    if (histBuffer.Count == 0)
        //    {
        //        MessageBox.Show("Pas de donnes disponibles pour l'histogramme.");
        //        return;
        //    }

        //    int count = histBuffer.Count;
        //    int[] leftSamples = new int[count];
        //    int[] rightSamples = new int[count];

        //    for (int i = 0; i < count; i++)
        //    {
        //        leftSamples[i] = histBuffer[i].Left;
        //        rightSamples[i] = histBuffer[i].Right;
        //    }

        //    var histForm = new HistogramWindow(leftSamples, rightSamples);
        //    histForm.StartPosition = FormStartPosition.CenterParent;
        //    histForm.Show(this);
        //}
        private void ShowHistogramArea()
        {
            if (histogramControl == null)
            {
                int sampleRate = currentFormat?.SampleRate ?? 48000;

                histogramControl = new HistogramControl(GetAnalysisFrame, sampleRate);
                histogramControl.RunRequested += EnsureReadRunning;
                histogramControl.RunStateChanged += running =>
                {
                    _histogramRunning = running;
                    if (running) EnsureReadRunning();
                    else EnsureReadStoppedIfNoTabRunning();
                };

                histogramControl.Dock = DockStyle.Fill;
                histogramControl.SetGlobalSampleSize(analysisSampleSize);
                histogramControl.UpdateHopSamples(analysisHopSamples);

                if (_loadedSessionSettings != null)
                {
                    histogramControl.ApplyHistogramSettings(_loadedSessionSettings.Histogram);
                }


                panelHistogram.Controls.Clear();
                panelHistogram.Controls.Add(histogramControl);
            }

            panelHistogram.Visible = true;
        }
        //
        private void ShowSpectrumArea()
        {
            if (spectrumControl == null)
            {
                int sampleRate = currentFormat?.SampleRate ?? 48000;

                spectrumControl = new SpectrumControl();
                // ---- ChannelMode -> active/désactive les voies FFT côté worker (core) ----
                // IMPORTANT : SpectrumControl est créé ICI (ShowSpectrumArea), pas dans le ctor.
                // Donc l'abonnement doit être fait ici, sinon le changement de voie pendant RUN
                // ne reconfigure jamais le worker (symptôme : Right ne s'affiche pas sans relancer Read).

                spectrumControl.ChannelModeChanged += mode =>
                {
                    ApplyActiveChannelsToWorker(mode);
                };


                spectrumControl.UpdateSampleRate(sampleRate);

                _lblFftTicker = spectrumControl.FftTickerLabel;
                _lblFftTicker.Visible = false; // optionnel
                // THD : logique + persistance (UI lives in SpectrumControl) (button + checkbox in the Spectrum tab)
                spectrumControl.ThdEnabledChanged += enabled => ApplyThdEnabled(enabled);
                spectrumControl.ThdSettingsRequested += () => ShowThdSettings();
                // Sync state from session
                spectrumControl.ApplyThdSettings(_session.Spectrum.Thd);
                spectrumControl.SetThdMeasuresEnabled(_session.Spectrum.EnableThdMeasures);
                spectrumControl.SetThdEnabledUi(_session.Spectrum.EnableThdMeasures);
   
                spectrumControl.RunRequested += EnsureReadRunning;
                spectrumControl.RunStateChanged += running =>
                {
                    _spectrumRunning = running;
                    if (running) EnsureReadRunning();
                    else EnsureReadStoppedIfNoTabRunning();
                };
                // Window change -> recrée le worker SFFT pour appliquer immédiatement la nouvelle fenêtre
                spectrumControl.WindowTypeChangedForExternalFft += (_, __) =>
                {
                    if (_pipeline != null && _pipeline.IsRunning)
                        RestartSfftFromUi();
                };

                // Mode AC/DC -> propage immédiatement à SfftWorker (flag volatile + drain queue)
                spectrumControl.RemoveDcChanged += (_, __) => ApplyDcModeToWorker();
                
                spectrumControl.Dock = DockStyle.Fill;
                spectrumControl.SetGlobalSampleSize(analysisSampleSize);
                spectrumControl.UpdateHopSamples(analysisHopSamples);

                if (_loadedSessionSettings != null)
                {
                    spectrumControl.ApplySpectrumSettings(_loadedSessionSettings.Spectrum);
                    var initColors = _loadedSessionSettings.Spectrum?.Colors ?? SpectrumColorSettings.Defaults();
                    histogramControl?.ApplySpectrumColorSettings(initColors);
                }

                // tabSpectrum est l'onglet du bas
                tabSpectrum.Controls.Clear();
                tabSpectrum.Controls.Add(spectrumControl);
            }

            // Optionnel : slectionner longlet Spectrum quand on louvre
            tabAnalysis.SelectedTab = tabSpectrum;
        }

        private void ShowOscilloscopeArea()
        {
            if (oscilloscopeControl == null)
            {
                oscilloscopeControl = new OscilloscopeControl
                {
                    Dock          = DockStyle.Fill,
                    GetRing       = () => _pipeline?.Ring,
                    GetSampleRate = () => currentFormat?.SampleRate ?? 0,
                    GetInputCalibration = () =>
                    {
                        var sp = _session.Spectrum;
                        return (sp.LevelCalValidLeft,  sp.LevelCalOffsetDbLeft,
                                sp.LevelCalValidRight, sp.LevelCalOffsetDbRight);
                    }
                };

                oscilloscopeControl.RunRequested += EnsureReadRunning;
                oscilloscopeControl.RunStateChanged += running =>
                {
                    _oscilloscopeRunning = running;
                    if (running) EnsureReadRunning();
                    else EnsureReadStoppedIfNoTabRunning();
                };

                // Appliquer les couleurs courantes
                var colors = _loadedSessionSettings?.Spectrum?.Colors
                          ?? _session?.Spectrum?.Colors
                          ?? SpectrumColorSettings.Defaults();
                oscilloscopeControl.ApplySpectrumColorSettings(colors);

                // Appliquer les settings sauvegardés
                oscilloscopeControl.ApplySettings(_loadedSessionSettings?.Oscilloscope
                                               ?? _session?.Oscilloscope);

                if (currentFormat != null)
                    oscilloscopeControl.UpdateSampleRate(currentFormat.SampleRate);

                tabOscilloscope.Controls.Clear();
                tabOscilloscope.Controls.Add(oscilloscopeControl);
            }
        }
        //
        private void UpdateMainDisplayFromLastValues()
        {
            if (_dataViewForm!.comboDisplayMode.SelectedItem is not string mode)
                mode = "Decimal (24b)";// mode par dfaut

            // Prparer les valeurs formates
            string leftText = "";
            string rightText = "";

            switch (mode)
            {
                case "Float":
                    leftText = lastLeftFloat.ToString("F8", CultureInfo.InvariantCulture);
                    rightText = lastRightFloat.ToString("F8", CultureInfo.InvariantCulture);
                    break;

                case "Decimal (24b)":
                    leftText = lastLeft24.ToString();
                    rightText = lastRight24.ToString();
                    break;

                case "Hex (24 bits)":
                    leftText = lastLeftHex24;
                    rightText = lastRightHex24;
                    break;

                case "Binary (24b)":
                    leftText = lastLeftBits;
                    rightText = lastRightBits;
                    break;

                case "Voltage (Vrms)":
                case "Voltage (Vpeak)":
                case "Voltage (Vdc)":
                    // Mesures en volts issues de SpectrumControl (calibration unifiée)
                    double? vrmsL = null, vpkL = null, vdcL = null;
                    double? vrmsR = null, vpkR = null, vdcR = null;
                    bool calL = false, calR = false;

                    if (spectrumControl != null)
                    {
                        calL = spectrumControl.VoltageCalibrationValidLeft;
                        calR = spectrumControl.VoltageCalibrationValidRight;
                        spectrumControl.GetLastVoltages(out vrmsL, out vpkL, out vdcL, out vrmsR, out vpkR, out vdcR);
                                               
                    }
                    else
                    {
                        // optionnel : si spectrumControl est null, on affiche aussi ---
                        leftText = "---";
                        rightText = "---";
                        break;
                    }

                    double? selL =
                        mode == "Voltage (Vrms)" ? vrmsL :
                        mode == "Voltage (Vpeak)" ? vpkL :
                        vdcL;

                    double? selR =
                        mode == "Voltage (Vrms)" ? vrmsR :
                        mode == "Voltage (Vpeak)" ? vpkR :
                        vdcR;

                    bool leftActive = spectrumControl?.IsLeftChannelActive ?? true;
                    bool rightActive = spectrumControl?.IsRightChannelActive ?? true;


                    if (!leftActive)
                        leftText = "---";
                    else if (!calL)
                        leftText = "Uncalibrated";
                    else if (selL.HasValue)
                        leftText = FormatVoltageWithUnit(selL.Value);
                    else
                        leftText = "---"; // pas encore de mesure exploitable

                    if (!rightActive)
                        rightText = "---";
                    else if (!calR)
                        rightText = "Uncalibrated";
                    else if (selR.HasValue)
                        rightText = FormatVoltageWithUnit(selR.Value);
                    else
                        rightText = "---";

                    break;

                    default:
                    leftText = "";
                    rightText = "";
                    break;
            }

            // Met à jour les labels seulement si DataView est en RUN
            if (_dataViewForm?.IsRunning == true)
            {
                string? modeStr = _dataViewForm.comboDisplayMode.SelectedItem as string;
                _dataViewForm.UpdateValues(leftText, rightText, modeStr ?? mode);
            }
        }
        //
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private const int WM_SETREDRAW = 0x000B;

        private static void BeginNoRedraw(Control c)
        {
            if (c.IsHandleCreated) SendMessage(c.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
        }

        private static void EndNoRedraw(Control c)
        {
            if (c.IsHandleCreated)
            {
                SendMessage(c.Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
                c.Invalidate();
                c.Update();
            }
        }

        //
        private void ColorBinaryXs(RichTextBox rtb, string binaryLabel)
        {
            string fullText = rtb.Text;
            int labelPos = fullText.IndexOf(binaryLabel, StringComparison.Ordinal);
            if (labelPos < 0)
                return;

            int lineEnd = fullText.IndexOf('\n', labelPos);
            if (lineEnd < 0)
                lineEnd = fullText.Length;

            int bitsStart = fullText.IndexOf(':', labelPos);
            if (bitsStart < 0 || bitsStart + 1 >= lineEnd)
                return;

            bitsStart += 2; // on saute ": "

            // On sauvegarde la slection actuelle
            int oldStart = rtb.SelectionStart;
            int oldLength = rtb.SelectionLength;
            Color oldColor = rtb.SelectionColor;

            rtb.SuspendLayout();
            BeginNoRedraw(rtb);

            for (int i = bitsStart; i < lineEnd; i++)
            {
                rtb.SelectionStart = i;
                rtb.SelectionLength = 1;

                char c = fullText[i];
                if (c == 'x')
                {
                    rtb.SelectionColor = Color.Red;
                }
                else
                {
                    rtb.SelectionColor = rtb.ForeColor;
                }
            }
            EndNoRedraw(rtb);

            rtb.ResumeLayout();

            // On restaure la slection comme si de rien ntait
            rtb.SelectionStart = oldStart;
            rtb.SelectionLength = oldLength;
            rtb.SelectionColor = oldColor;
        }



        private string FormatBitsWithStability(uint value24, bool[] lastBits, DateTime[] lastChange)
        {
            var now = DateTime.Now;
            char[] bits = new char[24];

            // On parcourt du MSB (bit 23) au LSB (bit 0)
            for (int i = 0; i < 24; i++)
            {
                int bitIndex = 23 - i; // i=0 => bit 23 (MSB), i=23 => bit 0 (LSB)
                bool bitValue = ((value24 >> bitIndex) & 1) != 0;

                // Si le bit a chang depuis la dernire fois, on note le moment
                if (bitValue != lastBits[bitIndex])
                {
                    lastBits[bitIndex] = bitValue;
                    lastChange[bitIndex] = now;
                }

                // Si le bit a chang dans la dernire seconde -> on affiche 'x'
                if (now - lastChange[bitIndex] < bitStabilityThreshold)
                {
                    bits[i] = 'x';
                }
                else
                {
                    bits[i] = bitValue ? '1' : '0';
                }
            }

            string raw = new string(bits);
            return GroupBits(raw, 4); // On rutilise ta fonction de groupement par 4 bits
        }
        //
        //
        private string BuildDeviceInfoText()
        {
            var sb = new StringBuilder();

            if (selectedDevice == null)
            {
                sb.AppendLine("Selected device : (none)");
                sb.AppendLine();
                sb.AppendLine("Capture not started.");
                return sb.ToString();
            }

            var dev = selectedDevice;

            WaveFormat? fmt = currentFormat;
            if (fmt == null)
            {
                try
                {
                    fmt = dev.AudioClient.MixFormat;
                }
                catch
                {
                    fmt = null;
                }
            }

            sb.AppendLine("Selected device");
            sb.AppendLine(new string('-', 40));

            sb.AppendLine($"Name   : {dev.FriendlyName}");
            sb.AppendLine($"ID     : {dev.ID}");
            sb.AppendLine();

            sb.AppendLine("Endpoint");
            sb.AppendLine($"  Type : {(captureOutput ? "Output (Render)" : "Input  (Capture)")}");
            sb.AppendLine($"  State: {dev.State}");
            sb.AppendLine();

            sb.AppendLine("Format (Windows)");
            if (fmt != null)
            {
                string enc;
                if (fmt.Encoding == WaveFormatEncoding.IeeeFloat)
                    enc = "IEEE Float";
                else if (fmt.Encoding == WaveFormatEncoding.Pcm)
                    enc = "PCM";
                else
                    enc = fmt.Encoding.ToString();

                sb.AppendLine($"  {fmt.Channels} ch, {fmt.BitsPerSample} bits, {fmt.SampleRate} Hz");
                sb.AppendLine($"  {enc}");
            }
            else
            {
                sb.AppendLine("  (unknown)");
            }

            sb.AppendLine();

            sb.AppendLine("Capture status");
            if (capture == null)
            {
                sb.AppendLine("  Idle (not capturing)");
            }
            else
            {
                sb.AppendLine("  Active");
                sb.AppendLine($"  Mode : {(captureOutput ? "OUT (loopback on output)" : "IN (device input)")}");
            }

            return sb.ToString();
        }
        /// <summary>
        /// 
        //
        /// </summary>
        private void UpdateDeviceInfo()
        {
            string text = BuildDeviceInfoText();

        }



        // ── VU-mètre entrée ───────────────────────────────────────────────────
        private void InitInputVuMeter()
        {
            // VU-mètre déplacé dans DataViewForm

            _vuInTimer = new System.Windows.Forms.Timer { Interval = 80 };
            _vuInTimer.Tick += VuInTimer_Tick;

            // Active/désactive le timer selon l'onglet visible
            tabAnalysis.SelectedIndexChanged += (_, _) => UpdateVuInTimer();
        }

        // ── Barre de titre ────────────────────────────────────────────────
        private void UpdateTitleBar()
        {
            var parts = new System.Collections.Generic.List<string>();

            // Entrée active
            if (capture != null && currentFormat != null)
            {
                string devName = selectedDevice?.FriendlyName ?? "Loopback";
                if (devName.Length > 30) devName = devName[..27] + "…";
                string sr = currentFormat.SampleRate >= 1000
                    ? $"{currentFormat.SampleRate / 1000.0:0.###} kHz"
                    : $"{currentFormat.SampleRate} Hz";
                parts.Add($"IN : {devName}  {sr}");
            }

            // Sortie active (générateur)
            if (_generatorWindow?.IsGenerating == true)
            {
                string devOut = _selectedOutputDevice?.FriendlyName ?? "Output";
                if (devOut.Length > 30) devOut = devOut[..27] + "…";
                string srOut = _generatorWindow.CurrentSampleRate >= 1000
                    ? $"{_generatorWindow.CurrentSampleRate / 1000.0:0.###} kHz"
                    : $"{_generatorWindow.CurrentSampleRate} Hz";
                parts.Add($"OUT : {devOut}  {srOut}");
            }

            // Titre : "AudioHexViewer" seul si rien d'actif, sinon ajoute les infos
            Text = parts.Count > 0
                ? $"AudioHexViewer  —  {string.Join("   |   ", parts)}"
                : "AudioHexViewer";
        }

        private void UpdateVuInTimer()
        {
            bool analysisVisible = tabAnalysis.Visible &&
                (tabAnalysis.SelectedTab == tabHistogram ||
                 tabAnalysis.SelectedTab == tabSpectrum  ||
                 tabAnalysis.SelectedTab == tabOscilloscope);
            bool dataViewRunning = _dataViewForm?.IsRunning == true;
            if ((analysisVisible || dataViewRunning) && capture != null)
                _vuInTimer?.Start();
            else
            {
                _vuInTimer?.Stop();
                _vuInPctL = _vuInPctR = 0;
                _vuInStrL = _vuInStrR = "-∞ dBFS";
                _dataViewForm?.ResetVu();
            }
        }

        private void VuInTimer_Tick(object? s, EventArgs e)
        {
            // Timer sur le thread UI — déclenche le repaint proprement
        }

        // Appelé depuis Form1.AudioCapture.cs — stocke seulement, le timer 80ms déclenche le repaint
        internal void SetVuInLevels(double dbL, double dbR)
        {
            _vuInPctL = VuDbToPct(dbL);
            _vuInPctR = VuDbToPct(dbR);
            _vuInStrL = VuFmtDb(dbL);
            _vuInStrR = VuFmtDb(dbR);
            // Forward to DataView VU if running
            if (_dataViewForm?.IsRunning == true)
                _dataViewForm.SetVuLevels(dbL, dbR);
        }

        private static int VuDbToPct(double db)
            => db <= VuDbMin ? 0 : db >= 0.0 ? 100
             : (int)((db - VuDbMin) / -VuDbMin * 100.0);

        private static string VuFmtDb(double db)
            => double.IsNegativeInfinity(db) ? "-∞ dBFS" : $"{db:F1} dBFS";

        private void PnlInputVu_Paint(object? sender, PaintEventArgs e)
        {
            var g  = e.Graphics;
            var sz = new System.Drawing.Size(400, 52);
            g.Clear(Color.FromArgb(0, 0, 64));

            // Barre L — y=4, hauteur=16
            PaintVuBarGdi(g, new Rectangle(22, 4, sz.Width - 116, 16), _vuInPctL);
            // Barre R — y=28, hauteur=16
            PaintVuBarGdi(g, new Rectangle(22, 28, sz.Width - 116, 16), _vuInPctR);

            using var fSmall = new Font("Segoe UI", 7f);
            TextRenderer.DrawText(g, "L", fSmall,
                new Rectangle(4, 4, 16, 16), Color.FromArgb(160, 160, 180),
                TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, "R", fSmall,
                new Rectangle(4, 28, 16, 16), Color.FromArgb(160, 160, 180),
                TextFormatFlags.VerticalCenter);

            using var fLevel = new Font("Segoe UI", 7.5f);
            int lx = sz.Width - 112;
            TextRenderer.DrawText(g, _vuInStrL, fLevel,
                new Rectangle(lx, 4, 108, 16), Color.Lime,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
            TextRenderer.DrawText(g, _vuInStrR, fLevel,
                new Rectangle(lx, 28, 108, 16), Color.Lime,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
        }

        private static void PaintVuBarGdi(Graphics g, Rectangle r, int pct)
        {
            if (r.Width <= 4 || r.Height <= 4) return;
            int vm   = Math.Max(1, r.Height / 6);
            int lh   = r.Height - 2 * vm;
            double pB = (VuBlueEnd   - VuDbMin) / -VuDbMin * 100.0;
            double pG = (VuGreenEnd  - VuDbMin) / -VuDbMin * 100.0;
            double pO = (VuOrangeEnd - VuDbMin) / -VuDbMin * 100.0;
            for (int i = 0; i < VuLedCount; i++)
            {
                int x0 = r.X + (int)((double)i       / VuLedCount * r.Width);
                int x1 = r.X + (int)((double)(i + 1) / VuLedCount * r.Width);
                int w  = Math.Max(1, x1 - x0 - 2);
                double cp = ((i + 0.5) / VuLedCount) * 100.0;
                Color col = cp > pct           ? Color.FromArgb(30, 30, 60)
                          : cp <= pB           ? Color.FromArgb(0, 120, 215)
                          : cp <= pG           ? Color.Lime
                          : cp <= pO           ? Color.Gold
                                               : Color.Red;
                using var br = new SolidBrush(col);
                g.FillRectangle(br, x0, r.Y + vm, w, lh);
            }
        }

        private void CreateDbMarkers(Panel bar, string[] labels, double[] dbValues)
        {
            if (bar == null || labels == null || dbValues == null)
                return;
            if (labels.Length != dbValues.Length)
                return;

            int n = labels.Length;

            for (int i = 0; i < n; i++)
            {
                double db = dbValues[i];
                double percent = DbToPercent(db); // 0..100

                // Position X relative  la barre
                int x = bar.Left + (int)(bar.Width * percent / 100.0);

                var lbl = new Label();
                lbl.AutoSize = true;
                lbl.Text = labels[i];
                lbl.Font = new Font(Font.FontFamily, 7); // petite police
                lbl.Top = bar.Top - lbl.Height + 10;      // juste au-dessus de la barre
                lbl.Left = x - lbl.PreferredWidth / 2;   // centr sur la position

                //Controls.Add(lbl);//
                bar.Parent.Controls.Add(lbl); //
                lbl.BringToFront();
            }
        }


        private void UpdateLevelBar(Panel panel, int levelPercent)
        {
            if (panel == null || panel.Width <= 0 || panel.Height <= 0)
                return;

            // Clamp
            if (levelPercent < 0) levelPercent = 0;
            if (levelPercent > 100) levelPercent = 100;

            // Rcuprer / crer les LEDs
            Panel[] leds;
            if (panel.Tag is Panel[] arr && arr.Length == LedCount && panel.Controls.Count == LedCount)
            {
                leds = arr;
            }
            else
            {
                leds = new Panel[LedCount];
                panel.Controls.Clear();

                int totalWidth = panel.ClientSize.Width;
                int totalHeight = panel.ClientSize.Height;

                // petites marges verticales pour ne pas coller au bord
                int vMargin = Math.Max(1, totalHeight / 6);
                int ledHeight = Math.Max(2, totalHeight - 2 * vMargin);

                int spacing = 2; // espace horizontal entre LEDs

                for (int i = 0; i < LedCount; i++)
                {
                    var led = new Panel();

                    // dcoupe en bandes gales sur toute la largeur
                    double xStart = (double)i / LedCount * totalWidth;
                    double xEnd = (double)(i + 1) / LedCount * totalWidth;

                    int left = (int)Math.Round(xStart);
                    int right = (int)Math.Round(xEnd);
                    int width = right - left;

                    if (width > spacing)
                        width -= spacing;

                    led.Left = left;
                    led.Width = Math.Max(1, width);
                    led.Top = vMargin;
                    led.Height = ledHeight;

                    // couleur LED teinte
                    led.BackColor = Color.FromArgb(64, 64, 64);
                    led.BorderStyle = BorderStyle.None;

                    panel.Controls.Add(led);
                    leds[i] = led;
                }

                panel.Tag = leds;
            }

            // Pourcentage des frontires de couleur
            double pBlueEnd = DbToPercent(DbBlueToGreen);
            double pGreenEnd = DbToPercent(DbGreenToOrange);
            double pOrangeEnd = DbToPercent(DbOrangeToRed);

            for (int i = 0; i < LedCount; i++)
            {
                var led = leds[i];

                // Position de cette LED en % de la barre
                double ledCenterPercent = ((i + 0.5) / LedCount) * 100.0;

                bool isOn = ledCenterPercent <= levelPercent;
                if (!isOn)
                {
                    // LED teinte
                    led.BackColor = Color.FromArgb(64, 64, 64);
                    continue;
                }

                // Couleur en fonction de la zone de dB
                if (ledCenterPercent <= pBlueEnd)
                {
                    led.BackColor = Color.FromArgb(0, 120, 215);      // bleu "Windows"
                }
                else if (ledCenterPercent <= pGreenEnd)
                {
                    led.BackColor = Color.Lime;                       // vert vif
                }
                else if (ledCenterPercent <= pOrangeEnd)
                {
                    led.BackColor = Color.Gold;                       // jaune/orange
                }
                else // zone rouge
                {
                    led.BackColor = Color.Red;
                }
            }
        }
        //
        //
        private void alwaysOnTopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Le ToolStripMenuItem appelle ce handler aprs avoir chang son tat Checked
            this.TopMost = alwaysOnTopToolStripMenuItem.Checked;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Pré-scan des devices pendant que le splash est encore visible
            // (thread UI STA = objets COM WASAPI valides)

            // Double-buffering sur la barre de debug pour supprimer tout clignotement résiduel
            typeof(Panel)
                .GetProperty("DoubleBuffered",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(panelDebugInfo, true);

            LoadUiSettingsFromSettingsFiles();

            if (_loadedSessionSettings != null)
                ApplyAnalysisSettings(_loadedSessionSettings);

            ApplyLoadedSessionSettingsToExistingControls();

        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {
        }
        private void lblFormatInfo_Click(object sender, EventArgs e)
        {
        }
        //
        // ── Helpers export ────────────────────────────────────────────────────
        private enum ActiveTab { Spectrum, Histogram, Oscilloscope }

        private ActiveTab GetActiveTab()
        {
            if (tabAnalysis.SelectedTab == tabSpectrum)     return ActiveTab.Spectrum;
            if (tabAnalysis.SelectedTab == tabOscilloscope) return ActiveTab.Oscilloscope;
            return ActiveTab.Histogram;
        }

        private static string TabKindName(ActiveTab t) => t switch
        {
            ActiveTab.Spectrum     => "Spectrum",
            ActiveTab.Oscilloscope => "Oscilloscope",
            _                      => "Histogram"
        };

        private bool CheckExportReady(ActiveTab t)
        {
            return t switch
            {
                ActiveTab.Spectrum     => spectrumControl     != null,
                ActiveTab.Oscilloscope => oscilloscopeControl != null,
                _                      => histogramControl    != null,
            };
        }

        private void DoExportPng(string filePath, int scale, string? title = null, string? footer = null)
        {
            var tab = GetActiveTab();
            switch (tab)
            {
                case ActiveTab.Spectrum:     spectrumControl!    .ExportPng(filePath, scale, title, footer); break;
                case ActiveTab.Oscilloscope: oscilloscopeControl!.ExportPng(filePath, scale, title, footer); break;
                default:                     histogramControl!   .ExportPng(filePath, scale, title, footer); break;
            }
        }

        private string GetDeviceFooterText()
        {
            string inName  = _selectedInputDevice?.FriendlyName  ?? "—";
            string outName = _selectedOutputDevice?.FriendlyName ?? "—";
            string date    = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            return $"Input: {inName}    Output: {outName}    {date}";
        }

        //
        private void ExportActiveGraphToPng()
        {
            var tab = GetActiveTab();
            if (!CheckExportReady(tab))
            {
                MessageBox.Show($"L'onglet {TabKindName(tab)} n'est pas initialisé.", "Export PNG",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter     = "PNG (*.png)|*.png",
                DefaultExt = "png",
                FileName   = $"AudioHexViewer_{TabKindName(tab)}_{DateTime.Now:yyyyMMdd_HHmmss}.png"
            };

            if (sfd.ShowDialog(this) != DialogResult.OK) return;

            try   { DoExportPng(sfd.FileName, scale: 2); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Export PNG", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // ── Copy graph to clipboard ───────────────────────────────────────────

        private void menuCopyClipboard_Click(object sender, EventArgs e)
        {
            var tab = GetActiveTab();
            if (!CheckExportReady(tab))
            {
                MessageBox.Show($"L'onglet {TabKindName(tab)} n'est pas initialisé.", "Copy",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                switch (tab)
                {
                    case ActiveTab.Spectrum:
                        ChartExport.CopyChartToClipboard(spectrumControl!.GetChartForClipboard());
                        break;
                    case ActiveTab.Histogram:
                        ChartExport.CopyChartToClipboard(histogramControl!.GetChartForClipboard());
                        break;
                    case ActiveTab.Oscilloscope:
                        ChartExport.CopyControlToClipboard(oscilloscopeControl!.GetPanelForClipboard());
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Copy to clipboard", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        // ── Export to PDF ─────────────────────────────────────────────────────
        private void menuExportCsv_Click(object? sender, EventArgs e)
        {
            using var dlg = new ExportCsvDialog();

            // Pré-sélectionne l'onglet actif
            dlg.PreSelectTab(GetActiveTab() switch
            {
                ActiveTab.Spectrum     => "Spectrum",
                ActiveTab.Oscilloscope => "Oscilloscope",
                _                      => "Histogram"
            });

            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                System.Text.StringBuilder? data = dlg.SelectedTab switch
                {
                    "Histogram"    => histogramControl?.GetCsvData(dlg.Separator, dlg.IncludeHeader, dlg.DecimalSeparator),
                    "Spectrum"     => spectrumControl?.GetCsvData(dlg.Separator, dlg.IncludeHeader, dlg.DecimalSeparator),
                    "Oscilloscope" => oscilloscopeControl?.GetCsvData(dlg.Separator, dlg.IncludeHeader, dlg.DecimalSeparator),
                    _              => null
                };

                if (data == null || data.Length == 0)
                {
                    MessageBox.Show(this, "No data available for export.", "Export CSV",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                File.WriteAllText(dlg.FilePath, data.ToString(), System.Text.Encoding.UTF8);

                // Message 1s auto-dismiss
                ShowAutoClosingMessage($"CSV exported successfully:\n{dlg.FilePath}", 1000);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Export CSV", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowAutoClosingMessage(string message, int durationMs)
        {
            var dlg = new Form
            {
                Text = "Export CSV",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition   = FormStartPosition.CenterParent,
                MaximizeBox     = false, MinimizeBox = false,
                ShowInTaskbar   = false, ControlBox = false,
                ClientSize      = new Size(380, 70)
            };
            var lbl = new Label
            {
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Text      = message,
                Font      = new Font("Segoe UI", 9f)
            };
            dlg.Controls.Add(lbl);
            var t = new System.Windows.Forms.Timer { Interval = durationMs };
            t.Tick += (_, _) => { t.Stop(); t.Dispose(); dlg.Close(); };
            t.Start();
            dlg.Show(this);
        }

        private void menuExportPdf_Click(object sender, EventArgs e)
        {
            using var dlg = new AudioHexViewer.ExportPdfDialog();
            dlg.SetAvailableTabs(
                hasSpectrum:   spectrumControl     != null,
                hasHistogram:  histogramControl    != null,
                hasOscillo:    oscilloscopeControl != null);

            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            string outPath = dlg.FilePath;
            if (string.IsNullOrWhiteSpace(outPath)) return;

            string titleText    = dlg.GetTitleText();
            string subtitleText = dlg.GetSubtitleText();
            string fullTitle    = string.IsNullOrWhiteSpace(subtitleText)
                                  ? titleText
                                  : titleText + "\n" + subtitleText;
            int scale = dlg.ExpScale;

            var pages = new System.Collections.Generic.List<(string name, System.Action<string> export)>();
            string footer = GetDeviceFooterText();
            if (dlg.IncludeSpectrum  && spectrumControl     != null)
                pages.Add(("Spectrum",     p => spectrumControl!    .ExportPng(p, scale, fullTitle, footer)));
            if (dlg.IncludeHistogram && histogramControl    != null)
                pages.Add(("Histogram",    p => histogramControl!   .ExportPng(p, scale, fullTitle, footer)));
            if (dlg.IncludeOscillo   && oscilloscopeControl != null)
                pages.Add(("Oscilloscope", p => oscilloscopeControl!.ExportPng(p, scale, fullTitle, footer)));

            if (pages.Count == 0)
            {
                MessageBox.Show(this, "No initialized tab selected.", "Export to PDF",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var tmpFiles = new string[pages.Count];
            try
            {
                for (int i = 0; i < pages.Count; i++)
                {
                    tmpFiles[i] = Path.Combine(Path.GetTempPath(),
                        $"AudioHexViewer_{pages[i].name}_{Guid.NewGuid():N}.png");
                    pages[i].export(tmpFiles[i]);
                }

                string[] pageTitles = System.Linq.Enumerable
                    .Range(0, pages.Count).Select(i => pages[i].name).ToArray();

                PdfExport.SaveMultiPageFromPngs(tmpFiles, outPath,
                    docTitle:   string.IsNullOrWhiteSpace(titleText) ? null : titleText,
                    pageTitles: pageTitles);

                // Confirmation de fin de génération
                string pageWord = tmpFiles.Length > 1 ? $"{tmpFiles.Length} pages" : "1 page";
                string msg = $"PDF generated successfully ({pageWord}).\n\n{outPath}";
                if (dlg.OpenAfter)
                {
                    ExportPdfDialog.TryOpenFile(outPath);
                    MessageBox.Show(this, msg, "Export to PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(this, msg, "Export to PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Export to PDF", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                foreach (var tmp in tmpFiles)
                    if (tmp != null) try { File.Delete(tmp); } catch { /* ignore */ }
            }
        }


        //
        private static SpectrumFrame DecimateFrameForDisplay(SpectrumFrame src, double fMin, double fMax, int maxPoints, bool useLogX)
        {
            var fxIn = src.FreqAxisHz;
            int nIn = fxIn.Length;
            if (nIn <= 0) return src;

            // bornes en indices (bins)
            int i0 = Array.BinarySearch(fxIn, fMin);
            if (i0 < 0) i0 = ~i0;
            int i1 = Array.BinarySearch(fxIn, fMax);
            if (i1 < 0) i1 = ~i1;

            i0 = Math.Max(0, Math.Min(nIn - 1, i0));
            i1 = Math.Max(i0 + 1, Math.Min(nIn, i1));

            if (maxPoints <= 0) maxPoints = 2000;

            // plage minuscule : pas de décimation
            if (i1 - i0 <= maxPoints)
                return SliceFrame(src, i0, i1);

            // -----------------------------------------------------------------
            // MODE LOG X : décimation uniforme en log10(f) (meilleur rendu basses fréquences)
            // -----------------------------------------------------------------
            if (useLogX)
            {
                // Sécurité : log nécessite f>0
                if (fMin <= 0) fMin = 1.0;
                if (fMax <= fMin) fMax = fMin * 10.0;

                int nOutLog = maxPoints;
                var fxLog = new double[nOutLog];
                MathNet.Numerics.Complex32[]? lLog = (src.LeftBins != null) ? new MathNet.Numerics.Complex32[nOutLog] : null;
                MathNet.Numerics.Complex32[]? rLog = (src.RightBins != null) ? new MathNet.Numerics.Complex32[nOutLog] : null;


                double logMin = Math.Log10(fMin);
                double logMax = Math.Log10(fMax);
                double logStep = (logMax - logMin) / nOutLog;

                for (int j = 0; j < nOutLog; j++)
                {
                    double fa = Math.Pow(10.0, logMin + j * logStep);
                    double fb = Math.Pow(10.0, logMin + (j + 1) * logStep);

                    int a = Array.BinarySearch(fxIn, fa);
                    if (a < 0) a = ~a;
                    int b = Array.BinarySearch(fxIn, fb);
                    if (b < 0) b = ~b;

                    // Clamp à la zone [i0..i1)
                    if (a < i0) a = i0;
                    if (b > i1) b = i1;

                    if (b <= a) b = Math.Min(i1, a + 1);
                    if (a >= i1) a = i1 - 1;
                    if (b <= a) b = a + 1; // ultime sécurité

                    int mid = (a + b) / 2;
                    if (mid >= i1) mid = i1 - 1;
                    fxLog[j] = fxIn[mid];

                    // Gauche : bin de magnitude max dans le segment (préserve phase)
                    if (lLog != null && src.LeftBins != null)
                    {
                        float maxMag = -1f;
                        int kMax = a;
                        for (int k = a; k < b; k++)
                        {
                            float mag = src.LeftBins[k].Magnitude;
                            if (mag > maxMag) { maxMag = mag; kMax = k; }
                        }
                        lLog[j] = src.LeftBins[kMax];
                    }

                    // Droite
                    if (rLog != null && src.RightBins != null)
                    {
                        float maxMag = -1f;
                        int kMax = a;
                        for (int k = a; k < b; k++)
                        {
                            float mag = src.RightBins[k].Magnitude;
                            if (mag > maxMag) { maxMag = mag; kMax = k; }
                        }
                        rLog[j] = src.RightBins[kMax];
                    }
                }

                return new SpectrumFrame(src.SampleRate, src.FftSize, src.AbsEnd, lLog, rLog, fxLog);
            }


            int nOut = maxPoints;
            var fx = new double[nOut];

            MathNet.Numerics.Complex32[]? l = (src.LeftBins != null) ? new MathNet.Numerics.Complex32[nOut] : null;
            MathNet.Numerics.Complex32[]? r = (src.RightBins != null) ? new MathNet.Numerics.Complex32[nOut] : null;

            double step = (double)(i1 - i0) / nOut;

            for (int j = 0; j < nOut; j++)
            {
                int a = i0 + (int)Math.Floor(j * step);
                int b = i0 + (int)Math.Floor((j + 1) * step);

                if (a < i0) a = i0;
                if (b <= a) b = a + 1;
                if (b > i1) b = i1;

                // fréquence représentative : centre du segment
                int mid = (a + b) / 2;
                if (mid >= i1) mid = i1 - 1;
                fx[j] = fxIn[mid];

                // Gauche : on garde le bin de magnitude max dans le segment (préserve phase)
                if (l != null && src.LeftBins != null)
                {
                    float maxMag = -1f;
                    int kMax = a;

                    for (int k = a; k < b; k++)
                    {
                        float mag = src.LeftBins[k].Magnitude;
                        if (mag > maxMag)
                        {
                            maxMag = mag;
                            kMax = k;
                        }
                    }
                    l[j] = src.LeftBins[kMax];
                }

                // Droite
                if (r != null && src.RightBins != null)
                {
                    float maxMag = -1f;
                    int kMax = a;

                    for (int k = a; k < b; k++)
                    {
                        float mag = src.RightBins[k].Magnitude;
                        if (mag > maxMag)
                        {
                            maxMag = mag;
                            kMax = k;
                        }
                    }
                    r[j] = src.RightBins[kMax];
                }
            }

            return new SpectrumFrame(src.SampleRate, src.FftSize, src.AbsEnd, l, r, fx);
        }

        private static SpectrumFrame SliceFrame(SpectrumFrame src, int i0, int i1)
        {
            var fxIn = src.FreqAxisHz;
            int nIn = fxIn.Length;

            i0 = Math.Max(0, Math.Min(nIn - 1, i0));
            i1 = Math.Max(i0 + 1, Math.Min(nIn, i1));

            int n = i1 - i0;

            var fx = new double[n];
            Array.Copy(fxIn, i0, fx, 0, n);

            MathNet.Numerics.Complex32[]? l = null;
            MathNet.Numerics.Complex32[]? r = null;

            if (src.LeftBins != null)
            {
                l = new MathNet.Numerics.Complex32[n];
                Array.Copy(src.LeftBins, i0, l, 0, n);
            }

            if (src.RightBins != null)
            {
                r = new MathNet.Numerics.Complex32[n];
                Array.Copy(src.RightBins, i0, r, 0, n);
            }

            return new SpectrumFrame(src.SampleRate, src.FftSize, src.AbsEnd, l, r, fx);
        }

        //        
        private static int LowerBound(double[] a, double x)
        {
            int lo = 0, hi = a.Length;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (a[mid] < x) lo = mid + 1;
                else hi = mid;
            }
            return lo;
        }
        //



        //
        private void SetLevelLabels(string leftText, string rightText)
        {
            // Form1 (si présents)
            if (lblLeftLevelMain != null) lblLeftLevelMain.Text = leftText;
            if (lblRightLevelMain != null) lblRightLevelMain.Text = rightText;

            // DataView (si ouverte / existante)
            if (_dataViewForm != null)
            {
                if (_dataViewForm?.IsRunning == true && _dataViewForm.lblLeftLevelDV != null) _dataViewForm.lblLeftLevelDV.Text = leftText;
                if (_dataViewForm?.IsRunning == true && _dataViewForm.lblRightLevelDV != null) _dataViewForm.lblRightLevelDV.Text = rightText;
            }
        }
        private void ApplyLeftLevelDisplay(string text, Color color, Font font)
        {
            if (lblLeftLevelMain != null)
            {
                lblLeftLevelMain.Text = text;
                lblLeftLevelMain.ForeColor = color;
                lblLeftLevelMain.Font = font;
            }

            if (_dataViewForm?.IsRunning == true && _dataViewForm.lblLeftLevelDV != null)
            {
                _dataViewForm.lblLeftLevelDV.Text = text;
                _dataViewForm.lblLeftLevelDV.ForeColor = color;
                _dataViewForm.lblLeftLevelDV.Font = font;
            }
        }

        private void ApplyRightLevelDisplay(string text, Color color, Font font)
        {
            if (lblRightLevelMain != null)
            {
                lblRightLevelMain.Text = text;
                lblRightLevelMain.ForeColor = color;
                lblRightLevelMain.Font = font;
            }

            if (_dataViewForm?.IsRunning == true && _dataViewForm.lblRightLevelDV != null)
            {
                _dataViewForm.lblRightLevelDV.Text = text;
                _dataViewForm.lblRightLevelDV.ForeColor = color;
                _dataViewForm.lblRightLevelDV.Font = font;
            }
        }



    }

}