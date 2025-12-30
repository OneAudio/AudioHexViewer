using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    partial class SpectrumColorsDialog
    {
        private System.ComponentModel.IContainer components = null;

        private GroupBox grpFft;
        private GroupBox grpTraces;
        private GroupBox grpChart;

        private Label lblFftL;
        private Label lblFftR;
        private Label lblOverlay;
        private Label lblPeakL;
        private Label lblPeakR;
        private Label lblGrid;
        private Label lblBack;

        private PictureBox pbFftL;
        private PictureBox pbFftR;
        private PictureBox pbOverlay;
        private PictureBox pbPeakL;
        private PictureBox pbPeakR;
        private PictureBox pbGrid;
        private PictureBox pbBack;

        private Button btnPickFftL;
        private Button btnPickFftR;
        private Button btnPickOverlay;
        private Button btnPickPeakL;
        private Button btnPickPeakR;
        private Button btnPickGrid;
        private Button btnPickBack;

        private Button btnOk;
        private Button btnCancel;
        private Button btnDefault;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            // Form
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(280, 380);
            this.MinimumSize = new Size(260, 360);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Colors";
            this.Padding = new Padding(12);

            // GroupBoxes
            this.grpFft = new GroupBox();
            this.grpTraces = new GroupBox();
            this.grpChart = new GroupBox();

            this.grpFft.Text = "FFT";
            this.grpTraces.Text = "Traces";
            this.grpChart.Text = "Chart";

            grpFft.Location = new Point(12, 12);
            grpFft.Size = new Size(250, 88);

            grpTraces.Location = new Point(12, 108);
            grpTraces.Size = new Size(250, 130);

            grpChart.Location = new Point(12, 246);
            grpChart.Size = new Size(250, 92);


            // Labels
            this.lblFftL = new Label();
            this.lblFftR = new Label();
            this.lblOverlay = new Label();
            this.lblPeakL = new Label();
            this.lblPeakR = new Label();
            this.lblGrid = new Label();
            this.lblBack = new Label();

            this.lblFftL.Text = "FFT Left";
            this.lblFftL.AutoSize = true;
            this.lblFftL.Location = new Point(12, 28);

            this.lblFftR.Text = "FFT Right";
            this.lblFftR.AutoSize = true;
            this.lblFftR.Location = new Point(12, 56);

            this.lblOverlay.Text = "Overlays";
            this.lblOverlay.AutoSize = true;
            this.lblOverlay.Location = new Point(12, 28);

            this.lblPeakL.Text = "PeakHold Left";
            this.lblPeakL.AutoSize = true;
            this.lblPeakL.Location = new Point(12, 56);

            this.lblPeakR.Text = "PeakHold Right";
            this.lblPeakR.AutoSize = true;
            this.lblPeakR.Location = new Point(12, 84);

            this.lblGrid.Text = "Grid";
            this.lblGrid.AutoSize = true;
            this.lblGrid.Location = new Point(12, 28);

            this.lblBack.Text = "Background";
            this.lblBack.AutoSize = true;
            this.lblBack.Location = new Point(12, 56);

            // PictureBoxes (swatches) - no helper function (Designer-friendly)
            this.pbFftL = new PictureBox();
            this.pbFftR = new PictureBox();
            this.pbOverlay = new PictureBox();
            this.pbPeakL = new PictureBox();
            this.pbPeakR = new PictureBox();
            this.pbGrid = new PictureBox();
            this.pbBack = new PictureBox();

            SetupSwatch(this.pbFftL, new Point(130, 26));
            SetupSwatch(this.pbFftR, new Point(130, 54));
            SetupSwatch(this.pbOverlay, new Point(130, 26));
            SetupSwatch(this.pbPeakL, new Point(130, 54));
            SetupSwatch(this.pbPeakR, new Point(130, 82));
            SetupSwatch(this.pbGrid, new Point(130, 26));
            SetupSwatch(this.pbBack, new Point(130, 54));


            // Buttons "..."
            this.btnPickFftL = new Button();
            this.btnPickFftR = new Button();
            this.btnPickOverlay = new Button();
            this.btnPickPeakL = new Button();
            this.btnPickPeakR = new Button();
            this.btnPickGrid = new Button();
            this.btnPickBack = new Button();

            SetupPickButton(this.btnPickFftL, new Point(160, 22));
            SetupPickButton(this.btnPickFftR, new Point(160, 50));
            SetupPickButton(this.btnPickOverlay, new Point(160, 22));
            SetupPickButton(this.btnPickPeakL, new Point(160, 50));
            SetupPickButton(this.btnPickPeakR, new Point(160, 78));
            SetupPickButton(this.btnPickGrid, new Point(160, 22));
            SetupPickButton(this.btnPickBack, new Point(160, 50));


            // Add FFT group controls
            this.grpFft.Controls.Add(this.lblFftL);
            this.grpFft.Controls.Add(this.pbFftL);
            this.grpFft.Controls.Add(this.btnPickFftL);
            this.grpFft.Controls.Add(this.lblFftR);
            this.grpFft.Controls.Add(this.pbFftR);
            this.grpFft.Controls.Add(this.btnPickFftR);

            // Add Traces group controls
            this.grpTraces.Controls.Add(this.lblOverlay);
            this.grpTraces.Controls.Add(this.pbOverlay);
            this.grpTraces.Controls.Add(this.btnPickOverlay);

            this.grpTraces.Controls.Add(this.lblPeakL);
            this.grpTraces.Controls.Add(this.pbPeakL);
            this.grpTraces.Controls.Add(this.btnPickPeakL);

            this.grpTraces.Controls.Add(this.lblPeakR);
            this.grpTraces.Controls.Add(this.pbPeakR);
            this.grpTraces.Controls.Add(this.btnPickPeakR);

            // Add Chart group controls
            this.grpChart.Controls.Add(this.lblGrid);
            this.grpChart.Controls.Add(this.pbGrid);
            this.grpChart.Controls.Add(this.btnPickGrid);

            this.grpChart.Controls.Add(this.lblBack);
            this.grpChart.Controls.Add(this.pbBack);
            this.grpChart.Controls.Add(this.btnPickBack);

            // Bottom buttons
            this.btnOk = new Button();
            this.btnCancel = new Button();
            this.btnDefault = new Button();

            this.btnOk.Text = "OK";
            this.btnOk.DialogResult = DialogResult.OK;
            this.btnOk.Size = new Size(90, 28);
            this.btnOk.Location = new Point(175, 344);

            this.btnCancel.Text = "Cancel";
            this.btnCancel.DialogResult = DialogResult.Cancel;
            this.btnCancel.Size = new Size(90, 28);
            this.btnCancel.Location = new Point(75, 344);

            this.btnDefault.Text = "Default";
            this.btnDefault.Size = new Size(60, 28);
            this.btnDefault.Location = new Point(5, 344);

            // Accept/Cancel buttons
            this.AcceptButton = this.btnOk;
            this.CancelButton = this.btnCancel;

            // Add controls to Form
            this.Controls.Add(this.grpFft);
            this.Controls.Add(this.grpTraces);
            this.Controls.Add(this.grpChart);
            this.Controls.Add(this.btnDefault);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
        }

        // IMPORTANT: The WinForms Designer also dislikes local functions in InitializeComponent,
        // but it accepts private helper methods on the class.
        private static void SetupSwatch(PictureBox pb, Point location)
        {
            pb.BorderStyle = BorderStyle.FixedSingle;
            pb.Size = new Size(18, 18);          // carré (au lieu de 70x22)
            pb.Location = location;
            pb.Cursor = Cursors.Hand;
            pb.TabStop = false;
        }


        private static void SetupPickButton(Button b, Point location)
        {
            b.Text = "…";
            b.Size = new Size(36, 24);
            b.Location = location;
        }
    }
}
