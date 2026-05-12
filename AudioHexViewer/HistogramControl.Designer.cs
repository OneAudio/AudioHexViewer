namespace AudioHexViewer
{
    partial class HistogramControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur de composants

        private void InitializeComponent()
        {
            chartHistogram = new System.Windows.Forms.DataVisualization.Charting.Chart();
            panelTop = new Panel();
            lblChannel = new Label();
            comboChannel = new ComboBox();
            lblRangePow = new Label();
            comboRangeLsb = new ComboBox();
            lblBinPow = new Label();
            comboBinLsb = new ComboBox();
            ChkAC = new CheckBox();
            btnRun = new Button();

            ((System.ComponentModel.ISupportInitialize)chartHistogram).BeginInit();
            panelTop.SuspendLayout();
            SuspendLayout();

            // ── panelTop (même hauteur que SpectrumControl) ──────────────────
            panelTop.Dock = DockStyle.Top;
            panelTop.Size = new Size(1138, 70);
            panelTop.BackColor = SystemColors.Control;
            panelTop.Controls.AddRange(new Control[]
            {
                lblChannel, comboChannel,
                lblRangePow, comboRangeLsb,
                lblBinPow, comboBinLsb,
                btnRun
            });

            // lblChannel
            lblChannel.AutoSize = true;
            lblChannel.Text = "Channel";
            lblChannel.Location = new Point(6, 28);

            // comboChannel
            comboChannel.DropDownStyle = ComboBoxStyle.DropDownList;
            comboChannel.FormattingEnabled = true;
            comboChannel.Items.AddRange(new object[] { "Left", "Right", "Both" });
            comboChannel.Location = new Point(60, 24);
            comboChannel.Size = new Size(80, 23);
            comboChannel.SelectedIndexChanged += ComboChannel_SelectedIndexChanged;

            // lblRangePow
            lblRangePow.AutoSize = true;
            lblRangePow.Text = "Range LSB";
            lblRangePow.Location = new Point(152, 28);

            // comboRangeLsb
            comboRangeLsb.DropDownStyle = ComboBoxStyle.DropDownList;
            comboRangeLsb.FormattingEnabled = true;
            comboRangeLsb.Location = new Point(228, 24);
            comboRangeLsb.Size = new Size(120, 23);
            comboRangeLsb.SelectedIndexChanged += ComboRangeLsb_SelectedIndexChanged;

            // lblBinPow
            lblBinPow.AutoSize = true;
            lblBinPow.Text = "Bin LSB";
            lblBinPow.Location = new Point(360, 28);

            // comboBinLsb
            comboBinLsb.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBinLsb.FormattingEnabled = true;
            comboBinLsb.Location = new Point(416, 24);
            comboBinLsb.Size = new Size(85, 23);
            comboBinLsb.SelectedIndexChanged += ComboBinLsb_SelectedIndexChanged;

            // ChkAC
            ChkAC.AutoSize = true;
            ChkAC.Text = "Remove DC";
            ChkAC.Location = new Point(515, 26);
            ChkAC.CheckedChanged += ChkAC_CheckedChanged;


            // btnRun — mêmes dimensions que SpectrumControl.btnRun
            btnRun.FlatStyle = FlatStyle.Flat;
            btnRun.Size = new Size(60, 46);
            btnRun.Location = new Point(1075, 12);
            btnRun.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRun.Text = "RUN";

            // ── chartHistogram ───────────────────────────────────────────────
            chartHistogram.Dock = DockStyle.Fill;
            chartHistogram.Location = new Point(0, 70);
            chartHistogram.Name = "chartHistogram";
            chartHistogram.Text = "chartHistogram";

            // ── HistogramControl ─────────────────────────────────────────────
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(chartHistogram);
            Controls.Add(panelTop);
            Name = "HistogramControl";
            Size = new Size(1138, 550);

            ((System.ComponentModel.ISupportInitialize)chartHistogram).EndInit();
            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.DataVisualization.Charting.Chart chartHistogram;
        private Panel panelTop;
        private Label lblChannel;
        private ComboBox comboChannel;
        private Label lblRangePow;
        private ComboBox comboRangeLsb;
        private Label lblBinPow;
        private ComboBox comboBinLsb;
        private CheckBox ChkAC;
        private Button btnRun;
    }
}