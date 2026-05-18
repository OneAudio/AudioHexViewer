#nullable enable
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    internal partial class ThdSettingsDialog
    {
        private IContainer? components = null;

        // THD frequency limits
        private GroupBox grpThd;
        private Label lblFlow;
        private NumericUpDown nudFlow;
        private CheckBox chkHPF;
        private Label lblFhigh;
        private NumericUpDown nudFhigh;
        private CheckBox chkLPF;

        // SFDR frequency limits
        private GroupBox grpSfdr;
        private Label lblSfdrFlow;
        private NumericUpDown nudSfdrFlow;
        private CheckBox chkSfdrHPF;
        private Label lblSfdrFhigh;
        private NumericUpDown nudSfdrFhigh;
        private CheckBox chkSfdrLPF;

        // Harmonics / unit / avg
        private Label lblHarms;
        private NumericUpDown nudHarms;
        private CheckBox chkShowHarms;
        private Label lblUnit;
        private RadioButton rbDb;
        private RadioButton rbPct;
        private Label lblCohAvg;
        private CheckBox chkCohAvg;
        private Label lblCohAvgHint;
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
            grpThd        = new GroupBox();
            lblFlow       = new Label();
            nudFlow       = new NumericUpDown();
            chkHPF        = new CheckBox();
            lblFhigh      = new Label();
            nudFhigh      = new NumericUpDown();
            chkLPF        = new CheckBox();

            grpSfdr       = new GroupBox();
            lblSfdrFlow   = new Label();
            nudSfdrFlow   = new NumericUpDown();
            chkSfdrHPF    = new CheckBox();
            lblSfdrFhigh  = new Label();
            nudSfdrFhigh  = new NumericUpDown();
            chkSfdrLPF    = new CheckBox();

            lblHarms      = new Label();
            nudHarms      = new NumericUpDown();
            chkShowHarms  = new CheckBox();
            lblUnit       = new Label();
            rbDb          = new RadioButton();
            rbPct         = new RadioButton();
            lblCohAvg     = new Label();
            chkCohAvg     = new CheckBox();
            lblCohAvgHint = new Label();
            btnOk         = new Button();
            btnCancel     = new Button();

            ((ISupportInitialize)nudFlow).BeginInit();
            ((ISupportInitialize)nudFhigh).BeginInit();
            ((ISupportInitialize)nudSfdrFlow).BeginInit();
            ((ISupportInitialize)nudSfdrFhigh).BeginInit();
            ((ISupportInitialize)nudHarms).BeginInit();
            grpThd.SuspendLayout();
            grpSfdr.SuspendLayout();
            SuspendLayout();

            // grpThd
            grpThd.Location = new Point(8, 6);
            grpThd.Name = "grpThd";
            grpThd.Size = new Size(272, 95);
            grpThd.TabStop = false;
            grpThd.Text = "THD frequency limits";

            lblFlow.AutoSize = true;
            lblFlow.Location = new Point(8, 26);
            lblFlow.Name = "lblFlow";
            lblFlow.Text = "Flow (Hz)";

            nudFlow.DecimalPlaces = 1;
            nudFlow.Location = new Point(95, 23);
            nudFlow.Maximum = new decimal(new int[] { 768000, 0, 0, 0 });
            nudFlow.Name = "nudFlow";
            nudFlow.Size = new Size(96, 23);
            nudFlow.TabIndex = 0;

            chkHPF.AutoSize = true;
            chkHPF.Location = new Point(205, 25);
            chkHPF.Name = "chkHPF";
            chkHPF.Text = "HPF";
            chkHPF.TabIndex = 1;
            chkHPF.UseVisualStyleBackColor = true;

            lblFhigh.AutoSize = true;
            lblFhigh.Location = new Point(8, 62);
            lblFhigh.Name = "lblFhigh";
            lblFhigh.Text = "Fhigh (Hz)";

            nudFhigh.DecimalPlaces = 1;
            nudFhigh.Increment = new decimal(new int[] { 10, 0, 0, 0 });
            nudFhigh.Location = new Point(95, 59);
            nudFhigh.Maximum = new decimal(new int[] { 768000, 0, 0, 0 });
            nudFhigh.Name = "nudFhigh";
            nudFhigh.Size = new Size(96, 23);
            nudFhigh.TabIndex = 2;

            chkLPF.AutoSize = true;
            chkLPF.Location = new Point(205, 61);
            chkLPF.Name = "chkLPF";
            chkLPF.Text = "LPF";
            chkLPF.TabIndex = 3;
            chkLPF.UseVisualStyleBackColor = true;

            grpThd.Controls.Add(lblFlow);
            grpThd.Controls.Add(nudFlow);
            grpThd.Controls.Add(chkHPF);
            grpThd.Controls.Add(lblFhigh);
            grpThd.Controls.Add(nudFhigh);
            grpThd.Controls.Add(chkLPF);

            // grpSfdr
            grpSfdr.Location = new Point(8, 108);
            grpSfdr.Name = "grpSfdr";
            grpSfdr.Size = new Size(272, 95);
            grpSfdr.TabStop = false;
            grpSfdr.Text = "SFDR frequency limits";

            lblSfdrFlow.AutoSize = true;
            lblSfdrFlow.Location = new Point(8, 26);
            lblSfdrFlow.Name = "lblSfdrFlow";
            lblSfdrFlow.Text = "Flow (Hz)";

            nudSfdrFlow.DecimalPlaces = 1;
            nudSfdrFlow.Location = new Point(95, 23);
            nudSfdrFlow.Maximum = new decimal(new int[] { 768000, 0, 0, 0 });
            nudSfdrFlow.Name = "nudSfdrFlow";
            nudSfdrFlow.Size = new Size(96, 23);
            nudSfdrFlow.TabIndex = 4;

            chkSfdrHPF.AutoSize = true;
            chkSfdrHPF.Location = new Point(205, 25);
            chkSfdrHPF.Name = "chkSfdrHPF";
            chkSfdrHPF.Text = "HPF";
            chkSfdrHPF.TabIndex = 5;
            chkSfdrHPF.UseVisualStyleBackColor = true;

            lblSfdrFhigh.AutoSize = true;
            lblSfdrFhigh.Location = new Point(8, 62);
            lblSfdrFhigh.Name = "lblSfdrFhigh";
            lblSfdrFhigh.Text = "Fhigh (Hz)";

            nudSfdrFhigh.DecimalPlaces = 1;
            nudSfdrFhigh.Increment = new decimal(new int[] { 10, 0, 0, 0 });
            nudSfdrFhigh.Location = new Point(95, 59);
            nudSfdrFhigh.Maximum = new decimal(new int[] { 768000, 0, 0, 0 });
            nudSfdrFhigh.Name = "nudSfdrFhigh";
            nudSfdrFhigh.Size = new Size(96, 23);
            nudSfdrFhigh.TabIndex = 6;

            chkSfdrLPF.AutoSize = true;
            chkSfdrLPF.Location = new Point(205, 61);
            chkSfdrLPF.Name = "chkSfdrLPF";
            chkSfdrLPF.Text = "LPF";
            chkSfdrLPF.TabIndex = 7;
            chkSfdrLPF.UseVisualStyleBackColor = true;

            grpSfdr.Controls.Add(lblSfdrFlow);
            grpSfdr.Controls.Add(nudSfdrFlow);
            grpSfdr.Controls.Add(chkSfdrHPF);
            grpSfdr.Controls.Add(lblSfdrFhigh);
            grpSfdr.Controls.Add(nudSfdrFhigh);
            grpSfdr.Controls.Add(chkSfdrLPF);

            // Harmonics number  y=216
            lblHarms.AutoSize = true;
            lblHarms.Location = new Point(9, 216);
            lblHarms.Name = "lblHarms";
            lblHarms.Text = "Harmonics number";

            nudHarms.Location = new Point(145, 212);
            nudHarms.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            nudHarms.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
            nudHarms.Name = "nudHarms";
            nudHarms.Size = new Size(49, 23);
            nudHarms.TabIndex = 8;
            nudHarms.Value = new decimal(new int[] { 5, 0, 0, 0 });

            chkShowHarms.AutoSize = true;
            chkShowHarms.Location = new Point(213, 216);
            chkShowHarms.Name = "chkShowHarms";
            chkShowHarms.Text = "Show";
            chkShowHarms.TabIndex = 9;
            chkShowHarms.UseVisualStyleBackColor = true;

            // THD unit  y=254
            lblUnit.AutoSize = true;
            lblUnit.Location = new Point(9, 256);
            lblUnit.Name = "lblUnit";
            lblUnit.Text = "THD unit";

            rbDb.AutoSize = true;
            rbDb.Location = new Point(95, 254);
            rbDb.Name = "rbDb";
            rbDb.Text = "dB";
            rbDb.TabIndex = 10;
            rbDb.UseVisualStyleBackColor = true;

            rbPct.AutoSize = true;
            rbPct.Location = new Point(155, 254);
            rbPct.Name = "rbPct";
            rbPct.Text = "%";
            rbPct.TabIndex = 11;
            rbPct.UseVisualStyleBackColor = true;

            // Coherent avg  y=288
            lblCohAvg.AutoSize = true;
            lblCohAvg.Location = new Point(9, 290);
            lblCohAvg.Name = "lblCohAvg";
            lblCohAvg.Text = "Coherent avg";

            chkCohAvg.AutoSize = true;
            chkCohAvg.Location = new Point(110, 288);
            chkCohAvg.Name = "chkCohAvg";
            chkCohAvg.Text = "Active";
            chkCohAvg.TabIndex = 12;
            chkCohAvg.UseVisualStyleBackColor = true;

            lblCohAvgHint.AutoSize = true;
            lblCohAvgHint.Location = new Point(178, 290);
            lblCohAvgHint.Name = "lblCohAvgHint";
            lblCohAvgHint.ForeColor = System.Drawing.SystemColors.GrayText;
            lblCohAvgHint.Text = "(uses Avg count)";

            // Buttons  y=320
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(29, 320);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(90, 27);
            btnCancel.TabIndex = 13;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;

            btnOk.DialogResult = DialogResult.OK;
            btnOk.Location = new Point(161, 320);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(90, 27);
            btnOk.TabIndex = 14;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += btnOk_Click;

            // ThdSettingsDialog
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(292, 358);
            Controls.Add(grpThd);
            Controls.Add(grpSfdr);
            Controls.Add(lblHarms);
            Controls.Add(nudHarms);
            Controls.Add(chkShowHarms);
            Controls.Add(lblUnit);
            Controls.Add(rbDb);
            Controls.Add(rbPct);
            Controls.Add(lblCohAvg);
            Controls.Add(chkCohAvg);
            Controls.Add(lblCohAvgHint);
            Controls.Add(btnCancel);
            Controls.Add(btnOk);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ThdSettingsDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "THD Settings";

            ((ISupportInitialize)nudFlow).EndInit();
            ((ISupportInitialize)nudFhigh).EndInit();
            ((ISupportInitialize)nudSfdrFlow).EndInit();
            ((ISupportInitialize)nudSfdrFhigh).EndInit();
            ((ISupportInitialize)nudHarms).EndInit();
            grpThd.ResumeLayout(false);
            grpThd.PerformLayout();
            grpSfdr.ResumeLayout(false);
            grpSfdr.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
#nullable restore
