// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

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
        /// <summary>Sets LightGreen/normal appearance on a Bode CheckBox-as-Button.</summary>
        private static void ApplyBodeToggleVisual(CheckBox b, bool active)
        {
            b.Checked = active;
            b.UseVisualStyleBackColor = !active;
            b.BackColor = active ? Color.LightGreen : SystemColors.Control;
            b.ForeColor = Color.Black;
        }

        private void BtnPhiEnable_Click(object? sender, EventArgs e)
        {
            _frfPhaseEnabled = !_frfPhaseEnabled;

            ApplyFrfPhiButtonsUi();
            SyncCursorUiFromModel();

            // Y2 axis visibility changed — force layout + redraw
            ApplyChartStatsMargin(chartSpectrum, invalidate: true, _measPanelVisible ? 1f : 0f);
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
            bool frf = IsFrfMode();
            // Always visible — grayed (Enabled=false) when not in Bode Plot mode
            btnPhiEnable.Visible  = true;
            btnPhiUnwrap.Visible  = true;
            lblPhaseGroup.Visible = true;
            btnPhiEnable.Enabled  = frf;
            btnPhiUnwrap.Enabled  = frf;

            if (!frf)
            {
                // Hors Bode Plot : remettre apparence neutre même si boutons étaient verts
                ApplyBodeToggleVisual(btnPhiEnable, false);
                ApplyBodeToggleVisual(btnPhiUnwrap, false);
                return;
            }

            // CheckBox.Appearance.Button: use explicit BackColor for green (not native blue)
            ApplyBodeToggleVisual(btnPhiEnable, _frfPhaseEnabled);
            ApplyBodeToggleVisual(btnPhiUnwrap, _frfPhaseUnwrap);
        }

        // ════════════════════════════════════════════════════════════════════
        // Normalisation FRF (point 1)
        //   "Cap Ref"  : capture H_ref(f) = gainDb/phaseDeg courants (RAW, pleine
        //                résolution). Ctrl+clic : efface la référence courante.
        //   "Norm"     : active/désactive la correction H_corr = H_mesuré / H_ref.
        // ════════════════════════════════════════════════════════════════════

        /// <summary>Ramène un écart de phase en degrés dans l'intervalle ]-180, 180].</summary>
        private static double WrapDeg180(double d)
        {
            d %= 360.0;
            if (d > 180.0) d -= 360.0;
            else if (d <= -180.0) d += 360.0;
            return d;
        }

        /// <summary>
        /// Ré-échantillonne une référence FRF (gain dB + phase °) capturée sur l'axe
        /// <paramref name="refFreq"/> vers l'axe <paramref name="targetFreq"/>.
        /// Utilisé quand N (FFT) ou Fs changent après la capture de la référence
        /// (espacement des bins différent, mais même plage [0, Fs/2]).
        ///
        /// - Gain : interpolation linéaire en dB vs Hz.
        /// - Phase : interpolation du vecteur unitaire (cos/sin) puis atan2, pour
        ///   éviter les artefacts au passage ±180°.
        /// - En dehors de la plage capturée : extrapolation constante (clamp aux bornes).
        /// </summary>
        private static void ResampleFrfReference(
            double[] refFreq, double[] refGainDb, double[] refPhaseDeg,
            double[] targetFreq,
            out double[] gainOut, out double[] phaseOut)
        {
            int m = refFreq.Length;
            int n = targetFreq.Length;
            gainOut = new double[n];
            phaseOut = new double[n];

            if (m == 0)
                return;

            if (m == 1)
            {
                for (int i = 0; i < n; i++)
                {
                    gainOut[i] = refGainDb[0];
                    phaseOut[i] = refPhaseDeg[0];
                }
                return;
            }

            for (int i = 0; i < n; i++)
            {
                double f = targetFreq[i];

                if (f <= refFreq[0])
                {
                    gainOut[i] = refGainDb[0];
                    phaseOut[i] = refPhaseDeg[0];
                    continue;
                }
                if (f >= refFreq[m - 1])
                {
                    gainOut[i] = refGainDb[m - 1];
                    phaseOut[i] = refPhaseDeg[m - 1];
                    continue;
                }

                int idx = Array.BinarySearch(refFreq, f);
                int j1 = (idx >= 0) ? idx : ~idx;     // premier bin >= f
                int j0 = Math.Max(0, j1 - 1);
                if (j1 == j0) j1 = Math.Min(m - 1, j0 + 1);

                double f0 = refFreq[j0], f1 = refFreq[j1];
                double t = (f1 > f0) ? (f - f0) / (f1 - f0) : 0.0;
                if (t < 0.0) t = 0.0;
                if (t > 1.0) t = 1.0;

                gainOut[i] = refGainDb[j0] + (refGainDb[j1] - refGainDb[j0]) * t;

                double rad0 = refPhaseDeg[j0] * Math.PI / 180.0;
                double rad1 = refPhaseDeg[j1] * Math.PI / 180.0;
                double cx = Math.Cos(rad0) * (1.0 - t) + Math.Cos(rad1) * t;
                double cy = Math.Sin(rad0) * (1.0 - t) + Math.Sin(rad1) * t;
                phaseOut[i] = Math.Atan2(cy, cx) * 180.0 / Math.PI;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // Référence FRF : Clear / Save / Load
        // ════════════════════════════════════════════════════════════════════

        internal void ClearFrfReference()
        {
            _frfRefGainDb         = null;
            _frfRefPhaseDeg       = null;
            _frfRefFreqHz         = null;
            _frfRefGainDbActive   = null;
            _frfRefPhaseDegActive = null;
            _frfRefActiveBins     = -1;
            _frfNormEnabled       = false;
            ApplyFrfNormButtonsUi();
            chartSpectrum.Invalidate();
        }

        // ── BodeRef folder ────────────────────────────────────────────────────
        private static string GetBodeRefDir()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "AudioHexViewer", "BodeRef");
            Directory.CreateDirectory(dir);
            return dir;
        }

        internal void SaveFrfReference()
        {
            if (_frfRefGainDb == null || _frfRefPhaseDeg == null || _frfRefFreqHz == null)
            {
                MessageBox.Show("No reference captured yet.\nCapture a reference first (\"Capture\").",
                    "Save Bode Reference", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string fileName = $"BodeRef_{DateTime.Now:yyyyMMdd_HHmmss}.ahvref";
            string filePath = Path.Combine(GetBodeRefDir(), fileName);

            try
            {
                using var sw = new System.IO.StreamWriter(
                    filePath, false, System.Text.Encoding.UTF8);

                double df = _frfRefFreqHz.Length > 1
                    ? _frfRefFreqHz[1] - _frfRefFreqHz[0] : 0.0;

                sw.WriteLine("# AudioHexViewer Bode Reference");
                sw.WriteLine($"# Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sw.WriteLine($"# SampleRate: {_sampleRate}");
                sw.WriteLine($"# FFTSize: {lastFftSize}");
                sw.WriteLine($"# DeltaF: {df:F6} Hz");
                sw.WriteLine($"# Bins: {_frfRefGainDb.Length}");
                sw.WriteLine("Frequency (Hz),Gain (dB),Phase (deg)");

                var ic = System.Globalization.CultureInfo.InvariantCulture;
                int n  = _frfRefGainDb.Length;
                for (int k = 0; k < n; k++)
                    sw.WriteLine(string.Format(ic, "{0:F6},{1:F6},{2:F6}",
                        _frfRefFreqHz[k], _frfRefGainDb[k], _frfRefPhaseDeg[k]));

                MessageBox.Show($"Reference saved:\n{filePath}",
                    "Save Bode Reference", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving reference:\n{ex.Message}",
                    "Save Bode Reference", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        internal void LoadFrfReference()
        {
            using var dlg = new OpenFileDialog
            {
                Title            = "Load Bode Reference",
                Filter           = "AudioHexViewer Reference (*.ahvref)|*.ahvref|CSV (*.csv)|*.csv|All files (*.*)|*.*",
                DefaultExt       = "ahvref",
                InitialDirectory = GetBodeRefDir()
            };
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

            try
            {
                var freqs  = new System.Collections.Generic.List<double>();
                var gains  = new System.Collections.Generic.List<double>();
                var phases = new System.Collections.Generic.List<double>();
                var ic     = System.Globalization.CultureInfo.InvariantCulture;

                foreach (var line in System.IO.File.ReadLines(dlg.FileName,
                    System.Text.Encoding.UTF8))
                {
                    if (string.IsNullOrWhiteSpace(line)
                        || line.StartsWith('#')
                        || line.StartsWith("Frequency")) continue;

                    var p = line.Split(',');
                    if (p.Length < 3) continue;

                    if (double.TryParse(p[0], System.Globalization.NumberStyles.Float, ic, out double f)
                     && double.TryParse(p[1], System.Globalization.NumberStyles.Float, ic, out double g)
                     && double.TryParse(p[2], System.Globalization.NumberStyles.Float, ic, out double ph))
                    {
                        freqs .Add(f);
                        gains .Add(g);
                        phases.Add(ph);
                    }
                }

                if (freqs.Count < 2)
                {
                    MessageBox.Show("File contains insufficient data (< 2 bins).",
                        "Load Bode Reference", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _frfRefGainDb         = gains .ToArray();
                _frfRefPhaseDeg       = phases.ToArray();
                _frfRefFreqHz         = freqs .ToArray();
                // Invalide le cache actif → ré-échantillonnage auto à la prochaine frame
                _frfRefGainDbActive   = null;
                _frfRefPhaseDegActive = null;
                _frfRefActiveBins     = -1;

                ApplyFrfNormButtonsUi();
                chartSpectrum.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading reference:\n{ex.Message}",
                    "Load Bode Reference", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnFrfRefCapture_Click(object? sender, EventArgs e)
        {
            // Ctrl+clic : efface la référence (conservé pour compatibilité clavier).
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                ClearFrfReference();
                return;
            }

            if (_lastFrfGainDbRaw == null || _lastFrfPhaseDegRaw == null || _lastFrfFreqHzRaw == null)
            {
                MessageBox.Show(
                    "No FRF measurement available yet.\n" +
                    "Start acquisition (RUN) before capturing a reference.",
                    "Capture FRF Reference",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int n = _lastFrfGainDbRaw.Length;
            _frfRefGainDb = new double[n];
            _frfRefPhaseDeg = new double[n];
            Array.Copy(_lastFrfGainDbRaw, _frfRefGainDb, n);
            Array.Copy(_lastFrfPhaseDegRaw, _frfRefPhaseDeg, n);

            // Mémorise l'axe fréquentiel de capture pour pouvoir ré-échantillonner
            // la référence si N (FFT) ou Fs changent par la suite.
            _frfRefFreqHz = (double[])_lastFrfFreqHzRaw.Clone();

            // Invalide le cache "actif" : sera recalculé (direct ou ré-échantillonné)
            // dès la prochaine frame en mode Norm.
            _frfRefGainDbActive = null;
            _frfRefPhaseDegActive = null;
            _frfRefActiveBins = -1;

            ApplyFrfNormButtonsUi();
            chartSpectrum.Invalidate();
        }

        private void BtnFrfNormToggle_Click(object? sender, EventArgs e)
        {
            if (_frfRefGainDb == null || _frfRefPhaseDeg == null)
                return; // bouton désactivé dans ce cas (cf. ApplyFrfNormButtonsUi)

            _frfNormEnabled = !_frfNormEnabled;
            ApplyFrfNormButtonsUi();
            chartSpectrum.Invalidate();
        }

        private void ApplyFrfNormButtonsUi()
        {
            bool frf = IsFrfMode();
            // Always visible — grayed (Enabled=false) when not in Bode Plot mode
            btnFrfRefCapture.Visible = true;
            btnFrfNormToggle.Visible = true;
            lblNormGroup.Visible     = true;
            btnFrfRefCapture.Enabled = frf;
            btnFrfNormToggle.Enabled = frf;

            bool hasRef = _frfRefGainDb != null && _frfRefPhaseDeg != null;

            if (!frf)
            {
                // Hors Bode Plot : remettre apparence neutre
                ApplyBodeToggleVisual(btnFrfRefCapture, false);
                ApplyBodeToggleVisual(btnFrfNormToggle, false);
                return;
            }

            // CheckBox.Appearance.Button: use explicit BackColor for green (not native blue)
            ApplyBodeToggleVisual(btnFrfRefCapture, hasRef);
            ApplyBodeToggleVisual(btnFrfNormToggle, _frfNormEnabled && hasRef);
            btnFrfNormToggle.Enabled = hasRef;

            _toolTipFrfNorm ??= new ToolTip();
            _toolTipFrfNorm.SetToolTip(btnFrfRefCapture,
                hasRef
                    ? "Reference captured.\nClick: re-capture current measurement as reference.\nCtrl+click: clear reference."
                    : "Capture current measurement (gain+phase) as reference H_ref(f).");
            _toolTipFrfNorm.SetToolTip(btnFrfNormToggle,
                hasRef
                    ? "Enable/disable normalisation: H_corr = H_measured / H_ref"
                    : "Capture a reference first (\"Capture\")");
        }

    }
}
