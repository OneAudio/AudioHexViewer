using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;
using System.Text.Json;



namespace AudioHexViewer
{
    public partial class HistogramWindow : Form
    {
        private int lastRangeLsb = 1024;
        private int lastBinLsb = 1;

        private bool _updatingRange = false;
        private bool _updatingBin = false;

        private class HistogramSettings
        {
            public bool AC { get; set; } = true;
            public string Channel { get; set; } = "Both";

            // Valeurs directes en LSB
            public int RangeLsb { get; set; } = 1024; // ±1024 LSB
            public int BinLsb { get; set; } = 1;      // 1 LSB/bin

            public int SplitterDistance { get; set; } = 740;
            public int RecordMs { get; set; } = 1000;

        }

        private static HistogramSettings? cachedSettings;

        private static string SettingsFilePath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HistogramSettings.json");


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

                // On sécurise un minimum (au cas où l'ancien format traîne)
                if (s == null || s.RangeLsb <= 0 || s.BinLsb <= 0)
                {
                    cachedSettings = null;
                    return;
                }

                cachedSettings = s;
            }
            catch
            {
                // si problème, on ignore les réglages existants
                cachedSettings = null;
            }
        }


        private void ApplySettingsOrDefaults()
        {
            LoadSettingsIfAny();

            // Valeurs par défaut "sécurisées"
            int defaultRange = 1024;
            int defaultBin = 1;

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

                // RangeLsb / BinLsb en respectant les Min/Max des NumericUpDown
                int rangeLsb = s.RangeLsb;
                int binLsb = s.BinLsb;

                if (rangeLsb < numRangePow.Minimum) rangeLsb = (int)numRangePow.Minimum;
                if (rangeLsb > numRangePow.Maximum) rangeLsb = (int)numRangePow.Maximum;
                if (binLsb < numBinPow.Minimum) binLsb = (int)numBinPow.Minimum;
                if (binLsb > numBinPow.Maximum) binLsb = (int)numBinPow.Maximum;

                // On force à des puissances de 2
                rangeLsb = NextPowerOfTwo(PrevPowerOfTwo(rangeLsb)); // "snap" to nearest power of two plus proche > ou =
                binLsb = NextPowerOfTwo(PrevPowerOfTwo(binLsb));

                _updatingRange = true;
                numRangePow.Value = rangeLsb;
                _updatingRange = false;
                lastRangeLsb = rangeLsb;

                _updatingBin = true;
                numBinPow.Value = binLsb;
                _updatingBin = false;
                lastBinLsb = binLsb;

                // Record time (ms)
                int recMs = s.RecordMs;
                if (recMs < (int)numRecordTimeMs.Minimum) recMs = (int)numRecordTimeMs.Minimum;
                if (recMs > (int)numRecordTimeMs.Maximum) recMs = (int)numRecordTimeMs.Maximum;
                numRecordTimeMs.Value = recMs;

                // Splitter
                try
                {
                    if (s.SplitterDistance > 0 && s.SplitterDistance < splitContainerMain.Width - 50)
                        splitContainerMain.SplitterDistance = s.SplitterDistance;
                }
                catch { }
            }
            else
            {
                // Aucun réglage sauvegardé → defaults
                comboChannel.SelectedItem = "Both";
                chkAC.Checked = true;

                int rangeLsb = defaultRange;
                int binLsb = defaultBin;

                _updatingRange = true;
                numRangePow.Value = rangeLsb;
                _updatingRange = false;
                lastRangeLsb = rangeLsb;

                _updatingBin = true;
                numBinPow.Value = binLsb;
                _updatingBin = false;
                lastBinLsb = binLsb;


            }


            ResetStatsLabels();
        }



        public delegate (int[] Left, int[] Right) SampleProvider(int durationMs);

        private readonly SampleProvider _provider;
        private readonly int _sampleRate;

        private int[] leftSamples = Array.Empty<int>();
        private int[] rightSamples = Array.Empty<int>();

        private System.Windows.Forms.Timer liveTimer;
        private bool isLive = false;

        private const int MaxBins = 256;

        public HistogramWindow(SampleProvider provider, int sampleRate)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _sampleRate = sampleRate;

            InitializeComponent();
            InitChart();
            InitDefaults();
            SetupLiveTimer();

            grpLeftStats.Paint += (s, e) => DrawColoredBorder(grpLeftStats, e, Color.DodgerBlue);
            grpRightStats.Paint += (s, e) => DrawColoredBorder(grpRightStats, e, Color.OrangeRed);

            // Premier calcul d'histo avec la durée courante (par ex. 1000 ms)
            AcquireOnce();
        }
        //
        private void SetupLiveTimer()
        {
            liveTimer = new System.Windows.Forms.Timer();
            liveTimer.Tick += LiveTimer_Tick;
            liveTimer.Interval = (int)numRecordTimeMs.Value; // par défaut, même durée que la fenêtre
        }

        private void AcquireOnce()
        {
            if (_provider == null)
                return;

            int durationMs = (int)numRecordTimeMs.Value;
            if (durationMs < 10) durationMs = 10;

            // 👉 ICI on appelle Form1 via le délégué
            var (left, right) = _provider(durationMs);

            leftSamples = left ?? Array.Empty<int>();
            rightSamples = right ?? Array.Empty<int>();

            RebuildHistogram();
        }

        //
        private void LiveTimer_Tick(object? sender, EventArgs e)
        {
            // Si la fenêtre est en train de se fermer / détruite, on coupe le LIVE
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
                // Sécurité : on coupe le LIVE si quelque chose tourne mal
                if (liveTimer != null)
                    liveTimer.Stop();

                isLive = false;
                btnLive.Text = "LIVE";

                MessageBox.Show("Erreur dans le mode LIVE : " + ex.Message,
                                "Histogram LIVE",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }
        //
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (liveTimer != null)
            {
                liveTimer.Stop();
                liveTimer.Dispose();
                liveTimer = null!;
            }

            base.OnFormClosing(e);
        }

        //
        //
        private void btnOnce_Click(object sender, EventArgs e)
        {
            AcquireOnce();
        }
        //
        private void btnLive_Click(object sender, EventArgs e)
        {
            if (!isLive)
            {
                isLive = true;
                btnLive.Text = "STOP LIVE";

                int durationMs = (int)numRecordTimeMs.Value;
                if (durationMs < 10) durationMs = 10;
                liveTimer.Interval = durationMs;

                liveTimer.Start();
            }
            else
            {
                isLive = false;
                btnLive.Text = "LIVE";
                liveTimer.Stop();
            }
        }
        //
        private void numRecordTimeMs_ValueChanged(object sender, EventArgs e)
        {
            if (liveTimer != null && liveTimer.Enabled)
            {
                int durationMs = (int)numRecordTimeMs.Value;
                if (durationMs < 10) durationMs = 10;
                liveTimer.Interval = durationMs;
            }
        }
        //
        private void DrawColoredBorder(GroupBox gb, PaintEventArgs e, Color color)
        {
            // Rectangle légèrement à l’intérieur pour éviter d'écraser la bordure système
            Rectangle r = gb.ClientRectangle;
            r.X += 2;
            r.Y += 8;   // pour laisser le texte du GroupBox
            r.Width -= 4;
            r.Height -= 10;

            using (var pen = new Pen(color, 1))
            {
                e.Graphics.DrawRectangle(pen, r);
            }
        }

        //
        private void InitChart()
        {
            var area = new ChartArea("HistogramArea");
            area.AxisX.Title = "Code (LSB, centered)";
            area.AxisY.Title = "Count";
            area.AxisX.MajorGrid.LineColor = Color.LightGray;
            area.AxisY.MajorGrid.LineColor = Color.LightGray;
            area.AxisX.Crossing = 0.0;

            chartHistogram.ChartAreas.Clear();
            chartHistogram.ChartAreas.Add(area);
        }

        private void InitDefaults()
        {

            ApplySettingsOrDefaults(); // a voir ?
        }

        //
        private void SaveSettingsToFile()
        {
            var s = new HistogramSettings
            {
                AC = chkAC.Checked,
                Channel = comboChannel.SelectedItem as string ?? "Both",
                RangeLsb = (int)numRangePow.Value,
                BinLsb = (int)numBinPow.Value,
                RecordMs = (int)numRecordTimeMs.Value,
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


        //

        private int NextPowerOfTwo(int v)
        {
            const int max = 1 << 23;
            if (v < 1) return 1;
            if (v >= max) return max;

            int p = 1;
            while (p <= v && p < max)
                p <<= 1;
            return p;
        }

        private int PrevPowerOfTwo(int v)
        {
            if (v <= 1) return 1;

            int p = 1;
            const int max = 1 << 23;
            while ((p << 1) < v && (p << 1) <= max)
                p <<= 1;
            return p;
        }


        private void ResetStatsLabels()
        {
            lblLeftSamples.Text = "Samples:";
            lblLeftDC.Text = "DC offset:";
            lblLeftRms.Text = "RMS:";
            lblLeftMinMax.Text = "Min/Max:";

            lblRightSamples.Text = "Samples:";
            lblRightDC.Text = "DC offset:";
            lblRightRms.Text = "RMS:";
            lblRightMinMax.Text = "Min/Max:";
        }

        private void comboChannel_SelectedIndexChanged(object sender, EventArgs e) => RebuildHistogram();
        private void chkAC_CheckedChanged(object sender, EventArgs e) => RebuildHistogram();
        private void numRangePow_ValueChanged(object sender, EventArgs e)
        {
            if (_updatingRange) return;

            int requested = (int)numRangePow.Value;
            int snapped;

            if (requested > lastRangeLsb)
            {
                snapped = NextPowerOfTwo(lastRangeLsb);
            }
            else if (requested < lastRangeLsb)
            {
                snapped = PrevPowerOfTwo(lastRangeLsb);
            }
            else
            {
                snapped = lastRangeLsb;
            }

            if (snapped != requested)
            {
                _updatingRange = true;
                numRangePow.Value = snapped;
                _updatingRange = false;
            }

            lastRangeLsb = snapped;
            RebuildHistogram();
        }

        private void numBinPow_ValueChanged(object sender, EventArgs e)
        {
            if (_updatingBin) return;

            int requested = (int)numBinPow.Value;
            int snapped;

            if (requested > lastBinLsb)
            {
                snapped = NextPowerOfTwo(lastBinLsb);
            }
            else if (requested < lastBinLsb)
            {
                snapped = PrevPowerOfTwo(lastBinLsb);
            }
            else
            {
                snapped = lastBinLsb;
            }

            if (snapped != requested)
            {
                _updatingBin = true;
                numBinPow.Value = snapped;
                _updatingBin = false;
            }

            lastBinLsb = snapped;
            RebuildHistogram();
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

            int lsbRange = (int)numRangePow.Value;
            if (lsbRange < 1) lsbRange = 1;
            int range = 2 * lsbRange + 1;
            if (range <= 0)
            {
                chartHistogram.Series.Clear();
                ResetStatsLabels();
                return;
            }

            int binWidth = (int)numBinPow.Value;
            if (binWidth < 1) binWidth = 1;

            // DC offsets bruts
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

            // Limiter le nombre de colonnes si ±LSB > 1024
            if (lsbRange > 1024)
            {
                while (true)
                {
                    int binCountTest = (int)Math.Ceiling(range / (double)binWidth);
                    if (binCountTest <= MaxBins) break;

                    if (binWidth >= (1 << 23)) break; // sécurité

                    // double la largeur de bin
                    binWidth <<= 1;
                }

                _updatingBin = true;
                numBinPow.Value = binWidth;
                _updatingBin = false;
            }


            int binCount = (int)Math.Ceiling(range / (double)binWidth);
            if (binCount <= 0)
            {
                chartHistogram.Series.Clear();
                ResetStatsLabels();
                return;
            }

            int[] binsLeft = null;
            int[] binsRight = null;

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
                lblLeftDC.Text = $"DC Offset: {dcOffsetL} LSB";
                lblLeftRms.Text = $"RMS: {rmsL:F2} LSB";
                lblLeftMinMax.Text = $"Min/Max: {leftMin} / {leftMax}";
            }
            else
            {
                lblLeftSamples.Text = "Samples: 0";
                lblLeftDC.Text = $"DC Offset: {dcOffsetL} LSB";
                lblLeftRms.Text = "RMS: -";
                lblLeftMinMax.Text = "Min/Max: -";
            }

            if (rightCountWin > 0)
            {
                double rmsR = Math.Sqrt(rightSumSqWin / (double)rightCountWin);
                lblRightSamples.Text = $"Samples: {rightCountWin}";
                lblRightDC.Text = $"DC Offset: {dcOffsetR} LSB";
                lblRightRms.Text = $"RMS: {rmsR:F2} LSB";
                lblRightMinMax.Text = $"Min/Max: {rightMin} / {rightMax}";
            }
            else
            {
                lblRightSamples.Text = "Samples: 0";
                lblRightDC.Text = $"DC Offset: {dcOffsetR} LSB";
                lblRightRms.Text = "RMS: -";
                lblRightMinMax.Text = "Min/Max: -";
            }
        }

        private void btnSaveSettings_Click(object sender, EventArgs e)
        {
            SaveSettingsToFile();
        }

        private void grpRightStats_Enter(object sender, EventArgs e)
        {

        }
    }
}
