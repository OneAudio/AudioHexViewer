#nullable enable
using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    internal sealed partial class GeneratorCalibrationDialog
    {
        private System.ComponentModel.IContainer? components = null;

        private Label   lblInstruction;
        private Label   lblCurrentGen;
        private Label   lblSeparator;
        private Label   lblVrmsLTitle;
        private TextBox txtVrmsL;
        private Label   lblVrmsLUnit;
        private Label   lblVrmsRTitle;
        private TextBox txtVrmsR;
        private Label   lblVrmsRUnit;
        private Button  btnApply;
        private Button  btnClose;
        private Button  btnClearCal;
        private Label   lblVrmsLStatus;
        private Label   lblVrmsRStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing) { components?.Dispose(); _refreshTimer?.Dispose(); }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            lblInstruction = new Label();
            lblCurrentGen  = new Label();
            lblSeparator   = new Label();
            lblVrmsLTitle  = new Label();
            txtVrmsL       = new TextBox();
            lblVrmsLUnit   = new Label();
            lblVrmsRTitle  = new Label();
            txtVrmsR       = new TextBox();
            lblVrmsRUnit   = new Label();
            btnApply       = new Button();
            btnClose       = new Button();
            btnClearCal    = new Button();
            lblVrmsLStatus = new Label();
            lblVrmsRStatus = new Label();

            SuspendLayout();

            // ── Instruction ───────────────────────────────────────────────────
            lblInstruction.Location  = new Point(12, 14);
            lblInstruction.Size      = new Size(360, 72);
            lblInstruction.Text      =
                "Run the generator with a Sine signal at the desired calibration " +
                "frequency and amplitude. Measure the output voltage with a calibrated " +
                "voltmeter, then enter the RMS values below and click Apply.";
            lblInstruction.Font      = new Font("Segoe UI", 9f);
            lblInstruction.ForeColor = SystemColors.ControlText;

            // ── Générateur courant (dynamique) ────────────────────────────────
            lblCurrentGen.Location  = new Point(12, 92);
            lblCurrentGen.Size      = new Size(360, 20);
            lblCurrentGen.Text      = "Generator is not running";
            lblCurrentGen.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblCurrentGen.ForeColor = Color.OrangeRed;

            // ── Séparateur ────────────────────────────────────────────────────
            lblSeparator.Location  = new Point(12, 120);
            lblSeparator.Size      = new Size(360, 1);
            lblSeparator.BorderStyle = BorderStyle.Fixed3D;

            // ── L Channel ─────────────────────────────────────────────────────
            lblVrmsLTitle.Location  = new Point(12, 132);
            lblVrmsLTitle.AutoSize  = true;
            lblVrmsLTitle.Text      = "L  —  measured Vrms:";
            lblVrmsLTitle.Font      = new Font("Segoe UI", 9.5f);

            txtVrmsL.Location  = new Point(192, 129);
            txtVrmsL.Size      = new Size(110, 23);
            txtVrmsL.Font      = new Font("Segoe UI", 10f);
            txtVrmsL.Text      = "";
            txtVrmsL.TabIndex  = 0;

            lblVrmsLUnit.Location  = new Point(308, 132);
            lblVrmsLUnit.AutoSize  = true;
            lblVrmsLUnit.Text      = "Vrms";
            lblVrmsLUnit.Font      = new Font("Segoe UI", 9f);
            lblVrmsLUnit.ForeColor = SystemColors.GrayText;

            lblVrmsLStatus.Location  = new Point(192, 155);
            lblVrmsLStatus.Size      = new Size(180, 16);
            lblVrmsLStatus.Text      = "0 dBFS = 1.000 Vrms";
            lblVrmsLStatus.Font      = new Font("Segoe UI", 8f);
            lblVrmsLStatus.ForeColor = SystemColors.GrayText;

            // ── R Channel ─────────────────────────────────────────────────────
            lblVrmsRTitle.Location  = new Point(12, 178);
            lblVrmsRTitle.AutoSize  = true;
            lblVrmsRTitle.Text      = "R  —  measured Vrms:";
            lblVrmsRTitle.Font      = new Font("Segoe UI", 9.5f);

            txtVrmsR.Location  = new Point(192, 175);
            txtVrmsR.Size      = new Size(110, 23);
            txtVrmsR.Font      = new Font("Segoe UI", 10f);
            txtVrmsR.Text      = "";
            txtVrmsR.TabIndex  = 1;

            lblVrmsRUnit.Location  = new Point(308, 178);
            lblVrmsRUnit.AutoSize  = true;
            lblVrmsRUnit.Text      = "Vrms";
            lblVrmsRUnit.Font      = new Font("Segoe UI", 9f);
            lblVrmsRUnit.ForeColor = SystemColors.GrayText;

            lblVrmsRStatus.Location  = new Point(192, 201);
            lblVrmsRStatus.Size      = new Size(180, 16);
            lblVrmsRStatus.Text      = "0 dBFS = 1.000 Vrms";
            lblVrmsRStatus.Font      = new Font("Segoe UI", 8f);
            lblVrmsRStatus.ForeColor = SystemColors.GrayText;

            // ── Boutons ───────────────────────────────────────────────────────
            btnClearCal.Location  = new Point(12, 228);
            btnClearCal.Size      = new Size(90, 32);
            btnClearCal.Text      = "CLEAR";
            btnClearCal.TabIndex  = 4;
            btnClearCal.UseVisualStyleBackColor = true;

            btnApply.Location  = new Point(192, 228);
            btnApply.Size      = new Size(110, 32);
            btnApply.Text      = "Apply";
            btnApply.BackColor = Color.SeaGreen;
            btnApply.ForeColor = Color.White;
            btnApply.FlatStyle = FlatStyle.Flat;
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnApply.TabIndex  = 2;
            btnApply.UseVisualStyleBackColor = false;

            btnClose.Location  = new Point(312, 228);
            btnClose.Size      = new Size(60, 32);
            btnClose.Text      = "Close";
            btnClose.TabIndex  = 3;
            btnClose.UseVisualStyleBackColor = true;

            // ── Form ──────────────────────────────────────────────────────────
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode       = AutoScaleMode.Font;
            ClientSize          = new Size(384, 274);
            FormBorderStyle     = FormBorderStyle.FixedDialog;
            MaximizeBox         = false; MinimizeBox = false;
            StartPosition       = FormStartPosition.Manual;
            Text                = "Generator — Level Calibration";

            Controls.AddRange(new Control[]
            {
                lblInstruction, lblCurrentGen, lblSeparator,
                lblVrmsLTitle,  txtVrmsL, lblVrmsLUnit, lblVrmsLStatus,
                lblVrmsRTitle,  txtVrmsR, lblVrmsRUnit, lblVrmsRStatus,
                btnClearCal, btnApply, btnClose
            });

            // ── Context help Name assignments ─────────────────────────────
            txtVrmsL.Name = "txtVrmsL";
            txtVrmsR.Name = "txtVrmsR";
            btnClearCal.Name = "btnClearCal";
            btnApply.Name = "btnApply";
            btnClose.Name = "btnClose";
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
#nullable restore
