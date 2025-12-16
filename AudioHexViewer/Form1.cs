using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Globalization;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using System.Linq;
// ...


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
        private int _heightWithGraphs;
        private bool _hasHeightWithGraphs;
        // Hauteur minimale de la fentre
        private int _minWidth;
        private int _minHeightNoGraphs;
        // Dsactivation temporaire du redimensionnement
        private FormBorderStyle _borderStyleWithGraphs;
        private bool _borderStyleSaved;


        // --- Enregistrement 1 seconde ---
        private bool isRecordingOneSecond = false;
        private int recordSamplesNeeded = 0;
        private int recordSamplesCollected = 0;
        private List<RecordFrame> recordBuffer = new List<RecordFrame>();

        // --- Capture 1 seconde pour histogramme ---
        //private List<RecordFrame> histBuffer = new List<RecordFrame>();

        //// Historique continu (tampon roulant) alimenté par la carte son
        //private readonly object histLock = new object();

        //// Index absolu du premier échantillon présent dans histBuffer (sert à gérer les RemoveRange)
        //private long histBaseIndex = 0;

        // Ring buffer “REW-like” pour les derniers échantillons audio (capacité = menu Buffer)
        private StereoRingBuffer? _audioRing;
        private readonly object _audioRingLock = new object();

        // Index absolu de fin de la dernière fenêtre d’analyse produite (pour hop/overlap)
        private long _lastAnalysisAbsEnd = 0;

        // Pour le cas N > Buffer : accumulation dans une “fenêtre FFT” logique (taille N)
        private StereoRingBuffer? _fftWindow;  // capacité = N (peut être > buffer)
        private long _windowFeedAbsIndex = 0;  // dernier index absolu déjà injecté dans _fftWindow


        // --- Fenêtre d'analyse commune Histogram + Spectrum ---
        private int analysisSampleSize = 131072;  // défaut
        private double analysisOverlap = 0.0;     // 0..1
        private int analysisHopSamples = 131072;  // = N*(1-overlap)
        private bool _updatingSampleSizeUi = false;
        private long analysisCursorStart = 0;     // index absolu de début de la dernière fenêtre utilisée

        private System.Windows.Forms.Timer? analysisTimer;

        private int[] lastAnalysisLeft = Array.Empty<int>();
        private int[] lastAnalysisRight = Array.Empty<int>();
        private readonly object analysisLock = new object();

        //     1048576 samples / 44100 Hz = 23,78 s 
        private const int MaxHistogramSeconds = 24; // on garde jusqu' 24 s d'historique

        // --- Mode "Once" pour l'histogramme ---
        private bool histOnceActive = false;
        private int histOnceTargetSamples = 0;
        private int histOnceAccumSamples = 0;

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


        // Limitation de frquence pour la mise  jour des textes (pour viter le scintillement)
        private DateTime lastTextDump = DateTime.MinValue;
        private readonly TimeSpan textDumpInterval = TimeSpan.FromMilliseconds(0); // ~5 fps


        private Form? dataFormatsWindow;

        private bool histogramVisible = false;

        private class UiHistogramSettings
        {
            public string? DeviceId { get; set; }
            public bool CaptureOutput { get; set; }
            public string? MainDataFormat { get; set; }
        }


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
        //
        private static double ParseOverlapPercent(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0.0;
            s = s.Trim().Replace("%", "");
            if (double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double v))
                return v / 100.0;
            // cas fr (virgule)
            if (double.TryParse(s, out v))
                return v / 100.0;
            return 0.0;
        }

        private void SetAnalysisSampleSize(int nSamples)
        {
            if (nSamples < 2048) nSamples = 2048;
            if (nSamples > 1048576) nSamples = 1048576;

            analysisSampleSize = nSamples;
            analysisCursorStart = 0; // repart sur la fin du buffer

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
            analysisCursorStart = 0;

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
            {
                intervalMs = (int)Math.Max(10, Math.Round((double)analysisHopSamples * 1000.0 / currentFormat.SampleRate));
            }
            analysisTimer.Interval = intervalMs;

            // Le timer ne tourne que quand la capture est active
            if (capture != null && capture.CaptureState == CaptureState.Capturing)
                analysisTimer.Start();
            else
                analysisTimer.Stop();
        }

        private void UpdateAnalysisFrame()
        {
            if (currentFormat == null) return;

            int N = analysisSampleSize;
            if (N <= 0) return;

            int hop = analysisHopSamples > 0 ? analysisHopSamples : N;

            int[] left = new int[N];
            int[] right = new int[N];

            lock (_audioRingLock)
            {
                if (_audioRing == null) return;

                long absEnd = _audioRing.AbsWrite;
                if (absEnd < N) return; // pas encore assez de données pour une FFT

                // ---------- Cas 1 : N <= Buffer => on peut extraire directement les N derniers ----------
                if (N <= _audioRing.Capacity)
                {
                    // Ne calcule une nouvelle fenêtre que si on a au moins hop nouveaux samples depuis la dernière
                    if (_lastAnalysisAbsEnd != 0 && (absEnd - _lastAnalysisAbsEnd) < hop)
                        return;

                    // Toujours "dernier bloc" (REW-like), donc start = absEnd - N
                    if (!_audioRing.TryCopyLast(N, left, right))
                        return;

                    _lastAnalysisAbsEnd = absEnd;
                }
                // ---------- Cas 2 : N > Buffer => accumulation progressive ----------
                else
                {
                    // Crée une fenêtre logique de taille N (c'est elle qui accumule)
                    if (_fftWindow == null || _fftWindow.Capacity != N)
                    {
                        _fftWindow = new StereoRingBuffer(N);
                        _fftWindow.Clear(resetAbsIndex: true);

                        // On part du plus ancien sample disponible dans le ring pour remplir la fenêtre
                        // (sinon on risquerait de demander une plage trop ancienne)
                        long absOldest = absEnd - _audioRing.Count;
                        _windowFeedAbsIndex = absOldest;
                        _lastAnalysisAbsEnd = 0;
                    }

                    // Injecte dans _fftWindow tous les nouveaux samples arrivés depuis la dernière fois
                    // Attention : on ne peut copier que ce qui est encore présent dans _audioRing
                    long absOldestNow = absEnd - _audioRing.Count;
                    if (_windowFeedAbsIndex < absOldestNow)
                    {
                        // On a "raté" des samples (UI trop lente) => on se recale sur ce qui reste
                        _windowFeedAbsIndex = absOldestNow;
                    }

                    long toFeed = absEnd - _windowFeedAbsIndex;
                    if (toFeed <= 0) return;

                    // On alimente par chunks raisonnables (pour éviter des allocations énormes)
                    const int CHUNK = 32768;
                    int[] tmpL = new int[Math.Min(CHUNK, (int)Math.Min(toFeed, int.MaxValue))];
                    int[] tmpR = new int[tmpL.Length];

                    while (_windowFeedAbsIndex < absEnd)
                    {
                        long remaining = absEnd - _windowFeedAbsIndex;
                        int take = (int)Math.Min(tmpL.Length, remaining);

                        if (!_audioRing.TryCopyFromAbs(_windowFeedAbsIndex, take, tmpL, tmpR))
                        {
                            // Si ça échoue, c'est qu'on a demandé trop vieux => recaler
                            long oldest = absEnd - _audioRing.Count;
                            _windowFeedAbsIndex = oldest;
                            return;
                        }

                        for (int i = 0; i < take; i++)
                            _fftWindow.WriteSample(tmpL[i], tmpR[i]);

                        _windowFeedAbsIndex += take;
                    }

                    // Tant que la fenêtre logique n'a pas N samples, on ne peut pas produire de FFT
                    if (_fftWindow.Count < N) return;

                    // Hop logic : on ne “publie” que si hop nouveaux samples se sont ajoutés depuis la dernière FFT publiée
                    // Ici, absEnd représente l'instant de fin (one past last) courant.
                    if (_lastAnalysisAbsEnd != 0 && (absEnd - _lastAnalysisAbsEnd) < hop)
                        return;

                    // La fenêtre logique contient les N derniers samples accumulés => extraire ces N derniers
                    if (!_fftWindow.TryCopyLast(N, left, right))
                        return;

                    _lastAnalysisAbsEnd = absEnd;
                }
            }

            // Publie la nouvelle frame d'analyse (pour SpectrumControl + éventuel histogramme)
            lock (analysisLock)
            {
                lastAnalysisLeft = left;
                lastAnalysisRight = right;
            }

            // (Optionnel) si tu as un histogramme basé sur lastAnalysis*, il sera naturellement “temps réel” aussi.
        }



        public Form1()
        {
            InitializeComponent();

            InitAnalysisUi();

            //this.WindowState = FormWindowState.Maximized; // Plein cran au dmarrage
            ShowHistogramArea();
            ShowSpectrumArea();

            // Verrouille la largeur minimale  la largeur par dfaut
            this.MinimumSize = new Size(this.Width, this.MinimumSize.Height);

            // Largeur et hauteur minimales = taille par dfaut au dmarrage
            _minWidth = this.Width;
            // Calcule la hauteur minimale "sans graphs" (sans bouger la fentre)
            _minHeightNoGraphs = ComputeNoGraphsHeight();
            // Hauteur avec graphs
            _borderStyleWithGraphs = this.FormBorderStyle;
            _borderStyleSaved = true;

            // Verrouille largeur min + hauteur min (sans graphs)
            this.MinimumSize = new Size(_minWidth, _minHeightNoGraphs);


            chkHistoOnce.CheckAlign = ContentAlignment.MiddleRight;
            chkHistoOnce.TextAlign = ContentAlignment.MiddleLeft;

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
                "Voltage (V)"
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
        }

        private void LoadUiSettingsFromHistogramFile()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string path = Path.Combine(baseDir, "HistogramSettings.json");

                if (!File.Exists(path))
                    return;

                string json = File.ReadAllText(path);
                var settings = JsonSerializer.Deserialize<UiHistogramSettings>(json);

                if (settings == null)
                    return;

                // 1) IN / OUT
                // On met  jour le bool avant de cocher les boutons
                captureOutput = settings.CaptureOutput;

                if (settings.CaptureOutput)
                {
                    rbOutput.Checked = true;   // levent CheckedChanged appellera LoadDevices()
                }
                else
                {
                    rbInput.Checked = true;
                }

                //  ce stade, LoadDevices() a recharg comboDevices avec IN ou OUT

                // 2) Slection du device par ID
                if (!string.IsNullOrEmpty(settings.DeviceId))
                {
                    for (int i = 0; i < comboDevices.Items.Count; i++)
                    {
                        if (comboDevices.Items[i] is MMDevice dev && dev.ID == settings.DeviceId)
                        {
                            comboDevices.SelectedIndex = i;
                            break;
                        }
                    }
                }

                // 3) Mode daffichage principal (MainDataFormat)
                if (!string.IsNullOrEmpty(settings.MainDataFormat))
                {
                    int idx = comboDisplayMode.Items.IndexOf(settings.MainDataFormat);
                    if (idx >= 0)
                        comboDisplayMode.SelectedIndex = idx;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement des rglages d'interface : " + ex.Message,
                                "Histogram settings",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }



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
            dlg.Size = new Size(700, 140); // Taille de la fentre pour 2 colonnes en popu^p
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
                "AudioHexViewer V0.1 11/2025\n\n" +
                "Digital audio data viewer\n" +
                "OnE Audio Projects",
                "About",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
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
                    MessageBox.Show("Slectionne un priphrique audio.");
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

                    // --- NOUVEAU : config mode Once / Continu ---
                    bool onceMode = chkHistoOnce.Checked;

                    if (onceMode)
                    {
                                                // Nombre de samples  acqurir = fentre d'histo
                        int windowSamples = HistogramControl.LastUsedWindowSamples;

                        // Si jamais l'histo n'a pas encore t ouvert, on prend 1 seconde par dfaut
                        if (windowSamples <= 0 && currentFormat != null)
                            windowSamples = currentFormat.SampleRate;

                        histOnceTargetSamples = windowSamples;
                        histOnceAccumSamples = 0;
                        histOnceActive = true;

                        // En mode Once, on NE dmarre PAS l'histogramme tout de suite :
                        // on attend que ProcessBuffer ait accumul N chantillons.
                    }
                    else
                    {
                        // Mode continu : pas de squence Once
                        histOnceActive = false;
                        histOnceTargetSamples = 0;
                        histOnceAccumSamples = 0;
                    }
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

                    // Si l'histogramme existe dj et qu'on est en mode continu,
                    // on le dmarre tout de suite en "live"
                    //if (histogramControl != null && !chkHistoOnce.Checked)
                    //{
                    //    histogramControl.StartFromRun(false);  // mode continu
                    //}
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur lors du dmarrage de la capture : " + ex.Message);
                    capture = null;
                    currentFormat = null;
                    UpdateStartButtonAppearance();   // remet en mode READ vert
                    UpdateDeviceInfo();
                    UpdateDeviceSummary();
                }
            }
            else
            {
                // Si on est dj en train de capturer -> clique = STOP (on annule aussi un ventuel Once en cours)
                histOnceActive = false;
                histOnceTargetSamples = 0;
                histOnceAccumSamples = 0;

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
                    analysisCursorStart = 0;
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
            if (capture == null || currentFormat == null)
            {
                MessageBox.Show("Dmarre la capture (READ) avant de lancer la calibration.",
                                "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (calibTimer.Enabled || calibStage != CalStage.None)
            {
                MessageBox.Show("Une calibration est dj en cours.",
                                "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Demander quelles voies calibrer
            var rLeft = MessageBox.Show("Calibrer la voie GAUCHE ?", "Calibration",
                                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            calibDoLeft = (rLeft == DialogResult.Yes);

            var rRight = MessageBox.Show("Calibrer la voie DROITE ?", "Calibration",
                                         MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            calibDoRight = (rRight == DialogResult.Yes);

            if (!calibDoLeft && !calibDoRight)
            {
                MessageBox.Show("Aucune voie  calibrer. Calibration annule.",
                                "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Avertir si calibration existante
            if ((calibDoLeft && calibValidLeft) || (calibDoRight && calibValidRight))
            {
                var r = MessageBox.Show(
                    "Une calibration existe dj pour au moins une voie. Voulez-vous la remplacer ?",
                    "Calibration",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (r != DialogResult.Yes)
                    return;
            }

            // On commence par la voie gauche si demande, sinon la droite
            if (calibDoLeft)
            {
                StartChannelCalibration(CalChannel.Left);
            }
            else
            {
                StartChannelCalibration(CalChannel.Right);
            }
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
        private void menuShowGraphs_Click(object? sender, EventArgs e)
        {
            if (menuShowGraphs.Checked)
            {
                // Show graphs
                splitMain.Panel2Collapsed = false;

                // Ractiver le redimensionnement
                if (_borderStyleSaved)
                    this.FormBorderStyle = _borderStyleWithGraphs;
                this.MaximizeBox = true;

                if (_hasHeightWithGraphs)
                    this.Height = _heightWithGraphs;

                return;
            }

            // Hide graphs
            _heightWithGraphs = this.Height;
            _hasHeightWithGraphs = true;

            splitMain.Panel2Collapsed = true;

            // Mettre la hauteur compacte (ton code existant)
            int targetHeight = ComputeNoGraphsHeight();
            this.Height = Math.Max(targetHeight, this.MinimumSize.Height);

            // Verrouiller compltement la taille (plus de resize possible)
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
        }




        //
        private void btnRecord_Click(object sender, EventArgs e)
        {
            if (currentFormat == null)
            {
                MessageBox.Show("Dmarre la lecture (READ) avant d'enregistrer.");
                return;
            }

            if (isRecordingOneSecond)
            {
                MessageBox.Show("Un enregistrement est dj en cours.");
                return;
            }

            // On dmarre l'enregistrement
            isRecordingOneSecond = true;
            recordSamplesCollected = 0;
            recordBuffer.Clear();

            // Nombre d'chantillons correspondant  la fentre Histogram
            int windowSamples = HistogramControl.LastUsedWindowSamples;

            // Si l'histogramme n'a encore jamais t ouvert, on garde un fallback : 1 seconde
            if (windowSamples <= 0 && currentFormat != null)
                windowSamples = currentFormat.SampleRate;

            recordSamplesNeeded = windowSamples;

            //MessageBox.Show($"Enregistrement dmarr : {recordSamplesNeeded} chantillons.");

        }

        private void SaveRecordingToCsv()
        {
            try
            {
                string folder = GetRecordsFolderPath();
                string file = Path.Combine(folder, $"audio_record_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

                using (var sw = new StreamWriter(file))
                {
                    sw.WriteLine("Index;Left_decimal;Right_decimal;Left_hex;Right_hex");
                    int index = 0;

                    foreach (var f in recordBuffer)
                    {
                        uint hl = (uint)(f.Left & 0xFFFFFF);
                        uint hr = (uint)(f.Right & 0xFFFFFF);

                        sw.WriteLine($"{index};{f.Left};{f.Right};0x{hl:X6};0x{hr:X6}");
                        index++;
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

                    _audioRing.WriteSample(L, R);
                }
            }

            // 2) Enregistrement 1 seconde (annexe)
            if (isRecordingOneSecond)
            {
                // Ici tu peux remplir recordBuffer (si tu veux garder cette feature)
                // ou carrément la désactiver plus tard.
                // IMPORTANT: ne pas toucher au ring buffer ici.
            }

            // 3) Mode "Once" histogramme : on accumule juste un compteur
            if (histOnceActive)
            {
                histOnceAccumSamples += frameCount;

                if (histOnceAccumSamples >= histOnceTargetSamples)
                {
                    histOnceActive = false;

                    BeginInvoke((Action)(() =>
                    {
                        // Démarre l’affichage histo “une fois” (mais sans stopper la capture ici)
                        histogramControl?.StartFromRun(true);
                    }));
                }
            }


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
            if (calibValidLeft)
                leftText.AppendLine(FormatRow("Voltage (V)", displayVoltsLeft.ToString("F4", CultureInfo.InvariantCulture)));
            else
                leftText.AppendLine(FormatRow("Voltage (V)", "N/A"));

            var rightText = new StringBuilder();
            rightText.AppendLine(FormatRow("---RIGHT---", ""));
            rightText.AppendLine(FormatRow("Float", rightF.ToString("G9")));
            rightText.AppendLine(FormatRow("Decimal (24b)", right24.ToString()));
            rightText.AppendLine(FormatRow("Hex (24 bits)", rightHex24));
            rightText.AppendLine(FormatRow("Binary (24b)", rightBits));
            if (calibValidRight)
                rightText.AppendLine(FormatRow("Voltage (V)", displayVoltsRight.ToString("F4", CultureInfo.InvariantCulture)));
            else
                rightText.AppendLine(FormatRow("Voltage (V)", "N/A"));

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
                histogramControl.Dock = DockStyle.Fill;
                histogramControl.SetGlobalSampleSize(analysisSampleSize);
                histogramControl.UpdateHopSamples(analysisHopSamples);

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
                spectrumControl.Dock = DockStyle.Fill;
                spectrumControl.SetGlobalSampleSize(analysisSampleSize);
                spectrumControl.UpdateHopSamples(analysisHopSamples);

                // tabSpectrum est l'onglet du bas
                tabSpectrum.Controls.Clear();
                tabSpectrum.Controls.Add(spectrumControl);
            }

            // Optionnel : slectionner longlet Spectrum quand on louvre
            tabAnalysis.SelectedTab = tabSpectrum;
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

                case "Voltage (V)":
                    // si pas calibr, on signale N/A
                    if (calibValidLeft)
                        leftText = lastLeftVolts.ToString("F4", CultureInfo.InvariantCulture);
                    else
                        leftText = "N/A";

                    if (calibValidRight)
                        rightText = lastRightVolts.ToString("F4", CultureInfo.InvariantCulture);
                    else
                        rightText = "N/A";
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

                Controls.Add(lbl);
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
            double pRedEnd = 100.0;

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

        private void saveHistogramSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (histogramControl == null)
            {
                MessageBox.Show("L'histogramme n'est pas encore initialis.", "Histogram settings",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // 1) Priphrique slectionn
                string? devId = null;
                if (comboDevices.SelectedItem is MMDevice dev)
                    devId = dev.ID;   // Proprit ID de MMDevice (NAudio)

                // 2) Mode IN / OUT (entre / sortie)
                //    -> captureOutput est ton bool priv : false = entre, true = sortie
                bool isOutput = captureOutput;

                // 3) Main data format (comboDisplayMode)
                string? mainFormat = comboDisplayMode.SelectedItem as string;

                // On pousse ces infos vers l'histogramme AVANT la sauvegarde
                histogramControl.DeviceIdForSave = devId;
                histogramControl.CaptureOutputForSave = isOutput;
                histogramControl.MainDataFormatForSave = mainFormat;
                histogramControl.SaveHistogramSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la sauvegarde des rglages d'histogramme :\n" + ex.Message,
                    "Histogram settings",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void alwaysOnTopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Le ToolStripMenuItem appelle ce handler aprs avoir chang son tat Checked
            this.TopMost = alwaysOnTopToolStripMenuItem.Checked;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadUiSettingsFromHistogramFile();

        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void lblFormatInfo_Click(object sender, EventArgs e)
        {

        }
    }

}