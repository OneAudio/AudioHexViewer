using System;
using System.Drawing;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AudioHexViewer
{
    internal partial class GeneratorWindow : Form
    {
        // ── Couleurs identiques à l'analyseur ─────────────────────────────────
        private static readonly Color ColorRunOff = Color.SeaGreen;
        private static readonly Color ColorRunOn  = Color.Firebrick;

        // ── Délégués vers Form1 ───────────────────────────────────────────────
        private readonly Func<MMDevice?>              _getOutputDevice;
        private readonly Func<MMDevice?>              _getInputDevice;
        private readonly Func<WaveFormatExtensible?>  _getOutputFormat;
        private readonly Func<int>                    _getAnalyzerSR;
        private readonly Func<int>                    _getAnalyzerN;
        private readonly Action<IWaveProvider?>       _onLoopbackRequest;

        // ── État ──────────────────────────────────────────────────────────────
        private WasapiOut?               _wasapiShared;
        private ExclusiveWasapiPlayback? _wasapiExclusive;
        private IWaveProvider?           _provider;
        private bool                     _isRunning;
        private bool                     _loopbackActive;

        // ──────────────────────────────────────────────────────────────────────
        public GeneratorWindow(
            Func<MMDevice?>             getOutputDevice,
            Func<MMDevice?>             getInputDevice,
            Func<WaveFormatExtensible?> getOutputFormat,
            Func<int>                   getAnalyzerSR,
            Func<int>                   getAnalyzerN,
            Action<IWaveProvider?>      onLoopbackRequest)
        {
            _getOutputDevice   = getOutputDevice;
            _getInputDevice    = getInputDevice;
            _getOutputFormat   = getOutputFormat;
            _getAnalyzerSR     = getAnalyzerSR;
            _getAnalyzerN      = getAnalyzerN;
            _onLoopbackRequest = onLoopbackRequest;

            InitializeComponent();

            // Peupler le combobox type
            foreach (var v in Enum.GetValues<SignalType>())
                cmbType.Items.Add(new TypeItem(v));
            cmbType.SelectedIndex = 0;

            // Événements
            cmbType.SelectedIndexChanged   += (_, _) => { UpdateParamPanels(); RefreshSynchLabel(); RefreshDeviceStatus(); };
            nudF1.ValueChanged             += (_, _) => RefreshSynchLabel();
            chkSynch.CheckedChanged        += (_, _) => RefreshSynchLabel();
            chkLoopback.CheckedChanged     += (_, _) => RefreshDeviceStatus();
            rbL.CheckedChanged             += (_, _) => chkInvert.Enabled = rbLR.Checked;
            rbR.CheckedChanged             += (_, _) => chkInvert.Enabled = rbLR.Checked;
            rbLR.CheckedChanged            += (_, _) => chkInvert.Enabled = rbLR.Checked;
            btnPreset100.Click             += (_, _) => nudF1.Value = 100m;
            btnPreset1k.Click              += (_, _) => nudF1.Value = 1000m;
            btnPreset10k.Click             += (_, _) => nudF1.Value = 10000m;
            btnPresetNyq.Click             += (_, _) => nudF1.Value = nudF1.Maximum;
            btnRun.Click                   += BtnRun_Click;

            UpdateParamPanels();
            SetRunState(false);
            RefreshSynchLabel();
        }

        // ── Show / Hide panels selon le type de signal ────────────────────────
        private void UpdateParamPanels()
        {
            var type = CurrentType();
            bool hasFreq  = type != SignalType.WhiteNoise && type != SignalType.PinkNoise;
            bool isSweep  = type == SignalType.Sweep;
            bool isTwoTone= type == SignalType.TwoTone;

            nudF1.Visible          = hasFreq;
            lblHz1.Visible         = hasFreq;
            pnlPresets.Visible     = hasFreq;
            chkSynch.Visible       = hasFreq;
            lblNoFreq.Visible      = !hasFreq;

            pnlSweep.Visible       = isSweep;
            pnlTwoTone.Visible     = isTwoTone;

            lblFreqTitle.Text      = isSweep ? "START" : "FREQUENCY";
        }

        // ── Fréquence cohérente ───────────────────────────────────────────────
        private void RefreshSynchLabel()
        {
            if (!chkSynch.Checked || !nudF1.Visible)
            { lblSynchFreq.Visible = false; return; }
            double fc = CoherentFreq((double)nudF1.Value);
            lblSynchFreq.Text    = $"→ {fc:F2} Hz";
            lblSynchFreq.Visible = true;
        }

        private double CoherentFreq(double f)
        {
            int sr = _getAnalyzerSR(), n = _getAnalyzerN();
            if (sr <= 0 || n <= 0) return f;
            int k = Math.Max(1, Math.Min(n / 2 - 1, (int)Math.Round(f * n / (double)sr)));
            return k * (double)sr / n;
        }

        // ── Rafraîchissement périphérique ─────────────────────────────────────
        public new void Show(IWin32Window owner)
        { RefreshDeviceStatus(); base.Show(owner); BringToFront(); }

        public new void Show()
        { RefreshDeviceStatus(); base.Show(); BringToFront(); }

        private void RefreshDeviceStatus()
        {
            var outDev = _getOutputDevice();
            var inDev  = _getInputDevice();
            var outFmt = _getOutputFormat();

            if (chkLoopback.Checked)
            {
                int sr = outFmt?.SampleRate > 0 ? outFmt.SampleRate
                       : outDev != null ? MixSR(outDev) : 48000;
                lblSR.Text       = $"SR: {sr:N0} Hz  (Virtual)";
                lblLoopback.Visible = true;
                btnRun.Enabled   = !_isRunning || _loopbackActive;
                nudF1.Maximum    = (decimal)(sr / 2 - 1);
            }
            else if (outDev != null)
            {
                int sr = outFmt?.SampleRate > 0 ? outFmt.SampleRate : MixSR(outDev);
                string mode = outFmt != null ? "Exclusive" : "Shared";
                lblSR.Text       = $"SR: {sr:N0} Hz  ({mode})";
                bool loopback    = inDev != null && outDev.ID == inDev.ID;
                lblLoopback.Visible = loopback;
                btnRun.Enabled   = true;
                nudF1.Maximum    = (decimal)(sr / 2 - 1);
            }
            else
            {
                lblSR.Text       = "SR: — (no output device)";
                lblLoopback.Visible = false;
                btnRun.Enabled   = _isRunning;
            }
        }

        private static int MixSR(MMDevice d)
        { try { return d.AudioClient.MixFormat.SampleRate; } catch { return 48000; } }

        // ── Bouton RUN / STOP ─────────────────────────────────────────────────
        private void BtnRun_Click(object? sender, EventArgs e)
        {
            if (_isRunning) StopGenerator();
            else            StartGenerator();
        }

        private void StartGenerator()
        {
            double f1  = chkSynch.Checked ? CoherentFreq((double)nudF1.Value)
                                           : (double)nudF1.Value;
            double f2  = (double)nudF2.Value;
            double amp = Math.Pow(10.0, (double)nudAmplitude.Value / 20.0);
            int    ch  = rbL.Checked ? 0 : rbR.Checked ? 1 : 2;
            bool   inv = chkInvert.Checked && ch == 2;

            var p = new SignalParams(
                type:             CurrentType(),
                f1Hz:             f1,
                f2Hz:             CurrentType() == SignalType.Sweep
                                    ? (double)nudSweepEnd.Value
                                    : f2,
                amplitudeLinear:  amp,
                channel:          ch,
                invertSecond:     inv,
                sweepDurationSec: (double)nudSweepTime.Value,
                sweepLog:         chkSweepLog.Checked,
                sweepLoop:        chkSweepLoop.Checked);

            if (chkLoopback.Checked)
            { StartLoopback(p); return; }

            var outDev = _getOutputDevice();
            if (outDev == null)
            {
                MessageBox.Show(this,
                    "Aucun périphérique de sortie sélectionné.\nOuvre Devices et choisis un Generator Output.",
                    "Generator", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var fmt = _getOutputFormat();
            try
            {
                _provider = SignalProviderFactory.Create(p, fmt?.SampleRate > 0 ? fmt.SampleRate : MixSR(outDev));
                if (fmt != null) StartExclusive(outDev, fmt, _provider);
                else             StartShared(outDev, _provider);
                SetRunState(true);
            }
            catch (Exception ex)
            {
                Cleanup();
                MessageBox.Show(this, $"Impossible de démarrer le générateur :\n{ex.Message}",
                    "Generator", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StartShared(MMDevice dev, IWaveProvider prov)
        {
            _wasapiShared = new WasapiOut(dev, AudioClientShareMode.Shared, false, 50);
            _wasapiShared.Init(prov);
            _wasapiShared.Play();
        }

        private void StartExclusive(MMDevice dev, WaveFormatExtensible reqFmt, IWaveProvider prov)
        {
            WaveFormat? drv = FindExclusiveFormat(dev, reqFmt.SampleRate);
            if (drv == null)
                throw new InvalidOperationException(
                    $"SR {reqFmt.SampleRate / 1000.0:0.###} kHz non supporté en mode Exclusif.\n" +
                    "Choisis 'Auto (Windows)' dans Devices.");
            _wasapiExclusive = new ExclusiveWasapiPlayback(dev, drv, prov);
            _wasapiExclusive.PlaybackStopped += (_, a) =>
            {
                if (a.Exception != null && IsHandleCreated)
                    BeginInvoke(() => { StopGenerator();
                        MessageBox.Show(this, $"Arrêt inattendu :\n{a.Exception.Message}",
                            "Generator", MessageBoxButtons.OK, MessageBoxIcon.Warning); });
            };
            _wasapiExclusive.Play();
        }

        private void StartLoopback(SignalParams p)
        {
            var outFmt = _getOutputFormat();
            var outDev = _getOutputDevice();
            int sr = outFmt?.SampleRate > 0 ? outFmt.SampleRate
                   : outDev != null ? MixSR(outDev) : 48000;
            try
            {
                _provider = SignalProviderFactory.Create(p, sr);
                _onLoopbackRequest(_provider);
                _loopbackActive = true;
                SetRunState(true);
            }
            catch (Exception ex)
            {
                _loopbackActive = false; _provider = null;
                MessageBox.Show(this, $"Impossible de démarrer le loopback :\n{ex.Message}",
                    "Generator", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static WaveFormat? FindExclusiveFormat(MMDevice dev, int sr)
        {
            WaveFormat[] cands =
            {
                WaveFormat.CreateIeeeFloatWaveFormat(sr, 2),
                new WaveFormatExtensible(sr, 32, 2),
                new WaveFormatExtensible(sr, 24, 2),
                new WaveFormatExtensible(sr, 16, 2),
            };
            foreach (var wf in cands)
                try { if (dev.AudioClient.IsFormatSupported(AudioClientShareMode.Exclusive, wf)) return wf; }
                catch { }
            return null;
        }

        private void StopGenerator()
        {
            if (_loopbackActive) { _onLoopbackRequest(null); _loopbackActive = false; }
            Cleanup();
            SetRunState(false);
        }

        private void Cleanup()
        {
            try { _wasapiShared?.Stop(); }    catch { }
            try { _wasapiExclusive?.Stop(); } catch { }
            _wasapiShared?.Dispose();
            _wasapiExclusive?.Dispose();
            _wasapiShared    = null;
            _wasapiExclusive = null;
            _provider        = null;
        }

        private void SetRunState(bool running)
        {
            _isRunning       = running;
            btnRun.BackColor = running ? ColorRunOn : ColorRunOff;
            btnRun.ForeColor = Color.White;
            btnRun.Text      = running ? "■  STOP" : "▶  RUN";

            cmbType.Enabled      = !running;
            nudF1.Enabled        = !running;
            nudF2.Enabled        = !running;
            nudAmplitude.Enabled = !running;
            nudSweepEnd.Enabled  = !running;
            nudSweepTime.Enabled = !running;
            chkSynch.Enabled     = !running;
            chkSweepLog.Enabled  = !running;
            chkSweepLoop.Enabled = !running;
            rbL.Enabled          = !running;
            rbR.Enabled          = !running;
            rbLR.Enabled         = !running;
            chkInvert.Enabled    = !running && rbLR.Checked;
            chkLoopback.Enabled  = !running;
            pnlPresets.Enabled   = !running;

            if (!running) RefreshDeviceStatus();
        }

        private SignalType CurrentType()
            => cmbType.SelectedItem is TypeItem ti ? ti.Type : SignalType.Sine;

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            { e.Cancel = true; if (_isRunning) StopGenerator(); Hide(); return; }
            StopGenerator();
            base.OnFormClosing(e);
        }

        // ── Helper ────────────────────────────────────────────────────────────
        private sealed class TypeItem
        {
            public readonly SignalType Type;
            private readonly string    _label;
            public TypeItem(SignalType t) { Type = t; _label = TypeLabel(t); }
            public override string ToString() => _label;
        }

        private static string TypeLabel(SignalType t) => t switch
        {
            SignalType.Sine       => "Sine",
            SignalType.Square     => "Square",
            SignalType.Triangle   => "Triangle",
            SignalType.Sawtooth   => "Sawtooth",
            SignalType.WhiteNoise => "White noise",
            SignalType.PinkNoise  => "Pink noise",
            SignalType.Sweep      => "Sweep",
            SignalType.TwoTone    => "Two-tone  (IMD)",
            _                     => t.ToString()
        };
    }
}
