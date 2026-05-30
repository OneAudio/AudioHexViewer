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
            tlpPhi              = new TableLayoutPanel();
            btnPhiEnable        = new Button();
            btnPhiUnwrap        = new Button();
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
            //  x=116→260  WINDOWING
            //  x=268→380  AVERAGING
            //  x=390→514  PEAK HOLD + THD
            //  x=522→652  OVERLAY + (spare)
            //  x=652→860  CURSORS C1/C2  (same x as Oscilloscope, 734→931)
            //  x=870→980  dB UNIT / LOG
            //  x=1012      T ACQ (anchored right)
            //  x=1060      RUN  (anchored right)
            // ══════════════════════════════════════════════════════════════════

            // ── panelTop ─────────────────────────────────────────────────────
            panelTop.BackColor = System.Drawing.SystemColors.Control;
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
                // Cursors label + buttons C1/C2
                lblCursors,
                btnC1, btnC1Track, btnC1PrevPk, btnC1Pk, btnC1NextPk, btnC1Set,
                btnC2, btnC2Track, btnC2PrevPk, btnC2Pk, btnC2NextPk, btnC2Set,
                // tlpCursors hidden (kept for CursorsBar.cs binding)
                tlpCursors,
                // dB Unit / LOG
                lblUnits, chkLogX,
                // Phase
                lblWindowInfo, tlpPhi,
                // Separators
                sep1, sep2, sep3, sep4, sep5,
                // Right: T ACQ + RUN
                lblAvgProgress, label1, lblFftTicker, btnRun
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
            comboWindow.Size          = new Size(140, 24);
            comboWindow.TabIndex      = 3;

            // ── SEP 2 (after Windowing, x=258) ───────────────────────────────
            sep2.BackColor = System.Drawing.SystemColors.ControlDark;
            sep2.Location  = new Point(258, 6);
            sep2.Name      = "sep2";
            sep2.Size      = new Size(1, 76);
            sep2.TabIndex  = 99;

            // ── 3. AVERAGING  (x=266) ────────────────────────────────────────
            lblAveraging.AutoSize  = true;
            lblAveraging.Font      = gS;
            lblAveraging.ForeColor = System.Drawing.SystemColors.GrayText;
            lblAveraging.Location  = new Point(266, 4);
            lblAveraging.Name      = "lblAveraging";
            lblAveraging.TabIndex  = 4;
            lblAveraging.Text      = "AVERAGING";

            comboAveraging.DropDownStyle = ComboBoxStyle.DropDownList;
            comboAveraging.Font          = std9;
            comboAveraging.Location      = new Point(266, 18);
            comboAveraging.Name          = "comboAveraging";
            comboAveraging.Size          = new Size(110, 24);
            comboAveraging.TabIndex      = 5;

            // btnResetAvg + lblAvgProgress côte à côte en row 2
            btnResetAvg.Location = new Point(266, 60);
            btnResetAvg.Name     = "btnResetAvg";
            btnResetAvg.Size     = new Size(55, 24);
            btnResetAvg.TabIndex = 12;
            btnResetAvg.Text     = "CLEAR";
            btnResetAvg.UseVisualStyleBackColor = true;
            btnResetAvg.Click += BtnResetAvg_Click;

            // lblAvgProgress : compteur k/N à droite du bouton CLEAR
            lblAvgProgress.AutoSize  = false;
            lblAvgProgress.Font      = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblAvgProgress.ForeColor = Color.Firebrick;   // rouge par défaut (en cours)
            lblAvgProgress.Location  = new Point(325, 60);
            lblAvgProgress.Size      = new Size(48, 24);
            lblAvgProgress.TextAlign = ContentAlignment.MiddleLeft;
            lblAvgProgress.Name      = "lblAvgProgress";
            lblAvgProgress.TabIndex  = 14;
            lblAvgProgress.Text      = "";

            // ── SEP 3 (after Averaging, x=382) ───────────────────────────────
            sep3.BackColor = System.Drawing.SystemColors.ControlDark;
            sep3.Location  = new Point(382, 6);
            sep3.Name      = "sep3";
            sep3.Size      = new Size(1, 76);
            sep3.TabIndex  = 99;

            // ── 4. PEAK HOLD  (x=390) ────────────────────────────────────────
            // label implicit (PEAK H toggle is self-labelling)
            chkPeakHold.Appearance = Appearance.Button;
            chkPeakHold.Font       = std9;
            chkPeakHold.Location   = new Point(390, 18);
            chkPeakHold.Name       = "chkPeakHold";
            chkPeakHold.Size       = new Size(62, 24);
            chkPeakHold.TabIndex   = 17;
            chkPeakHold.Text       = "PEAK H";
            chkPeakHold.TextAlign  = ContentAlignment.MiddleCenter;
            chkPeakHold.UseVisualStyleBackColor = true;
            chkPeakHold.CheckedChanged += chkPeakHold_CheckedChanged;

            btnResetPeakHold.Location = new Point(390, 60);
            btnResetPeakHold.Name     = "btnResetPeakHold";
            btnResetPeakHold.Size     = new Size(62, 24);
            btnResetPeakHold.TabIndex = 18;
            btnResetPeakHold.TabStop  = false;
            btnResetPeakHold.Text     = "Reset";
            btnResetPeakHold.UseVisualStyleBackColor = true;
            btnResetPeakHold.Click += BtnResetPeakHold_Click;

            // ── 5. THD  (x=458) ──────────────────────────────────────────────
            chkEnableThd.Appearance = Appearance.Button;
            chkEnableThd.Font       = std9;
            chkEnableThd.Location   = new Point(458, 18);
            chkEnableThd.Name       = "chkEnableThd";
            chkEnableThd.Size       = new Size(62, 24);
            chkEnableThd.TabIndex   = 30;
            chkEnableThd.Text       = "THD";
            chkEnableThd.TextAlign  = ContentAlignment.MiddleCenter;
            chkEnableThd.UseVisualStyleBackColor = true;
            chkEnableThd.CheckedChanged += ChkEnableThd_CheckedChanged;

            btnThdSettings.Location = new Point(458, 60);
            btnThdSettings.Name     = "btnThdSettings";
            btnThdSettings.Size     = new Size(62, 24);
            btnThdSettings.TabIndex = 31;
            btnThdSettings.TabStop  = false;
            btnThdSettings.Text     = "Settings";
            btnThdSettings.UseMnemonic = false;
            btnThdSettings.UseVisualStyleBackColor = true;
            btnThdSettings.Click += BtnThdSettings_Click;

            // ── 6. OVERLAY  (x=526) ──────────────────────────────────────────
            btnOverlay.Location = new Point(526, 18);
            btnOverlay.Name     = "btnOverlay";
            btnOverlay.Size     = new Size(62, 24);
            btnOverlay.TabIndex = 19;
            btnOverlay.Text     = "Overlay";
            btnOverlay.UseVisualStyleBackColor = true;
            btnOverlay.Click += btnOverlay_Click;

            // ── SEP 4 (before Cursors, x=596) ────────────────────────────────
            sep4.BackColor = System.Drawing.SystemColors.ControlDark;
            sep4.Location  = new Point(596, 6);
            sep4.Name      = "sep4";
            sep4.Size      = new Size(1, 76);
            sep4.TabIndex  = 99;

            // ── 7. CURSORS  (x=604, same column widths as Oscilloscope) ──────
            // Col widths: C=34 | Chan=45 | <=22 | Peak=40 | >=22 | Set=34  (total 197px)
            // x offsets:  604  | 638     | 683  | 705     | 745  | 767  → end 801

            lblCursors.AutoSize  = true;
            lblCursors.Font      = gS;
            lblCursors.ForeColor = System.Drawing.SystemColors.GrayText;
            lblCursors.Location  = new Point(654, 4);
            lblCursors.Name      = "lblCursors";
            lblCursors.TabIndex  = 34;
            lblCursors.Text      = "CURSORS SETTINGS";
            lblCursors.Click    += label2_Click;

            // ── Cursor row 1 (y=18) ──
            btnC1.Location = new Point(604, 18); btnC1.Margin = new Padding(0);
            btnC1.Size     = new Size(34, 24);   btnC1.TabIndex = 14;
            btnC1.Text     = "C1";               btnC1.UseVisualStyleBackColor = true;

            btnC1Track.Location = new Point(638, 18); btnC1Track.Margin = new Padding(0);
            btnC1Track.Size     = new Size(45, 24);   btnC1Track.TabIndex = 15;
            btnC1Track.Text     = "Left";             btnC1Track.UseVisualStyleBackColor = true;

            btnC1PrevPk.Location = new Point(683, 18); btnC1PrevPk.Margin = new Padding(0);
            btnC1PrevPk.Size     = new Size(22, 24);   btnC1PrevPk.TabIndex = 21;
            btnC1PrevPk.Text     = "<";                btnC1PrevPk.UseVisualStyleBackColor = true;

            btnC1Pk.Location = new Point(705, 18); btnC1Pk.Margin = new Padding(0);
            btnC1Pk.Size     = new Size(40, 24);   btnC1Pk.TabIndex = 20;
            btnC1Pk.Text     = "Peak";             btnC1Pk.UseVisualStyleBackColor = true;

            btnC1NextPk.Location = new Point(745, 18); btnC1NextPk.Margin = new Padding(0);
            btnC1NextPk.Size     = new Size(22, 24);   btnC1NextPk.TabIndex = 22;
            btnC1NextPk.Text     = ">";                btnC1NextPk.UseVisualStyleBackColor = true;

            btnC1Set.Location = new Point(767, 18); btnC1Set.Margin = new Padding(0);
            btnC1Set.Size     = new Size(34, 24);   btnC1Set.TabIndex = 23;
            btnC1Set.Text     = "Set";              btnC1Set.UseVisualStyleBackColor = true;

            // ── Cursor row 2 (y=60) ──
            btnC2.Location = new Point(604, 60); btnC2.Margin = new Padding(0);
            btnC2.Size     = new Size(34, 24);   btnC2.TabIndex = 16;
            btnC2.Text     = "C2";               btnC2.UseVisualStyleBackColor = true;

            btnC2Track.Location = new Point(638, 60); btnC2Track.Margin = new Padding(0);
            btnC2Track.Size     = new Size(45, 24);   btnC2Track.TabIndex = 17;
            btnC2Track.Text     = "Right";            btnC2Track.UseVisualStyleBackColor = true;

            btnC2PrevPk.Location = new Point(683, 60); btnC2PrevPk.Margin = new Padding(0);
            btnC2PrevPk.Size     = new Size(22, 24);   btnC2PrevPk.TabIndex = 25;
            btnC2PrevPk.Text     = "<";                btnC2PrevPk.UseVisualStyleBackColor = true;

            btnC2Pk.Location = new Point(705, 60); btnC2Pk.Margin = new Padding(0);
            btnC2Pk.Size     = new Size(40, 24);   btnC2Pk.TabIndex = 24;
            btnC2Pk.Text     = "Peak";             btnC2Pk.UseVisualStyleBackColor = true;

            btnC2NextPk.Location = new Point(745, 60); btnC2NextPk.Margin = new Padding(0);
            btnC2NextPk.Size     = new Size(22, 24);   btnC2NextPk.TabIndex = 26;
            btnC2NextPk.Text     = ">";                btnC2NextPk.UseVisualStyleBackColor = true;

            btnC2Set.Location = new Point(767, 60); btnC2Set.Margin = new Padding(0);
            btnC2Set.Size     = new Size(34, 24);   btnC2Set.TabIndex = 27;
            btnC2Set.Text     = "Set";              btnC2Set.UseVisualStyleBackColor = true;

            // tlpCursors: hidden stub kept so CursorsBar.cs can bind _cursorBar
            tlpCursors.ColumnCount = 6;
            tlpCursors.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34F));
            tlpCursors.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 45F));
            tlpCursors.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22F));
            tlpCursors.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));
            tlpCursors.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22F));
            tlpCursors.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34F));
            tlpCursors.RowCount = 2;
            tlpCursors.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            tlpCursors.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            tlpCursors.Location = new Point(604, 18);
            tlpCursors.Margin   = new Padding(0);
            tlpCursors.Name     = "tlpCursors";
            tlpCursors.Size     = new Size(197, 66);
            tlpCursors.TabIndex = 33;
            tlpCursors.Visible  = false;   // visual layout is done with individual buttons above

            // ── SEP 5 (after Cursors, x=806) ─────────────────────────────────
            sep5.BackColor = System.Drawing.SystemColors.ControlDark;
            sep5.Location  = new Point(808, 6);
            sep5.Name      = "sep5";
            sep5.Size      = new Size(1, 76);
            sep5.TabIndex  = 99;

            // ── 8. dB UNIT / LOG  (x=816) ────────────────────────────────────
            lblUnits.AutoSize  = true;
            lblUnits.Font      = gS;
            lblUnits.ForeColor = System.Drawing.SystemColors.GrayText;
            lblUnits.Location  = new Point(816, 4);
            lblUnits.Name      = "lblUnits";
            lblUnits.TabIndex  = 13;
            lblUnits.Text      = "dB UNIT";

            comboLevelUnit.DropDownStyle = ComboBoxStyle.DropDownList;
            comboLevelUnit.Font          = std9;
            comboLevelUnit.Location      = new Point(816, 18);
            comboLevelUnit.Name          = "comboLevelUnit";
            comboLevelUnit.Size          = new Size(56, 24);
            comboLevelUnit.TabIndex      = 14;

            chkLogX.Appearance = Appearance.Button;
            chkLogX.Font       = std9;
            chkLogX.Location   = new Point(816, 60);
            chkLogX.Name       = "chkLogX";
            chkLogX.Size       = new Size(56, 24);
            chkLogX.TabIndex   = 11;
            chkLogX.Text       = "LOG";
            chkLogX.TextAlign  = ContentAlignment.MiddleCenter;

            // ── 9. PHASE φ  (x=878) ──────────────────────────────────────────
            tlpPhi.ColumnCount = 1;
            tlpPhi.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 47F));
            tlpPhi.Controls.Add(btnPhiEnable, 0, 0);
            tlpPhi.Controls.Add(btnPhiUnwrap, 0, 1);
            tlpPhi.Location  = new Point(878, 18);
            tlpPhi.Margin    = new Padding(0);
            tlpPhi.Name      = "tlpPhi";
            tlpPhi.RowCount  = 2;
            tlpPhi.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            tlpPhi.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            tlpPhi.Size      = new Size(47, 66);
            tlpPhi.TabIndex  = 36;

            btnPhiEnable.Dock       = DockStyle.Fill;
            btnPhiEnable.Location   = new Point(0, 0);
            btnPhiEnable.Margin     = new Padding(0);
            btnPhiEnable.Name       = "btnPhiEnable";
            btnPhiEnable.Size       = new Size(47, 24);
            btnPhiEnable.TabIndex   = 8;
            btnPhiEnable.Text       = "φ";
            btnPhiEnable.UseVisualStyleBackColor = true;

            btnPhiUnwrap.Dock       = DockStyle.Fill;
            btnPhiUnwrap.Location   = new Point(0, 24);
            btnPhiUnwrap.Margin     = new Padding(0);
            btnPhiUnwrap.Name       = "btnPhiUnwrap";
            btnPhiUnwrap.Size       = new Size(47, 24);
            btnPhiUnwrap.TabIndex   = 9;
            btnPhiUnwrap.Text       = "Uw";
            btnPhiUnwrap.UseVisualStyleBackColor = true;

            // ── 10. T ACQ (anchored right, x=1012) ───────────────────────────
            label1.Anchor    = AnchorStyles.Top | AnchorStyles.Right;
            label1.AutoSize  = true;
            label1.Font      = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location  = new Point(1012, 14);
            label1.Name      = "label1";
            label1.TabIndex  = 32;
            label1.Text      = "T ACQ:";

            lblWindowInfo.Anchor    = AnchorStyles.Top | AnchorStyles.Right;
            lblWindowInfo.AutoSize  = true;
            lblWindowInfo.Font      = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblWindowInfo.ForeColor = SystemColors.HotTrack;
            lblWindowInfo.Location  = new Point(1006, 36);
            lblWindowInfo.Name      = "lblWindowInfo";
            lblWindowInfo.TabIndex  = 8;
            lblWindowInfo.Text      = "≈ 0.00 s";

            lblFftTicker.Anchor    = AnchorStyles.Top | AnchorStyles.Right;
            lblFftTicker.AutoSize  = true;
            lblFftTicker.Location  = new Point(1093, 52);
            lblFftTicker.Name      = "lblFftTicker";
            lblFftTicker.TabIndex  = 35;
            lblFftTicker.Text      = "\"\"";

            // ── 11. RUN (anchored right, x=1060) ─────────────────────────────
            btnRun.Anchor   = AnchorStyles.Top | AnchorStyles.Right;
            btnRun.Location = new Point(1060, 10);
            btnRun.Size     = new Size(70, 40);
            btnRun.Text     = "▶  RUN";
            btnRun.BackColor = Color.SeaGreen;
            btnRun.ForeColor = Color.White;
            btnRun.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
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
        private Button   btnOverlay;
        private CheckBox chkDcMode;

        // Cursor buttons (individual, pixel-exact like OscilloscopeControl)
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
        private Button           btnPhiEnable;
        private Button           btnPhiUnwrap;

        // Visual separators
        private Label sep1;
        private Label sep2;
        private Label sep3;
        private Label sep4;
        private Label sep5;
    }
}
