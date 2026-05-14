using System;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;

namespace AudioHexViewer
{
public sealed class SpectrumColorSettings
{
    // Stockage ARGB (Color.ToArgb()) pour sérialisation JSON simple

    // --- FFT ---
    public int FftLeftArgb { get; set; } = Color.DodgerBlue.ToArgb();
    public int FftRightArgb { get; set; } = Color.OrangeRed.ToArgb();
    public int FftWidth { get; set; } = 2;                 // 1..3

    // --- PeakHold ---
    public int PeakHoldLeftArgb { get; set; } = Color.DodgerBlue.ToArgb();
    public int PeakHoldRightArgb { get; set; } = Color.OrangeRed.ToArgb();
    public int PeakHoldWidth { get; set; } = 1;             // 1..3

    // --- Overlays ---
    // Ancien réglage (compat): couleur unique pour tous les overlays
    public int OverlayArgb { get; set; } = Color.Gray.ToArgb();

    // Nouveau: une couleur par slot (Overlay1=Slot1, ... Overlay8=Slot8)
    public int[] OverlaySlotArgb { get; set; } = DefaultOverlayRainbowArgb();
    public int OverlayWidth { get; set; } = 1;              // 1..3
    
    // Style de ligne des overlays (ChartDashStyle)
    public int OverlayDashStyle { get; set; } = (int)ChartDashStyle.Dot;

    // --- Chart (fond / grilles / labels) ---
    // Grille Major (compat: ancien GridArgb)
    public int GridArgb { get; set; } = Color.LightGray.ToArgb();

    // Nouveau: Grille Minor
    public int GridMinorArgb { get; set; } = Color.Gainsboro.ToArgb();

    public int BackgroundArgb { get; set; } = Color.White.ToArgb();

    // Labels (Major/Minor). Axis line + ticks = LabelMajor
    public int LabelMajorArgb { get; set; } = Color.Black.ToArgb();
    public int LabelMinorArgb { get; set; } = Color.DimGray.ToArgb();
    public int InfoTextArgb { get; set; } = Color.DarkBlue.ToArgb();

    public static SpectrumColorSettings Defaults()
    {
        var s = new SpectrumColorSettings();
        s.Normalize();
        return s;
    }

    /// <summary>
    /// Normalise les valeurs (taille overlays, bornes épaisseur).
    /// Appeler après chargement JSON pour garantir une config cohérente.
    /// </summary>
    public void Normalize()
    {
        // Overlay array
        if (OverlaySlotArgb == null || OverlaySlotArgb.Length != 8)
            OverlaySlotArgb = DefaultOverlayRainbowArgb();

        // Clamp widths
        FftWidth = Clamp(FftWidth, 1, 3);
        PeakHoldWidth = Clamp(PeakHoldWidth, 1, 3);
        OverlayWidth = Clamp(OverlayWidth, 1, 3);

        // If someone is upgrading from older settings and explicitly set OverlayArgb,
        // and OverlaySlotArgb is still default, they can later customize per-slot.
        // (On ne force pas OverlaySlotArgb à OverlayArgb pour ne pas écraser le rainbow par défaut.)
    }

    private static int Clamp(int v, int min, int max)
        => v < min ? min : (v > max ? max : v);

    private static int[] DefaultOverlayRainbowArgb()
    {
        // Palette arc-en-ciel (lisible sur fond clair/sombre).
        // Overlay1..Overlay8
        Color[] colors = new[]
        {
            Color.Red,
            Color.Orange,
            Color.Goldenrod,
            Color.LimeGreen,
            Color.Cyan,
            Color.DodgerBlue,
            Color.BlueViolet,
            Color.Magenta
        };

        var arr = new int[8];
        for (int i = 0; i < 8; i++)
            arr[i] = colors[i].ToArgb();
        return arr;
    }
}
}
