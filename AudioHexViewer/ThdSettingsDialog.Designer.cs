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

        // Harmonics group
        private GroupBox grpHarmonics;
        private Label lblHarms;
        private NumericUpDown nudHarms;
        private CheckBox chkShowHarms;
        private CheckBox chkShowPhase;
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
            grpThd       = new GroupBox();
            lblFlow      = new Label();
            nudFlow      = new NumericUpDown();
            chkHPF       = new CheckBox();
            lblFhigh     = new Label();
            nudFhigh     = new NumericUpDown();
            chkLPF       = new CheckBox();

            grpSfdr      = new GroupBox();
            lblSfdrFlow  = new Label();
            nudSfdrFlow  = new NumericUpDown();
            chkSfdrHPF   = new CheckBox();
            lblSfdrFhigh = new Label();
            nudSfdrFhigh = new NumericUpDown();
            chkSfdrLPF   = new CheckBox();

            grpHarmonics  = new GroupBox();
            lblHarms      = new Label();
            nudHarms      = new NumericUpDown();
            chkShowHarms  = new CheckBox();
            chkShowPhase  = new CheckBox();
            lblUnit       = new Label();
            rbDb          = new RadioButton();
            rbPct         = new RadioButton();
            lblCohAvg     = new Label();
            chkCohAvg     = new CheckBox();
            lblCohAvgHint = new Label();

            btnOk     = new Button();
            btnCancel = new Button();

            ((ISupportInitialize)nudFlow).BeginInit();
            ((ISupportInitialize)nudFhigh).BeginInit();
            ((ISupportInitialize)nudSfdrFlow).BeginInit();
            ((ISupportInitialize)nudSfdrFhigh).BeginInit();
            ((ISupportInitialize)nudHarms).BeginInit();
            grpThd.SuspendLayout();
            grpSfdr.SuspendLayout();
            grpHarmonics.SuspendLayout();
            SuspendLayout();

            // ── grpThd (y=6) ─────────────────────────────────────────────────
            grpThd.Location = new Point(8, 6);
            grpThd.Size     = new Size(272, 95);
            grpThd.TabStop  = false;
            grpThd.Text     = "THD frequency limits";

            lblFlow.AutoSize = true;
            lblFlow.Location = new Point(8, 26);
            lblFlow.Text     = "Flow (Hz)";

            nudFlow.DecimalPlaces = 1;
            nudFlow.Location      = new Point(95, 23);
            nudFlow.Maximum       = new decimal(new int[] { 768000, 0, 0, 0 });
            nudFlow.Size          = new Size(96, 23);
            nudFlow.TabIndex      = 0;

            chkHPF.AutoSize = true;
            chkHPF.Location = new Point(205, 25);
            chkHPF.Text     = "HPF";
            chkHPF.TabIndex = 1;
            chkHPF.UseVisualStyleBackColor = true;

            lblFhigh.AutoSize = true;
            lblFhigh.Location = new Point(8, 62);
            lblFhigh.Text     = "Fhigh (Hz)";

            nudFhigh.DecimalPlaces = 1;
            nudFhigh.Increment     = new decimal(new int[] { 10, 0, 0, 0 });
            nudFhigh.Location      = new Point(95, 59);
            nudFhigh.Maximum       = new decimal(new int[] { 768000, 0, 0, 0 });
            nudFhigh.Size          = new Size(96, 23);
            nudFhigh.TabIndex      = 2;

            chkLPF.AutoSize = true;
            chkLPF.Location = new Point(205, 61);
            chkLPF.Text     = "LPF";
            chkLPF.TabIndex = 3;
            chkLPF.UseVisualStyleBackColor = true;

            grpThd.Controls.Add(lblFlow);
            grpThd.Controls.Add(nudFlow);
            grpThd.Controls.Add(chkHPF);
            grpThd.Controls.Add(lblFhigh);
            grpThd.Controls.Add(nudFhigh);
            grpThd.Controls.Add(chkLPF);

            // ── grpSfdr (y=109) ───────────────────────────────────────────────
            grpSfdr.Location = new Point(8, 109);
            grpSfdr.Size     = new Size(272, 95);
            grpSfdr.TabStop  = false;
            grpSfdr.Text     = "SFDR frequency limits";

            lblSfdrFlow.AutoSize = true;
            lblSfdrFlow.Location = new Point(8, 26);
            lblSfdrFlow.Text     = "Flow (Hz)";

            nudSfdrFlow.DecimalPlaces = 1;
            nudSfdrFlow.Location      = new Point(95, 23);
            nudSfdrFlow.Maximum       = new decimal(new int[] { 768000, 0, 0, 0 });
            nudSfdrFlow.Size          = new Size(96, 23);
            nudSfdrFlow.TabIndex      = 4;

            chkSfdrHPF.AutoSize = true;
            chkSfdrHPF.Location = new Point(205, 25);
            chkSfdrHPF.Text     = "HPF";
            chkSfdrHPF.TabIndex = 5;
            chkSfdrHPF.UseVisualStyleBackColor = true;

            lblSfdrFhigh.AutoSize = true;
            lblSfdrFhigh.Location = new Point(8, 62);
            lblSfdrFhigh.Text     = "Fhigh (Hz)";

            nudSfdrFhigh.DecimalPlaces = 1;
            nudSfdrFhigh.Increment     = new decimal(new int[] { 10, 0, 0, 0 });
            nudSfdrFhigh.Location      = new Point(95, 59);
            nudSfdrFhigh.Maximum       = new decimal(new int[] { 768000, 0, 0, 0 });
            nudSfdrFhigh.Size          = new Size(96, 23);
            nudSfdrFhigh.TabIndex      = 6;

            chkSfdrLPF.AutoSize = true;
            chkSfdrLPF.Location = new Point(205, 61);
            chkSfdrLPF.Text     = "LPF";
            chkSfdrLPF.TabIndex = 7;
            chkSfdrLPF.UseVisualStyleBackColor = true;

            grpSfdr.Controls.Add(lblSfdrFlow);
            grpSfdr.Controls.Add(nudSfdrFlow);
            grpSfdr.Controls.Add(chkSfdrHPF);
            grpSfdr.Controls.Add(lblSfdrFhigh);
            grpSfdr.Controls.Add(nudSfdrFhigh);
            grpSfdr.Controls.Add(chkSfdrLPF);

            // ── grpHarmonics (y=212) ──────────────────────────────────────────
            // Regroupe : Harmonics number+Show+Phase / THD unit / Coherent avg
            grpHarmonics.Location = new Point(8, 212);
            grpHarmonics.Size     = new Size(272, 120);
            grpHarmonics.TabStop  = false;
            grpHarmonics.Text     = "Harmonics";

            // Row 1 — number + Show + Phase  (y=24)
            lblHarms.AutoSize = true;
            lblHarms.Location = new Point(8, 26);
            lblHarms.Text     = "Number";

            nudHarms.Location = new Point(72, 23);
            nudHarms.Maximum  = new decimal(new int[] { 20, 0, 0, 0 });
            nudHarms.Minimum  = new decimal(new int[] { 2, 0, 0, 0 });
            nudHarms.Size     = new Size(44, 23);
            nudHarms.TabIndex = 8;
            nudHarms.Value    = new decimal(new int[] { 5, 0, 0, 0 });

            chkShowHarms.AutoSize = true;
            chkShowHarms.Location = new Point(128, 26);
            chkShowHarms.Text     = "Show";
            chkShowHarms.TabIndex = 9;
            chkShowHarms.UseVisualStyleBackColor = true;

            chkShowPhase.AutoSize = true;
            chkShowPhase.Location = new Point(192, 26);
            chkShowPhase.Text     = "Phase";
            chkShowPhase.TabIndex = 10;
            chkShowPhase.UseVisualStyleBackColor = true;

            // Row 2 — Unit  (y=56)
            lblUnit.AutoSize = true;
            lblUnit.Location = new Point(8, 59);
            lblUnit.Text     = "THD unit";

            rbDb.AutoSize = true;
            rbDb.Location = new Point(95, 57);
            rbDb.Text     = "dB";
            rbDb.TabIndex = 11;
            rbDb.UseVisualStyleBackColor = true;

            rbPct.AutoSize = true;
            rbPct.Location = new Point(150, 57);
            rbPct.Text     = "%";
            rbPct.TabIndex = 12;
            rbPct.UseVisualStyleBackColor = true;

            // Row 3 — Coherent avg  (y=88)
            lblCohAvg.AutoSize = true;
            lblCohAvg.Location = new Point(8, 91);
            lblCohAvg.Text     = "Coherent avg";

            chkCohAvg.AutoSize = true;
            chkCohAvg.Location = new Point(103, 89);
            chkCohAvg.Text     = "Active";
            chkCohAvg.TabIndex = 13;
            chkCohAvg.UseVisualStyleBackColor = true;

            lblCohAvgHint.AutoSize   = true;
            lblCohAvgHint.Location   = new Point(166, 91);
            lblCohAvgHint.ForeColor  = System.Drawing.SystemColors.GrayText;
            lblCohAvgHint.Text       = "(uses Avg count)";

            grpHarmonics.Controls.Add(lblHarms);
            grpHarmonics.Controls.Add(nudHarms);
            grpHarmonics.Controls.Add(chkShowHarms);
            grpHarmonics.Controls.Add(chkShowPhase);
            grpHarmonics.Controls.Add(lblUnit);
            grpHarmonics.Controls.Add(rbDb);
            grpHarmonics.Controls.Add(rbPct);
            grpHarmonics.Controls.Add(lblCohAvg);
            grpHarmonics.Controls.Add(chkCohAvg);
            grpHarmonics.Controls.Add(lblCohAvgHint);

            // ── Buttons (y=342) ───────────────────────────────────────────────
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location     = new Point(29, 342);
            btnCancel.Size         = new Size(90, 27);
            btnCancel.TabIndex     = 14;
            btnCancel.Text         = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;

            btnOk.DialogResult = DialogResult.OK;
            btnOk.Location     = new Point(161, 342);
            btnOk.Size         = new Size(90, 27);
            btnOk.TabIndex     = 15;
            btnOk.Text         = "OK";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += btnOk_Click;

            // ── ThdSettingsDialog ─────────────────────────────────────────────
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode       = AutoScaleMode.Font;
            ClientSize          = new Size(292, 380);
            Controls.Add(grpThd);
            Controls.Add(grpSfdr);
            Controls.Add(grpHarmonics);
            Controls.Add(btnCancel);
            Controls.Add(btnOk);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            StartPosition   = FormStartPosition.CenterParent;
            Text            = "THD Settings";

            ((ISupportInitialize)nudFlow).EndInit();
            ((ISupportInitialize)nudFhigh).EndInit();
            ((ISupportInitialize)nudSfdrFlow).EndInit();
            ((ISupportInitialize)nudSfdrFhigh).EndInit();
            ((ISupportInitialize)nudHarms).EndInit();
            grpThd.ResumeLayout(false);
            grpThd.PerformLayout();
            grpSfdr.ResumeLayout(false);
            grpSfdr.PerformLayout();
            grpHarmonics.ResumeLayout(false);
            grpHarmonics.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
#nullable restore
