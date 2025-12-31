using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    partial class SpectrumColorsDialog
    {
        private System.ComponentModel.IContainer components = null;

        private TableLayoutPanel tlpRoot;
        private GroupBox grpTraces;
        private GroupBox grpOverlays;
        private GroupBox grpChart;

        private TableLayoutPanel tlpTraces;
        private TableLayoutPanel tlpOverlays;
        private TableLayoutPanel tlpChart;
        private TableLayoutPanel tlpButtons;

        private Label lblFftL;
        private Button btnFftL;
        private Label lblFftR;
        private Button btnFftR;
        private NumericUpDown nudFftWidth;

        private Label lblPkL;
        private Button btnPkL;
        private Label lblPkR;
        private Button btnPkR;
        private NumericUpDown nudPkWidth;

        private Label lblOv1;
        private Button btnOv1;
        private Label lblOv2;
        private Button btnOv2;
        private Label lblOv3;
        private Button btnOv3;
        private Label lblOv4;
        private Button btnOv4;
        private Label lblOv5;
        private Button btnOv5;
        private Label lblOv6;
        private Button btnOv6;
        private Label lblOv7;
        private Button btnOv7;
        private Label lblOv8;
        private Button btnOv8;
        private Label lblOvWidth;
        private NumericUpDown nudOvWidth;

        private Label lblGridMaj;
        private Button btnGridMaj;
        private Label lblGridMin;
        private Button btnGridMin;
        private Label lblBack;
        private Button btnBack;
        private Label lblLabMaj;
        private Button btnLabMaj;
        private Label lblLabMin;
        private Button btnLabMin;

        private Button btnDefault;
        private Button btnCancel;
        private Button btnOk;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            this.tlpRoot = new TableLayoutPanel();
            this.grpTraces = new GroupBox();
            this.grpOverlays = new GroupBox();
            this.grpChart = new GroupBox();

            this.tlpTraces = new TableLayoutPanel();
            this.tlpOverlays = new TableLayoutPanel();
            this.tlpChart = new TableLayoutPanel();
            this.tlpButtons = new TableLayoutPanel();

            // Form
            this.AutoScaleMode = AutoScaleMode.Font;
            this.AutoScaleMode = AutoScaleMode.Font;
            // IMPORTANT : on laisse AutoSize gérer la taille
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Colors";
            this.Padding = new Padding(10);
            // optionnel : une largeur mini pour éviter les retours à la ligne bizarres
            this.MinimumSize = new Size(300, 0);


            // Root layout
            this.tlpRoot.ColumnCount = 1;
            this.tlpRoot.RowCount = 4;
            this.tlpRoot.Dock = DockStyle.Fill;
            this.tlpRoot.AutoSize = true;
            this.tlpRoot.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.tlpRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.tlpRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.tlpRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.tlpRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // GroupBoxes
            this.grpTraces.Text = "Traces";
            this.grpOverlays.Text = "Overlays";
            this.grpChart.Text = "Chart";
            this.grpTraces.Dock = DockStyle.Top;
            this.grpOverlays.Dock = DockStyle.Top;
            this.grpChart.Dock = DockStyle.Top;

            this.grpTraces.AutoSize = true;
            this.grpTraces.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.grpOverlays.AutoSize = true;
            this.grpOverlays.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.grpChart.AutoSize = true;
            this.grpChart.AutoSizeMode = AutoSizeMode.GrowAndShrink;


            // --- Traces table: 5 cols (L label, L swatch, R label, R swatch, width) ---
            this.tlpTraces.ColumnCount = 5;
            this.tlpTraces.RowCount = 2;
            this.tlpTraces.Dock = DockStyle.Fill;
            this.tlpTraces.AutoSize = true;
            this.tlpTraces.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.tlpTraces.Padding = new Padding(8, 10, 8, 8);
            this.tlpTraces.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
            this.tlpTraces.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 26F));
            this.tlpTraces.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
            this.tlpTraces.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 26F));
            this.tlpTraces.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48F));
            this.tlpTraces.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.tlpTraces.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            this.lblFftL = new Label();
            this.lblFftL.Text = "FFT Left";
            this.lblFftL.Dock = DockStyle.Fill;
            this.lblFftL.TextAlign = ContentAlignment.MiddleLeft;

            this.btnFftL = new Button();
            SetupColorButton(this.btnFftL);

            this.lblFftR = new Label();
            this.lblFftR.Text = "FFT Right";
            this.lblFftR.Dock = DockStyle.Fill;
            this.lblFftR.TextAlign = ContentAlignment.MiddleLeft;

            this.btnFftR = new Button();
            SetupColorButton(this.btnFftR);

            this.nudFftWidth = new NumericUpDown();
            SetupWidthNud(this.nudFftWidth);

            this.lblPkL = new Label();
            this.lblPkL.Text = "PkHold Left";
            this.lblPkL.Dock = DockStyle.Fill;
            this.lblPkL.TextAlign = ContentAlignment.MiddleLeft;

            this.btnPkL = new Button();
            SetupColorButton(this.btnPkL);

            this.lblPkR = new Label();
            this.lblPkR.Text = "PkHold Right";
            this.lblPkR.Dock = DockStyle.Fill;
            this.lblPkR.TextAlign = ContentAlignment.MiddleLeft;

            this.btnPkR = new Button();
            SetupColorButton(this.btnPkR);

            this.nudPkWidth = new NumericUpDown();
            SetupWidthNud(this.nudPkWidth);

            this.tlpTraces.Controls.Add(this.lblFftL, 0, 0);
            this.tlpTraces.Controls.Add(this.btnFftL, 1, 0);
            this.tlpTraces.Controls.Add(this.lblFftR, 2, 0);
            this.tlpTraces.Controls.Add(this.btnFftR, 3, 0);
            this.tlpTraces.Controls.Add(this.nudFftWidth, 4, 0);

            this.tlpTraces.Controls.Add(this.lblPkL, 0, 1);
            this.tlpTraces.Controls.Add(this.btnPkL, 1, 1);
            this.tlpTraces.Controls.Add(this.lblPkR, 2, 1);
            this.tlpTraces.Controls.Add(this.btnPkR, 3, 1);
            this.tlpTraces.Controls.Add(this.nudPkWidth, 4, 1);

            this.grpTraces.Controls.Add(this.tlpTraces);

            // --- Overlays table: 4 cols (labelL, swatchL, labelR, swatchR) + width row ---
            this.tlpOverlays.ColumnCount = 4;
            this.tlpOverlays.RowCount = 5;
            this.tlpOverlays.Dock = DockStyle.Fill;
            this.tlpOverlays.AutoSize = true;
            this.tlpOverlays.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.tlpOverlays.Padding = new Padding(8, 10, 8, 8);
            this.tlpOverlays.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.tlpOverlays.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 26F));
            this.tlpOverlays.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.tlpOverlays.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 26F));
            this.tlpOverlays.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            this.tlpOverlays.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            this.tlpOverlays.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            this.tlpOverlays.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            this.tlpOverlays.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));

            this.lblOv1 = MakeLabel("Overlay 1");
            this.btnOv1 = MakeColorButton();
            this.lblOv5 = MakeLabel("Overlay 5");
            this.btnOv5 = MakeColorButton();

            this.lblOv2 = MakeLabel("Overlay 2");
            this.btnOv2 = MakeColorButton();
            this.lblOv6 = MakeLabel("Overlay 6");
            this.btnOv6 = MakeColorButton();

            this.lblOv3 = MakeLabel("Overlay 3");
            this.btnOv3 = MakeColorButton();
            this.lblOv7 = MakeLabel("Overlay 7");
            this.btnOv7 = MakeColorButton();

            this.lblOv4 = MakeLabel("Overlay 4");
            this.btnOv4 = MakeColorButton();
            this.lblOv8 = MakeLabel("Overlay 8");
            this.btnOv8 = MakeColorButton();

            this.lblOvWidth = new Label();
            this.lblOvWidth.Text = "Overlay thickness";
            this.lblOvWidth.Dock = DockStyle.Fill;
            this.lblOvWidth.TextAlign = ContentAlignment.MiddleLeft;

            this.nudOvWidth = new NumericUpDown();
            SetupWidthNud(this.nudOvWidth);

            this.tlpOverlays.Controls.Add(this.lblOv1, 0, 0);
            this.tlpOverlays.Controls.Add(this.btnOv1, 1, 0);
            this.tlpOverlays.Controls.Add(this.lblOv5, 2, 0);
            this.tlpOverlays.Controls.Add(this.btnOv5, 3, 0);

            this.tlpOverlays.Controls.Add(this.lblOv2, 0, 1);
            this.tlpOverlays.Controls.Add(this.btnOv2, 1, 1);
            this.tlpOverlays.Controls.Add(this.lblOv6, 2, 1);
            this.tlpOverlays.Controls.Add(this.btnOv6, 3, 1);

            this.tlpOverlays.Controls.Add(this.lblOv3, 0, 2);
            this.tlpOverlays.Controls.Add(this.btnOv3, 1, 2);
            this.tlpOverlays.Controls.Add(this.lblOv7, 2, 2);
            this.tlpOverlays.Controls.Add(this.btnOv7, 3, 2);

            this.tlpOverlays.Controls.Add(this.lblOv4, 0, 3);
            this.tlpOverlays.Controls.Add(this.btnOv4, 1, 3);
            this.tlpOverlays.Controls.Add(this.lblOv8, 2, 3);
            this.tlpOverlays.Controls.Add(this.btnOv8, 3, 3);

            this.tlpOverlays.Controls.Add(this.lblOvWidth, 0, 4);
            this.tlpOverlays.SetColumnSpan(this.lblOvWidth, 2);
            this.tlpOverlays.Controls.Add(this.nudOvWidth, 2, 4);
            this.tlpOverlays.SetColumnSpan(this.nudOvWidth, 2);

            this.grpOverlays.Controls.Add(this.tlpOverlays);

            // --- Chart table: 4 cols, 3 rows ---
            this.tlpChart.ColumnCount = 4;
            this.tlpChart.RowCount = 3;
            this.tlpChart.Dock = DockStyle.Fill;
            this.tlpChart.AutoSize = true;
            this.tlpChart.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.tlpChart.Padding = new Padding(8, 10, 8, 8);
            this.tlpChart.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.tlpChart.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 26F));
            this.tlpChart.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.tlpChart.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 26F));
            this.tlpChart.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            this.tlpChart.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            this.tlpChart.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));

            this.lblGridMaj = MakeLabel("Grid Major");
            this.btnGridMaj = MakeColorButton();
            this.lblBack = MakeLabel("Background");
            this.btnBack = MakeColorButton();

            this.lblGridMin = MakeLabel("Grid Minor");
            this.btnGridMin = MakeColorButton();
            // right cell row2 used for labels
            this.lblLabMaj = MakeLabel("Label Major");
            this.btnLabMaj = MakeColorButton();
            this.lblLabMin = MakeLabel("Label Minor");
            this.btnLabMin = MakeColorButton();

            this.tlpChart.Controls.Add(this.lblGridMaj, 0, 0);
            this.tlpChart.Controls.Add(this.btnGridMaj, 1, 0);
            this.tlpChart.Controls.Add(this.lblBack, 2, 0);
            this.tlpChart.Controls.Add(this.btnBack, 3, 0);

            this.tlpChart.Controls.Add(this.lblGridMin, 0, 1);
            this.tlpChart.Controls.Add(this.btnGridMin, 1, 1);

            this.tlpChart.Controls.Add(this.lblLabMaj, 0, 2);
            this.tlpChart.Controls.Add(this.btnLabMaj, 1, 2);
            this.tlpChart.Controls.Add(this.lblLabMin, 2, 2);
            this.tlpChart.Controls.Add(this.btnLabMin, 3, 2);

            this.grpChart.Controls.Add(this.tlpChart);

            // Buttons row
            this.tlpButtons.ColumnCount = 3;
            this.tlpButtons.RowCount = 1;
            this.tlpButtons.Dock = DockStyle.Fill;
            this.tlpButtons.AutoSize = true;
            this.tlpButtons.Padding = new Padding(0, 6, 0, 0);
            this.tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            this.tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            this.tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            this.btnDefault = new Button();
            this.btnDefault.Text = "Default";
            this.btnDefault.AutoSize = true;
            this.btnDefault.Anchor = AnchorStyles.None;
            this.btnDefault.Padding = new Padding(6, 2, 6, 2);

            this.btnCancel = new Button();
            this.btnCancel.Text = "Cancel";
            this.btnCancel.DialogResult = DialogResult.Cancel;
            this.btnCancel.AutoSize = true;
            this.btnCancel.Anchor = AnchorStyles.None;
            this.btnCancel.Padding = new Padding(6, 2, 6, 2);

            this.btnOk = new Button();
            this.btnOk.Text = "OK";
            this.btnOk.DialogResult = DialogResult.OK;
            this.btnOk.AutoSize = true;
            this.btnOk.Anchor = AnchorStyles.None;
            this.btnOk.Padding = new Padding(6, 2, 6, 2);

            this.tlpButtons.Controls.Add(this.btnDefault, 0, 0);
            this.tlpButtons.Controls.Add(this.btnCancel, 1, 0);
            this.tlpButtons.Controls.Add(this.btnOk, 2, 0);

            // Add to root
            this.tlpRoot.Controls.Add(this.grpTraces, 0, 0);
            this.tlpRoot.Controls.Add(this.grpOverlays, 0, 1);
            this.tlpRoot.Controls.Add(this.grpChart, 0, 2);
            this.tlpRoot.Controls.Add(this.tlpButtons, 0, 3);

            this.Controls.Add(this.tlpRoot);

            this.AcceptButton = this.btnOk;
            this.CancelButton = this.btnCancel;
        }

    }
}