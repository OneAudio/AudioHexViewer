#nullable enable
using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    internal partial class OscilloscopeControl
    {
        private System.ComponentModel.IContainer? components = null;

        // ── Top bar ───────────────────────────────────────────────────────────
        private Panel         panelTop;
        private Button        btnRun;
        private Label         lblChannel;
        private ComboBox      cmbChannel;
        private Label         lblTimediv;
        private ComboBox      cmbTimediv;
        private Label         lblTrig;
        private ComboBox      cmbTrigMode;
        private ComboBox      cmbTrigEdge;
        private Label         lblTrigSrc;
        private ComboBox      cmbTrigSrc;
        private Label         lblTrigLvl;
        private NumericUpDown nudTrigLevel;
        private Label         lblVdiv;
        private ComboBox      cmbVdiv;

        // ── Zone principale ───────────────────────────────────────────────────
        private ScopePanel pnlScope;

        protected override void Dispose(bool disposing)
        { if (disposing) components?.Dispose(); base.Dispose(disposing); }

        private void InitializeComponent()
        {
            panelTop     = new Panel();
            btnRun       = new Button();
            lblChannel   = new Label();
            cmbChannel   = new ComboBox();
            lblTimediv   = new Label();
            cmbTimediv   = new ComboBox();
            lblTrig      = new Label();
            cmbTrigMode  = new ComboBox();
            cmbTrigEdge  = new ComboBox();
            lblTrigSrc   = new Label();
            cmbTrigSrc   = new ComboBox();
            lblTrigLvl   = new Label();
            nudTrigLevel = new NumericUpDown();
            lblVdiv      = new Label();
            cmbVdiv      = new ComboBox();
            pnlScope     = new ScopePanel();

            nudTrigLevel.BeginInit();
            panelTop.SuspendLayout();
            SuspendLayout();

            // ── panelTop ─────────────────────────────────────────────────────
            panelTop.BackColor = Color.Transparent;
            panelTop.Dock      = DockStyle.Top;
            panelTop.Size      = new Size(1138, 90);
            panelTop.Controls.AddRange(new Control[]
            {
                lblChannel, cmbChannel,
                lblTimediv, cmbTimediv,
                lblTrig, cmbTrigMode, cmbTrigEdge,
                lblTrigSrc, cmbTrigSrc,
                lblTrigLvl, nudTrigLevel,
                lblVdiv, cmbVdiv,
                btnRun          // en dernier = au-dessus des autres (z-order)
            });

            var grayS = new Font("Segoe UI", 7.5f);

            // ── Channel ───────────────────────────────────────────────────────
            lblChannel.AutoSize = true; lblChannel.Font = grayS;
            lblChannel.ForeColor = System.Drawing.SystemColors.GrayText;
            lblChannel.Location = new Point(8, 14); lblChannel.Text = "CHANNEL MODE";

            cmbChannel.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbChannel.Font = new Font("Segoe UI", 9f);
            cmbChannel.Location = new Point(8, 30); cmbChannel.Size = new Size(88, 24);
            cmbChannel.TabIndex = 1;
            cmbChannel.Items.AddRange(new object[] { "Left", "Right", "L+R" });
            cmbChannel.SelectedIndex = 2;

            // ── Timebase ──────────────────────────────────────────────────────
            lblTimediv.AutoSize = true; lblTimediv.Font = grayS;
            lblTimediv.ForeColor = System.Drawing.SystemColors.GrayText;
            lblTimediv.Location = new Point(108, 14); lblTimediv.Text = "TIME/DIV";

            cmbTimediv.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTimediv.Font = new Font("Segoe UI", 9f);
            cmbTimediv.Location = new Point(108, 30); cmbTimediv.Size = new Size(110, 24);
            cmbTimediv.TabIndex = 2;
            cmbTimediv.Items.AddRange(new object[]
            {
                "0,01 ms/div","0,02 ms/div","0,05 ms/div","0,1 ms/div","0,2 ms/div","0,5 ms/div",
                "1 ms/div","2 ms/div","5 ms/div","10 ms/div","20 ms/div","50 ms/div",
                "100 ms/div","200 ms/div","500 ms/div","1 s/div","2 s/div","5 s/div"
            });
            cmbTimediv.SelectedIndex = 6;  // 1 ms/div

            // ── Trigger ───────────────────────────────────────────────────────
            lblTrig.AutoSize = true; lblTrig.Font = grayS;
            lblTrig.ForeColor = System.Drawing.SystemColors.GrayText;
            lblTrig.Location = new Point(230, 14); lblTrig.Text = "TRIGGER";

            cmbTrigMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTrigMode.Font = new Font("Segoe UI", 9f);
            cmbTrigMode.Location = new Point(230, 30); cmbTrigMode.Size = new Size(72, 24);
            cmbTrigMode.TabIndex = 3;
            cmbTrigMode.Items.AddRange(new object[] { "Auto", "Normal", "Single" });
            cmbTrigMode.SelectedIndex = 0;

            cmbTrigEdge.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTrigEdge.Font = new Font("Segoe UI", 10f);
            cmbTrigEdge.Location = new Point(308, 30); cmbTrigEdge.Size = new Size(48, 24);
            cmbTrigEdge.TabIndex = 4;
            cmbTrigEdge.Items.AddRange(new object[] { "↑", "↓" });
            cmbTrigEdge.SelectedIndex = 0;

            lblTrigSrc.AutoSize = true; lblTrigSrc.Font = grayS;
            lblTrigSrc.ForeColor = System.Drawing.SystemColors.GrayText;
            lblTrigSrc.Location = new Point(364, 14); lblTrigSrc.Text = "SRC";

            cmbTrigSrc.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTrigSrc.Font = new Font("Segoe UI", 9f);
            cmbTrigSrc.Location = new Point(364, 30); cmbTrigSrc.Size = new Size(48, 24);
            cmbTrigSrc.TabIndex = 5;
            cmbTrigSrc.Items.AddRange(new object[] { "L", "R" });
            cmbTrigSrc.SelectedIndex = 0;

            lblTrigLvl.AutoSize = true; lblTrigLvl.Font = grayS;
            lblTrigLvl.ForeColor = System.Drawing.SystemColors.GrayText;
            lblTrigLvl.Location = new Point(420, 14); lblTrigLvl.Text = "LEVEL";

            nudTrigLevel.DecimalPlaces = 3; nudTrigLevel.Increment = 0.01m;
            nudTrigLevel.Minimum = -1.000m; nudTrigLevel.Maximum = 1.000m; nudTrigLevel.Value = 0.000m;
            nudTrigLevel.Font = new Font("Segoe UI", 9f);
            nudTrigLevel.Location = new Point(420, 29); nudTrigLevel.Size = new Size(84, 26);
            nudTrigLevel.TabIndex = 6;

            // ── Zoom vertical ─────────────────────────────────────────────────
            lblVdiv.AutoSize = true; lblVdiv.Font = grayS;
            lblVdiv.ForeColor = System.Drawing.SystemColors.GrayText;
            lblVdiv.Location = new Point(516, 14); lblVdiv.Text = "V ZOOM";

            cmbVdiv.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbVdiv.Font = new Font("Segoe UI", 9f);
            cmbVdiv.Location = new Point(516, 30); cmbVdiv.Size = new Size(72, 24);
            cmbVdiv.TabIndex = 7;
            cmbVdiv.Items.AddRange(new object[] { "×1", "×2", "×5", "×10", "×20" });
            cmbVdiv.SelectedIndex = 0;

            // ── RUN — ancré à droite (même que Spectrum) ─────────────────────
            btnRun.Anchor    = AnchorStyles.Top | AnchorStyles.Right;
            btnRun.Location  = new Point(1060, 10);
            btnRun.Size      = new Size(70, 40);
            btnRun.Text      = "▶  RUN";
            btnRun.BackColor = Color.SeaGreen;
            btnRun.ForeColor = Color.White;
            btnRun.FlatStyle = FlatStyle.Flat;
            btnRun.FlatAppearance.BorderSize = 0;
            btnRun.Font      = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnRun.TabIndex  = 8;
            btnRun.UseVisualStyleBackColor = false;

            // ── pnlScope ─────────────────────────────────────────────────────
            pnlScope.Dock      = DockStyle.Fill;
            pnlScope.BackColor = System.Drawing.Color.White;

            // ── UserControl ───────────────────────────────────────────────────
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode       = AutoScaleMode.Font;
            Controls.Add(pnlScope);
            Controls.Add(panelTop);

            nudTrigLevel.EndInit();
            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            ResumeLayout(false);
        }
    }
}
#nullable restore
