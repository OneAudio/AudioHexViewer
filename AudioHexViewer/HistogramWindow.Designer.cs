namespace AudioHexViewer
{
    partial class HistogramWindow
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            panelTop = new Panel();
            btnSaveSettings = new Button();
            lblBinPow = new Label();
            lblRangePow = new Label();
            numBinPow = new NumericUpDown();
            numRangePow = new NumericUpDown();
            chkAC = new CheckBox();
            lblChannel = new Label();
            comboChannel = new ComboBox();
            splitContainerMain = new SplitContainer();
            chartHistogram = new System.Windows.Forms.DataVisualization.Charting.Chart();
            panelStats = new Panel();
            lblRightMinMax = new Label();
            lblRightRms = new Label();
            lblRightDC = new Label();
            lblRightSamples = new Label();
            lblRightHeader = new Label();
            lblLeftMinMax = new Label();
            lblLeftRms = new Label();
            lblLeftDC = new Label();
            lblLeftSamples = new Label();
            lblLeftHeader = new Label();
            panelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numBinPow).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numRangePow).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).BeginInit();
            splitContainerMain.Panel1.SuspendLayout();
            splitContainerMain.Panel2.SuspendLayout();
            splitContainerMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)chartHistogram).BeginInit();
            panelStats.SuspendLayout();
            SuspendLayout();
            // 
            // panelTop
            // 
            panelTop.Controls.Add(btnSaveSettings);
            panelTop.Controls.Add(lblBinPow);
            panelTop.Controls.Add(lblRangePow);
            panelTop.Controls.Add(numBinPow);
            panelTop.Controls.Add(numRangePow);
            panelTop.Controls.Add(chkAC);
            panelTop.Controls.Add(lblChannel);
            panelTop.Controls.Add(comboChannel);
            panelTop.Dock = DockStyle.Top;
            panelTop.Location = new Point(0, 0);
            panelTop.Name = "panelTop";
            panelTop.Size = new Size(984, 80);
            panelTop.TabIndex = 0;
            // 
            // btnSaveSettings
            // 
            btnSaveSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSaveSettings.Location = new Point(885, 12);
            btnSaveSettings.Name = "btnSaveSettings";
            btnSaveSettings.Size = new Size(87, 23);
            btnSaveSettings.TabIndex = 9;
            btnSaveSettings.Text = "Save settings";
            btnSaveSettings.UseVisualStyleBackColor = true;
            btnSaveSettings.Click += btnSaveSettings_Click;
            // 
            // lblBinPow
            // 
            lblBinPow.AutoSize = true;
            lblBinPow.Location = new Point(295, 39);
            lblBinPow.Name = "lblBinPow";
            lblBinPow.Size = new Size(81, 15);
            lblBinPow.TabIndex = 6;
            lblBinPow.Text = "LSB/bin width";
            // 
            // lblRangePow
            // 
            lblRangePow.AutoSize = true;
            lblRangePow.Location = new Point(190, 39);
            lblRangePow.Name = "lblRangePow";
            lblRangePow.Size = new Size(64, 15);
            lblRangePow.TabIndex = 5;
            lblRangePow.Text = "±LSB Scale";
            // 
            // numBinPow
            // 
            numBinPow.Location = new Point(295, 9);
            numBinPow.Maximum = new decimal(new int[] { 1024, 0, 0, 0 });
            numBinPow.Name = "numBinPow";
            numBinPow.Size = new Size(81, 23);
            numBinPow.TabIndex = 4;
            numBinPow.ValueChanged += numBinPow_ValueChanged;
            // 
            // numRangePow
            // 
            numRangePow.Location = new Point(190, 9);
            numRangePow.Maximum = new decimal(new int[] { 8388608, 0, 0, 0 });
            numRangePow.Name = "numRangePow";
            numRangePow.Size = new Size(81, 23);
            numRangePow.TabIndex = 3;
            numRangePow.ValueChanged += numRangePow_ValueChanged;
            // 
            // chkAC
            // 
            chkAC.AutoSize = true;
            chkAC.Location = new Point(85, 39);
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
            lblChannel.Location = new Point(12, 12);
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
            comboChannel.Location = new Point(70, 9);
            comboChannel.Name = "comboChannel";
            comboChannel.Size = new Size(100, 23);
            comboChannel.TabIndex = 0;
            comboChannel.SelectedIndexChanged += comboChannel_SelectedIndexChanged;
            // 
            // splitContainerMain
            // 
            splitContainerMain.Dock = DockStyle.Fill;
            splitContainerMain.Location = new Point(0, 80);
            splitContainerMain.Name = "splitContainerMain";
            // 
            // splitContainerMain.Panel1
            // 
            splitContainerMain.Panel1.Controls.Add(chartHistogram);
            // 
            // splitContainerMain.Panel2
            // 
            splitContainerMain.Panel2.Controls.Add(panelStats);
            splitContainerMain.Size = new Size(984, 381);
            splitContainerMain.SplitterDistance = 799;
            splitContainerMain.TabIndex = 1;
            // 
            // chartHistogram
            // 
            chartHistogram.Dock = DockStyle.Fill;
            chartHistogram.Location = new Point(0, 0);
            chartHistogram.Name = "chartHistogram";
            chartHistogram.Size = new Size(799, 381);
            chartHistogram.TabIndex = 0;
            chartHistogram.Text = "chartHistogram";
            // 
            // panelStats
            // 
            panelStats.BackColor = Color.WhiteSmoke;
            panelStats.Controls.Add(lblRightMinMax);
            panelStats.Controls.Add(lblRightRms);
            panelStats.Controls.Add(lblRightDC);
            panelStats.Controls.Add(lblRightSamples);
            panelStats.Controls.Add(lblRightHeader);
            panelStats.Controls.Add(lblLeftMinMax);
            panelStats.Controls.Add(lblLeftRms);
            panelStats.Controls.Add(lblLeftDC);
            panelStats.Controls.Add(lblLeftSamples);
            panelStats.Controls.Add(lblLeftHeader);
            panelStats.Dock = DockStyle.Fill;
            panelStats.Location = new Point(0, 0);
            panelStats.Name = "panelStats";
            panelStats.Padding = new Padding(8);
            panelStats.Size = new Size(181, 381);
            panelStats.TabIndex = 0;
            // 
            // lblRightMinMax
            // 
            lblRightMinMax.AutoSize = true;
            lblRightMinMax.Location = new Point(11, 212);
            lblRightMinMax.Name = "lblRightMinMax";
            lblRightMinMax.Size = new Size(67, 15);
            lblRightMinMax.TabIndex = 9;
            lblRightMinMax.Text = "Min/Max: -";
            // 
            // lblRightRms
            // 
            lblRightRms.AutoSize = true;
            lblRightRms.Location = new Point(11, 192);
            lblRightRms.Name = "lblRightRms";
            lblRightRms.Size = new Size(42, 15);
            lblRightRms.TabIndex = 8;
            lblRightRms.Text = "RMS: -";
            // 
            // lblRightDC
            // 
            lblRightDC.AutoSize = true;
            lblRightDC.Location = new Point(11, 172);
            lblRightDC.Name = "lblRightDC";
            lblRightDC.Size = new Size(67, 15);
            lblRightDC.TabIndex = 7;
            lblRightDC.Text = "DC offset: -";
            // 
            // lblRightSamples
            // 
            lblRightSamples.AutoSize = true;
            lblRightSamples.Location = new Point(11, 152);
            lblRightSamples.Name = "lblRightSamples";
            lblRightSamples.Size = new Size(62, 15);
            lblRightSamples.TabIndex = 6;
            lblRightSamples.Text = "Samples: -";
            // 
            // lblRightHeader
            // 
            lblRightHeader.AutoSize = true;
            lblRightHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblRightHeader.Location = new Point(11, 130);
            lblRightHeader.Name = "lblRightHeader";
            lblRightHeader.Size = new Size(44, 15);
            lblRightHeader.TabIndex = 5;
            lblRightHeader.Text = "RIGHT";
            // 
            // lblLeftMinMax
            // 
            lblLeftMinMax.AutoSize = true;
            lblLeftMinMax.Location = new Point(11, 90);
            lblLeftMinMax.Name = "lblLeftMinMax";
            lblLeftMinMax.Size = new Size(67, 15);
            lblLeftMinMax.TabIndex = 4;
            lblLeftMinMax.Text = "Min/Max: -";
            // 
            // lblLeftRms
            // 
            lblLeftRms.AutoSize = true;
            lblLeftRms.Location = new Point(11, 70);
            lblLeftRms.Name = "lblLeftRms";
            lblLeftRms.Size = new Size(42, 15);
            lblLeftRms.TabIndex = 3;
            lblLeftRms.Text = "RMS: -";
            // 
            // lblLeftDC
            // 
            lblLeftDC.AutoSize = true;
            lblLeftDC.Location = new Point(11, 50);
            lblLeftDC.Name = "lblLeftDC";
            lblLeftDC.Size = new Size(67, 15);
            lblLeftDC.TabIndex = 2;
            lblLeftDC.Text = "DC offset: -";
            // 
            // lblLeftSamples
            // 
            lblLeftSamples.AutoSize = true;
            lblLeftSamples.Location = new Point(11, 30);
            lblLeftSamples.Name = "lblLeftSamples";
            lblLeftSamples.Size = new Size(62, 15);
            lblLeftSamples.TabIndex = 1;
            lblLeftSamples.Text = "Samples: -";
            // 
            // lblLeftHeader
            // 
            lblLeftHeader.AutoSize = true;
            lblLeftHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblLeftHeader.Location = new Point(11, 8);
            lblLeftHeader.Name = "lblLeftHeader";
            lblLeftHeader.Size = new Size(32, 15);
            lblLeftHeader.TabIndex = 0;
            lblLeftHeader.Text = "LEFT";
            // 
            // HistogramWindow
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(984, 461);
            Controls.Add(splitContainerMain);
            Controls.Add(panelTop);
            Name = "HistogramWindow";
            Text = "Histogram";
            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numBinPow).EndInit();
            ((System.ComponentModel.ISupportInitialize)numRangePow).EndInit();
            splitContainerMain.Panel1.ResumeLayout(false);
            splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).EndInit();
            splitContainerMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)chartHistogram).EndInit();
            panelStats.ResumeLayout(false);
            panelStats.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Label lblBinPow;
        private System.Windows.Forms.Label lblRangePow;
        private System.Windows.Forms.NumericUpDown numBinPow;
        private System.Windows.Forms.NumericUpDown numRangePow;
        private System.Windows.Forms.CheckBox chkAC;
        private System.Windows.Forms.Label lblChannel;
        private System.Windows.Forms.ComboBox comboChannel;
        private System.Windows.Forms.SplitContainer splitContainerMain;
        private System.Windows.Forms.DataVisualization.Charting.Chart chartHistogram;
        private System.Windows.Forms.Label lblRightMinMax;
        private System.Windows.Forms.Label lblRightRms;
        private System.Windows.Forms.Label lblRightDC;
        private System.Windows.Forms.Label lblRightSamples;
        private System.Windows.Forms.Label lblRightHeader;
        private System.Windows.Forms.Label lblLeftMinMax;
        private System.Windows.Forms.Label lblLeftRms;
        private System.Windows.Forms.Label lblLeftDC;
        private System.Windows.Forms.Label lblLeftSamples;
        private System.Windows.Forms.Label lblLeftHeader;
        public Panel panelStats;
        private Button btnSaveSettings;
    }
}
