// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

using System;
using System.Text.Json.Serialization;

namespace AudioHexViewer
{
    /// <summary>
    /// Format fichier overlay (V3) - utilisé pour superposer des courbes capturées.
    ///
    /// IMPORTANT:
    /// - On stocke les points "décimés" (tels qu'affichés), pas le spectre full.
    /// - L'axe fréquentiel est explicite (FreqHz[]), donc pas d'interpolation nécessaire.
    /// - Trois natures possibles:
    ///     * Spectrum     : LeftDbFs[] / RightDbFs[] (dBFS)
    ///     * Bode         : GainDb[] / PhaseDeg[] (dB, degrés)
    ///     * Oscilloscope : LeftV[] / RightV[] (échantillons normalisés), axe TimeMs[]
    /// </summary>
    public sealed class OverlayFile
    {
        public const int CurrentVersion = 3;

        public int Version { get; set; } = CurrentVersion;

        public string Name { get; set; } = "Overlay";
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        
        /// Type de la capture (détermine sur quelle vue l'overlay est affichable).
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public OverlayKind Kind { get; set; } = OverlayKind.Spectrum;

        /// Numéro de slot (1..8) pour conserver une couleur stable.
        public int SlotNumber { get; set; }


        // Contexte (informatif, non utilisé pour filtrer strictement l'affichage)
        public int SampleRate { get; set; }
        public int FftSize { get; set; }
        public string Window { get; set; } = "Rectangular";
        public bool RemoveDc { get; set; }

        /// <summary>
        /// Axe X explicite : même longueur que les séries de données associées.
        /// Pour Kind=Oscilloscope, ce champ n'est pas utilisé (voir TimeMs).
        /// </summary>
        public double[] FreqHz { get; set; } = Array.Empty<double>();

        // ====== Spectrum (dBFS) ======
        public double[]? LeftDbFs { get; set; }
        public double[]? RightDbFs { get; set; }

        // ====== Bode (gain dB / phase degrés) ======
        public double[]? GainDb { get; set; }
        public double[]? PhaseDeg { get; set; }

        // ====== Oscilloscope (temporel) ======
        /// <summary>
        /// Axe X explicite en millisecondes (t=0 = position du trigger au moment de la capture).
        /// Même longueur que LeftV[] / RightV[].
        /// </summary>
        public double[]? TimeMs { get; set; }

        /// <summary>Échantillons normalisés (même représentation que _capL/_capR : plein écran ≈ ±1).</summary>
        public float[]? LeftV { get; set; }
        public float[]? RightV { get; set; }

        /// <summary>TIME/DIV (ms) au moment de la capture (informatif).</summary>
        public double TimeDivMs { get; set; }

        /// <summary>Position du trigger en % de la fenêtre (0..100), au moment de la capture (informatif).</summary>
        public double PreTrigPct { get; set; }

        /// <summary>Canal(aux) capturé(s) : 0=Left, 1=Right, 2=L+R.</summary>
        public int Channel { get; set; }
    }

    public enum OverlayKind
    {
        Spectrum = 0,
        Bode = 1,
        Oscilloscope = 2
    }
}
