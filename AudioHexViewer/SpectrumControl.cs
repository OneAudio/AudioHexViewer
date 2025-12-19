using System;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
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

        private const int Max24Pos = 0x7FFFFF; // 24-bit full scale

        public enum WindowType
        {
            Rectangular,
            Hann,
            Hamming,
            BlackmanHarris
        }

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
        private double[]? avgLeftSpectrum;
        private double[]? avgRightSpectrum;

        // --- Batch averaging (vraie moyenne sur N frames) ---
        private int avgFrameCount = 0;          // combien de frames accumulées
        private double[]? avgSumLeftPower = null;   // somme des puissances (linéaire)
        private double[]? avgSumRightPower = null;

        // Moving average (glissant sur N frames)
        private int avgRingPos = 0;        // index courant dans l’anneau
        private int avgFillCount = 0;      // 0..N (sature à N pour l'affichage)

        private double[][]? avgRingLeftPower = null;   // [N][bins]
        private double[][]? avgRingRightPower = null;  // [N][bins]


        // Spectres courants & axe fréquence
        private double[]? currentLeftSpectrum;
        private double[]? currentRightSpectrum;
        private double[]? freqAxis;

        private bool isLive = false;

        // Peak-hold (en dBFS) : max par bin depuis le dernier reset
        private double[]? peakHoldLeftDb;
        private double[]? peakHoldRightDb;

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

        // État axe X logarithmique (pour redessin manuel des sous-divisions)
        private bool _xAxisIsLog = false;

        // Formatage axe Y (dBFS), divisions secondaires
        private static string FormatDbMinor(double db) => db.ToString("0");

        // Échelle des labels secondaires X et Y (95% de la taille normale)
        private const float MinorLabelScale = 0.9f;
        


        private static double NiceStep125(double rawStep)
        {
            if (rawStep <= 0 || double.IsNaN(rawStep) || double.IsInfinity(rawStep))
                return 1.0;

            double exp = Math.Floor(Math.Log10(rawStep));
            double f = rawStep / Math.Pow(10, exp); // 1..10

            double niceF;
            if (f <= 1.0) niceF = 1.0;
            else if (f <= 2.0) niceF = 2.0;
            else if (f <= 5.0) niceF = 5.0;
            else niceF = 10.0;

            return niceF * Math.Pow(10, exp);
        }

        private static double AlignToStepCeil(double value, double step)
        {
            if (step <= 0) return value;
            return Math.Ceiling(value / step) * step;
        }


        public SpectrumControl()
        {
            InitializeComponent();
            InitChartAppearance();
            InitializeUiOptions();
            UpdateRunButtonAppearance();
            InitOverlayAnnotation();

            // Clic droit sur la zone du spectre -> ouverture de la boîte d'axes
            chartSpectrum.MouseUp += ChartSpectrum_MouseUp;

            // Dessin manuel des sous-divisions log
            chartSpectrum.PostPaint += ChartSpectrum_PostPaint;
            chartSpectrum.FormatNumber += ChartSpectrum_FormatNumber;


            InitCursors();
            chartSpectrum.MouseMove += ChartSpectrum_MouseMove;
            chartSpectrum.MouseDoubleClick += ChartSpectrum_MouseDoubleClick;
            chartSpectrum.MouseLeave += (s, e) => { /* optionnel : cacher texte */ };

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
                // Axe Y (dBFS) : on garde simple
                e.LocalizedValue = $"{v:0}";
            }
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
                _overlayInfoAnno.Visible = false;
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

            _overlayInfoAnno.Text =
                $"SR: {srText}\n" +
                $"N = {N:N0}\n" +
                $"Overlap: {overlapPct:F0}%";

            // ---- Position (en % du chart) ----
            // On se base sur l'InnerPlotPosition (zone interne du graphe), en coordonnées relatives 0..100.
            var area = chartSpectrum.ChartAreas["SpectrumArea"];
            var ip = area.InnerPlotPosition; // ElementPosition

            float margin = 4.0f;    // marge en % (augmente si tu veux plus "à l'intérieur")
            float width = 250.0f;    // largeur en % (ajuste selon ton texte)
            float height = 15.0f;   // hauteur en %

            _overlayInfoAnno.Width = width;
            _overlayInfoAnno.Height = height;

            // Top-right à l'intérieur du plot
            _overlayInfoAnno.X = ip.X + ip.Width - margin - width;
            _overlayInfoAnno.Y = ip.Y + margin;

            _overlayInfoAnno.Visible = true;
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
            comboWindow.Items.Add("Blackman-Harris");
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
            if (chkRemoveDC != null)
            {
                chkRemoveDC.Checked = true; // pour garder le comportement actuel
                chkRemoveDC.CheckedChanged += (s, e) =>
                {
                    // Si live => ça sera pris en compte au prochain tick.
                    // Si pas live => on recalc tout de suite.
                    if (!isLive)
                        AcquireOnce();
                };
            }

            // Peak-hold
            if (chkPeakHold != null)
            {
                chkPeakHold.Checked = false;
                chkPeakHold.CheckedChanged += (s, e) =>
                {
                    // Quand on active : repartir de zéro pour que le peak-hold soit cohérent
                    if (chkPeakHold.Checked)
                        ResetPeakHoldState();

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
                // Re-dessine sans recalc FFT
                UpdateSpectrumChartAndStats();
            };

            // Bouton RUN
            btnRun.Click += (s, e) =>
            {
                bool once = chkOnce.Checked;

                if (once)
                {
                    StartFromRun(true);
                }
                else
                {
                    if (isLive)
                        StopFromRun();
                    else
                        StartFromRun(false);
                }
            };
        }
        //
        private void UpdateCursorVisibility()
        {
            bool onL = chkCursorL != null && chkCursorL.Checked;
            bool onR = chkCursorR != null && chkCursorR.Checked;

            if (_cursorLineL != null) _cursorLineL.Visible = onL;
            if (_cursorTextL != null) _cursorTextL.Visible = onL;

            if (_cursorLineR != null) _cursorLineR.Visible = onR;
            if (_cursorTextR != null) _cursorTextR.Visible = onR;

            chartSpectrum.Invalidate();
        }
        // ---------------- CURSORS ----------------

        private void InitCursors()
        {
            // Si pas encore de ChartArea, on ne fait rien
            if (chartSpectrum.ChartAreas.Count == 0)
                return;

            // Eviter double init
            if (_cursorLineL != null || _cursorLineR != null || _cursorTextL != null || _cursorTextR != null)
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

            _cursorTextL = new TextAnnotation
            {
                ClipToChartArea = "SpectrumArea",
                AxisX = area.AxisX,
                AxisY = area.AxisY,
                IsSizeAlwaysRelative = false,
                BackColor = Color.FromArgb(160, 230, 230, 230),
                ForeColor = Color.FromArgb(230, 30, 30, 30),
                Font = new Font("Segoe UI", 8.5f),
                Alignment = ContentAlignment.TopLeft,
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

            _cursorTextR = new TextAnnotation
            {
                ClipToChartArea = "SpectrumArea",
                AxisX = area.AxisX,
                AxisY = area.AxisY,
                IsSizeAlwaysRelative = false,
                BackColor = Color.FromArgb(160, 230, 230, 230),
                ForeColor = Color.FromArgb(230, 30, 30, 30),
                Font = new Font("Segoe UI", 8.5f),
                Alignment = ContentAlignment.TopLeft,
                Visible = false
            };

            chartSpectrum.Annotations.Add(_cursorLineL);
            chartSpectrum.Annotations.Add(_cursorTextL);
            chartSpectrum.Annotations.Add(_cursorLineR);
            chartSpectrum.Annotations.Add(_cursorTextR);

            // Appliquer l'état des checkbox au démarrage
            UpdateCursorVisibility();
        }

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
        private void RefreshCursorsFromCurrentSpectrum()
        {
            if (freqAxis == null || freqAxis.Length == 0)
                return;

            if (chartSpectrum.ChartAreas.Count == 0)
                return;

            var area = chartSpectrum.ChartAreas["SpectrumArea"];

            bool onL = (chkCursorL != null && chkCursorL.Checked);
            bool onR = (chkCursorR != null && chkCursorR.Checked);

            bool useLog = (chkLogX != null && chkLogX.Checked);
            int minIdx = useLog ? GetMinIndexForLog(freqAxis) : 0;

            // ----- Re-snap par fréquence (important quand N change) -----
            if (!double.IsNaN(_cursorFreqL))
                _cursorIdxL = FindNearestFreqIndex(freqAxis, _cursorFreqL, minIdx);

            if (!double.IsNaN(_cursorFreqR))
                _cursorIdxR = FindNearestFreqIndex(freqAxis, _cursorFreqR, minIdx);

            // clamp log
            if (_cursorIdxL >= 0 && _cursorIdxL < minIdx) _cursorIdxL = minIdx;
            if (_cursorIdxR >= 0 && _cursorIdxR < minIdx) _cursorIdxR = minIdx;

            // --- L ---
            if (onL && _cursorLineL != null && _cursorTextL != null &&
                _cursorIdxL >= minIdx && _cursorIdxL < freqAxis.Length)
            {
                double f = freqAxis[_cursorIdxL];

                // garder fréquence exacte affichée et mémorisée
                _cursorFreqL = f;

                _cursorLineL.X = f;
                _cursorLineL.Visible = true;

                string text;
                if (currentLeftSpectrum != null && _cursorIdxL < currentLeftSpectrum.Length)
                {
                    double db = currentLeftSpectrum[_cursorIdxL];
                    text = $"{f:0.##} Hz\nL: {db:0.##} dBFS" + (_cursorLockedL ? "\n(LOCK)" : "");
                }
                else
                {
                    text = $"{f:0.##} Hz\nL: --" + (_cursorLockedL ? "\n(LOCK)" : "");
                }

                _cursorTextL.Text = text;
                _cursorTextL.X = f;
                _cursorTextL.Y = area.AxisY.Maximum;
                _cursorTextL.Visible = true;
            }
            else
            {
                if (_cursorLineL != null) _cursorLineL.Visible = false;
                if (_cursorTextL != null) _cursorTextL.Visible = false;
            }

            // --- R ---
            if (onR && _cursorLineR != null && _cursorTextR != null &&
                _cursorIdxR >= minIdx && _cursorIdxR < freqAxis.Length)
            {
                double f = freqAxis[_cursorIdxR];

                _cursorFreqR = f;

                _cursorLineR.X = f;
                _cursorLineR.Visible = true;

                string text;
                if (currentRightSpectrum != null && _cursorIdxR < currentRightSpectrum.Length)
                {
                    double db = currentRightSpectrum[_cursorIdxR];
                    text = $"{f:0.##} Hz\nR: {db:0.##} dBFS" + (_cursorLockedR ? "\n(LOCK)" : "");
                }
                else
                {
                    text = $"{f:0.##} Hz\nR: --" + (_cursorLockedR ? "\n(LOCK)" : "");
                }

                _cursorTextR.Text = text;
                _cursorTextR.X = f;
                _cursorTextR.Y = area.AxisY.Maximum;
                _cursorTextR.Visible = true;
            }
            else
            {
                if (_cursorLineR != null) _cursorLineR.Visible = false;
                if (_cursorTextR != null) _cursorTextR.Visible = false;
            }

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
            avgFrameCount = 0;       // tu peux le garder, mais il ne servira plus au calcul
            avgRingPos = 0;
            avgFillCount = 0;

            avgSumLeftPower = null;
            avgSumRightPower = null;

            avgRingLeftPower = null;
            avgRingRightPower = null;

            UpdateAvgProgressUi(0, avgTargetCount);
        }
        //
        // ---------------- PEAK HOLD ----------------

        private void ResetPeakHoldState()
        {
            peakHoldLeftDb = null;
            peakHoldRightDb = null;
        }

        private void btnResetPeakHold_Click(object? sender, EventArgs e)
        {
            ResetPeakHoldState();
            UpdateSpectrumChartAndStats();
        }

        private void UpdatePeakHoldFromCurrentSpectrum()
        {
            // Peak-hold actif uniquement si la checkbox est cochée
            if (chkPeakHold == null || !chkPeakHold.Checked)
                return;

            if (currentLeftSpectrum == null)
                return;

            int bins = currentLeftSpectrum.Length;

            // (Re)alloc si N change
            if (peakHoldLeftDb == null || peakHoldLeftDb.Length != bins)
                peakHoldLeftDb = Enumerable.Repeat(-200.0, bins).ToArray();

            for (int i = 0; i < bins; i++)
            {
                double v = currentLeftSpectrum[i];
                if (v > peakHoldLeftDb[i]) peakHoldLeftDb[i] = v;
            }

            if (currentRightSpectrum != null)
            {
                int n = currentRightSpectrum.Length;

                if (peakHoldRightDb == null || peakHoldRightDb.Length != n)
                    peakHoldRightDb = Enumerable.Repeat(-200.0, n).ToArray();

                for (int i = 0; i < n; i++)
                {
                    double v = currentRightSpectrum[i];
                    if (v > peakHoldRightDb[i]) peakHoldRightDb[i] = v;
                }
            }
        }

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

            if (leftSamples.Length == 0 && rightSamples.Length == 0)
            {
                ClearSpectrum();
                return;
            }

            // 4) Construire buffers double pour chaque voie
            double[]? bufL = leftSamples.Length > 0 ? BuildChannelBuffer(leftSamples, N) : null;
            double[]? bufR = rightSamples.Length > 0 ? BuildChannelBuffer(rightSamples, N) : null;

            // Copie brute pour les stats (avant remove DC et avant fenêtre)
            double[]? bufLRaw = (bufL != null) ? (double[])bufL.Clone() : null;
            double[]? bufRRaw = (bufR != null) ? (double[])bufR.Clone() : null;


            // 5) Supprimer DC si demandé
            bool removeDc = (chkRemoveDC != null && chkRemoveDC.Checked);

            if (removeDc)
            {
                if (bufL != null)
                {
                    double mean = bufL.Average();
                    for (int i = 0; i < bufL.Length; i++)
                        bufL[i] -= mean;
                }
                if (bufR != null)
                {
                    double mean = bufR.Average();
                    for (int i = 0; i < bufR.Length; i++)
                        bufR[i] -= mean;
                }
            }

            // 6) Fenêtre + FFT par voie
            double[]? specL = null;
            double[]? specR = null;

            if (bufL != null)
            {
                double sumWinL = ApplyWindowInPlace(bufL);
                specL = ComputeSpectrumDbFs(bufL, sumWinL);
            }
            if (bufR != null)
            {
                double sumWinR = ApplyWindowInPlace(bufR);
                specR = ComputeSpectrumDbFs(bufR, sumWinR);
            }

            // Si une seule voie, on l’utilise au moins comme Left
            if (specL == null && specR != null)
                specL = (double[])specR.Clone();

            if (specL == null)
            {
                ClearSpectrum();
                return;
            }

            // 7) Moyennage
            ApplyAveraging(specL, specR);

            // Peak-hold (sur la courbe déjà calculée et moyennée)
            UpdatePeakHoldFromCurrentSpectrum();

            // 8) Axe fréquence
            RebuildFreqAxis(N);

            // On garde une copie des derniers buffers temps pour les stats
            lastTimeLeft = bufLRaw;
            lastTimeRight = bufRRaw;

            // 9) Mise à jour graphe + stats temporelles
            UpdateSpectrumChartAndStats(bufLRaw, bufRRaw);
        }

        private static double[] BuildChannelBuffer(int[] src, int N)
        {
            var buffer = new double[N];

            if (src.Length >= N)
            {
                int start = src.Length - N;
                for (int i = 0; i < N; i++)
                    buffer[i] = src[start + i];
            }
            else
            {
                int offset = N - src.Length;
                for (int i = 0; i < offset; i++)
                    buffer[i] = 0.0;

                for (int i = 0; i < src.Length; i++)
                    buffer[offset + i] = src[i];
            }

            return buffer;
        }

        private double ApplyWindowInPlace(double[] buffer)
        {
            int N = buffer.Length;
            double[] win;

            switch (lastWindow)
            {
                case WindowType.Rectangular:
                    win = Enumerable.Repeat(1.0, N).ToArray();
                    break;
                case WindowType.Hann:
                    win = Window.Hann(N);
                    break;
                case WindowType.Hamming:
                    win = Window.Hamming(N);
                    break;
                case WindowType.BlackmanHarris:
                    win = Window.BlackmanHarris(N);
                    break;
                default:
                    win = Window.Hann(N);
                    break;
            }

            double sumWin = 0.0;
            for (int i = 0; i < N; i++)
            {
                buffer[i] *= win[i];
                sumWin += win[i];
            }

            return sumWin;
        }

        private double[] ComputeSpectrumDbFs(double[] timeData, double windowSum)
        {
            int N = timeData.Length;

            Complex[] fft = new Complex[N];
            for (int i = 0; i < N; i++)
                fft[i] = new Complex(timeData[i], 0.0);

            Fourier.Forward(fft, FourierOptions.Matlab);

            int bins = N / 2 + 1;
            var spectrum = new double[bins];

            if (windowSum <= 0.0)
                windowSum = N;

            double scale = 2.0 / windowSum / Max24Pos;

            for (int k = 0; k < bins; k++)
            {
                double mag = fft[k].Magnitude;
                double amp = mag * scale;

                double db;
                if (amp <= 0.0)
                    db = -200.0;
                else
                    db = 20.0 * Math.Log10(amp);

                if (db < -200.0) db = -200.0;
                if (db > 0.0) db = 0.0;

                spectrum[k] = db;
            }

            return spectrum;
        }

        private void ApplyAveraging(double[] leftNewDb, double[]? rightNewDb)
        {
            // None => pas de moyenne
            if (avgTargetCount <= 1)
            {
                ResetAveragingState();
                currentLeftSpectrum = leftNewDb;
                currentRightSpectrum = rightNewDb;
                UpdateAvgProgressUi(0, 0);
                return;
            }

            int bins = leftNewDb.Length;

            // (Re)allocation si taille FFT change ou si N change
            if (avgSumLeftPower == null || avgSumLeftPower.Length != bins ||
                avgRingLeftPower == null || avgRingLeftPower.Length != avgTargetCount)
            {
                avgSumLeftPower = new double[bins];
                avgRingLeftPower = new double[avgTargetCount][];
                for (int i = 0; i < avgTargetCount; i++)
                    avgRingLeftPower[i] = new double[bins];

                avgRingPos = 0;
                avgFillCount = 0;
            }

            bool hasRight = (rightNewDb != null);
            if (hasRight)
            {
                if (avgSumRightPower == null || avgSumRightPower.Length != bins ||
                    avgRingRightPower == null || avgRingRightPower.Length != avgTargetCount)
                {
                    avgSumRightPower = new double[bins];
                    avgRingRightPower = new double[avgTargetCount][];
                    for (int i = 0; i < avgTargetCount; i++)
                        avgRingRightPower[i] = new double[bins];
                }
            }
            else
            {
                avgSumRightPower = null;
                avgRingRightPower = null;
            }

            // 1) Retirer la frame la plus vieille si la fenêtre est pleine (avgFillCount == N)
            if (avgFillCount == avgTargetCount)
            {
                var oldL = avgRingLeftPower![avgRingPos];
                for (int i = 0; i < bins; i++)
                    avgSumLeftPower![i] -= oldL[i];

                if (hasRight && avgRingRightPower != null && avgSumRightPower != null)
                {
                    var oldR = avgRingRightPower[avgRingPos];
                    for (int i = 0; i < bins; i++)
                        avgSumRightPower[i] -= oldR[i];
                }
            }

            // 2) Convert dB(amplitude) -> puissance linéaire (power = 10^(dB/10)) et stocker dans l’anneau
            var slotL = avgRingLeftPower![avgRingPos];
            for (int i = 0; i < bins; i++)
            {
                double db = leftNewDb[i];
                double p = (db <= -200.0) ? 0.0 : Math.Pow(10.0, db / 10.0);
                slotL[i] = p;
                avgSumLeftPower![i] += p;
            }

            if (hasRight && rightNewDb != null && avgRingRightPower != null && avgSumRightPower != null)
            {
                var slotR = avgRingRightPower[avgRingPos];
                for (int i = 0; i < bins; i++)
                {
                    double db = rightNewDb[i];
                    double p = (db <= -200.0) ? 0.0 : Math.Pow(10.0, db / 10.0);
                    slotR[i] = p;
                    avgSumRightPower[i] += p;
                }
            }

            // 3) Avancer dans l’anneau
            avgRingPos++;
            if (avgRingPos >= avgTargetCount) avgRingPos = 0;

            // 4) Compteur “warm-up” : 0..N puis saturé à N (affichage seulement)
            if (avgFillCount < avgTargetCount) avgFillCount++;
            UpdateAvgProgressUi(avgFillCount, avgTargetCount);

            // 5) Construire le spectre moyen (division par avgFillCount pendant warm-up, puis N)
            double inv = 1.0 / avgFillCount;

            var avgLeftDb = new double[bins];
            for (int i = 0; i < bins; i++)
            {
                double pAvg = avgSumLeftPower![i] * inv;
                double db = (pAvg <= 0.0) ? -200.0 : 10.0 * Math.Log10(pAvg);
                if (db < -200.0) db = -200.0;
                if (db > 0.0) db = 0.0;
                avgLeftDb[i] = db;
            }

            double[]? avgRightDb = null;
            if (hasRight && avgSumRightPower != null)
            {
                avgRightDb = new double[bins];
                for (int i = 0; i < bins; i++)
                {
                    double pAvg = avgSumRightPower[i] * inv;
                    double db = (pAvg <= 0.0) ? -200.0 : 10.0 * Math.Log10(pAvg);
                    if (db < -200.0) db = -200.0;
                    if (db > 0.0) db = 0.0;
                    avgRightDb[i] = db;
                }
            }

            currentLeftSpectrum = avgLeftDb;
            currentRightSpectrum = avgRightDb;
        }



        private void RebuildFreqAxis(int fftSize)
        {
            if (_sampleRate <= 0)
                return;

            int bins = fftSize / 2 + 1;
            freqAxis = new double[bins];
            double df = (double)_sampleRate / fftSize;

            for (int k = 0; k < bins; k++)
                freqAxis[k] = k * df;
        }

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
                dlg.FitYProvider = GetCurrentYRangeGlobal;

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

        private void UpdateSpectrumChartAndStats(double[]? timeL, double[]? timeR)
        {
            if (chartSpectrum.ClientSize.Width <= 0 || chartSpectrum.ClientSize.Height <= 0)
                return;
            if (freqAxis == null || currentLeftSpectrum == null)
                return;

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
            if (fMax > fMaxAuto) fMax = fMaxAuto;
            if (fMin >= fMax)
                fMin = fMax * 0.5;

            // --- Axe Y (dBFS) avec limites custom éventuelles ---
            double yMinAuto = -200.0;
            double yMaxAuto = 0.0;

            double yMin = customYMin ?? yMinAuto;
            double yMax = customYMax ?? yMaxAuto;

            if (yMin >= yMax)
                yMin = yMax - 10.0; // au moins 10 dB de span

            // ---------------- Grilles & divisions ----------------

            // Axe Y : 5 grandes divisions + 5 sous-divisions
            double ySpan = yMax - yMin;
            double yMajor = ySpan / 5.0;
            if (yMajor <= 0) yMajor = 10.0;

            area.AxisY.Minimum = yMin;
            area.AxisY.Maximum = yMax;

            area.AxisY.MajorGrid.Enabled = true;
            area.AxisY.MajorGrid.Interval = yMajor;
            area.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Solid;

            area.AxisY.MinorGrid.Enabled = true;
            area.AxisY.MinorGrid.Interval = yMajor / 5.0;
            area.AxisY.MinorGrid.LineDashStyle = ChartDashStyle.Dot;
            area.AxisY.MinorGrid.LineColor = Color.LightGray;

            // --- Axe X : on fixe d'abord les bornes, puis on active le log si besoin ---

            area.AxisX.Minimum = fMin;
            area.AxisX.Maximum = fMax;

            if (useLog)
            {
                // en log, il FAUT des bornes > 0, sinon exception
                if (area.AxisX.Minimum <= 0)
                    area.AxisX.Minimum = 1.0;
                if (area.AxisX.Maximum <= area.AxisX.Minimum)
                    area.AxisX.Maximum = area.AxisX.Minimum * 10.0;
                //
                // --- LOG : forcer des décades propres ---
                // Snap du MIN sur la décade inférieure (évite les labels du type 19.99, 199.99...)
                double minDecade = Math.Pow(10.0, Math.Floor(Math.Log10(area.AxisX.Minimum)));
                area.AxisX.Minimum = Math.Max(1.0, minDecade);

                // Laisse Maximum tel quel (pas besoin de le monter à la décade suivante)
                // mais assure la sécurité
                if (area.AxisX.Maximum <= area.AxisX.Minimum)
                    area.AxisX.Maximum = area.AxisX.Minimum * 10.0;
                //

                area.AxisX.IsLogarithmic = true;
                area.AxisX.LogarithmBase = 10.0;

                // 1 unité en log = 1 décade => labels/ticks uniquement sur 10^n
                area.AxisX.Interval = 1.0;
                area.AxisX.LabelStyle.Interval = 1.0;
                area.AxisX.MajorTickMark.Interval = 1.0;


                // Major = décades, Minor désactivée (sous-divisions dessinées à la main)
                area.AxisX.MajorGrid.Enabled = true;
                area.AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Solid;
                area.AxisX.MajorGrid.Interval = 1.0;      // 1 unité log = 1 décade

                area.AxisX.MinorGrid.Enabled = false;
            }
            else
            {
                area.AxisX.IsLogarithmic = false;

                double xSpan = fMax - fMin;

                // cible ~8 graduations “lisibles” (ajuste 6..10 selon ton goût)
                double raw = xSpan / 12.0;
                double xMajor = NiceStep125(raw);

                // Grille + labels calés sur xMajor
                area.AxisX.MajorGrid.Enabled = true;
                area.AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Solid;
                area.AxisX.MajorGrid.Interval = xMajor;

                area.AxisX.MinorGrid.Enabled = true;
                area.AxisX.MinorGrid.LineDashStyle = ChartDashStyle.Dot;
                area.AxisX.MinorGrid.LineColor = Color.LightGray;
                area.AxisX.MinorGrid.Interval = xMajor / 5.0;


                // (optionnel mais utile) : aligner les ticks sur un multiple de xMajor
                double first = AlignToStepCeil(fMin, xMajor);
                area.AxisX.Interval = xMajor;
                area.AxisX.IntervalOffset = first - fMin;
                area.AxisX.LabelStyle.Interval = xMajor;
                area.AxisX.LabelStyle.IntervalOffset = first - fMin;

                // --- Evite le "barre noire" : ticks mineurs trop denses ---
                area.AxisX.MinorTickMark.Enabled = false;

                // Et on force les ticks majeurs à suivre exactement xMajor
                area.AxisX.MajorTickMark.Enabled = true;
                area.AxisX.MajorTickMark.Interval = xMajor;
                area.AxisX.MajorTickMark.IntervalOffset = first - fMin;

                // Optionnel: ticks majeurs plus courts (si tu veux)
                area.AxisX.MajorTickMark.Size = 2f;




                area.AxisX.MinorGrid.LineDashStyle = ChartDashStyle.Dot;
                area.AxisX.MinorGrid.LineColor = Color.LightGray;
            }

            // --- Ce qu'on affiche en fonction du combo Channel ---
            string mode = comboChannel.SelectedItem as string ?? "L+R";
            bool showLeft = (mode == "Left" || mode == "L+R");
            bool showRight = (currentRightSpectrum != null) && (mode == "Right" || mode == "L+R");
            bool peakHoldOn = (chkPeakHold != null && chkPeakHold.Checked);

            // Courbe gauche
            if (showLeft)
            {
                var seriesL = new System.Windows.Forms.DataVisualization.Charting.Series("Left");
                seriesL.ChartType = SeriesChartType.FastLine;
                seriesL.ChartArea = "SpectrumArea";
                seriesL.Color = Color.DodgerBlue;

                for (int i = startIndex; i < freqAxis.Length && i < currentLeftSpectrum.Length; i++)
                    seriesL.Points.AddXY(freqAxis[i], currentLeftSpectrum[i]);

                chartSpectrum.Series.Add(seriesL);

                // Peak-hold gauche (pointillés)
                if (peakHoldOn && peakHoldLeftDb != null)
                {
                    var seriesLph = new ChartSeries("LeftPeakHold");
                    seriesLph.ChartType = SeriesChartType.FastLine;
                    seriesLph.ChartArea = "SpectrumArea";
                    seriesLph.Color = Color.DodgerBlue;
                    seriesLph.BorderDashStyle = ChartDashStyle.Dash;
                    seriesLph.BorderWidth = 1;

                    for (int i = startIndex; i < freqAxis.Length && i < peakHoldLeftDb.Length; i++)
                        seriesLph.Points.AddXY(freqAxis[i], peakHoldLeftDb[i]);

                    chartSpectrum.Series.Add(seriesLph);
                }


            }

            // Courbe droite
            if (showRight && currentRightSpectrum != null)
            {
                var seriesR = new System.Windows.Forms.DataVisualization.Charting.Series("Right");
                seriesR.ChartType = SeriesChartType.FastLine;
                seriesR.ChartArea = "SpectrumArea";
                seriesR.Color = Color.OrangeRed;

                for (int i = startIndex; i < freqAxis.Length && i < currentRightSpectrum.Length; i++)
                    seriesR.Points.AddXY(freqAxis[i], currentRightSpectrum[i]);

                chartSpectrum.Series.Add(seriesR);

                // Peak-hold droite (pointillés)
                if (peakHoldOn && peakHoldRightDb != null)
                {
                    var seriesRph = new ChartSeries("RightPeakHold");
                    seriesRph.ChartType = SeriesChartType.FastLine;
                    seriesRph.ChartArea = "SpectrumArea";
                    seriesRph.Color = Color.OrangeRed;
                    seriesRph.BorderDashStyle = ChartDashStyle.Dash;
                    seriesRph.BorderWidth = 1;

                    for (int i = startIndex; i < freqAxis.Length && i < peakHoldRightDb.Length; i++)
                        seriesRph.Points.AddXY(freqAxis[i], peakHoldRightDb[i]);

                    chartSpectrum.Series.Add(seriesRph);
                }
            }
            //
            //
            // --- Stats temporelles : LEFT ---
            // Peak = sur brut (avec DC) ; RMS = sans DC (AC) => on retire la moyenne pour le calcul RMS uniquement
            if (timeL != null && timeL.Length > 0)
            {
                // 1) Peak sur brut
                double peakL = 0.0;
                for (int i = 0; i < timeL.Length; i++)
                {
                    double x = timeL[i] / (double)Max24Pos;
                    double ax = Math.Abs(x);
                    if (ax > peakL) peakL = ax;
                }

                // 2) RMS AC : retirer la moyenne (DC) uniquement pour le calcul RMS
                double meanL = 0.0;
                for (int i = 0; i < timeL.Length; i++)
                    meanL += timeL[i];
                meanL /= timeL.Length;

                double sumSqL = 0.0;
                for (int i = 0; i < timeL.Length; i++)
                {
                    double x = (timeL[i] - meanL) / (double)Max24Pos;
                    sumSqL += x * x;
                }
                double rmsL = Math.Sqrt(sumSqL / timeL.Length);

                // RMS référencé "full-scale sine"
                double rmsLDb = rmsL > 0 ? 20.0 * Math.Log10(rmsL * Math.Sqrt(2.0)) : -200.0;
                // Peak référencé "full-scale"
                double peakLDb = peakL > 0 ? 20.0 * Math.Log10(peakL) : -200.0;

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
                // Peak sur brut
                double peakR = 0.0;
                for (int i = 0; i < timeR.Length; i++)
                {
                    double x = timeR[i] / (double)Max24Pos;
                    double ax = Math.Abs(x);
                    if (ax > peakR) peakR = ax;
                }

                // RMS AC (sans DC)
                double meanR = 0.0;
                for (int i = 0; i < timeR.Length; i++)
                    meanR += timeR[i];
                meanR /= timeR.Length;

                double sumSqR = 0.0;
                for (int i = 0; i < timeR.Length; i++)
                {
                    double x = (timeR[i] - meanR) / (double)Max24Pos;
                    sumSqR += x * x;
                }
                double rmsR = Math.Sqrt(sumSqR / timeR.Length);

                // RMS référencé "full-scale sine"
                double rmsRDb = rmsR > 0 ? 20.0 * Math.Log10(rmsR * Math.Sqrt(2.0)) : -200.0;
                // Peak référencé "full-scale"
                double peakRDb = peakR > 0 ? 20.0 * Math.Log10(peakR) : -200.0;

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

            RefreshCursorAfterRedraw();

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
        private void ChartSpectrum_PostPaint(object sender, ChartPaintEventArgs e)
        {
            // IMPORTANT: ne pas comparer les instances (e.ChartElement != area peut être vrai même pour la bonne zone)
            var area = e.ChartElement as ChartArea;
            if (area == null || area.Name != "SpectrumArea")
                return;

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

                        // Démarre en haut et descend => l'anti-chevauchement "lastLabelBottom" fonctionne
                        double yStart = Math.Floor(yMax / yMinor) * yMinor;

                        for (double y = yStart; y >= yMin - 1e-9; y -= yMinor)
                        {
                            // skip majeurs
                            double r = y / yMajor;
                            if (Math.Abs(r - Math.Round(r)) < 1e-9) continue;

                            float yPix = (float)area.AxisY.ValueToPixelPosition(y);

                            // Tick
                            e.ChartGraphics.Graphics.DrawLine(p, xLeftPlot - gap, yPix, xLeftPlot - gap - tickLenLeft, yPix);

                            // Texte (à gauche de l’axe)
                            string txt = FormatDbMinor(y);
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

        }


        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}
