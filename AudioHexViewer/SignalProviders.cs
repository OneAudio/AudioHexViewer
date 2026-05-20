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
        TwoTone,
        ToneBurst
    }

    internal enum BurstWindowType
    {
        Rectangular = 0,
        Tukey025, Tukey050, Tukey075,
        Cosine,
        Hann,
        Hamming,
        Blackman,
        BlackmanHarris4,
        FlatTop
    }

    internal readonly struct SignalParams
    {
        public readonly SignalType Type;
        public readonly double     F1Hz, F2Hz;
        public readonly double     AmplitudeLinear, AmplitudeF2Linear;
        public readonly int        Channel;
        public readonly bool       InvertSecond;
        // Sweep
        public readonly double     SweepDurationSec;
        public readonly bool       SweepLog, SweepLoop;
        // Burst
        public readonly double          BurstOnValue;        // nombre de périodes OU ms
        public readonly bool            BurstOnInPeriods;
        public readonly double          BurstOffValue;
        public readonly bool            BurstOffInPeriods;
        public readonly BurstWindowType BurstWindow;
        public readonly bool            BurstLoop;
        // Dithering (0=off, 16, 32)
        public readonly int DitheringBits;

        public SignalParams(SignalType type,
            double f1Hz, double f2Hz,
            double amp, double ampF2,
            int channel, bool invertSecond,
            double sweepDuration = 5.0, bool sweepLog = true, bool sweepLoop = false,
            double burstOnValue = 5.0,  bool burstOnInPeriods = true,
            double burstOffValue = 20.0,bool burstOffInPeriods = true,
            BurstWindowType burstWindow = BurstWindowType.Rectangular,
            bool burstLoop = false,
            int ditheringBits = 0)
        {
            Type              = type;
            F1Hz              = f1Hz;  F2Hz = f2Hz;
            AmplitudeLinear   = amp;   AmplitudeF2Linear = ampF2;
            Channel           = channel; InvertSecond = invertSecond;
            SweepDurationSec  = sweepDuration; SweepLog = sweepLog; SweepLoop = sweepLoop;
            BurstOnValue      = burstOnValue;  BurstOnInPeriods  = burstOnInPeriods;
            BurstOffValue     = burstOffValue; BurstOffInPeriods = burstOffInPeriods;
            BurstWindow       = burstWindow;   BurstLoop         = burstLoop;
            DitheringBits     = ditheringBits;
        }
    }

    // ── Base routage canal ────────────────────────────────────────────────────
    internal abstract class ChanneledProvider : IWaveProvider
    {
        protected readonly WaveFormat _fmt;
        protected readonly double _ampL, _ampR;

        protected ChanneledProvider(int sr, int ch, double amp, bool inv)
        {
            _fmt  = WaveFormat.CreateIeeeFloatWaveFormat(sr, 2);
            _ampL = (ch == 0 || ch == 2) ? amp : 0.0;
            _ampR = ch == 1 ? amp : ch == 2 ? (inv ? -amp : amp) : 0.0;
        }

        public WaveFormat WaveFormat => _fmt;

        public int Read(byte[] buf, int offset, int count)
        {
            var sp = MemoryMarshal.Cast<byte, float>(new Span<byte>(buf, offset, count));
            int n  = sp.Length / 2;
            FillFrames(sp, n);
            return n * 8;
        }

        protected abstract void FillFrames(Span<float> sp, int n);
    }

    // ── Sinus ─────────────────────────────────────────────────────────────────
    internal sealed class SineProvider : ChanneledProvider
    {
        private double _phase, _dphi;
        public SineProvider(in SignalParams p, int sr) : base(sr, p.Channel, p.AmplitudeLinear, p.InvertSecond)
            => _dphi = 2.0 * Math.PI * p.F1Hz / sr;

        protected override void FillFrames(Span<float> sp, int n)
        {
            for (int i = 0; i < n; i++)
            {
                float s = (float)Math.Sin(_phase);
                sp[i*2] = (float)(_ampL * s);  sp[i*2+1] = (float)(_ampR * s);
                _phase += _dphi;
                if (_phase > 2.0 * Math.PI) _phase -= 2.0 * Math.PI;
            }
        }
    }

    // ── Formes d'onde ─────────────────────────────────────────────────────────
    internal sealed class WaveformProvider : ChanneledProvider
    {
        private double _phase, _dphi;
        private readonly SignalType _shape;

        public WaveformProvider(in SignalParams p, int sr) : base(sr, p.Channel, p.AmplitudeLinear, p.InvertSecond)
        { _shape = p.Type; _dphi = 2.0 * Math.PI * p.F1Hz / sr; }

        protected override void FillFrames(Span<float> sp, int n)
        {
            for (int i = 0; i < n; i++)
            {
                float s = _shape switch
                {
                    SignalType.Square   => Math.Sin(_phase) >= 0 ? 1f : -1f,
                    SignalType.Triangle => (float)(2.0 / Math.PI * Math.Asin(Math.Sin(_phase))),
                    _                  => (float)(_phase / Math.PI - 1.0)
                };
                sp[i*2] = (float)(_ampL * s);  sp[i*2+1] = (float)(_ampR * s);
                _phase = (_phase + _dphi) % (2.0 * Math.PI);
            }
        }
    }

    // ── Bruit blanc et rose ───────────────────────────────────────────────────
    internal sealed class NoiseProvider : ChanneledProvider
    {
        private readonly bool _pink;
        private uint _rng = 0x12345678u;
        private double _b0, _b1, _b2, _b3, _b4, _b5, _b6;

        public NoiseProvider(in SignalParams p, int sr, bool pink) : base(sr, p.Channel, p.AmplitudeLinear, p.InvertSecond)
            => _pink = pink;

        private float W() { _rng ^= _rng << 13; _rng ^= _rng >> 17; _rng ^= _rng << 5;
                            return ((_rng & 0xFFFFFF) / (float)0x1000000) * 2f - 1f; }

        private float Pink()
        {
            float w = W();
            _b0 = 0.99886*_b0+w*0.0555179; _b1 = 0.99332*_b1+w*0.0750759;
            _b2 = 0.96900*_b2+w*0.1538520; _b3 = 0.86650*_b3+w*0.3104856;
            _b4 = 0.55000*_b4+w*0.5329522; _b5 = -0.7616*_b5-w*0.0168980;
            float v = (float)((_b0+_b1+_b2+_b3+_b4+_b5+_b6+w*0.5362)*0.11);
            _b6 = w*0.115926;
            return Math.Clamp(v, -1f, 1f);
        }

        protected override void FillFrames(Span<float> sp, int n)
        {
            for (int i = 0; i < n; i++)
            { float s = _pink ? Pink() : W(); sp[i*2]=(float)(_ampL*s); sp[i*2+1]=(float)(_ampR*s); }
        }
    }

    // ── Sweep ─────────────────────────────────────────────────────────────────
    internal sealed class SweepProvider : ChanneledProvider
    {
        private readonly double _f1, _f2, _T;
        private readonly int    _sr;
        private readonly bool   _log, _loop;
        private readonly long   _total;
        private double _phase;
        private long   _n;
        private bool   _done;

        public bool Done => _done;

        public SweepProvider(in SignalParams p, int sr) : base(sr, p.Channel, p.AmplitudeLinear, p.InvertSecond)
        {
            _f1=Math.Max(1.0, p.F1Hz); _f2=Math.Max(_f1+1, p.F2Hz);
            _T=Math.Max(0.1, p.SweepDurationSec); _sr=sr;
            _log=p.SweepLog && _f1>0 && p.F2Hz>_f1; _loop=p.SweepLoop;
            _total=(long)(_T*sr);
        }

        protected override void FillFrames(Span<float> sp, int n)
        {
            for (int i = 0; i < n; i++)
            {
                if (_done) { sp[i*2]=sp[i*2+1]=0f; continue; }
                float s=(float)Math.Sin(_phase);
                sp[i*2]=(float)(_ampL*s); sp[i*2+1]=(float)(_ampR*s);
                double t=_n/(double)_sr;
                double f=_log ? _f1*Math.Pow(_f2/_f1, t/_T) : _f1+(_f2-_f1)*t/_T;
                _phase += 2.0*Math.PI*f/_sr;
                if (_phase > 2.0*Math.PI) _phase -= 2.0*Math.PI;
                if (++_n >= _total) { if (_loop) _n=0; else _done=true; }
            }
        }
    }

    // ── Deux tons ─────────────────────────────────────────────────────────────
    internal sealed class TwoToneProvider : IWaveProvider
    {
        private readonly WaveFormat _fmt;
        private readonly double _a1L, _a1R, _a2L, _a2R;
        private double _ph1, _ph2, _dp1, _dp2;

        public WaveFormat WaveFormat => _fmt;

        public TwoToneProvider(in SignalParams p, int sr)
        {
            _fmt = WaveFormat.CreateIeeeFloatWaveFormat(sr, 2);
            _dp1 = 2.0*Math.PI*p.F1Hz/sr; _dp2 = 2.0*Math.PI*p.F2Hz/sr;
            double a1=p.AmplitudeLinear, a2=p.AmplitudeF2Linear;
            bool inv=p.InvertSecond; int ch=p.Channel;
            _a1L=(ch==0||ch==2)?a1:0.0; _a1R=ch==1?a1:ch==2?(inv?-a1:a1):0.0;
            _a2L=(ch==0||ch==2)?a2:0.0; _a2R=ch==1?a2:ch==2?(inv?-a2:a2):0.0;
        }

        public int Read(byte[] buf, int off, int count)
        {
            var sp=MemoryMarshal.Cast<byte,float>(new Span<byte>(buf,off,count));
            int n=sp.Length/2;
            for (int i=0;i<n;i++)
            {
                float s1=(float)Math.Sin(_ph1), s2=(float)Math.Sin(_ph2);
                sp[i*2]  =Math.Clamp((float)(_a1L*s1+_a2L*s2),-1f,1f);
                sp[i*2+1]=Math.Clamp((float)(_a1R*s1+_a2R*s2),-1f,1f);
                _ph1+=_dp1; if(_ph1>2*Math.PI)_ph1-=2*Math.PI;
                _ph2+=_dp2; if(_ph2>2*Math.PI)_ph2-=2*Math.PI;
            }
            return n*8;
        }
    }

    // ── Tone Burst — durée ON/OFF en périodes ou ms, fenêtrage ───────────────
    internal sealed class ToneBurstProvider : ChanneledProvider
    {
        private readonly float[] _window;
        private readonly int     _onSamples, _period;
        private readonly bool    _loop;
        private double           _phase, _dphi;
        private int              _pos;
        private bool             _done;

        public bool Done => _done;

        public ToneBurstProvider(in SignalParams p, int sr)
            : base(sr, p.Channel, p.AmplitudeLinear, p.InvertSecond)
        {
            _dphi = 2.0 * Math.PI * p.F1Hz / sr;
            int spc = Math.Max(1, (int)Math.Round(sr / p.F1Hz));   // samples per cycle

            _onSamples  = p.BurstOnInPeriods
                ? Math.Max(1, (int)Math.Round(p.BurstOnValue  * spc))
                : Math.Max(1, (int)Math.Round(p.BurstOnValue  * sr / 1000.0));

            int offSamples = p.BurstOffInPeriods
                ? Math.Max(1, (int)Math.Round(p.BurstOffValue * spc))
                : Math.Max(1, (int)Math.Round(p.BurstOffValue * sr / 1000.0));

            _period = _onSamples + offSamples;
            _loop   = p.BurstLoop;
            _window = BurstWindows.Compute(_onSamples, p.BurstWindow);
        }

        protected override void FillFrames(Span<float> sp, int n)
        {
            for (int i = 0; i < n; i++)
            {
                if (_done) { sp[i*2]=sp[i*2+1]=0f; continue; }

                if (_pos < _onSamples)
                {
                    float s = _window[_pos] * (float)Math.Sin(_phase);
                    sp[i*2]=(float)(_ampL*s); sp[i*2+1]=(float)(_ampR*s);
                }
                else
                    sp[i*2]=sp[i*2+1]=0f;

                _phase += _dphi;
                if (_phase > 2.0*Math.PI) _phase -= 2.0*Math.PI;

                if (++_pos >= _period)
                {
                    if (_loop) _pos=0;
                    else       _done=true;
                }
            }
        }
    }

    // ── Calcul des fenêtres burst ─────────────────────────────────────────────
    internal static class BurstWindows
    {
        public static float[] Compute(int n, BurstWindowType t)
        {
            var w = new float[n];
            if (n <= 0) return w;

            double N1 = n - 1;  // dénominateur pour fenêtres symétriques

            switch (t)
            {
                case BurstWindowType.Rectangular:
                    for (int i=0;i<n;i++) w[i]=1f;
                    break;

                case BurstWindowType.Tukey025:
                case BurstWindowType.Tukey050:
                case BurstWindowType.Tukey075:
                    float alpha = t == BurstWindowType.Tukey025 ? 0.25f
                                : t == BurstWindowType.Tukey050 ? 0.50f : 0.75f;
                    int flank = Math.Max(1, (int)(alpha * n / 2));
                    for (int i=0;i<n;i++)
                    {
                        if      (i < flank)      w[i] = 0.5f*(1f-(float)Math.Cos(Math.PI*i/flank));
                        else if (i >= n - flank)  w[i] = 0.5f*(1f-(float)Math.Cos(Math.PI*(n-i)/flank));
                        else                       w[i] = 1f;
                    }
                    break;

                case BurstWindowType.Cosine:
                    for (int i=0;i<n;i++) w[i] = (float)Math.Sin(Math.PI * i / N1);
                    break;

                case BurstWindowType.Hann:
                    for (int i=0;i<n;i++) w[i] = (float)(0.5 * (1-Math.Cos(2*Math.PI*i/N1)));
                    break;

                case BurstWindowType.Hamming:
                    for (int i=0;i<n;i++) w[i] = (float)(0.54 - 0.46*Math.Cos(2*Math.PI*i/N1));
                    break;

                case BurstWindowType.Blackman:
                    for (int i=0;i<n;i++)
                        w[i] = (float)(0.42 - 0.5*Math.Cos(2*Math.PI*i/N1) + 0.08*Math.Cos(4*Math.PI*i/N1));
                    break;

                case BurstWindowType.BlackmanHarris4:
                    for (int i=0;i<n;i++)
                    { double p=2*Math.PI*i/N1;
                      w[i]=(float)(0.35875-0.48829*Math.Cos(p)+0.14128*Math.Cos(2*p)-0.01168*Math.Cos(3*p)); }
                    break;

                case BurstWindowType.FlatTop:
                    for (int i=0;i<n;i++)
                    { double p=2*Math.PI*i/N1;
                      w[i]=(float)(0.21558-0.41663*Math.Cos(p)+0.27726*Math.Cos(2*p)-0.08358*Math.Cos(3*p)+0.00695*Math.Cos(4*p)); }
                    break;
            }
            return w;
        }

        /// <summary>Noms affichés dans le combo de la fenêtre burst.</summary>
        public static readonly (BurstWindowType Type, string Label)[] Labels =
        {
            (BurstWindowType.Rectangular,     "Rectangular"),
            (BurstWindowType.Tukey025,        "Tukey 0.25"),
            (BurstWindowType.Tukey050,        "Tukey 0.5"),
            (BurstWindowType.Tukey075,        "Tukey 0.75"),
            (BurstWindowType.Cosine,          "Cosine"),
            (BurstWindowType.Hann,            "Hann"),
            (BurstWindowType.Hamming,         "Hamming"),
            (BurstWindowType.Blackman,        "Blackman"),
            (BurstWindowType.BlackmanHarris4, "Blackman-Harris 4"),
            (BurstWindowType.FlatTop,         "Flat-Top"),
        };
    }

    // ── Dithering TPDF (wrapper) ──────────────────────────────────────────────
    /// <summary>
    /// Enveloppe un IWaveProvider et ajoute du dithering TPDF (bruit triangulaire)
    /// calibré pour la résolution cible (16 ou 32 bits).
    /// À utiliser pour les signaux périodiques (Sine, Square, Triangle, Sawtooth).
    /// </summary>
    internal sealed class DitheredProvider : IWaveProvider
    {
        private readonly IWaveProvider _source;
        private readonly float         _q;     // 1 LSB (amplitude du dither = 2 LSB)
        private uint                   _rng = 0x87654321u;

        public WaveFormat WaveFormat => _source.WaveFormat;

        public DitheredProvider(IWaveProvider source, int bits)
        {
            _source = source;
            // 1 LSB pour N bits signés : q = 1 / 2^(N-1)
            _q = (float)(1.0 / Math.Pow(2.0, bits - 1));
        }

        private float U() { _rng^=_rng<<13; _rng^=_rng>>17; _rng^=_rng<<5;
                            return ((_rng&0xFFFFFF)/(float)0x1000000)*2f-1f; }

        public int Read(byte[] buf, int off, int count)
        {
            int read = _source.Read(buf, off, count);
            var sp   = MemoryMarshal.Cast<byte, float>(new Span<byte>(buf, off, read));
            // TPDF : U1+U2 → distribution triangulaire dans [-2q, +2q]
            for (int i = 0; i < sp.Length; i++)
                sp[i] = Math.Clamp(sp[i] + (U() + U()) * _q, -1f, 1f);
            return read;
        }
    }

    // ── Factory ───────────────────────────────────────────────────────────────
    internal static class SignalProviderFactory
    {
        private static readonly SignalType[] _periodicTypes =
            { SignalType.Sine, SignalType.Square, SignalType.Triangle, SignalType.Sawtooth };

        public static IWaveProvider Create(in SignalParams p, int sr)
        {
            IWaveProvider prov = p.Type switch
            {
                SignalType.Sine       => new SineProvider(p, sr),
                SignalType.Square     => new WaveformProvider(p, sr),
                SignalType.Triangle   => new WaveformProvider(p, sr),
                SignalType.Sawtooth   => new WaveformProvider(p, sr),
                SignalType.WhiteNoise => new NoiseProvider(p, sr, false),
                SignalType.PinkNoise  => new NoiseProvider(p, sr, true),
                SignalType.Sweep      => new SweepProvider(p, sr),
                SignalType.TwoTone    => new TwoToneProvider(p, sr),
                SignalType.ToneBurst  => new ToneBurstProvider(p, sr),
                _ => throw new ArgumentException($"Unknown: {p.Type}")
            };

            // Dithering uniquement pour les signaux périodiques simples
            if (p.DitheringBits > 0 && Array.IndexOf(_periodicTypes, p.Type) >= 0)
                prov = new DitheredProvider(prov, p.DitheringBits);

            return prov;
        }
    }
}
