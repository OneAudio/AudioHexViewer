using MathNet.Numerics;

namespace AudioHexViewer.Core
{
    internal sealed class SpectrumFrame
    {
        public int SampleRate { get; }
        public int FftSize { get; }
        public long AbsEnd { get; }

        // FFT complexe (RFFT) : bins [0 .. N/2], longueur = N/2+1
        public Complex32[]? LeftBins { get; }
        public Complex32[]? RightBins { get; }

        // Hz, longueur = N/2+1
        public double[] FreqAxisHz { get; }

        public SpectrumFrame(int sampleRate, int fftSize, long absEnd,
                             Complex32[]? leftBins, Complex32[]? rightBins,
                             double[] freqAxisHz)
        {
            SampleRate = sampleRate;
            FftSize = fftSize;
            AbsEnd = absEnd;
            LeftBins = leftBins;
            RightBins = rightBins;
            FreqAxisHz = freqAxisHz;
        }
    }
}
