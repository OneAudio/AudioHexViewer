using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace AudioHexViewer
{
    public class HistogramForm : Form
    {
        private readonly int[] leftSamples;
        private readonly int[] rightSamples;

        private ComboBox comboChannel;
        private CheckBox chkAC;
        private NumericUpDown numRangePow;   // n : plage ±(2^n) LSB
        private NumericUpDown numBinPow;     // m : LSB/bin = 2^m
        private Label lblRangeValue;         // affiche ±LSB
        private Label lblBinValue;           // affiche LSB/bin
        private Chart chart;

        // Panneau Stats
        private Panel panelStats;
        private Label lblLeftHeader;
        private Label lblLeftSamples;
        private Label lblLeftDC;
        private Label lblLeftRms;
        private Label lblLeftMinMax;

        private Label lblRightHeader;
        private Label lblRightSamples;
        private Label lblRightDC;
        private Label lblRightRms;
        private Label lblRightMinMax;

        private const int Max24Pos = (1 << 23) - 1;  // +0x7FFFFF
        private const int Min24Neg = -(1 << 23);     // -0x800000
        private const int MaxBins = 256;            // nombre max de colonnes

        public HistogramForm(int[] leftSamples, int[] rightSamples)
        {
            this.leftSamples = leftSamples ?? Array.Empty<int>();
            this.rightSamples = rightSamples ?? Array.Empty<int>();

            InitializeComponent();
            InitDefaults();
            RebuildHistogram();
        }

        private void InitializeComponent()
        {
            this.Text = "Histogram";
            this.Size = new Size(1000, 500);

            comboChannel = new ComboBox();
            chkAC = new CheckBox();
            numRangePow = new NumericUpDown();
            numBinPow = new NumericUpDown();
            lblRangeValue = new Label();
            lblBinValue = new Label();
            chart = new Chart();
            panelStats = new Panel();

            // --- Combo Channel ---
            comboChannel.DropDownStyle = ComboBoxStyle.DropDownList;
            comboChannel.Items.AddRange(new object[] { "Left", "Right", "Both" });
            comboChannel.Location = new Point(10, 10);
            comboChannel.Size = new Size(100, 23);
            comboChannel.SelectedIndexChanged += (s, e) => RebuildHistogram();

            var lblChannel = new Label
            {
                Text = "Channel",
                Location = new Point(10, 35),
                AutoSize = true
            };

            // --- CheckBox AC (retrait DC) ---
            chkAC.Text = "AC";
            chkAC.AutoSize = true;
            chkAC.Location = new Point(130, 12);
            chkAC.CheckedChanged += (s, e) => RebuildHistogram();

            // --- NumericUpDown RangePow : n pour ±(2^n) LSB ---
            numRangePow.Location = new Point(190, 10);
            numRangePow.Size = new Size(60, 23);
            numRangePow.Minimum = 0;   // 2^0 = 1
            numRangePow.Maximum = 23;  // 2^23 = 8 388 608
            numRangePow.ValueChanged += (s, e) => RebuildHistogram();

            var lblRangePow = new Label
            {
                Text = "±LSB : 2^n",
                Location = new Point(190, 35),
                AutoSize = true
            };

            lblRangeValue.Location = new Point(190, 52);
            lblRangeValue.AutoSize = true;
            lblRangeValue.Text = "±LSB = 1024";

            // --- NumericUpDown BinPow : m pour LSB/bin = 2^m ---
            numBinPow.Location = new Point(280, 10);
            numBinPow.Size = new Size(60, 23);
            numBinPow.Minimum = 0;   // 2^0 = 1
            numBinPow.Maximum = 23;  // 2^23
            numBinPow.ValueChanged += (s, e) => RebuildHistogram();

            var lblBinPow = new Label
            {
                Text = "LSB/bin : 2^m",
                Location = new Point(280, 35),
                AutoSize = true
            };

            lblBinValue.Location = new Point(280, 52);
            lblBinValue.AutoSize = true;
            lblBinValue.Text = "LSB/bin = 1";

            // --- Chart ---
            chart.Dock = DockStyle.Fill;

            var area = new ChartArea("HistogramArea");
            area.AxisX.Title = "Code (LSB, centré)";
            area.AxisY.Title = "Count";
            area.AxisX.MajorGrid.LineColor = Color.LightGray;
            area.AxisY.MajorGrid.LineColor = Color.LightGray;
            area.AxisX.Crossing = 0.0; // axe X croise Y à 0

            chart.ChartAreas.Add(area);

            // --- Panel Stats (à droite) ---
            panelStats.Dock = DockStyle.Right;
            panelStats.Width = 220;
            panelStats.Padding = new Padding(8);
            panelStats.BackColor = Color.WhiteSmoke;

            // Création des labels de stats
            lblLeftHeader = new Label
            {
                Text = "LEFT",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(8, 8)
            };

            lblLeftSamples = new Label
            {
                Text = "Samples: -",
                AutoSize = true,
                Location = new Point(8, 30)
            };

            lblLeftDC = new Label
            {
                Text = "DC offset: -",
                AutoSize = true,
                Location = new Point(8, 50)
            };

            lblLeftRms = new Label
            {
                Text = "RMS: -",
                AutoSize = true,
                Location = new Point(8, 70)
            };

            lblLeftMinMax = new Label
            {
                Text = "Min/Max: -",
                AutoSize = true,
                Location = new Point(8, 90)
            };

            lblRightHeader = new Label
            {
                Text = "RIGHT",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(8, 130)
            };

            lblRightSamples = new Label
            {
                Text = "Samples: -",
                AutoSize = true,
                Location = new Point(8, 152)
            };

            lblRightDC = new Label
            {
                Text = "DC offset: -",
                AutoSize = true,
                Location = new Point(8, 172)
            };

            lblRightRms = new Label
            {
                Text = "RMS: -",
                AutoSize = true,
                Location = new Point(8, 192)
            };

            lblRightMinMax = new Label
            {
                Text = "Min/Max: -",
                AutoSize = true,
                Location = new Point(8, 212)
            };

            panelStats.Controls.Add(lblLeftHeader);
            panelStats.Controls.Add(lblLeftSamples);
            panelStats.Controls.Add(lblLeftDC);
            panelStats.Controls.Add(lblLeftRms);
            panelStats.Controls.Add(lblLeftMinMax);
            panelStats.Controls.Add(lblRightHeader);
            panelStats.Controls.Add(lblRightSamples);
            panelStats.Controls.Add(lblRightDC);
            panelStats.Controls.Add(lblRightRms);
            panelStats.Controls.Add(lblRightMinMax);

            // --- Mise en page générale ---
            // On met le chart + panelStats dans un panel bas, le reste (contrôles) en haut.
            var panelTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80
            };

            panelTop.Controls.Add(comboChannel);
            panelTop.Controls.Add(lblChannel);
            panelTop.Controls.Add(chkAC);
            panelTop.Controls.Add(numRangePow);
            panelTop.Controls.Add(lblRangePow);
            panelTop.Controls.Add(lblRangeValue);
            panelTop.Controls.Add(numBinPow);
            panelTop.Controls.Add(lblBinPow);
            panelTop.Controls.Add(lblBinValue);

            var panelCenter = new Panel
            {
                Dock = DockStyle.Fill
            };

            panelCenter.Controls.Add(panelStats);
            panelCenter.Controls.Add(chart);

            this.Controls.Add(panelCenter);
            this.Controls.Add(panelTop);
        }

        private void InitDefaults()
        {
            // Channel par défaut : Both
            comboChannel.SelectedItem = "Both";

            // AC : par défaut ON
            chkAC.Checked = true;

            // plage ±(2^n) : n = 10 => ±1024
            numRangePow.Value = 10;

            // LSB/bin = 2^m : m = 0 => 1 LSB/bin
            numBinPow.Value = 0;

            UpdateLabels();
            ResetStatsLabels();
        }

        private void UpdateLabels()
        {
            int rangePow = (int)numRangePow.Value;
            int binPow = (int)numBinPow.Value;

            int lsbRange = 1 << rangePow; // ±lsbRange
            int binWidth = 1 << binPow;   // LSB/bin

            lblRangeValue.Text = $"±LSB = {lsbRange}";
            lblBinValue.Text = $"LSB/bin = {binWidth}";
        }

        private void ResetStatsLabels()
        {
            lblLeftSamples.Text = "Samples: -";
            lblLeftDC.Text = "DC offset: -";
            lblLeftRms.Text = "RMS: -";
            lblLeftMinMax.Text = "Min/Max: -";

            lblRightSamples.Text = "Samples: -";
            lblRightDC.Text = "DC offset: -";
            lblRightRms.Text = "RMS: -";
            lblRightMinMax.Text = "Min/Max: -";
        }

        private void RebuildHistogram()
        {
            // Sécurité taille du chart
            if (chart.ClientSize.Width <= 0 || chart.ClientSize.Height <= 0)
                return; 
            
            if (leftSamples.Length == 0 && rightSamples.Length == 0)
            {
                chart.Series.Clear();
                ResetStatsLabels();
                return;
            }

            // --- Lecture des réglages courants ---
            string mode = comboChannel.SelectedItem as string ?? "Both";
            bool ac = chkAC.Checked;

            int rangePow = (int)numRangePow.Value;
            int binPow = (int)numBinPow.Value;

            if (rangePow < 0) rangePow = 0;
            if (rangePow > 23) rangePow = 23;
            if (binPow < 0) binPow = 0;
            if (binPow > 23) binPow = 23;

            int lsbRange = 1 << rangePow;    // plage symétrique [-lsbRange ; +lsbRange]
            int range = 2 * lsbRange + 1; // nombre de codes

            if (range <= 0)
            {
                chart.Series.Clear();
                ResetStatsLabels();
                return;
            }

            int binWidth = 1 << binPow;      // LSB/bin

            // --- Calcul des offsets DC "raw" ---
            int dcOffsetL = 0;
            int dcOffsetR = 0;

            if (leftSamples.Length > 0)
            {
                long sumLraw = 0;
                foreach (int v in leftSamples)
                    sumLraw += v;
                double meanLraw = sumLraw / (double)leftSamples.Length;
                dcOffsetL = (int)Math.Round(meanLraw);
            }

            if (rightSamples.Length > 0)
            {
                long sumRraw = 0;
                foreach (int v in rightSamples)
                    sumRraw += v;
                double meanRraw = sumRraw / (double)rightSamples.Length;
                dcOffsetR = (int)Math.Round(meanRraw);
            }

            // offsets utilisés pour AC :
            int offsetL = ac ? dcOffsetL : 0;
            int offsetR = ac ? dcOffsetR : 0;

            // --- Limitation du nombre de colonnes à MaxBins SEULEMENT si lsbRange > 1024 ---
            if (lsbRange > 1024)
            {
                while (true)
                {
                    int binCountTest = (int)Math.Ceiling(range / (double)binWidth);
                    if (binCountTest <= MaxBins)
                        break;

                    if (binPow >= 23) // sécurité
                        break;

                    binPow++;
                    binWidth = 1 << binPow;
                }

                numBinPow.Value = binPow;
            }

            UpdateLabels();

            int binCount = (int)Math.Ceiling(range / (double)binWidth);
            if (binCount <= 0)
            {
                chart.Series.Clear();
                ResetStatsLabels();
                return;
            }

            int[] binsLeft = null;
            int[] binsRight = null;

            if (mode == "Left" || mode == "Both")
                binsLeft = new int[binCount];
            if (mode == "Right" || mode == "Both")
                binsRight = new int[binCount];

            // Stats accumulées dans la fenêtre ±lsbRange, après correction AC
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

            // --- Remplissage des bins + stats LEFT ---
            if (binsLeft != null)
            {
                foreach (int v in leftSamples)
                {
                    int vAdj = v - offsetL; // correction AC si active

                    if (vAdj < -lsbRange || vAdj > lsbRange) continue;
                    int idx = (vAdj + lsbRange) / binWidth; // 0..binCount-1
                    if (idx >= 0 && idx < binCount)
                    {
                        binsLeft[idx]++;

                        // Stats
                        leftCountWin++;
                        leftSumWin += vAdj;
                        leftSumSqWin += (long)vAdj * vAdj;
                        if (vAdj < leftMin) leftMin = vAdj;
                        if (vAdj > leftMax) leftMax = vAdj;
                    }
                }
            }

            // --- Remplissage des bins + stats RIGHT ---
            if (binsRight != null)
            {
                foreach (int v in rightSamples)
                {
                    int vAdj = v - offsetR; // correction AC si active

                    if (vAdj < -lsbRange || vAdj > lsbRange) continue;
                    int idx = (vAdj + lsbRange) / binWidth;
                    if (idx >= 0 && idx < binCount)
                    {
                        binsRight[idx]++;

                        // Stats
                        rightCountWin++;
                        rightSumWin += vAdj;
                        rightSumSqWin += (long)vAdj * vAdj;
                        if (vAdj < rightMin) rightMin = vAdj;
                        if (vAdj > rightMax) rightMax = vAdj;
                    }
                }
            }

            // --- Construction des séries ---
            chart.Series.Clear();

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

                chart.Series.Add(seriesL);
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

                chart.Series.Add(seriesR);
            }

            // --- Réglage de l'axe X : symétrique, 0 au centre, 8 subdivisions de chaque côté ---
            var chartArea = chart.ChartAreas["HistogramArea"];
            chartArea.AxisX.Minimum = -lsbRange;
            chartArea.AxisX.Maximum = lsbRange;
            chartArea.AxisX.Crossing = 0.0;

            double step = lsbRange / 8.0;
            if (step <= 0) step = 1;
            chartArea.AxisX.Interval = step;

            // Axe Y auto sur le max des counts
            chartArea.RecalculateAxesScale();

            // --- Mise à jour des stats affichées ---
            UpdateStatsDisplay(
                leftCountWin, leftSumWin, leftSumSqWin, leftMin, leftMax, dcOffsetL,
                rightCountWin, rightSumWin, rightSumSqWin, rightMin, rightMax, dcOffsetR);
        }

        private void UpdateStatsDisplay(
            int leftCountWin, long leftSumWin, long leftSumSqWin, int leftMin, int leftMax, int dcOffsetL,
            int rightCountWin, long rightSumWin, long rightSumSqWin, int rightMin, int rightMax, int dcOffsetR)
        {
            // LEFT
            if (leftCountWin > 0)
            {
                double meanL = leftSumWin / (double)leftCountWin;
                double rmsL = Math.Sqrt(leftSumSqWin / (double)leftCountWin);

                lblLeftSamples.Text = $"Samples: {leftCountWin}";
                lblLeftDC.Text = $"DC offset (raw): {dcOffsetL} LSB";
                lblLeftRms.Text = $"RMS: {rmsL:F2} LSB";
                lblLeftMinMax.Text = $"Min/Max: {leftMin} / {leftMax}";
            }
            else
            {
                lblLeftSamples.Text = "Samples: 0";
                lblLeftDC.Text = $"DC offset (raw): {dcOffsetL} LSB";
                lblLeftRms.Text = "RMS: -";
                lblLeftMinMax.Text = "Min/Max: -";
            }

            // RIGHT
            if (rightCountWin > 0)
            {
                double meanR = rightSumWin / (double)rightCountWin;
                double rmsR = Math.Sqrt(rightSumSqWin / (double)rightCountWin);

                lblRightSamples.Text = $"Samples: {rightCountWin}";
                lblRightDC.Text = $"DC offset (raw): {dcOffsetR} LSB";
                lblRightRms.Text = $"RMS: {rmsR:F2} LSB";
                lblRightMinMax.Text = $"Min/Max: {rightMin} / {rightMax}";
            }
            else
            {
                lblRightSamples.Text = "Samples: 0";
                lblRightDC.Text = $"DC offset (raw): {dcOffsetR} LSB";
                lblRightRms.Text = "RMS: -";
                lblRightMinMax.Text = "Min/Max: -";
            }
        }
    }
}
