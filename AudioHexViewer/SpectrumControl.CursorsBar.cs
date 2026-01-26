using System;
using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    public partial class SpectrumControl
    {
        private TableLayoutPanel? _cursorBar;

        private Button? _c1Toggle;
        private Button? _c1TrackBtn;
        private Button? _c1Peak;
        private Button? _c1Set;

        private Button? _c2Toggle;
        private Button? _c2TrackBtn;
        private Button? _c2Peak;
        private Button? _c2Set;

        private bool _cursorUiSync;
        private Font? _toggleFontRegular;
        private Font? _toggleFontBold;

        private Color? _toggleBackColor;
        private Color? _toggleForeColor;

        private const int CursorBarMarginRight = 10;   // marge depuis le coin droit
        private const int CursorBarMarginBottom = 10;  // marge depuis le bas
        private const int CursorBarOffsetLeft = 15 ;     // marge pour déplacer à gauche



        /// <summary>
        /// Crée la barre curseurs (C1/C2) dans panelStats, en bas à droite sous grpRightStats.
        /// Appelée depuis le constructeur SpectrumControl() après InitializeComponent().
        /// </summary>
        private void InitCursorBarUi()
        {
            // Déjà bindé ? (important : tlpCursors existe déjà car Designer)
            if (_c1Toggle != null) return;

            // On masque les anciennes cases "Cursor Bode Plot" (elles restent la source de vérité)
            if (chkCursorL != null) chkCursorL.Visible = false;
            if (chkCursorR != null) chkCursorR.Visible = false;

            // --- Bind vers les contrôles créés dans SpectrumControl.Designer.cs ---
            _cursorBar = tlpCursors;

            _c1Toggle = btnC1;
            _c1TrackBtn = btnC1Track;
            _c1Peak = btnC1Pk;
            _c1Set = btnC1Set;

            _c2Toggle = btnC2;
            _c2TrackBtn = btnC2Track;
            _c2Peak = btnC2Pk;
            _c2Set = btnC2Set;

            if (_cursorBar == null || _c1Toggle == null || _c2Toggle == null
                || _c1TrackBtn == null || _c2TrackBtn == null
                || _c1Peak == null || _c2Peak == null
                || _c1Set == null || _c2Set == null)
                return;

            // Sauvegarde couleurs "OFF" (une seule fois)
            _toggleBackColor ??= _c1Toggle.BackColor;
            _toggleForeColor ??= _c1Toggle.ForeColor;


            // Fix “centrage” + look uniforme (sinon dépend du Designer)
            var all = new[] { _c1Toggle, _c1TrackBtn, _c1Peak, _c1Set, _c2Toggle, _c2TrackBtn, _c2Peak, _c2Set };
            foreach (var b in all)
            {
                b.TextAlign = ContentAlignment.MiddleCenter;
                b.Margin = new Padding(0);
                b.Padding = new Padding(0);
                b.TabStop = false;
            }

            // Police pour C1/C2 (actif = bold)
            _toggleFontRegular ??= _c1Toggle.Font;
            _toggleFontBold ??= new Font(_c1Toggle.Font, FontStyle.Bold);

            // Défaut : Auto pour les deux
            C1TrackMode = CursorTrackMode.Auto;
            C2TrackMode = CursorTrackMode.Auto;


            WireCursorBarEvents();
            SyncCursorUiFromModel();
        }
        //
        private void WireCursorBarEvents()
        {
            if (_c1Toggle == null || _c2Toggle == null || _c1TrackBtn == null || _c2TrackBtn == null
                || _c1Peak == null || _c2Peak == null || _c1Set == null || _c2Set == null)
                return;

            // IMPORTANT : éviter de brancher deux fois si InitCursorBarUi est rappelé
            _c1Toggle.Click -= C1Toggle_Click;
            _c2Toggle.Click -= C2Toggle_Click;
            _c1TrackBtn.Click -= C1Track_Click;
            _c2TrackBtn.Click -= C2Track_Click;
            _c1Peak.Click -= C1Peak_Click;
            _c2Peak.Click -= C2Peak_Click;
            _c1Set.Click -= C1Set_Click;
            _c2Set.Click -= C2Set_Click;

            _c1Toggle.Click += C1Toggle_Click;
            _c2Toggle.Click += C2Toggle_Click;
            _c1TrackBtn.Click += C1Track_Click;
            _c2TrackBtn.Click += C2Track_Click;
            _c1Peak.Click += C1Peak_Click;
            _c2Peak.Click += C2Peak_Click;
            _c1Set.Click += C1Set_Click;
            _c2Set.Click += C2Set_Click;

            // Si quelque chose ailleurs modifie les checkbox (même invisibles), on resynchronise l’UI
            if (chkCursorL != null)
            {
                chkCursorL.CheckedChanged -= ChkCursorL_CheckedChanged;
                chkCursorL.CheckedChanged += ChkCursorL_CheckedChanged;
            }
            if (chkCursorR != null)
            {
                chkCursorR.CheckedChanged -= ChkCursorR_CheckedChanged;
                chkCursorR.CheckedChanged += ChkCursorR_CheckedChanged;
            }
        }

        private void C1Toggle_Click(object? sender, EventArgs e)
        {
            if (_cursorUiSync) return;
            CursorLeftEnabled = !CursorLeftEnabled;   // <-- C’EST ÇA “piloter chkCursorL”
            SyncCursorUiFromModel();
        }

        private void C2Toggle_Click(object? sender, EventArgs e)
        {
            if (_cursorUiSync) return;
            CursorRightEnabled = !CursorRightEnabled; // <-- C’EST ÇA “piloter chkCursorR”
            SyncCursorUiFromModel();
        }

        private void C1Track_Click(object? sender, EventArgs e)
        {
            if (_cursorUiSync) return;
            CycleTrackButton(_c1TrackBtn!, isC1: true);
            SyncCursorUiFromModel();
        }

        private void C2Track_Click(object? sender, EventArgs e)
        {
            if (_cursorUiSync) return;
            CycleTrackButton(_c2TrackBtn!, isC1: false);
            SyncCursorUiFromModel();
        }

        private void C1Peak_Click(object? sender, EventArgs e) => MoveCursorToPeak(isC1: true);
        private void C2Peak_Click(object? sender, EventArgs e) => MoveCursorToPeak(isC1: false);

        private void C1Set_Click(object? sender, EventArgs e) => OpenSimpleSetDialog(isC1: true);
        private void C2Set_Click(object? sender, EventArgs e) => OpenSimpleSetDialog(isC1: false);

        private void ChkCursorL_CheckedChanged(object? sender, EventArgs e)
        {
            if (_cursorUiSync) return;
            SyncCursorUiFromModel();
        }

        private void ChkCursorR_CheckedChanged(object? sender, EventArgs e)
        {
            if (_cursorUiSync) return;
            SyncCursorUiFromModel();
        }



        private void SyncCursorUiFromModel()
        {
            if (_c1Toggle == null || _c2Toggle == null || _c1TrackBtn == null || _c2TrackBtn == null) return;

            _cursorUiSync = true;
            try
            {
                _c1TrackBtn.Text = ModeToText(C1TrackMode);
                _c2TrackBtn.Text = ModeToText(C2TrackMode);

                UpdateToggleVisual(_c1Toggle, CursorLeftEnabled);
                UpdateToggleVisual(_c2Toggle, CursorRightEnabled);
            }
            finally
            {
                _cursorUiSync = false;
            }
        }

        private void UpdateToggleVisual(Button b, bool enabled)
        {
            // Désactive le gras (optionnel) : on garde une police constante
            if (_toggleFontRegular != null)
                b.Font = _toggleFontRegular;

            // Vert quand actif, couleurs normales quand inactif
            if (enabled)
            {
                b.UseVisualStyleBackColor = false; // important sinon Windows peut ignorer BackColor
                b.BackColor = Color.LightGreen;
                b.ForeColor = Color.Black;
            }
            else
            {
                b.UseVisualStyleBackColor = true; // restaure le rendu Windows
                if (_toggleBackColor.HasValue) b.BackColor = _toggleBackColor.Value;
                if (_toggleForeColor.HasValue) b.ForeColor = _toggleForeColor.Value;
            }
        }


        // --------- Track cycle L->R->A->L ---------

        private void CycleTrackButton(Button btn, bool isC1)
        {
            string next = NextTrackText(btn.Text);
            btn.Text = next;

            var mode = TextToMode(next);
            if (isC1) C1TrackMode = mode;
            else C2TrackMode = mode;

            chartSpectrum?.Invalidate();
        }

        private static string NextTrackText(string cur) => cur switch { "Left" => "Right", "Right" => "Auto", _ => "Left" };

        private static CursorTrackMode TextToMode(string t) => t switch
        {
            "Left" => CursorTrackMode.Left,
            "Right" => CursorTrackMode.Right,
            _ => CursorTrackMode.Auto
        };

        private static string ModeToText(CursorTrackMode m) => m switch
        {
            CursorTrackMode.Left => "Left",
            CursorTrackMode.Right => "Right",
            _ => "Auto"
        };
        //
        // --------- Actions Pk / Set ---------
        private void MoveCursorToPeak(bool isC1)
        {
            if (isC1)
            {
                if (!CursorLeftEnabled) CursorLeftEnabled = true;

                if (TryFindPeakFrequencyForCursor1(out var hz))
                    SetCursor1FrequencyHz(hz);
                else
                    MessageBox.Show(this.FindForm(), "Peak non disponible pour C1 (pas de data ou voie inactive).");
            }
            else
            {
                if (!CursorRightEnabled) CursorRightEnabled = true;

                if (TryFindPeakFrequencyForCursor2(out var hz))
                    SetCursor2FrequencyHz(hz);
                else
                    MessageBox.Show(this.FindForm(), "Peak non disponible pour C2 (pas de data ou voie inactive).");
            }

            SyncCursorUiFromModel();
        }

        private void OpenSimpleSetDialog(bool isC1)
        {
            double? current = null;

            if (isC1)
            {
                if (TryGetCursor1FrequencyHz(out var hz)) current = hz;
            }
            else
            {
                if (TryGetCursor2FrequencyHz(out var hz)) current = hz;
            }

            // Bornes : 0..Nyquist (ou 1..Nyquist si LOG actif)
            double nyq = (_sampleRate > 0) ? (_sampleRate / 2.0) : 384000.0;
            bool log = (chkLogX != null && chkLogX.Checked);
            double minHz = log ? 1.0 : 0.0;
            double maxHz = Math.Max(minHz + 1.0, nyq);

            using var dlg = new CursorFrequencyDialog(
                title: isC1 ? "Set C1 frequency (Hz)" : "Set C2 frequency (Hz)",
                minHz: minHz,
                maxHz: maxHz,
                initialHz: current);

            var owner = this.FindForm();
            var r = owner != null ? dlg.ShowDialog(owner) : dlg.ShowDialog();
            if (r != DialogResult.OK) return;

            if (isC1)
            {
                CursorLeftEnabled = true;
                SetCursor1FrequencyHz(dlg.SelectedHz);
            }
            else
            {
                CursorRightEnabled = true;
                SetCursor2FrequencyHz(dlg.SelectedHz);
            }

            SyncCursorUiFromModel();
        }
    }
}
