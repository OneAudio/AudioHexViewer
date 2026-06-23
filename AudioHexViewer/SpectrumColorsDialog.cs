using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace AudioHexViewer
{
    public partial class SpectrumColorsDialog : Form
    {
        private SpectrumColorSettings _colors;
        private SpectrumColorSettings _defaults;

        public SpectrumColorSettings SelectedColors   => Clone(_colors);
        public bool DefaultsChanged { get; private set; }
        public SpectrumColorSettings SelectedDefaults => Clone(_defaults);

        public SpectrumColorsDialog(SpectrumColorSettings initial,
                                    SpectrumColorSettings? defaults = null)
        {
            InitializeComponent();

            // Allow closing the dialog with the Escape key
            KeyPreview = true;
            KeyDown   += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };

            _colors   = Clone(initial ?? SpectrumColorSettings.Defaults());
            _colors.Normalize();
            _defaults = Clone(defaults ?? SpectrumColorSettings.Defaults());
            _defaults.Normalize();
            DefaultsChanged = false;

            // ── Traces swatches — Data Left + Right side by side ─────────────
            btnFftL.Click += (_, _) => Pick(() => _colors.FftLeftArgb,  v => _colors.FftLeftArgb = v,  btnFftL);
            btnFftR.Click += (_, _) => Pick(() => _colors.FftRightArgb, v => _colors.FftRightArgb = v, btnFftR);

            // ── Trace Hold swatches — Hold Max (= former PkHold) + Hold Min ──
            btnPkL.Click += (_, _) => Pick(() => _colors.PeakHoldLeftArgb,  v => _colors.PeakHoldLeftArgb = v,  btnPkL);
            btnPkR.Click += (_, _) => Pick(() => _colors.PeakHoldRightArgb, v => _colors.PeakHoldRightArgb = v, btnPkR);
            btnHoldMinL.Click += (_, _) => Pick(() => _colors.HoldMinLeftArgb,  v => _colors.HoldMinLeftArgb = v,  btnHoldMinL);
            btnHoldMinR.Click += (_, _) => Pick(() => _colors.HoldMinRightArgb, v => _colors.HoldMinRightArgb = v, btnHoldMinR);

            // ── Overlay swatches ──────────────────────────────────────────────
            btnOv1.Click += (_, _) => PickOverlay(0, btnOv1); btnOv2.Click += (_, _) => PickOverlay(1, btnOv2);
            btnOv3.Click += (_, _) => PickOverlay(2, btnOv3); btnOv4.Click += (_, _) => PickOverlay(3, btnOv4);
            btnOv5.Click += (_, _) => PickOverlay(4, btnOv5); btnOv6.Click += (_, _) => PickOverlay(5, btnOv6);
            btnOv7.Click += (_, _) => PickOverlay(6, btnOv7); btnOv8.Click += (_, _) => PickOverlay(7, btnOv8);

            // ── Chart swatches ────────────────────────────────────────────────
            btnGridMaj.Click  += (_, _) => Pick(() => _colors.GridArgb,       v => _colors.GridArgb = v,       btnGridMaj);
            btnGridMin.Click  += (_, _) => Pick(() => _colors.GridMinorArgb,  v => _colors.GridMinorArgb = v,  btnGridMin);
            btnBack.Click     += (_, _) => Pick(() => _colors.BackgroundArgb, v => _colors.BackgroundArgb = v, btnBack);
            btnLabMaj.Click   += (_, _) => Pick(() => _colors.LabelMajorArgb, v => _colors.LabelMajorArgb = v, btnLabMaj);
            btnLabMin.Click   += (_, _) => Pick(() => _colors.LabelMinorArgb, v => _colors.LabelMinorArgb = v, btnLabMin);
            btnInfoText.Click += (_, _) => Pick(() => _colors.InfoTextArgb,   v => _colors.InfoTextArgb = v,   btnInfoText);

            // ── Thickness NUDs ────────────────────────────────────────────────
            nudFftWidth.Value = Clamp3(_colors.FftWidth);
            nudPkWidth.Value  = Clamp3(_colors.PeakHoldWidth);
            nudHoldMinWidth.Value = Clamp3(_colors.HoldMinWidth);
            nudOvWidth.Value  = Clamp3(_colors.OverlayWidth);
            nudFftWidth.ValueChanged     += (_, _) => _colors.FftWidth      = (int)nudFftWidth.Value;
            nudPkWidth.ValueChanged      += (_, _) => _colors.PeakHoldWidth = (int)nudPkWidth.Value;
            nudHoldMinWidth.ValueChanged += (_, _) => _colors.HoldMinWidth  = (int)nudHoldMinWidth.Value;
            nudOvWidth.ValueChanged      += (_, _) => _colors.OverlayWidth  = (int)nudOvWidth.Value;

            // ── Overlay line style ────────────────────────────────────────────
            cmbOvStyle.Items.AddRange(new object[] {
                ChartDashStyle.Solid, ChartDashStyle.Dash, ChartDashStyle.Dot,
                ChartDashStyle.DashDot, ChartDashStyle.DashDotDot
            });
            cmbOvStyle.SelectedIndexChanged += (_, _) =>
            {
                if (cmbOvStyle.SelectedItem is ChartDashStyle s)
                    _colors.OverlayDashStyle = (int)s;
            };

            // ── Negative per group — live preview ────────────────────────────
            chkNegTraces.CheckedChanged += (_, _) =>
            { _colors.NegativeTraces   = chkNegTraces.Checked;   UpdateSwatchDisplay(); };
            chkNegHold.CheckedChanged += (_, _) =>
            { _colors.NegativeHold     = chkNegHold.Checked;     UpdateSwatchDisplay(); };
            chkNegOverlays.CheckedChanged += (_, _) =>
            { _colors.NegativeOverlays = chkNegOverlays.Checked; UpdateSwatchDisplay(); };
            chkNegChart.CheckedChanged += (_, _) =>
            { _colors.NegativeChart    = chkNegChart.Checked;    UpdateSwatchDisplay(); };

            // ── Action buttons ────────────────────────────────────────────────
            btnSaveDefault.Click    += (_, _) => { _defaults = Clone(_colors); DefaultsChanged = true; };
            btnRestoreDefault.Click += (_, _) => { _colors = Clone(_defaults); UpdateUiFromSettings(); };

            UpdateUiFromSettings();
        }

        private void UpdateUiFromSettings()
        {
            _colors.Normalize();

            btnFftL.BackColor     = ShowColor(_colors.FftLeftArgb,         _colors.NegativeTraces);
            btnFftR.BackColor     = ShowColor(_colors.FftRightArgb,        _colors.NegativeTraces);
            btnPkL.BackColor      = ShowColor(_colors.PeakHoldLeftArgb,    _colors.NegativeTraces);
            btnPkR.BackColor      = ShowColor(_colors.PeakHoldRightArgb,   _colors.NegativeTraces);
            btnHoldMinL.BackColor = ShowColor(_colors.HoldMinLeftArgb,     _colors.NegativeTraces);
            btnHoldMinR.BackColor = ShowColor(_colors.HoldMinRightArgb,    _colors.NegativeTraces);

            btnGridMaj.BackColor  = ShowColor(_colors.GridArgb,       _colors.NegativeChart);
            btnGridMin.BackColor  = ShowColor(_colors.GridMinorArgb,  _colors.NegativeChart);
            btnBack.BackColor     = ShowColor(_colors.BackgroundArgb, _colors.NegativeChart);
            btnLabMaj.BackColor   = ShowColor(_colors.LabelMajorArgb, _colors.NegativeChart);
            btnLabMin.BackColor   = ShowColor(_colors.LabelMinorArgb, _colors.NegativeChart);
            btnInfoText.BackColor = ShowColor(_colors.InfoTextArgb,   _colors.NegativeChart);

            var ov = _colors.OverlaySlotArgb;
            bool no = _colors.NegativeOverlays;
            btnOv1.BackColor=ShowColor(ov[0],no); btnOv2.BackColor=ShowColor(ov[1],no);
            btnOv3.BackColor=ShowColor(ov[2],no); btnOv4.BackColor=ShowColor(ov[3],no);
            btnOv5.BackColor=ShowColor(ov[4],no); btnOv6.BackColor=ShowColor(ov[5],no);
            btnOv7.BackColor=ShowColor(ov[6],no); btnOv8.BackColor=ShowColor(ov[7],no);

            nudFftWidth.Value = Clamp3(_colors.FftWidth);
            nudPkWidth.Value  = Clamp3(_colors.PeakHoldWidth);
            nudOvWidth.Value  = Clamp3(_colors.OverlayWidth);

            var style = (ChartDashStyle)_colors.OverlayDashStyle;
            if (!Enum.IsDefined(typeof(ChartDashStyle), style)) style = ChartDashStyle.Dot;
            cmbOvStyle.SelectedItem = style;

            chkNegTraces.Checked   = _colors.NegativeTraces;
            chkNegHold.Checked     = _colors.NegativeHold;
            chkNegOverlays.Checked = _colors.NegativeOverlays;
            chkNegChart.Checked    = _colors.NegativeChart;
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private void PickOverlay(int idx, Button swatch)
        {
            _colors.Normalize();
            int[] arr = _colors.OverlaySlotArgb;
            using var cd = new ColorDialog {
                Color = Color.FromArgb(idx < arr.Length ? arr[idx] : _colors.OverlayArgb),
                FullOpen = true
            };
            if (cd.ShowDialog(this) != DialogResult.OK) return;
            if (idx < arr.Length) arr[idx] = cd.Color.ToArgb();
            swatch.BackColor = cd.Color;
        }

        private static void Pick(Func<int> getArgb, Action<int> setArgb, Button swatch)
        {
            using var cd = new ColorDialog { Color = Color.FromArgb(getArgb()), FullOpen = true };
            if (cd.ShowDialog() != DialogResult.OK) return;
            setArgb(cd.Color.ToArgb());
            swatch.BackColor = cd.Color;
        }


        private void UpdateSwatchDisplay() => UpdateUiFromSettings();
        private static Color ShowColor(int argb, bool inv) { var c = Color.FromArgb(argb); return inv ? Color.FromArgb(c.A,255-c.R,255-c.G,255-c.B) : c; }
        private static decimal Clamp3(int v) => v < 1 ? 1 : v > 3 ? 3 : v;

        private static SpectrumColorSettings Clone(SpectrumColorSettings src)
        {
            var dst = new SpectrumColorSettings {
                FftLeftArgb      = src.FftLeftArgb,   FftRightArgb      = src.FftRightArgb,
                FftWidth         = src.FftWidth,       PeakHoldLeftArgb  = src.PeakHoldLeftArgb,
                PeakHoldRightArgb= src.PeakHoldRightArgb, PeakHoldWidth = src.PeakHoldWidth,
                HoldMinLeftArgb  = src.HoldMinLeftArgb,  HoldMinRightArgb = src.HoldMinRightArgb,
                HoldMinWidth     = src.HoldMinWidth,
                OverlayArgb      = src.OverlayArgb,   OverlayWidth      = src.OverlayWidth,
                GridArgb         = src.GridArgb,       GridMinorArgb     = src.GridMinorArgb,
                BackgroundArgb   = src.BackgroundArgb, LabelMajorArgb    = src.LabelMajorArgb,
                LabelMinorArgb   = src.LabelMinorArgb, InfoTextArgb      = src.InfoTextArgb,
                OverlayDashStyle = src.OverlayDashStyle,
                NegativeTraces   = src.NegativeTraces,
                NegativeHold     = src.NegativeHold,
                NegativeOverlays = src.NegativeOverlays,
                NegativeChart    = src.NegativeChart,
            };
            if (src.OverlaySlotArgb != null)
            {
                dst.OverlaySlotArgb = new int[src.OverlaySlotArgb.Length];
                Array.Copy(src.OverlaySlotArgb, dst.OverlaySlotArgb, src.OverlaySlotArgb.Length);
            }
            else dst.OverlaySlotArgb = SpectrumColorSettings.Defaults().OverlaySlotArgb;
            dst.Normalize();
            return dst;
        }
    }
}
