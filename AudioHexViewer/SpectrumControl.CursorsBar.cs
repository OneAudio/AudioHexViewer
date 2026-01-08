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

        private const int CursorBarMarginRight = 10;   // marge depuis le coin droit
        private const int CursorBarMarginBottom = 10;  // marge depuis le bas
        private const int CursorBarOffsetLeft = 15 ;     // marge pour déplacer à gauche



        /// <summary>
        /// Crée la barre curseurs (C1/C2) dans panelStats, en bas à droite sous grpRightStats.
        /// Appelée depuis le constructeur SpectrumControl() après InitializeComponent().
        /// </summary>
        private void InitCursorBarUi()
        {
            if (_cursorBar != null) return;
            if (panelStats == null) return;

            // On masque les anciennes cases "Cursor L/R" (elles restent la source de vérité)
            if (chkCursorL != null) chkCursorL.Visible = false;
            if (chkCursorR != null) chkCursorR.Visible = false;

            // Défauts demandés : C1 sur L, C2 sur R
            C1TrackMode = CursorTrackMode.Left;
            C2TrackMode = CursorTrackMode.Right;

            _cursorBar = new TableLayoutPanel
            {
                ColumnCount = 4,
                RowCount = 2,
                Size = new Size(150, 46),
                Anchor = AnchorStyles.None,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            _cursorBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 35)); // C1/C2
            _cursorBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 45)); // L/R/A (bouton)
            _cursorBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 35)); // Pk
            _cursorBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 35)); // Set

            _cursorBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 23));
            _cursorBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 23));

            // --- Ligne C1 ---
            _c1Toggle = MakeToggleButton("C1");
            _c1TrackBtn = MakeTrackCycleButton("Left");
            _c1Peak = MakeSmallButton("Pk");
            _c1Set = MakeSmallButton("Set");

            // --- Ligne C2 ---
            _c2Toggle = MakeToggleButton("C2");
            _c2TrackBtn = MakeTrackCycleButton("Right");
            _c2Peak = MakeSmallButton("Pk");
            _c2Set = MakeSmallButton("Set");

            _cursorBar.Controls.Add(_c1Toggle, 0, 0);
            _cursorBar.Controls.Add(_c1TrackBtn, 1, 0);
            _cursorBar.Controls.Add(_c1Peak, 2, 0);
            _cursorBar.Controls.Add(_c1Set, 3, 0);

            _cursorBar.Controls.Add(_c2Toggle, 0, 1);
            _cursorBar.Controls.Add(_c2TrackBtn, 1, 1);
            _cursorBar.Controls.Add(_c2Peak, 2, 1);
            _cursorBar.Controls.Add(_c2Set, 3, 1);

            chartSpectrum.Controls.Add(_cursorBar);
            chartSpectrum.Resize += (_, __) => PositionCursorBar();
            this.Resize += (_, __) => PositionCursorBar();
            PositionCursorBar();


            _cursorBar.BringToFront();  
            _cursorBar.BringToFront();

            // --- EVENTS ---
            _c1Toggle.Click += (_, __) =>
            {
                if (_cursorUiSync) return;
                CursorLeftEnabled = !CursorLeftEnabled;
                SyncCursorUiFromModel();
            };

            _c2Toggle.Click += (_, __) =>
            {
                if (_cursorUiSync) return;
                CursorRightEnabled = !CursorRightEnabled;
                SyncCursorUiFromModel();
            };

            _c1TrackBtn.Click += (_, __) =>
            {
                if (_cursorUiSync) return;
                CycleTrackButton(_c1TrackBtn, isC1: true);
            };

            _c2TrackBtn.Click += (_, __) =>
            {
                if (_cursorUiSync) return;
                CycleTrackButton(_c2TrackBtn, isC1: false);
            };

            _c1Peak.Click += (_, __) => MoveCursorToPeak(isC1: true);
            _c2Peak.Click += (_, __) => MoveCursorToPeak(isC1: false);

            _c1Set.Click += (_, __) => OpenSimpleSetDialog(isC1: true);
            _c2Set.Click += (_, __) => OpenSimpleSetDialog(isC1: false);

            // Repositionnement automatique sous le panneau mesures
            panelStats.Resize += (_, __) => PositionCursorBar();
            if (chartSpectrum != null)
                chartSpectrum.Resize += (_, __) => PositionCursorBar();

            // Si quelqu’un change les anciennes checkbox (même invisibles), on se resynchronise
            if (chkCursorL != null) chkCursorL.CheckedChanged += (_, __) => { if (!_cursorUiSync) SyncCursorUiFromModel(); };
            if (chkCursorR != null) chkCursorR.CheckedChanged += (_, __) => { if (!_cursorUiSync) SyncCursorUiFromModel(); };

            // Init visuel + position
            SyncCursorUiFromModel();
            PositionCursorBar();
        }

        /// <summary>
        /// Positionne la barre sous le panneau de mesures (grpRightStats), alignée à droite.
        /// </summary>
        private void PositionCursorBar()
        {
            if (_cursorBar == null || chartSpectrum == null) return;
            if (chartSpectrum.ChartAreas.Count == 0) return;

            var area = chartSpectrum.ChartAreas.FirstOrDefault(a => a.Name == "SpectrumArea")
                       ?? chartSpectrum.ChartAreas[0];

            var plot = GetPlotRectPixels(chartSpectrum, area);
            if (plot.IsEmpty) return;

            var cr = chartSpectrum.ClientRectangle;

            // Limite gauche : ne pas rentrer dans la zone du graphe (colonne blanche commence après plot.Right)
            int panelLeft = (int)Math.Round(plot.Right + 8f);

            // Ancrage coin bas-droite (fixe) : position basée sur le contrôle, pas sur plot.Bottom
            int x = cr.Right - _cursorBar.Width - CursorBarMarginRight - CursorBarOffsetLeft;
            int y = cr.Bottom - _cursorBar.Height - CursorBarMarginBottom;

            // Clamp : garder dans la "colonne blanche" à droite du plot
            if (x < panelLeft) x = panelLeft;

            // Clamp sécurité vertical (évite de monter trop haut si fenêtre très petite)
            int minY = (int)Math.Round(plot.Top + 2f);
            if (y < minY) y = minY;

            _cursorBar.Location = new Point(x, y);
            _cursorBar.Visible = true;
            _cursorBar.BringToFront();

            // Important : repositionner les labels C1/C2/Δ pour éviter chevauchement
            PositionCursorInfoLabels();
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

        // --------- UI helpers ---------

        private Button MakeToggleButton(string text)
        {
            var b = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0),
                Padding = new Padding(0),
                UseVisualStyleBackColor = true
            };

            // cache fonts (1 fois)
            _toggleFontRegular ??= b.Font;
            _toggleFontBold ??= new Font(b.Font, FontStyle.Bold);

            return b;
        }

        private static Button MakeTrackCycleButton(string initialText)
        {
            return new Button
            {
                Text = initialText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
        }

        private static Button MakeSmallButton(string text)
        {
            return new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
        }

        private void UpdateToggleVisual(Button b, bool enabled)
        {
            // rendu "comme les autres" : même bouton Windows, juste en gras quand actif
            if (_toggleFontRegular != null && _toggleFontBold != null)
                b.Font = enabled ? _toggleFontBold : _toggleFontRegular;
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
            "L" => CursorTrackMode.Left,
            "R" => CursorTrackMode.Right,
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
