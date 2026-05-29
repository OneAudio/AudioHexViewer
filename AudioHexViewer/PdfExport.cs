using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace AudioHexViewer
{
    // ══════════════════════════════════════════════════════════════════════════
    //  PdfExport — génère un PDF A4 paysage sans dépendance externe
    //
    //  Améliorations v2 vs v1 :
    //   • Image PNG embarquée en FlateDecode (zlib/deflate) au lieu de JPEG
    //     → pas de double compression, qualité texte parfaite
    //   • Métadonnées PDF : Title, Author, CreationDate, Producer
    //   • PDF 1.5 (au lieu de 1.4)
    //   • Structure objet plus lisible (objet Info séparé)
    // ══════════════════════════════════════════════════════════════════════════
    internal static class PdfExport
    {
        // A4 paysage en points PDF (1 pt = 1/72 inch)
        private const float PageW = 842f;
        private const float PageH = 595f;

        /// <summary>
        /// Crée un PDF A4 paysage depuis un PNG existant sur disque.
        /// L'image est intégrée en FlateDecode (PNG raw) — aucune recompression JPEG.
        /// </summary>
        public static void SaveA4LandscapeFromPng(string pngPath, string pdfPath,
                                                   string? docTitle = null)
        {
            // ── 1. Lecture et décodage du PNG source ──────────────────────────
            int pxW, pxH;
            byte[] rgbFlate;   // pixels RGB bruts compressés en zlib/deflate

            using (var src = Image.FromFile(pngPath))
            {
                pxW = src.Width;
                pxH = src.Height;
                rgbFlate = ExtractRgbFlate(src);
            }

            // ── 2. Calcul du placement centré, fit dans la page ───────────────
            float scaleW = PageW / pxW;
            float drawW  = PageW;
            float drawH  = pxH * scaleW;

            if (drawH > PageH)
            {
                float scaleH = PageH / pxH;
                drawH = PageH;
                drawW = pxW * scaleH;
            }

            // Marges de 6 pt pour éviter le bord
            const float margin = 6f;
            if (drawW > PageW - 2 * margin) drawW = PageW - 2 * margin;
            if (drawH > PageH - 2 * margin) drawH = PageH - 2 * margin;

            float x = (PageW - drawW) / 2f;
            float y = (PageH - drawH) / 2f;

            // ── 3. Assemblage PDF ─────────────────────────────────────────────
            string title    = docTitle ?? "AudioHexViewer Export";
            string dateStr  = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            string pdfDate  = $"D:{dateStr}+00'00'";

            using var ms = new MemoryStream();
            var offsets = new List<long> { 0 };   // obj 0 réservé (free list)

            void W(string s) => ms.Write(Encoding.Latin1.GetBytes(s));

            void WriteObj(int n, string dict, byte[]? stream = null)
            {
                offsets.Add(ms.Position);
                W($"{n} 0 obj\n{dict}\n");
                if (stream != null)
                {
                    W("stream\n");
                    ms.Write(stream);
                    W("\nendstream\n");
                }
                W("endobj\n");
            }

            // Header PDF
            W("%PDF-1.5\n");
            W("%\xE2\xE3\xCF\xD3\n");   // 4 octets > 127 pour indiquer fichier binaire

            // obj 1 — Catalog
            WriteObj(1, "<< /Type /Catalog /Pages 2 0 R >>");

            // obj 2 — Pages
            WriteObj(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");

            // obj 3 — Page A4 paysage
            WriteObj(3,
                "<< /Type /Page /Parent 2 0 R " +
                $"/MediaBox [0 0 {F(PageW)} {F(PageH)}] " +
                "/Resources << /XObject << /Im0 4 0 R >> >> " +
                "/Contents 5 0 R >>");

            // obj 4 — Image XObject (FlateDecode = PNG row-data deflated)
            string imgDict =
                "<< /Type /XObject /Subtype /Image " +
                $"/Width {pxW} /Height {pxH} " +
                "/ColorSpace /DeviceRGB /BitsPerComponent 8 " +
                $"/Filter /FlateDecode /Length {rgbFlate.Length} >>";
            WriteObj(4, imgDict, rgbFlate);

            // obj 5 — Content stream
            string contentStr =
                "q\n" +
                $"{F(drawW)} 0 0 {F(drawH)} {F(x)} {F(y)} cm\n" +
                "/Im0 Do\n" +
                "Q\n";
            byte[] contentBytes = Encoding.ASCII.GetBytes(contentStr);
            WriteObj(5, $"<< /Length {contentBytes.Length} >>", contentBytes);

            // obj 6 — Document Info (métadonnées)
            WriteObj(6,
                "<< /Title (" + EscapePdfString(title) + ") " +
                "/Author (AudioHexViewer) " +
                "/Producer (AudioHexViewer PdfExport v2) " +
                $"/CreationDate ({pdfDate}) " +
                $"/ModDate ({pdfDate}) >>");

            // ── xref table ───────────────────────────────────────────────────
            long xrefPos = ms.Position;
            W($"xref\n0 {offsets.Count}\n");
            W("0000000000 65535 f \n");
            for (int i = 1; i < offsets.Count; i++)
                W(offsets[i].ToString("0000000000", CultureInfo.InvariantCulture) + " 00000 n \n");

            // ── trailer ──────────────────────────────────────────────────────
            W($"trailer\n<< /Size {offsets.Count} /Root 1 0 R /Info 6 0 R >>\n");
            W($"startxref\n{xrefPos}\n%%EOF\n");

            File.WriteAllBytes(pdfPath, ms.ToArray());
        }
        // ══════════════════════════════════════════════════════════════════
        //  PDF multi-pages — une page par PNG fourni
        // ══════════════════════════════════════════════════════════════════
        public static void SaveMultiPageFromPngs(
            string[]  pngPaths,
            string    pdfPath,
            string?   docTitle   = null,
            string[]? pageTitles = null)
        {
            if (pngPaths == null || pngPaths.Length == 0)
                throw new ArgumentException("At least one PNG path is required.", nameof(pngPaths));

            string title   = docTitle ?? "AudioHexViewer Export";
            string dateStr = DateTime.Now.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
            string pdfDate = "D:" + dateStr + "+00'00'";

            using var ms = new MemoryStream();
            var offsets = new System.Collections.Generic.List<long> { 0 };

            void W(string s) => ms.Write(System.Text.Encoding.Latin1.GetBytes(s));
            void WriteObj(int n, string dict, byte[]? stream = null)
            {
                offsets.Add(ms.Position);
                W(n + " 0 obj\n" + dict + "\n");
                if (stream != null) { W("stream\n"); ms.Write(stream); W("\nendstream\n"); }
                W("endobj\n");
            }

            int pageCount   = pngPaths.Length;
            int idCatalog   = 1;
            int idPages     = 2;
            int idFirstPage = 3;
            int idFirstImg  = 3 + pageCount;
            int idFirstCont = 3 + pageCount * 2;
            int idInfo      = 3 + pageCount * 3;

            W("%PDF-1.5\n");
            W("%\xE2\xE3\xCF\xD3\n");

            WriteObj(idCatalog, "<< /Type /Catalog /Pages " + idPages + " 0 R >>");

            var kidsList = string.Join(" ", System.Linq.Enumerable.Range(idFirstPage, pageCount)
                                            .Select(i => i + " 0 R"));
            WriteObj(idPages, "<< /Type /Pages /Kids [" + kidsList + "] /Count " + pageCount + " >>");

            for (int i = 0; i < pageCount; i++)
            {
                int idPage = idFirstPage + i;
                int idImg  = idFirstImg  + i;
                int idCont = idFirstCont + i;

                int pxW, pxH;
                byte[] rgbFlate;
                using (var src = System.Drawing.Image.FromFile(pngPaths[i]))
                {
                    pxW = src.Width; pxH = src.Height;
                    rgbFlate = ExtractRgbFlate(src);
                }

                float scaleW = PageW / pxW;
                float drawW  = PageW;
                float drawH  = pxH * scaleW;
                if (drawH > PageH) { float sh = PageH / pxH; drawH = PageH; drawW = pxW * sh; }
                const float margin = 6f;
                if (drawW > PageW - 2 * margin) drawW = PageW - 2 * margin;
                if (drawH > PageH - 2 * margin) drawH = PageH - 2 * margin;
                float x = (PageW - drawW) / 2f;
                float y = (PageH - drawH) / 2f;

                WriteObj(idPage,
                    "<< /Type /Page /Parent " + idPages + " 0 R " +
                    "/MediaBox [0 0 " + F(PageW) + " " + F(PageH) + "] " +
                    "/Resources << /XObject << /Im" + i + " " + idImg + " 0 R >> >> " +
                    "/Contents " + idCont + " 0 R >>");

                string imgDict =
                    "<< /Type /XObject /Subtype /Image " +
                    "/Width " + pxW + " /Height " + pxH + " " +
                    "/ColorSpace /DeviceRGB /BitsPerComponent 8 " +
                    "/Filter /FlateDecode /Length " + rgbFlate.Length + " >>";
                WriteObj(idImg, imgDict, rgbFlate);

                string contentStr = "q\n" + F(drawW) + " 0 0 " + F(drawH) + " " + F(x) + " " + F(y) + " cm\n/Im" + i + " Do\nQ\n";
                byte[] contentBytes = System.Text.Encoding.ASCII.GetBytes(contentStr);
                WriteObj(idCont, "<< /Length " + contentBytes.Length + " >>", contentBytes);
            }

            WriteObj(idInfo,
                "<< /Title (" + EscapePdfString(title) + ") " +
                "/Author (AudioHexViewer) " +
                "/Producer (AudioHexViewer PdfExport v2) " +
                "/CreationDate (" + pdfDate + ") " +
                "/ModDate (" + pdfDate + ") >>");

            long xrefPos = ms.Position;
            W("xref\n0 " + offsets.Count + "\n");
            W("0000000000 65535 f \n");
            for (int i = 1; i < offsets.Count; i++)
                W(offsets[i].ToString("0000000000", System.Globalization.CultureInfo.InvariantCulture) + " 00000 n \n");

            W("trailer\n<< /Size " + offsets.Count + " /Root " + idCatalog + " 0 R /Info " + idInfo + " 0 R >>\n");
            W("startxref\n" + xrefPos + "\n%%EOF\n");

            File.WriteAllBytes(pdfPath, ms.ToArray());
        }


        // ── Extraction RGB + compression FlateDecode ──────────────────────────
        //
        //  Le format PDF FlateDecode pour une image RGB sans prédicteur attend :
        //  les pixels compressés en zlib (deflate avec header zlib 0x789C).
        //  On lit le bitmap en 24bpp et on compresse avec ZLibStream (.NET 6+).
        private static byte[] ExtractRgbFlate(Image src)
        {
            int w = src.Width;
            int h = src.Height;

            // Convertit en 24bpp RGB (élimine l'alpha si présent)
            using var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.DrawImage(src, 0, 0, w, h);
            }

            // Lecture pixels row-by-row en RGB (stride peut avoir du padding)
            var bd = bmp.LockBits(new Rectangle(0, 0, w, h),
                                   ImageLockMode.ReadOnly,
                                   PixelFormat.Format24bppRgb);
            try
            {
                // Copie en buffer RGB pur (pas de padding entre lignes)
                // LockBits 24bpp = BGR en mémoire Windows → on swap à RGB
                int stride = Math.Abs(bd.Stride);
                byte[] raw = new byte[stride * h];
                Marshal.Copy(bd.Scan0, raw, 0, raw.Length);

                // Assemblage en flux RGB ligne par ligne (swap B↔R)
                using var rawRgb  = new MemoryStream(w * h * 3);
                byte[] row = new byte[w * 3];
                for (int r = 0; r < h; r++)
                {
                    int rowBase = r * stride;
                    for (int c = 0; c < w; c++)
                    {
                        // Windows BMP = BGR
                        row[c * 3 + 0] = raw[rowBase + c * 3 + 2]; // R
                        row[c * 3 + 1] = raw[rowBase + c * 3 + 1]; // G
                        row[c * 3 + 2] = raw[rowBase + c * 3 + 0]; // B
                    }
                    rawRgb.Write(row);
                }

                // Compression zlib (FlateDecode PDF = zlib avec header)
                rawRgb.Position = 0;
                using var compressed = new MemoryStream();
                using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
                    rawRgb.CopyTo(zlib);

                return compressed.ToArray();
            }
            finally { bmp.UnlockBits(bd); }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        // Escape minimal pour une string PDF entre parenthèses
        private static string EscapePdfString(string s)
            => s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }
}
