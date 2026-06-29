// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace AudioHexViewer;

/// <summary>
/// Draws the subtle "AHV" watermark in the top-right corner of a graph area.
/// Calibri Bold 25 pt (1/3 of the splash-screen 75 pt).
///
/// Opacity is simulated by pre-blending black over white into a solid gray,
/// which is compatible with every GDI+ surface (panel, Chart PostPaint, etc.)
/// without requiring alpha-blending support on the destination.
///
/// Adjust the <see cref="Opacity"/> constant to tune visibility:
///   0.0  = invisible   |   0.18 = subtle (default)   |   1.0 = solid black
/// </summary>
internal static class GraphWatermark
{
    private const string Text       = "AHV";
    private const float  FontSizePt = 25f;    // 1/3 of splash-screen 75 pt
    private const int    Margin     = 8;      // pixels from inner-plot edge
    public  const float  Opacity    = 0.2f;  // ← adjust here to taste

    public static void Draw(Graphics g, RectangleF plotRect)
    {
        if (plotRect.Width < 80 || plotRect.Height < 40) return;

        using var font = new Font("Calibri", FontSizePt, FontStyle.Bold, GraphicsUnit.Point);

        SizeF sz = g.MeasureString(Text, font);
        float x  = plotRect.Right - sz.Width - Margin;
        float y  = plotRect.Top   + Margin;

        var saved = g.Save();
        try
        {
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            // Pre-blend: "black text at Opacity% over white background"
            // → solid gray = 255 × (1 − Opacity)
            // This requires zero alpha-blending support from the destination surface.
            int gray = (int)(255f * (1f - Opacity));
            using var brush = new SolidBrush(Color.FromArgb(gray, gray, gray));
            g.DrawString(Text, font, brush, x, y);
        }
        finally
        {
            g.Restore(saved);
        }
    }
}
