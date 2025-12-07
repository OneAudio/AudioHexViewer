using System;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace AudioHexViewer
{
    public partial class SpectrumControl : UserControl
    {
        public delegate (int[] Left, int[] Right) SampleProvider(int durationMs);

        private readonly SampleProvider? _provider;
        private int _sampleRate;
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

        public SpectrumControl()
        {
            InitializeComponent();
            InitChartAppearance();
            InitializeUiOptions();
            UpdateRunButtonAppearance();
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

        private void InitChartAppearance()
        {
            var areas = chartSpectrum.ChartAreas;
            if (areas.Count == 0)
            {
                var area = new System.Windows.Forms.DataVisualization.Charting.ChartArea("SpectrumArea");
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
            comboFftSize.SelectedIndexChanged += (s, e) =>
            {
                lastFftSize = FftSizeOptions[comboFftSize.SelectedIndex];
                UpdateWindowInfo();
            };

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
                UpdateSpectrumChartAndStats(null, null);
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
                    int intervalMs = (int)Math.Max(
                        10,
                        Math.Round((double)lastFftSize * 1000.0 / _sampleRate)
                    );
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
            if (comboFftSize.SelectedIndex >= 0 && comboFftSize.SelectedIndex < FftSizeOptions.Length)
                N = FftSizeOptions[comboFftSize.SelectedIndex];
            else
                N = 16384;

            if (N < 512) N = 512;
            lastFftSize = N;

            // 2) Durée correspondant à N échantillons
            double msExact = (double)N * 1000.0 / _sampleRate;
            int durationMs = (int)Math.Max(10, Math.Round(msExact));

            // 3) Récupérer L/R
            var (left, right) = _provider(durationMs);
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
            area.AxisX.IsLogarithmic = useLog;

            double fMax = _sampleRate > 0 ? _sampleRate / 2.0 : 1.0;
            double fMin = useLog ? 1.0 : 0.0;   // en log, pas de 0 Hz

            area.AxisX.Minimum = fMin;
            area.AxisX.Maximum = fMax;

            area.AxisY.Minimum = -200.0;
            area.AxisY.Maximum = 0.0;

            // --- Ce qu'on affiche en fonction du combo Channel ---
            string mode = comboChannel.SelectedItem as string ?? "L+R";
            bool showLeft = (mode == "Left" || mode == "L+R");
            bool showRight = (currentRightSpectrum != null) && (mode == "Right" || mode == "L+R");

            // En log, on évite le bin 0
            int startIndex = useLog ? 1 : 0;

            // Courbe gauche
            if (showLeft)
            {
                var seriesL = new System.Windows.Forms.DataVisualization.Charting.Series("Left");
                seriesL.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;
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
                seriesR.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;
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


        private void ClearSpectrum()
        {
            foreach (var s in chartSpectrum.Series)
                s.Points.Clear();

            lblLeftRms.Text = "RMS:";
            lblLeftPeak.Text = "Peak:";
            lblRightRms.Text = "";
            lblRightPeak.Text = "";
        }
    }
}
