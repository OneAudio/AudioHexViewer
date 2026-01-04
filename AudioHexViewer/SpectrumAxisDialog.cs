using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace AudioHexViewer
{
    public partial class SpectrumAxisDialog : Form
    {
        private bool _updating = false;

        public SpectrumAxisDialog()
        {
            InitializeComponent();

            btnDefault.Click += BtnDefault_Click;
            btnFitY.Click += BtnFitY_Click;
            btnFitX.Click += BtnFitX_Click;

            // synchro X
            numFMin.ValueChanged += (_, __) => { if (!_updating) SyncFromMinMax(); };
            numFMax.ValueChanged += (_, __) => { if (!_updating) SyncFromMinMax(); };
            numCenter.ValueChanged += (_, __) => { if (!_updating) SyncFromCenterSpan(); };
            numSpan.ValueChanged += (_, __) => { if (!_updating) SyncFromCenterSpan(); };

            tabX.SelectedIndexChanged += (_, __) =>
            {
                // juste remettre des valeurs cohérentes à l’affichage
                if (tabX.SelectedTab == tabCenterSpan)
                    UpdateCenterSpanFromMinMax();
                else
                    SyncFromCenterSpan(); // pour remettre Min/Max cohérents si Center/Span a bougé
            };
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UpdateFrequencyRanges();
            UpdateCenterSpanFromMinMax(); // initialise center/span depuis les valeurs actuelles min/max
        }

        // caps UI
        private static readonly double[] AllowedFmaxCaps =
        {
            25_000,
            50_000,
            100_000,
            200_000,
            400_000
        };

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public double SampleRateHz { get; set; } = 0.0;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public int FftSizeN { get; set; } = 0;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public Func<(double yMin, double yMax)>? FitYProvider { get; set; }

        // ---------- Public API conservée (compat avec ton code actuel) ----------
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

        // ---------- Helpers fréquences ----------
        private double Nyq => (SampleRateHz > 0.0) ? (SampleRateHz / 2.0) : 0.0;
        private double Df => (SampleRateHz > 0.0 && FftSizeN > 0) ? (SampleRateHz / FftSizeN) : 0.0;

        // règles demandées
        private double CenterMin => Df > 0 ? Df : 0.0;                   // Fs/N
        private double CenterMax => Df > 0 ? Math.Max(Df, Nyq - Df) : 0;  // < Fs/2

        private double SpanMin => Df > 0 ? 2.0 * Df : 0.0;               // 2*Fs/N
        private double SpanMaxGlobal => Df > 0 ? Math.Max(2.0 * Df, Nyq - Df) : 0.0; // Fs/2 - Fs/N

        public static double GetAllowedFmax(double sampleRateHz)
        {
            double nyquist = sampleRateHz / 2.0;
            foreach (double cap in AllowedFmaxCaps)
                if (cap >= nyquist) return cap;

            return AllowedFmaxCaps.Last();
        }

        public void SetFmaxMaximum(double maxHz)
        {
            numFMax.Maximum = (decimal)maxHz;
            numFMin.Maximum = (decimal)maxHz;

            if (numFMax.Value > numFMax.Maximum)
                numFMax.Value = numFMax.Maximum;

            if (numFMin.Value > numFMin.Maximum)
                numFMin.Value = numFMin.Maximum;

            UpdateFrequencyRanges();
        }

        private void UpdateFrequencyRanges()
        {
            // Si pas d'info fs/N, on ne touche pas trop
            if (SampleRateHz <= 0.0 || FftSizeN <= 0)
                return;

            _updating = true;
            try
            {
                // bornes min/max (min accepte 0)
                numFMin.Minimum = 0;
                numFMax.Minimum = 0;

                // Center/Span selon tes règles
                numCenter.Minimum = (decimal)CenterMin;
                numCenter.Maximum = (decimal)CenterMax;

                numSpan.Minimum = (decimal)SpanMin;
                numSpan.Maximum = (decimal)SpanMaxGlobal;

                // defaults si vide
                if (numCenter.Value <= 0)
                    numCenter.Value = (decimal)Math.Max(CenterMin, SampleRateHz / 4.0);

                if (numSpan.Value <= 0)
                {
                    double d = Math.Min(SpanMaxGlobal, Nyq); // max “pratique”
                    numSpan.Value = (decimal)Math.Max(SpanMin, d);
                }
            }
            finally
            {
                _updating = false;
            }
        }

        // --------- Synchro X (Min/Max <-> Center/Span) ---------

        private void UpdateCenterSpanFromMinMax()
        {
            if (SampleRateHz <= 0.0 || FftSizeN <= 0)
            {
                // fallback : simple calcul
                double fmin0 = (double)numFMin.Value;
                double fmax0 = (double)numFMax.Value;
                double c0 = (fmin0 + fmax0) / 2.0;
                double s0 = Math.Max(0.0, fmax0 - fmin0);

                _updating = true;
                try
                {
                    if (c0 > 0) numCenter.Value = (decimal)c0;
                    if (s0 > 0) numSpan.Value = (decimal)s0;
                }
                finally { _updating = false; }

                return;
            }

            _updating = true;
            try
            {
                double fmin = (double)numFMin.Value;
                double fmax = (double)numFMax.Value;

                // clamp simple
                fmin = Math.Max(0.0, fmin);
                fmax = Math.Min(Math.Min((double)numFMax.Maximum, Nyq), fmax);
                if (fmax <= fmin) fmax = fmin + Math.Max(Df, 1.0);

                double center = (fmin + fmax) / 2.0;
                double span = fmax - fmin;

                // clamp center + span (center prioritaire, span borné)
                center = Clamp(center, CenterMin, CenterMax);
                span = ClampSpanForCenter(span, center);

                numCenter.Value = (decimal)center;
                numSpan.Value = (decimal)span;
            }
            finally
            {
                _updating = false;
            }
        }

        private void SyncFromMinMax()
        {
            if (SampleRateHz <= 0.0 || FftSizeN <= 0)
                return;

            _updating = true;
            try
            {
                double fmin = (double)numFMin.Value;
                double fmax = (double)numFMax.Value;

                // clamp dans [0 ; min(nyq, cap)]
                double fmaxHard = Math.Min((double)numFMax.Maximum, Nyq);

                fmin = Math.Max(0.0, fmin);
                fmax = Math.Min(fmaxHard, fmax);

                // span min : 2*df
                double minSpan = SpanMin;
                if ((fmax - fmin) < minSpan)
                {
                    // on essaie d'étendre fmax d'abord
                    double target = fmin + minSpan;
                    if (target <= fmaxHard) fmax = target;
                    else
                    {
                        // sinon on recule fmin
                        fmax = fmaxHard;
                        fmin = Math.Max(0.0, fmax - minSpan);
                    }
                }

                // compute center/span puis applique règles (center prioritaire => ici center dérivé)
                double center = (fmin + fmax) / 2.0;
                double span = fmax - fmin;

                center = Clamp(center, CenterMin, CenterMax);
                span = ClampSpanForCenter(span, center);

                // reconstruit min/max cohérents
                double newFmin = center - span / 2.0;
                double newFmax = center + span / 2.0;

                // borne finales
                if (newFmin < 0.0)
                {
                    newFmax -= newFmin;
                    newFmin = 0.0;
                }
                if (newFmax > fmaxHard)
                {
                    double over = newFmax - fmaxHard;
                    newFmin -= over;
                    newFmax = fmaxHard;
                    if (newFmin < 0.0) newFmin = 0.0;
                }

                numFMin.Value = (decimal)newFmin;
                numFMax.Value = (decimal)newFmax;

                numCenter.Value = (decimal)center;
                numSpan.Value = (decimal)span;
            }
            finally
            {
                _updating = false;
            }
        }

        private void SyncFromCenterSpan()
        {
            if (SampleRateHz <= 0.0 || FftSizeN <= 0)
                return;

            _updating = true;
            try
            {
                double fmaxHard = Math.Min((double)numFMax.Maximum, Nyq);

                double center = (double)numCenter.Value;
                double span = (double)numSpan.Value;

                // clamp center
                center = Clamp(center, CenterMin, CenterMax);

                // clamp span selon règles + centre (centre prioritaire)
                span = ClampSpanForCenter(span, center);

                double fmin = center - span / 2.0;
                double fmax = center + span / 2.0;

                // borne [0 ; fmaxHard] (centre prioritaire => on réduit span si besoin)
                if (fmin < 0.0 || fmax > fmaxHard)
                {
                    double maxSpanByBounds = 2.0 * Math.Min(center - 0.0, fmaxHard - center);
                    double maxSpan = Math.Min(SpanMaxGlobal, maxSpanByBounds);
                    span = Clamp(span, SpanMin, maxSpan);

                    fmin = center - span / 2.0;
                    fmax = center + span / 2.0;

                    if (fmin < 0.0) fmin = 0.0;
                    if (fmax > fmaxHard) fmax = fmaxHard;
                }

                numCenter.Value = (decimal)center;
                numSpan.Value = (decimal)span;
                numFMin.Value = (decimal)fmin;
                numFMax.Value = (decimal)fmax;
            }
            finally
            {
                _updating = false;
            }
        }

        private double ClampSpanForCenter(double span, double center)
        {
            double fmaxHard = Math.Min((double)numFMax.Maximum, Nyq);

            double maxSpanByBounds = 2.0 * Math.Min(center - 0.0, fmaxHard - center);
            double maxSpan = Math.Min(SpanMaxGlobal, maxSpanByBounds);

            // si maxSpan devient trop petit, on force au minimum possible
            if (maxSpan < SpanMin)
                maxSpan = SpanMin;

            return Clamp(span, SpanMin, maxSpan);
        }

        private static double Clamp(double v, double lo, double hi)
        {
            if (v < lo) return lo;
            if (v > hi) return hi;
            return v;
        }

        // --------- Boutons existants (inchangés côté user) ---------

        private void BtnDefault_Click(object? sender, EventArgs e)
        {
            // X = 1 Hz .. Fs/2 (comme avant)
            double fs = SampleRateHz;
            double fMin = 1.0;
            double fMax = (fs > 0.0) ? (fs / 2.0) : (double)numFMax.Maximum;

            // Clamp
            fMin = Math.Max((double)numFMin.Minimum, Math.Min((double)numFMin.Maximum, fMin));
            fMax = Math.Max((double)numFMax.Minimum, Math.Min((double)numFMax.Maximum, fMax));

            // Y = 0 .. -200 dBFS
            double yMax = 0.0;
            double yMin = -200.0;

            yMax = Math.Max((double)numYMax.Minimum, Math.Min((double)numYMax.Maximum, yMax));
            yMin = Math.Max((double)numYMin.Minimum, Math.Min((double)numYMin.Maximum, yMin));

            _updating = true;
            try
            {
                numFMin.Value = (decimal)fMin;
                numFMax.Value = (decimal)fMax;
                numYMax.Value = (decimal)yMax;
                numYMin.Value = (decimal)yMin;
            }
            finally { _updating = false; }

            // synchro center/span
            if (SampleRateHz > 0.0 && FftSizeN > 0)
                SyncFromMinMax();
        }

        private void BtnFitY_Click(object? sender, EventArgs e)
        {
            if (FitYProvider == null)
            {
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            var (yMinData, yMaxData) = FitYProvider();

            if (double.IsNaN(yMinData) || double.IsNaN(yMaxData) ||
                double.IsInfinity(yMinData) || double.IsInfinity(yMaxData))
            {
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            // Clamp to numeric bounds (supports dBFS and dBV)
            double yMaxAllowed = (double)numYMax.Maximum;
            double yMinAllowed = (double)numYMin.Minimum;

            // Guard: ensure we have a sane range from the provider
            if (yMaxData < yMinData)
            {
                var tmp = yMaxData;
                yMaxData = yMinData;
                yMinData = tmp;
            }

            // Pre-clamp to allowed bounds
            yMaxData = Math.Min(yMaxAllowed, yMaxData);
            yMinData = Math.Max(yMinAllowed, yMinData);

            // Round to nice 10 dB steps
            double yMax = Math.Ceiling(yMaxData / 10.0) * 10.0;
            double yMin = Math.Floor(yMinData / 10.0) * 10.0;

            // Final clamp after rounding
            yMax = Math.Min(yMaxAllowed, yMax);
            yMin = Math.Max(yMinAllowed, yMin);

            if ((yMax - yMin) < 20.0)
            {
                yMin = yMax - 20.0;

                if (yMin < yMinAllowed) { yMin = yMinAllowed; yMax = yMin + 20.0; }
                if (yMax > yMaxAllowed)
                {
                    yMax = yMaxAllowed;
                    yMin = yMax - 20.0;
                    if (yMin < yMinAllowed) yMin = yMinAllowed;
                }
            }

            yMax = Math.Max((double)numYMax.Minimum, Math.Min((double)numYMax.Maximum, yMax));
            yMin = Math.Max((double)numYMin.Minimum, Math.Min((double)numYMin.Maximum, yMin));

            numYMax.Value = (decimal)yMax;
            numYMin.Value = (decimal)yMin;
        }

        private void BtnFitX_Click(object? sender, EventArgs e)
        {
            double fs = SampleRateHz;
            int n = FftSizeN;

            if (fs <= 0.0 || n <= 0)
            {
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            double fMax = fs / 2.0;
            double fMin = fs / n;

            if (double.IsNaN(fMin) || double.IsInfinity(fMin) || fMin <= 0.0 || fMin >= fMax)
            {
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            fMin = Math.Max((double)numFMin.Minimum, Math.Min((double)numFMin.Maximum, fMin));
            fMax = Math.Max((double)numFMax.Minimum, Math.Min((double)numFMax.Maximum, fMax));

            if (fMin >= fMax)
            {
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            _updating = true;
            try
            {
                numFMin.Value = (decimal)fMin;
                numFMax.Value = (decimal)fMax;
            }
            finally { _updating = false; }

            SyncFromMinMax();
        }
    }
}
