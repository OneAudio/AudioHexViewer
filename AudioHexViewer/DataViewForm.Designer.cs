// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    partial class DataViewForm
    {
        private System.ComponentModel.IContainer? components = null;

        // ── VU-mètre ──────────────────────────────────────────────────────────
        internal Panel pnlVu;

        // ── Canal L ───────────────────────────────────────────────────────────
        internal Label lblLeftTitle;       // "LEFT"
        internal Label lblLeftLevelDV;     // "-26.5 dBFS"
        internal Label lblLeftMainTitle;   // "Float"
        internal Label lblLeftMainValue;   // "-0.00888062"

        // ── Canal R ───────────────────────────────────────────────────────────
        internal Label lblRightTitle;      // "RIGHT"
        internal Label lblRightLevelDV;    // "-29.0 dBFS"
        internal Label lblRightMainTitle;  // "Float"
        internal Label lblRightMainValue;  // "-0.00286865"
        // 4-row binary display (MSB bits, MSB hex, LSB bits, LSB hex)
        internal Label lblLeftMsbBits,  lblLeftMsbHex;
        internal Label lblLeftLsbBits,  lblLeftLsbHex;
        internal Label lblRightMsbBits, lblRightMsbHex;
        internal Label lblRightLsbBits, lblRightLsbHex;

        // ── Barre inférieure ──────────────────────────────────────────────────
        internal Label    label4;           // "Main data format"
        internal ComboBox comboDisplayMode;
        internal Button   btnRunStop;

        // Aliases for legacy code
        internal Label label2 => lblLeftTitle;
        internal Label label3 => lblRightTitle;

        protected override void Dispose(bool disposing)
        {
            if (disposing) components?.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            // Width = 400 (VU-mètre width)
            // Layout:
            //   y=  0..52  : pnlVu (400×52, fond noir)
            //   y= 52.. 58  : séparateur implicite
            //   y= 58..100  : ligne L/R — titre + dBFS
            //   y=100..130  : ligne L/R — format title
            //   y=130..168  : ligne L/R — valeur principale
            //   y=168..204  : barre bas (format combo + RUN/STOP)

            const int W  = 400;
            const int CW = W / 2;  // 200 par colonne

            pnlVu             = new Panel();
            lblLeftTitle      = new Label();
            lblLeftLevelDV    = new Label();
            lblLeftMainTitle  = new Label();
            lblLeftMainValue  = new Label();
            lblRightTitle     = new Label();
            lblRightLevelDV   = new Label();
            lblRightMainTitle = new Label();
            lblRightMainValue = new Label();
            lblLeftMsbBits  = new Label(); lblLeftMsbHex  = new Label();
            lblLeftLsbBits  = new Label(); lblLeftLsbHex  = new Label();
            lblRightMsbBits = new Label(); lblRightMsbHex = new Label();
            lblRightLsbBits = new Label(); lblRightLsbHex = new Label();
            label4            = new Label();
            comboDisplayMode  = new ComboBox();
            btnRunStop        = new Button();

            SuspendLayout();

            // ── VU-mètre ─────────────────────────────────────────────────────
            pnlVu.Location  = new Point(0, 0);
            pnlVu.Size      = new Size(W, 52);
            pnlVu.BackColor = Color.Black;
            pnlVu.Name      = "pnlVu";

            // ── LEFT title + dBFS ─────────────────────────────────────────────
            lblLeftTitle.Location  = new Point(4, 58);
            lblLeftTitle.Size      = new Size(80, 26);
            lblLeftTitle.Text      = "LEFT";
            lblLeftTitle.Font      = new Font("Segoe UI", 12f, FontStyle.Bold);
            lblLeftTitle.ForeColor = SystemColors.HotTrack;
            lblLeftTitle.TextAlign = ContentAlignment.MiddleLeft;

            lblLeftLevelDV.Location  = new Point(CW - 96, 58);
            lblLeftLevelDV.Size      = new Size(92, 26);
            lblLeftLevelDV.Text      = "-∞ dBFS";
            lblLeftLevelDV.Font      = new Font("Segoe UI", 10f);
            lblLeftLevelDV.ForeColor = SystemColors.HotTrack;
            lblLeftLevelDV.BackColor = SystemColors.GradientInactiveCaption;
            lblLeftLevelDV.TextAlign = ContentAlignment.MiddleCenter;

            // ── RIGHT title + dBFS ────────────────────────────────────────────
            lblRightTitle.Location  = new Point(CW + 4, 58);
            lblRightTitle.Size      = new Size(80, 26);
            lblRightTitle.Text      = "RIGHT";
            lblRightTitle.Font      = new Font("Segoe UI", 12f, FontStyle.Bold);
            lblRightTitle.ForeColor = SystemColors.HotTrack;
            lblRightTitle.TextAlign = ContentAlignment.MiddleLeft;

            lblRightLevelDV.Location  = new Point(W - 96, 58);
            lblRightLevelDV.Size      = new Size(92, 26);
            lblRightLevelDV.Text      = "-∞ dBFS";
            lblRightLevelDV.Font      = new Font("Segoe UI", 10f);
            lblRightLevelDV.ForeColor = SystemColors.HotTrack;
            lblRightLevelDV.BackColor = SystemColors.GradientInactiveCaption;
            lblRightLevelDV.TextAlign = ContentAlignment.MiddleCenter;

            // ── Format title L / R ────────────────────────────────────────────
            lblLeftMainTitle.Location  = new Point(0, 90);
            lblLeftMainTitle.Size      = new Size(CW, 26);
            lblLeftMainTitle.Text      = "Float";
            lblLeftMainTitle.Font      = new Font("Segoe UI", 10f);
            lblLeftMainTitle.BackColor = SystemColors.ControlLight;
            lblLeftMainTitle.TextAlign = ContentAlignment.MiddleCenter;

            lblRightMainTitle.Location  = new Point(CW, 90);
            lblRightMainTitle.Size      = new Size(CW, 26);
            lblRightMainTitle.Text      = "Float";
            lblRightMainTitle.Font      = new Font("Segoe UI", 10f);
            lblRightMainTitle.BackColor = SystemColors.ControlLight;
            lblRightMainTitle.TextAlign = ContentAlignment.MiddleCenter;

            // ── Valeur principale L / R ───────────────────────────────────────
            lblLeftMainValue.Location  = new Point(0, 116);
            lblLeftMainValue.Size      = new Size(CW, 65);
            lblLeftMainValue.Text      = "0.000000";
            lblLeftMainValue.Font      = new Font("Segoe UI", 14f, FontStyle.Bold);
            lblLeftMainValue.BackColor = SystemColors.InactiveCaption;
            lblLeftMainValue.TextAlign = ContentAlignment.MiddleCenter;

            lblRightMainValue.Location  = new Point(CW, 116);
            lblRightMainValue.Size      = new Size(CW, 65);
            lblRightMainValue.Text      = "0.000000";
            lblRightMainValue.Font      = new Font("Segoe UI", 14f, FontStyle.Bold);
            lblRightMainValue.BackColor = SystemColors.InactiveCaption;
            lblRightMainValue.TextAlign = ContentAlignment.MiddleCenter;

            // ── Barre inférieure ──────────────────────────────────────────────
            // ── 4-row binary labels (MSB bits, MSB hex, LSB bits, LSB hex) ─
            // y=116..181 (65px) same footprint as extended lblMainValue
            Font binFont = new Font("Courier New", 9f, FontStyle.Bold);
            var  binBG   = SystemColors.InactiveCaption;
            var  hexClr  = System.Drawing.Color.FromArgb(0, 80, 200);
            var  binClr  = System.Drawing.Color.FromArgb(0, 120, 0);

            void SetBinLbl(Label l, int x, int y, bool isHex) {
                l.Location  = new Point(x, y);
                l.Size      = new Size(CW, 16);
                l.Font      = binFont;
                l.BackColor = binBG;
                l.ForeColor = isHex ? hexClr : binClr;
                l.Visible   = false;
            }

            SetBinLbl(lblLeftMsbBits,  0,  116, false);
            SetBinLbl(lblLeftMsbHex,   0,  133, true);
            SetBinLbl(lblLeftLsbBits,  0,  150, false);
            SetBinLbl(lblLeftLsbHex,   0,  167, true);
            SetBinLbl(lblRightMsbBits, CW, 116, false);
            SetBinLbl(lblRightMsbHex,  CW, 133, true);
            SetBinLbl(lblRightLsbBits, CW, 150, false);
            SetBinLbl(lblRightLsbHex,  CW, 167, true);

            // ── Format row position ────────────────────────────────────────
            label4.Location = new Point(6, 198);
            label4.AutoSize = true;
            label4.Text     = "Format";
            label4.Font     = new Font("Segoe UI", 8.5f);
            label4.ForeColor= SystemColors.GrayText;

            comboDisplayMode.Location      = new Point(54, 194);
            comboDisplayMode.Size          = new Size(150, 23);
            comboDisplayMode.DropDownStyle = ComboBoxStyle.DropDownList;
            comboDisplayMode.Font          = new Font("Segoe UI", 9f);

            btnRunStop.Location  = new Point(W - 70, 188);
            btnRunStop.Size      = new Size(70, 40);
            btnRunStop.Text      = "▶  RUN";
            btnRunStop.Font      = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnRunStop.BackColor = Color.SeaGreen;
            btnRunStop.ForeColor = Color.White;
            btnRunStop.FlatStyle = FlatStyle.Flat;
            btnRunStop.FlatAppearance.BorderSize = 0;
            btnRunStop.UseVisualStyleBackColor = false;
            btnRunStop.TextAlign = ContentAlignment.MiddleCenter;
            // Paint override : rendu stable quel que soit l'état focus/combo
            btnRunStop.Paint += (s, e) =>
            {
                var b = (Button)s!;
                e.Graphics.Clear(b.BackColor);
                TextRenderer.DrawText(e.Graphics, b.Text, b.Font,
                    b.ClientRectangle, b.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPadding);
            };

            // ── Form ──────────────────────────────────────────────────────────
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode       = AutoScaleMode.Font;
            ClientSize          = new Size(W, 234);
            FormBorderStyle     = FormBorderStyle.FixedSingle;
            MaximizeBox         = false;
            SizeGripStyle       = SizeGripStyle.Hide;
            StartPosition       = FormStartPosition.Manual;
            Text                = "DataView";

            Controls.AddRange(new Control[]
            {
                pnlVu,
                lblLeftTitle,  lblLeftLevelDV,  lblLeftMainTitle,  lblLeftMainValue,
                lblRightTitle, lblRightLevelDV, lblRightMainTitle, lblRightMainValue,
                lblLeftMsbBits,  lblLeftMsbHex,  lblLeftLsbBits,  lblLeftLsbHex,
                lblRightMsbBits, lblRightMsbHex, lblRightLsbBits, lblRightLsbHex,
                label4, comboDisplayMode, btnRunStop
            });


            // ── Context help Name assignments ─────────────────────────────────
            comboDisplayMode.Name = "comboDisplayMode";
            btnRunStop.Name       = "btnRunStop";
            ResumeLayout(false);
        }
    }
}
