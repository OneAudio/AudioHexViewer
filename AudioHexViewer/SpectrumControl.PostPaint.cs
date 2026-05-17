// ═══════════════════════════════════════════════════════════════════════════
// SpectrumControl.PostPaint.cs  —  Dessin GDI+ sur le graphe
//
// Responsabilités :
//   • ChartSpectrum_PostPaint — redessine axes Y et panneau stats à droite
//   • DrawChartStatsPanel — panneau Vrms/Vpeak/THD/SINAD/SFDR
//   • DrawCursorInfoOverlay — bulle d'info des curseurs
//   • RebuildStatsRowsFromLabels — collecte les lignes de stats depuis l'UI
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
using System.Globalization;

namespace AudioHexViewer
{
    partial class SpectrumControl
    {
        //
        private void DrawCursorInfoOverlay(Graphics g, Chart chart)
        {
            if (_lblCursorLInfo == null || _lblCursorRInfo == null || _lblCursorDeltaInfo == null)
                return;

            bool anyVisible = _lblCursorLInfo.Visible || _lblCursorRInfo.Visible || _lblCursorDeltaInfo.Visible;
            if (!anyVisible)
                return;

            // === SCALE EXPORT ===
            // chart = clone en export, chartSpectrum = chart UI
            float s = 1f;
            if (chartSpectrum != null && chartSpectrum.Width > 0)
                s = (float)chart.Width / chartSpectrum.Width;

            // Même logique que PositionCursorInfoLabels(), mais scale-aware
            int yOffsetFromBottom = (int)Math.Round(40f * s);
            int xOffsetFromRight = (int)Math.Round(450f * s);
            int lineStep = (int)Math.Round(16f * s);
            int padDelta = (int)Math.Round(14f * s);

            int y = Math.Max(0, chart.ClientSize.Height - yOffsetFromBottom);
            int xStart = Math.Max(6, chart.ClientSize.Width - xOffsetFromRight);

            var saved = g.Save();
            g.ResetClip();

            using var font = new Font("Segoe UI", 8.5f * s, FontStyle.Regular);

            // Lignes C1/C2
            if (_lblCursorLInfo.Visible && !string.IsNullOrWhiteSpace(_lblCursorLInfo.Text))
                using (var b = new SolidBrush(_lblCursorLInfo.ForeColor))
                    g.DrawString(_lblCursorLInfo.Text, font, b, xStart, y);

            if (_lblCursorRInfo.Visible && !string.IsNullOrWhiteSpace(_lblCursorRInfo.Text))
                using (var b = new SolidBrush(_lblCursorRInfo.ForeColor))
                    g.DrawString(_lblCursorRInfo.Text, font, b, xStart, y + lineStep);

            // Delta à droite
            int w1 = (int)g.MeasureString(_lblCursorLInfo.Text ?? "", font).Width;
            int w2 = (int)g.MeasureString(_lblCursorRInfo.Text ?? "", font).Width;
            int xDelta = xStart + Math.Max(w1, w2) + padDelta;

            int maxX = Math.Max(6, chart.ClientSize.Width - (int)g.MeasureString(_lblCursorDeltaInfo.Text ?? "", font).Width - 6);
            if (xDelta > maxX) xDelta = maxX;

            if (_lblCursorDeltaInfo.Visible && !string.IsNullOrWhiteSpace(_lblCursorDeltaInfo.Text))
                using (var b = new SolidBrush(_lblCursorDeltaInfo.ForeColor))
                    g.DrawString(_lblCursorDeltaInfo.Text, font, b, xDelta, y + lineStep / 2);

            g.Restore(saved);
        }

        //
        private void ApplyChartStatsMargin()
        {
            ApplyChartStatsMargin(chartSpectrum, invalidate: true);
        }

        private string BuildChartStatsText(bool left)
        {
            string rms = left ? (lblLeftRms?.Text ?? "RMS:") : (lblRightRms?.Text ?? "RMS:");
            string peak = left ? (lblLeftPeak?.Text ?? "Peak:") : (lblRightPeak?.Text ?? "Peak:");
            string meas = left ? (lblLeftMeas?.Text ?? "—") : (lblRightMeas?.Text ?? "—");

            meas = meas.Replace("\r\n", "\n").Trim();

            return $"{rms}\n{peak}\n{meas}";
        }

        private void DrawChartStatsPanel(Graphics g, Chart chart, ChartArea area)
        {
            var plot = GetPlotRectPixels(chart, area);
            if (plot.IsEmpty) return;

            var cr = chart.ClientRectangle;

            // Rectangle EXTERNE de la ChartArea (inclut les axes/labels, dont l’axe Y2 phase)
            var pos = area.Position;
            RectangleF outer = new RectangleF(
                cr.Width * pos.X / 100f,
                cr.Height * pos.Y / 100f,
                cr.Width * pos.Width / 100f,
                cr.Height * pos.Height / 100f
            );

            // IMPORTANT : on part du bord droit EXTERNE de la ChartArea pour ne pas recouvrir les labels de l’axe Y2 (phase)
            float panelLeft = outer.Right + 8f;

            float panelRight = cr.Right - 6f;
            if (panelRight - panelLeft < 120f) return;

            var rect = RectangleF.FromLTRB(panelLeft, plot.Top, panelRight, plot.Bottom);

            var saved = g.Save();
            g.ResetClip();

            // Fond blanc + bordure
            using (var bg = new SolidBrush(Color.White))
                g.FillRectangle(bg, rect);
            using (var pen = new Pen(Color.LightGray, 1f))
                g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);

            float pad = 8f;
            float xL = rect.X + pad;
            float xR = rect.Right - pad;

            // Polices
            var fontBase = chart.Font;
            using var fontHeader = new Font(fontBase, FontStyle.Bold);
            using var fontBold = new Font(fontBase, FontStyle.Bold);
            using var fontStrong = new Font(fontBase.FontFamily, fontBase.SizeInPoints + 1.5f, FontStyle.Bold);
            using var brushStrong = new SolidBrush(Color.DimGray);
            using var fontHarm = new Font(fontBase.FontFamily, Math.Max(6f, fontBase.SizeInPoints - 1.0f), FontStyle.Regular);

            using var brushText = new SolidBrush(Color.DimGray);
            using var brushLabel = new SolidBrush(Color.DimGray);

            float y = rect.Y + pad;

            using var penSep = new Pen(Color.Gainsboro, 1f);
            using var fmtCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };
            using var fmtRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Near };
            using var fmtRightMeas = (StringFormat)fmtRight.Clone();
            fmtRightMeas.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
                        
            float DrawSection(string title, Color titleColor, List<StatRow> rows)
            {
                // Titre centré
                var headerRect = new RectangleF(rect.X + pad, y, rect.Width - 2 * pad, fontHeader.GetHeight(g));
                using (var bTitle = new SolidBrush(titleColor))
                    g.DrawString(title, fontHeader, bTitle, headerRect, fmtCenter);

                y += headerRect.Height + 4f;

                // Trait sous le titre (largeur table)
                g.DrawLine(penSep, rect.X + pad, y, rect.Right - pad, y);
                y += 8f;
                if (rows.Count == 0)
                {
                    g.DrawString("—", fontBase, brushLabel, xL, y);
                    y += fontBase.GetHeight(g) + 10f;
                    return y;
                }

                float maxLabelW = 0f;
                foreach (var rr in rows)
                {
                    Font fLabel = rr.Emphasis == StatEmphasis.Strong ? fontBold : fontBase;
                    var sz = g.MeasureString(rr.Label, fLabel);
                    if (sz.Width > maxLabelW) maxLabelW = sz.Width;
                }
                float valueLeft = xL + maxLabelW + 12f; // séparation label -> valeur
                var topRows = rows.Where(r => !r.IsBottomGroup).ToList();
                var bottomRows = rows.Where(r => r.IsBottomGroup).ToList();
                bool enteredBottomGroup = false;
                using var penSepStrong = new Pen(Color.FromArgb(170, 200, 200, 200), 1.4f);



                void RenderRows(List<StatRow> rr)
                {
                    for (int i = 0; i < rr.Count; i++)
                    {
                        var r = rr[i];

                        // Séparation visuelle entre mesures globales (haut) et curseurs (bas)
                        if (!enteredBottomGroup && r.IsBottomGroup)
                        {
                            enteredBottomGroup = true;
                            g.DrawLine(penSepStrong, rect.X + pad, y + 6f, rect.Right - pad, y + 6f);
                            y += 6f; // 
                        }


                        if (r.IsSeparator)
                        {
                            g.DrawLine(penSep, rect.X + pad, y + 4f, rect.Right - pad, y + 4f);
                            y += 12f;
                            continue;
                        }

                        // Ligne "titre" centrée (utilisée pour Cursor 1/2/Delta en Bode)
                        if (r.Emphasis == StatEmphasis.Medium && string.IsNullOrEmpty(r.Value))
                        {
                            var titleRect = new RectangleF(rect.X + pad, y, rect.Width - 2 * pad, fontStrong.GetHeight(g));
                            g.DrawString(r.Label, fontStrong, brushText, titleRect, fmtCenter);
                            y += titleRect.Height + 6f;
                            continue;
                        }

                        // Police réduite pour les harmoniques (H2, H3…) → tient dans le panneau
                        Font fLabel = r.IsSmall ? fontHarm :
                                      r.Emphasis == StatEmphasis.Strong ? fontBold : fontBase;
                        Font fVal = r.IsSmall ? fontHarm :
                                    r.Emphasis == StatEmphasis.Strong ? fontStrong :
                                    r.Emphasis == StatEmphasis.Medium ? fontBold :
                                    fontBase;

                        // Label
                        g.DrawString(r.Label, fLabel, brushLabel, xL, y);

                        // Hauteur de ligne maîtrisée
                        float rowHeight = Math.Max(fLabel.GetHeight(g), fVal.GetHeight(g));
                        var valueRect = new RectangleF(valueLeft, y, (rect.Right - pad) - valueLeft, rowHeight);

                        // Highlight EXACT (sans empiéter verticalement)
                        if (r.Emphasis == StatEmphasis.Strong)
                        {
                            using var fmtRange = (StringFormat)fmtRightMeas.Clone();
                            fmtRange.SetMeasurableCharacterRanges(new[] { new CharacterRange(0, r.Value.Length) });

                            var regions = g.MeasureCharacterRanges(r.Value, fVal, valueRect, fmtRange);
                            var bounds = regions[0].GetBounds(g);

                            bounds.Inflate(4f, 0.5f);

                            using var fill = new SolidBrush(Color.FromArgb(18, 30, 144, 255));
                            g.FillRectangle(fill, bounds);
                        }

                        // Valeur (alignée à droite)
                        g.DrawString(r.Value, fVal, brushText, valueRect, fmtRight);

                        y += rowHeight + 4f;
                    }
                }

                float EstimateHeight(List<StatRow> rr)
                {
                    float h = 0f;
                    for (int i = 0; i < rr.Count; i++)
                    {
                        var r = rr[i];
                        if (r.IsSeparator) { h += 12f; continue; }

                        if (r.Emphasis == StatEmphasis.Medium && string.IsNullOrEmpty(r.Value))
                        {
                            h += fontStrong.GetHeight(g) + 6f;
                            continue;
                        }

                        h += fontBase.GetHeight(g) + 4f;
                    }
                    return h;
                }

                // Harmoniques H2…HN : rendues directement depuis les champs d'instance,
                // en police réduite (fontHarm), APRÈS les topRows standard.
                // Ce chemin direct évite les aléas du pipeline lblMeas→InjectVolts→BuildRows.
                bool isLeftSection = title.Equals("LEFT", StringComparison.OrdinalIgnoreCase) ||
                                     title.Equals("BODE", StringComparison.OrdinalIgnoreCase);
                var harms = isLeftSection ? _lastHarmonicsLeft : _lastHarmonicsRight;
                bool showH  = isLeftSection ? _showHarmonicsLeft : _showHarmonicsRight;

                // Calcul de l'espace réservé pour le bloc harmoniques (séparateur + N lignes).
                // On le fait AVANT RenderRows(topRows) pour que les topRows ne mangent pas
                // tout l'espace disponible et laissent de la place aux harmoniques.
                float harmRowH = fontHarm.GetHeight(g) + 3f;
                float harmBlockH = 0f;
                if (showH && harms != null && harms.Count > 0)
                    harmBlockH = 12f + harms.Count * harmRowH; // 12px séparateur + lignes

                // Espace réservé pour les bottomRows (curseurs)
                float hBottomEst = bottomRows.Count > 0 ? EstimateHeight(bottomRows) + pad : 0f;

                // Limite verticale pour les topRows : on réserve l'espace des harmoniques
                // et des bottomRows en bas du panneau.
                float yTopMax = rect.Bottom - hBottomEst - harmBlockH;

                // 1) Render du haut (min/max etc.) avec clip vertical pour ne pas empiéter
                //    sur la zone réservée aux harmoniques.
                var savedState = g.Save();
                if (harmBlockH > 0f && yTopMax > rect.Top)
                {
                    g.SetClip(new RectangleF(
                        rect.Left, rect.Top,
                        rect.Width, yTopMax - rect.Top));
                }
                RenderRows(topRows);
                g.Restore(savedState);

                // Si les topRows ont avancé y au-delà de yTopMax (avant clip), on le recale
                // pour que les harmoniques démarrent exactement à la bonne position.
                if (harmBlockH > 0f && y > yTopMax)
                    y = yTopMax;

                if (showH && harms != null && harms.Count > 0)
                {
                    // Trait de séparation
                    g.DrawLine(penSep, rect.X + pad, y + 4f, rect.Right - pad, y + 4f);
                    y += 12f;

                    foreach (var h in harms)
                    {
                        if (y + fontHarm.GetHeight(g) > rect.Bottom - hBottomEst) break; // sécurité

                        string hLabel = $"H{h.Order}";
                        string hValue = (_thdUi.Unit == SpectrumSettings.ThdSettings.ThdUnit.Percent)
                            ? $"{h.RatioPercent:F5} %"
                            : $"{h.LevelDbc:F1} dB";

                        g.DrawString(hLabel, fontHarm, brushLabel, xL, y);
                        var hRect = new RectangleF(valueLeft, y, (rect.Right - pad) - valueLeft, fontHarm.GetHeight(g));
                        g.DrawString(hValue, fontHarm, brushText, hRect, fmtRight);
                        y += harmRowH;
                    }
                }

                float yAfterTop = y;

                // 2) Render du bas collé au bas du panneau (curseurs/delta)
                if (bottomRows.Count > 0)
                {
                    float hBottom = EstimateHeight(bottomRows);
                    float yBottomStart = rect.Bottom - pad - hBottom;

                    if (yBottomStart < yAfterTop)
                        yBottomStart = yAfterTop;

                    y = yBottomStart;
                    RenderRows(bottomRows);
                }

                // y final (pour section suivante)
                y += 10f;
                return y;

            }

            // En mode FRF (Bode Plot) : un seul panneau, destiné aux mesures spécifiques FRF.
            // On réutilise la section LEFT (même rendu) mais avec un titre dédié.
            if (IsFrfMode())
            {
                y = DrawSection("BODE", FftLeftColor, _rowsLeft);
            }
            else
            {
                // Affichage sections LEFT / RIGHT si activées
                if (_statsShowLeft)
                    y = DrawSection("LEFT", FftLeftColor, _rowsLeft);

                if (_statsShowRight)
                    y = DrawSection("RIGHT", FftRightColor, _rowsRight);
            }


            g.Restore(saved);
        }

        //
        private void ChartSpectrum_PostPaint(object sender, ChartPaintEventArgs e)
        {
            var chart = (System.Windows.Forms.DataVisualization.Charting.Chart)sender;

            bool isExport = !ReferenceEquals(chart, chartSpectrum);

            // Facteur géométrique (utile pour triangles/ticks), UI=1, export≈scale
            float sExport = 1f;
            if (isExport && chartSpectrum != null && chartSpectrum.Width > 0)
                sExport = (float)chart.Width / chartSpectrum.Width;

            var area = e.ChartElement as ChartArea;
            if (area == null || area.Name != "SpectrumArea")
                return;

            RectangleF plotForText = GetPlotRectPixels(chart, area);

            if (isExport && chart.Tag is ExportRenderContext ctx && !string.IsNullOrWhiteSpace(ctx.HeaderTitle))
            {
                var g = e.ChartGraphics.Graphics;
                var saved = g.Save();
                g.ResetClip();

                try
                {
                    if (!plotForText.IsEmpty)
                    {
                        var lines = ctx.HeaderTitle
                            .Replace("\r\n", "\n")
                            .Split('\n')
                            .Select(l => l.Trim())
                            .Where(l => l.Length > 0)
                            .Take(3)
                            .Select(l => l.Length > 50 ? l.Substring(0, 50) : l)
                            .ToArray();

                        if (lines.Length > 0)
                        {
                            //Dessin du texte centré au dessus du plot, police agrandie et en gras   
                            using var titleFont = new Font(chart.Font.FontFamily, chart.Font.SizeInPoints * 1.20f, FontStyle.Bold);
                            // Couleur noire pour le texte (plus lisible sur fond blanc exporté)
                            using var b = new SolidBrush(Color.Black);
                            // Format centré
                            using var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };

                            float lineH = titleFont.GetHeight(g);
                            float totalH = lineH * lines.Length;

                            // Position Y : un peu au dessus du plot, décalage 20 pixels * sExport
                            float y = plotForText.Top - totalH - (20f * sExport);
                            if (y < 2f) y = 2f;

                            for (int i = 0; i < lines.Length; i++)
                            {
                                var r = new RectangleF(plotForText.Left, y + i * lineH, plotForText.Width, lineH);
                                g.DrawString(lines[i], titleFont, b, r, fmt);
                            }
                        }
                    }
                }
                finally { g.Restore(saved); }
            }

            /// ----------------------------
            // Dessin des marqueurs de curseur
            /// ----------------------------
            void DrawDownTriangle(double f, double yValue, bool useY2, Color color)
            {
                float x = (float)area.AxisX.ValueToPixelPosition(f);
                float y = useY2
                    ? (float)area.AxisY2.ValueToPixelPosition(yValue)
                    : (float)area.AxisY.ValueToPixelPosition(yValue);

                float h = 10f * sExport;
                float w = 12f * sExport;

                var pts = new[]
                {
                    new PointF(x, y),
                    new PointF(x - w/2f, y - h),
                    new PointF(x + w/2f, y - h)
                };

                using var brush = new SolidBrush(Color.FromArgb(220, color));
                e.ChartGraphics.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.ChartGraphics.Graphics.FillPolygon(brush, pts);
            }


            if (_cursorMarkLVisible)
                DrawDownTriangle(_cursorMarkLFreq, _cursorMarkLDb, _cursorMarkLUsesY2, _cursorMarkLColor);

            if (_cursorMarkRVisible)
                DrawDownTriangle(_cursorMarkRFreq, _cursorMarkRDb, _cursorMarkRUsesY2, _cursorMarkRColor);

            // ----------------------------
            // Y : labels majeurs (remplacement complet du Chart)
            // ----------------------------
            {
                double yMajor = area.AxisY.MajorGrid.Interval;
                if (yMajor <= 0) yMajor = area.AxisY.Interval; // sécurité

                if (yMajor > 0)
                {
                    double yMin = area.AxisY.Minimum;
                    double yMax = area.AxisY.Maximum;

                    // même repère X que les mineurs (bord gauche du plot)
                    float xLeftPlot = (float)area.AxisX.ValueToPixelPosition(area.AxisX.Minimum);

                    Font baseFont = area.AxisY.LabelStyle.Font ?? chart.Font;
                    using (Font majorFont = new Font(baseFont.FontFamily, baseFont.Size, baseFont.Style))
                    {
                        float margin = 8f * sExport;
                        float lastLabelBottom = float.NegativeInfinity;

                        // format déterministe cohérent avec le pas majeur
                        string fmt;
                        if (Math.Abs(yMajor - Math.Round(yMajor)) < 1e-9) fmt = "0";
                        else fmt = "F" + GetStepDecimals(yMajor);

                        // On parcourt du haut vers le bas (comme les mineurs)
                        double yStart = yMin + Math.Floor((yMax - yMin) / yMajor) * yMajor;

                        for (double y = yStart; y >= yMin - 1e-9; y -= yMajor)
                        {
                            float yPix = (float)area.AxisY.ValueToPixelPosition(y);

                            //string txt = y.ToString(fmt, CultureInfo.InvariantCulture);
                            // En mode Bode Plot (FRF), l’axe Y2 correspond à la phase => unité en degrés sur les labels majeurs
                            string txt = y.ToString(fmt, CultureInfo.InvariantCulture) + " dB";

                            SizeF sz = e.ChartGraphics.Graphics.MeasureString(txt, majorFont);

                            float xText = xLeftPlot - sz.Width - margin;
                            float yText = yPix - sz.Height / 2f;

                            if (yText <= lastLabelBottom + 2f * sExport) continue;

                            var g = e.ChartGraphics.Graphics;
                            var saved = g.Save();
                            g.ResetClip();

                            using var b = new SolidBrush(LabelMajorColor);
                            g.DrawString(txt, majorFont, b, xText, yText);

                            g.Restore(saved);

                            lastLabelBottom = yText + sz.Height;
                        }
                    }
                }
            }

            // ----------------------------
            // Y2 : labels majeurs (remplacement complet du Chart)
            // ----------------------------
            if (area.AxisY2.Enabled == AxisEnabled.True)
            {
                double yMajor2 = area.AxisY2.Interval;
                if (yMajor2 <= 0) yMajor2 = area.AxisY2.MajorGrid.Interval; // sécurité

                if (yMajor2 > 0)
                {
                    double yMin2 = area.AxisY2.Minimum;
                    double yMax2 = area.AxisY2.Maximum;

                    // même repère X que tes mineurs, mais côté droit
                    float xRightPlot = (float)area.AxisX.ValueToPixelPosition(area.AxisX.Maximum);

                    Font baseFont = area.AxisY2.LabelStyle.Font ?? chart.Font;
                    using (Font majorFont = new Font(baseFont.FontFamily, baseFont.Size, baseFont.Style))
                    {
                        float margin = 8f * sExport;
                        float gap = 1f * sExport;
                        float lastLabelBottom = float.NegativeInfinity;

                        string fmt;
                        if (Math.Abs(yMajor2 - Math.Round(yMajor2)) < 1e-9) fmt = "0";
                        else fmt = "F" + GetStepDecimals(yMajor2);

                        double yStart = yMin2 + Math.Floor((yMax2 - yMin2) / yMajor2) * yMajor2;

                        for (double y = yStart; y >= yMin2 - 1e-9; y -= yMajor2)
                        {
                            float yPix = (float)area.AxisY2.ValueToPixelPosition(y);

                            //string txt = y.ToString(fmt, CultureInfo.InvariantCulture);
                            // Phase axis (Y2) in FRF/Bode plot: add degree symbol on major labels
                            string txt = y.ToString(fmt, CultureInfo.InvariantCulture) + "°";

                            SizeF sz = e.ChartGraphics.Graphics.MeasureString(txt, majorFont);

                            float xText = xRightPlot + gap + margin;
                            float yText = yPix - sz.Height / 2f;

                            if (yText <= lastLabelBottom + 2f * sExport) continue;

                            var g = e.ChartGraphics.Graphics;
                            var saved = g.Save();
                            g.ResetClip();

                            using var b = new SolidBrush(LabelMajorColor);
                            g.DrawString(txt, majorFont, b, xText, yText);

                            g.Restore(saved);

                            lastLabelBottom = yText + sz.Height;
                        }
                    }
                }
            }

            // ----------------------------
            // Y : ticks + labels mineurs
            // ----------------------------
            {
                double yMajor = area.AxisY.MajorGrid.Interval;
                double yMinor = area.AxisY.MinorGrid.Interval;

                if (yMajor > 0 && yMinor > 0)
                {
                    double yMin = area.AxisY.Minimum;
                    double yMax = area.AxisY.Maximum;

                    float xLeftPlot = (float)area.AxisX.ValueToPixelPosition(area.AxisX.Minimum);

                    Font baseFont = area.AxisY.LabelStyle.Font ?? chart.Font;
                    using (Font minorFont = new Font(baseFont.FontFamily, Math.Max(6f, baseFont.Size * MinorLabelScale), baseFont.Style))
                    using (Pen p = new Pen(LabelMinorColor, 1.0f))
                    {
                        float tickLenLeft = 5f * sExport;
                        float gap = 1f * sExport;
                        float margin = 8f * sExport;

                        float lastLabelBottom = float.NegativeInfinity;

                        double yStart = yMin + Math.Floor((yMax - yMin) / yMinor) * yMinor;
                        double y0 = yMin;

                        int minorPerMajor = (int)Math.Round(yMajor / yMinor);
                        if (minorPerMajor <= 0) minorPerMajor = 5;

                        for (double y = yStart; y >= yMin - 1e-9; y -= yMinor)
                        {
                            int idx = (int)Math.Round((y - y0) / yMinor);
                            int m = idx % minorPerMajor;
                            if (m < 0) m += minorPerMajor;
                            if (m == 0) continue;

                            float yPix = (float)area.AxisY.ValueToPixelPosition(y);

                            e.ChartGraphics.Graphics.DrawLine(p, xLeftPlot - gap, yPix, xLeftPlot - gap - tickLenLeft, yPix);

                            string txt = FormatDbMinor(y, yMinor);

                            SizeF sz = e.ChartGraphics.Graphics.MeasureString(txt, minorFont);
                            float xText = xLeftPlot - sz.Width - margin;
                            float yText = yPix - sz.Height / 2f;

                            if (yText <= lastLabelBottom + 2f * sExport) continue;

                            var g = e.ChartGraphics.Graphics;
                            var saved = g.Save();
                            g.ResetClip();

                            using var b = new SolidBrush(LabelMinorColor);
                            g.DrawString(txt, minorFont, b, xText, yText);

                            g.Restore(saved);

                            lastLabelBottom = yText + sz.Height;
                        }
                    }
                }
                //
                // ----------------------------
                // Y2 : ticks + labels mineurs (Phase en Bode Plot)
                // Même style que l'axe Y principal, sans grille supplémentaire.
                // Subdivisions secondaires fixes : 5
                // ----------------------------
                if (area.AxisY2.Enabled == AxisEnabled.True)
                {
                    double yMajor2 = area.AxisY2.Interval;
                    if (yMajor2 > 0)
                    {
                        double yMinor2 = yMajor2 / 5.0; // demandé : 5 subdivisions secondaires

                        double yMin2 = area.AxisY2.Minimum;
                        double yMax2 = area.AxisY2.Maximum;

                        // Bord droit du plot (en pixels)
                        float xRightPlot = (float)area.AxisX.ValueToPixelPosition(area.AxisX.Maximum);

                        Font baseFont = area.AxisY.LabelStyle.Font ?? chart.Font;
                        using (Font minorFont = new Font(baseFont.FontFamily, Math.Max(6f, baseFont.Size * MinorLabelScale), baseFont.Style))
                        using (Pen p = new Pen(LabelMinorColor, 1.0f))
                        {
                            float tickLenRight = 5f * sExport;  // même longueur que Y (gauche)
                            float gap = 1f * sExport;           // même gap
                            float margin = 8f * sExport;        // même margin (mais côté droit)

                            float lastLabelBottom = float.NegativeInfinity;

                            // Démarrage aligné sur la grille minor
                            double y0 = yMin2;
                            double yStart = yMin2 + Math.Floor((yMax2 - yMin2) / yMinor2) * yMinor2;

                            // minorPerMajor = 5 (fixe) ; m==0 => tick major => skip (déjà géré par Chart)
                            int minorPerMajor = 5;

                            for (double y = yStart; y >= yMin2 - 1e-9; y -= yMinor2)
                            {
                                int idx = (int)Math.Round((y - y0) / yMinor2);
                                int m = idx % minorPerMajor;
                                if (m < 0) m += minorPerMajor;
                                if (m == 0) continue; // pas de label/tick mineur sur un major

                                float yPix = (float)area.AxisY2.ValueToPixelPosition(y);

                                // Tick mineur côté droit
                                e.ChartGraphics.Graphics.DrawLine(p,
                                    xRightPlot + gap,
                                    yPix,
                                    xRightPlot + gap + tickLenRight,
                                    yPix);

                                // Format label mineur : cohérent avec le pas (comme dB), mais sans "deg" ni symbole
                                string txt = FormatDbMinor(y, yMinor2); // réutilise la logique de décimales existante

                                SizeF sz = e.ChartGraphics.Graphics.MeasureString(txt, minorFont);
                                float xText = xRightPlot + gap + tickLenRight + margin;
                                float yText = yPix - sz.Height / 2f;

                                // Anti-chevauchement vertical (même logique que Y)
                                if (yText <= lastLabelBottom + 0f * sExport)
                                    continue;

                                var g = e.ChartGraphics.Graphics;
                                var saved = g.Save();
                                g.ResetClip();

                                using var b = new SolidBrush(LabelMinorColor);
                                g.DrawString(txt, minorFont, b, xText, yText);

                                g.Restore(saved);

                                lastLabelBottom = yText + sz.Height;
                            }
                        }
                    }
                }


                // ---------------------------------------------------------
                // Ligne SR/N/Overlap au dessus du graphe
                // ---------------------------------------------------------
                //if (!isExport && !string.IsNullOrWhiteSpace(_TopInfoLine))
                if (!string.IsNullOrWhiteSpace(_TopInfoLine))
                {
                    var g = e.ChartGraphics.Graphics;
                    var saved = g.Save();
                    g.ResetClip();

                    try
                    {
                        RectangleF plot = GetPlotRectPixels(chart, area);
                        if (plot.IsEmpty) return;

                        // IMPORTANT : ici on NE multiplie PAS par sExport.
                        // En export, les fonts du clone sont déjà agrandies dans ExportPng().
                        Font titleFont = area.AxisX.TitleFont ?? chart.Font;

                        float y = plot.Top - titleFont.GetHeight(g) - 2f * sExport;
                        if (y < 2f) y = 2f;

                        float xLeft = plot.Left;

                        using (Brush b = new SolidBrush(InfoTextColor))
                        {
                            g.DrawString(_TopInfoLine, titleFont, b, xLeft, y);

                            if (_thdMeasuresEnabled)
                            {
                                string FormatHz(double hz) => (hz >= 1000.0) ? $"{hz / 1000.0:F0} kHz" : $"{hz:F0} Hz";

                                string hpf = _thdUi.UseHPF ? FormatHz(_thdUi.FlowHz) : "OFF";
                                string lpf = _thdUi.UseLPF ? FormatHz(_thdUi.FhighHz) : "OFF";
                                string thdLine = $"THD filter - HPF: {hpf}, LPF: {lpf}";

                                SizeF sz = g.MeasureString(thdLine, titleFont);
                                float xRight = plot.Right - sz.Width - 6f * sExport;
                                if (xRight < xLeft + 10f * sExport) xRight = xLeft + 10f * sExport;

                                g.DrawString(thdLine, titleFont, b, xRight, y);
                            }
                        }
                    }
                    finally
                    {
                        g.Restore(saved);
                    }
                }
            }

            bool useLog = chkLogX != null && chkLogX.Checked;

            if (useLog)
            {
                double fMin = area.AxisX.Minimum;
                double fMax = area.AxisX.Maximum;

                if (fMin <= 0 || fMax <= fMin)
                    return;

                using (Pen minorPen = new Pen(GridMinorColor, 1f))
                using (Pen tickPen = new Pen(LabelMinorColor, 1f))
                {
                    minorPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;

                    float top = (float)area.AxisY.ValueToPixelPosition(area.AxisY.Maximum);
                    float bottom = (float)area.AxisY.ValueToPixelPosition(area.AxisY.Minimum);

                    Font baseFont = area.AxisX.LabelStyle.Font ?? chart.Font;
                    using (Font minorFont = new Font(baseFont.FontFamily, Math.Max(6f, baseFont.Size * MinorLabelScale), baseFont.Style))
                    using (Brush minorBrush = new SolidBrush(LabelMinorColor))
                    {
                        float lastLabelRight = float.NegativeInfinity;

                        double decadeStart = Math.Pow(10, Math.Floor(Math.Log10(fMin)));
                        double decadeEnd = Math.Pow(10, Math.Ceiling(Math.Log10(fMax)));

                        for (double d = decadeStart; d <= decadeEnd; d *= 10.0)
                        {
                            for (int k = 2; k <= 9; k++)
                            {
                                double f = d * k;
                                if (f < fMin || f > fMax)
                                    continue;

                                float xPixel = (float)area.AxisX.ValueToPixelPosition(f);

                                e.ChartGraphics.Graphics.DrawLine(minorPen, xPixel, top, xPixel, bottom);

                                float minorTickLen = 5f * sExport;
                                e.ChartGraphics.Graphics.DrawLine(tickPen, xPixel, bottom, xPixel, bottom + minorTickLen);

                                string txt = FormatHzK(f);

                                var sz = e.ChartGraphics.Graphics.MeasureString(txt, minorFont);
                                float xText = xPixel - sz.Width / 2f;

                                float minorLabelYOffset = 10f * sExport;
                                float yText = bottom - minorTickLen + minorLabelYOffset;

                                if (xText <= lastLabelRight + 4f * sExport)
                                    continue;

                                using var b = new SolidBrush(LabelMinorColor);
                                e.ChartGraphics.Graphics.DrawString(txt, minorFont, b, xText, yText);

                                lastLabelRight = xText + sz.Width;
                            }
                        }
                    }
                }
            }
            else
            {
                double fMin = area.AxisX.Minimum;
                double fMax = area.AxisX.Maximum;

                double xMajor = area.AxisX.MajorGrid.Interval;
                double xMinor = area.AxisX.MinorGrid.Interval;

                if (xMajor > 0)
                {
                    if (xMinor <= 0)
                        xMinor = xMajor / 5.0;

                    if (xMinor > 0)
                    {
                        float top = (float)area.AxisY.ValueToPixelPosition(area.AxisY.Maximum);
                        float bottom = (float)area.AxisY.ValueToPixelPosition(area.AxisY.Minimum);

                        using (Pen minorPen = new Pen(LabelMinorColor, 1f))
                        {
                            minorPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;

                            Font baseFont = area.AxisX.LabelStyle.Font ?? chart.Font;
                            using (Font minorFont = new Font(baseFont.FontFamily, Math.Max(6f, baseFont.Size * MinorLabelScale), baseFont.Style))
                            using (Brush minorBrush = new SolidBrush(LabelMinorColor))
                            {
                                float minorTickLen = 5f * sExport;
                                float lastLabelRight = float.NegativeInfinity;

                                double firstMinor = fMin + xMinor;
                                const double eps = 1e-9;

                                for (double f = firstMinor; f <= fMax + eps; f += xMinor)
                                {
                                    if (f < fMin - eps) continue;

                                    double q = (f - fMin) / xMajor;
                                    if (Math.Abs(q - Math.Round(q)) < 1e-6)
                                        continue;

                                    float xPixel = (float)area.AxisX.ValueToPixelPosition(f);

                                    e.ChartGraphics.Graphics.DrawLine(minorPen, xPixel, bottom, xPixel, bottom + minorTickLen);

                                    string txt = FormatHzK(f);

                                    var sz = e.ChartGraphics.Graphics.MeasureString(txt, minorFont);
                                    float xText = xPixel - sz.Width / 2f;

                                    float yText = bottom + minorTickLen + 2f * sExport;

                                    if (xText <= lastLabelRight + 4f * sExport)
                                        continue;

                                    using var b = new SolidBrush(LabelMinorColor);
                                    e.ChartGraphics.Graphics.DrawString(txt, minorFont, b, xText, yText);
                                    lastLabelRight = xText + sz.Width;
                                }
                            }
                        }
                    }
                }
            }

            // Cadre du plot
            {
                var g = e.ChartGraphics.Graphics;

                float xLeft = (float)area.AxisX.ValueToPixelPosition(area.AxisX.Minimum);
                float xRight = (float)area.AxisX.ValueToPixelPosition(area.AxisX.Maximum);

                float yTop = (float)area.AxisY.ValueToPixelPosition(area.AxisY.Maximum);
                float yBottom = (float)area.AxisY.ValueToPixelPosition(area.AxisY.Minimum);

                float x0 = Math.Min(xLeft, xRight);
                float x1 = Math.Max(xLeft, xRight);
                float y0 = Math.Min(yTop, yBottom);
                float y1 = Math.Max(yTop, yBottom);

                var saved = g.Save();
                g.ResetClip();

                using (var pBorder = new Pen(Color.Black, 1f))
                {
                    g.DrawRectangle(pBorder, x0, y0, x1 - x0, y1 - y0);
                }

                g.Restore(saved);
            }

            try
            {
                // Panneau Stats (tableau) : affiché dans tous les modes, y compris FRF/Bode.
                DrawChartStatsPanel(e.ChartGraphics.Graphics, chart, area);

                // Export only
                if (isExport)
                {
                    DrawCursorInfoOverlay(e.ChartGraphics.Graphics, chart);
                }
            }
            catch
            {
                // sécurité : ne jamais laisser un crash UI tuer l'application
            }
        }

        private void RebuildStatsRowsFromLabels()
        {
            _rowsLeft.Clear();
            _rowsRight.Clear();

            // Mode Bode Plot : panneau dédié (pas les mesures audio classiques)
            if (SelectedChannelMode.Equals("Bode Plot", StringComparison.OrdinalIgnoreCase))
            {
                _rowsLeft.Clear();
                _rowsRight.Clear();

                BuildRowsForBodePlot(_rowsLeft);   // on n’utilise que la section "BODE" (LEFT)
                return;
            }

            string leftMeas = InjectVoltsLines(
                lblLeftMeas?.Text,
                _levelCalValidLeft,
                _lastVrmsVoltsLeft,
                _lastVpkVoltsLeft,
                _lastVdcVoltsLeft,
                ref _vrmsUnitLeft,
                ref _vpkUnitLeft,
                ref _vdcUnitLeft);


            string rightMeas = InjectVoltsLines(
                lblRightMeas?.Text,
                _levelCalValidRight,
                _lastVrmsVoltsRight,
                _lastVpkVoltsRight,
                _lastVdcVoltsRight,
                ref _vrmsUnitRight,
                ref _vpkUnitRight,
                ref _vdcUnitRight);



            BuildRowsForChannel(_rowsLeft,
                lblLeftRms?.Text,
                lblLeftPeak?.Text,
                leftMeas);

            BuildRowsForChannel(_rowsRight,
                lblRightRms?.Text,
                lblRightPeak?.Text,
                rightMeas);

            // ------------------------------
            // Curseurs dans les panneaux (hors Bode)
            // Règles:
            // 1) L+R : afficher QUE si fenêtre maximisée ET THD désactivé
            // 2) Left/Right : afficher si THD désactivé, SAUF si fenêtre maximisée => toujours afficher
            // ------------------------------
            bool thdEnabled = (chkEnableThd?.Checked == true);
            bool isMaximized = (this.FindForm()?.WindowState == FormWindowState.Maximized);

            string mode = SelectedChannelMode;

            bool showCursors =
                mode.Equals("L+R", StringComparison.OrdinalIgnoreCase)
                    ? (isMaximized && !thdEnabled)
                    : (mode.Equals("Left", StringComparison.OrdinalIgnoreCase) || mode.Equals("Right", StringComparison.OrdinalIgnoreCase))
                        ? (isMaximized || !thdEnabled)
                        : false;

            if (showCursors)
            {
                // On utilise le même rendu/placement que Bode (IsBottomGroup + titres centrés).
                // IMPORTANT : en L+R, on ajoute le bloc UNE SEULE FOIS dans la section du bas (RIGHT),
                // sinon 2 blocs "bottom" se marchent dessus.
                if (mode.Equals("Left", StringComparison.OrdinalIgnoreCase))
                    AppendCursorStatsBlock(_rowsLeft, "Level");
                else
                    AppendCursorStatsBlock(_rowsRight, "Level");
            }

        }

        //
        private void BuildRowsForBodePlot(List<StatRow> dst)
        {
            dst.Clear();

            // Sources "plot" (déjà décimées pour affichage, et limitées à la zone visible)
            var fx = _plotFreqAxis;
            var gain = _plotLeftSpectrum;          // en mode Bode Plot, LEFT = Gain (dB)
            var phi = _plotFrfPhaseDeg;            // Phase (°) si activée

            static string FmtHz(double v) => double.IsNaN(v) ? "—" : (v.ToString("0.##") + " Hz");
            static string FmtDb(double v) => double.IsNaN(v) ? "—" : (v.ToString("0.##") + " dB");
            static string FmtDeg(double v) => double.IsNaN(v) ? "—" : (v.ToString("0.##") + " °");

            // --- Min/Max visibles ---
            double gMin = double.NaN, gMax = double.NaN;
            if (gain != null && gain.Length > 0)
            {
                gMin = gain.Min();
                gMax = gain.Max();
            }

            double pMin = double.NaN, pMax = double.NaN;
            bool hasPhi = _frfPhaseEnabled && phi != null && phi.Length > 0;
            if (hasPhi)
            {
                pMin = phi.Min();
                pMax = phi.Max();
            }

            dst.Add(new StatRow { Label = "Gain min", Value = FmtDb(gMin), Emphasis = StatEmphasis.Strong });
            dst.Add(new StatRow { Label = "Gain max", Value = FmtDb(gMax), Emphasis = StatEmphasis.Strong });
            dst.Add(new StatRow { Label = "φ min", Value = hasPhi ? FmtDeg(pMin) : "---", Emphasis = StatEmphasis.Strong });
            dst.Add(new StatRow { Label = "φ max", Value = hasPhi ? FmtDeg(pMax) : "---", Emphasis = StatEmphasis.Strong });


            dst.Add(new StatRow { IsSeparator = true });

            // --- Curseurs ---
            void AddCursorBlock(string title, bool visible, double f, double y, bool usesY2)
            {
                dst.Add(new StatRow { Label = "Frequency", Value = visible ? FmtHz(f) : "—", IsBottomGroup = true });

                string what = usesY2 ? "φ" : "Gain";
                dst.Add(new StatRow { Label = what, Value = visible ? (usesY2 ? FmtDeg(y) : FmtDb(y)) : "—", IsBottomGroup = true });

            }

            bool c1On = _cursorMarkLVisible && !double.IsNaN(_cursorMarkLFreq);
            bool c2On = _cursorMarkRVisible && !double.IsNaN(_cursorMarkRFreq);

            // --- Cursor 1---
            dst.Add(new StatRow { Label = "Cursor 1", Value = "", Emphasis = StatEmphasis.Medium, IsBottomGroup = true });

            AddCursorBlock("Cursor 1", c1On, _cursorMarkLFreq, _cursorMarkLDb, _cursorMarkLUsesY2);
            dst.Add(new StatRow { IsSeparator = true, IsBottomGroup = true });

            // --- Cursor 2 ---
            dst.Add(new StatRow { Label = "Cursor 2", Value = "", Emphasis = StatEmphasis.Medium, IsBottomGroup = true });

            AddCursorBlock("Cursor 2", c2On, _cursorMarkRFreq, _cursorMarkRDb, _cursorMarkRUsesY2);
            dst.Add(new StatRow { IsSeparator = true, IsBottomGroup = true });

            // --- Delta ---
            dst.Add(new StatRow { Label = "Δ Delta", Value = "", Emphasis = StatEmphasis.Medium, IsBottomGroup = true });

            if (c1On && c2On)
            {
                double df = Math.Abs(_cursorMarkRFreq - _cursorMarkLFreq);
                dst.Add(new StatRow { Label = "ΔFrequency", Value = FmtHz(df) , IsBottomGroup = true });

                // Delta valeur seulement si mêmes unités (même axe)
                if (_cursorMarkLUsesY2 == _cursorMarkRUsesY2)
                {
                    double dy = _cursorMarkRDb - _cursorMarkLDb;
                    dst.Add(new StatRow
                    {
                        Label = _cursorMarkLUsesY2 ? "Δφ" : "ΔGain",
                        Value = _cursorMarkLUsesY2 ? (dy.ToString("0.##") + " °") : (dy.ToString("0.##") + " dB"), IsBottomGroup = true

                    });
                }
                else
                {
                    dst.Add(new StatRow { Label = "ΔGain/Δφ", Value = "—", IsBottomGroup = true });
                }
            }
            else
            {
                dst.Add(new StatRow { Label = "ΔFrequency", Value = "—", IsBottomGroup = true });
                dst.Add(new StatRow { Label = "ΔGain/Δφ", Value = "—", IsBottomGroup = true });
            }
        }

        private string InjectVoltsLines(string baseMeas, bool calValid, double? vrms, double? vpk, double? vdc,
        ref VoltUnit vrmsState, ref VoltUnit vpkState, ref VoltUnit vdcState)

        {
            if (baseMeas == null) baseMeas = string.Empty;            

            var sb = new System.Text.StringBuilder();

            // Les 3 lignes doivent TOUJOURS être présentes.
            if (!calValid)
            {   // Non calibré : indiquer "uncalibrated"
                sb.AppendLine("Vrms:  uncalibrated");
                sb.AppendLine("Vpeak: uncalibrated");
                sb.AppendLine("Vdc:   uncalibrated");
            }
            else
            {   // Calibré : afficher les valeurs
                sb.AppendLine("Vrms:  " + (vrms.HasValue ? FormatVoltage(vrms.Value, ref vrmsState) : "---"));
                sb.AppendLine("Vpeak: " + (vpk.HasValue ? FormatVoltage(vpk.Value, ref vpkState) : "---"));
                sb.AppendLine("Vdc:   " + (vdc.HasValue ? FormatVoltage(vdc.Value, ref vdcState) : "---"));
            }

            // Séparateur + ajout des mesures existantes
            if (!string.IsNullOrWhiteSpace(baseMeas))
            {
                sb.AppendLine("----");
                sb.Append(baseMeas);
            }

            return sb.ToString();
        }

        //
        private void AppendCursorStatsBlock(List<StatRow> dst, string valueLabel)
        {
            static string FmtHz(double v) => double.IsNaN(v) ? "—" : (v.ToString("0.##") + " Hz");
            static string FmtDb(double v) => double.IsNaN(v) ? "—" : (v.ToString("0.##") + " dB");

            void AddCursorLines(bool visible, double f, double yDb)
            {
                dst.Add(new StatRow { Label = "Frequency", Value = visible ? FmtHz(f) : "—", IsBottomGroup = true });
                dst.Add(new StatRow { Label = valueLabel, Value = visible ? FmtDb(yDb) : "—", IsBottomGroup = true });
            }

            bool c1On = _cursorMarkLVisible && !double.IsNaN(_cursorMarkLFreq);
            bool c2On = _cursorMarkRVisible && !double.IsNaN(_cursorMarkRFreq);

            // --- Cursor 1 ---
            dst.Add(new StatRow { Label = "Cursor 1", Value = "", Emphasis = StatEmphasis.Medium, IsBottomGroup = true });
            AddCursorLines(c1On, _cursorMarkLFreq, _cursorMarkLDb);
            dst.Add(new StatRow { IsSeparator = true, IsBottomGroup = true });

            // --- Cursor 2 ---
            dst.Add(new StatRow { Label = "Cursor 2", Value = "", Emphasis = StatEmphasis.Medium, IsBottomGroup = true });
            AddCursorLines(c2On, _cursorMarkRFreq, _cursorMarkRDb);
            dst.Add(new StatRow { IsSeparator = true, IsBottomGroup = true });

            // --- Delta ---
            dst.Add(new StatRow { Label = "ΔDelta", Value = "", Emphasis = StatEmphasis.Medium, IsBottomGroup = true });

            if (c1On && c2On)
            {
                double df = Math.Abs(_cursorMarkRFreq - _cursorMarkLFreq);
                double dy = _cursorMarkRDb - _cursorMarkLDb;

                dst.Add(new StatRow { Label = "ΔFrequency", Value = FmtHz(df), IsBottomGroup = true });
                dst.Add(new StatRow { Label = "ΔF"+valueLabel, Value = FmtDb(dy), IsBottomGroup = true });
            }
            else
            {
                dst.Add(new StatRow { Label = "ΔFrequency", Value = "—", IsBottomGroup = true });
                dst.Add(new StatRow { Label = "ΔF"+valueLabel, Value = "—", IsBottomGroup = true });
            }
        }

        private static void BuildRowsForChannel(List<StatRow> dst, string? rms, string? peak, string? meas)
        {
            BuildRowsForChannel(dst, rms, peak, rmsV: null, peakV: null, meas: meas);
        }

        //
        public void ExportPng(string filePath, int scale = 2, string? headerTitle = null)
        {
            RebuildStatsRowsFromLabels();


            using var clone = ChartExport.CloneChart(chartSpectrum);

            int headerPx = (headerTitle != null && headerTitle.Trim().Length > 0)
            ? (int)(60 * scale)   // 80px à scale=1, donc 120px à scale=2
            : 0;

            clone.Size = new Size(chartSpectrum.Width * scale, chartSpectrum.Height * scale + headerPx);

            clone.BackColor = Color.White;
            foreach (var a in clone.ChartAreas)
                a.BackColor = Color.White;

            // Uniformiser la taille des textes UNIQUEMENT SUR LE CLONE
            ApplyUniformExportScaling(clone, scale);

            // Même formatage chiffres/labels que la GUI
            clone.FormatNumber += ChartSpectrum_FormatNumber;

            // Contexte export (titre user) — uniquement pour le clone
            clone.Tag = new ExportRenderContext { HeaderTitle = headerTitle };

            // Marge stats sur le clone (tu as déjà la version reservedScale qui marche)
            ApplyChartStatsMargin(clone, invalidate: false, reservedScale: scale);

            if (headerPx > 0)
                ApplyExportHeaderMargin(clone, headerPx);

            clone.PostPaint += ChartSpectrum_PostPaint;

            ChartExport.SavePngRendered(clone, filePath, dpi: 300);

            // ===== helper local =====
            static void ApplyUniformExportScaling(System.Windows.Forms.DataVisualization.Charting.Chart c, int s)
            {
                if (s <= 1) return;

                c.Font = new Font(c.Font.FontFamily, c.Font.SizeInPoints * s, c.Font.Style);

                foreach (var area in c.ChartAreas)
                {
                    if (area.AxisX.LabelStyle.Font != null)
                        area.AxisX.LabelStyle.Font = Scale(area.AxisX.LabelStyle.Font, s);
                    if (area.AxisY.LabelStyle.Font != null)
                        area.AxisY.LabelStyle.Font = Scale(area.AxisY.LabelStyle.Font, s);

                    if (area.AxisX.TitleFont != null)
                        area.AxisX.TitleFont = Scale(area.AxisX.TitleFont, s);
                    if (area.AxisY.TitleFont != null)
                        area.AxisY.TitleFont = Scale(area.AxisY.TitleFont, s);
                }

                foreach (var leg in c.Legends)
                    if (leg.Font != null)
                        leg.Font = Scale(leg.Font, s);

                foreach (var t in c.Titles)
                    if (t.Font != null)
                        t.Font = Scale(t.Font, s);

                static Font Scale(Font f, int s)
                    => new Font(f.FontFamily, f.SizeInPoints * s, f.Style);
            }
        }

        //
        private static void ApplyExportHeaderMargin(System.Windows.Forms.DataVisualization.Charting.Chart chart, int headerPx)
        {
            var area = chart.ChartAreas["SpectrumArea"];

            int h = chart.ClientSize.Height;
            if (h <= 0) return;

            //
            float headerPct = 100f * headerPx / h;

            // On garde la même largeur, on descend juste l’area et on réduit légèrement la hauteur
            area.Position.Auto = false;
            area.Position = new ElementPosition(
                area.Position.X,
                area.Position.Y + headerPct,
                area.Position.Width,
                Math.Max(5f, area.Position.Height - headerPct)
            );
        }

    }
}
