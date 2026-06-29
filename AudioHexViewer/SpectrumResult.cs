// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

using MathNet.Numerics;

namespace AudioHexViewer
{
    internal sealed class SpectrumResult
    {
        public int FftSize { get; }
        public int SampleRate { get; }
        public double[] FreqAxisHz { get; }

        public double[]? LeftDbFs { get; }
        public double[]? RightDbFs { get; }
        public Complex32[]? LeftFft { get; }
        public Complex32[]? RightFft { get; }

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
            double[]? rightTimeRaw,
            Complex32[]? leftFft,
            Complex32[]? rightFft
            )
        {
            FftSize = fftSize;
            SampleRate = sampleRate;
            FreqAxisHz = freqAxisHz;

            LeftDbFs = leftDbFs;
            RightDbFs = rightDbFs;

            LeftTimeRaw = leftTimeRaw;
            RightTimeRaw = rightTimeRaw;

            LeftFft = leftFft;
            RightFft = rightFft;
        }
    }
}
