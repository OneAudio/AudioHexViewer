// ═══════════════════════════════════════════════════════════════════════════
// SpectrumControl.Bode.cs  —  Mode FRF / Bode Plot
//
// Responsabilités :
//   • Détection du mode Bode actif (IsFrfMode)
//   • Boutons φ : activation et dépliement de phase
//   • Verrous UI spécifiques au mode FRF (UpdateFrfUiLocks)
//   • Calcul de la plage Y2 (phase) pour le fit automatique
// ═══════════════════════════════════════════════════════════════════════════
using AudioHexViewer.Core;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace AudioHexViewer
{
    partial class SpectrumControl
    {
        //
        private (double minDeg, double maxDeg) GetVisiblePhaseRangeDeg_ForFitY2()
        {
            // Fit demandé sur données visibles (plot)
            var phase = _plotFrfPhaseDeg;
            var fx = _plotFreqAxis;

            // Si pas de phase dispo -> fallback safe
            if (phase == null || fx == null || phase.Length == 0 || fx.Length != phase.Length)
                return (-180.0, +180.0);

            var (fMin, fMax) = GetVisibleFreqRangeHz();

            double min = double.PositiveInfinity;
            double max = double.NegativeInfinity;

            for (int i = 0; i < phase.Length; i++)
            {
                double f = fx[i];
                if (f < fMin || f > fMax) continue;

                double p = phase[i];
                if (double.IsNaN(p) || double.IsInfinity(p)) continue;

                if (p < min) min = p;
                if (p > max) max = p;
            }

            if (!double.IsFinite(min) || !double.IsFinite(max))
                return (-180.0, +180.0);

            // Spec: Fit borné à +/-180°
            if (min < -180.0) min = -180.0;
            if (max > +180.0) max = +180.0;

            return (min, max);
        }

        private bool IsFrfMode()
        {
            var m = SelectedChannelMode ?? "";
            return m.Equals("L/R", StringComparison.OrdinalIgnoreCase)
                || m.Equals("Bode Plot", StringComparison.OrdinalIgnoreCase); // si tu l'utilises dans l'UI
        }

        private void UpdateFrfUiLocks()
        {
            bool frf = IsFrfMode();

            // 1) THD : griser en FRF
            if (chkEnableThd != null)
            {
                if (frf)
                    chkEnableThd.Checked = false; // sécurité : on coupe la mesure THD si on arrive en FRF
                chkEnableThd.Enabled = !frf;
            }
            if (btnThdSettings != null)
            {
                btnThdSettings.Enabled = !frf;
            }

            // 2) Windowing : forcer Hann + griser en FRF
            if (comboWindow == null)
                return;

            if (frf)
            {
                // mémorise la sélection précédente (une seule fois)
                if (_windowIndexBeforeFrf < 0)
                    _windowIndexBeforeFrf = comboWindow.SelectedIndex;

                int hannIdx = comboWindow.Items.IndexOf("Hann");
                if (hannIdx < 0) hannIdx = 1; // fallback “raisonnable” si la liste change

                // Forcer Hann même si on est en live / même si les events sont inhibés :
                if (comboWindow.SelectedIndex != hannIdx)
                {
                    bool prevRestore = _isRestoringSettings;
                    _isRestoringSettings = true;
                    try
                    {
                        comboWindow.SelectedIndex = hannIdx;
                    }
                    finally
                    {
                        _isRestoringSettings = prevRestore;
                    }

                    // IMPORTANT : garder l’état interne cohérent (comme dans ApplySpectrumSettings) :contentReference[oaicite:4]{index=4}
                    lastWindow = (WindowType)hannIdx;

                    // Mise à jour texte overlay “Win=…” si nécessaire
                    UpdateWindowInfo();
                }

                comboWindow.Enabled = false;
            }
            else
            {
                comboWindow.Enabled = true;

                // Restaurer la fenêtre d’avant FRF (si on l’avait mémorisée)
                if (_windowIndexBeforeFrf >= 0 && _windowIndexBeforeFrf < comboWindow.Items.Count)
                {
                    int restoreIdx = _windowIndexBeforeFrf;
                    _windowIndexBeforeFrf = -1;

                    if (comboWindow.SelectedIndex != restoreIdx)
                    {
                        bool prevRestore = _isRestoringSettings;
                        _isRestoringSettings = true;
                        try
                        {
                            comboWindow.SelectedIndex = restoreIdx;
                        }
                        finally
                        {
                            _isRestoringSettings = prevRestore;
                        }

                        lastWindow = (WindowType)comboWindow.SelectedIndex;
                        UpdateWindowInfo();
                    }
                }
            }
        }

        //
        private void BtnPhiEnable_Click(object? sender, EventArgs e)
        {
            _frfPhaseEnabled = !_frfPhaseEnabled;

            // On ne bloque pas les curseurs Phase même si la courbe est masquée (ton point 1)
            ApplyFrfPhiButtonsUi();
            SyncCursorUiFromModel();

            // Force un redraw rapide
            chartSpectrum.Invalidate();
        }

        private void BtnPhiUnwrap_Click(object? sender, EventArgs e)
        {
            _frfPhaseUnwrap = !_frfPhaseUnwrap; // vert quand unwrap φ (ton point 2)
            ApplyFrfPhiButtonsUi();

            // Force recalcul display (unwrap change la courbe)
            chartSpectrum.Invalidate();
        }

        private void ApplyFrfPhiButtonsUi()
        {
            // Visible uniquement en mode FRF (Bode)
            bool frf = IsFrfMode(); // tu l’as déjà dans le control (utilisé dans le rendu FRF)
            btnPhiEnable.Visible = frf;
            btnPhiUnwrap.Visible = frf;

            if (!frf)
                return;

            // Mini toggle look (même logique que tes carrés C1/C2)
            btnPhiEnable.BackColor = _frfPhaseEnabled ? Color.LightGreen : SystemColors.Control;
            btnPhiUnwrap.BackColor = _frfPhaseUnwrap ? Color.LightGreen : SystemColors.Control;
        }

    }
}
