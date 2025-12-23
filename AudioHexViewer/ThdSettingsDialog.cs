using System;
using System.Windows.Forms;

namespace AudioHexViewer
{
    internal partial class ThdSettingsDialog : Form
    {
        public SpectrumSettings.ThdSettings Result { get; private set; }

        public ThdSettingsDialog(SpectrumSettings.ThdSettings initial, int sampleRate)
        {
            Result = initial ?? new SpectrumSettings.ThdSettings();

            InitializeComponent();

            // Clamp maxs selon Nyquist
            double nyq = (sampleRate > 0) ? (sampleRate / 2.0) : 384000.0;
            nudFhigh.Maximum = (decimal)nyq;
            nudFlow.Maximum = (decimal)nyq;

            // Init UI depuis Result
            chkHPF.Checked = Result.UseHPF;
            nudFlow.Value = (decimal)Math.Min(Math.Max(Result.FlowHz, 0.0), nyq);

            chkLPF.Checked = Result.UseLPF;
            nudFhigh.Value = (decimal)Math.Min(Math.Max(Result.FhighHz, 0.0), nyq);

            nudHarms.Value = Math.Min(Math.Max(Result.HarmonicsNumber, 2), 20);
            chkShowHarms.Checked = Result.ShowHarmonicsValues;

            rbDb.Checked = (Result.Unit == SpectrumSettings.ThdSettings.ThdUnit.Db);
            rbPct.Checked = !rbDb.Checked;

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private void btnOk_Click(object? sender, EventArgs e)
        {
            Result = new SpectrumSettings.ThdSettings
            {
                UseHPF = chkHPF.Checked,
                FlowHz = (double)nudFlow.Value,
                UseLPF = chkLPF.Checked,
                FhighHz = (double)nudFhigh.Value,
                HarmonicsNumber = (int)nudHarms.Value,
                ShowHarmonicsValues = chkShowHarms.Checked,
                Unit = rbPct.Checked
                    ? SpectrumSettings.ThdSettings.ThdUnit.Percent
                    : SpectrumSettings.ThdSettings.ThdUnit.Db
            };
        }
    }
}
