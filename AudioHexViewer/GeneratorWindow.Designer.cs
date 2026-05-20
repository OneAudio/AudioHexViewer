#nullable enable
using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    internal partial class GeneratorWindow
    {
        private System.ComponentModel.IContainer? components = null;

        private Label lblTypeLabel; private ComboBox cmbType;
        private System.Windows.Forms.GroupBox grpParams;
        private Label lblFreqTitle; private NumericUpDown nudF1; private Label lblHz1;
        private Label lblNoFreq; private CheckBox chkSynch; private Label lblSynchFreq;
        private Panel pnlPresets;
        private Button btnPreset100, btnPreset1k, btnPreset10k, btnPreset100k;
        private Label lblAmpTitle; private NumericUpDown nudAmplitude; private Label lblDbfs;

        // Sweep
        private Panel pnlSweep;
        private Label lblSweepEndT, lblSweepTimeT;
        private NumericUpDown nudSweepEnd, nudSweepTime;
        private Label lblSweepEndHz, lblSweepS;
        private CheckBox chkSweepLog, chkSweepLoop;

        // Two-tone
        private Panel pnlTwoTone;
        private Label lblIMDLabel; private ComboBox cmbIMDPreset;
        private Label lblF2Title; private NumericUpDown nudF2; private Label lblHz2;
        private Label lblAmpF2Title; private NumericUpDown nudAmplitudeF2; private Label lblDbfsF2;

        // Burst
        private Panel pnlBurst;
        private Label lblBurstOn, lblBurstOff;
        private NumericUpDown nudBurstOn, nudBurstOff;
        private RadioButton rbBurstOnPer, rbBurstOnMs, rbBurstOffPer, rbBurstOffMs;
        private Label lblBurstWin; private ComboBox cmbBurstWindow;
        private CheckBox chkBurstLoop;

        // Dither
        private Panel pnlDither;
        private CheckBox       chkDitherEnable;
        private NumericUpDown  nudDitherBits;
        private Label          lblDitherBits;

        // Output
        private System.Windows.Forms.GroupBox grpOutput;
        private Label lblChannel;
        private RadioButton rbL, rbR, rbLR;
        private CheckBox chkInvert, chkLoopback;
        private Label lblSR, lblLoopback;
        private Button btnRun;

        protected override void Dispose(bool disposing){if(disposing)components?.Dispose();base.Dispose(disposing);}

        private void InitializeComponent()
        {
            lblTypeLabel=new Label(); cmbType=new ComboBox(); grpParams=new System.Windows.Forms.GroupBox();
            lblFreqTitle=new Label(); nudF1=new NumericUpDown(); lblHz1=new Label();
            lblNoFreq=new Label(); chkSynch=new CheckBox(); lblSynchFreq=new Label();
            pnlPresets=new Panel();
            btnPreset100=new Button(); btnPreset1k=new Button(); btnPreset10k=new Button(); btnPreset100k=new Button();
            lblAmpTitle=new Label(); nudAmplitude=new NumericUpDown(); lblDbfs=new Label();
            pnlSweep=new Panel(); lblSweepEndT=new Label(); nudSweepEnd=new NumericUpDown();
            lblSweepEndHz=new Label(); lblSweepTimeT=new Label(); nudSweepTime=new NumericUpDown();
            lblSweepS=new Label(); chkSweepLog=new CheckBox(); chkSweepLoop=new CheckBox();
            pnlTwoTone=new Panel(); lblIMDLabel=new Label(); cmbIMDPreset=new ComboBox();
            lblF2Title=new Label(); nudF2=new NumericUpDown(); lblHz2=new Label();
            lblAmpF2Title=new Label(); nudAmplitudeF2=new NumericUpDown(); lblDbfsF2=new Label();
            pnlBurst=new Panel(); lblBurstOn=new Label(); nudBurstOn=new NumericUpDown();
            rbBurstOnPer=new RadioButton(); rbBurstOnMs=new RadioButton();
            lblBurstOff=new Label(); nudBurstOff=new NumericUpDown();
            rbBurstOffPer=new RadioButton(); rbBurstOffMs=new RadioButton();
            lblBurstWin=new Label(); cmbBurstWindow=new ComboBox(); chkBurstLoop=new CheckBox();
            pnlDither=new Panel(); chkDitherEnable=new CheckBox();
            nudDitherBits=new NumericUpDown(); lblDitherBits=new Label();
            grpOutput=new System.Windows.Forms.GroupBox();
            lblChannel=new Label(); rbL=new RadioButton(); rbR=new RadioButton(); rbLR=new RadioButton();
            chkInvert=new CheckBox(); chkLoopback=new CheckBox(); lblSR=new Label(); lblLoopback=new Label();
            btnRun=new Button();

            var allNud=new System.ComponentModel.ISupportInitialize[]{nudF1,nudAmplitude,nudSweepEnd,nudSweepTime,nudF2,nudAmplitudeF2,nudBurstOn,nudBurstOff,nudDitherBits};
            foreach(var n in allNud) n.BeginInit();
            grpParams.SuspendLayout(); pnlPresets.SuspendLayout();
            pnlSweep.SuspendLayout(); pnlTwoTone.SuspendLayout();
            pnlBurst.SuspendLayout(); pnlDither.SuspendLayout(); grpOutput.SuspendLayout();
            SuspendLayout();

            var grayS=new Font("Segoe UI",8f); var big=new Font("Segoe UI",14f,FontStyle.Bold);
            var med=new Font("Segoe UI",12f,FontStyle.Bold); var std=new Font("Segoe UI",9f);

            // ── Type ──────────────────────────────────────────────────────────
            lblTypeLabel.AutoSize=true; lblTypeLabel.Location=new Point(12,14); lblTypeLabel.Text="Signal type:";
            cmbType.DropDownStyle=ComboBoxStyle.DropDownList; cmbType.Location=new Point(100,10); cmbType.Size=new Size(210,23); cmbType.TabIndex=0;

            // ── grpParams ─────────────────────────────────────────────────────
            grpParams.Location=new Point(8,44); grpParams.Size=new Size(384,310); grpParams.TabStop=false; grpParams.Text="Parameters";

            lblFreqTitle.AutoSize=true; lblFreqTitle.Font=grayS; lblFreqTitle.ForeColor=SystemColors.GrayText; lblFreqTitle.Location=new Point(10,22); lblFreqTitle.Text="FREQUENCY";
            nudF1.DecimalPlaces=2; nudF1.Font=big; nudF1.Location=new Point(10,38); nudF1.Maximum=24000m; nudF1.Minimum=1m; nudF1.Size=new Size(162,34); nudF1.TabIndex=1; nudF1.Value=1000m;
            lblHz1.AutoSize=true; lblHz1.Font=new Font("Segoe UI",10f); lblHz1.Location=new Point(175,44); lblHz1.Text="Hz";
            lblNoFreq.AutoSize=true; lblNoFreq.Font=new Font("Segoe UI",9f,FontStyle.Italic); lblNoFreq.ForeColor=SystemColors.GrayText; lblNoFreq.Location=new Point(10,52); lblNoFreq.Text="Broadband — no frequency"; lblNoFreq.Visible=false;

            pnlPresets.Location=new Point(10,80); pnlPresets.Size=new Size(232,26);
            void Preset(Button b,int x,int w,string t){b.Location=new Point(x,0);b.Size=new Size(w,24);b.Text=t;b.Font=new Font("Segoe UI",7.5f);b.FlatStyle=FlatStyle.Popup;}
            Preset(btnPreset100,0,52,"100 Hz"); Preset(btnPreset1k,56,44,"1 kHz"); Preset(btnPreset10k,104,50,"10 kHz"); Preset(btnPreset100k,158,62,"100 kHz");
            pnlPresets.Controls.AddRange(new Control[]{btnPreset100,btnPreset1k,btnPreset10k,btnPreset100k});

            chkSynch.AutoSize=true; chkSynch.Location=new Point(10,112); chkSynch.Text="Synch (coherent)"; chkSynch.TabIndex=2; chkSynch.UseVisualStyleBackColor=true;
            lblSynchFreq.AutoSize=true; lblSynchFreq.Font=grayS; lblSynchFreq.ForeColor=SystemColors.GrayText; lblSynchFreq.Location=new Point(10,134); lblSynchFreq.Text="→ 1000.12 Hz"; lblSynchFreq.Visible=false;

            lblAmpTitle.AutoSize=true; lblAmpTitle.Font=grayS; lblAmpTitle.ForeColor=SystemColors.GrayText; lblAmpTitle.Location=new Point(218,22); lblAmpTitle.Text="AMPLITUDE";
            nudAmplitude.DecimalPlaces=1; nudAmplitude.Font=big; nudAmplitude.Increment=0.5m; nudAmplitude.Location=new Point(218,38); nudAmplitude.Maximum=0m; nudAmplitude.Minimum=-180m; nudAmplitude.Size=new Size(108,34); nudAmplitude.TabIndex=3; nudAmplitude.Value=-12m;
            lblDbfs.AutoSize=true; lblDbfs.Font=new Font("Segoe UI",10f); lblDbfs.Location=new Point(330,44); lblDbfs.Text="dBFS";

            // ── pnlSweep (y=155, h=115) ───────────────────────────────────────
            pnlSweep.Location=new Point(0,155); pnlSweep.Size=new Size(384,115); pnlSweep.Visible=false;
            lblSweepEndT.AutoSize=true; lblSweepEndT.Font=grayS; lblSweepEndT.ForeColor=SystemColors.GrayText; lblSweepEndT.Location=new Point(10,2); lblSweepEndT.Text="END";
            nudSweepEnd.DecimalPlaces=0; nudSweepEnd.Font=big; nudSweepEnd.Location=new Point(10,18); nudSweepEnd.Maximum=96000m; nudSweepEnd.Minimum=1m; nudSweepEnd.Size=new Size(150,34); nudSweepEnd.TabIndex=4; nudSweepEnd.Value=20000m;
            lblSweepEndHz.AutoSize=true; lblSweepEndHz.Font=new Font("Segoe UI",10f); lblSweepEndHz.Location=new Point(164,24); lblSweepEndHz.Text="Hz";
            lblSweepTimeT.AutoSize=true; lblSweepTimeT.Font=grayS; lblSweepTimeT.ForeColor=SystemColors.GrayText; lblSweepTimeT.Location=new Point(220,2); lblSweepTimeT.Text="DURATION";
            nudSweepTime.DecimalPlaces=1; nudSweepTime.Font=big; nudSweepTime.Location=new Point(220,18); nudSweepTime.Maximum=600m; nudSweepTime.Minimum=0.1m; nudSweepTime.Size=new Size(110,34); nudSweepTime.TabIndex=5; nudSweepTime.Value=5m;
            lblSweepS.AutoSize=true; lblSweepS.Font=new Font("Segoe UI",10f); lblSweepS.Location=new Point(334,24); lblSweepS.Text="s";
            chkSweepLog.AutoSize=true; chkSweepLog.Checked=true; chkSweepLog.Location=new Point(10,60); chkSweepLog.Text="Logarithmique"; chkSweepLog.TabIndex=6; chkSweepLog.UseVisualStyleBackColor=true;
            chkSweepLoop.AutoSize=true; chkSweepLoop.Location=new Point(140,60); chkSweepLoop.Text="Loop"; chkSweepLoop.TabIndex=7; chkSweepLoop.UseVisualStyleBackColor=true;
            pnlSweep.Controls.AddRange(new Control[]{lblSweepEndT,nudSweepEnd,lblSweepEndHz,lblSweepTimeT,nudSweepTime,lblSweepS,chkSweepLog,chkSweepLoop});

            // ── pnlTwoTone (y=155, h=120) ─────────────────────────────────────
            pnlTwoTone.Location=new Point(0,155); pnlTwoTone.Size=new Size(384,120); pnlTwoTone.Visible=false;
            lblIMDLabel.AutoSize=true; lblIMDLabel.Location=new Point(10,4); lblIMDLabel.Text="IMD preset:";
            cmbIMDPreset.DropDownStyle=ComboBoxStyle.DropDownList; cmbIMDPreset.Location=new Point(85,1); cmbIMDPreset.Size=new Size(285,23); cmbIMDPreset.TabIndex=8;
            lblF2Title.AutoSize=true; lblF2Title.Font=grayS; lblF2Title.ForeColor=SystemColors.GrayText; lblF2Title.Location=new Point(10,33); lblF2Title.Text="F2";
            nudF2.DecimalPlaces=2; nudF2.Font=med; nudF2.Location=new Point(10,50); nudF2.Maximum=24000m; nudF2.Minimum=1m; nudF2.Size=new Size(140,30); nudF2.TabIndex=9; nudF2.Value=2000m;
            lblHz2.AutoSize=true; lblHz2.Font=new Font("Segoe UI",10f); lblHz2.Location=new Point(153,55); lblHz2.Text="Hz";
            lblAmpF2Title.AutoSize=true; lblAmpF2Title.Font=grayS; lblAmpF2Title.ForeColor=SystemColors.GrayText; lblAmpF2Title.Location=new Point(218,33); lblAmpF2Title.Text="AMPLITUDE F2";
            nudAmplitudeF2.DecimalPlaces=1; nudAmplitudeF2.Font=med; nudAmplitudeF2.Increment=0.5m; nudAmplitudeF2.Location=new Point(218,50); nudAmplitudeF2.Maximum=0m; nudAmplitudeF2.Minimum=-180m; nudAmplitudeF2.Size=new Size(108,30); nudAmplitudeF2.TabIndex=10; nudAmplitudeF2.Value=-12m;
            lblDbfsF2.AutoSize=true; lblDbfsF2.Font=std; lblDbfsF2.Location=new Point(330,55); lblDbfsF2.Text="dBFS";
            pnlTwoTone.Controls.AddRange(new Control[]{lblIMDLabel,cmbIMDPreset,lblF2Title,nudF2,lblHz2,lblAmpF2Title,nudAmplitudeF2,lblDbfsF2});

            // ── pnlBurst (y=155, h=110) ───────────────────────────────────────
            pnlBurst.Location=new Point(0,155); pnlBurst.Size=new Size(384,110); pnlBurst.Visible=false;

            lblBurstOn.AutoSize=true; lblBurstOn.Location=new Point(10,5); lblBurstOn.Text="ON:";
            nudBurstOn.DecimalPlaces=1; nudBurstOn.Location=new Point(38,2); nudBurstOn.Minimum=0.1m; nudBurstOn.Maximum=99999m; nudBurstOn.Size=new Size(72,23); nudBurstOn.TabIndex=11; nudBurstOn.Value=5m;
            rbBurstOnPer.AutoSize=true; rbBurstOnPer.Checked=true; rbBurstOnPer.Location=new Point(115,4); rbBurstOnPer.Text="Periods"; rbBurstOnPer.TabIndex=12; rbBurstOnPer.UseVisualStyleBackColor=true;
            rbBurstOnMs.AutoSize=true; rbBurstOnMs.Location=new Point(185,4); rbBurstOnMs.Text="ms"; rbBurstOnMs.TabIndex=13; rbBurstOnMs.UseVisualStyleBackColor=true;

            lblBurstOff.AutoSize=true; lblBurstOff.Location=new Point(10,33); lblBurstOff.Text="OFF:";
            nudBurstOff.DecimalPlaces=1; nudBurstOff.Location=new Point(38,30); nudBurstOff.Minimum=0.1m; nudBurstOff.Maximum=999999m; nudBurstOff.Size=new Size(72,23); nudBurstOff.TabIndex=14; nudBurstOff.Value=20m;
            rbBurstOffPer.AutoSize=true; rbBurstOffPer.Checked=true; rbBurstOffPer.Location=new Point(115,32); rbBurstOffPer.Text="Periods"; rbBurstOffPer.TabIndex=15; rbBurstOffPer.UseVisualStyleBackColor=true;
            rbBurstOffMs.AutoSize=true; rbBurstOffMs.Location=new Point(185,32); rbBurstOffMs.Text="ms"; rbBurstOffMs.TabIndex=16; rbBurstOffMs.UseVisualStyleBackColor=true;

            lblBurstWin.AutoSize=true; lblBurstWin.Location=new Point(10,63); lblBurstWin.Text="Window:";
            cmbBurstWindow.DropDownStyle=ComboBoxStyle.DropDownList; cmbBurstWindow.Location=new Point(72,60); cmbBurstWindow.Size=new Size(164,23); cmbBurstWindow.TabIndex=17;
            chkBurstLoop.AutoSize=true; chkBurstLoop.Location=new Point(248,62); chkBurstLoop.Text="Loop"; chkBurstLoop.TabIndex=18; chkBurstLoop.UseVisualStyleBackColor=true;

            pnlBurst.Controls.AddRange(new Control[]{lblBurstOn,nudBurstOn,rbBurstOnPer,rbBurstOnMs,lblBurstOff,nudBurstOff,rbBurstOffPer,rbBurstOffMs,lblBurstWin,cmbBurstWindow,chkBurstLoop});

            // ── pnlDither (y=155, h=32) ───────────────────────────────────────
            pnlDither.Location=new Point(0,155); pnlDither.Size=new Size(384,32); pnlDither.Visible=false;
            chkDitherEnable.AutoSize=true; chkDitherEnable.Location=new Point(10,6); chkDitherEnable.Text="Dithering (TPDF):"; chkDitherEnable.TabIndex=19; chkDitherEnable.UseVisualStyleBackColor=true;
            nudDitherBits.Location=new Point(158,3); nudDitherBits.Minimum=16; nudDitherBits.Maximum=32; nudDitherBits.Value=24;
            nudDitherBits.Size=new Size(52,23); nudDitherBits.TabIndex=20; nudDitherBits.DecimalPlaces=0;
            lblDitherBits.AutoSize=true; lblDitherBits.Location=new Point(214,7); lblDitherBits.Text="bits";
            pnlDither.Controls.AddRange(new Control[]{chkDitherEnable,nudDitherBits,lblDitherBits});

            grpParams.Controls.AddRange(new Control[]{lblFreqTitle,nudF1,lblHz1,lblNoFreq,pnlPresets,chkSynch,lblSynchFreq,lblAmpTitle,nudAmplitude,lblDbfs,pnlSweep,pnlTwoTone,pnlBurst,pnlDither});

            // ── grpOutput (y=362) ─────────────────────────────────────────────
            grpOutput.Location=new Point(8,362); grpOutput.Size=new Size(384,105); grpOutput.TabStop=false; grpOutput.Text="Output";
            lblChannel.AutoSize=true; lblChannel.Location=new Point(10,26); lblChannel.Text="Channel:";
            rbL.AutoSize=true; rbL.Checked=true; rbL.Location=new Point(78,24); rbL.Text="L"; rbL.TabIndex=22; rbL.UseVisualStyleBackColor=true;
            rbR.AutoSize=true; rbR.Location=new Point(116,24); rbR.Text="R"; rbR.TabIndex=23; rbR.UseVisualStyleBackColor=true;
            rbLR.AutoSize=true; rbLR.Location=new Point(154,24); rbLR.Text="L+R"; rbLR.TabIndex=24; rbLR.UseVisualStyleBackColor=true;
            chkInvert.AutoSize=true; chkInvert.Enabled=false; chkInvert.Location=new Point(212,24); chkInvert.Text="Invert R"; chkInvert.TabIndex=25; chkInvert.UseVisualStyleBackColor=true;
            chkLoopback.AutoSize=true; chkLoopback.Location=new Point(10,52); chkLoopback.Text="Loopback Test  (no device)"; chkLoopback.TabIndex=26; chkLoopback.UseVisualStyleBackColor=true;
            lblSR.AutoSize=true; lblSR.ForeColor=SystemColors.GrayText; lblSR.Location=new Point(10,78); lblSR.Text="SR: —";
            lblLoopback.AutoSize=true; lblLoopback.ForeColor=Color.DodgerBlue; lblLoopback.Font=new Font("Segoe UI",8.5f,FontStyle.Italic); lblLoopback.Location=new Point(260,78); lblLoopback.Text="Loopback mode"; lblLoopback.Visible=false;
            grpOutput.Controls.AddRange(new Control[]{lblChannel,rbL,rbR,rbLR,chkInvert,chkLoopback,lblSR,lblLoopback});

            // ── RUN ───────────────────────────────────────────────────────────
            btnRun.Location=new Point(240,476); btnRun.Size=new Size(152,42); btnRun.TabIndex=27;
            btnRun.Text="▶  RUN"; btnRun.BackColor=Color.SeaGreen; btnRun.ForeColor=Color.White;
            btnRun.FlatStyle=FlatStyle.Flat; btnRun.FlatAppearance.BorderSize=0;
            btnRun.Font=new Font("Segoe UI",11f,FontStyle.Bold); btnRun.UseVisualStyleBackColor=false;

            // ── Form ──────────────────────────────────────────────────────────
            AutoScaleDimensions=new SizeF(7F,15F); AutoScaleMode=AutoScaleMode.Font;
            ClientSize=new Size(400,529);
            Controls.AddRange(new Control[]{lblTypeLabel,cmbType,grpParams,grpOutput,btnRun});
            FormBorderStyle=FormBorderStyle.FixedSingle; MinimizeBox=true; MaximizeBox=false;
            StartPosition=FormStartPosition.Manual; Text="Generator";

            foreach(var n in allNud) n.EndInit();
            grpParams.ResumeLayout(false); grpParams.PerformLayout(); pnlPresets.ResumeLayout(false);
            pnlSweep.ResumeLayout(false); pnlSweep.PerformLayout();
            pnlTwoTone.ResumeLayout(false); pnlTwoTone.PerformLayout();
            pnlBurst.ResumeLayout(false); pnlBurst.PerformLayout();
            pnlDither.ResumeLayout(false);
            grpOutput.ResumeLayout(false); grpOutput.PerformLayout();
            ResumeLayout(false); PerformLayout();
        }
    }
}
#nullable restore
