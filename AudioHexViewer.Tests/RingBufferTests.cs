// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

// ═══════════════════════════════════════════════════════════════════════════
// RingBufferTests.cs  —  Tests unitaires pour AudioHexViewer.Core.RingBufferStereoFloat
//
// Couvre :
//   Construction (capacité invalide)
//   PushBlock + TryReadWindow (round-trip, valeurs exactes)
//   Wraparound (push > capacité)
//   TryReadWindow : données pas encore disponibles → false
//   TryReadWindow : données écrasées → false
//   AbsWrite incrémente correctement
//   LockStats (reset, non-négatif)
//   Accès concurrent basique (push + lecture simultanés)
// ═══════════════════════════════════════════════════════════════════════════

using AudioHexViewer.Core;
using Xunit;

namespace AudioHexViewer.Tests
{
    public class RingBufferTests
    {
        private const double Tol = 1e-6;

        // ════════════════════════════════════════════════════════════════════
        // Construction
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void Constructor_ZeroCapacity_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RingBufferStereoFloat(0));
        }

        [Fact]
        public void Constructor_NegativeCapacity_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RingBufferStereoFloat(-1));
        }

        [Fact]
        public void Constructor_ValidCapacity_AbsWriteIsZero()
        {
            var ring = new RingBufferStereoFloat(100);
            Assert.Equal(0L, ring.AbsWrite);
        }

        [Fact]
        public void Constructor_ValidCapacity_CapacityReturned()
        {
            var ring = new RingBufferStereoFloat(256);
            Assert.Equal(256, ring.Capacity);
        }

        // ════════════════════════════════════════════════════════════════════
        // PushBlock + AbsWrite
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void PushBlock_IncrementsAbsWrite()
        {
            var ring = new RingBufferStereoFloat(100);
            ring.PushBlock(new float[] { 1f, 2f, 3f }, new float[] { 4f, 5f, 6f });
            Assert.Equal(3L, ring.AbsWrite);
        }

        [Fact]
        public void PushBlock_LengthMismatch_Throws()
        {
            var ring = new RingBufferStereoFloat(100);
            Assert.Throws<ArgumentException>(
                () => ring.PushBlock(new float[] { 1f, 2f }, new float[] { 3f }));
        }

        [Fact]
        public void PushBlock_Empty_NoEffect()
        {
            var ring = new RingBufferStereoFloat(100);
            ring.PushBlock(Array.Empty<float>(), Array.Empty<float>());
            Assert.Equal(0L, ring.AbsWrite);
        }

        // ════════════════════════════════════════════════════════════════════
        // TryReadWindow — cas normaux
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void TryReadWindow_EmptyRing_ReturnsFalse()
        {
            var ring = new RingBufferStereoFloat(100);
            var outL = new float[4];
            var outR = new float[4];
            Assert.False(ring.TryReadWindow(0, 4, outL, outR));
        }

        [Fact]
        public void TryReadWindow_DataAvailable_ReturnsTrueAndCorrectValues()
        {
            var ring = new RingBufferStereoFloat(100);
            float[] srcL = { 1f, 2f, 3f, 4f };
            float[] srcR = { 5f, 6f, 7f, 8f };
            ring.PushBlock(srcL, srcR);

            var outL = new float[4];
            var outR = new float[4];
            bool ok = ring.TryReadWindow(0, 4, outL, outR);

            Assert.True(ok);
            Assert.Equal(srcL, outL);
            Assert.Equal(srcR, outR);
        }

        [Fact]
        public void TryReadWindow_PartialRead_ReturnsCorrectSlice()
        {
            var ring = new RingBufferStereoFloat(100);
            ring.PushBlock(new float[] { 1f, 2f, 3f, 4f, 5f },
                           new float[] { 10f, 20f, 30f, 40f, 50f });

            // Lire les indices absolus 1..3 (length=3)
            var outL = new float[3];
            var outR = new float[3];
            bool ok = ring.TryReadWindow(1, 3, outL, outR);

            Assert.True(ok);
            Assert.Equal(new float[] { 2f, 3f, 4f }, outL);
            Assert.Equal(new float[] { 20f, 30f, 40f }, outR);
        }

        [Fact]
        public void TryReadWindow_FuturePosition_ReturnsFalse()
        {
            var ring = new RingBufferStereoFloat(100);
            ring.PushBlock(new float[] { 1f, 2f, 3f }, new float[] { 4f, 5f, 6f });

            // Demander absStart=2, length=4 → absEnd=6 > absWrite=3
            var outL = new float[4];
            var outR = new float[4];
            Assert.False(ring.TryReadWindow(2, 4, outL, outR));
        }

        [Fact]
        public void TryReadWindow_InvalidArgs_Throws()
        {
            var ring = new RingBufferStereoFloat(100);
            ring.PushBlock(new float[10], new float[10]);

            // length <= 0
            Assert.Throws<ArgumentOutOfRangeException>(
                () => ring.TryReadWindow(0, 0, new float[1], new float[1]));

            // output trop petit
            Assert.Throws<ArgumentException>(
                () => ring.TryReadWindow(0, 5, new float[3], new float[5]));
        }

        // ════════════════════════════════════════════════════════════════════
        // Wraparound
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void TryReadWindow_Wraparound_ReadsCorrectValues()
        {
            // Capacité = 8, on va remplir 10 samples pour forcer un wrap
            const int Cap = 8;
            var ring = new RingBufferStereoFloat(Cap);

            // Remplissage initial : 6 samples (0..5)
            float[] firstL  = { 0f, 1f, 2f, 3f, 4f, 5f };
            float[] firstR  = { 10f, 11f, 12f, 13f, 14f, 15f };
            ring.PushBlock(firstL, firstR);

            // 4 nouveaux samples (6..9) → provoque un wrap dans le ring
            float[] secondL = { 6f, 7f, 8f, 9f };
            float[] secondR = { 16f, 17f, 18f, 19f };
            ring.PushBlock(secondL, secondR);

            // absWrite = 10; les 8 derniers samples disponibles = indices abs [2..9]
            // Lire [2..9] = firstL[2..5] + secondL[0..3]
            var outL = new float[8];
            var outR = new float[8];
            bool ok = ring.TryReadWindow(2, 8, outL, outR);

            Assert.True(ok);
            Assert.Equal(new float[] { 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f },  outL);
            Assert.Equal(new float[] { 12f, 13f, 14f, 15f, 16f, 17f, 18f, 19f }, outR);
        }

        [Fact]
        public void TryReadWindow_OverwrittenData_ReturnsFalse()
        {
            // Capacité = 4 ; écrire 10 samples en plusieurs blocs (contrainte : n ≤ capacité par push)
            // pour que les premiers samples soient écrasés par le wraparound.
            const int Cap = 4;
            var ring = new RingBufferStereoFloat(Cap);

            // 10 samples = bloc de 4 + bloc de 4 + bloc de 2
            ring.PushBlock(new float[] { 1f, 2f, 3f, 4f }, new float[4]);
            ring.PushBlock(new float[] { 5f, 6f, 7f, 8f }, new float[4]);
            ring.PushBlock(new float[] { 9f, 10f },        new float[2]);

            // absWrite = 10 ; zone disponible = [6..9] (derniers Cap=4 samples)
            // absStart = 0 (écrasé) → false
            Assert.False(ring.TryReadWindow(0, 2, new float[2], new float[2]));

            // absStart = 5 (aussi écrasé) → false
            Assert.False(ring.TryReadWindow(5, 2, new float[2], new float[2]));

            // absStart = 6 (dans la fenêtre disponible) → true
            Assert.True(ring.TryReadWindow(6, 4, new float[4], new float[4]));
        }

        [Fact]
        public void TryReadWindow_ExactCapacityFill_AllSamplesAvailable()
        {
            const int N = 16;
            var ring = new RingBufferStereoFloat(N);
            var srcL = Enumerable.Range(0, N).Select(i => (float)i).ToArray();
            var srcR = Enumerable.Range(N, N).Select(i => (float)i).ToArray();
            ring.PushBlock(srcL, srcR);

            var outL = new float[N];
            var outR = new float[N];
            Assert.True(ring.TryReadWindow(0, N, outL, outR));
            Assert.Equal(srcL, outL);
            Assert.Equal(srcR, outR);
        }

        // ════════════════════════════════════════════════════════════════════
        // LockStats
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void LockStats_InitialValuesAreZero()
        {
            var ring = new RingBufferStereoFloat(100);
            Assert.Equal(0.0, ring.LockWaitMaxMs,  1e-9);
            Assert.Equal(0.0, ring.LockWaitAvgMs,  1e-9);
            Assert.Equal(0L,  ring.LockWaitCount);
        }

        [Fact]
        public void LockStats_AfterPush_CountIncremented()
        {
            var ring = new RingBufferStereoFloat(100);
            ring.PushBlock(new float[10], new float[10]);
            Assert.True(ring.LockWaitCount >= 1);
        }

        [Fact]
        public void LockStats_AfterReset_CountIsZero()
        {
            var ring = new RingBufferStereoFloat(100);
            ring.PushBlock(new float[10], new float[10]);
            ring.ResetLockStats();
            Assert.Equal(0L, ring.LockWaitCount);
            Assert.Equal(0.0, ring.LockWaitMaxMs, 1e-9);
        }

        [Fact]
        public void LockStats_MaxIsNonNegative()
        {
            var ring = new RingBufferStereoFloat(100);
            ring.PushBlock(new float[100], new float[100]);
            Assert.True(ring.LockWaitMaxMs >= 0.0);
            Assert.True(ring.LockWaitAvgMs >= 0.0);
        }

        // ════════════════════════════════════════════════════════════════════
        // Accès concurrent
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task ConcurrentPushAndRead_NoException()
        {
            // Test de robustesse : un thread écrit, un autre lit en parallèle.
            // On vérifie l'absence d'exception et la cohérence de AbsWrite.
            const int Cap    = 1024;
            const int Pushes = 500;
            const int BlockSz = 64;

            var ring = new RingBufferStereoFloat(Cap);
            var srcL = new float[BlockSz];
            var srcR = new float[BlockSz];
            for (int i = 0; i < BlockSz; i++) { srcL[i] = i; srcR[i] = i * 2; }

            var outL = new float[BlockSz];
            var outR = new float[BlockSz];
            var errors = new System.Collections.Concurrent.ConcurrentBag<string>();

            var writer = Task.Run(() =>
            {
                for (int i = 0; i < Pushes; i++)
                    ring.PushBlock(srcL, srcR);
            });

            var reader = Task.Run(() =>
            {
                for (int i = 0; i < Pushes; i++)
                {
                    long w = ring.AbsWrite;
                    if (w >= BlockSz)
                    {
                        try { ring.TryReadWindow(w - BlockSz, BlockSz, outL, outR); }
                        catch (Exception ex) { errors.Add(ex.Message); }
                    }
                    Thread.SpinWait(10);
                }
            });

            await Task.WhenAll(writer, reader);

            Assert.Empty(errors);
            Assert.Equal((long)Pushes * BlockSz, ring.AbsWrite);
        }
    }
}
