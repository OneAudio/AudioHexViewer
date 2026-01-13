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
                        long nextEndAbs, int progressPermille)
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
                progressPermille: Volatile.Read(ref _progressPermille)
            );
        }
    }
}
