#nullable enable
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    internal partial class ThdSettingsDialog
    {
        private IContainer? components = null;

        private CheckBox chkHPF;
        private NumericUpDown nudFlow;
        private Label lblFlow;

        private CheckBox chkLPF;
        private NumericUpDown nudFhigh;
        private Label lblFhigh;

        private Label lblHarms;
        private NumericUpDown nudHarms;
        private CheckBox chkShowHarms;

        private Label lblUnit;
        private RadioButton rbDb;
        private RadioButton rbPct;

        private Button btnOk;
        private Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            chkHPF = new CheckBox();
            nudFlow = new NumericUpDown();
            lblFlow = new Label();
            chkLPF = new CheckBox();
            nudFhigh = new NumericUpDown();
            lblFhigh = new Label();
            lblHarms = new Label();
            nudHarms = new NumericUpDown();
            chkShowHarms = new CheckBox();
            lblUnit = new Label();
            rbDb = new RadioButton();
            rbPct = new RadioButton();
            btnOk = new Button();
            btnCancel = new Button();
            ((ISupportInitialize)nudFlow).BeginInit();
            ((ISupportInitialize)nudFhigh).BeginInit();
            ((ISupportInitialize)nudHarms).BeginInit();
            SuspendLayout();
            // 
            // chkHPF
            // 
            chkHPF.AutoSize = true;
            chkHPF.Location = new Point(220, 19);
            chkHPF.Name = "chkHPF";
            chkHPF.Size = new Size(48, 19);
            chkHPF.TabIndex = 0;
            chkHPF.Text = "HPF";
            chkHPF.UseVisualStyleBackColor = true;
            // 
            // nudFlow
            // 
            nudFlow.DecimalPlaces = 1;
            nudFlow.Location = new Point(100, 15);
            nudFlow.Maximum = new decimal(new int[] { 768000, 0, 0, 0 });
            nudFlow.Name = "nudFlow";
            nudFlow.Size = new Size(96, 23);
            nudFlow.TabIndex = 2;
            // 
            // lblFlow
            // 
            lblFlow.AutoSize = true;
            lblFlow.Location = new Point(9, 19);
            lblFlow.Name = "lblFlow";
            lblFlow.Size = new Size(57, 15);
            lblFlow.TabIndex = 1;
            lblFlow.Text = "Flow (Hz)";
            // 
            // chkLPF
            // 
            chkLPF.AutoSize = true;
            chkLPF.Location = new Point(220, 64);
            chkLPF.Name = "chkLPF";
            chkLPF.Size = new Size(45, 19);
            chkLPF.TabIndex = 3;
            chkLPF.Text = "LPF";
            chkLPF.UseVisualStyleBackColor = true;
            // 
            // nudFhigh
            // 
            nudFhigh.DecimalPlaces = 1;
            nudFhigh.Increment = new decimal(new int[] { 10, 0, 0, 0 });
            nudFhigh.Location = new Point(100, 60);
            nudFhigh.Maximum = new decimal(new int[] { 768000, 0, 0, 0 });
            nudFhigh.Name = "nudFhigh";
            nudFhigh.Size = new Size(96, 23);
            nudFhigh.TabIndex = 5;
            // 
            // lblFhigh
            // 
            lblFhigh.AutoSize = true;
            lblFhigh.Location = new Point(9, 62);
            lblFhigh.Name = "lblFhigh";
            lblFhigh.Size = new Size(62, 15);
            lblFhigh.TabIndex = 4;
            lblFhigh.Text = "Fhigh (Hz)";
            // 
            // lblHarms
            // 
            lblHarms.AutoSize = true;
            lblHarms.Location = new Point(9, 109);
            lblHarms.Name = "lblHarms";
            lblHarms.Size = new Size(110, 15);
            lblHarms.TabIndex = 6;
            lblHarms.Text = "Harmonics number";
            // 
            // nudHarms
            // 
            nudHarms.Location = new Point(145, 105);
            nudHarms.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            nudHarms.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
            nudHarms.Name = "nudHarms";
            nudHarms.Size = new Size(49, 23);
            nudHarms.TabIndex = 7;
            nudHarms.Value = new decimal(new int[] { 5, 0, 0, 0 });
            // 
            // chkShowHarms
            // 
            chkShowHarms.AutoSize = true;
            chkShowHarms.Location = new Point(220, 109);
            chkShowHarms.Name = "chkShowHarms";
            chkShowHarms.Size = new Size(55, 19);
            chkShowHarms.TabIndex = 9;
            chkShowHarms.Text = "Show";
            chkShowHarms.UseVisualStyleBackColor = true;
            // 
            // lblUnit
            // 
            lblUnit.AutoSize = true;
            lblUnit.Location = new Point(9, 154);
            lblUnit.Name = "lblUnit";
            lblUnit.Size = new Size(54, 15);
            lblUnit.TabIndex = 10;
            lblUnit.Text = "THD unit";
            // 
            // rbDb
            // 
            rbDb.AutoSize = true;
            rbDb.Location = new Point(100, 152);
            rbDb.Name = "rbDb";
            rbDb.Size = new Size(39, 19);
            rbDb.TabIndex = 11;
            rbDb.Text = "dB";
            rbDb.UseVisualStyleBackColor = true;
            // 
            // rbPct
            // 
            rbPct.AutoSize = true;
            rbPct.Location = new Point(161, 152);
            rbPct.Name = "rbPct";
            rbPct.Size = new Size(35, 19);
            rbPct.TabIndex = 12;
            rbPct.Text = "%";
            rbPct.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Location = new Point(161, 206);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(90, 27);
            btnOk.TabIndex = 13;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += btnOk_Click;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(29, 206);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(90, 27);
            btnCancel.TabIndex = 14;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // ThdSettingsDialog
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(287, 245);
            Controls.Add(chkHPF);
            Controls.Add(lblFlow);
            Controls.Add(nudFlow);
            Controls.Add(chkLPF);
            Controls.Add(lblFhigh);
            Controls.Add(nudFhigh);
            Controls.Add(lblHarms);
            Controls.Add(nudHarms);
            Controls.Add(chkShowHarms);
            Controls.Add(lblUnit);
            Controls.Add(rbDb);
            Controls.Add(rbPct);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ThdSettingsDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "THD Settings";
            ((ISupportInitialize)nudFlow).EndInit();
            ((ISupportInitialize)nudFhigh).EndInit();
            ((ISupportInitialize)nudHarms).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
#nullable restore
