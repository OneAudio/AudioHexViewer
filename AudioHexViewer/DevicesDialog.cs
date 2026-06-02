using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using NAudio.Wave;

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
            Action                   onInfosClick)
        {
            _srOptions = sampleRateOptions;
            _onInfos   = onInfosClick;
            InitializeComponent();

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
            SelectedInputDevice  = (cmbInputDevice.SelectedItem  as DeviceItem)?.Device;
            SelectedOutputDevice = (cmbOutputDevice.SelectedItem as DeviceItem)?.Device;
            SelectedInputFormat  = SrItemToFormat(cmbInputRate.SelectedItem  as SrItem);
            SelectedOutputFormat = SrItemToFormat(cmbOutputRate.SelectedItem as SrItem);
            DialogResult = DialogResult.OK;
        }

        private static WaveFormatExtensible? SrItemToFormat(SrItem? item)
            => (item == null || item.Rate == 0) ? null
             : new WaveFormatExtensible(item.Rate, 32, 2);

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
