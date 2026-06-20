using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.IO;

namespace AudioHexViewer
{
    internal partial class DevicesDialog : Form
    {
        // ── Cache SR ──────────────────────────────────────────────────────────
        internal static readonly Dictionary<(string id, int sr), bool> SrCache
            = new Dictionary<(string id, int sr), bool>();

        // ── Résultats ─────────────────────────────────────────────────────────
        public MMDevice?             SelectedInputDevice  { get; private set; }
        public WaveFormatExtensible? SelectedInputFormat  { get; private set; }
        public MMDevice?             SelectedOutputDevice { get; private set; }
        public WaveFormatExtensible? SelectedOutputFormat { get; private set; }
        public int                   SelectedBufferSize   { get; private set; } = 128 * 1024;
        public bool                  PostProcessMode      { get; private set; }
        public string?               PostProcessFilePath  { get; private set; }
        public bool                  PostProcessLoop      { get; private set; }

        private string? _postProcessFilePath;   // working copy while dialog is open

        private static readonly int[] BufferOptions =
            { 16*1024, 32*1024, 64*1024, 128*1024, 256*1024, 512*1024 };

        private readonly int[]   _srOptions;
        private readonly Action  _onInfos;

        public DevicesDialog(
            IReadOnlyList<MMDevice>  inputDevices,
            IReadOnlyList<MMDevice>  outputDevices,
            MMDevice?                currentInput,
            MMDevice?                currentOutput,
            int[]                    sampleRateOptions,
            WaveFormatExtensible?    currentInputFormat,
            WaveFormatExtensible?    currentOutputFormat,
            Action                   onInfosClick,
            int                      currentBufferSize   = 128 * 1024,
            bool                     currentPostProcess  = false,
            string?                  currentFilePath     = null,
            bool                     currentLoop         = false)
        {
            _srOptions = sampleRateOptions;
            _onInfos   = onInfosClick;
            InitializeComponent();

            // ── Buffer combo ─────────────────────────────────────────────
            foreach (int n in BufferOptions)
                cmbBuffer.Items.Add(new BufferItem(n));
            SelectedBufferSize = currentBufferSize;
            int selIdx = Array.IndexOf(BufferOptions, currentBufferSize);
            cmbBuffer.SelectedIndex = selIdx >= 0 ? selIdx : 3; // default 128k
            cmbBuffer.SelectedIndexChanged += (_, _) =>
            {
                if (cmbBuffer.SelectedItem is BufferItem bi)
                    SelectedBufferSize = bi.Value;
            };

            // ── Remplissage combos ────────────────────────────────────────────
            foreach (var d in inputDevices)
            {
                string name = SplashScreen.CachedNames.TryGetValue(d.ID, out var n) ? n : d.FriendlyName;
                cmbInputDevice.Items.Add(new DeviceItem(d, name));
            }
            foreach (var d in outputDevices)
            {
                string name = SplashScreen.CachedNames.TryGetValue(d.ID, out var n) ? n : d.FriendlyName;
                cmbOutputDevice.Items.Add(new DeviceItem(d, name));
            }

            SelectDevice(cmbInputDevice,  currentInput);
            SelectDevice(cmbOutputDevice, currentOutput);

            cmbInputDevice.SelectedIndexChanged  += (_, _) =>
            {
                RefreshSrCombo(cmbInputRate,  (cmbInputDevice.SelectedItem  as DeviceItem)?.Device, currentInputFormat);
                UpdateFormatLabel(lblInputFormat,  (cmbInputDevice.SelectedItem  as DeviceItem)?.Device, currentInputFormat);
            };
            cmbOutputDevice.SelectedIndexChanged += (_, _) =>
            {
                RefreshSrCombo(cmbOutputRate, (cmbOutputDevice.SelectedItem as DeviceItem)?.Device, currentOutputFormat);
                UpdateFormatLabel(lblOutputFormat, (cmbOutputDevice.SelectedItem as DeviceItem)?.Device, currentOutputFormat);
            };

            RefreshSrCombo(cmbInputRate,  (cmbInputDevice.SelectedItem  as DeviceItem)?.Device, currentInputFormat);
            RefreshSrCombo(cmbOutputRate, (cmbOutputDevice.SelectedItem as DeviceItem)?.Device, currentOutputFormat);
            UpdateFormatLabel(lblInputFormat,  (cmbInputDevice.SelectedItem  as DeviceItem)?.Device, currentInputFormat);
            UpdateFormatLabel(lblOutputFormat, (cmbOutputDevice.SelectedItem as DeviceItem)?.Device, currentOutputFormat);

            btnInfos.Click  += (_, _) => _onInfos?.Invoke();
            btnOk.Click     += BtnOk_Click;
            AcceptButton = btnOk;
            CancelButton = btnCancel;

            // ── Post-Process section ──────────────────────────────────────
            _postProcessFilePath = currentFilePath;
            chkLoop.Checked = currentLoop;
            if (!string.IsNullOrEmpty(currentFilePath))
                lblFilePath.Text = Path.GetFileName(currentFilePath);

            btnLive.Click        += (_, _) => SetMode(false);
            btnOffline.Click += (_, _) => SetMode(true);
            btnOpenFile.Click    += BtnOpenFile_Click;
            SetMode(currentPostProcess);
        }

        // ── Format label (Windows mix format) ────────────────────────────────
        private static void UpdateFormatLabel(Label lbl, MMDevice? device, WaveFormatExtensible? requested)
        {
            if (device == null) { lbl.Text = ""; return; }
            try
            {
                var fmt = device.AudioClient.MixFormat;
                string enc = fmt.Encoding == WaveFormatEncoding.IeeeFloat ? "Float" : "PCM";
                lbl.Text = $"{fmt.BitsPerSample}-bit {enc}  ·  {fmt.SampleRate / 1000.0:0.###} kHz  ·  {fmt.Channels}ch";
            }
            catch { lbl.Text = ""; }
        }

        // ── Sample rate combo ─────────────────────────────────────────────────
        private void RefreshSrCombo(ComboBox cb, MMDevice? device, WaveFormatExtensible? current)
        {
            int curSr = current?.SampleRate ?? 0;
            if (cb.SelectedItem is SrItem sel) curSr = sel.Rate;
            cb.Items.Clear();
            cb.Items.Add(new SrItem("Auto (Windows)", 0));
            foreach (int sr in _srOptions)
            {
                if (device != null && !IsSRSupported(device, sr)) continue;
                string lbl = sr >= 1000 ? $"{sr / 1000.0:0.###} kHz" : $"{sr} Hz";
                cb.Items.Add(new SrItem(lbl, sr));
            }
            for (int i = 0; i < cb.Items.Count; i++)
                if (cb.Items[i] is SrItem si && si.Rate == curSr)
                { cb.SelectedIndex = i; return; }
            cb.SelectedIndex = 0;
        }

        // ── Réflexion : reset du cache AudioClient entre les tests ────────
        private static readonly System.Reflection.FieldInfo? _acField =
            typeof(MMDevice).GetField(
                "audioClient",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

        private static void ResetAudioClient(MMDevice device)
        {
            try
            {
                var old = _acField?.GetValue(device) as NAudio.CoreAudioApi.AudioClient;
                if (old != null) try { old.Dispose(); } catch { }
                _acField?.SetValue(device, null);
            }
            catch { }
        }

        private static bool IsSRSupported(MMDevice device, int sampleRate)
        {
            var key = (device.ID, sampleRate);
            if (SrCache.TryGetValue(key, out bool cached)) return cached;

            WaveFormat[] cands =
            {
                WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2),
                new WaveFormatExtensible(sampleRate, 32, 2),
                new WaveFormatExtensible(sampleRate, 24, 2),
                new WaveFormatExtensible(sampleRate, 16, 2),
                // Formats PCM non-extensibles (tag 0x0001) — requis par certains
                // drivers embarqués (ex: STM32 USB Audio, ST libs)
                new WaveFormat(sampleRate, 32, 2),
                new WaveFormat(sampleRate, 24, 2),
                new WaveFormat(sampleRate, 16, 2),
            };

            bool allWereComErrors = true;
            foreach (var wf in cands)
            {
                try
                {
                    if (device.AudioClient.IsFormatSupported(
                            AudioClientShareMode.Exclusive, wf))
                    { SrCache[key] = true; return true; }
                    allWereComErrors = false;
                }
                catch (System.Runtime.InteropServices.COMException ex)
                {
                    if (unchecked((uint)ex.HResult) == 0x88890008u)
                        allWereComErrors = false;
                    else
                        ResetAudioClient(device);
                }
                catch { ResetAudioClient(device); }
            }

            if (allWereComErrors)
            {
                foreach (var wf in cands)
                {
                    if (ProbeExclusiveInit(device, wf))
                    { SrCache[key] = true; return true; }
                }
            }

            SrCache[key] = false;
            return false;
        }

        private static bool ProbeExclusiveInit(MMDevice device, WaveFormat fmt)
        {
            NAudio.CoreAudioApi.AudioClient? ac = null;
            try
            {
                ResetAudioClient(device);
                ac = device.AudioClient;
                long period = ac.DefaultDevicePeriod;
                ac.Initialize(
                    AudioClientShareMode.Exclusive,
                    NAudio.CoreAudioApi.AudioClientStreamFlags.EventCallback,
                    period, period, fmt, Guid.Empty);
                return true;
            }
            catch { return false; }
            finally
            {
                if (ac != null) try { ac.Dispose(); } catch { }
                ResetAudioClient(device);
            }
        }

        private static void SelectDevice(ComboBox cb, MMDevice? target)
        {
            if (target != null)
                for (int i = 0; i < cb.Items.Count; i++)
                    if (cb.Items[i] is DeviceItem di && di.Device.ID == target.ID)
                    { cb.SelectedIndex = i; return; }
            if (cb.Items.Count > 0) cb.SelectedIndex = 0;
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            PostProcessMode     = _isPostProcessMode;
            PostProcessFilePath = _postProcessFilePath;
            PostProcessLoop     = chkLoop.Checked;

            if (!PostProcessMode)
            {
                // Normal mode: collect device selection
                SelectedInputDevice  = (cmbInputDevice.SelectedItem  as DeviceItem)?.Device;
                SelectedOutputDevice = (cmbOutputDevice.SelectedItem as DeviceItem)?.Device;
                SelectedInputFormat  = SrItemToFormat(cmbInputRate.SelectedItem  as SrItem);
                SelectedOutputFormat = SrItemToFormat(cmbOutputRate.SelectedItem as SrItem);
            }
            DialogResult = DialogResult.OK;
        }

        private bool _isPostProcessMode;

        /// <summary>
        /// Bascule entre les modes LIVE et POST-PROCESS.
        /// Met à jour l'apparence des boutons et active/désactive les contrôles.
        /// </summary>
        private void SetMode(bool isPostProcess)
        {
            _isPostProcessMode = isPostProcess;

            // ── Apparence des boutons exclusifs ──────────────────────────
            // Bouton actif : fond bleu, texte blanc
            // Bouton inactif : fond système, texte normal
            static void StyleBtn(Button b, bool active)
            {
                b.BackColor = active
                    ? System.Drawing.Color.FromArgb(0, 122, 204)
                    : SystemColors.Control;
                b.ForeColor = active
                    ? System.Drawing.Color.White
                    : SystemColors.ControlText;
                b.FlatAppearance.BorderColor = active
                    ? System.Drawing.Color.FromArgb(0, 100, 180)
                    : SystemColors.ControlDark;
            }
            StyleBtn(btnLive,        !isPostProcess);
            StyleBtn(btnOffline,  isPostProcess);

            // ── Activation des contrôles selon le mode ───────────────────
            // LIVE : device/SR actifs, section fichier grisée
            // POST-PROCESS : device/SR grisés, section fichier active
            cmbInputDevice.Enabled = !isPostProcess;
            cmbInputRate.Enabled   = !isPostProcess;
            lblInputDevice.Enabled = !isPostProcess;
            lblInputSR.Enabled     = !isPostProcess;
            btnOpenFile.Enabled    =  isPostProcess;
            chkLoop.Enabled        =  isPostProcess;
            lblSelectFile.Enabled  =  isPostProcess;
            lblFilePath.Enabled    =  isPostProcess;
        }

        // Conservé pour compatibilité appelants éventuels
        private void ApplyPostProcessUi(bool isPostProcess) => SetMode(isPostProcess);

        private void BtnOpenFile_Click(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title  = "Select Audio File",
                Filter = "Audio Files|*.wav;*.mp3;*.aiff;*.aif;*.wma|All Files|*.*",
            };
            if (!string.IsNullOrEmpty(_postProcessFilePath))
                try { dlg.InitialDirectory = Path.GetDirectoryName(_postProcessFilePath) ?? ""; } catch { }

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _postProcessFilePath  = dlg.FileName;
                lblFilePath.Text      = Path.GetFileName(dlg.FileName);
                lblFilePath.ForeColor = System.Drawing.Color.SteelBlue;
            }
        }

        private static WaveFormatExtensible? SrItemToFormat(SrItem? item)
            => (item == null || item.Rate == 0) ? null
             : new WaveFormatExtensible(item.Rate, 32, 2);

        private sealed class BufferItem
        {
            public int    Value { get; }
            public BufferItem(int v) { Value = v; }
            public override string ToString() => (Value / 1024) + " k";
        }

        internal sealed class DeviceItem
        {
            public MMDevice Device      { get; }
            public string   DisplayName { get; }
            public DeviceItem(MMDevice d, string name) { Device = d; DisplayName = name; }
            public override string ToString() => DisplayName;
        }

        private sealed class SrItem
        {
            public string Label { get; }
            public int    Rate  { get; }
            public SrItem(string label, int rate) { Label = label; Rate = rate; }
            public override string ToString() => Label;
        }
    }
}
