#nullable enable
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    partial class SpectrumAxisDialog
    {
        private System.ComponentModel.IContainer? components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            // NUDs first (BeginInit required)
            numYMax   = new NumericUpDown(); numY2Max  = new NumericUpDown();
            numFMin   = new NumericUpDown(); numFMax   = new NumericUpDown();
            numCenter = new NumericUpDown(); numSpan   = new NumericUpDown();
            ((ISupportInitialize)numYMax).BeginInit();
            ((ISupportInitialize)numY2Max).BeginInit();
            ((ISupportInitialize)numFMin).BeginInit();
            ((ISupportInitialize)numFMax).BeginInit();
            ((ISupportInitialize)numCenter).BeginInit();
            ((ISupportInitialize)numSpan).BeginInit();
            SuspendLayout();

            // ── Fonts / colors ────────────────────────────────────────────
            Font std     = new("Segoe UI", 9f);
            Font secFont = new("Segoe UI Semibold", 8.5f);
            Font fitFont = new("Segoe UI", 7.5f);
            Color blue     = Color.FromArgb(0, 90, 160);
            Color blueLine = Color.FromArgb(180, 210, 240);

            // ── Layout constants ──────────────────────────────────────────
            const int PL   = 12;           // left/right form padding
            const int CW   = 224;          // content width
            const int LW   = 110;          // label width (right-aligned)
            const int CTLX = PL + LW + 2;  // 124 — NUD / combo left
            const int CTLW = 76;           // NUD / combo width  → right = 200
            const int FITX = CTLX + CTLW + 4; // 204 — [Fit] button x
            const int FITW = 32;           //         right = 268 = PL+CW ✓
            const int ROW  = 22;           // row height

            // ── Section header helper ──────────────────────────────────────
            Panel MakeSec(string title, int secY, Button? rb = null)
            {
                var p = new Panel {
                    Location = new Point(PL, secY), Size = new Size(CW, 20),
                    BackColor = SystemColors.Window
                };
                int tw = rb == null ? CW : (CW - rb.Width - 6);
                p.Controls.Add(new Label {
                    Text = title, Font = secFont, ForeColor = blue,
                    AutoSize = false, Location = new Point(0, 0), Size = new Size(tw, 14)
                });
                p.Controls.Add(new Panel {
                    Location = new Point(0, 18), Size = new Size(CW, 1), BackColor = blueLine
                });
                if (rb != null) {
                    rb.Location = new Point(CW - rb.Width, 0);
                    rb.Height = 16; rb.FlatStyle = FlatStyle.Flat;
                    rb.Font = fitFont; rb.FlatAppearance.BorderSize = 1;
                    rb.UseVisualStyleBackColor = true;
                    p.Controls.Add(rb);
                }
                return p;
            }

            // ── Row label helper ───────────────────────────────────────────
            Label RowLbl(string text, int rowY) => new Label {
                Text = text, Location = new Point(PL, rowY + 4),
                Size = new Size(LW, 14), TextAlign = ContentAlignment.MiddleRight,
                Font = std, AutoSize = false
            };

            // ── Fit button helper ──────────────────────────────────────────
            Button FitBtn(int rowY) => new Button {
                Location = new Point(FITX, rowY), Size = new Size(FITW, ROW),
                Text = "Fit", Font = std, UseVisualStyleBackColor = true
            };

            // ════════════════════════════════════════════════════════════════
            //  LEVEL
            // ════════════════════════════════════════════════════════════════
            int y = 10;
            Controls.Add(MakeSec("LEVEL", y));

            y += 20; // 30 — Level Top row
            lblYMax = RowLbl("Level Top (dBFS)", y);
            numYMax.Location = new Point(CTLX, y); numYMax.Size = new Size(CTLW, ROW);
            numYMax.Increment = 10; numYMax.DecimalPlaces = 0;
            numYMax.Minimum = -200; numYMax.Maximum = 0; numYMax.Font = std;
            btnFitY = FitBtn(y);

            y += 25; // 54 — Range row
            lblYMin = RowLbl("Range (dBFS)", y);
            cmbYRange = new ComboBox {
                Location = new Point(CTLX, y), Size = new Size(CTLW, ROW),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = std
            };

            y += 25; // 79 — Vertical Unit row
            lblVertUnit = RowLbl("Vertical Unit", y);
            cmbVertUnit = new ComboBox {
                Location = new Point(CTLX, y), Size = new Size(CTLW, ROW),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = std
            };
            cmbVertUnit.Items.AddRange(new object[] { "dBFS", "dBV" });

            Controls.AddRange(new Control[] { lblYMax, numYMax, btnFitY, lblYMin, cmbYRange,
                                              lblVertUnit, cmbVertUnit });

            // ════════════════════════════════════════════════════════════════
            //  PHASE
            // ════════════════════════════════════════════════════════════════
            y += 27; // 106
            Controls.Add(MakeSec("PHASE", y));

            y += 20; // 104 — Phase max row
            lblY2Max = RowLbl("Phase max (°)", y);
            numY2Max.Location = new Point(CTLX, y); numY2Max.Size = new Size(CTLW, ROW);
            numY2Max.Increment = 15; numY2Max.DecimalPlaces = 0;
            numY2Max.Minimum = -360; numY2Max.Maximum = 360; numY2Max.Font = std;
            btnFitY2 = FitBtn(y);

            y += 25; // 125 — Phase range row
            lblY2Min = RowLbl("Phase range (°)", y);
            cmbY2Range = new ComboBox {
                Location = new Point(CTLX, y), Size = new Size(CTLW, ROW),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = std
            };

            Controls.AddRange(new Control[] { lblY2Max, numY2Max, btnFitY2, lblY2Min, cmbY2Range });

            // ════════════════════════════════════════════════════════════════
            //  FREQUENCY RANGE
            // ════════════════════════════════════════════════════════════════
            y += 27; // 152
            Controls.Add(MakeSec("FREQUENCY RANGE", y));

            y += 20; // 178 — TabControl
            tabX = new TabControl {
                Location = new Point(PL, y), Size = new Size(CW, 83),
                Font = std, TabIndex = 4, SelectedIndex = 0
            };

            // ── Mini/Maxi tab ─────────────────────────────────────────────
            tabMinMax = new TabPage { Text = "Mini/Maxi", UseVisualStyleBackColor = true };
            tabMinMax.SuspendLayout();
            lblFMin = new Label { Text = "F min (Hz)", AutoSize = true, Location = new Point(6, 10), Font = std };
            numFMin.Location = new Point(80, 7); numFMin.Size = new Size(92, ROW); numFMin.Font = std;
            numFMin.Maximum = 1_000_000m; numFMin.DecimalPlaces = 3;
            numFMin.Increment = new decimal(new int[] { 1, 0, 0, 131072 }); // 0.001
            lblFMax = new Label { Text = "F max (Hz)", AutoSize = true, Location = new Point(6, 36), Font = std };
            numFMax.Location = new Point(80, 33); numFMax.Size = new Size(92, ROW); numFMax.Font = std;
            numFMax.Maximum = 1_000_000m; numFMax.Value = 20000;
            tabMinMax.Controls.AddRange(new Control[] { lblFMin, numFMin, lblFMax, numFMax });
            tabMinMax.ResumeLayout(false); tabMinMax.PerformLayout();

            // ── Center/Span tab ───────────────────────────────────────────
            tabCenterSpan = new TabPage { Text = "Center/Span", UseVisualStyleBackColor = true };
            tabCenterSpan.SuspendLayout();
            lblCenter = new Label { Text = "Center (Hz)", AutoSize = true, Location = new Point(6, 10), Font = std };
            numCenter.Location = new Point(80, 7); numCenter.Size = new Size(92, ROW); numCenter.Font = std;
            numCenter.Maximum = 1_000_000m;
            lblSpan = new Label { Text = "Span (Hz)", AutoSize = true, Location = new Point(6, 36), Font = std };
            numSpan.Location = new Point(80, 33); numSpan.Size = new Size(92, ROW); numSpan.Font = std;
            numSpan.Maximum = 1_000_000m;
            tabCenterSpan.Controls.AddRange(new Control[] { lblCenter, numCenter, lblSpan, numSpan });
            tabCenterSpan.ResumeLayout(false); tabCenterSpan.PerformLayout();

            tabX.Controls.AddRange(new Control[] { tabMinMax, tabCenterSpan });
            Controls.Add(tabX);


            // ════════════════════════════════════════════════════════════════
            //  BUTTONS
            // ════════════════════════════════════════════════════════════════
            y += 90; // 261
            btnDefault = new Button {
                Location = new Point(PL, y), Size = new Size(80, 26),
                Text = "Default", Font = std, UseVisualStyleBackColor = true
            };
            btnOk = new Button {
                Location = new Point(PL + CW - 64, y), Size = new Size(64, 26),
                Text = "OK", Font = std, DialogResult = DialogResult.OK,
                UseVisualStyleBackColor = false,
                BackColor = blue, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnOk.FlatAppearance.BorderSize = 0;
            Controls.AddRange(new Control[] { btnDefault, btnOk });

            // ── Form ──────────────────────────────────────────────────────
            AutoScaleMode   = AutoScaleMode.Font;
            ClientSize      = new Size(PL + CW + PL, y + 26 + 10); // 280 × 297
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false; MinimizeBox = false;
            ShowInTaskbar   = false;
            StartPosition   = FormStartPosition.CenterParent;
            Text            = "Axis Parameters";
            Font            = std;
            BackColor       = SystemColors.Window;
            AcceptButton    = btnOk;

            ((ISupportInitialize)numYMax).EndInit();
            ((ISupportInitialize)numY2Max).EndInit();
            ((ISupportInitialize)numFMin).EndInit();
            ((ISupportInitialize)numFMax).EndInit();
            ((ISupportInitialize)numCenter).EndInit();
            ((ISupportInitialize)numSpan).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        // ── Field declarations ────────────────────────────────────────────
        private Label lblYMax = null!, lblYMin = null!, lblVertUnit = null!;
        private NumericUpDown numYMax = null!;
        private ComboBox cmbYRange = null!, cmbVertUnit = null!;
        private Label lblY2Max = null!, lblY2Min = null!;
        private NumericUpDown numY2Max = null!;
        private ComboBox cmbY2Range = null!;
        private TabControl tabX = null!;
        private TabPage tabMinMax = null!, tabCenterSpan = null!;
        private Label lblFMin = null!, lblFMax = null!;
        private NumericUpDown numFMin = null!, numFMax = null!;
        private Label lblCenter = null!, lblSpan = null!;
        private NumericUpDown numCenter = null!, numSpan = null!;
        private Button btnOk = null!, btnDefault = null!;
        private Button btnFitY = null!, btnFitY2 = null!;
    }
}
#nullable restore
