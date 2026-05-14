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
        // _audioRing (StereoRingBuffer int24) supprimé : était écrit mais jamais lu.
        // =============================================================
        private RingBufferStereoFloat? _floatRing;

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
        private readonly SfftStats _sfftStats = new SfftStats();

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

        // --- Test source: sine 1 kHz -> écrit dans _floatRing à la place de la carte son ---
        private bool _useSineTest = false;
        // --- Test source: signal de référence "mesures" (THD/SINAD/SNR/SFDR) ---
        private bool _useMetricsRefTest = false;
        private CancellationTokenSource? _sineCts;
        private Task? _sineTask;


        private SfftWorker? _sfftWorker;

        private readonly ConcurrentQueue<SpectrumFrame> _sfftFrames = new ConcurrentQueue<SpectrumFrame>();
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

        // =============================================================
        // ROBUST FIX: Fenêtre d'analyse alimentée sur le thread audio
        // (évite les discontinuités quand N est grand, ex: 1M pts)
        // =============================================================
        private readonly object _analysisWindowLock = new object();

        // Fenêtre circulaire (taille = analysisSampleSize)
        private int[] _analysisWinL = Array.Empty<int>();
        private int[] _analysisWinR = Array.Empty<int>();
        private int _analysisWinCap = 0;     // capacité = N
        private int _analysisWinWrite = 0;   // index d'écriture circulaire
        private int _analysisWinCount = 0;   // nb de samples valides (<= cap)
        private long _analysisWinAbsWrite = 0; // compteur absolu d'écriture (monotone)


        // Index absolu de fin de la dernière fenêtre d’analyse produite (pour hop/overlap)
        private long _lastAnalysisAbsEnd = 0;

        // \u2500\u2500 Buffers snapshot pré-alloués (supprime 32 MB LOH à chaque tick UI) \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500
        // UpdateAnalysisFrame allouait new int[N]×2 par appel \u2192 GC Gen2 STW
        // \u2192 pauses 0.7-2s \u2192 SfftWorker suspendu > 1 hop \u2192 TryReadWindow miss \u2192 saut FFT.
        // Ces tableaux sont réalloués uniquement si N change (rare, sur changement de config).
        private int[] _analysisSnapshotL = Array.Empty<int>();
        private int[] _analysisSnapshotR = Array.Empty<int>();
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
        private int GetUiFftSize()
        {
            // N affiché/choisi dans la GUI (sample size)
            return analysisSampleSize;
        }

        private double GetUiOverlap()
        {
            // Ratio overlap (0..1) tel que géré par la GUI
            return analysisOverlap;
        }


        private int[] lastAnalysisLeft = Array.Empty<int>();
        private int[] lastAnalysisRight = Array.Empty<int>();
        private readonly object analysisLock = new object();

        //     1048576 samples / 44100 Hz = 23,78 s 
        private const int MaxHistogramSeconds = 24; // on garde jusqu' 24 s d'historique

        // --- Mode "Once" pour l'histogramme ---
        //private bool histOnceActive = false;
        //private int histOnceTargetSamples = 0;
        //private int histOnceAccumSamples = 0;

        // Seuils dBFS pour le bargraphe
        private const double DbMin = -80.0;      // plancher d'affichage
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
        private SpectrumControl? spectrumControl;
        private ToolStripMenuItem? menuColors;

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

        private string _lastExportPngTitle = "";


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
        private void StartSineTestSource(int sampleRate, double freqHz = 1000.0, double amp = 0.25)
        {
            if (_floatRing == null) return;
            if (_sineTask != null) return;

            _sineCts = new CancellationTokenSource();
            var ct = _sineCts.Token;

            _sineTask = Task.Run(() =>
            {
                // écrit par paquets pour réduire la charge
                int chunk = 1024;
                var l = new float[chunk];
                var r = new float[chunk];

                double phase = 0.0;
                double dphi = 2.0 * Math.PI * freqHz / sampleRate;

                var sw = Stopwatch.StartNew();
                long total = 0;

                while (!ct.IsCancellationRequested)
                {
                    TestWaves.FillStereoSine(l, r, chunk, amp,0.05, dphi, ref phase); // ajoute offset DC 0.05 pour test mesures DC

                    // Écrit dans le ring (remplace l'entrée carte son)
                    _floatRing.PushBlock(l, r);

                    // En mode F9, on doit aussi alimenter la fenêtre d'analyse "temps" (int24)
                    // sinon RMS/Peak/Calibration ne voient rien.
                    lock (_analysisWindowLock)
                    {
                        EnsureAnalysisWindowCapacity_NoLock(analysisSampleSize);

                        const int Max24 = 1 << 23;        // 8388608
                        const int Max24Pos = Max24 - 1;   // 8388607
                        const int Min24Neg = -Max24;      // -8388608

                        for (int i = 0; i < chunk; i++)
                        {
                            int Li = (int)Math.Round(l[i] * Max24Pos);
                            int Ri = (int)Math.Round(r[i] * Max24Pos);

                            if (Li > Max24Pos) Li = Max24Pos;
                            if (Li < Min24Neg) Li = Min24Neg;
                            if (Ri > Max24Pos) Ri = Max24Pos;
                            if (Ri < Min24Neg) Ri = Min24Neg;

                            FeedAnalysisWindowSample_NoLock(Li, Ri);
                        }
                    }

                    total += chunk;

                    // Throttle pour se caler sur le temps réel (~Fs)
                    // Temps théorique écoulé pour total samples
                    double targetSec = total / (double)sampleRate;
                    double nowSec = sw.Elapsed.TotalSeconds;
                    double sleepSec = targetSec - nowSec;

                    if (sleepSec > 0.001)
                        Thread.Sleep((int)(sleepSec * 1000.0));
                    else
                        Thread.Yield();
                }
            });
        }
        //
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F8)
            {
                ToggleDiagLog();
                return true;
            }

            if (keyData == Keys.F9)
            {
                _useSineTest = !_useSineTest;

                // évite d’avoir 2 modes en même temps
                if (_useSineTest) _useMetricsRefTest = false;

                MessageBox.Show(_useSineTest
                    ? "Sine test ON (1 kHz). Redémarre READ."
                    : "Sine test OFF (carte son). Redémarre READ.");
                return true;
            }

            if (keyData == Keys.F10)
            {
                _useMetricsRefTest = !_useMetricsRefTest;

                // évite d’avoir 2 modes en même temps
                if (_useMetricsRefTest) _useSineTest = false;

                MessageBox.Show(_useMetricsRefTest
                    ? "Ref mesures ON (coherent FFT + harmoniques + spur + bruit). Redémarre READ."
                    : "Ref mesures OFF (carte son). Redémarre READ.");
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        //
        private void StartMetricsReferenceTestSource(int sampleRate)
        {
            if (_floatRing == null) return;
            if (_sineTask != null) return; // on réutilise la même task/cts que le mode F9 (un seul générateur)

            // FFT size utilisée par ton pipeline (celle choisie dans l'UI)
            // -> cohérent avec RestartSfftFromUi() qui redémarre _floatRing/_sfftWorker sur analysisSampleSize
            int fftSize = analysisSampleSize;

            _sineCts = new CancellationTokenSource();
            var ct = _sineCts.Token;

            _sineTask = Task.Run(() =>
            {
                int chunk = 1024;
                var l = new float[chunk];
                var r = new float[chunk];

                // Spéc du signal de référence (coherent FFT)
                var spec = TestWaves.CreateReferenceSpec(
                    fs: sampleRate,
                    fftSize: fftSize,
                    targetFundamentalHz: 1000.0,
                    fundamentalDbfs: -12.0,
                    h2Dbc: -60.0,
                    h3Dbc: -70.0,
                    spurDbc: -80.0,
                    noiseRmsDbfs: -90.0
                );

                double phaseFund = 0.0;
                double phaseSpur = 0.0;
                uint rngState = 0x12345678;

                var sw = Stopwatch.StartNew();
                long total = 0;

                while (!ct.IsCancellationRequested)
                {
                    // Génère le chunk (Bode Plot identiques) via ton vrai générateur
                    TestWaves.FillStereoReferenceSignal(
                        l, r, chunk,
                        spec,
                        ref phaseFund,
                        ref phaseSpur,
                        ref rngState
                    );

                    // Écrit dans le ring (remplace l'entrée carte son)
                    _floatRing.PushBlock(l, r);

                    // Alimente aussi la fenêtre d'analyse "temps" (int24)
                    // pour que RMS/Peak/Calibration fonctionnent comme en mode carte son.
                    lock (_analysisWindowLock)
                    {
                        EnsureAnalysisWindowCapacity_NoLock(analysisSampleSize);

                        const int Max24 = 1 << 23;        // 8388608
                        const int Max24Pos = Max24 - 1;   // 8388607
                        const int Min24Neg = -Max24;      // -8388608

                        for (int i = 0; i < chunk; i++)
                        {
                            int Li = (int)Math.Round(l[i] * Max24Pos);
                            int Ri = (int)Math.Round(r[i] * Max24Pos);

                            if (Li > Max24Pos) Li = Max24Pos;
                            if (Li < Min24Neg) Li = Min24Neg;
                            if (Ri > Max24Pos) Ri = Max24Pos;
                            if (Ri < Min24Neg) Ri = Min24Neg;

                            FeedAnalysisWindowSample_NoLock(Li, Ri);
                        }
                    }

                    total += chunk;

                    // Throttle pour se caler sur le temps réel (~Fs)
                    double targetSec = total / (double)sampleRate;
                    double nowSec = sw.Elapsed.TotalSeconds;
                    double sleepSec = targetSec - nowSec;

                    if (sleepSec > 0.001)
                        Thread.Sleep((int)(sleepSec * 1000.0));
                    else
                        Thread.Yield();
                }
            });
        }


        //
        private void StopSineTestSource()
        {
            try
            {
                _sineCts?.Cancel();
                _sineTask?.Wait(500);
            }
            catch { /* ignore */ }
            finally
            {
                _sineTask = null;
                _sineCts?.Dispose();
                _sineCts = null;
            }
        }
        //
        private void ApplyActiveChannelsToWorker(string mode)
        {
            // Détermine les voies requises par le mode d'affichage.
            // IMPORTANT:
            // - "Left"  => seulement L
            // - "Right" => seulement R
            // - "L+R"   => L et R
            // - "Bode Plot"   => nécessite L et R (FRF/H1)
            bool needL = (mode == "Left" || mode == "L+R" || mode == "Bode Plot");
            bool needR = (mode == "Right" || mode == "L+R" || mode == "Bode Plot");

            _sfftWorker?.SetActiveChannels(needL, needR);
        }
        //
        private void RestartSfftFromUi()
        {
            // Ne rien faire si on n'est pas en acquisition
            if (currentFormat == null) return;

            // 1) Lire les paramètres UI
            int fs = currentFormat.SampleRate;
            int fftSize = GetUiFftSize();        // doit renvoyer une puissance de 2 (8k..4M)
            double overlap = GetUiOverlap();     // [0..1]

            // clamps sécurité
            if (fftSize < 8192) fftSize = 8192;
            if (fftSize > 4194304) fftSize = 4194304;
            if (overlap < 0.0) overlap = 0.0;
            if (overlap > 0.99) overlap = 0.99; // évite hop=0

            // Calcul du hop
            int hop = Math.Max(1, (int)Math.Round(fftSize * (1.0 - overlap)));

            //// Fenêtre sélectionnée dans la GUI (SpectrumControl)
            //var winType = spectrumControl?.SelectedWindowType
            //              ?? SpectrumControl.WindowType.BlackmanHarris7;

            // Arme le compteur dès maintenant (priming), sans attendre la 1ère FFT
            ArmFftTickerPriming(fs, fftSize, hop, overlap);


            // 2) Stop worker existant
            if (_sfftWorker != null)
            {
                _sfftWorker.Stop();
                _sfftWorker = null;
            }

            // 3) Redimensionner / recréer le ring buffer (cohérent avec N)
            // Règle : max(6s, 6*N) pour absorber les pics de latence FFT
            // À 768kHz N=1M : 6×1M = 6M samples = ~48 Mo — acceptable
            int ringCapacity = Math.Max(fs * 6, fftSize * 6);
            _floatRing = new RingBufferStereoFloat(ringCapacity);

            // 4) Vider la queue pour éviter mélange ancien/nouveau N
            while (_sfftFrames.TryDequeue(out var old))
            {
                old.Dispose(); // IMPORTANT: rend les bins au pool
            }

            // IMPORTANT: rendre aussi la dernière frame UI (si elle existait)
            _lastFrameUi?.Dispose();
            _lastFrameUi = null;


            // 5) Redémarrer le worker avec les bons paramètres
            var windowType = spectrumControl?.SelectedWindowType ?? WindowType.BlackmanHarris7;
            _sfftWorker = new SfftWorker(_floatRing, _sfftStats, fs, fftSize, hop, windowType, _sfftFrames);

            // Applique le mode voie courant dès la création du worker
            ApplyActiveChannelsToWorker(spectrumControl?.SelectedChannelMode ?? "L+R");

            _sfftWorker.Start();
            // Met à jour les stats visibles tout de suite (sans attendre la 1ère FFT)
            _sfftStats.SetFormat(sampleRate: fs, fftSize: fftSize, hop: hop, overlapRequested01: overlap);


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
        //
        private sealed class StereoRingBuffer
        {
            private int[] _l;
            private int[] _r;
            private int _write;     // index d'écriture (0..cap-1)
            private int _count;     // nombre d'échantillons valides (<= cap)
            private long _absWrite; // nombre total d'échantillons écrits depuis le dernier reset

            public int Capacity => _l.Length;
            public int Count => _count;
            public long AbsWrite => _absWrite; // index absolu "one past last"

            public StereoRingBuffer(int capacity)
            {
                if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
                _l = new int[capacity];
                _r = new int[capacity];
                _write = 0;
                _count = 0;
                _absWrite = 0;
            }

            public void Resize(int capacity, bool reset = true)
            {
                if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
                _l = new int[capacity];
                _r = new int[capacity];
                if (reset)
                {
                    _write = 0;
                    _count = 0;
                    _absWrite = 0;
                }
                else
                {
                    // Pour l'instant on n'implémente pas de conservation partielle (inutile ici)
                    _write = 0;
                    _count = 0;
                    _absWrite = 0;
                }
            }

            public void Clear(bool resetAbsIndex = true)
            {
                _write = 0;
                _count = 0;
                if (resetAbsIndex) _absWrite = 0;
            }

            public void WriteSample(int left, int right)
            {
                _l[_write] = left;
                _r[_write] = right;

                _write++;
                if (_write >= _l.Length) _write = 0;

                if (_count < _l.Length) _count++;
                _absWrite++;
            }

            /// <summary>
            /// Copie une plage d'échantillons à partir d'un index absolu (absStart) dans des buffers de sortie.
            /// absStart..absStart+length doit être contenu dans la zone encore présente dans le ring.
            /// </summary>
            public bool TryCopyFromAbs(long absStart, int length, int[] dstL, int[] dstR)
            {
                if (length <= 0) return false;
                if (dstL.Length < length || dstR.Length < length) return false;

                long absEnd = _absWrite;
                long absOldest = absEnd - _count;

                if (absStart < absOldest) return false;
                if (absStart + length > absEnd) return false;

                int cap = _l.Length;

                // index de l'échantillon le plus ancien dans le tableau
                int oldestIndex = (_count == cap) ? _write : 0;

                long offsetFromOldest = absStart - absOldest;
                int idx = (int)((oldestIndex + offsetFromOldest) % cap);

                int remaining = length;
                int outPos = 0;

                while (remaining > 0)
                {
                    int chunk = Math.Min(remaining, cap - idx);
                    Array.Copy(_l, idx, dstL, outPos, chunk);
                    Array.Copy(_r, idx, dstR, outPos, chunk);
                    remaining -= chunk;
                    outPos += chunk;
                    idx = 0;
                }

                return true;
            }

            public bool TryCopyLast(int length, int[] dstL, int[] dstR)
            {
                long absEnd = _absWrite;
                long absStart = absEnd - length;
                return TryCopyFromAbs(absStart, length, dstL, dstR);
            }
        }


        // Fournit la DERNIÈRE fenêtre d'analyse commune (Histogram + Spectrum).
        // Les contrôles appellent ce provider avec sampleCount (= analysisSampleSize).
        // Fournit la DERNIÈRE fenêtre d'analyse commune (Histogram + Spectrum).
        // Les contrôles appellent ce provider avec sampleCount (= analysisSampleSize).
        private (int[] Left, int[] Right) GetAnalysisFrame(int sampleCount)
        {
            lock (analysisLock)
            {
                if (sampleCount <= 0)
                    return (Array.Empty<int>(), Array.Empty<int>());

                // Chemin rapide : tailles identiques
                if (lastAnalysisLeft.Length == sampleCount && lastAnalysisRight.Length == sampleCount)
                    return ((int[])lastAnalysisLeft.Clone(), (int[])lastAnalysisRight.Clone());

                // Sinon : tronque / zero-pad (on prend toujours les derniers échantillons disponibles)
                int[] l = new int[sampleCount];
                int[] r = new int[sampleCount];

                int copy = Math.Min(sampleCount, lastAnalysisLeft.Length);
                if (copy > 0)
                    Array.Copy(lastAnalysisLeft, lastAnalysisLeft.Length - copy, l, sampleCount - copy, copy);

                copy = Math.Min(sampleCount, lastAnalysisRight.Length);
                if (copy > 0)
                    Array.Copy(lastAnalysisRight, lastAnalysisRight.Length - copy, r, sampleCount - copy, copy);

                return (l, r);
            }
        }
        //
        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            SaveSessionSettingsSilently();
        }
        //
        private void InitAnalysisUi()
        {
            // Init hop + timer
            RecomputeHopAndTimer();

            // Pousse les valeurs aux contrôles si déjà créés
            histogramControl?.SetGlobalSampleSize(analysisSampleSize);
            histogramControl?.UpdateHopSamples(analysisHopSamples);

            spectrumControl?.SetGlobalSampleSize(analysisSampleSize);
            spectrumControl?.UpdateHopSamples(analysisHopSamples);

        }
        //

        private void SetAnalysisSampleSize(int nSamples)
        {
            if (nSamples < 8192) nSamples = 8192;
            if (nSamples > 4194304) nSamples = 4194304;

            analysisSampleSize = nSamples;
            //analysisCursorStart = 0; // repart sur la fin du buffer

            RecomputeHopAndTimer();

            histogramControl?.SetGlobalSampleSize(analysisSampleSize);
            histogramControl?.UpdateHopSamples(analysisHopSamples);

            spectrumControl?.SetGlobalSampleSize(analysisSampleSize);
            spectrumControl?.UpdateHopSamples(analysisHopSamples);

            // (Étape 5) Restart immédiat du worker SFFT si la capture tourne
            if (capture != null && ((capture as WasapiCapture)?.CaptureState ?? CaptureState.Capturing) == CaptureState.Capturing)
            {
                RestartSfftFromUi();
            }

        }
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

        private void InitSampleSizeMenu()
        {
            if (menuSampleSize == null) return;

            menuSampleSize.DropDownItems.Clear();

            foreach (int n in _sampleSizeOptions)
            {
                var it = new ToolStripMenuItem(n.ToString("N0"))
                {
                    Tag = n,
                    CheckOnClick = true
                };

                it.Click += (s, e) =>
                {
                    if (_updatingSampleSizeMenuUi) return;

                    int chosen = (int)((ToolStripMenuItem)s!).Tag!;
                    _updatingSampleSizeMenuUi = true;
                    try
                    {
                        SetAnalysisSampleSize(chosen);
                        UpdateSampleSizeMenuChecks();
                    }
                    finally
                    {
                        _updatingSampleSizeMenuUi = false;
                    }
                };

                menuSampleSize.DropDownItems.Add(it);
            }
        }
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
        private void InitBufferMenu()
        {
            if (menuBuffer == null) return;

            menuBuffer.DropDownItems.Clear();

            foreach (int n in _bufferOptions)
            {
                string label = (n / 1024).ToString() + "k";

                var it = new ToolStripMenuItem(label)
                {
                    Tag = n,
                    CheckOnClick = true
                };

                it.Click += (s, e) =>
                {
                    if (_updatingBufferMenuUi) return;

                    int chosen = (int)((ToolStripMenuItem)s!).Tag!;
                    _updatingBufferMenuUi = true;
                    try
                    {
                        SetAnalysisBufferSize(chosen);
                        UpdateBufferMenuChecks();
                    }
                    finally
                    {
                        _updatingBufferMenuUi = false;
                    }
                };

                menuBuffer.DropDownItems.Add(it);
            }
        }
        //
        private void UpdateBufferMenuChecks()
        {
            if (menuBuffer == null) return;

            foreach (ToolStripMenuItem it in menuBuffer.DropDownItems)
            {
                int n = (int)it.Tag!;
                it.Checked = (n == analysisBufferSize);
            }
        }
        //
        private void SetAnalysisBufferSize(int bufferSize)
        {
            if (bufferSize < 16 * 1024) bufferSize = 16 * 1024;
            if (bufferSize > 512 * 1024) bufferSize = 512 * 1024;

            analysisBufferSize = bufferSize;

            _lastAnalysisAbsEnd = 0;
        }
        //
        private void ResetAnalysisPipeline()
        {
            _lastAnalysisAbsEnd = 0;
        }

        //
        //
        private void InitOverlapMenu()
        {
            if (menuOverlap == null) return;

            menuOverlap.DropDownItems.Clear();

            foreach (var (label, value) in _overlapOptions)
            {
                var it = new ToolStripMenuItem(label)
                {
                    Tag = value,
                    CheckOnClick = true
                };

                it.Click += (s, e) =>
                {
                    if (_updatingOverlapMenuUi) return;

                    double chosen = (double)((ToolStripMenuItem)s!).Tag!;
                    _updatingOverlapMenuUi = true;
                    try
                    {
                        SetAnalysisOverlap(chosen);
                        UpdateOverlapMenuChecks();
                    }
                    finally
                    {
                        _updatingOverlapMenuUi = false;
                    }
                };

                menuOverlap.DropDownItems.Add(it);
            }
        }

        private void UpdateOverlapMenuChecks()
        {
            if (menuOverlap == null) return;

            foreach (ToolStripMenuItem it in menuOverlap.DropDownItems)
                it.Checked = (Math.Abs((double)it.Tag! - analysisOverlap) < 1e-12);
        }

        //
        private void UpdateSampleSizeMenuChecks()
        {
            if (menuSampleSize == null) return;

            foreach (ToolStripMenuItem it in menuSampleSize.DropDownItems)
                it.Checked = ((int)it.Tag! == analysisSampleSize);
        }

        //
        private void SetAnalysisOverlap(double overlap)
        {
            if (overlap < 0.0) overlap = 0.0;
            if (overlap > 0.99) overlap = 0.99;

            analysisOverlap = overlap;
            //analysisCursorStart = 0;

            RecomputeHopAndTimer();

            histogramControl?.UpdateHopSamples(analysisHopSamples);
            spectrumControl?.UpdateHopSamples(analysisHopSamples);

            // (Étape 5) Restart immédiat du worker SFFT si la capture tourne
            if (capture != null && ((capture as WasapiCapture)?.CaptureState ?? CaptureState.Capturing) == CaptureState.Capturing)
            {
                RestartSfftFromUi();
            }

        }

        private void RecomputeHopAndTimer()
        {
            analysisHopSamples = (int)Math.Round(analysisSampleSize * (1.0 - analysisOverlap));
            if (analysisHopSamples < 1) analysisHopSamples = 1;

            if (analysisTimer == null)
            {
                analysisTimer = new System.Windows.Forms.Timer();
                analysisTimer.Tick += (s, e) => UpdateAnalysisFrame();
            }

            // ROBUST FIX: timer UI = cadence d'affichage, pas cadence d'acquisition
            analysisTimer.Interval = 20; // 50 Hz UI (ajuste si besoin: 10..50 ms)

            // Le timer ne tourne que quand la capture est active
            if (capture != null && ((capture as WasapiCapture)?.CaptureState ?? CaptureState.Capturing) == CaptureState.Capturing)
                analysisTimer.Start();
            else
                analysisTimer.Stop();
        }
        //
        private void EnsureAnalysisWindowCapacity_NoLock(int requiredN)
        {
            if (requiredN <= 0) return;

            if (_analysisWinCap == requiredN)
                return;

            _analysisWinL = new int[requiredN];
            _analysisWinR = new int[requiredN];
            _analysisWinCap = requiredN;
            _analysisWinWrite = 0;
            _analysisWinCount = 0;
            _analysisWinAbsWrite = 0;

            // On reset le hop pour forcer une première publication propre
            _lastAnalysisAbsEnd = 0;
        }

        // Appelé sur le thread audio (à chaque sample)
        private void FeedAnalysisWindowSample_NoLock(int L, int R)
        {
            if (_analysisWinCap <= 0) return;

            _analysisWinL[_analysisWinWrite] = L;
            _analysisWinR[_analysisWinWrite] = R;

            _analysisWinWrite++;
            if (_analysisWinWrite >= _analysisWinCap)
                _analysisWinWrite = 0;

            if (_analysisWinCount < _analysisWinCap)
                _analysisWinCount++;

            _analysisWinAbsWrite++;
        }

        // Snapshot des N derniers samples (thread UI)
        private bool TrySnapshotAnalysisWindowLast_NoLock(int N, int[] dstL, int[] dstR, out long absEnd)
        {
            absEnd = _analysisWinAbsWrite;

            if (_analysisWinCap < N) return false;
            if (_analysisWinCount < N) return false;

            int start = _analysisWinWrite - N;
            if (start < 0) start += _analysisWinCap;

            int firstPart = Math.Min(N, _analysisWinCap - start);

            Array.Copy(_analysisWinL, start, dstL, 0, firstPart);
            Array.Copy(_analysisWinR, start, dstR, 0, firstPart);

            int remaining = N - firstPart;
            if (remaining > 0)
            {
                Array.Copy(_analysisWinL, 0, dstL, firstPart, remaining);
                Array.Copy(_analysisWinR, 0, dstR, firstPart, remaining);
            }

            return true;
        }

        //

        private void UpdateAnalysisFrame()
        {
            if (currentFormat == null) return;

            // Le ticker doit bouger même si aucune nouvelle FFT n'arrive
            TickFftTickerUi();

            // --- Étape 3 : afficher la FFT produite par le worker (pas de FFT UI) ---
            int drained = 0;
            SpectrumFrame? last = null;

            while (_sfftFrames.TryDequeue(out var f))
            {
                // On conserve uniquement la dernière frame
                if (last != null)
                    last.Dispose(); // rend au pool celle qu'on s'apprêtait à remplacer

                last = f;
                drained++;
            }


            if (drained > 0)
            {
                ResetFftTickerFromCurrentStats(); // réarme à chaque nouvelle FFT (après la 1ère, passe en hop/Fs)
                // --- Mise à jour du titre : DOIT tourner même sans nouvelle FFT ---
                // (sinon prog% ne peut pas "compter" entre 2 FFT)
                double t = _uiSw.Elapsed.TotalSeconds;
            

                if (t - _uiLastRateT > 0.2) // 5 Hz sur le titre (suffisant et stable)
                {
                    var snap = _sfftStats.Snapshot();

                    double expectedFps = (snap.Hop > 0 && snap.SampleRate > 0)
                        ? (snap.SampleRate / (double)snap.Hop)
                        : 0.0;

                    double progPct = snap.Progress01 * 100.0;

                    // Option REW-like : n'affiche le prog que si c'est lent (sinon ça va trop vite)
                    string progStr = (expectedFps < 1.0) ? $" prog~{progPct:F0}%" : "";

                    string absStr = (_lastFrameUi != null)
                        ? $" AbsEnd={_lastFrameUi.AbsEnd}"
                        : $" AbsWrite={snap.AbsWrite}";

                    // --- Barre de debug (lblDebugInfo) + titre de fenêtre ---
                    // Le titre n'affiche plus les infos de debug (évite la troncature Windows).
                    // Toutes les infos sont dans la barre interne, activée via View > Debug info.
                    string fullErr = (snap.LastError ?? "").Replace("\r", " ").Replace("\n", " ");
                    string fullErrStr = string.IsNullOrWhiteSpace(fullErr) ? "" : $" | ERR: {fullErr}";
                    _lastDebugBarText =
                        $"FFT/s act={snap.FftPerSec:F1} | FFT={snap.FftAvgMs:F3} ms{fullErrStr}";

                    // Titre de fenêtre : nom de l'appli uniquement (stable, jamais tronqué)
                    if (this.Text != "AudioHexViewer")
                        this.Text = "AudioHexViewer";

                    // Mise à jour de la barre debug interne — uniquement si le texte a changé
                    // (évite le clignotement causé par une réaffectation inutile)
                    if (panelDebugInfo.Visible && lblDebugInfo.Text != _lastDebugBarText)
                        lblDebugInfo.Text = _lastDebugBarText;

                    _uiLastRateT = t;
                }

            _uiFramesDrained += drained;
            }

            if (last != null && spectrumControl != null)
            {
                // On remplace la frame UI précédente => il faut la rendre au pool
                _lastFrameUi?.Dispose();
                _lastFrameUi = last;

                // Log de diagnostic : une ligne par FFT produite (si F8 actif)
                if (_diagLogEnabled)
                    WriteDiagLogLine(_lastFrameUi, drained);

                // Throttle: on draine toujours, mais on n'affiche qu'à 10 Hz max
                double tNow = _uiSw.Elapsed.TotalSeconds;
                bool canDisplay = (tNow - _uiLastDisplayT) >= UiDisplayPeriodSec;

                // N'afficher la FFT QUE si l'onglet Spectrum est en cours (bouton RUN spectrum actif)
                // => les 2 boutons RUN sont vraiment indépendants
                if (canDisplay && _spectrumRunning)
                {
                    spectrumControl.EnableExternalSpectrumMode(true);

                    // IMPORTANT : on envoie TOUJOURS la frame complète pour que les métriques soient justes.
                    // La décimation sera faite dans SpectrumControl uniquement pour le tracé.
                    spectrumControl.FeedExternalSpectrum(_lastFrameUi);


                }

            }


            int N = analysisSampleSize;
            if (N <= 0) return;

            int hop = analysisHopSamples > 0 ? analysisHopSamples : N;

            // ── Réutilisation des buffers snapshot pré-alloués ────────────────
            // Évite new int[N]×2 à chaque tick UI → plus de GC Gen2 STW pendant
            // les premières secondes qui causaient les sauts de hop dans SfftWorker.
            // Réallocation uniquement si N change (changement de config par l'user).
            if (_analysisSnapshotL.Length != N)
            {
                _analysisSnapshotL = new int[N];
                _analysisSnapshotR = new int[N];
            }
            int[] left  = _analysisSnapshotL;
            int[] right = _analysisSnapshotR;
            // ─────────────────────────────────────────────────────────────────

            long absEnd;

            // ── Phase 1 : validation sous lock (microsecondes) ───────────────
            // On ne fait QUE vérifier la disponibilité et calculer snapStart.
            // La copie (jusqu'à 32 MB à N=4M) se fait HORS lock en Phase 2.
            int snapStart;
            lock (_analysisWindowLock)
            {
                EnsureAnalysisWindowCapacity_NoLock(N);

                if (_analysisWinCount < N) return;        // pas encore assez de données

                absEnd = _analysisWinAbsWrite;

                // Hop/overlap : ne publier que si assez de nouveaux samples
                if (_lastAnalysisAbsEnd != 0 && (absEnd - _lastAnalysisAbsEnd) < hop)
                    return;

                _lastAnalysisAbsEnd = absEnd;

                // Index de départ de la fenêtre dans le ring circulaire
                snapStart = _analysisWinWrite - N;
                if (snapStart < 0) snapStart += _analysisWinCap;
            }
            // ── Phase 2 : copie SANS lock ─────────────────────────────────────
            // Pendant cette copie, le writer (_processTask) peut avancer de
            // quelques milliers de samples au plus, écrasant les slots les PLUS
            // ANCIENS du ring. Or _analysisWinCap == N : au pire ~0,1% des samples
            // en tête de fenêtre sont légèrement désynchronisés. Imperceptible
            // pour les calculs VU/RMS/peak. Gain : lock tenu ~1µs au lieu de ~5ms.
            int firstPart = Math.Min(N, _analysisWinCap - snapStart);
            Array.Copy(_analysisWinL, snapStart, left,  0,         firstPart);
            Array.Copy(_analysisWinR, snapStart, right, 0,         firstPart);
            if (firstPart < N)
            {
                Array.Copy(_analysisWinL, 0, left,  firstPart, N - firstPart);
                Array.Copy(_analysisWinR, 0, right, firstPart, N - firstPart);
            }

            // Stats temporelles (VU, RMS, peak, Vdc, calibration) — hors lock
            // Guard : on n'alimente le spectre que si son propre RUN est actif
            if (spectrumControl != null && _spectrumRunning)
                spectrumControl.FeedExternalTimeStats24(left, right);

            // Publie la nouvelle frame d'analyse (pour SpectrumControl + histogramme)
            lock (analysisLock)
            {
                lastAnalysisLeft = left;
                lastAnalysisRight = right;
            }
        }




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


            // Relance le SFFT quand l'utilisateur change la fenêtre
            if (spectrumControl != null)
            {
                spectrumControl.WindowTypeChangedForExternalFft += (_, __) =>
                {
                    // Si le SFFT n'est pas démarré, ne rien faire.
                    // Si tu as un bool de run, utilise-le, sinon on fait simple :
                    if (_sfftWorker != null)
                        RestartSfftFromUi();
                };

               
            }

            InitDataView();

            InitAnalysisUi();
           
            InitColorsMenu();

            //this.WindowState = FormWindowState.Maximized; // Plein cran au dmarrage
            ShowHistogramArea();
            ShowSpectrumArea();

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

            comboDevices.DisplayMember = "FriendlyName";
            LoadDevices();

            comboDevices.SelectedIndexChanged += comboDevices_SelectedIndexChanged;

            // Timer calibration
            calibTimer = new System.Windows.Forms.Timer();
            calibTimer.Interval = 50;
            calibTimer.Tick += CalibTimer_Tick;

            // Charger la calibration existante
            LoadCalibration();

            // Crer les repres dBFS au-dessus des barres
            CreateDbMarkers(
                barLeftLevel,
                new[] { "-80", "-36", "-12", "-6", "0" },
                new[] { -80, DbBlueToGreen, DbGreenToOrange, DbOrangeToRed, 0.0 }
            );

            CreateDbMarkers(
                barRightLevel,
                new[] { "-80", "-36", "-12", "-6", "0" },
                new[] { -80, DbBlueToGreen, DbGreenToOrange, DbOrangeToRed, 0.0 }
            );

            // 
            InitSampleSizeMenu();
            InitOverlapMenu();
            UpdateSampleSizeMenuChecks();
            UpdateOverlapMenuChecks();
            InitBufferMenu();
            UpdateBufferMenuChecks();
            InitSampleRateMenu();
            UpdateSampleRateMenuChecks();

            this.FormClosing += Form1_FormClosing;
        }
        //
        private void InitDataView()
        {
            _dataViewForm = new DataViewForm();

            // Optionnel mais pratique : la placer près de Form1
            _dataViewForm.StartPosition = FormStartPosition.Manual;
            _dataViewForm.Left = this.Right + 10;
            _dataViewForm.Top = this.Top;

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
            _dataViewForm.Hide();
        }
        //
        private void menuDataView_Click(object? sender, EventArgs e)
        {
            if (_dataViewForm == null)
                InitDataView();

            _dataViewForm!.Show();
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
            menuColors = new ToolStripMenuItem("Colors")
            {
                Alignment = ToolStripItemAlignment.Right
            };

            menuColors.Click += (_, __) => ShowColorsDialog();

            // Insert right after Measures (same pattern as InitCursorsMenu)
            // Ajouter au menu principal (top-level)
            menuStrip1.Items.Add(menuColors);
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
            UpdateSampleRateMenuChecks();

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

            if (ui.CaptureOutput)
                rbOutput.Checked = true;   // CheckedChanged appelle LoadDevices()
            else
                rbInput.Checked = true;

            // 2) Device ID
            if (!string.IsNullOrEmpty(ui.DeviceId))
            {
                for (int i = 0; i < comboDevices.Items.Count; i++)
                {
                    if (comboDevices.Items[i] is MMDevice dev && dev.ID == ui.DeviceId)
                    {
                        comboDevices.SelectedIndex = i;
                        break;
                    }
                }
            }
            // 3) Main Data Format
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
            }
        }

        //
        private SessionSettings BuildSessionSettingsSnapshot()
        {
            var s = new SessionSettings
            {
                Version = 1
            };

            // UI
            string? devId = null;
            if (comboDevices.SelectedItem is MMDevice dev)
                devId = dev.ID;

            s.Ui.DeviceId = devId;
            s.Ui.CaptureOutput = captureOutput;
            s.Ui.MainDataFormat = _dataViewForm!.comboDisplayMode.SelectedItem as string;

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

            WaveFormat[] candidates = new WaveFormat[]
            {
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

        private void InitSampleRateMenu()
        {
            if (menuSampleRate == null) return;

            menuSampleRate.DropDownItems.Clear();

            // Première entrée : mode Auto (Windows Shared Mode, SR imposé par Windows)
            var autoItem = new ToolStripMenuItem("Auto (Windows)")
            {
                Tag = 0,
                Checked = (_requestedCaptureFormat == null)
            };
            autoItem.Click += (s, e) =>
            {
                _requestedCaptureFormat = null;
                UpdateSampleRateMenuChecks();
            };
            menuSampleRate.DropDownItems.Add(autoItem);

            menuSampleRate.DropDownItems.Add(new ToolStripSeparator());

            // Une entrée par sample rate
            foreach (int sr in _sampleRateOptions)
            {
                string label = sr < 1000
                    ? $"{sr} Hz"
                    : $"{sr / 1000.0:0.###} kHz";

                var item = new ToolStripMenuItem(label)
                {
                    Tag = sr
                };

                item.Click += (s, e) =>
                {
                    if (s is not ToolStripMenuItem clicked) return;
                    int chosen = (int)clicked.Tag!;
                    // WaveFormatExtensible requis pour le mode Exclusif
                    // Le constructeur 3-args règle automatiquement le sous-type IEEE Float pour 32 bits
                    _requestedCaptureFormat = new WaveFormatExtensible(chosen, 32, 2);
                    UpdateSampleRateMenuChecks();
                };

                menuSampleRate.DropDownItems.Add(item);
            }
        }

        private void UpdateSampleRateMenuChecks()
        {
            if (menuSampleRate == null) return;

            int currentSr = _requestedCaptureFormat?.SampleRate ?? 0;

            foreach (ToolStripItem rawItem in menuSampleRate.DropDownItems)
            {
                if (rawItem is not ToolStripMenuItem item) continue;

                int tag = (item.Tag is int t) ? t : -1;

                if (tag == 0)
                    item.Checked = (_requestedCaptureFormat == null); // Auto
                else
                    item.Checked = (tag == currentSr);
            }
        }

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

            // Si aucun device slectionn
            if (comboDevices.SelectedItem is not MMDevice dev)
            {
                //lblDeviceSummary.Text = "No device selected";
                return;
            }

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
        private void menuDeviceInfo_Click(object? sender, EventArgs e)
        {
            try
            {
                string text = BuildDeviceInfoText();

                using (var dlg = new Form())
                {
                    dlg.Text = "Audio device info";
                    dlg.StartPosition = FormStartPosition.CenterParent;
                    dlg.Size = new Size(500, 350);
                    dlg.MinimizeBox = false;
                    dlg.MaximizeBox = false;
                    dlg.ShowIcon = false;

                    var box = new RichTextBox();
                    box.Dock = DockStyle.Fill;
                    box.ReadOnly = true;
                    box.BorderStyle = BorderStyle.None;
                    box.Font = new Font(FontFamily.GenericMonospace, 9f);
                    box.Text = text;
                    box.BackColor = SystemColors.Window;

                    dlg.Controls.Add(box);

                    dlg.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Crash in DeviceInfos", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //
        //
        private void menuShowAllFormats_Click(object? sender, EventArgs e)
        {
            // Si la fentre existe dj, on la raffiche simplement
            if (dataFormatsWindow != null && !dataFormatsWindow.IsDisposed)
            {
                dataFormatsWindow.Show();
                dataFormatsWindow.BringToFront();
                return;
            }

            var dlg = new Form();
            dataFormatsWindow = dlg;

            dlg.Text = "All data formats";
            dlg.StartPosition = FormStartPosition.CenterParent;

            // Taille par dfaut : un peu large pour faire deux colonnes
            dlg.Size = new Size(700, 160); // Taille de la fentre pour 2 colonnes en popu^p
            dlg.MinimizeBox = false;
            dlg.MaximizeBox = true;
            dlg.ShowIcon = false;

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical
            };

            // Au cas o ils auraient encore un parent (scurit)
            if (txtLeft.Parent != null)
                txtLeft.Parent.Controls.Remove(txtLeft);
            if (txtRight.Parent != null)
                txtRight.Parent.Controls.Remove(txtRight);

            txtLeft.Dock = DockStyle.Fill;
            txtRight.Dock = DockStyle.Fill;
            txtLeft.Visible = true;
            txtRight.Visible = true;


            split.Panel1.Controls.Add(txtLeft);
            split.Panel2.Controls.Add(txtRight);

            dlg.Controls.Add(split);

            // Sparation au milieu  l'ouverture
            dlg.Load += (_, __) =>
            {
                split.SplitterDistance = dlg.ClientSize.Width / 2;
            };

            // On garde ~50/50 si on redimensionne
            dlg.Resize += (_, __) =>
            {
                split.SplitterDistance = dlg.ClientSize.Width / 2;
            };

            // Trs important : ne pas dtruire les contrles  la fermeture
            dlg.FormClosing += (s, eArgs) =>
            {
                if (eArgs.CloseReason == CloseReason.UserClosing)
                {
                    // L'utilisateur clique sur la croix de la petite fentre :
                    // on cache juste, mais on ne dtruit pas txtLeft / txtRight.
                    eArgs.Cancel = true;
                    dlg.Hide();
                }
                // Pour les autres cas (fermeture de l'appli, shutdown, etc.),
                // on laisse faire (pas de Cancel).
            };


            dlg.Show(this);
        }

        //
        //
        private void menuAbout_Click(object? sender, EventArgs e)
        {
            MessageBox.Show(
                "AudioHexViewer V0.9 01/2026\n\n" +
                "Digital audio data Anayzer\n" +
                "OnE Audio Projects",
                "About");
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
        private void LoadDevices()
        {
            comboDevices.Items.Clear();

            var enumerator = new MMDeviceEnumerator();

            // IN -> priphriques de capture, OUT -> priphriques de rendu
            var flow = captureOutput ? DataFlow.Render : DataFlow.Capture;

            var devices = enumerator.EnumerateAudioEndPoints(flow, DeviceState.Active);
            foreach (var dev in devices)
            {
                // Tu avais dj un DeviceItem qui expose FriendlyName
                comboDevices.Items.Add(dev);
            }

            if (comboDevices.Items.Count > 0)
                comboDevices.SelectedIndex = 0;
        }

        private void comboDevices_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // On nest plus en train de capturer si on change de device
            currentFormat = null;
            UpdateDeviceInfo();
            UpdateDeviceSummary();   // 
        }
        //
        /// <summary>
        /// Démarre la capture audio + pipeline d'analyse si elle n'est pas déjà active.
        /// Remplace l'ancien bouton READ.
        /// </summary>
        private void StartCapture()
        {
            if (capture != null)
                return; // déjà démarré

            if (comboDevices.SelectedItem is not MMDevice dev)
            {
                MessageBox.Show("Selectionne un priphrique audio.");
                return;
            }

            selectedDevice = dev;

            try
            {
                // === Choix IN / OUT + Exclusif si SR explicite demandé ===
                if (!captureOutput)
                {
                    if (_requestedCaptureFormat != null)
                    {
                        // Négocie le format réel supporté par ce driver pour ce SR
                        int requestedSr = _requestedCaptureFormat.SampleRate;
                        WaveFormat? actualFormat = FindSupportedExclusiveFormat(selectedDevice, requestedSr);

                        if (actualFormat == null)
                        {
                            MessageBox.Show(
                                $"Le SR {requestedSr / 1000.0:0.###} kHz n'est pas supporté en mode\n" +
                                "Exclusif par ce périphérique (aucun format 32/24/16 bits accepté).\n\n" +
                                "Choisis 'Auto (Windows)' dans le menu Sample Rate.",
                                "Format non supporté",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                            capture = null;
                            return;
                        }

                        capture = new ExclusiveWasapiCapture(selectedDevice, actualFormat);
                    }
                    else
                    {
                        // Mode Auto : Shared Mode standard, SR imposé par Windows
                        capture = new WasapiCapture(selectedDevice, useEventSync: false, audioBufferMillisecondsLength: 100);
                    }
                }
                else
                {
                    // Mode OUT : loopback — toujours Shared, SR du renderer Windows
                    capture = new WasapiLoopbackCapture(selectedDevice);
                }
                // ============================================================

                currentFormat = capture.WaveFormat;

                if (histogramControl != null && currentFormat != null)
                {
                    histogramControl.UpdateSampleRate(currentFormat.SampleRate);
                    histogramControl.UpdateHopSamples(analysisHopSamples);
                }

                capture.DataAvailable += Capture_DataAvailable;
                capture.RecordingStopped += Capture_RecordingStopped;

                // Met à jour le sample rate des modules d'analyse
                if (currentFormat != null)
                {
                    if (histogramControl != null)
                    {
                        histogramControl.UpdateSampleRate(currentFormat.SampleRate);
                        histogramControl.UpdateHopSamples(analysisHopSamples);
                    }

                    if (spectrumControl != null)
                    {
                        spectrumControl.UpdateSampleRate(currentFormat.SampleRate);
                        spectrumControl.UpdateHopSamples(analysisHopSamples);
                    }
                }

                // ── Mode GC bas-latence pendant la capture ────────────────────────
                // SustainedLowLatency interdit les GC Gen2 sauf urgence mémoire.
                // Sans ça, les allocations LOH répétées (buffers snapshot, etc.)
                // déclenchent des pauses STW de 0.7-2s qui suspendent SfftWorker
                // pendant plus d'un hop → TryReadWindow miss → sauts de FFT.
                System.Runtime.GCSettings.LatencyMode =
                    System.Runtime.GCLatencyMode.SustainedLowLatency;
                // ─────────────────────────────────────────────────────────────────

                // Démarrage capture audio
                capture.StartRecording();

                // ── Canal de découplage capture → ProcessBuffer ──────────────────
                // Créé APRÈS StartRecording pour ne pas perdre les premiers callbacks.
                // BoundedCapacity=8 → ~80ms de tampon.
                // DropNewest (≠ DropOldest) : quand le canal est plein, on rejette
                // le NOUVEAU paquet entrant (TryWrite retourne false) plutôt que
                // d'éjecter l'ancien. Avantage : on peut COMPTER les drops via la
                // valeur de retour de TryWrite → visible dans le diagnostic titre.
                _processCts?.Cancel();
                _processCts?.Dispose();
                _processCts = new CancellationTokenSource();
                _captureChannel = Channel.CreateBounded<(byte[], int)>(
                    new BoundedChannelOptions(8)
                    {
                        FullMode     = BoundedChannelFullMode.DropNewest,
                        SingleReader = true,
                        SingleWriter = true
                    });

                var procToken = _processCts.Token;
                _processTask = Task.Run(async () =>
                {
                    await foreach (var (buf, count) in
                        _captureChannel.Reader.ReadAllAsync(procToken))
                    {
                        try   { ProcessBuffer(buf, count); }
                        finally { ArrayPool<byte>.Shared.Return(buf); }
                    }
                }, procToken);
                // ─────────────────────────────────────────────────────────────────

                // Réinitialisation pipeline d'analyse
                ResetAnalysisPipeline();

                // Démarre pipeline SFFT (ring + worker) avec UI courants
                RestartSfftFromUi();

                // --- START test source ---
                if (currentFormat != null)
                {
                    if (_useMetricsRefTest)
                        StartMetricsReferenceTestSource(currentFormat.SampleRate);
                    else if (_useSineTest)
                        StartSineTestSource(currentFormat.SampleRate, 1000.0, 0.25);
                }

                // Démarre le générateur de fenêtres d'analyse (Histogram + Spectrum)
                RecomputeHopAndTimer();
                analysisTimer?.Start();

                UpdateDeviceInfo();
                UpdateDeviceSummary();
            }
            catch (Exception ex)
            {
                // Décodage des erreurs WASAPI les plus fréquentes
                string errMsg;
                switch (unchecked((uint)ex.HResult))
                {
                    case 0x88890008: // AUDCLNT_E_UNSUPPORTED_FORMAT
                        errMsg = $"Format non supporté par le driver en mode Exclusif.\n" +
                                 $"({_requestedCaptureFormat?.SampleRate / 1000.0:0.###} kHz / 32 bits Float)\n\n" +
                                 "Solution : choisir 'Auto' dans le menu Sample Rate, ou essayer un autre SR.";
                        break;
                    case 0x88890001: // AUDCLNT_E_ALREADY_INITIALIZED
                    case 0x88890014: // AUDCLNT_E_DEVICE_IN_USE
                        errMsg = "Le périphérique est déjà utilisé par une autre application en mode Exclusif.\n\n" +
                                 "Ferme REW, le panneau Son de Windows, ou toute autre appli audio, puis réessaie.";
                        break;
                    case 0x88890004: // AUDCLNT_E_BUFFER_SIZE_NOT_ALIGNED
                        errMsg = "Taille de buffer incompatible avec ce driver en mode Exclusif.\n" +
                                 "Essaie un autre SR ou repasse en mode Auto.";
                        break;
                    default:
                        errMsg = $"Erreur lors du démarrage de la capture :\n{ex.Message}\n(HRESULT: 0x{ex.HResult:X8})";
                        break;
                }
                MessageBox.Show(errMsg, "Erreur capture", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                capture = null;
                currentFormat = null;

                UpdateDeviceInfo();
                UpdateDeviceSummary();
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // LOGGER DE DIAGNOSTIC FFT  (F8 pour activer/désactiver)
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Active ou désactive le logger CSV (F8). Une ligne par FFT produite.
        /// Fichier créé dans Mes Documents : AudioHexViewer_Diag_YYYYMMDD_HHmmss.csv
        /// </summary>
        private void ToggleDiagLog()
        {
            lock (_diagLogLock)
            {
                if (_diagLogEnabled)
                {
                    // ── Arrêt du log ──────────────────────────────────────────
                    _diagLogEnabled = false;
                    _diagLogWriter?.Flush();
                    _diagLogWriter?.Close();
                    _diagLogWriter = null;

                    string path = _diagLogPath ?? "(inconnu)";
                    BeginInvoke((Action)(() =>
                        MessageBox.Show($"Log arrêté.\nFichier : {path}",
                            "Diagnostic Log", MessageBoxButtons.OK, MessageBoxIcon.Information)));
                }
                else
                {
                    // ── Démarrage du log ──────────────────────────────────────
                    string folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    _diagLogPath   = Path.Combine(folder,
                        $"AudioHexViewer_Diag_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

                    _diagLogWriter = new StreamWriter(_diagLogPath, append: false,
                        encoding: Encoding.UTF8) { AutoFlush = false };

                    // En-tête lisible
                    var snap   = _sfftStats.Snapshot();
                    string mode = (_requestedCaptureFormat != null) ? "Exclusif" : "Shared";
                    _diagLogWriter.WriteLine("# AudioHexViewer - Log de diagnostic FFT");
                    _diagLogWriter.WriteLine($"# Démarré   : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                    _diagLogWriter.WriteLine($"# SR={snap.SampleRate} Hz | N={snap.FftSize} | " +
                                             $"Hop={snap.Hop} | Overlap={snap.Overlap01 * 100:F1}% | Mode={mode}");
                    _diagLogWriter.WriteLine("#");
                    _diagLogWriter.WriteLine("# HopActuel  = AbsEnd[n] - AbsEnd[n-1]");
                    _diagLogWriter.WriteLine("# HopErreur  = HopActuel - HopAttendu  (>0 = gap = discontinuité)");
                    _diagLogWriter.WriteLine("# Drained    = frames drainés par l'UI ce tick (>1 = drain multiple, normal)");
                    _diagLogWriter.WriteLine("# Drops      = paquets audio perdus (canal plein) depuis FFT précédente");
                    _diagLogWriter.WriteLine("# LockWait   = attente max sur _floatRing depuis FFT précédente");
                    _diagLogWriter.WriteLine("# FrmSkipped = FramesSkipped cumulé SfftWorker (vrais TryReadWindow miss)");
                    _diagLogWriter.WriteLine("# Anomalie   = UI_DRAIN si drain>1 normal | HOP⚠(REEL) si vrai gap | DROP si perte");
                    _diagLogWriter.WriteLine("#");
                    _diagLogWriter.WriteLine(
                        "Timestamp;FFT#;AbsEnd;HopActuel;HopAttendu;HopErreur_ms;Drained;Drops;LockWait_ms;FrmSkipped;Anomalie;FftPerSec;FftAvgMs;DebugBarInfo");

                    // Reset des compteurs spécifiques au log
                    _diagLogFftIndex    = 0;
                    _diagLogAbsEndPrev  = -1;
                    Interlocked.Exchange(ref _diagDropForLog, 0);
                    _floatRing?.ResetLockStats();

                    _diagLogEnabled = true;

                    string path = _diagLogPath;
                    BeginInvoke((Action)(() =>
                        MessageBox.Show($"Log démarré.\nFichier : {path}\n\nF8 pour arrêter.",
                            "Diagnostic Log", MessageBoxButtons.OK, MessageBoxIcon.Information)));
                }
            }
        }

        /// <summary>
        /// Écrit une ligne CSV pour la FFT dont la frame est <paramref name="frame"/>.
        /// Appelé depuis le thread UI (UpdateAnalysisFrame) à chaque nouvelle FFT.
        /// </summary>
        private void WriteDiagLogLine(SpectrumFrame frame, int drained)
        {
            if (!_diagLogEnabled) return; // check rapide sans lock

            lock (_diagLogLock)
            {
                if (!_diagLogEnabled || _diagLogWriter == null) return;

                _diagLogFftIndex++;

                long absEnd      = frame.AbsEnd;
                int  sr          = frame.SampleRate;
                var  snap        = _sfftStats.Snapshot();
                long hopAttendu  = snap.Hop;

                // Drops depuis la FFT précédente (reset atomique)
                long drops = Interlocked.Exchange(ref _diagDropForLog, 0);

                // Hop réellement observé entre deux FFTs consécutives.
                // Pour la 1ère FFT (_diagLogAbsEndPrev < 0), on ne peut pas calculer
                // de hop : on marque INIT et on ignore l'erreur (faux positif FFT#1).
                bool isFirstFft = (_diagLogAbsEndPrev < 0);
                long hopActuel = isFirstFft
                    ? hopAttendu                 // force hopActuel = hopAttendu → HopErreur = 0
                    : absEnd - _diagLogAbsEndPrev;

                // Erreur en ms (>0 = samples manquants = gap)
                double hopErreurMs = (sr > 0 && !isFirstFft)
                    ? (hopActuel - hopAttendu) * 1000.0 / sr
                    : 0.0;

                // LockWait depuis la FFT précédente puis reset
                double lockWaitMs = _floatRing?.LockWaitMaxMs ?? 0.0;
                _floatRing?.ResetLockStats();

                // FramesSkipped dans SfftWorker (vrais TryReadWindow miss ou frames suspects)
                long framesSkipped = _sfftStats.Snapshot().FramesSkipped;

                // ── Détection d'anomalie réelle ───────────────────────────────────
                // drained=2 + HopActuel=2×hop = drain UI normal (SfftWorker OK)
                // drained=1 + HopActuel=2×hop = VRAI gap (résync SfftWorker)
                // → anomalie seulement si HopActuel > drained×HopAttendu
                string anomalie;
                if (isFirstFft)
                {
                    anomalie = "INIT";
                }
                else
                {
                    anomalie = "";
                    if (drops > 0)
                        anomalie += $"DROP={drops} ";
                    if (!isFirstFft && hopActuel > (long)drained * hopAttendu + hopAttendu / 2)
                        anomalie += "HOP⚠(REEL) ";
                    else if (drained > 1)
                        anomalie += $"UI_DRAIN={drained} ";
                    if (anomalie.Length == 0)
                        anomalie = "OK";
                }

                _diagLogWriter.WriteLine(
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff};" +
                    $"{_diagLogFftIndex};" +
                    $"{absEnd};" +
                    $"{hopActuel};" +
                    $"{hopAttendu};" +
                    $"{hopErreurMs:F2};" +
                    $"{drained};" +
                    $"{drops};" +
                    $"{lockWaitMs:F3};" +
                    $"{framesSkipped};" +
                    $"{anomalie};" +
                    $"{snap.FftPerSec:F2};" +
                    $"{snap.FftAvgMs:F3};" +
                    $"{_lastDebugBarText.Replace(';', '|')}");

                // Flush toutes les 20 FFTs pour ne pas perdre de données
                // sans sur-solliciter le disque (20 FFTs ≈ 6s à 3 FFT/s)
                if (_diagLogFftIndex % 20 == 0)
                    _diagLogWriter.Flush();

                _diagLogAbsEndPrev = absEnd;
            }
        }

        /// <summary>Arrête proprement le log si actif (appelé par StopCapture).</summary>
        private void CloseDiagLog()
        {
            lock (_diagLogLock)
            {
                if (!_diagLogEnabled) return;
                _diagLogEnabled = false;
                _diagLogWriter?.Flush();
                _diagLogWriter?.Close();
                _diagLogWriter = null;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Arrête la capture audio + pipeline d'analyse si active.
        /// Remplace l'ancien bouton STOP.
        /// </summary>
        private void StopCapture()
        {
            if (capture == null)
                return; // déjà arrêté

            try
            {
                capture.DataAvailable -= Capture_DataAvailable;
                capture.RecordingStopped -= Capture_RecordingStopped;
                capture.StopRecording();

                // ── Arrêt propre du canal de découplage ──────────────────────────
                // 1. Signaler au Writer qu'aucun nouveau paquet n'arrivera.
                //    ReadAllAsync() dans _processTask se terminera naturellement.
                // 2. Attendre max 2s que _processTask vide les paquets en attente.
                // 3. Libérer CancellationTokenSource.
                _captureChannel?.Writer.TryComplete();
                try { _processTask?.Wait(2000); } catch { /* task déjà terminée ou annulée */ }
                _captureChannel = null;
                _processTask    = null;
                _processCts?.Cancel();
                _processCts?.Dispose();
                _processCts = null;
                // ─────────────────────────────────────────────────────────────────

                analysisTimer?.Stop();

                // Stop pipeline SFFT
                if (_sfftWorker != null)
                {
                    _sfftWorker.Stop();
                    _sfftWorker = null;
                }
                while (_sfftFrames.TryDequeue(out _)) { }
                _lastFrameUi = null;
                StopFftTicker();

                lock (analysisLock)
                {
                    lastAnalysisLeft = Array.Empty<int>();
                    lastAnalysisRight = Array.Empty<int>();
                }
            }
            catch
            {
                // ignore erreurs StopRecording
            }

            // Arrête aussi histogram + spectre s'ils tournent
            // On remet les flags à false AVANT StopFromRun pour éviter toute réentrance
            _histogramRunning = false;
            _spectrumRunning  = false;

            if (histogramControl != null)
                histogramControl.StopFromRun();

            if (spectrumControl != null)
                spectrumControl.StopFromRun();

            capture.Dispose();
            capture = null;
            currentFormat = null;

            // STOP ring float
            _floatRing = null;
            if (_sfftWorker != null)
            {
                _sfftWorker.Stop();
                _sfftWorker = null;
            }

            _sfftStats.SetWorkerState("STOP");

            StopSineTestSource();

            // Fermeture propre du log de diagnostic si actif
            CloseDiagLog();

            // Restauration du mode GC normal (Interactive) après la capture
            System.Runtime.GCSettings.LatencyMode =
                System.Runtime.GCLatencyMode.Interactive;

            UpdateDeviceInfo();
            UpdateDeviceSummary();
        }

        private void rbInput_CheckedChanged(object? sender, EventArgs e)
        {
            if (rbInput.Checked)
            {
                captureOutput = false;  // mode IN
                LoadDevices();
                UpdateDeviceSummary();
            }
        }

        private void rbOutput_CheckedChanged(object? sender, EventArgs e)
        {
            if (rbOutput.Checked)
            {
                captureOutput = true;   // mode OUT
                LoadDevices();
                UpdateDeviceSummary();
            }
        }
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
        private void btnRecord_Click(object sender, EventArgs e)
        {
            if (currentFormat == null)
            {
                MessageBox.Show("Dmarre la lecture (READ) avant d'enregistrer.");
                return;
            }

            lock (_recordLock)
            {
                if (isRecordingOneSecond)
                {
                    MessageBox.Show("Un enregistrement est dj en cours.");
                    return;
                }

                // Détermine le nombre de samples à enregistrer
                // => tu veux N = sample size (FFT + histogram) = valeur unique
                int windowSamples = analysisSampleSize;

                // fallback sécurité si valeur invalide
                if (windowSamples <= 0 && currentFormat != null)
                    windowSamples = currentFormat.SampleRate;

                recordSamplesNeeded = windowSamples;

                // Initialise le buffer
                recordSamplesCollected = 0;
                recordBuffer.Clear();
                recordBuffer.Capacity = Math.Max(recordBuffer.Capacity, recordSamplesNeeded);

                isRecordingOneSecond = true;
            }
        }

        private void SaveRecordingToCsv(IReadOnlyList<RecordFrame> frames)
        {
            try
            {
                string folder = GetRecordsFolderPath();
                string file = Path.Combine(folder, $"audio_record_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

                using (var sw = new StreamWriter(file))
                {
                    sw.WriteLine("Index;Left_decimal;Right_decimal;Left_hex;Right_hex");

                    for (int i = 0; i < frames.Count; i++)
                    {
                        var f = frames[i];
                        uint hl = (uint)(f.Left & 0xFFFFFF);
                        uint hr = (uint)(f.Right & 0xFFFFFF);
                        sw.WriteLine($"{i};{f.Left};{f.Right};0x{hl:X6};0x{hr:X6}");
                    }
                }

                MessageBox.Show($"Fichier enregistr :\n{file}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur CSV : " + ex.Message);
            }
        }
        //
        //
        private void Capture_DataAvailable(object? sender, WaveInEventArgs e)
        {
            // En mode test, la carte son est ignorée.
            if (_useSineTest || _useMetricsRefTest) return;

            // ── DÉCOUPLAGE : thread de capture → canal ───────────────────────
            // Ce handler s’exécute sur le thread de capture WASAPI (période 10 ms).
            // Il ne doit faire QUE copier les bytes et retourner immédiatement.
            //
            // ExclusiveWasapiCapture réutilise _outputBytes au prochain callback :
            // on loue un buffer ArrayPool (zéro GC), puis on le rend dans
            // la Task consommatrice (bloc finally dans _processTask).
            byte[] rented = ArrayPool<byte>.Shared.Rent(e.BytesRecorded);
            Buffer.BlockCopy(e.Buffer, 0, rented, 0, e.BytesRecorded);

            if (_captureChannel?.Writer.TryWrite((rented, e.BytesRecorded)) != true)
            {
                // Canal plein (ProcessBuffer trop lent) ou fermé → paquet perdu
                Interlocked.Increment(ref _diagDropCount);
                Interlocked.Increment(ref _diagDropForLog); // compteur séparé pour le log FFT
                ArrayPool<byte>.Shared.Return(rented);
            }
            // ─────────────────────────────────────────────────────────────────
        }

        //dbut de ProcessBuffer
        private void ProcessBuffer(byte[] buffer, int bytesRecorded)
        {
            if (currentFormat == null)
                return;

            // On s'attend  du float 32 bits stro
            if (currentFormat.Encoding != WaveFormatEncoding.IeeeFloat ||
                currentFormat.BitsPerSample != 32)
            {
                return;
            }

            int channels = currentFormat.Channels;
            if (channels < 2)
                return;

            int bytesPerSample = 4; // 32 bits float
            int blockAlign = bytesPerSample * channels;

            if (bytesRecorded < blockAlign)
                return;

            // ---------- Calcul du dernier frame pour affichage dtaill ----------
            int lastFrameIndex = bytesRecorded - blockAlign;

            float leftF = BitConverter.ToSingle(buffer, lastFrameIndex);
            float rightF = BitConverter.ToSingle(buffer, lastFrameIndex + bytesPerSample);

            const int Max24 = 1 << 23;      // 8388608
            const int Max24Pos = Max24 - 1; // +0x7FFFFF
            const int Min24Neg = -Max24;    // -0x800000

            int left24 = (int)Math.Truncate(leftF * Max24);
            int right24 = (int)Math.Truncate(rightF * Max24);

            // Mmoriser le dernier code 24 bits
            lastCodeLeft = left24;
            lastCodeRight = right24;

            double instVoltsLeft = 0.0;
            double instVoltsRight = 0.0;

            if (calibValidLeft)
                instVoltsLeft = calibGainLeft * left24 + calibOffsetLeft;

            if (calibValidRight)
                instVoltsRight = calibGainRight * right24 + calibOffsetRight;

            // Filtre daffichage 4 Hz
            var now = DateTime.Now;
            if (now - lastVoltageUpdate >= voltageUpdateInterval)
            {
                lastVoltageUpdate = now;
                displayVoltsLeft = instVoltsLeft;
                displayVoltsRight = instVoltsRight;
            }

            if (left24 > Max24Pos) left24 = Max24Pos;
            if (left24 < Min24Neg) left24 = Min24Neg;
            if (right24 > Max24Pos) right24 = Max24Pos;
            if (right24 < Min24Neg) right24 = Min24Neg;

            // ---- SATURATION PAR VOIE ----
            bool sampleSatLeft = (left24 == Max24Pos || left24 == Min24Neg);
            bool sampleSatRight = (right24 == Max24Pos || right24 == Min24Neg);

            var nowSat = DateTime.Now;

            // Voie gauche : on ne date QUE le dbut de saturation
            if (sampleSatLeft && !isCurrentlySatLeft)
            {
                isCurrentlySatLeft = true;
                lastSatLeft = nowSat;  // dbut de la saturation
            }
            else if (!sampleSatLeft && isCurrentlySatLeft)
            {
                // fin de la saturation continue
                isCurrentlySatLeft = false;
            }

            // Voie droite
            if (sampleSatRight && !isCurrentlySatRight)
            {
                isCurrentlySatRight = true;
                lastSatRight = nowSat;
            }
            else if (!sampleSatRight && isCurrentlySatRight)
            {
                isCurrentlySatRight = false;
            }

            uint left24u = unchecked((uint)(left24 & 0xFFFFFF));
            uint right24u = unchecked((uint)(right24 & 0xFFFFFF));

            // Nombre de frames dans ce callback
            int bytesPerFrame = currentFormat.Channels * 4;
            int frameCount = bytesRecorded / bytesPerFrame;

            // --- NOUVEAU: push du bloc float stéréo dans _floatRing (étape 1) ---
            // Ici on lit 1 seule fois le buffer en float32 stéréo et on pousse dans le ring.
            // Aucun impact sur l'ancien pipeline (int24), il reste en place.
            if (!(_useSineTest || _useMetricsRefTest) && _floatRing != null && frameCount > 0) // pas de carte son si test actif
            {
                float[]? tmpL = null;
                float[]? tmpR = null;

                try
                {
                    tmpL = ArrayPool<float>.Shared.Rent(frameCount);
                    tmpR = ArrayPool<float>.Shared.Rent(frameCount);

                    int p = 0;
                    for (int i = 0; i < frameCount; i++)
                    {
                        // buffer = float32 interleaved: L,R,L,R...
                        tmpL[i] = BitConverter.ToSingle(buffer, p); p += 4;
                        tmpR[i] = BitConverter.ToSingle(buffer, p); p += 4;
                    }

                    //for (int i = 0; i < frameCount; i++)
                    //{
                    //    int idxFrame = i * bytesPerFrame;   // bytesPerFrame = currentFormat.Channels * 4

                    //    tmpL[i] = BitConverter.ToSingle(buffer, idxFrame);       // ch0
                    //    tmpR[i] = BitConverter.ToSingle(buffer, idxFrame + 4);   // ch1
                    //}


                    // Push uniquement la partie utile [0..frameCount)
                    _floatRing.PushBlock(tmpL.AsSpan(0, frameCount), tmpR.AsSpan(0, frameCount));
                    _sfftStats.SetAbsWrite(_floatRing.AbsWrite);

                    // ── Diagnostic débit ──────────────────────────────────────────
                    var diagNow = DateTime.UtcNow;
                    if (_diagLastTime == DateTime.MinValue) _diagLastTime = diagNow;

                    _diagSamplesReceived += frameCount;
                    double diagElapsed = (diagNow - _diagLastTime).TotalSeconds;

                    if (diagElapsed >= 2.0 && currentFormat != null)
                    {
                        _diagSamplesExpected = (long)(currentFormat.SampleRate * diagElapsed);
                        long deficit = _diagSamplesExpected - _diagSamplesReceived;
                        _diagDeficitMs = deficit * 1000.0 / currentFormat.SampleRate;

                        // Drops dans cette fenêtre de 2s : Exchange remet le compteur à zéro
                        // atomiquement, évitant toute perte de compte entre lecture et reset.
                        long dropsWindow = Interlocked.Exchange(ref _diagDropCount, 0);

                        string lockInfo = _floatRing != null
                            ? $" | LockWait max={_floatRing.LockWaitMaxMs:F2}ms avg={_floatRing.LockWaitAvgMs:F3}ms n={_floatRing.LockWaitCount}"
                            : "";

                        string dropInfo = dropsWindow > 0
                            ? $" | DROP={dropsWindow} pkt ⚠"
                            : " | DROP=0 ✓";

                        string diagMsg = $"DIAG: reçu={_diagSamplesReceived} attendu={_diagSamplesExpected} " +
                                         $"déficit={deficit} ({_diagDeficitMs:F1}ms){lockInfo}{dropInfo}";

                        System.Diagnostics.Debug.WriteLine(diagMsg);
                        _sfftStats.SetLastError(diagMsg);  // visible dans la barre de titre

                        // Reset pour la prochaine fenêtre de 2s
                        _diagSamplesReceived = 0;
                        _diagLastTime = diagNow;
                        _floatRing?.ResetLockStats();
                    }
                    // ─────────────────────────────────────────────────────────────
                }
                finally
                {
                    if (tmpL != null) ArrayPool<float>.Shared.Return(tmpL);
                    if (tmpR != null) ArrayPool<float>.Shared.Return(tmpR);
                }


                _sfftStats.SetAbsWrite(_floatRing.AbsWrite);
                                
            }

            // ── Passe unique : fenêtre d'analyse + VU peak + REC ────────────────────
            // Supprimé : _audioRing (StereoRingBuffer int24) — jamais lu hors de ProcessBuffer.
            // Optimisé : _analysisWindowLock acquis UNE fois (et non N fois per-sample).
            //            _recordLock acquis UNE fois après la boucle (flush en batch).
            // ───────────────────────────────────────────────────────────────────────────
            List<RecordFrame>? recordSnapshotToSave = null;
            int peakLeftAbs  = 0;
            int peakRightAbs = 0;

            // Collecte REC locale sans lock dans la boucle
            List<RecordFrame>? localRec = null;
            bool recWasActive;
            lock (_recordLock) { recWasActive = isRecordingOneSecond; }

            lock (_analysisWindowLock)
            {
                EnsureAnalysisWindowCapacity_NoLock(analysisSampleSize);

                for (int i = 0; i < frameCount; i++)
                {
                    int idxFrame = i * bytesPerFrame;

                    float lf = BitConverter.ToSingle(buffer, idxFrame);
                    float rf = BitConverter.ToSingle(buffer, idxFrame + 4);

                    int L = (int)Math.Truncate(lf * Max24);
                    int R = (int)Math.Truncate(rf * Max24);

                    if (L > Max24Pos) L = Max24Pos;
                    if (L < Min24Neg) L = Min24Neg;
                    if (R > Max24Pos) R = Max24Pos;
                    if (R < Min24Neg) R = Min24Neg;

                    // Fenêtre d'analyse (FeedExternalTimeStats24 + histogramme)
                    FeedAnalysisWindowSample_NoLock(L, R);

                    // VU peak (fusionné ici — supprime la 2e boucle redondante)
                    int absL = L < 0 ? -L : L;
                    int absR = R < 0 ? -R : R;
                    if (absL > peakLeftAbs)  peakLeftAbs  = absL;
                    if (absR > peakRightAbs) peakRightAbs = absR;

                    // REC : accumulation locale
                    if (recWasActive)
                    {
                        localRec ??= new List<RecordFrame>(frameCount);
                        localRec.Add(new RecordFrame { Left = L, Right = R });
                    }
                }
            }

            // Flush REC avec une seule acquisition de _recordLock
            if (localRec != null)
            {
                lock (_recordLock)
                {
                    if (isRecordingOneSecond)
                    {
                        foreach (var recFr in localRec)
                        {
                            if (recordSamplesCollected >= recordSamplesNeeded) break;
                            recordBuffer.Add(recFr);
                            recordSamplesCollected++;
                        }
                        if (recordSamplesCollected >= recordSamplesNeeded)
                        {
                            isRecordingOneSecond = false;
                            recordSnapshotToSave = new List<RecordFrame>(recordBuffer);
                        }
                    }
                }
            }

            // Sauvegarde CSV déclenchée côté UI
            if (recordSnapshotToSave != null)
                BeginInvoke((Action)(() => SaveRecordingToCsv(recordSnapshotToSave)));

            // Conversion en dBFS
            double dbLeft, dbRight;
            const double fullScale = Max24Pos; // 0x7FFFFF

            if (peakLeftAbs == 0)
                dbLeft = double.NegativeInfinity;
            else
                dbLeft = 20.0 * Math.Log10(peakLeftAbs / fullScale);

            if (peakRightAbs == 0)
                dbRight = double.NegativeInfinity;
            else
                dbRight = 20.0 * Math.Log10(peakRightAbs / fullScale);

            // On clip  -120 dBFS pour l'affichage
            double dbLeftClamped = double.IsNegativeInfinity(dbLeft) ? -120.0 : Math.Max(dbLeft, -120.0);
            double dbRightClamped = double.IsNegativeInfinity(dbRight) ? -120.0 : Math.Max(dbRight, -120.0);

            // Mapping [-120, 0] dB -> [0, 100]
            int levelLeft = (int)Math.Round((dbLeftClamped + 120.0) / 120.0 * 100.0);
            int levelRight = (int)Math.Round((dbRightClamped + 120.0) / 120.0 * 100.0);

            if (levelLeft < 0) levelLeft = 0;
            if (levelLeft > 100) levelLeft = 100;
            if (levelRight < 0) levelRight = 0;
            if (levelRight > 100) levelRight = 100;

            // ---------- Texte existant ----------
            string leftHex24 = "0x" + left24u.ToString("X6");
            string rightHex24 = "0x" + right24u.ToString("X6");

            string leftBits = FormatBitsWithStability(left24u, lastBitsLeft, lastChangeLeft);
            string rightBits = FormatBitsWithStability(right24u, lastBitsRight, lastChangeRight);

            var leftText = new StringBuilder();
            leftText.AppendLine(FormatRow("---LEFT---", ""));
            leftText.AppendLine(FormatRow("Float", leftF.ToString("G9")));
            leftText.AppendLine(FormatRow("Decimal (24b)", left24.ToString()));
            leftText.AppendLine(FormatRow("Hex (24 bits)", leftHex24));
            leftText.AppendLine(FormatRow("Binary (24b)", leftBits));
            // Mesures en volts (issues de SpectrumControl / calibration unifiée)
            double? vrmsL = null, vpkL = null, vdcL = null;
            double? vrmsR = null, vpkR = null, vdcR = null;
            bool calL = false, calR = false;

            if (spectrumControl != null)
            {
                calL = spectrumControl.VoltageCalibrationValidLeft;
                calR = spectrumControl.VoltageCalibrationValidRight;
                spectrumControl.GetLastVoltages(out vrmsL, out vpkL, out vdcL, out vrmsR, out vpkR, out vdcR);
            }

            leftText.AppendLine(FormatRow("Voltage (Vrms)", (calL && vrmsL.HasValue) ? FormatVoltageWithUnit(vrmsL.Value) : "Uncalibrated"));
            leftText.AppendLine(FormatRow("Voltage (Vpeak)", (calL && vpkL.HasValue) ? FormatVoltageWithUnit(vpkL.Value) : "Uncalibrated"));
            leftText.AppendLine(FormatRow("Voltage (Vdc)", (calL && vdcL.HasValue) ? FormatVoltageWithUnit(vdcL.Value) : "Uncalibrated"));    
            var rightText = new StringBuilder();
            rightText.AppendLine(FormatRow("---RIGHT---", ""));
            rightText.AppendLine(FormatRow("Float", rightF.ToString("G9")));
            rightText.AppendLine(FormatRow("Decimal (24b)", right24.ToString()));
            rightText.AppendLine(FormatRow("Hex (24 bits)", rightHex24));
            rightText.AppendLine(FormatRow("Binary (24b)", rightBits));
            
            rightText.AppendLine(FormatRow("Voltage (Vrms)", (calR && vrmsR.HasValue) ? FormatVoltageWithUnit(vrmsR.Value) : "Uncalibrated"));
            rightText.AppendLine(FormatRow("Voltage (Vpeak)", (calR && vpkR.HasValue) ? FormatVoltageWithUnit(vpkR.Value) : "Uncalibrated"));
            rightText.AppendLine(FormatRow("Voltage (Vdc)", (calR && vdcR.HasValue) ? FormatVoltageWithUnit(vdcR.Value) : "Uncalibrated"));
            // Sauvegarde des dernires valeurs (si utilis ailleurs)
            lastLeftFloat = leftF;
            lastRightFloat = rightF;

            lastLeftScaled = (int)Math.Round(leftF * (1 << 23));
            lastRightScaled = (int)Math.Round(rightF * (1 << 23));

            lastLeft24 = left24;
            lastRight24 = right24;

            lastLeftHex24 = leftHex24;
            lastRightHex24 = rightHex24;

            lastLeftBits = leftBits;
            lastRightBits = rightBits;

            lastLeftVolts = displayVoltsLeft;
            lastRightVolts = displayVoltsRight;

            // ---------- Limitation frquence UI texte ----------
            var nowUi = DateTime.Now;
            bool updateText;

            if (nowUi - lastTextDump >= textDumpInterval)
            {
                updateText = true;
                lastTextDump = nowUi;
            }
            else
            {
                updateText = false;
            }

            // ---------- Mise  jour UI ----------
            BeginInvoke((Action)(() =>
            {
                // On ne met  jour le texte que si l'intervalle est coul
                if (updateText)
                {
                    var newLeft = leftText.ToString();
                    if (!string.Equals(newLeft, _lastAllFormatsLeft, StringComparison.Ordinal))
                    {
                        _lastAllFormatsLeft = newLeft;
                        txtLeft.Text = newLeft;
                        ColorBinaryXs(txtLeft, "Binary (24b)");
                    }

                    var newRight = rightText.ToString();
                    if (!string.Equals(newRight, _lastAllFormatsRight, StringComparison.Ordinal))
                    {
                        _lastAllFormatsRight = newRight;
                        txtRight.Text = newRight;
                        ColorBinaryXs(txtRight, "Binary (24b)");
                    }

                }

                // Bargraphs dBFS
                UpdateLevelBar(barLeftLevel, levelLeft);
                UpdateLevelBar(barRightLevel, levelRight);

                // SAT + dBFS
                bool satLActive =
                    isCurrentlySatLeft ||
                    (lastSatLeft.HasValue && (nowUi - lastSatLeft.Value) < satDisplayTime);

                if (satLActive)
                {
                    var elapsed = nowUi - lastSatLeft.Value;
                    bool blinkOn = ((int)(elapsed.TotalMilliseconds / (satBlinkPeriod.TotalMilliseconds / 2))) % 2 == 0;

                    if (blinkOn)
                    {
                        ApplyLeftLevelDisplay("      SAT", Color.Red, levelFontBold);

                    }
                    else
                    {
                        ApplyLeftLevelDisplay("", Color.Red, levelFontBold);

                    }
                }
                else
                {
                    ApplyLeftLevelDisplay(
                        double.IsNegativeInfinity(dbLeft) ? "- dBFS" : $"{dbLeft:F1} dBFS",
                        SystemColors.HotTrack,
                        levelFontRegular
                    );

                }

                bool satRActive =
                    isCurrentlySatRight ||
                    (lastSatRight.HasValue && (nowUi - lastSatRight.Value) < satDisplayTime);

                if (satRActive)
                {
                    var elapsed = nowUi - lastSatRight.Value;
                    bool blinkOn = ((int)(elapsed.TotalMilliseconds / (satBlinkPeriod.TotalMilliseconds / 2))) % 2 == 0;

                    if (blinkOn)
                    {
                        ApplyRightLevelDisplay("SAT", Color.Red, levelFontBold);

                    }
                    else
                    {
                        ApplyRightLevelDisplay("", Color.Red, levelFontBold);

                    }
                }
                else
                {
                    ApplyRightLevelDisplay(
                        double.IsNegativeInfinity(dbRight) ? "- dBFS" : $"{dbRight:F1} dBFS",
                        SystemColors.HotTrack,
                        levelFontRegular
                    );

                }

                UpdateMainDisplayFromLastValues();
            }));
        }

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
                    if (_sfftWorker != null)
                        RestartSfftFromUi();
                };
                
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
        //
        //
        private void Capture_RecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                MessageBox.Show("Erreur pendant la capture : " + e.Exception.Message);
            }
        }
        //
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

            // Met  jour les labels
            _dataViewForm!.lblLeftMainTitle.Text = $"{mode}";
            _dataViewForm!.lblRightMainTitle.Text = $"{mode}";

            _dataViewForm!.lblLeftMainValue.Text = leftText;
            _dataViewForm!.lblRightMainValue.Text = rightText;
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
        //
        //
        private void EnsureReadRunning()
        {
            if (capture != null) return; // déjà démarré
            StartCapture();
        }

        private void EnsureReadStoppedIfNoTabRunning()
        {
            if (capture == null) return; // déjà arrêté
            if (_spectrumRunning || _histogramRunning) return;

            StopCapture();
        }


        //
        private void ExportActiveGraphToPng()
        {
            // Détermine quel onglet est affiché (Spectrum vs Histogram)
            // Dans ton projet, tu as tabAnalysis et tabSpectrum.
            bool isSpectrum = (tabAnalysis.SelectedTab == tabSpectrum);

            if (isSpectrum && spectrumControl == null)
            {
                MessageBox.Show("Le spectre n'est pas initialisé.", "Export PNG",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!isSpectrum && histogramControl == null)
            {
                MessageBox.Show("L'histogramme n'est pas initialisé.", "Export PNG",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog();
            sfd.Filter = "PNG (*.png)|*.png";
            sfd.DefaultExt = "png";

            string kind = isSpectrum ? "Spectrum" : "Histogram";
            sfd.FileName = $"AudioHexViewer_{kind}_{DateTime.Now:yyyyMMdd_HHmmss}.png";

            if (sfd.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                // Qualité recommandée :
                // scale = 2 => très bon rendu, taille de fichier généralement raisonnable
                const int scale = 2;

                if (isSpectrum)
                    spectrumControl!.ExportPng(sfd.FileName, scale);
                else
                    histogramControl!.ExportPng(sfd.FileName, scale);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Export PNG", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void menuExportPng_Click(object sender, EventArgs e)
        {
            using var dlg = new ExportPngDialog();

            dlg.SetTitleText(_lastExportPngTitle);

            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            _lastExportPngTitle = dlg.GetTitleText();

            string outPath = dlg.FilePath;
            if (string.IsNullOrWhiteSpace(outPath))
                return;

            string ext = Path.GetExtension(outPath).ToLowerInvariant();

            try
            {
                // Détermine quel onglet est affiché (Spectrum vs Histogram)
                bool isSpectrum = (tabAnalysis.SelectedTab == tabSpectrum);

                if (isSpectrum && spectrumControl == null)
                {
                    MessageBox.Show("Le spectre n'est pas initialisé.", "Export",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (!isSpectrum && histogramControl == null)
                {
                    MessageBox.Show("L'histogramme n'est pas initialisé.", "Export",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (ext == ".pdf")
                {
                    // 1) Génère un PNG temporaire depuis l'onglet ACTIF (rendu IDENTIQUE)
                    string tmpPng = Path.Combine(
                        Path.GetTempPath(),
                        $"AudioHexViewer_{Guid.NewGuid():N}.png"
                    );

                    if (isSpectrum)
                        spectrumControl!.ExportPng(tmpPng, dlg.ExpScale, dlg.GetTitleText());
                    else
                        histogramControl!.ExportPng(tmpPng, dlg.ExpScale, dlg.GetTitleText());

                    // 2) Emballe dans un PDF A4 paysage, image fit largeur
                    PdfExport.SaveA4LandscapeFromPng(tmpPng, outPath);

                    // 3) Nettoyage
                    try { File.Delete(tmpPng); } catch { /* ignore */ }
                }
                else
                {
                    // PNG par défaut
                    if (ext != ".png")
                        outPath = Path.ChangeExtension(outPath, ".png");

                    if (isSpectrum)
                        spectrumControl!.ExportPng(outPath, dlg.ExpScale, dlg.GetTitleText());
                    else
                        histogramControl!.ExportPng(outPath, dlg.ExpScale, dlg.GetTitleText());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        private void ResetFftTickerFromCurrentStats()
        {
            if (_lblFftTicker == null) return;

            // Si on n'a pas de paramètres valides, on masque (évite un "figé" trompeur)
            if (_tickerFs <= 0 || _tickerN <= 0 || _tickerHop <= 0)
            {
                _fftTickerEnabled = false;
                _lblFftTicker.Visible = false;
                _lblFftTicker.Text = "";
                return;
            }

            // Fin du priming : la 1ère FFT affichée marque le passage en steady
            if (_tickerPriming)
                _tickerPriming = false;

            // À chaque FFT affichée, on repart à 0% pour l'intervalle SUIVANT
            // Période en steady = hop/Fs
            _fftTickerPeriodSec = _tickerHop / (double)_tickerFs;

            // REW-like : si < 1 s, on cache
            if (_fftTickerPeriodSec < 1.0)
            {
                _fftTickerEnabled = false;
                _lblFftTicker.Visible = false;
                _lblFftTicker.Text = "";
                return;
            }

            _fftTickerEnabled = true;
            _fftTickerStartSec = _uiSw.Elapsed.TotalSeconds;

            _lblFftTicker.Visible = true;
            _lblFftTicker.Text = "0%";
        }
        //
        private void ArmFftTickerPriming(int fs, int fftSize, int hop, double overlap01)
        {
            if (_lblFftTicker == null) return;

            _tickerFs = fs;
            _tickerN = fftSize;
            _tickerHop = hop;
            _tickerOverlap01 = overlap01;

            _tickerPriming = true;

            // En priming, si overlap>0 : première FFT après N/Fs
            // Si overlap==0 : hop==N donc N/Fs==hop/Fs, pas besoin d'une règle spéciale.
            double periodSec = (overlap01 > 0.0) ? (fftSize / (double)fs)
                                                 : (hop / (double)fs);

            _fftTickerPeriodSec = periodSec;

            // REW-like : si trop rapide, on cache dès le départ
            if (_fftTickerPeriodSec < 1.0)
            {
                _fftTickerEnabled = false;
                _lblFftTicker.Visible = false;
                _lblFftTicker.Text = "";
                return;
            }

            _fftTickerEnabled = true;
            _fftTickerStartSec = _uiSw.Elapsed.TotalSeconds;
            _lblFftTicker.Visible = true;
            _lblFftTicker.Text = "0%";
        }

        //
        private void ArmFftTicker(int sampleRate, int hop)
        {
            if (_lblFftTicker == null)
                return;

            double periodSec = (sampleRate > 0 && hop > 0) ? (hop / (double)sampleRate) : 0.0;

            _fftTickerPeriodSec = periodSec;
            _fftTickerStartSec = _uiSw.Elapsed.TotalSeconds;

            // Règle : on n’affiche que si une FFT théorique prend >= 1 s
            _fftTickerEnabled = (periodSec >= 1.0);

            if (_fftTickerEnabled)
            {
                _lblFftTicker.Visible = true;
                _lblFftTicker.Text = "0%";
            }
            else
            {
                _lblFftTicker.Visible = false;
                _lblFftTicker.Text = "";
            }
        }

        //
        private void TickFftTickerUi()
        {
            if (_lblFftTicker == null) return;

            if (!_fftTickerEnabled)
            {
                // évite une valeur figée résiduelle
                _lblFftTicker.Text = "";
                _lblFftTicker.Visible = false;
                return;
            }

            double now = _uiSw.Elapsed.TotalSeconds;
            double dt = now - _fftTickerStartSec;

            if (_fftTickerPeriodSec <= 0.0) return;

            double p01 = dt / _fftTickerPeriodSec;
            if (p01 < 0.0) p01 = 0.0;
            if (p01 > 1.0) p01 = 1.0;

            int pct = (int)Math.Round(p01 * 100.0);

            // Affiche 0..100%. Si la FFT tarde (dt > period), on reste à 100%.
            _lblFftTicker.Text = pct.ToString() + "%";
        }
        //
        private void StopFftTicker()
        {
            _fftTickerEnabled = false;
            _fftTickerPeriodSec = 0.0;
            _fftTickerStartSec = 0.0;
            _tickerFs = 0;
            _tickerN = 0;
            _tickerHop = 0;
            _tickerOverlap01 = 0.0;
            _tickerPriming = true;

            if (_lblFftTicker != null)
            {
                _lblFftTicker.Visible = false;
                _lblFftTicker.Text = "";
            }
        }

        //
        private void SetLevelLabels(string leftText, string rightText)
        {
            // Form1 (si présents)
            if (lblLeftLevelMain != null) lblLeftLevelMain.Text = leftText;
            if (lblRightLevelMain != null) lblRightLevelMain.Text = rightText;

            // DataView (si ouverte / existante)
            if (_dataViewForm != null)
            {
                if (_dataViewForm.lblLeftLevelDV != null) _dataViewForm.lblLeftLevelDV.Text = leftText;
                if (_dataViewForm.lblRightLevelDV != null) _dataViewForm.lblRightLevelDV.Text = rightText;
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

            if (_dataViewForm?.lblLeftLevelDV != null)
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

            if (_dataViewForm?.lblRightLevelDV != null)
            {
                _dataViewForm.lblRightLevelDV.Text = text;
                _dataViewForm.lblRightLevelDV.ForeColor = color;
                _dataViewForm.lblRightLevelDV.Font = font;
            }
        }



    }

}