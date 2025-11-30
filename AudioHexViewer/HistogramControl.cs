using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

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
        }

        private static HistogramSettings? cachedSettings;

        private static string SettingsFilePath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HistogramSettings.json");

        public delegate (int[] Left, int[] Right) SampleProvider(int durationMs);

        private readonly SampleProvider? _provider;
        private readonly int _sampleRate;

        private int[] leftSamples = Array.Empty<int>();
        private int[] rightSamples = Array.Empty<int>();

        private System.Windows.Forms.Timer? liveTimer;
        private bool isLive = false;

        private const int MaxBins = 256;

        // 1) Constructeur utilisé par le Designer
        public HistogramControl()
        {
            InitializeComponent();
            InitChart();
            InitDefaults();
            SetupLiveTimer();
            //lblWindowInfo.Text = "TEST LABEL"; // test
        }

        // 2) Constructeur utilisé par Form1 à l'exécution
        public HistogramControl(SampleProvider provider, int sampleRate)
            : this()
        {
            _provider = provider;
            _sampleRate = sampleRate;

            // Maintenant qu'on connaît le vrai SR, on met à jour le label
            UpdateWindowInfo();

            // Premier calcul d'histo si on a déjà un peu de données
            AcquireOnce();

            grpLeftStats.Paint += (s, e) => DrawColoredBorder(grpLeftStats, e, Color.DodgerBlue);
            grpRightStats.Paint += (s, e) => DrawColoredBorder(grpRightStats, e, Color.OrangeRed);
        }
        //
       
        private void LoadSettingsIfAny()
        {
            if (cachedSettings != null)
                return;

            try
            {
                if (!File.Exists(SettingsFilePath))
                    return;

                var json = File.ReadAllText(SettingsFilePath);
                var s = JsonSerializer.Deserialize<HistogramSettings>(json);

                if (s == null || s.RangeLsb <= 0 || s.BinLsb <= 0 || s.RecordSamples <= 0)
                {
                    cachedSettings = null;
                    return;
                }

                cachedSettings = s;
            }
            catch
            {
                cachedSettings = null;
            }
        }

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
            if (comboSampleWin.SelectedIndex < 0)
                return SampleWindowOptions[0];

            int samples = SampleWindowOptions[comboSampleWin.SelectedIndex];
            LastUsedWindowSamples = samples;
            return samples;
        }

        private void ApplySettingsOrDefaults()
        {
            LoadSettingsIfAny();

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
            comboSampleWin.SelectedIndex = idxWinDefault;

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
                comboSampleWin.SelectedIndex = idxWin;
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
                int samples = GetWindowSamples();
                int intervalMs = (int)Math.Max(10, Math.Round((double)samples * 1000.0 / _sampleRate));
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
            int durationMs = (int)Math.Max(10, Math.Round(msExact));

            var (left, right) = _provider(durationMs);

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
                // Acquisition unique : on lit une seule fenêtre et on ne démarre PAS le timer
                isLive = false;
                liveTimer?.Stop();
                AcquireOnce();
            }
            else
            {
                // Mode continu : histogramme mis à jour en continu via le timer
                if (liveTimer != null)
                {
                    int samples = GetWindowSamples();
                    int intervalMs = (int)Math.Max(10, Math.Round((double)samples * 1000.0 / _sampleRate));
                    liveTimer.Interval = intervalMs;
                }

                isLive = true;

                // Première acquisition immédiate
                AcquireOnce();

                // Puis mises à jour régulières
                liveTimer?.Start();
            }
        }

        // Appelé par Form1 quand on appuie sur STOP
        public void StopFromRun()
        {
            isLive = false;
            liveTimer?.Stop();
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
            double ms = seconds ;

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

            // Remplissage ComboBox fenêtre (samples)
            comboSampleWin.Items.Clear();
            foreach (var v in SampleWindowOptions)
                comboSampleWin.Items.Add(v.ToString("N0"));

            ApplySettingsOrDefaults();
        }
        //
        public void SaveHistogramSettings()
        {
            SaveSettingsToFile();
        }

        //
        private void SaveSettingsToFile()
        {
            var rangeLsb = GetRangeLsb();
            var binLsb = GetBinLsb();
            var winSamples = GetWindowSamples();

            var s = new HistogramSettings
            {
                AC = chkAC.Checked,
                Channel = comboChannel.SelectedItem as string ?? "Both",
                RangeLsb = rangeLsb,
                BinLsb = binLsb,
                RecordSamples = winSamples,
                SplitterDistance = splitContainerMain.SplitterDistance
            };

            cachedSettings = s;

            try
            {
                var dir = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(s, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(SettingsFilePath, json);

                MessageBox.Show("Histogram settings saved.",
                    "Histogram",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving settings:\n" + ex.Message,
                    "Histogram",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

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

        private void comboSampleWin_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateWindowInfo();

            if (liveTimer != null && liveTimer.Enabled && _sampleRate > 0)
            {
                int samples = GetWindowSamples();
                int intervalMs = (int)Math.Max(10, Math.Round((double)samples * 1000.0 / _sampleRate));
                liveTimer.Interval = intervalMs;
            }

            if (!isLive)
            {
                AcquireOnce();
            }
        }

        private void RebuildHistogram()
        {
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

            if (leftSamples.Length > 0)
            {
                long sumLraw = 0;
                foreach (int v in leftSamples) sumLraw += v;
                dcOffsetL = (int)Math.Round(sumLraw / (double)leftSamples.Length);
            }

            if (rightSamples.Length > 0)
            {
                long sumRraw = 0;
                foreach (int v in rightSamples) sumRraw += v;
                dcOffsetR = (int)Math.Round(sumRraw / (double)rightSamples.Length);
            }

            int offsetL = ac ? dcOffsetL : 0;
            int offsetR = ac ? dcOffsetR : 0;

            if (lsbRange > 1024)
            {
                while (true)
                {
                    int binCountTest = (int)Math.Ceiling(range / (double)binWidth);
                    if (binCountTest <= MaxBins) break;

                    binWidth <<= 1;

                    if (binWidth >= BinLsbOptions[BinLsbOptions.Length - 1])
                        break;
                }

                int newIndex = FindClosestIndex(BinLsbOptions, binWidth);
                binWidth = BinLsbOptions[newIndex];
                comboBinLsb.SelectedIndex = newIndex;
                lastBinLsb = binWidth;
            }

            int binCount = (int)Math.Ceiling(range / (double)binWidth);
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

        private void btnSaveSettings_Click(object sender, EventArgs e)
        {
            SaveSettingsToFile();
        }


    }
}
