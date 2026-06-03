#nullable enable
using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    partial class SpectrumColorsDialog
    {
        private System.ComponentModel.IContainer? components = null;

        internal Button btnFftL, btnFftR, btnPkL, btnPkR;
        internal NumericUpDown nudFftWidth, nudPkWidth;
        internal CheckBox chkNegTraces;
        internal Button btnOv1, btnOv2, btnOv3, btnOv4;
        internal Button btnOv5, btnOv6, btnOv7, btnOv8;
        internal NumericUpDown nudOvWidth;
        internal ComboBox cmbOvStyle;
        internal CheckBox chkNegOverlays;
        internal Button btnGridMaj, btnGridMin, btnBack;
        internal Button btnInfoText, btnLabMaj, btnLabMin;
        internal CheckBox chkNegChart;
        internal Button btnSaveDefault, btnRestoreDefault, btnOk;

        protected override void Dispose(bool disposing)
        {
            if (disposing) components?.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Font stdFont   = new("Segoe UI", 9f);
            Font smallGray = new("Segoe UI", 8f);
            Font boldSec   = new("Segoe UI Semibold", 8.5f);
            Color blue     = Color.FromArgb(0, 90, 160);
            Color blueLine = Color.FromArgb(180, 210, 240);
            Color bdrCol   = Color.FromArgb(100, 100, 100);

            // Column geometry — right column shifted 12px left vs previous version
            // → right margin within content = 12px = form left/right padding
            const int LW  = 74;   // label width
            const int SW  = 22;   // swatch size
            const int S1  = LW + 2;           // left swatch x = 76
            const int LCR = S1 + SW;           // left col right = 98

            const int MID = 166;              // right column label x (was 178, -12)
            const int S2  = MID + LW + 2;    // right swatch x = 242
            const int RCR = S2 + SW;          // right col right = 264 = W

            const int NUD1X = LCR - 44;       // 54  (right = LCR = 98)
            const int NUD2X = RCR - 44;       // 220 (right = RCR = 264)
            const int TH1X  = NUD1X - 52;     // 2
            const int TH2X  = NUD2X - 52;     // 168

            // Invert checkbox: right = RCR, "Invert" label before it
            const int CHKX = RCR - 16;        // 248
            const int INVX = CHKX - 52;       // 196

            // Overlay common controls — LEFT-aligned, same x
            // Line style combo right = RCR, width = 90
            // Thickness NUD: same x, reduced to 44px (standard NUD size)
            const int CMB_W  = 90;            // line style combo width
            const int OV_CMB_X = RCR - CMB_W; // 174 (same x for both controls)
            const int OV_LW  = 70;            // overlay label width
            const int OV_LX  = OV_CMB_X - 2 - OV_LW; // 102

            const int W = RCR;                // 264 — content width = RCR

            // ── Helpers ───────────────────────────────────────────────────────
            Panel MakeSection(string title, CheckBox chk)
            {
                var p = new Panel { Width = W, Height = 22, Margin = new Padding(0, 10, 0, 2) };
                p.Controls.Add(new Label {
                    Text = title, Font = boldSec, ForeColor = blue,
                    AutoSize = false, Location = new Point(0, 1), Size = new Size(INVX - 4, 16)
                });
                p.Controls.Add(new Panel {
                    Location = new Point(0, 18), Size = new Size(W, 1), BackColor = blueLine
                });
                p.Controls.Add(new Label {
                    Text = "Invert", Font = smallGray, ForeColor = Color.Gray,
                    AutoSize = false, Location = new Point(INVX, 3),
                    Size = new Size(48, 14), TextAlign = ContentAlignment.MiddleRight
                });
                chk.Text = ""; chk.AutoSize = false;
                chk.Location = new Point(CHKX, 2); chk.Size = new Size(16, 16);
                p.Controls.Add(chk);
                return p;
            }

            Panel MakeRow(int h = 24) =>
                new Panel { Width = W, Height = h, Margin = new Padding(0, 1, 0, 1) };

            Label MakeLbl(string text, int x, int w = LW) => new Label {
                Text = text, Location = new Point(x, 4),
                Size = new Size(w, 16), TextAlign = ContentAlignment.MiddleRight,
                Font = stdFont
            };

            Button MakeSwatch(int x) {
                var b = new Button {
                    Location = new Point(x, 3), Size = new Size(SW, SW),
                    FlatStyle = FlatStyle.Flat, Text = "", TabStop = false,
                    Cursor = Cursors.Hand, UseVisualStyleBackColor = false
                };
                b.FlatAppearance.BorderSize = 1;
                b.FlatAppearance.BorderColor = bdrCol;
                return b;
            }

            NumericUpDown MakeNud(int x, int w = 44) => new NumericUpDown {
                Location = new Point(x, 2), Size = new Size(w, 22),
                Minimum = 1, Maximum = 3, Value = 1, DecimalPlaces = 0,
                Increment = 1, TextAlign = HorizontalAlignment.Center, Font = stdFont
            };

            Label MakeSmall(string t, int x) => new Label {
                Text = t, Location = new Point(x, 6), AutoSize = false,
                Size = new Size(50, 14), TextAlign = ContentAlignment.MiddleRight,
                Font = smallGray, ForeColor = Color.Gray
            };

            var root = new FlowLayoutPanel {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, AutoScroll = false, Padding = new Padding(0)
            };
            Controls.Add(root);

            // ═══ TRACES ═══════════════════════════════════════════════════════
            chkNegTraces = new CheckBox();
            root.Controls.Add(MakeSection("TRACES", chkNegTraces));

            btnFftL = MakeSwatch(S1); btnPkL = MakeSwatch(S2);
            var rT0 = MakeRow();
            rT0.Controls.AddRange(new Control[] {
                MakeLbl("Data Left",    0),  btnFftL,
                MakeLbl("PkHold Left", MID), btnPkL
            });
            root.Controls.Add(rT0);

            btnFftR = MakeSwatch(S1); btnPkR = MakeSwatch(S2);
            var rT1 = MakeRow();
            rT1.Controls.AddRange(new Control[] {
                MakeLbl("Data Right",   0),   btnFftR,
                MakeLbl("PkHold Right", MID), btnPkR
            });
            root.Controls.Add(rT1);

            nudFftWidth = MakeNud(NUD1X); nudPkWidth = MakeNud(NUD2X);
            var rTThick = MakeRow(26);
            rTThick.Controls.AddRange(new Control[] {
                MakeSmall("Thick:", TH1X), nudFftWidth,
                MakeSmall("Thick:", TH2X), nudPkWidth
            });
            root.Controls.Add(rTThick);

            // ═══ OVERLAYS ═════════════════════════════════════════════════════
            chkNegOverlays = new CheckBox();
            root.Controls.Add(MakeSection("OVERLAYS", chkNegOverlays));

            (string lL, string lR)[] ovNames = {
                ("Overlay 1","Overlay 5"),("Overlay 2","Overlay 6"),
                ("Overlay 3","Overlay 7"),("Overlay 4","Overlay 8")
            };
            Button[] bArr = new Button[8];
            for (int i = 0; i < 4; i++) {
                bArr[i] = MakeSwatch(S1); bArr[i+4] = MakeSwatch(S2);
                var r = MakeRow();
                r.Controls.AddRange(new Control[] {
                    MakeLbl(ovNames[i].lL, 0), bArr[i],
                    MakeLbl(ovNames[i].lR, MID), bArr[i+4]
                });
                root.Controls.Add(r);
            }
            btnOv1=bArr[0]; btnOv2=bArr[1]; btnOv3=bArr[2]; btnOv4=bArr[3];
            btnOv5=bArr[4]; btnOv6=bArr[5]; btnOv7=bArr[6]; btnOv8=bArr[7];

            var rSep = new Panel { Width = W, Height = 7, Margin = new Padding(0, 3, 0, 0) };
            rSep.Controls.Add(new Panel {
                Location = new Point(20, 4), Size = new Size(W-40, 1),
                BackColor = Color.FromArgb(215, 215, 215)
            });
            root.Controls.Add(rSep);

            // Overlay common: Thick + Style on ONE row, both labels in gray
            // "Thick:" label + NUD on left, "Style" label + combo on right
            nudOvWidth = MakeNud(NUD1X, 44);   // same x as Traces left NUD
            cmbOvStyle = new ComboBox {
                Location = new Point(OV_CMB_X, 2), Size = new Size(CMB_W, 22),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = stdFont
            };
            var rOvCommon = MakeRow(26);
            rOvCommon.Controls.AddRange(new Control[] {
                MakeSmall("Thick:", TH1X),  nudOvWidth,
                MakeSmall("Style",  110),   cmbOvStyle
            });
            root.Controls.Add(rOvCommon);

            // ═══ CHART ════════════════════════════════════════════════════════
            chkNegChart = new CheckBox();
            root.Controls.Add(MakeSection("CHART", chkNegChart));

            (string lL, string lR, Button bL, Button bR)[] chartRows = {
                ("Grid Major",  "Background", btnGridMaj = MakeSwatch(S1), btnBack     = MakeSwatch(S2)),
                ("Grid Minor",  "Info Text",  btnGridMin = MakeSwatch(S1), btnInfoText = MakeSwatch(S2)),
                ("Label Major", "Label Minor",btnLabMaj  = MakeSwatch(S1), btnLabMin   = MakeSwatch(S2)),
            };
            foreach (var (lL, lR, bL, bR) in chartRows) {
                var r = MakeRow();
                r.Controls.AddRange(new Control[] { MakeLbl(lL,0), bL, MakeLbl(lR,MID), bR });
                root.Controls.Add(r);
            }

            // ═══ BUTTONS ══════════════════════════════════════════════════════
            // Save + Restore: small, left. OK right = RCR = W
            const int SB_W = 64;
            const int OK_W = 62;
            var rBot = new Panel { Width = W, Height = 32, Margin = new Padding(0, 34, 0, 0) };
            btnSaveDefault = new Button {
                Text = "Save", Location = new Point(0, 2),
                Size = new Size(SB_W, 28), UseVisualStyleBackColor = true
            };
            btnRestoreDefault = new Button {
                Text = "Restore", Location = new Point(SB_W + 6, 2),
                Size = new Size(SB_W, 28), UseVisualStyleBackColor = true
            };
            btnOk = new Button {
                Text = "OK", Location = new Point(W - OK_W, 2), Size = new Size(OK_W, 28),
                DialogResult = DialogResult.OK, UseVisualStyleBackColor = false,
                BackColor = Color.FromArgb(0, 90, 160), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnOk.FlatAppearance.BorderSize = 0;
            rBot.Controls.AddRange(new Control[] { btnSaveDefault, btnRestoreDefault, btnOk });
            root.Controls.Add(rBot);

            // W=264, form = 264+24 = 288 — right margin = left margin = 12px
            AutoScaleMode   = AutoScaleMode.Font;
            ClientSize      = new Size(W + 24, 488);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false; MinimizeBox = false;
            ShowInTaskbar   = false;
            StartPosition   = FormStartPosition.CenterParent;
            Text            = "Display setup";
            Padding         = new Padding(12, 10, 12, 10);
            Font            = stdFont;
            BackColor       = SystemColors.Window;
            AcceptButton    = btnOk;
            ResumeLayout(false);
        }
    }
}
#nullable restore
