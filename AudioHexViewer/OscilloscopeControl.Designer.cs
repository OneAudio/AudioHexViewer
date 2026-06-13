#nullable enable
using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    internal partial class OscilloscopeControl
    {
        private System.ComponentModel.IContainer? components = null;

        private Panel         panelTop;
        private Button        btnRun;
        private Label         lblChannel;   private ComboBox cmbChannel;
        private Label         lblTimediv;   private ComboBox cmbTimediv;
        private Label         lblTrig;
        private ComboBox      cmbTrigMode;  private ComboBox cmbTrigEdge;
        private Label         lblTrigSrc;   private ComboBox cmbTrigSrc;
        private Label         lblTrigSlope;
        private Label         lblTrigLvl;   private TextBox nudTrigLevel;
        private Label         lblTrigAuto;  private Button  btnTrigAuto50;
        private Label         lblHoldoff;   private NumericUpDown nudHoldoff;
        private Label         lblTrigPos;   private NumericUpDown nudTrigPos;
        private Label         lblGrid;      private ComboBox cmbGrid;
        private Button        btnScaleL, btnScaleR;
        private Label         lblGainHdr;   private Label lblCoupHdr;
        private Label         lblVdivL;     private ComboBox cmbVdivL;
        private Label         lblCoupL;     private ComboBox cmbCouplingL;
        private Label         lblVdivR;     private ComboBox cmbVdivR;
        private Label         lblCoupR;     private ComboBox cmbCouplingR;
        private Button        btnRoll;
        private Label         lblRoll;
        private ComboBox      cmbTraceMode;
        private Label         lblTraceMode;
        private Button        btnVdiv;
        private Label         lblVdiv;
        private Button        btnOverlay;
        // ── Curseurs X/Y ────────────────────────────────────────────────────
        private Label         lblCursors;
        private Label         lblLinking;
        private Label         lblColModes, lblColEdit, lblColGlue, lblColDisp;
        private Button        btnCurX,   btnCurY;
        private Button        btnLinkedX,btnLinkedY;
        private Button        btnSetX,   btnSetY;
        private Button        btnLblX,   btnLblY;
        private Button        btnDisp;
        private Button        btnGlueX;
        private Label         lblTrigStatus;
        private ScopePanel    pnlScope;

        protected override void Dispose(bool disposing)
        { if (disposing) components?.Dispose(); base.Dispose(disposing); }

        private void InitializeComponent()
        {
            panelTop     = new Panel();
            btnRun       = new Button();
            lblChannel   = new Label();   cmbChannel  = new ComboBox();
            lblTimediv   = new Label();   cmbTimediv  = new ComboBox();
            lblTrig      = new Label();
            cmbTrigMode  = new ComboBox(); cmbTrigEdge = new ComboBox();
            lblTrigSrc   = new Label();   cmbTrigSrc  = new ComboBox();
            lblTrigSlope = new Label();
            lblTrigLvl   = new Label();   nudTrigLevel= new TextBox();
            lblTrigAuto  = new Label();   btnTrigAuto50 = new Button();
            lblHoldoff   = new Label();   nudHoldoff  = new NumericUpDown();
            lblTrigPos   = new Label();   nudTrigPos  = new NumericUpDown();
            lblGrid      = new Label();   cmbGrid     = new ComboBox();
            btnScaleL    = new Button(); btnScaleR = new Button();
            lblGainHdr   = new Label();   lblCoupHdr  = new Label();
            lblVdivL     = new Label();   cmbVdivL    = new ComboBox();
            lblCoupL     = new Label();   cmbCouplingL= new ComboBox();
            lblVdivR     = new Label();   cmbVdivR    = new ComboBox();
            lblCoupR     = new Label();   cmbCouplingR= new ComboBox();
            btnRoll      = new Button();   lblRoll     = new Label();
            cmbTraceMode = new ComboBox();
            lblTraceMode = new Label();
            btnVdiv      = new Button();   lblVdiv     = new Label();
            btnOverlay   = new Button();
            lblCursors   = new Label();
            lblLinking   = new Label();
            lblColModes  = new Label();   lblColEdit = new Label();
            lblColGlue   = new Label();   lblColDisp = new Label();
            btnCurX      = new Button();   btnCurY    = new Button();
            btnLinkedX   = new Button();   btnLinkedY = new Button();
            btnSetX      = new Button();   btnSetY    = new Button();
            btnLblX      = new Button();   btnLblY    = new Button();
            btnDisp      = new Button();
            btnGlueX     = new Button();
            lblTrigStatus= new Label();
            pnlScope     = new ScopePanel();

            nudTrigPos.BeginInit();
            nudHoldoff.BeginInit();
            panelTop.SuspendLayout();
            SuspendLayout();

            var gS   = new Font("Segoe UI", 7.5f);
            var std9 = new Font("Segoe UI", 9f);

            // ── panelTop ─────────────────────────────────────────────────────
            panelTop.BackColor = System.Drawing.SystemColors.Control;
            panelTop.Dock = DockStyle.Top;
            panelTop.Size = new Size(1138, 90);
            panelTop.Controls.AddRange(new System.Windows.Forms.Control[]
            {
                lblTimediv, cmbTimediv,
                lblTrig, cmbTrigMode, cmbTrigEdge,
                lblTrigSrc, cmbTrigSrc, lblTrigSlope,
                lblTrigLvl, nudTrigLevel, lblTrigAuto, btnTrigAuto50,
                lblHoldoff, nudHoldoff,
                lblTrigPos, nudTrigPos,
                lblGrid, cmbGrid,
                btnScaleL, btnScaleR,
                cmbTraceMode, lblTraceMode,
                lblGainHdr, lblCoupHdr,
                lblVdivL, cmbVdivL, cmbCouplingL,
                lblVdivR, cmbVdivR, cmbCouplingR,
                btnRoll, lblRoll, btnVdiv, lblVdiv,
                btnOverlay,
                lblCursors, lblLinking, lblColModes, lblColEdit, lblColGlue, lblColDisp, btnGlueX,
                btnCurX, btnLinkedX, btnSetX, btnLblX, btnDisp,
                btnCurY, btnLinkedY, btnSetY, btnLblY,
                lblTrigStatus,
                btnRun
            });

            // ══════════════════════════════════════════════════════════════
            //  LAYOUT PANELTOP
            //  x=8→96    CHANNEL (L GAIN / R GAIN)
            //  x=96→258  COUPLING (L/R + V/div)
            //  x=258→392 TIMEBASE
            //  x=392→608 TRIGGER
            //  x=608→730 DISPLAY (GRID + TRACE)
            //  x=730→965 CURSORS X/Y
            //  x=1050    RUN
            // ══════════════════════════════════════════════════════════════

            // ── 1. L GAIN / R GAIN ────────────────────────────────────────
            lblGainHdr.AutoSize=true; lblGainHdr.Font=gS; lblGainHdr.ForeColor=System.Drawing.SystemColors.GrayText;
            lblGainHdr.Location=new Point(8,4); lblGainHdr.Text="L GAIN";
            cmbVdivL.DropDownStyle=ComboBoxStyle.DropDownList; cmbVdivL.Font=std9;
            cmbVdivL.Location=new Point(8,18); cmbVdivL.Size=new Size(80,24); cmbVdivL.TabIndex=0;
            lblVdivR.AutoSize=true; lblVdivR.Font=gS; lblVdivR.ForeColor=System.Drawing.SystemColors.GrayText;
            lblVdivR.Location=new Point(8,46); lblVdivR.Text="R GAIN";
            cmbVdivR.DropDownStyle=ComboBoxStyle.DropDownList; cmbVdivR.Font=std9;
            cmbVdivR.Location=new Point(8,60); cmbVdivR.Size=new Size(80,24); cmbVdivR.TabIndex=1;

            // ── 2. L COUPLING / R COUPLING + L/R + V/div ─────────────────
            lblCoupHdr.AutoSize=true; lblCoupHdr.Font=gS; lblCoupHdr.ForeColor=System.Drawing.SystemColors.GrayText;
            lblCoupHdr.Location=new Point(96,4); lblCoupHdr.Text="L COUPLING";
            cmbCouplingL.DropDownStyle=ComboBoxStyle.DropDownList; cmbCouplingL.Font=std9;
            cmbCouplingL.Location=new Point(96,18); cmbCouplingL.Size=new Size(66,24); cmbCouplingL.TabIndex=9;
            cmbCouplingL.Items.AddRange(new object[]{"DC","AC","OFF"}); cmbCouplingL.SelectedIndex=0;
            lblVdivL.AutoSize=true; lblVdivL.Font=gS; lblVdivL.ForeColor=System.Drawing.SystemColors.GrayText;
            lblVdivL.Location=new Point(96,46); lblVdivL.Text="R COUPLING";
            cmbCouplingR.DropDownStyle=ComboBoxStyle.DropDownList; cmbCouplingR.Font=std9;
            cmbCouplingR.Location=new Point(96,60); cmbCouplingR.Size=new Size(66,24); cmbCouplingR.TabIndex=11;
            cmbCouplingR.Items.AddRange(new object[]{"DC","AC","OFF"}); cmbCouplingR.SelectedIndex=0;
            // V/div button + label (replaces chkVdiv)
            lblVdiv.AutoSize=true; lblVdiv.Font=gS; lblVdiv.ForeColor=SystemColors.GrayText;
            lblVdiv.Location=new Point(200,46); lblVdiv.Text="V/div";
            btnVdiv.Location=new Point(200,60); btnVdiv.Margin=new Padding(0);
            btnVdiv.Size=new Size(30,24); btnVdiv.TabIndex=13;
            btnVdiv.Font=new Font("Segoe UI",8.5f); btnVdiv.UseVisualStyleBackColor=true;
            btnScaleL.Location=new Point(168,18); btnScaleL.Margin=new Padding(0);
            btnScaleL.Size=new Size(28,24); btnScaleL.TabIndex=30;
            btnScaleL.Text="L"; btnScaleL.UseVisualStyleBackColor=false;
            btnScaleR.Location=new Point(168,60); btnScaleR.Margin=new Padding(0);
            btnScaleR.Size=new Size(28,24); btnScaleR.TabIndex=31;
            btnScaleR.Text="R"; btnScaleR.UseVisualStyleBackColor=false;

            // ── 3. TIMEBASE (x=280) ───────────────────────────────────────
            lblTimediv.AutoSize=true; lblTimediv.Font=gS; lblTimediv.ForeColor=System.Drawing.SystemColors.GrayText;
            lblTimediv.Location=new Point(259,4); lblTimediv.Text="TIME/DIV";
            cmbTimediv.DropDownStyle=ComboBoxStyle.DropDownList; cmbTimediv.Font=std9;
            cmbTimediv.Location=new Point(259,18); cmbTimediv.Size=new Size(90,24); cmbTimediv.TabIndex=2;
            cmbTimediv.Items.AddRange(new object[]{
                "1 µs/div","2 µs/div","5 µs/div",
                "10 µs/div","20 µs/div","50 µs/div",
                "100 µs/div","200 µs/div","500 µs/div",
                "1 ms/div","2 ms/div","5 ms/div",
                "10 ms/div","20 ms/div","50 ms/div",
                "100 ms/div","200 ms/div","500 ms/div",
                "1 s/div"});
            cmbTimediv.SelectedIndex=10; // 2 ms/div
            lblTrigPos.AutoSize=true; lblTrigPos.Font=gS; lblTrigPos.ForeColor=System.Drawing.SystemColors.GrayText;
            lblTrigPos.Location=new Point(293,46); lblTrigPos.Text="T POS (%)";
            nudTrigPos.DecimalPlaces=0; nudTrigPos.Increment=1m;
            nudTrigPos.Minimum=0m; nudTrigPos.Maximum=100m; nudTrigPos.Value=20m;
            nudTrigPos.Font=std9; nudTrigPos.Location=new Point(293,60); nudTrigPos.Size=new Size(56,24); nudTrigPos.TabIndex=18;
            // Roll button + label (replaces chkRoll)
            lblRoll.AutoSize=true; lblRoll.Font=gS; lblRoll.ForeColor=SystemColors.GrayText;
            lblRoll.Location=new Point(259,46); lblRoll.Text="ROLL";
            btnRoll.Location=new Point(259,60); btnRoll.Margin=new Padding(0);
            btnRoll.Size=new Size(30,24); btnRoll.TabIndex=12; btnRoll.Enabled=false;
            btnRoll.Text=""; btnRoll.Font=new Font("Segoe UI",8.5f); btnRoll.UseVisualStyleBackColor=true;

            // ── 4. TRIGGER ── x=415: TRIGGER|SOURCE|SLOPE  /  LEVEL|HOLDOFF ─
            lblTrig.AutoSize=true; lblTrig.Font=gS; lblTrig.ForeColor=System.Drawing.SystemColors.GrayText;
            lblTrig.Location=new Point(378,4); lblTrig.Text="TRIGGER";
            cmbTrigMode.DropDownStyle=ComboBoxStyle.DropDownList; cmbTrigMode.Font=std9;
            cmbTrigMode.Location=new Point(378,18); cmbTrigMode.Size=new Size(70,24); cmbTrigMode.TabIndex=3;
            cmbTrigMode.Items.AddRange(new object[]{"Auto","Normal","Single"}); cmbTrigMode.SelectedIndex=0;
            cmbTrigEdge.DropDownStyle=ComboBoxStyle.DropDownList; cmbTrigEdge.Font=std9;
            cmbTrigEdge.Location=new Point(508,18); cmbTrigEdge.Size=new Size(40,24); cmbTrigEdge.TabIndex=4;
            cmbTrigEdge.Items.AddRange(new object[]{"\u2191","\u2193"}); cmbTrigEdge.SelectedIndex=0;
            lblTrigSrc.AutoSize=true; lblTrigSrc.Font=gS; lblTrigSrc.ForeColor=System.Drawing.SystemColors.GrayText;
            lblTrigSrc.Location=new Point(454,4); lblTrigSrc.Text="SOURCE";
            cmbTrigSrc.DropDownStyle=ComboBoxStyle.DropDownList; cmbTrigSrc.Font=std9;
            cmbTrigSrc.Location=new Point(454,18); cmbTrigSrc.Size=new Size(48,24); cmbTrigSrc.TabIndex=5;
            cmbTrigSrc.Items.AddRange(new object[]{"L","R"}); cmbTrigSrc.SelectedIndex=0;
            lblTrigLvl.AutoSize=true; lblTrigLvl.Font=gS; lblTrigLvl.ForeColor=System.Drawing.SystemColors.GrayText;
            lblTrigLvl.Location=new Point(378,46); lblTrigLvl.Text="LEVEL";
            lblTrigSlope.AutoSize=true; lblTrigSlope.Font=gS; lblTrigSlope.ForeColor=System.Drawing.SystemColors.GrayText;
            lblTrigSlope.Location=new Point(508,4); lblTrigSlope.Text="SLOPE";
            nudTrigLevel.Text="0.000";
            nudTrigLevel.Font=std9; nudTrigLevel.Location=new Point(378,60); nudTrigLevel.Size=new Size(50,24); nudTrigLevel.TabIndex=6;
            nudTrigLevel.TextAlign=HorizontalAlignment.Right;
            // Auto 50% trigger level button (style identique ROLL/V-div)
            lblTrigAuto.AutoSize=true; lblTrigAuto.Font=gS; lblTrigAuto.ForeColor=SystemColors.GrayText;
            lblTrigAuto.Location=new Point(432,46); lblTrigAuto.Text="50%";
            btnTrigAuto50.Location=new Point(432,60); btnTrigAuto50.Margin=new Padding(0);
            btnTrigAuto50.Size=new Size(28,24); btnTrigAuto50.TabIndex=32;
            btnTrigAuto50.Text=""; btnTrigAuto50.Font=new Font("Segoe UI",8.5f); btnTrigAuto50.UseVisualStyleBackColor=true;
            lblHoldoff.AutoSize=true; lblHoldoff.Font=gS; lblHoldoff.ForeColor=System.Drawing.SystemColors.GrayText;
            lblHoldoff.Location=new Point(468,46); lblHoldoff.Text="HOLD-OFF (ms)";
            nudHoldoff.DecimalPlaces=1; nudHoldoff.Increment=1.0m;
            nudHoldoff.Minimum=0m; nudHoldoff.Maximum=2000m; nudHoldoff.Value=0m;
            nudHoldoff.Font=std9; nudHoldoff.Location=new Point(468,60); nudHoldoff.Size=new Size(81,24); nudHoldoff.TabIndex=31;
            nudHoldoff.TextAlign=HorizontalAlignment.Right;

            // ── 5. DISPLAY (x=636) ── Grid + Trace ───────────────────────
            lblGrid.AutoSize=true; lblGrid.Font=gS; lblGrid.ForeColor=System.Drawing.SystemColors.GrayText;
            lblGrid.Location=new Point(578,4); lblGrid.Text="GRID";
            cmbGrid.DropDownStyle=ComboBoxStyle.DropDownList; cmbGrid.Font=std9;
            cmbGrid.Location=new Point(578,18); cmbGrid.Size=new Size(88,24); cmbGrid.TabIndex=7;
            cmbGrid.Items.AddRange(new object[]{"Full","Axis","None"}); cmbGrid.SelectedIndex=0;
            lblTraceMode.AutoSize=true; lblTraceMode.Font=gS; lblTraceMode.ForeColor=System.Drawing.SystemColors.GrayText;
            lblTraceMode.Location=new Point(578,46); lblTraceMode.Text="TRACE";
            cmbTraceMode.DropDownStyle=ComboBoxStyle.DropDownList; cmbTraceMode.Font=std9;
            cmbTraceMode.Location=new Point(578,60); cmbTraceMode.Size=new Size(88,24); cmbTraceMode.TabIndex=29;
            cmbTraceMode.Items.AddRange(new object[]{"Segment","Mixed","Dot"});
            cmbTraceMode.SelectedIndex=0;

            // ── 6. CURSORS X/Y ── 5 colonnes × 2 lignes ─────────────────
            // Colonnes : Chan=34 | Type=34 | Linked=52 | Set=46 | Lbl=46
            // x :        734    | 772     | 810       | 866    | 916    → fin 962
            // Cursor zone — Option B layout (channel from Scale L/R buttons)
            // Titre général du groupe (au-dessus des sous-titres)
            lblCursors.AutoSize=true; lblCursors.Font=gS; lblCursors.ForeColor=SystemColors.GrayText;
            lblCursors.Location=new Point(772,3); lblCursors.Text="CURSORS SETTINGS";

            // Grille 5 colonnes x 2 lignes, boutons 45x24 (×1.5 vs V/div), gap=2px
            //   CURSOR | LINK | SET | GLUE  [sep]  DISP
            //     X       1-2   Set   GlueX          P/G  (row1, y=34)
            //     Y       1-2   Set   GlueY          LBL  (row2, y=60)
            void Btn(Button b, int x, int y, int w, string txt, int tab)
            { b.Location=new Point(x,y); b.Margin=new Padding(0); b.Size=new Size(w,24);
              b.Text=txt; b.TabIndex=tab; b.UseVisualStyleBackColor=true;
              b.Font=new Font("Segoe UI",8.5f); }

            // Sous-titres de colonnes (y=19, descendus et recentrés sur les colonnes 45px)
            lblColModes.AutoSize=true; lblColModes.Font=gS; lblColModes.ForeColor=SystemColors.GrayText;
            lblColModes.Location=new Point(700,19); lblColModes.Text="CURSOR"; // col1: 695-740
            lblLinking.AutoSize=true; lblLinking.Font=gS; lblLinking.ForeColor=SystemColors.GrayText;
            lblLinking.Location=new Point(747,19); lblLinking.Text="LINKED";  // col2: 742-787
            lblColEdit.AutoSize=true; lblColEdit.Font=gS; lblColEdit.ForeColor=SystemColors.GrayText;
            lblColEdit.Location=new Point(793,19); lblColEdit.Text="SET VAL"; // col3: 789-834
            lblColGlue.AutoSize=true; lblColGlue.Font=gS; lblColGlue.ForeColor=SystemColors.GrayText;
            lblColGlue.Location=new Point(847,19); lblColGlue.Text="GLUE";    // col4: 836-881
            lblColDisp.AutoSize=true; lblColDisp.Font=gS; lblColDisp.ForeColor=SystemColors.GrayText;
            lblColDisp.Location=new Point(896,19); lblColDisp.Text="DISPLAY"; // col5: 893-938

            // Row 1 (y=34) — X
            Btn(btnCurX,    695, 34, 45, "X",   41);
            Btn(btnLinkedX, 742, 34, 45, "1-2", 42);
            Btn(btnSetX,    789, 34, 45, "X= ?", 43);
            Btn(btnGlueX,   836, 34, 45, "",    51);
            Btn(btnDisp,    893, 34, 45, "Panel", 50);
            // Row 2 (y=60) — Y
            Btn(btnCurY,    695, 60, 45, "Y",   46);
            Btn(btnLinkedY, 742, 60, 45, "1-2", 47);
            Btn(btnSetY,    789, 60, 45, "Y= ?", 48);
            Btn(btnLblY,    836, 60, 45, "",    49);   // GLUE-Y (ex "Y track")
            Btn(btnLblX,    893, 60, 45, "Label", 44); // DISP-LABEL (ex "Lbls")

            lblTrigStatus.Visible=false; lblTrigStatus.Location=new Point(966,4); lblTrigStatus.Size=new Size(82,18);
            lblTrigStatus.Text=""; lblTrigStatus.Font=new Font("Segoe UI",8.5f,FontStyle.Bold);
            lblTrigStatus.TextAlign=System.Drawing.ContentAlignment.MiddleCenter;
            lblTrigStatus.ForeColor=System.Drawing.Color.Gray;

            // ── Overlay (zone libre entre CURSORS SETTINGS et RUN) ────────────
            // Identique au bouton "Overlay" du Spectrum (Size 62x24), aligné sur la
            // 2e ligne de contrôles (y=60), entre x=952 (séparateur) et x=1050 (RUN).
            btnOverlay.Location=new Point(965,60);
            btnOverlay.Size=new Size(62,24); btnOverlay.TabIndex=33;
            btnOverlay.Text="Overlay"; btnOverlay.UseVisualStyleBackColor=true;
            btnOverlay.Click += btnOverlay_Click;

            // ── RUN ───────────────────────────────────────────────────────────
            btnRun.Anchor  =System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Right;
            btnRun.Location=new Point(1050,6); btnRun.Size=new Size(82,76);
            btnRun.Text="▶  RUN"; btnRun.BackColor=Color.SeaGreen; btnRun.ForeColor=Color.White;
            btnRun.FlatStyle=FlatStyle.Flat; btnRun.FlatAppearance.BorderSize=0;
            btnRun.Font=new Font("Segoe UI",10f,FontStyle.Bold); btnRun.TabIndex=14;
            btnRun.UseVisualStyleBackColor=false;

            // ── pnlScope ─────────────────────────────────────────────────────
            pnlScope.Dock=DockStyle.Fill; pnlScope.BackColor=System.Drawing.Color.White;

            AutoScaleDimensions=new SizeF(7F,15F); AutoScaleMode=AutoScaleMode.Font;
            Controls.Add(pnlScope); Controls.Add(panelTop);

            nudTrigPos.EndInit();
            nudHoldoff.EndInit();
            panelTop.ResumeLayout(false); panelTop.PerformLayout();
            ResumeLayout(false);
        }
    }
}
#nullable restore
