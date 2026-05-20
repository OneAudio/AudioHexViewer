using System;
using System.Runtime.InteropServices;
using NAudio.Wave;

namespace AudioHexViewer
{
    internal enum SignalType
    {
        Sine,
        Square, Triangle, Sawtooth,
        WhiteNoise, PinkNoise,
        Sweep,
        TwoTone
    }

    internal readonly struct SignalParams
    {
        public readonly SignalType Type;
        public readonly double     F1Hz;            // fréquence principale (ou départ sweep)
        public readonly double     F2Hz;            // deux tons : F2 ; sweep : fréquence finale
        public readonly double     AmplitudeLinear; // 0..1 (peak)
        public readonly int        Channel;         // 0=L, 1=R, 2=L+R
        public readonly bool       InvertSecond;
        // Sweep only
        public readonly double     SweepDurationSec;
        public readonly bool       SweepLog;
        public readonly bool       SweepLoop;

        public SignalParams(SignalType type, double f1Hz, double f2Hz,
                            double amplitudeLinear, int channel, bool invertSecond,
                            double sweepDurationSec = 5.0,
                            bool sweepLog = true, bool sweepLoop = false)
        {
            Type             = type;
            F1Hz             = f1Hz;
            F2Hz             = f2Hz;
            AmplitudeLinear  = amplitudeLinear;
            Channel          = channel;
            InvertSecond     = invertSecond;
            SweepDurationSec = sweepDurationSec;
            SweepLog         = sweepLog;
            SweepLoop        = sweepLoop;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Classe de base : float32 stéréo, routage canal
    // ──────────────────────────────────────────────────────────────────────────
    internal abstract class ChanneledProvider : IWaveProvider
    {
        protected readonly WaveFormat _fmt;
        protected readonly double _ampL, _ampR;

        protected ChanneledProvider(int sr, int channel, double amplitude, bool invertSecond)
        {
            _fmt  = WaveFormat.CreateIeeeFloatWaveFormat(sr, 2);
            _ampL = (channel == 0 || channel == 2) ? amplitude : 0.0;
            _ampR = channel == 1 ? amplitude
                  : channel == 2 ? (invertSecond ? -amplitude : amplitude)
                  : 0.0;
        }

        public WaveFormat WaveFormat => _fmt;

        public int Read(byte[] buffer, int offset, int count)
        {
            var span   = MemoryMarshal.Cast<byte, float>(new Span<byte>(buffer, offset, count));
            int frames = span.Length / 2;
            FillFrames(span, frames);
            return frames * 8;   // float32 stéréo = 8 octets / frame
        }

        protected abstract void FillFrames(Span<float> span, int frames);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Sinus
    // ──────────────────────────────────────────────────────────────────────────
    internal sealed class SineProvider : ChanneledProvider
    {
        private double _phase, _dphi;

        public SineProvider(in SignalParams p, int sr)
            : base(sr, p.Channel, p.AmplitudeLinear, p.InvertSecond)
        {
            _dphi = 2.0 * Math.PI * p.F1Hz / sr;
        }

        protected override void FillFrames(Span<float> span, int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                float s = (float)Math.Sin(_phase);
                span[i * 2]     = (float)(_ampL * s);
                span[i * 2 + 1] = (float)(_ampR * s);
                _phase += _dphi;
                if (_phase > 2.0 * Math.PI) _phase -= 2.0 * Math.PI;
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Formes d'onde : Carré, Triangle, Dent de scie
    // ──────────────────────────────────────────────────────────────────────────
    internal sealed class WaveformProvider : ChanneledProvider
    {
        private double _phase, _dphi;
        private readonly SignalType _shape;

        public WaveformProvider(in SignalParams p, int sr)
            : base(sr, p.Channel, p.AmplitudeLinear, p.InvertSecond)
        {
            _shape = p.Type;
            _dphi  = 2.0 * Math.PI * p.F1Hz / sr;
        }

        protected override void FillFrames(Span<float> span, int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                float s;
                switch (_shape)
                {
                    case SignalType.Square:
                        s = Math.Sin(_phase) >= 0 ? 1f : -1f;
                        break;
                    case SignalType.Triangle:
                        s = (float)(2.0 / Math.PI * Math.Asin(Math.Sin(_phase)));
                        break;
                    default: // Sawtooth
                        s = (float)(_phase / Math.PI - 1.0);
                        break;
                }
                span[i * 2]     = (float)(_ampL * s);
                span[i * 2 + 1] = (float)(_ampR * s);
                _phase = (_phase + _dphi) % (2.0 * Math.PI);
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Bruit blanc et rose (algorithme de Paul Kellet)
    // ──────────────────────────────────────────────────────────────────────────
    internal sealed class NoiseProvider : ChanneledProvider
    {
        private readonly bool _pink;
        private          uint  _rng = 0x12345678u;
        private double _b0, _b1, _b2, _b3, _b4, _b5, _b6;

        public NoiseProvider(in SignalParams p, int sr, bool pink)
            : base(sr, p.Channel, p.AmplitudeLinear, p.InvertSecond)
        {
            _pink = pink;
        }

        private float NextWhite()
        {
            _rng ^= _rng << 13; _rng ^= _rng >> 17; _rng ^= _rng << 5;
            return ((_rng & 0xFFFFFFu) / (float)0x1000000u) * 2f - 1f;
        }

        private float NextPink()
        {
            float w = NextWhite();
            _b0 = 0.99886 * _b0 + w * 0.0555179;
            _b1 = 0.99332 * _b1 + w * 0.0750759;
            _b2 = 0.96900 * _b2 + w * 0.1538520;
            _b3 = 0.86650 * _b3 + w * 0.3104856;
            _b4 = 0.55000 * _b4 + w * 0.5329522;
            _b5 = -0.7616 * _b5 - w * 0.0168980;
            float v = (float)((_b0+_b1+_b2+_b3+_b4+_b5+_b6 + w*0.5362) * 0.11);
            _b6 = w * 0.115926;
            return Math.Clamp(v, -1f, 1f);
        }

        protected override void FillFrames(Span<float> span, int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                float s = _pink ? NextPink() : NextWhite();
                span[i * 2]     = (float)(_ampL * s);
                span[i * 2 + 1] = (float)(_ampR * s);
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Sweep (balayage lin ou log, avec loop optionnel)
    // ──────────────────────────────────────────────────────────────────────────
    internal sealed class SweepProvider : ChanneledProvider
    {
        private readonly double _f1, _f2, _T;
        private readonly int    _sr;
        private readonly bool   _log, _loop;
        private readonly long   _totalSamples;
        private          double _phase;
        private          long   _n;
        private          bool   _done;

        public bool Done => _done;

        public SweepProvider(in SignalParams p, int sr)
            : base(sr, p.Channel, p.AmplitudeLinear, p.InvertSecond)
        {
            _f1   = Math.Max(1.0,  p.F1Hz);
            _f2   = Math.Max(_f1 + 1, p.F2Hz);
            _T    = Math.Max(0.1,  p.SweepDurationSec);
            _sr   = sr;
            _log  = p.SweepLog && _f1 > 0 && _f2 > _f1;
            _loop = p.SweepLoop;
            _totalSamples = (long)(_T * sr);
        }

        private double InstantFreq(long n)
        {
            double t = n / (double)_sr;
            if (_log)
                return _f1 * Math.Pow(_f2 / _f1, t / _T);
            else
                return _f1 + (_f2 - _f1) * t / _T;
        }

        protected override void FillFrames(Span<float> span, int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                if (_done)
                { span[i * 2] = span[i * 2 + 1] = 0f; continue; }

                float s = (float)Math.Sin(_phase);
                span[i * 2]     = (float)(_ampL * s);
                span[i * 2 + 1] = (float)(_ampR * s);

                double f = InstantFreq(_n);
                _phase += 2.0 * Math.PI * f / _sr;
                if (_phase > 2.0 * Math.PI) _phase -= 2.0 * Math.PI;

                _n++;
                if (_n >= _totalSamples)
                {
                    if (_loop) _n = 0;
                    else       _done = true;
                }
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Deux tons (IMD) — amplitude divisée par 2 pour rester dans la plage
    // ──────────────────────────────────────────────────────────────────────────
    internal sealed class TwoToneProvider : ChanneledProvider
    {
        private double _ph1, _ph2, _dp1, _dp2;

        public TwoToneProvider(in SignalParams p, int sr)
            : base(sr, p.Channel, p.AmplitudeLinear * 0.5, p.InvertSecond)
            // ×0.5 : chaque ton à moitié amplitude pour que la somme reste ≤ amplitude
        {
            _dp1 = 2.0 * Math.PI * p.F1Hz / sr;
            _dp2 = 2.0 * Math.PI * p.F2Hz / sr;
        }

        protected override void FillFrames(Span<float> span, int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                float s = (float)(Math.Sin(_ph1) + Math.Sin(_ph2));
                span[i * 2]     = (float)(_ampL * s);
                span[i * 2 + 1] = (float)(_ampR * s);
                _ph1 += _dp1; if (_ph1 > 2.0 * Math.PI) _ph1 -= 2.0 * Math.PI;
                _ph2 += _dp2; if (_ph2 > 2.0 * Math.PI) _ph2 -= 2.0 * Math.PI;
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Factory
    // ──────────────────────────────────────────────────────────────────────────
    internal static class SignalProviderFactory
    {
        public static IWaveProvider Create(in SignalParams p, int sampleRate) =>
            p.Type switch
            {
                SignalType.Sine      => new SineProvider(p, sampleRate),
                SignalType.Square    => new WaveformProvider(p, sampleRate),
                SignalType.Triangle  => new WaveformProvider(p, sampleRate),
                SignalType.Sawtooth  => new WaveformProvider(p, sampleRate),
                SignalType.WhiteNoise=> new NoiseProvider(p, sampleRate, false),
                SignalType.PinkNoise => new NoiseProvider(p, sampleRate, true),
                SignalType.Sweep     => new SweepProvider(p, sampleRate),
                SignalType.TwoTone   => new TwoToneProvider(p, sampleRate),
                _                   => throw new ArgumentException($"Unknown type: {p.Type}")
            };
    }
}
