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

            // Clamp maxima selon Nyquist
            double nyq = (sampleRate > 0) ? (sampleRate / 2.0) : 384000.0;
            nudFlow.Maximum      = (decimal)nyq;
            nudFhigh.Maximum     = (decimal)nyq;
            nudSfdrFlow.Maximum  = (decimal)nyq;
            nudSfdrFhigh.Maximum = (decimal)nyq;

            // ── THD frequency limits ─────────────────────────────────────────
            chkHPF.Checked  = Result.UseHPF;
            nudFlow.Value   = (decimal)Math.Min(Math.Max(Result.FlowHz,  0.0), nyq);
            chkLPF.Checked  = Result.UseLPF;
            nudFhigh.Value  = (decimal)Math.Min(Math.Max(Result.FhighHz, 0.0), nyq);

            // ── SFDR frequency limits ────────────────────────────────────────
            chkSfdrHPF.Checked  = Result.SfdrUseHPF;
            nudSfdrFlow.Value   = (decimal)Math.Min(Math.Max(Result.SfdrFlowHz,  0.0), nyq);
            chkSfdrLPF.Checked  = Result.SfdrUseLPF;
            nudSfdrFhigh.Value  = (decimal)Math.Min(Math.Max(Result.SfdrFhighHz, 0.0), nyq);

            // ── Harmonics / unit / avg ───────────────────────────────────────
            nudHarms.Value       = Math.Min(Math.Max(Result.HarmonicsNumber, 2), 20);
            chkShowHarms.Checked = Result.ShowHarmonicsValues;

            rbDb.Checked  = (Result.Unit == SpectrumSettings.ThdSettings.ThdUnit.Db);
            rbPct.Checked = !rbDb.Checked;

            chkCohAvg.Checked = Result.UseCohAvg;

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private void btnOk_Click(object? sender, EventArgs e)
        {
            Result = new SpectrumSettings.ThdSettings
            {
                // THD band
                UseHPF  = chkHPF.Checked,
                FlowHz  = (double)nudFlow.Value,
                UseLPF  = chkLPF.Checked,
                FhighHz = (double)nudFhigh.Value,

                // SFDR band
                SfdrUseHPF  = chkSfdrHPF.Checked,
                SfdrFlowHz  = (double)nudSfdrFlow.Value,
                SfdrUseLPF  = chkSfdrLPF.Checked,
                SfdrFhighHz = (double)nudSfdrFhigh.Value,

                // Harmonics / unit / avg
                HarmonicsNumber    = (int)nudHarms.Value,
                ShowHarmonicsValues = chkShowHarms.Checked,
                Unit = rbPct.Checked
                    ? SpectrumSettings.ThdSettings.ThdUnit.Percent
                    : SpectrumSettings.ThdSettings.ThdUnit.Db,
                UseCohAvg = chkCohAvg.Checked,
            };
        }
    }
}
