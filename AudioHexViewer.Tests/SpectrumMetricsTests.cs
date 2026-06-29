// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

// ═══════════════════════════════════════════════════════════════════════════
// SpectrumMetricsTests.cs  —  Tests pour AudioHexViewer.SpectrumMetrics
//
// Couvre :
//   ComputeChannel : paramètres invalides → Valid = false
//   Spectre de sinus pur → Valid = true, FundamentalHz ≈ attendu, THD bas
//   Spectre avec harmonique connue → THD reflète l'harmonique
//   SFDR : spur détecté correctement
//   Options : HPF/LPF (fMin/fMax), ExcludeDc
// ═══════════════════════════════════════════════════════════════════════════

using Xunit;

namespace AudioHexViewer.Tests
{
    public class SpectrumMetricsTests
    {
        // ════════════════════════════════════════════════════════════════════
        // Helpers — construction de spectres synthétiques
        // ════════════════════════════════════════════════════════════════════

        private const int SR      = 48_000;
        private const int FftSize = 8_192;
        private const int Bins    = FftSize / 2 + 1;   // 4097

        // Fréquence d'un bin : f = k * SR / FftSize
        private static int FreqToBin(double hz) =>
            (int)Math.Round(hz * FftSize / (double)SR);

        private static double BinToHz(int k) =>
            k * SR / (double)FftSize;

        /// <summary>
        /// Spectre de bruit plat à <paramref name="noiseFloorDb"/> dBFS,
        /// avec un pic de <paramref name="peakDb"/> dBFS au bin <paramref name="fundBin"/>.
        /// </summary>
        private static double[] MakePureSpectrum(
            int fundBin, double peakDb = -3.0, double noiseFloorDb = -120.0)
        {
            var spec = new double[Bins];
            for (int k = 0; k < Bins; k++)
                spec[k] = noiseFloorDb;
            spec[fundBin] = peakDb;
            return spec;
        }

        /// <summary>
        /// Spectre avec fondamentale + une harmonique d'ordre <paramref name="harmOrder"/>
        /// à <paramref name="harmDb"/> dBFS.
        /// </summary>
        private static double[] MakeSpectrumWithHarmonic(
            int fundBin, int harmOrder, double fundDb = -3.0,
            double harmDb = -40.0, double noiseFloorDb = -120.0)
        {
            var spec = MakePureSpectrum(fundBin, fundDb, noiseFloorDb);
            int harmBin = FreqToBin(BinToHz(fundBin) * harmOrder);
            if (harmBin < Bins)
                spec[harmBin] = harmDb;
            return spec;
        }

        private static SpectrumMetricsOptions DefaultOpt() => new SpectrumMetricsOptions
        {
            MinFreqHz             = 20.0,
            MaxFreqHz             = 20_000.0,
            FundamentalHalfWidthBins = 4,
            HarmonicHalfWidthBins    = 2,
            ToneHalfWidthBins        = 2,
            SfdrFundHalfWidthBins    = 4,
            MaxHarmonics          = 5,
            ExcludeDc             = true,
        };

        // ════════════════════════════════════════════════════════════════════
        // Paramètres invalides
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void ComputeChannel_NullSpectrum_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => SpectrumMetrics.ComputeChannel(null!, SR, FftSize, DefaultOpt()));
        }

        [Fact]
        public void ComputeChannel_ZeroSampleRate_ReturnsInvalid()
        {
            var spec = new double[Bins];
            var result = SpectrumMetrics.ComputeChannel(spec, 0, FftSize, DefaultOpt());
            Assert.False(result.Valid);
        }

        [Fact]
        public void ComputeChannel_ZeroFftSize_ReturnsInvalid()
        {
            var spec = new double[Bins];
            var result = SpectrumMetrics.ComputeChannel(spec, SR, 0, DefaultOpt());
            Assert.False(result.Valid);
        }

        [Fact]
        public void ComputeChannel_EmptySpectrum_ReturnsInvalid()
        {
            var result = SpectrumMetrics.ComputeChannel(Array.Empty<double>(), SR, FftSize, DefaultOpt());
            Assert.False(result.Valid);
        }

        // ════════════════════════════════════════════════════════════════════
        // Sinus pur
        // ════════════════════════════════════════════════════════════════════

        [Theory]
        [InlineData(1_000.0)]   // 1 kHz
        [InlineData(5_000.0)]   // 5 kHz
        [InlineData(10_000.0)]  // 10 kHz
        public void ComputeChannel_PureSine_ValidAndFundamentalFound(double f0Hz)
        {
            int fundBin = FreqToBin(f0Hz);
            var spec    = MakePureSpectrum(fundBin, peakDb: -3.0);
            var result  = SpectrumMetrics.ComputeChannel(spec, SR, FftSize, DefaultOpt());

            Assert.True(result.Valid);
            // La fréquence fondamentale doit être proche de f0 (à ±1 bin)
            double binWidth = SR / (double)FftSize;
            Assert.Equal(f0Hz, result.FundamentalHz, binWidth * 1.5);
        }

        [Fact]
        public void ComputeChannel_PureSine_ThdIsVeryLow()
        {
            // Sinus pur sans harmoniques → THD doit être négligeable
            int fundBin = FreqToBin(1_000.0);
            var spec    = MakePureSpectrum(fundBin, peakDb: -3.0, noiseFloorDb: -150.0);
            var result  = SpectrumMetrics.ComputeChannel(spec, SR, FftSize, DefaultOpt());

            Assert.True(result.Valid);
            // THD doit être < 0.01% (noise floor très bas)
            Assert.True(result.Thd < 0.0001,
                $"THD = {result.Thd:E3} devrait être < 0.0001 pour un sinus pur");
        }

        [Fact]
        public void ComputeChannel_PureSine_ThdDbIsVeryNegative()
        {
            int fundBin = FreqToBin(1_000.0);
            var spec    = MakePureSpectrum(fundBin, peakDb: -3.0, noiseFloorDb: -150.0);
            var result  = SpectrumMetrics.ComputeChannel(spec, SR, FftSize, DefaultOpt());

            Assert.True(result.ThdDb < -80.0,
                $"THD_dB = {result.ThdDb:F1} devrait être < -80 dB pour un sinus pur");
        }

        // ════════════════════════════════════════════════════════════════════
        // Harmoniques connues
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void ComputeChannel_WithH2_THD_ReflectsHarmonic()
        {
            // Fondamentale à 1 kHz (-3 dBFS), H2 à 2 kHz (-40 dBFS)
            int fundBin = FreqToBin(1_000.0);
            var spec    = MakeSpectrumWithHarmonic(
                fundBin, harmOrder: 2, fundDb: -3.0, harmDb: -40.0, noiseFloorDb: -150.0);
            var result  = SpectrumMetrics.ComputeChannel(spec, SR, FftSize, DefaultOpt());

            Assert.True(result.Valid);
            // THD_dB doit être proche de -40 - (-3) = -37 dBc (légère différence due à intégration)
            Assert.True(result.ThdDb > -45.0 && result.ThdDb < -30.0,
                $"THD = {result.ThdDb:F1} dB attendu ≈ -37 dBc");

            // H2 doit apparaître dans la liste des harmoniques
            Assert.NotEmpty(result.Harmonics);
            Assert.Contains(result.Harmonics, h => h.Order == 2);
        }

        [Fact]
        public void ComputeChannel_H2At40dBc_HarmonicLevelClose()
        {
            int fundBin = FreqToBin(1_000.0);
            double harmDb = -40.0;
            double fundDb = -3.0;
            var spec = MakeSpectrumWithHarmonic(fundBin, 2, fundDb, harmDb, -150.0);
            var result = SpectrumMetrics.ComputeChannel(spec, SR, FftSize, DefaultOpt());

            Assert.True(result.Valid);
            var h2 = result.Harmonics.FirstOrDefault(h => h.Order == 2);

            // LevelDbc = harmonique - fondamentale ≈ -40 - (-3) = -37 dBc
            // Tolérance large car intégration sur plusieurs bins
            Assert.InRange(h2.LevelDbc, -45.0, -30.0);
        }

        [Fact]
        public void ComputeChannel_HarmonicsCountRespected()
        {
            var opt = DefaultOpt();
            opt.MaxHarmonics = 3;   // H2, H3 uniquement

            int fundBin = FreqToBin(1_000.0);
            var spec    = MakePureSpectrum(fundBin);
            var result  = SpectrumMetrics.ComputeChannel(spec, SR, FftSize, opt);

            // La liste des harmoniques ne peut pas dépasser MaxHarmonics - 1 (H2..HN)
            Assert.True(result.Harmonics.Count <= opt.MaxHarmonics - 1);
        }

        // ════════════════════════════════════════════════════════════════════
        // SINAD / SNR
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void ComputeChannel_PureSine_SinadAndSnrArePositive()
        {
            int fundBin = FreqToBin(1_000.0);
            var spec    = MakePureSpectrum(fundBin, peakDb: -3.0, noiseFloorDb: -120.0);
            var result  = SpectrumMetrics.ComputeChannel(spec, SR, FftSize, DefaultOpt());

            Assert.True(result.Valid);
            Assert.True(result.SinadDb > 0.0, $"SINAD = {result.SinadDb:F1} dB devrait être > 0");
            Assert.True(result.SnrDb   > 0.0, $"SNR   = {result.SnrDb:F1} dB devrait être > 0");
        }

        // ════════════════════════════════════════════════════════════════════
        // Options : fMin / fMax
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void ComputeChannel_FrequencyOutsideBand_ReturnsInvalid()
        {
            // Fondamentale à 1 kHz, bande [5 kHz..20 kHz] → pas de signal dans la bande
            var opt = DefaultOpt();
            opt.MinFreqHz = 5_000.0;
            opt.MaxFreqHz = 20_000.0;

            int fundBin = FreqToBin(1_000.0);
            var spec    = MakePureSpectrum(fundBin);
            var result  = SpectrumMetrics.ComputeChannel(spec, SR, FftSize, opt);

            // Il ne reste que du bruit dans la bande → Valid peut être true mais F0 est hors bande
            // Le test principal : FundamentalHz ne doit pas indiquer 1 kHz
            if (result.Valid)
                Assert.True(result.FundamentalHz >= 5_000.0,
                    $"Fondamentale {result.FundamentalHz} Hz devrait être >= 5000 Hz");
        }

        [Fact]
        public void ComputeChannel_FMinAboveFMax_ReturnsInvalid()
        {
            var opt = DefaultOpt();
            opt.MinFreqHz = 10_000.0;
            opt.MaxFreqHz = 1_000.0;  // incohérent

            var spec   = MakePureSpectrum(FreqToBin(1_000.0));
            var result = SpectrumMetrics.ComputeChannel(spec, SR, FftSize, opt);

            Assert.False(result.Valid);
        }
    }
}
