using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms.DataVisualization.Charting;

namespace AudioHexViewer
{
    // ══════════════════════════════════════════════════════════════════════════
    //  ChartExport — hub centralisé pour tous les exports PNG/PDF
    //
    //  Utilisé par : SpectrumControl, HistogramControl, OscilloscopeControl
    //
    //  Workflow standard pour un Chart WinForms :
    //    1. CloneChart()              — clone serialisé (pas de modification du live)
    //    2. ApplyExportScaling()      — agrandit les fonts × scale
    //    3. ApplyChartHeaderMargin()  — pousse la ChartArea vers le bas pour faire
    //                                   de la place au titre
    //    4. SavePngRendered()         — rend le clone dans un PNG 300 dpi
    //    5. DrawHeaderOnPng()         — compose le titre en GDI+ pur par-dessus
    //                                   (plus robuste que PostPaint)
    //
    //  Workflow pour l'OscilloscopeControl (rendu GDI+ pur, pas de Chart) :
    //    1. CaptureControlBitmap()    — DrawToBitmap sur le panel de rendu
    //    2. ComposeWithHeader()       — ajoute le bandeau titre
    //    3. SaveBitmap()
    // ══════════════════════════════════════════════════════════════════════════
    internal static class ChartExport
    {
        // ── 1. Clone Chart ────────────────────────────────────────────────────
        public static Chart CloneChart(Chart source)
        {
            var clone = new Chart();
            using var ms = new MemoryStream();
            source.Serializer.Content = SerializationContents.All;
            source.Serializer.Save(ms);
            ms.Position = 0;
            clone.Serializer.Content = SerializationContents.All;
            clone.Serializer.Load(ms);
            return clone;
        }

        // ── 2. Scale fonts × scale ────────────────────────────────────────────
        public static void ApplyExportScaling(Chart c, int scale)
        {
            if (scale <= 1) return;
            c.Font = ScaleFont(c.Font, scale);
            foreach (var area in c.ChartAreas)
            {
                if (area.AxisX.LabelStyle.Font  != null) area.AxisX.LabelStyle.Font  = ScaleFont(area.AxisX.LabelStyle.Font,  scale);
                if (area.AxisY.LabelStyle.Font  != null) area.AxisY.LabelStyle.Font  = ScaleFont(area.AxisY.LabelStyle.Font,  scale);
                if (area.AxisX2.LabelStyle.Font != null) area.AxisX2.LabelStyle.Font = ScaleFont(area.AxisX2.LabelStyle.Font, scale);
                if (area.AxisY2.LabelStyle.Font != null) area.AxisY2.LabelStyle.Font = ScaleFont(area.AxisY2.LabelStyle.Font, scale);
                if (area.AxisX.TitleFont  != null) area.AxisX.TitleFont  = ScaleFont(area.AxisX.TitleFont,  scale);
                if (area.AxisY.TitleFont  != null) area.AxisY.TitleFont  = ScaleFont(area.AxisY.TitleFont,  scale);
                if (area.AxisX2.TitleFont != null) area.AxisX2.TitleFont = ScaleFont(area.AxisX2.TitleFont, scale);
                if (area.AxisY2.TitleFont != null) area.AxisY2.TitleFont = ScaleFont(area.AxisY2.TitleFont, scale);
            }
            foreach (var leg in c.Legends) if (leg.Font != null) leg.Font = ScaleFont(leg.Font, scale);
            foreach (var t   in c.Titles)  if (t.Font   != null) t.Font   = ScaleFont(t.Font,   scale);
        }

        // ── 3. Push ChartArea down to make room for the header ────────────────
        //  areaName : "SpectrumArea" ou "HistogramArea"
        public static void ApplyChartHeaderMargin(Chart chart, string areaName, int headerPx)
        {
            var area = chart.ChartAreas.FindByName(areaName) ?? chart.ChartAreas[0];
            int h = chart.ClientSize.Height;
            if (h <= 0) return;
            float pct = 100f * headerPx / h;

            float x  = (area.Position.Auto || area.Position.Width  < 1) ? 3f  : area.Position.X;
            float ay = (area.Position.Auto || area.Position.Height < 1) ? 3f  : area.Position.Y;
            float w  = (area.Position.Auto || area.Position.Width  < 1) ? 94f : area.Position.Width;
            float ah = (area.Position.Auto || area.Position.Height < 1) ? 90f : area.Position.Height;

            area.Position.Auto = false;
            float ny = ay + pct;
            float nh = Math.Max(5f, ah - pct);
            if (ny > 95f) ny = 95f;
            if (ny + nh > 99f) nh = Math.Max(5f, 99f - ny);
            area.Position = new ElementPosition(x, ny, w, nh);
        }

        // ── 4. Render Chart clone → PNG bytes in memory ───────────────────────
        public static byte[] RenderChartToPngBytes(Chart chart)
        {
            using var ms = new MemoryStream();
            chart.SaveImage(ms, ChartImageFormat.Png);
            return ms.ToArray();
        }

        // ── 5. Draw header title on top of a Bitmap (GDI+ pur) ───────────────
        //  headerPx  : hauteur en pixels réservée en haut du bitmap
        //  title     : texte multi-lignes (\n ou \r\n)
        //  bmp       : bitmap à modifier sur place
        public static void DrawHeaderOnBitmap(Bitmap bmp, int headerPx, string title, int scale = 1)
        {
            if (headerPx <= 0 || string.IsNullOrWhiteSpace(title)) return;

            var lines = title
                .Replace("\r\n", "\n").Replace('\r', '\n')
                .Split('\n');

            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode   = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            // Fond blanc dans la zone header (au cas où le chart a une couleur)
            g.FillRectangle(Brushes.White, 0, 0, bmp.Width, headerPx);

            // Ligne de séparation fine entre header et graphique
            using var sepPen = new Pen(Color.LightGray, Math.Max(1, scale));
            g.DrawLine(sepPen, 0, headerPx - 1, bmp.Width, headerPx - 1);

            float basePt = 11f * scale;
            using var font   = new Font("Segoe UI", basePt, FontStyle.Bold, GraphicsUnit.Point);
            using var brush  = new SolidBrush(Color.FromArgb(30, 30, 30));
            using var fmt    = new StringFormat
            {
                Alignment     = StringAlignment.Center,
                LineAlignment = StringAlignment.Near,
                Trimming      = StringTrimming.EllipsisCharacter
            };

            float lineH    = font.GetHeight(g);
            float totalH   = lineH * lines.Length;
            float yStart   = (headerPx - totalH) / 2f;
            if (yStart < 4f) yStart = 4f;

            var rect = new RectangleF(0, yStart, bmp.Width, lineH + 2);
            foreach (var line in lines)
            {
                g.DrawString(line.Trim(), font, brush, rect, fmt);
                rect.Y += lineH;
            }
        }

        // ── 6. Save PNG Bitmap → file (force DPI) ────────────────────────────
        public static void SaveBitmap(Bitmap bmp, string filePath, int dpi = 300)
        {
            bmp.SetResolution(dpi, dpi);
            bmp.Save(filePath, ImageFormat.Png);
        }

        // ── 7. Full pipeline : PNG bytes + header → file ─────────────────────
        //  Utilisé par SpectrumControl et HistogramControl
        public static void SaveChartPng(
            byte[]  pngBytes,
            string  filePath,
            int     headerPx,
            string? headerTitle,
            int     scale  = 2,
            int     dpi    = 300)
        {
            using var ms  = new MemoryStream(pngBytes);
            using var src = new Bitmap(ms);

            if (headerPx > 0 && !string.IsNullOrWhiteSpace(headerTitle))
            {
                // Crée un bitmap plus grand avec le bandeau
                using var full = new Bitmap(src.Width, src.Height + headerPx);
                using (var g = Graphics.FromImage(full))
                    g.DrawImage(src, 0, headerPx);

                DrawHeaderOnBitmap(full, headerPx, headerTitle!, scale);
                SaveBitmap(full, filePath, dpi);
            }
            else
            {
                // Pas de header : save direct
                using var bmp = new Bitmap(src);
                SaveBitmap(bmp, filePath, dpi);
            }
        }

        // ── 8. Capture un Control GDI+ en Bitmap (pour l'Oscilloscope) ───────
        public static Bitmap CaptureControl(System.Windows.Forms.Control control, int scale = 1)
        {
            int w = control.Width  * scale;
            int h = control.Height * scale;
            var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);

            if (scale == 1)
            {
                // Rendu direct
                control.DrawToBitmap(bmp, new Rectangle(0, 0, w, h));
            }
            else
            {
                // Capture à résolution native, puis upscale haute qualité
                using var native = new Bitmap(control.Width, control.Height, PixelFormat.Format32bppArgb);
                control.DrawToBitmap(native, new Rectangle(0, 0, control.Width, control.Height));

                using var g = Graphics.FromImage(bmp);
                g.InterpolationMode  = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode      = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode    = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.DrawImage(native, 0, 0, w, h);
            }

            return bmp;
        }

        // ── 9. Copy active graph to clipboard (screen resolution, no scale) ──
        //  Pour Chart WinForms : clone + SaveImage en mémoire → Bitmap → Clipboard
        //  Pour Control GDI+ (Oscilloscope) : DrawToBitmap → Clipboard
        public static void CopyChartToClipboard(
            System.Windows.Forms.DataVisualization.Charting.Chart chart)
        {
            using var ms  = new MemoryStream();
            chart.SaveImage(ms, System.Windows.Forms.DataVisualization.Charting.ChartImageFormat.Png);
            ms.Position = 0;
            using var bmp = new Bitmap(ms);
            Clipboard.SetImage(new Bitmap(bmp));   // copie défensive (détache du MemoryStream)
        }

        public static void CopyControlToClipboard(System.Windows.Forms.Control control)
        {
            using var bmp = new Bitmap(control.Width, control.Height, PixelFormat.Format32bppArgb);
            control.DrawToBitmap(bmp, new Rectangle(0, 0, control.Width, control.Height));
            Clipboard.SetImage(new Bitmap(bmp));
        }

        // ── Helper ────────────────────────────────────────────────────────────
        public static Font ScaleFont(Font f, int scale)
            => new Font(f.FontFamily, f.SizeInPoints * scale, f.Style, GraphicsUnit.Point);

        // ── Calcul headerPx : 20px par ligne + 10px de marge, minimum 50px ────
        public static int HeaderPixels(string? title, int scale)
        {
            if (string.IsNullOrWhiteSpace(title)) return 0;
            int lines = 1 + title!.Replace("\r\n", "\n").Count(c => c == '\n');
            int px    = Math.Max(50, lines * 20 + 10);
            return px * scale;
        }
    }
}
