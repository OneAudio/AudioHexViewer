#nullable enable
using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    internal partial class GeneratorWindow
    {
        private System.ComponentModel.IContainer? components = null;

        // Signal type
        private Label    lblTypeLabel;
        private ComboBox cmbType;

        // GroupBox params
        private System.Windows.Forms.GroupBox grpParams;
        private Label          lblFreqTitle;
        private NumericUpDown  nudF1;
        private Label          lblHz1;
        private Label          lblNoFreq;          // noise: "N/A"
        private CheckBox       chkSynch;
        private Label          lblSynchFreq;
        private Panel          pnlPresets;
        private Button         btnPreset100;
        private Button         btnPreset1k;
        private Button         btnPreset10k;
        private Button         btnPresetNyq;

        private Label          lblAmpTitle;
        private NumericUpDown  nudAmplitude;
        private Label          lblDbfs;

        // Two-tone panel
        private Panel          pnlTwoTone;
        private Label          lblF2;
        private NumericUpDown  nudF2;
        private Label          lblHz2;
        private Label          lblTwoToneNote;

        // Sweep panel
        private Panel          pnlSweep;
        private Label          lblSweepEnd;
        private NumericUpDown  nudSweepEnd;
        private Label          lblSweepEndHz;
        private Label          lblSweepTime;
        private NumericUpDown  nudSweepTime;
        private Label          lblSweepS;
        private CheckBox       chkSweepLog;
        private CheckBox       chkSweepLoop;

        // Output GroupBox
        private System.Windows.Forms.GroupBox grpOutput;
        private Label     lblChannel;
        private RadioButton rbL, rbR, rbLR;
        private CheckBox    chkInvert;
        private CheckBox    chkLoopback;
        private Label       lblSR;
        private Label       lblLoopback;

        // RUN button
        private Button btnRun;

        protected override void Dispose(bool disposing)
        { if (disposing) components?.Dispose(); base.Dispose(disposing); }

        private void InitializeComponent()
        {
            lblTypeLabel  = new Label();
            cmbType       = new ComboBox();
            grpParams     = new System.Windows.Forms.GroupBox();
            lblFreqTitle  = new Label();
            nudF1         = new NumericUpDown();
            lblHz1        = new Label();
            lblNoFreq     = new Label();
            chkSynch      = new CheckBox();
            lblSynchFreq  = new Label();
            pnlPresets    = new Panel();
            btnPreset100  = new Button();
            btnPreset1k   = new Button();
            btnPreset10k  = new Button();
            btnPresetNyq  = new Button();
            lblAmpTitle   = new Label();
            nudAmplitude  = new NumericUpDown();
            lblDbfs       = new Label();
            pnlTwoTone    = new Panel();
            lblF2         = new Label();
            nudF2         = new NumericUpDown();
            lblHz2        = new Label();
            lblTwoToneNote= new Label();
            pnlSweep      = new Panel();
            lblSweepEnd   = new Label();
            nudSweepEnd   = new NumericUpDown();
            lblSweepEndHz = new Label();
            lblSweepTime  = new Label();
            nudSweepTime  = new NumericUpDown();
            lblSweepS     = new Label();
            chkSweepLog   = new CheckBox();
            chkSweepLoop  = new CheckBox();
            grpOutput     = new System.Windows.Forms.GroupBox();
            lblChannel    = new Label();
            rbL           = new RadioButton();
            rbR           = new RadioButton();
            rbLR          = new RadioButton();
            chkInvert     = new CheckBox();
            chkLoopback   = new CheckBox();
            lblSR         = new Label();
            lblLoopback   = new Label();
            btnRun        = new Button();

            ((System.ComponentModel.ISupportInitialize)nudF1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudAmplitude).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudF2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudSweepEnd).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudSweepTime).BeginInit();
            grpParams.SuspendLayout();
            pnlPresets.SuspendLayout();
            pnlTwoTone.SuspendLayout();
            pnlSweep.SuspendLayout();
            grpOutput.SuspendLayout();
            SuspendLayout();

            // ── Signal type row (y=10) ────────────────────────────────────────
            lblTypeLabel.AutoSize = true;
            lblTypeLabel.Location = new Point(12, 14);
            lblTypeLabel.Text     = "Signal type:";

            cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbType.Location      = new Point(100, 10);
            cmbType.Size          = new Size(200, 23);
            cmbType.TabIndex      = 0;

            // ── grpParams (y=44, height=225) ──────────────────────────────────
            grpParams.Location = new Point(8, 44);
            grpParams.Size     = new Size(454, 230);
            grpParams.TabStop  = false;
            grpParams.Text     = "Parameters";

            // ── Frequency side (x=10) ─────────────────────────────────────────
            lblFreqTitle.AutoSize = true;
            lblFreqTitle.Font     = new Font("Segoe UI", 8f);
            lblFreqTitle.ForeColor= System.Drawing.SystemColors.GrayText;
            lblFreqTitle.Location = new Point(10, 22);
            lblFreqTitle.Text     = "FREQUENCY";

            nudF1.DecimalPlaces = 2;
            nudF1.Font          = new Font("Segoe UI", 14f, FontStyle.Bold);
            nudF1.Location      = new Point(10, 38);
            nudF1.Maximum       = 24000m;
            nudF1.Minimum       = 1m;
            nudF1.Size          = new Size(162, 34);
            nudF1.TabIndex      = 1;
            nudF1.Value         = 1000m;

            lblHz1.AutoSize  = true;
            lblHz1.Font      = new Font("Segoe UI", 10f);
            lblHz1.Location  = new Point(175, 44);
            lblHz1.Text      = "Hz";

            lblNoFreq.AutoSize  = true;
            lblNoFreq.Font      = new Font("Segoe UI", 9f, FontStyle.Italic);
            lblNoFreq.ForeColor = System.Drawing.SystemColors.GrayText;
            lblNoFreq.Location  = new Point(10, 52);
            lblNoFreq.Text      = "Broadband — no frequency";
            lblNoFreq.Visible   = false;

            // Preset buttons
            pnlPresets.Location = new Point(10, 80);
            pnlPresets.Size     = new Size(220, 26);

            btnPreset100.Location = new Point(0,   0);
            btnPreset100.Size     = new Size(48, 24);
            btnPreset100.Text     = "100 Hz";
            btnPreset100.Font     = new Font("Segoe UI", 7.5f);
            btnPreset100.FlatStyle= FlatStyle.Popup;

            btnPreset1k.Location  = new Point(52, 0);
            btnPreset1k.Size      = new Size(40, 24);
            btnPreset1k.Text      = "1 kHz";
            btnPreset1k.Font      = new Font("Segoe UI", 7.5f);
            btnPreset1k.FlatStyle = FlatStyle.Popup;

            btnPreset10k.Location = new Point(96, 0);
            btnPreset10k.Size     = new Size(46, 24);
            btnPreset10k.Text     = "10 kHz";
            btnPreset10k.Font     = new Font("Segoe UI", 7.5f);
            btnPreset10k.FlatStyle= FlatStyle.Popup;

            btnPresetNyq.Location = new Point(146, 0);
            btnPresetNyq.Size     = new Size(48, 24);
            btnPresetNyq.Text     = "Nyq/2";
            btnPresetNyq.Font     = new Font("Segoe UI", 7.5f);
            btnPresetNyq.FlatStyle= FlatStyle.Popup;

            pnlPresets.Controls.Add(btnPreset100);
            pnlPresets.Controls.Add(btnPreset1k);
            pnlPresets.Controls.Add(btnPreset10k);
            pnlPresets.Controls.Add(btnPresetNyq);

            chkSynch.AutoSize = true;
            chkSynch.Location = new Point(10, 112);
            chkSynch.Text     = "Synch (coherent frequency)";
            chkSynch.TabIndex = 2;
            chkSynch.UseVisualStyleBackColor = true;

            lblSynchFreq.AutoSize  = true;
            lblSynchFreq.Font      = new Font("Segoe UI", 8f);
            lblSynchFreq.ForeColor = System.Drawing.SystemColors.GrayText;
            lblSynchFreq.Location  = new Point(10, 134);
            lblSynchFreq.Text      = "→ 1000.12 Hz";
            lblSynchFreq.Visible   = false;

            // ── Amplitude side (x=245) ────────────────────────────────────────
            lblAmpTitle.AutoSize  = true;
            lblAmpTitle.Font      = new Font("Segoe UI", 8f);
            lblAmpTitle.ForeColor = System.Drawing.SystemColors.GrayText;
            lblAmpTitle.Location  = new Point(258, 22);
            lblAmpTitle.Text      = "AMPLITUDE";

            nudAmplitude.DecimalPlaces = 1;
            nudAmplitude.Font          = new Font("Segoe UI", 14f, FontStyle.Bold);
            nudAmplitude.Increment     = 0.5m;
            nudAmplitude.Location      = new Point(258, 38);
            nudAmplitude.Maximum       = 0m;
            nudAmplitude.Minimum       = -180m;
            nudAmplitude.Size          = new Size(118, 34);
            nudAmplitude.TabIndex      = 3;
            nudAmplitude.Value         = -12m;

            lblDbfs.AutoSize = true;
            lblDbfs.Font     = new Font("Segoe UI", 10f);
            lblDbfs.Location = new Point(380, 44);
            lblDbfs.Text     = "dBFS";

            // ── pnlTwoTone (y=155) ────────────────────────────────────────────
            pnlTwoTone.Location = new Point(0, 155);
            pnlTwoTone.Size     = new Size(454, 65);
            pnlTwoTone.Visible  = false;

            lblF2.AutoSize  = true;
            lblF2.Font      = new Font("Segoe UI", 8f);
            lblF2.ForeColor = System.Drawing.SystemColors.GrayText;
            lblF2.Location  = new Point(10, 2);
            lblF2.Text      = "F2  (SECOND TONE)";

            nudF2.DecimalPlaces = 2;
            nudF2.Font          = new Font("Segoe UI", 12f, FontStyle.Bold);
            nudF2.Location      = new Point(10, 18);
            nudF2.Maximum       = 24000m;
            nudF2.Minimum       = 1m;
            nudF2.Size          = new Size(140, 30);
            nudF2.TabIndex      = 4;
            nudF2.Value         = 2000m;

            lblHz2.AutoSize  = true;
            lblHz2.Font      = new Font("Segoe UI", 10f);
            lblHz2.Location  = new Point(154, 23);
            lblHz2.Text      = "Hz";

            lblTwoToneNote.AutoSize  = true;
            lblTwoToneNote.Font      = new Font("Segoe UI", 7.5f, FontStyle.Italic);
            lblTwoToneNote.ForeColor = System.Drawing.SystemColors.GrayText;
            lblTwoToneNote.Location  = new Point(220, 24);
            lblTwoToneNote.Text      = "Each tone at amplitude − 6 dB";

            pnlTwoTone.Controls.Add(lblF2);
            pnlTwoTone.Controls.Add(nudF2);
            pnlTwoTone.Controls.Add(lblHz2);
            pnlTwoTone.Controls.Add(lblTwoToneNote);

            // ── pnlSweep (y=155) ─────────────────────────────────────────────
            pnlSweep.Location = new Point(0, 155);
            pnlSweep.Size     = new Size(454, 65);
            pnlSweep.Visible  = false;

            lblSweepEnd.AutoSize = true;
            lblSweepEnd.Location = new Point(10, 6);
            lblSweepEnd.Text     = "End:";

            nudSweepEnd.DecimalPlaces = 0;
            nudSweepEnd.Location      = new Point(48, 2);
            nudSweepEnd.Maximum       = 96000m;
            nudSweepEnd.Minimum       = 1m;
            nudSweepEnd.Size          = new Size(100, 23);
            nudSweepEnd.TabIndex      = 5;
            nudSweepEnd.Value         = 20000m;

            lblSweepEndHz.AutoSize = true;
            lblSweepEndHz.Location = new Point(152, 6);
            lblSweepEndHz.Text     = "Hz";

            lblSweepTime.AutoSize = true;
            lblSweepTime.Location = new Point(185, 6);
            lblSweepTime.Text     = "Duration:";

            nudSweepTime.DecimalPlaces = 1;
            nudSweepTime.Location      = new Point(248, 2);
            nudSweepTime.Maximum       = 600m;
            nudSweepTime.Minimum       = 0.1m;
            nudSweepTime.Size          = new Size(70, 23);
            nudSweepTime.TabIndex      = 6;
            nudSweepTime.Value         = 5m;

            lblSweepS.AutoSize = true;
            lblSweepS.Location = new Point(322, 6);
            lblSweepS.Text     = "s";

            chkSweepLog.AutoSize = true;
            chkSweepLog.Checked  = true;
            chkSweepLog.Location = new Point(10, 36);
            chkSweepLog.Text     = "Log";
            chkSweepLog.TabIndex = 7;
            chkSweepLog.UseVisualStyleBackColor = true;

            chkSweepLoop.AutoSize = true;
            chkSweepLoop.Location = new Point(80, 36);
            chkSweepLoop.Text     = "Loop";
            chkSweepLoop.TabIndex = 8;
            chkSweepLoop.UseVisualStyleBackColor = true;

            pnlSweep.Controls.Add(lblSweepEnd);
            pnlSweep.Controls.Add(nudSweepEnd);
            pnlSweep.Controls.Add(lblSweepEndHz);
            pnlSweep.Controls.Add(lblSweepTime);
            pnlSweep.Controls.Add(nudSweepTime);
            pnlSweep.Controls.Add(lblSweepS);
            pnlSweep.Controls.Add(chkSweepLog);
            pnlSweep.Controls.Add(chkSweepLoop);

            grpParams.Controls.Add(lblFreqTitle);
            grpParams.Controls.Add(nudF1);
            grpParams.Controls.Add(lblHz1);
            grpParams.Controls.Add(lblNoFreq);
            grpParams.Controls.Add(pnlPresets);
            grpParams.Controls.Add(chkSynch);
            grpParams.Controls.Add(lblSynchFreq);
            grpParams.Controls.Add(lblAmpTitle);
            grpParams.Controls.Add(nudAmplitude);
            grpParams.Controls.Add(lblDbfs);
            grpParams.Controls.Add(pnlTwoTone);
            grpParams.Controls.Add(pnlSweep);

            // ── grpOutput (y=282) ─────────────────────────────────────────────
            grpOutput.Location = new Point(8, 282);
            grpOutput.Size     = new Size(454, 105);
            grpOutput.TabStop  = false;
            grpOutput.Text     = "Output";

            lblChannel.AutoSize = true;
            lblChannel.Location = new Point(10, 26);
            lblChannel.Text     = "Channel:";

            rbL.AutoSize = true;  rbL.Checked  = true;
            rbL.Location = new Point(78, 24);   rbL.Text = "L";  rbL.TabIndex = 9;
            rbL.UseVisualStyleBackColor = true;

            rbR.AutoSize = true;
            rbR.Location = new Point(116, 24);  rbR.Text = "R";  rbR.TabIndex = 10;
            rbR.UseVisualStyleBackColor = true;

            rbLR.AutoSize = true;
            rbLR.Location = new Point(154, 24); rbLR.Text = "L+R"; rbLR.TabIndex = 11;
            rbLR.UseVisualStyleBackColor = true;

            chkInvert.AutoSize = true;  chkInvert.Enabled = false;
            chkInvert.Location = new Point(218, 24);
            chkInvert.Text     = "Invert R";  chkInvert.TabIndex = 12;
            chkInvert.UseVisualStyleBackColor = true;

            chkLoopback.AutoSize = true;
            chkLoopback.Location = new Point(10, 52);
            chkLoopback.Text     = "Loopback Test  (no device)";
            chkLoopback.TabIndex = 13;
            chkLoopback.UseVisualStyleBackColor = true;

            lblSR.AutoSize  = true;
            lblSR.ForeColor = System.Drawing.SystemColors.GrayText;
            lblSR.Location  = new Point(10, 78);
            lblSR.Text      = "SR: —";

            lblLoopback.AutoSize  = true;
            lblLoopback.ForeColor = System.Drawing.Color.DodgerBlue;
            lblLoopback.Font      = new Font("Segoe UI", 8.5f, FontStyle.Italic);
            lblLoopback.Location  = new Point(270, 78);
            lblLoopback.Text      = "Loopback mode";
            lblLoopback.Visible   = false;

            grpOutput.Controls.Add(lblChannel);
            grpOutput.Controls.Add(rbL);
            grpOutput.Controls.Add(rbR);
            grpOutput.Controls.Add(rbLR);
            grpOutput.Controls.Add(chkInvert);
            grpOutput.Controls.Add(chkLoopback);
            grpOutput.Controls.Add(lblSR);
            grpOutput.Controls.Add(lblLoopback);

            // ── RUN button ────────────────────────────────────────────────────
            btnRun.Location   = new Point(316, 396);
            btnRun.Size       = new Size(146, 42);
            btnRun.TabIndex   = 14;
            btnRun.Text       = "▶  RUN";
            btnRun.BackColor  = Color.SeaGreen;
            btnRun.ForeColor  = Color.White;
            btnRun.FlatStyle  = FlatStyle.Flat;
            btnRun.FlatAppearance.BorderSize = 0;
            btnRun.Font       = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnRun.UseVisualStyleBackColor = false;

            // ── Form ──────────────────────────────────────────────────────────
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode       = AutoScaleMode.Font;
            ClientSize          = new System.Drawing.Size(470, 450);
            Controls.Add(lblTypeLabel);
            Controls.Add(cmbType);
            Controls.Add(grpParams);
            Controls.Add(grpOutput);
            Controls.Add(btnRun);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            MinimizeBox     = true;
            MaximizeBox     = false;
            StartPosition   = FormStartPosition.Manual;
            Text            = "Generator";

            ((System.ComponentModel.ISupportInitialize)nudF1).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudAmplitude).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudF2).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudSweepEnd).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudSweepTime).EndInit();
            grpParams.ResumeLayout(false);
            grpParams.PerformLayout();
            pnlPresets.ResumeLayout(false);
            pnlTwoTone.ResumeLayout(false);
            pnlTwoTone.PerformLayout();
            pnlSweep.ResumeLayout(false);
            pnlSweep.PerformLayout();
            grpOutput.ResumeLayout(false);
            grpOutput.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
#nullable restore
