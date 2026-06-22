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
            btnLevelUnit        = new Button();
            btnAxisOpen         = new Button();
            btnDcAc             = new Button();
            lblCoupling         = new Label();
            lblAvgCount         = new Label();
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
            sep6                = new Label();
            sep7                = new Label();
            sep8                = new Label();
            sepCursorInternal   = new Label();
            lblDistortion       = new Label();
            lblPeakHold         = new Label();
            lblBodePlot         = new Label();
            lblAxisSet          = new Label();
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
            //  LAYOUT PANELTOP — new design v2 (uniform 8px gaps, sep7 at x=933)
            //  Rows: labels y=4, sub-labels y=19, buttons y=34 and y=60
            //
            //  x=16   CHANNEL MODE (w=100) → end=116 → gap=8 → sep1=124
            //  x=133  WINDOWING (w=84) → end=217 → gap=8 → sep_wa=225
            //  x=234  AVERAGING (w=66) → end=300 → gap=8 → sep2=308
            //  x=317  DISTORTION (w=62) → end=379 → gap=8 → sep3=387
            //  x=396  PEAK HOLD (w=62) → end=458 → gap=8 → sep4=466
            //  x=475  CURSORS 5×45=225 → end=700 → gap=8 → sep5=708
            //  x=717  BODE PLOT φ(47)+gap(8)+NORM(62)=117 → end=834 → gap=8 → sep6=842
            //  x=851  AXIS SET (w=62) → end=913 → gap=20 → sep7=933 (graph right edge)
            //  x=960  Overlay (centered between sep7 and RUN)
            //  x=1050 RUN (anchored right)
            // ══════════════════════════════════════════════════════════════════

            // ── panelTop ─────────────────────────────────────────────────────
            panelTop.BackColor = System.Drawing.SystemColors.Control;
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
                lblChannel, lblWindow, lblAveraging,
                lblDistortion, lblPeakHold, lblBodePlot, lblAxisSet,
                lblCursors, lblColCursor, lblColChannel, lblColSetVal, lblColPeak, lblColDisplay,
                comboChannel, comboWindow, comboAveraging,
                chkEnableThd, chkPeakHold,
                btnX1, btnX1Chan, btnX1Set, btnX1Peak, btnDispGraph,
                btnX2, btnX2Chan, btnX2Set, btnX2Peak, btnDispLabel,
                lblPhaseGroup, tlpPhi,
                lblNormGroup, tlpNorm,
                lblUnits, lblAxisSet, comboLevelUnit, btnLevelUnit, btnAxisOpen, chkLogX,
                btnThdSettings, btnResetPeakHold,
                btnResetAvg, lblAvgProgress,
                btnOverlay,
                chkDcMode, btnDcAc, lblCoupling, lblAvgCount,
                btnC1, btnC1Track, btnC1PrevPk, btnC1Pk, btnC1NextPk, btnC1Set,
                btnC2, btnC2Track, btnC2PrevPk, btnC2Pk, btnC2NextPk, btnC2Set,
                tlpCursors,
                sep1, sep2, sep3, sep4, sep5, sep6, sep7, sep8, sepCursorInternal,
                lblWindowInfo,
                btnRun
            });

            // Helpers
            void SecLbl(Label l, int x, string txt)
            {
                l.AutoSize = true; l.Font = gS; l.ForeColor = SystemColors.GrayText;
                l.Location = new Point(x, 4); l.Text = txt;
            }
            void Sep(Label l, int x, int h = 76, int y = 6)
            {
                l.BackColor = SystemColors.ControlDark;
                l.Location  = new Point(x, y); l.Size = new Size(1, h); l.Text = "";
            }
            void BtnC(Button b, int x, int y, int w, string txt, int tab)
            {
                b.Location = new Point(x, y); b.Margin = new Padding(0);
                b.Size = new Size(w, 24); b.Text = txt; b.TabIndex = tab;
                b.UseVisualStyleBackColor = true; b.Font = new Font("Segoe UI", 8.5f);
            }
            void ColLbl(Label l, int x, int y2, string txt)
            {
                l.AutoSize = true; l.Font = gS; l.ForeColor = SystemColors.GrayText;
                l.Location = new Point(x, y2); l.Text = txt;
            }

            // ── 1. CHANNEL MODE (x=16) ───────────────────────────────────────
            SecLbl(lblChannel, 16, "TRACE MODE");
            comboChannel.DropDownStyle = ComboBoxStyle.DropDownList; comboChannel.Font = std9;
            comboChannel.Location = new Point(11, 18); comboChannel.Name = "comboChannel";
            comboChannel.Size = new Size(82, 24); comboChannel.TabIndex = 7;
            // chkDcMode hidden — replaced by btnDcAc toggle button
            chkDcMode.Visible = false; chkDcMode.Location = new Point(-200, -200);
            // btnDcAc: same size as cursor buttons (45×24)
            // COUPLING label above btnDcAc
            lblCoupling.AutoSize = true; lblCoupling.Font = gS; lblCoupling.ForeColor = SystemColors.GrayText;
            lblCoupling.Location = new Point(11, 46); lblCoupling.Name = "lblCoupling"; lblCoupling.Text = "COUPLE";
            // AVG Count label above lblAvgProgress (y=46, same as COUPLING)
            lblAvgCount.AutoSize = true; lblAvgCount.Font = gS; lblAvgCount.ForeColor = SystemColors.GrayText;
            lblAvgCount.Location = new Point(58, 46); lblAvgCount.Name = "lblAvgCount"; lblAvgCount.Text = "N AVG";
            btnDcAc.Location = new Point(11, 60); btnDcAc.Name = "btnDcAc";
            btnDcAc.Size = new Size(45, 24); btnDcAc.Font = std9;
            btnDcAc.Text = "AC"; btnDcAc.UseVisualStyleBackColor = true; btnDcAc.TabIndex = 8;

            sep1.BackColor = SystemColors.ControlDark; sep1.Location = new Point(102, 6);
            sep1.Size = new Size(1, 80); sep1.Text = "";

            // ── 2. WINDOWING (x=133, w=84) ───────────────────────────────────
            SecLbl(lblWindow, 112, "WINDOWING");
            comboWindow.DropDownStyle = ComboBoxStyle.DropDownList; comboWindow.Font = std9;
            comboWindow.Location = new Point(112, 18); comboWindow.Name = "comboWindow";
            comboWindow.Size = new Size(133, 24); comboWindow.TabIndex = 3;

            // sepCursorInternal no longer used as WIN/AVG sep (avg now under window)
            sepCursorInternal.Visible = false;

            // ── 3. AVERAGING (x=234, w=66) ───────────────────────────────────
            // AVERAGING label above the comboAveraging row
            lblAveraging.AutoSize = true; lblAveraging.Font = gS;
            lblAveraging.ForeColor = SystemColors.GrayText; lblAveraging.Visible = true;
            lblAveraging.Location = new Point(112, 46); lblAveraging.Text = "AVERAGING";
            comboAveraging.DropDownStyle = ComboBoxStyle.DropDownList; comboAveraging.Font = std9;
            comboAveraging.Location = new Point(112, 60); comboAveraging.Name = "comboAveraging";
            comboAveraging.Size = new Size(86, 24); comboAveraging.TabIndex = 5;
            btnResetAvg.Location = new Point(200, 60); btnResetAvg.Name = "btnResetAvg";
            btnResetAvg.Size = new Size(45, 24); btnResetAvg.TabIndex = 12;
            btnResetAvg.Text = "CLR"; btnResetAvg.UseVisualStyleBackColor = true;
            btnResetAvg.Click += BtnResetAvg_Click;
            lblAvgProgress.AutoSize = false;
            lblAvgProgress.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblAvgProgress.ForeColor = Color.Firebrick;
            lblAvgProgress.Location = new Point(58, 60); lblAvgProgress.Size = new Size(35, 24);
            lblAvgProgress.TextAlign = ContentAlignment.MiddleRight;
            lblAvgProgress.Name = "lblAvgProgress"; lblAvgProgress.TabIndex = 14; lblAvgProgress.Text = "";

            Sep(sep2, 254);

            // ── 4. DISTORTION (x=317, w=62) ──────────────────────────────────
            SecLbl(lblDistortion, 264, "DISTORTION");
            chkEnableThd.Appearance = Appearance.Button; chkEnableThd.Font = std9;
            chkEnableThd.Location = new Point(264, 34); chkEnableThd.Name = "chkEnableThd";
            chkEnableThd.Size = new Size(62, 24); chkEnableThd.TabIndex = 30;
            chkEnableThd.Text = "THD"; chkEnableThd.TextAlign = ContentAlignment.MiddleCenter;
            chkEnableThd.UseVisualStyleBackColor = true; chkEnableThd.CheckedChanged += ChkEnableThd_CheckedChanged;
            btnThdSettings.Location = new Point(264, 60); btnThdSettings.Name = "btnThdSettings";
            btnThdSettings.Size = new Size(62, 24); btnThdSettings.TabIndex = 31; btnThdSettings.TabStop = false;
            btnThdSettings.Text = "Setting"; btnThdSettings.UseMnemonic = false;
            btnThdSettings.UseVisualStyleBackColor = true; btnThdSettings.Click += BtnThdSettings_Click;

            Sep(sep3, 335);

            // ── 5. PEAK HOLD (x=396, w=62) ───────────────────────────────────
            SecLbl(lblPeakHold, 345, "PEAK HOLD");
            chkPeakHold.Appearance = Appearance.Button; chkPeakHold.Font = std9;
            chkPeakHold.Location = new Point(345, 34); chkPeakHold.Name = "chkPeakHold";
            chkPeakHold.Size = new Size(62, 24); chkPeakHold.TabIndex = 17;
            chkPeakHold.Text = "PEAK H"; chkPeakHold.TextAlign = ContentAlignment.MiddleCenter;
            chkPeakHold.UseVisualStyleBackColor = true; chkPeakHold.CheckedChanged += chkPeakHold_CheckedChanged;
            btnResetPeakHold.Location = new Point(345, 60); btnResetPeakHold.Name = "btnResetPeakHold";
            btnResetPeakHold.Size = new Size(62, 24); btnResetPeakHold.TabIndex = 18; btnResetPeakHold.TabStop = false;
            btnResetPeakHold.Text = "Reset"; btnResetPeakHold.UseVisualStyleBackColor = true;
            btnResetPeakHold.Click += BtnResetPeakHold_Click;

            Sep(sep4, 416);

            // ── 6. CURSORS SETTINGS (x=475, 5×45=225px) ──────────────────────
            // Cols: X1/Chan/Set/Peak/Disp at x=475|520|565|610|655
            // Short internal sep between PEAK(610+45=655) and DISPLAY(655): x=655
            // Title centred: 475 + 225/2 = 587 → place label to auto-centre
            ColLbl(lblCursors,    500, 4,  "CURSORS SETTINGS");
            ColLbl(lblColCursor,  429, 19, "CURSOR");
            ColLbl(lblColChannel, 475, 19, "TRACE");
            ColLbl(lblColSetVal,  519, 19, "SET VAL");
            ColLbl(lblColPeak,    571, 19, "PEAK");
            ColLbl(lblColDisplay, 620, 19, "DISPLAY");
            lblCursors.Click += label2_Click;
            BtnC(btnX1,        426, 34, 45, "X1",    14);
            BtnC(btnX1Chan,    471, 34, 45, "Gain",  15);
            BtnC(btnX1Set,     516, 34, 45, "X1= ?", 20);
            BtnC(btnX1Peak,    561, 34, 45, "Peak",  21);
            BtnC(btnDispGraph, 619, 34, 45, "Panel", 22);
            BtnC(btnX2,        426, 60, 45, "X2",    16);
            BtnC(btnX2Chan,    471, 60, 45, "Gain",  17);
            BtnC(btnX2Set,     516, 60, 45, "X2= ?", 23);
            BtnC(btnX2Peak,    561, 60, 45, "Peak",  24);
            BtnC(btnDispLabel, 619, 60, 45, "Label", 25);
            // Short sep (between PEAK col end and DISPLAY col, h=50, y=34)
            sep6.BackColor = System.Drawing.Color.Silver;
            sep6.Location  = new Point(612, 34); sep6.Size = new Size(1, 50); sep6.Text = "";

            // Legacy stubs
            foreach (var b in new Control[] { btnC1, btnC1Track, btnC1PrevPk, btnC1Pk, btnC1NextPk, btnC1Set,
                                              btnC2, btnC2Track, btnC2PrevPk, btnC2Pk, btnC2NextPk, btnC2Set })
            { b.Visible = false; b.Size = new Size(1, 1); b.Location = new Point(-100, -100); b.Name = b.Text = ""; }
            tlpCursors.ColumnCount = 1; tlpCursors.Location = new Point(-200, -200);
            tlpCursors.Name = "tlpCursors"; tlpCursors.Size = new Size(1, 1); tlpCursors.TabIndex = 33; tlpCursors.Visible = false;

            Sep(sep5, 673);

            // ── 7. BODE PLOT (x=717): PHASE(x=717,w=47) gap(8) NORM(x=772,w=62) ──
            // Title centred on total 117px: center = 717+58 = 775 → label auto-sized
            // BODE PLOT title centred on 117px zone (x=717..834): center=775, label ~65px → x≈742
            SecLbl(lblBodePlot, 721, "BODE PLOT");

            lblPhaseGroup.AutoSize = false; lblPhaseGroup.Font = gS; lblPhaseGroup.ForeColor = SystemColors.GrayText;
            lblPhaseGroup.Location = new Point(683, 19); lblPhaseGroup.Name = "lblPhaseGroup";
            lblPhaseGroup.Size = new Size(62, 12); lblPhaseGroup.TabIndex = 98;
            lblPhaseGroup.Text = "PHASE"; lblPhaseGroup.TextAlign = ContentAlignment.MiddleCenter;

            tlpPhi.ColumnCount = 1; tlpPhi.ColumnStyles.Clear();
            tlpPhi.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 62F));
            tlpPhi.Controls.Clear();
            tlpPhi.Controls.Add(btnPhiEnable, 0, 0); tlpPhi.Controls.Add(btnPhiUnwrap, 0, 1);
            tlpPhi.Location = new Point(683, 34); tlpPhi.Margin = new Padding(0); tlpPhi.Name = "tlpPhi";
            tlpPhi.RowCount = 2; tlpPhi.RowStyles.Clear();
            tlpPhi.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            tlpPhi.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            tlpPhi.Size = new Size(62, 50); tlpPhi.TabIndex = 36;

            btnPhiEnable.Appearance = Appearance.Button; btnPhiEnable.Dock = DockStyle.Fill;
            btnPhiEnable.Margin = new Padding(0); btnPhiEnable.Name = "btnPhiEnable";
            btnPhiEnable.Size = new Size(62, 24); btnPhiEnable.TabIndex = 8;
            btnPhiEnable.Text = "φ"; btnPhiEnable.Font = std9;
            btnPhiEnable.TextAlign = ContentAlignment.MiddleCenter; btnPhiEnable.UseVisualStyleBackColor = true;

            btnPhiUnwrap.Appearance = Appearance.Button; btnPhiUnwrap.Dock = DockStyle.Fill;
            btnPhiUnwrap.Margin = new Padding(0); btnPhiUnwrap.Name = "btnPhiUnwrap";
            btnPhiUnwrap.Size = new Size(62, 24); btnPhiUnwrap.TabIndex = 9;
            btnPhiUnwrap.Text = "Uw"; btnPhiUnwrap.Font = std9;
            btnPhiUnwrap.TextAlign = ContentAlignment.MiddleCenter; btnPhiUnwrap.UseVisualStyleBackColor = true;

            lblNormGroup.AutoSize = false; lblNormGroup.Font = gS; lblNormGroup.ForeColor = SystemColors.GrayText;
            lblNormGroup.Location = new Point(753, 19); lblNormGroup.Name = "lblNormGroup";
            lblNormGroup.Size = new Size(62, 12); lblNormGroup.TabIndex = 97;
            lblNormGroup.Text = "NORM"; lblNormGroup.TextAlign = ContentAlignment.MiddleCenter;

            tlpNorm.ColumnCount = 1; tlpNorm.ColumnStyles.Clear();
            tlpNorm.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 62F));
            tlpNorm.Controls.Clear();
            tlpNorm.Controls.Add(btnFrfRefCapture, 0, 0); tlpNorm.Controls.Add(btnFrfNormToggle, 0, 1);
            tlpNorm.Location = new Point(753, 34); tlpNorm.Margin = new Padding(0); tlpNorm.Name = "tlpNorm";
            tlpNorm.RowCount = 2; tlpNorm.RowStyles.Clear();
            tlpNorm.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            tlpNorm.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            tlpNorm.Size = new Size(62, 50); tlpNorm.TabIndex = 37;

            btnFrfRefCapture.Appearance = Appearance.Button; btnFrfRefCapture.Dock = DockStyle.Fill;
            btnFrfRefCapture.Margin = new Padding(0); btnFrfRefCapture.Name = "btnFrfRefCapture";
            btnFrfRefCapture.Size = new Size(62, 24); btnFrfRefCapture.TabIndex = 10;
            btnFrfRefCapture.Text = "Capture"; btnFrfRefCapture.Font = std9;
            btnFrfRefCapture.TextAlign = ContentAlignment.MiddleCenter; btnFrfRefCapture.UseVisualStyleBackColor = true;

            btnFrfNormToggle.Appearance = Appearance.Button; btnFrfNormToggle.Dock = DockStyle.Fill;
            btnFrfNormToggle.Margin = new Padding(0); btnFrfNormToggle.Name = "btnFrfNormToggle";
            btnFrfNormToggle.Size = new Size(62, 24); btnFrfNormToggle.TabIndex = 11;
            btnFrfNormToggle.Text = "Norm"; btnFrfNormToggle.Font = std9;
            btnFrfNormToggle.TextAlign = ContentAlignment.MiddleCenter; btnFrfNormToggle.UseVisualStyleBackColor = true;

            Sep(sep7, 824);

            // ── 8. AXIS SET (x=851, w=62) ────────────────────────────────────
            SecLbl(lblAxisSet, 844, "AXIS SET");
            // lblUnits et comboLevelUnit masqués (unité maintenant dans AxisDialog)
            lblUnits.Visible = false;
            comboLevelUnit.Visible = false; comboLevelUnit.Location = new Point(-200,-200);
            // btnLevelUnit masqué — remplacé par btnAxisOpen
            btnLevelUnit.Visible = false; btnLevelUnit.Location = new Point(-200,-200);
            // btnAxisOpen : ouvre le dialogue Axis Parameters (remplace dBFS/dBV + Axis Setup)
            btnAxisOpen.Location = new Point(834, 34); btnAxisOpen.Name = "btnAxisOpen";
            btnAxisOpen.Size = new Size(62, 24); btnAxisOpen.TabIndex = 14;
            btnAxisOpen.Text = "AXIS"; btnAxisOpen.Font = std9;
            btnAxisOpen.UseVisualStyleBackColor = true;
            chkLogX.Appearance = Appearance.Button; chkLogX.Font = std9;
            chkLogX.Location = new Point(834, 60); chkLogX.Name = "chkLogX";
            chkLogX.Size = new Size(62, 24); chkLogX.TabIndex = 11;
            chkLogX.Text = "LOG"; chkLogX.TextAlign = ContentAlignment.MiddleCenter;

            // sep8 at x=933 (right edge of graph)
            sep8.BackColor = SystemColors.ControlDark;
            sep8.Location  = new Point(908, 6); sep8.Size = new Size(1, 76); sep8.Text = "";

            // ── OVERLAY (x=960, centred between sep7=933 and RUN=1050) ────────
            btnOverlay.Location = new Point(965, 60); btnOverlay.Name = "btnOverlay";
            btnOverlay.Size = new Size(62, 24); btnOverlay.TabIndex = 19;
            btnOverlay.Text = "Overlay"; btnOverlay.UseVisualStyleBackColor = true;
            btnOverlay.Click += btnOverlay_Click;

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
        private Button   btnLevelUnit;    // toggle dBFS/dBV (remplacé — masqué)
        private Button   btnAxisOpen;     // ouvre le dialogue Axis Parameters
        private Button   btnDcAc;         // toggle AC/DC mode (replaces chkDcMode)
        private Label    lblCoupling;      // "COUPLE" title above btnDcAc
        private Label    lblAvgCount;       // "N AVG" title above lblAvgProgress
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
        private Label sep6;
        private Label sep7;              // after BODE PLOT (x=842)
        private Label sep8;              // right edge of graph (x=933)
        private Label sepCursorInternal; // short sep between PEAK and DISPLAY columns
        private Label lblDistortion;     // "DISTORTION" group title
        private Label lblPeakHold;       // "PEAK HOLD" group title
        private Label lblBodePlot;       // "BODE PLOT" group title
        private Label lblAxisSet;        // "AXIS SET" group title
    }
}
