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
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
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
            lblRightMinMax = new Label();
            lblRightRms = new Label();
            grpRightStats = new GroupBox();
            panelStats = new Panel();
            chartHistogram = new System.Windows.Forms.DataVisualization.Charting.Chart();
            splitContainerMain = new SplitContainer();
            lblBinPow = new Label();
            lblRangePow = new Label();
            chkAC = new CheckBox();
            lblChannel = new Label();
            comboChannel = new ComboBox();
            panelTop = new Panel();
            lblWindowInfo = new Label();
            comboBinLsb = new ComboBox();
            comboRangeLsb = new ComboBox();
            btnRun = new Button();
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
            lblLeftHeader.Location = new Point(6, 19);
            lblLeftHeader.Name = "lblLeftHeader";
            lblLeftHeader.Size = new Size(36, 15);
            lblLeftHeader.TabIndex = 0;
            lblLeftHeader.Text = "Left : ";
            // 
            // lblLeftSamples
            // 
            lblLeftSamples.AutoSize = true;
            lblLeftSamples.Location = new Point(0, 40);
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
            grpLeftStats.Location = new Point(5, 5);
            grpLeftStats.Name = "grpLeftStats";
            grpLeftStats.Size = new Size(140, 180);
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
            lblRightHeader.Location = new Point(6, 15);
            lblRightHeader.Name = "lblRightHeader";
            lblRightHeader.Size = new Size(44, 15);
            lblRightHeader.TabIndex = 0;
            lblRightHeader.Text = "Right : ";
            // 
            // lblRightSamples
            // 
            lblRightSamples.AutoSize = true;
            lblRightSamples.Location = new Point(0, 35);
            lblRightSamples.Name = "lblRightSamples";
            lblRightSamples.Size = new Size(0, 15);
            lblRightSamples.TabIndex = 1;
            // 
            // lblRightDC
            // 
            lblRightDC.AutoSize = true;
            lblRightDC.Location = new Point(6, 55);
            lblRightDC.Name = "lblRightDC";
            lblRightDC.Size = new Size(0, 15);
            lblRightDC.TabIndex = 2;
            // 
            // lblRightMinMax
            // 
            lblRightMinMax.AutoSize = true;
            lblRightMinMax.Location = new Point(6, 95);
            lblRightMinMax.Name = "lblRightMinMax";
            lblRightMinMax.Size = new Size(0, 15);
            lblRightMinMax.TabIndex = 4;
            // 
            // lblRightRms
            // 
            lblRightRms.AutoSize = true;
            lblRightRms.Location = new Point(6, 75);
            lblRightRms.Name = "lblRightRms";
            lblRightRms.Size = new Size(0, 15);
            lblRightRms.TabIndex = 3;
            // 
            // grpRightStats
            // 
            grpRightStats.Controls.Add(lblRightHeader);
            grpRightStats.Controls.Add(lblRightSamples);
            grpRightStats.Controls.Add(lblRightDC);
            grpRightStats.Controls.Add(lblRightRms);
            grpRightStats.Controls.Add(lblRightMinMax);
            grpRightStats.ForeColor = Color.Black;
            grpRightStats.Location = new Point(5, 189);
            grpRightStats.Name = "grpRightStats";
            grpRightStats.Size = new Size(140, 180);
            grpRightStats.TabIndex = 1;
            grpRightStats.TabStop = false;
            // 
            // panelStats
            // 
            panelStats.Controls.Add(grpRightStats);
            panelStats.Controls.Add(grpLeftStats);
            panelStats.Dock = DockStyle.Fill;
            panelStats.Location = new Point(0, 0);
            panelStats.Name = "panelStats";
            panelStats.Padding = new Padding(8);
            panelStats.Size = new Size(217, 440);
            panelStats.TabIndex = 0;
            // 
            // chartHistogram
            // 
            chartHistogram.Dock = DockStyle.Fill;
            chartHistogram.Location = new Point(0, 0);
            chartHistogram.Name = "chartHistogram";
            chartHistogram.Size = new Size(917, 440);
            chartHistogram.TabIndex = 0;
            chartHistogram.Text = "chartHistogram";
            // 
            // splitContainerMain
            // 
            splitContainerMain.Dock = DockStyle.Fill;
            splitContainerMain.Location = new Point(0, 60);
            splitContainerMain.Name = "splitContainerMain";
            // 
            // splitContainerMain.Panel1
            // 
            splitContainerMain.Panel1.Controls.Add(chartHistogram);
            // 
            // splitContainerMain.Panel2
            // 
            splitContainerMain.Panel2.Controls.Add(panelStats);
            splitContainerMain.Size = new Size(1138, 440);
            splitContainerMain.SplitterDistance = 917;
            splitContainerMain.TabIndex = 3;
            // 
            // lblBinPow
            // 
            lblBinPow.AutoSize = true;
            lblBinPow.Location = new Point(408, 10);
            lblBinPow.Name = "lblBinPow";
            lblBinPow.Size = new Size(46, 15);
            lblBinPow.TabIndex = 14;
            lblBinPow.Text = "Bin LSB";
            // 
            // lblRangePow
            // 
            lblRangePow.AutoSize = true;
            lblRangePow.Location = new Point(200, 10);
            lblRangePow.Name = "lblRangePow";
            lblRangePow.Size = new Size(62, 15);
            lblRangePow.TabIndex = 13;
            lblRangePow.Text = "Range LSB";
            // 
            // chkAC
            // 
            chkAC.AutoSize = true;
            chkAC.Location = new Point(60, 35);
            chkAC.Name = "chkAC";
            chkAC.Size = new Size(85, 19);
            chkAC.TabIndex = 2;
            chkAC.Text = "remove DC";
            chkAC.UseVisualStyleBackColor = true;
            chkAC.CheckedChanged += chkAC_CheckedChanged;
            // 
            // lblChannel
            // 
            lblChannel.AutoSize = true;
            lblChannel.Location = new Point(3, 10);
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
            comboChannel.Location = new Point(60, 7);
            comboChannel.Name = "comboChannel";
            comboChannel.Size = new Size(100, 23);
            comboChannel.TabIndex = 0;
            comboChannel.SelectedIndexChanged += comboChannel_SelectedIndexChanged;
            // 
            // panelTop
            // 
            panelTop.Controls.Add(lblWindowInfo);
            panelTop.Controls.Add(comboBinLsb);
            panelTop.Controls.Add(comboRangeLsb);
            panelTop.Controls.Add(btnRun);
            panelTop.Controls.Add(lblBinPow);
            panelTop.Controls.Add(lblRangePow);
            panelTop.Controls.Add(chkAC);
            panelTop.Controls.Add(lblChannel);
            panelTop.Controls.Add(comboChannel);
            panelTop.Dock = DockStyle.Top;
            panelTop.Location = new Point(0, 0);
            panelTop.Name = "panelTop";
            panelTop.Size = new Size(1138, 60);
            panelTop.TabIndex = 2;
            // 
            // lblWindowInfo
            // 
            lblWindowInfo.AutoSize = true;
            lblWindowInfo.Location = new Point(990, 40);
            lblWindowInfo.Name = "lblWindowInfo";
            lblWindowInfo.Size = new Size(38, 15);
            lblWindowInfo.TabIndex = 17;
            lblWindowInfo.Text = "label1";
            // 
            // comboBinLsb
            // 
            comboBinLsb.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBinLsb.FormattingEnabled = true;
            comboBinLsb.Location = new Point(460, 7);
            comboBinLsb.Name = "comboBinLsb";
            comboBinLsb.Size = new Size(90, 23);
            comboBinLsb.TabIndex = 15;
            comboBinLsb.SelectedIndexChanged += comboBinLsb_SelectedIndexChanged;
            // 
            // comboRangeLsb
            // 
            comboRangeLsb.DropDownStyle = ComboBoxStyle.DropDownList;
            comboRangeLsb.FormattingEnabled = true;
            comboRangeLsb.Location = new Point(260, 7);
            comboRangeLsb.Name = "comboRangeLsb";
            comboRangeLsb.Size = new Size(130, 23);
            comboRangeLsb.TabIndex = 13;
            comboRangeLsb.SelectedIndexChanged += comboRangeLsb_SelectedIndexChanged;
            // 
            // btnRun
            // 
            btnRun.FlatStyle = FlatStyle.Flat;
            btnRun.Location = new Point(1044, 4);
            btnRun.Name = "btnRun";
            btnRun.Size = new Size(79, 51);
            btnRun.TabIndex = 18;
            btnRun.Text = "RUN";
            btnRun.UseVisualStyleBackColor = true;
            // 
            // HistogramControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(splitContainerMain);
            Controls.Add(panelTop);
            Name = "HistogramControl";
            Size = new Size(1138, 500);
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
        private Label lblRightMinMax;
        private Label lblRightRms;
        private GroupBox grpRightStats;
        private Panel panelStats;
        private System.Windows.Forms.DataVisualization.Charting.Chart chartHistogram;
        private SplitContainer splitContainerMain;
        private Label lblBinPow;
        private Label lblRangePow;
        private CheckBox chkAC;
        private Label lblChannel;
        private ComboBox comboChannel;
        private Panel panelTop;
        private ComboBox comboBinLsb;
        private ComboBox comboRangeLsb;
        private Label lblWindowInfo;
        private Button btnRun;
    }
}
