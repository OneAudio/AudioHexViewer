// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

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
        private Panel pnlFreqPresets, pnlAmpPresets;  // deux lignes de presets
        // Freq presets (6)
        private Button btnF1, btnF10, btnF100, btnF1k, btnF10k, btnF100k;
        // Amp presets (6)
        private Button btnA0, btnAm3, btnAm6, btnAm12, btnAm20, btnAm60;
        private Label lblAmpTitle; private NumericUpDown nudAmplitude; private Label lblDbfs;
        private RadioButton rbAmpdBFS, rbAmpVrms;  // toggle unité amplitude
        // Sweep
        private Panel pnlSweep;
        private Label lblSweepEndT, lblSweepTimeT;
        private NumericUpDown nudSweepEnd, nudSweepTime;
        private Label lblSweepEndHz, lblSweepS;
        private CheckBox chkSweepLog, chkSweepLoop;
        // Two-tone
        private Panel pnlTwoTone;
        private Label lblIMDLabel; private ComboBox cmbIMDPreset;
        private Label lblF2FreqTitle; private NumericUpDown nudF2; private Label lblHz2;
        private Label lblF2AmpTitle;  private NumericUpDown nudAmplitudeF2; private Label lblDbfsF2;
        // Burst
        private Panel pnlBurst;
        private Label lblTon, lblToff;
        private Panel pnlOnMode, pnlOffMode;
        private RadioButton rbBurstOnPer, rbBurstOnMs, rbBurstOffPer, rbBurstOffMs;
        private NumericUpDown nudBurstOnPer, nudBurstOnMs;
        private NumericUpDown nudBurstOffPer, nudBurstOffMs;
        private Label lblBurstWin; private ComboBox cmbBurstWindow; private CheckBox chkBurstLoop;
        // Dither
        private Panel pnlDither;
        private CheckBox chkDitherEnable;
        private NumericUpDown nudDitherBits;
        private Label lblDitherBits;
        // Output
        private System.Windows.Forms.GroupBox grpOutput;
        private Label lblChannel;
        private RadioButton rbL, rbR, rbLR;
        private CheckBox chkInvert, chkLoopback;
        private Label lblSR, lblLoopback;
        // Calibration de niveau de sortie
        private Button btnCalibrateLevel;
        private Label  lblCalL, lblCalR;
        private Panel  pnlVuMeter;
        private Label  lblVuL, lblVuR, lblVuLevelL, lblVuLevelR;
        private Panel  barGenL, barGenR;
        private Button btnRun;
        private Panel  pnlWavePreview;   // prévisualisation de la forme d'onde

        protected override void Dispose(bool disposing) { if (disposing) components?.Dispose(); base.Dispose(disposing); }

        private void InitializeComponent()
        {
            lblTypeLabel=new Label(); cmbType=new ComboBox(); grpParams=new System.Windows.Forms.GroupBox();
            lblFreqTitle=new Label(); nudF1=new NumericUpDown(); lblHz1=new Label();
            lblNoFreq=new Label(); chkSynch=new CheckBox(); lblSynchFreq=new Label();
            pnlFreqPresets=new Panel(); pnlAmpPresets=new Panel();
            btnF1=new Button(); btnF10=new Button(); btnF100=new Button();
            btnF1k=new Button(); btnF10k=new Button(); btnF100k=new Button();
            btnA0=new Button(); btnAm3=new Button(); btnAm6=new Button();
            btnAm12=new Button(); btnAm20=new Button(); btnAm60=new Button();
            lblAmpTitle=new Label(); nudAmplitude=new NumericUpDown(); lblDbfs=new Label();
            rbAmpdBFS=new RadioButton(); rbAmpVrms=new RadioButton();
            pnlSweep=new Panel(); lblSweepEndT=new Label(); nudSweepEnd=new NumericUpDown();
            lblSweepEndHz=new Label(); lblSweepTimeT=new Label(); nudSweepTime=new NumericUpDown();
            lblSweepS=new Label(); chkSweepLog=new CheckBox(); chkSweepLoop=new CheckBox();
            pnlTwoTone=new Panel(); lblIMDLabel=new Label(); cmbIMDPreset=new ComboBox();
            lblF2FreqTitle=new Label(); nudF2=new NumericUpDown(); lblHz2=new Label();
            lblF2AmpTitle=new Label(); nudAmplitudeF2=new NumericUpDown(); lblDbfsF2=new Label();
            pnlBurst=new Panel(); lblTon=new Label(); lblToff=new Label();
            pnlOnMode=new Panel(); pnlOffMode=new Panel();
            rbBurstOnPer=new RadioButton(); rbBurstOnMs=new RadioButton();
            rbBurstOffPer=new RadioButton(); rbBurstOffMs=new RadioButton();
            nudBurstOnPer=new NumericUpDown(); nudBurstOnMs=new NumericUpDown();
            nudBurstOffPer=new NumericUpDown(); nudBurstOffMs=new NumericUpDown();
            lblBurstWin=new Label(); cmbBurstWindow=new ComboBox(); chkBurstLoop=new CheckBox();
            pnlDither=new Panel(); chkDitherEnable=new CheckBox();
            nudDitherBits=new NumericUpDown(); lblDitherBits=new Label();
            grpOutput=new System.Windows.Forms.GroupBox();
            lblChannel=new Label(); rbL=new RadioButton(); rbR=new RadioButton(); rbLR=new RadioButton();
            chkInvert=new CheckBox(); chkLoopback=new CheckBox(); lblSR=new Label(); lblLoopback=new Label();
            btnCalibrateLevel=new Button(); lblCalL=new Label(); lblCalR=new Label();
            pnlVuMeter=new Panel(); lblVuL=new Label(); lblVuR=new Label();
            lblVuLevelL=new Label(); lblVuLevelR=new Label();
            barGenL=new Panel(); barGenR=new Panel();
            btnRun=new Button(); pnlWavePreview=new Panel();

            var allNud = new System.ComponentModel.ISupportInitialize[]{
                nudF1,nudAmplitude,nudSweepEnd,nudSweepTime,nudF2,nudAmplitudeF2,
                nudBurstOnPer,nudBurstOnMs,nudBurstOffPer,nudBurstOffMs,nudDitherBits};
            foreach(var n in allNud) n.BeginInit();
            grpParams.SuspendLayout();
            pnlSweep.SuspendLayout(); pnlTwoTone.SuspendLayout();
            pnlBurst.SuspendLayout(); pnlOnMode.SuspendLayout(); pnlOffMode.SuspendLayout();
            pnlDither.SuspendLayout(); pnlFreqPresets.SuspendLayout(); pnlAmpPresets.SuspendLayout();
            pnlVuMeter.SuspendLayout(); grpOutput.SuspendLayout();
            SuspendLayout();

            var grayS = new Font("Segoe UI", 8f);
            var big   = new Font("Segoe UI", 14f, FontStyle.Bold);
            var std10 = new Font("Segoe UI", 10f);

            // ── Type row ──────────────────────────────────────────────────────
            lblTypeLabel.AutoSize=true; lblTypeLabel.Location=new Point(12,66); lblTypeLabel.Text="Signal type:";
            cmbType.DropDownStyle=ComboBoxStyle.DropDownList; cmbType.Location=new Point(100,62);
            cmbType.Size=new Size(210,23); cmbType.TabIndex=0;

            // ── grpParams (y=44, h=332) ───────────────────────────────────────
            grpParams.Location=new Point(8,96); grpParams.Size=new Size(384,322); grpParams.TabStop=false; grpParams.Text="Parameters";

            // Frequency
            lblFreqTitle.AutoSize=true; lblFreqTitle.Font=grayS; lblFreqTitle.ForeColor=SystemColors.GrayText;
            lblFreqTitle.Location=new Point(10,22); lblFreqTitle.Text="FREQUENCY";
            nudF1.DecimalPlaces=2; nudF1.Font=big; nudF1.Location=new Point(10,38);
            nudF1.Maximum=24000m; nudF1.Minimum=1m; nudF1.Size=new Size(162,34); nudF1.TabIndex=1; nudF1.Value=1000m;
            lblHz1.AutoSize=true; lblHz1.Font=std10; lblHz1.Location=new Point(175,44); lblHz1.Text="Hz";
            lblNoFreq.AutoSize=true; lblNoFreq.Font=new Font("Segoe UI",9f,FontStyle.Italic);
            lblNoFreq.ForeColor=SystemColors.GrayText; lblNoFreq.Location=new Point(10,52);
            lblNoFreq.Text="Broadband — no frequency"; lblNoFreq.Visible=false;

            // Presets fréquence — ligne pleine largeur (y=80)
            void Btn(Button b,int x,string t){b.Location=new Point(x,1);b.Size=new Size(60,24);b.Text=t;b.Font=new Font("Segoe UI",7.5f);b.FlatStyle=FlatStyle.Popup;}
            pnlFreqPresets.Location=new Point(10,80); pnlFreqPresets.Size=new Size(360,26);
            Btn(btnF1,0,"1 Hz"); Btn(btnF10,60,"10 Hz"); Btn(btnF100,120,"100 Hz");
            Btn(btnF1k,180,"1 kHz"); Btn(btnF10k,240,"10 kHz"); Btn(btnF100k,300,"100 kHz");
            pnlFreqPresets.Controls.AddRange(new Control[]{btnF1,btnF10,btnF100,btnF1k,btnF10k,btnF100k});
            // Presets amplitude — ligne pleine largeur (y=108)
            pnlAmpPresets.Location=new Point(10,108); pnlAmpPresets.Size=new Size(360,26);
            Btn(btnA0,0,"0 dBFS"); Btn(btnAm3,60,"-3 dBFS"); Btn(btnAm6,120,"-6 dBFS");
            Btn(btnAm12,180,"-12 dBFS"); Btn(btnAm20,240,"-20 dBFS"); Btn(btnAm60,300,"-60 dBFS");
            pnlAmpPresets.Controls.AddRange(new Control[]{btnA0,btnAm3,btnAm6,btnAm12,btnAm20,btnAm60});

            chkSynch.AutoSize=true; chkSynch.Location=new Point(10,138);
            chkSynch.Text="Synch (coherent)"; chkSynch.TabIndex=2; chkSynch.UseVisualStyleBackColor=true;
            lblSynchFreq.AutoSize=true; lblSynchFreq.Font=grayS; lblSynchFreq.ForeColor=SystemColors.GrayText;
            lblSynchFreq.Location=new Point(10,160); lblSynchFreq.Text="→ 1000.12 Hz"; lblSynchFreq.Visible=false;

            // Amplitude
            lblAmpTitle.AutoSize=true; lblAmpTitle.Font=grayS; lblAmpTitle.ForeColor=SystemColors.GrayText;
            lblAmpTitle.Location=new Point(218,22); lblAmpTitle.Text="AMPLITUDE";
            nudAmplitude.DecimalPlaces=1; nudAmplitude.Font=big; nudAmplitude.Increment=0.5m;
            nudAmplitude.Location=new Point(218,38); nudAmplitude.Maximum=0m; nudAmplitude.Minimum=-180m;
            nudAmplitude.Size=new Size(108,34); nudAmplitude.TabIndex=3; nudAmplitude.Value=-12m;
            lblDbfs.AutoSize=true; lblDbfs.Font=std10; lblDbfs.Location=new Point(330,44); lblDbfs.Text="dBFS";
            // ── Toggle dBFS / Vrms ────────────────────────────────────────────
            rbAmpdBFS.AutoSize=true; rbAmpdBFS.Checked=true; rbAmpdBFS.Font=new Font("Segoe UI",8.5f);
            rbAmpdBFS.Location=new Point(330,36); rbAmpdBFS.Text="dBFS"; rbAmpdBFS.TabIndex=4;
            rbAmpdBFS.UseVisualStyleBackColor=true;
            rbAmpVrms.AutoSize=true; rbAmpVrms.Font=new Font("Segoe UI",8.5f); rbAmpVrms.Enabled=false;
            rbAmpVrms.Location=new Point(330,54); rbAmpVrms.Text="Vrms"; rbAmpVrms.TabIndex=5;
            rbAmpVrms.UseVisualStyleBackColor=true;

            // ── pnlSweep (y=155, h=115) ───────────────────────────────────────
            pnlSweep.Location=new Point(0,155); pnlSweep.Size=new Size(384,115); pnlSweep.Visible=false;
            lblSweepEndT.AutoSize=true; lblSweepEndT.Font=grayS; lblSweepEndT.ForeColor=SystemColors.GrayText; lblSweepEndT.Location=new Point(10,2); lblSweepEndT.Text="END";
            nudSweepEnd.DecimalPlaces=0; nudSweepEnd.Font=big; nudSweepEnd.Location=new Point(10,18); nudSweepEnd.Maximum=96000m; nudSweepEnd.Minimum=1m; nudSweepEnd.Size=new Size(150,34); nudSweepEnd.TabIndex=4; nudSweepEnd.Value=20000m;
            lblSweepEndHz.AutoSize=true; lblSweepEndHz.Font=std10; lblSweepEndHz.Location=new Point(164,24); lblSweepEndHz.Text="Hz";
            lblSweepTimeT.AutoSize=true; lblSweepTimeT.Font=grayS; lblSweepTimeT.ForeColor=SystemColors.GrayText; lblSweepTimeT.Location=new Point(220,2); lblSweepTimeT.Text="DURATION";
            nudSweepTime.DecimalPlaces=1; nudSweepTime.Font=big; nudSweepTime.Location=new Point(220,18); nudSweepTime.Maximum=600m; nudSweepTime.Minimum=0.1m; nudSweepTime.Size=new Size(110,34); nudSweepTime.TabIndex=5; nudSweepTime.Value=5m;
            lblSweepS.AutoSize=true; lblSweepS.Font=std10; lblSweepS.Location=new Point(334,24); lblSweepS.Text="s";
            chkSweepLog.AutoSize=true; chkSweepLog.Checked=true; chkSweepLog.Location=new Point(10,60); chkSweepLog.Text="Logarithmic"; chkSweepLog.TabIndex=6; chkSweepLog.UseVisualStyleBackColor=true;
            chkSweepLoop.AutoSize=true; chkSweepLoop.Location=new Point(140,60); chkSweepLoop.Text="Loop"; chkSweepLoop.TabIndex=7; chkSweepLoop.UseVisualStyleBackColor=true;
            pnlSweep.Controls.AddRange(new Control[]{lblSweepEndT,nudSweepEnd,lblSweepEndHz,lblSweepTimeT,nudSweepTime,lblSweepS,chkSweepLog,chkSweepLoop});

            // ── pnlTwoTone (y=155, h=100) ─────────────────────────────────────
            // 4 combos identiques (14pt bold, 34px) : F1 en haut (dans grpParams),
            // F2 ici. Labels : FREQUENCY F1/F2, AMPLITUDE F1/F2.
            pnlTwoTone.Location=new Point(0,155); pnlTwoTone.Size=new Size(384,100); pnlTwoTone.Visible=false;

            lblIMDLabel.AutoSize=true; lblIMDLabel.Location=new Point(10,4); lblIMDLabel.Text="IMD preset:";
            cmbIMDPreset.DropDownStyle=ComboBoxStyle.DropDownList; cmbIMDPreset.Location=new Point(85,1);
            cmbIMDPreset.Size=new Size(285,23); cmbIMDPreset.TabIndex=8;

            // F2 row (même taille que F1/Amplitude)
            lblF2FreqTitle.AutoSize=true; lblF2FreqTitle.Font=grayS; lblF2FreqTitle.ForeColor=SystemColors.GrayText;
            lblF2FreqTitle.Location=new Point(10,30); lblF2FreqTitle.Text="FREQUENCY F2";
            nudF2.DecimalPlaces=2; nudF2.Font=big; nudF2.Location=new Point(10,46);
            nudF2.Maximum=24000m; nudF2.Minimum=1m; nudF2.Size=new Size(162,34); nudF2.TabIndex=9; nudF2.Value=2000m;
            lblHz2.AutoSize=true; lblHz2.Font=std10; lblHz2.Location=new Point(175,52); lblHz2.Text="Hz";

            lblF2AmpTitle.AutoSize=true; lblF2AmpTitle.Font=grayS; lblF2AmpTitle.ForeColor=SystemColors.GrayText;
            lblF2AmpTitle.Location=new Point(218,30); lblF2AmpTitle.Text="AMPLITUDE F2";
            nudAmplitudeF2.DecimalPlaces=1; nudAmplitudeF2.Font=big; nudAmplitudeF2.Increment=0.5m;
            nudAmplitudeF2.Location=new Point(218,46); nudAmplitudeF2.Maximum=0m; nudAmplitudeF2.Minimum=-180m;
            nudAmplitudeF2.Size=new Size(108,34); nudAmplitudeF2.TabIndex=10; nudAmplitudeF2.Value=-12m;
            lblDbfsF2.AutoSize=true; lblDbfsF2.Font=std10; lblDbfsF2.Location=new Point(330,52); lblDbfsF2.Text="dBFS";

            pnlTwoTone.Controls.AddRange(new Control[]{lblIMDLabel,cmbIMDPreset,lblF2FreqTitle,nudF2,lblHz2,lblF2AmpTitle,nudAmplitudeF2,lblDbfsF2});

            // ── pnlBurst (y=155, h=205) ───────────────────────────────────────
            // Layout empilé : Ton/Toff à gauche, radio buttons devant, puis NUD
            // pnlOnMode et pnlOffMode isolent les groupes de RadioButtons
            pnlBurst.Location=new Point(0,112); pnlBurst.Size=new Size(384,196); pnlBurst.Visible=false;

            // ── Ton ───────────────────────────────────────────────────────────
            lblTon.AutoSize=true; lblTon.Font=grayS; lblTon.ForeColor=SystemColors.GrayText;
            lblTon.Location=new Point(90,34); lblTon.Text="TON";

            pnlOnMode.Location=new Point(130,4); pnlOnMode.Size=new Size(82,56);
            rbBurstOnPer.AutoSize=true; rbBurstOnPer.Checked=true; rbBurstOnPer.Location=new Point(0,2); rbBurstOnPer.Text="Periods"; rbBurstOnPer.TabIndex=0; rbBurstOnPer.UseVisualStyleBackColor=true;
            rbBurstOnMs.AutoSize=true; rbBurstOnMs.Location=new Point(0,34); rbBurstOnMs.Text="ms"; rbBurstOnMs.TabIndex=1; rbBurstOnMs.UseVisualStyleBackColor=true;
            pnlOnMode.Controls.AddRange(new Control[]{rbBurstOnPer,rbBurstOnMs});

            nudBurstOnPer.DecimalPlaces=1; nudBurstOnPer.Font=big; nudBurstOnPer.Location=new Point(218,4);
            nudBurstOnPer.Minimum=0.1m; nudBurstOnPer.Maximum=99999m; nudBurstOnPer.Size=new Size(108,34); nudBurstOnPer.TabIndex=11; nudBurstOnPer.Value=5m;

            nudBurstOnMs.DecimalPlaces=1; nudBurstOnMs.Font=big; nudBurstOnMs.Location=new Point(218,42);
            nudBurstOnMs.Minimum=0.01m; nudBurstOnMs.Maximum=999999m; nudBurstOnMs.Size=new Size(108,34); nudBurstOnMs.TabIndex=12; nudBurstOnMs.Value=100m;

            // ── Toff ──────────────────────────────────────────────────────────
            lblToff.AutoSize=true; lblToff.Font=grayS; lblToff.ForeColor=SystemColors.GrayText;
            lblToff.Location=new Point(90,116); lblToff.Text="TOFF";

            pnlOffMode.Location=new Point(130,86); pnlOffMode.Size=new Size(82,56);
            rbBurstOffPer.AutoSize=true; rbBurstOffPer.Checked=true; rbBurstOffPer.Location=new Point(0,2); rbBurstOffPer.Text="Periods"; rbBurstOffPer.TabIndex=0; rbBurstOffPer.UseVisualStyleBackColor=true;
            rbBurstOffMs.AutoSize=true; rbBurstOffMs.Location=new Point(0,34); rbBurstOffMs.Text="ms"; rbBurstOffMs.TabIndex=1; rbBurstOffMs.UseVisualStyleBackColor=true;
            pnlOffMode.Controls.AddRange(new Control[]{rbBurstOffPer,rbBurstOffMs});

            nudBurstOffPer.DecimalPlaces=1; nudBurstOffPer.Font=big; nudBurstOffPer.Location=new Point(218,86);
            nudBurstOffPer.Minimum=0.1m; nudBurstOffPer.Maximum=99999m; nudBurstOffPer.Size=new Size(108,34); nudBurstOffPer.TabIndex=13; nudBurstOffPer.Value=20m;

            nudBurstOffMs.DecimalPlaces=1; nudBurstOffMs.Font=big; nudBurstOffMs.Location=new Point(218,124);
            nudBurstOffMs.Minimum=0.01m; nudBurstOffMs.Maximum=999999m; nudBurstOffMs.Size=new Size(108,34); nudBurstOffMs.TabIndex=14; nudBurstOffMs.Value=400m;

            // ── Window + Loop ─────────────────────────────────────────────────
            lblBurstWin.AutoSize=true; lblBurstWin.Location=new Point(10,166); lblBurstWin.Text="Window:";
            cmbBurstWindow.DropDownStyle=ComboBoxStyle.DropDownList; cmbBurstWindow.Location=new Point(72,163);
            cmbBurstWindow.Size=new Size(164,23); cmbBurstWindow.TabIndex=15;
            chkBurstLoop.AutoSize=true; chkBurstLoop.Location=new Point(248,165);
            chkBurstLoop.Text="Loop"; chkBurstLoop.TabIndex=16; chkBurstLoop.UseVisualStyleBackColor=true;

            pnlBurst.Controls.AddRange(new Control[]{lblTon,pnlOnMode,nudBurstOnPer,nudBurstOnMs,lblToff,pnlOffMode,nudBurstOffPer,nudBurstOffMs,lblBurstWin,cmbBurstWindow,chkBurstLoop});

            // ── pnlDither (y=155, h=40) ───────────────────────────────────────
            // "bits" centré verticalement sur nudDitherBits (h=34)
            pnlDither.Location=new Point(0,183); pnlDither.Size=new Size(384,40); pnlDither.Visible=false;

            chkDitherEnable.AutoSize=true; chkDitherEnable.Location=new Point(10,11);
            chkDitherEnable.Text="Dithering  (TPDF):"; chkDitherEnable.TabIndex=17; chkDitherEnable.UseVisualStyleBackColor=true;

            nudDitherBits.Font=big; nudDitherBits.Location=new Point(218,3);
            nudDitherBits.Minimum=16; nudDitherBits.Maximum=32; nudDitherBits.Value=24;
            nudDitherBits.Size=new Size(108,34); nudDitherBits.TabIndex=18; nudDitherBits.DecimalPlaces=0;

            // "bits" centré verticalement avec nudDitherBits
            lblDitherBits.AutoSize=true; lblDitherBits.Font=std10;
            lblDitherBits.Location=new Point(330,11); lblDitherBits.Text="bits";

            pnlDither.Controls.AddRange(new Control[]{chkDitherEnable,nudDitherBits,lblDitherBits});

            grpParams.Controls.AddRange(new Control[]{lblFreqTitle,nudF1,lblHz1,lblNoFreq,pnlFreqPresets,pnlAmpPresets,chkSynch,lblSynchFreq,lblAmpTitle,nudAmplitude,rbAmpdBFS,rbAmpVrms,pnlSweep,pnlTwoTone,pnlBurst,pnlDither});

            // ── grpOutput (y=384) ─────────────────────────────────────────────
            // ── pnlVuMeter (y=0, h=52) — taille manquante dans les versions précédentes
            pnlVuMeter.Location=new Point(0,0); pnlVuMeter.Size=new Size(400,52);
            grpOutput.Location=new Point(8,426); grpOutput.Size=new Size(384,105); grpOutput.TabStop=false; grpOutput.Text="Output";
            lblChannel.AutoSize=true; lblChannel.Location=new Point(10,26); lblChannel.Text="Channel:";
            rbL.AutoSize=true; rbL.Checked=true; rbL.Location=new Point(78,24); rbL.Text="L"; rbL.TabIndex=22; rbL.UseVisualStyleBackColor=true;
            rbR.AutoSize=true; rbR.Location=new Point(116,24); rbR.Text="R"; rbR.TabIndex=23; rbR.UseVisualStyleBackColor=true;
            rbLR.AutoSize=true; rbLR.Location=new Point(154,24); rbLR.Text="L+R"; rbLR.TabIndex=24; rbLR.UseVisualStyleBackColor=true;
            chkInvert.AutoSize=true; chkInvert.Enabled=false; chkInvert.Location=new Point(212,24); chkInvert.Text="Invert R"; chkInvert.TabIndex=25; chkInvert.UseVisualStyleBackColor=true;
            chkLoopback.AutoSize=true; chkLoopback.Location=new Point(10,52); chkLoopback.Text="Loopback Test  (no device)"; chkLoopback.TabIndex=26; chkLoopback.UseVisualStyleBackColor=true;
            lblSR.AutoSize=true; lblSR.ForeColor=SystemColors.GrayText; lblSR.Location=new Point(10,78); lblSR.Text="SR: —";
            lblLoopback.AutoSize=true; lblLoopback.ForeColor=Color.DodgerBlue; lblLoopback.Font=new Font("Segoe UI",8.5f,FontStyle.Italic);
            lblLoopback.Location=new Point(200,78); lblLoopback.Text="Loopback mode"; lblLoopback.Visible=false;
            // ── Calibration : bouton à droite de "Invert R", labels sur les 2 lignes dessous ──
            btnCalibrateLevel.Location=new Point(282,19); btnCalibrateLevel.Size=new Size(94,26);
            btnCalibrateLevel.Text="Calibrate Level"; btnCalibrateLevel.TabIndex=28;
            btnCalibrateLevel.UseVisualStyleBackColor=true;
            btnCalibrateLevel.Font=new Font("Segoe UI",8f);
            lblCalL.AutoSize=true; lblCalL.Font=new Font("Segoe UI",8f);
            lblCalL.ForeColor=SystemColors.GrayText;
            lblCalL.Location=new Point(282,50); lblCalL.Text="L: FS = — Vrms";
            lblCalR.AutoSize=true; lblCalR.Font=new Font("Segoe UI",8f);
            lblCalR.ForeColor=SystemColors.GrayText;
            lblCalR.Location=new Point(282,66); lblCalR.Text="R: FS = — Vrms";
            grpOutput.Controls.AddRange(new Control[]{lblChannel,rbL,rbR,rbLR,chkInvert,chkLoopback,lblSR,lblLoopback,btnCalibrateLevel,lblCalL,lblCalR});

            // ── RUN ───────────────────────────────────────────────────────────
            btnRun.Location=new Point(240,540); btnRun.Size=new Size(152,42); btnRun.TabIndex=27;
            btnRun.Text="▶  RUN"; btnRun.BackColor=Color.SeaGreen; btnRun.ForeColor=Color.White;
            btnRun.FlatStyle=FlatStyle.Flat; btnRun.FlatAppearance.BorderSize=0;
            btnRun.Font=new Font("Segoe UI",11f,FontStyle.Bold); btnRun.UseVisualStyleBackColor=false;
            // ── Prévisualisation forme d'onde ─────────────────────────────────
            pnlWavePreview.Location=new Point(8,540); pnlWavePreview.Size=new Size(224,42);
            pnlWavePreview.BackColor=Color.Transparent;

            // ── Form ──────────────────────────────────────────────────────────
            AutoScaleDimensions=new SizeF(7F,15F); AutoScaleMode=AutoScaleMode.Font;
            ClientSize=new Size(400,593);
            Controls.AddRange(new Control[]{pnlVuMeter,lblTypeLabel,cmbType,grpParams,grpOutput,pnlWavePreview,btnRun});
            FormBorderStyle=FormBorderStyle.FixedSingle; MinimizeBox=true; MaximizeBox=false;
            StartPosition=FormStartPosition.Manual; Text="Generator";

            foreach(var n in allNud) n.EndInit();
            grpParams.ResumeLayout(false); grpParams.PerformLayout();
            pnlFreqPresets.ResumeLayout(false); pnlAmpPresets.ResumeLayout(false);
            pnlSweep.ResumeLayout(false); pnlSweep.PerformLayout();
            pnlTwoTone.ResumeLayout(false); pnlTwoTone.PerformLayout();
            pnlBurst.ResumeLayout(false); pnlBurst.PerformLayout();
            pnlOnMode.ResumeLayout(false); pnlOnMode.PerformLayout();
            pnlOffMode.ResumeLayout(false); pnlOffMode.PerformLayout();
            pnlDither.ResumeLayout(false);
            pnlVuMeter.ResumeLayout(false);
            grpOutput.ResumeLayout(false); grpOutput.PerformLayout();
            // ── Context help Name assignments ─────────────────────────────
            cmbType.Name = "cmbType";
            nudF1.Name = "nudF1";
            chkSynch.Name = "chkSynch";
            btnF1.Name = "btnF1";
            btnF10.Name = "btnF10";
            btnF100.Name = "btnF100";
            btnF1k.Name = "btnF1k";
            btnF10k.Name = "btnF10k";
            btnF100k.Name = "btnF100k";
            btnA0.Name = "btnA0";
            btnAm3.Name = "btnAm3";
            btnAm6.Name = "btnAm6";
            btnAm12.Name = "btnAm12";
            btnAm20.Name = "btnAm20";
            btnAm60.Name = "btnAm60";
            nudAmplitude.Name = "nudAmplitude";
            rbAmpdBFS.Name = "rbAmpdBFS";
            rbAmpVrms.Name = "rbAmpVrms";
            nudSweepEnd.Name = "nudSweepEnd";
            nudSweepTime.Name = "nudSweepTime";
            chkSweepLog.Name = "chkSweepLog";
            chkSweepLoop.Name = "chkSweepLoop";
            cmbIMDPreset.Name = "cmbIMDPreset";
            nudF2.Name = "nudF2";
            nudAmplitudeF2.Name = "nudAmplitudeF2";
            rbBurstOnPer.Name = "rbBurstOnPer";
            rbBurstOnMs.Name = "rbBurstOnMs";
            rbBurstOffPer.Name = "rbBurstOffPer";
            rbBurstOffMs.Name = "rbBurstOffMs";
            nudBurstOnPer.Name = "nudBurstOnPer";
            nudBurstOnMs.Name = "nudBurstOnMs";
            nudBurstOffPer.Name = "nudBurstOffPer";
            nudBurstOffMs.Name = "nudBurstOffMs";
            cmbBurstWindow.Name = "cmbBurstWindow";
            chkBurstLoop.Name = "chkBurstLoop";
            chkDitherEnable.Name = "chkDitherEnable";
            nudDitherBits.Name = "nudDitherBits";
            rbL.Name = "rbL";
            rbR.Name = "rbR";
            rbLR.Name = "rbLR";
            chkInvert.Name = "chkInvert";
            chkLoopback.Name = "chkLoopback";
            btnCalibrateLevel.Name = "btnCalibrateLevel";
            btnRun.Name = "btnRun";
            ResumeLayout(false); PerformLayout();
        }
    }
}
#nullable restore
