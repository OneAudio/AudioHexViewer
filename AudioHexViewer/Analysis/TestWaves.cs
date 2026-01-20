using System;

namespace AudioHexViewer.Core
{
    internal static class TestWaves
    {
        // Remplit un chunk stéréo avec une sinusoïde.
        // phase est en radians, conservée entre appels pour continuité.
        public static void FillStereoSine(float[] l, float[] r, int count,
                                          double amp, double dphi,
                                          ref double phase)
        {
            for (int i = 0; i < count; i++)
            {
                float v = (float)(amp * Math.Sin(phase));
                phase += dphi;
                if (phase > 2.0 * Math.PI) phase -= 2.0 * Math.PI;

                l[i] = v;
                r[i] = v;
            }
        }
        //
        //
        public readonly struct RefSignalSpec
        {
            public readonly double Fs;
            public readonly int N;
            public readonly int k0;
            public readonly double F0Hz;
            public readonly double FundamentalDbfs;
            public readonly double H2Dbc;
            public readonly double H3Dbc;
            public readonly double SpurDbc;
            public readonly double SpurHz;
            public readonly double NoiseRmsDbfs;

            public RefSignalSpec(double fs, int n, int k0, double f0Hz,
                                 double fundamentalDbfs,
                                 double h2Dbc, double h3Dbc,
                                 double spurDbc, double spurHz,
                                 double noiseRmsDbfs)
            {
                Fs = fs; N = n; this.k0 = k0; F0Hz = f0Hz;
                FundamentalDbfs = fundamentalDbfs;
                H2Dbc = h2Dbc; H3Dbc = h3Dbc;
                SpurDbc = spurDbc; SpurHz = spurHz;
                NoiseRmsDbfs = noiseRmsDbfs;
            }
        }

        public static RefSignalSpec CreateReferenceSpec(
            double fs,
            int fftSize,
            double targetFundamentalHz = 1000.0,
            double fundamentalDbfs = -12.0,
            double h2Dbc = -60.0,
            double h3Dbc = -70.0,
            double spurDbc = -80.0,
            double noiseRmsDbfs = -90.0)
        {
            // Choisit un bin k0 proche de 1 kHz : f0 = k0*Fs/N
            int k0 = (int)Math.Round(targetFundamentalHz * fftSize / fs);
            k0 = Math.Max(1, Math.Min((fftSize / 2) - 1, k0));
            double f0 = k0 * fs / fftSize;

            // Spur: on le place à une fréquence cohérente aussi, loin de f0 et des harmoniques
            // Exemple: kspur = k0*17/13 (arbitraire) puis clamp.
            int kSpur = (int)Math.Round(k0 * 17.0 / 13.0);
            kSpur = Math.Max(2, Math.Min((fftSize / 2) - 2, kSpur));
            double fSpur = kSpur * fs / fftSize;

            return new RefSignalSpec(
                fs: fs,
                n: fftSize,
                k0: k0,
                f0Hz: f0,
                fundamentalDbfs: fundamentalDbfs,
                h2Dbc: h2Dbc,
                h3Dbc: h3Dbc,
                spurDbc: spurDbc,
                spurHz: fSpur,
                noiseRmsDbfs: noiseRmsDbfs);
        }

        public static void FillStereoReferenceSignal(
            float[] l, float[] r, int count,
            RefSignalSpec spec,
            ref double phaseFund,
            ref double phaseSpur,
            ref uint rngState)
        {
            // amplitude fondamentale en lin (peak)
            double aFund = DbfsToPeak(spec.FundamentalDbfs);

            // harmoniques en dBc => amplitude = aFund * 10^(dBc/20)
            double aH2 = aFund * Math.Pow(10.0, spec.H2Dbc / 20.0);
            double aH3 = aFund * Math.Pow(10.0, spec.H3Dbc / 20.0);

            // spur en dBc
            double aSpur = aFund * Math.Pow(10.0, spec.SpurDbc / 20.0);

            // bruit blanc : on fixe RMS cible en dBFS RMS.
            // Pour un bruit uniforme [-1;1], RMS ~ 0.577 => on scale ensuite.
            // On génère du bruit blanc uniforme puis on ajuste au RMS voulu.
            double noiseRms = DbfsRmsToLin(spec.NoiseRmsDbfs);

            // Pas angulaires
            double dphiFund = 2.0 * Math.PI * spec.F0Hz / spec.Fs;
            double dphiSpur = 2.0 * Math.PI * spec.SpurHz / spec.Fs;

            for (int i = 0; i < count; i++)
            {
                // Fondamental + harmoniques (cohérentes car f0 cohérent)
                double x =
                    aFund * Math.Sin(phaseFund) +
                    aH2 * Math.Sin(2.0 * phaseFund) +
                    aH3 * Math.Sin(3.0 * phaseFund);

                // Spur (fréquence indépendante)
                x += aSpur * Math.Sin(phaseSpur);

                // Bruit blanc uniforme approx
                double n = NextUniformMinus1To1(ref rngState);
                // RMS du bruit uniforme [-1;1] : 1/sqrt(3) ~ 0.577
                // donc scale = noiseRms / 0.577...
                x += n * (noiseRms * Math.Sqrt(3.0));

                // Clamp soft pour rester dans [-1;1] (évite wrap si somme >1)
                if (x > 0.999999) x = 0.999999;
                else if (x < -0.999999) x = -0.999999;

                float v = (float)x;
                l[i] = v;
                r[i] = v;

                phaseFund += dphiFund;
                if (phaseFund > 2.0 * Math.PI) phaseFund -= 2.0 * Math.PI;

                phaseSpur += dphiSpur;
                if (phaseSpur > 2.0 * Math.PI) phaseSpur -= 2.0 * Math.PI;
            }
        }

        // --- Helpers --------------------------------------------------------

        // dBFS -> peak amplitude (sinus)
        private static double DbfsToPeak(double dbfs)
            => Math.Pow(10.0, dbfs / 20.0);

        // dBFS RMS -> lin RMS
        private static double DbfsRmsToLin(double dbfsRms)
            => Math.Pow(10.0, dbfsRms / 20.0);

        // RNG simple, déterministe, sans allocation (xorshift32)
        private static double NextUniformMinus1To1(ref uint state)
        {
            // xorshift32
            uint x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x;

            // [0,1)
            double u = (x & 0xFFFFFF) / (double)0x1000000;
            return (u * 2.0) - 1.0;
        }
    }
}
