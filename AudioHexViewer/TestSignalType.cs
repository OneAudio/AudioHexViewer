// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

namespace AudioHexViewer.Core
{
    /// <summary>Test signal types available in the Test Signal Generator dialog.</summary>
    internal enum TestSignalType
    {
        SinePure1kHz      = 0,
        SineMultiHarmonic = 1,
        DcPure            = 2,
        SquareWave1kHz    = 3,
        TwoToneIMD        = 4,
        GaussianNoise     = 5,
        Sawtooth1kHz      = 6,
        QuasiFullScale    = 7,
        LoopbackLR        = 8,
        Silence           = 9,
        GaussianNoiseLow  = 10,   // -80 dBFS RMS
    }

    internal static class TestSignalInfo
    {
        public static string GetDisplayName(TestSignalType t) => t switch
        {
            TestSignalType.SinePure1kHz      => "Pure Sine 1 kHz  (−12 dBFS)",
            TestSignalType.SineMultiHarmonic  => "Multi-Harmonic Reference  (THD / SFDR)",
            TestSignalType.DcPure             => "Pure DC  (−6 dBFS)",
            TestSignalType.SquareWave1kHz     => "Square Wave 1 kHz  (THD ≈ 48 %)",
            TestSignalType.TwoToneIMD         => "Two-Tone IMD  (19 kHz + 20 kHz)",
            TestSignalType.GaussianNoise      => "Gaussian White Noise  (−9 dBFS RMS)",
            TestSignalType.Sawtooth1kHz       => "Sawtooth 1 kHz  (all harmonics)",
            TestSignalType.QuasiFullScale     => "Quasi Full-Scale Sine  (−0.5 dBFS)",
            TestSignalType.LoopbackLR         => "Loopback L = R  (Bode baseline)",
            TestSignalType.Silence            => "Silence  (digital zeros)",
            TestSignalType.GaussianNoiseLow  => "Gaussian White Noise  (−80 dBFS RMS)",
            _                                 => t.ToString(),
        };
    }
}
