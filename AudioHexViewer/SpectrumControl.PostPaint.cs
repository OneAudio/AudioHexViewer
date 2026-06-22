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

                        // Ligne "titre" centrée (utilisée pour Cursor 1/2/Delta en Bode, et COHERENCE γ²)
                        if (r.Emphasis == StatEmphasis.Medium && string.IsNullOrEmpty(r.Value))
                        {
                            Font fTitle = r.IsSmall ? fontHarm : fontStrong;
                            var titleRect = new RectangleF(rect.X + pad, y, rect.Width - 2 * pad, fTitle.GetHeight(g));
                            g.DrawString(r.Label, fTitle, brushText, titleRect, fmtCenter);
                            y += titleRect.Height + (r.IsSmall ? 3f : 6f);
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

                        // Annotation "@xxx Hz" en police réduite sur la même ligne, à droite
                        if (!string.IsNullOrEmpty(r.Annotation))
                        {
                            var annRect = new RectangleF(xL, y + rowHeight, rect.Width - 2 * pad, fontHarm.GetHeight(g));
                            g.DrawString(r.Annotation, fontHarm, brushLabel, annRect, fmtRight);
                            y += fontHarm.GetHeight(g) + 1f;
                        }

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
                            h += (r.IsSmall ? fontHarm.GetHeight(g) : fontStrong.GetHeight(g)) + (r.IsSmall ? 3f : 6f);
                            continue;
                        }

                        h += (r.IsSmall ? fontHarm.GetHeight(g) : fontBase.GetHeight(g)) + 4f;
                        if (!string.IsNullOrEmpty(r.Annotation))
                            h += fontHarm.GetHeight(g) + 1f;
                    }
                    return h;
                }

                // Harmoniques H2…HN : rendues directement depuis les champs d'instance,
                // en police réduite (fontHarm), APRÈS les topRows standard.
                // Ce chemin direct évite les aléas du pipeline lblMeas→InjectVolts→BuildRows.
                bool isLeftSection = title.Equals("LEFT", StringComparison.OrdinalIgnoreCase) ||
                                     title.Equals("BODE", StringComparison.OrdinalIgnoreCase);
                var harms = isLeftSection ? _lastHarmonicsLeft : _lastHarmonicsRight;
                // Guard direct : pas d'harmoniques si THD désactivé, quelle que soit la valeur des champs
                bool thdIsOn = (chkEnableThd?.Checked == true);
                bool showH  = thdIsOn && (isLeftSection ? _showHarmonicsLeft : _showHarmonicsRight);

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

                        float xRight = rect.Right - pad;

                        if (_thdUi.ShowHarmonicsPhase)
                        {
                            // Phase relative à la fondamentale à gauche, amplitude à droite
                            string hPhase = $"{h.PhaseDeg:+0.0;-0.0}°";
                            float ampW   = g.MeasureString(hValue, fontHarm).Width;
                            float phaseW = g.MeasureString(hPhase, fontHarm).Width;
                            g.DrawString(hValue, fontHarm, brushText,  xRight - ampW, y);
                            g.DrawString(hPhase, fontHarm, brushLabel, xRight - ampW - phaseW - 4f, y);
                        }
                        else
                        {
                            var hRect = new RectangleF(valueLeft, y, xRight - valueLeft, fontHarm.GetHeight(g));
                            g.DrawString(hValue, fontHarm, brushText, hRect, fmtRight);
                        }

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
        // ── PrePaint : enforcement de InnerPlotPosition ─────────────────────
        // MS Chart recalcule InnerPlotPosition pendant le layout (déclenché par
        // ResumeUpdates, changement d'interval, etc.). PrePaint fire APRÈS le layout
        // mais AVANT le rendu : c'est le seul moment où on peut encore imposer notre
        // valeur sans que la frame soit déjà dessinée avec la mauvaise position.
        private void ChartSpectrum_PrePaint(object sender, ChartPaintEventArgs e)
        {
            var area = e.ChartElement as ChartArea;
            if (area == null || area.Name != "SpectrumArea") return;

            // MS Chart peut réactiver LabelStyle pendant son layout interne.
            // On force la désactivation ici (après le layout, avant le rendu).
            area.AxisY.LabelStyle.Enabled  = false;
            area.AxisY2.LabelStyle.Enabled = false;

            if (_ipLocked)
            {
                area.InnerPlotPosition.Auto   = false;
                area.InnerPlotPosition.X      = _ipX;
                area.InnerPlotPosition.Y      = _ipY;
                area.InnerPlotPosition.Width  = _ipW;
                area.InnerPlotPosition.Height = _ipH;
            }
        }

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

            // ── Capture de InnerPlotPosition (PostPaint = premier moment valide) ──
            // On capture ici la valeur calculée par MS Chart pour la verrouiller
            // AVANT le prochain rendu (appliqué dans UpdateSpectrumChartAndStats).
            if (!isExport)
            {
                var ip = area.InnerPlotPosition;
                if (!_ipLocked && ip.Width > 1f && ip.Height > 1f)
                {
                    _ipX = ip.X; _ipY = ip.Y; _ipW = ip.Width; _ipH = ip.Height;
                    _ipLocked = true;
                }
            }

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
            // Dessin des marqueurs de curseur — style identique oscilloscope :
            // losange avec halo semi-transparent BgColor pour la visibilité
            /// ----------------------------
            Color chartBackColor = BackgroundColor;   // doit être déclaré avant la fonction locale

            void DrawDiamondMarker(double f, double yValue, bool useY2, Color color)
            {
                float cx = (float)area.AxisX.ValueToPixelPosition(f);
                float cy = useY2
                    ? (float)area.AxisY2.ValueToPixelPosition(yValue)
                    : (float)area.AxisY.ValueToPixelPosition(yValue);

                float d  = 6f  * sExport;
                float dh = (d + 3.5f) * sExport;

                var g2 = e.ChartGraphics.Graphics;
                g2.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Halo (légèrement plus grand, couleur de fond semi-transparente)
                var ptsHalo = new[]
                {
                    new PointF(cx,      cy - dh),
                    new PointF(cx + dh, cy),
                    new PointF(cx,      cy + dh),
                    new PointF(cx - dh, cy)
                };
                using var brHalo = new SolidBrush(Color.FromArgb(180, chartBackColor));
                g2.FillPolygon(brHalo, ptsHalo);

                // Losange principal
                var pts = new[]
                {
                    new PointF(cx,     cy - d),
                    new PointF(cx + d, cy),
                    new PointF(cx,     cy + d),
                    new PointF(cx - d, cy)
                };
                using var brDiamond = new SolidBrush(Color.FromArgb(240, color));
                g2.FillPolygon(brDiamond, pts);
            }

            if (_cursorMarkLVisible)
                DrawDiamondMarker(_cursorMarkLFreq, _cursorMarkLDb, _cursorMarkLUsesY2, _cursorMarkLColor);

            if (_cursorMarkRVisible)
                DrawDiamondMarker(_cursorMarkRFreq, _cursorMarkRDb, _cursorMarkRUsesY2, _cursorMarkRColor);

            // ----------------------------
            // Y : labels majeurs (remplacement complet du Chart)
            // ----------------------------
            // Guard : ValueToPixelPosition n'est fiable que lorsque InnerPlotPosition a été
            // calculé par le Chart (après le premier layout complet). Au premier rendu, si
            // les pixels min/max sont identiques ou absurdes, on déclenche un second rendu
            // via BeginInvoke et on sort sans tracer (les labels natifs étant désactivés,
            // l'écran reste propre jusqu'au second passage).
            {
                float _pixChk0 = (float)area.AxisY.ValueToPixelPosition(area.AxisY.Maximum);
                float _pixChkN = (float)area.AxisY.ValueToPixelPosition(area.AxisY.Minimum);
                bool _layoutOk = !isExport && Math.Abs(_pixChkN - _pixChk0) > 10f;
                if (!_layoutOk && !isExport)
                {
                    // Layout pas encore prêt → repaint différé (ne pas boucler depuis PostPaint)
                    BeginInvoke(new Action(() => chartSpectrum?.Invalidate()));
                    goto skipCustomYLabels;
                }
            }
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

                        // Premier label : plus grand multiple de yMajor ≤ yMax.
                        // Ex : yMax=-7, yMajor=5 → floor(-7/5)*5 = -10 → labels à -10,-15,...
                        // La grille native (SpectrumControl.cs) est ancrée sur
                        // ceil(yMin/yMajor)*yMajor avec IntervalOffset, même ensemble de valeurs.
                        double yStart = Math.Floor(yMax / yMajor) * yMajor;

                        for (double y = yStart; y >= yMin - 1e-9; y -= yMajor)
                        {
                            float yPix = (float)area.AxisY.ValueToPixelPosition(y);

                            //string txt = y.ToString(fmt, CultureInfo.InvariantCulture);
                            // En mode Bode Plot (FRF), l’axe Y2 correspond à la phase => unité en degrés sur les labels majeurs
                            string txt = y.ToString(fmt, CultureInfo.InvariantCulture) + YUnitSuffix();

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

            skipCustomYLabels:
            // ── Y2 : labels majeurs Phase (PostPaint, labels natifs désactivés) ──
            // Un seul bloc d'autorité. Pas de labels mineurs sur l'axe de phase.
            if (area.AxisY2.Enabled == AxisEnabled.True)
            {
                double yMajor2 = area.AxisY2.Interval;
                if (yMajor2 <= 0) yMajor2 = area.AxisY2.MajorGrid.Interval;

                float xRightPlot = (float)area.AxisX.ValueToPixelPosition(area.AxisX.Maximum);
                // Labels droite-alignés à 40px du bord droit du plot.
                // Titre vertical à 44px (après les labels).
                float xLabelRight = xRightPlot + 40f;
                float xTitlePivot = xRightPlot + 45f;

                Font baseFont2 = area.AxisY2.LabelStyle.Font ?? chart.Font;
                using Font majorFont2 = new Font(baseFont2.FontFamily, baseFont2.Size, baseFont2.Style);
                using var brushMaj2  = new SolidBrush(LabelMajorColor);

                if (yMajor2 > 0)
                {
                    double yMin2 = area.AxisY2.Minimum;
                    double yMax2 = area.AxisY2.Maximum;

                    string fmt = (Math.Abs(yMajor2 - Math.Round(yMajor2)) < 1e-9) ? "0"
                                 : "F" + GetStepDecimals(yMajor2);

                    // Itère du haut vers le bas (yStart = plus grand multiple)
                    double yStart = yMin2 + Math.Floor((yMax2 - yMin2) / yMajor2) * yMajor2;
                    float lastBottom = float.NegativeInfinity;

                    var gM = e.ChartGraphics.Graphics;
                    var savedM = gM.Save();
                    gM.ResetClip();
                    for (double y = yStart; y >= yMin2 - 1e-9; y -= yMajor2)
                    {
                        float yPix  = (float)area.AxisY2.ValueToPixelPosition(y);
                        string txt  = y.ToString(fmt, CultureInfo.InvariantCulture) + "°";
                        SizeF  sz   = gM.MeasureString(txt, majorFont2);
                        float  xTxt = xLabelRight - sz.Width;   // right-align
                        float  yTxt = yPix - sz.Height / 2f;
                        if (yTxt <= lastBottom + 2f * sExport) continue;
                        gM.DrawString(txt, majorFont2, brushMaj2, xTxt, yTxt);
                        lastBottom = yTxt + sz.Height;
                    }
                    gM.Restore(savedM);

                    // ── Titre "Phase (°)" vertical ─────────────────────────
                    float yTop2 = (float)area.AxisY2.ValueToPixelPosition(yMax2);
                    float yBot2 = (float)area.AxisY2.ValueToPixelPosition(yMin2);
                    float yMid2 = (yTop2 + yBot2) / 2f;
                    const string titleTxt = "Phase (°)";
                    SizeF  szT = gM.MeasureString(titleTxt, majorFont2);
                    var savedT = gM.Save();
                    gM.ResetClip();
                    gM.TranslateTransform(xTitlePivot, yMid2 + szT.Width / 2f);
                    gM.RotateTransform(-90f);
                    using var sfTitle = new System.Drawing.StringFormat(
                        System.Drawing.StringFormatFlags.NoWrap)
                        { Trimming = System.Drawing.StringTrimming.None };
                    var oldHint = gM.TextRenderingHint;
                    gM.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    gM.DrawString(titleTxt, majorFont2, brushMaj2, 0f, 0f, sfTitle);
                    gM.TextRenderingHint = oldHint;
                    gM.Restore(savedT);
                }
            }

            // ----------------------------
            // Y : labels mineurs supprimés (style REW — un seul pas adaptatif).
            // Les lignes de grille mineures horizontales restent via area.AxisY.MinorGrid.
            {
                // Phase : pas de labels mineurs (Y2 majeurs seulement).


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
                // ── Grille LOG complète dessinée par PostPaint (style REW) ───────────
                // Le Chart natif a ses grilles/ticks/labels X désactivés.
                // On dessine ici TOUT l'axe X : majeurs (k=1) et mineurs (k=2..9)
                // pour chaque décade d couvrant [fMin, fMax].
                //
                // Règle d'affichage des labels (identique REW) :
                //   • k=1 (décade) → label MAJEUR, police normale, trait solide
                //   • k=2..9       → label MINEUR, police réduite, trait pointillé
                //   • anti-chevauchement : on skip le label si trop proche du précédent
                //
                // fMin = valeur réelle (peut être non-décade, ex. 23 Hz ou 340 Hz).

                double fMin = customFMin.HasValue && customFMin.Value > 0
                              ? customFMin.Value
                              : area.AxisX.Minimum;
                double fMax = area.AxisX.Maximum;

                if (fMin <= 0 || fMax <= fMin)
                    goto skipLogAxis;

                {
                    var g_log = e.ChartGraphics.Graphics;

                    float top    = (float)area.AxisY.ValueToPixelPosition(area.AxisY.Maximum);
                    float bottom = (float)area.AxisY.ValueToPixelPosition(area.AxisY.Minimum);

                    Font baseFont = area.AxisX.LabelStyle.Font ?? chart.Font;
                    using (Font majorFont = new Font(baseFont.FontFamily, baseFont.Size, baseFont.Style))
                    using (Font minorFont = new Font(baseFont.FontFamily, Math.Max(6f, baseFont.Size * MinorLabelScale), baseFont.Style))
                    using (Pen majorGridPen = new Pen(GridMajorColor, 1f))
                    using (Pen minorGridPen = new Pen(GridMinorColor, 1f))
                    using (Pen majorTickPen = new Pen(LabelMajorColor, 1f))
                    using (Pen minorTickPen = new Pen(LabelMinorColor, 1f))
                    {
                        minorGridPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;

                        // Anti-chevauchement séparé : les majeurs (k=1) sont TOUJOURS
                        // affichés ; seuls les mineurs (k=2..9) sont soumis au filtre.
                        float lastMinorLabelRight = float.NegativeInfinity;

                        float majorTickLen = 7f * sExport;
                        float minorTickLen = 4f * sExport;

                        // Décade de départ : décade inférieure contenant fMin
                        double decadeStart = Math.Pow(10.0, Math.Floor(Math.Log10(fMin)));
                        double decadeEnd   = Math.Pow(10.0, Math.Ceiling(Math.Log10(fMax)));

                        // Passe 1 : dessiner les majeurs en premier pour connaître
                        // leurs emprises et les exclure des mineurs.
                        var majorRangesLog = new System.Collections.Generic.List<(float L, float R)>();

                        for (double d = decadeStart; d <= decadeEnd * 1.0001; d *= 10.0)
                        {
                            double f = d; // k=1
                            if (f < fMin || f > fMax) continue;

                            float xPixel = (float)area.AxisX.ValueToPixelPosition(f);
                            var sv = g_log.Save();
                            g_log.ResetClip();

                            g_log.DrawLine(majorGridPen, xPixel, top, xPixel, bottom);
                            g_log.DrawLine(majorTickPen, xPixel, bottom, xPixel, bottom + majorTickLen);

                            string txt  = FormatHzK(f);
                            var    sz   = g_log.MeasureString(txt, majorFont);
                            float  xText = xPixel - sz.Width / 2f;
                            float  yText = bottom + majorTickLen + 2f * sExport;

                            using (var brush = new SolidBrush(LabelMajorColor))
                                g_log.DrawString(txt, majorFont, brush, xText, yText);

                            majorRangesLog.Add((xText - 2f * sExport, xText + sz.Width + 2f * sExport));
                            g_log.Restore(sv);
                        }

                        // Passe 2 : mineurs k=2..9, anti-collision avec majeurs ET mineurs précédents
                        for (double d = decadeStart; d <= decadeEnd * 1.0001; d *= 10.0)
                        {
                            for (int k = 2; k <= 9; k++)
                            {
                                double f = d * k;
                                if (f < fMin || f > fMax) continue;

                                float xPixel = (float)area.AxisX.ValueToPixelPosition(f);
                                var sv = g_log.Save();
                                g_log.ResetClip();

                                // Grille + tick mineur (toujours dessinés)
                                g_log.DrawLine(minorGridPen, xPixel, top, xPixel, bottom);
                                g_log.DrawLine(minorTickPen, xPixel, bottom, xPixel, bottom + minorTickLen);

                                // Label mineur : soumis à l'anti-chevauchement
                                string txt   = FormatHzK(f);
                                var    sz    = g_log.MeasureString(txt, minorFont);
                                float  xText = xPixel - sz.Width / 2f;
                                float  yText = bottom + minorTickLen + 2f * sExport;
                                float  xR    = xText + sz.Width;

                                bool skip = xText <= lastMinorLabelRight + 2f * sExport;
                                if (!skip)
                                {
                                    foreach (var (mL, mR) in majorRangesLog)
                                        if (xR > mL && xText < mR) { skip = true; break; }
                                }

                                if (!skip)
                                {
                                    using var brush = new SolidBrush(LabelMinorColor);
                                    g_log.DrawString(txt, minorFont, brush, xText, yText);
                                    lastMinorLabelRight = xR;
                                }

                                g_log.Restore(sv);
                            }
                        }
                    }
                }

                skipLogAxis:;
            }
            else
            {
                double fMin   = area.AxisX.Minimum;
                double fMax   = area.AxisX.Maximum;
                double xMajor = area.AxisX.MajorGrid.Interval;
                double xMinor = area.AxisX.MinorGrid.Interval;

                if (xMajor > 0)
                {
                    float top    = (float)area.AxisY.ValueToPixelPosition(area.AxisY.Maximum);
                    float bottom = (float)area.AxisY.ValueToPixelPosition(area.AxisY.Minimum);
                    var   g_lin  = e.ChartGraphics.Graphics;

                    Font baseFont  = area.AxisX.LabelStyle.Font ?? chart.Font;
                    Font majorFont = new Font(baseFont.FontFamily, baseFont.Size, baseFont.Style);
                    Font minorFont = new Font(baseFont.FontFamily, Math.Max(6f, baseFont.Size * MinorLabelScale), baseFont.Style);
                    try
                    {
                        // ── Choix du diviseur mineur optimal ────────────────────────────
                        // Priorité 1 : diviseur qui produit des valeurs entières en Hz
                        //              ET assez de pixels pour afficher le label.
                        // Priorité 2 : si aucun entier possible, diviseur le plus fin
                        //              qui garantit l'espace physique minimum.
                        float xPxPerMajor = Math.Abs(
                            (float)area.AxisX.ValueToPixelPosition(fMin + xMajor) -
                            (float)area.AxisX.ValueToPixelPosition(fMin));

                        // ── Sélection du pas mineur par liste de pas "audio naturels" ──
                        // Au lieu de diviser xMajor par un diviseur fixe, on cherche le plus
                        // grand pas naturel (1, 2, 5, 10, 20, 50, 100... Hz) qui :
                        //   1) divise xMajor en un nombre entier de subdivisions
                        //   2) laisse assez de place pour le label (marge ×1.8 pour tenir compte
                        //      de la zone d'exclusion autour des labels majeurs)
                        //   3) n'est pas une valeur à virgule en kHz (évite "12,5k" → "13k")

                        // Label représentatif : valeur typique d'un label mineur
                        string sampleTxt = FormatHzKWithStep(fMin + xMajor * 0.5 + 1, 0);
                        float  minorLabelW = g_lin.MeasureString(sampleTxt, minorFont).Width
                                          + 4f * sExport; // + marge inter-labels

                        float minPxPerMinor = minorLabelW * 1.8f;

                        // Pas naturels décroissants (du plus grossier au plus fin)
                        double[] niceSteps = {
                            100000, 50000, 20000, 10000, 5000, 2000, 1000,
                            500, 200, 100, 50, 20, 10, 5, 2, 1,
                            0.5, 0.2, 0.1
                        };

                        xMinor = 0;
                        foreach (double ns in niceSteps)
                        {
                            if (ns >= xMajor) continue;           // pas trop grand
                            double ratio = xMajor / ns;
                            if (Math.Abs(ratio - Math.Round(ratio)) > 1e-9) continue; // divise xMajor ?
                            double pxPerNs = xPxPerMajor / ratio;
                            if (pxPerNs < minPxPerMinor) continue; // assez de place ?
                            // Pas kHz-entier pour les valeurs ≥ 1000 Hz
                            if (ns >= 1000 && Math.Abs(ns / 1000.0 - Math.Round(ns / 1000.0)) > 1e-9)
                                continue;
                            xMinor = ns;
                            break;
                        }

                        // Fallback absolu (pas naturel le plus grossier possible)
                        if (xMinor <= 0) xMinor = xMajor / 2.0;

                        // Synchroniser la grille native
                        area.AxisX.MinorGrid.Interval = xMinor;

                        // ── Labels mineurs avec anti-collision majeurs+mineurs ───────
                        var majorRanges = new System.Collections.Generic.List<(float L, float R)>();
                        const double eps = 1e-9;

                        // Pré-calculer les emprises des labels majeurs natifs du Chart
                        for (double fm = fMin; fm <= fMax + eps; fm += xMajor)
                        {
                            double q = (fm - fMin) / xMajor;
                            if (Math.Abs(q - Math.Round(q)) > 1e-6) continue;
                            float  xPx = (float)area.AxisX.ValueToPixelPosition(fm);
                            var    sz  = g_lin.MeasureString(FormatHzKWithStep(fm, xMajor), majorFont);
                            float  xL  = xPx - sz.Width / 2f;
                            majorRanges.Add((xL - 4f * sExport, xL + sz.Width + 4f * sExport));
                        }

                        using (Pen minorPen = new Pen(LabelMinorColor, 1f))
                        {
                            minorPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                            float minorTickLen  = 5f * sExport;
                            float lastLabelRight = float.NegativeInfinity;

                            for (double f = fMin; f <= fMax + eps; f += xMinor)
                            {
                                if (f < fMin - eps) continue;

                                // Ignorer les positions majeures
                                double q = (f - fMin) / xMajor;
                                if (Math.Abs(q - Math.Round(q)) < 1e-6) continue;

                                float xPixel = (float)area.AxisX.ValueToPixelPosition(f);

                                // Tick mineur
                                var sv = g_lin.Save();
                                g_lin.ResetClip();
                                g_lin.DrawLine(minorPen, xPixel, bottom, xPixel, bottom + minorTickLen);
                                g_lin.Restore(sv);

                                // Label mineur
                                string lbl   = FormatHzKWithStep(f, xMinor);
                                var    sz2   = g_lin.MeasureString(lbl, minorFont);
                                float  xText = xPixel - sz2.Width / 2f;
                                float  yText = bottom + minorTickLen + 2f * sExport;

                                if (xText <= lastLabelRight + 4f * sExport) continue;

                                bool collides = false;
                                float xR = xText + sz2.Width;
                                foreach (var (mL, mR) in majorRanges)
                                    if (xR > mL && xText < mR) { collides = true; break; }
                                if (collides) continue;

                                var sv2 = g_lin.Save();
                                g_lin.ResetClip();
                                using var b = new SolidBrush(LabelMinorColor);
                                g_lin.DrawString(lbl, minorFont, b, xText, yText);
                                g_lin.Restore(sv2);

                                lastLabelRight = xText + sz2.Width;
                            }
                        }
                    }
                    finally
                    {
                        majorFont.Dispose();
                        minorFont.Dispose();
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

                using (var pBorder = new Pen(LabelMajorColor, 1f))
                {
                    g.DrawRectangle(pBorder, x0, y0, x1 - x0, y1 - y0);
                }

                g.Restore(saved);
            }

            // ── Titre axe Y dessiné en PostPaint ────────────────────────────────
            // Le titre natif MS Chart Y est désactivé (Title="") car sa distance
            // aux labels n'est pas contrôlable. On le dessine ici pivoté à 90°,
            // à une position X fixe suffisamment à gauche des labels.
            {
                string yTitle = (SelectedChannelMode == "Bode Plot") ? "Gain (dB)"
                              : (_levelUnit == "dBV") ? "Level (dBV)" : "Level (dBFS)";

                var  g    = e.ChartGraphics.Graphics;
                Font baseFont  = area.AxisY.LabelStyle.Font ?? chart.Font;
                using (Font titleFont  = new Font(baseFont.FontFamily, baseFont.Size, baseFont.Style))
                using (var  titleBrush = new SolidBrush(LabelMajorColor))
                {
                    var   sz     = g.MeasureString(yTitle, titleFont);
                    float yTop   = (float)area.AxisY.ValueToPixelPosition(area.AxisY.Maximum);
                    float yBot   = (float)area.AxisY.ValueToPixelPosition(area.AxisY.Minimum);
                    float yMid   = (yTop + yBot) / 2f;

                    // Le texte pivoté à -90° a une emprise horizontale = sz.Height.
                    // On le positionne de façon que son bord droit soit à 4px du
                    // bord gauche du Chart, bien à gauche des labels Y.
                    float xPivot = sz.Height + 2f * sExport;

                    var saved2 = g.Save();
                    g.ResetClip();
                    g.TranslateTransform(xPivot, yMid + sz.Width / 2f);
                    g.RotateTransform(-90f);
                    var sfY = new System.Drawing.StringFormat(System.Drawing.StringFormatFlags.NoWrap)
                              { Trimming = System.Drawing.StringTrimming.None };
                    var oldHint = g.TextRenderingHint;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    g.DrawString(yTitle, titleFont, titleBrush, 0f, 0f, sfY);
                    g.TextRenderingHint = oldHint;
                    sfY.Dispose();
                    g.Restore(saved2);
                }
            }

            // Y2 labels + titre : gérés dans le bloc unique Y2 ci-dessus.

            // ── Titre axe X dessiné en PostPaint ────────────────────────────────
            // Positionné en coordonnées Chart client fixes (pas relatives au plot),
            // pour rester stable quelle que soit la hauteur de la ChartArea.
            {
                const string xTitle = "Frequency (Hz)";
                var  g    = e.ChartGraphics.Graphics;
                Font baseFont  = area.AxisX.LabelStyle.Font ?? chart.Font;
                using (Font titleFont  = new Font(baseFont.FontFamily, baseFont.Size, baseFont.Style))
                using (var  titleBrush = new SolidBrush(LabelMajorColor))
                {
                    var   sz      = g.MeasureString(xTitle, titleFont);
                    float xLeft   = (float)area.AxisX.ValueToPixelPosition(area.AxisX.Minimum);
                    float xRight  = (float)area.AxisX.ValueToPixelPosition(area.AxisX.Maximum);
                    float xCenter = (xLeft + xRight) / 2f;

                    // Y fixe : 10px avant le bas du contrôle Chart
                    float yTitle = chart.ClientSize.Height - sz.Height - 4f * sExport;

                    var saved2 = g.Save();
                    g.ResetClip();
                    g.DrawString(xTitle, titleFont, titleBrush,
                                 xCenter - sz.Width / 2f, yTitle);
                    g.Restore(saved2);
                }
            }

            try
            {
                // Panneau Stats : seulement si visible
                if (_measPanelVisible)
                    DrawChartStatsPanel(e.ChartGraphics.Graphics, chart, area);

                // Floating cursor box (Graph / Both modes)
                DrawCursorFloatBox(e.ChartGraphics.Graphics, plotForText);

                // Delta label arrow (Label button)
                DrawCursorDeltaLabel(e.ChartGraphics.Graphics, plotForText);

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

            // ── AHV watermark — top-right of plot area (live view only) ──
            if (!isExport)
                GraphWatermark.Draw(e.ChartGraphics.Graphics, plotForText);
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
            // Curseurs dans les panneaux (hors Bode — déjà géré dans BuildRowsForBodePlot)
            // Règles :
            //   L ou R : maximisé → toujours ; non-maximisé → seulement si THD Off
            //   L+R    : maximisé ET THD Off seulement
            // ------------------------------
            bool thdEnabled = (chkEnableThd?.Checked == true);
            bool isMaximized = (this.FindForm()?.WindowState == FormWindowState.Maximized);

            string mode = SelectedChannelMode;

            bool showCursors = mode.Equals("L+R", StringComparison.OrdinalIgnoreCase)
                ? (isMaximized && !thdEnabled)   // L+R : maximisé ET THD désactivé
                : (isMaximized || !thdEnabled);  // L ou R : maximisé OU THD désactivé

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
            var coh = _plotFrfCoherence;           // Cohérence γ² ∈ [0,1]

            static string FmtHz(double v)  => double.IsNaN(v) ? "—" : (v.ToString("0.##") + " Hz");
            static string FmtDb(double v)  => double.IsNaN(v) ? "—" : (v.ToString("0.##") + " dB");
            static string FmtDeg(double v) => double.IsNaN(v) ? "—" : (v.ToString("0.##") + " °");
            static string FmtPct(double v) => double.IsNaN(v) ? "—" : (v * 100.0).ToString("0.#") + " %";

            // Helper : trouve la fréquence (Hz) associée à la valeur min/max d'un tableau,
            // retourne (valeur, annotation "@xxx Hz") séparément pour un rendu à deux polices.
            (string val, string ann) DbAtHz(double dbVal, double[]? arr)
            {
                if (double.IsNaN(dbVal) || arr == null || fx == null || fx.Length != arr.Length)
                    return (FmtDb(dbVal), "");
                int idx = -1;
                for (int i = 0; i < arr.Length; i++)
                    if (arr[i] == dbVal) { idx = i; break; }
                return idx < 0 ? (FmtDb(dbVal), "") : (FmtDb(dbVal), "@ " + FmtHz(fx[idx]));
            }
            (string val, string ann) DegAtHz(double degVal, double[]? arr)
            {
                if (double.IsNaN(degVal) || arr == null || fx == null || fx.Length != arr.Length)
                    return (FmtDeg(degVal), "");
                int idx = -1;
                for (int i = 0; i < arr.Length; i++)
                    if (arr[i] == degVal) { idx = i; break; }
                return idx < 0 ? (FmtDeg(degVal), "") : (FmtDeg(degVal), "@ " + FmtHz(fx[idx]));
            }

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

            // --- Cohérence γ² : min/moyenne sur la bande visible ---
            // Les bins NaN (γ² indéfini, ex: bin DC sans signal d'entrée) sont exclus :
            // ils ne représentent pas une perte de cohérence mesurée.
            double cMin = double.NaN, cAvg = double.NaN;
            bool hasCoh = false;
            if (coh != null && coh.Length > 0)
            {
                var valid = coh.Where(v => !double.IsNaN(v)).ToArray();
                if (valid.Length > 0)
                {
                    hasCoh = true;
                    cMin = valid.Min();
                    cAvg = valid.Average();
                }
            }

            // --- Normalisation status (point 1) ---
            bool hasRef = _frfRefGainDb != null && _frfRefPhaseDeg != null;
            string normStatus = (_frfNormEnabled && hasRef) ? "ON" : hasRef ? "OFF (ref. captured)" : "---";
            dst.Add(new StatRow { Label = "Normalisation", Value = normStatus, Emphasis = StatEmphasis.Strong });

            // Gain/Phase min/max avec fréquence associée en suffix "@xxx Hz" (Annotation, police réduite)
            var (gMinV, gMinA) = DbAtHz(gMin, gain);
            var (gMaxV, gMaxA) = DbAtHz(gMax, gain);
            var (pMinV, pMinA) = hasPhi ? DegAtHz(pMin, phi) : ("---", "");
            var (pMaxV, pMaxA) = hasPhi ? DegAtHz(pMax, phi) : ("---", "");

            dst.Add(new StatRow { Label = "Gain min", Value = gMinV, Annotation = gMinA, Emphasis = StatEmphasis.Strong });
            dst.Add(new StatRow { Label = "Gain max", Value = gMaxV, Annotation = gMaxA, Emphasis = StatEmphasis.Strong });
            dst.Add(new StatRow { Label = "φ min",    Value = pMinV, Annotation = pMinA, Emphasis = StatEmphasis.Strong });
            dst.Add(new StatRow { Label = "φ max",    Value = pMaxV, Annotation = pMaxA, Emphasis = StatEmphasis.Strong });

            dst.Add(new StatRow { IsSeparator = true });

            // --- Coherence γ² : small section (reduced font via IsSmall) ---
            dst.Add(new StatRow { Label = "COHERENCE γ²", Value = "", Emphasis = StatEmphasis.Medium, IsSmall = true });
            dst.Add(new StatRow { Label = "γ² min", Value = hasCoh ? FmtPct(cMin) : "---", Emphasis = StatEmphasis.Normal, IsSmall = true });
            dst.Add(new StatRow { Label = "γ² avg", Value = hasCoh ? FmtPct(cAvg) : "---", Emphasis = StatEmphasis.Normal, IsSmall = true });
            // Pas de séparateur ici — le titre "Cursor 1" (IsBottomGroup) crée déjà la séparation visuelle forte

            // --- Curseurs (compact: titre + fréquence + valeur sur 2 lignes, IsSmall) ---
            void AddCursorCompact(bool visible, double f, double yVal, bool usesY2)
            {
                string valStr = usesY2 ? FmtDeg(yVal) : FmtDb(yVal);
                dst.Add(new StatRow { Label = "Frequency", Value = visible ? FmtHz(f)   : "—", IsBottomGroup = true, IsSmall = true });
                dst.Add(new StatRow { Label = usesY2 ? "φ" : "Gain", Value = visible ? valStr : "—", IsBottomGroup = true, IsSmall = true });
            }

            bool c1On = _cursorMarkLVisible && !double.IsNaN(_cursorMarkLFreq);
            bool c2On = _cursorMarkRVisible && !double.IsNaN(_cursorMarkRFreq);

            // Cursor values in panel : only shown when display mode is Panel (0) or Both (2).
            // Graph (1) and Off (3) hide the panel rows.
            bool showInPanel = (_cursorDispMode == 0 || _cursorDispMode == 2);

            if (showInPanel)
            {
                dst.Add(new StatRow { Label = "Cursor 1", Value = "", Emphasis = StatEmphasis.Medium, IsBottomGroup = true, IsSmall = true });
                AddCursorCompact(c1On, _cursorMarkLFreq, _cursorMarkLDb, _cursorMarkLUsesY2);

                dst.Add(new StatRow { IsSeparator = true, IsBottomGroup = true });

                dst.Add(new StatRow { Label = "Cursor 2", Value = "", Emphasis = StatEmphasis.Medium, IsBottomGroup = true, IsSmall = true });
                AddCursorCompact(c2On, _cursorMarkRFreq, _cursorMarkRDb, _cursorMarkRUsesY2);

                dst.Add(new StatRow { IsSeparator = true, IsBottomGroup = true });

                dst.Add(new StatRow { Label = "Δ Delta", Value = "", Emphasis = StatEmphasis.Medium, IsBottomGroup = true, IsSmall = true });

                if (c1On && c2On)
                {
                    double df = Math.Abs(_cursorMarkRFreq - _cursorMarkLFreq);
                    dst.Add(new StatRow { Label = "ΔFrequency", Value = FmtHz(df), IsBottomGroup = true, IsSmall = true });

                    if (_cursorMarkLUsesY2 == _cursorMarkRUsesY2)
                    {
                        double dy = _cursorMarkRDb - _cursorMarkLDb;
                        dst.Add(new StatRow
                        {
                            Label = _cursorMarkLUsesY2 ? "Δφ" : "ΔGain",
                            Value = _cursorMarkLUsesY2 ? (dy.ToString("0.##") + " °") : (dy.ToString("0.##") + " dB"),
                            IsBottomGroup = true, IsSmall = true
                        });
                    }
                    else
                        dst.Add(new StatRow { Label = "ΔGain/Δφ", Value = "—", IsBottomGroup = true, IsSmall = true });
                }
                else
                {
                    dst.Add(new StatRow { Label = "ΔFrequency", Value = "—", IsBottomGroup = true, IsSmall = true });
                    dst.Add(new StatRow { Label = "ΔGain/Δφ",  Value = "—", IsBottomGroup = true, IsSmall = true });
                }
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
            // Only shown when display mode is Panel (0) or Both (2).
            if (_cursorDispMode != 0 && _cursorDispMode != 2) return;

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
        /// <summary>Retourne le Chart live pour copie presse-papier (résolution écran).</summary>
        public System.Windows.Forms.DataVisualization.Charting.Chart GetChartForClipboard()
            => chartSpectrum;

        // ── Export CSV ───────────────────────────────────────────────────────
        public System.Text.StringBuilder GetCsvData(char sep, bool header, char decSep = '.')
        {
            var sb = new System.Text.StringBuilder();
            string s = sep.ToString();
            string ic = System.Globalization.CultureInfo.InvariantCulture.ToString();
            var ic2 = System.Globalization.CultureInfo.InvariantCulture;
            // Formate un double avec le séparateur décimal choisi
            string Fmt(double v, string fmt) {
                var s2 = v.ToString(fmt, ic2);
                return decSep == ',' ? s2.Replace('.', ',') : s2;
            }

            bool hasL = _plotLeftSpectrum  != null && _plotLeftSpectrum.Length  > 0;
            bool hasR = _plotRightSpectrum != null && _plotRightSpectrum.Length > 0;
            if (_plotFreqAxis == null || _plotFreqAxis.Length == 0) return sb;

            int n = _plotFreqAxis.Length;
            if (hasL) n = Math.Min(n, _plotLeftSpectrum!.Length);
            if (hasR) n = Math.Min(n, _plotRightSpectrum!.Length);

            // Active overlays
            var activeOverlays = new System.Collections.Generic.List<(string name, double[] freqHz, double[]? leftDb, double[]? rightDb)>();
            foreach (var ov in _overlayCache)
            {
                if (!ov.Visible) continue;
                var d = ov.Data;
                if (d?.FreqHz == null) continue;
                activeOverlays.Add((ov.Entry?.Name ?? "Overlay", d.FreqHz, d.LeftDbFs, d.RightDbFs));
            }

            if (header)
            {
                sb.Append("Freq_Hz");
                if (hasL) sb.Append($"{s}Left_dBFS");
                if (hasR) sb.Append($"{s}Right_dBFS");
                foreach (var (name, _, ovL, ovR) in activeOverlays)
                {
                    if (ovL != null) sb.Append($"{s}{name}_L_dBFS");
                    if (ovR != null) sb.Append($"{s}{name}_R_dBFS");
                }
                sb.AppendLine();
            }

            for (int i = 0; i < n; i++)
            {
                sb.Append(_plotFreqAxis[i].ToString("F4", ic2));
                if (hasL) sb.Append($"{s}{_plotLeftSpectrum![i].ToString("F4", ic2)}");
                if (hasR) sb.Append($"{s}{_plotRightSpectrum![i].ToString("F4", ic2)}");
                foreach (var (_, ovFx, ovL, ovR) in activeOverlays)
                {
                    // Interpolate overlay to same freq grid
                    if (ovL != null) sb.Append($"{s}{Fmt(InterpolateDb(ovFx, ovL, _plotFreqAxis[i]), "F4")}");
                    if (ovR != null) sb.Append($"{s}{Fmt(InterpolateDb(ovFx, ovR, _plotFreqAxis[i]), "F4")}");
                }
                sb.AppendLine();
            }
            return sb;
        }

        private static double InterpolateDb(double[] fx, double[] db, double f)
        {
            if (fx.Length == 0) return double.NaN;
            int lo = Array.BinarySearch(fx, f);
            if (lo >= 0) return db[lo];
            lo = ~lo;
            if (lo == 0) return db[0];
            if (lo >= fx.Length) return db[^1];
            double t = (f - fx[lo-1]) / (fx[lo] - fx[lo-1]);
            return db[lo-1] + t * (db[lo] - db[lo-1]);
        }

        public void ExportPng(string filePath, int scale = 2, string? headerTitle = null, string? footerText = null)
        {
            RebuildStatsRowsFromLabels();

            int headerPx = ChartExport.HeaderPixels(headerTitle, scale);

            using var clone = ChartExport.CloneChart(chartSpectrum);
            clone.Size      = new Size(chartSpectrum.Width * scale, chartSpectrum.Height * scale);
            clone.BackColor = Color.White;
            foreach (var a in clone.ChartAreas) a.BackColor = Color.White;

            ChartExport.ApplyExportScaling(clone, scale);

            // Même formatage chiffres/labels que la GUI
            clone.FormatNumber += ChartSpectrum_FormatNumber;
            clone.Tag           = new ExportRenderContext { HeaderTitle = null };  // titre géré en GDI+ pur ci-dessous

            // Marge panneau stats (droite)
            ApplyChartStatsMargin(clone, invalidate: false, reservedScale: scale);

            // Marge haute si titre
            if (headerPx > 0)
                ChartExport.ApplyChartHeaderMargin(clone, "SpectrumArea", headerPx);

            clone.PostPaint += ChartSpectrum_PostPaint;

            // Rendu clone → bytes PNG
            var pngBytes = ChartExport.RenderChartToPngBytes(clone);

            // Composition finale : header + footer GDI+ pur par-dessus
            ChartExport.SaveChartPng(pngBytes, filePath, headerPx, headerTitle, scale, dpi: 300, footerText: footerText);
        }

    }
}