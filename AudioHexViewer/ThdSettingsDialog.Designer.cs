#nullable enable
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    internal partial class ThdSettingsDialog
    {
        private IContainer? components = null;

        private Label lblFlow,    lblFhigh;
        private NumericUpDown nudFlow, nudFhigh;
        private CheckBox chkHPF,  chkLPF;

        private Label lblSfdrFlow,  lblSfdrFhigh;
        private NumericUpDown nudSfdrFlow, nudSfdrFhigh;
        private CheckBox chkSfdrHPF, chkSfdrLPF;

        private Label lblHarms, lblUnit, lblCohAvg;
        private NumericUpDown nudHarms;
        private CheckBox chkShowHarms, chkShowPhase;
        private RadioButton rbDb, rbPct;
        private CheckBox chkCohAvg;

        private Button btnOk, btnCancel;

        private GroupBox grpThd      = new GroupBox { Visible = false };
        private GroupBox grpSfdr     = new GroupBox { Visible = false };
        private GroupBox grpHarmonics= new GroupBox { Visible = false };

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            nudFlow      = new NumericUpDown(); nudFhigh     = new NumericUpDown();
            nudSfdrFlow  = new NumericUpDown(); nudSfdrFhigh = new NumericUpDown();
            nudHarms     = new NumericUpDown();
            ((ISupportInitialize)nudFlow).BeginInit();
            ((ISupportInitialize)nudFhigh).BeginInit();
            ((ISupportInitialize)nudSfdrFlow).BeginInit();
            ((ISupportInitialize)nudSfdrFhigh).BeginInit();
            ((ISupportInitialize)nudHarms).BeginInit();
            SuspendLayout();

            Font std     = new("Segoe UI", 9f);
            Font gS      = new("Segoe UI", 7.5f);
            Font secFont = new("Segoe UI Semibold", 8.5f);
            Color blue     = Color.FromArgb(0, 90, 160);
            Color blueLine = Color.FromArgb(180, 210, 240);

            const int PL   = 12;
            const int CW   = 248;               // content width (réduit)
            const int LW   = 76;                // label width right-aligned
            const int CTLX = PL + LW + 4;       // 92 — NUD x
            const int NUDW = 90;                 // NUD width → right=182
            // All checkboxes (HPF/LPF + Phase + Coherent) aligned at CHKX
            const int CHKX = 206;               // aligned with Phase checkbox

            Panel MakeSec(string title, int y)
            {
                var p = new Panel { Location = new Point(PL, y), Size = new Size(CW, 17),
                    BackColor = SystemColors.Window };
                p.Controls.Add(new Label {
                    Text = title, Font = secFont, ForeColor = blue,
                    AutoSize = false, Location = new Point(0, 0), Size = new Size(CW, 14)
                });
                p.Controls.Add(new Panel {
                    Location = new Point(0, 15), Size = new Size(CW, 1), BackColor = blueLine
                });
                return p;
            }

            Label RowLbl(string text, int y) => new Label {
                Text = text, Font = std, Location = new Point(PL, y + 4),
                Size = new Size(LW, 14), TextAlign = ContentAlignment.MiddleRight,
                AutoSize = false
            };

            void SetNud(NumericUpDown n, int y, decimal max, decimal inc = 10m, int dp = 1) {
                n.Location = new Point(CTLX, y); n.Size = new Size(NUDW, 22); n.Font = std;
                n.DecimalPlaces = dp; n.Maximum = max; n.Increment = inc;
                // Enter validates without closing (no AcceptButton on form)
                n.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) e.SuppressKeyPress = true; };
            }

            CheckBox MakeChk(string text, int x, int y, int tab) => new CheckBox {
                Text = text, AutoSize = true, Font = std,
                Location = new Point(x, y + 3), TabIndex = tab,
                UseVisualStyleBackColor = true
            };

            // ══ THD FREQUENCY LIMITS ══════════════════════════════════════════
            int y = 10;
            Controls.Add(MakeSec("THD FREQUENCY LIMITS", y));

            y += 20; // 30
            lblFlow  = RowLbl("Flow (Hz)", y);
            SetNud(nudFlow, y, 768000m, 10m);
            chkHPF   = MakeChk("HPF", CHKX, y, 1);

            y += 26; // 56
            lblFhigh = RowLbl("Fhigh (Hz)", y);
            SetNud(nudFhigh, y, 768000m, 10m);
            chkLPF   = MakeChk("LPF", CHKX, y, 3);

            Controls.AddRange(new Control[] { lblFlow, nudFlow, chkHPF, lblFhigh, nudFhigh, chkLPF });

            // ══ SFDR FREQUENCY LIMITS ═════════════════════════════════════════
            y += 30; // 86
            Controls.Add(MakeSec("SFDR FREQUENCY LIMITS", y));

            y += 20; // 106
            lblSfdrFlow  = RowLbl("Flow (Hz)", y);
            SetNud(nudSfdrFlow, y, 768000m, 10m);
            chkSfdrHPF   = MakeChk("HPF", CHKX, y, 5);

            y += 26; // 132
            lblSfdrFhigh = RowLbl("Fhigh (Hz)", y);
            SetNud(nudSfdrFhigh, y, 768000m, 10m);
            chkSfdrLPF   = MakeChk("LPF", CHKX, y, 7);

            Controls.AddRange(new Control[] { lblSfdrFlow, nudSfdrFlow, chkSfdrHPF,
                                               lblSfdrFhigh, nudSfdrFhigh, chkSfdrLPF });

            // ══ HARMONICS ═════════════════════════════════════════════════════
            y += 30; // 162
            Controls.Add(MakeSec("HARMONICS", y));

            // Number [nud] ☑ Show ☑ Phase  — Phase at CHKX=206
            y += 20; // 182
            lblHarms = RowLbl("Number", y);
            nudHarms.Location = new Point(CTLX, y); nudHarms.Size = new Size(44, 22);
            nudHarms.Font = std; nudHarms.Minimum = 2; nudHarms.Maximum = 20; nudHarms.Value = 5;
            nudHarms.TabIndex = 8;
            int showX = CTLX + 44 + 6;               // 142
            int phaseX = CHKX;                        // 206 — aligned with HPF/LPF
            chkShowHarms = MakeChk("Show",  showX,  y, 9);
            chkShowPhase = MakeChk("Phase", phaseX, y, 10);

            Controls.AddRange(new Control[] { lblHarms, nudHarms, chkShowHarms, chkShowPhase });

            // THD unit  ● dB  ○ %
            y += 26; // 208
            lblUnit = RowLbl("THD unit", y);
            rbDb  = new RadioButton { Text = "dB", AutoSize = true, Font = std,
                Location = new Point(CTLX, y + 3), TabIndex = 11, UseVisualStyleBackColor = true };
            rbPct = new RadioButton { Text = "%",  AutoSize = true, Font = std,
                Location = new Point(CTLX + 52, y + 3), TabIndex = 12, UseVisualStyleBackColor = true };
            Controls.AddRange(new Control[] { lblUnit, rbDb, rbPct });

            // Coherent averaging  □ — checkbox at CHKX, no text, label AutoSize
            y += 26; // 234
            lblCohAvg = new Label {
                Text = "Coherent averaging", Font = std,
                Location = new Point(PL, y + 4), AutoSize = true
            };
            chkCohAvg = new CheckBox {
                Text = "", AutoSize = false, Size = new Size(16, 16),
                Location = new Point(CHKX, y + 5), TabIndex = 13,
                UseVisualStyleBackColor = true
            };
            Controls.AddRange(new Control[] { lblCohAvg, chkCohAvg });

            // ══ BUTTONS ═══════════════════════════════════════════════════════
            y += 32; // 266
            btnCancel = new Button {
                DialogResult = DialogResult.Cancel, Text = "Cancel", Font = std,
                Location = new Point(PL, y), Size = new Size(76, 27),
                UseVisualStyleBackColor = true, TabIndex = 14
            };
            btnOk = new Button {
                DialogResult = DialogResult.OK, Text = "OK", Font = std,
                Location = new Point(PL + CW - 76, y), Size = new Size(76, 27),
                BackColor = blue, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, TabIndex = 15
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += btnOk_Click;
            Controls.AddRange(new Control[] { btnCancel, btnOk });

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode       = AutoScaleMode.Font;
            ClientSize          = new Size(PL + CW + PL, y + 27 + 10); // 272 × 303
            FormBorderStyle     = FormBorderStyle.FixedDialog;
            MaximizeBox         = false; MinimizeBox = false;
            StartPosition       = FormStartPosition.CenterParent;
            Text                = "THD Settings";
            Font                = std;
            BackColor           = SystemColors.Window;
            // Pas d'AcceptButton : Enter valide les NUD sans fermer le dialog

            ((ISupportInitialize)nudFlow).EndInit();
            ((ISupportInitialize)nudFhigh).EndInit();
            ((ISupportInitialize)nudSfdrFlow).EndInit();
            ((ISupportInitialize)nudSfdrFhigh).EndInit();
            ((ISupportInitialize)nudHarms).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
#nullable restore
