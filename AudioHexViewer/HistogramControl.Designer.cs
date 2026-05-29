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
            panelTop       = new Panel();
            lblChannel     = new Label();
            comboChannel   = new ComboBox();
            lblRangePow    = new Label();
            comboRangeLsb  = new ComboBox();
            lblBinPow      = new Label();
            comboBinLsb    = new ComboBox();
            ChkAC          = new CheckBox();
            ChkGauss       = new CheckBox();
            ChkMarkers     = new CheckBox();
            comboSigma     = new ComboBox();
            ChkNorm        = new CheckBox();
            sep1           = new Label();
            sep2           = new Label();
            sep3           = new Label();
            sep4           = new Label();
            btnRun         = new Button();

            ((System.ComponentModel.ISupportInitialize)chartHistogram).BeginInit();
            panelTop.SuspendLayout();
            SuspendLayout();

            // ── Fonts (same as OscilloscopeControl / SpectrumControl) ────────
            var gS   = new Font("Segoe UI", 7.5f);
            var std9 = new Font("Segoe UI", 9f);

            // ══════════════════════════════════════════════════════════════════
            //  LAYOUT PANELTOP — same grid as OscilloscopeControl
            //  y=4   → group labels  (gS, GrayText)
            //  y=18  → controls ROW 1  (h=24)
            //  y=46  → group labels  ROW 2   (used only where needed)
            //  y=60  → controls ROW 2  (h=24)
            //
            //  Histogram has a single row of controls, so:
            //    labels at y=4, combos/checks at y=18  (row 1 only)
            //    second row used for ChkAC / ChkGauss / ChkMarkers / ChkNorm
            //    which are check buttons stacked under their own label
            //
            //  x=8→100     CHANNEL
            //  x=112→260   RANGE LSB
            //  x=272→386   BIN LSB
            //  x=398→510   MODE AC
            //  x=522→780   GAUSSIAN + MARKERS
            //  x=792→870   NORMALISED
            //  x=1060      RUN (anchored right)
            // ══════════════════════════════════════════════════════════════════

            // ── panelTop ─────────────────────────────────────────────────────
            panelTop.Dock      = DockStyle.Top;
            panelTop.Size      = new Size(1138, 90);
            panelTop.BackColor = SystemColors.Control;
            panelTop.Controls.AddRange(new Control[]
            {
                lblChannel,   comboChannel,
                lblRangePow,  comboRangeLsb,
                lblBinPow,    comboBinLsb,
                ChkAC,
                ChkGauss,
                ChkMarkers,   comboSigma,
                ChkNorm,
                sep1, sep2, sep3, sep4,
                btnRun
            });

            // ── 1. CHANNEL  (x=8) ────────────────────────────────────────────
            lblChannel.AutoSize  = true;
            lblChannel.Font      = gS;
            lblChannel.ForeColor = SystemColors.GrayText;
            lblChannel.Location  = new Point(8, 4);
            lblChannel.Text      = "CHANNEL";

            comboChannel.DropDownStyle     = ComboBoxStyle.DropDownList;
            comboChannel.Font              = std9;
            comboChannel.FormattingEnabled = true;
            comboChannel.Items.AddRange(new object[] { "Left", "Right", "Both" });
            comboChannel.Location = new Point(8, 18);
            comboChannel.Size     = new Size(90, 24);
            comboChannel.SelectedIndexChanged += ComboChannel_SelectedIndexChanged;

            // ── SEP 1 (x=104) ────────────────────────────────────────────────
            sep1.BackColor = SystemColors.ControlDark;
            sep1.Location  = new Point(104, 6);
            sep1.Name      = "sep1";
            sep1.Size      = new Size(1, 76);

            // ── 2. RANGE LSB  (x=112) ────────────────────────────────────────
            lblRangePow.AutoSize  = true;
            lblRangePow.Font      = gS;
            lblRangePow.ForeColor = SystemColors.GrayText;
            lblRangePow.Location  = new Point(112, 4);
            lblRangePow.Text      = "RANGE LSB";

            comboRangeLsb.DropDownStyle     = ComboBoxStyle.DropDownList;
            comboRangeLsb.Font              = std9;
            comboRangeLsb.FormattingEnabled = true;
            comboRangeLsb.Location = new Point(112, 18);
            comboRangeLsb.Size     = new Size(140, 24);
            comboRangeLsb.SelectedIndexChanged += ComboRangeLsb_SelectedIndexChanged;

            // ── SEP 2 (x=258) ────────────────────────────────────────────────
            sep2.BackColor = SystemColors.ControlDark;
            sep2.Location  = new Point(258, 6);
            sep2.Name      = "sep2";
            sep2.Size      = new Size(1, 76);

            // ── 3. BIN LSB  (x=266) ──────────────────────────────────────────
            lblBinPow.AutoSize  = true;
            lblBinPow.Font      = gS;
            lblBinPow.ForeColor = SystemColors.GrayText;
            lblBinPow.Location  = new Point(266, 4);
            lblBinPow.Text      = "BIN LSB";

            comboBinLsb.DropDownStyle     = ComboBoxStyle.DropDownList;
            comboBinLsb.Font              = std9;
            comboBinLsb.FormattingEnabled = true;
            comboBinLsb.Location = new Point(266, 18);
            comboBinLsb.Size     = new Size(100, 24);
            comboBinLsb.SelectedIndexChanged += ComboBinLsb_SelectedIndexChanged;

            // ── SEP 3 (x=372) ────────────────────────────────────────────────
            sep3.BackColor = SystemColors.ControlDark;
            sep3.Location  = new Point(372, 6);
            sep3.Name      = "sep3";
            sep3.Size      = new Size(1, 76);

            // ── 4. MODE AC  (x=380) ──────────────────────────────────────────
            // Label row 1, checkbox row 2 — matches 2-line pattern
            ChkAC.AutoSize  = true;
            ChkAC.Font      = std9;
            ChkAC.Text      = "MODE AC";
            ChkAC.Location  = new Point(380, 18);
            ChkAC.CheckedChanged += ChkAC_CheckedChanged;

            // ── SEP 4 (x=488) ────────────────────────────────────────────────
            sep4.BackColor = SystemColors.ControlDark;
            sep4.Location  = new Point(488, 6);
            sep4.Name      = "sep4";
            sep4.Size      = new Size(1, 76);

            // ── 5. DISPLAY OPTIONS  (x=496) ──────────────────────────────────
            // Row 1 : Gaussienne   Row 2 : Marqueurs + sigma combo
            ChkGauss.AutoSize  = true;
            ChkGauss.Font      = std9;
            ChkGauss.Text      = "GAUSSIENNE";
            ChkGauss.Location  = new Point(496, 18);
            ChkGauss.CheckedChanged += ChkGauss_CheckedChanged;

            ChkMarkers.AutoSize  = true;
            ChkMarkers.Font      = std9;
            ChkMarkers.Text      = "MARQUEURS";
            ChkMarkers.Location  = new Point(496, 60);
            ChkMarkers.CheckedChanged += ChkMarkers_CheckedChanged;

            comboSigma.DropDownStyle     = ComboBoxStyle.DropDownList;
            comboSigma.Font              = std9;
            comboSigma.FormattingEnabled = true;
            comboSigma.Items.AddRange(new object[] { "±1σ", "±2σ", "±3σ" });
            comboSigma.SelectedIndex = 2;
            comboSigma.Location = new Point(610, 60);
            comboSigma.Size     = new Size(66, 24);
            comboSigma.SelectedIndexChanged += ComboSigma_SelectedIndexChanged;

            // ── 6. NORMALISÉ  (x=686) ────────────────────────────────────────
            ChkNorm.AutoSize  = true;
            ChkNorm.Font      = std9;
            ChkNorm.Text      = "NORMALISÉ";
            ChkNorm.Location  = new Point(686, 18);
            ChkNorm.CheckedChanged += ChkNorm_CheckedChanged;

            // ── RUN (anchored right) ──────────────────────────────────────────
            btnRun.Anchor    = AnchorStyles.Top | AnchorStyles.Right;
            btnRun.FlatStyle = FlatStyle.Flat;
            btnRun.FlatAppearance.BorderSize = 0;
            btnRun.Font      = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnRun.Size      = new Size(70, 40);
            btnRun.Location  = new Point(1060, 10);
            btnRun.Text      = "▶  RUN";
            btnRun.BackColor = Color.SeaGreen;
            btnRun.ForeColor = Color.White;
            btnRun.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            btnRun.UseVisualStyleBackColor = false;
            btnRun.Name      = "btnRun";

            // ── chartHistogram ────────────────────────────────────────────────
            chartHistogram.Dock     = DockStyle.Fill;
            chartHistogram.Location = new Point(0, 90);
            chartHistogram.Name     = "chartHistogram";
            chartHistogram.Text     = "chartHistogram";

            // ── HistogramControl ──────────────────────────────────────────────
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode       = AutoScaleMode.Font;
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
        private Panel    panelTop;
        private Label    lblChannel;
        private ComboBox comboChannel;
        private Label    lblRangePow;
        private ComboBox comboRangeLsb;
        private Label    lblBinPow;
        private ComboBox comboBinLsb;
        private CheckBox ChkAC;
        private CheckBox ChkGauss;
        private CheckBox ChkMarkers;
        private ComboBox comboSigma;
        private CheckBox ChkNorm;
        private Button   btnRun;
        private Label    sep1;
        private Label    sep2;
        private Label    sep3;
        private Label    sep4;
    }
}
