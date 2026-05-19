using System;

namespace AudioHexViewer.Core
{
    internal static class TestWaves
    {
        // F9 : sinus simple
        public static void FillStereoSine(float[] l, float[] r, int count,
                                          double amp, double offset, double dphi,
                                          ref double phase)
        {
            for (int i = 0; i < count; i++)
            {
                double x = offset + amp * Math.Sin(phase);
                phase += dphi;
                if (phase > 2.0 * Math.PI) phase -= 2.0 * Math.PI;
                if (x > 1.0) x = 1.0;
                else if (x < -1.0) x = -1.0;
                float v = (float)x;
                l[i] = v; r[i] = v;
            }
        }

        // F10 : signal de reference multi-composantes
        //
        // Specification du signal :
        //   Fondamentale   ~1 kHz     -12 dBFS peak   coherente (k0 = round(1000*N/Fs))
        //   H2             ~2 kHz     -90 dBFS peak   coherente (2*k0)
        //   H3             ~3 kHz    -100 dBFS peak   coherente (3*k0)
        //   H4             ~4 kHz    -110 dBFS peak   coherente (4*k0)
        //   H5             ~5 kHz    -120 dBFS peak   coherente (5*k0)
        //   Spur           ~19.73 kHz -70 dBFS peak   coherente (kSpur)
        //   Bruit blanc    -140 dBFS RMS              (quasi-absent)
        //
        // Valeurs theoriques mesurables :
        //   dBFS peak   = -12.00 dBFS
        //   THD (hors spur) = ~-77.5 dBc  (domine par H2 a -78 dBc)
        //   SFDR        = 58 dBc  (spur -70 dBFS vs fond -12 dBFS)

        public readonly struct RefSignalSpec
        {
            public readonly double Fs;
            public readonly int    N;
            public readonly int    k0;
            public readonly double F0Hz;
            public readonly double FundamentalDbfs;
            public readonly double H2Dbfs;
            public readonly double H3Dbfs;
            public readonly double H4Dbfs;
            public readonly double H5Dbfs;
            public readonly double SpurDbfs;
            public readonly double SpurHz;
            public readonly double NoiseRmsDbfs;
            public readonly double ThdDbc;   // calcule, hors spur

            public RefSignalSpec(double fs, int n, int k0, double f0Hz,
                                 double fundamentalDbfs,
                                 double h2Dbfs, double h3Dbfs,
                                 double h4Dbfs, double h5Dbfs,
                                 double spurDbfs, double spurHz,
                                 double noiseRmsDbfs, double thdDbc)
            {
                Fs = fs; N = n; this.k0 = k0; F0Hz = f0Hz;
                FundamentalDbfs = fundamentalDbfs;
                H2Dbfs = h2Dbfs; H3Dbfs = h3Dbfs;
                H4Dbfs = h4Dbfs; H5Dbfs = h5Dbfs;
                SpurDbfs = spurDbfs; SpurHz = spurHz;
                NoiseRmsDbfs = noiseRmsDbfs;
                ThdDbc = thdDbc;
            }
        }

        public static RefSignalSpec CreateReferenceSpec(double fs, int fftSize)
        {
            // Fondamentale coherente proche de 1 kHz
            int k0 = (int)Math.Round(1000.0 * fftSize / fs);
            k0 = Math.Max(1, Math.Min((fftSize / 2) - 5, k0));
            double f0 = k0 * fs / fftSize;

            // Spur coherent proche de 19.73 kHz (clipe sous Nyquist - 2 bins)
            int kSpur = (int)Math.Round(19730.0 * fftSize / fs);
            kSpur = Math.Max(k0 + 10, Math.Min((fftSize / 2) - 2, kSpur));
            double fSpur = kSpur * fs / fftSize;

            const double fundDbfs  = -12.0;
            const double h2Dbfs    = -90.0;
            const double h3Dbfs    = -100.0;
            const double h4Dbfs    = -110.0;
            const double h5Dbfs    = -120.0;
            const double spurDbfs  = -70.0;
            const double noiseDbfs = -140.0;

            // THD theorique hors spur : sqrt(H2^2+H3^2+H4^2+H5^2) / A_fund
            double aFund = Pow20(fundDbfs);
            double thdRms = Math.Sqrt(
                Sq(Pow20(h2Dbfs)) + Sq(Pow20(h3Dbfs)) +
                Sq(Pow20(h4Dbfs)) + Sq(Pow20(h5Dbfs))) / aFund;
            double thdDbc = 20.0 * Math.Log10(thdRms);   // ~-77.5 dBc

            return new RefSignalSpec(
                fs, fftSize, k0, f0,
                fundDbfs, h2Dbfs, h3Dbfs, h4Dbfs, h5Dbfs,
                spurDbfs, fSpur, noiseDbfs, thdDbc);
        }

        public static void FillStereoReferenceSignal(
            float[] l, float[] r, int count,
            RefSignalSpec spec,
            ref double phaseFund,
            ref double phaseSpur,
            ref uint   rngState)
        {
            double aFund  = Pow20(spec.FundamentalDbfs);
            double aH2    = Pow20(spec.H2Dbfs);
            double aH3    = Pow20(spec.H3Dbfs);
            double aH4    = Pow20(spec.H4Dbfs);
            double aH5    = Pow20(spec.H5Dbfs);
            double aSpur  = Pow20(spec.SpurDbfs);
            double noiseScale = Pow20(spec.NoiseRmsDbfs) * Math.Sqrt(3.0);

            double dphiFund = 2.0 * Math.PI * spec.F0Hz   / spec.Fs;
            double dphiSpur = 2.0 * Math.PI * spec.SpurHz / spec.Fs;

            for (int i = 0; i < count; i++)
            {
                double x =
                    aFund * Math.Sin(phaseFund)       +
                    aH2   * Math.Sin(2.0 * phaseFund) +
                    aH3   * Math.Sin(3.0 * phaseFund) +
                    aH4   * Math.Sin(4.0 * phaseFund) +
                    aH5   * Math.Sin(5.0 * phaseFund) +
                    aSpur * Math.Sin(phaseSpur)        +
                    NextUniform(ref rngState) * noiseScale;

                if (x >  0.999999) x =  0.999999;
                else if (x < -0.999999) x = -0.999999;

                float v = (float)x;
                l[i] = v; r[i] = v;

                phaseFund += dphiFund;
                if (phaseFund > 2.0 * Math.PI) phaseFund -= 2.0 * Math.PI;
                phaseSpur += dphiSpur;
                if (phaseSpur > 2.0 * Math.PI) phaseSpur -= 2.0 * Math.PI;
            }
        }

        // dBFS (peak ou RMS selon contexte) -> amplitude lineaire
        private static double Pow20(double dbfs) => Math.Pow(10.0, dbfs / 20.0);
        private static double Sq(double x) => x * x;

        // xorshift32 -> [-1, 1)
        private static double NextUniform(ref uint state)
        {
            uint x = state;
            x ^= x << 13; x ^= x >> 17; x ^= x << 5;
            state = x;
            return ((x & 0xFFFFFF) / (double)0x1000000) * 2.0 - 1.0;
        }
    }
}
