// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

#nullable enable
using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    internal partial class DevicesDialog
    {
        private System.ComponentModel.IContainer? components = null;

        private GroupBox  grpInput,  grpOutput;
        private Label     lblInputDevice,  lblInputSR,   lblInputFmtHdr,  lblInputFormat;
        private Label     lblOutputDevice, lblOutputSR,  lblOutputFmtHdr, lblOutputFormat;
        private ComboBox  cmbInputDevice,  cmbInputRate;
        private ComboBox  cmbOutputDevice, cmbOutputRate;
        private Label     lblBufferHdr;
        private ComboBox  cmbBuffer;
        private Button    btnInfos, btnCancel, btnOk;
        private Button    btnLive, btnOffline;   // boutons de mode exclusifs
        private CheckBox  chkLoop;
        private Button    btnOpenFile;
        private Label     lblFilePath, lblSepPP, lblSelectFile;

        protected override void Dispose(bool disposing)
        { if (disposing) components?.Dispose(); base.Dispose(disposing); }

        private void InitializeComponent()
        {
            var gS    = new Font("Segoe UI", 7.5f, FontStyle.Regular, GraphicsUnit.Point);
            var std9  = new Font("Segoe UI", 9f,   FontStyle.Regular, GraphicsUnit.Point);
            var bold9 = new Font("Segoe UI", 9f,   FontStyle.Bold,    GraphicsUnit.Point);

            // ── Layout constants ──────────────────────────────────────────────
            const int SR_X   = 248;   // x of SR / Buffer combos  (right margin = left margin = 10 px)
            const int SR_W   = 75;    // width of SR / Buffer combos
            const int GW     = 333;   // group width (right edge aligns with SR/Buffer combos)
            const int BUF_LW = 50;    // buffer label width
            const int BTN_W  = 88;    // largeur des boutons de mode (= bouton OK)
            const int BTN_H  = 27;    // hauteur des boutons de mode (= bouton OK)

            // ── Control creation ──────────────────────────────────────────────
            grpInput        = new GroupBox();
            lblInputDevice  = new Label();  cmbInputDevice  = new ComboBox();
            lblInputSR      = new Label();  cmbInputRate    = new ComboBox();
            lblInputFmtHdr  = new Label();  lblInputFormat  = new Label();
            lblBufferHdr    = new Label();  cmbBuffer       = new ComboBox();

            grpOutput       = new GroupBox();
            lblOutputDevice = new Label();  cmbOutputDevice = new ComboBox();
            lblOutputSR     = new Label();  cmbOutputRate   = new ComboBox();
            lblOutputFmtHdr = new Label();  lblOutputFormat = new Label();

            btnInfos    = new Button();
            btnCancel   = new Button();
            btnOk       = new Button();
            btnLive     = new Button();
            btnOffline  = new Button();
            chkLoop     = new CheckBox();
            btnOpenFile = new Button();
            lblFilePath   = new Label();
            lblSepPP      = new Label();
            lblSelectFile = new Label();

            grpInput.SuspendLayout();
            grpOutput.SuspendLayout();
            SuspendLayout();

            // ══════════════════════════════════════════════════════════════════
            // grpInput
            // ══════════════════════════════════════════════════════════════════
            grpInput.Text     = "Analyzer Input";
            grpInput.Font     = bold9;
            grpInput.Location = new Point(10, 8);
            grpInput.Size     = new Size(GW, 200);
            grpInput.TabStop  = false;

            // ── Boutons LIVE / OFFLINE ────────────────────────────────────────
            // Même taille que le bouton OK (88×27), côte à côte
            btnLive.Text      = "LIVE";
            btnLive.Font      = bold9;
            btnLive.Location  = new Point(10, 22);
            btnLive.Size      = new Size(BTN_W, BTN_H);
            btnLive.FlatStyle = FlatStyle.Flat;
            btnLive.FlatAppearance.BorderSize = 1;
            btnLive.UseVisualStyleBackColor   = false;

            btnOffline.Text     = "OFFLINE";
            btnOffline.Font     = bold9;
            btnOffline.Location = new Point(10 + BTN_W + 6, 22);
            btnOffline.Size     = new Size(BTN_W, BTN_H);
            btnOffline.FlatStyle = FlatStyle.Flat;
            btnOffline.FlatAppearance.BorderSize = 1;
            btnOffline.UseVisualStyleBackColor   = false;

            // ── DEVICE ────────────────────────────────────────────────────────
            lblInputDevice.Text      = "DEVICE";
            lblInputDevice.Font      = gS;
            lblInputDevice.ForeColor = SystemColors.GrayText;
            lblInputDevice.Location  = new Point(10, 58);
            lblInputDevice.AutoSize  = true;

            cmbInputDevice.Font              = std9;
            cmbInputDevice.DropDownStyle     = ComboBoxStyle.DropDownList;
            cmbInputDevice.FormattingEnabled = true;
            cmbInputDevice.Location          = new Point(10, 72);
            cmbInputDevice.Size              = new Size(232, 24);

            // ── SAMPLE RATE ───────────────────────────────────────────────────
            lblInputSR.Text      = "SAMPLE RATE";
            lblInputSR.Font      = gS;
            lblInputSR.ForeColor = SystemColors.GrayText;
            lblInputSR.Location  = new Point(SR_X, 58);
            lblInputSR.AutoSize  = true;

            cmbInputRate.Font              = std9;
            cmbInputRate.DropDownStyle     = ComboBoxStyle.DropDownList;
            cmbInputRate.FormattingEnabled = true;
            cmbInputRate.Location          = new Point(SR_X, 72);
            cmbInputRate.Size              = new Size(SR_W, 24);

            // ── FORMAT ────────────────────────────────────────────────────────
            lblInputFmtHdr.Text      = "FORMAT";
            lblInputFmtHdr.Font      = gS;
            lblInputFmtHdr.ForeColor = SystemColors.GrayText;
            lblInputFmtHdr.Location  = new Point(10, 100);
            lblInputFmtHdr.AutoSize  = true;

            lblInputFormat.Text      = "";
            lblInputFormat.Font      = std9;
            lblInputFormat.ForeColor = System.Drawing.Color.SteelBlue;
            lblInputFormat.Location  = new Point(60, 100);
            lblInputFormat.Size      = new Size(263, 18);

            // ── BUFFER (même colonne que SR) ──────────────────────────────────
            lblBufferHdr.Text      = "BUFFER";
            lblBufferHdr.Font      = gS;
            lblBufferHdr.ForeColor = SystemColors.GrayText;
            lblBufferHdr.Location  = new Point(SR_X - BUF_LW - 4, 120);
            lblBufferHdr.Size      = new Size(BUF_LW, 14);
            lblBufferHdr.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            lblBufferHdr.AutoSize  = false;

            cmbBuffer.DropDownStyle     = ComboBoxStyle.DropDownList;
            cmbBuffer.Font              = std9;
            cmbBuffer.FormattingEnabled = true;
            cmbBuffer.Location          = new Point(SR_X, 116);
            cmbBuffer.Size              = new Size(SR_W, 24);

            // ── Séparateur ────────────────────────────────────────────────────
            lblSepPP.BackColor = SystemColors.ControlDark;
            lblSepPP.Location  = new Point(10, 146);
            lblSepPP.Size      = new Size(GW - 20, 1);

            // ── SELECT AUDIO FILE ─────────────────────────────────────────────
            lblSelectFile.Text      = "SELECT AUDIO FILE";
            lblSelectFile.Font      = gS;
            lblSelectFile.ForeColor = SystemColors.GrayText;
            lblSelectFile.Location  = new Point(10, 152);
            lblSelectFile.AutoSize  = true;

            // ── Bouton Open ───────────────────────────────────────────────────
            btnOpenFile.Text     = "Open";
            btnOpenFile.Font     = std9;
            btnOpenFile.Location = new Point(10, 167);
            btnOpenFile.Size     = new Size(72, 27);
            btnOpenFile.UseVisualStyleBackColor = true;

            // ── Chemin fichier (à droite du bouton Open) ──────────────────────
            lblFilePath.Text         = "";
            lblFilePath.Font         = std9;
            lblFilePath.ForeColor    = System.Drawing.Color.SteelBlue;
            lblFilePath.Location     = new Point(88, 171);
            lblFilePath.Size         = new Size(SR_X - 96, 18);   // jusqu'au séparateur SR
            lblFilePath.AutoEllipsis = true;

            // ── Loop : aligné colonne SR (x=258), même ligne que Open ─────────
            chkLoop.Text     = "Loop";
            chkLoop.Font     = std9;
            chkLoop.Location = new Point(SR_X, 170);
            chkLoop.AutoSize = true;

            grpInput.Controls.AddRange(new Control[]
            {
                btnLive, btnOffline,
                lblInputDevice, cmbInputDevice,
                lblInputSR,     cmbInputRate,
                lblInputFmtHdr, lblInputFormat,
                lblBufferHdr,   cmbBuffer,
                lblSepPP, lblSelectFile, btnOpenFile, lblFilePath, chkLoop
            });

            // ══════════════════════════════════════════════════════════════════
            // grpOutput
            // ══════════════════════════════════════════════════════════════════
            grpOutput.Text     = "Generator Output";
            grpOutput.Font     = bold9;
            grpOutput.Location = new Point(10, 218);
            grpOutput.Size     = new Size(GW, 92);
            grpOutput.TabStop  = false;

            lblOutputDevice.Text      = "DEVICE";
            lblOutputDevice.Font      = gS;
            lblOutputDevice.ForeColor = SystemColors.GrayText;
            lblOutputDevice.Location  = new Point(10, 22);
            lblOutputDevice.AutoSize  = true;

            cmbOutputDevice.Font              = std9;
            cmbOutputDevice.DropDownStyle     = ComboBoxStyle.DropDownList;
            cmbOutputDevice.FormattingEnabled = true;
            cmbOutputDevice.Location          = new Point(10, 36);
            cmbOutputDevice.Size              = new Size(232, 24);

            lblOutputSR.Text      = "SAMPLE RATE";
            lblOutputSR.Font      = gS;
            lblOutputSR.ForeColor = SystemColors.GrayText;
            lblOutputSR.Location  = new Point(SR_X, 22);
            lblOutputSR.AutoSize  = true;

            cmbOutputRate.Font              = std9;
            cmbOutputRate.DropDownStyle     = ComboBoxStyle.DropDownList;
            cmbOutputRate.FormattingEnabled = true;
            cmbOutputRate.Location          = new Point(SR_X, 36);
            cmbOutputRate.Size              = new Size(SR_W, 24);

            lblOutputFmtHdr.Text      = "FORMAT";
            lblOutputFmtHdr.Font      = gS;
            lblOutputFmtHdr.ForeColor = SystemColors.GrayText;
            lblOutputFmtHdr.Location  = new Point(10, 64);
            lblOutputFmtHdr.AutoSize  = true;

            lblOutputFormat.Text      = "";
            lblOutputFormat.Font      = std9;
            lblOutputFormat.ForeColor = System.Drawing.Color.SteelBlue;
            lblOutputFormat.Location  = new Point(60, 64);
            lblOutputFormat.Size      = new Size(263, 18);

            grpOutput.Controls.AddRange(new Control[]
            {
                lblOutputDevice, cmbOutputDevice,
                lblOutputSR,     cmbOutputRate,
                lblOutputFmtHdr, lblOutputFormat
            });

            // ══════════════════════════════════════════════════════════════════
            // Boutons bas
            // ══════════════════════════════════════════════════════════════════
            btnInfos.Text     = "Device info…";
            btnInfos.Font     = std9;
            btnInfos.Location = new Point(10, 320);
            btnInfos.Size     = new Size(92, BTN_H);
            btnInfos.UseVisualStyleBackColor = true;

            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Text         = "CANCEL";
            btnCancel.Font         = std9;
            btnCancel.Location     = new Point(161, 320);
            btnCancel.Size         = new Size(BTN_W, BTN_H);
            btnCancel.UseVisualStyleBackColor = true;

            btnOk.Text      = "OK";
            btnOk.Font      = bold9;
            btnOk.Location  = new Point(255, 320);
            btnOk.Size      = new Size(BTN_W, BTN_H);
            btnOk.FlatStyle = FlatStyle.Flat;
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.BackColor = System.Drawing.Color.FromArgb(0, 122, 204);
            btnOk.ForeColor = System.Drawing.Color.White;

            // ── Form ──────────────────────────────────────────────────────────
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode       = AutoScaleMode.Font;
            ClientSize          = new Size(353, 357);
            Font                = std9;
            Controls.AddRange(new Control[]
                { grpInput, grpOutput, btnInfos, btnCancel, btnOk });
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false; MinimizeBox = false;
            StartPosition   = FormStartPosition.CenterParent;
            Text            = "Audio Devices";

            grpInput.ResumeLayout(false);
            grpInput.PerformLayout();
            grpOutput.ResumeLayout(false);
            grpOutput.PerformLayout();
            // ── Context help Name assignments ─────────────────────────────
            cmbInputDevice.Name = "cmbInputDevice";
            cmbInputRate.Name = "cmbInputRate";
            cmbOutputDevice.Name = "cmbOutputDevice";
            cmbOutputRate.Name = "cmbOutputRate";
            cmbBuffer.Name = "cmbBuffer";
            btnLive.Name = "btnLive";
            btnOffline.Name = "btnOffline";
            chkLoop.Name = "chkLoop";
            btnOpenFile.Name = "btnOpenFile";
            btnInfos.Name = "btnInfos";
            btnCancel.Name = "btnCancel";
            btnOk.Name = "btnOk";
            ResumeLayout(false);
        }
    }
}
#nullable restore
