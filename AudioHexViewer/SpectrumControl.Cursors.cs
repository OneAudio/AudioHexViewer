// ═══════════════════════════════════════════════════════════════════════════
// SpectrumControl.Cursors.cs  —  Curseurs fréquence et détection de pics
//
// Responsabilités :
//   • Curseurs C1/C2 : init, positionnement, delta (TryGet/SetCursor*)
//   • Détection de pics spectraux (TryFindPeakFrequency*, TryFindAdjacent*)
//   • Gestion souris : MouseMove, MouseUp, MouseDoubleClick
//   • Helpers : FindNearestFreqIndex, MedianOfRange, IsProminentPeak
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
        private void UpdateCursorVisibility()
        {
            bool onL = chkCursorL != null && chkCursorL.Checked;
            bool onR = chkCursorR != null && chkCursorR.Checked;

            if (_cursorLineL != null) _cursorLineL.Visible = onL;
            if (_cursorLineR != null) _cursorLineR.Visible = onR;

            // Inline text labels (bottom of chart) are hidden when the floating box
            // is shown instead (Graph=1 or Both=2) or Off (3).
            bool showInlineLabels = (_cursorDispMode == 0 || _cursorDispMode == 2);
            if (_lblCursorLInfo != null) _lblCursorLInfo.Visible = onL && showInlineLabels;
            if (_lblCursorRInfo != null) _lblCursorRInfo.Visible = onR && showInlineLabels;

            if (!onL) _cursorMarkLVisible = false;
            if (!onR) _cursorMarkRVisible = false;

            chartSpectrum.Invalidate();
        }

        //
        private void InitCursorInfoLabels()
        {
            if (_lblCursorLInfo != null || _lblCursorRInfo != null)
                return;

            // On les place directement SUR le control Chart (plus simple, pas de Designer)
            _lblCursorLInfo = new Label
            {
                AutoSize = true,
                Text = "",
                BackColor = Color.Transparent,          // plus de fond gris
                ForeColor = FftLeftColor,           // couleur courbe L
                Font = new Font("Segoe UI", 8.5f),
                Visible = false
            };

            _lblCursorRInfo = new Label
            {
                AutoSize = true,
                Text = "",
                BackColor = Color.Transparent,          // plus de fond gris
                ForeColor = FftRightColor,            // couleur courbe R
                Font = new Font("Segoe UI", 8.5f),
                Visible = false
            };

            _lblCursorDeltaInfo = new Label
            {
                AutoSize = true,
                Text = "",
                BackColor = Color.Transparent,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8.5f),
                Visible = false
            };


            chartSpectrum.Controls.Add(_lblCursorLInfo);
            chartSpectrum.Controls.Add(_lblCursorRInfo);
            chartSpectrum.Controls.Add(_lblCursorDeltaInfo);
            _lblCursorLInfo.BringToFront();
            _lblCursorRInfo.BringToFront();
            _lblCursorDeltaInfo.BringToFront();

            chartSpectrum.Resize += (_, __) => PositionCursorInfoLabels();
        }

        private void PositionCursorInfoLabels()
        {
            if (_lblCursorLInfo == null || _lblCursorRInfo == null || _lblCursorDeltaInfo == null)
                return;

            const int yOffsetFromBottom = 40;   // on revient à 2 lignes
            const int xOffsetFromRight = 450;

            int y = Math.Max(0, chartSpectrum.ClientSize.Height - yOffsetFromBottom);
            int xStart = Math.Max(6, chartSpectrum.ClientSize.Width - xOffsetFromRight);

            _lblCursorLInfo.Location = new Point(xStart, y);
            _lblCursorRInfo.Location = new Point(xStart, y + 16);

            // Delta à droite des 2 lignes
            int w1 = _lblCursorLInfo.PreferredWidth;
            int w2 = _lblCursorRInfo.PreferredWidth;
            int xDelta = xStart + Math.Max(w1, w2) + 14;

            // clamp pour ne pas sortir de la zone chart
            int maxX = Math.Max(6, chartSpectrum.ClientSize.Width - _lblCursorDeltaInfo.PreferredWidth - 6);
            if (xDelta > maxX) xDelta = maxX;

            _lblCursorDeltaInfo.Location = new Point(xDelta, y + 8); // centré verticalement
        }

        private void InitCursors()
        {
            if (chartSpectrum.ChartAreas.Count == 0)
                return;

            // Eviter double init
            if (_cursorLineL != null || _cursorLineR != null)
                return;

            var area = chartSpectrum.ChartAreas["SpectrumArea"];

            // --- Ligne curseur L ---
            _cursorLineL = new VerticalLineAnnotation
            {
                ClipToChartArea = "SpectrumArea",
                AxisX = area.AxisX,
                AxisY = area.AxisY,
                IsInfinitive = true,
                LineDashStyle = ChartDashStyle.Dash,
                LineWidth = 1,
                LineColor = Color.FromArgb(180, FftLeftColor),
                Visible = false
            };

            // --- Ligne curseur R ---
            _cursorLineR = new VerticalLineAnnotation
            {
                ClipToChartArea = "SpectrumArea",
                AxisX = area.AxisX,
                AxisY = area.AxisY,
                IsInfinitive = true,
                LineDashStyle = ChartDashStyle.Dash,
                LineWidth = 1,
                LineColor = Color.FromArgb(180, FftRightColor),
                Visible = false
            };

            // --- Triangle curseur L ---
            // --- Triangle curseur L ---



            chartSpectrum.Annotations.Add(_cursorLineL);
            chartSpectrum.Annotations.Add(_cursorLineR);

            // Labels en bas
            InitCursorInfoLabels();

            UpdateCursorVisibility();
        }

        //
        private void ChartSpectrum_MouseMove(object? sender, MouseEventArgs e)
        {
            // Delta label drag takes priority
            if (_deltaLabelDragging)
            {
                var plot = GetPlotRectPixels(chartSpectrum, chartSpectrum.ChartAreas[0]);
                UpdateDeltaLabelDrag(e, plot);
                return;
            }

            if (freqAxis == null || freqAxis.Length == 0)
                return;

            bool onL = (chkCursorL != null && chkCursorL.Checked);
            bool onR = (chkCursorR != null && chkCursorR.Checked);
            if (!onL && !onR)
                return;

            bool useLog = (chkLogX != null && chkLogX.Checked);

            // fréquence sous la souris (conversion log-friendly)
            double f = GetFrequencyFromMouseX(e.X);

            if (useLog && f <= 0.0) f = LogXAbsMinHz;
            if (!useLog && f < 0.0) f = 0.0;

            int minIdx = useLog ? GetMinIndexForLog(freqAxis) : 0;

            int idx = FindNearestFreqIndex(freqAxis, f, minIdx);
            if (idx < 0 || idx >= freqAxis.Length)
                return;

            bool canMoveL = onL && !_cursorLockedL;
            bool canMoveR = onR && !_cursorLockedR;

            // Si les deux sont lockés : on rafraîchit juste l'affichage (valeurs suivent le spectre)
            if (!canMoveL && !canMoveR)
            {
                RefreshCursorsFromCurrentSpectrum();
                return;
            }

            if (canMoveL && !canMoveR)
            {
                //_activeCursor = ActiveCursor.Left;
                _cursorIdxL = idx;
                _cursorFreqL = freqAxis[idx];
                _cursorTargetHzL = _cursorFreqL;

            }
            else if (!canMoveL && canMoveR)
            {
                //_activeCursor = ActiveCursor.Right;
                _cursorIdxR = idx;
                _cursorFreqR = freqAxis[idx];
                _cursorTargetHzR = _cursorFreqR;

            }
            else
            {
                // Les deux peuvent bouger : déplacer celui le plus proche du pointeur
                var area = chartSpectrum.ChartAreas["SpectrumArea"];

                // Si indices non init, init
                if (_cursorIdxL < minIdx) { _cursorIdxL = idx; _cursorFreqL = freqAxis[idx]; }
                if (_cursorIdxR < minIdx) { _cursorIdxR = idx; _cursorFreqR = freqAxis[idx]; }

                double fL = freqAxis[_cursorIdxL];
                double fR = freqAxis[_cursorIdxR];

                double xL = area.AxisX.ValueToPixelPosition(fL);
                double xR = area.AxisX.ValueToPixelPosition(fR);

                double dL = Math.Abs(e.X - xL);
                double dR = Math.Abs(e.X - xR);

                if (dL <= dR)
                {
                    //_activeCursor = ActiveCursor.Left;
                    _cursorIdxL = idx;
                    _cursorFreqL = freqAxis[idx];
                }
                else
                {
                    //_activeCursor = ActiveCursor.Right;
                    _cursorIdxR = idx;
                    _cursorFreqR = freqAxis[idx];
                }
            }

            RefreshCursorsFromCurrentSpectrum();
        }

        //
        private void ChartSpectrum_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            if (freqAxis == null || freqAxis.Length == 0) return;

            var area = chartSpectrum.ChartAreas["SpectrumArea"];

            bool c1Enabled = (chkCursorL != null && chkCursorL.Checked);
            bool c2Enabled = (chkCursorR != null && chkCursorR.Checked);

            // rien à faire si aucun curseur
            if (!c1Enabled && !c2Enabled) return;

            // distances en pixels
            double d1 = double.PositiveInfinity;
            double d2 = double.PositiveInfinity;

            if (c1Enabled && _cursorIdxL >= 0 && _cursorIdxL < freqAxis.Length)
            {
                double x1 = area.AxisX.ValueToPixelPosition(freqAxis[_cursorIdxL]);
                d1 = Math.Abs(e.X - x1);
            }

            if (c2Enabled && _cursorIdxR >= 0 && _cursorIdxR < freqAxis.Length)
            {
                double x2 = area.AxisX.ValueToPixelPosition(freqAxis[_cursorIdxR]);
                d2 = Math.Abs(e.X - x2);
            }

            // choisir le plus proche si dans la tolérance
            bool hitC1 = d1 <= CursorHitTolPx;
            bool hitC2 = d2 <= CursorHitTolPx;

            if (!hitC1 && !hitC2)
                return; // <-- IMPORTANT : plus de toggle "loin"

            bool toggleC1 = hitC1 && (!hitC2 || d1 <= d2);

            if (toggleC1)
            {
                _cursorLockedL = !_cursorLockedL;
                _cursorTargetHzL = _cursorFreqL; // sync intention
            }
            else
            {
                _cursorLockedR = !_cursorLockedR;
                _cursorTargetHzR = _cursorFreqR; // sync intention
            }

            RefreshCursorsFromCurrentSpectrum();
        }

        private static int FindNearestFreqIndex(double[] axis, double f)
        {
            return FindNearestFreqIndex(axis, f, 0);
        }

        //
        private static int GetMinIndexForLog(double[] axis)
        {
            // En mode LogX, l’axe n’accepte pas 0 Hz => on prend le premier bin >= 1 Hz
            if (axis == null || axis.Length == 0)
                return 0;

            for (int i = 0; i < axis.Length; i++)
            {
                if (axis[i] > 0.0)
                    return i;
            }
            return 0;
        }

        private void RefreshCursorAfterRedraw()
        {
            if (chartSpectrum.ChartAreas.Count == 0)
                return;

            var area = chartSpectrum.ChartAreas["SpectrumArea"];

            if (_cursorLineL != null) { _cursorLineL.AxisX = area.AxisX; _cursorLineL.AxisY = area.AxisY; }
            //if (_cursorTextL != null) { _cursorTextL.AxisX = area.AxisX; _cursorTextL.AxisY = area.AxisY; }

            if (_cursorLineR != null) { _cursorLineR.AxisX = area.AxisX; _cursorLineR.AxisY = area.AxisY; }
            //if (_cursorTextR != null) { _cursorTextR.AxisX = area.AxisX; _cursorTextR.AxisY = area.AxisY; }

            // IMPORTANT : rafraîchit les valeurs au bin fixé (LOCK) même si la souris ne bouge pas
            RefreshCursorsFromCurrentSpectrum();
        }

        private ResolvedCurve ResolveCurveForCursor(bool isCursor1)
        {
            CursorTrackMode mode = isCursor1 ? _c1TrackMode : _c2TrackMode;

            // -----------------------------
            // Mode FRF / Bode Plot
            // -----------------------------
            if (IsFrfMode())
            {
                // Disponibilités spécifiques FRF :
                // - Gain : stocké dans currentLeftSpectrum (dB)
                // - Phase : stockée dans currentFrfPhaseDeg (deg)
                bool gainAvail = (currentLeftSpectrum != null);

                // IMPORTANT : “phase dispo pour les curseurs” == la série phase est réellement affichée
                // (quand tu désactives φ, le chart ne trace la phase que si _plotFrfPhaseDeg != null)
                bool phaseShown = (_plotFrfPhaseDeg != null);                 // phase visible sur le chart :contentReference[oaicite:1]{index=1}
                bool phaseAvail = (currentFrfPhaseDeg != null) && phaseShown; // curseurs autorisés sur Phase uniquement si visible

                switch (mode)
                {
                    case CursorTrackMode.Left:   // Left == Gain (mapping UI)
                        return gainAvail ? ResolvedCurve.Gain : ResolvedCurve.None;

                    case CursorTrackMode.Right:  // Right == Phase (mapping UI)
                                                 // Si la phase est désactivée => on force Gain (si dispo)
                        return phaseAvail ? ResolvedCurve.Phase : (gainAvail ? ResolvedCurve.Gain : ResolvedCurve.None);

                    default: // Auto
                             // Si la phase est désactivée => Auto retombe forcément sur Gain
                        if (gainAvail && phaseAvail)
                            return isCursor1 ? ResolvedCurve.Gain : ResolvedCurve.Phase; // spec: C1=Gain, C2=Phase

                        if (gainAvail) return ResolvedCurve.Gain;
                        if (phaseAvail) return ResolvedCurve.Phase;
                        return ResolvedCurve.None;
                }

            }

            // -----------------------------
            // Mode FFT normal (Left/Right)
            // -----------------------------
            // "Voie dispo" = affichée ET spectre présent
            bool leftAvail = _statsShowLeft && currentLeftSpectrum != null;
            bool rightAvail = _statsShowRight && currentRightSpectrum != null;

            switch (mode)
            {
                case CursorTrackMode.Left:
                    return leftAvail ? ResolvedCurve.Left : ResolvedCurve.None;

                case CursorTrackMode.Right:
                    return rightAvail ? ResolvedCurve.Right : ResolvedCurve.None;

                default: // Auto
                    if (leftAvail && rightAvail)
                        return isCursor1 ? ResolvedCurve.Left : ResolvedCurve.Right;
                    if (leftAvail) return ResolvedCurve.Left;
                    if (rightAvail) return ResolvedCurve.Right;
                    return ResolvedCurve.None;
            }
        }

        private static string CurveLetter(ResolvedCurve c)
        {
            return c switch
            {
                ResolvedCurve.Left => "L",
                ResolvedCurve.Right => "R",
                _ => "—"
            };
        }

        private string CursorCurveLabel(ResolvedCurve c)
        {
            if (IsFrfMode())
            {
                return c switch
                {
                    ResolvedCurve.Gain => "Gain",
                    ResolvedCurve.Phase => phi,
                    // Tolérance si jamais un ancien chemin envoie Left/Right en FRF
                    ResolvedCurve.Left => "Gain",
                    ResolvedCurve.Right => phi,
                    _ => "—"
                };
            }

            return c switch
            {
                ResolvedCurve.Left => "L",
                ResolvedCurve.Right => "R",
                _ => "—"
            };
        }

        //
        public bool TryGetCursor1FrequencyHz(out double hz)
        {
            hz = 0;
            if (freqAxis == null || freqAxis.Length == 0) return false;
            int idx = Math.Max(0, Math.Min(_cursorIdxL, freqAxis.Length - 1));
            hz = freqAxis[idx];
            return true;
        }

        public bool TryGetCursor2FrequencyHz(out double hz)
        {
            hz = 0;
            if (freqAxis == null || freqAxis.Length == 0) return false;
            int idx = Math.Max(0, Math.Min(_cursorIdxR, freqAxis.Length - 1));
            hz = freqAxis[idx];
            return true;
        }

        public void SetCursor1FrequencyHz(double targetHz)
        {
            if (freqAxis == null || freqAxis.Length == 0) return;

            bool useLog = (chkLogX != null && chkLogX.Checked);
            int minIdx = useLog ? GetMinIndexForLog(freqAxis) : 0;

            _cursorTargetHzL = targetHz;

            int idx = FindNearestFreqIndex(freqAxis, targetHz, minIdx);
            _cursorIdxL  = idx;
            _cursorFreqL = freqAxis[idx];

            // lock auto si placé par dialogue
            _cursorLockedL = true;

            RefreshCursorsFromCurrentSpectrum();
        }

        public void SetCursor2FrequencyHz(double targetHz)
        {
            if (freqAxis == null || freqAxis.Length == 0) return;

            bool useLog = (chkLogX != null && chkLogX.Checked);
            int minIdx = useLog ? GetMinIndexForLog(freqAxis) : 0;

            _cursorTargetHzR = targetHz;

            int idx = FindNearestFreqIndex(freqAxis, targetHz, minIdx);
            _cursorIdxR  = idx;
            _cursorFreqR = freqAxis[idx];

            // lock auto si placé par dialogue
            _cursorLockedR = true;

            RefreshCursorsFromCurrentSpectrum();
        }

        public bool TryFindPeakFrequencyForCursor1(out double peakHz)
            => TryFindPeakFrequencyForCursor(isCursor1: true, out peakHz);

        public bool TryFindPeakFrequencyForCursor2(out double peakHz)
            => TryFindPeakFrequencyForCursor(isCursor1: false, out peakHz);

        // --- NEW: Prev/Next Peak (for buttons "<" and ">") ------------------------------------
        //
        // These methods search for the next/previous "prominent peak" relative to the CURRENT
        // cursor position, using the SAME peak definition as the "Peak" button:
        //
        // - Local maximum (3-point test)
        // - Minimum prominence vs neighbors (±PEAK_PROMINENCE_HALF_WINDOW_BINS)
        // - Minimum dB above local noise floor (median estimate)
        //
        // The search is done within the currently visible X range of the chart.
        // --------------------------------------------------------------------------------------

        public bool TryFindNextPeakFrequencyForCursor1(out double peakHz)
            => TryFindAdjacentPeakFrequencyForCursor(isCursor1: true, direction: +1, out peakHz);

        public bool TryFindPreviousPeakFrequencyForCursor1(out double peakHz)
            => TryFindAdjacentPeakFrequencyForCursor(isCursor1: true, direction: -1, out peakHz);

        public bool TryFindNextPeakFrequencyForCursor2(out double peakHz)
            => TryFindAdjacentPeakFrequencyForCursor(isCursor1: false, direction: +1, out peakHz);

        public bool TryFindPreviousPeakFrequencyForCursor2(out double peakHz)
            => TryFindAdjacentPeakFrequencyForCursor(isCursor1: false, direction: -1, out peakHz);

        private bool TryFindAdjacentPeakFrequencyForCursor(bool isCursor1, int direction, out double peakHz)
        {
            peakHz = 0;

            if (freqAxis == null || freqAxis.Length == 0) return false;
            if (direction != -1 && direction != +1) return false;

            // which curve is actually tracked by this cursor?
            var curve = ResolveCurveForCursor(isCursor1);
            if (curve == ResolvedCurve.None) return false;

            double[]? spec = null;

            switch (curve)
            {
                case ResolvedCurve.Left:
                    spec = currentLeftSpectrum;
                    break;

                case ResolvedCurve.Right:
                    spec = currentRightSpectrum;
                    break;

                case ResolvedCurve.Gain:
                    spec = currentLeftSpectrum;      // FRF gain stored here (dB)
                    break;

                case ResolvedCurve.Phase:
                    spec = currentFrfPhaseDeg;       // FRF phase (deg)
                    break;
            }

            if (spec == null || spec.Length == 0) return false;

            var area = chartSpectrum.ChartAreas["SpectrumArea"];

            // visible X range
            double fMin = area.AxisX.Minimum;
            double fMax = area.AxisX.Maximum;
            if (fMax <= fMin) return false;

            bool useLog = (chkLogX != null && chkLogX.Checked);
            int minIdx = useLog ? GetMinIndexForLog(freqAxis) : 0;

            int i0 = LowerBound(freqAxis, fMin);
            int i1 = UpperBound(freqAxis, fMax) - 1;

            i0 = Math.Max(i0, minIdx);
            i1 = Math.Min(i1, Math.Min(freqAxis.Length - 1, spec.Length - 1));
            if (i1 <= i0) return false;

            // start from current cursor index, clamped in visible range
            int cur = isCursor1 ? _cursorIdxL : _cursorIdxR;
            cur = Math.Max(i0, Math.Min(cur, i1));
            // ------------------------------------------------------------------
            // < / > buttons peak navigation based on what is VISIBLE in the chart:
            //
            // We intentionally use the *PLOT* arrays (_plot*) because they are exactly
            // what is displayed (decimated / aligned with current zoom).
            // => The noise floor we compute matches the visible noise floor.
            // ------------------------------------------------------------------

            // 0) Choose PLOT arrays according to the curve tracked by this cursor
            double[]? plotSpec = null;
            double[]? plotFreq = _plotFreqAxis;

            switch (curve)
            {
                case ResolvedCurve.Left:
                    plotSpec = _plotLeftSpectrum;
                    break;

                case ResolvedCurve.Right:
                    plotSpec = _plotRightSpectrum;
                    break;

                case ResolvedCurve.Gain:
                    plotSpec = _plotLeftSpectrum; // FRF gain is stored/plot on left
                    break;

                case ResolvedCurve.Phase:
                    plotSpec = _plotFrfPhaseDeg;  // phase plot
                    break;
            }

            if (plotFreq == null || plotSpec == null || plotFreq.Length == 0 || plotSpec.Length == 0)
                return false;

            // 1) Current cursor frequency (FULL) -> Hz
            double curHz = freqAxis[cur];

            // 2) Noise floor = MEDIAN of the VISIBLE plot values
            // (ignore NaN/Inf)
            var tmp = new List<double>(plotSpec.Length);
            for (int i = 0; i < plotSpec.Length; i++)
            {
                double v = plotSpec[i];
                if (double.IsNaN(v) || double.IsInfinity(v)) continue;
                tmp.Add(v);
            }
            if (tmp.Count == 0) return false;

            tmp.Sort();
            double noiseFloorDb = (tmp.Count & 1) == 1
                ? tmp[tmp.Count / 2]
                : 0.5 * (tmp[tmp.Count / 2 - 1] + tmp[tmp.Count / 2]);

            double thresholdDb = noiseFloorDb + _noiseToPeakThresholdDb;


            // 3) Build groups of contiguous PLOT points above threshold
            // and keep one peak per group (max value inside group)
            var peaksHz = new List<double>();

            bool inGroup = false;
            double groupTopDb = double.NegativeInfinity;
            double groupTopHz = 0;

            int n = Math.Min(plotFreq.Length, plotSpec.Length);

            for (int i = 0; i < n; i++)
            {
                double v = plotSpec[i];
                if (double.IsNaN(v) || double.IsInfinity(v)) continue;

                if (v >= thresholdDb)
                {
                    if (!inGroup)
                    {
                        inGroup = true;
                        groupTopDb = v;
                        groupTopHz = plotFreq[i];
                    }
                    else if (v > groupTopDb)
                    {
                        groupTopDb = v;
                        groupTopHz = plotFreq[i];
                    }
                }
                else if (inGroup)
                {
                    peaksHz.Add(groupTopHz);
                    inGroup = false;
                }
            }

            if (inGroup)
                peaksHz.Add(groupTopHz);

            if (peaksHz.Count == 0)
                return false;

            // 4) navEps = demi-largeur du bucket au curseur courant.
            //
            // PREUVE que ça ne reboucle jamais :
            //   Le bin max retourné est dans [bucketCenter ± halfBucket], donc
            //   freqAxis[binMax] >= bucketCenter - halfBucket.
            //   Après placement, curHz = freqAxis[binMax].
            //   Condition pour re-sélectionner le même bucket :
            //     bucketCenter > curHz + navEps
            //     = freqAxis[binMax] + halfBucket
            //     <= bucketCenter - halfBucket + halfBucket
            //     = bucketCenter   → FALSE (strict >).    QED ✓
            //
            // PREUVE que le bucket suivant est toujours trouvé :
            //   nextBucketCenter = bucketCenter + 2*halfBucket
            //   nextBucketCenter > curHz + navEps
            //   >= bucketCenter - halfBucket + halfBucket = bucketCenter   → TRUE ✓
            double navEps;
            if (_plotFreqAxis != null && _plotFreqAxis.Length >= 2)
            {
                int pi = Math.Max(0, Math.Min(_plotFreqAxis.Length - 1,
                             LowerBound(_plotFreqAxis, curHz)));
                navEps = (pi > 0 && pi < _plotFreqAxis.Length - 1)
                    ? (_plotFreqAxis[pi + 1] - _plotFreqAxis[pi - 1]) * 0.5
                    : Math.Abs(_plotFreqAxis[1] - _plotFreqAxis[0]);
            }
            else
                navEps = freqAxis.Length > 1 ? freqAxis[1] - freqAxis[0] : 1.0;

            double selectedHz = 0;
            bool found = false;

            if (direction > 0)
            {
                for (int k = 0; k < peaksHz.Count; k++)
                {
                    if (peaksHz[k] > curHz + navEps) { selectedHz = peaksHz[k]; found = true; break; }
                }
            }
            else
            {
                for (int k = peaksHz.Count - 1; k >= 0; k--)
                {
                    if (peaksHz[k] < curHz - navEps) { selectedHz = peaksHz[k]; found = true; break; }
                }
            }

            if (!found)
                return false;

            // 5) Raffiner vers le bin de puissance maximale dans le bucket décimé.
            //    selectedHz = centre du bucket → le vrai pic FFT peut être décalé
            //    (jusqu'à ±halfBucket pour un signal cohérent comme F10).
            //    La demi-largeur du bucket est estimée depuis _plotFreqAxis.
            {
                double hb;
                if (_plotFreqAxis != null && _plotFreqAxis.Length >= 2)
                {
                    int pi = Math.Max(0, Math.Min(_plotFreqAxis.Length - 1,
                                 LowerBound(_plotFreqAxis, selectedHz)));
                    hb = (pi > 0 && pi < _plotFreqAxis.Length - 1)
                        ? (_plotFreqAxis[pi + 1] - _plotFreqAxis[pi - 1]) * 0.5
                        : Math.Abs(_plotFreqAxis[1] - _plotFreqAxis[0]);
                }
                else
                    hb = navEps;

                int ri0 = Math.Max(i0, LowerBound(freqAxis, selectedHz - hb));
                int ri1 = Math.Min(i1, UpperBound(freqAxis, selectedHz + hb) - 1);

                if (ri1 > ri0)
                {
                    int best2 = ri0;
                    double bd2 = spec[ri0];
                    for (int k = ri0 + 1; k <= ri1; k++)
                        if (spec[k] > bd2) { bd2 = spec[k]; best2 = k; }
                    peakHz = freqAxis[best2];
                    return true;
                }
            }

            peakHz = selectedHz;
            return true;


        }

        /// <summary>
        /// Retourne true si <paramref name="hz"/> est exactement un centre de bucket décimé
        /// (valeur issue de <see cref="_plotFreqAxis"/>), ce qui indique un appel depuis les
        /// boutons &lt; / &gt; (navigation entre pics adjacents).
        /// </summary>
        private bool IsBucketCenter(double hz)
        {
            var pf = _plotFreqAxis;
            if (pf == null || pf.Length == 0) return false;
            int j = LowerBound(pf, hz);
            if (j < pf.Length && pf[j] == hz) return true;
            if (j > 0 && pf[j - 1] == hz) return true;
            return false;
        }

        /// <summary>
        /// Affine <paramref name="idx"/> vers le bin de puissance maximale dans le bucket
        /// décimé contenant <paramref name="targetHz"/>. Résout le décalage entre le centre
        /// du bucket (retourné par la recherche de pic adjacent) et le vrai bin max FFT.
        /// La largeur du bucket est estimée depuis <see cref="_plotFreqAxis"/>.
        /// </summary>
        private int SnapIdxToLocalPeak(int idx, double targetHz, int minIdx, double[]? spec)
        {
            if (spec == null || freqAxis == null || _plotFreqAxis == null || _plotFreqAxis.Length < 2)
                return idx;

            // Estimer la demi-largeur du bucket décimé à la fréquence targetHz
            int pi = LowerBound(_plotFreqAxis, targetHz);
            pi = Math.Max(0, Math.Min(_plotFreqAxis.Length - 1, pi));

            double halfBucket;
            if (pi > 0 && pi < _plotFreqAxis.Length - 1)
                halfBucket = (_plotFreqAxis[pi + 1] - _plotFreqAxis[pi - 1]) * 0.5;
            else
                halfBucket = Math.Abs(_plotFreqAxis[1] - _plotFreqAxis[0]);

            // Chercher le bin maximal en pleine résolution dans [targetHz ± halfBucket]
            double refMin = targetHz - halfBucket;
            double refMax = targetHz + halfBucket;

            int ri0 = Math.Max(minIdx, LowerBound(freqAxis, refMin));
            int ri1 = Math.Min(Math.Min(freqAxis.Length, spec.Length) - 1,
                               UpperBound(freqAxis, refMax) - 1);

            if (ri1 <= ri0) return idx;

            int best = idx;
            double bestDb = (idx >= 0 && idx < spec.Length) ? spec[idx] : double.NegativeInfinity;

            for (int k = ri0; k <= ri1; k++)
            {
                if (spec[k] > bestDb) { bestDb = spec[k]; best = k; }
            }

            return best;
        }

        private static double MedianOfRange(double[] data, int i0, int i1)
        {
            int n = i1 - i0 + 1;
            if (n <= 0) return double.NaN;

            var tmp = new double[n];
            Array.Copy(data, i0, tmp, 0, n);
            Array.Sort(tmp);

            return (n & 1) == 1
                ? tmp[n / 2]
                : 0.5 * (tmp[n / 2 - 1] + tmp[n / 2]);
        }

        private static double MedianInclusive(List<double> values)
        {
            if (values.Count == 0) return double.NegativeInfinity;
            values.Sort();
            int n = values.Count;
            if ((n & 1) == 1) return values[n / 2];
            return 0.5 * (values[n / 2 - 1] + values[n / 2]);
        }

        private static bool IsProminentPeak(double[] specDb, int i, int i0, int i1)
        {
            // Sécurité indices
            if (i <= i0 || i >= i1)
                return false;

            double center = specDb[i];

            // --------------------------------------------------------------------
            // 1) Max local simple (forme)
            // --------------------------------------------------------------------
            if (center < specDb[i - 1] || center < specDb[i + 1])
                return false;

            // --------------------------------------------------------------------
            // 2) Prominence par rapport aux "épaules" (hors lobe principal)
            // --------------------------------------------------------------------
            int a = Math.Max(i0, i - PEAK_PROMINENCE_HALF_WINDOW_BINS);
            int b = Math.Min(i1, i + PEAK_PROMINENCE_HALF_WINDOW_BINS);

            double bestShoulder = double.NegativeInfinity;

            for (int k = a; k <= b; k++)
            {
                // Exclure le lobe principal
                if (Math.Abs(k - i) <= PEAK_PROMINENCE_EXCLUDE_BINS)
                    continue;

                bestShoulder = Math.Max(bestShoulder, specDb[k]);
            }

            if (double.IsNegativeInfinity(bestShoulder))
                return false;

            double prominenceDb = center - bestShoulder;
            if (prominenceDb < PEAK_MIN_PROMINENCE_DB)
                return false;

            // --------------------------------------------------------------------
            // 3) Noise floor local (médiane robuste)
            // --------------------------------------------------------------------
            int na = Math.Max(i0, i - NOISE_FLOOR_HALF_WINDOW_BINS);
            int nb = Math.Min(i1, i + NOISE_FLOOR_HALF_WINDOW_BINS);

            var noiseVals = new List<double>(nb - na);

            for (int k = na; k <= nb; k++)
            {
                // On exclut la zone du pic lui-même
                if (Math.Abs(k - i) <= PEAK_PROMINENCE_EXCLUDE_BINS)
                    continue;

                noiseVals.Add(specDb[k]);
            }

            if (noiseVals.Count == 0)
                return false;

            double noiseFloor = MedianInclusive(noiseVals);

            if ((center - noiseFloor) < PEAK_MIN_ABOVE_NOISE_DB)
                return false;

            // --------------------------------------------------------------------
            // → Pic valide
            // --------------------------------------------------------------------
            return true;
        }

        private bool TryFindPeakFrequencyForCursor(bool isCursor1, out double peakHz)
        {
            peakHz = 0;

            if (freqAxis == null || freqAxis.Length == 0) return false;

            // voie réellement trackée
            var curve = ResolveCurveForCursor(isCursor1);
            if (curve == ResolvedCurve.None) return false;

            double[]? spec = null;
            double[]? plotSpec = null;

            switch (curve)
            {
                case ResolvedCurve.Left:
                    spec     = currentLeftSpectrum;
                    plotSpec = _plotLeftSpectrum;
                    break;

                case ResolvedCurve.Right:
                    spec     = currentRightSpectrum;
                    plotSpec = _plotRightSpectrum;
                    break;

                case ResolvedCurve.Gain:
                    spec     = currentLeftSpectrum;
                    plotSpec = _plotLeftSpectrum;
                    break;

                case ResolvedCurve.Phase:
                    spec     = currentFrfPhaseDeg;
                    plotSpec = _plotFrfPhaseDeg;
                    break;
            }

            if (spec == null || spec.Length == 0) return false;

            var area = chartSpectrum.ChartAreas["SpectrumArea"];

            // Plage visible : on utilise GetVisibleFreqRangeHz() pour être cohérent
            // avec l'affichage (ScaleView ou Minimum/Maximum selon le mode).
            var (fMin, fMax) = GetVisibleFreqRangeHz();
            if (fMax <= fMin) return false;

            bool useLog = (chkLogX != null && chkLogX.Checked);
            int minIdx = useLog ? GetMinIndexForLog(freqAxis) : 0;

            int i0 = LowerBound(freqAxis, fMin);
            int i1 = UpperBound(freqAxis, fMax) - 1;

            i0 = Math.Max(i0, minIdx);
            i1 = Math.Min(i1, Math.Min(freqAxis.Length - 1, spec.Length - 1));
            if (i1 <= i0) return false;

            // ── Phase 1 : trouver le bucket visuel maximum dans les données décimées ──
            // L'affichage utilise _plotLeftSpectrum (MAX-HOLD par bucket). Le centre
            // visuel du pic est _plotFreqAxis[bestPlot], PAS freqAxis[bestFullRes].
            // Si on cherchait directement en pleine résolution, le curseur se poserait
            // au bin exact (correct) mais légèrement décalé visuellement par rapport au
            // centre du bucket affiché → impression de "quelques bins" d'erreur.
            // → On identifie d'abord le bucket visuel, puis on affine en pleine résolution.
            double[]? plotFreq = _plotFreqAxis;
            int bestFullRes = -1;

            if (plotSpec != null && plotFreq != null &&
                plotSpec.Length > 0 && plotFreq.Length == plotSpec.Length)
            {
                // Trouver le max dans les données décimées (plage visible)
                int pi0 = LowerBound(plotFreq, fMin);
                int pi1 = UpperBound(plotFreq, fMax) - 1;
                pi0 = Math.Max(0, Math.Min(plotSpec.Length - 1, pi0));
                pi1 = Math.Max(pi0, Math.Min(plotSpec.Length - 1, pi1));

                int bestPlot = pi0;
                double bestPlotDb = plotSpec[pi0];
                for (int j = pi0 + 1; j <= pi1; j++)
                {
                    if (plotSpec[j] > bestPlotDb) { bestPlotDb = plotSpec[j]; bestPlot = j; }
                }

                // Estimer la largeur du bucket (centre ± demi-largeur)
                double bucketCenter = plotFreq[bestPlot];
                double halfBucket;
                if (bestPlot > 0 && bestPlot < plotFreq.Length - 1)
                    halfBucket = (plotFreq[bestPlot + 1] - plotFreq[bestPlot - 1]) * 0.5;
                else if (plotFreq.Length > 1)
                    halfBucket = Math.Abs(plotFreq[1] - plotFreq[0]);
                else
                    halfBucket = (fMax - fMin) / Math.Max(1, plotFreq.Length);

                // Phase 2 : affiner en pleine résolution dans ce bucket
                double refMin = bucketCenter - halfBucket;
                double refMax = bucketCenter + halfBucket;

                int ri0 = LowerBound(freqAxis, refMin);
                int ri1 = UpperBound(freqAxis, refMax) - 1;
                ri0 = Math.Max(i0, ri0);
                ri1 = Math.Min(i1, ri1);

                if (ri1 > ri0)
                {
                    // Maximum pleine résolution dans le bucket visuel
                    bestFullRes = ri0;
                    double bestDb2 = spec[ri0];
                    for (int i = ri0 + 1; i <= ri1; i++)
                    {
                        if (spec[i] > bestDb2) { bestDb2 = spec[i]; bestFullRes = i; }
                    }
                }
            }

            if (bestFullRes >= 0)
            {
                peakHz = freqAxis[bestFullRes];
                return true;
            }

            // ── Fallback : recherche classique pleine résolution avec IsProminentPeak ──
            int best = -1;
            double bestDb = double.NegativeInfinity;

            int start = i0 + 1;
            int end   = i1 - 1;

            for (int i = start; i <= end; i++)
            {
                if (!IsProminentPeak(spec, i, i0, i1)) continue;
                double db = spec[i];
                if (db > bestDb) { bestDb = db; best = i; }
            }

            // Fallback ultime : max simple sans critères de prominance
            if (best < 0)
            {
                best   = i0;
                bestDb = spec[i0];
                for (int i = i0 + 1; i <= i1; i++)
                {
                    double db = spec[i];
                    if (db > bestDb) { bestDb = db; best = i; }
                }
            }

            peakHz = freqAxis[best];
            return true;
        }

        //
        private static int LowerBound(double[] a, double x)
        {
            int lo = 0, hi = a.Length;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (a[mid] < x) lo = mid + 1;
                else hi = mid;
            }
            return lo;
        }

        private static int UpperBound(double[] a, double x)
        {
            int lo = 0, hi = a.Length;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (a[mid] <= x) lo = mid + 1;
                else hi = mid;
            }
            return lo;
        }

        //
        private void RefreshCursorsFromCurrentSpectrum()
        {
            if (freqAxis == null || freqAxis.Length == 0)
                return;

            var area = chartSpectrum.ChartAreas["SpectrumArea"];

            bool onC1 = chkCursorL != null && chkCursorL.Checked; // C1
            bool onC2 = chkCursorR != null && chkCursorR.Checked; // C2

            bool useLog = (chkLogX != null && chkLogX.Checked);
            int minIdx = useLog ? GetMinIndexForLog(freqAxis) : 0;

            // --- Garder la fréquence des curseurs constante même si N change ---
            // (recalcule l'index à partir de la fréquence mémorisée)
            if (useLog)
            {
                if (_cursorFreqL <= 0.0) _cursorFreqL = LogXAbsMinHz;
                if (_cursorFreqR <= 0.0) _cursorFreqR = LogXAbsMinHz;
                if (_cursorTargetHzL <= 0.0) _cursorTargetHzL = LogXAbsMinHz;
                if (_cursorTargetHzR <= 0.0) _cursorTargetHzR = LogXAbsMinHz;

            }

            if (onC1 && _cursorTargetHzL > 0)
                _cursorIdxL = FindNearestFreqIndex(freqAxis, _cursorTargetHzL, minIdx);

            if (onC2 && _cursorTargetHzR > 0)
                _cursorIdxR = FindNearestFreqIndex(freqAxis, _cursorTargetHzR, minIdx);


            // Sécurise les index (si pas init / hors plage)
            if (_cursorIdxL < minIdx) _cursorIdxL = minIdx;
            if (_cursorIdxR < minIdx) _cursorIdxR = minIdx;
            if (_cursorIdxL >= freqAxis.Length) _cursorIdxL = freqAxis.Length - 1;
            if (_cursorIdxR >= freqAxis.Length) _cursorIdxR = freqAxis.Length - 1;

            // Résolution voie pour chaque curseur
            var c1Curve = ResolveCurveForCursor(isCursor1: true);
            var c2Curve = ResolveCurveForCursor(isCursor1: false);

            void UpdateOne(
                bool enabled,
                bool isCursor1,
                int idx,
                VerticalLineAnnotation? line,
                Label? lbl,
                bool locked,
                ResolvedCurve curve,
                ref bool markVisible,
                ref double markFreq,
                ref double markDb,
                ref Color markColor,
                ref bool markUsesY2)
            {
                if (!enabled || line == null || idx < minIdx || idx >= freqAxis.Length)
                {
                    if (line != null) line.Visible = false;
                    if (lbl != null) { lbl.Text = ""; lbl.Visible = false; }
                    markVisible = false;
                    return;
                }

                double f = freqAxis[idx];

                // Ligne verticale : toujours à la bonne fréquence
                line.X = f;
                line.Visible = true;

                // Si la voie demandée n'existe pas -> grisé, pas de triangle
                if (curve == ResolvedCurve.None)
                {
                    line.LineColor = Color.FromArgb(180, Color.Gray);
                    markVisible = false;
                    markColor = Color.Gray;

                    if (lbl != null)
                    {
                        lbl.ForeColor = Color.Gray;
                        lbl.Text = $"{(isCursor1 ? "C1" : "C2")}({CursorCurveLabel(curve)}): {f:0.##} Hz  --" + (locked ? "  (LOCK)" : "");
                        lbl.Visible = true;
                    }
                    return;
                }

                // Sélection de la source selon la courbe réellement trackée
                double[]? spec = null;
                bool useY2 = false;          // phase => AxisY2
                string unit = CurrentLevelUnitLabel(); // par défaut (FFT normal)

                switch (curve)
                {
                    case ResolvedCurve.Left:
                        spec = currentLeftSpectrum;
                        break;

                    case ResolvedCurve.Right:
                        spec = currentRightSpectrum;
                        break;

                    case ResolvedCurve.Gain:
                        spec = currentLeftSpectrum; // Gain FRF stocké ici (déjà en dB)
                        unit = "dB";
                        break;

                    case ResolvedCurve.Phase:
                        spec = currentFrfPhaseDeg;  // Phase FRF
                        unit = "°";
                        useY2 = true;
                        break;
                }

                if (spec == null || idx >= spec.Length)
                {
                    line.LineColor = Color.FromArgb(180, Color.Gray);
                    markVisible = false;
                    markColor = Color.Gray;
                    markUsesY2 = false;

                    if (lbl != null)
                    {
                        lbl.ForeColor = Color.Gray;
                        lbl.Text = $"{(isCursor1 ? "C1" : "C2")}({CurveLetter(curve)}): {f:0.##} Hz  --" + (locked ? "  (LOCK)" : "");
                        lbl.Visible = true;
                    }
                    return;
                }

                double yVal = spec[idx];

                // IMPORTANT :
                // - En mode FRF : Gain est déjà en dB => pas de dBV/dBFS
                // - En mode FFT normal : on conserve la conversion dBFS->dBV si activée
                if (!IsFrfMode())
                {
                    if (UseDbvForCurrentMode())
                    {
                        // Ici curve = Left/Right uniquement (normal)
                        yVal += (curve == ResolvedCurve.Right) ? _levelCalOffsetDbRight : _levelCalOffsetDbLeft;
                    }
                }

                var col = CurveColor(curve);
                line.LineColor = Color.FromArgb(180, col);

                markVisible = true;
                markFreq = f;
                markDb = yVal;       // conserve le nom "Db" mais contient Y (dB ou deg) selon la courbe
                markColor = col;
                markUsesY2 = useY2;

                if (lbl != null)
                {
                    lbl.ForeColor = InfoTextColor;
                    lbl.Text = $"{(isCursor1 ? "C1" : "C2")}({CursorCurveLabel(curve)}): {f:0.##} Hz  {yVal:0.##} {unit}" + (locked ? "  (LOCK)" : "");
                    lbl.Visible = true;
                }

            }

            // C1
            if (onC1 && _cursorLineL != null)
            {
                _cursorFreqL = freqAxis[_cursorIdxL];
                UpdateOne(onC1, true, _cursorIdxL, _cursorLineL, _lblCursorLInfo, _cursorLockedL, c1Curve,
                    ref _cursorMarkLVisible, ref _cursorMarkLFreq, ref _cursorMarkLDb, ref _cursorMarkLColor, ref _cursorMarkLUsesY2);

            }
            else
            {
                if (_cursorLineL != null) _cursorLineL.Visible = false;
                if (_lblCursorLInfo != null) { _lblCursorLInfo.Text = ""; _lblCursorLInfo.Visible = false; }
                _cursorMarkLVisible = false;
                _cursorMarkLUsesY2 = false;
            }

            // C2
            if (onC2 && _cursorLineR != null)
            {
                _cursorFreqR = freqAxis[_cursorIdxR];
                UpdateOne(onC2, false, _cursorIdxR, _cursorLineR, _lblCursorRInfo, _cursorLockedR, c2Curve,
                        ref _cursorMarkRVisible, ref _cursorMarkRFreq, ref _cursorMarkRDb, ref _cursorMarkRColor, ref _cursorMarkRUsesY2);

            }
            else
            {
                if (_cursorLineR != null) _cursorLineR.Visible = false;
                if (_lblCursorRInfo != null) { _lblCursorRInfo.Text = ""; _lblCursorRInfo.Visible = false; }
                _cursorMarkRVisible = false;
                _cursorMarkRUsesY2 = false;
            }
            //
            // --- Delta (C2 - C1) ---
            if (_lblCursorDeltaInfo != null)
            {
                // visible seulement si au moins un curseur activé
                if (!onC1 && !onC2)
                {
                    _lblCursorDeltaInfo.Text = "";
                    _lblCursorDeltaInfo.Visible = false;
                }
                else
                {
                    _lblCursorDeltaInfo.Visible = true;

                    // seulement si les 2 curseurs ont une valeur amplitude valide
                    if (_cursorMarkLVisible && _cursorMarkRVisible)
                    {
                        double dF = _cursorMarkRFreq - _cursorMarkLFreq; // C2 - C1

                        // Si les curseurs sont sur des courbes différentes (Gain vs Phase) => delta uniquement fréquence
                        if (_cursorMarkLUsesY2 != _cursorMarkRUsesY2)
                        {
                            _lblCursorDeltaInfo.ForeColor = InfoTextColor;
                            _lblCursorDeltaInfo.Text = $"Δ:  ΔF={dF:0.##} Hz";
                        }
                        else
                        {
                            double dY = _cursorMarkRDb - _cursorMarkLDb; // Y = dB (Gain) OU deg (Phase)

                            string unit = _cursorMarkLUsesY2 ? "°" : (IsFrfMode() ? "dB" : CurrentLevelUnitLabel());

                            _lblCursorDeltaInfo.ForeColor = InfoTextColor;
                            _lblCursorDeltaInfo.Text = $"Δ:  ΔF={dF:0.##} Hz   ΔY={dY:0.##} {unit}";
                        }

                    }
                    else
                    {
                        _lblCursorDeltaInfo.ForeColor = InfoTextColor;
                        _lblCursorDeltaInfo.Text = "Δ:  —";
                    }
                }
            }

            //
            PositionCursorInfoLabels();
            chartSpectrum.Invalidate();
        }

        //
        private void ChartSpectrum_MouseUp(object? sender, MouseEventArgs e)
        {
            StopDeltaLabelDrag();
            _cursorBoxDragging = false;

            if (e.Button == MouseButtons.Right)
            {
                ShowAxisDialog();
            }
        }








    }
}
