using System;
using System.Globalization;
using System.Windows.Forms;
using System.ComponentModel;

namespace AudioHexViewer
{
    internal sealed partial class LevelCalibrationDialog : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<double, (bool ok, string message)>? CalibrateLeft { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<double, (bool ok, string message)>? CalibrateRight { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Action? ResetCalibration { get; set; }

        // Aliases for backward compat with callers using SetCurrentCalibration
        private Label _lblLeftStatus  => lblVrmsLStatus;
        private Label _lblRightStatus => lblVrmsRStatus;

        public LevelCalibrationDialog()
        {
            InitializeComponent();

            btnApply .Click += BtnApply_Click;
            btnClear .Click += BtnClear_Click;
            _btnClose.Click += (_, _) => Close();

            AcceptButton = btnApply;
            CancelButton = _btnClose;
        }

        // ── Mise à jour initiale des status ───────────────────────────────────
        public void SetCurrentCalibration(
            bool validLeft,  double offsetLeft,
            bool validRight, double offsetRight,
            double? lastRmsLeftDbfs, double? lastRmsRightDbfs)
        {
            lblVrmsLStatus.Text = BuildFsLine(validLeft,  offsetLeft);
            lblVrmsRStatus.Text = BuildFsLine(validRight, offsetRight);

            // Ligne de statut général
            if (validLeft || validRight)
            {
                lblCurrentStatus.Text      = "Calibration active";
                lblCurrentStatus.ForeColor = System.Drawing.Color.SeaGreen;
            }
            else
            {
                lblCurrentStatus.Text      = "";
            }
        }

        // "0 dBFS = X.XXX Vrms" avec 4 chiffres significatifs
        private static string BuildFsLine(bool valid, double offset)
        {
            if (!valid) return "0 dBFS = 1.000 Vrms  (not calibrated)";
            double vrmsFs = Math.Pow(10.0, offset / 20.0);
            return $"0 dBFS = {Sig4(vrmsFs)} Vrms";
        }

        // ── Apply (les deux voies) ────────────────────────────────────────────
        private void BtnApply_Click(object? sender, EventArgs e)
        {
            bool okL = TryParseDouble(_txtLeft.Text,  out double vrmsL);
            bool okR = TryParseDouble(_txtRight.Text, out double vrmsR);

            if (!okL && !okR)
            {
                MessageBox.Show(this,
                    "Enter at least one valid Vrms value (positive number, e.g. 1.414).",
                    "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Calibre L si une valeur est saisie
            if (okL && CalibrateLeft != null)
            {
                var (ok, msg) = CalibrateLeft(vrmsL);
                if (!ok) { MessageBox.Show(this, msg, "Calibration L",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                lblVrmsLStatus.Text = msg;
            }

            // Calibre R si une valeur est saisie
            if (okR && CalibrateRight != null)
            {
                var (ok, msg) = CalibrateRight(vrmsR);
                if (!ok) { MessageBox.Show(this, msg, "Calibration R",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                lblVrmsRStatus.Text = msg;
            }

            lblCurrentStatus.Text      = "Calibration active";
            lblCurrentStatus.ForeColor = System.Drawing.Color.SeaGreen;
        }

        // ── Clear ─────────────────────────────────────────────────────────────
        private void BtnClear_Click(object? sender, EventArgs e)
        {
            ResetCalibration?.Invoke();
            lblVrmsLStatus.Text      = "0 dBFS = 1.000 Vrms  (not calibrated)";
            lblVrmsRStatus.Text      = "0 dBFS = 1.000 Vrms  (not calibrated)";
            lblCurrentStatus.Text    = "";
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static string Sig4(double v)
        {
            if (v <= 0) return "0";
            int mag      = (int)Math.Floor(Math.Log10(v));
            int decimals = Math.Max(0, 3 - mag);
            return v.ToString("F" + decimals, CultureInfo.InvariantCulture);
        }

        private static bool TryParseDouble(string s, out double value)
        {
            s = s.Trim();
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture,   out value) && value > 0) return true;
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value > 0) return true;
            s = s.Replace(',', '.');
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value > 0;
        }
    }
}
