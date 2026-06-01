#nullable enable
using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    internal partial class DevicesDialog
    {
        private System.ComponentModel.IContainer? components = null;

        private GroupBox  grpInput,  grpOutput;
        private Label     lblInputDevice,  lblInputSR,   lblInputFmtHdr,  lblInputFormat;
        private Label     lblOutputDevice, lblOutputSR,  lblOutputFmtHdr, lblOutputFormat;
        private ComboBox  cmbInputDevice,  cmbInputRate;
        private ComboBox  cmbOutputDevice, cmbOutputRate;
        private Button    btnInfos, btnCancel, btnOk;

        protected override void Dispose(bool disposing)
        { if (disposing) components?.Dispose(); base.Dispose(disposing); }

        private void InitializeComponent()
        {
            var gS   = new Font("Segoe UI", 7.5f, FontStyle.Regular, GraphicsUnit.Point);
            var std9 = new Font("Segoe UI", 9f,   FontStyle.Regular, GraphicsUnit.Point);

            grpInput        = new GroupBox();
            lblInputDevice  = new Label();
            cmbInputDevice  = new ComboBox();
            lblInputSR      = new Label();
            cmbInputRate    = new ComboBox();
            lblInputFmtHdr  = new Label();
            lblInputFormat  = new Label();

            grpOutput       = new GroupBox();
            lblOutputDevice = new Label();
            cmbOutputDevice = new ComboBox();
            lblOutputSR     = new Label();
            cmbOutputRate   = new ComboBox();
            lblOutputFmtHdr = new Label();
            lblOutputFormat = new Label();

            btnInfos  = new Button();
            btnCancel = new Button();
            btnOk     = new Button();

            grpInput.SuspendLayout();
            grpOutput.SuspendLayout();
            SuspendLayout();

            // ── grpInput ─────────────────────────────────────────────────────
            grpInput.Text     = "Analyzer Input";
            grpInput.Font     = new Font("Segoe UI", 9f, FontStyle.Bold, GraphicsUnit.Point);
            grpInput.Location = new Point(10, 8);
            grpInput.Size     = new Size(450, 92);
            grpInput.TabStop  = false;

            lblInputDevice.Text      = "DEVICE";
            lblInputDevice.Font      = gS;
            lblInputDevice.ForeColor = System.Drawing.SystemColors.GrayText;
            lblInputDevice.Location  = new Point(10, 22);
            lblInputDevice.AutoSize  = true;

            cmbInputDevice.Font              = std9;
            cmbInputDevice.DropDownStyle     = ComboBoxStyle.DropDownList;
            cmbInputDevice.FormattingEnabled = true;
            cmbInputDevice.Location          = new Point(10, 36);
            cmbInputDevice.Size              = new Size(280, 24);

            lblInputSR.Text      = "SAMPLE RATE";
            lblInputSR.Font      = gS;
            lblInputSR.ForeColor = System.Drawing.SystemColors.GrayText;
            lblInputSR.Location  = new Point(298, 22);
            lblInputSR.AutoSize  = true;

            cmbInputRate.Font              = std9;
            cmbInputRate.DropDownStyle     = ComboBoxStyle.DropDownList;
            cmbInputRate.FormattingEnabled = true;
            cmbInputRate.Location          = new Point(298, 36);
            cmbInputRate.Size              = new Size(140, 24);

            lblInputFmtHdr.Text      = "FORMAT";
            lblInputFmtHdr.Font      = gS;
            lblInputFmtHdr.ForeColor = System.Drawing.SystemColors.GrayText;
            lblInputFmtHdr.Location  = new Point(10, 64);
            lblInputFmtHdr.AutoSize  = true;

            lblInputFormat.Text      = "";
            lblInputFormat.Font      = std9;
            lblInputFormat.ForeColor = System.Drawing.Color.SteelBlue;
            lblInputFormat.Location  = new Point(65, 64);
            lblInputFormat.Size      = new Size(370, 18);

            grpInput.Controls.AddRange(new System.Windows.Forms.Control[]
                { lblInputDevice, cmbInputDevice, lblInputSR, cmbInputRate,
                  lblInputFmtHdr, lblInputFormat });

            // ── grpOutput ────────────────────────────────────────────────────
            grpOutput.Text     = "Generator Output";
            grpOutput.Font     = new Font("Segoe UI", 9f, FontStyle.Bold, GraphicsUnit.Point);
            grpOutput.Location = new Point(10, 110);
            grpOutput.Size     = new Size(450, 92);
            grpOutput.TabStop  = false;

            lblOutputDevice.Text      = "DEVICE";
            lblOutputDevice.Font      = gS;
            lblOutputDevice.ForeColor = System.Drawing.SystemColors.GrayText;
            lblOutputDevice.Location  = new Point(10, 22);
            lblOutputDevice.AutoSize  = true;

            cmbOutputDevice.Font              = std9;
            cmbOutputDevice.DropDownStyle     = ComboBoxStyle.DropDownList;
            cmbOutputDevice.FormattingEnabled = true;
            cmbOutputDevice.Location          = new Point(10, 36);
            cmbOutputDevice.Size              = new Size(280, 24);

            lblOutputSR.Text      = "SAMPLE RATE";
            lblOutputSR.Font      = gS;
            lblOutputSR.ForeColor = System.Drawing.SystemColors.GrayText;
            lblOutputSR.Location  = new Point(298, 22);
            lblOutputSR.AutoSize  = true;

            cmbOutputRate.Font              = std9;
            cmbOutputRate.DropDownStyle     = ComboBoxStyle.DropDownList;
            cmbOutputRate.FormattingEnabled = true;
            cmbOutputRate.Location          = new Point(298, 36);
            cmbOutputRate.Size              = new Size(140, 24);

            lblOutputFmtHdr.Text      = "FORMAT";
            lblOutputFmtHdr.Font      = gS;
            lblOutputFmtHdr.ForeColor = System.Drawing.SystemColors.GrayText;
            lblOutputFmtHdr.Location  = new Point(10, 64);
            lblOutputFmtHdr.AutoSize  = true;

            lblOutputFormat.Text      = "";
            lblOutputFormat.Font      = std9;
            lblOutputFormat.ForeColor = System.Drawing.Color.SteelBlue;
            lblOutputFormat.Location  = new Point(65, 64);
            lblOutputFormat.Size      = new Size(370, 18);

            grpOutput.Controls.AddRange(new System.Windows.Forms.Control[]
                { lblOutputDevice, cmbOutputDevice, lblOutputSR, cmbOutputRate,
                  lblOutputFmtHdr, lblOutputFormat });

            // ── Buttons ───────────────────────────────────────────────────────
            btnInfos.Text     = "Device info…";
            btnInfos.Font     = std9;
            btnInfos.Location = new Point(10, 214);
            btnInfos.Size     = new Size(92, 27);
            btnInfos.UseVisualStyleBackColor = true;

            btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            btnCancel.Text         = "CANCEL";
            btnCancel.Font         = std9;
            btnCancel.Location     = new Point(278, 214);
            btnCancel.Size         = new Size(88, 27);
            btnCancel.UseVisualStyleBackColor = true;

            btnOk.Text     = "OK";
            btnOk.Font     = new Font("Segoe UI", 9f, FontStyle.Bold, GraphicsUnit.Point);
            btnOk.Location = new Point(372, 214);
            btnOk.Size     = new Size(88, 27);
            btnOk.FlatStyle = FlatStyle.Flat;
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.BackColor = System.Drawing.Color.FromArgb(0, 122, 204);
            btnOk.ForeColor = System.Drawing.Color.White;

            // ── Form ──────────────────────────────────────────────────────────
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode       = AutoScaleMode.Font;
            ClientSize          = new Size(470, 252);
            Font                = std9;
            Controls.AddRange(new System.Windows.Forms.Control[]
                { grpInput, grpOutput, btnInfos, btnCancel, btnOk });
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false; MinimizeBox = false;
            StartPosition   = FormStartPosition.CenterParent;
            Text            = "Audio Devices";

            grpInput.ResumeLayout(false);
            grpInput.PerformLayout();
            grpOutput.ResumeLayout(false);
            grpOutput.PerformLayout();
            ResumeLayout(false);
        }
    }
}
#nullable restore
