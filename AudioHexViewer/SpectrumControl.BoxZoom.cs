// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace AudioHexViewer
{
    /// <summary>
    /// Rubber-band "box zoom" on the spectrum chart.
    /// <para>
    /// Left-press + drag in the plot area draws a selection rectangle; on release the
    /// X (frequency) and Y (level) axes are zoomed to that rectangle, snapped OUTWARD
    /// to the nearest grid lines so the axis bounds stay on round values. The view is
    /// applied through the existing custom* override fields (the same path used by the
    /// AXIS dialog), so no averaging is reset — a zoom is a view change, not a data change.
    /// </para>
    /// <para>
    /// Left double-click steps back one zoom level (multi-level undo). When there is no
    /// zoom to undo, the existing double-click behaviour (cursor-to-peak snap) applies.
    /// </para>
    /// <para>Linear X only for now; log-X snapping (to decades) is a follow-up.</para>
    /// </summary>
    public partial class SpectrumControl
    {
        // ── Drag state ──────────────────────────────────────────────────────
        private bool  _zoomDragging;
        private Point _zoomStartPx;
        private Point _zoomCurPx;

        // Below this drag size (in pixels, either axis) the gesture is treated as a click.
        private const int ZoomMinDragPx = 8;

        // View history for multi-level undo (one entry pushed per applied zoom).
        private readonly Stack<(double? fMin, double? fMax, double? yMin, double? yMax)> _zoomHistory = new();

        /// <summary>
        /// Wire the box-zoom handlers. MUST be called AFTER InitCursors() so that the
        /// cursor-box and delta-label MouseDown handlers run first and we can yield to them.
        /// </summary>
        private void WireBoxZoom()
        {
            if (chartSpectrum == null) return;
            chartSpectrum.MouseDown += BoxZoom_MouseDown;
            chartSpectrum.MouseMove += BoxZoom_MouseMove;
            chartSpectrum.MouseUp   += BoxZoom_MouseUp;
            chartSpectrum.PostPaint += BoxZoom_PostPaint; // draws the rubber-band on top
        }

        // True when another left-drag interaction already owns the mouse.
        private bool BoxZoomBusyElsewhere()
            => _cursorBoxDragging || _dxDragging || _dyDragging;

        private void BoxZoom_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (_zoomDragging)                 return;
            if (BoxZoomBusyElsewhere())         return; // cursors / Δ labels have priority
            if (chartSpectrum == null || chartSpectrum.ChartAreas.Count == 0) return;

            var plot = GetPlotRectPixels(chartSpectrum, chartSpectrum.ChartAreas[0]);
            if (plot.IsEmpty || !plot.Contains(e.Location)) return;

            _zoomDragging = true;
            _zoomStartPx  = e.Location;
            _zoomCurPx    = e.Location;
        }

        private void BoxZoom_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!_zoomDragging) return;
            _zoomCurPx = e.Location;
            chartSpectrum?.Invalidate(); // triggers BoxZoom_PostPaint to redraw the rectangle
        }

        private void BoxZoom_MouseUp(object? sender, MouseEventArgs e)
        {
            if (!_zoomDragging || e.Button != MouseButtons.Left) return;
            _zoomDragging = false;

            if (chartSpectrum == null || chartSpectrum.ChartAreas.Count == 0) return;
            var area = chartSpectrum.ChartAreas[0];
            var plot = GetPlotRectPixels(chartSpectrum, area);
            if (plot.IsEmpty) { chartSpectrum.Invalidate(); return; }

            // Too small → it was a click, not a zoom.
            if (Math.Abs(_zoomCurPx.X - _zoomStartPx.X) < ZoomMinDragPx ||
                Math.Abs(_zoomCurPx.Y - _zoomStartPx.Y) < ZoomMinDragPx)
            {
                chartSpectrum.Invalidate();
                return;
            }

            try
            {
                // Clamp both corners to the plot area.
                float x1 = Math.Clamp(_zoomStartPx.X, plot.Left, plot.Right);
                float x2 = Math.Clamp(_zoomCurPx.X,   plot.Left, plot.Right);
                float y1 = Math.Clamp(_zoomStartPx.Y, plot.Top,  plot.Bottom);
                float y2 = Math.Clamp(_zoomCurPx.Y,   plot.Top,  plot.Bottom);

                // Pixel -> value. X is mapped manually (mirrors FreqToPlotX): on a logarithmic
                // axis PixelPositionToValue returns log10(value), not the value, so we convert
                // ourselves. Y (always linear) is read directly.
                double fa = PixelToFreqHz(area, plot, x1);
                double fb = PixelToFreqHz(area, plot, x2);
                double la = area.AxisY.PixelPositionToValue(y1);
                double lb = area.AxisY.PixelPositionToValue(y2);

                double fMinRaw = Math.Min(fa, fb);
                double fMaxRaw = Math.Max(fa, fb);
                double yMinRaw = Math.Min(la, lb);
                double yMaxRaw = Math.Max(la, lb);

                int ph = Math.Max(50, (int)plot.Height);

                // Y: snap outward to the grid step the renderer would pick.
                var (yMin, yMax) = SnapY(yMinRaw, yMaxRaw, ph);

                // X: linear → snap to the CURRENT grid; log → raw bounds (decade snap is a follow-up).
                double fMin, fMax;
                if (chkLogX?.Checked == true)
                {
                    (fMin, fMax) = SnapXLog(fMinRaw, fMaxRaw);
                }
                else
                {
                    (fMin, fMax) = SnapXLinear(fMinRaw, fMaxRaw);
                }

                // Remember the current view, then apply the new one.
                PushZoomHistory();
                customFMin = fMin;
                customFMax = fMax;
                customYMin = yMin;
                customYMax = yMax;

                // View change only — do NOT call ResetAveragingState() here.
                UpdateSpectrumChartAndStats();
            }
            catch
            {
                // PixelPositionToValue can throw if the layout is not ready: ignore.
            }
            finally
            {
                chartSpectrum.Invalidate();
            }
        }

        // Snap a [min,max] level range outward to multiples of the nice Y step.
        private (double min, double max) SnapY(double yMinRaw, double yMaxRaw, int plotHeightPx)
        {
            double step = ChooseNiceYStep(Math.Max(1.0, yMaxRaw - yMinRaw), plotHeightPx);
            if (step <= 0) step = 1.0;
            double yMin = Math.Floor(yMinRaw / step) * step;
            double yMax = Math.Ceiling(yMaxRaw / step) * step;
            if (yMax <= yMin) yMax = yMin + step;
            return (yMin, yMax);
        }

        // Map an X pixel to a frequency (Hz), mirroring FreqToPlotX. Robust for linear AND
        // logarithmic axes (avoids MS Chart returning log10(value) on a log axis).
        private double PixelToFreqHz(ChartArea area, RectangleF plot, float px)
        {
            double min = area.AxisX.Minimum, max = area.AxisX.Maximum;
            if (plot.Width <= 0 || max <= min) return min;
            double frac = Math.Clamp((px - plot.Left) / plot.Width, 0.0, 1.0);
            if (area.AxisX.IsLogarithmic && min > 0 && max > 0)
                return Math.Pow(10.0, Math.Log10(min) + frac * (Math.Log10(max) - Math.Log10(min)));
            return min + frac * (max - min);
        }

        // Snap a [min,max] frequency range outward to the CURRENTLY DISPLAYED X grid
        // step (e.g. 2 kHz), so the zoom bounds — and the span — stay on round grid values.
        private (double min, double max) SnapXLinear(double fMinRaw, double fMaxRaw)
        {
            double step = _axisXMajorStepHz;      // current major grid step in Hz
            if (step < 1.0) step = 1000.0;
            double fMin = Math.Floor(fMinRaw / step) * step;
            double fMax = Math.Ceiling(fMaxRaw / step) * step;
            if (fMin < 0) fMin = 0;
            if (fMax <= fMin) fMax = fMin + step;
            return (fMin, fMax);
        }

        // Snap a [min,max] frequency range outward to the displayed log grid lines
        // (k x 10^d, k in 1..9): fMin to the subdivision below, fMax to the one above.
        private (double min, double max) SnapXLog(double fMinRaw, double fMaxRaw)
        {
            double lo = SnapLogDown(Math.Max(LogXAbsMinHz, fMinRaw));
            double hi = SnapLogUp(Math.Max(LogXAbsMinHz, fMaxRaw));
            if (hi <= lo) hi = lo * 10.0; // at least one decade
            return (lo, hi);
        }

        // Largest grid line (k x 10^d, k in 1..9) less than or equal to v.
        private static double SnapLogDown(double v)
        {
            double decade = Math.Pow(10.0, Math.Floor(Math.Log10(v)));
            double m = Math.Floor(v / decade + 1e-9); // mantissa 1..9 (epsilon guards exact lines)
            if (m < 1) m = 1;
            return m * decade;
        }

        // Smallest grid line (k x 10^d, k in 1..9) greater than or equal to v.
        // A mantissa above 9 rolls over to the next decade (10 x decade).
        private static double SnapLogUp(double v)
        {
            double decade = Math.Pow(10.0, Math.Floor(Math.Log10(v)));
            double m = Math.Ceiling(v / decade - 1e-9); // 1..10
            if (m < 1) m = 1;
            return m * decade;
        }

        private void PushZoomHistory()
            => _zoomHistory.Push((customFMin, customFMax, customYMin, customYMax));

        /// <summary>
        /// Restore the view that was active before the last box-zoom.
        /// Returns true if a level was popped (call first in ChartSpectrum_MouseDoubleClick).
        /// </summary>
        internal bool TryZoomUndo()
        {
            if (_zoomHistory.Count == 0) return false;
            var prev = _zoomHistory.Pop();
            customFMin = prev.fMin;
            customFMax = prev.fMax;
            customYMin = prev.yMin;
            customYMax = prev.yMax;
            UpdateSpectrumChartAndStats();
            return true;
        }

        private void BoxZoom_PostPaint(object? sender, ChartPaintEventArgs e)
        {
            if (!_zoomDragging) return;
            if (sender is not Chart chart || !ReferenceEquals(chart, chartSpectrum)) return; // skip export
            if (e.ChartElement is not ChartArea area || area.Name != "SpectrumArea") return;

            var plot = GetPlotRectPixels(chartSpectrum, chartSpectrum.ChartAreas[0]);
            if (plot.IsEmpty) return;

            float x = Math.Min(_zoomStartPx.X, _zoomCurPx.X);
            float y = Math.Min(_zoomStartPx.Y, _zoomCurPx.Y);
            float w = Math.Abs(_zoomCurPx.X - _zoomStartPx.X);
            float h = Math.Abs(_zoomCurPx.Y - _zoomStartPx.Y);

            var r = RectangleF.Intersect(new RectangleF(x, y, w, h), plot);
            if (r.Width < 1 || r.Height < 1) return;

            var g = e.ChartGraphics.Graphics;
            using var fill = new SolidBrush(Color.FromArgb(40, 120, 170, 255));
            using var pen  = new Pen(Color.FromArgb(200, 90, 150, 255), 1f) { DashStyle = DashStyle.Dash };
            g.FillRectangle(fill, r);
            g.DrawRectangle(pen, r.X, r.Y, r.Width, r.Height);
        }
    }
}
