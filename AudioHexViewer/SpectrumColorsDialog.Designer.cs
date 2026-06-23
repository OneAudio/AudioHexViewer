#nullable enable
using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    partial class SpectrumColorsDialog
    {
        private System.ComponentModel.IContainer? components = null;

        // ── Traces ───────────────────────────────────────────────────────────
        internal Button btnFftL, btnFftR;
        internal NumericUpDown nudFftWidth;
        internal CheckBox chkNegTraces;

        // ── Trace Hold ───────────────────────────────────────────────────────
        internal Button btnHoldMinL, btnHoldMinR;
        internal Button btnPkL, btnPkR;
        internal NumericUpDown nudHoldMinWidth, nudPkWidth;
        internal CheckBox chkNegHold;

        // ── Overlays ─────────────────────────────────────────────────────────
        internal Button btnOv1, btnOv2, btnOv3, btnOv4;
        internal Button btnOv5, btnOv6, btnOv7, btnOv8;
        internal NumericUpDown nudOvWidth;
        internal ComboBox cmbOvStyle;
        internal CheckBox chkNegOverlays;

        // ── Chart ────────────────────────────────────────────────────────────
        internal Button btnGridMaj, btnGridMin, btnBack;
        internal Button btnInfoText, btnLabMaj, btnLabMin;
        internal CheckBox chkNegChart;

        // ── Action buttons ───────────────────────────────────────────────────
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

            // ── SHARED column geometry ────────────────────────────────────────
            // Both left and right columns use the same proportions so sub-column
            // spacing is visually identical in all sections.
            //
            // Sub-col layout per row:  [LabelL(LW)][SwL(SW)][GAP][LabelR(LW)][SwR(SW)]
            //
            const int LW  = 68;   // label width
            const int SW  = 22;   // swatch size
            const int GAP = 10;   // gap between the two sub-columns
            const int S1  = LW + 2;               // left swatch x  = 70
            const int LCR = S1 + SW;              // left col right  = 92
            const int MID = LCR + GAP;            // right label x   = 102
            const int S2  = MID + LW + 2;         // right swatch x  = 172
            const int WC  = S2 + SW;              // column width    = 194

            // Invert checkbox in section header
            const int CHKX = WC - 16;             // 178
            const int INVX = CHKX - 54;           // 124

            // Thick NUD: right-aligned under each sub-column
            // "T:" label (22px) + NUD (40px) = 62px, right = LCR/WC
            const int NUD_W  = 40;
            const int TLBL_W = 22;
            const int NUD1X  = LCR - NUD_W;       // 52
            const int TH1X   = NUD1X - TLBL_W;    // 30  ← always positive ✓
            const int NUD2X  = WC  - NUD_W;       // 154
            const int TH2X   = NUD2X - TLBL_W;    // 132

            // Overlay style combo
            const int CMB_W   = 88;
            const int OV_CMBX = WC - CMB_W;       // 106

            // ── Helpers ───────────────────────────────────────────────────────
            Panel MakeSection(string title, CheckBox? chk, int w = WC)
            {
                var p = new Panel { Width = w, Height = 22, Margin = new Padding(0, 8, 0, 2) };
                p.Controls.Add(new Label {
                    Text = title, Font = boldSec, ForeColor = blue,
                    AutoSize = false, Location = new Point(0, 1),
                    Size = new Size(chk != null ? INVX - 4 : w, 16)
                });
                p.Controls.Add(new Panel {
                    Location = new Point(0, 18), Size = new Size(w, 1), BackColor = blueLine
                });
                if (chk != null) {
                    p.Controls.Add(new Label {
                        Text = "Invert", Font = smallGray, ForeColor = Color.Gray,
                        AutoSize = false, Location = new Point(INVX, 3),
                        Size = new Size(52, 14), TextAlign = ContentAlignment.MiddleRight
                    });
                    chk.Text = ""; chk.AutoSize = false;
                    chk.Location = new Point(CHKX, 2); chk.Size = new Size(16, 16);
                    p.Controls.Add(chk);
                }
                return p;
            }

            Panel MakeRow(int w = WC, int h = 24) =>
                new Panel { Width = w, Height = h, Margin = new Padding(0, 1, 0, 1) };

            Label MakeLbl(string text, int x) => new Label {
                Text = text, Location = new Point(x, 4),
                Size = new Size(LW, 16), TextAlign = ContentAlignment.MiddleRight, Font = stdFont
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

            NumericUpDown MakeNud(int x, int w = NUD_W) => new NumericUpDown {
                Location = new Point(x, 2), Size = new Size(w, 22),
                Minimum = 1, Maximum = 3, Value = 1, DecimalPlaces = 0,
                Increment = 1, TextAlign = HorizontalAlignment.Center, Font = stdFont
            };

            Label MakeTLbl(int x) => new Label {
                Text = "T:", Location = new Point(x, 6), AutoSize = false,
                Size = new Size(TLBL_W, 14), TextAlign = ContentAlignment.MiddleRight,
                Font = smallGray, ForeColor = Color.Gray
            };

            Label MakeOvLbl(string t, int x) => new Label {
                Text = t, Location = new Point(x, 6), AutoSize = false,
                Size = new Size(50, 14), TextAlign = ContentAlignment.MiddleRight,
                Font = smallGray, ForeColor = Color.Gray
            };

            // ── ROOT: TableLayoutPanel [left | sep | right] ───────────────────
            const int SEP_W = 14;   // separator + margins
            int TW = WC + SEP_W + WC + 24;  // total client width

            var tbl = new TableLayoutPanel {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
                Padding = new Padding(12, 8, 12, 8)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, WC));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, SEP_W));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, WC));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(tbl);

            // Vertical separator
            var sepPanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(6, 0, 6, 0) };
            sepPanel.Controls.Add(new Panel {
                Width = 1, Dock = DockStyle.Left, BackColor = Color.FromArgb(200, 200, 200)
            });
            tbl.Controls.Add(sepPanel, 1, 0);

            // ════════════════════════════════════════════════════════════════
            // LEFT COLUMN — TRACES / TRACE HOLD / OVERLAYS
            // ════════════════════════════════════════════════════════════════
            var leftFlow = new FlowLayoutPanel {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, AutoScroll = false, Padding = new Padding(0)
            };
            tbl.Controls.Add(leftFlow, 0, 0);

            // ═══ TRACES ══════════════════════════════════════════════════════
            chkNegTraces = new CheckBox();
            leftFlow.Controls.Add(MakeSection("TRACES", chkNegTraces));

            btnFftL = MakeSwatch(S1); btnFftR = MakeSwatch(S2);
            var rT0 = MakeRow();
            rT0.Controls.AddRange(new Control[] {
                MakeLbl("Data Left",  0),   btnFftL,
                MakeLbl("Data Right", MID), btnFftR
            });
            leftFlow.Controls.Add(rT0);

            nudFftWidth = MakeNud(NUD1X);
            var rTThick = MakeRow(h: 26);
            rTThick.Controls.AddRange(new Control[] { MakeTLbl(TH1X), nudFftWidth });
            leftFlow.Controls.Add(rTThick);

            // ═══ TRACE HOLD ══════════════════════════════════════════════════
            chkNegHold = new CheckBox();
            leftFlow.Controls.Add(MakeSection("TRACE HOLD", chkNegHold));

            btnHoldMinL = MakeSwatch(S1); btnPkL = MakeSwatch(S2);
            var rH0 = MakeRow();
            rH0.Controls.AddRange(new Control[] {
                MakeLbl("Min Left",  0),   btnHoldMinL,
                MakeLbl("Max Left",  MID), btnPkL
            });
            leftFlow.Controls.Add(rH0);

            btnHoldMinR = MakeSwatch(S1); btnPkR = MakeSwatch(S2);
            var rH1 = MakeRow();
            rH1.Controls.AddRange(new Control[] {
                MakeLbl("Min Right", 0),   btnHoldMinR,
                MakeLbl("Max Right", MID), btnPkR
            });
            leftFlow.Controls.Add(rH1);

            nudHoldMinWidth = MakeNud(NUD1X); nudPkWidth = MakeNud(NUD2X);
            var rHThick = MakeRow(h: 26);
            rHThick.Controls.AddRange(new Control[] {
                MakeTLbl(TH1X), nudHoldMinWidth,
                MakeTLbl(TH2X), nudPkWidth
            });
            leftFlow.Controls.Add(rHThick);

            // ═══ OVERLAYS ════════════════════════════════════════════════════
            chkNegOverlays = new CheckBox();
            leftFlow.Controls.Add(MakeSection("OVERLAYS", chkNegOverlays));

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
                leftFlow.Controls.Add(r);
            }
            btnOv1=bArr[0]; btnOv2=bArr[1]; btnOv3=bArr[2]; btnOv4=bArr[3];
            btnOv5=bArr[4]; btnOv6=bArr[5]; btnOv7=bArr[6]; btnOv8=bArr[7];

            nudOvWidth = MakeNud(NUD1X, NUD_W);
            cmbOvStyle = new ComboBox {
                Location = new Point(OV_CMBX, 2), Size = new Size(CMB_W, 22),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = stdFont
            };
            var rOvCommon = MakeRow(h: 26);
            rOvCommon.Controls.AddRange(new Control[] {
                MakeTLbl(TH1X), nudOvWidth,
                MakeOvLbl("Style", OV_CMBX - 54), cmbOvStyle
            });
            leftFlow.Controls.Add(rOvCommon);

            // ════════════════════════════════════════════════════════════════
            // RIGHT COLUMN — CHART + BUTTONS
            // ════════════════════════════════════════════════════════════════
            var rightFlow = new FlowLayoutPanel {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, AutoScroll = false, Padding = new Padding(0)
            };
            tbl.Controls.Add(rightFlow, 2, 0);

            // ═══ CHART ═══════════════════════════════════════════════════════
            chkNegChart = new CheckBox();
            rightFlow.Controls.Add(MakeSection("CHART", chkNegChart));

            (string lL, string lR, Button bL, Button bR)[] chartRows = {
                ("Grid Major",  "Background", btnGridMaj = MakeSwatch(S1), btnBack     = MakeSwatch(S2)),
                ("Grid Minor",  "Info Text",  btnGridMin = MakeSwatch(S1), btnInfoText = MakeSwatch(S2)),
                ("Label Major", "Label Minor",btnLabMaj  = MakeSwatch(S1), btnLabMin   = MakeSwatch(S2)),
            };
            foreach (var (lL, lR, bL, bR) in chartRows) {
                var r = MakeRow();
                r.Controls.AddRange(new Control[] {
                    MakeLbl(lL, 0), bL, MakeLbl(lR, MID), bR
                });
                rightFlow.Controls.Add(r);
            }

            // ═══ BUTTONS — directly below CHART, no filler ═══════════════════
            const int SB_W = 62;
            const int OK_W = 60;
            var rBot = new Panel { Width = WC, Height = 30, Margin = new Padding(0, 14, 0, 0) };
            btnSaveDefault = new Button {
                Text = "Save", Location = new Point(0, 1),
                Size = new Size(SB_W, 28), UseVisualStyleBackColor = true
            };
            btnRestoreDefault = new Button {
                Text = "Restore", Location = new Point(SB_W + 4, 1),
                Size = new Size(SB_W, 28), UseVisualStyleBackColor = true
            };
            btnOk = new Button {
                Text = "OK", Location = new Point(WC - OK_W, 1), Size = new Size(OK_W, 28),
                DialogResult = DialogResult.OK, UseVisualStyleBackColor = false,
                BackColor = Color.FromArgb(0, 90, 160), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnOk.FlatAppearance.BorderSize = 0;
            rBot.Controls.AddRange(new Control[] { btnSaveDefault, btnRestoreDefault, btnOk });
            rightFlow.Controls.Add(rBot);

            // ── Form dimensions ───────────────────────────────────────────────
            // Left content height (approx):
            //   TRACES:    30+24+28    = 82
            //   TRACEH:    30+24+24+28 = 106
            //   OVERLAYS:  30+4×24+28  = 154
            //   Total left ≈ 342
            // Right content:
            //   CHART:     30+3×24     = 102
            //   Buttons:   14+30       = 44
            //   Total right ≈ 146
            // Form = max(342, 146) + top/bottom padding(16) = 358 → round to 370
            AutoScaleMode   = AutoScaleMode.Font;
            ClientSize      = new Size(TW, 370);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false; MinimizeBox = false;
            ShowInTaskbar   = false;
            StartPosition   = FormStartPosition.CenterParent;
            Text            = "Display setup";
            Font            = stdFont;
            BackColor       = SystemColors.Window;
            AcceptButton    = btnOk;
            ResumeLayout(false);
        }
    }
}
#nullable restore
