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
        private Label         lblTrigLvl;   private NumericUpDown nudTrigLevel;
        private Label         lblTrigPos;   private NumericUpDown nudTrigPos;
        private Label         lblGrid;      private ComboBox cmbGrid;
        private Label         lblGainHdr;   private Label lblCoupHdr;
        private Label         lblVdivL;     private ComboBox cmbVdivL;
        private Label         lblCoupL;     private ComboBox cmbCouplingL;
        private Label         lblVdivR;     private ComboBox cmbVdivR;
        private Label         lblCoupR;     private ComboBox cmbCouplingR;
        private CheckBox      chkRoll;
        private CheckBox      chkVdiv;
        private Label         lblCursors;
        private Button        btnC1, btnC1Chan, btnC2, btnC2Chan;
        private Button        btnC1Peak, btnC1Prev, btnC1Next, btnC1Lock;
        private Button        btnC2Peak, btnC2Prev, btnC2Next, btnC2Lock;
        private CheckBox      chkTrigRaw;
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
            lblTrigLvl   = new Label();   nudTrigLevel= new NumericUpDown();
            lblTrigPos   = new Label();   nudTrigPos  = new NumericUpDown();
            lblGrid      = new Label();   cmbGrid     = new ComboBox();
            lblGainHdr   = new Label();   lblCoupHdr  = new Label();
            lblVdivL     = new Label();   cmbVdivL    = new ComboBox();
            lblCoupL     = new Label();   cmbCouplingL= new ComboBox();
            lblVdivR     = new Label();   cmbVdivR    = new ComboBox();
            lblCoupR     = new Label();   cmbCouplingR= new ComboBox();
            chkRoll      = new CheckBox();
            chkVdiv      = new CheckBox();
            lblCursors   = new Label();
            btnC1        = new Button(); btnC1Chan = new Button();
            btnC2        = new Button(); btnC2Chan = new Button();
            btnC1Peak=new Button();btnC1Prev=new Button();btnC1Next=new Button();btnC1Lock=new Button();
            btnC2Peak=new Button();btnC2Prev=new Button();btnC2Next=new Button();btnC2Lock=new Button();
            chkTrigRaw   = new CheckBox();
            lblTrigStatus= new Label();
            pnlScope     = new ScopePanel();

            nudTrigLevel.BeginInit();
            nudTrigPos.BeginInit();
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
                lblChannel, cmbChannel,
                lblTimediv, cmbTimediv,
                lblTrig, cmbTrigMode, cmbTrigEdge,
                lblTrigSrc, cmbTrigSrc,
                lblTrigLvl, nudTrigLevel,
                lblTrigPos, nudTrigPos,
                lblGrid, cmbGrid,
                lblGainHdr, lblCoupHdr,
                lblVdivL, cmbVdivL, lblCoupL, cmbCouplingL,
                lblVdivR, cmbVdivR, lblCoupR, cmbCouplingR,
                chkRoll, chkVdiv,
                lblCursors, btnC1, btnC1Chan, btnC2, btnC2Chan,
                btnC1Peak, btnC1Prev, btnC1Next, btnC1Lock,
                btnC2Peak, btnC2Prev, btnC2Next, btnC2Lock,
                chkTrigRaw,
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
            lblChannel.AutoSize=true; lblChannel.Font=gS; lblChannel.ForeColor=System.Drawing.SystemColors.GrayText;
            lblChannel.Location=new Point(8,4); lblChannel.Text="CHANNEL";
            cmbChannel.DropDownStyle=ComboBoxStyle.DropDownList; cmbChannel.Font=std9;
            cmbChannel.Location=new Point(8,18); cmbChannel.Size=new Size(88,24); cmbChannel.TabIndex=1;
            cmbChannel.Items.AddRange(new object[]{"Left","Right","L+R"}); cmbChannel.SelectedIndex=2;

            // ── 2. VERTICAL Gain+Coup+V/div (x=104→314) ──────────────────────
            lblGainHdr.AutoSize=true; lblGainHdr.Font=gS; lblGainHdr.ForeColor=System.Drawing.SystemColors.GrayText;
            lblGainHdr.Location=new Point(122,4); lblGainHdr.Text="GAIN";
            lblCoupHdr.AutoSize=true; lblCoupHdr.Font=gS; lblCoupHdr.ForeColor=System.Drawing.SystemColors.GrayText;
            lblCoupHdr.Location=new Point(202,4); lblCoupHdr.Text="COUP.";
            lblVdivL.AutoSize=true; lblVdivL.Font=gS; lblVdivL.Location=new Point(107,22); lblVdivL.Text="L";
            cmbVdivL.DropDownStyle=ComboBoxStyle.DropDownList; cmbVdivL.Font=std9;
            cmbVdivL.Location=new Point(118,18); cmbVdivL.Size=new Size(78,24); cmbVdivL.TabIndex=8;
            cmbCouplingL.DropDownStyle=ComboBoxStyle.DropDownList; cmbCouplingL.Font=std9;
            cmbCouplingL.Location=new Point(200,18); cmbCouplingL.Size=new Size(52,24); cmbCouplingL.TabIndex=9;
            cmbCouplingL.Items.AddRange(new object[]{"DC","AC"}); cmbCouplingL.SelectedIndex=0;
            lblVdivR.AutoSize=true; lblVdivR.Font=gS; lblVdivR.Location=new Point(107,64); lblVdivR.Text="R";
            cmbVdivR.DropDownStyle=ComboBoxStyle.DropDownList; cmbVdivR.Font=std9;
            cmbVdivR.Location=new Point(118,60); cmbVdivR.Size=new Size(78,24); cmbVdivR.TabIndex=10;
            cmbCouplingR.DropDownStyle=ComboBoxStyle.DropDownList; cmbCouplingR.Font=std9;
            cmbCouplingR.Location=new Point(200,60); cmbCouplingR.Size=new Size(52,24); cmbCouplingR.TabIndex=11;
            cmbCouplingR.Items.AddRange(new object[]{"DC","AC"}); cmbCouplingR.SelectedIndex=0;
            chkVdiv.AutoSize=true; chkVdiv.Font=std9;
            chkVdiv.Location=new Point(8,64); chkVdiv.Text="V/div"; chkVdiv.TabIndex=13;
            chkVdiv.Enabled=false; chkVdiv.UseVisualStyleBackColor=true;

            // ── 3. TIMEBASE (x=330) ───────────────────────────────────────────
            lblTimediv.AutoSize=true; lblTimediv.Font=gS; lblTimediv.ForeColor=System.Drawing.SystemColors.GrayText;
            lblTimediv.Location=new Point(330,4); lblTimediv.Text="TIME/DIV";
            cmbTimediv.DropDownStyle=ComboBoxStyle.DropDownList; cmbTimediv.Font=std9;
            cmbTimediv.Location=new Point(330,18); cmbTimediv.Size=new Size(110,24); cmbTimediv.TabIndex=2;
            cmbTimediv.Items.AddRange(new object[]{
                "0,01 ms/div","0,02 ms/div","0,05 ms/div","0,1 ms/div","0,2 ms/div","0,5 ms/div",
                "1 ms/div","2 ms/div","5 ms/div","10 ms/div","20 ms/div","50 ms/div","100 ms/div"});
            cmbTimediv.SelectedIndex=7;
            lblTrigPos.AutoSize=true; lblTrigPos.Font=gS; lblTrigPos.ForeColor=System.Drawing.SystemColors.GrayText;
            lblTrigPos.Location=new Point(330,46); lblTrigPos.Text="T POS (div)";
            nudTrigPos.DecimalPlaces=1; nudTrigPos.Increment=0.5m;
            nudTrigPos.Minimum=0.5m; nudTrigPos.Maximum=9.5m; nudTrigPos.Value=2.0m;
            nudTrigPos.Font=std9; nudTrigPos.Location=new Point(330,60); nudTrigPos.Size=new Size(62,24); nudTrigPos.TabIndex=18;

            // ── 4. TRIGGER (x=452) ── Mode+Edge+RAW / Src+Level ──────────────
            lblTrig.AutoSize=true; lblTrig.Font=gS; lblTrig.ForeColor=System.Drawing.SystemColors.GrayText;
            lblTrig.Location=new Point(452,4); lblTrig.Text="TRIGGER";
            cmbTrigMode.DropDownStyle=ComboBoxStyle.DropDownList; cmbTrigMode.Font=std9;
            cmbTrigMode.Location=new Point(452,18); cmbTrigMode.Size=new Size(72,24); cmbTrigMode.TabIndex=3;
            cmbTrigMode.Items.AddRange(new object[]{"Auto","Normal","Single"}); cmbTrigMode.SelectedIndex=0;
            cmbTrigEdge.DropDownStyle=ComboBoxStyle.DropDownList; cmbTrigEdge.Font=std9;
            cmbTrigEdge.Location=new Point(528,18); cmbTrigEdge.Size=new Size(44,24); cmbTrigEdge.TabIndex=4;
            cmbTrigEdge.Items.AddRange(new object[]{"\u2191","\u2193"}); cmbTrigEdge.SelectedIndex=0;
            chkTrigRaw.AutoSize=true; chkTrigRaw.Font=std9;
            chkTrigRaw.Location=new Point(576,22); chkTrigRaw.Text="RAW"; chkTrigRaw.TabIndex=19;
            chkTrigRaw.UseVisualStyleBackColor=true;
            lblTrigSrc.AutoSize=true; lblTrigSrc.Font=gS; lblTrigSrc.ForeColor=System.Drawing.SystemColors.GrayText;
            lblTrigSrc.Location=new Point(452,46); lblTrigSrc.Text="SRC";
            cmbTrigSrc.DropDownStyle=ComboBoxStyle.DropDownList; cmbTrigSrc.Font=std9;
            cmbTrigSrc.Location=new Point(452,60); cmbTrigSrc.Size=new Size(44,24); cmbTrigSrc.TabIndex=5;
            cmbTrigSrc.Items.AddRange(new object[]{"L","R"}); cmbTrigSrc.SelectedIndex=0;
            lblTrigLvl.AutoSize=true; lblTrigLvl.Font=gS; lblTrigLvl.ForeColor=System.Drawing.SystemColors.GrayText;
            lblTrigLvl.Location=new Point(500,46); lblTrigLvl.Text="LEVEL";
            nudTrigLevel.DecimalPlaces=3; nudTrigLevel.Increment=0.010m;
            nudTrigLevel.Minimum=-1.000m; nudTrigLevel.Maximum=1.000m; nudTrigLevel.Value=0.000m;
            nudTrigLevel.Font=std9; nudTrigLevel.Location=new Point(500,60); nudTrigLevel.Size=new Size(82,24); nudTrigLevel.TabIndex=6;

            // ── 5. DISPLAY (x=636) ── Grid + Roll ────────────────────────────
            lblGrid.AutoSize=true; lblGrid.Font=gS; lblGrid.ForeColor=System.Drawing.SystemColors.GrayText;
            lblGrid.Location=new Point(636,4); lblGrid.Text="GRID";
            cmbGrid.DropDownStyle=ComboBoxStyle.DropDownList; cmbGrid.Font=std9;
            cmbGrid.Location=new Point(636,18); cmbGrid.Size=new Size(88,24); cmbGrid.TabIndex=7;
            cmbGrid.Items.AddRange(new object[]{"Full","Axis","None"}); cmbGrid.SelectedIndex=0;
            chkRoll.AutoSize=true; chkRoll.Font=std9;
            chkRoll.Location=new Point(636,64); chkRoll.Text="Roll"; chkRoll.TabIndex=12;
            chkRoll.Enabled=false; chkRoll.UseVisualStyleBackColor=true;

            // ── 6. CURSEURS (x=734) ── C1/C2 + Peak < > Lock ─────────────────
            lblCursors.AutoSize=true; lblCursors.Font=gS; lblCursors.ForeColor=System.Drawing.SystemColors.GrayText;
            lblCursors.Location=new Point(734,4); lblCursors.Text="CURSORS";
            // Ligne C1 (y=18)
            btnC1.Location=new Point(734,18); btnC1.Margin=new Padding(0); btnC1.Size=new Size(34,23); btnC1.TabIndex=14;
            btnC1.Text="C1"; btnC1.UseVisualStyleBackColor=true;
            btnC1Chan.Location=new Point(772,18); btnC1Chan.Margin=new Padding(0); btnC1Chan.Size=new Size(45,23); btnC1Chan.TabIndex=15;
            btnC1Chan.Text="Left"; btnC1Chan.UseVisualStyleBackColor=true;
            btnC1Peak.FlatStyle=FlatStyle.Flat; btnC1Peak.Location=new Point(822,18); btnC1Peak.Size=new Size(42,23); btnC1Peak.TabIndex=20;
            btnC1Peak.Text="Peak"; btnC1Peak.UseVisualStyleBackColor=true;
            btnC1Prev.FlatStyle=FlatStyle.Flat; btnC1Prev.Location=new Point(868,18); btnC1Prev.Size=new Size(24,23); btnC1Prev.TabIndex=21;
            btnC1Prev.Text="<"; btnC1Prev.UseVisualStyleBackColor=true;
            btnC1Next.FlatStyle=FlatStyle.Flat; btnC1Next.Location=new Point(896,18); btnC1Next.Size=new Size(24,23); btnC1Next.TabIndex=22;
            btnC1Next.Text=">"; btnC1Next.UseVisualStyleBackColor=true;
            btnC1Lock.Location=new Point(924,18); btnC1Lock.Size=new Size(36,23); btnC1Lock.TabIndex=23;
            btnC1Lock.Text="\u269B"; btnC1Lock.UseVisualStyleBackColor=true;
            // Ligne C2 (y=60)
            btnC2.Location=new Point(734,60); btnC2.Margin=new Padding(0); btnC2.Size=new Size(34,23); btnC2.TabIndex=16;
            btnC2.Text="C2"; btnC2.UseVisualStyleBackColor=true;
            btnC2Chan.Location=new Point(772,60); btnC2Chan.Margin=new Padding(0); btnC2Chan.Size=new Size(45,23); btnC2Chan.TabIndex=17;
            btnC2Chan.Text="Right"; btnC2Chan.UseVisualStyleBackColor=true;
            btnC2Peak.FlatStyle=FlatStyle.Flat; btnC2Peak.Location=new Point(822,60); btnC2Peak.Size=new Size(42,23); btnC2Peak.TabIndex=24;
            btnC2Peak.Text="Peak"; btnC2Peak.UseVisualStyleBackColor=true;
            btnC2Prev.FlatStyle=FlatStyle.Flat; btnC2Prev.Location=new Point(868,60); btnC2Prev.Size=new Size(24,23); btnC2Prev.TabIndex=25;
            btnC2Prev.Text="<"; btnC2Prev.UseVisualStyleBackColor=true;
            btnC2Next.FlatStyle=FlatStyle.Flat; btnC2Next.Location=new Point(896,60); btnC2Next.Size=new Size(24,23); btnC2Next.TabIndex=26;
            btnC2Next.Text=">"; btnC2Next.UseVisualStyleBackColor=true;
            btnC2Lock.Location=new Point(924,60); btnC2Lock.Size=new Size(36,23); btnC2Lock.TabIndex=27;
            btnC2Lock.Text="\u269B"; btnC2Lock.UseVisualStyleBackColor=true;

                        lblTrigStatus.Location=new Point(1060,54); lblTrigStatus.Size=new Size(70,18);
            lblTrigStatus.Text=""; lblTrigStatus.Font=new Font("Segoe UI",8.5f,FontStyle.Bold);
            lblTrigStatus.TextAlign=System.Drawing.ContentAlignment.MiddleCenter;
            lblTrigStatus.ForeColor=System.Drawing.Color.Gray;

            // ── RUN ───────────────────────────────────────────────────────────
            btnRun.Anchor  =System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Right;
            btnRun.Location=new Point(1060,10); btnRun.Size=new Size(70,40);
            btnRun.Text="▶  RUN"; btnRun.BackColor=Color.SeaGreen; btnRun.ForeColor=Color.White;
            btnRun.TextAlign=System.Drawing.ContentAlignment.MiddleCenter;
            btnRun.FlatStyle=FlatStyle.Flat; btnRun.FlatAppearance.BorderSize=0;
            btnRun.Font=new Font("Segoe UI",10f,FontStyle.Bold); btnRun.TabIndex=14;
            btnRun.UseVisualStyleBackColor=false;

            // ── pnlScope ─────────────────────────────────────────────────────
            pnlScope.Dock=DockStyle.Fill; pnlScope.BackColor=System.Drawing.Color.White;

            AutoScaleDimensions=new SizeF(7F,15F); AutoScaleMode=AutoScaleMode.Font;
            Controls.Add(pnlScope); Controls.Add(panelTop);

            nudTrigLevel.EndInit();
            nudTrigPos.EndInit();
            panelTop.ResumeLayout(false); panelTop.PerformLayout();
            ResumeLayout(false);
        }
    }
}
#nullable restore
