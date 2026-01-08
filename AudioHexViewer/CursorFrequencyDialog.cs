using System;
using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    public sealed class CursorFrequencyDialog : Form
    {
        private readonly NumericUpDown _nud;
        private readonly Button _btnOk;

        public double SelectedHz { get; private set; }

        public CursorFrequencyDialog(string title, double minHz, double maxHz, double? initialHz)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            ClientSize = new Size(260, 72);

            var lbl = new Label
            {
                Text = "Frequency (Hz):",
                AutoSize = true,
                Location = new Point(10, 12)
            };

            _nud = new NumericUpDown
            {
                Location = new Point(110, 10),
                Size = new Size(140, 22),
                DecimalPlaces = 1,
                ThousandsSeparator = true,
                Minimum = (decimal)minHz,
                Maximum = (decimal)maxHz,
                Increment = 1
            };

            double init = initialHz ?? Math.Min(Math.Max(1000.0, minHz), maxHz);
            if (init < minHz) init = minHz;
            if (init > maxHz) init = maxHz;
            _nud.Value = (decimal)init;

            _btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(180, 38),
                Size = new Size(70, 25)
            };

            AcceptButton = _btnOk;   // Enter valide
            CancelButton = _btnOk;   // (optionnel) Esc ferme aussi

            Controls.Add(lbl);
            Controls.Add(_nud);
            Controls.Add(_btnOk);

            _btnOk.Click += (_, __) =>
            {
                SelectedHz = (double)_nud.Value;
            };
        }
    }
}
