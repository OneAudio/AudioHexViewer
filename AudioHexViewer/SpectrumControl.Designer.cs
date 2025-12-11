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
            btnResetAvg = new Button();
            lblFftSize = new Label();
            comboFftSize = new ComboBox();
            lblWindow = new Label();
            comboWindow = new ComboBox();
            lblAveraging = new Label();
            comboAveraging = new ComboBox();
            lblChannel = new Label();
            comboChannel = new ComboBox();
            lblWindowInfo = new Label();
            btnRun = new Button();
            chkOnce = new CheckBox();
            chkLogX = new CheckBox();
            splitMain = new SplitContainer();
            chartSpectrum = new System.Windows.Forms.DataVisualization.Charting.Chart();
            panelStats = new Panel();
            grpLeftStats = new GroupBox();
            lblLeftHeader = new Label();
            lblLeftRms = new Label();
            lblLeftPeak = new Label();
            grpRightStats = new GroupBox();
            lblRightHeader = new Label();
            lblRightRms = new Label();
            lblRightPeak = new Label();
            panelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
            splitMain.Panel1.SuspendLayout();
            splitMain.Panel2.SuspendLayout();
            splitMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)chartSpectrum).BeginInit();
            panelStats.SuspendLayout();
            grpLeftStats.SuspendLayout();
            grpRightStats.SuspendLayout();
            SuspendLayout();
            // 
            // panelTop
            // 
            panelTop.Controls.Add(btnResetAvg);
            panelTop.Controls.Add(lblFftSize);
            panelTop.Controls.Add(comboFftSize);
            panelTop.Controls.Add(lblWindow);
            panelTop.Controls.Add(comboWindow);
            panelTop.Controls.Add(lblAveraging);
            panelTop.Controls.Add(comboAveraging);
            panelTop.Controls.Add(lblChannel);
            panelTop.Controls.Add(comboChannel);
            panelTop.Controls.Add(lblWindowInfo);
            panelTop.Controls.Add(btnRun);
            panelTop.Controls.Add(chkOnce);
            panelTop.Controls.Add(chkLogX);
            panelTop.Dock = DockStyle.Top;
            panelTop.Location = new Point(0, 0);
            panelTop.Name = "panelTop";
            panelTop.Size = new Size(1138, 60);
            panelTop.TabIndex = 1;
            // 
            // btnResetAvg
            // 
            btnResetAvg.Location = new Point(450, 31);
            btnResetAvg.Name = "btnResetAvg";
            btnResetAvg.Size = new Size(42, 23);
            btnResetAvg.TabIndex = 12;
            btnResetAvg.Text = "RAZ";
            btnResetAvg.UseVisualStyleBackColor = true;
            btnResetAvg.Click += btnResetAvg_Click;
            // 
            // lblFftSize
            // 
            lblFftSize.AutoSize = true;
            lblFftSize.Location = new Point(10, 10);
            lblFftSize.Name = "lblFftSize";
            lblFftSize.Size = new Size(47, 15);
            lblFftSize.TabIndex = 0;
            lblFftSize.Text = "FFT size";
            // 
            // comboFftSize
            // 
            comboFftSize.DropDownStyle = ComboBoxStyle.DropDownList;
            comboFftSize.Location = new Point(70, 7);
            comboFftSize.Name = "comboFftSize";
            comboFftSize.Size = new Size(120, 23);
            comboFftSize.TabIndex = 1;
            // 
            // lblWindow
            // 
            lblWindow.AutoSize = true;
            lblWindow.Location = new Point(210, 10);
            lblWindow.Name = "lblWindow";
            lblWindow.Size = new Size(51, 15);
            lblWindow.TabIndex = 2;
            lblWindow.Text = "Window";
            // 
            // comboWindow
            // 
            comboWindow.DropDownStyle = ComboBoxStyle.DropDownList;
            comboWindow.Location = new Point(270, 7);
            comboWindow.Name = "comboWindow";
            comboWindow.Size = new Size(120, 23);
            comboWindow.TabIndex = 3;
            // 
            // lblAveraging
            // 
            lblAveraging.AutoSize = true;
            lblAveraging.Location = new Point(410, 10);
            lblAveraging.Name = "lblAveraging";
            lblAveraging.Size = new Size(28, 15);
            lblAveraging.TabIndex = 4;
            lblAveraging.Text = "Avg";
            // 
            // comboAveraging
            // 
            comboAveraging.DropDownStyle = ComboBoxStyle.DropDownList;
            comboAveraging.Location = new Point(450, 7);
            comboAveraging.Name = "comboAveraging";
            comboAveraging.Size = new Size(90, 23);
            comboAveraging.TabIndex = 5;
            // 
            // lblChannel
            // 
            lblChannel.AutoSize = true;
            lblChannel.Location = new Point(560, 10);
            lblChannel.Name = "lblChannel";
            lblChannel.Size = new Size(51, 15);
            lblChannel.TabIndex = 6;
            lblChannel.Text = "Channel";
            // 
            // comboChannel
            // 
            comboChannel.DropDownStyle = ComboBoxStyle.DropDownList;
            comboChannel.Location = new Point(625, 7);
            comboChannel.Name = "comboChannel";
            comboChannel.Size = new Size(90, 23);
            comboChannel.TabIndex = 7;
            // 
            // lblWindowInfo
            // 
            lblWindowInfo.AutoSize = true;
            lblWindowInfo.Location = new Point(730, 10);
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
            // chkOnce
            // 
            chkOnce.AutoSize = true;
            chkOnce.Location = new Point(860, 35);
            chkOnce.Name = "chkOnce";
            chkOnce.Size = new Size(52, 19);
            chkOnce.TabIndex = 10;
            chkOnce.Text = "once";
            // 
            // chkLogX
            // 
            chkLogX.AutoSize = true;
            chkLogX.Location = new Point(70, 35);
            chkLogX.Name = "chkLogX";
            chkLogX.Size = new Size(67, 19);
            chkLogX.TabIndex = 11;
            chkLogX.Text = "log freq";
            // 
            // splitMain
            // 
            splitMain.Dock = DockStyle.Fill;
            splitMain.Location = new Point(0, 60);
            splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            splitMain.Panel1.Controls.Add(chartSpectrum);
            // 
            // splitMain.Panel2
            // 
            splitMain.Panel2.Controls.Add(panelStats);
            splitMain.Size = new Size(1138, 440);
            splitMain.SplitterDistance = 917;
            splitMain.TabIndex = 0;
            // 
            // chartSpectrum
            // 
            chartSpectrum.Dock = DockStyle.Fill;
            chartSpectrum.Location = new Point(0, 0);
            chartSpectrum.Name = "chartSpectrum";
            chartSpectrum.Size = new Size(917, 440);
            chartSpectrum.TabIndex = 0;
            // 
            // panelStats
            // 
            panelStats.Controls.Add(grpLeftStats);
            panelStats.Controls.Add(grpRightStats);
            panelStats.Dock = DockStyle.Fill;
            panelStats.Location = new Point(0, 0);
            panelStats.Name = "panelStats";
            panelStats.Padding = new Padding(8);
            panelStats.Size = new Size(217, 440);
            panelStats.TabIndex = 0;
            // 
            // grpLeftStats
            // 
            grpLeftStats.Controls.Add(lblLeftHeader);
            grpLeftStats.Controls.Add(lblLeftRms);
            grpLeftStats.Controls.Add(lblLeftPeak);
            grpLeftStats.Location = new Point(5, 5);
            grpLeftStats.Name = "grpLeftStats";
            grpLeftStats.Size = new Size(140, 120);
            grpLeftStats.TabIndex = 0;
            grpLeftStats.TabStop = false;
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
            // grpRightStats
            // 
            grpRightStats.Controls.Add(lblRightHeader);
            grpRightStats.Controls.Add(lblRightRms);
            grpRightStats.Controls.Add(lblRightPeak);
            grpRightStats.Location = new Point(5, 140);
            grpRightStats.Name = "grpRightStats";
            grpRightStats.Size = new Size(140, 120);
            grpRightStats.TabIndex = 1;
            grpRightStats.TabStop = false;
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
            // SpectrumControl
            // 
            BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;   // 
            Controls.Add(splitMain);
            Controls.Add(panelTop);
            Name = "SpectrumControl";
            Size = new Size(1138, 500);
            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            splitMain.Panel1.ResumeLayout(false);
            splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
            splitMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)chartSpectrum).EndInit();
            panelStats.ResumeLayout(false);
            grpLeftStats.ResumeLayout(false);
            grpRightStats.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel panelTop;
        private Label lblFftSize;
        private ComboBox comboFftSize;
        private Label lblWindow;
        private ComboBox comboWindow;
        private Label lblAveraging;
        private ComboBox comboAveraging;
        private Label lblChannel;
        private ComboBox comboChannel;

        private Label lblWindowInfo;

        private Button btnRun;
        private CheckBox chkOnce;

        private SplitContainer splitMain;
        private System.Windows.Forms.DataVisualization.Charting.Chart chartSpectrum;

        public Panel panelStats;
        private GroupBox grpLeftStats;
        private GroupBox grpRightStats;

        private Label lblLeftHeader;
        private Label lblLeftRms;
        private Label lblLeftPeak;

        private Label lblRightHeader;
        private Label lblRightRms;
        private Label lblRightPeak;
        private CheckBox chkLogX;
        private Button btnResetAvg;
    }
}
