using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace AudioHexViewer
{
    public partial class SpectrumAxisDialog : Form
    {
        public SpectrumAxisDialog()
        {
            InitializeComponent();
            btnDefault.Click += BtnDefault_Click;
            btnFitY.Click += BtnFitY_Click;
            btnFitX.Click += BtnFitX_Click;

        }
        //
        private static readonly double[] AllowedFmaxCaps =
        {
            25_000,
            50_000,
            100_000,
            200_000,
            400_000
        };

        // Sample rate courant (Hz), fourni par SpectrumControl
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public double SampleRateHz { get; set; } = 0.0;

        // Taille FFT N (nb points). Fournie par SpectrumControl.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public int FftSizeN { get; set; } = 0;

        // Callback pour récupérer min/max Y (dBFS) des données courantes (2 voies)
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public Func<(double yMin, double yMax)>? FitYProvider { get; set; }


        // Y max (dBFS) – lecture/écriture, non sérialisé par le designer
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public double YMax
        {
            get => (double)numYMax.Value;
            set
            {
                if ((decimal)value < numYMax.Minimum) value = (double)numYMax.Minimum;
                if ((decimal)value > numYMax.Maximum) value = (double)numYMax.Maximum;
                numYMax.Value = (decimal)value;
            }
        }

        // Y min (dBFS)
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public double YMin
        {
            get => (double)numYMin.Value;
            set
            {
                if ((decimal)value < numYMin.Minimum) value = (double)numYMin.Minimum;
                if ((decimal)value > numYMin.Maximum) value = (double)numYMin.Maximum;
                numYMin.Value = (decimal)value;
            }
        }

        // F min en Hz
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public double FMinHz
        {
            get => (double)numFMin.Value;
            set
            {
                if ((decimal)value < numFMin.Minimum) value = (double)numFMin.Minimum;
                if ((decimal)value > numFMin.Maximum) value = (double)numFMin.Maximum;
                numFMin.Value = (decimal)value;
            }
        }
        //
        // F max en Hz
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public double FMaxHz
        {
            get => (double)numFMax.Value;
            set
            {
                if ((decimal)value < numFMax.Minimum) value = (double)numFMax.Minimum;
                if ((decimal)value > numFMax.Maximum) value = (double)numFMax.Maximum;
                numFMax.Value = (decimal)value;
            }
        }
        //
        public static double GetAllowedFmax(double sampleRateHz)
        {
            double nyquist = sampleRateHz / 2.0;

            foreach (double cap in AllowedFmaxCaps)
            {
                if (cap >= nyquist)
                    return cap;
            }

            // Sécurité ultime
            return AllowedFmaxCaps.Last();
        }
        //
        public void SetFmaxMaximum(double maxHz)
        {
            numFMax.Maximum = (decimal)maxHz;

            if (numFMax.Value > numFMax.Maximum)
                numFMax.Value = numFMax.Maximum;
        }

        //
        private void BtnDefault_Click(object? sender, EventArgs e)
        {
            // X = 1 Hz .. Fs/2
            double fs = SampleRateHz;
            double fMin = 1.0;
            double fMax = (fs > 0.0) ? (fs / 2.0) : (double)numFMax.Maximum;

            // Clamp aux bornes des NumericUpDown
            if (fMin < (double)numFMin.Minimum) fMin = (double)numFMin.Minimum;
            if (fMin > (double)numFMin.Maximum) fMin = (double)numFMin.Maximum;

            if (fMax < (double)numFMax.Minimum) fMax = (double)numFMax.Minimum;
            if (fMax > (double)numFMax.Maximum) fMax = (double)numFMax.Maximum;

            // Y = 0 .. -200 dBFS
            double yMax = 0.0;
            double yMin = -200.0;

            if (yMax < (double)numYMax.Minimum) yMax = (double)numYMax.Minimum;
            if (yMax > (double)numYMax.Maximum) yMax = (double)numYMax.Maximum;

            if (yMin < (double)numYMin.Minimum) yMin = (double)numYMin.Minimum;
            if (yMin > (double)numYMin.Maximum) yMin = (double)numYMin.Maximum;

            // Appliquer
            numFMin.Value = (decimal)fMin;
            numFMax.Value = (decimal)fMax;
            numYMax.Value = (decimal)yMax;
            numYMin.Value = (decimal)yMin;
        }

        private void BtnFitY_Click(object? sender, EventArgs e)
        {
            if (FitYProvider == null)
            {
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            var (yMinData, yMaxData) = FitYProvider();

            // Si pas de données exploitables
            if (double.IsNaN(yMinData) || double.IsNaN(yMaxData) ||
                double.IsInfinity(yMinData) || double.IsInfinity(yMaxData))
            {
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            // Clamp dans [-200 ; 0]
            yMaxData = Math.Min(0.0, yMaxData);
            yMinData = Math.Max(-200.0, yMinData);

            // Arrondi à la dizaine :
            // max vers le haut, min vers le bas
            double yMax = Math.Ceiling(yMaxData / 10.0) * 10.0;
            double yMin = Math.Floor(yMinData / 10.0) * 10.0;

            // Clamp encore
            yMax = Math.Min(0.0, yMax);
            yMin = Math.Max(-200.0, yMin);

            // Échelle minimale 20 dB
            if ((yMax - yMin) < 20.0)
            {
                yMin = yMax - 20.0;
                if (yMin < -200.0)
                {
                    yMin = -200.0;
                    yMax = yMin + 20.0;
                }
                if (yMax > 0.0)
                {
                    yMax = 0.0;
                    yMin = yMax - 20.0;
                    if (yMin < -200.0) yMin = -200.0;
                }
            }

            // Clamp aux bornes NumericUpDown
            if (yMax < (double)numYMax.Minimum) yMax = (double)numYMax.Minimum;
            if (yMax > (double)numYMax.Maximum) yMax = (double)numYMax.Maximum;

            if (yMin < (double)numYMin.Minimum) yMin = (double)numYMin.Minimum;
            if (yMin > (double)numYMin.Maximum) yMin = (double)numYMin.Maximum;

            // Appliquer Y uniquement (X inchangé)
            numYMax.Value = (decimal)yMax;
            numYMin.Value = (decimal)yMin;
        }
        //
        //
        private void BtnFitX_Click(object? sender, EventArgs e)
        {
            double fs = SampleRateHz;
            int n = FftSizeN;

            if (fs <= 0.0 || n <= 0)
            {
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            // Nyquist
            double fMax = fs / 2.0;

            // Résolution fréquentielle de la FFT
            double fMin = fs / n;

            // Sécurité si jamais N trop grand / fs trop petit
            if (double.IsNaN(fMin) || double.IsInfinity(fMin) || fMin <= 0.0 || fMin >= fMax)
            {
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            // Clamp aux bornes NumericUpDown
            if (fMin < (double)numFMin.Minimum) fMin = (double)numFMin.Minimum;
            if (fMin > (double)numFMin.Maximum) fMin = (double)numFMin.Maximum;

            if (fMax < (double)numFMax.Minimum) fMax = (double)numFMax.Minimum;
            if (fMax > (double)numFMax.Maximum) fMax = (double)numFMax.Maximum;

            // Garantir fMin < fMax après clamp
            if (fMin >= fMax)
            {
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            // Appliquer X uniquement (Y inchangé)
            numFMin.Value = (decimal)fMin;
            numFMax.Value = (decimal)fMax;
        }


    }
}
