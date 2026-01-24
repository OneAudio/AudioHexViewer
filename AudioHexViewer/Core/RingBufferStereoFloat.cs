using System;

namespace AudioHexViewer.Core
{
    /// <summary>
    /// Ring buffer stéréo float (L÷R séparés), index absolu en samples (par canal).
    /// Thread-safe via lock (simple et robuste pour démarrer).
    ///
    /// Convention: AbsWrite = nombre total de samples écrits (par canal) depuis le start.
    /// Les données disponibles sont dans [AbsWrite - Capacity, AbsWrite).
    /// </summary>
    internal sealed class RingBufferStereoFloat
    {
        private readonly float[] _l;
        private readonly float[] _r;
        private readonly int _cap;

        private long _absWrite; // compteur absolu (samples par canal)
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

        public void PushBlock(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
        {
            if (left.Length != right.Length) throw new ArgumentException("L÷R length mismatch");

            lock (_lock)
            {
                int n = left.Length;
                long w0 = _absWrite;

                for (int i = 0; i < n; i++)
                {
                    int idx = (int)((w0 + i) % _cap);
                    _l[idx] = left[i];
                    _r[idx] = right[i];
                }

                _absWrite += n;
            }
        }

        /// <summary>
        /// Copie une fenêtre [absStart, absStart+length) dans outL/outR.
        /// Retourne false si la fenêtre est hors zone (pas encore dispo ou déjà écrasée).
        /// </summary>
        public bool TryReadWindow(long absStart, int length, Span<float> outL, Span<float> outR)
        {
            if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));
            if (outL.Length < length || outR.Length < length) throw new ArgumentException("Output span too small");

            lock (_lock)
            {
                long absEnd = absStart + length;
                long w = _absWrite;

                if (absEnd > w) return false; // pas encore écrit
                long oldest = w - _cap;
                if (absStart < oldest) return false; // écrasé

                for (int i = 0; i < length; i++)
                {
                    int idx = (int)((absStart + i) % _cap);
                    outL[i] = _l[idx];
                    outR[i] = _r[idx];
                }
                return true;
            }
        }
    }
}
