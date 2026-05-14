using System;
using MathNet.Numerics;

namespace AudioHexViewer.Core
{
    /// <summary>
    /// Fonctions DSP pures — sans état, sans UI.
    ///
    /// Extraites de SpectrumControl (Phase 4) où elles étaient des méthodes
    /// <c>private static</c> enfouies dans un fichier de 7 000 lignes.
    /// Ici elles sont testables, réutilisables et découvertes facilement.
    ///
    /// Convention de signe : 0 dBFS = pleine échelle (SfftWorker normalise
    /// ses bins pour que 0 dBFS ≡ amplitude = 1.0).
    /// </summary>
    internal static class DspMath
    {
        // ── Conversion bins complexes → dBFS ─────────────────────────────────
        /// <summary>
        /// Convertit un tableau de bins complexes FFT en dBFS (20·log10|bin|).
        /// Clamp : [-200, 0] dBFS.
        /// </summary>
        internal static double[] ComplexBinsToDbFs(MathNet.Numerics.Complex32[] bins)
        {
            int n = bins.Length;
            var db = new double[n];

            for (int k = 0; k < n; k++)
            {
                double mag = bins[k].Magnitude;
                double v = (mag <= 0.0) ? -200.0 : 20.0 * Math.Log10(mag);
                if (v < -200.0) v = -200.0;
                if (v > 0.0)    v = 0.0;
                db[k] = v;
            }

            return db;
        }

        // ── Conversion bins complexes → puissance linéaire ────────────────────
        /// <summary>
        /// Convertit des bins complexes en puissance linéaire : P[k] = re² + im².
        /// Utilisé avant le moyennage Welch (qui s'effectue en puissance, pas en dB).
        /// </summary>
        internal static double[] ComplexBinsToPower(MathNet.Numerics.Complex32[] bins)
        {
            int n = bins.Length;
            var p = new double[n];

            for (int k = 0; k < n; k++)
            {
                float re = bins[k].Real;
                float im = bins[k].Imaginary;
                p[k] = (double)re * re + (double)im * im;
            }

            return p;
        }

        // ── Puissance → dBFS ──────────────────────────────────────────────────
        /// <summary>
        /// Convertit un tableau de puissances linéaires en dBFS (10·log10 P).
        /// Clamp : [-200, 0] dBFS.
        /// </summary>
        internal static double[] PowerToDbFs(double[] power)
        {
            int n = power.Length;
            var db = new double[n];

            for (int k = 0; k < n; k++)
            {
                double p = power[k];
                double v = (p <= 0.0) ? -200.0 : 10.0 * Math.Log10(p);
                if (v < -200.0) v = -200.0;
                if (v > 0.0)    v = 0.0;
                db[k] = v;
            }

            return db;
        }

        // ── Rapport de puissances → dB ────────────────────────────────────────
        /// <summary>
        /// Calcule le rapport en dB entre deux spectres de puissance :
        /// H[k] = 10·log10(leftPower[k] / rightPower[k]).
        /// Utilisé pour l'affichage Bode (FRF H1).
        /// Clamp : [-200, +200] dB — pas de clamp 0 dBFS car le ratio peut être positif.
        /// </summary>
        internal static double[] PowerRatioToDb(double[] leftPower, double[] rightPower)
        {
            int n = Math.Min(leftPower.Length, rightPower.Length);
            var db = new double[n];

            const double EPS = 1e-30; // évite les divisions par 0 → +Inf

            for (int k = 0; k < n; k++)
            {
                double pL = leftPower[k]; if (pL < EPS) pL = EPS;
                double pR = rightPower[k]; if (pR < EPS) pR = EPS;

                double v = 10.0 * Math.Log10(pL / pR);
                if (double.IsNaN(v) || double.IsInfinity(v)) v = 0.0;
                if (v < -200.0) v = -200.0;
                if (v >  200.0) v =  200.0;

                db[k] = v;
            }

            return db;
        }

        // ── Dépliement de phase ───────────────────────────────────────────────
        /// <summary>
        /// Déplie un tableau de phases en degrés (wrapped ±180°) en phase continue.
        /// Corrige les sauts de ±360° bin par bin.
        /// </summary>
        internal static double[] UnwrapPhaseDegrees(double[] wrappedDeg)
        {
            if (wrappedDeg.Length == 0) return wrappedDeg;

            var u = new double[wrappedDeg.Length];
            u[0] = wrappedDeg[0];

            double offset = 0.0;
            for (int i = 1; i < wrappedDeg.Length; i++)
            {
                double d = wrappedDeg[i] - wrappedDeg[i - 1];
                if      (d >  180.0) offset -= 360.0;
                else if (d < -180.0) offset += 360.0;
                u[i] = wrappedDeg[i] + offset;
            }

            return u;
        }

        // ── Versions InTo (Phase 7) ───────────────────────────────────────────────
        // Remplissent un buffer pré-alloué au lieu d'en créer un nouveau.
        // Utilisées par FeedExternalSpectrum pour éliminer les 4 allocations
        // new double[N/2+1] par frame (jusqu'à 64 MB/frame à 384kHz/N=4M).

        /// <inheritdoc cref="ComplexBinsToPower"/>
        internal static void ComplexBinsToPowerInto(
            MathNet.Numerics.Complex32[] bins, double[] outBuf)
        {
            int n = Math.Min(bins.Length, outBuf.Length);
            for (int k = 0; k < n; k++)
            {
                float re = bins[k].Real;
                float im = bins[k].Imaginary;
                outBuf[k] = (double)re * re + (double)im * im;
            }
        }

        /// <inheritdoc cref="PowerToDbFs"/>
        internal static void PowerToDbFsInto(double[] power, double[] outDb)
        {
            int n = Math.Min(power.Length, outDb.Length);
            for (int k = 0; k < n; k++)
            {
                double p = power[k];
                double v = (p <= 0.0) ? -200.0 : 10.0 * Math.Log10(p);
                if (v < -200.0) v = -200.0;
                if (v > 0.0)    v = 0.0;
                outDb[k] = v;
            }
        }
    }
}
