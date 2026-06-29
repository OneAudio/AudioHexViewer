// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

// ═══════════════════════════════════════════════════════════════════════════
// SpectrumControl.CursorsBarX.cs
//
// Wiring and logic for the new X1/X2 cursor bar (replacing the old C1/C2 bar).
//
// Layout (calqué oscilloscope) :
//   Row 1 (y=34) : [X1] [Channel] [X1= ?] [Peak]  [Graph ]
//   Row 2 (y=60) : [X2] [Channel] [X2= ?] [Peak]  [Label ]
//
// Display modes (btnDispGraph, global):
//   0 = Panel  — values in right stats panel only
//   1 = Graph  — draggable floating box in chart only
//   2 = Both   — panel + floating box
//   3 = Off    — nothing displayed
//
// Label mode (btnDispLabel toggle):
//   OFF → ON  : shows draggable Δ arrow between X1 and X2 in the chart
// ═══════════════════════════════════════════════════════════════════════════
using System;
using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    public partial class SpectrumControl
    {
        // ── Display state ────────────────────────────────────────────────────
        private int  _cursorDispMode = 0;    // 0=Panel 1=Graph 2=Both 3=Off
        private bool _cursorLabelOn  = false; // Δ label arrow in chart

        // Floating cursor box drag state (mirrors OscilloscopeControl)
        private float     _cursorBoxOffX = 8f;
        private float     _cursorBoxOffY = 8f;
        private RectangleF _cursorBoxRect = RectangleF.Empty;
        private bool      _cursorBoxDragging = false;
        private PointF    _cursorBoxDragOffset;

        // ── Init ─────────────────────────────────────────────────────────────
        private bool _cursorBarXInited = false;

        private void InitCursorBarX()
        {
            if (_cursorBarXInited) return;
            _cursorBarXInited = true;

            // Sync initial text on channel buttons
            SyncCursorXUi();

            // Wire events
            btnX1.Click += (_, _) =>
            {
                CursorLeftEnabled = !CursorLeftEnabled;
                // Premier placement 1/3 de l'axe si encore jamais positionné
                if (CursorLeftEnabled && double.IsNaN(_cursorTargetHzL))
                    AutoPlaceCursor(isLeft: true);
                SyncCursorXUi();
            };
            btnX2.Click += (_, _) =>
            {
                CursorRightEnabled = !CursorRightEnabled;
                // Premier placement 2/3 de l'axe si encore jamais positionné
                if (CursorRightEnabled && double.IsNaN(_cursorTargetHzR))
                    AutoPlaceCursor(isLeft: false);
                SyncCursorXUi();
            };

            btnX1Chan.Click    += (_, _) => { CycleChannelBtn(isX1: true);  SyncCursorXUi(); };
            btnX2Chan.Click    += (_, _) => { CycleChannelBtn(isX1: false); SyncCursorXUi(); };

            btnX1Set.Click     += (_, _) => OpenSimpleSetDialog(isC1: true);
            btnX2Set.Click     += (_, _) => OpenSimpleSetDialog(isC1: false);

            // Peak: left click = max peak, right click = context menu
            btnX1Peak.Click    += (_, _) => MoveCursorToPeak(isC1: true);
            btnX2Peak.Click    += (_, _) => MoveCursorToPeak(isC1: false);
            btnX1Peak.MouseUp  += Peak_MouseUp;
            btnX2Peak.MouseUp  += Peak_MouseUp;

            // Display: Graph cycles 4 states, Label toggles
            btnDispGraph.Click += (_, _) => { _cursorDispMode = (_cursorDispMode + 1) % 4; UpdateDispButtons(); chartSpectrum?.Invalidate(); };
            btnDispLabel.Click += (_, _) => { _cursorLabelOn = !_cursorLabelOn;             UpdateDispButtons(); chartSpectrum?.Invalidate(); };

            // Drag on chartSpectrum for floating cursor box
            chartSpectrum.MouseDown += ChartSpectrum_CursorBoxMouseDown;
            chartSpectrum.MouseMove += ChartSpectrum_CursorBoxMouseMove;
            chartSpectrum.MouseUp   += (_, _) => _cursorBoxDragging = false;

            UpdateDispButtons();
        }

        // ── Channel cycling ──────────────────────────────────────────────────
        // In Bode Plot mode:  Auto → Gain → Phase → Auto
        // In other modes:     Auto → Left → Right → Auto

        private void CycleChannelBtn(bool isX1)
        {
            string cur = isX1 ? btnX1Chan.Text : btnX2Chan.Text;
            string next = NextChannelText(cur);
            if (isX1) { btnX1Chan.Text = next; C1TrackMode = TextToMode(next); }
            else      { btnX2Chan.Text = next; C2TrackMode = TextToMode(next); }
            chartSpectrum?.Invalidate();
        }

        private string NextChannelText(string cur)
        {
            if (IsFrfMode())
            {
                if (!_frfPhaseEnabled)
                    return cur == "Gain" ? "Auto" : "Gain";

                return cur switch { "Gain" => "Phase", "Phase" => "Auto", _ => "Gain" };
            }
            return cur switch { "Left" => "Right", "Right" => "Auto", _ => "Left" };
        }

        // ── Peak context menu ────────────────────────────────────────────────
        private void Peak_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            bool isX1 = (sender == btnX1Peak);

            var menu = new ContextMenuStrip();
            menu.Items.Add("Prev Peak",  null, (_, _) => MoveCursorToAdjacentPeak(isX1, next: false));
            menu.Items.Add("Next Peak",  null, (_, _) => MoveCursorToAdjacentPeak(isX1, next: true));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Negative Peak",      null, (_, _) => MoveCursorToNegativePeak(isX1));
            menu.Items.Add("Prev Negative Peak", null, (_, _) => MoveCursorToAdjacentNegativePeak(isX1, next: false));
            menu.Items.Add("Next Negative Peak", null, (_, _) => MoveCursorToAdjacentNegativePeak(isX1, next: true));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Threshold Settings...", null, (_, _) => OpenNoiseToPeakThresholdDialog());

            var btn = (Button)sender!;
            menu.Show(btn, new Point(0, btn.Height));
        }

        // ── Display button visuals ───────────────────────────────────────────
        private void UpdateDispButtons()
        {
            if (btnDispGraph == null || btnDispLabel == null) return;

            bool anyCursor = CursorLeftEnabled || CursorRightEnabled;

            // Grisé si aucun curseur actif (les boutons Display n'ont pas de sens)
            btnDispGraph.Enabled = anyCursor;
            btnDispLabel.Enabled = anyCursor;

            if (!anyCursor)
            {
                SetBtnToggle(btnDispGraph, false);
                btnDispGraph.Text = "Panel";
                SetBtnToggle(btnDispLabel, false);
                return;
            }

            (string txt, bool active) = _cursorDispMode switch
            {
                0 => ("Panel", true),
                1 => ("Graph", true),
                2 => ("Both",  true),
                _ => ("Off",   false),
            };
            btnDispGraph.Text = txt;
            SetBtnToggle(btnDispGraph, active);

            SetBtnToggle(btnDispLabel, _cursorLabelOn);
        }

        private static void SetBtnToggle(Button b, bool on)
        {
            b.UseVisualStyleBackColor = !on;
            b.BackColor = on ? Color.LightGreen : SystemColors.Control;
            b.ForeColor = Color.Black;
        }

        // ── SyncCursorXUi — mirrors SyncCursorUiFromModel for new controls ──
        internal void SyncCursorXUi()
        {
            if (btnX1 == null || btnX2 == null) return;

            // Force phase-off constraint in Bode mode
            if (IsFrfMode() && !_frfPhaseEnabled)
            {
                if (C1TrackMode == CursorTrackMode.Right) C1TrackMode = CursorTrackMode.Left;
                if (C2TrackMode == CursorTrackMode.Right) C2TrackMode = CursorTrackMode.Left;
            }

            // X1 toggle
            SetBtnToggle(btnX1, CursorLeftEnabled);
            btnX1.Font = new Font(btnX1.Font, CursorLeftEnabled ? FontStyle.Bold : FontStyle.Regular);

            // X2 toggle
            SetBtnToggle(btnX2, CursorRightEnabled);
            btnX2.Font = new Font(btnX2.Font, CursorRightEnabled ? FontStyle.Bold : FontStyle.Regular);

            // Channel text from model
            btnX1Chan.Text = ModeToText(C1TrackMode);
            btnX2Chan.Text = ModeToText(C2TrackMode);

            UpdateDispButtons();
        }

        // ── Cursor color (matches the trace the cursors are on) ──────────────
        // Utilise directement _cursorMarkL/RColor qui reflètent déjà la couleur
        // de la courbe où se trouve chaque curseur (Gain rouge, Phase bleu,
        // Right bleu, Left rouge, etc.).
        // Si les deux curseurs ont des couleurs différentes (mode L+R avec
        // C1 sur Left et C2 sur Right), on retombe sur FftLeftColor.
        private Color CursorCol
        {
            get
            {
                bool c1Active = CursorLeftEnabled  && _cursorMarkLVisible;
                bool c2Active = CursorRightEnabled && _cursorMarkRVisible;

                Color baseColor;
                if (c1Active && c2Active)
                {
                    // Les deux actifs : si les couleurs concordent, on l'utilise ;
                    // sinon (curseurs sur courbes différentes), repli sur FftLeftColor.
                    baseColor = (_cursorMarkLColor.ToArgb() == _cursorMarkRColor.ToArgb())
                              ? _cursorMarkLColor
                              : FftLeftColor;
                }
                else if (c1Active) baseColor = _cursorMarkLColor;
                else if (c2Active) baseColor = _cursorMarkRColor;
                else               baseColor = FftLeftColor;

                return Color.FromArgb(210, baseColor);
            }
        }
        private Color BgCol     => BackgroundColor;   // chart background colour

        // ── Floating cursor box: draw ────────────────────────────────────────
        // Style identique oscilloscope : fond semi-transparent BgColor,
        // police "Segoe UI" 9pt Bold, pas de bordure.
        internal void DrawCursorFloatBox(Graphics g, RectangleF plotRect)
        {
            if (_cursorDispMode != 1 && _cursorDispMode != 2) return;
            bool anyActive = CursorLeftEnabled || CursorRightEnabled;
            if (!anyActive) return;

            var lines = BuildCursorFloatLines();
            if (lines.Count == 0) return;

            Color col = CursorCol;
            using var fDx    = new Font("Segoe UI", 9f, FontStyle.Bold);
            using var brBg   = new SolidBrush(Color.FromArgb(210, BgCol));
            using var brTxt  = new SolidBrush(col);
            using var fmtR   = new StringFormat { Alignment = StringAlignment.Far };

            float lineH = fDx.GetHeight(g) + 2f;
            float maxLblW = 0f, maxValW = 0f;
            foreach (var (l, v) in lines)
            {
                float lw = g.MeasureString(l, fDx).Width;
                float vw = g.MeasureString(v, fDx).Width;
                if (lw > maxLblW) maxLblW = lw;
                if (vw > maxValW) maxValW = vw;
            }

            float pad = 4f, gap = 8f;
            float boxW = pad * 2f + maxLblW + gap + maxValW;
            float boxH = pad * 2f + lines.Count * lineH;

            float bx = Math.Clamp(plotRect.Left + _cursorBoxOffX, plotRect.Left, Math.Max(plotRect.Left, plotRect.Right - boxW));
            float by = Math.Clamp(plotRect.Top  + _cursorBoxOffY, plotRect.Top,  Math.Max(plotRect.Top,  plotRect.Bottom - boxH));
            _cursorBoxOffX = bx - plotRect.Left;
            _cursorBoxOffY = by - plotRect.Top;
            _cursorBoxRect = new RectangleF(bx, by, boxW, boxH);

            g.FillRectangle(brBg, _cursorBoxRect);

            float ty = by + pad;
            foreach (var (l, v) in lines)
            {
                g.DrawString(l, fDx, brTxt, bx + pad, ty);
                g.DrawString(v, fDx, brTxt, new RectangleF(bx + pad + maxLblW + gap, ty, maxValW, lineH), fmtR);
                ty += lineH;
            }
        }

        private System.Collections.Generic.List<(string lbl, string val)> BuildCursorFloatLines()
        {
            var lines = new System.Collections.Generic.List<(string, string)>();

            bool c1On = CursorLeftEnabled  && _cursorMarkLVisible && !double.IsNaN(_cursorMarkLFreq);
            bool c2On = CursorRightEnabled && _cursorMarkRVisible && !double.IsNaN(_cursorMarkRFreq);

            static string FmtHz(double hz) => hz >= 1000.0 ? $"{hz / 1000.0:0.###} kHz" : $"{hz:0.##} Hz";
            static string FmtDb(double db) => $"{db:0.##} dB";
            static string FmtDeg(double d) => $"{d:0.##} °";

            // "Level" in spectrum modes, "Gain" only in Bode Plot
            string yLabel = IsFrfMode() ? "Gain" : "Level";

            if (c1On)
            {
                lines.Add(("X1", FmtHz(_cursorMarkLFreq)));
                lines.Add((_cursorMarkLUsesY2 ? "φ" : yLabel, _cursorMarkLUsesY2 ? FmtDeg(_cursorMarkLDb) : FmtDb(_cursorMarkLDb)));
            }
            if (c2On)
            {
                lines.Add(("X2", FmtHz(_cursorMarkRFreq)));
                lines.Add((_cursorMarkRUsesY2 ? "φ" : yLabel, _cursorMarkRUsesY2 ? FmtDeg(_cursorMarkRDb) : FmtDb(_cursorMarkRDb)));
            }
            if (c1On && c2On)
            {
                double df = Math.Abs(_cursorMarkRFreq - _cursorMarkLFreq);
                lines.Add(("ΔF", FmtHz(df)));
                if (_cursorMarkLUsesY2 == _cursorMarkRUsesY2)
                {
                    double dy = _cursorMarkRDb - _cursorMarkLDb;
                    lines.Add((_cursorMarkLUsesY2 ? "Δφ" : "Δ" + yLabel, _cursorMarkLUsesY2 ? FmtDeg(dy) : FmtDb(dy)));
                }
            }
            return lines;
        }

        // ── Floating cursor box: drag ────────────────────────────────────────
        private void ChartSpectrum_CursorBoxMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (_cursorDispMode != 1 && _cursorDispMode != 2) return;
            if (!_cursorBoxRect.Contains(e.Location)) return;
            _cursorBoxDragging = true;
            _cursorBoxDragOffset = new PointF(e.X - _cursorBoxRect.X, e.Y - _cursorBoxRect.Y);
        }

        private void ChartSpectrum_CursorBoxMouseMove(object? sender, MouseEventArgs e)
        {
            if (!_cursorBoxDragging) return;
            var plot = GetPlotRectPixels(chartSpectrum, chartSpectrum.ChartAreas[0]);
            if (plot.IsEmpty) return;
            float nx = e.X - _cursorBoxDragOffset.X - plot.Left;
            float ny = e.Y - _cursorBoxDragOffset.Y - plot.Top;
            _cursorBoxOffX = Math.Clamp(nx, 0f, Math.Max(0f, plot.Width  - _cursorBoxRect.Width));
            _cursorBoxOffY = Math.Clamp(ny, 0f, Math.Max(0f, plot.Height - _cursorBoxRect.Height));
            chartSpectrum.Invalidate();
        }

        // ── Delta label + arrows (Label button) ─────────────────────────────
        // Style identique oscilloscope :
        //   - ΔX: texte "ΔF= xxx Hz\n1/ΔF= yyy Hz" + flèche double horizontale
        //   - ΔY: texte "ΔLevel= xxx dB" + flèche double verticale
        //   Police : "Segoe UI" 9pt Bold (même que oscilloscope fDx/fDy)
        //   Fond : Color.FromArgb(210, BgColor)
        //   Flèches : Pen 1.5px, AdjustableArrowCap(4f, 5f)

        // Drag state — ΔX label moves horizontally AND vertically
        private float     _dxLblFracX    = 0.50f;
        private float     _dxLblFracY    = 0.05f;  // vertical position (fraction of plot height)
        private RectangleF _dxLblRect    = RectangleF.Empty;
        private bool      _dxDragging    = false;
        private float     _dxDragStartX, _dxDragStartFrac;
        private float     _dxDragStartY, _dxDragStartFracY;

        // Drag state — ΔY label moves vertically AND horizontally
        private float     _dyLblFracY    = 0.50f;
        private float     _dyLblFracX    = 0.04f;  // horizontal position (fraction of plot width)
        private RectangleF _dyLblRect    = RectangleF.Empty;
        private bool      _dyDragging    = false;
        private float     _dyDragStartY, _dyDragStartFrac;
        private float     _dyDragStartX, _dyDragStartFracX;

        internal void DrawCursorDeltaLabel(Graphics g, RectangleF plotRect)
        {
            if (!_cursorLabelOn) return;

            bool c1On = CursorLeftEnabled  && _cursorMarkLVisible && !double.IsNaN(_cursorMarkLFreq);
            bool c2On = CursorRightEnabled && _cursorMarkRVisible && !double.IsNaN(_cursorMarkRFreq);
            if (!c1On || !c2On) return;

            Color col = CursorCol;

            // ── ΔX : horizontal double arrow + text (identical to oscilloscope) ──
            float? px1 = FreqToPlotX(_cursorMarkLFreq, plotRect);
            float? px2 = FreqToPlotX(_cursorMarkRFreq, plotRect);

            if (px1 != null && px2 != null)
            {
                double df   = Math.Abs(_cursorMarkRFreq - _cursorMarkLFreq);
                string dts  = df >= 1000.0 ? $"{df / 1000.0:0.###} kHz" : $"{df:0.##} Hz";

                using var fDx  = new Font("Segoe UI", 9f, FontStyle.Bold);
                using var brL  = new SolidBrush(col);
                using var brBg = new SolidBrush(Color.FromArgb(210, BgCol));

                // Single line: only ΔF (no 1/ΔF — removed per point 2)
                string txt = $"ΔF= {dts}";
                var sz = g.MeasureString(txt, fDx);

                float x1px = Math.Min(px1.Value, px2.Value);
                float x2px = Math.Max(px1.Value, px2.Value);

                float lx = Math.Clamp(plotRect.Left + _dxLblFracX * plotRect.Width - sz.Width / 2f,
                                      plotRect.Left, plotRect.Right - sz.Width);
                // ly driven by _dxLblFracY (vertical drag) — point 4
                float ly = Math.Clamp(plotRect.Top + _dxLblFracY * plotRect.Height,
                                      plotRect.Top, plotRect.Bottom - sz.Height - 14f);

                _dxLblRect = new RectangleF(lx - 2, ly, sz.Width + 4, sz.Height + 2);
                g.FillRectangle(brBg, _dxLblRect);
                g.DrawString(txt, fDx, brL, lx, ly);

                // Double arrow just below the label (follows the label vertically — point 4)
                if (x2px - x1px > 8f)
                {
                    float ay = ly + sz.Height + 4f;
                    using var penArrow = new Pen(col, 1.5f);
                    var cap = new System.Drawing.Drawing2D.AdjustableArrowCap(4f, 5f, true);
                    penArrow.CustomStartCap = cap;
                    penArrow.CustomEndCap   = cap;
                    g.DrawLine(penArrow, x1px, ay, x2px, ay);
                }
            }

            // ── ΔY : vertical double arrow + text ──────────────────────────────
            // Only when both cursors are on the same axis type
            if (_cursorMarkLUsesY2 == _cursorMarkRUsesY2)
            {
                double dy      = _cursorMarkRDb - _cursorMarkLDb;
                bool   isPhase = _cursorMarkLUsesY2;
                string yLabel  = IsFrfMode() ? "Gain" : "Level";
                string dyLabel = isPhase ? $"Δφ= {dy:0.##} °" : $"Δ{yLabel}= {dy:0.##} dB";

                // Convert dB (or °) to pixel Y using the appropriate axis
                float? py1 = DbToPlotY(_cursorMarkLDb, _cursorMarkLUsesY2, plotRect);
                float? py2 = DbToPlotY(_cursorMarkRDb, _cursorMarkRUsesY2, plotRect);

                if (py1 != null && py2 != null)
                {
                    using var fDy  = new Font("Segoe UI", 9f, FontStyle.Bold);
                    using var brL  = new SolidBrush(col);
                    using var brBg = new SolidBrush(Color.FromArgb(210, BgCol));

                    var sz = g.MeasureString(dyLabel, fDy);

                    // Horizontal position driven by _dyLblFracX (draggable left/right)
                    float lx = Math.Clamp(plotRect.Left + _dyLblFracX * plotRect.Width,
                                          plotRect.Left, plotRect.Right - sz.Width - 10f);
                    float ly = Math.Clamp(plotRect.Top + _dyLblFracY * plotRect.Height - sz.Height / 2f,
                                          plotRect.Top, plotRect.Bottom - sz.Height);

                    _dyLblRect = new RectangleF(lx - 2, ly, sz.Width + 4, sz.Height + 2);
                    g.FillRectangle(brBg, _dyLblRect);
                    g.DrawString(dyLabel, fDy, brL, lx, ly);

                    float ya = Math.Min(py1.Value, py2.Value);
                    float yb = Math.Max(py1.Value, py2.Value);

                    // Horizontal dashed lines at Y1 and Y2 levels
                    using var penDash = new Pen(Color.FromArgb(140, col), 1.0f);
                    penDash.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    g.DrawLine(penDash, plotRect.Left, ya, plotRect.Right, ya);
                    g.DrawLine(penDash, plotRect.Left, yb, plotRect.Right, yb);

                    // Vertical double arrow just to the right of the label
                    if (yb - ya > 8f)
                    {
                        float ax = lx + sz.Width + 6f;
                        using var penArrow = new Pen(col, 1.5f);
                        var cap = new System.Drawing.Drawing2D.AdjustableArrowCap(4f, 5f, true);
                        penArrow.CustomStartCap = cap;
                        penArrow.CustomEndCap   = cap;
                        g.DrawLine(penArrow, ax, ya, ax, yb);
                    }
                }
            }
        }

        // ── Convert dB value → pixel Y in the plot rect ─────────────────────
        private float? DbToPlotY(double dbVal, bool usesY2, RectangleF plotRect)
        {
            if (chartSpectrum == null || chartSpectrum.ChartAreas.Count == 0) return null;
            var area = chartSpectrum.ChartAreas[0];
            var axis = usesY2 ? area.AxisY2 : area.AxisY;
            if (axis == null || axis.Maximum <= axis.Minimum) return null;
            double min = axis.Minimum, max = axis.Maximum;
            // Clamp to visible range
            if (dbVal < min || dbVal > max) return null;
            // Linear Y axis: top=max, bottom=min
            float frac = (float)((max - dbVal) / (max - min));
            return plotRect.Top + frac * plotRect.Height;
        }

        // ── Delta label drags ────────────────────────────────────────────────
        internal bool TryStartDeltaLabelDrag(MouseEventArgs e, RectangleF plotRect)
        {
            if (!_cursorLabelOn || e.Button != MouseButtons.Left) return false;
            if (_dxLblRect.Contains(e.Location))
            {
                _dxDragging = true;
                _dxDragStartX     = e.X;
                _dxDragStartFrac  = _dxLblFracX;
                _dxDragStartY     = e.Y;
                _dxDragStartFracY = _dxLblFracY;
                return true;
            }
            if (_dyLblRect.Contains(e.Location))
            {
                _dyDragging = true;
                _dyDragStartY     = e.Y;
                _dyDragStartFrac  = _dyLblFracY;
                _dyDragStartX     = e.X;
                _dyDragStartFracX = _dyLblFracX;
                return true;
            }
            return false;
        }

        internal void UpdateDeltaLabelDrag(MouseEventArgs e, RectangleF plotRect)
        {
            if (_dxDragging && plotRect.Width > 0)
            {
                float dx = e.X - _dxDragStartX;
                float dy = e.Y - _dxDragStartY;
                _dxLblFracX = Math.Clamp(_dxDragStartFrac  + dx / plotRect.Width,  0f, 1f);
                _dxLblFracY = Math.Clamp(_dxDragStartFracY + dy / plotRect.Height, 0f, 1f);
                chartSpectrum?.Invalidate();
            }
            else if (_dyDragging && plotRect.Height > 0)
            {
                float dy = e.Y - _dyDragStartY;
                float dx = e.X - _dyDragStartX;
                _dyLblFracY = Math.Clamp(_dyDragStartFrac  + dy / plotRect.Height, 0f, 1f);
                _dyLblFracX = Math.Clamp(_dyDragStartFracX + dx / plotRect.Width,  0f, 1f);
                chartSpectrum?.Invalidate();
            }
        }

        internal void StopDeltaLabelDrag()
        {
            _dxDragging = false;
            _dyDragging = false;
        }

        // ── Converts a frequency (Hz) to an X pixel in the plot rect ─────────
        private float? FreqToPlotX(double hz, RectangleF plotRect)
        {
            if (chartSpectrum == null || chartSpectrum.ChartAreas.Count == 0) return null;
            var ax = chartSpectrum.ChartAreas[0].AxisX;
            double min = ax.Minimum, max = ax.Maximum;
            if (max <= min || hz < min || hz > max) return null;

            float frac;
            if (ax.IsLogarithmic && min > 0 && max > 0)
                frac = (float)((Math.Log10(hz) - Math.Log10(min)) / (Math.Log10(max) - Math.Log10(min)));
            else
                frac = (float)((hz - min) / (max - min));

            return plotRect.Left + frac * plotRect.Width;
        }
    }
}
