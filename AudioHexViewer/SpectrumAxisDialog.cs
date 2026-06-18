using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using System.Globalization;


namespace AudioHexViewer
{
    public partial class SpectrumAxisDialog : Form
    {
        private bool _updating = false;

        private const double PHASE_ABS_MAX_DEG = 1800.0; // max absolu (unwrapped); wrapped limité par NUD bounds

        private static readonly int[] AllowedY2RangesDeg = new[] { 10, 20, 40, 80, 100, 120,180 ,360 };

        private static bool TryParseDeg(string? s, out double deg)
        {
            deg = 0.0;
            if (string.IsNullOrWhiteSpace(s)) return false;
            return double.TryParse(s, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out deg);
        }


        public SpectrumAxisDialog()
        {
            InitializeComponent();

            // Close dialog on Escape (same behavior as Cancel)
            KeyPreview = true;
            KeyDown += SpectrumAxisDialog_KeyDown;

            // --- Main Y axis (Y Max + Y Range libre)
            numYRange.Minimum = 1; numYRange.Maximum = 400; numYRange.Increment = 5;

            // Events
            numYMax.ValueChanged   += (_, __) => { if (!_updating) ValidateAndApplyMainY(); };
            numYMax.Validated      += (_, __) => { if (!_updating) ValidateAndApplyMainY(); };
            numYRange.ValueChanged += (_, __) => { if (!_updating) ValidateAndApplyMainY(); };

            cmbVertUnit.SelectedIndexChanged += (_, __) => { if (!_updating) UpdateVertUnitLabels(); };
            // Sélection initiale
            if (cmbVertUnit.Items.Count > 0) cmbVertUnit.SelectedIndex = 0;

            // Default scale (caller will overwrite MainYScale before showing the dialog)
            ApplyMainYScaleToUi();

            // --- Phase Y2 axis (Y2 Max + Y2 Range) ---
            cmbY2Range.DropDownStyle = ComboBoxStyle.DropDownList;

            cmbY2Range.Items.Clear();
            cmbY2Range.Items.AddRange(AllowedY2RangesDeg.Select(v => v.ToString(CultureInfo.InvariantCulture))
                                                       .Cast<object>()
                                                       .ToArray());

            // Events: keep Y2 Max rounded/clamped and keep the range list consistent.
            numY2Max.ValueChanged += (_, __) => { if (!_updating) ValidateAndApplyY2(); };
            numY2Max.Validated += (_, __) => { if (!_updating) ValidateAndApplyY2(); };

            cmbY2Range.SelectedIndexChanged += (_, __) => { if (!_updating) ValidateAndApplyY2(); };
            btnDefRange.Click += (_, __) =>
            {
                double yMaxDef = (_yScaleMode == YScaleMode.DbGain) ? 20.0 : 0.0;
                YMax = yMaxDef;
                SetYRangeDb(200);
                ValidateAndApplyMainY(); // inclut PreviewChanged
            };
            btnDefPhaseRange.Click += (_, __) => SetY2RangeCombo(_isUnwrappedPhase ? 3600 : 360);

            // Defaults
            _updating = true;
            try
            {
                numY2Max.Minimum = -180m;
                numY2Max.Maximum = +180m;  // limites étendues par ApplyPhaseRangeLimits si Unwrapped
                numY2Max.Increment = 10;
                numY2Max.DecimalPlaces = 0;
                numY2Max.Value = 180;

                cmbY2Range.SelectedItem = "360"; // sera refiltré juste après
            }
            finally { _updating = false; }

            ValidateAndApplyY2();


            btnDefault.Click += BtnDefault_Click;
            btnFitY.Click += BtnFitY_Click;
            btnFitY2.Click += BtnFitY2_Click;

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

        // -------------------- Y Axis (main) --------------------
        // UI requirement:
        // - User selects Y Max (dB) on a 5 dB grid (editable combobox).
        // - User selects Y Range (dB) from a fixed list (DropDownList).
        // - YMin is derived: YMin = YMax - YRange.
        // - Allowed YMax bounds depend on the display unit (dBFS / dBV / Gain(dB)).
        //
        public enum YScaleMode
        {
            Dbfs,   // Level (dBFS) : YMax in [ -195 ; 0 ]
            Dbv,    // Level (dBV)  : YMax in [ -195 ; +100 ]
            DbGain  // Gain (dB)    : YMax in [ -195 ; +100 ]
        }


        private const double YMIN_HARD = -195.0;
        private const double YMAX_DBFS_HARD = 0.0;
        private const double YMAX_DBV_HARD = 100.0;

        private YScaleMode _yScaleMode = YScaleMode.Dbfs;

        private double _yMaxAllowed = YMAX_DBFS_HARD;
        private double _yMinAllowed = YMIN_HARD;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public YScaleMode MainYScale
        {
            get => _yScaleMode;
            set
            {
                _yScaleMode = value;
                ApplyMainYScaleToUi();
            }
        }


        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UpdateFrequencyRanges();
            UpdateCenterSpanFromMinMax(); // initialise center/span depuis les valeurs actuelles min/max
        }

        // caps UI
        private static readonly double[] AllowedFmaxCaps =
            { 25_000, 50_000, 100_000, 200_000, 400_000 };

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public double SampleRateHz { get; set; } = 0.0;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public int FftSizeN { get; set; } = 0;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        // Mode axe X : true=LOG, false=LIN (influence le Fmin par défaut)
        public bool IsLogX { get; set; } = false;

        // Mode phase : true=Unwrapped (±1800°/3600°), false=Wrapped (±180°/360°)
        private bool _isUnwrappedPhase = false;
        public bool IsUnwrappedPhase
        {
            get => _isUnwrappedPhase;
            set { _isUnwrappedPhase = value; ApplyPhaseRangeLimits(); }
        }

        private void ApplyPhaseRangeLimits()
        {
            if (_isUnwrappedPhase)
            {
                // Unwrapped : ±1800°, range jusqu'à 3600°
                numY2Max.Minimum = -1800; numY2Max.Maximum = 1800;
                numY2Max.Increment = 180;
                // Agrandir la liste Y2 Range
                int cur2 = (int)(cmbY2Range.SelectedItem is string s2 &&
                    int.TryParse(s2, out int r2) ? r2 : 360);
                cmbY2Range.Items.Clear();
                foreach (var v in new[] {90,180,360,720,1440,3600})
                    cmbY2Range.Items.Add(v.ToString());
                SetY2RangeCombo(cur2 <= 360 ? 360 : cur2);
            }
            else
            {
                // Wrapped : ±180°, range jusqu'à 360°
                numY2Max.Minimum = -180; numY2Max.Maximum = 180;
                numY2Max.Increment = 15;
                if ((double)numY2Max.Value > 180)  numY2Max.Value = 180;
                if ((double)numY2Max.Value < -180) numY2Max.Value = -180;
                cmbY2Range.Items.Clear();
                foreach (var v in new[] {30,45,90,180,360})
                    cmbY2Range.Items.Add(v.ToString());
                SetY2RangeCombo(360);
            }
        }

        private void SetY2RangeCombo(int degrees)
        {
            string target = degrees.ToString();
            for (int i = 0; i < cmbY2Range.Items.Count; i++)
                if (cmbY2Range.Items[i]?.ToString() == target)
                { cmbY2Range.SelectedIndex = i; return; }
            if (cmbY2Range.Items.Count > 0)
                cmbY2Range.SelectedIndex = cmbY2Range.Items.Count - 1;
        }

        public Func<(double yMin, double yMax)>? FitYProvider { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public Func<(double y2Min, double y2Max)>? FitY2Provider { get; set; }

        // Persistance de l'onglet X sélectionné (0=Mini/Maxi, 1=Center/Span)
        public int XTabSelectedIndex
        {
            get => tabX?.SelectedIndex ?? 0;
            set
            {
                if (tabX != null && value >= 0 && value < tabX.TabCount)
                    tabX.SelectedIndex = value;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public Action? PreviewChanged { get; set; }


        // ── Vertical Unit (dBFS / dBV) ──────────────────────────────────────
        // Initialisé par l'appelant avant ShowDialog, lu après DialogResult.OK.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public string VerticalUnit
        {
            get => cmbVertUnit.SelectedItem as string ?? "dBFS";
            set
            {
                int idx = cmbVertUnit.Items.IndexOf(value);
                cmbVertUnit.SelectedIndex = (idx >= 0) ? idx : 0;
                // Mettre à jour le libellé "Range (dBFS/dBV)"
                UpdateVertUnitLabels();
            }
        }

        private void UpdateVertUnitLabels()
        {
            string unit = VerticalUnit;
            if (lblYMin != null) lblYMin.Text = $"Range ({unit})";
            if (lblYMax != null) lblYMax.Text = _yScaleMode == YScaleMode.DbGain
                ? "Gain Top (dB)"
                : $"Level Top ({unit})";

            // Adapter les bornes de numYMax selon l'unité choisie.
            // dBFS : max = 0 (impossible au-dessus de 0 dBFS)
            // dBV  : max = +100 (signaux > 1 V → dBV positif possible)
            if (_yScaleMode != YScaleMode.DbGain)
            {
                double newMax = (unit == "dBV") ? YMAX_DBV_HARD : YMAX_DBFS_HARD;
                if (Math.Abs(_yMaxAllowed - newMax) > 1e-9)
                {
                    _yMaxAllowed = newMax;
                    _updating = true;
                    try
                    {
                        numYMax.Maximum = (decimal)_yMaxAllowed;
                        // Re-clamp current value if it was at the old limit
                        double cur = Clamp((double)numYMax.Value, _yMinAllowed, _yMaxAllowed);
                        numYMax.Value = (decimal)cur;
                    }
                    finally { _updating = false; }
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public double YMax
        {
            get => (double)numYMax.Value;
            set
            {
                // Clamp to unit bounds (no forced rounding)
                double v = Clamp(value, _yMinAllowed, _yMaxAllowed);

                _updating = true;
                try
                {
                    numYMax.Value = (decimal)v;
                }
                finally { _updating = false; }

                ValidateAndApplyMainY();
            }
        }



        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool PhaseRangeEnabled
        {
            get => numY2Max.Enabled && cmbY2Range.Enabled;
            set
            {
                numY2Max.Enabled = value;
                cmbY2Range.Enabled = value;
                lblY2Min.Enabled = value;
                lblY2Max.Enabled = value;
            }
        }


        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public double PhaseMaxDeg
        {
            get => (double)numY2Max.Value;
            set
            {
                double v = RoundUpToStep(value, 5.0);
                double absMax1 = _isUnwrappedPhase ? 1800.0 : 180.0;
                v = Clamp(v, -absMax1, +absMax1);

                _updating = true;
                try { numY2Max.Value = (decimal)v; }
                finally { _updating = false; }

                ValidateAndApplyY2();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public double PhaseMinDeg
        {
            get
            {
                double y2Max = PhaseMaxDeg;
                int range = GetSelectedY2RangeDeg(fallback: 360);
                return y2Max - range;
            }
            set
            {
                // Derive range from desired min (same concept as Y main axis)
                double y2Max = PhaseMaxDeg;
                double desiredRange = y2Max - value;
                SetY2RangeFromDesired(desiredRange);
                ValidateAndApplyY2();
            }
        }

        private static double ClampPhase(double v)
        {
            if (v >  1800.0) return  1800.0;
            if (v < -1800.0) return -1800.0;
            return v;
        }

        private double GetPhaseComboValue(ComboBox cmb, double fallback)
        {
            if (TryParseDeg(cmb.Text, out var v))
                return ClampPhase(v);

            return fallback;
        }

        private void SetPhaseComboValue(ComboBox cmb, double value)
        {
            value = ClampPhase(value);
            cmb.Text = value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        }


        private static double RoundUpToStep(double value, double step)
        {
            // Requirement: round to the upper multiple of 5 dB.
            // Example: -83 -> -80, +12 -> +15, -80 -> -80.
            if (step <= 0.0) return value;

            // Important: Math.Ceiling works with negatives as expected for "upper" rounding.
            return Math.Ceiling(value / step) * step;
        }


        private static double Clamp(double v, double lo, double hi)
        {
            if (v < lo) return lo;
            if (v > hi) return hi;
            return v;
        }

        private void ApplyMainYScaleToUi()
        {
            _yMinAllowed = YMIN_HARD;

            switch (_yScaleMode)
            {
                case YScaleMode.Dbfs:
                    _yMaxAllowed = YMAX_DBFS_HARD;
                    lblYMax.Text = "Level Top (dBFS)";
                    lblYMin.Text = "Range (dBFS)";
                    break;

                case YScaleMode.Dbv:
                    _yMaxAllowed = YMAX_DBV_HARD;
                    lblYMax.Text = "Level Top (dBV)";
                    lblYMin.Text = "Range (dBV)";
                    break;

                case YScaleMode.DbGain:
                    _yMaxAllowed = YMAX_DBV_HARD;
                    lblYMax.Text = "Gain Top (dB)";
                    lblYMin.Text = "Range (dB)";
                    break;
            }

            // Configure NumericUpDown bounds based on unit
            _updating = true;
            try
            {
                numYMax.Minimum = (decimal)_yMinAllowed;
                numYMax.Maximum = (decimal)_yMaxAllowed;
                numYMax.Increment = 5;
                numYMax.DecimalPlaces = 0;

                // Optional: force current value into bounds immediately
                double v = Clamp((double)numYMax.Value, _yMinAllowed, _yMaxAllowed);
                numYMax.Value = (decimal)v;
            }
            finally { _updating = false; }

            ValidateAndApplyMainY();

        }

        private double GetYRangeDb(double fallback = 180)
        {
            return numYRange.Value > 0 ? (double)numYRange.Value : fallback;
        }

        private void SetYRangeDb(double rangeDb)
        {
            _updating = true;
            try
            {
                double clamped = Math.Max(1, Math.Min(400, rangeDb));
                numYRange.Value = (decimal)Math.Round(clamped);
            }
            finally { _updating = false; }
        }

        private void ValidateAndApplyMainY()
        {
            _updating = true;
            try
            {
                double yMax = (double)numYMax.Value;

                // Clamp only (no forced rounding)
                yMax = Clamp(yMax, _yMinAllowed, _yMaxAllowed);

                // Push corrected value back to the control
                numYMax.Value = (decimal)yMax;

                // Clamp range so YMin stays within allowed bounds
                double maxRange = yMax - _yMinAllowed;
                if ((double)numYRange.Value > maxRange)
                    numYRange.Value = (decimal)Math.Max(1, Math.Floor(maxRange));
            }
            finally { _updating = false; }

            PreviewChanged?.Invoke();
        }



        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public double YMin
        {
            get => YMax - GetYRangeDb(180);
            set
            {
                double desiredRange = YMax - value;
                SetYRangeDb(desiredRange);
                ValidateAndApplyMainY();
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
        //
        private int GetSelectedY2RangeDeg(int fallback)
        {
            if (cmbY2Range.SelectedItem is string s &&
                int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int r))
                return r;

            if (int.TryParse(cmbY2Range.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out r))
                return r;

            return fallback;
        }

        private void ApplyY2RangeListFilter(int maxAllowedRangeDeg, int? preferred = null)
        {
            _updating = true;
            try
            {
                var allowed = AllowedY2RangesDeg.Where(r => r <= maxAllowedRangeDeg).ToArray();
                if (allowed.Length == 0)
                    allowed = new[] { AllowedY2RangesDeg.Last() }; // defensive

                int current = GetSelectedY2RangeDeg(fallback: allowed.Max());

                cmbY2Range.Items.Clear();
                cmbY2Range.Items.AddRange(
                    allowed.Select(v => v.ToString(CultureInfo.InvariantCulture))
                           .Cast<object>()
                           .ToArray()
                );

                int target;
                if (preferred.HasValue && allowed.Contains(preferred.Value))
                    target = preferred.Value;
                else if (allowed.Contains(current))
                    target = current;
                else
                    target = allowed.Max();

                cmbY2Range.SelectedItem = target.ToString(CultureInfo.InvariantCulture);
            }
            finally { _updating = false; }
        }

        private void SetY2RangeFromDesired(double desiredRangeDeg)
        {
            int maxAllowed = (int)Math.Floor(PhaseMaxDeg - (-PHASE_ABS_MAX_DEG));
            if (maxAllowed < 0) maxAllowed = 0;

            int chosen = AllowedY2RangesDeg
                .Where(r => r <= maxAllowed)
                .OrderByDescending(r => r)
                .FirstOrDefault(r => r <= desiredRangeDeg);

            if (chosen == 0)
                chosen = AllowedY2RangesDeg.Where(r => r <= maxAllowed).DefaultIfEmpty(AllowedY2RangesDeg.Last()).Max();

            ApplyY2RangeListFilter(maxAllowed, preferred: chosen);
        }

        private void ValidateAndApplyY2()
        {
            _updating = true;
            try
            {
                double y2Max = (double)numY2Max.Value;

                // Round UP to upper multiple of 5° then clamp
                y2Max = RoundUpToStep(y2Max, 5.0);
                double absMax = _isUnwrappedPhase ? 1800.0 : 180.0;
                y2Max = Clamp(y2Max, -absMax, +absMax);

                numY2Max.Value = (decimal)y2Max;

                // Range must be <= 2 * phaseAbsMax (dynamic per wrapped/unwrapped)
                double phaseAbsMax = _isUnwrappedPhase ? 1800.0 : 180.0;
                int maxAllowedRange = (int)Math.Floor(y2Max - (-phaseAbsMax));
                ApplyY2RangeListFilter(maxAllowedRange);
            }
            finally { _updating = false; }

            PreviewChanged?.Invoke();
        }


        //
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

            PreviewChanged?.Invoke();
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

            PreviewChanged?.Invoke();
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

        //private static double Clamp(double v, double lo, double hi)
        //{
        //    if (v < lo) return lo;
        //    if (v > hi) return hi;
        //    return v;
        //}

        // --------- Boutons existants (inchangés côté user) ---------

        private void BtnDefault_Click(object? sender, EventArgs e)
        {
            // X : 0..Nyq en LIN, ΔF..Nyq en LOG
            // On calcule directement les 4 valeurs X sans passer par SyncFromMinMax
            // (qui peut recalculer via CenterMin/CenterMax et changer fMin en ΔF).
            double fs  = SampleRateHz;
            int    n   = FftSizeN;
            double df  = (fs > 0.0 && n > 0) ? (fs / n) : 1.0;
            double nyq = (fs > 0.0) ? (fs / 2.0) : (double)numFMax.Maximum;

            double fMin    = IsLogX ? df : 0.0;   // LIN = 0, LOG = ΔF
            double fMax    = nyq;
            double center  = (fMin + fMax) / 2.0;
            double span    = fMax - fMin;

            // Clamp dans les bornes des NUDs (sans déclencher d'événements)
            fMin   = Math.Max((double)numFMin.Minimum,   Math.Min((double)numFMin.Maximum,   fMin));
            fMax   = Math.Max((double)numFMax.Minimum,   Math.Min((double)numFMax.Maximum,   fMax));
            center = Math.Max((double)numCenter.Minimum, Math.Min((double)numCenter.Maximum, center));
            span   = Math.Max((double)numSpan.Minimum,   Math.Min((double)numSpan.Maximum,   span));

            // Y defaults
            double yMaxDefault = (_yScaleMode == YScaleMode.Dbfs) ? 0.0 : 100.0;

            _updating = true;
            try
            {
                // Assigner les 4 valeurs X directement
                numFMin.Value   = (decimal)fMin;
                numFMax.Value   = (decimal)fMax;
                numCenter.Value = (decimal)center;
                numSpan.Value   = (decimal)span;

                // Y
                numYMax.Value = (decimal)Clamp(yMaxDefault, _yMinAllowed, _yMaxAllowed);
                SetYRangeDb(200);
                ValidateAndApplyMainY();
            }
            finally { _updating = false; }

            // Déclencher le preview avec les valeurs par défaut
            PreviewChanged?.Invoke();
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

            if (yMaxData < yMinData)
            {
                var tmp = yMaxData;
                yMaxData = yMinData;
                yMinData = tmp;
            }

            // Clamp to unit-specific bounds
            yMaxData = Math.Min(_yMaxAllowed, yMaxData);
            yMinData = Math.Max(_yMinAllowed, yMinData);

            // Round YMax UP to upper multiple of 5 dB
            double yMax = Clamp(yMaxData, _yMinAllowed, _yMaxAllowed);


            // Span libre : la différence arrondie au dB supérieur
            double neededSpan = Math.Ceiling(yMax - yMinData);
            neededSpan = Math.Max(1, Math.Min(400, neededSpan));

            _updating = true;
            try
            {
                numYMax.Value  = (decimal)yMax;
                SetYRangeDb(neededSpan);
                ValidateAndApplyMainY();
                PreviewChanged?.Invoke();
            }
            finally { _updating = false; }

            //
            

        }

        // --------- Escape key handling to cancel the dialog ---------
        private void SpectrumAxisDialog_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                DialogResult = DialogResult.Cancel; // important: caller sees "Cancel"
                Close();
            }
        }

        private void BtnFitY2_Click(object? sender, EventArgs e)
        {
            if (FitY2Provider == null)
                return;

            var (y2Min, y2Max) = FitY2Provider();

            // Limites imposées pour le fit phase (spécification)
            const double HARD_MIN = -180.0;
            const double HARD_MAX = +180.0;

            y2Min = Clamp(y2Min, HARD_MIN, HARD_MAX);
            y2Max = Clamp(y2Max, HARD_MIN, HARD_MAX);

            // Sécurité : si inversé
            if (y2Max < y2Min)
            {
                double tmp = y2Max;
                y2Max = y2Min;
                y2Min = tmp;
            }

            // Y2 max arrondi au multiple de 10 supérieur
            y2Max = RoundUpToStep(y2Max, 10.0);
            y2Max = Clamp(y2Max, HARD_MIN, HARD_MAX);

            // Range minimal nécessaire pour inclure y2Min (sans dépasser HARD_MIN)
            // RangeNeeded = y2Max - y2Min, mais aussi borné par (y2Max - HARD_MIN)
            double rangeNeeded = y2Max - y2Min;
            double rangeMaxAllowed = y2Max - HARD_MIN;
            if (rangeNeeded > rangeMaxAllowed)
                rangeNeeded = rangeMaxAllowed;

            // Choisir le plus petit range de la liste qui couvre rangeNeeded
            // Liste existante : AllowedY2RangesDeg = { 360,270,180,120,90,45,30,15,5 } :contentReference[oaicite:2]{index=2}
            int chosenRange = AllowedY2RangesDeg
                .Where(r => r <= rangeMaxAllowed + 1e-9)      // ne doit pas contredire HARD_MIN
                .Where(r => r >= rangeNeeded - 1e-9)          // doit couvrir
                .OrderBy(r => r)                               // plus petit possible
                .FirstOrDefault();

            // Fallback défensif : prendre le plus grand range autorisé
            if (chosenRange == 0)
                chosenRange = AllowedY2RangesDeg.Where(r => r <= rangeMaxAllowed + 1e-9).DefaultIfEmpty(AllowedY2RangesDeg.Last()).Max();

            _updating = true;
            try
            {
                // Applique Y2 Max
                numY2Max.Value = (decimal)y2Max;

                // Applique la liste filtrée + sélection (via ton mécanisme existant)
                // -> On laisse ValidateAndApplyY2() faire le filtrage final cohérent.
                cmbY2Range.SelectedItem = chosenRange.ToString(CultureInfo.InvariantCulture);
                PreviewChanged?.Invoke();

            }
            finally { _updating = false; }

            ValidateAndApplyY2();
            

        }

    }
}
