using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AudioHexViewer
{
    internal partial class GeneratorWindow : Form
    {
        private static readonly Color ColorRunOff = Color.SeaGreen;
        private static readonly Color ColorRunOn  = Color.Firebrick;

        private readonly Func<MMDevice?>              _getOutputDevice;
        private readonly Func<MMDevice?>              _getInputDevice;
        private readonly Func<WaveFormatExtensible?>  _getOutputFormat;
        private readonly Func<int>                    _getAnalyzerSR;
        private readonly Func<int>                    _getAnalyzerN;
        private readonly Action<IWaveProvider?>       _onLoopbackRequest;

        // ── Calibration de niveau de sortie ──────────────────────────────────
        private readonly SessionSettings              _settings;
        private readonly Action                       _saveSettings;
        private readonly Action?                      _onRunStateChanged;
        private GeneratorCalibrationDialog?           _calDialog;

        // Propriétés de calibration (raccourcis vers _settings)
        private bool   _calValidL  { get => _settings.GenCalValidLeft;           set => _settings.GenCalValidLeft           = value; }
        private double _calVrmsL   { get => _settings.GenCalVrmsFullScaleLeft;   set => _settings.GenCalVrmsFullScaleLeft   = value; }
        private bool   _calValidR  { get => _settings.GenCalValidRight;          set => _settings.GenCalValidRight          = value; }
        private double _calVrmsR   { get => _settings.GenCalVrmsFullScaleRight;  set => _settings.GenCalVrmsFullScaleRight  = value; }

        private WasapiOut?               _wasapiShared;
        private ExclusiveWasapiPlayback? _wasapiExclusive;
        private IWaveProvider?           _provider;
        private bool                     _isRunning;
        private int                      _currentSR;

        public bool IsGenerating      => _isRunning;
        public int  CurrentSampleRate => _currentSR;
        private bool                     _loopbackActive;
        private readonly System.Windows.Forms.Timer _doneTimer;
        private readonly System.Windows.Forms.Timer _levelTimer;
        private LevelMeasuringProvider? _levelProbe;
        private IWaveProvider?          _rawProvider;  // underlying provider (before LevelMeasuringProvider wrap)
        private int    _vuPctL, _vuPctR;
        private string _vuLevelStrL = "-∞ dBFS", _vuLevelStrR = "-∞ dBFS";

        public GeneratorWindow(
            Func<MMDevice?>             getOutputDevice,
            Func<MMDevice?>             getInputDevice,
            Func<WaveFormatExtensible?> getOutputFormat,
            Func<int>                   getAnalyzerSR,
            Func<int>                   getAnalyzerN,
            Action<IWaveProvider?>      onLoopbackRequest,
            SessionSettings             settings,
            Action                      saveSettings,
            Action?                     onRunStateChanged = null)
        {
            _getOutputDevice   = getOutputDevice;
            _getInputDevice    = getInputDevice;
            _getOutputFormat   = getOutputFormat;
            _getAnalyzerSR     = getAnalyzerSR;
            _getAnalyzerN      = getAnalyzerN;
            _onLoopbackRequest = onLoopbackRequest;
            _settings          = settings;
            _saveSettings          = saveSettings;
            _onRunStateChanged     = onRunStateChanged;

            InitializeComponent();
            InitVuBars();   // ← WAS MISSING: wire pnlVuMeter.Paint event
            InitWavePreview();
            foreach (var v in Enum.GetValues<SignalType>())
                cmbType.Items.Add(new TypeItem(v));
            cmbType.SelectedIndex = 0;

            foreach (var (_, lbl) in BurstWindows.Labels)
                cmbBurstWindow.Items.Add(lbl);
            cmbBurstWindow.SelectedIndex = 0;

            foreach (var pr in IMDPresets) cmbIMDPreset.Items.Add(pr);
            cmbIMDPreset.SelectedIndex = 0;

            // Événements
            cmbType.SelectedIndexChanged    += (_, _) => { UpdateParamPanels(); RefreshSynchLabel(); RefreshDeviceStatus(); InvalidateWavePreview(); };
            nudF1.ValueChanged              += (_, _) => RefreshSynchLabel();
            chkSynch.CheckedChanged         += (_, _) => RefreshSynchLabel();
            chkLoopback.CheckedChanged      += (_, _) => RefreshDeviceStatus();
            rbL.CheckedChanged              += (_, _) => chkInvert.Enabled = rbLR.Checked;
            rbR.CheckedChanged              += (_, _) => chkInvert.Enabled = rbLR.Checked;
            rbLR.CheckedChanged             += (_, _) => chkInvert.Enabled = rbLR.Checked;
            // Invalidation preview sur changements de paramètres burst/sweep
            nudBurstOnPer.ValueChanged      += (_, _) => InvalidateWavePreview();
            nudBurstOnMs.ValueChanged       += (_, _) => InvalidateWavePreview();
            cmbBurstWindow.SelectedIndexChanged += (_, _) => InvalidateWavePreview();
            chkSweepLog.CheckedChanged      += (_, _) => InvalidateWavePreview();
            nudF2.ValueChanged              += (_, _) => InvalidateWavePreview();
            // ─ Freq presets ─────────────────────────────────────────────────────
            btnF1.Click    += (_, _) => nudF1.Value = 1m;
            btnF10.Click   += (_, _) => nudF1.Value = 10m;
            btnF100.Click  += (_, _) => nudF1.Value = 100m;
            btnF1k.Click   += (_, _) => nudF1.Value = 1000m;
            btnF10k.Click  += (_, _) => nudF1.Value = 10000m;
            btnF100k.Click += (_, _) => { if (btnF100k.Enabled) nudF1.Value = Math.Min(100000m, nudF1.Maximum); };
            // ─ Amp presets ──────────────────────────────────────────────────────
            btnA0.Click   += (_, _) => nudAmplitude.Value =   0m;
            btnAm3.Click  += (_, _) => nudAmplitude.Value =  -3m;
            btnAm6.Click  += (_, _) => nudAmplitude.Value =  -6m;
            btnAm12.Click += (_, _) => nudAmplitude.Value = -12m;
            btnAm20.Click += (_, _) => nudAmplitude.Value = -20m;
            btnAm60.Click += (_, _) => nudAmplitude.Value = -60m;
            cmbIMDPreset.SelectedIndexChanged += CmbIMDPreset_Changed;
            btnRun.Click                    += BtnRun_Click;
            // ── Calibration et toggle Vrms ────────────────────────────────────
            btnCalibrateLevel.Click         += BtnCalibrateLevel_Click;
            rbAmpdBFS.CheckedChanged        += (_, _) => { if (rbAmpdBFS.Checked) SwitchAmplitudeUnit(toVrms: false); };
            rbAmpVrms.CheckedChanged        += (_, _) => { if (rbAmpVrms.Checked) SwitchAmplitudeUnit(toVrms: true);  };

            _doneTimer = new System.Windows.Forms.Timer { Interval = 200 };
            _doneTimer.Tick += DoneTimer_Tick;
            _levelTimer = new System.Windows.Forms.Timer { Interval = 80 };
            _levelTimer.Tick += LevelTimer_Tick;

            UpdateParamPanels();
            SetRunState(false);
            RefreshSynchLabel();
            // Initialiser les barres VU après affichage (ClientSize valide)
            Shown += (_, _) => { pnlVuMeter.Invalidate(); RefreshCalibrationLabels(); RefreshVrmsToggle(); InvalidateWavePreview(); };
        }

        private void UpdateParamPanels()
        {
            var t = CurrentType();
            bool hasFreq   = t != SignalType.WhiteNoise && t != SignalType.PinkNoise;
            bool isSweep   = t == SignalType.Sweep;
            bool isTwoTone = t == SignalType.TwoTone;
            bool isBurst   = t == SignalType.ToneBurst;
            bool isPeriodic= t == SignalType.Sine || t == SignalType.Square
                          || t == SignalType.Triangle || t == SignalType.Sawtooth;

            // ── Visibilité ───────────────────────────────────────────────────
            nudF1.Visible      = hasFreq;
            lblHz1.Visible     = hasFreq;
            chkSynch.Visible   = hasFreq && !isBurst;
            lblNoFreq.Visible  = !hasFreq;

            pnlFreqPresets.Visible = isPeriodic;
            pnlAmpPresets.Visible  = isPeriodic;
            pnlSweep.Visible   = isSweep;
            pnlTwoTone.Visible = isTwoTone;
            pnlBurst.Visible   = isBurst;
            pnlDither.Visible  = isPeriodic;

            // ── Labels ───────────────────────────────────────────────────────
            lblFreqTitle.Text  = isSweep   ? "START"
                               : isBurst   ? "FREQ (BURST)"
                               : isTwoTone ? "FREQUENCY F1"
                               : "FREQUENCY";
            lblAmpTitle.Text   = isTwoTone ? "AMPLITUDE F1" : "AMPLITUDE";

            // ── Repositionnement dynamique (supprimer espace vide) ────────────
            if (isPeriodic)
            {   chkSynch.Top     = 138;
                pnlDither.Top    = 160;
                lblSynchFreq.Top = 205;
            }
            else if (isSweep)
            {   chkSynch.Top     = 78;
                lblSynchFreq.Top = 100;
                pnlSweep.Top     = 118;
            }
            else if (isTwoTone)
            {   chkSynch.Top     = 78;
                lblSynchFreq.Top = 100;
                pnlTwoTone.Top   = 120;
            }
            else if (isBurst)
            {   pnlBurst.Top = 80; }

            // ── Mise à jour du toggle Vrms (dépend du type) ──────────────────
            RefreshVrmsToggle();
        }

        // ── Synch ─────────────────────────────────────────────────────────────
        private void RefreshSynchLabel()
        {
            if (!chkSynch.Checked || !nudF1.Visible) { lblSynchFreq.Visible = false; return; }
            double fc = CoherentFreq((double)nudF1.Value);
            lblSynchFreq.Text    = $"→ {fc:F2} Hz";
            lblSynchFreq.Visible = true;
        }

        private double CoherentFreq(double f)
        {
            int sr = _getAnalyzerSR(), n = _getAnalyzerN();
            if (sr <= 0 || n <= 0) return f;
            int k = Math.Max(1, Math.Min(n/2-1, (int)Math.Round(f * n / (double)sr)));
            return k * (double)sr / n;
        }

        // ── IMD presets ───────────────────────────────────────────────────────
        private void CmbIMDPreset_Changed(object? sender, EventArgs e)
        {
            if (cmbIMDPreset.SelectedItem is not IMDPreset p || p.IsManual) return;
            nudF1.Value = (decimal)p.F1Hz;  nudF2.Value = (decimal)p.F2Hz;
            nudAmplitude.Value = (decimal)p.AmpF1Dbfs;
            nudAmplitudeF2.Value  = (decimal)p.AmpF2Dbfs;
        }

        // ── Status périphérique ───────────────────────────────────────────────
        public new void Show(IWin32Window owner)
        {
            RefreshDeviceStatus();
            if (!Visible) base.Show(owner);
            BringToFront();
        }

        public new void Show()
        {
            RefreshDeviceStatus();
            if (!Visible) base.Show();
            BringToFront();
        }

        private void RefreshDeviceStatus()
        {
            var outDev = _getOutputDevice();
            var inDev  = _getInputDevice();
            var outFmt = _getOutputFormat();

            if (chkLoopback.Checked)
            {
                int sr = outFmt?.SampleRate > 0 ? outFmt.SampleRate : outDev != null ? MixSR(outDev) : 48000;
                lblSR.Text = $"SR: {sr:N0} Hz  (Virtual)";
                lblLoopback.Visible = true;
                btnRun.Enabled = true;
                UpdateNudMax(sr);
            }
            else if (outDev != null)
            {
                int sr = outFmt?.SampleRate > 0 ? outFmt.SampleRate : MixSR(outDev);
                lblSR.Text = $"SR: {sr:N0} Hz  ({(outFmt != null ? "Exclusive" : "Shared")})";
                lblLoopback.Visible = inDev != null && outDev.ID == inDev.ID;
                btnRun.Enabled = true;
                UpdateNudMax(sr);
            }
            else
            {
                lblSR.Text = "SR: — (no output device)";
                lblLoopback.Visible = false;
                btnRun.Enabled = _isRunning;
            }
        }

        private void UpdateNudMax(int sr)
        {
            nudF1.Maximum = nudSweepEnd.Maximum = nudF2.Maximum = (decimal)(sr / 2 - 1);
            btnF100k.Enabled   = 100000 <= sr / 2;
            btnF100k.ForeColor = btnF100k.Enabled ? SystemColors.ControlText : SystemColors.GrayText;
        }

        private static int MixSR(MMDevice d) { try { return d.AudioClient.MixFormat.SampleRate; } catch { return 48000; } }

        // ── RUN / STOP ────────────────────────────────────────────────────────
        private void BtnRun_Click(object? s, EventArgs e) { if (_isRunning) StopGenerator(); else StartGenerator(); }

        private void StartGenerator()
        {
            double f1   = chkSynch.Checked ? CoherentFreq((double)nudF1.Value) : (double)nudF1.Value;
            int    ch   = rbL.Checked ? 0 : rbR.Checked ? 1 : 2;
            double amp  = GetAmplitudeLinear(ch);
            double ampF2= Math.Pow(10.0, (double)nudAmplitudeF2.Value / 20.0);
            bool   inv  = chkInvert.Checked && ch == 2;
            var    type = CurrentType();

            int dither = chkDitherEnable.Checked ? (int)nudDitherBits.Value : 0;

            var p = new SignalParams(
                type:               type,
                f1Hz:               f1,
                f2Hz:               type == SignalType.Sweep ? (double)nudSweepEnd.Value : (double)nudF2.Value,
                amp:                amp,
                ampF2:              ampF2,
                channel:            ch,
                invertSecond:       inv,
                sweepDuration:      (double)nudSweepTime.Value,
                sweepLog:           chkSweepLog.Checked,
                sweepLoop:          chkSweepLoop.Checked,
                burstOnValue:       rbBurstOnPer.Checked ? (double)nudBurstOnPer.Value : (double)nudBurstOnMs.Value,
                burstOnInPeriods:   rbBurstOnPer.Checked,
                burstOffValue:      rbBurstOffPer.Checked ? (double)nudBurstOffPer.Value : (double)nudBurstOffMs.Value,
                burstOffInPeriods:  rbBurstOffPer.Checked,
                burstWindow:        BurstWindows.Labels[Math.Max(0, cmbBurstWindow.SelectedIndex)].Type,
                burstLoop:          chkBurstLoop.Checked,
                ditheringBits:      dither);

            if (chkLoopback.Checked) { StartLoopback(p); return; }

            var outDev = _getOutputDevice();
            if (outDev == null)
            {
                MessageBox.Show(this, "No output device selected.\nOpen Devices and choose a Generator Output.",
                    "Generator", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var fmt = _getOutputFormat();
            try
            {
                int sr = fmt?.SampleRate > 0 ? fmt.SampleRate : MixSR(outDev);
                _currentSR = sr;
                var rawProv = SignalProviderFactory.Create(p, sr);
                _rawProvider = rawProv;
                _levelProbe = new LevelMeasuringProvider(rawProv);
                _provider = _levelProbe;
                if (fmt != null) StartExclusive(outDev, fmt, _provider);
                else             StartShared(outDev, _provider);
                SetRunState(true);
                _onRunStateChanged?.Invoke();
                StartDoneTimerIfOneShot(type, p);
            }
            catch (Exception ex) { Cleanup(); MessageBox.Show(this, $"Cannot start:\n{ex.Message}", "Generator", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void StartDoneTimerIfOneShot(SignalType t, SignalParams p)
        {
            bool oneShot = (t == SignalType.Sweep && !p.SweepLoop)
                        || (t == SignalType.ToneBurst && !p.BurstLoop);
            if (oneShot) _doneTimer.Start();
        }

        private void DoneTimer_Tick(object? s, EventArgs e)
        {
            bool done = (_rawProvider as SweepProvider)?.Done == true
                     || (_rawProvider as ToneBurstProvider)?.Done == true;
            if (done) StopGenerator();
        }

        private void StartShared(MMDevice dev, IWaveProvider prov)
        {
            _wasapiShared = new WasapiOut(dev, AudioClientShareMode.Shared, false, 50);
            _wasapiShared.Init(prov);
            _wasapiShared.Play();
        }

        private void StartExclusive(MMDevice dev, WaveFormatExtensible req, IWaveProvider prov)
        {
            WaveFormat[] cands = { WaveFormat.CreateIeeeFloatWaveFormat(req.SampleRate, 2),
                                   new WaveFormatExtensible(req.SampleRate, 32, 2),
                                   new WaveFormatExtensible(req.SampleRate, 24, 2),
                                   new WaveFormatExtensible(req.SampleRate, 16, 2) };
            WaveFormat? drv = null;
            foreach (var wf in cands)
                try { if (dev.AudioClient.IsFormatSupported(AudioClientShareMode.Exclusive, wf)) { drv=wf; break; } } catch {}
            if (drv == null) throw new InvalidOperationException($"SR {req.SampleRate/1000.0:0.###} kHz not supported in Exclusive mode.\nUse Auto in Devices.");
            _wasapiExclusive = new ExclusiveWasapiPlayback(dev, drv, prov);
            _wasapiExclusive.PlaybackStopped += (_, a) => { if (a.Exception != null && IsHandleCreated) BeginInvoke(() => { StopGenerator(); MessageBox.Show(this, $"Stopped:\n{a.Exception.Message}", "Generator", MessageBoxButtons.OK, MessageBoxIcon.Warning); }); };
            _wasapiExclusive.Play();
        }

        private void StartLoopback(SignalParams p)
        {
            int sr = _getOutputFormat()?.SampleRate > 0 ? _getOutputFormat()!.SampleRate
                   : (_getOutputDevice() is MMDevice d ? MixSR(d) : 48000);
            try
            {
                var rawProv2 = SignalProviderFactory.Create(p, sr);
                _rawProvider = rawProv2;
                _levelProbe = new LevelMeasuringProvider(rawProv2);
                _provider = _levelProbe;
                _onLoopbackRequest(_provider);
                _loopbackActive = true;
                SetRunState(true);
                StartDoneTimerIfOneShot(p.Type, p);
            }
            catch (Exception ex) { _loopbackActive=false; _provider=null; MessageBox.Show(this, ex.Message, "Generator", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void StopGenerator()
        {
            _currentSR = 0;
            _doneTimer.Stop();
            if (_loopbackActive) { _onLoopbackRequest(null); _loopbackActive = false; }
            Cleanup();
            SetRunState(false);
            _onRunStateChanged?.Invoke();
        }

        private void Cleanup()
        {
            try { _wasapiShared?.Stop(); }    catch {}
            try { _wasapiExclusive?.Stop(); } catch {}
            _wasapiShared?.Dispose(); _wasapiExclusive?.Dispose();
            _wasapiShared=null; _wasapiExclusive=null; _provider=null; _rawProvider=null;
        }

        private void SetRunState(bool on)
        {
            _isRunning = on;
            if (on) _levelTimer.Start();
            else  { _levelTimer.Stop(); ResetVuMeter(); }
            btnRun.BackColor = on ? ColorRunOn : ColorRunOff;
            btnRun.ForeColor = Color.White;
            btnRun.Text      = on ? "■  STOP" : "▶  RUN";
            bool off = !on;
            cmbType.Enabled=nudF1.Enabled=nudAmplitude.Enabled=nudF2.Enabled=nudAmplitudeF2.Enabled=off;
            nudSweepEnd.Enabled=nudSweepTime.Enabled=chkSweepLog.Enabled=chkSweepLoop.Enabled=off;
            nudBurstOnPer.Enabled=nudBurstOnMs.Enabled=off;
            nudBurstOffPer.Enabled=nudBurstOffMs.Enabled=cmbBurstWindow.Enabled=off;
            rbBurstOnPer.Enabled=rbBurstOnMs.Enabled=rbBurstOffPer.Enabled=rbBurstOffMs.Enabled=off;
            chkBurstLoop.Enabled=chkSynch.Enabled=chkLoopback.Enabled=off;
            pnlFreqPresets.Enabled=pnlAmpPresets.Enabled=off;
            cmbIMDPreset.Enabled=rbL.Enabled=rbR.Enabled=rbLR.Enabled=off;
            chkDitherEnable.Enabled=nudDitherBits.Enabled=off;
            chkInvert.Enabled=off && rbLR.Checked;
            // Le toggle Vrms est verrouillé pendant la génération
            rbAmpdBFS.Enabled=rbAmpVrms.Enabled=off;
            btnCalibrateLevel.Enabled=off;
            if (off) { RefreshDeviceStatus(); RefreshVrmsToggle(); }
        }

        private SignalType CurrentType() => cmbType.SelectedItem is TypeItem ti ? ti.Type : SignalType.Sine;

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            { e.Cancel=true; if (_isRunning) StopGenerator(); Hide(); return; }
            StopGenerator(); _doneTimer.Dispose(); _levelTimer.Dispose();
            _calDialog?.Close();
            base.OnFormClosing(e);
        }


        // ── Calibration de niveau de sortie ──────────────────────────────────

        private void BtnCalibrateLevel_Click(object? s, EventArgs e)
        {
            // 1) Forcer le type Sinus
            var sineItem = cmbType.Items.Cast<TypeItem>()
                                   .FirstOrDefault(ti => ti.Type == SignalType.Sine);
            if (sineItem != null && !_isRunning)
                cmbType.SelectedItem = sineItem;

            // 2) Ouvrir/ramener le dialogue (non-modal)
            if (_calDialog == null || _calDialog.IsDisposed)
            {
                _calDialog = new GeneratorCalibrationDialog
                {
                    GetCurrentAmplitudeDbfs = () =>
                        _isRunning ? (double?)nudAmplitude.Value : null,
                    OnCalibrationApplied = ApplyCalibration
                };

                // Positionner à gauche de la fenêtre générateur
                _calDialog.StartPosition = FormStartPosition.Manual;
                _calDialog.Location = new System.Drawing.Point(
                    Math.Max(0, Left - _calDialog.Width - 8),
                    Top);
            }

            if (!_calDialog.Visible) _calDialog.Show(this);
            _calDialog.BringToFront();
        }

        private void ApplyCalibration(double vrmsFS_L, double vrmsFS_R)
        {
            _calValidL = true; _calVrmsL = vrmsFS_L;
            _calValidR = true; _calVrmsR = vrmsFS_R;
            _saveSettings();
            RefreshCalibrationLabels();
            RefreshVrmsToggle();
        }

        private void RefreshCalibrationLabels()
        {
            if (_calValidL)
            {
                lblCalL.Text      = $"L: FS = {_calVrmsL:G4} Vrms";
                lblCalL.ForeColor = System.Drawing.SystemColors.GrayText;
            }
            else
            {
                lblCalL.Text      = "L: FS = — Vrms";
                lblCalL.ForeColor = System.Drawing.SystemColors.GrayText;
            }

            if (_calValidR)
            {
                lblCalR.Text      = $"R: FS = {_calVrmsR:G4} Vrms";
                lblCalR.ForeColor = System.Drawing.SystemColors.GrayText;
            }
            else
            {
                lblCalR.Text      = "R: FS = — Vrms";
                lblCalR.ForeColor = System.Drawing.SystemColors.GrayText;
            }
        }

        /// <summary>
        /// Active/désactive le RadioButton "Vrms" selon la calibration et le type de signal.
        /// Le mode Vrms est disponible pour tous les types sauf Bruit blanc et Bruit rose.
        /// </summary>
        private void RefreshVrmsToggle()
        {
            bool calDone = _calValidL && _calValidR;
            var type     = CurrentType();
            bool noiseType = type == SignalType.WhiteNoise || type == SignalType.PinkNoise;

            rbAmpVrms.Enabled = calDone && !noiseType && !_isRunning;

            // Si la calibration vient d'être invalidée (reset), repasser en dBFS
            if (!calDone && rbAmpVrms.Checked)
                rbAmpdBFS.Checked = true;

            // Si on vient de sélectionner un type bruit, repasser en dBFS
            if (noiseType && rbAmpVrms.Checked)
                rbAmpdBFS.Checked = true;
        }

        /// <summary>
        /// Bascule le NUD amplitude entre le mode dBFS et le mode Vrms.
        /// </summary>
        private void SwitchAmplitudeUnit(bool toVrms)
        {
            if (toVrms)
            {
                // Utiliser VrmsFS du canal actif (L par défaut, ou R si R seul)
                double vrmsFS = rbR.Checked ? _calVrmsR : _calVrmsL;
                if (vrmsFS <= 0) vrmsFS = 1.0;

                // Convertir la valeur courante dBFS → Vrms
                double currentDbfs = (double)nudAmplitude.Value;
                double currentVrms = Math.Pow(10.0, currentDbfs / 20.0) * vrmsFS;

                nudAmplitude.Minimum      = 0.001m;
                nudAmplitude.Maximum      = (decimal)(vrmsFS * 1.2);
                nudAmplitude.DecimalPlaces = 3;
                nudAmplitude.Increment    = 0.01m;
                nudAmplitude.Value        = (decimal)Math.Round(Math.Clamp(currentVrms, 0.001, (double)(nudAmplitude.Maximum)), 3);
                lblDbfs.Visible           = false;   // caché, remplacé par les radio buttons
            }
            else
            {
                // Convertir la valeur courante Vrms → dBFS
                double vrmsFS = rbR.Checked ? _calVrmsR : _calVrmsL;
                if (vrmsFS <= 0) vrmsFS = 1.0;
                double currentVrms = (double)nudAmplitude.Value;
                double currentDbfs = 20.0 * Math.Log10(Math.Max(1e-10, currentVrms / vrmsFS));

                nudAmplitude.Minimum      = -180m;
                nudAmplitude.Maximum      = 0m;
                nudAmplitude.DecimalPlaces = 1;
                nudAmplitude.Increment    = 0.5m;
                nudAmplitude.Value        = (decimal)Math.Round(Math.Clamp(currentDbfs, -180.0, 0.0), 1);
                lblDbfs.Visible           = false;   // remplacé définitivement par les radio buttons
            }
        }

        /// <summary>
        /// Retourne l'amplitude linéaire [0..1] pour le canal ch (0=L, 1=R, 2=L+R).
        /// Tient compte du mode Vrms si actif.
        /// En mode L+R, VrmsFS_L est utilisé comme référence (cf. commentaire ci-dessous).
        /// Note : en L+R, les deux canaux reçoivent le même dBFS. Si le hardware a une légère
        /// asymétrie entre L et R, la tension de sortie sur R différera de la valeur Vrms entrée.
        /// Pour une précision maximale, calibrer et utiliser L ou R séparément.
        /// </summary>
        private double GetAmplitudeLinear(int ch)
        {
            if (rbAmpVrms.Checked)
            {
                double vrmsFS = (ch == 1) ? _calVrmsR : _calVrmsL;  // R seul → VrmsFS_R, sinon VrmsFS_L
                if (vrmsFS <= 0) vrmsFS = 1.0;
                double vrms = (double)nudAmplitude.Value;
                return Math.Clamp(vrms / vrmsFS, 0.0, 1.0);
            }
            return Math.Pow(10.0, (double)nudAmplitude.Value / 20.0);
        }

        // ── Prévisualisation forme d'onde ─────────────────────────────────────

        private void InitWavePreview()
        {
            pnlWavePreview.Paint += PnlWavePreview_Paint;
        }

        /// <summary>Invalide la preview — à appeler sur tout changement de paramètre visible.</summary>
        private void InvalidateWavePreview() => pnlWavePreview?.Invalidate();

        private void PnlWavePreview_Paint(object? sender, PaintEventArgs e)
        {
            var g  = e.Graphics;
            var sz = pnlWavePreview.ClientSize;
            if (sz.Width < 10 || sz.Height < 10) return;

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            float yMid = sz.Height / 2f;
            float yAmp = sz.Height * 0.40f;

            float[] samples = GenerateWavePreviewSamples(sz.Width);

            float peak = 0f;
            foreach (var s in samples) if (Math.Abs(s) > peak) peak = Math.Abs(s);
            if (peak < 1e-6f) peak = 1f;
            float scale = yAmp * 0.94f / peak;

            var pts = new PointF[samples.Length];
            for (int i = 0; i < samples.Length; i++)
                pts[i] = new PointF(i, yMid - samples[i] * scale);

            using var penWave = new Pen(SystemColors.GrayText, 1.3f);
            g.DrawLines(penWave, pts);
        }

        private float[] GenerateWavePreviewSamples(int count)
        {
            var samples = new float[count];
            var type    = CurrentType();

            switch (type)
            {
                case SignalType.Sine:
                    for (int i = 0; i < count; i++)
                        samples[i] = (float)Math.Sin(2 * Math.PI * 4.5 * i / count);
                    break;

                case SignalType.Square:
                    for (int i = 0; i < count; i++)
                        samples[i] = ((4.5 * i / count) % 1.0) < 0.5 ? 1f : -1f;
                    break;

                case SignalType.Triangle:
                    for (int i = 0; i < count; i++)
                    {
                        double ph = (4.5 * i / count) % 1.0;
                        samples[i] = (float)(ph < 0.25 ? 4*ph : ph < 0.75 ? 2 - 4*ph : 4*ph - 4);
                    }
                    break;

                case SignalType.Sawtooth:
                    for (int i = 0; i < count; i++)
                        samples[i] = (float)(2.0 * ((4.5 * i / count) % 1.0) - 1.0);
                    break;

                case SignalType.WhiteNoise:
                {
                    var rng = new Random(137);
                    for (int i = 0; i < count; i++)
                        samples[i] = (float)(rng.NextDouble() * 2 - 1);
                    break;
                }

                case SignalType.PinkNoise:
                {
                    // Filtre IIR Paul Kellet (warmup de 100 samples)
                    var rng = new Random(137);
                    double b0=0,b1=0,b2=0,b3=0,b4=0,b5=0,b6=0;
                    int total = count + 100;
                    for (int i = 0; i < total; i++)
                    {
                        double w = rng.NextDouble() * 2 - 1;
                        b0 = 0.99886*b0 + w*0.0555179; b1 = 0.99332*b1 + w*0.0750759;
                        b2 = 0.96900*b2 + w*0.1538520; b3 = 0.86650*b3 + w*0.3104856;
                        b4 = 0.55000*b4 + w*0.5329522; b5 = -0.7616*b5 - w*0.0168980;
                        double pink = (b0+b1+b2+b3+b4+b5+b6 + w*0.5362) * 0.11;
                        b6 = w * 0.115926;
                        if (i >= 100) samples[i - 100] = (float)Math.Tanh(pink);
                    }
                    break;
                }

                case SignalType.Sweep:
                {
                    // Sweep log : 0.5 → 7 cycles sur la largeur
                    bool isLog = chkSweepLog.Checked;
                    double ph = 0;
                    for (int i = 0; i < count; i++)
                    {
                        double t    = (double)i / count;
                        double fNorm = isLog
                            ? 0.5 * Math.Pow(14.0, t)            // 0.5 → 7 cycles/panel (log)
                            : 0.5 + 6.5 * t;                      // 0.5 → 7 cycles/panel (lin)
                        ph += 2 * Math.PI * fNorm / count;
                        samples[i] = (float)Math.Sin(ph);
                    }
                    break;
                }

                case SignalType.TwoTone:
                {
                    // Ratio 3:5 pour visualiser l'IMD
                    for (int i = 0; i < count; i++)
                        samples[i] = (float)(Math.Sin(2*Math.PI*3*i/count)*0.5
                                           + Math.Sin(2*Math.PI*5*i/count)*0.5);
                    break;
                }

                case SignalType.ToneBurst:
                {
                    // Lire les paramètres actuels
                    int onPer  = rbBurstOnPer.Checked
                        ? Math.Max(1, Math.Min(12, (int)nudBurstOnPer.Value))
                        : 5;
                    int offPer = 3;
                    int totalPer = onPer + 2 * offPer;
                    int onStart  = (int)(offPer  * count / (double)totalPer);
                    int onEnd    = (int)((offPer + onPer) * count / (double)totalPer);
                    int onLen    = Math.Max(2, onEnd - onStart);

                    var winType = BurstWindows.Labels[
                        Math.Clamp(cmbBurstWindow.SelectedIndex, 0, BurstWindows.Labels.Length-1)].Type;
                    float[] win;
                    try   { win = BurstWindows.Compute(onLen, winType); }
                    catch { win = new float[onLen]; for (int k=0;k<onLen;k++) win[k]=1f; }

                    for (int i = onStart; i < onEnd; i++)
                    {
                        int j  = i - onStart;
                        double phase = 2 * Math.PI * onPer * j / (double)onLen;
                        float  w = j < win.Length ? win[j] : 1f;
                        samples[i] = (float)Math.Sin(phase) * w;
                    }
                    break;
                }
            }
            return samples;
        }

        // ── VU-mètre ──────────────────────────────────────────────────────────
        private const int    _vuLedCount  = 40;
        private const double _vuDbMin     = -80.0;
        private const double _vuBlueEnd   = -36.0;
        private const double _vuGreenEnd  = -12.0;
        private const double _vuOrangeEnd = -6.0;

        private void InitVuBars()
        {
            // On dessine TOUT dans pnlVuMeter.Paint :
            // un seul handler, pas de sous-contrôles, pas de z-order issues.
            pnlVuMeter.BackColor = Color.FromArgb(25, 25, 25);
            pnlVuMeter.Paint += PnlVuMeter_Paint;
        }

        private void PnlVuMeter_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var sz = pnlVuMeter.ClientSize;
            g.Clear(Color.FromArgb(25, 25, 25));

            // Barre gauche (L)  — y=4, hauteur=16
            PaintVuBar(g, new Rectangle(22, 4, sz.Width - 116, 16), _vuPctL);
            // Barre droite (R)  — y=28, hauteur=16
            PaintVuBar(g, new Rectangle(22, 28, sz.Width - 116, 16), _vuPctR);

            // Textes "L" / "R"
            using var fSmall = new Font("Segoe UI", 7f);
            TextRenderer.DrawText(g, "L", fSmall,
                new Rectangle(4, 4, 16, 16), Color.FromArgb(140,140,140),
                TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, "R", fSmall,
                new Rectangle(4, 28, 16, 16), Color.FromArgb(140,140,140),
                TextFormatFlags.VerticalCenter);

            // Valeurs numériques
            using var fLevel = new Font("Segoe UI", 7.5f);
            int lx = sz.Width - 112;
            TextRenderer.DrawText(g, _vuLevelStrL, fLevel,
                new Rectangle(lx, 4, 108, 16), Color.Lime,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
            TextRenderer.DrawText(g, _vuLevelStrR, fLevel,
                new Rectangle(lx, 28, 108, 16), Color.Lime,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
        }

        private void LevelTimer_Tick(object? s, EventArgs e)
        {
            if (_levelProbe == null) return;
            var (l, r) = _levelProbe.GetAndResetPeak();
            double dbL = l > 0f ? 20.0 * Math.Log10(l) : double.NegativeInfinity;
            double dbR = r > 0f ? 20.0 * Math.Log10(r) : double.NegativeInfinity;
            _vuPctL     = DbToPct(dbL);  _vuLevelStrL = FmtDb(dbL);
            _vuPctR     = DbToPct(dbR);  _vuLevelStrR = FmtDb(dbR);
            pnlVuMeter.Invalidate();
        }

        private void ResetVuMeter()
        {
            _vuPctL = _vuPctR = 0;
            _vuLevelStrL = _vuLevelStrR = "-∞ dBFS";
            pnlVuMeter.Invalidate();
        }

        private static int DbToPct(double db)
            => db <= _vuDbMin ? 0 : db >= 0.0 ? 100
             : (int)((db - _vuDbMin) / -_vuDbMin * 100.0);

        private static string FmtDb(double db)
            => double.IsNegativeInfinity(db) ? "-∞ dBFS" : $"{db:F1} dBFS";

        private static void PaintVuBar(Graphics g, Rectangle r, int pct)
        {
            if (r.Width <= 4 || r.Height <= 4) return;
            int vm   = Math.Max(1, r.Height / 6);
            int lh   = r.Height - 2 * vm;
            int nLed = _vuLedCount;
            double pB = (_vuBlueEnd   - _vuDbMin) / -_vuDbMin * 100.0;
            double pG = (_vuGreenEnd  - _vuDbMin) / -_vuDbMin * 100.0;
            double pO = (_vuOrangeEnd - _vuDbMin) / -_vuDbMin * 100.0;

            for (int i = 0; i < nLed; i++)
            {
                int x0 = r.X + (int)((double)i       / nLed * r.Width);
                int x1 = r.X + (int)((double)(i + 1) / nLed * r.Width);
                int w  = Math.Max(1, x1 - x0 - 2);
                double cp = ((i + 0.5) / nLed) * 100.0;
                Color c = cp > pct                 ? Color.FromArgb(64, 64, 64)
                        : cp <= pB                 ? Color.FromArgb(0, 120, 215)
                        : cp <= pG                 ? Color.Lime
                        : cp <= pO                 ? Color.Gold
                                                   : Color.Red;
                using var br = new SolidBrush(c);
                g.FillRectangle(br, x0, r.Y + vm, w, lh);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private sealed class TypeItem { public readonly SignalType Type;
            public TypeItem(SignalType t) => Type=t;
            public override string ToString() => Type switch {
                SignalType.Sine=>"Sine", SignalType.Square=>"Square",
                SignalType.Triangle=>"Triangle", SignalType.Sawtooth=>"Sawtooth",
                SignalType.WhiteNoise=>"White noise", SignalType.PinkNoise=>"Pink noise",
                SignalType.Sweep=>"Sweep", SignalType.TwoTone=>"Two-tone  (IMD)",
                SignalType.ToneBurst=>"Tone burst", _=>Type.ToString() }; }

        internal sealed class IMDPreset {
            public readonly string Name; public readonly double F1Hz,F2Hz,AmpF1Dbfs,AmpF2Dbfs; public readonly bool IsManual;
            public IMDPreset(string n,double f1,double f2,double a1,double a2){Name=n;F1Hz=f1;F2Hz=f2;AmpF1Dbfs=a1;AmpF2Dbfs=a2;}
            public IMDPreset(){Name="Manual";IsManual=true;}
            public override string ToString()=>Name; }

        internal static IMDPreset[] IMDPresets => new[]{
            new IMDPreset(),
            new IMDPreset("SMPTE / IEC 60268  (60 Hz + 7 kHz, 4:1)",  60, 7000,-3.5,-15.5),
            new IMDPreset("DIN / IEC 60268-21 (250 Hz + 8 kHz, 1:1)",250, 8000,-6.0, -6.0),
            new IMDPreset("CCIF / ITU-T       (19 kHz + 20 kHz, 1:1)",19000,20000,-6.0,-6.0),
            new IMDPreset("IHF                (18 kHz + 19 kHz, 1:1)",18000,19000,-6.0,-6.0) };
    }
}
