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
        public static SpectrumResult ComputeFrame( int[] leftSamples, int[] rightSamples, int fftSize, int sampleRate, SpectrumOptions options)
        {
            // 1) Construire buffers double (dernier N, zero-pad si besoin)
            double[]? bufL = (leftSamples != null && leftSamples.Length > 0) ? BuildChannelBuffer(leftSamples, fftSize) : null;
            double[]? bufR = (rightSamples != null && rightSamples.Length > 0) ? BuildChannelBuffer(rightSamples, fftSize) : null;

            // 2) Copier brut pour les stats (AVANT remove DC + fenêtre)
            double[]? rawL = (bufL != null) ? (double[])bufL.Clone() : null;
            double[]? rawR = (bufR != null) ? (double[])bufR.Clone() : null;

            // 3) Remove DC si demandé
            if (options.RemoveDc)
            {
                if (bufL != null)
                {
                    double mean = bufL.Average();
                    for (int i = 0; i < bufL.Length; i++) bufL[i] -= mean;
                }
                if (bufR != null)
                {
                    double mean = bufR.Average();
                    for (int i = 0; i < bufR.Length; i++) bufR[i] -= mean;
                }
            }

            // 4) Fenêtre + FFT par voie
            double[]? specL = null;
            double[]? specR = null;

            MathNet.Numerics.Complex32[]? fftL = null;
            MathNet.Numerics.Complex32[]? fftR = null;

            if (bufL != null)
            {
                double sumWin = ApplyWindowInPlace(bufL, options.WindowType);

                if (options.NeedComplexFft)
                {
                    fftL = ComputeComplexFft(bufL);
                    specL = ComputeSpectrumDbFsFromFft(fftL, sumWin);
                }
                else
                {
                    specL = ComputeSpectrumDbFs(bufL, sumWin);
                }
            }

            if (bufR != null)
            {
                double sumWin = ApplyWindowInPlace(bufR, options.WindowType);

                if (options.NeedComplexFft)
                {
                    fftR = ComputeComplexFft(bufR);
                    specR = ComputeSpectrumDbFsFromFft(fftR, sumWin);
                }
                else
                {
                    specR = ComputeSpectrumDbFs(bufR, sumWin);
                }
            }


            // Si une seule voie dispo : on l’utilise au moins comme Left
            if (specL == null && specR != null)
                specL = (double[])specR.Clone();

            // 5) Axe fréquence
            double[] freq = BuildFreqAxis(sampleRate, fftSize);

            return new SpectrumResult(
            fftSize,
            sampleRate,
            freq,
            specL,
            specR,
            rawL,
            rawR,
            fftL,
            fftR);

        }
        public static MathNet.Numerics.Complex32[] ComputeComplexFft(double[] timeData)
        {
            int N = timeData.Length;
            var fft = new MathNet.Numerics.Complex32[N];
            for (int i = 0; i < N; i++)
                fft[i] = new MathNet.Numerics.Complex32((float)timeData[i], 0f);

            Fourier.Forward(fft, FourierOptions.Matlab);
            return fft;
        }


    }
}
