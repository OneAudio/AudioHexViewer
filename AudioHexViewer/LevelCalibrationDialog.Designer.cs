// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

#nullable enable
using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    internal sealed partial class LevelCalibrationDialog
    {
        private System.ComponentModel.IContainer? components = null;

        private Label   lblInstruction;
        private Label   lblCurrentStatus;   // "Signal on input" or status line
        private Label   lblSeparator;
        private Label   lblVrmsLTitle;
        private TextBox _txtLeft;
        private Label   lblVrmsLUnit;
        private Label   lblVrmsLStatus;     // "0 dBFS = X.XXX Vrms"
        private Label   lblVrmsRTitle;
        private TextBox _txtRight;
        private Label   lblVrmsRUnit;
        private Label   lblVrmsRStatus;     // "0 dBFS = X.XXX Vrms"
        private Button  btnApply;
        private Button  btnClear;
        private Button  _btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing) components?.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            lblInstruction  = new Label();
            lblCurrentStatus= new Label();
            lblSeparator    = new Label();
            lblVrmsLTitle   = new Label();
            _txtLeft        = new TextBox();
            lblVrmsLUnit    = new Label();
            lblVrmsLStatus  = new Label();
            lblVrmsRTitle   = new Label();
            _txtRight       = new TextBox();
            lblVrmsRUnit    = new Label();
            lblVrmsRStatus  = new Label();
            btnApply        = new Button();
            btnClear        = new Button();
            _btnClose       = new Button();

            SuspendLayout();

            // ── Instruction ───────────────────────────────────────────────────
            lblInstruction.Location = new Point(12, 14);
            lblInstruction.Size     = new Size(360, 72);
            lblInstruction.Text     =
                "Apply a known AC signal to the input. Measure the RMS voltage " +
                "with a calibrated voltmeter, then enter the values below and click Apply.";
            lblInstruction.Font     = new Font("Segoe UI", 9f);

            // ── Status ligne (visible si déjà calibré) ────────────────────────
            lblCurrentStatus.Location  = new Point(12, 92);
            lblCurrentStatus.Size      = new Size(360, 20);
            lblCurrentStatus.Text      = "";
            lblCurrentStatus.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblCurrentStatus.ForeColor = System.Drawing.Color.SeaGreen;

            // ── Séparateur ────────────────────────────────────────────────────
            lblSeparator.Location    = new Point(12, 120);
            lblSeparator.Size        = new Size(360, 1);
            lblSeparator.BorderStyle = BorderStyle.Fixed3D;

            // ── L channel ─────────────────────────────────────────────────────
            lblVrmsLTitle.Location = new Point(12, 132);
            lblVrmsLTitle.AutoSize = true;
            lblVrmsLTitle.Text     = "L  —  measured Vrms:";
            lblVrmsLTitle.Font     = new Font("Segoe UI", 9.5f);

            _txtLeft.Location = new Point(192, 129);
            _txtLeft.Size     = new Size(110, 23);
            _txtLeft.Font     = new Font("Segoe UI", 10f);
            _txtLeft.TabIndex = 0;

            lblVrmsLUnit.Location  = new Point(308, 132);
            lblVrmsLUnit.AutoSize  = true;
            lblVrmsLUnit.Text      = "Vrms";
            lblVrmsLUnit.Font      = new Font("Segoe UI", 9f);
            lblVrmsLUnit.ForeColor = SystemColors.GrayText;

            lblVrmsLStatus.Location  = new Point(192, 155);
            lblVrmsLStatus.Size      = new Size(180, 16);
            lblVrmsLStatus.Text      = "0 dBFS = 1.000 Vrms";
            lblVrmsLStatus.Font      = new Font("Segoe UI", 8f);
            lblVrmsLStatus.ForeColor = SystemColors.GrayText;

            // ── R channel ─────────────────────────────────────────────────────
            lblVrmsRTitle.Location = new Point(12, 178);
            lblVrmsRTitle.AutoSize = true;
            lblVrmsRTitle.Text     = "R  —  measured Vrms:";
            lblVrmsRTitle.Font     = new Font("Segoe UI", 9.5f);

            _txtRight.Location = new Point(192, 175);
            _txtRight.Size     = new Size(110, 23);
            _txtRight.Font     = new Font("Segoe UI", 10f);
            _txtRight.TabIndex = 1;

            lblVrmsRUnit.Location  = new Point(308, 178);
            lblVrmsRUnit.AutoSize  = true;
            lblVrmsRUnit.Text      = "Vrms";
            lblVrmsRUnit.Font      = new Font("Segoe UI", 9f);
            lblVrmsRUnit.ForeColor = SystemColors.GrayText;

            lblVrmsRStatus.Location  = new Point(192, 201);
            lblVrmsRStatus.Size      = new Size(180, 16);
            lblVrmsRStatus.Text      = "0 dBFS = 1.000 Vrms";
            lblVrmsRStatus.Font      = new Font("Segoe UI", 8f);
            lblVrmsRStatus.ForeColor = SystemColors.GrayText;

            // ── Boutons ───────────────────────────────────────────────────────
            btnApply.Location  = new Point(192, 228);
            btnApply.Size      = new Size(110, 32);
            btnApply.Text      = "Apply";
            btnApply.BackColor = Color.SeaGreen;
            btnApply.ForeColor = Color.White;
            btnApply.FlatStyle = FlatStyle.Flat;
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnApply.TabIndex  = 2;
            btnApply.UseVisualStyleBackColor = false;

            _btnClose.Location = new Point(312, 228);
            _btnClose.Size     = new Size(60, 32);
            _btnClose.Text     = "Close";
            _btnClose.TabIndex = 3;
            _btnClose.UseVisualStyleBackColor = true;

            btnClear.Location  = new Point(12, 228);
            btnClear.Size      = new Size(90, 32);
            btnClear.Text      = "CLEAR";
            btnClear.TabIndex  = 4;
            btnClear.UseVisualStyleBackColor = true;

            // ── Form ──────────────────────────────────────────────────────────
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode       = AutoScaleMode.Font;
            ClientSize          = new Size(384, 274);
            FormBorderStyle     = FormBorderStyle.FixedDialog;
            MaximizeBox         = false; MinimizeBox = false;
            StartPosition       = FormStartPosition.CenterParent;
            ShowInTaskbar       = false;
            Text                = "Calibrate Input Level (dBFS → dBV)";

            Controls.AddRange(new Control[]
            {
                lblInstruction, lblCurrentStatus, lblSeparator,
                lblVrmsLTitle, _txtLeft,  lblVrmsLUnit, lblVrmsLStatus,
                lblVrmsRTitle, _txtRight, lblVrmsRUnit, lblVrmsRStatus,
                btnApply, _btnClose, btnClear
            });

            // ── Context help Name assignments ─────────────────────────────
            _txtLeft.Name = "_txtLeft";
            _txtRight.Name = "_txtRight";
            btnApply.Name = "btnApply";
            btnClear.Name = "btnClear";
            _btnClose.Name = "btnClose";
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
#nullable restore
