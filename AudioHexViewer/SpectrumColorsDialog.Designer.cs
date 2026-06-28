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
            // Both left and right columns share the same sub-column proportions.
            //
            // Sub-col layout per row:  [LabelL(LW)][SwL(SW)][GAP][LabelR(LW)][SwR(SW)]
            //
            // LW = 80 so "Background" / "Label Major" / "Label Minor" are fully visible.
            // WC includes +2 so the right swatch border (at WC-2) never touches the
            // panel edge (at WC). This margin also applies to the Invert checkbox.
            //
            const int LW  = 80;   // label width
            const int SW  = 22;   // swatch size
            const int GAP = 10;   // gap between the two sub-columns
            const int S1  = LW + 2;               // left swatch x    = 82
            const int LCR = S1 + SW;              // left col right   = 104
            const int MID = LCR + GAP;            // right label x    = 114
            const int S2  = MID + LW + 2;         // right swatch x   = 196
            const int WC  = S2 + SW + 2;          // column width     = 220
                                                   // (+2: right border safety margin)

            // Invert checkbox in section header  (2 px clear of panel right edge)
            const int CHKX = WC - 18;             // 202
            const int INVX = CHKX - 78;           // 124  (wider: "Invert colors")

            // Thick NUDs – right-aligned under each sub-column
            // "T:" label (22px) + NUD (40px) right edge lands on LCR / WC-2
            const int NUD_W  = 40;
            const int TLBL_W = 38;                 // wide enough for "Thick:" at 8pt
            const int NUD1X  = LCR - NUD_W;       // 64
            const int TH1X   = NUD1X - TLBL_W;    // 26
            const int NUD2X  = WC - 2 - NUD_W;    // 178
            const int TH2X   = NUD2X - TLBL_W;    // 140

            // Overlay style combo (right-aligned, 2 px from panel right edge)
            const int CMB_W   = 88;
            const int OV_CMBX = WC - 2 - CMB_W;   // 130

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
                        Text = "Invert colors", Font = smallGray, ForeColor = Color.Gray,
                        AutoSize = false, Location = new Point(INVX, 3),
                        Size = new Size(74, 14), TextAlign = ContentAlignment.MiddleRight
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
                Text = "Thick:", Location = new Point(x, 6), AutoSize = false,
                Size = new Size(TLBL_W, 14), TextAlign = ContentAlignment.MiddleRight,
                Font = smallGray, ForeColor = Color.Gray
            };

            Label MakeOvLbl(string t, int x) => new Label {
                Text = t, Location = new Point(x, 6), AutoSize = false,
                Size = new Size(50, 14), TextAlign = ContentAlignment.MiddleRight,
                Font = smallGray, ForeColor = Color.Gray
            };

            // ── ROOT: TableLayoutPanel [left | sep | right] ───────────────────
            const int SEP_W = 14;
            int TW = WC + SEP_W + WC + 24;        // total client width = 478

            var tbl = new TableLayoutPanel {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
                Padding = new Padding(12, 8, 12, 8)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, WC));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, SEP_W));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, WC));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Vertical separator
            var sepPanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(6, 0, 6, 0) };
            sepPanel.Controls.Add(new Panel {
                Width = 1, Dock = DockStyle.Left, BackColor = Color.FromArgb(200, 200, 200)
            });
            tbl.Controls.Add(sepPanel, 1, 0);

            // ════════════════════════════════════════════════════════════════
            // LEFT COLUMN — TRACES / OVERLAYS
            // ════════════════════════════════════════════════════════════════
            //
            // IMPORTANT: Margin = new Padding(0) is mandatory.
            // WinForms Panel default Margin is (3,3,3,3). With DockStyle.Fill
            // inside a TableLayoutPanel cell that margin is respected, making
            // the FlowLayoutPanel 6 px narrower than the column (214 instead of
            // 220). Child panels (Width=WC=220) then overflow and their right
            // content is clipped, producing the missing-right-border artefact.
            // Setting Margin=0 makes the FlowLayoutPanel fill the full 220 px.
            //
            var leftFlow = new FlowLayoutPanel {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, AutoScroll = false,
                Padding = new Padding(0), Margin = new Padding(0)
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
                cmbOvStyle
            });
            leftFlow.Controls.Add(rOvCommon);

            // ════════════════════════════════════════════════════════════════
            // RIGHT COLUMN — TRACE HOLD / CHART
            // TRACE HOLD moved here from the left column for visual balance:
            //   Left  (TRACES + OVERLAYS)  ≈ 250 px
            //   Right (TRACE HOLD + CHART) ≈ 222 px
            // ════════════════════════════════════════════════════════════════
            var rightFlow = new FlowLayoutPanel {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, AutoScroll = false,
                Padding = new Padding(0), Margin = new Padding(0)  // same fix as leftFlow
            };
            tbl.Controls.Add(rightFlow, 2, 0);

            // ═══ TRACE HOLD ══════════════════════════════════════════════════
            chkNegHold = new CheckBox();
            rightFlow.Controls.Add(MakeSection("TRACES HOLD", chkNegHold));

            btnHoldMinL = MakeSwatch(S1); btnPkL = MakeSwatch(S2);
            var rH0 = MakeRow();
            rH0.Controls.AddRange(new Control[] {
                MakeLbl("Min Left",  0),   btnHoldMinL,
                MakeLbl("Max Left",  MID), btnPkL
            });
            rightFlow.Controls.Add(rH0);

            btnHoldMinR = MakeSwatch(S1); btnPkR = MakeSwatch(S2);
            var rH1 = MakeRow();
            rH1.Controls.AddRange(new Control[] {
                MakeLbl("Min Right", 0),   btnHoldMinR,
                MakeLbl("Max Right", MID), btnPkR
            });
            rightFlow.Controls.Add(rH1);

            nudHoldMinWidth = MakeNud(NUD1X); nudPkWidth = MakeNud(NUD2X);
            var rHThick = MakeRow(h: 26);
            rHThick.Controls.AddRange(new Control[] {
                MakeTLbl(TH1X), nudHoldMinWidth,
                MakeTLbl(TH2X), nudPkWidth
            });
            rightFlow.Controls.Add(rHThick);

            // ═══ CHART ═══════════════════════════════════════════════════════
            chkNegChart = new CheckBox();
            rightFlow.Controls.Add(MakeSection("CHART", chkNegChart));

            (string lL, string lR, Button bL, Button bR)[] chartRows = {
                ("Grid Major",  "Background", btnGridMaj = MakeSwatch(S1), btnBack     = MakeSwatch(S2)),
                ("Grid Minor",  "Top info Text",  btnGridMin = MakeSwatch(S1), btnInfoText = MakeSwatch(S2)),
                ("Label Major", "Label Minor",btnLabMaj  = MakeSwatch(S1), btnLabMin   = MakeSwatch(S2)),
            };
            foreach (var (lL, lR, bL, bR) in chartRows) {
                var r = MakeRow();
                r.Controls.AddRange(new Control[] {
                    MakeLbl(lL, 0), bL, MakeLbl(lR, MID), bR
                });
                rightFlow.Controls.Add(r);
            }

            // ════════════════════════════════════════════════════════════════
            // BOTTOM PANEL — Save / Restore / OK (full dialog width)
            // ════════════════════════════════════════════════════════════════
            const int SB_W = 62;
            const int OK_W = 60;
            var pBot = new Panel { Dock = DockStyle.Bottom, Height = 44 };
            pBot.Controls.Add(new Panel {
                Dock = DockStyle.Top, Height = 1,
                BackColor = Color.FromArgb(200, 200, 200)
            });
            int bY = 8;
            btnSaveDefault = new Button {
                Text = "Save", Location = new Point(12, bY),
                Size = new Size(SB_W, 28), UseVisualStyleBackColor = true
            };
            btnRestoreDefault = new Button {
                Text = "Restore", Location = new Point(12 + SB_W + 4, bY),
                Size = new Size(SB_W, 28), UseVisualStyleBackColor = true
            };
            btnOk = new Button {
                Text = "OK", Location = new Point(TW - 12 - OK_W, bY),
                Size = new Size(OK_W, 28),
                DialogResult = DialogResult.OK, UseVisualStyleBackColor = false,
                BackColor = Color.FromArgb(0, 90, 160), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnOk.FlatAppearance.BorderSize = 0;
            pBot.Controls.AddRange(new Control[] { btnSaveDefault, btnRestoreDefault, btnOk });

            // ── Form dimensions ───────────────────────────────────────────────
            // Left  (TRACES + OVERLAYS)  : 86 + 164 = 250 px  ← dominant
            // Right (TRACE HOLD + CHART) : 112 + 110 = 222 px
            // Table: 250 + 16 (tbl.Padding v) = 266 px
            // pBot:  44 px
            // Total: 266 + 44 = 310 px  →  320 (10 px breathing room)
            AutoScaleMode   = AutoScaleMode.Font;
            ClientSize      = new Size(TW, 320);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false; MinimizeBox = false;
            ShowInTaskbar   = false;
            StartPosition   = FormStartPosition.CenterParent;
            Text            = "Display setup";
            Font            = stdFont;
            BackColor       = SystemColors.Window;
            AcceptButton    = btnOk;

            Controls.Add(tbl);    // DockStyle.Fill  – processed last by WinForms
            Controls.Add(pBot);   // DockStyle.Bottom – processed first (higher index)

            // ── Context help Name assignments ─────────────────────────────
            btnFftL.Name = "btnFftL";
            btnFftR.Name = "btnFftR";
            nudFftWidth.Name = "nudFftWidth";
            chkNegTraces.Name = "chkNegTraces";
            btnHoldMinL.Name = "btnHoldMinL";
            btnHoldMinR.Name = "btnHoldMinR";
            btnPkL.Name = "btnPkL";
            btnPkR.Name = "btnPkR";
            nudHoldMinWidth.Name = "nudHoldMinWidth";
            nudPkWidth.Name = "nudPkWidth";
            chkNegHold.Name = "chkNegHold";
            btnOv1.Name = "btnOv1";
            btnOv2.Name = "btnOv2";
            btnOv3.Name = "btnOv3";
            btnOv4.Name = "btnOv4";
            btnOv5.Name = "btnOv5";
            btnOv6.Name = "btnOv6";
            btnOv7.Name = "btnOv7";
            btnOv8.Name = "btnOv8";
            nudOvWidth.Name = "nudOvWidth";
            cmbOvStyle.Name = "cmbOvStyle";
            chkNegOverlays.Name = "chkNegOverlays";
            btnGridMaj.Name = "btnGridMaj";
            btnGridMin.Name = "btnGridMin";
            btnBack.Name = "btnBack";
            btnInfoText.Name = "btnInfoText";
            btnLabMaj.Name = "btnLabMaj";
            btnLabMin.Name = "btnLabMin";
            chkNegChart.Name = "chkNegChart";
            btnSaveDefault.Name = "btnSaveDefault";
            btnRestoreDefault.Name = "btnRestoreDefault";
            btnOk.Name = "btnOk";
            ResumeLayout(false);
        }
    }
}
#nullable restore
