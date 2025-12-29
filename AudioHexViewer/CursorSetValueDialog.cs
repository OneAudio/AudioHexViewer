using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace AudioHexViewer
{
    public partial class CursorSetValueDialog : Form
    {
        private readonly NumericUpDown nudC1;
        private readonly NumericUpDown nudC2;
        private readonly Button btnPeakC1;
        private readonly Button btnPeakC2;

        public double? C1Hz { get; private set; }
        public double? C2Hz { get; private set; }

        public CursorSetValueDialog(
            bool c1Enabled, bool c2Enabled,
            double? c1CurrentHz, double? c2CurrentHz,
            Func<double?> getPeakC1Hz,
            Func<double?> getPeakC2Hz)
        {
            Text = "Set Cursor Frequency";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 3,
                Padding = new Padding(10),
                AutoSize = true
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));

            // Row C1
            table.Controls.Add(new Label { Text = "C1 f (Hz)", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);

            nudC1 = new NumericUpDown
            {
                DecimalPlaces = 2,
                Minimum = 0,
                Maximum = 10_000_000,
                Increment = 1,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Width = 200
            };
            if (c1CurrentHz.HasValue)
                nudC1.Value = (decimal)Math.Max(0, Math.Min(10_000_000, c1CurrentHz.Value));
            table.Controls.Add(nudC1, 1, 0);

            btnPeakC1 = new Button { Text = "Set to Peak", Anchor = AnchorStyles.Right, Width = 100 };
            btnPeakC1.Click += (_, __) =>
            {
                var p = getPeakC1Hz();
                if (p.HasValue)
                    nudC1.Value = (decimal)Math.Max(0, Math.Min(10_000_000, p.Value));
                else
                    MessageBox.Show(this, "Peak not available for C1 (channel not available or no data).", "Peak",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            table.Controls.Add(btnPeakC1, 2, 0);

            // Row C2
            table.Controls.Add(new Label { Text = "C2 f (Hz)", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);

            nudC2 = new NumericUpDown
            {
                DecimalPlaces = 2,
                Minimum = 0,
                Maximum = 10_000_000,
                Increment = 1,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Width = 200
            };
            if (c2CurrentHz.HasValue)
                nudC2.Value = (decimal)Math.Max(0, Math.Min(10_000_000, c2CurrentHz.Value));
            table.Controls.Add(nudC2, 1, 1);

            btnPeakC2 = new Button { Text = "Set to Peak", Anchor = AnchorStyles.Right, Width = 100 };
            btnPeakC2.Click += (_, __) =>
            {
                var p = getPeakC2Hz();
                if (p.HasValue)
                    nudC2.Value = (decimal)Math.Max(0, Math.Min(10_000_000, p.Value));
                else
                    MessageBox.Show(this, "Peak not available for C2 (channel not available or no data).", "Peak",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            table.Controls.Add(btnPeakC2, 2, 1);

            // Buttons OK/Cancel
            var panelButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true
            };

            var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 90 };
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };
            panelButtons.Controls.Add(btnOk);
            panelButtons.Controls.Add(btnCancel);
            table.Controls.Add(panelButtons, 0, 2);
            table.SetColumnSpan(panelButtons, 3);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            // Disable rows if cursor not enabled
            nudC1.Enabled = btnPeakC1.Enabled = c1Enabled;
            nudC2.Enabled = btnPeakC2.Enabled = c2Enabled;

            Controls.Add(table);

            // Capture results
            btnOk.Click += (_, __) =>
            {
                C1Hz = c1Enabled ? (double)nudC1.Value : null;
                C2Hz = c2Enabled ? (double)nudC2.Value : null;
            };
        }
    }
}

