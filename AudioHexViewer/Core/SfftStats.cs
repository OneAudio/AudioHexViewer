// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

using System;
using System.Threading;

namespace AudioHexViewer.Core
{
    internal sealed class SfftStats
    {
        // ---- Counters / state ----
        private long _absWrite;
        private long _framesComputed;
        private long _framesSkipped;

        private string _workerState = "STOP";
        private string _lastError = "";

        // ---- Perf / timings (thread-safe) ----
        // Stockage en "milli-unité" pour éviter volatile/double.
        // - FFT/s en milli-FFT/s (ex: 1234 => 1.234 FFT/s)
        private int _fftPerSecMilli;

        // Temps FFT en microsecondes (µs)
        private int _fftLastUs;
        private int _fftAvgUs; // moyenne lissée (EWMA)


        // ---- Format ----
        private int _sampleRate;
        private int _fftSize;
        private int _hop;

        // Overlap stocké en permille (0..1000) pour éviter double/volatile
        private int _overlapPermille;

        // ---- Progress / scheduling ----
        private long _nextEndAbs;        // abs end de la prochaine fenêtre attendue
        private int _progressPermille;   // 0..1000 (progression de la fenêtre courante)

        // Snapshot immutable
        internal readonly struct Snap
        {
            public readonly long AbsWrite;
            public readonly long FramesComputed;
            public readonly long FramesSkipped;

            public readonly string WorkerState;
            public readonly string LastError;

            // ---- Perf ----
            public readonly int FftPerSecMilli;
            public double FftPerSec => FftPerSecMilli / 1000.0;

            public readonly int FftLastUs;
            public readonly int FftAvgUs;

            public double FftLastMs => FftLastUs / 1000.0;
            public double FftAvgMs => FftAvgUs / 1000.0;


            public readonly int SampleRate;
            public readonly int FftSize;
            public readonly int Hop;

            public readonly int OverlapPermille;
            public double Overlap01 => OverlapPermille / 1000.0;

            public readonly long NextEndAbs;
            public readonly int ProgressPermille;
            public double Progress01 => ProgressPermille / 1000.0;

            public Snap(long absWrite, long framesComputed, long framesSkipped,
                        string workerState, string lastError,
                        int sampleRate, int fftSize, int hop,
                        int overlapPermille,
                        long nextEndAbs, int progressPermille,
                        int fftPerSecMilli,
                        int fftLastUs, int fftAvgUs)

            {
                AbsWrite = absWrite;
                FramesComputed = framesComputed;
                FramesSkipped = framesSkipped;
                WorkerState = workerState;
                LastError = lastError;
                SampleRate = sampleRate;
                FftSize = fftSize;
                Hop = hop;
                OverlapPermille = overlapPermille;
                NextEndAbs = nextEndAbs;
                ProgressPermille = progressPermille;
                FftPerSecMilli = fftPerSecMilli;
                FftLastUs = fftLastUs;
                FftAvgUs = fftAvgUs;

            }
        }

        // ---- Writers (thread-safe) ----
        public void SetAbsWrite(long absWrite) => Interlocked.Exchange(ref _absWrite, absWrite);

        public void IncFramesComputed() => Interlocked.Increment(ref _framesComputed);
        public void IncFramesSkipped() => Interlocked.Increment(ref _framesSkipped);

        public void ResetCounters()
        {
            Interlocked.Exchange(ref _absWrite, 0);
            Interlocked.Exchange(ref _framesComputed, 0);
            Interlocked.Exchange(ref _framesSkipped, 0);

            Interlocked.Exchange(ref _nextEndAbs, 0);
            Volatile.Write(ref _progressPermille, 0);

            Volatile.Write(ref _fftPerSecMilli, 0);
            Volatile.Write(ref _fftLastUs, 0);
            Volatile.Write(ref _fftAvgUs, 0);

            SetWorkerState("STOP");
            SetLastError("");
        }


        public void SetWorkerState(string s)
        {
            if (s == null) s = "";
            Volatile.Write(ref _workerState, s);
        }

        public void SetLastError(string s)
        {
            if (s == null) s = "";
            Volatile.Write(ref _lastError, s);
        }

        public void SetFormat(int sampleRate, int fftSize, int hop, double overlapRequested01)
        {
            Volatile.Write(ref _sampleRate, sampleRate);
            Volatile.Write(ref _fftSize, fftSize);
            Volatile.Write(ref _hop, hop);

            if (overlapRequested01 < 0) overlapRequested01 = 0;
            if (overlapRequested01 > 0.999) overlapRequested01 = 0.999;

            int pm = (int)Math.Round(overlapRequested01 * 1000.0);
            if (pm < 0) pm = 0;
            if (pm > 1000) pm = 1000;
            Volatile.Write(ref _overlapPermille, pm);
        }

        public void SetNextEndAbs(long v) => Interlocked.Exchange(ref _nextEndAbs, v);

        public void SetProgress01(double p01)
        {
            if (p01 < 0) p01 = 0;
            if (p01 > 1) p01 = 1;
            int pm = (int)Math.Round(p01 * 1000.0);
            if (pm < 0) pm = 0;
            if (pm > 1000) pm = 1000;
            Volatile.Write(ref _progressPermille, pm);
        }
        //
        /// <summary>
        /// Met à jour la cadence FFT réelle (FFT/s).
        /// Stockage en milli-FFT/s pour éviter double cross-thread.
        /// </summary>
        public void SetFftPerSec(double fps)
        {
            if (double.IsNaN(fps) || double.IsInfinity(fps) || fps < 0) fps = 0;
            int m = (int)Math.Round(fps * 1000.0);
            if (m < 0) m = 0;
            Volatile.Write(ref _fftPerSecMilli, m);
        }

        /// <summary>
        /// Enregistre le temps CPU (en µs) d'une itération FFT (une frame).
        /// EWMA simple pour lisser (évite un affichage trop bruité).
        /// </summary>
        public void ReportFftTimeUs(long lastUs)
        {
            if (lastUs < 0) lastUs = 0;
            if (lastUs > int.MaxValue) lastUs = int.MaxValue;

            int u = (int)lastUs;

            // Dernière mesure
            Volatile.Write(ref _fftLastUs, u);

            // Moyenne lissée (EWMA): avg = avg + (u-avg)/8  (~alpha=0.125)
            int prev = Volatile.Read(ref _fftAvgUs);
            int next = (prev == 0) ? u : (prev + ((u - prev) >> 3));
            Volatile.Write(ref _fftAvgUs, next);
        }

        // ---- Reader ----
        public Snap Snapshot()
        {
            return new Snap(
                absWrite: Interlocked.Read(ref _absWrite),
                framesComputed: Interlocked.Read(ref _framesComputed),
                framesSkipped: Interlocked.Read(ref _framesSkipped),
                workerState: Volatile.Read(ref _workerState),
                lastError: Volatile.Read(ref _lastError),
                sampleRate: Volatile.Read(ref _sampleRate),
                fftSize: Volatile.Read(ref _fftSize),
                hop: Volatile.Read(ref _hop),
                overlapPermille: Volatile.Read(ref _overlapPermille),
                nextEndAbs: Interlocked.Read(ref _nextEndAbs),
                progressPermille: Volatile.Read(ref _progressPermille),
                // ---- Perf ----
                fftPerSecMilli: Volatile.Read(ref _fftPerSecMilli),
                fftLastUs: Volatile.Read(ref _fftLastUs),
                fftAvgUs: Volatile.Read(ref _fftAvgUs)
            );
        }
    }
}
