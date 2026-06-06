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
        private Label         lblHoldoff;   private NumericUpDown nudHoldoff;
        private Label         lblTrigPos;   private NumericUpDown nudTrigPos;
        private Label         lblGrid;      private ComboBox cmbGrid;
        private Button        btnScaleL, btnScaleR;  // sélection voie pour échelle Y
        private Label         lblGainHdr;   private Label lblCoupHdr;
        private Label         lblVdivL;     private ComboBox cmbVdivL;
        private Label         lblCoupL;     private ComboBox cmbCouplingL;
        private Label         lblVdivR;     private ComboBox cmbVdivR;
        private Label         lblCoupR;     private ComboBox cmbCouplingR;
        private CheckBox      chkRoll;
        private ComboBox     cmbTraceMode;
        private Label        lblTraceMode;
        private CheckBox      chkVdiv;
        private Label         lblCursors;
        private Button        btnC1, btnC1Chan, btnC2, btnC2Chan;
        private Button        btnC1Peak, btnC1Prev, btnC1Next, btnC1Lock;
        private Button        btnC2Peak, btnC2Prev, btnC2Next, btnC2Lock;
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
            lblHoldoff   = new Label();   nudHoldoff  = new NumericUpDown();
            lblTrigPos   = new Label();   nudTrigPos  = new NumericUpDown();
            lblGrid      = new Label();   cmbGrid     = new ComboBox();
            btnScaleL    = new Button(); btnScaleR = new Button();
            lblGainHdr   = new Label();   lblCoupHdr  = new Label();
            lblVdivL     = new Label();   cmbVdivL    = new ComboBox();
            lblCoupL     = new Label();   cmbCouplingL= new ComboBox();
            lblVdivR     = new Label();   cmbVdivR    = new ComboBox();
            lblCoupR     = new Label();   cmbCouplingR= new ComboBox();
            chkRoll      = new CheckBox();
            cmbTraceMode = new ComboBox();
            lblTraceMode = new Label();
            chkVdiv      = new CheckBox();
            lblCursors   = new Label();
            btnC1        = new Button(); btnC1Chan = new Button();
            btnC2        = new Button(); btnC2Chan = new Button();
            btnC1Peak=new Button();btnC1Prev=new Button();btnC1Next=new Button();btnC1Lock=new Button();
            btnC2Peak=new Button();btnC2Prev=new Button();btnC2Next=new Button();btnC2Lock=new Button();
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
                lblTrigLvl, nudTrigLevel,
                lblHoldoff, nudHoldoff,
                lblTrigPos, nudTrigPos,
                lblGrid, cmbGrid,
                btnScaleL, btnScaleR,
                cmbTraceMode, lblTraceMode,
                lblGainHdr, lblCoupHdr,
                lblVdivL, cmbVdivL, cmbCouplingL,
                lblVdivR, cmbVdivR, cmbCouplingR,
                chkRoll, chkVdiv,
                lblCursors,
                btnC1, btnC1Chan, btnC1Prev, btnC1Peak, btnC1Next, btnC1Lock,
                btnC2, btnC2Chan, btnC2Prev, btnC2Peak, btnC2Next, btnC2Lock,
                lblTrigStatus,
                btnRun
            });

            // ── Channel ───────────────────────────────────────────────────────
            lblChannel.AutoSize=true; lblChannel.Font=gS; lblChannel.ForeColor=System.Drawing.SystemColors.GrayText;
            // ── Règle de grille verticale (90px panelTop) ─────────────────────
            //   y= 4  → labels de groupe  (gS, gris clair, tous alignés)
            //   y=18  → combos / boutons  ROW 1  (fin y=41)
            //   y=46  → sous-labels        ROW 2  (gap 4px après y=42)
            //   y=60  → combos / NUDs      ROW 2  (fin y=83, ≤ 90px)

            // ══════════════════════════════════════════════════════════════
            //  LAYOUT PANELTOP — 6 groupes + fonctions avancées
            //  x=8→96    CHANNEL
            //  x=104→314 VERTICAL (Gain+Coup+V/div)
            //  x=330→440 TIMEBASE (TIME/DIV + T POS)
            //  x=452→626 TRIGGER  (Mode+Edge+RAW / Src+Level)
            //  x=636→724 DISPLAY  (Grid + Roll)
            //  x=734→960 CURSORS  (C1/C2 + Peak < > + Lock)
            //  x=1060    RUN (ancré droite)
            // ══════════════════════════════════════════════════════════════

            // ── 1. CHANNEL ────────────────────────────────────────────────────

            // ── 2. VERTICAL Gain+Coup+V/div (x=104→314) ──────────────────────
            cmbVdivL.DropDownStyle=ComboBoxStyle.DropDownList; cmbVdivL.Font=std9;
            // ── Groupe VERTICAL déplacé à x=8 — 4 labels renommés ──────────────
            lblGainHdr.AutoSize=true; lblGainHdr.Font=gS; lblGainHdr.ForeColor=System.Drawing.SystemColors.GrayText;
            lblGainHdr.Location=new Point(8,4); lblGainHdr.Text="L GAIN";
            lblCoupHdr.AutoSize=true; lblCoupHdr.Font=gS; lblCoupHdr.ForeColor=System.Drawing.SystemColors.GrayText;
            lblCoupHdr.Location=new Point(96,4); lblCoupHdr.Text="L COUPLING";
            lblVdivL.AutoSize=true; lblVdivL.Font=gS; lblVdivL.ForeColor=System.Drawing.SystemColors.GrayText;
            lblVdivL.Location=new Point(8,46); lblVdivL.Text="R GAIN";
            lblVdivR.AutoSize=true; lblVdivR.Font=gS; lblVdivR.ForeColor=System.Drawing.SystemColors.GrayText;
            lblVdivR.Location=new Point(96,46); lblVdivR.Text="R COUPLING";
            cmbVdivL.Location=new Point(8,18); cmbVdivL.Size=new Size(82,24); cmbVdivL.TabIndex=8;
            cmbCouplingL.DropDownStyle=ComboBoxStyle.DropDownList; cmbCouplingL.Font=std9;
            cmbCouplingL.Location=new Point(96,18); cmbCouplingL.Size=new Size(66,24); cmbCouplingL.TabIndex=9;
            cmbCouplingL.Items.AddRange(new object[]{"DC","AC","OFF"}); cmbCouplingL.SelectedIndex=0;
            cmbVdivR.DropDownStyle=ComboBoxStyle.DropDownList; cmbVdivR.Font=std9;
            cmbVdivR.Location=new Point(8,60); cmbVdivR.Size=new Size(82,24); cmbVdivR.TabIndex=10;
            cmbCouplingR.DropDownStyle=ComboBoxStyle.DropDownList; cmbCouplingR.Font=std9;
            cmbCouplingR.Location=new Point(96,60); cmbCouplingR.Size=new Size(66,24); cmbCouplingR.TabIndex=11;
            cmbCouplingR.Items.AddRange(new object[]{"DC","AC","OFF"}); cmbCouplingR.SelectedIndex=0;
            chkVdiv.AutoSize=true; chkVdiv.Font=std9;
            chkVdiv.Location=new Point(200,20); chkVdiv.Text="V/div"; chkVdiv.TabIndex=13;
            chkVdiv.Enabled=false; chkVdiv.UseVisualStyleBackColor=true;
            // Boutons L/R sélection de l'échelle verticale (exclusifs, style C1/C2)
            btnScaleL.Location=new Point(168,18); btnScaleL.Margin=new Padding(0);
            btnScaleL.Size=new Size(28,24); btnScaleL.TabIndex=30;
            btnScaleL.Text="L"; btnScaleL.UseVisualStyleBackColor=false;
            btnScaleR.Location=new Point(168,60); btnScaleR.Margin=new Padding(0);
            btnScaleR.Size=new Size(28,24); btnScaleR.TabIndex=31;
            btnScaleR.Text="R"; btnScaleR.UseVisualStyleBackColor=false;

            // ── 3. TIMEBASE (x=330) ───────────────────────────────────────────
            lblTimediv.AutoSize=true; lblTimediv.Font=gS; lblTimediv.ForeColor=System.Drawing.SystemColors.GrayText;
            lblTimediv.Location=new Point(280,4); lblTimediv.Text="TIME/DIV";
            cmbTimediv.DropDownStyle=ComboBoxStyle.DropDownList; cmbTimediv.Font=std9;
            cmbTimediv.Location=new Point(280,18); cmbTimediv.Size=new Size(90,24); cmbTimediv.TabIndex=2;
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
            lblTrigPos.Location=new Point(314,46); lblTrigPos.Text="T POS (div)";
            nudTrigPos.DecimalPlaces=1; nudTrigPos.Increment=0.5m;
            nudTrigPos.Minimum=0.5m; nudTrigPos.Maximum=9.5m; nudTrigPos.Value=2.0m;
            nudTrigPos.Font=std9; nudTrigPos.Location=new Point(314,60); nudTrigPos.Size=new Size(56,24); nudTrigPos.TabIndex=18;

            // ── 4. TRIGGER (x=452) ── Mode+Edge+RAW / Src+Level ──────────────
            lblTrig.AutoSize=true; lblTrig.Font=gS; lblTrig.ForeColor=System.Drawing.SystemColors.GrayText;
            lblTrig.Location=new Point(415,4); lblTrig.Text="TRIGGER";
            cmbTrigMode.DropDownStyle=ComboBoxStyle.DropDownList; cmbTrigMode.Font=std9;
            cmbTrigMode.Location=new Point(415,18); cmbTrigMode.Size=new Size(70,24); cmbTrigMode.TabIndex=3;
            cmbTrigMode.Items.AddRange(new object[]{"Auto","Normal","Single"}); cmbTrigMode.SelectedIndex=0;
            cmbTrigEdge.DropDownStyle=ComboBoxStyle.DropDownList; cmbTrigEdge.Font=std9;
            cmbTrigEdge.Location=new Point(545,18); cmbTrigEdge.Size=new Size(40,24); cmbTrigEdge.TabIndex=4;
            cmbTrigEdge.Items.AddRange(new object[]{"\u2191","\u2193"}); cmbTrigEdge.SelectedIndex=0;
            lblTrigSrc.AutoSize=true; lblTrigSrc.Font=gS; lblTrigSrc.ForeColor=System.Drawing.SystemColors.GrayText;
            lblTrigSrc.Location=new Point(491,4); lblTrigSrc.Text="SOURCE";
            cmbTrigSrc.DropDownStyle=ComboBoxStyle.DropDownList; cmbTrigSrc.Font=std9;
            cmbTrigSrc.Location=new Point(491,18); cmbTrigSrc.Size=new Size(48,24); cmbTrigSrc.TabIndex=5;
            cmbTrigSrc.Items.AddRange(new object[]{"L","R"}); cmbTrigSrc.SelectedIndex=0;
            lblTrigLvl.AutoSize=true; lblTrigLvl.Font=gS; lblTrigLvl.ForeColor=System.Drawing.SystemColors.GrayText;
            lblTrigLvl.Location=new Point(415,46); lblTrigLvl.Text="LEVEL";
            lblTrigSlope.AutoSize=true; lblTrigSlope.Font=gS; lblTrigSlope.ForeColor=System.Drawing.SystemColors.GrayText;
            lblTrigSlope.Location=new Point(545,4); lblTrigSlope.Text="SLOPE";
            nudTrigLevel.Text="0.000";
            nudTrigLevel.Font=std9; nudTrigLevel.Location=new Point(415,60); nudTrigLevel.Size=new Size(70,24); nudTrigLevel.TabIndex=6;
            nudTrigLevel.TextAlign=HorizontalAlignment.Right;

            lblHoldoff.AutoSize=true; lblHoldoff.Font=gS; lblHoldoff.ForeColor=System.Drawing.SystemColors.GrayText;
            lblHoldoff.Location=new Point(491,46); lblHoldoff.Text="HOLD-OFF (ms)";
            nudHoldoff.DecimalPlaces=1; nudHoldoff.Increment=1.0m;
            nudHoldoff.Minimum=0m; nudHoldoff.Maximum=2000m; nudHoldoff.Value=0m;
            nudHoldoff.Font=std9; nudHoldoff.Location=new Point(491,60); nudHoldoff.Size=new Size(95,24); nudHoldoff.TabIndex=31;
            nudHoldoff.TextAlign=HorizontalAlignment.Right;

            // ── 5. DISPLAY (x=636) ── Grid + Roll ────────────────────────────
            lblGrid.AutoSize=true; lblGrid.Font=gS; lblGrid.ForeColor=System.Drawing.SystemColors.GrayText;
            lblGrid.Location=new Point(636,4); lblGrid.Text="GRID";
            cmbGrid.DropDownStyle=ComboBoxStyle.DropDownList; cmbGrid.Font=std9;
            cmbGrid.Location=new Point(636,18); cmbGrid.Size=new Size(88,24); cmbGrid.TabIndex=7;
            cmbGrid.Items.AddRange(new object[]{"Full","Axis","None"}); cmbGrid.SelectedIndex=0;
            chkRoll.AutoSize=true; chkRoll.Font=std9;
            chkRoll.Location=new Point(280,64); chkRoll.Text="Roll"; chkRoll.TabIndex=12;
            chkRoll.Enabled=false; chkRoll.UseVisualStyleBackColor=true;
            // cmbTraceMode : sous cmbGrid (remplace chkRoll)
            lblTraceMode.AutoSize=true; lblTraceMode.Font=gS; lblTraceMode.ForeColor=System.Drawing.SystemColors.GrayText;
            lblTraceMode.Location=new Point(636,46); lblTraceMode.Text="TRACE";
            cmbTraceMode.DropDownStyle=ComboBoxStyle.DropDownList; cmbTraceMode.Font=std9;
            cmbTraceMode.Location=new Point(636,60); cmbTraceMode.Size=new Size(88,24); cmbTraceMode.TabIndex=29;
            cmbTraceMode.Items.AddRange(new object[]{"Segment","Mixed","Dot"});
            cmbTraceMode.SelectedIndex=0;

            // ── CURSORS : boutons individuels positionnés (alignement pixel-perfect) ─
            // Colonnes : C/C=34 | Chan=45 | <=22 | Peak=40 | >=22 | Trk=34  (total 197px)
            // x  :       734    | 768     | 813  | 835     | 875  | 897  → fin 931
            lblCursors.AutoSize=true; lblCursors.Font=gS; lblCursors.ForeColor=System.Drawing.SystemColors.GrayText;
            lblCursors.Location=new Point(784,4); lblCursors.Text="CURSORS SETTINGS";
            // ── Ligne C1 (y=18, h=24, identique aux combos de la ligne haute) ──
            btnC1.Location=new Point(734,18); btnC1.Margin=new Padding(0); btnC1.Size=new Size(34,24); btnC1.TabIndex=14;
            btnC1.Text="C1"; btnC1.UseVisualStyleBackColor=true;
            btnC1Chan.Location=new Point(768,18); btnC1Chan.Margin=new Padding(0); btnC1Chan.Size=new Size(45,24); btnC1Chan.TabIndex=15;
            btnC1Chan.Text="Left"; btnC1Chan.UseVisualStyleBackColor=true;
            btnC1Prev.Location=new Point(813,18); btnC1Prev.Margin=new Padding(0); btnC1Prev.Size=new Size(22,24); btnC1Prev.TabIndex=21;
            btnC1Prev.Text="<"; btnC1Prev.UseVisualStyleBackColor=true;
            btnC1Peak.Location=new Point(835,18); btnC1Peak.Margin=new Padding(0); btnC1Peak.Size=new Size(40,24); btnC1Peak.TabIndex=20;
            btnC1Peak.Text="Peak"; btnC1Peak.UseVisualStyleBackColor=true;
            btnC1Next.Location=new Point(875,18); btnC1Next.Margin=new Padding(0); btnC1Next.Size=new Size(22,24); btnC1Next.TabIndex=22;
            btnC1Next.Text=">"; btnC1Next.UseVisualStyleBackColor=true;
            btnC1Lock.Location=new Point(897,18); btnC1Lock.Margin=new Padding(0); btnC1Lock.Size=new Size(34,24); btnC1Lock.TabIndex=23;
            btnC1Lock.Text="Trk"; btnC1Lock.UseVisualStyleBackColor=true;
            // ── Ligne C2 (y=60, h=24, identique aux combos de la ligne basse) ──
            btnC2.Location=new Point(734,60); btnC2.Margin=new Padding(0); btnC2.Size=new Size(34,24); btnC2.TabIndex=16;
            btnC2.Text="C2"; btnC2.UseVisualStyleBackColor=true;
            btnC2Chan.Location=new Point(768,60); btnC2Chan.Margin=new Padding(0); btnC2Chan.Size=new Size(45,24); btnC2Chan.TabIndex=17;
            btnC2Chan.Text="Right"; btnC2Chan.UseVisualStyleBackColor=true;
            btnC2Prev.Location=new Point(813,60); btnC2Prev.Margin=new Padding(0); btnC2Prev.Size=new Size(22,24); btnC2Prev.TabIndex=25;
            btnC2Prev.Text="<"; btnC2Prev.UseVisualStyleBackColor=true;
            btnC2Peak.Location=new Point(835,60); btnC2Peak.Margin=new Padding(0); btnC2Peak.Size=new Size(40,24); btnC2Peak.TabIndex=24;
            btnC2Peak.Text="Peak"; btnC2Peak.UseVisualStyleBackColor=true;
            btnC2Next.Location=new Point(875,60); btnC2Next.Margin=new Padding(0); btnC2Next.Size=new Size(22,24); btnC2Next.TabIndex=26;
            btnC2Next.Text=">"; btnC2Next.UseVisualStyleBackColor=true;
            btnC2Lock.Location=new Point(897,60); btnC2Lock.Margin=new Padding(0); btnC2Lock.Size=new Size(34,24); btnC2Lock.TabIndex=27;
            btnC2Lock.Text="Trk"; btnC2Lock.UseVisualStyleBackColor=true;

            lblTrigStatus.Location=new Point(934,60); lblTrigStatus.Size=new Size(80,18);
            lblTrigStatus.Text=""; lblTrigStatus.Font=new Font("Segoe UI",8.5f,FontStyle.Bold);
            lblTrigStatus.TextAlign=System.Drawing.ContentAlignment.MiddleCenter;
            lblTrigStatus.ForeColor=System.Drawing.Color.Gray;

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
