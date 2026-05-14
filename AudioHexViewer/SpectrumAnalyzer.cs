using System;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using AudioHexViewer.Core;

namespace AudioHexViewer
{
    internal static class SpectrumAnalyzer
    {
        // Tu pourras y référer depuis ailleurs si besoin
        public const int Max24Pos = 0x7FFFFF; // 24-bit full scale


        /// <summary>
        /// Applique la fen\u00eatre sur <paramref name="buffer"/> en place.
        /// Utilise <see cref="WindowFactory"/> : aucune allocation, les coefficients
        /// sont mis en cache apr\u00e8s le premier appel pour chaque (type, taille).
        /// </summary>
        /// <returns>Somme des coefficients de la fen\u00eatre (windowSum), n\u00e9cessaire
        /// pour la normalisation FFT.</returns>
        public static double ApplyWindowInPlace(double[] buffer, WindowType windowType)
        {
            int N = buffer.Length;
            var entry = WindowFactory.Get(windowType, N);
            double[] win = entry.Coeffs;

            for (int i = 0; i < N; i++)
                buffer[i] *= win[i];

            return entry.Sum;
        }
        //
        public static double[] ComputeSpectrumDbFs(double[] timeData, double windowSum)
        {
            int N = timeData.Length;

            // IMPORTANT: MathNet veut Complex32 dans ton cas
            var fft = new MathNet.Numerics.Complex32[N];
            for (int i = 0; i < N; i++)
                fft[i] = new MathNet.Numerics.Complex32((float)timeData[i], 0f);

            Fourier.Forward(fft, FourierOptions.Matlab);

            int bins = N / 2 + 1;
            var spectrum = new double[bins];

            if (windowSum <= 0.0)
                windowSum = N;

            double scale = 2.0 / windowSum / Max24Pos;

            for (int k = 0; k < bins; k++)
            {
                double mag = fft[k].Magnitude;      // float -> auto convert
                double amp = mag * scale;

                double db = (amp <= 0.0) ? -200.0 : 20.0 * Math.Log10(amp);

                if (db < -200.0) db = -200.0;
                if (db > 0.0) db = 0.0;

                spectrum[k] = db;
            }

            return spectrum;
        }
        //
        public static double[] ComputeSpectrumDbFsFromFft(MathNet.Numerics.Complex32[] fft, double windowSum)
        {
            int N = fft.Length;
            int bins = N / 2 + 1;
            var spectrum = new double[bins];

            if (windowSum <= 0.0)
                windowSum = N;

            double scale = 2.0 / windowSum / Max24Pos;

            for (int k = 0; k < bins; k++)
            {
                double mag = fft[k].Magnitude;
                double amp = mag * scale;

                double db = (amp <= 0.0) ? -200.0 : 20.0 * Math.Log10(amp);

                if (db < -200.0) db = -200.0;
                if (db > 0.0) db = 0.0;

                spectrum[k] = db;
            }

            return spectrum;
        }

        //
        public static double[] BuildFreqAxis(int sampleRate, int fftSize)
        {
            if (sampleRate <= 0)
                return Array.Empty<double>();

            int bins = fftSize / 2 + 1;
            var axis = new double[bins];

            double df = (double)sampleRate / fftSize;
            for (int k = 0; k < bins; k++)
                axis[k] = k * df;

            return axis;
        }
        //


    }
}
