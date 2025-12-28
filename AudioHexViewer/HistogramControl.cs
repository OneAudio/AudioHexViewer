using System.Text.Json;
using System.Windows.Forms.DataVisualization.Charting;
using System.ComponentModel;


namespace AudioHexViewer
{
    public partial class HistogramControl : UserControl
    {
        private int lastRangeLsb = 1024;
        private int lastBinLsb = 1;

        // Table de valeurs possibles pour la portée ±LSB et LSB/bin
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

        // Fenêtres possibles en nombre d'échantillons (2^14 à 2^20)
        private static readonly int[] SampleWindowOptions =
        {
            2048,    // 2^11    
            4096,    // 2^12
            8192,    // 2^13
            16384,    // 2^14
            32768,    // 2^15
            65536,    // 2^16
            131072,   // 2^17
            262144,   // 2^18
            524288,   // 2^19
            1048576   // 2^20
        };

        // Valeur de fenêtre la plus récente (samples), accessible depuis Form1 pour le REC
        public static int LastUsedWindowSamples { get; private set; } = 131072; // 2^17

        // Événements pour Form1
        public event Action<bool>? RunStateChanged; // true=RUN, false=STOP
        public event Action? RunRequested;


        private class HistogramSettings
        {
            public bool AC { get; set; } = true;
            public string Channel { get; set; } = "Both";

            // Valeurs directes en LSB
            public int RangeLsb { get; set; } = 1024; // ±1024 LSB
            public int BinLsb { get; set; } = 1;      // 1 LSB/bin

            // Nombre d'échantillons de la fenêtre (pas un exposant)
            public int RecordSamples { get; set; } = 131072; // 2^17

            public int SplitterDistance { get; set; } = 740;


            public string? DeviceId { get; set; }          // ID Windows du périphérique audio
            public bool CaptureOutput { get; set; }        // false = Input, true = Output
            public string? MainDataFormat { get; set; }    // Valeur de comboDisplayMode ("Float", "Voltage 
        }

        private static HistogramSettings? cachedSettings;

        public delegate (int[] Left, int[] Right) SampleProvider(int sampleCount);

        private readonly SampleProvider? _provider;
        private int _sampleRate;

        // Pas de rafraîchissement en samples (hop). 0 => pas de hop (mise à jour = taille fenêtre)
        private int _hopSamples = 0;

        private int[] leftSamples = Array.Empty<int>();

        private int? _forcedWindowSamples = null;
        private int[] rightSamples = Array.Empty<int>();

        private System.Windows.Forms.Timer? liveTimer;
        private bool isLive = false;

        private const int MaxBins = 16384; // Nombre max de bins dans l'histogramme

        // Rapport max autorisé : LSBscale / (LSB/bin)
        private const int MaxScaleOverBin = 8192;


        // 1) Constructeur utilisé par le Designer
        public HistogramControl()
        {
            InitializeComponent();
            InitChart();
            InitDefaults();
            SetupLiveTimer();
            UpdateRunButtonAppearance();   // <-- ajouter
        }

        // 2) Constructeur utilisé par Form1 à l'exécution
        public HistogramControl(SampleProvider provider, int sampleRate)
    : this()
        {
            _provider = provider;

            // Utilise la méthode centralisée (met à jour SR + label + timer)
            UpdateSampleRate(sampleRate);

            // Premier calcul d'histo si on a déjà un peu de données
            AcquireOnce();

            grpLeftStats.Paint += (s, e) => DrawColoredBorder(grpLeftStats, e, Color.DodgerBlue);
            grpRightStats.Paint += (s, e) => DrawColoredBorder(grpRightStats, e, Color.OrangeRed);
        }

        //
        public AudioHexViewer.HistogramSettings GetHistogramSettingsSnapshot()
        {
            return new AudioHexViewer.HistogramSettings
            {
                AC = chkAC.Checked,
                Channel = comboChannel.SelectedItem as string ?? "Both",
                RangeLsb = GetRangeLsb(),
                BinLsb = GetBinLsb(),
                RecordSamples = LastUsedWindowSamples,
                SplitterDistance = splitContainerMain.SplitterDistance
            };
        }

        public void ApplyHistogramSettings(AudioHexViewer.HistogramSettings s)
        {
            if (s == null) return;

            chkAC.Checked = s.AC;

            int chIdx = comboChannel.Items.IndexOf(s.Channel);
            if (chIdx >= 0)
                comboChannel.SelectedIndex = chIdx;

            int rIdx = comboRangeLsb.Items.IndexOf(s.RangeLsb.ToString());
            if (rIdx >= 0)
                comboRangeLsb.SelectedIndex = rIdx;

            int bIdx = comboBinLsb.Items.IndexOf(s.BinLsb.ToString());
            if (bIdx >= 0)
                comboBinLsb.SelectedIndex = bIdx;

            if (s.SplitterDistance > 0)
                splitContainerMain.SplitterDistance = s.SplitterDistance;

            RebuildHistogram();
        }
        //
        //
        public void UpdateSampleRate(int sampleRate)
        {
            if (sampleRate <= 0)
                return;

            _sampleRate = sampleRate;

            // Met à jour le texte "≈ X.XX s"
            UpdateWindowInfo();

            // Ajuste l'intervalle du timer live pour coller à la fenêtre
            if (liveTimer != null)
            {
                int step = _hopSamples > 0 ? _hopSamples : GetWindowSamples();
                int intervalMs = (int)Math.Max(10, Math.Round((double)step * 1000.0 / _sampleRate));
                liveTimer.Interval = intervalMs;
            }
        }

        // Appelé par Form1 quand le paramètre Overlap change.
        // hopSamples = nombre d'échantillons entre deux calculs (0 => pas de hop, on suit la fenêtre).
        public void UpdateHopSamples(int hopSamples)
        {
            if (hopSamples < 0) hopSamples = 0;
            _hopSamples = hopSamples;

            if (liveTimer != null && _sampleRate > 0)
            {
                int step = _hopSamples > 0 ? _hopSamples : GetWindowSamples();
                int intervalMs = (int)Math.Max(10, Math.Round((double)step * 1000.0 / _sampleRate));
                liveTimer.Interval = intervalMs;
            }
        }

        // Appelé par Form1 : force l'option "Number of Samples" pour que Histogram + Spectrum
        // utilisent la même fenêtre d'analyse.
        public void SetGlobalSampleSize(int samples)
        {
            if (samples <= 0) return;
            _forcedWindowSamples = samples;
            UpdateWindowInfo();
        }
        //
        //
        private int FindClosestIndex(int[] options, int target)
        {
            int bestIndex = 0;
            int bestDiff = int.MaxValue;

            for (int i = 0; i < options.Length; i++)
            {
                int diff = Math.Abs(options[i] - target);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    bestIndex = i;
                }
            }
            return bestIndex;
        }

        private int GetRangeLsb()
        {
            if (comboRangeLsb.SelectedIndex < 0)
                return RangeLsbOptions[0];

            return RangeLsbOptions[comboRangeLsb.SelectedIndex];
        }

        private int GetBinLsb()
        {
            if (comboBinLsb.SelectedIndex < 0)
                return BinLsbOptions[0];

            return BinLsbOptions[comboBinLsb.SelectedIndex];
        }

        private int GetWindowSamples()
        {
            if (_forcedWindowSamples.HasValue && _forcedWindowSamples.Value > 0)
            {
                LastUsedWindowSamples = _forcedWindowSamples.Value;
                return _forcedWindowSamples.Value;
            }

            // Plus de sélection locale : on prend la dernière valeur connue ou un défaut
            if (LastUsedWindowSamples > 0)
                return LastUsedWindowSamples;

            LastUsedWindowSamples = 131072;
            return LastUsedWindowSamples;
        }


        private void ApplySettingsOrDefaults()
        {

            // Valeurs par défaut
            comboChannel.SelectedItem = "Both";
            chkAC.Checked = true;

            int defaultRange = 1024;
            int defaultBin = 1;
            int defaultWin = 131072; // 2^17

            // Defaults initiaux
            int idxRangeDefault = FindClosestIndex(RangeLsbOptions, defaultRange);
            int idxBinDefault = FindClosestIndex(BinLsbOptions, defaultBin);
            int idxWinDefault = FindClosestIndex(SampleWindowOptions, defaultWin);

            comboRangeLsb.SelectedIndex = idxRangeDefault;
            comboBinLsb.SelectedIndex = idxBinDefault;

            lastRangeLsb = RangeLsbOptions[idxRangeDefault];
            lastBinLsb = BinLsbOptions[idxBinDefault];
            LastUsedWindowSamples = SampleWindowOptions[idxWinDefault];

            if (cachedSettings != null)
            {
                var s = cachedSettings;

                // Channel
                if (comboChannel.Items.Contains(s.Channel))
                    comboChannel.SelectedItem = s.Channel;
                else
                    comboChannel.SelectedItem = "Both";

                // AC
                chkAC.Checked = s.AC;

                // Range / Bin via ComboBox
                int rangeLsb = s.RangeLsb < 1 ? 1 : s.RangeLsb;
                int binLsb = s.BinLsb < 1 ? 1 : s.BinLsb;

                int idxRange = FindClosestIndex(RangeLsbOptions, rangeLsb);
                int idxBin = FindClosestIndex(BinLsbOptions, binLsb);

                comboRangeLsb.SelectedIndex = idxRange;
                comboBinLsb.SelectedIndex = idxBin;

                lastRangeLsb = RangeLsbOptions[idxRange];
                lastBinLsb = BinLsbOptions[idxBin];

                // Fenêtre (samples)
                int winSamples = s.RecordSamples;
                if (winSamples <= 0)
                    winSamples = defaultWin;

                int idxWin = FindClosestIndex(SampleWindowOptions, winSamples);
                LastUsedWindowSamples = SampleWindowOptions[idxWin];
            }

            UpdateWindowInfo();
            ResetStatsLabels();
        }

        private void SetupLiveTimer()
        {
            liveTimer = new System.Windows.Forms.Timer();
            liveTimer.Tick += LiveTimer_Tick;

            if (_sampleRate > 0)
            {
                int step = _hopSamples > 0 ? _hopSamples : GetWindowSamples();
                int intervalMs = (int)Math.Max(10, Math.Round((double)step * 1000.0 / _sampleRate));
                liveTimer.Interval = intervalMs;
            }
            else
            {
                liveTimer.Interval = 1000;
            }
        }

        private void AcquireOnce()
        {
            if (_provider == null || _sampleRate <= 0)
                return;

            int samples = GetWindowSamples();
            double msExact = (double)samples * 1000.0 / _sampleRate;
            var (left, right) = _provider(samples);

            leftSamples = left ?? Array.Empty<int>();
            rightSamples = right ?? Array.Empty<int>();

            RebuildHistogram();
        }
        //
        // Appelé par Form1 quand on appuie sur RUN (btnStart)
        public void StartFromRun(bool onceMode)
        {
            if (_provider == null || _sampleRate <= 0)
                return;

            if (onceMode)
            {
                // Acquisition unique
                AcquireOnce();

                isLive = false;
                liveTimer?.Stop();
                UpdateRunButtonAppearance();
            }
            else
            {
                // Mode continu : histogramme mis à jour en continu via le timer
                if (liveTimer != null)
                {
                    int step = _hopSamples > 0 ? _hopSamples : GetWindowSamples();
                int intervalMs = (int)Math.Max(10, Math.Round((double)step * 1000.0 / _sampleRate));
                    liveTimer.Interval = intervalMs;
                }

                isLive = true;

                // Première acquisition immédiate
                AcquireOnce();

                // Puis mises à jour régulières
                liveTimer?.Start();

                UpdateRunButtonAppearance();
            }
        }


        // Appelé par Form1 quand on appuie sur STOP
        public void StopFromRun()
        {
            isLive = false;
            liveTimer?.Stop();  
            UpdateRunButtonAppearance();
        }

        //
        private void UpdateRunButtonAppearance()
        {
            if (btnRun == null)
                return;

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

        //
        private void LiveTimer_Tick(object? sender, EventArgs e)
        {
            if (this.IsDisposed || !this.IsHandleCreated)
            {
                if (liveTimer != null)
                    liveTimer.Stop();
                return;
            }

            try
            {
                AcquireOnce();
            }
            catch (Exception ex)
            {
                if (liveTimer != null)
                    liveTimer.Stop();

                isLive = false;

                MessageBox.Show("Erreur dans le mode LIVE : " + ex.Message,
                                "Histogram",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }

        }

        private void UpdateWindowInfo()
        {
            int samples = GetWindowSamples();

            // Si on ne connaît pas encore le SampleRate, on affiche au moins les samples
            if (_sampleRate <= 0)
            {
                lblWindowInfo.Text = $"{samples:N0} samples";
                return;
            }

            double seconds = (double)samples / _sampleRate;
            double ms = seconds;

            lblWindowInfo.Text = $"≈ {ms:F2} s";
        }


        private void DrawColoredBorder(GroupBox gb, PaintEventArgs e, Color color)
        {
            Rectangle r = gb.ClientRectangle;
            r.X += 2;
            r.Y += 8;
            r.Width -= 4;
            r.Height -= 10;

            using (var pen = new Pen(color, 1))
            {
                e.Graphics.DrawRectangle(pen, r);
            }
        }

        private void InitChart()
        {
            var area = new ChartArea("HistogramArea");
            area.AxisX.Title = "Code (LSB, centré)";
            area.AxisY.Title = "Count";
            area.AxisX.MajorGrid.LineColor = Color.LightGray;
            area.AxisY.MajorGrid.LineColor = Color.LightGray;
            area.AxisX.Crossing = 0.0;

            chartHistogram.ChartAreas.Clear();
            chartHistogram.ChartAreas.Add(area);
        }

        private void InitDefaults()
        {
            // Remplissage ComboBox Range
            comboRangeLsb.Items.Clear();
            foreach (var v in RangeLsbOptions)
                comboRangeLsb.Items.Add(v.ToString());

            // Remplissage ComboBox Bin
            comboBinLsb.Items.Clear();
            foreach (var v in BinLsbOptions)
                comboBinLsb.Items.Add(v.ToString());

            ApplySettingsOrDefaults();
            // --- Câblage du bouton RUN ---
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
        }
        //
        //


        private void ResetStatsLabels()
        {
            lblLeftSamples.Text = "Samples:";
            lblLeftDC.Text = "DC:";
            lblLeftRms.Text = "RMS:";
            lblLeftMinMax.Text = "Min/Max:";

            lblRightSamples.Text = "Samples:";
            lblRightDC.Text = "DC:";
            lblRightRms.Text = "RMS:";
            lblRightMinMax.Text = "Min/Max:";
        }

        private void comboChannel_SelectedIndexChanged(object sender, EventArgs e) => RebuildHistogram();
        private void chkAC_CheckedChanged(object sender, EventArgs e) => RebuildHistogram();

        private void comboRangeLsb_SelectedIndexChanged(object sender, EventArgs e)
        {
            lastRangeLsb = GetRangeLsb();
            RebuildHistogram();
        }

        private void comboBinLsb_SelectedIndexChanged(object sender, EventArgs e)
        {
            lastBinLsb = GetBinLsb();
            RebuildHistogram();
        }

        private void RebuildHistogram()
        {
            // NE *PAS* tester la taille du chart ici, on veut remplir les séries
            // et laisser le control redessiner quand il sera correctement dimensionné.
            // IMPORTANT : ne rien dessiner si le chart n'a pas encore de taille
            if (chartHistogram.ClientSize.Width <= 0 || chartHistogram.ClientSize.Height <= 0)
                return;

            if (leftSamples.Length == 0 && rightSamples.Length == 0)
            {
                chartHistogram.Series.Clear();
                ResetStatsLabels();
                return;
            }

            string mode = comboChannel.SelectedItem as string ?? "Both";
            bool ac = chkAC.Checked;

            int lsbRange = GetRangeLsb();
            if (lsbRange < 1) lsbRange = 1;
            int range = 2 * lsbRange + 1;
            if (range <= 0)
            {
                chartHistogram.Series.Clear();
                ResetStatsLabels();
                return;
            }

            int binWidth = GetBinLsb();
            if (binWidth < 1) binWidth = 1;

            int dcOffsetL = 0;
            int dcOffsetR = 0;

            bool wantLeft = (mode == "Left" || mode == "Both");
            bool wantRight = (mode == "Right" || mode == "Both");

            // Calcul DC uniquement si la voie est demandée (et si AC activé, sinon inutile)
            if (ac && wantLeft && leftSamples.Length > 0)
            {
                long sumLraw = 0;
                foreach (int v in leftSamples) sumLraw += v;
                dcOffsetL = (int)Math.Round(sumLraw / (double)leftSamples.Length);
            }

            if (ac && wantRight && rightSamples.Length > 0)
            {
                long sumRraw = 0;
                foreach (int v in rightSamples) sumRraw += v;
                dcOffsetR = (int)Math.Round(sumRraw / (double)rightSamples.Length);
            }


            int offsetL = ac ? dcOffsetL : 0;
            int offsetR = ac ? dcOffsetR : 0;


            if (lsbRange > 1024)
            {
                // On veut limiter directement le rapport :
                //   LSBscale / (LSB/bin) <= MaxScaleOverBin
                // soit : lsbRange / binWidth <= MaxScaleOverBin    
                while (true)
                {
                    // Test en entier pour éviter les flottants :
                    // lsbRange > MaxScaleOverBin * binWidth  <=>  lsbRange / binWidth > MaxScaleOverBin
                    if (lsbRange <= MaxScaleOverBin * binWidth)
                        break;

                    binWidth <<= 1; // binWidth *= 2

                    // on ne dépasse pas la plus grosse valeur possible dans BinLsbOptions
                    if (binWidth >= BinLsbOptions[BinLsbOptions.Length - 1])
                        break;
                }

                int newIndex = FindClosestIndex(BinLsbOptions, binWidth);
                binWidth = BinLsbOptions[newIndex];
                comboBinLsb.SelectedIndex = newIndex;
                lastBinLsb = binWidth;
            }

            int binCount = (int)Math.Ceiling(range / (double)binWidth);
            if (binCount > MaxBins)
                binCount = MaxBins;

            //int binCount = (int)Math.Ceiling(range / (double)binWidth);
            if (binCount <= 0)
            {
                chartHistogram.Series.Clear();
                ResetStatsLabels();
                return;
            }

            int[]? binsLeft = null;
            int[]? binsRight = null;

            if (mode == "Left" || mode == "Both")
                binsLeft = new int[binCount];
            if (mode == "Right" || mode == "Both")
                binsRight = new int[binCount];

            int leftCountWin = 0;
            long leftSumWin = 0;
            long leftSumSqWin = 0;
            int leftMin = int.MaxValue;
            int leftMax = int.MinValue;

            int rightCountWin = 0;
            long rightSumWin = 0;
            long rightSumSqWin = 0;
            int rightMin = int.MaxValue;
            int rightMax = int.MinValue;

            if (binsLeft != null)
            {
                foreach (int v in leftSamples)
                {
                    int vAdj = v - offsetL;

                    if (vAdj < -lsbRange || vAdj > lsbRange) continue;
                    int idx = (vAdj + lsbRange) / binWidth;
                    if (idx >= 0 && idx < binCount)
                    {
                        binsLeft[idx]++;
                        leftCountWin++;
                        leftSumWin += vAdj;
                        leftSumSqWin += (long)vAdj * vAdj;
                        if (vAdj < leftMin) leftMin = vAdj;
                        if (vAdj > leftMax) leftMax = vAdj;
                    }
                }
            }

            if (binsRight != null)
            {
                foreach (int v in rightSamples)
                {
                    int vAdj = v - offsetR;

                    if (vAdj < -lsbRange || vAdj > lsbRange) continue;
                    int idx = (vAdj + lsbRange) / binWidth;
                    if (idx >= 0 && idx < binCount)
                    {
                        binsRight[idx]++;
                        rightCountWin++;
                        rightSumWin += vAdj;
                        rightSumSqWin += (long)vAdj * vAdj;
                        if (vAdj < rightMin) rightMin = vAdj;
                        if (vAdj > rightMax) rightMax = vAdj;
                    }
                }
            }

            chartHistogram.Series.Clear();

            if (binsLeft != null)
            {
                var seriesL = new Series("Left");
                seriesL.ChartType = SeriesChartType.Column;
                seriesL.ChartArea = "HistogramArea";
                seriesL.Color = Color.DodgerBlue;

                for (int i = 0; i < binCount; i++)
                {
                    int binStart = -lsbRange + i * binWidth;
                    int binCenter = binStart + binWidth / 2;
                    seriesL.Points.AddXY(binCenter, binsLeft[i]);
                }

                chartHistogram.Series.Add(seriesL);
            }

            if (binsRight != null)
            {
                var seriesR = new Series("Right");
                seriesR.ChartType = SeriesChartType.Column;
                seriesR.ChartArea = "HistogramArea";
                seriesR.Color = Color.OrangeRed;

                for (int i = 0; i < binCount; i++)
                {
                    int binStart = -lsbRange + i * binWidth;
                    int binCenter = binStart + binWidth / 2;
                    seriesR.Points.AddXY(binCenter, binsRight[i]);
                }

                chartHistogram.Series.Add(seriesR);
            }

            var area = chartHistogram.ChartAreas["HistogramArea"];
            area.AxisX.Minimum = -lsbRange;
            area.AxisX.Maximum = lsbRange;
            area.AxisX.Crossing = 0.0;
            double step = lsbRange / 8.0;
            if (step <= 0) step = 1;
            area.AxisX.Interval = step;
            area.RecalculateAxesScale();

            UpdateStatsDisplay(
                leftCountWin, leftSumWin, leftSumSqWin, leftMin, leftMax, dcOffsetL,
                rightCountWin, rightSumWin, rightSumSqWin, rightMin, rightMax, dcOffsetR);
        }

        private void UpdateStatsDisplay(
            int leftCountWin, long leftSumWin, long leftSumSqWin, int leftMin, int leftMax, int dcOffsetL,
            int rightCountWin, long rightSumWin, long rightSumSqWin, int rightMin, int rightMax, int dcOffsetR)
        {
            if (leftCountWin > 0)
            {
                double rmsL = Math.Sqrt(leftSumSqWin / (double)leftCountWin);
                lblLeftSamples.Text = $"Samples: {leftCountWin}";
                lblLeftDC.Text = $"DC: {dcOffsetL} LSB";
                lblLeftRms.Text = $"RMS: {rmsL:F2} LSB";
                lblLeftMinMax.Text = $"Min/Max: {leftMin} / {leftMax}";
            }
            else
            {
                lblLeftSamples.Text = "Samples: 0";
                lblLeftDC.Text = $"DC: {dcOffsetL} LSB";
                lblLeftRms.Text = "RMS: -";
                lblLeftMinMax.Text = "Min/Max: -";
            }

            if (rightCountWin > 0)
            {
                double rmsR = Math.Sqrt(rightSumSqWin / (double)rightCountWin);
                lblRightSamples.Text = $"Samples: {rightCountWin}";
                lblRightDC.Text = $"DC: {dcOffsetR} LSB";
                lblRightRms.Text = $"RMS: {rmsR:F2} LSB";
                lblRightMinMax.Text = $"Min/Max: {rightMin} / {rightMax}";
            }
            else
            {
                lblRightSamples.Text = "Samples: 0";
                lblRightDC.Text = $"DC: {dcOffsetR} LSB";
                lblRightRms.Text = "RMS: -";
                lblRightMinMax.Text = "Min/Max: -";
            }
        }
               
    }
}
