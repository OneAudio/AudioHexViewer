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
            panelTop = new Panel();
            lblFftTicker = new Label();
            label2 = new Label();
            label1 = new Label();
            lblAvgProgress = new Label();
            btnOverlay = new Button();
            chkPeakHold = new CheckBox();
            btnResetPeakHold = new Button();
            chkEnableThd = new CheckBox();
            btnThdSettings = new Button();
            btnResetAvg = new Button();
            lblWindow = new Label();
            comboWindow = new ComboBox();
            lblAveraging = new Label();
            comboAveraging = new ComboBox();
            lblUnits = new Label();
            comboLevelUnit = new ComboBox();
            lblChannel = new Label();
            comboChannel = new ComboBox();
            chkDcMode = new CheckBox();
            lblWindowInfo = new Label();
            btnRun = new Button();
            chkLogX = new CheckBox();
            tlpCursors = new TableLayoutPanel();
            btnC1 = new Button();
            btnC1Track = new Button();
            btnC1PrevPk = new Button();
            btnC1Pk = new Button();
            btnC1NextPk = new Button();
            btnC1Set = new Button();
            btnC2 = new Button();
            btnC2Track = new Button();
            btnC2PrevPk = new Button();
            btnC2Pk = new Button();
            btnC2NextPk = new Button();
            btnC2Set = new Button();
            tlpPhi = new TableLayoutPanel();
            btnPhiEnable = new Button();
            btnPhiUnwrap = new Button();
            chartSpectrum = new System.Windows.Forms.DataVisualization.Charting.Chart();
            grpRightStats = new GroupBox();
            lblRightMeas = new Label();
            lblRightHeader = new Label();
            lblRightRms = new Label();
            lblRightPeak = new Label();
            lblRightRmsV = new Label();
            lblRightPeakV = new Label();
            grpLeftStats = new GroupBox();
            lblLeftMeas = new Label();
            lblLeftHeader = new Label();
            lblLeftRms = new Label();
            lblLeftPeak = new Label();
            lblLeftRmsV = new Label();
            lblLeftPeakV = new Label();
            chkCursorL = new CheckBox();
            chkCursorR = new CheckBox();
            panelTop.SuspendLayout();
            tlpCursors.SuspendLayout();
            tlpPhi.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)chartSpectrum).BeginInit();
            SuspendLayout();
            // 
            // panelTop
            // 
            panelTop.BackColor = Color.Transparent;
            panelTop.Controls.Add(lblFftTicker);
            panelTop.Controls.Add(label2);
            panelTop.Controls.Add(label1);
            panelTop.Controls.Add(lblAvgProgress);
            panelTop.Controls.Add(btnOverlay);
            panelTop.Controls.Add(chkPeakHold);
            panelTop.Controls.Add(btnResetPeakHold);
            panelTop.Controls.Add(chkEnableThd);
            panelTop.Controls.Add(btnThdSettings);
            panelTop.Controls.Add(btnResetAvg);
            panelTop.Controls.Add(lblWindow);
            panelTop.Controls.Add(comboWindow);
            panelTop.Controls.Add(lblAveraging);
            panelTop.Controls.Add(comboAveraging);
            panelTop.Controls.Add(lblUnits);
            panelTop.Controls.Add(comboLevelUnit);
            panelTop.Controls.Add(lblChannel);
            panelTop.Controls.Add(comboChannel);
            panelTop.Controls.Add(chkDcMode);
            panelTop.Controls.Add(lblWindowInfo);
            panelTop.Controls.Add(btnRun);
            panelTop.Controls.Add(chkLogX);
            panelTop.Controls.Add(tlpCursors);
            panelTop.Controls.Add(tlpPhi);
            panelTop.Dock = DockStyle.Top;
            panelTop.Location = new Point(0, 0);
            panelTop.Name = "panelTop";
            panelTop.Size = new Size(1138, 90);
            panelTop.TabIndex = 1;
            // 
            // lblFftTicker
            // 
            lblFftTicker.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblFftTicker.AutoSize = true;
            lblFftTicker.Location = new Point(1093, 52);
            lblFftTicker.Name = "lblFftTicker";
            lblFftTicker.Size = new Size(17, 15);
            lblFftTicker.TabIndex = 35;
            lblFftTicker.Text = "\"\"";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(704, 1);
            label2.Name = "label2";
            label2.Size = new Size(91, 15);
            label2.TabIndex = 34;
            label2.Text = "Cursors settings";
            label2.Click += label2_Click;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(1012, 14);
            label1.Name = "label1";
            label1.Size = new Size(44, 15);
            label1.TabIndex = 32;
            label1.Text = "T ACQ:";
            // 
            // lblAvgProgress
            // 
            lblAvgProgress.AutoSize = true;
            lblAvgProgress.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblAvgProgress.ForeColor = SystemColors.HotTrack;
            lblAvgProgress.Location = new Point(391, 14);
            lblAvgProgress.Name = "lblAvgProgress";
            lblAvgProgress.Size = new Size(51, 21);
            lblAvgProgress.TabIndex = 14;
            lblAvgProgress.Text = "label1";
            // 
            // btnOverlay
            // 
            btnOverlay.Location = new Point(455, 40);
            btnOverlay.Name = "btnOverlay";
            btnOverlay.Size = new Size(60, 23);
            btnOverlay.TabIndex = 17;
            btnOverlay.Text = "Overlay";
            btnOverlay.UseVisualStyleBackColor = true;
            btnOverlay.Click += btnOverlay_Click;
            // 
            // chkPeakHold
            // 
            chkPeakHold.Appearance = Appearance.Button;
            chkPeakHold.Location = new Point(521, 16);
            chkPeakHold.Name = "chkPeakHold";
            chkPeakHold.Size = new Size(60, 23);
            chkPeakHold.TabIndex = 17;
            chkPeakHold.Text = "PEAK H";
            chkPeakHold.TextAlign = ContentAlignment.MiddleCenter;
            chkPeakHold.UseVisualStyleBackColor = true;
            chkPeakHold.CheckedChanged += chkPeakHold_CheckedChanged;
            // 
            // btnResetPeakHold
            // 
            btnResetPeakHold.Location = new Point(521, 40);
            btnResetPeakHold.Name = "btnResetPeakHold";
            btnResetPeakHold.Size = new Size(60, 23);
            btnResetPeakHold.TabIndex = 18;
            btnResetPeakHold.TabStop = false;
            btnResetPeakHold.Text = "Reset";
            btnResetPeakHold.UseVisualStyleBackColor = true;
            btnResetPeakHold.Click += BtnResetPeakHold_Click;
            // 
            // chkEnableThd
            // 
            chkEnableThd.Appearance = Appearance.Button;
            chkEnableThd.Location = new Point(587, 17);
            chkEnableThd.Name = "chkEnableThd";
            chkEnableThd.Size = new Size(60, 23);
            chkEnableThd.TabIndex = 30;
            chkEnableThd.Text = "THD";
            chkEnableThd.TextAlign = ContentAlignment.MiddleCenter;
            chkEnableThd.UseVisualStyleBackColor = true;
            chkEnableThd.CheckedChanged += ChkEnableThd_CheckedChanged;
            // 
            // btnThdSettings
            // 
            btnThdSettings.Location = new Point(587, 39);
            btnThdSettings.Name = "btnThdSettings";
            btnThdSettings.Size = new Size(60, 23);
            btnThdSettings.TabIndex = 31;
            btnThdSettings.TabStop = false;
            btnThdSettings.Text = "Settings";
            btnThdSettings.UseMnemonic = false;
            btnThdSettings.UseVisualStyleBackColor = true;
            btnThdSettings.Click += BtnThdSettings_Click;
            // 
            // btnResetAvg
            // 
            btnResetAvg.Location = new Point(391, 40);
            btnResetAvg.Name = "btnResetAvg";
            btnResetAvg.Size = new Size(53, 23);
            btnResetAvg.TabIndex = 12;
            btnResetAvg.Text = "CLEAR";
            btnResetAvg.UseVisualStyleBackColor = true;
            btnResetAvg.Click += BtnResetAvg_Click;
            // 
            // lblWindow
            // 
            lblWindow.AutoSize = true;
            lblWindow.Location = new Point(115, 20);
            lblWindow.Name = "lblWindow";
            lblWindow.Size = new Size(68, 15);
            lblWindow.TabIndex = 2;
            lblWindow.Text = "Windowing";
            // 
            // comboWindow
            // 
            comboWindow.DropDownStyle = ComboBoxStyle.DropDownList;
            comboWindow.Location = new Point(115, 39);
            comboWindow.Name = "comboWindow";
            comboWindow.Size = new Size(130, 23);
            comboWindow.TabIndex = 3;
            // 
            // lblAveraging
            // 
            lblAveraging.AutoSize = true;
            lblAveraging.Location = new Point(280, 21);
            lblAveraging.Name = "lblAveraging";
            lblAveraging.Size = new Size(61, 15);
            lblAveraging.TabIndex = 4;
            lblAveraging.Text = "Averaging";
            // 
            // comboAveraging
            // 
            comboAveraging.DropDownStyle = ComboBoxStyle.DropDownList;
            comboAveraging.Location = new Point(263, 39);
            comboAveraging.Name = "comboAveraging";
            comboAveraging.Size = new Size(105, 23);
            comboAveraging.TabIndex = 5;
            // 
            // lblUnits
            // 
            lblUnits.AutoSize = true;
            lblUnits.Location = new Point(922, 1);
            lblUnits.Name = "lblUnits";
            lblUnits.Size = new Size(46, 15);
            lblUnits.TabIndex = 13;
            lblUnits.Text = "dB Unit";
            // 
            // comboLevelUnit
            // 
            comboLevelUnit.DropDownStyle = ComboBoxStyle.DropDownList;
            comboLevelUnit.Location = new Point(922, 16);
            comboLevelUnit.Name = "comboLevelUnit";
            comboLevelUnit.Size = new Size(53, 23);
            comboLevelUnit.TabIndex = 14;
            // 
            // lblChannel
            // 
            lblChannel.AutoSize = true;
            lblChannel.Location = new Point(16, 20);
            lblChannel.Name = "lblChannel";
            lblChannel.Size = new Size(85, 15);
            lblChannel.TabIndex = 6;
            lblChannel.Text = "Channel Mode";
            // 
            // comboChannel
            // 
            comboChannel.DropDownStyle = ComboBoxStyle.DropDownList;
            comboChannel.Location = new Point(15, 40);
            comboChannel.Name = "comboChannel";
            comboChannel.Size = new Size(85, 23);
            comboChannel.TabIndex = 7;
            // 
            // chkDcMode
            // 
            chkDcMode.AutoSize = true;
            chkDcMode.Checked = false;
            chkDcMode.CheckState = CheckState.Unchecked;
            chkDcMode.Location = new Point(15, 68);
            chkDcMode.Name = "chkDcMode";
            chkDcMode.Size = new Size(85, 19);
            chkDcMode.TabIndex = 8;
            chkDcMode.Text = "DC mode";
            chkDcMode.UseVisualStyleBackColor = true;
            // 
            // lblWindowInfo
            // 
            lblWindowInfo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblWindowInfo.AutoSize = true;
            lblWindowInfo.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblWindowInfo.ForeColor = SystemColors.HotTrack;
            lblWindowInfo.Location = new Point(1006, 36);
            lblWindowInfo.Name = "lblWindowInfo";
            lblWindowInfo.Size = new Size(50, 15);
            lblWindowInfo.TabIndex = 8;
            lblWindowInfo.Text = "≈ 0.00 s";
            // 
            // btnRun
            // 
            btnRun.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRun.FlatStyle = FlatStyle.Flat;
            btnRun.FlatAppearance.BorderSize = 0;
            btnRun.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnRun.Location = new Point(1060, 10);
            btnRun.Name = "btnRun";
            btnRun.Size = new Size(70, 40);
            btnRun.TabIndex = 9;
            btnRun.Text = "▶  RUN";
            btnRun.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            btnRun.UseVisualStyleBackColor = false;
            // 
            // chkLogX
            // 
            chkLogX.Appearance = Appearance.Button;
            chkLogX.Location = new Point(922, 39);
            chkLogX.Name = "chkLogX";
            chkLogX.Size = new Size(53, 23);
            chkLogX.TabIndex = 11;
            chkLogX.Text = "LOG";
            chkLogX.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tlpCursors
            // 
            tlpCursors.ColumnCount = 6;
            tlpCursors.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34F));
            tlpCursors.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 45F));
            tlpCursors.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22F));
            tlpCursors.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));
            tlpCursors.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22F));
            tlpCursors.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34F));
            tlpCursors.Controls.Add(btnC1, 0, 0);
            tlpCursors.Controls.Add(btnC1Track, 1, 0);
            tlpCursors.Controls.Add(btnC1PrevPk, 2, 0);
            tlpCursors.Controls.Add(btnC1Pk, 3, 0);
            tlpCursors.Controls.Add(btnC1NextPk, 4, 0);
            tlpCursors.Controls.Add(btnC1Set, 5, 0);
            tlpCursors.Controls.Add(btnC2, 0, 1);
            tlpCursors.Controls.Add(btnC2Track, 1, 1);
            tlpCursors.Controls.Add(btnC2PrevPk, 2, 1);
            tlpCursors.Controls.Add(btnC2Pk, 3, 1);
            tlpCursors.Controls.Add(btnC2NextPk, 4, 1);
            tlpCursors.Controls.Add(btnC2Set, 5, 1);
            tlpCursors.Location = new Point(650, 16);
            tlpCursors.Margin = new Padding(0);
            tlpCursors.Name = "tlpCursors";
            tlpCursors.RowCount = 2;
            tlpCursors.RowStyles.Add(new RowStyle(SizeType.Absolute, 23F));
            tlpCursors.RowStyles.Add(new RowStyle(SizeType.Absolute, 23F));
            tlpCursors.Size = new Size(197, 46);
            tlpCursors.TabIndex = 33;
            // 
            // btnC1
            // 
            btnC1.Dock = DockStyle.Fill;
            btnC1.Location = new Point(0, 0);
            btnC1.Margin = new Padding(0);
            btnC1.Name = "btnC1";
            btnC1.Size = new Size(34, 23);
            btnC1.TabIndex = 0;
            btnC1.Text = "C1";
            btnC1.UseVisualStyleBackColor = true;
            // 
            // btnC1Track
            // 
            btnC1Track.Dock = DockStyle.Fill;
            btnC1Track.Location = new Point(34, 0);
            btnC1Track.Margin = new Padding(0);
            btnC1Track.Name = "btnC1Track";
            btnC1Track.Size = new Size(45, 23);
            btnC1Track.TabIndex = 1;
            btnC1Track.Text = "Left";
            btnC1Track.UseVisualStyleBackColor = true;
            // 
            // btnC1PrevPk
            // 
            btnC1PrevPk.Dock = DockStyle.Fill;
            btnC1PrevPk.Location = new Point(79, 0);
            btnC1PrevPk.Margin = new Padding(0);
            btnC1PrevPk.Name = "btnC1PrevPk";
            btnC1PrevPk.Size = new Size(22, 23);
            btnC1PrevPk.TabIndex = 2;
            btnC1PrevPk.Text = "<";
            btnC1PrevPk.UseVisualStyleBackColor = true;
            // 
            // btnC1Pk
            // 
            btnC1Pk.Dock = DockStyle.Fill;
            btnC1Pk.Location = new Point(101, 0);
            btnC1Pk.Margin = new Padding(0);
            btnC1Pk.Name = "btnC1Pk";
            btnC1Pk.Size = new Size(40, 23);
            btnC1Pk.TabIndex = 3;
            btnC1Pk.Text = "Peak";
            btnC1Pk.UseVisualStyleBackColor = true;
            // 
            // btnC1NextPk
            // 
            btnC1NextPk.Dock = DockStyle.Fill;
            btnC1NextPk.Location = new Point(141, 0);
            btnC1NextPk.Margin = new Padding(0);
            btnC1NextPk.Name = "btnC1NextPk";
            btnC1NextPk.Size = new Size(22, 23);
            btnC1NextPk.TabIndex = 4;
            btnC1NextPk.Text = ">";
            btnC1NextPk.UseVisualStyleBackColor = true;
            // 
            // btnC1Set
            // 
            btnC1Set.Dock = DockStyle.Fill;
            btnC1Set.Location = new Point(163, 0);
            btnC1Set.Margin = new Padding(0);
            btnC1Set.Name = "btnC1Set";
            btnC1Set.Size = new Size(34, 23);
            btnC1Set.TabIndex = 5;
            btnC1Set.Text = "Set";
            btnC1Set.UseVisualStyleBackColor = true;
            // 
            // btnC2
            // 
            btnC2.Dock = DockStyle.Fill;
            btnC2.Location = new Point(0, 23);
            btnC2.Margin = new Padding(0);
            btnC2.Name = "btnC2";
            btnC2.Size = new Size(34, 23);
            btnC2.TabIndex = 4;
            btnC2.Text = "C2";
            btnC2.UseVisualStyleBackColor = true;
            // 
            // btnC2Track
            // 
            btnC2Track.Dock = DockStyle.Fill;
            btnC2Track.Location = new Point(34, 23);
            btnC2Track.Margin = new Padding(0);
            btnC2Track.Name = "btnC2Track";
            btnC2Track.Size = new Size(45, 23);
            btnC2Track.TabIndex = 5;
            btnC2Track.Text = "Right";
            btnC2Track.UseVisualStyleBackColor = true;
            // 
            // btnC2PrevPk
            // 
            btnC2PrevPk.Dock = DockStyle.Fill;
            btnC2PrevPk.Location = new Point(79, 23);
            btnC2PrevPk.Margin = new Padding(0);
            btnC2PrevPk.Name = "btnC2PrevPk";
            btnC2PrevPk.Size = new Size(22, 23);
            btnC2PrevPk.TabIndex = 6;
            btnC2PrevPk.Text = "<";
            btnC2PrevPk.UseVisualStyleBackColor = true;
            // 
            // btnC2Pk
            // 
            btnC2Pk.Dock = DockStyle.Fill;
            btnC2Pk.Location = new Point(101, 23);
            btnC2Pk.Margin = new Padding(0);
            btnC2Pk.Name = "btnC2Pk";
            btnC2Pk.Size = new Size(40, 23);
            btnC2Pk.TabIndex = 7;
            btnC2Pk.Text = "Peak";
            btnC2Pk.UseVisualStyleBackColor = true;
            // 
            // btnC2NextPk
            // 
            btnC2NextPk.Dock = DockStyle.Fill;
            btnC2NextPk.Location = new Point(141, 23);
            btnC2NextPk.Margin = new Padding(0);
            btnC2NextPk.Name = "btnC2NextPk";
            btnC2NextPk.Size = new Size(22, 23);
            btnC2NextPk.TabIndex = 8;
            btnC2NextPk.Text = ">";
            btnC2NextPk.UseVisualStyleBackColor = true;
            // 
            // btnC2Set
            // 
            btnC2Set.Dock = DockStyle.Fill;
            btnC2Set.Location = new Point(163, 23);
            btnC2Set.Margin = new Padding(0);
            btnC2Set.Name = "btnC2Set";
            btnC2Set.Size = new Size(34, 23);
            btnC2Set.TabIndex = 10;
            btnC2Set.Text = "Set";
            btnC2Set.UseVisualStyleBackColor = true;
            // 
            // tlpPhi
            // 
            tlpPhi.ColumnCount = 1;
            tlpPhi.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 47F));
            tlpPhi.Controls.Add(btnPhiEnable, 0, 0);
            tlpPhi.Controls.Add(btnPhiUnwrap, 0, 1);
            tlpPhi.Location = new Point(860, 16);
            tlpPhi.Margin = new Padding(0);
            tlpPhi.Name = "tlpPhi";
            tlpPhi.RowCount = 2;
            tlpPhi.RowStyles.Add(new RowStyle(SizeType.Absolute, 23F));
            tlpPhi.RowStyles.Add(new RowStyle(SizeType.Absolute, 23F));
            tlpPhi.Size = new Size(47, 46);
            tlpPhi.TabIndex = 34;
            // 
            // btnPhiEnable
            // 
            btnPhiEnable.Dock = DockStyle.Fill;
            btnPhiEnable.Location = new Point(0, 0);
            btnPhiEnable.Margin = new Padding(0);
            btnPhiEnable.Name = "btnPhiEnable";
            btnPhiEnable.Size = new Size(47, 23);
            btnPhiEnable.TabIndex = 8;
            btnPhiEnable.Text = "φ";
            btnPhiEnable.UseVisualStyleBackColor = true;
            // 
            // btnPhiUnwrap
            // 
            btnPhiUnwrap.Dock = DockStyle.Fill;
            btnPhiUnwrap.Location = new Point(0, 23);
            btnPhiUnwrap.Margin = new Padding(0);
            btnPhiUnwrap.Name = "btnPhiUnwrap";
            btnPhiUnwrap.Size = new Size(47, 23);
            btnPhiUnwrap.TabIndex = 9;
            btnPhiUnwrap.Text = "Uw";
            btnPhiUnwrap.UseVisualStyleBackColor = true;
            // 
            // chartSpectrum
            // 
            chartSpectrum.Dock = DockStyle.Fill;
            chartSpectrum.Location = new Point(0, 70);
            chartSpectrum.Name = "chartSpectrum";
            chartSpectrum.Size = new Size(1138, 480);
            chartSpectrum.TabIndex = 0;
            // 
            // grpRightStats
            // 
            grpRightStats.Controls.Add(lblRightMeas);
            grpRightStats.Controls.Add(lblRightHeader);
            grpRightStats.Controls.Add(lblRightRms);
            grpRightStats.Controls.Add(lblRightPeak);
            grpRightStats.Controls.Add(lblRightRmsV);
            grpRightStats.Controls.Add(lblRightPeakV);
            grpRightStats.Location = new Point(5, 222);
            grpRightStats.Name = "grpRightStats";
            grpRightStats.Size = new Size(140, 180);
            grpRightStats.TabIndex = 1;
            grpRightStats.TabStop = false;
            // 
            // lblRightMeas
            // 
            lblRightMeas.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblRightMeas.Location = new Point(12, 85);
            lblRightMeas.Name = "lblRightMeas";
            lblRightMeas.Size = new Size(128, 92);
            lblRightMeas.TabIndex = 18;
            lblRightMeas.Text = "---";
            lblRightMeas.Click += LblRightMeas_Click;
            // 
            // lblRightHeader
            // 
            lblRightHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblRightHeader.ForeColor = Color.OrangeRed;
            lblRightHeader.Location = new Point(10, 10);
            lblRightHeader.Name = "lblRightHeader";
            lblRightHeader.Size = new Size(100, 23);
            lblRightHeader.TabIndex = 0;
            lblRightHeader.Text = "RIGHT";
            // 
            // lblRightRms
            // 
            lblRightRms.Location = new Point(10, 40);
            lblRightRms.Name = "lblRightRms";
            lblRightRms.Size = new Size(100, 23);
            lblRightRms.TabIndex = 1;
            // 
            // lblRightPeak
            // 
            lblRightPeak.Location = new Point(10, 65);
            lblRightPeak.Name = "lblRightPeak";
            lblRightPeak.Size = new Size(100, 23);
            lblRightPeak.TabIndex = 2;
            // 
            // lblRightRmsV
            // 
            lblRightRmsV.Location = new Point(10, 88);
            lblRightRmsV.Name = "lblRightRmsV";
            lblRightRmsV.Size = new Size(120, 23);
            lblRightRmsV.TabIndex = 18;
            lblRightRmsV.Visible = false;
            // 
            // lblRightPeakV
            // 
            lblRightPeakV.Location = new Point(10, 111);
            lblRightPeakV.Name = "lblRightPeakV";
            lblRightPeakV.Size = new Size(120, 23);
            lblRightPeakV.TabIndex = 19;
            lblRightPeakV.Visible = false;
            // 
            // grpLeftStats
            // 
            grpLeftStats.Controls.Add(lblLeftMeas);
            grpLeftStats.Controls.Add(lblLeftHeader);
            grpLeftStats.Controls.Add(lblLeftRms);
            grpLeftStats.Controls.Add(lblLeftPeak);
            grpLeftStats.Controls.Add(lblLeftRmsV);
            grpLeftStats.Controls.Add(lblLeftPeakV);
            grpLeftStats.Location = new Point(5, 11);
            grpLeftStats.Name = "grpLeftStats";
            grpLeftStats.Size = new Size(140, 180);
            grpLeftStats.TabIndex = 0;
            grpLeftStats.TabStop = false;
            // 
            // lblLeftMeas
            // 
            lblLeftMeas.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblLeftMeas.Location = new Point(12, 88);
            lblLeftMeas.Name = "lblLeftMeas";
            lblLeftMeas.Size = new Size(128, 92);
            lblLeftMeas.TabIndex = 17;
            lblLeftMeas.Text = "---";
            lblLeftMeas.Click += LblLeftMeas_Click;
            // 
            // lblLeftHeader
            // 
            lblLeftHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblLeftHeader.ForeColor = Color.DodgerBlue;
            lblLeftHeader.Location = new Point(10, 10);
            lblLeftHeader.Name = "lblLeftHeader";
            lblLeftHeader.Size = new Size(100, 23);
            lblLeftHeader.TabIndex = 0;
            lblLeftHeader.Text = "LEFT";
            // 
            // lblLeftRms
            // 
            lblLeftRms.Location = new Point(10, 40);
            lblLeftRms.Name = "lblLeftRms";
            lblLeftRms.Size = new Size(100, 23);
            lblLeftRms.TabIndex = 1;
            // 
            // lblLeftPeak
            // 
            lblLeftPeak.Location = new Point(10, 65);
            lblLeftPeak.Name = "lblLeftPeak";
            lblLeftPeak.Size = new Size(100, 23);
            lblLeftPeak.TabIndex = 2;
            // 
            // lblLeftRmsV
            // 
            lblLeftRmsV.Location = new Point(10, 88);
            lblLeftRmsV.Name = "lblLeftRmsV";
            lblLeftRmsV.Size = new Size(120, 23);
            lblLeftRmsV.TabIndex = 18;
            lblLeftRmsV.Visible = false;
            // 
            // lblLeftPeakV
            // 
            lblLeftPeakV.Location = new Point(10, 111);
            lblLeftPeakV.Name = "lblLeftPeakV";
            lblLeftPeakV.Size = new Size(120, 23);
            lblLeftPeakV.TabIndex = 19;
            lblLeftPeakV.Visible = false;
            // 
            // chkCursorL
            // 
            chkCursorL.AutoSize = true;
            chkCursorL.Location = new Point(5, 197);
            chkCursorL.Name = "chkCursorL";
            chkCursorL.Size = new Size(70, 19);
            chkCursorL.TabIndex = 15;
            chkCursorL.Text = "Cursor L";
            // 
            // chkCursorR
            // 
            chkCursorR.AutoSize = true;
            chkCursorR.Location = new Point(5, 408);
            chkCursorR.Name = "chkCursorR";
            chkCursorR.Size = new Size(71, 19);
            chkCursorR.TabIndex = 16;
            chkCursorR.Text = "Cursor R";
            //             
            // SpectrumControl
            // 
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

        private Panel panelTop;
        private Label lblUnits;
        private ComboBox comboLevelUnit;
        private Label lblWindow;
        private ComboBox comboWindow;
        private Label lblAveraging;
        private ComboBox comboAveraging;
        private Label lblChannel;
        private ComboBox comboChannel;

        private Label lblWindowInfo;

        private Button btnRun;

        private System.Windows.Forms.DataVisualization.Charting.Chart chartSpectrum;
        private CheckBox chkLogX;
        private CheckBox chkEnableThd;
        private Button btnThdSettings;
        private Button btnResetAvg;
        private Label lblAvgProgress;
        private CheckBox chkPeakHold;
        private Button btnResetPeakHold;
        private CheckBox chkCursorR;
        private CheckBox chkCursorL;
        private GroupBox grpLeftStats;
        private Label lblLeftMeas;
        private Label lblLeftHeader;
        private Label lblLeftRms;
        private Label lblLeftPeak;
        private Label lblLeftRmsV;
        private Label lblLeftPeakV;
        private GroupBox grpRightStats;
        private Label lblRightMeas;
        private Label lblRightHeader;
        private Label lblRightRms;
        private Label lblRightPeak;
        private Label lblRightRmsV;
        private Label lblRightPeakV;
        private Label label1;
        //
        // Cursor Controls
        private TableLayoutPanel tlpCursors;
        private TableLayoutPanel tlpPhi;
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
        private Label label2;
        private Label lblFftTicker;
        private Button btnPhiEnable;
        private Button btnPhiUnwrap;
        private Button btnOverlay;
        private CheckBox chkDcMode;

    }
}