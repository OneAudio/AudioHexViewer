namespace AudioHexViewer.Core
{
    internal sealed class SpectrumFrame
    {
        public int SampleRate { get; }
        public int FftSize { get; }
        public long AbsEnd { get; }

        // dBFS, longueur = N/2+1
        public double[]? LeftDbFs { get; }
        public double[]? RightDbFs { get; }

        // Hz, longueur = N/2+1
        public double[] FreqAxisHz { get; }

        public SpectrumFrame(int sampleRate, int fftSize, long absEnd,
                             double[]? leftDbFs, double[]? rightDbFs,
                             double[] freqAxisHz)
        {
            SampleRate = sampleRate;
            FftSize = fftSize;
            AbsEnd = absEnd;
            LeftDbFs = leftDbFs;
            RightDbFs = rightDbFs;
            FreqAxisHz = freqAxisHz;
        }
    }
}
