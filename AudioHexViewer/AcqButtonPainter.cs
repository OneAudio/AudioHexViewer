// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

// ═══════════════════════════════════════════════════════════════════════════
// AcqButtonPainter.cs  —  Rendu GDI+ partagé pour le bouton RUN/STOP
//
// Utilisé par SpectrumControl, HistogramControl, OscilloscopeControl.
// Layout (de haut en bas) :
//   [RUN / STOP]        ← texte bold centré
//   ──────────────────  ← séparateur semi-transparent
//   T ACQ: ≈ 5,46 s     ← toujours affiché
//   ████████░░░░░░░░░░  ← barre fine (orange→vert, verte fixe si <1s)
//   62%                 ← affiché seulement si période ≥ 1s
// ═══════════════════════════════════════════════════════════════════════════
using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    internal static class AcqButtonPainter
    {
        // Couleurs de la barre / badge selon état
        private static readonly Color ColFilling = Color.FromArgb(255, 200, 80);   // jaune-ambre
        private static readonly Color ColDone    = Color.FromArgb(120, 230, 120);  // vert clair

        /// <summary>
        /// Dessine le contenu complet du bouton RUN/STOP.
        /// Appeler depuis btnRun.Paint.
        /// </summary>
        /// <param name="g">Graphics de l'événement Paint</param>
        /// <param name="btn">Le bouton (pour BackColor, Text, Font)</param>
        /// <param name="acqPct">0-100, progression courante</param>
        /// <param name="acqVisible">true si période ≥ 1s (affiche barre animée + %)</param>
        /// <param name="acqTimeStr">Ex: "≈ 5,46 s" — vide si inconnu</param>
        public static void Paint(Graphics g, Button btn,
                                 int acqPct, bool acqVisible, string acqTimeStr)
        {
            var rc = btn.ClientRectangle;
            g.Clear(btn.BackColor);

            using var fBtn   = new Font("Segoe UI", 10f, FontStyle.Bold);
            using var fSmall = new Font("Segoe UI", 7.5f, FontStyle.Regular);
            using var fPct   = new Font("Segoe UI", 7.5f, FontStyle.Bold);

            // ── 1. RUN / STOP (haut) ─────────────────────────────────────────
            TextRenderer.DrawText(g, btn.Text, fBtn,
                new Rectangle(0, 6, rc.Width, 22),
                Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine);

            // ── 2. Séparateur ────────────────────────────────────────────────
            using var brSep = new SolidBrush(Color.FromArgb(60, 255, 255, 255));
            g.FillRectangle(brSep, 6, 38, rc.Width - 12, 1);

            // ── 3. T ACQ ─────────────────────────────────────────────────────
            string acqText = "T ACQ: " + acqTimeStr;
            TextRenderer.DrawText(g, acqText, fSmall,
                new Rectangle(0, 41, rc.Width, 14),
                Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine);

            // ── 4. Barre de progression ───────────────────────────────────────
            int barX  = 6;
            int barY  = 57;
            int barW  = rc.Width - 12;
            int barH  = 4;
            // Si invisible (période < 1s) : barre verte fixe 100%
            int  fillPct = acqVisible ? acqPct : 100;
            bool done    = (fillPct >= 100);
            // Fond
            using var brBg = new SolidBrush(Color.FromArgb(80, 255, 255, 255));
            g.FillRectangle(brBg, barX, barY, barW, barH);
            // Remplissage
            int fillW = (int)System.Math.Round(barW * fillPct / 100.0);
            if (fillW > 0)
            {
                using var brFill = new SolidBrush(done ? ColDone : ColFilling);
                g.FillRectangle(brFill, barX, barY, fillW, barH);
            }

            // ── 5. Pourcentage (seulement si période ≥ 1s) ───────────────────
            if (acqVisible)
            {
                using var brPct = new SolidBrush(done ? ColDone : ColFilling);
                TextRenderer.DrawText(g, fillPct + "%", fPct,
                    new Rectangle(0, 63, rc.Width, 14),
                    done ? ColDone : ColFilling,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine);
            }
        }
    }
}
