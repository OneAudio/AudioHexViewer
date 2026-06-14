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
            btnX1.Click        += (_, _) => { CursorLeftEnabled  = !CursorLeftEnabled;  SyncCursorXUi(); };
            btnX2.Click        += (_, _) => { CursorRightEnabled = !CursorRightEnabled; SyncCursorXUi(); };

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
            menu.Items.Add("Prev Peak", null, (_, _) => MoveCursorToAdjacentPeak(isX1, next: false));
            menu.Items.Add("Next Peak", null, (_, _) => MoveCursorToAdjacentPeak(isX1, next: true));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Threshold Settings...", null, (_, _) => OpenNoiseToPeakThresholdDialog());

            var btn = (Button)sender!;
            menu.Show(btn, new Point(0, btn.Height));
        }

        // ── Display button visuals ───────────────────────────────────────────
        private void UpdateDispButtons()
        {
            if (btnDispGraph == null || btnDispLabel == null) return;

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

        // ── Floating cursor box: draw ────────────────────────────────────────
        // Called from PostPaint when _cursorDispMode == 1 (Graph) or 2 (Both).
        internal void DrawCursorFloatBox(Graphics g, RectangleF plotRect)
        {
            if (_cursorDispMode != 1 && _cursorDispMode != 2) return;
            bool anyActive = CursorLeftEnabled || CursorRightEnabled;
            if (!anyActive) return;

            using var font     = new Font("Segoe UI", 8f);
            using var fontBold = new Font("Segoe UI", 8f, FontStyle.Bold);
            using var brBg     = new SolidBrush(Color.FromArgb(210, BackColor));
            using var penBd    = new Pen(Color.FromArgb(160, Color.Gray), 1f);
            using var brTxt    = new SolidBrush(Color.DimGray);
            using var fmtR     = new StringFormat { Alignment = StringAlignment.Far };

            var lines = BuildCursorFloatLines();
            if (lines.Count == 0) return;

            float lineH = font.GetHeight(g) + 3f;
            float maxLblW = 0f, maxValW = 0f;
            foreach (var (l, v) in lines)
            {
                float lw = g.MeasureString(l, fontBold).Width;
                float vw = g.MeasureString(v, font).Width;
                if (lw > maxLblW) maxLblW = lw;
                if (vw > maxValW) maxValW = vw;
            }

            float pad = 6f, gap = 10f;
            float boxW = pad * 2f + maxLblW + gap + maxValW;
            float boxH = pad * 2f + lines.Count * lineH;

            float bx = Math.Clamp(plotRect.Left + _cursorBoxOffX, plotRect.Left, Math.Max(plotRect.Left, plotRect.Right - boxW));
            float by = Math.Clamp(plotRect.Top  + _cursorBoxOffY, plotRect.Top,  Math.Max(plotRect.Top,  plotRect.Bottom - boxH));
            _cursorBoxOffX = bx - plotRect.Left;
            _cursorBoxOffY = by - plotRect.Top;
            _cursorBoxRect = new RectangleF(bx, by, boxW, boxH);

            g.FillRectangle(brBg, _cursorBoxRect);
            g.DrawRectangle(penBd, bx, by, boxW, boxH);

            float ty = by + pad;
            foreach (var (l, v) in lines)
            {
                g.DrawString(l, fontBold, brTxt, bx + pad, ty);
                g.DrawString(v, font, brTxt, new RectangleF(bx + pad + maxLblW + gap, ty, maxValW, lineH), fmtR);
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

            if (c1On)
            {
                lines.Add(("X1", FmtHz(_cursorMarkLFreq)));
                lines.Add((_cursorMarkLUsesY2 ? "φ" : "Gain", _cursorMarkLUsesY2 ? FmtDeg(_cursorMarkLDb) : FmtDb(_cursorMarkLDb)));
            }
            if (c2On)
            {
                lines.Add(("X2", FmtHz(_cursorMarkRFreq)));
                lines.Add((_cursorMarkRUsesY2 ? "φ" : "Gain", _cursorMarkRUsesY2 ? FmtDeg(_cursorMarkRDb) : FmtDb(_cursorMarkRDb)));
            }
            if (c1On && c2On)
            {
                double df = Math.Abs(_cursorMarkRFreq - _cursorMarkLFreq);
                lines.Add(("ΔF", FmtHz(df)));
                if (_cursorMarkLUsesY2 == _cursorMarkRUsesY2)
                {
                    double dy = _cursorMarkRDb - _cursorMarkLDb;
                    lines.Add((_cursorMarkLUsesY2 ? "Δφ" : "ΔGain", _cursorMarkLUsesY2 ? FmtDeg(dy) : FmtDb(dy)));
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

        // ── Delta label arrow (Label button) ────────────────────────────────
        // Draws a horizontal double-arrow between X1 and X2 cursor lines in the chart.
        internal void DrawCursorDeltaLabel(Graphics g, RectangleF plotRect)
        {
            if (!_cursorLabelOn) return;

            bool c1On = CursorLeftEnabled  && _cursorMarkLVisible && !double.IsNaN(_cursorMarkLFreq);
            bool c2On = CursorRightEnabled && _cursorMarkRVisible && !double.IsNaN(_cursorMarkRFreq);
            if (!c1On || !c2On) return;

            // Find X pixel positions of X1/X2 cursor lines
            float? px1 = FreqToPlotX(_cursorMarkLFreq, plotRect);
            float? px2 = FreqToPlotX(_cursorMarkRFreq, plotRect);
            if (px1 == null || px2 == null) return;

            float x1 = Math.Min(px1.Value, px2.Value);
            float x2 = Math.Max(px1.Value, px2.Value);

            // Arrow y — stored relative offset, draggable (clamped inside plot)
            float arrowY = Math.Clamp(plotRect.Top + _deltaLabelOffY, plotRect.Top + 4f, plotRect.Bottom - 20f);

            using var pen = new Pen(Color.DimGray, 1.5f);
            pen.StartCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;
            pen.EndCap   = System.Drawing.Drawing2D.LineCap.ArrowAnchor;

            // Horizontal double arrow
            g.DrawLine(pen, x1, arrowY, x2, arrowY);

            // Delta text centred above arrow
            double df = Math.Abs(_cursorMarkRFreq - _cursorMarkLFreq);
            string label = df >= 1000.0 ? $"ΔF = {df / 1000.0:0.###} kHz" : $"ΔF = {df:0.##} Hz";
            if (_cursorMarkLUsesY2 == _cursorMarkRUsesY2)
            {
                double dy = _cursorMarkRDb - _cursorMarkLDb;
                string unit = _cursorMarkLUsesY2 ? "°" : " dB";
                label += $"  Δ = {dy:0.##}{unit}";
            }

            using var font = new Font("Segoe UI", 8f);
            using var br   = new SolidBrush(Color.DimGray);
            SizeF sz = g.MeasureString(label, font);
            float tx = (x1 + x2) / 2f - sz.Width / 2f;
            float ty = arrowY - sz.Height - 2f;
            tx = Math.Clamp(tx, plotRect.Left, plotRect.Right - sz.Width);
            ty = Math.Clamp(ty, plotRect.Top, plotRect.Bottom - sz.Height);
            g.DrawString(label, font, br, tx, ty);

            // Drag target: arrow + text area
            _deltaLabelRect = new RectangleF(Math.Min(tx, x1), ty, Math.Max(sz.Width, x2 - x1), sz.Height + 6f);
        }

        // Delta label drag state
        private float     _deltaLabelOffY = 50f;
        private RectangleF _deltaLabelRect = RectangleF.Empty;
        private bool      _deltaLabelDragging = false;
        private float     _deltaLabelDragStartY;
        private float     _deltaLabelDragStartOffY;

        // Called from chart MouseDown to initiate delta label drag
        internal bool TryStartDeltaLabelDrag(MouseEventArgs e, RectangleF plotRect)
        {
            if (!_cursorLabelOn || e.Button != MouseButtons.Left) return false;
            if (!_deltaLabelRect.Contains(e.Location)) return false;
            _deltaLabelDragging = true;
            _deltaLabelDragStartY    = e.Y;
            _deltaLabelDragStartOffY = _deltaLabelOffY;
            return true;
        }
        internal void UpdateDeltaLabelDrag(MouseEventArgs e, RectangleF plotRect)
        {
            if (!_deltaLabelDragging) return;
            float delta = e.Y - _deltaLabelDragStartY;
            _deltaLabelOffY = Math.Clamp(_deltaLabelDragStartOffY + delta, 4f, Math.Max(4f, plotRect.Height - 20f));
            chartSpectrum?.Invalidate();
        }
        internal void StopDeltaLabelDrag() => _deltaLabelDragging = false;

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
