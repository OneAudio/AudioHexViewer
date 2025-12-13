using System;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;

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
        private WindowType lastWindow = WindowType.Hann;
        private ChannelMode lastChannel = ChannelMode.Left;

        // Averaging
        private int avgTargetCount = 1;
        private double[]? avgLeftSpectrum;
        private double[]? avgRightSpectrum;

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

        public SpectrumControl()
        {
            InitializeComponent();
            InitChartAppearance();
            InitializeUiOptions();
            UpdateRunButtonAppearance();

            // Clic droit sur la zone du spectre -> ouverture de la boîte d'axes
            chartSpectrum.MouseUp += ChartSpectrum_MouseUp;

            // Dessin manuel des sous-divisions log
            chartSpectrum.PostPaint += ChartSpectrum_PostPaint;
        }

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
        }

        // Appelé par Form1 : force la taille FFT pour que Histogram + Spectrum utilisent la même fenêtre.
        public void SetGlobalSampleSize(int samples)
        {
            if (samples <= 0) return;
            _forcedFftSize = samples;
            lastFftSize = samples;

            int idx = Array.IndexOf(FftSizeOptions, samples);
            if (idx >= 0 && comboFftSize != null)
            {
                comboFftSize.SelectedIndexChanged -= ComboFftSize_SelectedIndexChanged;
                comboFftSize.SelectedIndex = idx;
                comboFftSize.SelectedIndexChanged += ComboFftSize_SelectedIndexChanged;
            }

            if (comboFftSize != null)
                comboFftSize.Enabled = false;
                comboFftSize.Visible = false; // <-- ajouté
            if (lblFftSize != null)
            {
                lblFftSize.Visible = false;   // <-- ajouté
            }
            UpdateWindowInfo();
        }



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
            // On remet les buffers de moyenne à zéro
            avgLeftSpectrum = null;
            avgRightSpectrum = null;

            // Optionnel : forcer un nouveau calcul immédiat
            if (lastTimeLeft != null || lastTimeRight != null)
            {
                // On relance une mesure "fraîche"
                AcquireOnce();
            }
            else
            {
                // Sinon on efface juste le graphe
                currentLeftSpectrum = null;
                currentRightSpectrum = null;
                ClearSpectrum();
            }
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

        private void InitializeUiOptions()
        {
            // FFT size
            comboFftSize.Items.Clear();
            foreach (int n in FftSizeOptions)
                comboFftSize.Items.Add(n.ToString("N0"));

            comboFftSize.SelectedIndex = 5; // 65536
            lastFftSize = FftSizeOptions[comboFftSize.SelectedIndex];
            comboFftSize.SelectedIndexChanged += ComboFftSize_SelectedIndexChanged;

            // Window
            comboWindow.Items.Clear();
            comboWindow.Items.Add("Rectangular");
            comboWindow.Items.Add("Hann");
            comboWindow.Items.Add("Hamming");
            comboWindow.Items.Add("Blackman-Harris");
            comboWindow.SelectedIndex = 1;
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
            };

            // Channel (affichage)
            comboChannel.Items.Clear();
            comboChannel.Items.Add("Left");
            comboChannel.Items.Add("Right");
            comboChannel.Items.Add("L+R");
            comboChannel.SelectedIndex = 0;
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

        private void ComboFftSize_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_forcedFftSize.HasValue)
                return; // taille forcée par Form1

            if (comboFftSize.SelectedIndex >= 0 && comboFftSize.SelectedIndex < FftSizeOptions.Length)
            {
                lastFftSize = FftSizeOptions[comboFftSize.SelectedIndex];
                UpdateWindowInfo();
            }
        }


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

        private void AcquireOnce()
        {
            if (_provider == null || _sampleRate <= 0)
                return;

            // 1) Taille FFT
            int N;

            if (_forcedFftSize.HasValue && _forcedFftSize.Value > 0)
            {
                N = _forcedFftSize.Value;
            }
            else if (comboFftSize.SelectedIndex >= 0 && comboFftSize.SelectedIndex < FftSizeOptions.Length)
            {
                N = FftSizeOptions[comboFftSize.SelectedIndex];
            }
            else
            {
                N = 16384;
            }
            if (N < 512) N = 512;
            lastFftSize = N;

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

            // 5) Supprimer DC
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

            // 8) Axe fréquence
            RebuildFreqAxis(N);

            // On garde une copie des derniers buffers temps pour les stats
            lastTimeLeft = bufL;
            lastTimeRight = bufR;

            // 9) Mise à jour graphe + stats temporelles
            UpdateSpectrumChartAndStats(bufL, bufR);
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

        private void ApplyAveraging(double[] leftNew, double[]? rightNew)
        {
            if (avgTargetCount <= 1)
            {
                avgLeftSpectrum = null;
                avgRightSpectrum = null;
                currentLeftSpectrum = leftNew;
                currentRightSpectrum = rightNew;
                return;
            }

            double alpha = 1.0 / avgTargetCount;

            if (avgLeftSpectrum == null || avgLeftSpectrum.Length != leftNew.Length)
            {
                avgLeftSpectrum = (double[])leftNew.Clone();
            }
            else
            {
                for (int i = 0; i < leftNew.Length; i++)
                    avgLeftSpectrum[i] = (1.0 - alpha) * avgLeftSpectrum[i] + alpha * leftNew[i];
            }

            if (rightNew != null)
            {
                if (avgRightSpectrum == null || avgRightSpectrum.Length != rightNew.Length)
                    avgRightSpectrum = (double[])rightNew.Clone();
                else
                {
                    for (int i = 0; i < rightNew.Length; i++)
                        avgRightSpectrum[i] = (1.0 - alpha) * avgRightSpectrum[i] + alpha * rightNew[i];
                }
            }
            else
            {
                avgRightSpectrum = null;
            }

            currentLeftSpectrum = avgLeftSpectrum;
            currentRightSpectrum = avgRightSpectrum;
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

                area.AxisX.IsLogarithmic = true;
                area.AxisX.LogarithmBase = 10.0;

                // Major = décades, Minor désactivée (sous-divisions dessinées à la main)
                area.AxisX.MajorGrid.Enabled = true;
                area.AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Solid;
                area.AxisX.MajorGrid.Interval = 1.0;      // 1 unité log = 1 décade

                area.AxisX.MinorGrid.Enabled = false;
            }
            else
            {
                area.AxisX.IsLogarithmic = false;

                // LIN X : 5 grandes divisions + 5 sous-divisions
                double xSpan = fMax - fMin;
                double xMajor = xSpan / 5.0;
                if (xMajor <= 0) xMajor = xSpan;

                area.AxisX.MajorGrid.Enabled = true;
                area.AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Solid;
                area.AxisX.MajorGrid.Interval = xMajor;

                area.AxisX.MinorGrid.Enabled = true;
                area.AxisX.MinorGrid.Interval = xMajor / 5.0;
                area.AxisX.MinorGrid.LineDashStyle = ChartDashStyle.Dot;
                area.AxisX.MinorGrid.LineColor = Color.LightGray;
            }

            // --- Ce qu'on affiche en fonction du combo Channel ---
            string mode = comboChannel.SelectedItem as string ?? "L+R";
            bool showLeft = (mode == "Left" || mode == "L+R");
            bool showRight = (currentRightSpectrum != null) && (mode == "Right" || mode == "L+R");

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
            }

            // --- Stats temporelles : LEFT ---
            if (timeL != null && timeL.Length > 0)
            {
                double rmsL = 0.0;
                double peakL = 0.0;
                for (int i = 0; i < timeL.Length; i++)
                {
                    double x = timeL[i] / (double)Max24Pos;
                    rmsL += x * x;
                    double ax = Math.Abs(x);
                    if (ax > peakL) peakL = ax;
                }
                rmsL = Math.Sqrt(rmsL / timeL.Length);
                double rmsLDb = rmsL > 0 ? 20.0 * Math.Log10(rmsL) : -200.0;
                double peakLDb = peakL > 0 ? 20.0 * Math.Log10(peakL) : -200.0;

                if (rmsLDb < -200.0) rmsLDb = -200.0;
                if (peakLDb < -200.0) peakLDb = -200.0;

                lblLeftRms.Text = $"RMS:  {rmsLDb:F1} dBFS";
                lblLeftPeak.Text = $"Peak: {peakLDb:F1} dBFS";
            }
            else
            {
                lblLeftRms.Text = "RMS:";
                lblLeftPeak.Text = "Peak:";
            }

            // --- Stats temporelles : RIGHT ---
            if (timeR != null && timeR.Length > 0)
            {
                double rmsR = 0.0;
                double peakR = 0.0;
                for (int i = 0; i < timeR.Length; i++)
                {
                    double x = timeR[i] / (double)Max24Pos;
                    rmsR += x * x;
                    double ax = Math.Abs(x);
                    if (ax > peakR) peakR = ax;
                }
                rmsR = Math.Sqrt(rmsR / timeR.Length);
                double rmsRDb = rmsR > 0 ? 20.0 * Math.Log10(rmsR) : -200.0;
                double peakRDb = peakR > 0 ? 20.0 * Math.Log10(peakR) : -200.0;

                if (rmsRDb < -200.0) rmsRDb = -200.0;
                if (peakRDb < -200.0) peakRDb = -200.0;

                lblRightRms.Text = $"RMS:  {rmsRDb:F1} dBFS";
                lblRightPeak.Text = $"Peak: {peakRDb:F1} dBFS";
            }
            else
            {
                lblRightRms.Text = "";
                lblRightPeak.Text = "";
            }
        }


        private void ChartSpectrum_PostPaint(object sender, ChartPaintEventArgs e)
        {
            var area = chartSpectrum.ChartAreas["SpectrumArea"];
            if (e.ChartElement != area)
                return;

            bool useLog = chkLogX != null && chkLogX.Checked;
            if (!useLog)
                return;

            double fMin = area.AxisX.Minimum;
            double fMax = area.AxisX.Maximum;

            // Sécurité : en log, on ne dessine rien si bornes invalides
            if (fMin <= 0 || fMax <= fMin)
                return;

            // Lignes mineures log dessinées à la main : 2..9 * 10^n
            using (Pen minorPen = new Pen(Color.FromArgb(80, Color.Gray), 1f))
            {
                minorPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;

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
                        float top = (float)area.AxisY.ValueToPixelPosition(area.AxisY.Maximum);
                        float bottom = (float)area.AxisY.ValueToPixelPosition(area.AxisY.Minimum);

                        e.ChartGraphics.Graphics.DrawLine(minorPen, xPixel, top, xPixel, bottom);
                    }
                }
            }
        }


        //private void ClearSpectrum()
        //{
        //    foreach (var s in chartSpectrum.Series)
        //        s.Points.Clear();

        //    lblLeftRms.Text = "RMS:";
        //    lblLeftPeak.Text = "Peak:";
        //    lblRightRms.Text = "";
        //    lblRightPeak.Text = "";
        //}
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
        }


        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}
