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
            lblAvgProgress = new Label();
            chkRemoveDC = new CheckBox();
            chkPeakHold = new CheckBox();
            btnResetPeakHold = new Button();
            btnResetAvg = new Button();
            lblWindow = new Label();
            comboWindow = new ComboBox();
            lblAveraging = new Label();
            comboAveraging = new ComboBox();
            lblChannel = new Label();
            comboChannel = new ComboBox();
            lblWindowInfo = new Label();
            btnRun = new Button();
            chkLogX = new CheckBox();
            chartSpectrum = new System.Windows.Forms.DataVisualization.Charting.Chart();
            grpRightStats = new GroupBox();
            lblRightMeas = new Label();
            lblRightHeader = new Label();
            lblRightRms = new Label();
            lblRightPeak = new Label();
            grpLeftStats = new GroupBox();
            lblLeftMeas = new Label();
            lblLeftHeader = new Label();
            lblLeftRms = new Label();
            lblLeftPeak = new Label();
            chkCursorL = new CheckBox();
            chkCursorR = new CheckBox();
            panelStats = new Panel();
            panelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)chartSpectrum).BeginInit();
            grpRightStats.SuspendLayout();
            grpLeftStats.SuspendLayout();
            panelStats.SuspendLayout();
            SuspendLayout();
            // 
            // panelTop
            // 
            panelTop.Controls.Add(lblAvgProgress);
            panelTop.Controls.Add(chkRemoveDC);
            panelTop.Controls.Add(chkPeakHold);
            panelTop.Controls.Add(btnResetPeakHold);
            panelTop.Controls.Add(btnResetAvg);
            panelTop.Controls.Add(lblWindow);
            panelTop.Controls.Add(comboWindow);
            panelTop.Controls.Add(lblAveraging);
            panelTop.Controls.Add(comboAveraging);
            panelTop.Controls.Add(lblChannel);
            panelTop.Controls.Add(comboChannel);
            panelTop.Controls.Add(lblWindowInfo);
            panelTop.Controls.Add(btnRun);
            panelTop.Controls.Add(chkLogX);
            panelTop.Dock = DockStyle.Top;
            panelTop.Location = new Point(0, 0);
            panelTop.Name = "panelTop";
            panelTop.Size = new Size(1138, 60);
            panelTop.TabIndex = 1;
            // 
            // lblAvgProgress
            // 
            lblAvgProgress.AutoSize = true;
            lblAvgProgress.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblAvgProgress.ForeColor = SystemColors.HotTrack;
            lblAvgProgress.Location = new Point(636, 7);
            lblAvgProgress.Name = "lblAvgProgress";
            lblAvgProgress.Size = new Size(51, 21);
            lblAvgProgress.TabIndex = 14;
            lblAvgProgress.Text = "label1";
            // 
            // chkRemoveDC
            // 
            chkRemoveDC.AutoSize = true;
            chkRemoveDC.Location = new Point(60, 35);
            chkRemoveDC.Name = "chkRemoveDC";
            chkRemoveDC.Size = new Size(85, 19);
            chkRemoveDC.TabIndex = 13;
            chkRemoveDC.Text = "remove DC";
            chkRemoveDC.UseVisualStyleBackColor = true;
            // 
            // chkPeakHold
            // 
            chkPeakHold.AutoSize = true;
            chkPeakHold.Location = new Point(460, 36);
            chkPeakHold.Name = "chkPeakHold";
            chkPeakHold.Size = new Size(77, 19);
            chkPeakHold.TabIndex = 17;
            chkPeakHold.Text = "PeakHold";
            chkPeakHold.UseVisualStyleBackColor = true;
            // 
            // btnResetPeakHold
            // 
            btnResetPeakHold.Location = new Point(556, 31);
            btnResetPeakHold.Name = "btnResetPeakHold";
            btnResetPeakHold.Size = new Size(74, 23);
            btnResetPeakHold.TabIndex = 18;
            btnResetPeakHold.TabStop = false;
            btnResetPeakHold.Text = "Clear Hold";
            btnResetPeakHold.UseVisualStyleBackColor = true;
            btnResetPeakHold.Click += btnResetPeakHold_Click;
            // 
            // btnResetAvg
            // 
            btnResetAvg.Location = new Point(556, 7);
            btnResetAvg.Name = "btnResetAvg";
            btnResetAvg.Size = new Size(74, 23);
            btnResetAvg.TabIndex = 12;
            btnResetAvg.Text = "Clear AVG";
            btnResetAvg.UseVisualStyleBackColor = true;
            btnResetAvg.Click += btnResetAvg_Click;
            // 
            // lblWindow
            // 
            lblWindow.AutoSize = true;
            lblWindow.Location = new Point(203, 10);
            lblWindow.Name = "lblWindow";
            lblWindow.Size = new Size(51, 15);
            lblWindow.TabIndex = 2;
            lblWindow.Text = "Window";
            // 
            // comboWindow
            // 
            comboWindow.DropDownStyle = ComboBoxStyle.DropDownList;
            comboWindow.Location = new Point(260, 7);
            comboWindow.Name = "comboWindow";
            comboWindow.Size = new Size(130, 23);
            comboWindow.TabIndex = 3;
            // 
            // lblAveraging
            // 
            lblAveraging.AutoSize = true;
            lblAveraging.Location = new Point(426, 10);
            lblAveraging.Name = "lblAveraging";
            lblAveraging.Size = new Size(28, 15);
            lblAveraging.TabIndex = 4;
            lblAveraging.Text = "Avg";
            // 
            // comboAveraging
            // 
            comboAveraging.DropDownStyle = ComboBoxStyle.DropDownList;
            comboAveraging.Location = new Point(460, 7);
            comboAveraging.Name = "comboAveraging";
            comboAveraging.Size = new Size(90, 23);
            comboAveraging.TabIndex = 5;
            // 
            // lblChannel
            // 
            lblChannel.AutoSize = true;
            lblChannel.Location = new Point(3, 10);
            lblChannel.Name = "lblChannel";
            lblChannel.Size = new Size(51, 15);
            lblChannel.TabIndex = 6;
            lblChannel.Text = "Channel";
            // 
            // comboChannel
            // 
            comboChannel.DropDownStyle = ComboBoxStyle.DropDownList;
            comboChannel.Location = new Point(60, 7);
            comboChannel.Name = "comboChannel";
            comboChannel.Size = new Size(100, 23);
            comboChannel.TabIndex = 7;
            // 
            // lblWindowInfo
            // 
            lblWindowInfo.AutoSize = true;
            lblWindowInfo.Location = new Point(990, 40);
            lblWindowInfo.Name = "lblWindowInfo";
            lblWindowInfo.Size = new Size(47, 15);
            lblWindowInfo.TabIndex = 8;
            lblWindowInfo.Text = "≈ 0.00 s";
            // 
            // btnRun
            // 
            btnRun.FlatStyle = FlatStyle.Flat;
            btnRun.Location = new Point(1044, 4);
            btnRun.Name = "btnRun";
            btnRun.Size = new Size(79, 51);
            btnRun.TabIndex = 9;
            btnRun.Text = "RUN";
            // 
            // chkLogX
            // 
            chkLogX.AutoSize = true;
            chkLogX.Location = new Point(260, 35);
            chkLogX.Name = "chkLogX";
            chkLogX.Size = new Size(67, 19);
            chkLogX.TabIndex = 11;
            chkLogX.Text = "log freq";
            // 
            // chartSpectrum
            // 
            chartSpectrum.Dock = DockStyle.Fill;
            chartSpectrum.Location = new Point(0, 60);
            chartSpectrum.Name = "chartSpectrum";
            chartSpectrum.Size = new Size(1138, 440);
            chartSpectrum.TabIndex = 0;
            // 
            // grpRightStats
            // 
            grpRightStats.Controls.Add(lblRightMeas);
            grpRightStats.Controls.Add(lblRightHeader);
            grpRightStats.Controls.Add(lblRightRms);
            grpRightStats.Controls.Add(lblRightPeak);
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
            lblRightMeas.Click += lblRightMeas_Click;
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
            // grpLeftStats
            // 
            grpLeftStats.Controls.Add(lblLeftMeas);
            grpLeftStats.Controls.Add(lblLeftHeader);
            grpLeftStats.Controls.Add(lblLeftRms);
            grpLeftStats.Controls.Add(lblLeftPeak);
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
            lblLeftMeas.Click += lblLeftMeas_Click;
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
            // panelStats
            // 
            panelStats.Controls.Add(chkCursorR);
            panelStats.Controls.Add(chkCursorL);
            panelStats.Controls.Add(grpLeftStats);
            panelStats.Controls.Add(grpRightStats);
            panelStats.Dock = DockStyle.Fill;
            panelStats.Location = new Point(0, 60);
            panelStats.Name = "panelStats";
            panelStats.Padding = new Padding(8);
            panelStats.Size = new Size(1138, 440);
            panelStats.TabIndex = 0;
            // 
            // SpectrumControl
            // 
            BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(chartSpectrum);
            Controls.Add(panelStats);
            Controls.Add(panelTop);
            Name = "SpectrumControl";
            Size = new Size(1138, 500);
            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)chartSpectrum).EndInit();
            grpRightStats.ResumeLayout(false);
            grpLeftStats.ResumeLayout(false);
            panelStats.ResumeLayout(false);
            panelStats.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel panelTop;
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
        private Button btnResetAvg;
        private CheckBox chkRemoveDC;
        private Label lblAvgProgress;
        private CheckBox chkPeakHold;
        private Button btnResetPeakHold;
        public Panel panelStats;
        private CheckBox chkCursorR;
        private CheckBox chkCursorL;
        private GroupBox grpLeftStats;
        private Label lblLeftMeas;
        private Label lblLeftHeader;
        private Label lblLeftRms;
        private Label lblLeftPeak;
        private GroupBox grpRightStats;
        private Label lblRightMeas;
        private Label lblRightHeader;
        private Label lblRightRms;
        private Label lblRightPeak;
    }
}
