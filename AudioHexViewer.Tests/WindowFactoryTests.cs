// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

// ═══════════════════════════════════════════════════════════════════════════
// WindowFactoryTests.cs  —  Tests unitaires pour AudioHexViewer.Core.WindowFactory
//
// Couvre :
//   Rectangular, Hann, Hamming, BlackmanHarris4, BlackmanHarris7
//   Cache (même objet retourné au 2ème appel)
//   Cas limites : taille 0 (exception), taille 1
// ═══════════════════════════════════════════════════════════════════════════

using AudioHexViewer.Core;
using Xunit;

namespace AudioHexViewer.Tests
{
    public class WindowFactoryTests
    {
        private const double Tol = 1e-9;

        // ════════════════════════════════════════════════════════════════════
        // Cas limites généraux
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void Get_SizeZero_ThrowsArgumentOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => WindowFactory.Get(WindowType.Rectangular, 0));
        }

        [Fact]
        public void Get_NegativeSize_ThrowsArgumentOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => WindowFactory.Get(WindowType.Hann, -1));
        }

        [Theory]
        [InlineData(WindowType.Rectangular)]
        [InlineData(WindowType.Hann)]
        [InlineData(WindowType.Hamming)]
        [InlineData(WindowType.BlackmanHarris4)]
        [InlineData(WindowType.BlackmanHarris7)]
        public void Get_SizeOne_ReturnsSingleCoefficientOne(WindowType type)
        {
            var entry = WindowFactory.Get(type, 1);
            Assert.Single(entry.Coeffs);
            Assert.Equal(1.0, entry.Coeffs[0], Tol);
            Assert.Equal(1.0, entry.Sum, Tol);
        }

        [Theory]
        [InlineData(WindowType.Rectangular)]
        [InlineData(WindowType.Hann)]
        [InlineData(WindowType.Hamming)]
        [InlineData(WindowType.BlackmanHarris4)]
        [InlineData(WindowType.BlackmanHarris7)]
        public void Get_CoeffsLengthEqualsSize(WindowType type)
        {
            const int N = 512;
            var entry = WindowFactory.Get(type, N);
            Assert.Equal(N, entry.Coeffs.Length);
        }

        [Theory]
        [InlineData(WindowType.Rectangular)]
        [InlineData(WindowType.Hann)]
        [InlineData(WindowType.Hamming)]
        [InlineData(WindowType.BlackmanHarris4)]
        [InlineData(WindowType.BlackmanHarris7)]
        public void Get_SumMatchesManualSum(WindowType type)
        {
            const int N = 1024;
            var entry = WindowFactory.Get(type, N);
            double manual = entry.Coeffs.Sum();
            Assert.Equal(manual, entry.Sum, 6);
        }

        [Theory]
        [InlineData(WindowType.Rectangular)]
        [InlineData(WindowType.Hann)]
        [InlineData(WindowType.Hamming)]
        [InlineData(WindowType.BlackmanHarris4)]
        [InlineData(WindowType.BlackmanHarris7)]
        public void Get_CacheSameObjectOnSecondCall(WindowType type)
        {
            var e1 = WindowFactory.Get(type, 256);
            var e2 = WindowFactory.Get(type, 256);
            // Le cache doit retourner le même tableau de coefficients (même référence)
            Assert.Same(e1.Coeffs, e2.Coeffs);
        }

        [Fact]
        public void Get_DifferentSizes_DifferentCoeffsArrays()
        {
            var e1 = WindowFactory.Get(WindowType.Hann, 256);
            var e2 = WindowFactory.Get(WindowType.Hann, 512);
            Assert.NotSame(e1.Coeffs, e2.Coeffs);
            Assert.Equal(256, e1.Coeffs.Length);
            Assert.Equal(512, e2.Coeffs.Length);
        }

        [Fact]
        public void Get_DifferentTypes_DifferentCoeffsArrays()
        {
            var rect = WindowFactory.Get(WindowType.Rectangular, 256);
            var hann = WindowFactory.Get(WindowType.Hann, 256);
            Assert.NotSame(rect.Coeffs, hann.Coeffs);
        }

        // ════════════════════════════════════════════════════════════════════
        // Rectangular
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void Rectangular_AllCoeffsAreOne()
        {
            const int N = 64;
            var entry = WindowFactory.Get(WindowType.Rectangular, N);
            Assert.All(entry.Coeffs, c => Assert.Equal(1.0, c, Tol));
        }

        [Fact]
        public void Rectangular_SumEqualsN()
        {
            const int N = 64;
            var entry = WindowFactory.Get(WindowType.Rectangular, N);
            Assert.Equal((double)N, entry.Sum, Tol);
        }

        // ════════════════════════════════════════════════════════════════════
        // Hann
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void Hann_FirstAndLastCoeffAreZero()
        {
            const int N = 64;
            var coeffs = WindowFactory.Get(WindowType.Hann, N).Coeffs;
            // w(0) = 0.5*(1 - cos(0)) = 0
            Assert.Equal(0.0, coeffs[0],   Tol);
            // w(N-1) = 0.5*(1 - cos(2π)) = 0
            Assert.Equal(0.0, coeffs[N-1], Tol);
        }

        [Fact]
        public void Hann_SymmetricAroundCenter()
        {
            const int N = 64;
            var coeffs = WindowFactory.Get(WindowType.Hann, N).Coeffs;
            for (int i = 0; i < N / 2; i++)
                Assert.Equal(coeffs[i], coeffs[N - 1 - i], Tol);
        }

        [Fact]
        public void Hann_PeakAtCenter_IsOne()
        {
            // Pour N pair, le maximum est aux indices N/2-1 et N/2
            // Pour N impair, au centre exact
            const int N = 65;   // impair
            var coeffs = WindowFactory.Get(WindowType.Hann, N).Coeffs;
            int center = (N - 1) / 2;
            // w(center) = 0.5*(1 - cos(2π * center / (N-1))) = 0.5*(1 - cos(π)) = 1.0
            Assert.Equal(1.0, coeffs[center], Tol);
        }

        [Fact]
        public void Hann_AllCoeffsInRange_0_1()
        {
            var coeffs = WindowFactory.Get(WindowType.Hann, 256).Coeffs;
            Assert.All(coeffs, c => Assert.InRange(c, 0.0, 1.0));
        }

        // ════════════════════════════════════════════════════════════════════
        // Hamming
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void Hamming_FirstCoeff_IsApproximatelyPoint08()
        {
            // w(0) = 0.54 - 0.46*cos(0) = 0.54 - 0.46 = 0.08
            var coeffs = WindowFactory.Get(WindowType.Hamming, 64).Coeffs;
            Assert.Equal(0.08, coeffs[0], 4);
        }

        [Fact]
        public void Hamming_MinimumIsPositive()
        {
            // Hamming ne descend jamais à zéro (min ≈ 0.08)
            var coeffs = WindowFactory.Get(WindowType.Hamming, 256).Coeffs;
            Assert.All(coeffs, c => Assert.True(c > 0.0, $"Coefficient {c} should be > 0"));
        }

        [Fact]
        public void Hamming_SymmetricAroundCenter()
        {
            const int N = 128;
            var coeffs = WindowFactory.Get(WindowType.Hamming, N).Coeffs;
            for (int i = 0; i < N / 2; i++)
                Assert.Equal(coeffs[i], coeffs[N - 1 - i], Tol);
        }

        // ════════════════════════════════════════════════════════════════════
        // BlackmanHarris4 et BlackmanHarris7
        // ════════════════════════════════════════════════════════════════════

        [Theory]
        [InlineData(WindowType.BlackmanHarris4)]
        [InlineData(WindowType.BlackmanHarris7)]
        public void BlackmanHarris_SymmetricAroundCenter(WindowType type)
        {
            const int N = 128;
            var coeffs = WindowFactory.Get(type, N).Coeffs;
            for (int i = 0; i < N / 2; i++)
                Assert.Equal(coeffs[i], coeffs[N - 1 - i], Tol);
        }

        [Theory]
        [InlineData(WindowType.BlackmanHarris4)]
        [InlineData(WindowType.BlackmanHarris7)]
        public void BlackmanHarris_AllCoeffsPositive(WindowType type)
        {
            var coeffs = WindowFactory.Get(type, 256).Coeffs;
            Assert.All(coeffs, c => Assert.True(c >= 0.0, $"Coefficient {c} should be ≥ 0"));
        }

        [Theory]
        [InlineData(WindowType.BlackmanHarris4)]
        [InlineData(WindowType.BlackmanHarris7)]
        public void BlackmanHarris_PeakLessOrEqualOne(WindowType type)
        {
            var coeffs = WindowFactory.Get(type, 256).Coeffs;
            Assert.All(coeffs, c => Assert.True(c <= 1.0 + 1e-12, $"Coefficient {c} > 1"));
        }

        [Fact]
        public void BlackmanHarris7_FirstCoeff_MatchesKnownA0()
        {
            // w(0) = a0 - a1 + a2 - a3 + a4 - a5 + a6 ≈ très petit (proche de 0)
            // Mais plus important : w(0) >= 0 et la valeur est documentée
            const double a0 = 0.27105140069342;
            const double a1 = 0.43329793923448;
            const double a2 = 0.21812299954311;
            const double a3 = 0.06592544638803;
            const double a4 = 0.01081174209837;
            const double a5 = 0.00077658482522;
            const double a6 = 0.00001388721735;
            double expected = a0 - a1 + a2 - a3 + a4 - a5 + a6;

            var coeffs = WindowFactory.Get(WindowType.BlackmanHarris7, 256).Coeffs;
            Assert.Equal(expected, coeffs[0], 10);
        }

        [Fact]
        public void BlackmanHarris7_SumIsStrictlyLessThanN()
        {
            // La somme d'une fenêtre apodisée est toujours < N
            const int N = 1024;
            var entry = WindowFactory.Get(WindowType.BlackmanHarris7, N);
            Assert.True(entry.Sum < N, $"Sum {entry.Sum} should be < {N}");
            Assert.True(entry.Sum > 0, "Sum should be > 0");
        }
    }
}
