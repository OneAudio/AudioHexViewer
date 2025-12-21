using System;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace AudioHexViewer
{
    internal static class SpectrumAnalyzer
    {
        // Tu pourras y référer depuis ailleurs si besoin
        public const int Max24Pos = 0x7FFFFF; // 24-bit full scale

        public static double[] BuildChannelBuffer(int[] src, int N)
        {
            var buffer = new double[N];

            if (src.Length >= N)
            {
                int start = src.Length - N;
                for (int i = 0; i < N; i++)
                    buffer[i] = src[start + i];
            }
            else
            {
                int offset = N - src.Length;
                for (int i = 0; i < offset; i++)
                    buffer[i] = 0.0;

                for (int i = 0; i < src.Length; i++)
                    buffer[offset + i] = src[i];
            }

            return buffer;
        }

        public static double ApplyWindowInPlace(double[] buffer, WindowType windowType)
        {
            int N = buffer.Length;
            double[] win = windowType switch
            {
                WindowType.Rectangular => Enumerable.Repeat(1.0, N).ToArray(),
                WindowType.Hann => Window.Hann(N),
                WindowType.Hamming => Window.Hamming(N),
                WindowType.BlackmanHarris => Window.BlackmanHarris(N),
                _ => Window.Hann(N),
            };

            double sumWin = 0.0;
            for (int i = 0; i < N; i++)
            {
                buffer[i] *= win[i];
                sumWin += win[i];
            }

            return sumWin;
        }

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
    }
}
