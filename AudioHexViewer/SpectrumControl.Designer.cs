namespace AudioHexViewer
{
    partial class SpectrumControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Code généré par concepteur

        private void InitializeComponent()
        {
            panelTop            = new Panel();
            pnlAcq              = new Panel();
            pbAcq               = new ProgressBar();
            lblAcqPct           = new Label();
            lblFftTicker        = new Label();
            lblCursors          = new Label();
            label1              = new Label();
            lblAvgProgress      = new Label();
            btnOverlay          = new Button();
            chkPeakHold         = new CheckBox();
            btnResetPeakHold    = new Button();
            chkEnableThd        = new CheckBox();
            btnThdSettings      = new Button();
            btnResetAvg         = new Button();
            lblWindow           = new Label();
            comboWindow         = new ComboBox();
            lblAveraging        = new Label();
            comboAveraging      = new ComboBox();
            lblUnits            = new Label();
            comboLevelUnit      = new ComboBox();
            lblChannel          = new Label();
            comboChannel        = new ComboBox();
            chkDcMode           = new CheckBox();
            lblWindowInfo       = new Label();
            btnRun              = new Button();
            chkLogX             = new CheckBox();
            // Cursor buttons (individual, like Oscilloscope)
            btnC1               = new Button();
            btnC1Track          = new Button();
            btnC1PrevPk         = new Button();
            btnC1Pk             = new Button();
            btnC1NextPk         = new Button();
            btnC1Set            = new Button();
            btnC2               = new Button();
            btnC2Track          = new Button();
            btnC2PrevPk         = new Button();
            btnC2Pk             = new Button();
            btnC2NextPk         = new Button();
            btnC2Set            = new Button();
            // tlpCursors kept as a thin wrapper so CursorsBar.cs binds without change
            tlpCursors          = new TableLayoutPanel();

            // ── New X1/X2 cursor controls ──
            btnX1           = new Button();
            btnX1Chan       = new Button();
            btnX1Set        = new Button();
            btnX1Peak       = new Button();
            btnDispGraph    = new Button();
            btnX2           = new Button();
            btnX2Chan       = new Button();
            btnX2Set        = new Button();
            btnX2Peak       = new Button();
            btnDispLabel    = new Button();
            lblColCursor    = new Label();
            lblColChannel   = new Label();
            lblColSetVal    = new Label();
            lblColPeak      = new Label();
            lblColDisplay   = new Label();
            tlpPhi              = new TableLayoutPanel();
            btnPhiEnable        = new CheckBox();
            btnPhiUnwrap        = new CheckBox();
            lblPhaseGroup       = new Label();
            tlpNorm             = new TableLayoutPanel();
            btnFrfRefCapture    = new CheckBox();
            btnFrfNormToggle    = new CheckBox();
            lblNormGroup        = new Label();
            // Separator labels
            sep1                = new Label();
            sep2                = new Label();
            sep3                = new Label();
            sep4                = new Label();
            sep5                = new Label();
            chartSpectrum       = new System.Windows.Forms.DataVisualization.Charting.Chart();
            grpRightStats       = new GroupBox();
            lblRightMeas        = new Label();
            lblRightHeader      = new Label();
            lblRightRms         = new Label();
            lblRightPeak        = new Label();
            lblRightRmsV        = new Label();
            lblRightPeakV       = new Label();
            grpLeftStats        = new GroupBox();
            lblLeftMeas         = new Label();
            lblLeftHeader       = new Label();
            lblLeftRms          = new Label();
            lblLeftPeak         = new Label();
            lblLeftRmsV         = new Label();
            lblLeftPeakV        = new Label();
            chkCursorL          = new CheckBox();
            chkCursorR          = new CheckBox();

            tlpCursors.SuspendLayout();
            tlpPhi.SuspendLayout();
            tlpNorm.SuspendLayout();
            panelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)chartSpectrum).BeginInit();
            SuspendLayout();

            // ── Fonts (same as OscilloscopeControl) ──────────────────────────
            var gS   = new Font("Segoe UI", 7.5f);
            var std9 = new Font("Segoe UI", 9f);

            // ══════════════════════════════════════════════════════════════════
            //  LAYOUT PANELTOP — same grid as OscilloscopeControl
            //  y=4   → group labels  (gS, GrayText)
            //  y=18  → controls ROW 1  (h=24)
            //  y=46  → group labels  ROW 2
            //  y=60  → controls ROW 2  (h=24)
            //
            //  x=8→100    CHANNEL
            //  x=112→298  WINDOWING (84) + AVERAGING (66) side by side
            //  x=300      sep2
            //  x=308→424  PEAK HOLD (56) + THD (56) / Reset + Settings
            //  x=428      sep3
            //  x=436→494  dB UNIT (56) / LOG
            //  x=496      sep4
            //  x=504→618  PHASE (47) + NORM (60)  [visible Bode only]
            //  x=619      sep5
            //  x=695→940  CURSORS X1/X2 (exact oscilloscope positions)
            //  x=952      sep (Overlay zone)
            //  x=965      Overlay
            //  x=1050     RUN
            // ══════════════════════════════════════════════════════════════════

            // ── panelTop ─────────────────────────────────────────────────────
            panelTop.BackColor = System.Drawing.SystemColors.Control;
            // ── Bordure blanche 2px ───────────────────────────────────
            { var bc = Color.White;
              panelTop.Controls.Add(new Panel { Dock = DockStyle.Top,    Height = 2, BackColor = bc });
              panelTop.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 2, BackColor = bc });
              panelTop.Controls.Add(new Panel { Dock = DockStyle.Left,   Width  = 2, BackColor = bc });
              panelTop.Controls.Add(new Panel { Dock = DockStyle.Right,  Width  = 2, BackColor = bc }); }
            panelTop.Dock      = DockStyle.Top;
            panelTop.Location  = new Point(0, 0);
            panelTop.Name      = "panelTop";
            panelTop.Size      = new Size(1138, 90);
            panelTop.TabIndex  = 1;
            panelTop.Controls.AddRange(new System.Windows.Forms.Control[]
            {
                // Group labels row 1
                lblChannel, lblWindow, lblAveraging,
                // Controls row 1
                comboChannel, comboWindow, comboAveraging,
                chkPeakHold, chkEnableThd,
                btnOverlay,
                // Group labels row 2
                // (channel has chkDcMode, others share row-2 label space)
                // Controls row 2
                comboLevelUnit, btnResetAvg,
                btnResetPeakHold, btnThdSettings,
                chkDcMode,
                // Legacy cursor stubs (hidden, kept for CursorsBar.cs binding)
                btnC1, btnC1Track, btnC1PrevPk, btnC1Pk, btnC1NextPk, btnC1Set,
                btnC2, btnC2Track, btnC2PrevPk, btnC2Pk, btnC2NextPk, btnC2Set,
                tlpCursors,
                // ── New X1/X2 cursor controls ──
                lblCursors,
                lblColCursor, lblColChannel, lblColSetVal, lblColPeak, lblColDisplay,
                btnX1, btnX1Chan, btnX1Set, btnX1Peak, btnDispGraph,
                btnX2, btnX2Chan, btnX2Set, btnX2Peak, btnDispLabel,
                // dB Unit / LOG
                lblUnits, chkLogX,
                // Phase
                lblWindowInfo, lblPhaseGroup, tlpPhi,
                // FRF Normalisation (Capture Ref / Norm toggle)
                lblNormGroup, tlpNorm,
                // Separators
                sep1, sep2, sep3, sep4, sep5,
                // Right: RUN (tout-en-un : T ACQ + barre + RUN/STOP owner-drawn)
                lblAvgProgress, btnRun
            });

            // ── 1. CHANNEL  (x=8) ────────────────────────────────────────────
            lblChannel.AutoSize  = true;
            lblChannel.Font      = gS;
            lblChannel.ForeColor = System.Drawing.SystemColors.GrayText;
            lblChannel.Location  = new Point(8, 4);
            lblChannel.Name      = "lblChannel";
            lblChannel.TabIndex  = 6;
            lblChannel.Text      = "CHANNEL MODE";

            comboChannel.DropDownStyle = ComboBoxStyle.DropDownList;
            comboChannel.Font          = std9;
            comboChannel.Location      = new Point(8, 18);
            comboChannel.Name          = "comboChannel";
            comboChannel.Size          = new Size(90, 24);
            comboChannel.TabIndex      = 7;

            chkDcMode.AutoSize  = true;
            chkDcMode.Font      = std9;
            chkDcMode.Location  = new Point(8, 64);
            chkDcMode.Name      = "chkDcMode";
            chkDcMode.Size      = new Size(90, 20);
            chkDcMode.TabIndex  = 8;
            chkDcMode.Text      = "DC mode";
            chkDcMode.UseVisualStyleBackColor = true;

            // ── SEP 1 (after Channel, x=104) ─────────────────────────────────
            sep1.BackColor = System.Drawing.SystemColors.ControlDark;
            sep1.Location  = new Point(104, 6);
            sep1.Name      = "sep1";
            sep1.Size      = new Size(1, 76);
            sep1.TabIndex  = 99;

            // ── 2. WINDOWING  (x=112) ────────────────────────────────────────
            lblWindow.AutoSize  = true;
            lblWindow.Font      = gS;
            lblWindow.ForeColor = System.Drawing.SystemColors.GrayText;
            lblWindow.Location  = new Point(112, 4);
            lblWindow.Name      = "lblWindow";
            lblWindow.TabIndex  = 2;
            lblWindow.Text      = "WINDOWING";

            comboWindow.DropDownStyle = ComboBoxStyle.DropDownList;
            comboWindow.Font          = std9;
            comboWindow.Location      = new Point(112, 18);
            comboWindow.Name          = "comboWindow";
            comboWindow.Size          = new Size(84, 24);   // was 140, -40%
            comboWindow.TabIndex      = 3;

            // ── 3. AVERAGING — alongside WINDOWING (no separator between them) ──
            lblAveraging.AutoSize  = true;
            lblAveraging.Font      = gS;
            lblAveraging.ForeColor = System.Drawing.SystemColors.GrayText;
            lblAveraging.Location  = new Point(200, 4);
            lblAveraging.Name      = "lblAveraging";
            lblAveraging.TabIndex  = 4;
            lblAveraging.Text      = "AVERAGING";

            comboAveraging.DropDownStyle = ComboBoxStyle.DropDownList;
            comboAveraging.Font          = std9;
            comboAveraging.Location      = new Point(200, 18);
            comboAveraging.Name          = "comboAveraging";
            comboAveraging.Size          = new Size(66, 24);
            comboAveraging.TabIndex      = 5;

            btnResetAvg.Location = new Point(200, 60);
            btnResetAvg.Name     = "btnResetAvg";
            btnResetAvg.Size     = new Size(50, 24);
            btnResetAvg.TabIndex = 12;
            btnResetAvg.Text     = "CLEAR";
            btnResetAvg.UseVisualStyleBackColor = true;
            btnResetAvg.Click += BtnResetAvg_Click;

            lblAvgProgress.AutoSize  = false;
            lblAvgProgress.Font      = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblAvgProgress.ForeColor = Color.Firebrick;
            lblAvgProgress.Location  = new Point(254, 60);
            lblAvgProgress.Size      = new Size(42, 24);
            lblAvgProgress.TextAlign = ContentAlignment.MiddleLeft;
            lblAvgProgress.Name      = "lblAvgProgress";
            lblAvgProgress.TabIndex  = 14;
            lblAvgProgress.Text      = "";

            // ── SEP 2 (after Windowing+Averaging, x=300) ─────────────────────
            sep2.BackColor = System.Drawing.SystemColors.ControlDark;
            sep2.Location  = new Point(300, 6);
            sep2.Name      = "sep2";
            sep2.Size      = new Size(1, 76);
            sep2.TabIndex  = 99;

            // ── 4. PEAK HOLD + THD  (x=308) ──────────────────────────────────
            chkPeakHold.Appearance = Appearance.Button;
            chkPeakHold.Font       = std9;
            chkPeakHold.Location   = new Point(308, 18);
            chkPeakHold.Name       = "chkPeakHold";
            chkPeakHold.Size       = new Size(56, 24);
            chkPeakHold.TabIndex   = 17;
            chkPeakHold.Text       = "PEAK H";
            chkPeakHold.TextAlign  = ContentAlignment.MiddleCenter;
            chkPeakHold.UseVisualStyleBackColor = true;
            chkPeakHold.CheckedChanged += chkPeakHold_CheckedChanged;

            btnResetPeakHold.Location = new Point(308, 60);
            btnResetPeakHold.Name     = "btnResetPeakHold";
            btnResetPeakHold.Size     = new Size(56, 24);
            btnResetPeakHold.TabIndex = 18;
            btnResetPeakHold.TabStop  = false;
            btnResetPeakHold.Text     = "Reset";
            btnResetPeakHold.UseVisualStyleBackColor = true;
            btnResetPeakHold.Click += BtnResetPeakHold_Click;

            // ── 5. THD  (x=368) ──────────────────────────────────────────────
            chkEnableThd.Appearance = Appearance.Button;
            chkEnableThd.Font       = std9;
            chkEnableThd.Location   = new Point(368, 18);
            chkEnableThd.Name       = "chkEnableThd";
            chkEnableThd.Size       = new Size(56, 24);
            chkEnableThd.TabIndex   = 30;
            chkEnableThd.Text       = "THD";
            chkEnableThd.TextAlign  = ContentAlignment.MiddleCenter;
            chkEnableThd.UseVisualStyleBackColor = true;
            chkEnableThd.CheckedChanged += ChkEnableThd_CheckedChanged;

            btnThdSettings.Location = new Point(368, 60);
            btnThdSettings.Name     = "btnThdSettings";
            btnThdSettings.Size     = new Size(56, 24);
            btnThdSettings.TabIndex = 31;
            btnThdSettings.TabStop  = false;
            btnThdSettings.Text     = "Settings";
            btnThdSettings.UseMnemonic = false;
            btnThdSettings.UseVisualStyleBackColor = true;
            btnThdSettings.Click += BtnThdSettings_Click;

            // ── SEP 3 (after PEAK+THD, x=428) ────────────────────────────────
            sep3.BackColor = System.Drawing.SystemColors.ControlDark;
            sep3.Location  = new Point(428, 6);
            sep3.Name      = "sep3";
            sep3.Size      = new Size(1, 76);
            sep3.TabIndex  = 99;

            // ── 6. dB UNIT / LOG  (x=436) ────────────────────────────────────
            lblUnits.AutoSize  = true;
            lblUnits.Font      = gS;
            lblUnits.ForeColor = System.Drawing.SystemColors.GrayText;
            lblUnits.Location  = new Point(436, 4);
            lblUnits.Name      = "lblUnits";
            lblUnits.TabIndex  = 13;
            lblUnits.Text      = "dB UNIT";

            comboLevelUnit.DropDownStyle = ComboBoxStyle.DropDownList;
            comboLevelUnit.Font          = std9;
            comboLevelUnit.Location      = new Point(436, 18);
            comboLevelUnit.Name          = "comboLevelUnit";
            comboLevelUnit.Size          = new Size(56, 24);
            comboLevelUnit.TabIndex      = 14;

            chkLogX.Appearance = Appearance.Button;
            chkLogX.Font       = std9;
            chkLogX.Location   = new Point(436, 60);
            chkLogX.Name       = "chkLogX";
            chkLogX.Size       = new Size(56, 24);
            chkLogX.TabIndex   = 11;
            chkLogX.Text       = "LOG";
            chkLogX.TextAlign  = ContentAlignment.MiddleCenter;

            // ── SEP 4 (after dB UNIT, x=496) ─────────────────────────────────
            sep4.BackColor = System.Drawing.SystemColors.ControlDark;
            sep4.Location  = new Point(496, 6);
            sep4.Name      = "sep4";
            sep4.Size      = new Size(1, 76);
            sep4.TabIndex  = 99;

            // ── 7. PHASE φ + NORM  (x=504..618) — visible only in Bode mode ──
            tlpPhi.ColumnCount = 1;
            tlpPhi.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 47F));
            tlpPhi.Controls.Add(btnPhiEnable, 0, 0);
            tlpPhi.Controls.Add(btnPhiUnwrap, 0, 1);
            tlpPhi.Location  = new Point(504, 18);
            tlpPhi.Margin    = new Padding(0);
            tlpPhi.Name      = "tlpPhi";
            tlpPhi.RowCount  = 2;
            tlpPhi.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            tlpPhi.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            tlpPhi.Size      = new Size(47, 66);
            tlpPhi.TabIndex  = 36;

            btnPhiEnable.Appearance = Appearance.Button;
            btnPhiEnable.Dock       = DockStyle.Bottom;
            btnPhiEnable.Margin     = new Padding(0);
            btnPhiEnable.Name       = "btnPhiEnable";
            btnPhiEnable.Size       = new Size(47, 24);
            btnPhiEnable.TabIndex   = 8;
            btnPhiEnable.Text       = "φ";
            btnPhiEnable.Font       = std9;
            btnPhiEnable.TextAlign  = ContentAlignment.MiddleCenter;
            btnPhiEnable.UseVisualStyleBackColor = true;

            btnPhiUnwrap.Appearance = Appearance.Button;
            btnPhiUnwrap.Dock       = DockStyle.Fill;
            btnPhiUnwrap.Margin     = new Padding(0);
            btnPhiUnwrap.Name       = "btnPhiUnwrap";
            btnPhiUnwrap.Size       = new Size(47, 24);
            btnPhiUnwrap.TabIndex   = 9;
            btnPhiUnwrap.Text       = "Uw";
            btnPhiUnwrap.Font       = std9;
            btnPhiUnwrap.TextAlign  = ContentAlignment.MiddleCenter;
            btnPhiUnwrap.UseVisualStyleBackColor = true;

            lblPhaseGroup.AutoSize  = false;
            lblPhaseGroup.Font      = gS;
            lblPhaseGroup.ForeColor = SystemColors.GrayText;
            lblPhaseGroup.Location  = new Point(504, 4);
            lblPhaseGroup.Name      = "lblPhaseGroup";
            lblPhaseGroup.Size      = new Size(47, 14);
            lblPhaseGroup.TabIndex  = 98;
            lblPhaseGroup.Text      = "PHASE";
            lblPhaseGroup.TextAlign = ContentAlignment.MiddleCenter;

            tlpNorm.ColumnCount = 1;
            tlpNorm.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60F));
            tlpNorm.Controls.Add(btnFrfRefCapture, 0, 0);
            tlpNorm.Controls.Add(btnFrfNormToggle, 0, 1);
            tlpNorm.Location = new Point(555, 18);
            tlpNorm.Margin   = new Padding(0);
            tlpNorm.Name     = "tlpNorm";
            tlpNorm.RowCount = 2;
            tlpNorm.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            tlpNorm.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            tlpNorm.Size     = new Size(60, 66);
            tlpNorm.TabIndex = 37;

            btnFrfRefCapture.Appearance = Appearance.Button;
            btnFrfRefCapture.Dock       = DockStyle.Bottom;
            btnFrfRefCapture.Margin     = new Padding(0);
            btnFrfRefCapture.Name       = "btnFrfRefCapture";
            btnFrfRefCapture.Size       = new Size(60, 24);
            btnFrfRefCapture.TabIndex   = 10;
            btnFrfRefCapture.Text       = "Cap Ref";
            btnFrfRefCapture.Font       = std9;
            btnFrfRefCapture.TextAlign  = ContentAlignment.MiddleCenter;
            btnFrfRefCapture.UseVisualStyleBackColor = true;

            btnFrfNormToggle.Appearance = Appearance.Button;
            btnFrfNormToggle.Dock       = DockStyle.Fill;
            btnFrfNormToggle.Margin     = new Padding(0);
            btnFrfNormToggle.Name       = "btnFrfNormToggle";
            btnFrfNormToggle.Size       = new Size(60, 24);
            btnFrfNormToggle.TabIndex   = 11;
            btnFrfNormToggle.Text       = "Norm";
            btnFrfNormToggle.Font       = std9;
            btnFrfNormToggle.TextAlign  = ContentAlignment.MiddleCenter;
            btnFrfNormToggle.UseVisualStyleBackColor = true;

            lblNormGroup.AutoSize  = false;
            lblNormGroup.Font      = gS;
            lblNormGroup.ForeColor = SystemColors.GrayText;
            lblNormGroup.Location  = new Point(555, 4);
            lblNormGroup.Name      = "lblNormGroup";
            lblNormGroup.Size      = new Size(60, 14);
            lblNormGroup.TabIndex  = 97;
            lblNormGroup.Text      = "NORM";
            lblNormGroup.TextAlign = ContentAlignment.MiddleCenter;

            // ── SEP 5 (before Cursors, x=619) ────────────────────────────────
            sep5.BackColor = System.Drawing.SystemColors.ControlDark;
            sep5.Location  = new Point(619, 6);
            sep5.Name      = "sep5";
            sep5.Size      = new Size(1, 76);
            sep5.TabIndex  = 99;

            // ── OVERLAY — same position as oscilloscope (x=965, y=60) ─────────
            btnOverlay.Location = new Point(965, 60);
            btnOverlay.Name     = "btnOverlay";
            btnOverlay.Size     = new Size(62, 24);
            btnOverlay.TabIndex = 19;
            btnOverlay.Text     = "Overlay";
            btnOverlay.UseVisualStyleBackColor = true;
            btnOverlay.Click += btnOverlay_Click;

            // ── 8. CURSORS X1/X2 — EXACT oscilloscope positions ───────────────
            // Col x: CURSOR=695 | CHANNEL=742 | SET VAL=789 | PEAK=836 | DISPLAY=893
            // Row 1 y=34 (X1), Row 2 y=60 (X2)
            // "CURSORS SETTINGS" title at (772, 3) exactly like oscilloscope

            // Hide legacy stub buttons (kept for CursorsBar.cs binding)
            foreach (var b in new Control[] { btnC1, btnC1Track, btnC1PrevPk, btnC1Pk, btnC1NextPk, btnC1Set,
                                              btnC2, btnC2Track, btnC2PrevPk, btnC2Pk, btnC2NextPk, btnC2Set })
            { b.Visible = false; b.Size = new Size(1, 1); b.Location = new Point(-100, -100); b.Name = b.Text = ""; }

            tlpCursors.ColumnCount = 1;
            tlpCursors.Location = new Point(-200, -200);
            tlpCursors.Name     = "tlpCursors";
            tlpCursors.Size     = new Size(1, 1);
            tlpCursors.TabIndex = 33;
            tlpCursors.Visible  = false;

            void BtnC(Button b, int x, int y, int w, string txt, int tab)
            {
                b.Location = new Point(x, y); b.Margin = new Padding(0);
                b.Size = new Size(w, 24); b.Text = txt; b.TabIndex = tab;
                b.UseVisualStyleBackColor = true;
                b.Font = new Font("Segoe UI", 8.5f);
            }

            void ColLbl(Label l, int x, int y2, string txt)
            {
                l.AutoSize = true; l.Font = gS; l.ForeColor = SystemColors.GrayText;
                l.Location = new Point(x, y2); l.Text = txt;
            }

            // Title + sub-column labels — exact oscilloscope positions
            ColLbl(lblCursors,    772, 3,  "CURSORS SETTINGS");
            ColLbl(lblColCursor,  700, 19, "CURSOR");
            ColLbl(lblColChannel, 747, 19, "CHANNEL");
            ColLbl(lblColSetVal,  793, 19, "SET VAL");
            ColLbl(lblColPeak,    844, 19, "PEAK");
            ColLbl(lblColDisplay, 896, 19, "DISPLAY");
            lblCursors.Click += label2_Click;

            // Row 1 — X1  (y=34)
            BtnC(btnX1,        695, 34, 45, "X1",    14);
            BtnC(btnX1Chan,    742, 34, 45, "Gain",  15);
            BtnC(btnX1Set,     789, 34, 45, "X1= ?", 20);
            BtnC(btnX1Peak,    836, 34, 45, "Peak",  21);
            BtnC(btnDispGraph, 893, 34, 45, "Panel", 22);

            // Row 2 — X2  (y=60)
            BtnC(btnX2,        695, 60, 45, "X2",    16);
            BtnC(btnX2Chan,    742, 60, 45, "Gain",  17);
            BtnC(btnX2Set,     789, 60, 45, "X2= ?", 23);
            BtnC(btnX2Peak,    836, 60, 45, "Peak",  24);
            BtnC(btnDispLabel, 893, 60, 45, "Label", 25);

            label1.Visible      = false;
            label1.Name         = "label1";
            lblWindowInfo.Visible = false;
            lblWindowInfo.Name  = "lblWindowInfo";
            lblFftTicker.Visible = false;
            lblFftTicker.Name   = "lblFftTicker";
            pnlAcq.Visible      = false;
            pnlAcq.Name         = "pnlAcq";
            pbAcq.Name          = "pbAcq";
            lblAcqPct.Name      = "lblAcqPct";

            // ── 11. RUN/STOP tout-en-un : T ACQ + barre + label (anchored right) ──
            btnRun.Anchor   = AnchorStyles.Top | AnchorStyles.Right;
            btnRun.Location = new Point(1050, 6);
            btnRun.Size     = new Size(82, 76);
            btnRun.Text     = "▶  RUN";
            btnRun.BackColor = Color.SeaGreen;
            btnRun.ForeColor = Color.White;
            btnRun.FlatStyle = FlatStyle.Flat;
            btnRun.FlatAppearance.BorderSize = 0;
            btnRun.Font      = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnRun.TabIndex  = 9;
            btnRun.UseVisualStyleBackColor = false;
            btnRun.Name      = "btnRun";

            // ── chartSpectrum ─────────────────────────────────────────────────
            chartSpectrum.Dock     = DockStyle.Fill;
            chartSpectrum.Location = new Point(0, 90);
            chartSpectrum.Name     = "chartSpectrum";
            chartSpectrum.Size     = new Size(1138, 460);
            chartSpectrum.TabIndex = 0;

            // ── grpRightStats ─────────────────────────────────────────────────
            grpRightStats.Controls.Add(lblRightMeas);
            grpRightStats.Controls.Add(lblRightHeader);
            grpRightStats.Controls.Add(lblRightRms);
            grpRightStats.Controls.Add(lblRightPeak);
            grpRightStats.Controls.Add(lblRightRmsV);
            grpRightStats.Controls.Add(lblRightPeakV);
            grpRightStats.Location = new Point(5, 222);
            grpRightStats.Name     = "grpRightStats";
            grpRightStats.Size     = new Size(140, 180);
            grpRightStats.TabIndex = 1;
            grpRightStats.TabStop  = false;

            lblRightMeas.Font     = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblRightMeas.Location = new Point(12, 85);
            lblRightMeas.Name     = "lblRightMeas";
            lblRightMeas.Size     = new Size(128, 92);
            lblRightMeas.TabIndex = 18;
            lblRightMeas.Text     = "---";
            lblRightMeas.Click   += LblRightMeas_Click;

            lblRightHeader.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblRightHeader.ForeColor = Color.OrangeRed;
            lblRightHeader.Location  = new Point(10, 10);
            lblRightHeader.Name      = "lblRightHeader";
            lblRightHeader.Size      = new Size(100, 23);
            lblRightHeader.TabIndex  = 0;
            lblRightHeader.Text      = "RIGHT";

            lblRightRms.Location = new Point(10, 40);
            lblRightRms.Name     = "lblRightRms";
            lblRightRms.Size     = new Size(100, 23);
            lblRightRms.TabIndex = 1;

            lblRightPeak.Location = new Point(10, 65);
            lblRightPeak.Name     = "lblRightPeak";
            lblRightPeak.Size     = new Size(100, 23);
            lblRightPeak.TabIndex = 2;

            lblRightRmsV.Location = new Point(10, 88);
            lblRightRmsV.Name     = "lblRightRmsV";
            lblRightRmsV.Size     = new Size(120, 23);
            lblRightRmsV.TabIndex = 18;
            lblRightRmsV.Visible  = false;

            lblRightPeakV.Location = new Point(10, 111);
            lblRightPeakV.Name     = "lblRightPeakV";
            lblRightPeakV.Size     = new Size(120, 23);
            lblRightPeakV.TabIndex = 19;
            lblRightPeakV.Visible  = false;

            // ── grpLeftStats ──────────────────────────────────────────────────
            grpLeftStats.Controls.Add(lblLeftMeas);
            grpLeftStats.Controls.Add(lblLeftHeader);
            grpLeftStats.Controls.Add(lblLeftRms);
            grpLeftStats.Controls.Add(lblLeftPeak);
            grpLeftStats.Controls.Add(lblLeftRmsV);
            grpLeftStats.Controls.Add(lblLeftPeakV);
            grpLeftStats.Location = new Point(5, 11);
            grpLeftStats.Name     = "grpLeftStats";
            grpLeftStats.Size     = new Size(140, 180);
            grpLeftStats.TabIndex = 0;
            grpLeftStats.TabStop  = false;

            lblLeftMeas.Font     = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblLeftMeas.Location = new Point(12, 88);
            lblLeftMeas.Name     = "lblLeftMeas";
            lblLeftMeas.Size     = new Size(128, 92);
            lblLeftMeas.TabIndex = 17;
            lblLeftMeas.Text     = "---";
            lblLeftMeas.Click   += LblLeftMeas_Click;

            lblLeftHeader.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblLeftHeader.ForeColor = Color.DodgerBlue;
            lblLeftHeader.Location  = new Point(10, 10);
            lblLeftHeader.Name      = "lblLeftHeader";
            lblLeftHeader.Size      = new Size(100, 23);
            lblLeftHeader.TabIndex  = 0;
            lblLeftHeader.Text      = "LEFT";

            lblLeftRms.Location = new Point(10, 40);
            lblLeftRms.Name     = "lblLeftRms";
            lblLeftRms.Size     = new Size(100, 23);
            lblLeftRms.TabIndex = 1;

            lblLeftPeak.Location = new Point(10, 65);
            lblLeftPeak.Name     = "lblLeftPeak";
            lblLeftPeak.Size     = new Size(100, 23);
            lblLeftPeak.TabIndex = 2;

            lblLeftRmsV.Location = new Point(10, 88);
            lblLeftRmsV.Name     = "lblLeftRmsV";
            lblLeftRmsV.Size     = new Size(120, 23);
            lblLeftRmsV.TabIndex = 18;
            lblLeftRmsV.Visible  = false;

            lblLeftPeakV.Location = new Point(10, 111);
            lblLeftPeakV.Name     = "lblLeftPeakV";
            lblLeftPeakV.Size     = new Size(120, 23);
            lblLeftPeakV.TabIndex = 19;
            lblLeftPeakV.Visible  = false;

            // ── chkCursorL / chkCursorR (hidden, kept for CursorsBar binding) ─
            chkCursorL.AutoSize  = true;
            chkCursorL.Location  = new Point(5, 197);
            chkCursorL.Name      = "chkCursorL";
            chkCursorL.Size      = new Size(70, 19);
            chkCursorL.TabIndex  = 15;
            chkCursorL.Text      = "Cursor L";
            chkCursorL.Visible   = false;

            chkCursorR.AutoSize  = true;
            chkCursorR.Location  = new Point(5, 408);
            chkCursorR.Name      = "chkCursorR";
            chkCursorR.Size      = new Size(71, 19);
            chkCursorR.TabIndex  = 16;
            chkCursorR.Text      = "Cursor R";
            chkCursorR.Visible   = false;

            // ── SpectrumControl ───────────────────────────────────────────────
            BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(chartSpectrum);
            Controls.Add(panelTop);
            Name = "SpectrumControl";
            Size = new Size(1138, 550);

            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            tlpCursors.ResumeLayout(false);
            tlpPhi.ResumeLayout(false);
            tlpNorm.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)chartSpectrum).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel    panelTop;
        private Label    lblUnits;
        private ComboBox comboLevelUnit;
        private Label    lblWindow;
        private ComboBox comboWindow;
        private Label    lblAveraging;
        private ComboBox comboAveraging;
        private Label    lblChannel;
        private ComboBox comboChannel;
        private Label    lblWindowInfo;
        private Button   btnRun;

        private System.Windows.Forms.DataVisualization.Charting.Chart chartSpectrum;
        private CheckBox chkLogX;
        private CheckBox chkEnableThd;
        private Button   btnThdSettings;
        private Button   btnResetAvg;
        private Label    lblAvgProgress;
        private CheckBox chkPeakHold;
        private Button   btnResetPeakHold;
        private CheckBox chkCursorR;
        private CheckBox chkCursorL;
        private GroupBox grpLeftStats;
        private Label    lblLeftMeas;
        private Label    lblLeftHeader;
        private Label    lblLeftRms;
        private Label    lblLeftPeak;
        private Label    lblLeftRmsV;
        private Label    lblLeftPeakV;
        private GroupBox grpRightStats;
        private Label    lblRightMeas;
        private Label    lblRightHeader;
        private Label    lblRightRms;
        private Label    lblRightPeak;
        private Label    lblRightRmsV;
        private Label    lblRightPeakV;
        private Label    label1;
        private Label    lblCursors;
        private Label    lblFftTicker;
        private Panel    pnlAcq;
        private ProgressBar pbAcq;
        private Label    lblAcqPct;
        private Button   btnOverlay;
        private CheckBox chkDcMode;

        // Cursor buttons — new X1/X2 layout (calqué oscilloscope)
        // Row 1 (y=34): [X1] [Channel] [X1= ?] [Peak]  [Graph]
        // Row 2 (y=60): [X2] [Channel] [X2= ?] [Peak]  [Label]
        private Button btnX1;         // toggle cursor X1 (was btnC1)
        private Button btnX1Chan;     // channel cycle for X1 (was btnC1Track)
        private Button btnX1Set;      // set value X1 (was btnC1Set)
        private Button btnX1Peak;     // peak for X1 (was btnC1Pk, left=max, right=context menu)
        private Button btnDispGraph;  // display: cycle Panel/Graph/Both/Off (row 1 DISPLAY col)

        private Button btnX2;         // toggle cursor X2 (was btnC2)
        private Button btnX2Chan;     // channel cycle for X2 (was btnC2Track)
        private Button btnX2Set;      // set value X2 (was btnC2Set)
        private Button btnX2Peak;     // peak for X2 (was btnC2Pk, left=max, right=context menu)
        private Button btnDispLabel;  // display: toggle delta label arrow (row 2 DISPLAY col)

        // Column sub-labels
        private Label lblColCursor;   // "CURSOR"
        private Label lblColChannel;  // "CHANNEL"
        private Label lblColSetVal;   // "SET VAL"
        private Label lblColPeak;     // "PEAK"
        private Label lblColDisplay;  // "DISPLAY"

        // Legacy buttons kept as hidden stubs so CursorsBar.cs compiles without change
        private Button btnC1;
        private Button btnC1Track;
        private Button btnC1PrevPk;
        private Button btnC1Pk;
        private Button btnC1NextPk;
        private Button btnC1Set;
        private Button btnC2;
        private Button btnC2Track;
        private Button btnC2PrevPk;
        private Button btnC2Pk;
        private Button btnC2NextPk;
        private Button btnC2Set;

        // Hidden TLP stub (kept so CursorsBar.cs binds _cursorBar without change)
        private TableLayoutPanel tlpCursors;
        private TableLayoutPanel tlpPhi;
        private CheckBox         btnPhiEnable;
        private CheckBox         btnPhiUnwrap;
        private Label            lblPhaseGroup;
        private TableLayoutPanel tlpNorm;
        private CheckBox         btnFrfRefCapture;
        private CheckBox         btnFrfNormToggle;
        private Label            lblNormGroup;

        // Visual separators
        private Label sep1;
        private Label sep2;
        private Label sep3;
        private Label sep4;
        private Label sep5;
    }
}
