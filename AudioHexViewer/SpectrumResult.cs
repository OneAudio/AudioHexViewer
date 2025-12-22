namespace AudioHexViewer
{
    internal sealed class SpectrumResult
    {
        public int FftSize { get; }
        public int SampleRate { get; }
        public double[] FreqAxisHz { get; }

        public double[]? LeftDbFs { get; }
        public double[]? RightDbFs { get; }

        // Buffers temporels bruts (avant Remove DC + fenêtre) pour tes stats
        public double[]? LeftTimeRaw { get; }
        public double[]? RightTimeRaw { get; }

        public SpectrumResult(
            int fftSize,
            int sampleRate,
            double[] freqAxisHz,
            double[]? leftDbFs,
            double[]? rightDbFs,
            double[]? leftTimeRaw,
            double[]? rightTimeRaw)
        {
            FftSize = fftSize;
            SampleRate = sampleRate;
            FreqAxisHz = freqAxisHz;

            LeftDbFs = leftDbFs;
            RightDbFs = rightDbFs;

            LeftTimeRaw = leftTimeRaw;
            RightTimeRaw = rightTimeRaw;
        }
    }
}
