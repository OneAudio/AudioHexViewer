using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;

namespace AudioHexViewer
{
    internal static class PdfExport
    {
        // A4 paysage en points PDF (1 pt = 1/72 inch)
        private const float A4W_Landscape = 842f;
        private const float A4H_Landscape = 595f;

        /// <summary>
        /// Crée un PDF A4 paysage avec le PNG "fit" sur la largeur (et auto-réduit si trop haut).
        /// </summary>
        public static void SaveA4LandscapeFromPng(string pngPath, string pdfPath)
        {
            using var src = Image.FromFile(pngPath);

            // Convertit en JPEG (PDF simple + compatible partout)
            byte[] jpg = EncodeAsJpeg24bpp(src, quality: 92L, out int pxW, out int pxH);

            // Calcul fit largeur (fallback fit hauteur si nécessaire pour éviter clipping)
            float scaleW = A4W_Landscape / pxW;
            float drawW = A4W_Landscape;
            float drawH = pxH * scaleW;

            if (drawH > A4H_Landscape)
            {
                float scaleH = A4H_Landscape / pxH;
                drawH = A4H_Landscape;
                drawW = pxW * scaleH;
            }

            float x = (A4W_Landscape - drawW) / 2f;
            float y = (A4H_Landscape - drawH) / 2f;

            // Content stream PDF : place l'image via une matrice cm
            string content =
                "q\n" +
                F(drawW) + " 0 0 " + F(drawH) + " " + F(x) + " " + F(y) + " cm\n" +
                "/Im0 Do\n" +
                "Q\n";

            byte[] contentBytes = System.Text.Encoding.ASCII.GetBytes(content);

            // Écriture PDF minimal : Catalog, Pages, Page, XObject image, Content
            using var ms = new MemoryStream();
            void W(string s) => ms.Write(System.Text.Encoding.ASCII.GetBytes(s));

            var offsets = new List<long> { 0 }; // obj 0 (free)

            void WriteObj(int objNum, string dict, byte[]? stream = null)
            {
                offsets.Add(ms.Position);
                W($"{objNum} 0 obj\n{dict}\n");
                if (stream != null)
                {
                    W("stream\n");
                    ms.Write(stream, 0, stream.Length);
                    W("\nendstream\n");
                }
                W("endobj\n");
            }

            W("%PDF-1.4\n%\u00E2\u00E3\u00CF\u00D3\n");

            // 1: Catalog
            WriteObj(1, "<< /Type /Catalog /Pages 2 0 R >>");

            // 2: Pages
            WriteObj(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");

            // 3: Page (A4 paysage)
            WriteObj(3,
                "<< /Type /Page /Parent 2 0 R " +
                "/MediaBox [0 0 " + F(A4W_Landscape) + " " + F(A4H_Landscape) + "] " +
                "/Resources << /XObject << /Im0 4 0 R >> >> " +
                "/Contents 5 0 R >>");

            // 4: Image XObject (JPEG)
            string imgDict =
                "<< /Type /XObject /Subtype /Image " +
                "/Width " + pxW + " /Height " + pxH + " " +
                "/ColorSpace /DeviceRGB /BitsPerComponent 8 " +
                "/Filter /DCTDecode /Length " + jpg.Length + " >>";
            WriteObj(4, imgDict, jpg);

            // 5: Content stream
            string contentDict = "<< /Length " + contentBytes.Length + " >>";
            WriteObj(5, contentDict, contentBytes);

            // xref
            long xrefPos = ms.Position;
            W($"xref\n0 {offsets.Count}\n");
            W("0000000000 65535 f \n");
            for (int i = 1; i < offsets.Count; i++)
            {
                W(offsets[i].ToString("0000000000", CultureInfo.InvariantCulture));
                W(" 00000 n \n");
            }

            // trailer
            W("trailer\n<< /Size ");
            W(offsets.Count.ToString(CultureInfo.InvariantCulture));
            W(" /Root 1 0 R >>\n");
            W("startxref\n");
            W(xrefPos.ToString(CultureInfo.InvariantCulture));
            W("\n%%EOF\n");

            File.WriteAllBytes(pdfPath, ms.ToArray());
        }

        private static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        private static byte[] EncodeAsJpeg24bpp(Image src, long quality, out int w, out int h)
        {
            w = src.Width;
            h = src.Height;

            using var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White); // si PNG avec alpha
                g.DrawImage(src, 0, 0, w, h);
            }

            using var outMs = new MemoryStream();
            var enc = GetJpegEncoder();
            using var encParams = new EncoderParameters(1);
            encParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);
            bmp.Save(outMs, enc, encParams);
            return outMs.ToArray();
        }

        private static ImageCodecInfo GetJpegEncoder()
        {
            foreach (var c in ImageCodecInfo.GetImageEncoders())
                if (c.FormatID == ImageFormat.Jpeg.Guid)
                    return c;
            throw new InvalidOperationException("JPEG encoder introuvable.");
        }
    }
}
