using System;
using System.Collections.Concurrent;

namespace AudioHexViewer.Core
{
    /// <summary>
    /// Fabrique et cache des fenêtres d'analyse spectrales.
    ///
    /// Les coefficients sont calculés une seule fois par (type, taille) puis
    /// réutilisés indéfiniment — aucune allocation en chemin chaud.
    /// Thread-safe via ConcurrentDictionary.
    ///
    /// IMPORTANT : le tableau <see cref="WindowEntry.Coeffs"/> est partagé.
    /// Ne jamais le modifier — le lire uniquement.
    /// </summary>
    internal static class WindowFactory
    {
        private static readonly ConcurrentDictionary<(WindowType type, int size), WindowEntry> _cache
            = new ConcurrentDictionary<(WindowType, int), WindowEntry>();

        /// <summary>
        /// Retourne l'entrée (coefficients + somme) pour la fenêtre demandée.
        /// Construit et met en cache au premier appel, retourne directement ensuite.
        /// </summary>
        public static WindowEntry Get(WindowType type, int size)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));

            return _cache.GetOrAdd((type, size), static key => Build(key.type, key.size));
        }

        // ── Dispatch ─────────────────────────────────────────────────────────
        private static WindowEntry Build(WindowType type, int N)
        {
            double[] coeffs = type switch
            {
                WindowType.Rectangular      => BuildRectangular(N),
                WindowType.Hann             => BuildHann(N),
                WindowType.Hamming          => BuildHamming(N),
                WindowType.BlackmanHarris4  => BuildBlackmanHarris4(N),
                WindowType.BlackmanHarris7  => BuildBlackmanHarris7(N),
                _                           => BuildBlackmanHarris7(N),
            };

            // Calcule la somme une seule fois ici — SfftWorker et SpectrumAnalyzer
            // en ont tous les deux besoin pour la normalisation.
            double sum = 0.0;
            for (int i = 0; i < coeffs.Length; i++) sum += coeffs[i];

            return new WindowEntry(coeffs, sum);
        }

        // ── Rectangular ──────────────────────────────────────────────────────
        private static double[] BuildRectangular(int N)
        {
            var win = new double[N];
            for (int i = 0; i < N; i++) win[i] = 1.0;
            return win;
        }

        // ── Hann ─────────────────────────────────────────────────────────────
        private static double[] BuildHann(int N)
        {
            var win = new double[N];
            if (N <= 1) { if (N == 1) win[0] = 1.0; return win; }

            double denom = N - 1;
            for (int n = 0; n < N; n++)
                win[n] = 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * n / denom));

            return win;
        }

        // ── Hamming ───────────────────────────────────────────────────────────
        private static double[] BuildHamming(int N)
        {
            var win = new double[N];
            if (N <= 1) { if (N == 1) win[0] = 1.0; return win; }

            double denom = N - 1;
            for (int n = 0; n < N; n++)
                win[n] = 0.54 - 0.46 * Math.Cos(2.0 * Math.PI * n / denom);

            return win;
        }

        // ── Blackman-Harris 4 termes ─────────────────────────────────────────
        private static double[] BuildBlackmanHarris4(int N)
        {
            var win = new double[N];
            if (N <= 1) { if (N == 1) win[0] = 1.0; return win; }

            const double a0 = 0.35875;
            const double a1 = 0.48829;
            const double a2 = 0.14128;
            const double a3 = 0.01168;

            double denom = N - 1;
            for (int n = 0; n < N; n++)
            {
                double w = 2.0 * Math.PI * n / denom;
                win[n] = a0
                       - a1 * Math.Cos(w)
                       + a2 * Math.Cos(2.0 * w)
                       - a3 * Math.Cos(3.0 * w);
            }
            return win;
        }

        // ── Blackman-Harris 7 termes ─────────────────────────────────────────
        // Source : NI / Wolfram. Identique aux coefficients utilisés dans
        // l'ancienne version de SfftWorker et SpectrumAnalyzer.
        private static double[] BuildBlackmanHarris7(int N)
        {
            var win = new double[N];
            if (N <= 1) { if (N == 1) win[0] = 1.0; return win; }

            const double a0 = 0.27105140069342;
            const double a1 = 0.43329793923448;
            const double a2 = 0.21812299954311;
            const double a3 = 0.06592544638803;
            const double a4 = 0.01081174209837;
            const double a5 = 0.00077658482522;
            const double a6 = 0.00001388721735;

            double denom = N - 1;
            for (int n = 0; n < N; n++)
            {
                double w = 2.0 * Math.PI * n / denom;
                win[n] = a0
                       - a1 * Math.Cos(w)
                       + a2 * Math.Cos(2.0 * w)
                       - a3 * Math.Cos(3.0 * w)
                       + a4 * Math.Cos(4.0 * w)
                       - a5 * Math.Cos(5.0 * w)
                       + a6 * Math.Cos(6.0 * w);
            }
            return win;
        }
    }

    // ── DTO résultat ──────────────────────────────────────────────────────────
    /// <summary>
    /// Fenêtre pré-calculée : coefficients (partagés, lecture seule) + somme.
    /// </summary>
    internal readonly struct WindowEntry
    {
        /// <summary>
        /// Tableau de coefficients. LECTURE SEULE — partagé entre tous les appelants.
        /// </summary>
        public readonly double[] Coeffs;

        /// <summary>
        /// Somme de tous les coefficients (= windowSum pour la normalisation FFT).
        /// </summary>
        public readonly double Sum;

        internal WindowEntry(double[] coeffs, double sum)
        {
            Coeffs = coeffs;
            Sum    = sum;
        }
    }
}
