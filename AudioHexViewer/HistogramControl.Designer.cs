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
            chartHistogram  = new System.Windows.Forms.DataVisualization.Charting.Chart();
            panelTop        = new Panel();
            lblChannel      = new Label();
            comboChannel    = new ComboBox();
            btnDcAcHist     = new Button();
            lblCoupleHist   = new Label();
            lblRangePow     = new Label();
            comboRangeLsb   = new ComboBox();
            lblBinPow       = new Label();
            comboBinLsb     = new ComboBox();
            lblOptions      = new Label();
            btnGauss        = new Button();
            btnNorm         = new Button();
            btnNSigma       = new Button();
            comboSigma      = new ComboBox();
            btnDiff         = new Button();
            lblYScale       = new Label();
            btnLockScale    = new Button();
            btnOverlay      = new Button();
            sep1            = new Label();
            sep2            = new Label();
            sep3            = new Label();
            sep4            = new Label();
            btnRun          = new Button();
            chkDcMode       = new CheckBox();
            ChkGauss        = new CheckBox();
            ChkMarkers      = new CheckBox();
            ChkNorm         = new CheckBox();
            chkDiff         = new CheckBox();
            chkLockScale    = new CheckBox();
            sep3_unused     = new Label();
            sepAvg          = new Label();
            comboAveraging  = new ComboBox();
            btnResetAvgHist = new Button();
            lblAveragingHist= new Label();
            lblAvgCountHist = new Label();
            sep4_unused     = new Label();

            ((System.ComponentModel.ISupportInitialize)chartHistogram).BeginInit();
            panelTop.SuspendLayout();
            SuspendLayout();

            var gS   = new Font("Segoe UI", 7.5f);
            var std9 = new Font("Segoe UI", 9f);

            // ══════════════════════════════════════════════════════════════════
            //  LAYOUT PANELTOP — Histogram
            //  CHANNEL/SCALE at original positions (x=8, x=112)
            //  OPTIONS + Y SCALE placed after sep2 with gap=9px (same as Spectrum)
            //
            //  x=8    CHANNEL  combo(y=18,w=90) + COUPLE(y=46) + DC/AC(y=60,w=45)
            //  x=104  sep1
            //  x=112  SCALE    RANGE LSB(y=18,w=90) + BIN LSB(y=60,w=90)
            //  x=208  sep2
            //  x=218  OPTIONS  row1: GaussFIT(62) V BARS(62) Normalize(62) [4px gaps]
            //                  row2: DELTA(62)    comboSigma(62) [4px gap]
            //  x=421  sep3
            //  x=431  Y SCALE  AUTO/LOCK toggle(w=62)
            //  x=502  sep4
            //  x=965  Overlay (y=60, anchored Top|Right)
            //  x=1050 RUN
            // ══════════════════════════════════════════════════════════════════

            panelTop.Dock      = DockStyle.Top;
            panelTop.Size      = new Size(1138, 90);
            panelTop.BackColor = SystemColors.Control;
            { var bc = Color.White;
              panelTop.Controls.Add(new Panel { Dock = DockStyle.Top,    Height = 2, BackColor = bc });
              panelTop.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 2, BackColor = bc });
              panelTop.Controls.Add(new Panel { Dock = DockStyle.Left,   Width  = 2, BackColor = bc });
              panelTop.Controls.Add(new Panel { Dock = DockStyle.Right,  Width  = 2, BackColor = bc }); }
            panelTop.Controls.AddRange(new Control[]
            {
                lblChannel, comboChannel, lblCoupleHist, btnDcAcHist,
                lblRangePow, comboRangeLsb, lblBinPow, comboBinLsb,
                lblOptions, btnGauss, btnNSigma, btnNorm, comboSigma, btnDiff,
                lblYScale, btnLockScale,
                btnOverlay,
                sep1, sep2, sepAvg, sep3, sep4,
                lblAveragingHist, comboAveraging, btnResetAvgHist, lblAvgCountHist,
                sep3_unused, sep4_unused,
                btnRun
            });

            void Sep(Label l, int x)
            {
                l.BackColor = SystemColors.ControlDark;
                l.Location  = new Point(x, 6); l.Size = new Size(1, 76); l.Text = "";
            }
            void SecLbl(Label l, int x, int y2, string txt)
            {
                l.AutoSize = true; l.Font = gS; l.ForeColor = SystemColors.GrayText;
                l.Location = new Point(x, y2); l.Text = txt;
            }
            void TBtn(Button b, int x, int y2, int w, string txt, int tab)
            {
                b.Location = new Point(x, y2); b.Size = new Size(w, 24);
                b.Text = txt; b.TabIndex = tab; b.Font = std9;
                b.UseVisualStyleBackColor = true;
            }

            // ── 1. CHANNEL (x=8) — position d'origine ────────────────────────
            SecLbl(lblChannel, 8, 4, "CHANNEL");
            comboChannel.DropDownStyle     = ComboBoxStyle.DropDownList;
            comboChannel.Font              = std9;
            comboChannel.FormattingEnabled = true;
            comboChannel.Items.AddRange(new object[] { "Left", "Right", "Both" });
            comboChannel.Location = new Point(8, 18);
            comboChannel.Size     = new Size(90, 24);
            comboChannel.SelectedIndexChanged += this.ComboChannel_SelectedIndexChanged;
            SecLbl(lblCoupleHist, 8, 46, "COUPLE");
            TBtn(btnDcAcHist, 8, 60, 45, "DC", 8);

            Sep(sep1, 107);

            // ── 2. SCALE (x=112) — position d'origine ────────────────────────
            SecLbl(lblRangePow, 117, 4, "RANGE LSB");
            comboRangeLsb.DropDownStyle     = ComboBoxStyle.DropDownList;
            comboRangeLsb.Font              = std9;
            comboRangeLsb.FormattingEnabled = true;
            comboRangeLsb.Location = new Point(117, 18);
            comboRangeLsb.Size     = new Size(90, 24);
            comboRangeLsb.SelectedIndexChanged += this.ComboRangeLsb_SelectedIndexChanged;
            SecLbl(lblBinPow, 117, 46, "BIN LSB");
            comboBinLsb.DropDownStyle     = ComboBoxStyle.DropDownList;
            comboBinLsb.Font              = std9;
            comboBinLsb.FormattingEnabled = true;
            comboBinLsb.Location = new Point(117, 60);
            comboBinLsb.Size     = new Size(90, 24);
            comboBinLsb.SelectedIndexChanged += this.ComboBinLsb_SelectedIndexChanged;

            Sep(sep2, 216);

            // ── AVERAGING (x=218) — same style as Spectrum ───────────────────
            SecLbl(lblAveragingHist, 226, 4, "AVERAGING");
            comboAveraging.DropDownStyle     = ComboBoxStyle.DropDownList;
            comboAveraging.Font              = std9;
            comboAveraging.FormattingEnabled = true;
            comboAveraging.Items.AddRange(new object[] { "None","2","4","8","16","32","64","128" });
            comboAveraging.SelectedIndex = 0;
            comboAveraging.Location = new Point(226, 18);
            comboAveraging.Size     = new Size(66, 24);

            btnResetAvgHist.Location = new Point(226, 60);
            btnResetAvgHist.Size     = new Size(44, 24);
            btnResetAvgHist.Text     = "CLR";
            btnResetAvgHist.Font     = std9;
            btnResetAvgHist.UseVisualStyleBackColor = true;
            btnResetAvgHist.Click   += this.BtnResetAvgHist_Click;

            lblAvgCountHist.AutoSize  = false;
            lblAvgCountHist.Font      = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            lblAvgCountHist.ForeColor = Color.Firebrick;
            lblAvgCountHist.Location  = new Point(264, 60);
            lblAvgCountHist.Size      = new Size(28, 24);
            lblAvgCountHist.TextAlign = ContentAlignment.MiddleRight;
            lblAvgCountHist.Text      = "";

            Sep(sepAvg, 301);


            // ── 3. OPTIONS (x=218) — 9px gap after sep2 ──────────────────────
            // Row1: GaussFIT(62) +4gap+ V BARS(62) +4gap+ Normalize(62) = 196px
            // Row2: DELTA(62)    +4gap+ comboSigma(62)
            SecLbl(lblOptions, 381, 4, "OPTIONS");
            TBtn(btnGauss,  311, 34, 62, "GaussFIT",   20);
            TBtn(btnNSigma, 377, 34, 62, "V BARS",     21);
            TBtn(btnNorm,   443, 60, 62, "NORMAL",     22);
            TBtn(btnDiff,   311, 60, 62, "Delta FIT",  23);
            comboSigma.DropDownStyle     = ComboBoxStyle.DropDownList;
            comboSigma.Font              = std9;
            comboSigma.FormattingEnabled = true;
            comboSigma.Items.AddRange(new object[] { "±1σ", "±2σ", "±3σ" });
            comboSigma.SelectedIndex = 2;
            comboSigma.Location = new Point(377, 60);
            comboSigma.Size     = new Size(62, 24);
            comboSigma.SelectedIndexChanged += this.ComboSigma_SelectedIndexChanged;

            Sep(sep3, 514);   // 218+196+7=421 (≈9px right of Normalize end=412)

            // ── 4. Y SCALE (x=431) — 9px gap after sep3 ─────────────────────
            SecLbl(lblYScale, 540, 4, "SCALE");
            TBtn(btnLockScale, 524, 60, 62, "AUTO",    30);

            Sep(sep4, 595);   // 431+62+9=502

            // ── OVERLAY (x=965, y=60) — ancré comme Spectrum/Oscilloscope ────
            btnOverlay.Location = new Point(965, 60);
            btnOverlay.Size     = new Size(62, 24);
            btnOverlay.Text     = "Overlay";
            btnOverlay.Font     = std9;
            btnOverlay.UseVisualStyleBackColor = true;
            btnOverlay.Click   += this.BtnOverlay_Click;

            // ── Legacy checkboxes (hidden) ─────────────────────────────────────
            foreach (var cb in new CheckBox[] { chkDcMode, ChkGauss, ChkMarkers, ChkNorm, chkDiff, chkLockScale })
            { cb.Visible = false; cb.Text = ""; cb.Location = new Point(-500, -500); cb.Size = new Size(1, 1); }
            sep3_unused.Size = new Size(0, 0); sep3_unused.Location = new Point(-200, -200);
            // Reuse sep4_unused as "Y SCALE" sub-label above AUTO button
            sep4_unused.AutoSize = true; sep4_unused.Font = gS; sep4_unused.ForeColor = SystemColors.GrayText;
            sep4_unused.Location = new Point(537, 46); sep4_unused.Text = "Y SCALE";
            sep4_unused.Size = new Size(0, 0);   // AutoSize will override

            // ── RUN (anchored right) ──────────────────────────────────────────
            btnRun.Anchor    = AnchorStyles.Top | AnchorStyles.Right;
            btnRun.FlatStyle = FlatStyle.Flat;
            btnRun.FlatAppearance.BorderSize = 0;
            btnRun.Font      = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnRun.Size      = new Size(82, 76);
            btnRun.Location  = new Point(1050, 6);
            btnRun.Text      = "▶  RUN";
            btnRun.BackColor = Color.SeaGreen;
            btnRun.ForeColor = Color.White;
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
            // Legacy checkboxes: NOT added to any Controls collection.
            // State is now managed by bool fields (_dcMode, _gaussEnabled, etc.)
            // The CheckBox objects still exist for session-restore compat only.
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
        private Button   btnDcAcHist;
        private Label    lblCoupleHist;
        private Label    lblRangePow;
        private ComboBox comboRangeLsb;
        private Label    lblBinPow;
        private ComboBox comboBinLsb;
        private Label    lblOptions;
        private Button   btnGauss;
        private Button   btnNorm;
        private Button   btnNSigma;
        private ComboBox comboSigma;
        private Button   btnDiff;
        private Label    lblYScale;
        private Button   btnLockScale;
        private Button   btnOverlay;
        private Label    sep1;
        private Label    sep2;
        private Label    sep3;
        private Label    sep4;
        private CheckBox chkDcMode;
        private CheckBox ChkGauss;
        private CheckBox ChkMarkers;
        private CheckBox ChkNorm;
        private CheckBox chkDiff;
        private CheckBox chkLockScale;
        private Label    sep3_unused;
        private Label    sepAvg;           // new sep after AVERAGING
        private ComboBox comboAveraging;
        private Button   btnResetAvgHist;
        private Label    lblAveragingHist;
        private Label    lblAvgCountHist;
        private Label    sep4_unused;
        private Button   btnRun;
        private Label    lblView;   // kept for compat
    }
}
