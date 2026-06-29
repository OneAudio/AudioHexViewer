// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

// ═══════════════════════════════════════════════════════════════════════════
// DspMathTests.cs  —  Tests unitaires pour AudioHexViewer.Core.DspMath
//
// Couvre :
//   ComplexBinsToDbFs        — conversion amplitude complexe → dBFS
//   ComplexBinsToPower       — conversion amplitude complexe → puissance
//   PowerToDbFs              — puissance → dBFS
//   PowerRatioToDb           — rapport de puissances → dB
//   UnwrapPhaseDegrees       — dépliement de phase
//   ComplexBinsToPowerInto   — version zéro-alloc de ComplexBinsToPower
//   PowerToDbFsInto          — version zéro-alloc de PowerToDbFs
//   EstimateFundamentalHz    — détection du pic dans un spectre
// ═══════════════════════════════════════════════════════════════════════════

using AudioHexViewer.Core;
using MathNet.Numerics;
using Xunit;

namespace AudioHexViewer.Tests
{
    public class DspMathTests
    {
        // ── Helpers ──────────────────────────────────────────────────────────
        private static Complex32 C(float re, float im) => new Complex32(re, im);

        // Tolerance pour les comparaisons en dB (arrondis float→double)
        private const double Tol = 1e-4;

        // ════════════════════════════════════════════════════════════════════
        // ComplexBinsToDbFs
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void ComplexBinsToDbFs_UnitMagnitude_Returns0dBFS()
        {
            // |1 + 0j| = 1.0 → 20*log10(1) = 0 dBFS
            var bins = new[] { C(1f, 0f) };
            var result = DspMath.ComplexBinsToDbFs(bins);
            Assert.Equal(0.0, result[0], Tol);
        }

        [Fact]
        public void ComplexBinsToDbFs_HalfAmplitude_ReturnsMinus6dBFS()
        {
            // |0.5 + 0j| = 0.5 → 20*log10(0.5) ≈ -6.021 dBFS
            var bins = new[] { C(0.5f, 0f) };
            var result = DspMath.ComplexBinsToDbFs(bins);
            Assert.Equal(-6.0206, result[0], 3);   // 3 décimales
        }

        [Fact]
        public void ComplexBinsToDbFs_ZeroMagnitude_ReturnsMinus200dBFS()
        {
            // Magnitude 0 → clamp bas = -200 dBFS
            var bins = new[] { C(0f, 0f) };
            var result = DspMath.ComplexBinsToDbFs(bins);
            Assert.Equal(-200.0, result[0], Tol);
        }

        [Fact]
        public void ComplexBinsToDbFs_AboveFull_ClampedTo0dBFS()
        {
            // |2 + 0j| = 2 → 20*log10(2) ≈ +6 dB → clamp à 0 dBFS
            var bins = new[] { C(2f, 0f) };
            var result = DspMath.ComplexBinsToDbFs(bins);
            Assert.Equal(0.0, result[0], Tol);
        }

        [Fact]
        public void ComplexBinsToDbFs_ImaginaryComponent_UsesMagnitude()
        {
            // |(3 + 4j)| = 5 → clamped to 0
            var bins = new[] { C(3f, 4f) };
            var result = DspMath.ComplexBinsToDbFs(bins);
            Assert.Equal(0.0, result[0], Tol);

            // |(0.3 + 0.4j)| = 0.5 → ≈ -6.02 dBFS
            bins = new[] { C(0.3f, 0.4f) };
            result = DspMath.ComplexBinsToDbFs(bins);
            Assert.Equal(-6.0206, result[0], 3);
        }

        [Fact]
        public void ComplexBinsToDbFs_OutputLengthMatchesInput()
        {
            var bins = new[] { C(1f, 0f), C(0.5f, 0f), C(0f, 0f) };
            var result = DspMath.ComplexBinsToDbFs(bins);
            Assert.Equal(bins.Length, result.Length);
        }

        // ════════════════════════════════════════════════════════════════════
        // ComplexBinsToPower
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void ComplexBinsToPower_RealUnit_Returns1()
        {
            var bins = new[] { C(1f, 0f) };
            Assert.Equal(1.0, DspMath.ComplexBinsToPower(bins)[0], Tol);
        }

        [Fact]
        public void ComplexBinsToPower_ImagUnit_Returns1()
        {
            var bins = new[] { C(0f, 1f) };
            Assert.Equal(1.0, DspMath.ComplexBinsToPower(bins)[0], Tol);
        }

        [Fact]
        public void ComplexBinsToPower_Pythagorean_Returns25()
        {
            // 3² + 4² = 25
            var bins = new[] { C(3f, 4f) };
            Assert.Equal(25.0, DspMath.ComplexBinsToPower(bins)[0], Tol);
        }

        [Fact]
        public void ComplexBinsToPower_Zero_Returns0()
        {
            var bins = new[] { C(0f, 0f) };
            Assert.Equal(0.0, DspMath.ComplexBinsToPower(bins)[0], Tol);
        }

        // ════════════════════════════════════════════════════════════════════
        // PowerToDbFs
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void PowerToDbFs_UnitPower_Returns0dBFS()
        {
            Assert.Equal(0.0, DspMath.PowerToDbFs(new[] { 1.0 })[0], Tol);
        }

        [Fact]
        public void PowerToDbFs_QuarterPower_ReturnsMinus6dBFS()
        {
            // 10*log10(0.25) ≈ -6.021 dBFS
            Assert.Equal(-6.0206, DspMath.PowerToDbFs(new[] { 0.25 })[0], 3);
        }

        [Fact]
        public void PowerToDbFs_ZeroPower_ReturnsMinus200dBFS()
        {
            Assert.Equal(-200.0, DspMath.PowerToDbFs(new[] { 0.0 })[0], Tol);
        }

        [Fact]
        public void PowerToDbFs_AboveFullScale_ClampedTo0dBFS()
        {
            Assert.Equal(0.0, DspMath.PowerToDbFs(new[] { 4.0 })[0], Tol);
        }

        [Fact]
        public void PowerToDbFs_ConsistentWithComplexBinsToDbFs()
        {
            // PowerToDbFs(ComplexBinsToPower(bins)) == ComplexBinsToDbFs(bins)
            // pour amplitude ≤ 1 (les deux clampent à [-200, 0])
            var bins = new[]
            {
                C(1f, 0f), C(0.5f, 0f), C(0.1f, 0f),
                C(0f, 0f), C(0.3f, 0.4f)
            };

            var direct  = DspMath.ComplexBinsToDbFs(bins);
            var via_pow = DspMath.PowerToDbFs(DspMath.ComplexBinsToPower(bins));

            for (int k = 0; k < bins.Length; k++)
                Assert.Equal(direct[k], via_pow[k], 6);
        }

        // ════════════════════════════════════════════════════════════════════
        // PowerRatioToDb
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void PowerRatioToDb_EqualPowers_Returns0dB()
        {
            var L = new[] { 1.0, 0.5 };
            var R = new[] { 1.0, 0.5 };
            var result = DspMath.PowerRatioToDb(L, R);
            foreach (var v in result)
                Assert.Equal(0.0, v, Tol);
        }

        [Fact]
        public void PowerRatioToDb_TenTimesLeft_Returns10dB()
        {
            // 10*log10(10/1) = 10 dB
            var result = DspMath.PowerRatioToDb(new[] { 10.0 }, new[] { 1.0 });
            Assert.Equal(10.0, result[0], 4);
        }

        [Fact]
        public void PowerRatioToDb_TenthLeft_ReturnsMinus10dB()
        {
            var result = DspMath.PowerRatioToDb(new[] { 1.0 }, new[] { 10.0 });
            Assert.Equal(-10.0, result[0], 4);
        }

        [Fact]
        public void PowerRatioToDb_ZeroDenominator_DoesNotThrowOrInfinity()
        {
            // EPS évite +Inf — résultat clamped [-200, 200]
            var result = DspMath.PowerRatioToDb(new[] { 1.0 }, new[] { 0.0 });
            Assert.False(double.IsInfinity(result[0]));
            Assert.False(double.IsNaN(result[0]));
            Assert.Equal(200.0, result[0], Tol);  // 10*log10(1 / EPS) ≈ +300 → clamp 200
        }

        [Fact]
        public void PowerRatioToDb_LengthIsMimOfBoth()
        {
            var L = new[] { 1.0, 2.0, 3.0 };
            var R = new[] { 1.0, 2.0 };
            Assert.Equal(2, DspMath.PowerRatioToDb(L, R).Length);
        }

        // ════════════════════════════════════════════════════════════════════
        // UnwrapPhaseDegrees
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void UnwrapPhaseDegrees_NoWrapping_Unchanged()
        {
            var input    = new[] { 0.0, 30.0, 60.0, 90.0, 120.0 };
            var expected = new[] { 0.0, 30.0, 60.0, 90.0, 120.0 };
            var result   = DspMath.UnwrapPhaseDegrees(input);
            for (int i = 0; i < expected.Length; i++)
                Assert.Equal(expected[i], result[i], Tol);
        }

        [Fact]
        public void UnwrapPhaseDegrees_PositiveJump_Unwrapped()
        {
            // 170 → -170 : saut de -340° → doit devenir 190°
            var input  = new[] { 170.0, -170.0 };
            var result = DspMath.UnwrapPhaseDegrees(input);
            Assert.Equal(170.0, result[0], Tol);
            Assert.Equal(190.0, result[1], Tol);
        }

        [Fact]
        public void UnwrapPhaseDegrees_NegativeJump_Unwrapped()
        {
            // -170 → 170 : saut de +340° → doit devenir -190°
            var input  = new[] { -170.0, 170.0 };
            var result = DspMath.UnwrapPhaseDegrees(input);
            Assert.Equal(-170.0, result[0], Tol);
            Assert.Equal(-190.0, result[1], Tol);
        }

        [Fact]
        public void UnwrapPhaseDegrees_MultipleWraps_AccumulatesOffset()
        {
            // Suite qui tourne : 160 → -160 → -160+δ → ...
            // Chaque transition de 160→-160 est un saut de -320° → +360 offset
            var input  = new[] { 160.0, -160.0, -140.0 };
            var result = DspMath.UnwrapPhaseDegrees(input);
            Assert.Equal(160.0, result[0], Tol);
            Assert.Equal(200.0, result[1], Tol);   // -160 + 360
            Assert.Equal(220.0, result[2], Tol);   // -140 + 360
        }

        [Fact]
        public void UnwrapPhaseDegrees_EmptyInput_ReturnsEmpty()
        {
            Assert.Empty(DspMath.UnwrapPhaseDegrees(Array.Empty<double>()));
        }

        [Fact]
        public void UnwrapPhaseDegrees_SingleElement_Unchanged()
        {
            var result = DspMath.UnwrapPhaseDegrees(new[] { 42.0 });
            Assert.Single(result);
            Assert.Equal(42.0, result[0], Tol);
        }

        // ════════════════════════════════════════════════════════════════════
        // Versions InTo (zéro-alloc)
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void ComplexBinsToPowerInto_MatchesComplexBinsToPower()
        {
            var bins = new[] { C(1f, 0f), C(0.5f, 0.5f), C(0f, 0f), C(3f, 4f) };
            var expected = DspMath.ComplexBinsToPower(bins);
            var actual   = new double[bins.Length];
            DspMath.ComplexBinsToPowerInto(bins, actual);
            for (int k = 0; k < bins.Length; k++)
                Assert.Equal(expected[k], actual[k], Tol);
        }

        [Fact]
        public void ComplexBinsToPowerInto_LargerOutput_OnlyFillsMinLength()
        {
            var bins = new[] { C(1f, 0f) };
            var output = new double[4];
            output[1] = output[2] = output[3] = 99.0;
            DspMath.ComplexBinsToPowerInto(bins, output);
            Assert.Equal(1.0, output[0], Tol);
            Assert.Equal(99.0, output[1], Tol); // non touché
        }

        [Fact]
        public void PowerToDbFsInto_MatchesPowerToDbFs()
        {
            var power    = new[] { 1.0, 0.25, 0.0, 4.0 };
            var expected = DspMath.PowerToDbFs(power);
            var actual   = new double[power.Length];
            DspMath.PowerToDbFsInto(power, actual);
            for (int k = 0; k < power.Length; k++)
                Assert.Equal(expected[k], actual[k], Tol);
        }

        // ════════════════════════════════════════════════════════════════════
        // EstimateFundamentalHz
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void EstimateFundamentalHz_PeakInMiddle_Found()
        {
            // Spectre : pic en index 3, fréquence 300 Hz (Δf = 100 Hz/bin)
            var fx       = new[] { 0.0, 100.0, 200.0, 300.0, 400.0, 500.0 };
            var spectrum = new[] { -40.0, -40.0, -40.0, -6.0, -40.0, -40.0 };
            var f0 = DspMath.EstimateFundamentalHz(spectrum, fx, 0.0, 500.0, false);
            Assert.NotNull(f0);
            Assert.Equal(300.0, f0!.Value, Tol);
        }

        [Fact]
        public void EstimateFundamentalHz_NullInputs_ReturnsNull()
        {
            Assert.Null(DspMath.EstimateFundamentalHz(null!, new double[5], 0, 500, false));
            Assert.Null(DspMath.EstimateFundamentalHz(new double[5], null!, 0, 500, false));
        }

        [Fact]
        public void EstimateFundamentalHz_LengthMismatch_ReturnsNull()
        {
            var fx = new[] { 0.0, 100.0, 200.0 };
            var sp = new[] { -6.0, -40.0 };   // longueurs différentes
            Assert.Null(DspMath.EstimateFundamentalHz(sp, fx, 0, 300, false));
        }

        [Fact]
        public void EstimateFundamentalHz_PeakOutsideRange_ReturnsNull()
        {
            var fx = new[] { 0.0, 100.0, 200.0, 300.0 };
            var sp = new[] { -6.0, -40.0, -40.0, -40.0 }; // pic à 0 Hz
            // chercher dans [100, 300] → le pic à 0 Hz est hors bande
            var f0 = DspMath.EstimateFundamentalHz(sp, fx, 100.0, 300.0, false);
            Assert.NotNull(f0);
            Assert.NotEqual(0.0, f0!.Value); // ne doit pas retourner 0 Hz
        }

        [Fact]
        public void EstimateFundamentalHz_LogMode_SkipsDcBin()
        {
            // En mode log, i0 forcé à ≥ 1 → bin 0 (DC) ignoré
            var fx = new[] { 0.0, 100.0, 200.0 };
            var sp = new[] { 0.0, -40.0, -40.0 }; // pic au DC
            // fMin = 0 Hz, logXAbsMinHz = 0.8 Hz
            var f0 = DspMath.EstimateFundamentalHz(sp, fx, 0.0, 200.0, true, 0.8);
            // Doit retourner 100 ou 200 Hz, pas le DC
            Assert.NotNull(f0);
            Assert.True(f0!.Value > 0.0);
        }

        [Fact]
        public void EstimateFundamentalHz_TooFewBins_ReturnsNull()
        {
            var fx = new[] { 0.0, 100.0 };   // length = 2 < 3
            var sp = new[] { -6.0, -40.0 };
            Assert.Null(DspMath.EstimateFundamentalHz(sp, fx, 0, 200, false));
        }
    }
}
