using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using System;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using static AudioHexViewer.SpectrumControl;
using ChartSeries = System.Windows.Forms.DataVisualization.Charting.Series;


namespace AudioHexViewer
{
    public partial class SpectrumControl : UserControl
    {
        public delegate (int[] Left, int[] Right) SampleProvider(int sampleCount);

        private readonly SampleProvider? _provider;
        private int _sampleRate;
        // Pas de rafraîchissement en samples (hop). 0 => pas de hop (mise à jour = taille fenêtre)
        private int _hopSamples = 0;
        private System.Windows.Forms.Timer? liveTimer;

        private readonly SpectrumPostProcessor _post = new SpectrumPostProcessor();

        private readonly OverlayManager _overlays = new OverlayManager();

        private readonly SpectrumMetricsOptions _metricsOpt = new SpectrumMetricsOptions();

        private SpectrumSettings.ThdSettings _thdUi = new SpectrumSettings.ThdSettings  ();

        public enum ChannelMode
        {
            Left,
            Right,
            Both // affiche les deux voies
        }

        private readonly int[] FftSizeOptions =
        {
            2048, 4096, 8192, 16384, 32768,
            65536, 131072, 262144, 524288, 1048576
        };

        private int lastFftSize;
        private int? _forcedFftSize = null;
        private WindowType lastWindow = WindowType.Rectangular;
        private ChannelMode lastChannel = ChannelMode.Both;

        // Averaging
        private int avgTargetCount = 1;

        // Spectres courants & axe fréquence
        private double[]? currentLeftSpectrum;
        private double[]? currentRightSpectrum;
        private double[]? freqAxis;

        private bool isLive = false;

        // Axes personnalisés (null = auto)
        private double? customYMin = null;
        private double? customYMax = null;
        private double? customFMin = null;   // en Hz
        private double? customFMax = null;   // en Hz

        // Derniers buffers temporels utilisés (pour les stats)
        private double[]? lastTimeLeft = null;
        private double[]? lastTimeRight = null;

        private TextAnnotation? _overlayInfoAnno;

        // Annotations curseur
        private VerticalLineAnnotation? _cursorLineL;
        private TextAnnotation? _cursorTextL;

        private VerticalLineAnnotation? _cursorLineR;
        private TextAnnotation? _cursorTextR;

        // Labels (texte curseur) affichés en bas du chart
        private Label? _lblCursorLInfo;
        private Label? _lblCursorRInfo;
        private Label? _lblCursorDeltaInfo; // NEW



        // --- Etat curseurs (indépendants) ---
        private bool _cursorLockedL = false;
        private bool _cursorLockedR = false;

        // index bin courant (snap) pour chaque curseur
        private int _cursorIdxL = -1;
        private int _cursorIdxR = -1;

        // curseur "actif" (celui que l’on déplace quand les deux sont déverrouillés)
        private enum ActiveCursor { Left, Right }
        private ActiveCursor _activeCursor = ActiveCursor.Left;

        // tolérance "clic sur curseur" en pixels
        private const int CursorHitTolPx = 10;

        private double _cursorFreqL = double.NaN;
        private double _cursorFreqR = double.NaN;

        // marques curseur visibles
        private bool _cursorMarkLVisible;
        private bool _cursorMarkRVisible;
        private double _cursorMarkLFreq, _cursorMarkLDb;
        private double _cursorMarkRFreq, _cursorMarkRDb;


        // État axe X logarithmique (pour redessin manuel des sous-divisions)
        private bool _xAxisIsLog = false;

        // Ligne d'info en bas (stats)
        private string _TopInfoLine = "";

        // Indicateur de restauration des settings (évite d’écrire dans les settings pendant la restauration)
        private bool _isRestoringSettings = false;

        // Pour le dessin manuel des sous-divisions Y
        private double _currentYMinor = 1.0;
        private double _currentYMajor = 5.0;

        // Événement Run demandé (par le bouton Run)
        public event Action<bool>? RunStateChanged; // true=RUN, false=STOP
        public event Action? RunRequested;

        // --- Options curseurs ---
        public bool CursorLeftEnabled
        {
            get => chkCursorL.Checked;
            set => chkCursorL.Checked = value;
        }
        public bool CursorRightEnabled
        {
            get => chkCursorR.Checked;
            set => chkCursorR.Checked = value;
        }

        public enum CursorTrackMode { Auto, Left, Right }

        private CursorTrackMode _c1TrackMode = CursorTrackMode.Auto;
        private CursorTrackMode _c2TrackMode = CursorTrackMode.Auto;

        public CursorTrackMode C1TrackMode
        {
            get => _c1TrackMode;
            set { _c1TrackMode = value; chartSpectrum?.Invalidate(); }
        }

        public CursorTrackMode C2TrackMode
        {
            get => _c2TrackMode;
            set { _c2TrackMode = value; chartSpectrum?.Invalidate(); }
        }


        private bool _statsShowLeft = true;
        private bool _statsShowRight = true;

        private bool _thdMeasuresEnabled = false;

        // --- Étape 2 : panneau stats dans le chart (zone dédiée à droite) ---
        private const int ChartStatsWidthPx = 170;      // largeur du panneau stats
        private const int ChartStatsMinPlotWidthPx = 420; // largeur mini du graphe (évite d’écraser le plot)

        //
        // Formatage axe Y (dBFS), divisions secondaires
        private static string FormatDbMinor(double db, double stepMinor)
        {
            if (!(stepMinor > 0))
                return db.ToString("0");

            // Si le pas mineur est entier => entier
            if (Math.Abs(stepMinor - Math.Round(stepMinor)) < 1e-9)
                return db.ToString("0");

            // Sinon => décimales cohérentes avec le pas (max 2)
            int decimals = GetStepDecimals(stepMinor);

            return db.ToString("F" + decimals);
        }
        //
        private RectangleF GetPlotRectPixels(ChartArea area)
        {
            var cr = chartSpectrum.ClientRectangle;
            if (cr.Width <= 0 || cr.Height <= 0) return RectangleF.Empty;

            // Rectangle externe de la ChartArea (en pixels)
            var pos = area.Position;
            RectangleF outer = new RectangleF(
                cr.Width * pos.X / 100f,
                cr.Height * pos.Y / 100f,
                cr.Width * pos.Width / 100f,
                cr.Height * pos.Height / 100f
            );

            // Rectangle interne (plot) de la ChartArea (en pixels)
            var ip = area.InnerPlotPosition;

            // Si Auto/valeurs non initialisées, fallback sur outer
            if (ip.Width <= 0f || ip.Height <= 0f)
                return outer;

            RectangleF inner = new RectangleF(
                outer.X + outer.Width * ip.X / 100f,
                outer.Y + outer.Height * ip.Y / 100f,
                outer.Width * ip.Width / 100f,
                outer.Height * ip.Height / 100f
            );

            return inner;
        }
        //
        // Échelle des labels secondaires X et Y (95% de la taille normale)
        private const float MinorLabelScale = 0.9f;

        private static int CountTrailingZeros(int value)
        {
            if (value == 0) return 9; // cas spécial (ne devrait pas arriver ici)
            int count = 0;
            while (value % 10 == 0)
            {
                count++;
                value /= 10;
            }
            return count;
        }
        //
        private static bool TryChooseBestExactStep(int spanHz, out int segments, out int stepHz)
        {
            int[] candidates = { 3, 4, 5, 6, 7, 8, 9, 10 };

            int bestSeg = -1;
            int bestStep = -1;
            int bestZeros = -1;

            foreach (int s in candidates)
            {
                if (s <= 0) continue;
                if (spanHz % s != 0) continue;

                int step = spanHz / s;
                int zeros = CountTrailingZeros(step);

                // priorité 1 : max trailing zeros
                // tie-break  : step le plus grand (moins chargé)
                if (zeros > bestZeros || (zeros == bestZeros && step > bestStep))
                {
                    bestZeros = zeros;
                    bestStep = step;
                    bestSeg = s;
                }
            }

            if (bestSeg > 0)
            {
                segments = bestSeg;
                stepHz = bestStep;
                return true;
            }

            segments = 0;
            stepHz = 0;
            return false;
        }
        //
        private static (int Segments, int StepHz, int SpanAdjustedHz) ChooseBestStepWithSpanAdjustment(int spanHz)
        {
            // 1) Si span exact => on prend direct
            if (TryChooseBestExactStep(spanHz, out int seg, out int step))
                return (seg, step, spanHz);

            // 2) Sinon, on ajuste span autour de spanHz (recherche locale)
            // Recherche dans une fenêtre raisonnable +/- 2000 Hz (modifiable)
            const int searchRadius = 2000;

            int bestSeg = 0;
            int bestStep = 0;
            int bestSpanAdj = spanHz;
            int bestZeros = -1;
            int bestDelta = int.MaxValue;

            for (int delta = 1; delta <= searchRadius; delta++)
            {
                // On teste spanHz - delta puis spanHz + delta (symétrique)
                int[] spansToTry = { spanHz - delta, spanHz + delta };

                foreach (int candidateSpan in spansToTry)
                {
                    if (candidateSpan <= 0) continue;

                    if (TryChooseBestExactStep(candidateSpan, out int s, out int st))
                    {
                        int zeros = CountTrailingZeros(st);

                        // priorité 1 : max trailing zeros
                        // priorité 2 : delta minimal (span ajusté le plus proche)
                        // tie-break : step le plus grand
                        if (zeros > bestZeros ||
                            (zeros == bestZeros && delta < bestDelta) ||
                            (zeros == bestZeros && delta == bestDelta && st > bestStep))
                        {
                            bestZeros = zeros;
                            bestDelta = delta;
                            bestSeg = s;
                            bestStep = st;
                            bestSpanAdj = candidateSpan;
                        }
                    }
                }

                // petit early-exit : si on trouve 4 zéros (ex 10000), c'est déjà excellent
                if (bestZeros >= 4)
                    break;
            }

            // fallback absolu (ne devrait pas arriver) : 6 segments
            if (bestSeg == 0)
            {
                bestSeg = 6;
                bestStep = Math.Max(1, spanHz / bestSeg);
                bestSpanAdj = bestStep * bestSeg;
            }

            return (bestSeg, bestStep, bestSpanAdj);
        }

        public SpectrumControl()
        {
            InitializeComponent();
            // Étape 1 : on garde le SplitContainer mais on masque la zone Stats (Panel2)
            panelStats.Visible = false; // panel stats n'est plus utilisé visuellement
            InitChartAppearance();
            this.Load += (_, __) => ApplyChartStatsMargin();
            chartSpectrum.Resize += (_, __) => ApplyChartStatsMargin();
            InitializeUiOptions();
            ApplyThdSettings(_thdUi);
            UpdateRunButtonAppearance();
            InitOverlayAnnotation();

            // Clic droit sur la zone du spectre -> ouverture de la boîte d'axes
            chartSpectrum.MouseUp += ChartSpectrum_MouseUp;

            // Dessin manuel des sous-divisions log
            chartSpectrum.FormatNumber += ChartSpectrum_FormatNumber;

            chartSpectrum.PostPaint -= ChartSpectrum_PostPaint;
            chartSpectrum.PostPaint += ChartSpectrum_PostPaint;


            InitCursors();
            chartSpectrum.MouseMove += ChartSpectrum_MouseMove;
            chartSpectrum.MouseDoubleClick += ChartSpectrum_MouseDoubleClick;
            chartSpectrum.MouseLeave += (s, e) => { /* optionnel : cacher texte */ };

        }
        //
        //
        public void ApplyThdSettings(SpectrumSettings.ThdSettings s)
        {
            _thdUi = s ?? new SpectrumSettings.ThdSettings();

            // Bande
            _metricsOpt.MinFreqHz = _thdUi.UseHPF ? Math.Max(0.0, _thdUi.FlowHz) : 0.0;

            if (_thdUi.UseLPF)
            {
                double nyq = (_sampleRate > 0) ? (_sampleRate / 2.0) : 384000.0; // fallback
                _metricsOpt.MaxFreqHz = Math.Min(Math.Max(0.0, _thdUi.FhighHz), nyq);
            }
            else
            {
                _metricsOpt.MaxFreqHz = 0.0; // 0 => Nyquist
            }

            // Harmoniques (voir fix B : exclure le fondamental)
            int countExclFund = _thdUi.HarmonicsNumber;
            if (countExclFund < 1) countExclFund = 1;
            if (countExclFund > 19) countExclFund = 19;     // car on va ajouter +1 (fondamental) => max 20
            _metricsOpt.MaxHarmonics = countExclFund + 1;

            // Rafraîchir selon contexte (pas de recalcul violent si pas prêt)
            if (_provider != null && isLive)
                AcquireOnce();
            else
            {
                RebuildStatsRowsFromLabels();
                chartSpectrum.Invalidate();
            }
        }

        //
        public void SetThdMeasuresEnabled(bool enabled)
        {
            _thdMeasuresEnabled = enabled;

            // Force un rafraîchissement affichage (sans recalcul lourd immédiat)
            RebuildStatsRowsFromLabels();
            chartSpectrum.Invalidate();
        }

        //
        private (double yMin, double yMax) GetCurrentYRangeGlobal()
        {
            if (freqAxis == null || freqAxis.Length == 0)
                return (-200.0, 0.0);

            if (chartSpectrum.ChartAreas.Count == 0)
                return (-200.0, 0.0);

            var area = chartSpectrum.ChartAreas["SpectrumArea"];

            // Plage X affichée (custom sinon axe)
            double fMin = customFMin ?? area.AxisX.Minimum;
            double fMax = customFMax ?? area.AxisX.Maximum;

            if (fMax <= fMin)
                return (-200.0, 0.0);

            // Trouver indices correspondant à la plage X
            int i0 = FindNearestFreqIndex(freqAxis, fMin);
            int i1 = FindNearestFreqIndex(freqAxis, fMax);

            if (i0 > i1) { int tmp = i0; i0 = i1; i1 = tmp; }

            // En log tu caches le bin 0 (k=0) (cf. startIndex=1) :contentReference[oaicite:2]{index=2}
            bool useLog = (chkLogX != null && chkLogX.Checked);
            if (useLog && i0 < 1) i0 = 1;

            // Sécurité bornes
            i0 = Math.Max(0, Math.Min(i0, freqAxis.Length - 1));
            i1 = Math.Max(0, Math.Min(i1, freqAxis.Length - 1));

            const double FloorDb = -199.9; // on ignore le plancher -200

            double min = double.PositiveInfinity;
            double max = double.NegativeInfinity;

            void Scan(double[]? arr)
            {
                if (arr == null) return;
                int n = Math.Min(arr.Length, freqAxis.Length);

                int a = Math.Min(i0, n - 1);
                int b = Math.Min(i1, n - 1);

                for (int i = a; i <= b; i++)
                {
                    double v = arr[i];
                    if (double.IsNaN(v) || double.IsInfinity(v)) continue;

                    // ignorer bins au plancher (sinon min reste collé à -200)
                    if (v <= FloorDb) continue;

                    if (v < min) min = v;
                    if (v > max) max = v;
                }
            }

            // Global sur 2 voies
            Scan(currentLeftSpectrum);
            Scan(currentRightSpectrum);

            // Si tout est au plancher (ou pas de data), fallback
            if (min == double.PositiveInfinity || max == double.NegativeInfinity)
                return (-200.0, 0.0);

            // Clamp final dans [-200 ; 0]
            min = Math.Max(-200.0, min);
            max = Math.Min(0.0, max);

            return (min, max);
        }
        //
        private void ChartSpectrum_FormatNumber(object sender, FormatNumberEventArgs e)
        {
            // On ne touche qu’aux labels d’axes
            if (e.ElementType != ChartElementType.AxisLabels)
                return;

            // Heuristique fiable dans TON graphe :
            // - Axe Y = valeurs négatives (dBFS)
            // - Axe X = fréquences >= 0
            double v = e.Value;

            // --- En LOG : ne garder que les décades 1,10,100,1k,10k,100k... ---
            if (_xAxisIsLog && v > 0.0)
            {
                double p = Math.Log10(v);
                double pr = Math.Round(p);

                // tolérance : accepte uniquement v ~= 10^n
                double decade = Math.Pow(10.0, pr);
                if (Math.Abs(v - decade) > decade * 1e-6)
                {
                    e.LocalizedValue = ""; // cache les labels intermédiaires (2,3,4,... ou 19.99)
                    return;
                }

                // ici c’est une décade => on affiche proprement (1,10,100,1k,...)
                e.LocalizedValue = FormatHzK(decade);
                return;
            }

            if (v >= 1000.0)
            {
                double k = v / 1000.0;

                // 1k, 2k, 10k, 20k... (et 1.5k si un jour ça arrive)
                bool isInt = Math.Abs(k - Math.Round(k)) < 1e-9;
                e.LocalizedValue = isInt ? $"{k:0}k" : $"{k:0.#}k";
            }
            else if (v >= 0.0)
            {
                e.LocalizedValue = $"{v:0}";
            }
            else
            {
                // Axe Y (dBFS) : afficher des décimales si le pas (major) n'est pas entier
                int decimals = GetStepDecimals(_currentYMajor);
                e.LocalizedValue = v.ToString("F" + decimals);
            }

        }
        //
        private static int GetStepDecimals(double step)
        {
            if (!(step > 0))
                return 0;

            // si step est un entier (à epsilon près) -> 0 décimales
            if (Math.Abs(step - Math.Round(step)) < 1e-9)
                return 0;

            // sinon on limite à 2 décimales (puisque tu arrondis minor à 2 décimales)
            // Ex: 12.5 -> 1 décimale, 12.25 -> 2 décimales
            double s10 = step * 10.0;
            if (Math.Abs(s10 - Math.Round(s10)) < 1e-9)
                return 1;

            return 2;
        }

        //
        public SpectrumControl(SampleProvider provider, int sampleRate)
            : this()
        {
            _provider = provider;
            UpdateSampleRate(sampleRate);
            AcquireOnce();
        }

        public void UpdateSampleRate(int sampleRate)
        {
            if (sampleRate > 0)
                _sampleRate = sampleRate;

            UpdateWindowInfo();
        }

        // Appelé par Form1 quand le paramètre Overlap change.
        // hopSamples = nombre d'échantillons entre deux calculs (0 => pas de hop, on suit N).
        public void UpdateHopSamples(int hopSamples)
        {
            if (hopSamples < 0) hopSamples = 0;
            _hopSamples = hopSamples;

            if (liveTimer != null && _sampleRate > 0 && lastFftSize > 0)
            {
                int step = _hopSamples > 0 ? _hopSamples : lastFftSize;
                int intervalMs = (int)Math.Max(10, Math.Round((double)step * 1000.0 / _sampleRate));
                liveTimer.Interval = intervalMs;
            }
            UpdateOverlayInfo();

        }

        // Appelé par Form1 : force la taille FFT pour que Histogram + Spectrum utilisent la même fenêtre.
        public void SetGlobalSampleSize(int samples)
        {
            if (samples <= 0) return;
            _forcedFftSize = samples;
            lastFftSize = samples;
            UpdateWindowInfo();
            UpdateOverlayInfo();

        }
        //
        private void UpdateAvgProgressUi(int k, int n)
        {
            if (lblAvgProgress == null) return;

            if (n <= 1) lblAvgProgress.Text = "";
            else lblAvgProgress.Text = $"{k}/{n}";
        }
        //
        private void UpdateWindowInfo()
        {
            if (_sampleRate <= 0 || lastFftSize <= 0)
            {
                lblWindowInfo.Text = "";
                return;
            }

            double sec = (double)lastFftSize / _sampleRate;
            lblWindowInfo.Text = $"≈ {sec:F2} s";
        }

        private void btnResetAvg_Click(object? sender, EventArgs e)
        {
            ResetAveragingState();

            // Optionnel : forcer un nouveau calcul immédiat
            AcquireOnce();
        }


        private void InitChartAppearance()
        {
            var areas = chartSpectrum.ChartAreas;
            if (areas.Count == 0)
            {
                var area = new ChartArea("SpectrumArea");
                chartSpectrum.ChartAreas.Add(area);
            }

            chartSpectrum.Series.Clear();

            var area0 = chartSpectrum.ChartAreas[0];
            area0.Name = "SpectrumArea";
            area0.AxisX.Title = "Frequency (Hz)";
            area0.AxisY.Title = "Level (dBFS)";
            area0.AxisY.Minimum = -200;
            area0.AxisY.Maximum = 0;
        }
        //
        private void InitOverlayAnnotation()
        {
            // éviter de créer plusieurs fois
            if (_overlayInfoAnno != null)
                return;

            // Vérifier qu'on a bien un ChartArea
            if (chartSpectrum.ChartAreas.Count == 0)
                return;

            // Si une annotation du même nom existe déjà (designer / reload), on la réutilise
            const string annoName = "OverlayInfo";
            foreach (var a in chartSpectrum.Annotations)
            {
                if (a is TextAnnotation ta && ta.Name == annoName)
                {
                    _overlayInfoAnno = ta;
                    return;
                }
            }

            var anno = new TextAnnotation
            {
                Name = annoName,
                Text = "",
                Visible = false,

                // Très important : coordonnées relatives (0..100)
                IsSizeAlwaysRelative = true,

                // On clippe à la zone SpectrumArea pour éviter qu'il déborde
                ClipToChartArea = "SpectrumArea",

                // Style
                BackColor = Color.FromArgb(160, 230, 230, 230),
                ForeColor = Color.FromArgb(230, 30, 30, 30),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),

                // Alignement du texte dans le bloc
                Alignment = ContentAlignment.TopRight
            };

            chartSpectrum.Annotations.Add(anno);
            _overlayInfoAnno = anno;
        }
        //
        private void UpdateOverlayInfo()
        {
            if (_overlayInfoAnno == null)
                return;

            if (_sampleRate <= 0 || lastFftSize <= 0)
            {
                _TopInfoLine = "";
                _overlayInfoAnno.Visible = false;
                chartSpectrum.Invalidate();
                return;
            }

            // ---- Texte ----
            string srText = _sampleRate >= 1000
                ? $"{_sampleRate / 1000.0:F0} kHz"
                : $"{_sampleRate} Hz";

            int N = lastFftSize;

            int hop = (_hopSamples > 0) ? _hopSamples : N;
            hop = Math.Max(1, Math.Min(hop, N));
            double overlapPct = 100.0 * (1.0 - (double)hop / N);
            if (overlapPct < 0) overlapPct = 0;

            // --- FFT Window ---
            string winText = lastWindow switch
            {
                WindowType.Rectangular => "Rect",
                WindowType.Hann => "Hann",
                WindowType.Hamming => "Hamming",
                WindowType.BlackmanHarris4 => "Blackman-Harris4",
                WindowType.BlackmanHarris7 => "Blackman-Harris7",
                _ => "?"
            };

            // --- Averaging ---
            string avgText = avgTargetCount <= 1
                ? "Avg: No"
                : $"Avg:{avgTargetCount}";

            // --- AC / DC ---
            string acdcText = (chkRemoveDC != null && chkRemoveDC.Checked)
                ? "AC mode"
                : "DC mode";

            // --- Ligne finale ---
            // --- Résolution FFT : ΔF = SR / N ---
            double df = (double)_sampleRate / N;

            // Format lisible
            string dfText = (df >= 1000.0)
                ? $"{df / 1000.0:F2} kHz"
                : (df >= 10.0 ? $"{df:F1} Hz" : $"{df:F2} Hz");

            _TopInfoLine =
                $"SR:{srText}    " +
                $"N={N:N0}    " +
                $"ΔF={dfText}    " +
                $"Overlap:{overlapPct:F1}%     " +
                $"Win:{winText}    " +
                $"{avgText}    " +
                $"{acdcText}";




            _overlayInfoAnno.Visible = false;   // on ne l’utilise plus
            chartSpectrum.Invalidate();         // force un repaint pour voir la ligne mise à jour

        }

        //
        private void InitializeUiOptions()
        {
            //
            chkCursorL.CheckedChanged += (s, e) => UpdateCursorVisibility();
            chkCursorR.CheckedChanged += (s, e) => UpdateCursorVisibility();

            // Window
            comboWindow.Items.Clear();
            comboWindow.Items.Add("Rectangular");
            comboWindow.Items.Add("Hann");
            comboWindow.Items.Add("Hamming");
            comboWindow.Items.Add("Blackman-Harris 4");
            comboWindow.Items.Add("Blackman-Harris  7");
            comboWindow.SelectedIndex = 0; // Fênêtre par défaut : 0= Rectangulaire
            comboWindow.SelectedIndexChanged += (s, e) =>
            {
                lastWindow = (WindowType)comboWindow.SelectedIndex;
            };

            // Averaging
            comboAveraging.Items.Clear();
            comboAveraging.Items.Add("None");
            int[] av = { 2, 4, 8, 16, 32, 64, 128 };
            foreach (int a in av)
                comboAveraging.Items.Add(a.ToString());
            comboAveraging.SelectedIndex = 0;

            comboAveraging.SelectedIndexChanged += (s, e) =>
            {
                if (comboAveraging.SelectedIndex == 0)
                    avgTargetCount = 1;
                else
                    avgTargetCount = av[comboAveraging.SelectedIndex - 1];

                ResetAveragingState();
            };


            // Sélection AC/DC
            chkRemoveDC.CheckedChanged += (s, e) =>
            {
                if (_isRestoringSettings)
                    return;
            };


            // Peak-hold
            if (chkPeakHold != null)
            {
                chkPeakHold.Checked = false;
                chkPeakHold.CheckedChanged += (s, e) =>
                {
                    // Quand on active : repartir de zéro pour que le peak-hold soit cohérent
                    if (chkPeakHold.Checked)
                        _post.ResetPeakHoldState();

                    UpdateSpectrumChartAndStats();
                };
            }

            // Channel (affichage)
            comboChannel.Items.Clear();
            comboChannel.Items.Add("Left");
            comboChannel.Items.Add("Right");
            comboChannel.Items.Add("L+R");
            comboChannel.SelectedIndex = 2; // Par défaut les 2 voies sont affichées

            comboChannel.SelectedIndexChanged += (s, e) =>
            {
                switch (comboChannel.SelectedIndex)
                {
                    case 0: lastChannel = ChannelMode.Left; break;
                    case 1: lastChannel = ChannelMode.Right; break;
                    case 2: lastChannel = ChannelMode.Both; break;
                }

                if (_isRestoringSettings)
                    return;

                // IMPORTANT :
                // En mode LIVE on ne force PAS un redraw immédiat, car la voie nouvellement choisie
                // n'a pas encore son spectre calculé (il arrive au prochain AcquireOnce()).
                // Forcer un redraw met le Chart en état transitoire (séries vides + log) => exception.
                if (isLive)
                    return;

                // Hors live (pause), on peut redessiner ce qu'on a en mémoire
                UpdateSpectrumChartAndStats();
            };


            
            // Bouton RUN
            btnRun.Click += (s, e) =>
            {
                if (!isLive)
                {
                    RunRequested?.Invoke();   // demande à Form1 de lancer READ si besoin
                    StartFromRun(false);
                    RunStateChanged?.Invoke(true);
                }
                else
                {
                    StopFromRun();
                    RunStateChanged?.Invoke(false);
                }
            };

            // Log X axis 
            if (chkLogX != null)
            {
                chkLogX.CheckedChanged += (s, e) =>
                {
                    if (_isRestoringSettings)
                        return;

                    UpdateSpectrumChartAndStats();
                };
            }

        }
        //
        private void UpdateCursorVisibility()
        {
            bool onL = chkCursorL != null && chkCursorL.Checked;
            bool onR = chkCursorR != null && chkCursorR.Checked;

            if (_cursorLineL != null) _cursorLineL.Visible = onL;

            if (_cursorLineR != null) _cursorLineR.Visible = onR;

            if (_lblCursorLInfo != null) _lblCursorLInfo.Visible = onL;
            if (_lblCursorRInfo != null) _lblCursorRInfo.Visible = onR;

            if (!onL) _cursorMarkLVisible = false;
            if (!onR) _cursorMarkRVisible = false;

            chartSpectrum.Invalidate();


            chartSpectrum.Invalidate();
        }

        //
        private void InitCursorInfoLabels()
        {
            if (_lblCursorLInfo != null || _lblCursorRInfo != null)
                return;

            // On les place directement SUR le control Chart (plus simple, pas de Designer)
            _lblCursorLInfo = new Label
            {
                AutoSize = true,
                Text = "",
                BackColor = Color.Transparent,          // plus de fond gris
                ForeColor = Color.DodgerBlue,           // couleur courbe L
                Font = new Font("Segoe UI", 8.5f),
                Visible = false
            };

            _lblCursorRInfo = new Label
            {
                AutoSize = true,
                Text = "",
                BackColor = Color.Transparent,          // plus de fond gris
                ForeColor = Color.OrangeRed,            // couleur courbe R
                Font = new Font("Segoe UI", 8.5f),
                Visible = false
            };

            _lblCursorDeltaInfo = new Label
            {
                AutoSize = true,
                Text = "",
                BackColor = Color.Transparent,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8.5f),
                Visible = false
            };


            chartSpectrum.Controls.Add(_lblCursorLInfo);
            chartSpectrum.Controls.Add(_lblCursorRInfo);
            chartSpectrum.Controls.Add(_lblCursorDeltaInfo);
            _lblCursorLInfo.BringToFront();
            _lblCursorRInfo.BringToFront();
            _lblCursorDeltaInfo.BringToFront();

            chartSpectrum.Resize += (_, __) => PositionCursorInfoLabels();
        }

        private void PositionCursorInfoLabels()
        {
            if (_lblCursorLInfo == null || _lblCursorRInfo == null || _lblCursorDeltaInfo == null)
                return;

            const int yOffsetFromBottom = 56; // (avant 40) -> pour caser 3 lignes
            const int xOffsetFromRight = 450;

            int y = Math.Max(0, chartSpectrum.ClientSize.Height - yOffsetFromBottom);
            int xStart = Math.Max(6, chartSpectrum.ClientSize.Width - xOffsetFromRight);

            _lblCursorLInfo.Location = new Point(xStart, y);
            _lblCursorRInfo.Location = new Point(xStart, y + 16);
            _lblCursorDeltaInfo.Location = new Point(xStart, y + 32);
        }



        //
        // ---------------- CURSORS ----------------
        private void InitCursors()
        {
            if (chartSpectrum.ChartAreas.Count == 0)
                return;

            // Eviter double init
            if (_cursorLineL != null || _cursorLineR != null )
                return;

            var area = chartSpectrum.ChartAreas["SpectrumArea"];

            // --- Ligne curseur L ---
            _cursorLineL = new VerticalLineAnnotation
            {
                ClipToChartArea = "SpectrumArea",
                AxisX = area.AxisX,
                AxisY = area.AxisY,
                IsInfinitive = true,
                LineDashStyle = ChartDashStyle.Dash,
                LineWidth = 1,
                LineColor = Color.FromArgb(180, Color.DodgerBlue),
                Visible = false
            };

            // --- Ligne curseur R ---
            _cursorLineR = new VerticalLineAnnotation
            {
                ClipToChartArea = "SpectrumArea",
                AxisX = area.AxisX,
                AxisY = area.AxisY,
                IsInfinitive = true,
                LineDashStyle = ChartDashStyle.Dash,
                LineWidth = 1,
                LineColor = Color.FromArgb(180, Color.OrangeRed),
                Visible = false
            };

            // --- Triangle curseur L ---
            // --- Triangle curseur L ---



            chartSpectrum.Annotations.Add(_cursorLineL);
            chartSpectrum.Annotations.Add(_cursorLineR);

            // Labels en bas
            InitCursorInfoLabels();

            UpdateCursorVisibility();
        }
        //
        //
        private void ChartSpectrum_MouseMove(object? sender, MouseEventArgs e)
        {
            if (freqAxis == null || freqAxis.Length == 0)
                return;

            bool onL = (chkCursorL != null && chkCursorL.Checked);
            bool onR = (chkCursorR != null && chkCursorR.Checked);
            if (!onL && !onR)
                return;

            bool useLog = (chkLogX != null && chkLogX.Checked);

            // fréquence sous la souris (conversion log-friendly)
            double f = GetFrequencyFromMouseX(e.X);

            if (useLog && f < 1.0) f = 1.0;
            if (!useLog && f < 0.0) f = 0.0;

            int minIdx = useLog ? GetMinIndexForLog(freqAxis) : 0;

            int idx = FindNearestFreqIndex(freqAxis, f, minIdx);
            if (idx < 0 || idx >= freqAxis.Length)
                return;

            bool canMoveL = onL && !_cursorLockedL;
            bool canMoveR = onR && !_cursorLockedR;

            // Si les deux sont lockés : on rafraîchit juste l'affichage (valeurs suivent le spectre)
            if (!canMoveL && !canMoveR)
            {
                RefreshCursorsFromCurrentSpectrum();
                return;
            }

            if (canMoveL && !canMoveR)
            {
                _activeCursor = ActiveCursor.Left;
                _cursorIdxL = idx;
                _cursorFreqL = freqAxis[idx];
            }
            else if (!canMoveL && canMoveR)
            {
                _activeCursor = ActiveCursor.Right;
                _cursorIdxR = idx;
                _cursorFreqR = freqAxis[idx];
            }
            else
            {
                // Les deux peuvent bouger : déplacer celui le plus proche du pointeur
                var area = chartSpectrum.ChartAreas["SpectrumArea"];

                // Si indices non init, init
                if (_cursorIdxL < minIdx) { _cursorIdxL = idx; _cursorFreqL = freqAxis[idx]; }
                if (_cursorIdxR < minIdx) { _cursorIdxR = idx; _cursorFreqR = freqAxis[idx]; }

                double fL = freqAxis[_cursorIdxL];
                double fR = freqAxis[_cursorIdxR];

                double xL = area.AxisX.ValueToPixelPosition(fL);
                double xR = area.AxisX.ValueToPixelPosition(fR);

                double dL = Math.Abs(e.X - xL);
                double dR = Math.Abs(e.X - xR);

                if (dL <= dR)
                {
                    _activeCursor = ActiveCursor.Left;
                    _cursorIdxL = idx;
                    _cursorFreqL = freqAxis[idx];
                }
                else
                {
                    _activeCursor = ActiveCursor.Right;
                    _cursorIdxR = idx;
                    _cursorFreqR = freqAxis[idx];
                }
            }

            RefreshCursorsFromCurrentSpectrum();
        }
        //
        public AudioHexViewer.SpectrumSettings GetSpectrumSettingsSnapshot()
        {
            var s = new AudioHexViewer.SpectrumSettings();

            // Channel
            if (comboChannel != null && comboChannel.SelectedItem is string ch)
                s.Channel = ch;
            else
                s.Channel = "L+R";

            // DC
            if (chkRemoveDC != null)
            {
                s.RemoveDC = chkRemoveDC.Checked;
            }

            // Window
            if (comboWindow != null && comboWindow.SelectedItem is string w)
                s.Window = w;
            else
                s.Window = "Rectangular";

            // Log freq
            if (chkLogX != null)
                s.LogFreq = chkLogX.Checked;

            // Averaging
            // comboAveraging contient "None" puis des nombres en string ("2","4","8"...)
            if (comboAveraging != null && comboAveraging.SelectedItem is string avgText)
            {
                if (string.Equals(avgText, "None", StringComparison.OrdinalIgnoreCase))
                    s.Averaging = 0;
                else if (int.TryParse(avgText, out int n))
                    s.Averaging = n;
                else
                    s.Averaging = 0;
            }
            else
            {
                s.Averaging = 0;
            }

            return s;
        }

        public void ApplySpectrumSettings(AudioHexViewer.SpectrumSettings s)
        {
            if (s == null)
                return;

            _isRestoringSettings = true;

            try
            {
                // Channel
                if (comboChannel != null)
                {
                    int idx = comboChannel.Items.IndexOf(s.Channel);
                    if (idx >= 0)
                        comboChannel.SelectedIndex = idx;
                }

                // DC
                if (chkRemoveDC != null)
                {
                    chkRemoveDC.Checked = s.RemoveDC;
                }

                // Window
                if (comboWindow != null)
                {
                    int wIdx = comboWindow.Items.IndexOf(s.Window);
                    if (wIdx >= 0)
                        comboWindow.SelectedIndex = wIdx;
                }

                // Log freq
                if (chkLogX != null)
                {
                    chkLogX.Checked = s.LogFreq;
                }

                // Averaging
                if (comboAveraging != null)
                {
                    if (s.Averaging <= 0)
                    {
                        int noneIdx = comboAveraging.Items.IndexOf("None");
                        comboAveraging.SelectedIndex = noneIdx >= 0 ? noneIdx : 0;
                    }
                    else
                    {
                        string target = s.Averaging.ToString();
                        int aIdx = comboAveraging.Items.IndexOf(target);
                        if (aIdx >= 0)
                            comboAveraging.SelectedIndex = aIdx;
                    }
                }
            }
            finally
            {
                _isRestoringSettings = false;
            }

            // Un seul refresh final (évite 10 redraws)
            UpdateSpectrumChartAndStats();
        }

        //
        private void ChartSpectrum_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            if (freqAxis == null || freqAxis.Length == 0)
                return;

            var area = chartSpectrum.ChartAreas["SpectrumArea"];

            bool onL = (chkCursorL != null && chkCursorL.Checked);
            bool onR = (chkCursorR != null && chkCursorR.Checked);

            if (!onL && !onR)
                return;

            // distance en px entre clic et curseur
            double DistPxToCursor(int idxCursor)
            {
                if (idxCursor < 0 || idxCursor >= freqAxis.Length) return double.PositiveInfinity;
                double fC = freqAxis[idxCursor];
                double xC = area.AxisX.ValueToPixelPosition(fC);
                return Math.Abs(e.X - xC);
            }

            bool toggleLeft = false;
            bool toggleRight = false;

            double dL = onL ? DistPxToCursor(_cursorIdxL) : double.PositiveInfinity;
            double dR = onR ? DistPxToCursor(_cursorIdxR) : double.PositiveInfinity;

            if (dL <= CursorHitTolPx && dL <= dR) toggleLeft = true;
            else if (dR <= CursorHitTolPx && dR < dL) toggleRight = true;
            else
            {
                if (_activeCursor == ActiveCursor.Left && onL) toggleLeft = true;
                else if (_activeCursor == ActiveCursor.Right && onR) toggleRight = true;
                else if (onL) toggleLeft = true;
                else if (onR) toggleRight = true;
            }

            // Lorsqu'on toggle, si l'index n'est pas initialisé, on le fixe depuis la position souris
            bool useLog = (chkLogX != null && chkLogX.Checked);
            int minIdx = useLog ? GetMinIndexForLog(freqAxis) : 0;

            if (toggleLeft)
            {
                if (_cursorIdxL < minIdx)
                {
                    double f = GetFrequencyFromMouseX(e.X);
                    if (useLog && f < 1.0) f = 1.0;
                    if (!useLog && f < 0.0) f = 0.0;
                    _cursorIdxL = FindNearestFreqIndex(freqAxis, f, minIdx);
                }

                if (_cursorIdxL >= 0 && _cursorIdxL < freqAxis.Length)
                    _cursorFreqL = freqAxis[_cursorIdxL];

                _cursorLockedL = !_cursorLockedL;
                _activeCursor = ActiveCursor.Left;
            }

            if (toggleRight)
            {
                if (_cursorIdxR < minIdx)
                {
                    double f = GetFrequencyFromMouseX(e.X);
                    if (useLog && f < 1.0) f = 1.0;
                    if (!useLog && f < 0.0) f = 0.0;
                    _cursorIdxR = FindNearestFreqIndex(freqAxis, f, minIdx);
                }

                if (_cursorIdxR >= 0 && _cursorIdxR < freqAxis.Length)
                    _cursorFreqR = freqAxis[_cursorIdxR];

                _cursorLockedR = !_cursorLockedR;
                _activeCursor = ActiveCursor.Right;
            }

            RefreshCursorsFromCurrentSpectrum();
        }


        //
        private static int FindNearestFreqIndex(double[] axis, double f)
        {
            return FindNearestFreqIndex(axis, f, 0);
        }

        private static int FindNearestFreqIndex(double[] axis, double f, int minIndex)
        {
            if (axis == null || axis.Length == 0)
                return -1;

            minIndex = Math.Max(0, Math.Min(minIndex, axis.Length - 1));

            int lo = minIndex;
            int hi = axis.Length - 1;

            if (f <= axis[lo]) return lo;
            if (f >= axis[hi]) return hi;

            while (hi - lo > 1)
            {
                int mid = (lo + hi) / 2;
                if (axis[mid] < f) lo = mid;
                else hi = mid;
            }

            return (Math.Abs(axis[lo] - f) <= Math.Abs(axis[hi] - f)) ? lo : hi;
        }
        //
        private static int GetMinIndexForLog(double[] axis)
        {
            // En mode LogX, l’axe n’accepte pas 0 Hz => on prend le premier bin >= 1 Hz
            if (axis == null || axis.Length == 0)
                return 0;

            for (int i = 0; i < axis.Length; i++)
            {
                if (axis[i] >= 1.0)
                    return i;
            }
            return 0;
        }


        private void RefreshCursorAfterRedraw()
        {
            if (chartSpectrum.ChartAreas.Count == 0)
                return;

            var area = chartSpectrum.ChartAreas["SpectrumArea"];

            if (_cursorLineL != null) { _cursorLineL.AxisX = area.AxisX; _cursorLineL.AxisY = area.AxisY; }
            if (_cursorTextL != null) { _cursorTextL.AxisX = area.AxisX; _cursorTextL.AxisY = area.AxisY; }

            if (_cursorLineR != null) { _cursorLineR.AxisX = area.AxisX; _cursorLineR.AxisY = area.AxisY; }
            if (_cursorTextR != null) { _cursorTextR.AxisX = area.AxisX; _cursorTextR.AxisY = area.AxisY; }

            // IMPORTANT : rafraîchit les valeurs au bin fixé (LOCK) même si la souris ne bouge pas
            RefreshCursorsFromCurrentSpectrum();
        }
        //
        //
        private void ApplyChartStatsMargin()
        {
            var area = chartSpectrum.ChartAreas.FirstOrDefault(a => a.Name == "SpectrumArea");
            if (area == null) return;

            int w = chartSpectrum.ClientSize.Width;
            if (w <= 0) return;

            int reserved = ChartStatsWidthPx;

            // Si la fenêtre devient petite : on réduit la marge stats pour garder un plot utilisable
            int maxReserved = Math.Max(120, w - ChartStatsMinPlotWidthPx);
            reserved = Math.Min(reserved, maxReserved);

            float reservedPct = 100f * reserved / w;
            reservedPct = Math.Max(0f, Math.Min(45f, reservedPct)); // sécurité

            // On force une position manuelle pour libérer une zone vide à droite du chartArea
            area.Position.Auto = false;

            // Marges "safe" (en %) pour éviter de rogner titres/axes
            const float left = 3f;
            const float top = 3f;
            const float height = 94f;

            float width = 97f - reservedPct; // 97% - marge stats
            if (width < 10f) width = 10f;

            area.Position = new ElementPosition(left, top, width, height);

            chartSpectrum.Invalidate();
        }

        private string BuildChartStatsText(bool left)
        {
            string rms = left ? (lblLeftRms?.Text ?? "RMS:") : (lblRightRms?.Text ?? "RMS:");
            string peak = left ? (lblLeftPeak?.Text ?? "Peak:") : (lblRightPeak?.Text ?? "Peak:");
            string meas = left ? (lblLeftMeas?.Text ?? "—") : (lblRightMeas?.Text ?? "—");

            meas = meas.Replace("\r\n", "\n").Trim();

            return $"{rms}\n{peak}\n{meas}";
        }

        void DrawChartStatsPanel(Graphics g, ChartArea area)
        {
            var plot = GetPlotRectPixels(area);
            if (plot.IsEmpty) return;

            var cr = chartSpectrum.ClientRectangle;

            float panelLeft = plot.Right + 8f;
            float panelRight = cr.Right - 6f;
            if (panelRight - panelLeft < 120f) return;

            var rect = RectangleF.FromLTRB(panelLeft, plot.Top, panelRight, plot.Bottom);

            var saved = g.Save();
            g.ResetClip();

            // Fond blanc + bordure
            using (var bg = new SolidBrush(Color.White))
                g.FillRectangle(bg, rect);
            using (var pen = new Pen(Color.LightGray, 1f))
                g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);

            float pad = 8f;
            float xL = rect.X + pad;
            float xR = rect.Right - pad;

            // Polices (NE PAS disposer chartSpectrum.Font)
            var fontBase = chartSpectrum.Font;
            using var fontHeader = new Font(fontBase, FontStyle.Bold);
            using var fontBold = new Font(fontBase, FontStyle.Bold);
            using var fontStrong = new Font(fontBase.FontFamily, fontBase.SizeInPoints + 1.5f, FontStyle.Bold);
            using var brushStrong = new SolidBrush(Color.DimGray); // très léger "fond"
            using var fontHarm = new Font(fontBase.FontFamily, Math.Max(6f, fontBase.SizeInPoints - 1.0f), FontStyle.Regular);

            using var brushText = new SolidBrush(Color.DimGray);
            using var brushLabel = new SolidBrush(Color.DimGray);

            float y = rect.Y + pad;

            using var penSep = new Pen(Color.Gainsboro, 1f);
            using var fmtCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };
            using var fmtRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Near };
            using var fmtRightMeas = (StringFormat)fmtRight.Clone();
            fmtRightMeas.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;

            float DrawSection(string title, Color titleColor, List<StatRow> rows)
            {
                // Titre centré
                var headerRect = new RectangleF(rect.X + pad, y, rect.Width - 2 * pad, fontHeader.GetHeight(g));
                using (var bTitle = new SolidBrush(titleColor))
                    g.DrawString(title, fontHeader, bTitle, headerRect, fmtCenter);

                y += headerRect.Height + 4f;

                // Trait sous le titre (largeur table)
                g.DrawLine(penSep, rect.X + pad, y, rect.Right - pad, y);
                y += 8f;

                static bool IsHarmonicLabel(string label)
                {
                    if (string.IsNullOrWhiteSpace(label)) return false;
                    label = label.Trim();
                    if (label.Length < 2) return false;
                    if (label[0] != 'H' && label[0] != 'h') return false;
                    return int.TryParse(label[1..], out _);
                }

                if (rows.Count == 0)
                {
                    g.DrawString("—", fontBase, brushLabel, xL, y);
                    y += fontBase.GetHeight(g) + 10f;
                    return y;
                }

                float maxLabelW = 0f;
                foreach (var rr in rows)
                {
                    Font fLabel = rr.Emphasis == StatEmphasis.Strong ? fontBold : fontBase;
                    var sz = g.MeasureString(rr.Label, fLabel);
                    if (sz.Width > maxLabelW) maxLabelW = sz.Width;
                }
                float valueLeft = xL + maxLabelW + 12f; // séparation label -> valeur

                for (int i = 0; i < rows.Count; i++)
                {
                    var r = rows[i];

                    if (r.IsSeparator)
                    {
                        g.DrawLine(penSep, rect.X + pad, y + 4f, rect.Right - pad, y + 4f);
                        y += 12f;
                        continue;
                    }

                    Font fLabel = r.Emphasis == StatEmphasis.Strong ? fontBold : fontBase;
                    Font fVal =
                        r.Emphasis == StatEmphasis.Strong ? fontStrong :
                        r.Emphasis == StatEmphasis.Medium ? fontBold :
                        fontBase;

                    // Label
                    g.DrawString(r.Label, fLabel, brushLabel, xL, y);

                    // Hauteur de ligne maîtrisée
                    float rowHeight = Math.Max(fLabel.GetHeight(g), fVal.GetHeight(g));
                    var valueRect = new RectangleF(valueLeft, y, (rect.Right - pad) - valueLeft, rowHeight);

                    // Highlight EXACT (sans empiéter verticalement)
                    if (r.Emphasis == StatEmphasis.Strong)
                    {
                        using var fmtRange = (StringFormat)fmtRightMeas.Clone();
                        fmtRange.SetMeasurableCharacterRanges(new[] { new CharacterRange(0, r.Value.Length) });

                        var regions = g.MeasureCharacterRanges(r.Value, fVal, valueRect, fmtRange);
                        var bounds = regions[0].GetBounds(g);

                        bounds.Inflate(4f, 0.5f);

                        using var fill = new SolidBrush(Color.FromArgb(18, 30, 144, 255));
                        g.FillRectangle(fill, bounds);
                    }

                    // Valeur (alignée à droite)
                    g.DrawString(r.Value, fVal, brushText, valueRect, fmtRight);

                    y += rowHeight + 4f;
                }

                y += 10f;
                return y;
            }

            // Affichage sections LEFT / RIGHT si activées
            if (_statsShowLeft)
                y = DrawSection("LEFT", Color.RoyalBlue, _rowsLeft);

            if (_statsShowRight)
                y = DrawSection("RIGHT", Color.OrangeRed, _rowsRight);

            g.Restore(saved);
        }
        //
        // Couleurs dynamiques des marqueurs (triangle) selon la voie réellement trackée
        private Color _cursorMarkLColor = Color.DodgerBlue; // C1
        private Color _cursorMarkRColor = Color.OrangeRed;  // C2

        private enum ResolvedCurve
        {
            None,
            Left,
            Right
        }

        private ResolvedCurve ResolveCurveForCursor(bool isCursor1)
        {
            // "Voie dispo" = affichée ET spectre présent (important depuis l'optim voies actives)
            bool leftAvail = _statsShowLeft && currentLeftSpectrum != null;
            bool rightAvail = _statsShowRight && currentRightSpectrum != null;

            // Remplace ces noms par ceux que tu as créés à l'étape 1 si besoin
            CursorTrackMode mode = isCursor1 ? _c1TrackMode : _c2TrackMode;

            switch (mode)
            {
                case CursorTrackMode.Left:
                    return leftAvail ? ResolvedCurve.Left : ResolvedCurve.None;

                case CursorTrackMode.Right:
                    return rightAvail ? ResolvedCurve.Right : ResolvedCurve.None;

                default: // Auto
                    if (leftAvail && rightAvail)
                        return isCursor1 ? ResolvedCurve.Left : ResolvedCurve.Right;
                    if (leftAvail) return ResolvedCurve.Left;
                    if (rightAvail) return ResolvedCurve.Right;
                    return ResolvedCurve.None;
            }
        }

        private static Color CurveColor(ResolvedCurve c)
        {
            return c switch
            {
                ResolvedCurve.Left => Color.DodgerBlue,
                ResolvedCurve.Right => Color.OrangeRed,
                _ => Color.Gray
            };
        }

        private static string CurveLetter(ResolvedCurve c)
        {
            return c switch
            {
                ResolvedCurve.Left => "L",
                ResolvedCurve.Right => "R",
                _ => "—"
            };
        }

        //
        //
        private void RefreshCursorsFromCurrentSpectrum()
        {
            if (freqAxis == null || freqAxis.Length == 0)
                return;

            var area = chartSpectrum.ChartAreas["SpectrumArea"];

            bool onC1 = chkCursorL != null && chkCursorL.Checked; // C1
            bool onC2 = chkCursorR != null && chkCursorR.Checked; // C2

            bool useLog = (chkLogX != null && chkLogX.Checked);
            int minIdx = useLog ? GetMinIndexForLog(freqAxis) : 0;

            // Sécurise les index (si pas init / hors plage)
            if (_cursorIdxL < minIdx) _cursorIdxL = minIdx;
            if (_cursorIdxR < minIdx) _cursorIdxR = minIdx;
            if (_cursorIdxL >= freqAxis.Length) _cursorIdxL = freqAxis.Length - 1;
            if (_cursorIdxR >= freqAxis.Length) _cursorIdxR = freqAxis.Length - 1;

            // Résolution voie pour chaque curseur
            var c1Curve = ResolveCurveForCursor(isCursor1: true);
            var c2Curve = ResolveCurveForCursor(isCursor1: false);

            void UpdateOne(
                bool enabled,
                bool isCursor1,
                int idx,
                VerticalLineAnnotation? line,
                Label? lbl,
                bool locked,
                ResolvedCurve curve,
                ref bool markVisible,
                ref double markFreq,
                ref double markDb,
                ref Color markColor)
            {
                if (!enabled || line == null || idx < minIdx || idx >= freqAxis.Length)
                {
                    if (line != null) line.Visible = false;
                    if (lbl != null) { lbl.Text = ""; lbl.Visible = false; }
                    markVisible = false;
                    return;
                }

                double f = freqAxis[idx];

                // Ligne verticale : toujours à la bonne fréquence
                line.X = f;
                line.Visible = true;

                // Si la voie demandée n'existe pas -> grisé, pas de triangle
                if (curve == ResolvedCurve.None)
                {
                    line.LineColor = Color.FromArgb(180, Color.Gray);
                    markVisible = false;
                    markColor = Color.Gray;

                    if (lbl != null)
                    {
                        lbl.ForeColor = Color.Gray;
                        lbl.Text = $"{(isCursor1 ? "C1" : "C2")}({CurveLetter(curve)}): {f:0.##} Hz  --" + (locked ? "  (LOCK)" : "");
                        lbl.Visible = true;
                    }
                    return;
                }

                double[]? spec = (curve == ResolvedCurve.Left) ? currentLeftSpectrum : currentRightSpectrum;
                if (spec == null || idx >= spec.Length)
                {
                    line.LineColor = Color.FromArgb(180, Color.Gray);
                    markVisible = false;
                    markColor = Color.Gray;

                    if (lbl != null)
                    {
                        lbl.ForeColor = Color.Gray;
                        lbl.Text = $"{(isCursor1 ? "C1" : "C2")}({CurveLetter(curve)}): {f:0.##} Hz  --" + (locked ? "  (LOCK)" : "");
                        lbl.Visible = true;
                    }
                    return;
                }

                double db = spec[idx];

                // Couleur selon la VOIE réellement trackée
                var col = CurveColor(curve);
                line.LineColor = Color.FromArgb(180, col);

                markVisible = true;
                markFreq = f;
                markDb = db;
                markColor = col;

                if (lbl != null)
                {
                    lbl.ForeColor = col;
                    lbl.Text = $"{(isCursor1 ? "C1" : "C2")}({CurveLetter(curve)}): {f:0.##} Hz  {db:0.##} dBFS" + (locked ? "  (LOCK)" : "");
                    lbl.Visible = true;
                }
            }

            // C1
            if (onC1 && _cursorLineL != null)
            {
                _cursorFreqL = freqAxis[_cursorIdxL];
                UpdateOne(onC1, true, _cursorIdxL, _cursorLineL, _lblCursorLInfo, _cursorLockedL, c1Curve,
                    ref _cursorMarkLVisible, ref _cursorMarkLFreq, ref _cursorMarkLDb, ref _cursorMarkLColor);
            }
            else
            {
                if (_cursorLineL != null) _cursorLineL.Visible = false;
                if (_lblCursorLInfo != null) { _lblCursorLInfo.Text = ""; _lblCursorLInfo.Visible = false; }
                _cursorMarkLVisible = false;
            }

            // C2
            if (onC2 && _cursorLineR != null)
            {
                _cursorFreqR = freqAxis[_cursorIdxR];
                UpdateOne(onC2, false, _cursorIdxR, _cursorLineR, _lblCursorRInfo, _cursorLockedR, c2Curve,
                    ref _cursorMarkRVisible, ref _cursorMarkRFreq, ref _cursorMarkRDb, ref _cursorMarkRColor);
            }
            else
            {
                if (_cursorLineR != null) _cursorLineR.Visible = false;
                if (_lblCursorRInfo != null) { _lblCursorRInfo.Text = ""; _lblCursorRInfo.Visible = false; }
                _cursorMarkRVisible = false;
            }
            //
            // --- Delta (C2 - C1) ---
            if (_lblCursorDeltaInfo != null)
            {
                // visible seulement si au moins un curseur activé
                if (!onC1 && !onC2)
                {
                    _lblCursorDeltaInfo.Text = "";
                    _lblCursorDeltaInfo.Visible = false;
                }
                else
                {
                    _lblCursorDeltaInfo.Visible = true;

                    // seulement si les 2 curseurs ont une valeur amplitude valide
                    if (_cursorMarkLVisible && _cursorMarkRVisible)
                    {
                        double dF = _cursorMarkRFreq - _cursorMarkLFreq; // C2 - C1
                        double dA = _cursorMarkRDb - _cursorMarkLDb;      // C2 - C1

                        _lblCursorDeltaInfo.ForeColor = Color.DimGray;
                        _lblCursorDeltaInfo.Text = $"Δ:  ΔF={dF:0.##} Hz   ΔA={dA:0.##} dB";
                    }
                    else
                    {
                        _lblCursorDeltaInfo.ForeColor = Color.Gray;
                        _lblCursorDeltaInfo.Text = "Δ:  —";
                    }
                }
            }

            //
            PositionCursorInfoLabels();
            chartSpectrum.Invalidate();
        }





        //
        private void UpdateRunButtonAppearance()
        {
            if (isLive)
            {
                btnRun.Text = "STOP";
                btnRun.BackColor = Color.Firebrick;
                btnRun.ForeColor = Color.White;
            }
            else
            {
                btnRun.Text = "RUN";
                btnRun.BackColor = Color.SeaGreen;
                btnRun.ForeColor = Color.White;
            }
        }

        public void StartFromRun(bool onceMode)
        {
            liveTimer?.Stop();

            if (onceMode)
            {
                isLive = false;
                AcquireOnce();
                UpdateRunButtonAppearance();
            }
            else
            {
                if (liveTimer == null)
                {
                    liveTimer = new System.Windows.Forms.Timer();
                    liveTimer.Tick += (s, e) => AcquireOnce();
                }

                if (_sampleRate <= 0 || lastFftSize <= 0)
                {
                    liveTimer.Interval = 100;
                }
                else
                {
                    int step = _hopSamples > 0 ? _hopSamples : lastFftSize;
                    int intervalMs = (int)Math.Max(10, Math.Round((double)step * 1000.0 / _sampleRate));
                    liveTimer.Interval = intervalMs;
                }

                isLive = true;
                AcquireOnce();
                liveTimer.Start();
                UpdateRunButtonAppearance();
            }
        }

        public void StopFromRun()
        {
            isLive = false;
            liveTimer?.Stop();
            UpdateRunButtonAppearance();
        }
        //
        private void ResetAveragingState()
        {
            _post.ResetAveragingState();
            UpdateAvgProgressUi(0, avgTargetCount);
        }

        //
        // ---------------- PEAK HOLD ----------------

        private void btnResetPeakHold_Click(object? sender, EventArgs e)
        {
            _post.ResetPeakHoldState();
            UpdateSpectrumChartAndStats();
        }
        //        
        //
        private void AcquireOnce()
        {
            if (_provider == null || _sampleRate <= 0)
                return;

            // 1) Taille FFT
            int N = (_forcedFftSize.HasValue && _forcedFftSize.Value > 0)
                ? _forcedFftSize.Value
                : (lastFftSize > 0 ? lastFftSize : 65536);

            if (N < 512) N = 512;
            lastFftSize = N;

            UpdateOverlayInfo();

            // 2) Durée correspondant à N échantillons
            double msExact = (double)N * 1000.0 / _sampleRate;

            // 3) Récupérer L/R
            var (left, right) = _provider(N);
            var leftSamples = left ?? Array.Empty<int>();
            var rightSamples = right ?? Array.Empty<int>();

            string mode = comboChannel.SelectedItem as string ?? "L+R";
            bool wantLeft = (mode == "Left" || mode == "L+R");
            bool wantRight = (mode == "Right" || mode == "L+R");

            // On ne calcule que ce qui est affiché
            var leftForFft = wantLeft ? leftSamples : Array.Empty<int>();
            var rightForFft = wantRight ? rightSamples : Array.Empty<int>();


            if (leftSamples.Length == 0 && rightSamples.Length == 0)
            {
                ClearSpectrum();
                return;
            }

            // 4) Calcul complet d'une "frame" (buffers bruts + remove DC + fenêtre + FFT + axe freq)
            bool removeDc = (chkRemoveDC != null && chkRemoveDC.Checked);

            var options = new SpectrumOptions
            {
                WindowType = lastWindow,
                RemoveDc = removeDc
            };

            SpectrumResult result = SpectrumAnalyzer.ComputeFrame(leftForFft, rightForFft, N, _sampleRate, options);

            double[]? specL = result.LeftDbFs;
            double[]? specR = result.RightDbFs;

            if (specL == null && specR == null)
            {
                ClearSpectrum();
                return;
            }


            // Averaging (piloté par comboAveraging -> avgTargetCount)
            _post.ApplyAveraging(specL, specR, avgTargetCount,
                out currentLeftSpectrum,
                out currentRightSpectrum,
                out int avgFill);


            if (avgTargetCount <= 1)
                UpdateAvgProgressUi(0, 0);
            else
                UpdateAvgProgressUi(avgFill, avgTargetCount);

            // Peak-hold (sur la courbe déjà calculée et moyennée)
            bool peakEnabled = (chkPeakHold != null && chkPeakHold.Checked);
            _post.UpdatePeakHold(currentLeftSpectrum, currentRightSpectrum, peakEnabled);



            // 8) Axe fréquence
            freqAxis = result.FreqAxisHz;

            // On garde une copie des derniers buffers temps pour les stats
            lastTimeLeft = result.LeftTimeRaw;
            lastTimeRight = result.RightTimeRaw;

            // 9) Mise à jour graphe + stats temporelles
            UpdateSpectrumChartAndStats(lastTimeLeft, lastTimeRight);
        }
        //
        //
        private void ChartSpectrum_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ShowAxisDialog();
            }
        }

        private void ShowAxisDialog()
        {
            if (chartSpectrum.ChartAreas.Count == 0)
                return;

            var area = chartSpectrum.ChartAreas["SpectrumArea"];

            // Valeurs actuelles (si custom non null, on les utilise)
            double yMinCurrent = customYMin ?? area.AxisY.Minimum;
            double yMaxCurrent = customYMax ?? area.AxisY.Maximum;
            double fMinCurrent = customFMin ?? area.AxisX.Minimum;   // Hz
            double fMaxCurrent = customFMax ?? area.AxisX.Maximum;   // Hz

            using (var dlg = new SpectrumAxisDialog())
            {
                dlg.YMin = yMinCurrent;
                dlg.YMax = yMaxCurrent;
                dlg.FMinHz = fMinCurrent;   // *** en Hz ***
                dlg.FMaxHz = fMaxCurrent;   // *** en Hz ***

                dlg.SampleRateHz = _sampleRate;
                // 🔧 Ajuster dynamiquement la borne max de Fmax
                double fmaxCap = SpectrumAxisDialog.GetAllowedFmax(_sampleRate);
                dlg.SetFmaxMaximum(fmaxCap);

                dlg.FitYProvider = GetCurrentYRangeGlobal;
                dlg.FftSizeN = lastFftSize;


                if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                {
                    customYMin = dlg.YMin;
                    customYMax = dlg.YMax;

                    customFMin = dlg.FMinHz;   // plus de *1000
                    customFMax = dlg.FMaxHz;

                    if (customYMin >= customYMax)
                        customYMin = customYMax - 10.0;

                    if (customFMin < 0.0)
                        customFMin = 0.0;
                    if (customFMax <= customFMin)
                        customFMax = customFMin + 10.0;

                    UpdateSpectrumChartAndStats();
                }
            }
        }
        //
        private static string FormatHzK(double hz)
        {
            if (hz >= 1000.0)
                return (hz / 1000.0).ToString("0.#") + "k";
            return hz.ToString("0");
        }

        // Surcharge pratique : réutilise les derniers buffers temps connus
        private void UpdateSpectrumChartAndStats()
        {
            UpdateSpectrumChartAndStats(lastTimeLeft, lastTimeRight);

        }
        //
        private void UpdateSpectrumChartAndStats(double[]? timeL, double[]? timeR)
        {
            if (chartSpectrum.ClientSize.Width <= 0 || chartSpectrum.ClientSize.Height <= 0)
                return;
            if (freqAxis == null || (currentLeftSpectrum == null && currentRightSpectrum == null))
                return;

            // Helper local : estimation rapide de F0 (pas de THD => pas de ComputeChannel)
            static double? EstimateFundamentalHz(double[] spectrum, int sampleRate, int fftSize)
            {
                if (spectrum == null || spectrum.Length < 3 || sampleRate <= 0 || fftSize <= 0)
                    return null;

                int startBin = 1; // ignore DC
                int endBin = spectrum.Length - 1;

                int maxIdx = startBin;
                double maxVal = spectrum[startBin];

                for (int i = startBin + 1; i <= endBin; i++)
                {
                    double v = spectrum[i];
                    if (v > maxVal)
                    {
                        maxVal = v;
                        maxIdx = i;
                    }
                }

                return maxIdx * (double)sampleRate / fftSize;
            }

            chartSpectrum.Series.Clear();

            var area = chartSpectrum.ChartAreas["SpectrumArea"];

            // --- Axe X lin / log ---
            bool useLog = (chkLogX != null && chkLogX.Checked);
            // Garder l'info pour les autres fonctions
            _xAxisIsLog = useLog;

            // En log, on évite le bin 0
            int startIndex = useLog ? 1 : 0;

            // Valeurs AUTO (sans contrainte utilisateur)
            double fMaxAuto = _sampleRate > 0 ? _sampleRate / 2.0 : 1.0;
            double fMinAuto;

            if (useLog)
            {
                if (freqAxis != null && freqAxis.Length > startIndex)
                    fMinAuto = Math.Max(freqAxis[startIndex], 1.0);
                else
                    fMinAuto = 1.0;

                area.AxisX.IsMarginVisible = false;   // évite le “padding” à gauche
            }
            else
            {
                fMinAuto = 0.0;
                area.AxisX.IsMarginVisible = true;
            }

            // --- Application des limites X custom (si définies) ---
            double fMin = customFMin ?? fMinAuto;   // Hz
            double fMax = customFMax ?? fMaxAuto;   // Hz

            // Sécurités de base
            double xMinPhys = useLog ? 1.0 : 0.0;
            if (fMin < xMinPhys) fMin = xMinPhys;
            double nyquist = _sampleRate / 2.0;

            // Sécurité fMax
            double uiMax = SpectrumAxisDialog.GetAllowedFmax(_sampleRate);
            if (fMax > uiMax)
                fMax = uiMax;

            // --- Axe Y (dBFS) avec limites custom éventuelles ---
            double yMinAuto = -200.0;
            double yMaxAuto = 0.0;

            double yMin = customYMin ?? yMinAuto;
            double yMax = customYMax ?? yMaxAuto;

            if (yMin >= yMax)
                yMin = yMax - 10.0; // au moins 10 dB de span

            // ---------------- Grilles & divisions ----------------
            const int majorDivCount = 4;
            const int minorPerMajor = 5;

            double ySpan = yMax - yMin;
            if (ySpan <= 0) ySpan = 1.0;

            double rawMinor = ySpan / (majorDivCount * minorPerMajor);
            if (rawMinor <= 0) rawMinor = 1.0;

            double yMinor = rawMinor;

            double scaled = yMinor * 100.0;
            double scaledRound = Math.Round(scaled);
            bool hasMoreThan2Decimals = Math.Abs(scaled - scaledRound) > 1e-9;

            if (hasMoreThan2Decimals)
                yMinor = Math.Round(yMinor, 2, MidpointRounding.AwayFromZero);

            if (yMinor <= 0)
                yMinor = 0.01;

            double yMajor = minorPerMajor * yMinor;

            area.AxisY.Minimum = yMin;
            area.AxisY.Maximum = yMax;

            _currentYMinor = yMinor;
            _currentYMajor = yMajor;

            area.AxisY.Interval = yMajor;
            area.AxisY.IntervalOffset = 0.0;

            area.AxisY.LabelStyle.Interval = yMajor;
            area.AxisY.LabelStyle.IntervalOffset = 0.0;

            area.AxisY.MajorTickMark.Enabled = true;
            area.AxisY.MajorTickMark.Interval = yMajor;
            area.AxisY.MajorTickMark.IntervalOffset = 0.0;

            area.AxisY.MajorGrid.Enabled = true;
            area.AxisY.MajorGrid.Interval = yMajor;
            area.AxisY.MajorGrid.IntervalOffset = 0.0;
            area.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Solid;

            area.AxisY.MinorGrid.Enabled = true;
            area.AxisY.MinorGrid.Interval = yMinor;
            area.AxisY.MinorGrid.IntervalOffset = 0.0;
            area.AxisY.MinorGrid.LineDashStyle = ChartDashStyle.Dot;
            area.AxisY.MinorGrid.LineColor = Color.LightGray;

            area.AxisY.MinorTickMark.Enabled = false;

            int yDecimals = 0;
            if (yMinor < 1.0)
            {
                yDecimals = (int)Math.Ceiling(-Math.Log10(yMinor));
                yDecimals = Math.Min(yDecimals, 2);
            }
            area.AxisY.LabelStyle.Format = "F" + yDecimals;

            // --- Axe X : on fixe d'abord les bornes, puis on active le log si besoin ---
            area.AxisX.Minimum = fMin;
            area.AxisX.Maximum = fMax;

            if (useLog)
            {
                if (area.AxisX.Minimum <= 0)
                    area.AxisX.Minimum = 1.0;
                if (area.AxisX.Maximum <= area.AxisX.Minimum)
                    area.AxisX.Maximum = area.AxisX.Minimum * 10.0;

                double minDecade = Math.Pow(10.0, Math.Floor(Math.Log10(area.AxisX.Minimum)));
                area.AxisX.Minimum = Math.Max(1.0, minDecade);

                if (area.AxisX.Maximum <= area.AxisX.Minimum)
                    area.AxisX.Maximum = area.AxisX.Minimum * 10.0;

                area.AxisX.IsLogarithmic = true;
                area.AxisX.LogarithmBase = 10.0;

                area.AxisX.Interval = 1.0;
                area.AxisX.LabelStyle.Interval = 1.0;
                area.AxisX.MajorTickMark.Interval = 1.0;

                area.AxisX.MajorGrid.Enabled = true;
                area.AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Solid;
                area.AxisX.MajorGrid.Interval = 1.0;

                area.AxisX.MinorGrid.Enabled = false;

                area.AxisX.MajorGrid.IntervalOffset = 0.0;
                area.AxisX.MinorGrid.IntervalOffset = 0.0;
                area.AxisX.MinorTickMark.IntervalOffset = 0.0;
            }
            else
            {
                area.AxisX.IsLogarithmic = false;

                double xSpan = fMax - fMin;
                if (xSpan <= 0) xSpan = 1.0;

                int spanHz = (int)Math.Round(xSpan);
                if (spanHz < 1) spanHz = 1;

                var grid = ChooseBestStepWithSpanAdjustment(spanHz);
                double xMajor = grid.StepHz;

                double fMaxAdjusted = fMin + grid.SpanAdjustedHz;

                area.AxisX.Minimum = fMin;
                area.AxisX.Maximum = fMaxAdjusted;

                area.AxisX.Interval = xMajor;
                area.AxisX.IntervalOffset = 0.0;

                area.AxisX.LabelStyle.Interval = xMajor;
                area.AxisX.LabelStyle.IntervalOffset = 0.0;

                area.AxisX.MajorTickMark.Enabled = true;
                area.AxisX.MajorTickMark.Interval = xMajor;
                area.AxisX.MajorTickMark.IntervalOffset = 0.0;

                area.AxisX.MajorGrid.Enabled = true;
                area.AxisX.MajorGrid.Interval = xMajor;
                area.AxisX.MajorGrid.IntervalOffset = 0.0;

                area.AxisX.MinorGrid.Enabled = true;
                area.AxisX.MinorGrid.Interval = xMajor / 5.0;
                area.AxisX.MinorGrid.IntervalOffset = 0.0;

                area.AxisX.MajorTickMark.Size = 2f;

                area.AxisX.MinorGrid.LineDashStyle = ChartDashStyle.Dot;
                area.AxisX.MinorGrid.LineColor = Color.LightGray;
            }

            // --- Ce qu'on affiche en fonction du combo Channel ---
            string mode = comboChannel.SelectedItem as string ?? "L+R";
            bool showLeft = (mode == "Left" || mode == "L+R") && (currentLeftSpectrum != null);
            bool showRight = (mode == "Right" || mode == "L+R") && (currentRightSpectrum != null);
            bool peakHoldOn = (chkPeakHold != null && chkPeakHold.Checked);

            _statsShowLeft = showLeft;
            _statsShowRight = showRight;

            // >>> MODIF COMMIT 3 : peak-hold vient du post-processor
            double[]? peakL = peakHoldOn ? _post.GetPeakLeft() : null;
            double[]? peakR = peakHoldOn ? _post.GetPeakRight() : null;

            // --- Overlays ---
            foreach (var slot in _overlays.Slots)
            {
                if (!slot.Enabled || slot.Data == null)
                    continue;

                if (_sampleRate > 0 && slot.Data.SampleRate != _sampleRate)
                    continue;

                if (showLeft)
                {
                    var ov = slot.Data.LeftDbFs ?? slot.Data.RightDbFs;
                    if (ov != null)
                    {
                        var s = new ChartSeries($"OV{slot.Index + 1}_L");
                        s.ChartType = SeriesChartType.FastLine;
                        s.ChartArea = "SpectrumArea";
                        s.Color = Color.Gray;
                        s.BorderDashStyle = ChartDashStyle.Dot;
                        s.BorderWidth = 1;

                        for (int i = startIndex; i < ov.Length; i++)
                        {
                            double f = (double)i * _sampleRate / slot.Data.FftSize;
                            s.Points.AddXY(f, ov[i]);
                        }
                        chartSpectrum.Series.Add(s);
                    }
                }

                if (showRight)
                {
                    var ov = slot.Data.RightDbFs ?? slot.Data.LeftDbFs;
                    if (ov != null)
                    {
                        var s = new ChartSeries($"OV{slot.Index + 1}_R");
                        s.ChartType = SeriesChartType.FastLine;
                        s.ChartArea = "SpectrumArea";
                        s.Color = Color.DarkGray;
                        s.BorderDashStyle = ChartDashStyle.Dot;
                        s.BorderWidth = 1;

                        for (int i = startIndex; i < ov.Length; i++)
                        {
                            double f = (double)i * _sampleRate / slot.Data.FftSize;
                            s.Points.AddXY(f, ov[i]);
                        }
                        chartSpectrum.Series.Add(s);
                    }
                }
            }

            // Courbe gauche
            if (showLeft)
            {
                var seriesL = new System.Windows.Forms.DataVisualization.Charting.Series("Left");
                seriesL.ChartType = SeriesChartType.Line;
                seriesL.BorderWidth = 2;
                seriesL.ChartArea = "SpectrumArea";
                seriesL.Color = Color.DodgerBlue;

                for (int i = startIndex; i < freqAxis.Length && i < currentLeftSpectrum.Length; i++)
                    seriesL.Points.AddXY(freqAxis[i], currentLeftSpectrum[i]);

                chartSpectrum.Series.Add(seriesL);

                if (peakHoldOn && peakL != null)
                {
                    var seriesLph = new ChartSeries("LeftPeakHold");
                    seriesLph.ChartType = SeriesChartType.FastLine;
                    seriesLph.ChartArea = "SpectrumArea";
                    seriesLph.Color = Color.DodgerBlue;
                    seriesLph.BorderDashStyle = ChartDashStyle.Dash;
                    seriesLph.BorderWidth = 1;

                    for (int i = startIndex; i < freqAxis.Length && i < peakL.Length; i++)
                        seriesLph.Points.AddXY(freqAxis[i], peakL[i]);

                    chartSpectrum.Series.Add(seriesLph);
                }
            }

            // Courbe droite
            if (showRight && currentRightSpectrum != null)
            {
                var seriesR = new System.Windows.Forms.DataVisualization.Charting.Series("Right");
                seriesR.ChartType = SeriesChartType.Line;
                seriesR.BorderWidth = 2;
                seriesR.ChartArea = "SpectrumArea";
                seriesR.Color = Color.OrangeRed;

                for (int i = startIndex; i < freqAxis.Length && i < currentRightSpectrum.Length; i++)
                    seriesR.Points.AddXY(freqAxis[i], currentRightSpectrum[i]);

                chartSpectrum.Series.Add(seriesR);

                if (peakHoldOn && peakR != null)
                {
                    var seriesRph = new ChartSeries("RightPeakHold");
                    seriesRph.ChartType = SeriesChartType.FastLine;
                    seriesRph.ChartArea = "SpectrumArea";
                    seriesRph.Color = Color.OrangeRed;
                    seriesRph.BorderDashStyle = ChartDashStyle.Dash;
                    seriesRph.BorderWidth = 1;

                    for (int i = startIndex; i < freqAxis.Length && i < peakR.Length; i++)
                        seriesRph.Points.AddXY(freqAxis[i], peakR[i]);

                    chartSpectrum.Series.Add(seriesRph);
                }
            }

            // --- Stats temporelles : LEFT ---
            if (timeL != null && timeL.Length > 0)
            {
                double peakLfs = 0.0;
                for (int i = 0; i < timeL.Length; i++)
                {
                    double x = timeL[i] / (double)SpectrumAnalyzer.Max24Pos;
                    double ax = Math.Abs(x);
                    if (ax > peakLfs) peakLfs = ax;
                }

                double meanL = 0.0;
                for (int i = 0; i < timeL.Length; i++)
                    meanL += timeL[i];
                meanL /= timeL.Length;

                double sumSqL = 0.0;
                for (int i = 0; i < timeL.Length; i++)
                {
                    double x = (timeL[i] - meanL) / (double)SpectrumAnalyzer.Max24Pos;
                    sumSqL += x * x;
                }
                double rmsL = Math.Sqrt(sumSqL / timeL.Length);

                double rmsLDb = rmsL > 0 ? 20.0 * Math.Log10(rmsL * Math.Sqrt(2.0)) : -200.0;
                double peakLDb = peakLfs > 0 ? 20.0 * Math.Log10(peakLfs) : -200.0;

                if (rmsLDb < -200.0) rmsLDb = -200.0;
                if (peakLDb < -200.0) peakLDb = -200.0;

                lblLeftRms.Text = $"RMS:  {rmsLDb:F2} dBFS";
                lblLeftPeak.Text = $"Peak: {peakLDb:F2} dBFS";
            }
            else
            {
                lblLeftRms.Text = "RMS:";
                lblLeftPeak.Text = "Peak:";
            }

            // --- Stats temporelles : RIGHT ---
            if (timeR != null && timeR.Length > 0)
            {
                double peakRfs = 0.0;
                for (int i = 0; i < timeR.Length; i++)
                {
                    double x = timeR[i] / (double)SpectrumAnalyzer.Max24Pos;
                    double ax = Math.Abs(x);
                    if (ax > peakRfs) peakRfs = ax;
                }

                double meanR = 0.0;
                for (int i = 0; i < timeR.Length; i++)
                    meanR += timeR[i];
                meanR /= timeR.Length;

                double sumSqR = 0.0;
                for (int i = 0; i < timeR.Length; i++)
                {
                    double x = (timeR[i] - meanR) / (double)SpectrumAnalyzer.Max24Pos;
                    sumSqR += x * x;
                }
                double rmsR = Math.Sqrt(sumSqR / timeR.Length);

                double rmsRDb = rmsR > 0 ? 20.0 * Math.Log10(rmsR * Math.Sqrt(2.0)) : -200.0;
                double peakRDb = peakRfs > 0 ? 20.0 * Math.Log10(peakRfs) : -200.0;

                if (rmsRDb < -200.0) rmsRDb = -200.0;
                if (peakRDb < -200.0) peakRDb = -200.0;

                lblRightRms.Text = $"RMS:  {rmsRDb:F2} dBFS";
                lblRightPeak.Text = $"Peak: {peakRDb:F2} dBFS";
            }
            else
            {
                lblRightRms.Text = "";
                lblRightPeak.Text = "";
            }

            // =========================
            //   MESURES ANALYTIQUES
            // =========================
            bool doThd = _thdMeasuresEnabled;
            ChannelMetrics? mL = null;
            ChannelMetrics? mR = null;

            bool singleChannel = (showLeft && !showRight) || (showRight && !showLeft);
            bool showHarmsInPanel = doThd && _thdUi.ShowHarmonicsValues && singleChannel;

            // LEFT
            if (lblLeftMeas != null && currentLeftSpectrum != null && _sampleRate > 0 && lastFftSize > 0)
            {
                if (doThd)
                {
                    mL = SpectrumMetrics.ComputeChannel(currentLeftSpectrum, _sampleRate, lastFftSize, _metricsOpt);

                    if (mL.Valid)
                    {
                        string thdText;
                        string thdnText;

                        if (_thdUi.Unit == SpectrumSettings.ThdSettings.ThdUnit.Percent)
                        {
                            // THD ratio -> %
                            thdText = $"THD: {mL.Thd * 100.0:F5} %";

                            // THD+N : on convertit depuis le dB (ratio = 10^(dB/20))
                            double thdnPct = Math.Pow(10.0, mL.ThdNDb / 20.0) * 100.0;
                            thdnText = $"THD+N: {thdnPct:F5} %";
                        }
                        else
                        {
                            thdText = $"THD: {mL.ThdDb:F1} dB";
                            thdnText = $"THD+N: {mL.ThdNDb:F1} dB";
                        }

                        lblLeftMeas.Text =
                            $"F0: {mL.FundamentalHz:F1} Hz\n" +
                            thdText + "\n" +
                            thdnText + "\n" +
                            $"SINAD: {mL.SinadDb:F1} dB\n" +
                            $"SNR: {mL.SnrDb:F1} dB\n" +
                            $"SFDR: {mL.SfdrDb:F1} dB";

                        if (showHarmsInPanel && showLeft)
                        {
                            var list = mL.Harmonics;
                            if (list != null && list.Count > 0)
                            {
                                lblLeftMeas.Text += "\n----";
                                foreach (var h in list)
                                {
                                    if (_thdUi.Unit == SpectrumSettings.ThdSettings.ThdUnit.Percent)
                                        lblLeftMeas.Text += $"\nH{h.Order}: {h.RatioPercent:F5} %";
                                    else
                                        lblLeftMeas.Text += $"\nH{h.Order}: {h.LevelDbc:F1} dB";
                                }
                            }
                        }


                    }
                    else
                    {
                        lblLeftMeas.Text = "—";
                    }
                }
                else
                {
                    // THD OFF => F0 uniquement (rapide)
                    var f0 = EstimateFundamentalHz(currentLeftSpectrum, _sampleRate, lastFftSize);
                    lblLeftMeas.Text = f0.HasValue ? $"F0: {f0.Value:F1} Hz" : "—";
                }
            }
            else if (lblLeftMeas != null)
            {
                lblLeftMeas.Text = "";
            }

            // RIGHT
            if (lblRightMeas != null && currentRightSpectrum != null && _sampleRate > 0 && lastFftSize > 0)
            {
                if (doThd)
                {
                    mR = SpectrumMetrics.ComputeChannel(currentRightSpectrum, _sampleRate, lastFftSize, _metricsOpt);

                    if (mR.Valid)
                    {
                        string thdText;
                        string thdnText;

                        if (_thdUi.Unit == SpectrumSettings.ThdSettings.ThdUnit.Percent)
                        {
                            thdText = $"THD: {mR.Thd * 100.0:F5} %";

                            double thdnPct = Math.Pow(10.0, mR.ThdNDb / 20.0) * 100.0;
                            thdnText = $"THD+N: {thdnPct:F5} %";
                        }
                        else
                        {
                            thdText = $"THD: {mR.ThdDb:F1} dB";
                            thdnText = $"THD+N: {mR.ThdNDb:F1} dB";
                        }

                        lblRightMeas.Text =
                            $"F0: {mR.FundamentalHz:F1} Hz\n" +
                            thdText + "\n" +
                            thdnText + "\n" +
                            $"SINAD: {mR.SinadDb:F1} dB\n" +
                            $"SNR: {mR.SnrDb:F1} dB\n" +
                            $"SFDR: {mR.SfdrDb:F1} dB";

                        if (showHarmsInPanel && showRight)
                        {
                            var list = mR.Harmonics;
                            if (list != null && list.Count > 0)
                            {
                                lblRightMeas.Text += "\n----";
                                foreach (var h in list)
                                {
                                    if (_thdUi.Unit == SpectrumSettings.ThdSettings.ThdUnit.Percent)
                                        lblRightMeas.Text += $"\nH{h.Order}: {h.RatioPercent:F5} %";
                                    else
                                        lblRightMeas.Text += $"\nH{h.Order}: {h.LevelDbc:F1} dB";
                                }
                            }
                        }

                    }
                    else
                    {
                        lblRightMeas.Text = "—";
                    }
                }
                else
                {
                    // THD OFF => F0 uniquement (rapide)
                    var f0 = EstimateFundamentalHz(currentRightSpectrum, _sampleRate, lastFftSize);
                    lblRightMeas.Text = f0.HasValue ? $"F0: {f0.Value:F1} Hz" : "—";
                }
            }
            else if (lblRightMeas != null)
            {
                lblRightMeas.Text = "";
            }

            RebuildStatsRowsFromLabels();
            RefreshCursorAfterRedraw();
        }

        //
        private static double? EstimateFundamentalHz(double[] spectrum, int sampleRate, int fftSize)
        {
            if (spectrum == null || spectrum.Length < 3 || sampleRate <= 0 || fftSize <= 0)
                return null;

            int startBin = 1; // ignore DC
            int endBin = spectrum.Length - 1;

            int maxIdx = startBin;
            double maxVal = spectrum[startBin];

            for (int i = startBin + 1; i <= endBin; i++)
            {
                double v = spectrum[i];
                if (v > maxVal)
                {
                    maxVal = v;
                    maxIdx = i;
                }
            }

            return maxIdx * (double)sampleRate / fftSize;
        }

        //
        private double GetFrequencyFromMouseX(int mouseX)
        {
            var area = chartSpectrum.ChartAreas["SpectrumArea"];
            bool useLog = (chkLogX != null && chkLogX.Checked);

            double xMinVal = area.AxisX.Minimum;
            double xMaxVal = area.AxisX.Maximum;

            if (xMaxVal <= xMinVal)
                return xMinVal;

            // Récupère les positions pixels du min/max axe X
            double pxMin = area.AxisX.ValueToPixelPosition(xMinVal);
            double pxMax = area.AxisX.ValueToPixelPosition(xMaxVal);

            // Si quelque chose est inversé ou invalide, fallback
            if (double.IsNaN(pxMin) || double.IsNaN(pxMax) || Math.Abs(pxMax - pxMin) < 1e-6)
            {
                try
                {
                    return area.AxisX.PixelPositionToValue(mouseX);
                }
                catch
                {
                    return xMinVal;
                }
            }

            // Position relative 0..1 dans la zone de tracé
            double t = (mouseX - pxMin) / (pxMax - pxMin);
            if (t < 0.0) t = 0.0;
            if (t > 1.0) t = 1.0;

            if (!useLog)
            {
                return xMinVal + t * (xMaxVal - xMinVal);
            }

            // LOG : f = min * (max/min)^t
            // (valeurs doivent être > 0)
            double min = Math.Max(1e-12, xMinVal);
            double max = Math.Max(min * 1.000001, xMaxVal);

            return min * Math.Pow(max / min, t);
        }
        //
        //
        private void ClearSpectrum()
        {
            // Sécuriser l’axe X quand on n’a plus de données
            if (chartSpectrum.ChartAreas.Count > 0)
            {
                var area = chartSpectrum.ChartAreas[0]; // ou "SpectrumArea" si tu préfères

                // On repasse en linéaire pour éviter les problèmes
                area.AxisX.IsLogarithmic = false;

                // Bornes "safe" par défaut
                area.AxisX.Minimum = 0.0;
                area.AxisX.Maximum = _sampleRate > 0 ? _sampleRate / 2.0 : 1.0;

                area.AxisX.MajorGrid.Enabled = true;
                area.AxisX.MinorGrid.Enabled = false;

            }

            // On vide les séries
            chartSpectrum.Series.Clear();

            // On remet les labels de stats
            lblLeftRms.Text = "RMS:";
            lblLeftPeak.Text = "Peak:";
            lblRightRms.Text = "";
            lblRightPeak.Text = "";

            if (_overlayInfoAnno != null)
                _overlayInfoAnno.Visible = false;

            currentLeftSpectrum = null;
            currentRightSpectrum = null;
            freqAxis = null;
            lastTimeLeft = null;
            lastTimeRight = null;
        }
        //
        public void OverlayLoad(int slotIndex, string filePath)
        {
            _overlays.LoadIntoSlot(slotIndex, filePath);
            UpdateSpectrumChartAndStats(lastTimeLeft, lastTimeRight);
        }

        public void OverlayClear(int slotIndex)
        {
            _overlays.ClearSlot(slotIndex);
            UpdateSpectrumChartAndStats(lastTimeLeft, lastTimeRight);
        }

        public void OverlaySetEnabled(int slotIndex, bool enabled)
        {
            _overlays.Slots[slotIndex].Enabled = enabled;
            UpdateSpectrumChartAndStats(lastTimeLeft, lastTimeRight);
        }

        public bool OverlayIsLoaded(int slotIndex) => _overlays.Slots[slotIndex].Data != null;
        public bool OverlayIsEnabled(int slotIndex) => _overlays.Slots[slotIndex].Enabled;

        public void OverlaySave(int slotIndex, string filePath, string name)
        {
            if (freqAxis == null || currentLeftSpectrum == null || _sampleRate <= 0 || lastFftSize <= 0)
                throw new InvalidOperationException("Pas de spectre courant à sauvegarder.");

            // Snapshot = ce qui est affiché (après averaging)
            var snap = new SpectrumResult(
                lastFftSize,
                _sampleRate,
                freqAxis,
                currentLeftSpectrum,
                currentRightSpectrum,
                lastTimeLeft,
                lastTimeRight,
                null,
                null);

            var options = new SpectrumOptions
            {
                WindowType = lastWindow,
                RemoveDc = (chkRemoveDC != null && chkRemoveDC.Checked),
                NeedComplexFft = false
            };

            _overlays.SaveToFile(filePath, snap, name, options);
            _overlays.LoadIntoSlot(slotIndex, filePath);   // <-- charge dans le slot
            UpdateSpectrumChartAndStats(lastTimeLeft, lastTimeRight); // <-- redraw immédiat
        }
        //
        public (bool loaded, bool enabled, string name, string? filePath, bool compatible) OverlayGetStatus(int slotIndex)
        {
            var s = _overlays.Slots[slotIndex];
            bool loaded = (s.Data != null);
            bool enabled = s.Enabled;

            string name = loaded ? s.Name : $"Overlay {slotIndex + 1}";
            string? filePath = s.FilePath;

            bool compatible = true;
            if (loaded && s.Data != null)
                if (_sampleRate <= 0)
                    compatible = true;          // SR inconnu => ne pas marquer incompatible
                else
                    compatible = (s.Data.SampleRate == _sampleRate);



            return (loaded, enabled, name, filePath, compatible);
        }

        public (string? filePath, bool enabled)[] OverlayExportState()
        {
            var state = new (string? filePath, bool enabled)[OverlayManager.MaxOverlays];
            for (int i = 0; i < OverlayManager.MaxOverlays; i++)
            {
                var s = _overlays.Slots[i];
                state[i] = (s.FilePath, s.Enabled);
            }
            return state;
        }

        public void OverlayImportState((string? filePath, bool enabled)[] state)
        {
            if (state == null) return;

            int n = Math.Min(state.Length, OverlayManager.MaxOverlays);
            for (int i = 0; i < n; i++)
            {
                var (path, enabled) = state[i];

                _overlays.ClearSlot(i);

                if (!string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path))
                {
                    try
                    {
                        _overlays.LoadIntoSlot(i, path);
                        _overlays.Slots[i].Enabled = enabled;
                    }
                    catch
                    {
                        // ignore: fichier invalide ou lecture impossible
                        _overlays.ClearSlot(i);
                    }
                }
                else
                {
                    _overlays.Slots[i].Enabled = enabled;
                }
            }

            UpdateSpectrumChartAndStats(lastTimeLeft, lastTimeRight);
        }


        private void lblLeftMeas_Click(object sender, EventArgs e)
        {

        }

        private void ChartSpectrum_PostPaint(object sender, ChartPaintEventArgs e)
        {
            // IMPORTANT: ne pas comparer les instances (e.ChartElement != area peut être vrai même pour la bonne zone)
            var area = e.ChartElement as ChartArea;
            if (area == null || area.Name != "SpectrumArea")
                return;

            /// ----------------------------
            // Dessin des marqueurs de curseur
            /// ----------------------------
            void DrawDownTriangle(double f, double db, Color color)
            {
                // Position exacte en pixels
                float x = (float)area.AxisX.ValueToPixelPosition(f);
                float y = (float)area.AxisY.ValueToPixelPosition(db);

                // Triangle inversé : pointe basse sur la courbe
                const float h = 10f;  // hauteur triangle (px)
                const float w = 12f;  // largeur triangle (px)

                var pts = new[]
                {
                new PointF(x, y),              // pointe basse (sur la courbe)
                new PointF(x - w/2f, y - h),    // haut gauche
                new PointF(x + w/2f, y - h)     // haut droit
            };

                using var brush = new SolidBrush(Color.FromArgb(220, color));
                e.ChartGraphics.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.ChartGraphics.Graphics.FillPolygon(brush, pts);
            }

            if (_cursorMarkLVisible)
                DrawDownTriangle(_cursorMarkLFreq, _cursorMarkLDb, _cursorMarkLColor);

            if (_cursorMarkRVisible)
                DrawDownTriangle(_cursorMarkRFreq, _cursorMarkRDb, _cursorMarkRColor);


            // ----------------------------
            // Y : ticks + labels mineurs (5 sous-divisions par majeur) - toujours actif
            // ----------------------------
            {
                double yMajor = area.AxisY.MajorGrid.Interval;
                double yMinor = area.AxisY.MinorGrid.Interval;

                if (yMajor > 0 && yMinor > 0)
                {
                    double yMin = area.AxisY.Minimum;
                    double yMax = area.AxisY.Maximum;

                    float xLeftPlot = (float)area.AxisX.ValueToPixelPosition(area.AxisX.Minimum);

                    // Style (comme sous-labels X): police plus petite + gris
                    Font baseFont = area.AxisY.LabelStyle.Font ?? chartSpectrum.Font;
                    using (Font minorFont = new Font(baseFont.FontFamily, Math.Max(6f, baseFont.Size * MinorLabelScale), baseFont.Style))
                    using (Brush minorBrush = new SolidBrush(Color.FromArgb(170, Color.Black)))
                    using (Pen p = new Pen(Color.FromArgb(140, Color.Gray), 1.0f))
                    {
                        const float tickLenLeft = 5f;   // longueur vers la gauche (extérieur)
                        const float gap = 1f;           // petit espace avant l’axe
                        const float margin = 8f;   // marge texte-axe

                        // Anti-chevauchement vertical simple
                        float lastLabelBottom = float.NegativeInfinity;

                        // Dernier tick mineur <= yMax, mais ancré sur yMin (et pas sur 0)
                        double yStart = yMin + Math.Floor((yMax - yMin) / yMinor) * yMinor;

                        // Ancrage d'index sur yMin (cohérent avec IntervalOffset = yMin)
                        double y0 = yMin;



                        int minorPerMajor = (int)Math.Round(yMajor / yMinor);
                        if (minorPerMajor <= 0) minorPerMajor = 5;

                        for (double y = yStart; y >= yMin - 1e-9; y -= yMinor)
                        {
                            // skip majeurs
                            // Skip si on tombe sur un majeur : on teste l'index (y - y0)/yMinor
                            int idx = (int)Math.Round((y - y0) / yMinor);

                            // modulo robuste (si idx négatif)
                            int m = idx % minorPerMajor;
                            if (m < 0) m += minorPerMajor;

                            // Skip si c'est un majeur
                            if (m == 0)
                                continue;



                            float yPix = (float)area.AxisY.ValueToPixelPosition(y);

                            // Tick
                            e.ChartGraphics.Graphics.DrawLine(p, xLeftPlot - gap, yPix, xLeftPlot - gap - tickLenLeft, yPix);

                            // Texte (à gauche de l’axe)
                            string txt = FormatDbMinor(y, yMinor);

                            SizeF sz = e.ChartGraphics.Graphics.MeasureString(txt, minorFont);

                            float xText = xLeftPlot - sz.Width - margin;
                            float yText = yPix - sz.Height / 2f;

                            if (yText <= lastLabelBottom + 2f) continue;

                            var g = e.ChartGraphics.Graphics;
                            var saved = g.Save();
                            g.ResetClip();
                            g.DrawString(txt, minorFont, minorBrush, xText, yText);
                            g.Restore(saved);

                            lastLabelBottom = yText + sz.Height;
                        }

                    }
                }
                // ---------------------------------------------------------
                // Ligne SR/N/Overlap au dessus du graphe, ALIGNÉE EN HAUTEUR avec AxisX.Title ("Frequency (Hz)")
                // ---------------------------------------------------------
                if (!string.IsNullOrWhiteSpace(_TopInfoLine))
                {
                    var g = e.ChartGraphics.Graphics;

                    // On sort du clip de la zone de tracé, car on dessine dans la marge (zone du titre d’axe)
                    var savedClip = g.Clip;
                    g.ResetClip();

                    try
                    {
                        // Font : même famille/taille que le titre d’axe X si défini, sinon chart font
                        Font titleFont = area.AxisX.TitleFont ?? chartSpectrum.Font;

                        // Coordonnées "absolues" (pixels) du bas du ChartArea
                        // area.Position est en % (0..100) du Chart
                        var pos = area.Position;

                        // bottom-left du ChartArea en pixels
                        //PointF bl = e.ChartGraphics.GetAbsolutePoint(new PointF(pos.X, pos.Y + pos.Height));
                        // Y : juste AU-DESSUS de la zone de tracé (plot)
                        float yPlotTop = (float)area.AxisY.ValueToPixelPosition(area.AxisY.Maximum);
                        float y = yPlotTop - titleFont.Height - 2f;

                        // top-left du ChartArea en pixels (utile pour obtenir x)
                        PointF tl = e.ChartGraphics.GetAbsolutePoint(new PointF(pos.X, pos.Y));

                        // Y : on se place sur la "ligne" du titre d’axe X :
                        // approximativement 1 hauteur de font au-dessus du bas du ChartArea.
                        //float y = bl.Y - titleFont.Height - 2f;

                        // X : à gauche, dans la même bande que le titre d’axe
                        float x = tl.X + 90f;

                        using (Brush b = new SolidBrush(Color.DarkBlue))
                        {
                            g.DrawString(_TopInfoLine, titleFont, b, x, y);

                            // ---- Coin droit : infos filtres THD (si THD active) ----
                            if (_thdMeasuresEnabled)
                            {
                                string FormatHz(double hz)
                                {
                                    if (hz >= 1000.0) return $"{hz / 1000.0:F0} kHz";
                                    return $"{hz:F0} Hz";
                                }

                                string hpf = _thdUi.UseHPF ? FormatHz(_thdUi.FlowHz) : "OFF";
                                string lpf = _thdUi.UseLPF ? FormatHz(_thdUi.FhighHz) : "OFF";

                                string thdLine = $"THD filter - HPF: {hpf}, LPF: {lpf}";

                                // top-right du ChartArea (pixels)
                                PointF tr = e.ChartGraphics.GetAbsolutePoint(new PointF(pos.X + pos.Width, pos.Y));

                                SizeF sz = g.MeasureString(thdLine, titleFont);
                                float xR = tr.X - sz.Width - 10f; // marge droite
                                float yR = y;                     // même hauteur que la ligne gauche

                                using (Brush b2 = new SolidBrush(Color.DarkBlue))
                                {
                                    g.DrawString(thdLine, titleFont, b2, xR, yR);
                                }
                            }
                        }
                    }
                    finally
                    {
                        g.Clip = savedClip;
                    }
                }

            }

            bool useLog = chkLogX != null && chkLogX.Checked;

            if (useLog)
            {
                double fMin = area.AxisX.Minimum;
                double fMax = area.AxisX.Maximum;

                // Sécurité : en log, on ne dessine rien si bornes invalides
                if (fMin <= 0 || fMax <= fMin)
                    return;

                // Lignes mineures log dessinées à la main : 2..9 * 10^n
                using (Pen minorPen = new Pen(Color.FromArgb(80, Color.Gray), 1f))
                {
                    minorPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;

                    // On calcule top/bottom une seule fois
                    float top = (float)area.AxisY.ValueToPixelPosition(area.AxisY.Maximum);
                    float bottom = (float)area.AxisY.ValueToPixelPosition(area.AxisY.Minimum);

                    // Police "sous-label" : ~ moitié de la taille des labels d’axe
                    Font baseFont = area.AxisX.LabelStyle.Font ?? chartSpectrum.Font;
                    using (Font minorFont = new Font(baseFont.FontFamily, Math.Max(6f, baseFont.Size * MinorLabelScale), baseFont.Style))
                    using (Brush minorBrush = new SolidBrush(Color.FromArgb(160, Color.Black)))
                    {
                        // Anti-chevauchement : on saute un label si trop proche du précédent
                        float lastLabelRight = float.NegativeInfinity;

                        double decadeStart = Math.Pow(10, Math.Floor(Math.Log10(fMin)));
                        double decadeEnd = Math.Pow(10, Math.Ceiling(Math.Log10(fMax)));

                        for (double d = decadeStart; d <= decadeEnd; d *= 10.0)
                        {
                            for (int k = 2; k <= 9; k++)
                            {
                                double f = d * k;
                                if (f < fMin || f > fMax)
                                    continue;

                                float xPixel = (float)area.AxisX.ValueToPixelPosition(f);

                                // 1) Ligne verticale (ta sous-grille existante)
                                e.ChartGraphics.Graphics.DrawLine(minorPen, xPixel, top, xPixel, bottom);

                                // 2) (optionnel) petite marque de tick en bas (évite l’effet "pas de marque")
                                const float minorTickLen = 8f;
                                e.ChartGraphics.Graphics.DrawLine(minorPen, xPixel, bottom, xPixel, bottom + minorTickLen);

                                // 3) Label intermédiaire (2..9), police plus petite
                                //    -> on réutilise ton format Hz/kHz
                                string txt = FormatHzK(f);   // ex: 2k, 3k, 5k, 20k...

                                // Mesure + centrage
                                var sz = e.ChartGraphics.Graphics.MeasureString(txt, minorFont);
                                float xText = xPixel - sz.Width / 2f;

                                // Dessin *dans* la zone de tracé, juste au-dessus de l’axe (évite de chevaucher les labels décades)
                                float yText = bottom - minorTickLen + 10f;

                                // Anti-chevauchement en pixels (même si fenêtre large, ça évite les cas extrêmes)
                                if (xText <= lastLabelRight + 4f)
                                    continue;

                                e.ChartGraphics.Graphics.DrawString(txt, minorFont, minorBrush, xText, yText);
                                lastLabelRight = xText + sz.Width;
                            }
                        }
                    }
                }

            }
            else
            {
                // ---------------------------------------------------------
                // X linéaire : ticks + labels mineurs (même esprit que pour Y)
                // ---------------------------------------------------------
                double fMin = area.AxisX.Minimum;
                double fMax = area.AxisX.Maximum;

                double xMajor = area.AxisX.MajorGrid.Interval;
                double xMinor = area.AxisX.MinorGrid.Interval;

                // Sécurités : si l'intervalle n'est pas fixé, on ne dessine pas
                if (xMajor > 0)
                {
                    if (xMinor <= 0)
                        xMinor = xMajor / 5.0; // fallback cohérent avec ton paramétrage linéaire

                    if (xMinor > 0)
                    {
                        // Zone verticale (top/bottom) + baseline (bottom)
                        float top = (float)area.AxisY.ValueToPixelPosition(area.AxisY.Maximum);
                        float bottom = (float)area.AxisY.ValueToPixelPosition(area.AxisY.Minimum);

                        using (Pen minorPen = new Pen(Color.FromArgb(80, Color.Gray), 1f))
                        {
                            minorPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;

                            Font baseFont = area.AxisX.LabelStyle.Font ?? chartSpectrum.Font;
                            using (Font minorFont = new Font(baseFont.FontFamily, Math.Max(6f, baseFont.Size * MinorLabelScale), baseFont.Style))
                            using (Brush minorBrush = new SolidBrush(Color.FromArgb(160, Color.Black)))
                            {
                                const float minorTickLen = 6f;

                                // Anti-chevauchement horizontal (en pixels)
                                float lastLabelRight = float.NegativeInfinity;

                                // Aligne le premier tick mineur sur la grille mineure
                                double firstMinor = fMin + xMinor;

                                // Petite tolérance pour tester "multiple de majeur"
                                const double eps = 1e-9;

                                for (double f = firstMinor; f <= fMax + eps; f += xMinor)
                                {
                                    if (f < fMin - eps) continue;

                                    // Skip si c'est un majeur (multiple de xMajor)
                                    double q = (f - fMin) / xMajor;
                                    if (Math.Abs(q - Math.Round(q)) < 1e-6)
                                        continue;

                                    float xPixel = (float)area.AxisX.ValueToPixelPosition(f);

                                    // 1) Tick mineur en bas (sans activer MinorTickMark du Chart)
                                    e.ChartGraphics.Graphics.DrawLine(minorPen, xPixel, bottom, xPixel, bottom + minorTickLen);

                                    // 2) Label mineur (même format que le log : Hz / kHz)
                                    string txt = FormatHzK(f);

                                    var sz = e.ChartGraphics.Graphics.MeasureString(txt, minorFont);
                                    float xText = xPixel - sz.Width / 2f;

                                    // Position du texte : 
                                    // Sous l’axe : on descend sous la ligne d'axe + tick
                                    float yText = bottom + minorTickLen + 2f;


                                    // Anti-chevauchement
                                    if (xText <= lastLabelRight + 4f)
                                        continue;

                                    // Dessin dans la zone de tracé (pas besoin de ResetClip ici)
                                    e.ChartGraphics.Graphics.DrawString(txt, minorFont, minorBrush, xText, yText);
                                    lastLabelRight = xText + sz.Width;
                                }
                            }
                        }
                    }
                }
            }
            // ---------------------------------------------------------
            // Cadre du plot : toujours visible (y compris à droite)
            // ---------------------------------------------------------
            {
                var g = e.ChartGraphics.Graphics;

                // Coordonnées du rectangle du plot en pixels
                float xLeft = (float)area.AxisX.ValueToPixelPosition(area.AxisX.Minimum);
                float xRight = (float)area.AxisX.ValueToPixelPosition(area.AxisX.Maximum);

                float yTop = (float)area.AxisY.ValueToPixelPosition(area.AxisY.Maximum);
                float yBottom = (float)area.AxisY.ValueToPixelPosition(area.AxisY.Minimum);

                // Sécurité : parfois les axes sont inversés selon config
                float x0 = Math.Min(xLeft, xRight);
                float x1 = Math.Max(xLeft, xRight);
                float y0 = Math.Min(yTop, yBottom);
                float y1 = Math.Max(yTop, yBottom);

                // On sort du clip pour ne pas perdre le bord
                var saved = g.Save();
                g.ResetClip();

                using (var pBorder = new Pen(Color.Black, 1f))
                {
                    g.DrawRectangle(pBorder, x0, y0, x1 - x0, y1 - y0);
                }

                g.Restore(saved);
            }
            try
            {
                DrawChartStatsPanel(e.ChartGraphics.Graphics, area);
            }
            catch
            {
                // sécurité : ne jamais laisser un crash UI tuer l'application
            }

        }
        private void lblRightMeas_Click(object sender, EventArgs e)
        {

        }
        //
        // ===== Stats panel (table) =====
        private enum StatEmphasis { Normal, Medium, Strong }

        private sealed class StatRow
        {
            public string Label { get; init; } = "";
            public string Value { get; init; } = "";
            public StatEmphasis Emphasis { get; init; } = StatEmphasis.Normal;

            // Nouveau : ligne de séparation visuelle
            public bool IsSeparator { get; init; } = false;
        }


        private readonly List<StatRow> _rowsLeft = new();
        private readonly List<StatRow> _rowsRight = new();

        private void RebuildStatsRowsFromLabels()
        {
            _rowsLeft.Clear();
            _rowsRight.Clear();

            BuildRowsForChannel(_rowsLeft,
                lblLeftRms?.Text,
                lblLeftPeak?.Text,
                lblLeftMeas?.Text);

            BuildRowsForChannel(_rowsRight,
                lblRightRms?.Text,
                lblRightPeak?.Text,
                lblRightMeas?.Text);
        }

        private static void BuildRowsForChannel(List<StatRow> dst, string? rms, string? peak, string? meas)
        {
            void AddLine(string? line)
            {
                if (string.IsNullOrWhiteSpace(line)) return;
                line = line.Trim();
                if (line == "—") return;

                // Marqueur de séparation (inséré par UpdateSpectrumChartAndStats)
                if (line == "----")
                {
                    dst.Add(new StatRow { IsSeparator = true });
                    return;
                }

                int idx = line.IndexOf(':');
                if (idx <= 0 || idx >= line.Length - 1) return;

                string label = line[..idx].Trim();
                string value = line[(idx + 1)..].Trim();

                if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(value))
                    return;

                var emph = StatEmphasis.Normal;
                if (label.Equals("RMS", StringComparison.OrdinalIgnoreCase) ||
                    label.Equals("F0", StringComparison.OrdinalIgnoreCase))
                    emph = StatEmphasis.Strong;
                else if (label.Equals("Peak", StringComparison.OrdinalIgnoreCase))
                    emph = StatEmphasis.Medium;

                dst.Add(new StatRow { Label = label, Value = value, Emphasis = emph });
            }

            AddLine(rms);
            AddLine(peak);

            if (!string.IsNullOrWhiteSpace(meas) && meas.Trim() != "—")
            {
                foreach (var ln in meas.Replace("\r\n", "\n").Split('\n'))
                    AddLine(ln);
            }
        }


        //
    }
}
