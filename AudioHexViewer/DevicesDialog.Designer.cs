#nullable enable
using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    internal partial class DevicesDialog
    {
        private System.ComponentModel.IContainer? components = null;

        private GroupBox  grpInput;
        private Label     lblInputDevice;
        private ComboBox  cmbInputDevice;
        private Label     lblInputRate;
        private ComboBox  cmbInputRate;

        private GroupBox  grpOutput;
        private Label     lblOutputDevice;
        private ComboBox  cmbOutputDevice;
        private Label     lblOutputRate;
        private ComboBox  cmbOutputRate;

        private Button    btnInfos;
        private Button    btnCancel;
        private Button    btnOk;

        protected override void Dispose(bool disposing)
        {
            if (disposing) components?.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            grpInput       = new GroupBox();
            lblInputDevice = new Label();
            cmbInputDevice = new ComboBox();
            lblInputRate   = new Label();
            cmbInputRate   = new ComboBox();

            grpOutput       = new GroupBox();
            lblOutputDevice = new Label();
            cmbOutputDevice = new ComboBox();
            lblOutputRate   = new Label();
            cmbOutputRate   = new ComboBox();

            btnInfos  = new Button();
            btnCancel = new Button();
            btnOk     = new Button();

            grpInput.SuspendLayout();
            grpOutput.SuspendLayout();
            SuspendLayout();

            // ── grpInput (y=8) ────────────────────────────────────────────────
            grpInput.Location = new Point(8, 8);
            grpInput.Size     = new Size(430, 72);
            grpInput.TabStop  = false;
            grpInput.Text     = "Analyzer Input";

            lblInputDevice.AutoSize = true;
            lblInputDevice.Location = new Point(8, 27);
            lblInputDevice.Text     = "Device:";

            cmbInputDevice.DropDownStyle     = ComboBoxStyle.DropDownList;
            cmbInputDevice.FormattingEnabled = true;
            cmbInputDevice.Location          = new Point(60, 23);
            cmbInputDevice.Size              = new Size(230, 23);
            cmbInputDevice.TabIndex          = 0;

            lblInputRate.AutoSize = true;
            lblInputRate.Location = new Point(298, 27);
            lblInputRate.Text     = "SR:";

            cmbInputRate.DropDownStyle     = ComboBoxStyle.DropDownList;
            cmbInputRate.FormattingEnabled = true;
            cmbInputRate.Location          = new Point(316, 23);
            cmbInputRate.Size              = new Size(104, 23);
            cmbInputRate.TabIndex          = 1;

            grpInput.Controls.Add(lblInputDevice);
            grpInput.Controls.Add(cmbInputDevice);
            grpInput.Controls.Add(lblInputRate);
            grpInput.Controls.Add(cmbInputRate);

            // ── grpOutput (y=90) ──────────────────────────────────────────────
            grpOutput.Location = new Point(8, 90);
            grpOutput.Size     = new Size(430, 72);
            grpOutput.TabStop  = false;
            grpOutput.Text     = "Generator Output";

            lblOutputDevice.AutoSize = true;
            lblOutputDevice.Location = new Point(8, 27);
            lblOutputDevice.Text     = "Device:";

            cmbOutputDevice.DropDownStyle     = ComboBoxStyle.DropDownList;
            cmbOutputDevice.FormattingEnabled = true;
            cmbOutputDevice.Location          = new Point(60, 23);
            cmbOutputDevice.Size              = new Size(230, 23);
            cmbOutputDevice.TabIndex          = 2;

            lblOutputRate.AutoSize = true;
            lblOutputRate.Location = new Point(298, 27);
            lblOutputRate.Text     = "SR:";

            cmbOutputRate.DropDownStyle     = ComboBoxStyle.DropDownList;
            cmbOutputRate.FormattingEnabled = true;
            cmbOutputRate.Location          = new Point(316, 23);
            cmbOutputRate.Size              = new Size(104, 23);
            cmbOutputRate.TabIndex          = 3;

            grpOutput.Controls.Add(lblOutputDevice);
            grpOutput.Controls.Add(cmbOutputDevice);
            grpOutput.Controls.Add(lblOutputRate);
            grpOutput.Controls.Add(cmbOutputRate);

            // ── Buttons (y=174) ───────────────────────────────────────────────
            btnInfos.Location = new Point(8, 174);
            btnInfos.Size     = new Size(70, 27);
            btnInfos.TabIndex = 4;
            btnInfos.Text     = "Infos";
            btnInfos.UseVisualStyleBackColor = true;

            btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            btnCancel.Location     = new Point(268, 174);
            btnCancel.Size         = new Size(80, 27);
            btnCancel.TabIndex     = 5;
            btnCancel.Text         = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;

            btnOk.Location = new Point(358, 174);
            btnOk.Size     = new Size(80, 27);
            btnOk.TabIndex = 6;
            btnOk.Text     = "OK";
            btnOk.UseVisualStyleBackColor = true;

            // ── Form ──────────────────────────────────────────────────────────
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize          = new Size(448, 213);
            Controls.Add(grpInput);
            Controls.Add(grpOutput);
            Controls.Add(btnInfos);
            Controls.Add(btnCancel);
            Controls.Add(btnOk);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            StartPosition   = System.Windows.Forms.FormStartPosition.CenterParent;
            Text            = "Devices";

            grpInput.ResumeLayout(false);
            grpInput.PerformLayout();
            grpOutput.ResumeLayout(false);
            grpOutput.PerformLayout();
            ResumeLayout(false);
        }
    }
}
#nullable restore
