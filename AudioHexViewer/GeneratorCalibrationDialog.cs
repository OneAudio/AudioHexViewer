// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

using System;
using System.Globalization;
using System.Windows.Forms;

namespace AudioHexViewer
{
    /// <summary>
    /// Dialogue (non-modal) de calibration du niveau de sortie du générateur.
    ///
    /// Principe :
    ///   1. L'utilisateur démarre le générateur en mode Sinus à une fréquence et amplitude connues.
    ///   2. Il mesure la tension de sortie avec un voltmètre calibré.
    ///   3. Il entre les valeurs Vrms lues (L et R) et clique "Apply".
    ///   4. Le système calcule : VrmsFullScale = VrmsMesuré / 10^(dBFS/20)
    ///      — ce qui représente la tension de sortie correspondant à 0 dBFS.
    ///
    /// La fenêtre reste non-modale pour permettre d'ajuster le générateur pendant la calibration.
    /// La validation ("Apply") échoue si le générateur n'est pas en cours d'exécution.
    /// </summary>
    internal sealed partial class GeneratorCalibrationDialog : Form
    {
    private ToolTip? _contextTip;   // context-sensitive tooltip for this form

        // ── Callbacks fournis par GeneratorWindow ─────────────────────────────
        /// <summary>Retourne l'amplitude courante du générateur en dBFS, ou null si arrêté.</summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Func<double?>? GetCurrentAmplitudeDbfs { get; set; }

        /// <summary>Appelé avec (VrmsFS_L, VrmsFS_R) quand la calibration est validée.</summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Action<double, double>? OnCalibrationApplied { get; set; }

        // ── Timer de mise à jour du label dBFS courant ────────────────────────
        private readonly System.Windows.Forms.Timer _refreshTimer;

        public GeneratorCalibrationDialog()
        {
            InitializeComponent();
            _contextTip = ContextHelpManager.CreateFormToolTip();
            Load += (_, _) => ContextHelpManager.ApplyTooltips(this.Controls, _contextTip);

            _refreshTimer = new System.Windows.Forms.Timer { Interval = 200 };
            _refreshTimer.Tick += (_, _) => RefreshAmplitudeLabel();

            btnApply.Click    += BtnApply_Click;
            btnClearCal.Click += BtnClear_Click;
            btnClose.Click    += (_, _) => Close();

            Shown    += (_, _) => _refreshTimer.Start();
            FormClosed += (_, _) => _refreshTimer.Stop();

            AcceptButton = btnApply;
            CancelButton = btnClose;
        }

        // ── Mise à jour dynamique du label "générateur courant" ───────────────
        private void RefreshAmplitudeLabel()
        {
            var db = GetCurrentAmplitudeDbfs?.Invoke();
            if (db.HasValue)
            {
                lblCurrentGen.Text      = $"Generator running at  {db.Value:F1} dBFS";
                lblCurrentGen.ForeColor = System.Drawing.Color.SeaGreen;
            }
            else
            {
                lblCurrentGen.Text      = "Generator is not running";
                lblCurrentGen.ForeColor = System.Drawing.Color.OrangeRed;
            }
        }

        // ── Validation ────────────────────────────────────────────────────────
        private void BtnApply_Click(object? s, EventArgs e)
        {
            // 1) Vérifier que le générateur tourne
            var db = GetCurrentAmplitudeDbfs?.Invoke();
            if (!db.HasValue)
            {
                MessageBox.Show(this,
                    "The generator is not running.\n\n" +
                    "Please start the generator with a Sine signal at the desired\n" +
                    "calibration frequency and amplitude, then click Apply.",
                    "Calibration — Generator not running",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2) Parser les valeurs Vrms saisies
            if (!TryParseVrms(txtVrmsL.Text, out double vrmsL))
            {
                ShowParseError("L");
                txtVrmsL.Focus(); txtVrmsL.SelectAll();
                return;
            }
            if (!TryParseVrms(txtVrmsR.Text, out double vrmsR))
            {
                ShowParseError("R");
                txtVrmsR.Focus(); txtVrmsR.SelectAll();
                return;
            }

            // 3) Calculer VrmsFullScale = Vrms mesuré / amplitude linéaire
            double ampLinear = Math.Pow(10.0, db.Value / 20.0);
            if (ampLinear < 1e-10)
            {
                MessageBox.Show(this,
                    "Generator amplitude is too low for a reliable calibration.\n" +
                    "Please set an amplitude ≥ -80 dBFS.",
                    "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double vrmsFS_L = vrmsL / ampLinear;
            double vrmsFS_R = vrmsR / ampLinear;

            // 4) Notifier GeneratorWindow
            OnCalibrationApplied?.Invoke(vrmsFS_L, vrmsFS_R);

            // 5) Mise à jour des status labels
            lblVrmsLStatus.Text = $"0 dBFS = {vrmsFS_L:F4} Vrms";
            lblVrmsRStatus.Text = $"0 dBFS = {vrmsFS_R:F4} Vrms";
            lblCurrentGen.Text      = $"Calibration applied  — L: {vrmsFS_L:F4} Vrms  |  R: {vrmsFS_R:F4} Vrms";
            lblCurrentGen.ForeColor = System.Drawing.Color.SeaGreen;
        }

        // ── Clear ────────────────────────────────────────────────────────────
        private void BtnClear_Click(object? sender, EventArgs e)
        {
            OnCalibrationApplied?.Invoke(1.0, 1.0);
            lblVrmsLStatus.Text     = "0 dBFS = 1.000 Vrms";
            lblVrmsRStatus.Text     = "0 dBFS = 1.000 Vrms";
            lblCurrentGen.Text      = "Calibration cleared";
            lblCurrentGen.ForeColor = System.Drawing.Color.DarkOrange;
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static bool TryParseVrms(string s, out double value)
        {
            s = s.Trim();
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
             && value > 0) return true;
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
             && value > 0) return true;
            s = s.Replace(',', '.');
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                && value > 0;
        }

        private void ShowParseError(string channel)
            => MessageBox.Show(this,
                $"Invalid Vrms value for channel {channel}.\n\n" +
                "Enter a positive number, e.g. \"1.414\" or \"0.707\".",
                "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
