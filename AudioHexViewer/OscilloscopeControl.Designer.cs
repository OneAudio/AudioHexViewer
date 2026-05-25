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
                lblTrigStatus,
                btnRun
            });

            // ── Channel ───────────────────────────────────────────────────────
            lblChannel.AutoSize=true; lblChannel.Font=gS; lblChannel.ForeColor=System.Drawing.SystemColors.GrayText;
            lblChannel.Location=new Point(8,14); lblChannel.Text="CHANNEL MODE";
            cmbChannel.DropDownStyle=ComboBoxStyle.DropDownList; cmbChannel.Font=std9;
            cmbChannel.Location=new Point(8,30); cmbChannel.Size=new Size(88,24); cmbChannel.TabIndex=1;
            cmbChannel.Items.AddRange(new object[]{"Left","Right","L+R"}); cmbChannel.SelectedIndex=2;

            // ── Timebase ──────────────────────────────────────────────────────
            lblTimediv.AutoSize=true; lblTimediv.Font=gS; lblTimediv.ForeColor=System.Drawing.SystemColors.GrayText;
            lblTimediv.Location=new Point(108,14); lblTimediv.Text="TIME/DIV";
            cmbTimediv.DropDownStyle=ComboBoxStyle.DropDownList; cmbTimediv.Font=std9;
            cmbTimediv.Location=new Point(108,30); cmbTimediv.Size=new Size(110,24); cmbTimediv.TabIndex=2;
            cmbTimediv.Items.AddRange(new object[]{"0,01 ms/div","0,02 ms/div","0,05 ms/div","0,1 ms/div","0,2 ms/div","0,5 ms/div","1 ms/div","2 ms/div","5 ms/div","10 ms/div","20 ms/div","50 ms/div","100 ms/div","200 ms/div","500 ms/div","1 s/div","2 s/div","5 s/div"});
            cmbTimediv.SelectedIndex=6;

            // ── Trigger ───────────────────────────────────────────────────────
            lblTrig.AutoSize=true; lblTrig.Font=gS; lblTrig.ForeColor=System.Drawing.SystemColors.GrayText;
            lblTrig.Location=new Point(230,14); lblTrig.Text="TRIGGER";
            cmbTrigMode.DropDownStyle=ComboBoxStyle.DropDownList; cmbTrigMode.Font=std9;
            cmbTrigMode.Location=new Point(230,30); cmbTrigMode.Size=new Size(72,24); cmbTrigMode.TabIndex=3;
            cmbTrigMode.Items.AddRange(new object[]{"Auto","Normal","Single"}); cmbTrigMode.SelectedIndex=0;
            cmbTrigEdge.DropDownStyle=ComboBoxStyle.DropDownList; cmbTrigEdge.Font=new Font("Segoe UI",10f);
            cmbTrigEdge.Location=new Point(308,30); cmbTrigEdge.Size=new Size(48,24); cmbTrigEdge.TabIndex=4;
            cmbTrigEdge.Items.AddRange(new object[]{"↑","↓"}); cmbTrigEdge.SelectedIndex=0;
            lblTrigSrc.AutoSize=true; lblTrigSrc.Font=gS; lblTrigSrc.ForeColor=System.Drawing.SystemColors.GrayText;
            lblTrigSrc.Location=new Point(364,14); lblTrigSrc.Text="SRC";
            cmbTrigSrc.DropDownStyle=ComboBoxStyle.DropDownList; cmbTrigSrc.Font=std9;
            cmbTrigSrc.Location=new Point(364,30); cmbTrigSrc.Size=new Size(48,24); cmbTrigSrc.TabIndex=5;
            cmbTrigSrc.Items.AddRange(new object[]{"L","R"}); cmbTrigSrc.SelectedIndex=0;
            lblTrigLvl.AutoSize=true; lblTrigLvl.Font=gS; lblTrigLvl.ForeColor=System.Drawing.SystemColors.GrayText;
            lblTrigLvl.Location=new Point(420,14); lblTrigLvl.Text="LEVEL";
            nudTrigLevel.DecimalPlaces=3; nudTrigLevel.Increment=0.01m;
            nudTrigLevel.Minimum=-1.000m; nudTrigLevel.Maximum=1.000m; nudTrigLevel.Value=0.000m;
            nudTrigLevel.Font=std9; nudTrigLevel.Location=new Point(420,29);
            nudTrigLevel.Size=new Size(84,26); nudTrigLevel.TabIndex=6;

            // ── Grille ────────────────────────────────────────────────────────
            lblGrid.AutoSize=true; lblGrid.Font=gS; lblGrid.ForeColor=System.Drawing.SystemColors.GrayText;
            lblGrid.Location=new Point(516,14); lblGrid.Text="GRID";
            cmbGrid.DropDownStyle=ComboBoxStyle.DropDownList; cmbGrid.Font=std9;
            cmbGrid.Location=new Point(516,30); cmbGrid.Size=new Size(90,24); cmbGrid.TabIndex=7;
            cmbGrid.Items.AddRange(new object[]{"Full","Axis","None"});
            cmbGrid.SelectedIndex=0;

            // ── Gain + Couplage par voie (L au-dessus de R) ───────────────────
            // En-têtes de colonnes, alignés avec les combos
            lblGainHdr.AutoSize=true; lblGainHdr.Font=gS; lblGainHdr.ForeColor=System.Drawing.SystemColors.GrayText;
            lblGainHdr.Location=new Point(632,14); lblGainHdr.Text="GAIN";
            lblCoupHdr.AutoSize=true; lblCoupHdr.Font=gS; lblCoupHdr.ForeColor=System.Drawing.SystemColors.GrayText;
            lblCoupHdr.Location=new Point(718,14); lblCoupHdr.Text="COUP.";

            // Voie L : combos à y=30 (alignés avec la barre principale)
            // "L" centré verticalement avec le combo (combo y=30, h=24, centre=42 → label y=36)
            lblVdivL.AutoSize=true; lblVdivL.Font=std9; lblVdivL.ForeColor=System.Drawing.SystemColors.GrayText;
            lblVdivL.Location=new Point(616,36); lblVdivL.Text="L";
            cmbVdivL.DropDownStyle=ComboBoxStyle.DropDownList; cmbVdivL.Font=std9;
            cmbVdivL.Location=new Point(630,30); cmbVdivL.Size=new Size(82,24); cmbVdivL.TabIndex=8;
            cmbVdivL.Items.AddRange(new object[]{"×1","×2","×5","×10","×20","×50","×100","×200","×500","×1000"});
            cmbVdivL.SelectedIndex=0;
            lblCoupL.Text=""; lblCoupL.Visible=false;   // pas de label redondant
            cmbCouplingL.DropDownStyle=ComboBoxStyle.DropDownList; cmbCouplingL.Font=std9;
            cmbCouplingL.Location=new Point(718,30); cmbCouplingL.Size=new Size(52,24); cmbCouplingL.TabIndex=9;
            cmbCouplingL.Items.AddRange(new object[]{"DC","AC"}); cmbCouplingL.SelectedIndex=0;

            // Voie R : combos à y=62 (décalés de 32px par rapport à L)
            // "R" centré avec son combo (combo y=62, h=24, centre=74 → label y=68)
            lblVdivR.AutoSize=true; lblVdivR.Font=std9; lblVdivR.ForeColor=System.Drawing.SystemColors.GrayText;
            lblVdivR.Location=new Point(616,68); lblVdivR.Text="R";
            cmbVdivR.DropDownStyle=ComboBoxStyle.DropDownList; cmbVdivR.Font=std9;
            cmbVdivR.Location=new Point(630,62); cmbVdivR.Size=new Size(82,24); cmbVdivR.TabIndex=10;
            cmbVdivR.Items.AddRange(new object[]{"×1","×2","×5","×10","×20","×50","×100","×200","×500","×1000"});
            cmbVdivR.SelectedIndex=0;
            lblCoupR.Text=""; lblCoupR.Visible=false;
            cmbCouplingR.DropDownStyle=ComboBoxStyle.DropDownList; cmbCouplingR.Font=std9;
            cmbCouplingR.Location=new Point(718,62); cmbCouplingR.Size=new Size(52,24); cmbCouplingR.TabIndex=11;
            cmbCouplingR.Items.AddRange(new object[]{"DC","AC"}); cmbCouplingR.SelectedIndex=0;

            // ── Roll + V/div (empilés) ─────────────────────────────────────────
            chkRoll.AutoSize=true; chkRoll.Font=std9;
            chkRoll.Location=new Point(780,32); chkRoll.Text="Roll"; chkRoll.TabIndex=12;
            chkRoll.Enabled=false; chkRoll.UseVisualStyleBackColor=true;

            chkVdiv.AutoSize=true; chkVdiv.Font=std9;
            chkVdiv.Location=new Point(780,62); chkVdiv.Text="V/div"; chkVdiv.TabIndex=13;
            chkVdiv.Enabled=false; chkVdiv.UseVisualStyleBackColor=true;

            // ── Curseurs C1 / C2 ─────────────────────────────────────────────
            lblCursors.AutoSize=true; lblCursors.Font=gS; lblCursors.ForeColor=System.Drawing.SystemColors.GrayText;
            lblCursors.Location=new Point(848,14); lblCursors.Text="CURSORS";

            // Ligne C1 (alignée avec barre principale, y=30)
            btnC1.FlatStyle=FlatStyle.Flat; btnC1.FlatAppearance.BorderSize=1;
            btnC1.Font=new Font("Segoe UI",9f,FontStyle.Bold);
            btnC1.Location=new Point(848,30); btnC1.Size=new Size(30,24);
            btnC1.Text="C1"; btnC1.TabIndex=14; btnC1.UseVisualStyleBackColor=false;
            btnC1.Paint += (s,e) => { var b=(Button)s!; e.Graphics.Clear(b.BackColor);
                TextRenderer.DrawText(e.Graphics,b.Text,b.Font,b.ClientRectangle,b.ForeColor,
                System.Windows.Forms.TextFormatFlags.HorizontalCenter|System.Windows.Forms.TextFormatFlags.VerticalCenter); };

            btnC1Chan.FlatStyle=FlatStyle.Flat; btnC1Chan.FlatAppearance.BorderSize=1;
            btnC1Chan.Font=std9;
            btnC1Chan.Location=new Point(882,30); btnC1Chan.Size=new Size(72,24);
            btnC1Chan.Text="Left"; btnC1Chan.TabIndex=15; btnC1Chan.UseVisualStyleBackColor=true;

            // Ligne C2 (décalée y=62 comme voie R)
            btnC2.FlatStyle=FlatStyle.Flat; btnC2.FlatAppearance.BorderSize=1;
            btnC2.Font=new Font("Segoe UI",9f,FontStyle.Bold);
            btnC2.Location=new Point(848,62); btnC2.Size=new Size(30,24);
            btnC2.Text="C2"; btnC2.TabIndex=16; btnC2.UseVisualStyleBackColor=false;
            btnC2.Paint += (s,e) => { var b=(Button)s!; e.Graphics.Clear(b.BackColor);
                TextRenderer.DrawText(e.Graphics,b.Text,b.Font,b.ClientRectangle,b.ForeColor,
                System.Windows.Forms.TextFormatFlags.HorizontalCenter|System.Windows.Forms.TextFormatFlags.VerticalCenter); };

            btnC2Chan.FlatStyle=FlatStyle.Flat; btnC2Chan.FlatAppearance.BorderSize=1;
            btnC2Chan.Font=std9;
            btnC2Chan.Location=new Point(882,62); btnC2Chan.Size=new Size(72,24);
            btnC2Chan.Text="Right"; btnC2Chan.TabIndex=17; btnC2Chan.UseVisualStyleBackColor=true;

            // ── Position horizontale du trigger ───────────────────────────────
            // Plage : 0,5 à 9,5 divisions (5 % à 95 % de la fenêtre), défaut = 2,0 div
            lblTrigPos.AutoSize=true; lblTrigPos.Font=gS; lblTrigPos.ForeColor=System.Drawing.SystemColors.GrayText;
            lblTrigPos.Location=new Point(960,14); lblTrigPos.Text="T POS (div)";
            nudTrigPos.DecimalPlaces=1; nudTrigPos.Increment=0.5m;
            nudTrigPos.Minimum=0.5m; nudTrigPos.Maximum=9.5m; nudTrigPos.Value=2.0m;
            nudTrigPos.Font=std9; nudTrigPos.Location=new Point(960,30);
            nudTrigPos.Size=new Size(62,26); nudTrigPos.TabIndex=18;
            lblTrigStatus.Anchor  =System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Right;
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
