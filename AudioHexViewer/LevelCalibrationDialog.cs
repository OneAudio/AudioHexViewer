using System;
using System.Globalization;
using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    /// <summary>
    /// Calibration de niveau pour le spectre :
    /// l'utilisateur applique un signal AC connu (Vrms) sur l'entrée, et on calcule
    /// un offset tel que : dBV = dBFS + offset, avec 0 dBV = 1 Vrms.
    /// </summary>
    internal sealed class LevelCalibrationDialog : Form
    {
        private readonly TextBox _txtLeft = new TextBox();
        private readonly TextBox _txtRight = new TextBox();

        private readonly Button _btnCalLeft = new Button();
        private readonly Button _btnCalRight = new Button();

        private readonly Label _lblLeftStatus = new Label();
        private readonly Label _lblRightStatus = new Label();

        private readonly Label _lblInfo = new Label();
        private readonly Button _btnClose = new Button();

        public Func<double, (bool ok, string message)>? CalibrateLeft { get; set; }
        public Func<double, (bool ok, string message)>? CalibrateRight { get; set; }

        public LevelCalibrationDialog()
        {
            Text = "Calibrate Level (dBFS → dBV)";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(560, 250);

            var pad = 12;

            _lblInfo.AutoSize = false;
            _lblInfo.Location = new Point(pad, pad);
            _lblInfo.Size = new Size(ClientSize.Width - 2 * pad, 60);
            _lblInfo.Text =
                "1) Applique un signal AC SINUSOÏDAL connu sur les entrées L/R (en Vrms).\r\n" +
                "2) Saisis la valeur Vrms mesurée (multimètre / générateur) dans le champ.\r\n" +
                "3) Clique 'Calibrate' pour enregistrer l'offset (dBV = dBFS + offset).";

            Controls.Add(_lblInfo);

            int y0 = pad + 70;

            // Left row
            var lblL = new Label { AutoSize = true, Location = new Point(pad, y0 + 4), Text = "Left (Vrms):" };
            _txtLeft.Location = new Point(110, y0);
            _txtLeft.Size = new Size(110, 23);

            _btnCalLeft.Text = "Calibrate";
            _btnCalLeft.Location = new Point(230, y0 - 1);
            _btnCalLeft.Size = new Size(90, 25);
            _btnCalLeft.Click += (_, __) => CalibrateOne(false);

            _lblLeftStatus.AutoSize = false;
            _lblLeftStatus.Location = new Point(330, y0 + 2);
            _lblLeftStatus.Size = new Size(220, 40);
            _lblLeftStatus.TextAlign = ContentAlignment.MiddleLeft;

            Controls.Add(lblL);
            Controls.Add(_txtLeft);
            Controls.Add(_btnCalLeft);
            Controls.Add(_lblLeftStatus);

            // Right row
            int y1 = y0 + 55;
            var lblR = new Label { AutoSize = true, Location = new Point(pad, y1 + 4), Text = "Right (Vrms):" };
            _txtRight.Location = new Point(110, y1);
            _txtRight.Size = new Size(110, 23);

            _btnCalRight.Text = "Calibrate";
            _btnCalRight.Location = new Point(230, y1 - 1);
            _btnCalRight.Size = new Size(90, 25);
            _btnCalRight.Click += (_, __) => CalibrateOne(true);

            _lblRightStatus.AutoSize = false;
            _lblRightStatus.Location = new Point(330, y1 + 2);
            _lblRightStatus.Size = new Size(220, 40);
            _lblRightStatus.TextAlign = ContentAlignment.MiddleLeft;

            Controls.Add(lblR);
            Controls.Add(_txtRight);
            Controls.Add(_btnCalRight);
            Controls.Add(_lblRightStatus);

            // Close
            _btnClose.Text = "Close";
            _btnClose.Size = new Size(90, 28);
            _btnClose.Location = new Point(ClientSize.Width - pad - _btnClose.Width, ClientSize.Height - pad - _btnClose.Height);
            _btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _btnClose.Click += (_, __) => Close();
            Controls.Add(_btnClose);

            AcceptButton = _btnClose;
        }

        public void SetCurrentCalibration(
            bool validLeft, double offsetLeft,
            bool validRight, double offsetRight,
            double? lastRmsLeftDbfs, double? lastRmsRightDbfs)
        {
            _lblLeftStatus.Text = BuildStatus(validLeft, offsetLeft, lastRmsLeftDbfs);
            _lblRightStatus.Text = BuildStatus(validRight, offsetRight, lastRmsRightDbfs);
        }

        private static string BuildStatus(bool valid, double offset, double? lastRmsDbfs)
        {
            if (!valid)
            {
                if (lastRmsDbfs != null)
                    return $"(last RMS: {lastRmsDbfs.Value:F2} dBFS)\r\nNot calibrated";
                return "Not calibrated";
            }

            double vrmsFs = Math.Pow(10.0, offset / 20.0);
            string rms = (lastRmsDbfs != null) ? $"(last RMS: {lastRmsDbfs.Value:F2} dBFS)\r\n" : "";
            return $"{rms}Offset: {offset:+0.00;-0.00;0.00} dB\r\n0 dBFS ≈ {vrmsFs:F4} Vrms";
        }

        private void CalibrateOne(bool isRight)
        {
            var txt = isRight ? _txtRight.Text : _txtLeft.Text;
            if (!TryParseDouble(txt, out double vrms))
            {
                MessageBox.Show(this, "Valeur Vrms invalide. Utilise un nombre (ex: 1.0 ou 1,0).", "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var func = isRight ? CalibrateRight : CalibrateLeft;
            if (func == null)
            {
                MessageBox.Show(this, "Calibration handler not set.", "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var (ok, message) = func(vrms);

            if (!ok)
            {
                MessageBox.Show(this, message, "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Refresh status text (message is already detailed)
            if (isRight)
                _lblRightStatus.Text = message;
            else
                _lblLeftStatus.Text = message;
        }

        private static bool TryParseDouble(string s, out double value)
        {
            // Accepte virgule ou point, selon culture
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
                return true;

            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return true;

            // fallback : remplace comma/dot
            s = s.Replace(',', '.');
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
