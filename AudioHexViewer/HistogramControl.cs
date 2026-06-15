using System.Windows.Forms.DataVisualization.Charting;
using System.ComponentModel;

namespace AudioHexViewer
{
    public partial class HistogramControl : UserControl
    {
        // ══════════════════════════════════════════════════════════════════════
        // Constantes
        // ══════════════════════════════════════════════════════════════════════
        private const int MaxBins           = 16384;
        private const int MaxScaleOverBin   = 8192;
        private const int StatsWidthPx      = 200;   // même que SpectrumControl.ChartStatsWidthPx
        private const int MinPlotWidthPx    = 420;   // même que SpectrumControl.ChartStatsMinPlotWidthPx
        private bool _measPanelVisible = true;        // global Show/Hide measurement panel

        /// <summary>Shows or hides the measurement stats panel on the right.</summary>
        public void SetMeasPanelVisible(bool visible)
        {
            _measPanelVisible = visible;
            ApplyChartStatsMargin();
        }

        private static readonly int[] RangeLsbOptions =
        {
            1, 2, 4, 8, 16, 32, 64,
            128, 256, 512, 1024,
            2048, 4096, 8192,
            16384, 32768, 65536,
            131072, 262144, 524288,
            1048576, 2097152, 4194304,
            8388608
        };

        private static readonly int[] BinLsbOptions =
        {
            1, 2, 4, 8, 16, 32, 64,
            128, 256, 512, 1024,
            2048, 4096, 8192,
            16384, 32768, 65536
        };

        // ══════════════════════════════════════════════════════════════════════
        // Couleurs — synchronisées avec SpectrumColorSettings (via ApplySpectrumColorSettings)
        // ══════════════════════════════════════════════════════════════════════
        private SpectrumColorSettings _colors = SpectrumColorSettings.Defaults();
        private static Color NegApply(int argb, bool inv)
        { var col = Color.FromArgb(argb); return inv ? Color.FromArgb(col.A,255-col.R,255-col.G,255-col.B) : col; }

        // Accesseurs identiques à SpectrumControl pour faciliter la mise à jour
        private Color BackgroundColor  => NegApply(_colors.BackgroundArgb, _colors.NegativeChart);
        private Color GridMajorColor   => NegApply(_colors.GridArgb,       _colors.NegativeChart);
        private Color GridMinorColor   => NegApply(_colors.GridMinorArgb,  _colors.NegativeChart);
        private Color LabelMajorColor  => NegApply(_colors.LabelMajorArgb, _colors.NegativeChart);
        private Color LabelMinorColor  => NegApply(_colors.LabelMinorArgb, _colors.NegativeChart);
        private Color BarLeftColor     => NegApply(_colors.FftLeftArgb,  _colors.NegativeTraces);
        private Color BarRightColor    => NegApply(_colors.FftRightArgb, _colors.NegativeTraces);

        // ══════════════════════════════════════════════════════════════════════
        // État & données
        // ══════════════════════════════════════════════════════════════════════
        public static int LastUsedWindowSamples { get; private set; } = 131072;

        public event Action<bool>? RunStateChanged;
        public event Action?       RunRequested;

        private int lastRangeLsb = 1024;
        private int lastBinLsb   = 1;

        private readonly SampleProvider? _provider;
        private int  _sampleRate;
        private int  _hopSamples;
        private int? _forcedWindowSamples;

        private int[] leftSamples  = Array.Empty<int>();
        private int[] rightSamples = Array.Empty<int>();

        private System.Windows.Forms.Timer? liveTimer;
        private bool isLive;

        // ── Acquisition progress (AcqButtonPainter) ───────────────────────────
        private int    _acqPct     = 0;
        private bool   _acqVisible = false;
        private string _acqTimeStr = "";

        public void SetAcqProgress(int pct, bool visible, string timeStr)
        {
            _acqPct     = pct;
            _acqVisible = visible;
            _acqTimeStr = timeStr;
            btnRun.Invalidate();
        }

        // Stopwatch pour animer la barre pendant l'acquisition
        private readonly System.Diagnostics.Stopwatch _acqSw = new();
        private System.Windows.Forms.Timer?           _acqUiTimer;
        private double _acqPeriodSec = 0.0;

        private void StartAcqTicker(double periodSec)
        {
            _acqPeriodSec = periodSec;
            _acqSw.Restart();
            bool show = periodSec >= 1.0;
            string timeStr = show ? $"≈ {periodSec:F2} s" : $"≈ {periodSec * 1000:F0} ms";
            SetAcqProgress(0, show, timeStr);

            if (_acqUiTimer == null)
            {
                _acqUiTimer = new System.Windows.Forms.Timer { Interval = 50 };
                _acqUiTimer.Tick += (_, _) => TickAcqProgress();
            }
            _acqUiTimer.Start();
        }

        private void StopAcqTicker()
        {
            _acqUiTimer?.Stop();
            _acqSw.Reset();
            SetAcqProgress(100, false, _acqTimeStr);
        }

        private void TickAcqProgress()
        {
            if (_acqPeriodSec <= 0) return;
            double p = Math.Clamp(_acqSw.Elapsed.TotalSeconds / _acqPeriodSec, 0.0, 1.0);
            int pct = (int)Math.Round(p * 100.0);
            SetAcqProgress(pct, _acqVisible, _acqTimeStr);
            if (pct >= 100) _acqSw.Restart(); // repart pour le cycle suivant
        }
        private bool _isNormalized;   // true quand ChkNorm est coché (mode PDF)
        private bool   _isScaleLocked;
        private double _lockedYMin, _lockedYMax, _lockedYInterval;
        private bool _isDiff;          // true = vue différentielle (mesuré - gaussienne)

        // Stats pour PostPaint
        private readonly struct StatsData
        {
            public readonly int    Count;
            public readonly int    DcLsb;     // moyenne brute des samples (toujours calculée)
            public readonly double RmsLsb;
            public readonly double SigmaLsb;  // σ réel des valeurs affichées
            public readonly double MeanLsb;   // μ des valeurs affichées (≈0 en AC, offset en DC)
            public readonly double DnlMax;    // max |DNL| vs modèle gaussien (en LSB)
            public readonly double Enob;      // Effective Number Of Bits
            public readonly int    Min, Max;
            public readonly bool   HasData;

            public int PeakToPeak => HasData ? Max - Min : 0;

            public StatsData(int count, int dc, double rms, double sigma, double mean,
                             double dnlMax, int min, int max, int lsbRange)
            {
                Count    = count; DcLsb    = dc;
                RmsLsb   = rms;   SigmaLsb = sigma; MeanLsb = mean;
                DnlMax   = dnlMax;
                Min      = min;   Max      = max;
                HasData  = count > 0;
                // ENOB = log2(pleine_échelle / RMS)
                // Pleine échelle = 2^23 = 8 388 608 LSB (ADC 24-bit signé)
                // Indépendant de la plage affichée — toujours rapporté à la résolution ADC.
                const double FullScaleLsb = 8_388_608.0;
                Enob = (rms > 0)
                    ? Math.Log2(FullScaleLsb / rms)
                    : 0;
            }
        }
        private StatsData _statsLeft;
        private StatsData _statsRight;
        // Derniers bins calculés — conservés pour export CSV
        private int[]? _lastBinsLeft, _lastBinsRight;
        private int    _lastBinWidth = 1, _lastLsbRange = 256;

        public delegate (int[] Left, int[] Right) SampleProvider(int sampleCount);

        // ══════════════════════════════════════════════════════════════════════
        // Constructeurs
        // ══════════════════════════════════════════════════════════════════════
        public HistogramControl()
        {
            InitializeComponent();
            InitChart();
            InitDefaults();
            SetupLiveTimer();
            UpdateRunButtonAppearance();
        }

        public HistogramControl(SampleProvider provider, int sampleRate) : this()
        {
            _provider = provider;
            UpdateSampleRate(sampleRate);
            AcquireOnce();
        }

        // ══════════════════════════════════════════════════════════════════════
        // Couleurs — API publique (appelée par Form1 comme pour SpectrumControl)
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Applique les mêmes couleurs que le spectre FFT (fond, grilles, labels, barres L/R).
        /// À appeler depuis Form1 chaque fois que les couleurs changent (menu Colors).
        /// </summary>
        public void ApplySpectrumColorSettings(SpectrumColorSettings? colors)
        {
            _colors = colors ?? SpectrumColorSettings.Defaults();
            _colors.Normalize();
            ApplyChartTheme();
            RebuildHistogram();
        }

        public SpectrumColorSettings GetColorSnapshot() => _colors;

        private void ApplyChartTheme()
        {
            if (chartHistogram == null || chartHistogram.ChartAreas.Count == 0) return;

            var area = chartHistogram.ChartAreas["HistogramArea"];

            // Fond du contrôle Chart : SystemColors.Control (gris système, toujours stable)
            // Ne pas utiliser Parent?.BackColor — Parent peut être null pendant l'init → flash blanc
            chartHistogram.BackColor = SystemColors.Control;

            // Fond de la zone plot uniquement
            area.BackColor = BackgroundColor;

            // Grilles X/Y
            area.AxisX.MajorGrid.LineColor = GridMajorColor;
            area.AxisY.MajorGrid.LineColor = GridMajorColor;
            area.AxisX.MinorGrid.LineColor = GridMinorColor;
            area.AxisY.MinorGrid.LineColor = GridMinorColor;

            // Labels et titres d'axes
            area.AxisX.LabelStyle.ForeColor = LabelMajorColor;
            // Y labels : masqués ici, redessinés via PostPaint dans la marge grise à gauche
            // (WinForms Column chart force l'axe Y à X=0 — impossible à déplacer via Crossing)
            area.AxisY.LabelStyle.ForeColor = Color.Transparent;
            area.AxisX.TitleForeColor       = LabelMajorColor;
            area.AxisY.TitleForeColor       = LabelMajorColor;

            // Lignes d'axes + ticks
            area.AxisX.LineColor              = LabelMajorColor;
            // f) Axe Y vertical au centre (X=0) : rendu transparent
            // (sinon il masque la grille verticale quand LabelMajor est identique au fond)
            area.AxisY.LineColor              = Color.Transparent;
            area.AxisX.MajorTickMark.LineColor = LabelMajorColor;
            area.AxisY.MajorTickMark.LineColor = LabelMajorColor;
            area.AxisX.MinorTickMark.LineColor = LabelMinorColor;
            area.AxisY.MinorTickMark.LineColor = LabelMinorColor;

            // Masquer les ticks natifs de l'axe Y (positionnés au centre X=0 par Chart).
            // Les ticks personnalisés sont dessinés dans la marge gauche via DrawCustomYLabels.
            area.AxisY.MajorTickMark.Enabled = false;
            area.AxisY.MinorTickMark.Enabled = false;

            // Mise à jour des couleurs des séries déjà présentes
            foreach (var s in chartHistogram.Series)
            {
                if (s.Name == "Left")  s.Color = BarLeftColor;
                if (s.Name == "Right") s.Color = BarRightColor;
            }

            chartHistogram.Invalidate();
        }

        // ══════════════════════════════════════════════════════════════════════
        // API publique (Form1)
        // ══════════════════════════════════════════════════════════════════════
        public AudioHexViewer.HistogramSettings GetHistogramSettingsSnapshot() =>
            new AudioHexViewer.HistogramSettings
            {
                AC              = chkDcMode.Checked,
                Channel         = comboChannel.SelectedItem as string ?? "Both",
                RangeLsb        = GetRangeLsb(),
                BinLsb          = GetBinLsb(),
                RecordSamples   = LastUsedWindowSamples,
                SplitterDistance = 0
            };

        public void ApplyHistogramSettings(AudioHexViewer.HistogramSettings s)
        {
            if (s == null) return;
            chkDcMode.Checked = s.AC;
            int ci = comboChannel.Items.IndexOf(s.Channel);
            if (ci >= 0) comboChannel.SelectedIndex = ci;
            int ri = comboRangeLsb.Items.IndexOf(s.RangeLsb.ToString());
            if (ri >= 0) comboRangeLsb.SelectedIndex = ri;
            int bi = comboBinLsb.Items.IndexOf(s.BinLsb.ToString());
            if (bi >= 0) comboBinLsb.SelectedIndex = bi;
            RebuildHistogram();
        }

        public void UpdateSampleRate(int sampleRate)
        {
            if (sampleRate <= 0) return;
            _sampleRate = sampleRate;
            UpdateWindowInfo();
            if (liveTimer != null)
            {
                int step = _hopSamples > 0 ? _hopSamples : GetWindowSamples();
                liveTimer.Interval = (int)Math.Max(10, Math.Round(step * 1000.0 / _sampleRate));
            }
        }

        public void UpdateHopSamples(int hopSamples)
        {
            if (hopSamples < 0) hopSamples = 0;
            _hopSamples = hopSamples;
            if (liveTimer != null && _sampleRate > 0)
            {
                int step = _hopSamples > 0 ? _hopSamples : GetWindowSamples();
                liveTimer.Interval = (int)Math.Max(10, Math.Round(step * 1000.0 / _sampleRate));
                if (isLive)
                {
                    double periodSec = step / (double)_sampleRate;
                    StartAcqTicker(periodSec);
                }
            }
        }

        public void SetGlobalSampleSize(int samples)
        {
            if (samples <= 0) return;
            _forcedWindowSamples = samples;
            UpdateWindowInfo();
            if (isLive && _sampleRate > 0)
            {
                int step = _hopSamples > 0 ? _hopSamples : GetWindowSamples();
                StartAcqTicker(step / (double)_sampleRate);
            }
        }

        public void StartFromRun(bool onceMode)
        {
            if (_provider == null || _sampleRate <= 0) return;
            if (onceMode)
            {
                AcquireOnce();
                isLive = false;
                liveTimer?.Stop();
                UpdateRunButtonAppearance();
                StopAcqTicker();
            }
            else
            {
                int step = _hopSamples > 0 ? _hopSamples : GetWindowSamples();
                double periodSec = step / (double)_sampleRate;
                if (liveTimer != null)
                    liveTimer.Interval = (int)Math.Max(10, Math.Round(step * 1000.0 / _sampleRate));
                isLive = true;
                AcquireOnce();
                liveTimer?.Start();
                UpdateRunButtonAppearance();
                StartAcqTicker(periodSec);
            }
        }

        public void StopFromRun()
        {
            isLive = false;
            liveTimer?.Stop();
            UpdateRunButtonAppearance();
            StopAcqTicker();
        }

        // ══════════════════════════════════════════════════════════════════════
        // Initialisation
        // ══════════════════════════════════════════════════════════════════════
        private void InitChart()
        {
            chartHistogram.ChartAreas.Clear();

            var area = new ChartArea("HistogramArea");

            // Fond : le BackColor du contrôle Chart est celui du parent (gris système)
            // seul area.BackColor prend la couleur du thème (appliquée par ApplyChartTheme)
            chartHistogram.BackColor = SystemColors.Control;
            area.BackColor           = BackgroundColor;
            area.BorderWidth         = 0;

            // Axes
            area.AxisX.Title                    = "Code (LSB, centered)";
            area.AxisY.Title                    = ""; // drawn via PostPaint in the gray margin
            area.AxisX.MajorGrid.LineDashStyle  = ChartDashStyle.Dot;
            area.AxisY.MajorGrid.LineDashStyle  = ChartDashStyle.Dot;
            area.AxisX.MinorGrid.Enabled        = false;
            area.AxisY.MinorGrid.Enabled        = false;
            area.AxisX.Crossing                 = 0.0;
            area.AxisY.IsStartedFromZero        = true;

            chartHistogram.ChartAreas.Add(area);

            // Position de la ChartArea : réserve ~200 px à droite pour les stats
            // (recalculée dynamiquement à chaque resize)
            ApplyChartStatsMargin();
            chartHistogram.Resize += (_, __) => ApplyChartStatsMargin();

            // Thème couleurs par défaut
            ApplyChartTheme();

            // PostPaint pour le panneau stats (style identique SpectrumControl)
            chartHistogram.PostPaint += ChartHistogram_PostPaint;
        }

        private void InitDefaults()
        {
            comboRangeLsb.Items.Clear();
            foreach (var v in RangeLsbOptions) comboRangeLsb.Items.Add(v.ToString());

            comboBinLsb.Items.Clear();
            foreach (var v in BinLsbOptions) comboBinLsb.Items.Add(v.ToString());

            comboChannel.SelectedItem = "Both";
            chkDcMode.Checked = true;

            comboRangeLsb.SelectedIndex = FindClosestIndex(RangeLsbOptions, 4194304);
            comboBinLsb.SelectedIndex   = FindClosestIndex(BinLsbOptions, 32768);
            lastRangeLsb = GetRangeLsb();
            lastBinLsb   = GetBinLsb();
            LastUsedWindowSamples = 131072;

            UpdateWindowInfo();

            // Owner-draw btnRun : T ACQ + barre + RUN/STOP (AcqButtonPainter partagé)
            btnRun.Paint += (s, e) =>
                AcqButtonPainter.Paint(e.Graphics, (Button)s!, _acqPct, _acqVisible, _acqTimeStr);

            btnRun.Click += (s, e) =>
            {
                if (!isLive)
                {
                    RunRequested?.Invoke();
                    StartFromRun(false);
                    // Ne signaler "running" à Form1 que si le démarrage a réellement eu lieu
                    if (isLive)
                        RunStateChanged?.Invoke(true);
                }
                else
                {
                    StopFromRun();
                    RunStateChanged?.Invoke(false);
                }
            };
        }

        private void SetupLiveTimer()
        {
            liveTimer           = new System.Windows.Forms.Timer();
            liveTimer.Tick     += LiveTimer_Tick;
            liveTimer.Interval  = _sampleRate > 0
                ? (int)Math.Max(10, Math.Round(GetWindowSamples() * 1000.0 / _sampleRate))
                : 1000;
        }

        // ══════════════════════════════════════════════════════════════════════
        // Proportions : marge droite pour le panneau stats (= SpectrumControl)
        // ══════════════════════════════════════════════════════════════════════
        private void ApplyChartStatsMargin()
        {
            if (chartHistogram == null || chartHistogram.ChartAreas.Count == 0) return;
            int w = chartHistogram.ClientSize.Width;
            if (w <= 0) return;
            ApplyChartStatsMarginToChart(chartHistogram, w, scale: _measPanelVisible ? 1 : 0);
            chartHistogram.Invalidate();
        }

        private static void ApplyChartStatsMarginToChart(
            System.Windows.Forms.DataVisualization.Charting.Chart chart, int chartWidth, int scale)
        {
            if (chart.ChartAreas.Count == 0) return;
            var area = chart.ChartAreas.FindByName("HistogramArea") ?? chart.ChartAreas[0];

            int reserved    = StatsWidthPx * scale;
            int maxReserved = Math.Max(120 * scale, chartWidth - MinPlotWidthPx * scale);
            reserved = Math.Min(reserved, maxReserved);

            float reservedPct = Math.Max(0f, Math.Min(45f, 100f * reserved / chartWidth));
            const float LeftPct = 5.5f;  // réduit avec labels en k
            float rightMargin = (scale == 0) ? 1f : 0f;  // small margin when panel hidden
            float width = (100f - LeftPct) - reservedPct - rightMargin;
            if (width < 10f) width = 10f;
            area.Position.Auto = false;
            area.Position = new ElementPosition(LeftPct, 3f, width, 94f);

            // InnerPlotPosition : utiliser quasi toute la ChartArea (supprime marge droite auto)
            area.InnerPlotPosition.Auto   = false;
            area.InnerPlotPosition.X      = 1f;
            area.InnerPlotPosition.Y      = 3f;
            area.InnerPlotPosition.Width  = 98f;
            area.InnerPlotPosition.Height = 91f;
        }

        // ══════════════════════════════════════════════════════════════════════
        // RUN / STOP
        // ══════════════════════════════════════════════════════════════════════
        private void UpdateRunButtonAppearance()
        {
            if (btnRun == null) return;
            if (isLive)
            {
                btnRun.Text      = "■  STOP";
                btnRun.BackColor = Color.Firebrick;
                btnRun.ForeColor = Color.White;
            }
            else
            {
                btnRun.Text      = "▶  RUN";
                btnRun.BackColor = Color.SeaGreen;
                btnRun.ForeColor = Color.White;
            }
        }

        private void LiveTimer_Tick(object? sender, EventArgs e)
        {
            if (IsDisposed || !IsHandleCreated) { liveTimer?.Stop(); return; }
            try { AcquireOnce(); }
            catch (Exception ex)
            {
                liveTimer?.Stop(); isLive = false;
                MessageBox.Show("Erreur dans le mode LIVE : " + ex.Message, "Histogram",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AcquireOnce()
        {
            if (_provider == null || _sampleRate <= 0) return;
            var (left, right) = _provider(GetWindowSamples());
            leftSamples  = left  ?? Array.Empty<int>();
            rightSamples = right ?? Array.Empty<int>();
            RebuildHistogram();
        }

        // ══════════════════════════════════════════════════════════════════════
        // Helpers
        // ══════════════════════════════════════════════════════════════════════
        private int FindClosestIndex(int[] options, int target)
        {
            int best = 0, bestDiff = int.MaxValue;
            for (int i = 0; i < options.Length; i++)
            {
                int d = Math.Abs(options[i] - target);
                if (d < bestDiff) { bestDiff = d; best = i; }
            }
            return best;
        }

        private int GetRangeLsb() =>
            comboRangeLsb.SelectedIndex >= 0 ? RangeLsbOptions[comboRangeLsb.SelectedIndex] : RangeLsbOptions[0];

        private int GetBinLsb() =>
            comboBinLsb.SelectedIndex >= 0 ? BinLsbOptions[comboBinLsb.SelectedIndex] : BinLsbOptions[0];

        private int GetWindowSamples()
        {
            if (_forcedWindowSamples.HasValue && _forcedWindowSamples.Value > 0)
            {
                LastUsedWindowSamples = _forcedWindowSamples.Value;
                return _forcedWindowSamples.Value;
            }
            return LastUsedWindowSamples > 0 ? LastUsedWindowSamples : (LastUsedWindowSamples = 131072);
        }

        private void UpdateWindowInfo()
        {
            // lblWindowInfo supprimé (durée déjà affichée sur l'onglet FFT)
        }

        // ══════════════════════════════════════════════════════════════════════
        // Événements contrôles
        // ══════════════════════════════════════════════════════════════════════
        private void ComboChannel_SelectedIndexChanged(object sender, EventArgs e) => RebuildHistogram();
        private void ChkDcMode_CheckedChanged(object sender, EventArgs e)             => RebuildHistogram();
        private void ChkLockScale_CheckedChanged(object sender, EventArgs e)
        {
            _isScaleLocked = chkLockScale.Checked;
            if (_isScaleLocked && chartHistogram.ChartAreas.Count > 0)
            {
                // Capture l'échelle actuelle au moment du verrouillage
                var axY = (chartHistogram.ChartAreas.FindByName("HistogramArea") ?? chartHistogram.ChartAreas[0]).AxisY;
                _lockedYMin      = axY.Minimum;
                _lockedYMax      = axY.Maximum;
                _lockedYInterval = axY.Interval;
            }
        }

        private void ChkGauss_CheckedChanged(object sender, EventArgs e)          => RebuildHistogram();
        private void ChkNorm_CheckedChanged(object sender, EventArgs e)           => RebuildHistogram();
        // Marqueurs σ : pas de recalcul des données, simple rafraîchissement du PostPaint
        private void ChkMarkers_CheckedChanged(object sender, EventArgs e)        => chartHistogram.Invalidate();
        private void ChkDiff_CheckedChanged(object sender, EventArgs e)
        {
            _isDiff = chkDiff.Checked;
            RebuildHistogram();
        }
        private void ComboSigma_SelectedIndexChanged(object sender, EventArgs e)  => chartHistogram.Invalidate();

        private void ComboRangeLsb_SelectedIndexChanged(object sender, EventArgs e)
        { lastRangeLsb = GetRangeLsb(); RebuildHistogram(); }

        private void ComboBinLsb_SelectedIndexChanged(object sender, EventArgs e)
        { lastBinLsb = GetBinLsb(); RebuildHistogram(); }

        // ══════════════════════════════════════════════════════════════════════
        // Reconstruction histogramme
        // ══════════════════════════════════════════════════════════════════════
        private void RebuildHistogram()
        {
            if (chartHistogram.ClientSize.Width <= 0 || chartHistogram.ClientSize.Height <= 0) return;

            if (leftSamples.Length == 0 && rightSamples.Length == 0)
            {
                chartHistogram.Series.Clear();
                _statsLeft = _statsRight = default;
                chartHistogram.Invalidate();
                return;
            }

            string mode = comboChannel.SelectedItem as string ?? "Both";
            bool ac = !chkDcMode.Checked;

            int lsbRange = Math.Max(1, GetRangeLsb());
            int binWidth = Math.Max(1, GetBinLsb());

            bool wantLeft  = mode is "Left"  or "Both";
            bool wantRight = mode is "Right" or "Both";

            // DC (moyenne brute) — toujours calculé depuis les samples bruts pour un affichage
            // correct que la case "Mode AC" soit cochée ou non.
            int dcL = 0, dcR = 0;
            if (wantLeft  && leftSamples.Length  > 0) { long s = 0; foreach (int v in leftSamples)  s += v; dcL = (int)Math.Round(s / (double)leftSamples.Length); }
            if (wantRight && rightSamples.Length > 0) { long s = 0; foreach (int v in rightSamples) s += v; dcR = (int)Math.Round(s / (double)rightSamples.Length); }

            // Adaptation binWidth si portée trop grande
            if (lsbRange > 1024)
            {
                while (lsbRange > MaxScaleOverBin * binWidth && binWidth < BinLsbOptions[^1])
                    binWidth <<= 1;
                int ni = FindClosestIndex(BinLsbOptions, binWidth);
                binWidth = BinLsbOptions[ni];
                comboBinLsb.SelectedIndex = ni;
                lastBinLsb = binWidth;
            }

            int binCount = Math.Min(MaxBins, (int)Math.Ceiling((2 * lsbRange + 1) / (double)binWidth));
            if (binCount <= 0) { chartHistogram.Series.Clear(); return; }

            int[]? binsLeft  = wantLeft  ? new int[binCount] : null;
            int[]? binsRight = wantRight ? new int[binCount] : null;

            int lCnt = 0; long lSum = 0, lSumSq = 0; int lMin = int.MaxValue, lMax = int.MinValue;
            int rCnt = 0; long rSum = 0, rSumSq = 0; int rMin = int.MaxValue, rMax = int.MinValue;

            if (binsLeft != null)
                foreach (int v in leftSamples)
                {
                    int va = v - (ac ? dcL : 0);
                    if (va < -lsbRange || va > lsbRange) continue;
                    int idx = (va + lsbRange) / binWidth;
                    if ((uint)idx < (uint)binCount)
                    {
                        binsLeft[idx]++; lCnt++; lSum += va; lSumSq += (long)va * va;
                        if (va < lMin) lMin = va; if (va > lMax) lMax = va;
                    }
                }

            if (binsRight != null)
                foreach (int v in rightSamples)
                {
                    int va = v - (ac ? dcR : 0);
                    if (va < -lsbRange || va > lsbRange) continue;
                    int idx = (va + lsbRange) / binWidth;
                    if ((uint)idx < (uint)binCount)
                    {
                        binsRight[idx]++; rCnt++; rSum += va; rSumSq += (long)va * va;
                        if (va < rMin) rMin = va; if (va > rMax) rMax = va;
                    }
                }

            // ── Calcul stats LEFT ────────────────────────────────────────────
            double lMean  = lCnt > 0 ? (double)lSum / lCnt : 0.0;
            double lRms   = lCnt > 0 ? Math.Sqrt(lSumSq / (double)lCnt) : 0.0;
            double lSigma = lCnt > 0 ? Math.Sqrt(Math.Max(0.0, lSumSq / (double)lCnt - lMean * lMean)) : 0.0;

            // ── Calcul stats RIGHT ───────────────────────────────────────────
            double rMean  = rCnt > 0 ? (double)rSum / rCnt : 0.0;
            double rRms   = rCnt > 0 ? Math.Sqrt(rSumSq / (double)rCnt) : 0.0;
            double rSigma = rCnt > 0 ? Math.Sqrt(Math.Max(0.0, rSumSq / (double)rCnt - rMean * rMean)) : 0.0;

            // ── DNL (Differential Non-Linearity) vs modèle gaussien ──────────
            // DNL(i) = |count(i)/expected(i) - 1|  → max sur les bins significatifs
            // expected(i) calculé depuis G(μ, σ) mesuré. Valable en mode binWidth=1 (code par code)
            // ou approximatif pour binWidth>1 (non-linéarité moyennée sur plusieurs codes).
            static double ComputeDnlMax(int[]? bins, int n, double mu, double sigma,
                                        int range, int bw)
            {
                if (bins == null || n <= 0 || sigma < 1e-6) return 0.0;
                int    bc        = bins.Length;
                double scale     = (double)n * bw / (sigma * Math.Sqrt(2.0 * Math.PI));
                double inv2s2    = 0.5 / (sigma * sigma);
                double threshold = Math.Max(3.0, n * 0.0005); // au moins 3 samples ou 0.05 %
                double maxDnl    = 0.0;
                for (int i = 0; i < bc; i++)
                {
                    double xi       = -range + (i + 0.5) * bw;
                    double expected = scale * Math.Exp(-(xi - mu) * (xi - mu) * inv2s2);
                    if (expected < threshold) continue;
                    double dnl = Math.Abs(bins[i] / expected - 1.0);
                    if (dnl > maxDnl) maxDnl = dnl;
                }
                return maxDnl;
            }

            double lDnl = ComputeDnlMax(binsLeft,  lCnt, lMean, lSigma, lsbRange, binWidth);
            double rDnl = ComputeDnlMax(binsRight, rCnt, rMean, rSigma, lsbRange, binWidth);
            // Stocker pour export CSV
            _lastBinsLeft  = binsLeft  != null ? (int[])binsLeft.Clone()  : null;
            _lastBinsRight = binsRight != null ? (int[])binsRight.Clone() : null;
            _lastBinWidth  = binWidth;
            _lastLsbRange  = lsbRange;

            _statsLeft  = lCnt > 0
                ? new StatsData(lCnt, dcL, lRms, lSigma, lMean, lDnl, lMin, lMax, lsbRange)
                : new StatsData(0, dcL, 0, 0, 0, 0, 0, 0, 0);
            _statsRight = rCnt > 0
                ? new StatsData(rCnt, dcR, rRms, rSigma, rMean, rDnl, rMin, rMax, lsbRange)
                : new StatsData(0, dcR, 0, 0, 0, 0, 0, 0, 0);

            // ── Normalisation PDF ───────────────────────────────────────────
            // Quand ChkNorm est coché, chaque bin est divisé par (N·binWidth) → densité en 1/LSB.
            bool isNorm = ChkNorm.Checked;
            _isNormalized = isNorm;

            // Séries — couleurs issues de SpectrumColorSettings
            chartHistogram.Series.Clear();

            // ── Helper : vue différentielle (mesuré − gaussienne idéale) ─────
            double DiffY(int count, double n, double mu, double sigma, int binIdx, double normF)
            {
                double x      = -lsbRange + binIdx * binWidth + binWidth / 2.0;
                double scale  = isNorm
                    ? 1.0 / (sigma * Math.Sqrt(2.0 * Math.PI))
                    : n * binWidth / (sigma * Math.Sqrt(2.0 * Math.PI));
                double gauss  = scale * Math.Exp(-((x - mu) * (x - mu)) / (2.0 * sigma * sigma));
                return count * normF - gauss;
            }

            if (binsLeft != null)
            {
                double normF = isNorm && lCnt > 0 ? 1.0 / ((double)lCnt * binWidth) : 1.0;
                var s = new Series("Left") { ChartType = SeriesChartType.Column, ChartArea = "HistogramArea", Color = BarLeftColor };
                for (int i = 0; i < binCount; i++)
                {
                    double y = _isDiff && _statsLeft.SigmaLsb > 1e-6
                        ? DiffY(binsLeft[i], lCnt, _statsLeft.MeanLsb, _statsLeft.SigmaLsb, i, normF)
                        : binsLeft[i] * normF;
                    s.Points.AddXY(-lsbRange + i * binWidth + binWidth / 2, y);
                }
                chartHistogram.Series.Add(s);
            }
            if (binsRight != null)
            {
                double normF = isNorm && rCnt > 0 ? 1.0 / ((double)rCnt * binWidth) : 1.0;
                var s = new Series("Right") { ChartType = SeriesChartType.Column, ChartArea = "HistogramArea", Color = BarRightColor };
                for (int i = 0; i < binCount; i++)
                {
                    double y = _isDiff && _statsRight.SigmaLsb > 1e-6
                        ? DiffY(binsRight[i], rCnt, _statsRight.MeanLsb, _statsRight.SigmaLsb, i, normF)
                        : binsRight[i] * normF;
                    s.Points.AddXY(-lsbRange + i * binWidth + binWidth / 2, y);
                }
                chartHistogram.Series.Add(s);
            }

            // ── Série Gaussienne G(μ, σ) en overlay ─────────────────────────
            // Dessinée APRÈS les barres pour passer au premier plan.
            // Formule : y = (N·bw / (σ·√(2π))) · exp(−(x−μ)²/(2σ²))
            // N = count en fenêtre, bw = binWidth → le pic correspond au count max attendu.
            if (ChkGauss.Checked && !_isDiff)
            {
                void AddGaussSeries(string name, StatsData d, Color baseColor)
                {
                    if (!d.HasData || d.SigmaLsb < 1e-6) return;
                    double mu     = d.MeanLsb;
                    double sigma  = d.SigmaLsb;
                    // En mode normalisé (PDF) : scale = 1/(σ√2π).  Sinon : N·bw/(σ√2π).
                    double scale  = isNorm
                        ? 1.0 / (sigma * Math.Sqrt(2.0 * Math.PI))
                        : d.Count * binWidth / (sigma * Math.Sqrt(2.0 * Math.PI));
                    double inv2s2 = 1.0 / (2.0 * sigma * sigma);

                    // Points calculés sur une grille 4× plus fine que les bins → courbe lisse
                    int    subDiv = Math.Max(1, binWidth / 4);
                    double step   = Math.Max(1.0, binWidth / (double)Math.Max(1, binWidth / subDiv));
                    // Mais on se limite à 2 048 points pour rester rapide
                    int nPts = Math.Min(2048, (int)Math.Ceiling(2.0 * lsbRange / step) + 1);
                    step = 2.0 * lsbRange / Math.Max(1, nPts - 1);

                    var gs = new Series(name)
                    {
                        ChartType      = SeriesChartType.Line,
                        ChartArea      = "HistogramArea",
                        Color          = Color.FromArgb(220, baseColor),
                        BorderWidth    = 2,
                        BorderDashStyle = ChartDashStyle.Solid,
                        IsVisibleInLegend = false
                    };
                    for (int k = 0; k < nPts; k++)
                    {
                        double x = -lsbRange + k * step;
                        double y = scale * Math.Exp(-((x - mu) * (x - mu)) * inv2s2);
                        gs.Points.AddXY(x, y);
                    }
                    chartHistogram.Series.Add(gs);
                }

                if (binsLeft  != null) AddGaussSeries("GaussLeft",  _statsLeft,  BarLeftColor);
                if (binsRight != null) AddGaussSeries("GaussRight", _statsRight, BarRightColor);
            }

            // Axe X
            var area = chartHistogram.ChartAreas["HistogramArea"];
            area.AxisX.Minimum  = -lsbRange;
            area.AxisX.Maximum  =  lsbRange;
            area.AxisX.Crossing = 0.0;  // ligne X à count=0 (bas du graphique)
            double xStep = lsbRange / 8.0;
            area.AxisX.Interval = xStep <= 0 ? 1 : xStep;

            // Axe Y — priorité : scale verrouillée → diff → auto
            if (_isScaleLocked && _lockedYMax > _lockedYMin)
            {
                area.AxisY.Minimum  = _lockedYMin;
                area.AxisY.Maximum  = _lockedYMax;
                area.AxisY.Interval = _lockedYInterval;
                area.AxisY.IsStartedFromZero = (_lockedYMin >= 0);
            }
            else if (_isDiff)
            {
                double yMinD = 0, yMaxD = 0;
                foreach (var ser in chartHistogram.Series)
                    foreach (var pt in ser.Points)
                    { if (pt.YValues[0] < yMinD) yMinD = pt.YValues[0];
                      if (pt.YValues[0] > yMaxD) yMaxD = pt.YValues[0]; }
                double absMax = Math.Max(Math.Abs(yMinD), yMaxD);
                var (ySymMax, ySymInterval) = ComputeNiceYScale(absMax > 0 ? absMax : 1.0);
                area.AxisY.Minimum  = -ySymMax;
                area.AxisY.Maximum  =  ySymMax;
                area.AxisY.Interval = ySymInterval;
                area.AxisY.IsStartedFromZero = false;
            }
            else
            {
                double maxYValue = 0;
                if (binsLeft  != null) { double nf = isNorm && lCnt > 0 ? 1.0 / ((double)lCnt * binWidth) : 1.0; foreach (int c in binsLeft)  { double v = c * nf; if (v > maxYValue) maxYValue = v; } }
                if (binsRight != null) { double nf = isNorm && rCnt > 0 ? 1.0 / ((double)rCnt * binWidth) : 1.0; foreach (int c in binsRight) { double v = c * nf; if (v > maxYValue) maxYValue = v; } }
                var (yMax, yInterval) = ComputeNiceYScale(maxYValue);
                area.AxisY.Minimum  = 0;
                area.AxisY.Maximum  = yMax;
                area.AxisY.Interval = yInterval;
                area.AxisY.IsStartedFromZero = true;
            }

            area.RecalculateAxesScale();
            chartHistogram.Invalidate();
        }

        // ══════════════════════════════════════════════════════════════════════
        // PostPaint — panneau stats (style identique à SpectrumControl)
        // ══════════════════════════════════════════════════════════════════════
        private void ChartHistogram_PostPaint(object? sender, ChartPaintEventArgs e)
        {
            var chart = sender as System.Windows.Forms.DataVisualization.Charting.Chart ?? chartHistogram;
            if (chart.ChartAreas.Count == 0) return;
            var area = chart.ChartAreas.FindByName("HistogramArea") ?? chart.ChartAreas[0];
            var g    = e.ChartGraphics.Graphics;
            var cr   = chart.ClientRectangle;
            float sExport = (chartHistogram.Width > 0) ? (float)chart.Width / chartHistogram.Width : 1f;
            // ── Labels Y custom dans la marge grise à gauche ─────────────────
            DrawCustomYLabels(g, area, cr, sExport, chart.Font);

            if (!_measPanelVisible) return;

            // Rectangle du panneau stats aligné sur le plot réel (comme DrawChartStatsPanel de SpectrumControl)
            var pos      = area.Position;
            float aRight = cr.Width  * (pos.X + pos.Width)  / 100f;
            float pLeft  = aRight + 4f * sExport;
            float pRight = cr.Right - 4f * sExport;
            float pW     = pRight - pLeft;

            if (pW < 80 * sExport) return;

            // Bornes VERTICALES = inner plot réel (ValueToPixelPosition)
            float pTop, pBot;
            try
            {
                pTop = (float)area.AxisY.ValueToPixelPosition(area.AxisY.Maximum);
                pBot = (float)area.AxisY.ValueToPixelPosition(area.AxisY.Minimum);
            }
            catch
            {
                // Fallback si axes pas encore calculés
                pTop = cr.Height * pos.Y               / 100f;
                pBot = cr.Height * (pos.Y + pos.Height)/ 100f;
            }
            if (pBot <= pTop) return;

            var panelRect = RectangleF.FromLTRB(pLeft, pTop, pRight, pBot);

            // ── Marqueurs ±nσ sur le plot ────────────────────────────────────
            if (ChkMarkers.Checked)
            {
                int nSigma = Math.Max(1, Math.Min(3, comboSigma.SelectedIndex + 1));
                string chanMode = comboChannel.SelectedItem as string ?? "Both";

                float innerLeft, innerRight;
                try
                {
                    innerLeft  = (float)area.AxisX.ValueToPixelPosition(area.AxisX.Minimum);
                    innerRight = (float)area.AxisX.ValueToPixelPosition(area.AxisX.Maximum);
                }
                catch { innerLeft = 0; innerRight = aRight; }

                bool doLeft  = _statsLeft.HasData  && chanMode is "Left"  or "Both";
                bool doRight = _statsRight.HasData && chanMode is "Right" or "Both";
                bool both    = doLeft && doRight;

                // chart.Font est déjà ×sExport (ApplyExportScaling)
                using var markerFont = new Font(
                    chart.Font ?? SystemFonts.DefaultFont, FontStyle.Regular);

                void DrawSigmaMarkers(StatsData d, Color col, string pfx)
                {
                    if (!d.HasData || d.SigmaLsb < 1e-6) return;
                    for (int n = 1; n <= nSigma; n++)
                    {
                        int alpha = Math.Max(70, 220 - (n - 1) * 65); // 1σ=220, 2σ=155, 3σ=90
                        using var pen = new Pen(Color.FromArgb(alpha, col), Math.Max(1f, sExport))
                        {
                            DashStyle = System.Drawing.Drawing2D.DashStyle.Dash
                        };
                        using var lblBrush = new SolidBrush(Color.FromArgb(alpha, col));

                        foreach (int sign in new[] { -1, 1 })
                        {
                            double xVal = d.MeanLsb + sign * n * d.SigmaLsb;
                            float  xPx;
                            try { xPx = (float)area.AxisX.ValueToPixelPosition(xVal); }
                            catch { continue; }
                            if (xPx < innerLeft - 1 || xPx > innerRight + 1) continue;

                            g.DrawLine(pen, xPx, pTop, xPx, pBot);

                            string lbl = both ? $"{pfx}{n}σ" : $"{n}σ";
                            SizeF  sz  = g.MeasureString(lbl, markerFont);
                            float  lx  = sign > 0 ? xPx + 2f * sExport : xPx - sz.Width - 2f * sExport;
                            lx = Math.Max(innerLeft + 1f, Math.Min(innerRight - sz.Width - 1f, lx));
                            g.DrawString(lbl, markerFont, lblBrush, lx, pTop + 2f * sExport);
                        }
                    }
                }

                if (doLeft)  DrawSigmaMarkers(_statsLeft,  BarLeftColor,  "L");
                if (doRight) DrawSigmaMarkers(_statsRight, BarRightColor, "R");
            }

            // Fond blanc + bordure gris clair — identique à SpectrumControl.DrawChartStatsPanel
            using (var bg = new SolidBrush(Color.White))
                g.FillRectangle(bg, panelRect);
            using (var border = new Pen(Color.LightGray, Math.Max(1f, sExport)))
                g.DrawRectangle(border, panelRect.X, panelRect.Y, panelRect.Width, panelRect.Height);

            // Polices & brosses — chart.Font est déjà ×scale (ApplyExportScaling l'a multiplié)
            var baseFont = chart.Font ?? SystemFonts.DefaultFont;
            float bPt    = baseFont.SizeInPoints;
            using var fntHdr   = new Font(baseFont.FontFamily, bPt,        FontStyle.Bold);
            using var fntNorm  = new Font(baseFont.FontFamily, bPt,        FontStyle.Regular);
            using var fntBold  = new Font(baseFont.FontFamily, bPt,        FontStyle.Bold);
            using var fntStrg  = new Font(baseFont.FontFamily, bPt + 1.5f, FontStyle.Bold);
            using var bText    = new SolidBrush(Color.DimGray);
            using var bLabel   = new SolidBrush(Color.DimGray);
            using var penSep   = new Pen(Color.Gainsboro, Math.Max(1f, sExport));
            using var fmtCtr   = new StringFormat { Alignment = StringAlignment.Center,  LineAlignment = StringAlignment.Near };
            using var fmtRight = new StringFormat { Alignment = StringAlignment.Far,     LineAlignment = StringAlignment.Near };

            float pad  = 8f * sExport;
            float xL   = panelRect.X + pad;
            float xR   = panelRect.Right - pad;
            float y    = panelRect.Y + pad;
            float rowH = fntNorm.GetHeight(g) + 4f * sExport;

            void DrawSection(string title, Color titleColor, StatsData d)
            {
                // En-tête coloré centré (DodgerBlue pour LEFT, OrangeRed pour RIGHT)
                using var bTitle = new SolidBrush(titleColor);
                var hdr = new RectangleF(xL, y, pW - 2 * pad, fntHdr.GetHeight(g));
                g.DrawString(title, fntHdr, bTitle, hdr, fmtCtr);
                y += hdr.Height + 4f;
                g.DrawLine(penSep, xL, y, xR, y);
                y += 7f;

                void Row(string lbl, string val, bool bold = false)
                {
                    Font fv = bold ? fntStrg : fntNorm;
                    g.DrawString(lbl, fntNorm, bLabel, xL, y);
                    g.DrawString(val, fv,      bText,  new RectangleF(xL, y, xR - xL, rowH), fmtRight);
                    y += rowH;
                }

                if (d.HasData)
                {
                    Row("Samples", $"{d.Count:N0}");
                    Row("Moy.",    $"{d.DcLsb:N0} LSB");
                    Row("RMS",     $"{d.RmsLsb:N0} LSB", bold: true);
                    Row("Min",     $"{d.Min:N0}");
                    Row("Max",     $"{d.Max:N0}");
                    Row("P-P",     $"{d.PeakToPeak:N0} LSB");
                    Row("DNL max", d.DnlMax > 0 ? $"{d.DnlMax:F3}" : "—");
                    Row("ENOB",    d.Enob > 0 ? $"{d.Enob:F2} bits" : "—", bold: d.Enob > 0);
                }
                else
                {
                    Row("Samples", "0");
                    Row("Moy.",    $"{d.DcLsb:N0} LSB");
                    Row("RMS",     "—");
                    Row("Min/Max", "—");
                }

                y += 8f;
                g.DrawLine(penSep, xL, y, xR, y);
                y += 10f;
            }

            DrawSection("LEFT",  BarLeftColor,  _statsLeft);
            DrawSection("RIGHT", BarRightColor, _statsRight);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Calcul de l'échelle Y "propre" (max arrondi + interval)
        // ══════════════════════════════════════════════════════════════════════
        private static (double yMax, double interval) ComputeNiceYScale(double maxValue)
        {
            if (maxValue <= 0) return (1000, 200);

            // Plafond avec 10 % de marge
            double raw = maxValue * 1.1;
            double mag = Math.Pow(10, Math.Floor(Math.Log10(raw)));
            double norm = raw / mag;

            double niceMax;
            if      (norm <= 1.0) niceMax = 1.0 * mag;
            else if (norm <= 2.0) niceMax = 2.0 * mag;
            else if (norm <= 2.5) niceMax = 2.5 * mag;
            else if (norm <= 3.0) niceMax = 3.0 * mag;
            else if (norm <= 4.0) niceMax = 4.0 * mag;
            else if (norm <= 5.0) niceMax = 5.0 * mag;
            else if (norm <= 6.0) niceMax = 6.0 * mag;
            else if (norm <= 8.0) niceMax = 8.0 * mag;
            else                  niceMax = 10.0 * mag;

            // Interval cible : 5 graduations
            double rawI = niceMax / 5.0;
            double iMag = Math.Pow(10, Math.Floor(Math.Log10(rawI)));
            double iNorm = rawI / iMag;
            double niceI;
            if      (iNorm <= 1.5) niceI = 1.0 * iMag;
            else if (iNorm <= 3.5) niceI = 2.5 * iMag;
            else if (iNorm <= 7.5) niceI = 5.0 * iMag;
            else                   niceI = 10.0 * iMag;

            return (niceMax, niceI);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Labels Y custom — dessinés dans la marge grise à gauche du graphique
        // (le type Column de WinForms Chart force l'axe Y à X=0, non contournable)
        // ══════════════════════════════════════════════════════════════════════
        private void DrawCustomYLabels(Graphics g, ChartArea area, Rectangle cr, float sExport = 1f, Font? chartFont = null)
        {
            double yMin = area.AxisY.Minimum;
            double yMax = area.AxisY.Maximum;
            double interval = area.AxisY.Interval;
            if (double.IsNaN(yMin) || double.IsNaN(yMax) || double.IsNaN(interval) || interval <= 0) return;

            // ── Bord gauche REEL du plot : AxisX.ValueToPixelPosition sur xMin ──────
            // (pas area.Position.X qui est en % du controle entier)
            float innerLeft;
            try   { innerLeft = (float)area.AxisX.ValueToPixelPosition(area.AxisX.Minimum); }
            catch { return; }
            if (innerLeft < 6) return;

            // baseFont: use chartFont param if provided (already ×sExport for export)
            var baseFont  = chartFont ?? chartHistogram.Font ?? SystemFonts.DefaultFont;
            Color labelColor = LabelMajorColor;
            using var penTick = new Pen(labelColor, Math.Max(1f, sExport));

            // Mesure largeur max en GDI (TextRenderer = meme moteur que Chart)
            double firstTick = Math.Ceiling(yMin / interval) * interval;
            int maxLabelW = 0;
            for (double v = firstTick; v <= yMax + interval * 0.01; v += interval)
            {
                int w = TextRenderer.MeasureText(FormatYCount(v), baseFont,
                            new Size(int.MaxValue, int.MaxValue),
                            TextFormatFlags.NoPadding).Width;
                if (w > maxLabelW) maxLabelW = w;
            }

            // d) Bord droit des labels = innerLeft - 5px (colle au bord du graphique)
            int labelRight = (int)(innerLeft - 5 * sExport);
            int labelLeft  = labelRight - maxLabelW;

            int lineH = TextRenderer.MeasureText("0", baseFont,
                            new Size(int.MaxValue, int.MaxValue),
                            TextFormatFlags.NoPadding).Height;

            float lastDrawnPyCenter = float.PositiveInfinity;

            for (double yVal = firstTick; yVal <= yMax + interval * 0.01; yVal += interval)
            {
                float py;
                try { py = (float)area.AxisY.ValueToPixelPosition(yVal); }
                catch { continue; }

                int ipy = (int)Math.Round(py);
                g.DrawLine(penTick, innerLeft - 4f * sExport, ipy, innerLeft, ipy);

                if (lastDrawnPyCenter - py < lineH + 2f * sExport) continue;

                // a) Qualite: TextRenderer (GDI) au lieu de DrawString (GDI+)
                string lbl = FormatYCount(yVal);
                Size   sz  = TextRenderer.MeasureText(lbl, baseFont,
                                 new Size(int.MaxValue, int.MaxValue),
                                 TextFormatFlags.NoPadding | TextFormatFlags.PreserveGraphicsClipping);
                // ClearType nécessite une couleur de fond explicite ;
                // sans elle, les sous-pixels sont calculés pour fond blanc → rendu baveux sur gris
                TextRenderer.DrawText(g, lbl, baseFont,
                    new Point(labelRight - sz.Width, ipy - sz.Height / 2),
                    labelColor, SystemColors.Control,
                    TextFormatFlags.NoPadding | TextFormatFlags.PreserveGraphicsClipping);
                lastDrawnPyCenter = py;
            }

            // e/c) Titre "Count" juste a gauche des labels numériques
            float pTop, pBot;
            try
            {
                pTop = (float)area.AxisY.ValueToPixelPosition(yMax);
                pBot = (float)area.AxisY.ValueToPixelPosition(yMin);
            }
            catch { return; }

            // Titre "Count" — rendu via bitmap GDI (TextRenderer) pour qualité identique
            // aux labels numériques, puis rotation 90° du bitmap sur le Graphics GDI+.
            using var titleFont = new Font(baseFont.FontFamily, baseFont.SizeInPoints, FontStyle.Regular);
            const string titleText = "Count";
            Size tSz = TextRenderer.MeasureText(titleText, titleFont,
                new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
            if (tSz.Width > 0 && tSz.Height > 0)
            {
                // Bitmap en format 32bpp pour interpolation propre
                using var bmp = new Bitmap(tSz.Width + 2, tSz.Height + 2,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (var bg = Graphics.FromImage(bmp))
                {
                    bg.Clear(SystemColors.Control); // fond = couleur marge chart
                    TextRenderer.DrawText(bg, titleText, titleFont, new Point(1, 1),
                        labelColor, SystemColors.Control, TextFormatFlags.NoPadding);
                }
                float titleH = tSz.Height + 2;
                float titleCx = labelLeft - titleH / 2f - 4f;
                if (titleCx < 2) titleCx = 2f;
                float titleCy = (pTop + pBot) / 2f;

                var state = g.Save();
                g.TranslateTransform(titleCx, titleCy);
                g.RotateTransform(-90f);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.DrawImage(bmp, -(tSz.Width + 2) / 2f, -(tSz.Height + 2) / 2f);
                g.Restore(state);
            }
        }

         private string FormatYLabel(double v)
        {
            if (_isNormalized)
                return v == 0 ? "0" : v.ToString("G3", System.Globalization.CultureInfo.InvariantCulture);
            long n = (long)Math.Round(v);
            if (n == 0) return "0";
            long abs = Math.Abs(n);
            if (abs >= 1_000_000) return (n / 1_000_000.0).ToString("0.#") + " M";
            if (abs >= 1_000)
            {
                double k = n / 1000.0;
                return (k % 1 == 0 ? k.ToString("0") : k.ToString("0.#")) + " k";
            }
            return n.ToString();
        }

        // Alias conservé pour la compatibilité des appels depuis DrawCustomYLabels
        private string FormatYCount(double v) => FormatYLabel(v);

        // ══════════════════════════════════════════════════════════════════════
        // Export PNG (inchangé)
        // ══════════════════════════════════════════════════════════════════════
        // ── Export CSV ───────────────────────────────────────────────────────
        public System.Text.StringBuilder GetCsvData(char sep, bool header, char decSep = '.')
        {
            var sb  = new System.Text.StringBuilder();
            var ic2 = System.Globalization.CultureInfo.InvariantCulture;
            string s = sep.ToString();

            bool hasL = _lastBinsLeft  != null && _statsLeft.HasData;
            bool hasR = _lastBinsRight != null && _statsRight.HasData;

            if (header)
            {
                // Stats header
                if (hasL)
                {
                    sb.AppendLine($"# LEFT  — Samples:{_statsLeft.Count}{s} RMS:{_statsLeft.RmsLsb:F1} LSB{s} ENOB:{_statsLeft.Enob:F2} bits{s} DNL max:{_statsLeft.DnlMax:F3}");
                }
                if (hasR)
                {
                    sb.AppendLine($"# RIGHT — Samples:{_statsRight.Count}{s} RMS:{_statsRight.RmsLsb:F1} LSB{s} ENOB:{_statsRight.Enob:F2} bits{s} DNL max:{_statsRight.DnlMax:F3}");
                }
                // Column headers
                sb.Append("Code_LSB");
                if (hasL) sb.Append($"{s}Count_L");
                if (hasR) sb.Append($"{s}Count_R");
                sb.AppendLine();
            }

            int n = Math.Max(hasL ? _lastBinsLeft!.Length : 0,
                             hasR ? _lastBinsRight!.Length : 0);
            for (int i = 0; i < n; i++)
            {
                double x = -_lastLsbRange + i * _lastBinWidth + _lastBinWidth / 2.0;
                sb.Append(decSep == ',' ? x.ToString("F1", ic2).Replace('.', ',') : x.ToString("F1", ic2));
                if (hasL) sb.Append($"{s}{(i < _lastBinsLeft!.Length ? _lastBinsLeft[i] : 0)}");
                if (hasR) sb.Append($"{s}{(i < _lastBinsRight!.Length ? _lastBinsRight[i] : 0)}");
                sb.AppendLine();
            }
            return sb;
        }

        // ── Export PNG / PDF ─────────────────────────────────────────────────
        public void ExportPng(string filePath, int scale = 2, string? headerTitle = null, string? footerText = null)
        {
            int headerPx = ChartExport.HeaderPixels(headerTitle, scale);

            var savedBack = chartHistogram.BackColor;
            chartHistogram.BackColor = Color.White;
            chartHistogram.Refresh();

            using var clone = ChartExport.CloneChart(chartHistogram);
            clone.Size      = new Size(chartHistogram.Width * scale, chartHistogram.Height * scale);
            clone.BackColor = Color.White;
            foreach (var a in clone.ChartAreas) a.BackColor = Color.White;

            chartHistogram.BackColor = savedBack;
            chartHistogram.Refresh();

            ChartExport.ApplyExportScaling(clone, scale);
            // Réserve la même largeur proportionnelle pour le panneau stats
            ApplyChartStatsMarginToChart(clone, clone.Width, scale);
            if (headerPx > 0)
                ChartExport.ApplyChartHeaderMargin(clone, "HistogramArea", headerPx);

            clone.PostPaint += ChartHistogram_PostPaint;

            var pngBytes = ChartExport.RenderChartToPngBytes(clone);
            ChartExport.SaveChartPng(pngBytes, filePath, headerPx, headerTitle, scale, dpi: 300, footerText: footerText);
        }

        /// <summary>Retourne le Chart live pour copie presse-papier (résolution écran).</summary>
        public System.Windows.Forms.DataVisualization.Charting.Chart GetChartForClipboard()
            => chartHistogram;

    }
}
