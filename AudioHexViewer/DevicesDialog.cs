using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AudioHexViewer
{
    internal partial class DevicesDialog : Form
    {
        // Cache statique : (deviceId, sampleRate) → supported
        // Rempli au démarrage par AudioInterfaceScanDialog, puis lu ici
        internal static readonly Dictionary<(string id, int sr), bool> SrCache
            = new Dictionary<(string id, int sr), bool>();

        // ── Résultats après OK ────────────────────────────────────────────────
        public MMDevice?              SelectedInputDevice  { get; private set; }
        public WaveFormatExtensible?  SelectedInputFormat  { get; private set; }   // null = Auto
        public MMDevice?              SelectedOutputDevice { get; private set; }
        public WaveFormatExtensible?  SelectedOutputFormat { get; private set; }   // null = Auto

        private readonly int[]  _srOptions;
        private readonly Action _onInfos;

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

            // ── Périphériques d'entrée ──────────────────────────────────────────
            // Pas de DisplayMember — on utilise DeviceItem.ToString() qui retourne
            // le nom pré-cacché → aucun appel COM lors de l'affichage
            foreach (var d in inputDevices)
            {
                // Utilise le nom pré-cacché pendant le scan (évite appel COM ~300ms/device)
                string name = SplashScreen.CachedNames.TryGetValue(d.ID, out var n) ? n : d.FriendlyName;
                cmbInputDevice.Items.Add(new DeviceItem(d, name));
            }
            SelectDevice(cmbInputDevice, currentInput);

            // ── Périphériques de sortie ──────────────────────────────────────────
            foreach (var d in outputDevices)
            {
                string name = SplashScreen.CachedNames.TryGetValue(d.ID, out var n) ? n : d.FriendlyName;
                cmbOutputDevice.Items.Add(new DeviceItem(d, name));
            }
            SelectDevice(cmbOutputDevice, currentOutput);

            // ── Sample rates (filtrés selon les capacités du périphérique) ────
            cmbInputDevice.SelectedIndexChanged  += (_, _) =>
                RefreshSrCombo(cmbInputRate,  (cmbInputDevice.SelectedItem  as DeviceItem)?.Device, currentInputFormat);
            cmbOutputDevice.SelectedIndexChanged += (_, _) =>
                RefreshSrCombo(cmbOutputRate, (cmbOutputDevice.SelectedItem as DeviceItem)?.Device, currentOutputFormat);

            RefreshSrCombo(cmbInputRate,  (cmbInputDevice.SelectedItem  as DeviceItem)?.Device, currentInputFormat);
            RefreshSrCombo(cmbOutputRate, (cmbOutputDevice.SelectedItem as DeviceItem)?.Device, currentOutputFormat);

            btnInfos.Click  += (_, _) => _onInfos?.Invoke();
            btnOk.Click     += BtnOk_Click;
            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void RefreshSrCombo(ComboBox cb, MMDevice? device, WaveFormatExtensible? current)
        {
            int curSr = current?.SampleRate ?? 0;

            // Conserver la sélection courante si possible
            if (cb.SelectedItem is SrItem sel) curSr = sel.Rate;

            cb.Items.Clear();
            cb.Items.Add(new SrItem("Auto (Windows)", 0));

            foreach (int sr in _srOptions)
            {
                // Si un périphérique est sélectionné, vérifier le support
                if (device != null && !IsSRSupported(device, sr))
                    continue;

                string lbl = sr >= 1000 ? $"{sr / 1000.0:0.###} kHz" : $"{sr} Hz";
                cb.Items.Add(new SrItem(lbl, sr));
            }

            // Restaurer la sélection
            for (int i = 0; i < cb.Items.Count; i++)
            {
                if (cb.Items[i] is SrItem si && si.Rate == curSr)
                { cb.SelectedIndex = i; return; }
            }
            cb.SelectedIndex = 0; // Auto par défaut
        }

        private static bool IsSRSupported(MMDevice device, int sampleRate)
        {
            var key = (device.ID, sampleRate);
            if (SrCache.TryGetValue(key, out bool cached)) return cached;

            // Premier appel pour ce (device, SR) : test COM réel
            WaveFormat[] cands =
            {
                WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2),
                new WaveFormatExtensible(sampleRate, 32, 2),
                new WaveFormatExtensible(sampleRate, 24, 2),
                new WaveFormatExtensible(sampleRate, 16, 2),
            };
            foreach (var wf in cands)
                try { if (device.AudioClient.IsFormatSupported(
                              NAudio.CoreAudioApi.AudioClientShareMode.Exclusive, wf))
                    { SrCache[key] = true; return true; } }
                catch { }

            bool result = device.DataFlow == NAudio.CoreAudioApi.DataFlow.Capture;
            SrCache[key] = result;
            return result;
        }

        private static void SelectDevice(ComboBox cb, MMDevice? target)
        {
            if (target != null)
            {
                for (int i = 0; i < cb.Items.Count; i++)
                {
                    if (cb.Items[i] is DeviceItem di && di.Device.ID == target.ID)
                    { cb.SelectedIndex = i; return; }
                }
            }
            if (cb.Items.Count > 0)
                cb.SelectedIndex = 0;
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
            => (item == null || item.Rate == 0)
               ? null
               : new WaveFormatExtensible(item.Rate, 32, 2);

        // ── SrItem ────────────────────────────────────────────────────────────
        // DeviceItem : nom pré-cacché → aucun appel COM lors de l'affichage dans le combo
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
