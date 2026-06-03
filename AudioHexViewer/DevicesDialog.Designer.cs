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
        private Label     lblBufferHdr;
        private ComboBox  cmbBuffer;
        private Button    btnInfos, btnCancel, btnOk;

        protected override void Dispose(bool disposing)
        { if (disposing) components?.Dispose(); base.Dispose(disposing); }

        private void InitializeComponent()
        {
            var gS   = new Font("Segoe UI", 7.5f, FontStyle.Regular, GraphicsUnit.Point);
            var std9 = new Font("Segoe UI", 9f,   FontStyle.Regular, GraphicsUnit.Point);

            // ── Layout constants ──────────────────────────────────────────────
            // SR combo reduced (140 → 90px), buffer inline under SR inside Input group
            const int SR_X = 298;  // x of Sample Rate combo (and Buffer combo)
            const int SR_W = 90;   // width of SR combos → right = 388
            const int GW   = 398;  // group width  (+10px right padding inside group)
            const int BUF_LW = 46; // buffer label width (right-aligned, gS)

            // ── Control creation ──────────────────────────────────────────────
            grpInput        = new GroupBox();
            lblInputDevice  = new Label();  cmbInputDevice  = new ComboBox();
            lblInputSR      = new Label();  cmbInputRate    = new ComboBox();
            lblInputFmtHdr  = new Label();  lblInputFormat  = new Label();
            lblBufferHdr    = new Label();  cmbBuffer       = new ComboBox();

            grpOutput       = new GroupBox();
            lblOutputDevice = new Label();  cmbOutputDevice = new ComboBox();
            lblOutputSR     = new Label();  cmbOutputRate   = new ComboBox();
            lblOutputFmtHdr = new Label();  lblOutputFormat = new Label();

            btnInfos  = new Button();
            btnCancel = new Button();
            btnOk     = new Button();

            grpInput.SuspendLayout();
            grpOutput.SuspendLayout();
            SuspendLayout();

            // ══════════════════════════════════════════════════════════════════
            // grpInput  — h=110 (adds buffer row under SR)
            // ══════════════════════════════════════════════════════════════════
            grpInput.Text     = "Analyzer Input";
            grpInput.Font     = new Font("Segoe UI", 9f, FontStyle.Bold, GraphicsUnit.Point);
            grpInput.Location = new Point(10, 8);
            grpInput.Size     = new Size(GW, 118);
            grpInput.TabStop  = false;

            // DEVICE
            lblInputDevice.Text      = "DEVICE";
            lblInputDevice.Font      = gS;
            lblInputDevice.ForeColor = SystemColors.GrayText;
            lblInputDevice.Location  = new Point(10, 22);
            lblInputDevice.AutoSize  = true;

            cmbInputDevice.Font              = std9;
            cmbInputDevice.DropDownStyle     = ComboBoxStyle.DropDownList;
            cmbInputDevice.FormattingEnabled = true;
            cmbInputDevice.Location          = new Point(10, 36);
            cmbInputDevice.Size              = new Size(280, 24);

            // SAMPLE RATE (réduit)
            lblInputSR.Text      = "SAMPLE RATE";
            lblInputSR.Font      = gS;
            lblInputSR.ForeColor = SystemColors.GrayText;
            lblInputSR.Location  = new Point(SR_X, 22);
            lblInputSR.AutoSize  = true;

            cmbInputRate.Font              = std9;
            cmbInputRate.DropDownStyle     = ComboBoxStyle.DropDownList;
            cmbInputRate.FormattingEnabled = true;
            cmbInputRate.Location          = new Point(SR_X, 36);
            cmbInputRate.Size              = new Size(SR_W, 24);

            // FORMAT
            lblInputFmtHdr.Text      = "FORMAT";
            lblInputFmtHdr.Font      = gS;
            lblInputFmtHdr.ForeColor = SystemColors.GrayText;
            lblInputFmtHdr.Location  = new Point(10, 64);
            lblInputFmtHdr.AutoSize  = true;

            lblInputFormat.Text      = "";
            lblInputFormat.Font      = std9;
            lblInputFormat.ForeColor = System.Drawing.Color.SteelBlue;
            lblInputFormat.Location  = new Point(65, 64);
            lblInputFormat.Size      = new Size(310, 18);

            // BUFFER — sous le SR, label à gauche du combo
            lblBufferHdr.Text      = "BUFFER";
            lblBufferHdr.Font      = gS;
            lblBufferHdr.ForeColor = SystemColors.GrayText;
            lblBufferHdr.Location  = new Point(SR_X - BUF_LW - 4, 87);
            lblBufferHdr.Size      = new Size(BUF_LW, 14);
            lblBufferHdr.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            lblBufferHdr.AutoSize  = false;

            cmbBuffer.DropDownStyle     = ComboBoxStyle.DropDownList;
            cmbBuffer.Font              = std9;
            cmbBuffer.FormattingEnabled = true;
            cmbBuffer.Location          = new Point(SR_X, 83);
            cmbBuffer.Size              = new Size(SR_W, 24);

            grpInput.Controls.AddRange(new Control[]
            {
                lblInputDevice, cmbInputDevice,
                lblInputSR,     cmbInputRate,
                lblInputFmtHdr, lblInputFormat,
                lblBufferHdr,   cmbBuffer
            });

            // ══════════════════════════════════════════════════════════════════
            // grpOutput  — y décalé de +18 par rapport à avant (grpInput plus grand)
            // ══════════════════════════════════════════════════════════════════
            grpOutput.Text     = "Generator Output";
            grpOutput.Font     = new Font("Segoe UI", 9f, FontStyle.Bold, GraphicsUnit.Point);
            grpOutput.Location = new Point(10, 136);
            grpOutput.Size     = new Size(GW, 92);
            grpOutput.TabStop  = false;

            // DEVICE
            lblOutputDevice.Text      = "DEVICE";
            lblOutputDevice.Font      = gS;
            lblOutputDevice.ForeColor = SystemColors.GrayText;
            lblOutputDevice.Location  = new Point(10, 22);
            lblOutputDevice.AutoSize  = true;

            cmbOutputDevice.Font              = std9;
            cmbOutputDevice.DropDownStyle     = ComboBoxStyle.DropDownList;
            cmbOutputDevice.FormattingEnabled = true;
            cmbOutputDevice.Location          = new Point(10, 36);
            cmbOutputDevice.Size              = new Size(280, 24);

            // SAMPLE RATE (réduit)
            lblOutputSR.Text      = "SAMPLE RATE";
            lblOutputSR.Font      = gS;
            lblOutputSR.ForeColor = SystemColors.GrayText;
            lblOutputSR.Location  = new Point(SR_X, 22);
            lblOutputSR.AutoSize  = true;

            cmbOutputRate.Font              = std9;
            cmbOutputRate.DropDownStyle     = ComboBoxStyle.DropDownList;
            cmbOutputRate.FormattingEnabled = true;
            cmbOutputRate.Location          = new Point(SR_X, 36);
            cmbOutputRate.Size              = new Size(SR_W, 24);

            // FORMAT
            lblOutputFmtHdr.Text      = "FORMAT";
            lblOutputFmtHdr.Font      = gS;
            lblOutputFmtHdr.ForeColor = SystemColors.GrayText;
            lblOutputFmtHdr.Location  = new Point(10, 64);
            lblOutputFmtHdr.AutoSize  = true;

            lblOutputFormat.Text      = "";
            lblOutputFormat.Font      = std9;
            lblOutputFormat.ForeColor = System.Drawing.Color.SteelBlue;
            lblOutputFormat.Location  = new Point(65, 64);
            lblOutputFormat.Size      = new Size(310, 18);

            grpOutput.Controls.AddRange(new Control[]
            {
                lblOutputDevice, cmbOutputDevice,
                lblOutputSR,     cmbOutputRate,
                lblOutputFmtHdr, lblOutputFormat
            });

            // ══════════════════════════════════════════════════════════════════
            // Buttons  — y = 128+92+12 = 232
            // ══════════════════════════════════════════════════════════════════
            btnInfos.Text     = "Device info…";
            btnInfos.Font     = std9;
            btnInfos.Location = new Point(10, 240);
            btnInfos.Size     = new Size(92, 27);
            btnInfos.UseVisualStyleBackColor = true;

            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Text         = "CANCEL";
            btnCancel.Font         = std9;
            btnCancel.Location     = new Point(226, 240);
            btnCancel.Size         = new Size(88, 27);
            btnCancel.UseVisualStyleBackColor = true;

            btnOk.Text     = "OK";
            btnOk.Font     = new Font("Segoe UI", 9f, FontStyle.Bold, GraphicsUnit.Point);
            btnOk.Location = new Point(320, 240);
            btnOk.Size     = new Size(88, 27);
            btnOk.FlatStyle = FlatStyle.Flat;
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.BackColor = System.Drawing.Color.FromArgb(0, 122, 204);
            btnOk.ForeColor = System.Drawing.Color.White;

            // ── Form ──────────────────────────────────────────────────────────
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode       = AutoScaleMode.Font;
            ClientSize          = new Size(418, 277);
            Font                = std9;
            Controls.AddRange(new Control[]
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
