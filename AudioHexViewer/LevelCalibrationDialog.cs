using System;
using System.Globalization;
using System.Windows.Forms;
using System.ComponentModel;

namespace AudioHexViewer
{
    /// <summary>
    /// Calibration de niveau pour le spectre :
    /// l'utilisateur applique un signal AC connu (Vrms) sur l'entrée, et on calcule
    /// un offset tel que : dBV = dBFS + offset, avec 0 dBV = 1 Vrms.
    /// </summary>
    internal sealed partial class LevelCalibrationDialog : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<double, (bool ok, string message)>? CalibrateLeft { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<double, (bool ok, string message)>? CalibrateRight { get; set; }

        // Callback fourni par SpectrumControl
        public Action? ResetCalibration { get; set; }

        private void _ResetCalibration_Click(object sender, EventArgs e)
        {
            ResetCalibration?.Invoke();

            // Optionnel : feedback utilisateur
            MessageBox.Show(this, "Calibration cleared.", "ResetCAL", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Rafraîchir l’affichage du dialog si tu affiches l'état (valid/offset/gain)
            // (voir point 1.3)
        }

        public LevelCalibrationDialog()
        {
            InitializeComponent();

            _btnCalLeft.Click += (_, __) => CalibrateOne(isRight: false);
            _btnCalRight.Click += (_, __) => CalibrateOne(isRight: true);
            _btnClose.Click += (_, __) => Close();

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
                MessageBox.Show(this,
                    "Valeur Vrms invalide. Utilise un nombre (ex: 1.0 ou 1,0).",
                    "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var func = isRight ? CalibrateRight : CalibrateLeft;
            if (func == null)
            {
                MessageBox.Show(this,
                    "Calibration handler not set.",
                    "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var (ok, message) = func(vrms);

            if (!ok)
            {
                MessageBox.Show(this,
                    message,
                    "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Refresh status text (message is already detailed)
            if (isRight) _lblRightStatus.Text = message;
            else _lblLeftStatus.Text = message;
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

        private void _lblRightStatus_Click(object sender, EventArgs e)
        {

        }

        private void _lblLeftStatus_Click(object sender, EventArgs e)
        {

        }

    }
}
