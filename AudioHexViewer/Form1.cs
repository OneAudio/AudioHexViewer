using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.IO;// ...
using System.Text.Json;
using System.Linq;

namespace AudioHexViewer
{
    public partial class Form1 : Form

    {
        // --- CHAMPS PRIVS ---
        private WasapiCapture? capture;   // pour la capture audio
        private MMDevice? selectedDevice; // priphrique audio choisi
        private WaveFormat? currentFormat;


        // Suivi de stabilit des bits (24 bits) sur 1 seconde
        private readonly bool[] lastBitsLeft = new bool[24];
        private readonly bool[] lastBitsRight = new bool[24];
        private readonly DateTime[] lastChangeLeft = new DateTime[24];
        private readonly DateTime[] lastChangeRight = new DateTime[24];
        private readonly TimeSpan bitStabilityThreshold = TimeSpan.FromSeconds(0.4);

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
        private int _minHeightWithGraphs;
        // Dsactivation temporaire du redimensionnement
        private FormBorderStyle _borderStyleWithGraphs;
        //private bool _borderStyleSaved;

        private SessionSettings _session = SessionSettingsManager.LoadOrNull() ?? new SessionSettings();

        // --- Enregistrement 1 seconde ---
        private bool isRecordingOneSecond = false;
        private int recordSamplesNeeded = 0;
        private int recordSamplesCollected = 0;
        private List<RecordFrame> recordBuffer = new List<RecordFrame>();

        private readonly object _recordLock = new object();

        // Ring buffer “REW-like” pour les derniers échantillons audio (capacité = menu Buffer)
        private StereoRingBuffer? _audioRing;
        private readonly object _audioRingLock = new object();

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

        // Pour le cas N > Buffer : accumulation dans une “fenêtre FFT” logique (taille N)
        private StereoRingBuffer? _fftWindow;  // capacité = N (peut être > buffer)
        private long _windowFeedAbsIndex = 0;  // dernier index absolu déjà injecté dans _fftWindow


        // --- Fenêtre d'analyse commune Histogram + Spectrum ---
        private int analysisSampleSize = 131072;  // défaut
        private double analysisOverlap = 0.0;     // 0..1
        private int analysisHopSamples = 131072;  // = N*(1-overlap)
        //private bool _updatingSampleSizeUi = false;
        //private long analysisCursorStart = 0;     // index absolu de début de la dernière fenêtre utilisée

        private System.Windows.Forms.Timer? analysisTimer;

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
        private ToolStripMenuItem? menuOverlay;
        private ToolStripMenuItem? menuColors;

        private bool _spectrumRunning;
        private bool _histogramRunning;

        // Limitation de frquence pour la mise  jour des textes (pour viter le scintillement)
        private DateTime lastTextDump = DateTime.MinValue;
        private readonly TimeSpan textDumpInterval = TimeSpan.FromMilliseconds(0); // ~5 fps

        private SessionSettings? _loadedSessionSettings;

        private Form? dataFormatsWindow;

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
            SaveOverlayState();
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
            if (nSamples < 2048) nSamples = 2048;
            if (nSamples > 1048576) nSamples = 1048576;

            analysisSampleSize = nSamples;
            //analysisCursorStart = 0; // repart sur la fin du buffer

            RecomputeHopAndTimer();

            histogramControl?.SetGlobalSampleSize(analysisSampleSize);
            histogramControl?.UpdateHopSamples(analysisHopSamples);

            spectrumControl?.SetGlobalSampleSize(analysisSampleSize);
            spectrumControl?.UpdateHopSamples(analysisHopSamples);
        }
        //
        private readonly int[] _sampleSizeOptions = new[] { 2048, 4096, 8192, 16384, 32768, 65536, 131072, 262144, 524288, 1048576 };

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

            lock (_audioRingLock)
            {
                if (_audioRing == null) _audioRing = new StereoRingBuffer(analysisBufferSize);
                else _audioRing.Resize(analysisBufferSize, reset: true);

                _lastAnalysisAbsEnd = 0;

                // La fenêtre logique FFT (N) sera recréée à la demande dans UpdateAnalysisFrame()
                _fftWindow = null;
                _windowFeedAbsIndex = 0;
            }
        }
        //
        private void ResetAnalysisPipeline()
        {
            lock (_audioRingLock)
            {
                if (_audioRing == null) _audioRing = new StereoRingBuffer(analysisBufferSize);
                else _audioRing.Clear(resetAbsIndex: true);

                _lastAnalysisAbsEnd = 0;
                _fftWindow = null;
                _windowFeedAbsIndex = 0;
            }
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

            int intervalMs = 100;
            if (currentFormat != null && currentFormat.SampleRate > 0)

            // ROBUST FIX: timer UI = cadence d'affichage, pas cadence d'acquisition
            analysisTimer.Interval = 20; // 50 Hz UI (ajuste si besoin: 10..50 ms)

            // Le timer ne tourne que quand la capture est active
            if (capture != null && capture.CaptureState == CaptureState.Capturing)
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

            int N = analysisSampleSize;
            if (N <= 0) return;

            int hop = analysisHopSamples > 0 ? analysisHopSamples : N;

            int[] left = new int[N];
            int[] right = new int[N];

            long absEnd;

            // ROBUST FIX: on lit directement la fenêtre d'analyse alimentée par ProcessBuffer
            lock (_analysisWindowLock)
            {
                EnsureAnalysisWindowCapacity_NoLock(N);

                if (!TrySnapshotAnalysisWindowLast_NoLock(N, left, right, out absEnd))
                    return;

                // Hop/overlap: ne publier que si on a assez de nouveaux samples
                if (_lastAnalysisAbsEnd != 0 && (absEnd - _lastAnalysisAbsEnd) < hop)
                    return;

                _lastAnalysisAbsEnd = absEnd;
            }

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

            //InitCursorsMenu();

            InitAnalysisUi();

            InitOverlayMenu();
            InitColorsMenu();

            //this.WindowState = FormWindowState.Maximized; // Plein cran au dmarrage
            ShowHistogramArea();
            ShowSpectrumArea();

            // Verrouille la largeur minimale  la largeur par dfaut
            this.MinimumSize = new Size(this.Width, this.MinimumSize.Height);

            // Largeur et hauteur minimales = taille par dfaut au dmarrage
            _minWidth = this.Width;
            // Calcule la hauteur minimale "sans graphs" (sans bouger la fentre)
            _minHeightNoGraphs = ComputeNoGraphsHeight();
            // Hauteur mini quand les graphs sont visibles (à ajuster si besoin)
            _minHeightWithGraphs = Math.Max(this.Height, _minHeightNoGraphs + 260);

            // Hauteur avec graphs
            _borderStyleWithGraphs = this.FormBorderStyle;
            //_borderStyleSaved = true;

            // Verrouille largeur min + hauteur min (sans graphs)
            this.MinimumSize = new Size(_minWidth, _minHeightNoGraphs);


            levelFontRegular = new Font("Tahoma", 12F, FontStyle.Regular);
            levelFontBold = new Font("Tahoma", 12F, FontStyle.Bold);

            lblLeftLevel.Font = levelFontRegular;
            lblRightLevel.Font = levelFontRegular;

            // Assurer que l'tat du split correspond au menu au dmarrage
            splitMain.Panel2Collapsed = !menuShowGraphs.Checked;

            // Modes possibles pour la valeur mise en avant
            comboDisplayMode.Items.AddRange(new object[]
            {
                "Decimal (24b)",
                "Hex (24 bits)",
                "Binary (24b)",
                "Float",
                "Voltage (Vrms)",
                "Voltage (Vpeak)",
                "Voltage (Vdc)"
            });
            comboDisplayMode.SelectedIndex = 0;
            comboDisplayMode.SelectedIndexChanged += comboDisplayMode_SelectedIndexChanged;

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

            UpdateStartButtonAppearance();

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

            this.FormClosing += Form1_FormClosing;
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
        private void InitOverlayMenu()
        {
            menuOverlay = new ToolStripMenuItem("Overlay");

            // 8 slots
            for (int i = 0; i < OverlayManager.MaxOverlays; i++)
            {
                var slot = new ToolStripMenuItem($"Slot {i + 1}");
                slot.Tag = i;

                var miLoad = new ToolStripMenuItem("Load…");
                miLoad.Tag = ("load", i);
                miLoad.Click += OverlayMenu_Click;
                SaveOverlayState();

                var miSave = new ToolStripMenuItem("Save…");
                miSave.Tag = ("save", i);
                miSave.Click += OverlayMenu_Click;
                SaveOverlayState();

                var miEnabled = new ToolStripMenuItem("Enabled");
                miEnabled.CheckOnClick = true;
                miEnabled.Checked = true;
                miEnabled.Tag = ("enabled", i);
                miEnabled.CheckedChanged += OverlayMenu_CheckedChanged;

                var miClear = new ToolStripMenuItem("Clear");
                miClear.Tag = ("clear", i);
                miClear.Click += OverlayMenu_Click;
                SaveOverlayState();

                miEnabled.Name = $"ov_enabled_{i}";
                slot.Name = $"ov_slot_{i}";
                slot.Text = $"Slot {i + 1}";

                slot.DropDownItems.Add(miLoad);
                slot.DropDownItems.Add(miSave);
                slot.DropDownItems.Add(new ToolStripSeparator());
                slot.DropDownItems.Add(miEnabled);
                slot.DropDownItems.Add(miClear);

                menuOverlay.DropDownItems.Add(slot);
            }
            menuOverlay.DropDownItems.Add(new ToolStripSeparator());

            var miClearAll = new ToolStripMenuItem("Clear all…");
            miClearAll.Click += OverlayClearAll_Click;
            menuOverlay.DropDownItems.Add(miClearAll);


            // Ajouter au menu principal (top-level)
            menuStrip1.Items.Add(menuOverlay);
            UpdateOverlayMenuCaptions();

        }
        //
        private void SaveOverlayState()
        {
            if (spectrumControl == null) return;

            var st = spectrumControl.OverlayExportState();

            var paths = new string[OverlayManager.MaxOverlays];
            var ens = new string[OverlayManager.MaxOverlays];

            for (int i = 0; i < OverlayManager.MaxOverlays; i++)
            {
                paths[i] = st[i].filePath ?? "";
                ens[i] = st[i].enabled ? "1" : "0";
            }

            Properties.Settings.Default.OverlayPaths = string.Join("|", paths);
            Properties.Settings.Default.OverlayEnabled = string.Join("|", ens);
            Properties.Settings.Default.Save();
        }
        //
        //
        private void OverlayMenu_Click(object? sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem mi || mi.Tag is not ValueTuple<string, int> tag)
                return;

            var (action, idx) = tag;

            if (spectrumControl == null)
            {
                MessageBox.Show("Le panneau Spectrum n'est pas initialisé.", "Overlay");
                return;
            }

            try
            {
                if (action == "load")
                {
                    using var ofd = new OpenFileDialog();
                    ofd.Filter = "Overlay JSON (*.json)|*.json|All files (*.*)|*.*";
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                        spectrumControl.OverlayLoad(idx, ofd.FileName);
                    var st = spectrumControl.OverlayGetStatus(idx);
                    if (st.loaded && !st.compatible)
                        MessageBox.Show("Overlay chargé mais incompatible avec la FFT courante (SampleRate ou N différent). Il ne sera pas affiché.", "Overlay");
                    UpdateOverlayMenuCaptions();
                }
                else if (action == "save")
                {
                    using var sfd = new SaveFileDialog();
                    sfd.Filter = "Overlay JSON (*.json)|*.json|All files (*.*)|*.*";
                    sfd.FileName = $"overlay_slot{idx + 1}.json";
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                        spectrumControl.OverlaySave(idx, sfd.FileName, $"Overlay {idx + 1}");
                    UpdateOverlayMenuCaptions();
                }
                else if (action == "clear")
                {
                    spectrumControl.OverlayClear(idx);
                    UpdateOverlayMenuCaptions();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Overlay", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OverlayMenu_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem mi || mi.Tag is not ValueTuple<string, int> tag)
                return;

            var (action, idx) = tag;
            if (action != "enabled") return;

            if (spectrumControl == null)
                return;

            spectrumControl.OverlaySetEnabled(idx, mi.Checked);
            UpdateOverlayMenuCaptions();

        }
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
                int idx = comboDisplayMode.Items.IndexOf(ui.MainDataFormat);
                if (idx >= 0)
                    comboDisplayMode.SelectedIndex = idx;
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
                spectrumControl.ApplySpectrumSettings(_loadedSessionSettings.Spectrum);
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
            s.Ui.MainDataFormat = comboDisplayMode.SelectedItem as string;

            // Analysis (valeurs actuelles, même si on ne les applique pas encore)
            s.Analysis.BufferSize = analysisBufferSize;
            s.Analysis.Overlap = analysisOverlap;
            s.Analysis.SampleSize = analysisSampleSize;

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

        //
        private void UpdateStartButtonAppearance()
        {
            if (capture == null)
            {
                // Mode arrt --> bouton READ vert
                btnStart.Text = "READ";
                btnStart.BackColor = Color.SeaGreen;
                btnStart.ForeColor = Color.White;
            }
            else
            {
                // Mode en cours de capture --> bouton STOP rouge
                btnStart.Text = "STOP";
                btnStart.BackColor = Color.Firebrick;
                btnStart.ForeColor = Color.White;
            }
        }
        //

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


        private void btnStart_Click(object sender, EventArgs e)
        {
            // Si on n'est PAS en train de capturer -> on dmarre (mode READ -> RUN)
            if (capture == null)
            {
                if (comboDevices.SelectedItem is not MMDevice dev)
                {
                    MessageBox.Show("Selectionne un priphrique audio.");
                    return;
                }

                selectedDevice = dev;

                try
                {
                    // === ICI : on choisit IN ou OUT ===
                    if (!captureOutput)
                    {
                        // Mode IN : priphrique d'entre (micro / ADC)
                        capture = new WasapiCapture(selectedDevice);
                    }
                    else
                    {
                        // Mode OUT : priphrique de sortie (loopback)
                        capture = new WasapiLoopbackCapture(selectedDevice);
                    }
                    // ================================

                    currentFormat = capture.WaveFormat;

                    if (histogramControl != null && currentFormat != null)
                    {
                        histogramControl.UpdateSampleRate(currentFormat.SampleRate);
                        histogramControl.UpdateHopSamples(analysisHopSamples);
                    }

                    capture.DataAvailable += Capture_DataAvailable;
                    capture.RecordingStopped += Capture_RecordingStopped;

                    // Met  jour le sample rate des modules danalyse
                    if (currentFormat != null)
                    {
                        if (histogramControl != null)
                            histogramControl.UpdateSampleRate(currentFormat.SampleRate);
                        histogramControl.UpdateHopSamples(analysisHopSamples);
                        if (spectrumControl != null)
                            spectrumControl.UpdateSampleRate(currentFormat.SampleRate);
                        spectrumControl.UpdateHopSamples(analysisHopSamples);
                    }


                    // Mode continu : pas de squence Once
                    //histOnceActive = false;
                    //histOnceTargetSamples = 0;
                    //histOnceAccumSamples = 0;

                    // -------------------------------------------

                    // Dmarrage de la capture audio
                    capture.StartRecording();

                    // Rinitialisation de la pipeline d'analyse
                    ResetAnalysisPipeline();


                    // Démarre le générateur de fenêtres d'analyse (Histogram + Spectrum)
                    RecomputeHopAndTimer();
                    analysisTimer?.Start();


                    // Bouton en mode STOP private void btnRead_Click(object sender, EventArgs e)
                    UpdateStartButtonAppearance();
                    UpdateDeviceInfo();
                    UpdateDeviceSummary();
                    UpdateOverlayMenuCaptions();


                    // Si l'histogramme existe dj et qu'on est en mode continu,
                    // on le dmarre tout de suite en "live"
                    //if (histogramControl != null && !chkHistoOnce.Checked)
                    //{
                    //    histogramControl.StartFromRun(false);  // mode continu
                    //}
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur lors du demarrage de la capture : " + ex.Message);
                    capture = null;
                    currentFormat = null;
                    UpdateStartButtonAppearance();   // remet en mode READ vert
                    UpdateDeviceInfo();
                    UpdateDeviceSummary();
                }
            }
            else
            {
                //// Si on est dj en train de capturer -> clique = STOP (on annule aussi un ventuel Once en cours)
                //histOnceActive = false;
                //histOnceTargetSamples = 0;
                //histOnceAccumSamples = 0;

                try
                {
                    capture.DataAvailable -= Capture_DataAvailable;
                    capture.RecordingStopped -= Capture_RecordingStopped;
                    capture.StopRecording();
                    analysisTimer?.Stop();
                    lock (analysisLock)
                    {
                        lastAnalysisLeft = Array.Empty<int>();
                        lastAnalysisRight = Array.Empty<int>();
                    }
                    //analysisCursorStart = 0;
                }
                catch
                {
                    // on ignore les erreurs de StopRecording
                }

                // On arrte aussi l'histogramme et le spectre s'ils tournent
                if (histogramControl != null)
                {
                    histogramControl.StopFromRun();
                }

                if (spectrumControl != null)
                {
                    spectrumControl.StopFromRun();
                }

                capture.Dispose();
                capture = null;
                currentFormat = null;

                UpdateStartButtonAppearance();       // remet READ vert
                UpdateDeviceInfo();
                UpdateDeviceSummary();
                UpdateOverlayMenuCaptions();

            }
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

        private int ComputeNoGraphsHeight()
        {
            int nonClientHeight = this.Height - this.ClientSize.Height;

            // Hauteur ncessaire du contenu du panneau haut
            int panel1Needed = 0;
            foreach (Control c in splitMain.Panel1.Controls)
                panel1Needed = Math.Max(panel1Needed, c.Bottom);

            int padding = 8;

            // Hauteur client cible
            int targetClientHeight = splitMain.Top + panel1Needed + padding;

            // Hauteur totale fentre
            return targetClientHeight + nonClientHeight;
        }
        //
        private void ApplyViewState()
        {
            bool showGraphs = menuShowGraphs.Checked;
            bool graphOnly = menuShowGraphOnly.Checked;

            // "Graph only" n'a pas de sens si les graphs sont cachés
            if (!showGraphs && graphOnly)
            {
                menuShowGraphOnly.Checked = false;
                graphOnly = false;
            }

            // Affiche/masque la zone graphs (Panel2)
            splitMain.Panel2Collapsed = !showGraphs;

            // Affiche/masque le panneau haut (Panel1)
            splitMain.Panel1Collapsed = graphOnly;

            if (showGraphs)
            {
                // Mode graphs => fenêtre redimensionnable
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.MaximizeBox = true;
                this.MinimizeBox = true;
                this.MaximumSize = Size.Empty;

                if (_minHeightWithGraphs <= 0)
                    _minHeightWithGraphs = Math.Max(this.Height, _minHeightNoGraphs + 260);

                this.MinimumSize = new Size(_minWidth, _minHeightWithGraphs);

                // Si on est en "Graph only", on force l'onglet Spectrum (logique avec ta demande)
                if (graphOnly && tabAnalysis != null && tabSpectrum != null)
                    tabAnalysis.SelectedTab = tabSpectrum;
            }
            else
            {
                // Mode compact (pas de graphs) => fenêtre fixe
                this.FormBorderStyle = FormBorderStyle.FixedSingle;
                this.MaximizeBox = false;

                this.MinimumSize = new Size(_minWidth, _minHeightNoGraphs);
                this.MaximumSize = Size.Empty;

                // si graphs cachés, on s'assure que le top est visible
                splitMain.Panel1Collapsed = false;

                if (this.WindowState == FormWindowState.Maximized)
                    this.WindowState = FormWindowState.Normal;

                this.Size = new Size(_minWidth, _minHeightNoGraphs);
            }
        }

        //
        private void ApplyShowGraphsState()
        {
            bool show = menuShowGraphs.Checked;

            splitMain.Panel2Collapsed = !show;

            if (show)
            {
                // IMPORTANT: réactiver le resize + le bouton "agrandir"
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.MaximizeBox = true;
                this.MinimizeBox = true;   // optionnel mais conseillé
                this.MaximumSize = Size.Empty; // aucune limite max (évite le carré grisé)

                if (_minHeightWithGraphs <= 0)
                    _minHeightWithGraphs = Math.Max(this.Height, _minHeightNoGraphs + 260);

                this.MinimumSize = new Size(_minWidth, _minHeightWithGraphs);
            }
            else
            {
                // Mode compact (pas de resize)
                this.FormBorderStyle = FormBorderStyle.FixedSingle;
                this.MaximizeBox = false;

                this.MinimumSize = new Size(_minWidth, _minHeightNoGraphs);
                this.MaximumSize = Size.Empty;

                // Si la fenêtre est maximisée, repasser en Normal sinon on ne peut pas la réduire
                if (this.WindowState == FormWindowState.Maximized)
                    this.WindowState = FormWindowState.Normal;

                // Forcer la taille compacte en largeur + hauteur
                this.Size = new Size(_minWidth, _minHeightNoGraphs);
            }
        }


        //
        private void menuShowGraphs_Click(object? sender, EventArgs e)
        {
            // Si l'utilisateur cache les graphs, on désactive aussi "Graph only"
            if (!menuShowGraphs.Checked && menuShowGraphOnly.Checked)
                menuShowGraphOnly.Checked = false;

            ApplyViewState();
        }
        //
        private void menuShowGraphOnly_Click(object? sender, EventArgs e)
        {
            // Si on active "Graph only", on doit forcément afficher les graphs
            if (menuShowGraphOnly.Checked && !menuShowGraphs.Checked)
                menuShowGraphs.Checked = true;

            ApplyViewState();
        }


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
            ProcessBuffer(e.Buffer, e.BytesRecorded);
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

            // 1) Toujours alimenter le ring buffer REW-like
            List<RecordFrame>? recordSnapshotToSave = null;

            lock (_audioRingLock)
            {
                if (_audioRing == null)
                    _audioRing = new StereoRingBuffer(analysisBufferSize);

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

                    // 1) Ring buffer audio (toujours)
                    _audioRing.WriteSample(L, R);

                    // ROBUST FIX: on alimente la fenêtre d'analyse sur le thread audio
                    lock (_analysisWindowLock)
                    {
                        // S'assure que la fenêtre correspond à la taille FFT/histo en cours
                        EnsureAnalysisWindowCapacity_NoLock(analysisSampleSize);

                        // Push du sample dans la fenêtre circulaire
                        FeedAnalysisWindowSample_NoLock(L, R);
                    }

                    // 2) Enregistrement REC (si actif)
                    if (recordSnapshotToSave == null) // si déjà terminé dans ce callback, on n'ajoute plus
                    {
                        bool shouldAdd;
                        int needed;
                        lock (_recordLock)
                        {
                            shouldAdd = isRecordingOneSecond;
                            needed = recordSamplesNeeded;
                        }

                        if (shouldAdd)
                        {
                            lock (_recordLock)
                            {
                                if (isRecordingOneSecond && recordSamplesCollected < recordSamplesNeeded)
                                {
                                    recordBuffer.Add(new RecordFrame { Left = L, Right = R });
                                    recordSamplesCollected++;

                                    if (recordSamplesCollected >= recordSamplesNeeded)
                                    {
                                        // Stop immédiat (plus aucun ajout après)
                                        isRecordingOneSecond = false;

                                        // Snapshot immuable pour sauvegarde UI
                                        recordSnapshotToSave = new List<RecordFrame>(recordBuffer);
                                    }
                                }
                            }

                            // (on ne fait rien ici avec justFinished; on déclenchera la sauvegarde après la boucle)
                        }
                    }
                }
            }

            // Si l'enregistrement vient de se terminer, on sauvegarde côté UI (pas sur thread audio)
            if (recordSnapshotToSave != null)
            {
                BeginInvoke((Action)(() =>
                {
                    SaveRecordingToCsv(recordSnapshotToSave);
                }));
            }
            //
            // ---------- Calcul du niveau crte sur ce buffer (pour les VU) ----------
            int bytesPerFrameAll = currentFormat.Channels * 4;
            int totalFrames = bytesRecorded / bytesPerFrameAll;

            int peakLeftAbs = 0;
            int peakRightAbs = 0;

            for (int i = 0; i < totalFrames; i++)
            {
                int idx = i * bytesPerFrameAll;

                float lf = BitConverter.ToSingle(buffer, idx);
                float rf = BitConverter.ToSingle(buffer, idx + 4);

                float sL = lf * Max24;
                float sR = rf * Max24;

                int L = (int)Math.Truncate(sL);
                int R = (int)Math.Truncate(sR);

                if (L > Max24Pos) L = Max24Pos;
                if (L < Min24Neg) L = Min24Neg;
                if (R > Max24Pos) R = Max24Pos;
                if (R < Min24Neg) R = Min24Neg;

                int absL = Math.Abs(L);
                int absR = Math.Abs(R);

                if (absL > peakLeftAbs) peakLeftAbs = absL;
                if (absR > peakRightAbs) peakRightAbs = absR;
            }

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
                    txtLeft.Text = leftText.ToString();
                    txtRight.Text = rightText.ToString();

                    ColorBinaryXs(txtLeft, "Binary (24b)");
                    ColorBinaryXs(txtRight, "Binary (24b)");
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
                        lblLeftLevel.Text = "      SAT";
                        lblLeftLevel.ForeColor = Color.Red;
                        lblLeftLevel.Font = levelFontBold;
                    }
                    else
                    {
                        lblLeftLevel.Text = "";
                        lblLeftLevel.ForeColor = Color.Red;
                        lblLeftLevel.Font = levelFontBold;
                    }
                }
                else
                {
                    lblLeftLevel.Text = double.IsNegativeInfinity(dbLeft) ? "- dBFS" : $"{dbLeft:F1} dBFS";
                    lblLeftLevel.ForeColor = SystemColors.HotTrack;
                    lblLeftLevel.Font = levelFontRegular;
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
                        lblRightLevel.Text = "SAT";
                        lblRightLevel.ForeColor = Color.Red;
                        lblRightLevel.Font = levelFontBold;
                    }
                    else
                    {
                        lblRightLevel.Text = "";
                        lblRightLevel.ForeColor = Color.Red;
                        lblRightLevel.Font = levelFontBold;
                    }
                }
                else
                {
                    lblRightLevel.Text = double.IsNegativeInfinity(dbRight) ? "- dBFS" : $"{dbRight:F1} dBFS";
                    lblRightLevel.ForeColor = SystemColors.HotTrack;
                    lblRightLevel.Font = levelFontRegular;
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

                spectrumControl = new SpectrumControl(GetAnalysisFrame, sampleRate);
                // THD : logique + persistance (UI lives in SpectrumControl) (button + checkbox in the Spectrum tab)
                spectrumControl.ThdEnabledChanged += enabled => ApplyThdEnabled(enabled);
                spectrumControl.ThdSettingsRequested += () => ShowThdSettings();
                // Sync state from session
                spectrumControl.ApplyThdSettings(_session.Spectrum.Thd);
                spectrumControl.SetThdMeasuresEnabled(_session.Spectrum.EnableThdMeasures);
                spectrumControl.SetThdEnabledUi(_session.Spectrum.EnableThdMeasures);
                // if (menuC1Enabled != null) spectrumControl.CursorLeftEnabled = menuC1Enabled.Checked;
                // if (menuC2Enabled != null) spectrumControl.CursorRightEnabled = menuC2Enabled.Checked;

                // // Track (état menu)
                // if (menuC1TrackLeft != null && menuC1TrackLeft.Checked) spectrumControl.C1TrackMode = SpectrumControl.CursorTrackMode.Left;
                // else if (menuC1TrackRight != null && menuC1TrackRight.Checked) spectrumControl.C1TrackMode = SpectrumControl.CursorTrackMode.Right;
                // else

                //     spectrumControl.C1TrackMode = SpectrumControl.CursorTrackMode.Auto;

                // if (menuC2TrackLeft != null && menuC2TrackLeft.Checked) spectrumControl.C2TrackMode = SpectrumControl.CursorTrackMode.Left;
                // else if (menuC2TrackRight != null && menuC2TrackRight.Checked) spectrumControl.C2TrackMode = SpectrumControl.CursorTrackMode.Right;
                // else
                //     spectrumControl.C2TrackMode = SpectrumControl.CursorTrackMode.Auto;


                spectrumControl.RunRequested += EnsureReadRunning;
                spectrumControl.RunStateChanged += running =>
                {
                    _spectrumRunning = running;
                    if (running) EnsureReadRunning();
                    else EnsureReadStoppedIfNoTabRunning();
                };

                RestoreOverlayState();
                spectrumControl.Dock = DockStyle.Fill;
                spectrumControl.SetGlobalSampleSize(analysisSampleSize);
                spectrumControl.UpdateHopSamples(analysisHopSamples);

                if (_loadedSessionSettings != null)
                {
                    spectrumControl.ApplySpectrumSettings(_loadedSessionSettings.Spectrum);
                }

                // tabSpectrum est l'onglet du bas
                tabSpectrum.Controls.Clear();
                tabSpectrum.Controls.Add(spectrumControl);
            }

            // Optionnel : slectionner longlet Spectrum quand on louvre
            tabAnalysis.SelectedTab = tabSpectrum;
        }
        //
        private void RestoreOverlayState()
        {
            if (spectrumControl == null) return;

            string paths = Properties.Settings.Default.OverlayPaths ?? "";
            string en = Properties.Settings.Default.OverlayEnabled ?? "";

            var pathParts = paths.Split('|');
            var enParts = en.Split('|');

            var state = new (string? filePath, bool enabled)[OverlayManager.MaxOverlays];

            for (int i = 0; i < OverlayManager.MaxOverlays; i++)
            {
                string? p = (i < pathParts.Length && !string.IsNullOrWhiteSpace(pathParts[i])) ? pathParts[i] : null;

                bool enabled = true;
                if (i < enParts.Length)
                    enabled = (enParts[i] == "1" || enParts[i].Equals("true", StringComparison.OrdinalIgnoreCase));

                state[i] = (p, enabled);
            }

            spectrumControl.OverlayImportState(state);
            UpdateOverlayMenuCaptions();
        }

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
            if (comboDisplayMode.SelectedItem is not string mode)
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

                        if (!spectrumControl.HasEverRun)
                        {
                            leftText = "---";
                            rightText = "---";
                            break;
                        }

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
            lblLeftMainTitle.Text = $"{mode}";
            lblRightMainTitle.Text = $"{mode}";

            lblLeftMainValue.Text = leftText;
            lblRightMainValue.Text = rightText;
        }
        //
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
            LoadUiSettingsFromSettingsFiles();

            if (_loadedSessionSettings != null)
                ApplyAnalysisSettings(_loadedSessionSettings);

            ApplyLoadedSessionSettingsToExistingControls();
            BeginInvoke(new Action(ApplyViewState));

        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {
        }
        private void lblFormatInfo_Click(object sender, EventArgs e)
        {
        }
        //
        private void UpdateOverlayMenuCaptions()
        {
            bool srKnown = (currentFormat != null && capture != null && capture.CaptureState == CaptureState.Capturing);

            if (menuOverlay == null)
                return;

            for (int i = 0; i < OverlayManager.MaxOverlays; i++)
            {
                var slotItem = menuOverlay.DropDownItems[i] as ToolStripMenuItem;
                if (slotItem == null)
                    continue;

                if (spectrumControl == null)
                {
                    slotItem.Text = $"Slot {i + 1}";
                    continue;
                }

                var st = spectrumControl.OverlayGetStatus(i);

                // Texte slot
                if (!st.loaded)
                    slotItem.Text = $"Slot {i + 1}";
                else
                    if (!srKnown)
                {
                    // SR pas encore connu => on n’affiche pas "incompatible"
                    slotItem.Text = $"Slot {i + 1}: {st.name}";
                }
                else
                {
                    slotItem.Text = st.compatible
                        ? $"Slot {i + 1}: {st.name}"
                        : $"Slot {i + 1}: {st.name} (incompatible)";
                }


                // checkbox Enabled reflète l’état réel
                foreach (ToolStripItem child in slotItem.DropDownItems)
                {
                    if (child is ToolStripMenuItem mi && mi.Tag is ValueTuple<string, int> tag && tag.Item1 == "enabled")
                    {
                        mi.Checked = st.enabled;
                        break;
                    }
                }
            }
        }
        //
        private void OverlayClearAll_Click(object? sender, EventArgs e)
        {
            if (spectrumControl == null)
            {
                MessageBox.Show(this, "Le panneau Spectrum n'est pas initialisé.", "Overlay");
                return;
            }

            var r = MessageBox.Show(
                this,
                "Supprimer tous les overlays (slots 1 à 8) ?",
                "Overlay - Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (r != DialogResult.Yes)
                return;

            for (int i = 0; i < OverlayManager.MaxOverlays; i++)
                spectrumControl.OverlayClear(i);

            UpdateOverlayMenuCaptions();
            SaveOverlayState();
        }
        //
        //
        private void EnsureReadRunning()
        {
            if (capture != null) return;      // déjà démarré
            btnStart.PerformClick();          // démarre READ (ta logique existante)
        }

        private void EnsureReadStoppedIfNoTabRunning()
        {
            if (capture == null) return;      // déjà arrêté
            if (_spectrumRunning || _histogramRunning) return;

            btnStart.PerformClick();          // stop READ (ta logique existante)
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
        //

    }

}