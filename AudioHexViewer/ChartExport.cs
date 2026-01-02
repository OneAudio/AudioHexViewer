using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms.DataVisualization.Charting;

namespace AudioHexViewer
{
    internal static class ChartExport
    {
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

        public static void SavePngRendered(Chart chartToRender, string filePath, int dpi = 300)
        {
            // Rend le chart dans un PNG
            using var ms = new MemoryStream();
            chartToRender.SaveImage(ms, ChartImageFormat.Png);
            ms.Position = 0;

            // Ré-écrit en bitmap pour forcer DPI + éviter surprises
            using var bmp = new Bitmap(ms);
            bmp.SetResolution(dpi, dpi);
            bmp.Save(filePath, ImageFormat.Png);
        }
    }
}
