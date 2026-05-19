using System;
using System.Windows.Forms;

namespace AudioHexViewer
{
    internal partial class ThdSettingsDialog : Form
    {
        public SpectrumSettings.ThdSettings Result { get; private set; }

        private readonly double _nyq;   // Nyquist du SR courant, pour les clamps

        public ThdSettingsDialog(SpectrumSettings.ThdSettings initial, int sampleRate)
        {
            Result = initial ?? new SpectrumSettings.ThdSettings();

            InitializeComponent();

            _nyq = (sampleRate > 0) ? (sampleRate / 2.0) : 384000.0;

            nudFlow.Maximum      = (decimal)_nyq;
            nudFhigh.Maximum     = (decimal)_nyq;
            nudSfdrFlow.Maximum  = (decimal)_nyq;
            nudSfdrFhigh.Maximum = (decimal)_nyq;

            // ── THD frequency limits ─────────────────────────────────────────
            chkHPF.Checked = Result.UseHPF;
            nudFlow.Value  = (decimal)Math.Min(Math.Max(Result.FlowHz, 0.0), _nyq);

            chkLPF.Checked = Result.UseLPF;
            nudFhigh.Value = (decimal)Math.Min(Math.Max(Result.FhighHz, 0.0), _nyq);

            // ── SFDR frequency limits ────────────────────────────────────────
            // SfdrFlowHz  = 0 → ΔF (premier bin) : on affiche 0.0
            // SfdrFhighHz = 0 → Nyquist auto      : on affiche la vraie valeur Nyquist
            chkSfdrHPF.Checked = Result.SfdrUseHPF;
            nudSfdrFlow.Value  = (decimal)Math.Min(Math.Max(Result.SfdrFlowHz, 0.0), _nyq);

            chkSfdrLPF.Checked = Result.SfdrUseLPF;
            double sfdrFhighDisplay = (Result.SfdrFhighHz <= 0.0) ? _nyq : Result.SfdrFhighHz;
            nudSfdrFhigh.Value = (decimal)Math.Min(Math.Max(sfdrFhighDisplay, 0.0), _nyq);

            // ── Harmonics / unit / avg ───────────────────────────────────────
            nudHarms.Value       = Math.Min(Math.Max(Result.HarmonicsNumber, 2), 20);
            chkShowHarms.Checked = Result.ShowHarmonicsValues;
            chkShowPhase.Checked = Result.ShowHarmonicsPhase;

            rbDb.Checked  = (Result.Unit == SpectrumSettings.ThdSettings.ThdUnit.Db);
            rbPct.Checked = !rbDb.Checked;

            chkCohAvg.Checked = Result.UseCohAvg;

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private void btnOk_Click(object? sender, EventArgs e)
        {
            // Quand un filtre est désactivé, on sauvegarde 0.0 :
            //   SfdrFlowHz  = 0 → ΔF (Nyquist auto côté bas)
            //   SfdrFhighHz = 0 → Nyquist auto (mis à jour si SR change)
            // Ainsi la prochaine ouverture de la dialog affichera toujours
            // la vraie valeur Nyquist du SR courant.
            Result = new SpectrumSettings.ThdSettings
            {
                // THD band
                UseHPF  = chkHPF.Checked,
                FlowHz  = (double)nudFlow.Value,
                UseLPF  = chkLPF.Checked,
                FhighHz = (double)nudFhigh.Value,

                // SFDR band : 0.0 = auto (ΔF côté bas, Nyquist côté haut)
                SfdrUseHPF  = chkSfdrHPF.Checked,
                SfdrFlowHz  = chkSfdrHPF.Checked  ? (double)nudSfdrFlow.Value  : 0.0,
                SfdrUseLPF  = chkSfdrLPF.Checked,
                SfdrFhighHz = chkSfdrLPF.Checked  ? (double)nudSfdrFhigh.Value : 0.0,

                // Harmonics / unit / avg
                HarmonicsNumber     = (int)nudHarms.Value,
                ShowHarmonicsValues = chkShowHarms.Checked,
                ShowHarmonicsPhase  = chkShowPhase.Checked,
                Unit = rbPct.Checked
                    ? SpectrumSettings.ThdSettings.ThdUnit.Percent
                    : SpectrumSettings.ThdSettings.ThdUnit.Db,
                UseCohAvg = chkCohAvg.Checked,
            };
        }
    }
}
