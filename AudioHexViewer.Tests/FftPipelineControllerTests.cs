// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

// ═══════════════════════════════════════════════════════════════════════════
// FftPipelineControllerTests.cs  —  Tests pour AudioHexViewer.Core.FftPipelineController
//
// Couvre :
//   Cycle de vie : Start / Stop / Dispose / double-Stop sans exception
//   Ring créé après Start, null avant / après Stop
//   PushAudio → AbsWrite augmente
//   TrySnapshotAsInt24 : avant données → false ; après push → true + valeurs
//   IsRunning reflect l'état réel
// ═══════════════════════════════════════════════════════════════════════════

using AudioHexViewer.Core;
using Xunit;

namespace AudioHexViewer.Tests
{
    public class FftPipelineControllerTests : IDisposable
    {
        private readonly FftPipelineController _ctrl = new FftPipelineController();

        public void Dispose() => _ctrl.Dispose();

        // Paramètres minimalistes — FFT la plus petite possible pour des tests rapides
        private const int SR   = 48_000;
        private const int N    = 256;
        private const int Hop  = 128;
        private static readonly WindowType Win = WindowType.Rectangular;

        // ════════════════════════════════════════════════════════════════════
        // Cycle de vie
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void InitialState_IsNotRunning()
        {
            Assert.False(_ctrl.IsRunning);
        }

        [Fact]
        public void InitialState_RingIsNull()
        {
            Assert.Null(_ctrl.Ring);
        }

        [Fact]
        public void Start_SetsIsRunningAndCreatesRing()
        {
            _ctrl.Start(SR, N, Hop, Win);
            Assert.True(_ctrl.IsRunning);
            Assert.NotNull(_ctrl.Ring);
        }

        [Fact]
        public void Stop_AfterStart_SetsIsRunningFalse()
        {
            _ctrl.Start(SR, N, Hop, Win);
            _ctrl.Stop();
            Assert.False(_ctrl.IsRunning);
        }

        [Fact]
        public void Stop_WithoutStart_NoException()
        {
            _ctrl.Stop(); // ne doit pas lever d'exception
        }

        [Fact]
        public void Stop_Twice_NoException()
        {
            _ctrl.Start(SR, N, Hop, Win);
            _ctrl.Stop();
            _ctrl.Stop(); // idempotent
        }

        [Fact]
        public void Start_InvalidSampleRate_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ctrl.Start(0, N, Hop, Win));
        }

        [Fact]
        public void Start_InvalidFftSize_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ctrl.Start(SR, 0, Hop, Win));
        }

        [Fact]
        public void Start_InvalidHop_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ctrl.Start(SR, N, 0, Win));
        }

        [Fact]
        public void Restart_StopsOldWorkerAndCreatesNewRing()
        {
            _ctrl.Start(SR, N, Hop, Win);
            var firstRing = _ctrl.Ring;

            _ctrl.Start(SR, N * 2, Hop * 2, Win); // paramètres différents
            Assert.True(_ctrl.IsRunning);
            Assert.NotNull(_ctrl.Ring);
            Assert.NotSame(firstRing, _ctrl.Ring); // nouveau ring
        }

        [Fact]
        public void Dispose_NoException()
        {
            _ctrl.Start(SR, N, Hop, Win);
            _ctrl.Dispose(); // ne doit pas lever d'exception
        }

        // ════════════════════════════════════════════════════════════════════
        // PushAudio
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void PushAudio_AfterStart_IncrementsRingAbsWrite()
        {
            _ctrl.Start(SR, N, Hop, Win);
            var samples = new float[64];

            _ctrl.PushAudio(samples, samples);

            Assert.Equal(64L, _ctrl.Ring!.AbsWrite);
        }

        [Fact]
        public void PushAudio_BeforeStart_NoException()
        {
            // Ring est null — PushAudio doit être silencieux (no-op)
            var samples = new float[10];
            _ctrl.PushAudio(samples, samples); // ne doit pas lever d'exception
        }

        [Fact]
        public void PushAudio_MultiplePushes_AccumulatesAbsWrite()
        {
            _ctrl.Start(SR, N, Hop, Win);
            var samples = new float[32];

            for (int i = 0; i < 5; i++)
                _ctrl.PushAudio(samples, samples);

            Assert.Equal(5L * 32, _ctrl.Ring!.AbsWrite);
        }

        // ════════════════════════════════════════════════════════════════════
        // TrySnapshotAsInt24
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void TrySnapshotAsInt24_NoDataYet_ReturnsFalse()
        {
            _ctrl.Start(SR, N, Hop, Win);
            // Ring vide → aucune donnée
            Assert.False(_ctrl.TrySnapshotAsInt24(N, out _, out _));
        }

        [Fact]
        public void TrySnapshotAsInt24_NegativeOrZeroN_ReturnsFalse()
        {
            _ctrl.Start(SR, N, Hop, Win);
            Assert.False(_ctrl.TrySnapshotAsInt24(0, out _, out _));
            Assert.False(_ctrl.TrySnapshotAsInt24(-1, out _, out _));
        }

        [Fact]
        public void TrySnapshotAsInt24_BeforeStart_ReturnsFalse()
        {
            Assert.False(_ctrl.TrySnapshotAsInt24(N, out _, out _));
        }

        [Fact]
        public void TrySnapshotAsInt24_AfterSufficientPush_ReturnsTrueWithCorrectLength()
        {
            _ctrl.Start(SR, N, Hop, Win);

            // Pousser exactement N samples
            var samplesL = new float[N];
            var samplesR = new float[N];
            _ctrl.PushAudio(samplesL, samplesR);

            bool ok = _ctrl.TrySnapshotAsInt24(N, out var outL, out var outR);
            Assert.True(ok);
            Assert.Equal(N, outL.Length);
            Assert.Equal(N, outR.Length);
        }

        [Fact]
        public void TrySnapshotAsInt24_KnownValues_ConvertsToInt24Correctly()
        {
            _ctrl.Start(SR, N, Hop, Win);

            // Valeur float connue → int24 attendu
            // 1.0f * 0x7FFFFF = 8388607 (Max24Pos)
            // -1.0f * 0x800000 = -8388608 (Min24Neg)
            const float fullScale    = 1.0f;
            const float halfScale    = 0.5f;

            int expectedHalf = (int)(halfScale    * (1 << 23));   // 4194304
            
            var srcL = Enumerable.Repeat(fullScale, N).ToArray();
            var srcR = Enumerable.Repeat(halfScale, N).ToArray();
            _ctrl.PushAudio(srcL, srcR);

            Assert.True(_ctrl.TrySnapshotAsInt24(N, out var outL, out var outR));

            // Full scale gauche → clampé à Max24Pos = 8388607
            Assert.All(outL, v => Assert.Equal(8_388_607, v));
            // Half scale droite → 4194304
            Assert.All(outR, v => Assert.Equal(expectedHalf, v));
        }

        // ════════════════════════════════════════════════════════════════════
        // Stats
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void Stats_NotNull()
        {
            Assert.NotNull(_ctrl.Stats);
        }

        [Fact]
        public void SetActiveChannels_NoException()
        {
            _ctrl.Start(SR, N, Hop, Win);
            _ctrl.SetActiveChannels(true, false);
            _ctrl.SetActiveChannels(false, true);
            _ctrl.SetActiveChannels(true, true);
        }
    }
}
