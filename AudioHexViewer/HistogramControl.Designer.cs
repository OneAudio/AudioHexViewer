namespace AudioHexViewer
{
    partial class HistogramControl
    {
        /// <summary> 
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur de composants

        /// <summary> 
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas 
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            lblLeftHeader = new Label();
            lblLeftSamples = new Label();
            lblLeftDC = new Label();
            lblLeftMinMax = new Label();
            grpLeftStats = new GroupBox();
            lblLeftRms = new Label();
            lblRightHeader = new Label();
            lblRightSamples = new Label();
            lblRightDC = new Label();
            lblRightRms = new Label();
            lblRightMinMax = new Label();
            grpRightStats = new GroupBox();
            panelStats = new Panel();
            chartHistogram = new System.Windows.Forms.DataVisualization.Charting.Chart();
            splitContainerMain = new SplitContainer();
            btnSaveSettings = new Button();
            lblBinPow = new Label();
            lblRangePow = new Label();
            chkAC = new CheckBox();
            lblChannel = new Label();
            comboChannel = new ComboBox();
            btnLive = new Button();
            btnOnce = new Button();
            lblRecordTime = new Label();
            panelTop = new Panel();
            lblWindowInfo = new Label();
            comboSampleWin = new ComboBox();
            comboBinLsb = new ComboBox();
            comboRangeLsb = new ComboBox();
            grpLeftStats.SuspendLayout();
            grpRightStats.SuspendLayout();
            panelStats.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)chartHistogram).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).BeginInit();
            splitContainerMain.Panel1.SuspendLayout();
            splitContainerMain.Panel2.SuspendLayout();
            splitContainerMain.SuspendLayout();
            panelTop.SuspendLayout();
            SuspendLayout();
            // 
            // lblLeftHeader
            // 
            lblLeftHeader.AutoSize = true;
            lblLeftHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblLeftHeader.ForeColor = Color.DodgerBlue;
            lblLeftHeader.Location = new Point(10, 10);
            lblLeftHeader.Name = "lblLeftHeader";
            lblLeftHeader.Size = new Size(32, 15);
            lblLeftHeader.TabIndex = 0;
            lblLeftHeader.Text = "LEFT";
            // 
            // lblLeftSamples
            // 
            lblLeftSamples.AutoSize = true;
            lblLeftSamples.Location = new Point(12, 40);
            lblLeftSamples.Name = "lblLeftSamples";
            lblLeftSamples.Size = new Size(0, 15);
            lblLeftSamples.TabIndex = 1;
            // 
            // lblLeftDC
            // 
            lblLeftDC.AutoSize = true;
            lblLeftDC.Location = new Point(12, 60);
            lblLeftDC.Name = "lblLeftDC";
            lblLeftDC.Size = new Size(0, 15);
            lblLeftDC.TabIndex = 2;
            // 
            // lblLeftMinMax
            // 
            lblLeftMinMax.AutoSize = true;
            lblLeftMinMax.Location = new Point(12, 100);
            lblLeftMinMax.Name = "lblLeftMinMax";
            lblLeftMinMax.Size = new Size(0, 15);
            lblLeftMinMax.TabIndex = 4;
            // 
            // grpLeftStats
            // 
            grpLeftStats.Controls.Add(lblLeftHeader);
            grpLeftStats.Controls.Add(lblLeftSamples);
            grpLeftStats.Controls.Add(lblLeftDC);
            grpLeftStats.Controls.Add(lblLeftRms);
            grpLeftStats.Controls.Add(lblLeftMinMax);
            grpLeftStats.ForeColor = Color.Black;
            grpLeftStats.Location = new Point(3, 12);
            grpLeftStats.Name = "grpLeftStats";
            grpLeftStats.Size = new Size(125, 180);
            grpLeftStats.TabIndex = 0;
            grpLeftStats.TabStop = false;
            // 
            // lblLeftRms
            // 
            lblLeftRms.AutoSize = true;
            lblLeftRms.Location = new Point(12, 80);
            lblLeftRms.Name = "lblLeftRms";
            lblLeftRms.Size = new Size(0, 15);
            lblLeftRms.TabIndex = 3;
            // 
            // lblRightHeader
            // 
            lblRightHeader.AutoSize = true;
            lblRightHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblRightHeader.ForeColor = Color.OrangeRed;
            lblRightHeader.Location = new Point(10, 10);
            lblRightHeader.Name = "lblRightHeader";
            lblRightHeader.Size = new Size(44, 15);
            lblRightHeader.TabIndex = 0;
            lblRightHeader.Text = "RIGHT";
            // 
            // lblRightSamples
            // 
            lblRightSamples.AutoSize = true;
            lblRightSamples.Location = new Point(12, 40);
            lblRightSamples.Name = "lblRightSamples";
            lblRightSamples.Size = new Size(0, 15);
            lblRightSamples.TabIndex = 1;
            // 
            // lblRightDC
            // 
            lblRightDC.AutoSize = true;
            lblRightDC.Location = new Point(12, 60);
            lblRightDC.Name = "lblRightDC";
            lblRightDC.Size = new Size(0, 15);
            lblRightDC.TabIndex = 2;
            // 
            // lblRightRms
            // 
            lblRightRms.AutoSize = true;
            lblRightRms.Location = new Point(12, 80);
            lblRightRms.Name = "lblRightRms";
            lblRightRms.Size = new Size(0, 15);
            lblRightRms.TabIndex = 3;
            // 
            // lblRightMinMax
            // 
            lblRightMinMax.AutoSize = true;
            lblRightMinMax.Location = new Point(12, 100);
            lblRightMinMax.Name = "lblRightMinMax";
            lblRightMinMax.Size = new Size(0, 15);
            lblRightMinMax.TabIndex = 4;
            // 
            // grpRightStats
            // 
            grpRightStats.Controls.Add(lblRightHeader);
            grpRightStats.Controls.Add(lblRightSamples);
            grpRightStats.Controls.Add(lblRightDC);
            grpRightStats.Controls.Add(lblRightRms);
            grpRightStats.Controls.Add(lblRightMinMax);
            grpRightStats.ForeColor = Color.Black;
            grpRightStats.Location = new Point(3, 198);
            grpRightStats.Name = "grpRightStats";
            grpRightStats.Size = new Size(125, 180);
            grpRightStats.TabIndex = 1;
            grpRightStats.TabStop = false;
            // 
            // panelStats
            // 
            panelStats.BackColor = Color.WhiteSmoke;
            panelStats.Controls.Add(grpRightStats);
            panelStats.Controls.Add(grpLeftStats);
            panelStats.Dock = DockStyle.Fill;
            panelStats.Location = new Point(0, 0);
            panelStats.Name = "panelStats";
            panelStats.Padding = new Padding(8);
            panelStats.Size = new Size(148, 398);
            panelStats.TabIndex = 0;
            // 
            // chartHistogram
            // 
            chartHistogram.Dock = DockStyle.Fill;
            chartHistogram.Location = new Point(0, 0);
            chartHistogram.Name = "chartHistogram";
            chartHistogram.Size = new Size(760, 398);
            chartHistogram.TabIndex = 0;
            chartHistogram.Text = "chartHistogram";
            // 
            // splitContainerMain
            // 
            splitContainerMain.Dock = DockStyle.Fill;
            splitContainerMain.Location = new Point(0, 99);
            splitContainerMain.Name = "splitContainerMain";
            // 
            // splitContainerMain.Panel1
            // 
            splitContainerMain.Panel1.Controls.Add(chartHistogram);
            // 
            // splitContainerMain.Panel2
            // 
            splitContainerMain.Panel2.Controls.Add(panelStats);
            splitContainerMain.Size = new Size(912, 398);
            splitContainerMain.SplitterDistance = 760;
            splitContainerMain.TabIndex = 3;
            // 
            // btnSaveSettings
            // 
            btnSaveSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSaveSettings.Location = new Point(802, 15);
            btnSaveSettings.Name = "btnSaveSettings";
            btnSaveSettings.Size = new Size(90, 25);
            btnSaveSettings.TabIndex = 9;
            btnSaveSettings.Text = "Save settings";
            btnSaveSettings.UseVisualStyleBackColor = true;
            btnSaveSettings.Click += btnSaveSettings_Click;
            // 
            // lblBinPow
            // 
            lblBinPow.AutoSize = true;
            lblBinPow.Location = new Point(332, 40);
            lblBinPow.Name = "lblBinPow";
            lblBinPow.Size = new Size(81, 15);
            lblBinPow.TabIndex = 6;
            lblBinPow.Text = "LSB/bin width";
            // 
            // lblRangePow
            // 
            lblRangePow.AutoSize = true;
            lblRangePow.Location = new Point(202, 40);
            lblRangePow.Name = "lblRangePow";
            lblRangePow.Size = new Size(64, 15);
            lblRangePow.TabIndex = 5;
            lblRangePow.Text = "±LSB Scale";
            // 
            // chkAC
            // 
            chkAC.AutoSize = true;
            chkAC.Location = new Point(60, 39);
            chkAC.Name = "chkAC";
            chkAC.Size = new Size(85, 19);
            chkAC.TabIndex = 2;
            chkAC.Text = "remove DC";
            chkAC.UseVisualStyleBackColor = true;
            chkAC.Click += chkAC_CheckedChanged;
            // 
            // lblChannel
            // 
            lblChannel.AutoSize = true;
            lblChannel.Location = new Point(3, 20);
            lblChannel.Name = "lblChannel";
            lblChannel.Size = new Size(51, 15);
            lblChannel.TabIndex = 1;
            lblChannel.Text = "Channel";
            // 
            // comboChannel
            // 
            comboChannel.DropDownStyle = ComboBoxStyle.DropDownList;
            comboChannel.FormattingEnabled = true;
            comboChannel.Items.AddRange(new object[] { "Left", "Right", "Both" });
            comboChannel.Location = new Point(60, 14);
            comboChannel.Name = "comboChannel";
            comboChannel.Size = new Size(100, 23);
            comboChannel.TabIndex = 0;
            comboChannel.Click += comboChannel_SelectedIndexChanged;
            // 
            // btnLive
            // 
            btnLive.Location = new Point(617, 14);
            btnLive.Name = "btnLive";
            btnLive.Size = new Size(60, 25);
            btnLive.TabIndex = 12;
            btnLive.Text = "LIVE";
            btnLive.UseVisualStyleBackColor = true;
            btnLive.Click += btnLive_Click;
            // 
            // btnOnce
            // 
            btnOnce.Location = new Point(617, 49);
            btnOnce.Name = "btnOnce";
            btnOnce.Size = new Size(60, 25);
            btnOnce.TabIndex = 11;
            btnOnce.Text = "ONCE";
            btnOnce.UseVisualStyleBackColor = true;
            btnOnce.Click += btnOnce_Click;
            // 
            // lblRecordTime
            // 
            lblRecordTime.AutoSize = true;
            lblRecordTime.Location = new Point(461, 40);
            lblRecordTime.Name = "lblRecordTime";
            lblRecordTime.Size = new Size(112, 15);
            lblRecordTime.TabIndex = 9;
            lblRecordTime.Text = "Number of Samples";
            // 
            // panelTop
            // 
            panelTop.Controls.Add(lblWindowInfo);
            panelTop.Controls.Add(comboSampleWin);
            panelTop.Controls.Add(comboBinLsb);
            panelTop.Controls.Add(comboRangeLsb);
            panelTop.Controls.Add(btnSaveSettings);
            panelTop.Controls.Add(lblBinPow);
            panelTop.Controls.Add(lblRangePow);
            panelTop.Controls.Add(chkAC);
            panelTop.Controls.Add(lblChannel);
            panelTop.Controls.Add(comboChannel);
            panelTop.Controls.Add(btnLive);
            panelTop.Controls.Add(btnOnce);
            panelTop.Controls.Add(lblRecordTime);
            panelTop.Dock = DockStyle.Top;
            panelTop.Location = new Point(0, 0);
            panelTop.Name = "panelTop";
            panelTop.Size = new Size(912, 99);
            panelTop.TabIndex = 2;
            // 
            // lblWindowInfo
            // 
            lblWindowInfo.AutoSize = true;
            lblWindowInfo.Location = new Point(461, 59);
            lblWindowInfo.Name = "lblWindowInfo";
            lblWindowInfo.Size = new Size(38, 15);
            lblWindowInfo.TabIndex = 17;
            lblWindowInfo.Text = "label1";
            // 
            // comboSampleWin
            // 
            comboSampleWin.DropDownStyle = ComboBoxStyle.DropDownList;
            comboSampleWin.FormattingEnabled = true;
            comboSampleWin.Location = new Point(461, 14);
            comboSampleWin.Name = "comboSampleWin";
            comboSampleWin.Size = new Size(121, 23);
            comboSampleWin.TabIndex = 16;
            comboSampleWin.SelectedIndexChanged += comboSampleWin_SelectedIndexChanged;
            // 
            // comboBinLsb
            // 
            comboBinLsb.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBinLsb.FormattingEnabled = true;
            comboBinLsb.Location = new Point(315, 14);
            comboBinLsb.Name = "comboBinLsb";
            comboBinLsb.Size = new Size(121, 23);
            comboBinLsb.TabIndex = 15;
            // 
            // comboRangeLsb
            // 
            comboRangeLsb.DropDownStyle = ComboBoxStyle.DropDownList;
            comboRangeLsb.FormattingEnabled = true;
            comboRangeLsb.Location = new Point(178, 14);
            comboRangeLsb.Name = "comboRangeLsb";
            comboRangeLsb.Size = new Size(121, 23);
            comboRangeLsb.TabIndex = 14;
            // 
            // HistogramControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(splitContainerMain);
            Controls.Add(panelTop);
            Name = "HistogramControl";
            Size = new Size(912, 497);
            grpLeftStats.ResumeLayout(false);
            grpLeftStats.PerformLayout();
            grpRightStats.ResumeLayout(false);
            grpRightStats.PerformLayout();
            panelStats.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)chartHistogram).EndInit();
            splitContainerMain.Panel1.ResumeLayout(false);
            splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).EndInit();
            splitContainerMain.ResumeLayout(false);
            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Label lblLeftHeader;
        private Label lblLeftSamples;
        private Label lblLeftDC;
        private Label lblLeftMinMax;
        private GroupBox grpLeftStats;
        private Label lblLeftRms;
        private Label lblRightHeader;
        private Label lblRightSamples;
        private Label lblRightDC;
        private Label lblRightRms;
        private Label lblRightMinMax;
        private GroupBox grpRightStats;
        public Panel panelStats;
        private System.Windows.Forms.DataVisualization.Charting.Chart chartHistogram;
        private SplitContainer splitContainerMain;
        private Button btnSaveSettings;
        private Label lblBinPow;
        private Label lblRangePow;
        private CheckBox chkAC;
        private Label lblChannel;
        private ComboBox comboChannel;
        private Button btnLive;
        private Button btnOnce;
        private Label lblRecordTime;
        private Panel panelTop;
        private ComboBox comboSampleWin;
        private ComboBox comboBinLsb;
        private ComboBox comboRangeLsb;
        private Label lblWindowInfo;
    }
}
