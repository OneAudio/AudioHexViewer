using System;
using System.Windows.Forms;

namespace AudioHexViewer
{
    public partial class CursorSetValueDialog : Form
    {
    private ToolTip? _contextTip;   // context-sensitive tooltip for this form

        private readonly Func<double?> _getPeakC1Hz;
        private readonly Func<double?> _getPeakC2Hz;
        private readonly bool _c1Enabled;
        private readonly bool _c2Enabled;

        public double? C1Hz { get; private set; }
        public double? C2Hz { get; private set; }

        public CursorSetValueDialog(
            bool c1Enabled, bool c2Enabled,
            double? c1CurrentHz, double? c2CurrentHz,
            Func<double?> getPeakC1Hz,
            Func<double?> getPeakC2Hz)
        {
            InitializeComponent();
            _contextTip = ContextHelpManager.CreateFormToolTip();
            Load += (_, _) => ContextHelpManager.ApplyTooltips(this.Controls, _contextTip);

            _c1Enabled = c1Enabled;
            _c2Enabled = c2Enabled;
            _getPeakC1Hz = getPeakC1Hz ?? (() => null);
            _getPeakC2Hz = getPeakC2Hz ?? (() => null);

            // init valeurs
            if (c1CurrentHz.HasValue)
                nudC1.Value = ClampToNud(nudC1, c1CurrentHz.Value);

            if (c2CurrentHz.HasValue)
                nudC2.Value = ClampToNud(nudC2, c2CurrentHz.Value);

            // enable/disable selon curseurs actifs
            nudC1.Enabled = btnPeakC1.Enabled = _c1Enabled;
            nudC2.Enabled = btnPeakC2.Enabled = _c2Enabled;

            // events
            btnPeakC1.Click += (_, __) => SetPeakInto(nudC1, _getPeakC1Hz, "C1");
            btnPeakC2.Click += (_, __) => SetPeakInto(nudC2, _getPeakC2Hz, "C2");

            btnOk.Click += (_, __) =>
            {
                C1Hz = _c1Enabled ? (double)nudC1.Value : null;
                C2Hz = _c2Enabled ? (double)nudC2.Value : null;
            };
        }

        private static decimal ClampToNud(NumericUpDown nud, double v)
        {
            double min = (double)nud.Minimum;
            double max = (double)nud.Maximum;
            if (v < min) v = min;
            if (v > max) v = max;
            return (decimal)v;
        }

        private void SetPeakInto(NumericUpDown nud, Func<double?> getPeak, string who)
        {
            var p = getPeak();
            if (p.HasValue)
            {
                nud.Value = ClampToNud(nud, p.Value);
            }
            else
            {
                MessageBox.Show(
                    this,
                    $"Peak not available for {who} (channel not available or no data).",
                    "Peak",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
    }
}
