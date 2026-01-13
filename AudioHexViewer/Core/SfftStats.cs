using System;

namespace AudioHexViewer.Core
{
    /// <summary>
    /// Stats partagées entre threads.
    /// On utilise un lock interne pour éviter les limitations de "volatile" (double/long interdits).
    /// L'UI lit un snapshot cohérent via Snapshot().
    /// </summary>
    internal sealed class SfftStats
    {
        private readonly object _lock = new object();

        // Champs internes (non-volatile)
        private int _sampleRate;
        private int _fftSize;
        private int _hop;
        private double _overlapRequested;

        private long _absWrite;
        private long _framesComputed;
        private long _framesSkipped;

        private string _workerState = "STOP"; // RUN / WAIT / STOP
        private string _lastError = "";

        // --- Write (depuis audio thread / worker) ---
        public void SetFormat(int sampleRate, int fftSize, int hop, double overlapRequested)
        {
            lock (_lock)
            {
                _sampleRate = sampleRate;
                _fftSize = fftSize;
                _hop = hop;
                _overlapRequested = overlapRequested;
            }
        }

        public void SetAbsWrite(long absWrite)
        {
            lock (_lock) { _absWrite = absWrite; }
        }

        public void IncFramesComputed()
        {
            lock (_lock) { _framesComputed++; }
        }

        public void IncFramesSkipped()
        {
            lock (_lock) { _framesSkipped++; }
        }

        public void SetWorkerState(string state)
        {
            lock (_lock) { _workerState = state ?? ""; }
        }

        public void SetLastError(string msg)
        {
            lock (_lock) { _lastError = msg ?? ""; }
        }

        public void ResetCounters()
        {
            lock (_lock)
            {
                _absWrite = 0;
                _framesComputed = 0;
                _framesSkipped = 0;
                _workerState = "STOP";
                _lastError = "";
            }
        }

        // --- Read (UI) : snapshot cohérent ---
        public StatsSnapshot Snapshot()
        {
            lock (_lock)
            {
                return new StatsSnapshot(
                    sampleRate: _sampleRate,
                    fftSize: _fftSize,
                    hop: _hop,
                    overlapRequested: _overlapRequested,
                    absWrite: _absWrite,
                    framesComputed: _framesComputed,
                    framesSkipped: _framesSkipped,
                    workerState: _workerState,
                    lastError: _lastError
                );
            }
        }

        internal readonly struct StatsSnapshot
        {
            public readonly int SampleRate;
            public readonly int FftSize;
            public readonly int Hop;
            public readonly double OverlapRequested;

            public readonly long AbsWrite;
            public readonly long FramesComputed;
            public readonly long FramesSkipped;

            public readonly string WorkerState;
            public readonly string LastError;

            public StatsSnapshot(int sampleRate, int fftSize, int hop, double overlapRequested,
                                 long absWrite, long framesComputed, long framesSkipped,
                                 string workerState, string lastError)
            {
                SampleRate = sampleRate;
                FftSize = fftSize;
                Hop = hop;
                OverlapRequested = overlapRequested;
                AbsWrite = absWrite;
                FramesComputed = framesComputed;
                FramesSkipped = framesSkipped;
                WorkerState = workerState;
                LastError = lastError;
            }
        }
    }
}
