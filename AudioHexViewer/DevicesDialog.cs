using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AudioHexViewer
{
    internal partial class DevicesDialog : Form
    {
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

            // ── Périphériques d'entrée ────────────────────────────────────────
            cmbInputDevice.DisplayMember = "FriendlyName";
            foreach (var d in inputDevices)
                cmbInputDevice.Items.Add(d);
            SelectDevice(cmbInputDevice, currentInput);

            // ── Périphériques de sortie ───────────────────────────────────────
            cmbOutputDevice.DisplayMember = "FriendlyName";
            foreach (var d in outputDevices)
                cmbOutputDevice.Items.Add(d);
            SelectDevice(cmbOutputDevice, currentOutput);

            // ── Sample rates (In) ─────────────────────────────────────────────
            PopulateSrCombo(cmbInputRate,  currentInputFormat);
            PopulateSrCombo(cmbOutputRate, currentOutputFormat);

            btnInfos.Click  += (_, _) => _onInfos?.Invoke();
            btnOk.Click     += BtnOk_Click;
            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void PopulateSrCombo(ComboBox cb, WaveFormatExtensible? current)
        {
            cb.Items.Add(new SrItem("Auto (Windows)", 0));
            foreach (int sr in _srOptions)
            {
                string lbl = sr < 1000 ? $"{sr} Hz" : $"{sr / 1000.0:0.###} kHz";
                cb.Items.Add(new SrItem(lbl, sr));
            }
            // Sélection courante
            int curSr = current?.SampleRate ?? 0;
            for (int i = 0; i < cb.Items.Count; i++)
            {
                if (cb.Items[i] is SrItem si && si.Rate == curSr)
                { cb.SelectedIndex = i; return; }
            }
            cb.SelectedIndex = 0; // Auto
        }

        private static void SelectDevice(ComboBox cb, MMDevice? target)
        {
            if (target != null)
            {
                for (int i = 0; i < cb.Items.Count; i++)
                {
                    if (cb.Items[i] is MMDevice d && d.ID == target.ID)
                    { cb.SelectedIndex = i; return; }
                }
            }
            if (cb.Items.Count > 0)
                cb.SelectedIndex = 0;
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            SelectedInputDevice  = cmbInputDevice.SelectedItem  as MMDevice;
            SelectedOutputDevice = cmbOutputDevice.SelectedItem as MMDevice;
            SelectedInputFormat  = SrItemToFormat(cmbInputRate.SelectedItem  as SrItem);
            SelectedOutputFormat = SrItemToFormat(cmbOutputRate.SelectedItem as SrItem);
            DialogResult = DialogResult.OK;
        }

        private static WaveFormatExtensible? SrItemToFormat(SrItem? item)
            => (item == null || item.Rate == 0)
               ? null
               : new WaveFormatExtensible(item.Rate, 32, 2);

        // ── SrItem ────────────────────────────────────────────────────────────
        private sealed class SrItem
        {
            public string Label { get; }
            public int    Rate  { get; }
            public SrItem(string label, int rate) { Label = label; Rate = rate; }
            public override string ToString() => Label;
        }
    }
}
