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

        // Accesseurs identiques à SpectrumControl pour faciliter la mise à jour
        private Color BackgroundColor  => Color.FromArgb(_colors.BackgroundArgb);
        private Color GridMajorColor   => Color.FromArgb(_colors.GridArgb);
        private Color GridMinorColor   => Color.FromArgb(_colors.GridMinorArgb);
        private Color LabelMajorColor  => Color.FromArgb(_colors.LabelMajorArgb);
        private Color LabelMinorColor  => Color.FromArgb(_colors.LabelMinorArgb);
        private Color BarLeftColor     => Color.FromArgb(_colors.FftLeftArgb);
        private Color BarRightColor    => Color.FromArgb(_colors.FftRightArgb);

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

        // Stats pour PostPaint
        private readonly struct StatsData
        {
            public readonly int    Count;
            public readonly int    DcLsb;
            public readonly double RmsLsb;
            public readonly int    Min, Max;
            public readonly bool   HasData;
            public StatsData(int count, int dc, double rms, int min, int max)
            { Count = count; DcLsb = dc; RmsLsb = rms; Min = min; Max = max; HasData = count > 0; }
        }
        private StatsData _statsLeft;
        private StatsData _statsRight;

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
                AC              = ChkAC.Checked,
                Channel         = comboChannel.SelectedItem as string ?? "Both",
                RangeLsb        = GetRangeLsb(),
                BinLsb          = GetBinLsb(),
                RecordSamples   = LastUsedWindowSamples,
                SplitterDistance = 0
            };

        public void ApplyHistogramSettings(AudioHexViewer.HistogramSettings s)
        {
            if (s == null) return;
            ChkAC.Checked = s.AC;
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
            }
        }

        public void SetGlobalSampleSize(int samples)
        {
            if (samples <= 0) return;
            _forcedWindowSamples = samples;
            UpdateWindowInfo();
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
            }
            else
            {
                if (liveTimer != null)
                {
                    int step = _hopSamples > 0 ? _hopSamples : GetWindowSamples();
                    liveTimer.Interval = (int)Math.Max(10, Math.Round(step * 1000.0 / _sampleRate));
                }
                isLive = true;
                AcquireOnce();
                liveTimer?.Start();
                UpdateRunButtonAppearance();
            }
        }

        public void StopFromRun()
        {
            isLive = false;
            liveTimer?.Stop();
            UpdateRunButtonAppearance();
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
            ChkAC.Checked = true;

            comboRangeLsb.SelectedIndex = FindClosestIndex(RangeLsbOptions, 4194304);
            comboBinLsb.SelectedIndex   = FindClosestIndex(BinLsbOptions, 32768);
            lastRangeLsb = GetRangeLsb();
            lastBinLsb   = GetBinLsb();
            LastUsedWindowSamples = 131072;

            UpdateWindowInfo();

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
            var area = chartHistogram.ChartAreas["HistogramArea"];
            int w = chartHistogram.ClientSize.Width;
            if (w <= 0) return;

            int reserved    = StatsWidthPx;
            int maxReserved = Math.Max(120, w - MinPlotWidthPx);
            reserved = Math.Min(reserved, maxReserved);

            float reservedPct = Math.Max(0f, Math.Min(45f, 100f * reserved / w));

            area.Position.Auto = false;
            // Marge gauche élargie pour loger les labels Y dans la zone grise.
            // On utilise 100f (au lieu de 97f) pour que le bord droit de la ChartArea
            // soit identique à SpectrumControl : right = 100 - reservedPct dans les deux cas.
            const float LeftPct = 11f; // espace pour titre "Count" + labels numériques
            float width = (100f - LeftPct) - reservedPct;
            if (width < 10f) width = 10f;
            area.Position = new ElementPosition(LeftPct, 3f, width, 94f);

            chartHistogram.Invalidate();
        }

        // ══════════════════════════════════════════════════════════════════════
        // RUN / STOP
        // ══════════════════════════════════════════════════════════════════════
        private void UpdateRunButtonAppearance()
        {
            if (btnRun == null) return;
            if (isLive)
            {
                btnRun.Text      = "STOP";
                btnRun.BackColor = Color.Firebrick;
                btnRun.ForeColor = Color.White;
            }
            else
            {
                btnRun.Text      = "RUN";
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
        private void ChkAC_CheckedChanged(object sender, EventArgs e)             => RebuildHistogram();

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
            bool ac = ChkAC.Checked;

            int lsbRange = Math.Max(1, GetRangeLsb());
            int binWidth = Math.Max(1, GetBinLsb());

            bool wantLeft  = mode is "Left"  or "Both";
            bool wantRight = mode is "Right" or "Both";

            // DC offsets
            int dcL = 0, dcR = 0;
            if (ac && wantLeft  && leftSamples.Length  > 0) { long s = 0; foreach (int v in leftSamples)  s += v; dcL = (int)Math.Round(s / (double)leftSamples.Length);  }
            if (ac && wantRight && rightSamples.Length > 0) { long s = 0; foreach (int v in rightSamples) s += v; dcR = (int)Math.Round(s / (double)rightSamples.Length); }

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

            _statsLeft  = lCnt > 0 ? new StatsData(lCnt, dcL, Math.Sqrt(lSumSq / (double)lCnt), lMin, lMax) : new StatsData(0, dcL, 0, 0, 0);
            _statsRight = rCnt > 0 ? new StatsData(rCnt, dcR, Math.Sqrt(rSumSq / (double)rCnt), rMin, rMax) : new StatsData(0, dcR, 0, 0, 0);

            // Séries — couleurs issues de SpectrumColorSettings
            chartHistogram.Series.Clear();
            if (binsLeft != null)
            {
                var s = new Series("Left") { ChartType = SeriesChartType.Column, ChartArea = "HistogramArea", Color = BarLeftColor };
                for (int i = 0; i < binCount; i++) s.Points.AddXY(-lsbRange + i * binWidth + binWidth / 2, binsLeft[i]);
                chartHistogram.Series.Add(s);
            }
            if (binsRight != null)
            {
                var s = new Series("Right") { ChartType = SeriesChartType.Column, ChartArea = "HistogramArea", Color = BarRightColor };
                for (int i = 0; i < binCount; i++) s.Points.AddXY(-lsbRange + i * binWidth + binWidth / 2, binsRight[i]);
                chartHistogram.Series.Add(s);
            }

            // Axe X
            var area = chartHistogram.ChartAreas["HistogramArea"];
            area.AxisX.Minimum  = -lsbRange;
            area.AxisX.Maximum  =  lsbRange;
            area.AxisX.Crossing = 0.0;  // ligne X à count=0 (bas du graphique)
            double step = lsbRange / 8.0;
            area.AxisX.Interval = step <= 0 ? 1 : step;

            // Axe Y : fixer explicitement min/max/interval
            // (en mode auto, Chart ne ré-expose pas les valeurs calculées → NaN → DrawCustomYLabels fail)
            int maxCount = 0;
            if (binsLeft  != null) foreach (int c in binsLeft)  if (c > maxCount) maxCount = c;
            if (binsRight != null) foreach (int c in binsRight) if (c > maxCount) maxCount = c;

            var (yMax, yInterval) = ComputeNiceYScale(maxCount);
            area.AxisY.Minimum  = 0;
            area.AxisY.Maximum  = yMax;
            area.AxisY.Interval = yInterval;
            area.AxisY.IsStartedFromZero = true;

            area.RecalculateAxesScale();
            chartHistogram.Invalidate();
        }

        // ══════════════════════════════════════════════════════════════════════
        // PostPaint — panneau stats (style identique à SpectrumControl)
        // ══════════════════════════════════════════════════════════════════════
        private void ChartHistogram_PostPaint(object? sender, ChartPaintEventArgs e)
        {
            if (chartHistogram.ChartAreas.Count == 0) return;

            var area = chartHistogram.ChartAreas["HistogramArea"];
            var g    = e.ChartGraphics.Graphics;
            var cr   = chartHistogram.ClientRectangle;

            // ── Labels Y custom dans la marge grise à gauche ─────────────────
            DrawCustomYLabels(g, area, cr);

            // Rectangle du panneau stats aligné sur le plot réel (comme DrawChartStatsPanel de SpectrumControl)
            var pos      = area.Position;
            float aRight = cr.Width  * (pos.X + pos.Width)  / 100f;
            float pLeft  = aRight + 8f;
            float pRight = cr.Right - 6f;
            float pW     = pRight - pLeft;

            if (pW < 80) return;

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

            // Fond blanc + bordure gris clair — identique à SpectrumControl.DrawChartStatsPanel
            using (var bg = new SolidBrush(Color.White))
                g.FillRectangle(bg, panelRect);
            using (var border = new Pen(Color.LightGray, 1f))
                g.DrawRectangle(border, panelRect.X, panelRect.Y, panelRect.Width, panelRect.Height);

            // Polices & brosses — identiques à SpectrumControl
            var baseFont = chartHistogram.Font ?? SystemFonts.DefaultFont;
            using var fntHdr   = new Font(baseFont, FontStyle.Bold);
            using var fntNorm  = new Font(baseFont, FontStyle.Regular);
            using var fntBold  = new Font(baseFont, FontStyle.Bold);
            using var fntStrg  = new Font(baseFont.FontFamily, baseFont.SizeInPoints + 1.5f, FontStyle.Bold);
            using var bText    = new SolidBrush(Color.DimGray);
            using var bLabel   = new SolidBrush(Color.DimGray);
            using var penSep   = new Pen(Color.Gainsboro, 1f);
            using var fmtCtr   = new StringFormat { Alignment = StringAlignment.Center,  LineAlignment = StringAlignment.Near };
            using var fmtRight = new StringFormat { Alignment = StringAlignment.Far,     LineAlignment = StringAlignment.Near };

            float pad  = 8f;
            float xL   = panelRect.X + pad;
            float xR   = panelRect.Right - pad;
            float y    = panelRect.Y + pad;
            float rowH = fntNorm.GetHeight(g) + 4f;

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
                    Row("DC",      $"{d.DcLsb:N0} LSB");
                    Row("RMS",     $"{d.RmsLsb:N0} LSB", bold: true);
                    Row("Min",     $"{d.Min:N0}");
                    Row("Max",     $"{d.Max:N0}");
                }
                else
                {
                    Row("Samples", "0");
                    Row("DC",      $"{d.DcLsb:N0} LSB");
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
        private static (double yMax, double interval) ComputeNiceYScale(int maxCount)
        {
            if (maxCount <= 0) return (1000, 200);

            // Plafond avec 10 % de marge
            double raw = maxCount * 1.1;
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
        private void DrawCustomYLabels(Graphics g, ChartArea area, Rectangle cr)
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

            var baseFont  = chartHistogram.Font ?? SystemFonts.DefaultFont;
            Color labelColor = LabelMajorColor;
            using var penTick = new Pen(labelColor, 1f);

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
            int labelRight = (int)(innerLeft - 5);
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
                g.DrawLine(penTick, innerLeft - 4f, ipy, innerLeft, ipy);

                if (lastDrawnPyCenter - py < lineH + 2f) continue;

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

            using var titleFont  = new Font(baseFont.FontFamily, baseFont.SizeInPoints, FontStyle.Regular);
            float titleH = titleFont.GetHeight(g); // epaisseur horizontale du titre une fois tourne
            float titleCx = labelLeft - titleH / 2f - 4f;
            if (titleCx < 2) titleCx = 2f;

            var prevHint = g.TextRenderingHint;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            var state = g.Save();
            g.TranslateTransform(titleCx, (pTop + pBot) / 2f);
            g.RotateTransform(-90f);
            using var titleBrush = new SolidBrush(labelColor);
            using var fmtC = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("Count", titleFont, titleBrush, 0f, 0f, fmtC);
            g.Restore(state);
            g.TextRenderingHint = prevHint;
        }

                private static string FormatYCount(double v)
        {
            // b) Affichage en unités complètes (10 000 et non 10k)
            return ((long)Math.Round(v)).ToString("N0");
        }

        // ══════════════════════════════════════════════════════════════════════
        // Export PNG (inchangé)
        // ══════════════════════════════════════════════════════════════════════
        public void ExportPng(string filePath, int scale = 2, string? headerTitle = null)
        {
            using var clone = ChartExport.CloneChart(chartHistogram);
            bool hasHeader = !string.IsNullOrWhiteSpace(headerTitle);
            int  headerPx  = hasHeader ? 60 * scale : 0;
            clone.Size = new Size(chartHistogram.Width * scale, chartHistogram.Height * scale + headerPx);
            clone.BackColor = Color.White;
            foreach (var a in clone.ChartAreas) a.BackColor = Color.White;
            ApplyUniformExportScaling(clone, scale);
            if (hasHeader)
            {
                ApplyExportHeaderMargin(clone, headerPx);
                clone.Tag = new ExportHeaderContext { HeaderTitle = headerTitle!, HeaderPx = headerPx, Scale = scale };
                clone.PostPaint += Clone_PostPaint_DrawHeader;
            }
            ChartExport.SavePngRendered(clone, filePath, dpi: 300);
            if (hasHeader) { clone.PostPaint -= Clone_PostPaint_DrawHeader; clone.Tag = null; }
        }

        public void ExportPng(string filePath, int scale = 2)
        {
            using var clone = ChartExport.CloneChart(chartHistogram);
            clone.Size = new Size(chartHistogram.Width * scale, chartHistogram.Height * scale);
            clone.BackColor = Color.White;
            foreach (var a in clone.ChartAreas) a.BackColor = Color.White;
            ChartExport.SavePngRendered(clone, filePath, dpi: 300);
        }

        private sealed class ExportHeaderContext { public string HeaderTitle = ""; public int HeaderPx, Scale; }

        private static void ApplyExportHeaderMargin(Chart chart, int headerPx)
        {
            var area = chart.ChartAreas["HistogramArea"];
            int h = chart.ClientSize.Height; if (h <= 0) return;
            float pct = 100f * headerPx / h;
            float x  = area.Position.Auto || area.Position.Width  < 1 ? 3f  : area.Position.X;
            float ay = area.Position.Auto || area.Position.Height < 1 ? 3f  : area.Position.Y;
            float w  = area.Position.Auto || area.Position.Width  < 1 ? 94f : area.Position.Width;
            float ah = area.Position.Auto || area.Position.Height < 1 ? 90f : area.Position.Height;
            area.Position.Auto = false;
            float ny = ay + pct, nh = Math.Max(5f, ah - pct);
            if (ny > 95) ny = 95; if (ny + nh > 99) nh = Math.Max(5f, 99f - ny);
            area.Position = new ElementPosition(x, ny, w, nh);
        }

        private static void Clone_PostPaint_DrawHeader(object? sender, ChartPaintEventArgs e)
        {
            if (sender is not Chart chart || chart.Tag is not ExportHeaderContext ctx || ctx.HeaderPx <= 0) return;
            var lines = ctx.HeaderTitle.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            using var font  = new Font(chart.Font.FontFamily, chart.Font.SizeInPoints * 1.20f, FontStyle.Bold);
            using var brush = new SolidBrush(Color.Black);
            using var sf    = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };
            float lh = font.GetHeight(e.ChartGraphics.Graphics), y = (ctx.HeaderPx - lines.Length * lh) / 2f;
            if (y < 2f) y = 2f;
            foreach (var line in lines)
            {
                e.ChartGraphics.Graphics.DrawString(line, font, brush, new RectangleF(0, y, chart.ClientSize.Width, lh + 2), sf);
                y += lh + 2; if (y > ctx.HeaderPx) break;
            }
        }

        private static void ApplyUniformExportScaling(Chart c, int s)
        {
            if (s <= 1) return;
            c.Font = new Font(c.Font.FontFamily, c.Font.SizeInPoints * s, c.Font.Style);
            foreach (var a in c.ChartAreas)
            {
                if (a.AxisX.LabelStyle.Font != null) a.AxisX.LabelStyle.Font = ScaleFont(a.AxisX.LabelStyle.Font, s);
                if (a.AxisY.LabelStyle.Font != null) a.AxisY.LabelStyle.Font = ScaleFont(a.AxisY.LabelStyle.Font, s);
                if (a.AxisX.TitleFont != null) a.AxisX.TitleFont = ScaleFont(a.AxisX.TitleFont, s);
                if (a.AxisY.TitleFont != null) a.AxisY.TitleFont = ScaleFont(a.AxisY.TitleFont, s);
            }
            foreach (var leg in c.Legends) if (leg.Font != null) leg.Font = ScaleFont(leg.Font, s);
            foreach (var t  in c.Titles)  if (t.Font  != null) t.Font   = ScaleFont(t.Font,  s);
            static Font ScaleFont(Font f, int s) => new Font(f.FontFamily, f.SizeInPoints * s, f.Style);
        }
    }
}
