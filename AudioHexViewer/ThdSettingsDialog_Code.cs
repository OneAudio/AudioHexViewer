using System;
using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    internal sealed class ThdSettingsDialog_Code : Form
    {
        private readonly CheckBox chkHPF = new() { Text = "HPF", AutoSize = true };
        private readonly NumericUpDown nudFlow = new() { DecimalPlaces = 1, Minimum = 0, Maximum = 384000, Increment = 1, Width = 100 };

        private readonly CheckBox chkLPF = new() { Text = "LPF", AutoSize = true };
        private readonly NumericUpDown nudFhigh = new() { DecimalPlaces = 1, Minimum = 0, Maximum = 384000, Increment = 10, Width = 100 };

        private readonly NumericUpDown nudHarms = new() { Minimum = 2, Maximum = 20, Value = 5, Width = 60 };
        private readonly CheckBox chkShowHarms = new() { AutoSize = true };

        private readonly RadioButton rbDb = new() { Text = "dB", AutoSize = true };
        private readonly RadioButton rbPct = new() { Text = "%", AutoSize = true };

        private readonly Button btnOk = new() { Text = "OK", DialogResult = DialogResult.OK, Width = 90 };
        private readonly Button btnCancel = new() { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };

       public SpectrumSettings.ThdSettings Result { get; private set; }

        public ThdSettingsDialog_Code(SpectrumSettings.ThdSettings initial, int sampleRate)
        {
            Result = initial ?? new SpectrumSettings.ThdSettings();

            Text = "THD Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(420, 260);

            double nyq = (sampleRate > 0) ? (sampleRate / 2.0) : 384000.0;
            nudFhigh.Maximum = (decimal)nyq;
            nudFlow.Maximum = (decimal)nyq;

            // Layout simple
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 6,
                Padding = new Padding(10),
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));



            // Row 0 : HPF
            grid.Controls.Add(chkHPF, 0, 0);
            grid.Controls.Add(new Label { Text = "Flow (Hz)", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft }, 1, 0);
            grid.Controls.Add(nudFlow, 2, 0);

            // Row 1 : LPF
            grid.Controls.Add(chkLPF, 0, 1);
            grid.Controls.Add(new Label { Text = "Fhigh (Hz)", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft }, 1, 1);
            grid.Controls.Add(nudFhigh, 2, 1);

            // Row 2 : Harmonics number
            grid.Controls.Add(new Label { Text = "Harmonics number", AutoSize = true }, 0, 2);
            grid.SetColumnSpan(grid.GetControlFromPosition(0, 2)!, 2);
            grid.Controls.Add(nudHarms, 2, 2);

            // Row 3 : Show values
            grid.Controls.Add(new Label { Text = "Show values", AutoSize = true }, 0, 3);
            grid.SetColumnSpan(grid.GetControlFromPosition(0, 3)!, 2);
            grid.Controls.Add(chkShowHarms, 2, 3);

            // Row 4 : Unit
            var pnlUnit = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            pnlUnit.Controls.Add(rbDb);
            pnlUnit.Controls.Add(rbPct);
            grid.Controls.Add(new Label { Text = "THD unit", AutoSize = true }, 0, 4);
            grid.SetColumnSpan(grid.GetControlFromPosition(0, 4)!, 2);
            grid.Controls.Add(pnlUnit, 2, 4);

            // Row 5 : Buttons
            var pnlBtns = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            pnlBtns.Controls.Add(btnOk);
            pnlBtns.Controls.Add(btnCancel);
            grid.Controls.Add(pnlBtns, 0, 5);
            grid.SetColumnSpan(pnlBtns, 3);

            Controls.Add(grid);

            // Init values
            chkHPF.Checked = Result.UseHPF;
            nudFlow.Value = (decimal)Math.Min(Math.Max(Result.FlowHz, 0.0), nyq);

            chkLPF.Checked = Result.UseLPF;
            nudFhigh.Value = (decimal)Math.Min(Math.Max(Result.FhighHz, 0.0), nyq);

            nudHarms.Value = Math.Min(Math.Max(Result.HarmonicsNumber, 2), 20);
            chkShowHarms.Checked = Result.ShowHarmonicsValues;

            rbDb.Checked = (Result.Unit == SpectrumSettings.ThdSettings.ThdUnit.Db);
            rbPct.Checked = !rbDb.Checked;

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            btnOk.Click += (_, __) =>
            {
                // clamp + store
                Result = new SpectrumSettings.ThdSettings
                {
                    UseHPF = chkHPF.Checked,
                    FlowHz = (double)nudFlow.Value,
                    UseLPF = chkLPF.Checked,
                    FhighHz = (double)nudFhigh.Value,
                    HarmonicsNumber = (int)nudHarms.Value,
                    ShowHarmonicsValues = chkShowHarms.Checked,
                    Unit = rbPct.Checked ? SpectrumSettings.ThdSettings.ThdUnit.Percent : SpectrumSettings.ThdSettings.ThdUnit.Db
                };
            };   
        }
    }
}
