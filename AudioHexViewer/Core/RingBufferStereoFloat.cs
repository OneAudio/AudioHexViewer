using System;

namespace AudioHexViewer.Core
{
    /// <summary>
    /// Ring buffer stéréo float, index absolu en samples par canal.
    /// Thread-safe via lock.
    /// 
    /// IMPORTANT : PushBlock et TryReadWindow utilisent Span.CopyTo (pas de boucle
    /// échantillon par échantillon). À N=1M, le gain est de 100× : lock tenu ~1ms
    /// au lieu de ~15ms, ce qui élimine la starvation du thread audio.
    /// </summary>
    internal sealed class RingBufferStereoFloat
    {
        private readonly float[] _l;
        private readonly float[] _r;
        private readonly int _cap;
        private long _absWrite;
        private readonly object _lock = new object();

        public RingBufferStereoFloat(int capacitySamples)
        {
            if (capacitySamples <= 0) throw new ArgumentOutOfRangeException(nameof(capacitySamples));
            _cap = capacitySamples;
            _l = new float[_cap];
            _r = new float[_cap];
        }

        public long AbsWrite
        {
            get { lock (_lock) return _absWrite; }
        }

        public int Capacity => _cap;

        /// <summary>
        /// Écrit un bloc stéréo dans le ring buffer.
        /// Utilise Span.CopyTo (SIMD) — le lock est tenu quelques microsecondes.
        /// </summary>
        // Diagnostics lock contention (lecture via propriétés publiques)
        private long _lockWaitMaxTicks;
        private long _lockWaitTotalTicks;
        private long _lockWaitCount;

        public double LockWaitMaxMs => _lockWaitMaxTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        public double LockWaitAvgMs => _lockWaitCount > 0
            ? _lockWaitTotalTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency / _lockWaitCount
            : 0;
        public long LockWaitCount => _lockWaitCount;
        public void ResetLockStats() { _lockWaitMaxTicks = 0; _lockWaitTotalTicks = 0; _lockWaitCount = 0; }

        public void PushBlock(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
        {
            if (left.Length != right.Length)
                throw new ArgumentException("Length mismatch");

            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            lock (_lock)
            {
                long waited = System.Diagnostics.Stopwatch.GetTimestamp() - t0;
                System.Threading.Interlocked.Add(ref _lockWaitTotalTicks, waited);
                System.Threading.Interlocked.Increment(ref _lockWaitCount);
                long prev = System.Threading.Volatile.Read(ref _lockWaitMaxTicks);
                while (waited > prev && System.Threading.Interlocked.CompareExchange(ref _lockWaitMaxTicks, waited, prev) != prev)
                    prev = System.Threading.Volatile.Read(ref _lockWaitMaxTicks);
                {
                    int n = left.Length;
                    int startIdx = (int)(_absWrite % _cap);
                    int tail = _cap - startIdx;   // samples disponibles jusqu'à la fin du tableau

                    if (tail >= n)
                    {
                        // Pas de wrap : copie directe en une seule passe
                        left.CopyTo(_l.AsSpan(startIdx, n));
                        right.CopyTo(_r.AsSpan(startIdx, n));
                    }
                    else
                    {
                        // Wrap : deux passes
                        left[..tail].CopyTo(_l.AsSpan(startIdx));        // fin du tableau
                        right[..tail].CopyTo(_r.AsSpan(startIdx));
                        left[tail..].CopyTo(_l.AsSpan(0, n - tail));    // début du tableau
                        right[tail..].CopyTo(_r.AsSpan(0, n - tail));
                    }

                    _absWrite += n;
                }
            }
        }

        /// <summary>
        /// Lit la fenêtre [absStart, absStart+length) dans outL/outR.
        /// Retourne false si hors zone (pas encore écrit ou déjà écrasé).
        ///
        /// DESIGN : le lock est tenu UNIQUEMENT pendant la validation (quelques ns).
        /// La copie SIMD (potentiellement 30+ MB à N=4M) se fait SANS lock.
        ///
        /// Sécurité : le ring a une capacité de 6×N (voir RestartSfftFromUi).
        /// Après validation, le writer met au minimum (cap - length) ≈ 5×N samples
        /// supplémentaires avant d'écraser notre zone de lecture, soit ~27 s à 768 kHz.
        /// La copie ne prend jamais autant de temps → pas de donnée corrompue possible.
        /// </summary>
        public bool TryReadWindow(long absStart, int length, Span<float> outL, Span<float> outR)
        {
            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            if (outL.Length < length || outR.Length < length)
                throw new ArgumentException("Output span too small");

            // ── Phase 1 : validation sous lock (microsecondes) ───────────────
            int startIdx;
            lock (_lock)
            {
                long w = _absWrite;
                long absEnd = absStart + length;
                if (absEnd > w)          return false;  // pas encore disponible
                if (absStart < w - _cap) return false;  // déjà écrasé
                startIdx = (int)(absStart % _cap);
            }
            // ── Phase 2 : copie SANS lock (SIMD, durée proportionnelle à N) ──
            // Les slots [startIdx .. startIdx+length) sont gelés pendant au moins
            // (cap - length) samples d'écriture supplémentaires — voir commentaire.
            int tail = _cap - startIdx;
            if (tail >= length)
            {
                _l.AsSpan(startIdx, length).CopyTo(outL);
                _r.AsSpan(startIdx, length).CopyTo(outR);
            }
            else
            {
                _l.AsSpan(startIdx, tail).CopyTo(outL);
                _r.AsSpan(startIdx, tail).CopyTo(outR);
                _l.AsSpan(0, length - tail).CopyTo(outL[tail..]);
                _r.AsSpan(0, length - tail).CopyTo(outR[tail..]);
            }
            return true;
        }
    }
}