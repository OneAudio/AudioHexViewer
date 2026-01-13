using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics;
using System.Collections.Concurrent;


namespace AudioHexViewer.Core
{
    internal sealed class SfftWorker : IDisposable
    {
        private readonly RingBufferStereoFloat _ring;
        private readonly SfftStats _stats;
        private readonly int _sampleRate;


        private readonly int _fftSize;
        private readonly int _hop;

        private CancellationTokenSource? _cts;
        private Task? _task;

        // Fenêtres de travail (réutilisées pour éviter allocations en boucle)
        private readonly float[] _winL;
        private readonly float[] _winR;

        private readonly ConcurrentQueue<SpectrumFrame> _outQueue;

        private readonly double[] _window;   // fenêtre pré-calculée
        private readonly double _windowSum;  // somme des coefficients
        private readonly double[] _freqAxis; // pré-calculé

        // Horloge / indices absolus
        private long _nextHopEndAbs;

        // Diagnostics
        private readonly Stopwatch _sw = new Stopwatch();
        private long _framesAtLastRate;
        private double _lastRateTime;

        public SfftWorker(RingBufferStereoFloat ring, SfftStats stats, int sampleRate, int fftSize, int hop,
          ConcurrentQueue<SpectrumFrame> outQueue)


        {
            if (fftSize <= 0) throw new ArgumentOutOfRangeException(nameof(fftSize));
            if (hop <= 0) throw new ArgumentOutOfRangeException(nameof(hop));
            if (hop > fftSize) throw new ArgumentException("hop must be <= fftSize");

            _ring = ring ?? throw new ArgumentNullException(nameof(ring));
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _sampleRate = sampleRate;

            _fftSize = fftSize;
            _hop = hop;

            _winL = new float[_fftSize];
            _winR = new float[_fftSize];

            _outQueue = outQueue ?? throw new ArgumentNullException(nameof(outQueue));

            // Fenêtre : pour l’instant BH7 (on connectera l’UI Windowing après)
            _window = BuildBlackmanHarris7(_fftSize, out _windowSum);

            // Axe fréquence
            _freqAxis = AudioHexViewer.SpectrumAnalyzer.BuildFreqAxis(_sampleRate, _fftSize);

        }
        //
        private static double[] BuildBlackmanHarris7(int N, out double sum)
        {
            sum = 0.0;
            var win = new double[N];
            if (N <= 1)
            {
                if (N == 1) { win[0] = 1.0; sum = 1.0; }
                return win;
            }

            // mêmes coefficients que SpectrumAnalyzer.BlackmanHarris7
            const double a0 = 0.27105140069342;
            const double a1 = 0.43329793923448;
            const double a2 = 0.21812299954311;
            const double a3 = 0.06592544638803;
            const double a4 = 0.01081174209837;
            const double a5 = 0.00077658482522;
            const double a6 = 0.00001388721735;

            double denom = N - 1;
            for (int n = 0; n < N; n++)
            {
                double w = 2.0 * System.Math.PI * n / denom;
                double v = a0
                         - a1 * System.Math.Cos(w)
                         + a2 * System.Math.Cos(2.0 * w)
                         - a3 * System.Math.Cos(3.0 * w)
                         + a4 * System.Math.Cos(4.0 * w)
                         - a5 * System.Math.Cos(5.0 * w)
                         + a6 * System.Math.Cos(6.0 * w);

                win[n] = v;
                sum += v;
            }

            return win;
        }

        //
        public void Start()
        {
            if (_task != null) return;

            _cts = new CancellationTokenSource();
            _task = Task.Run(() => RunLoop(_cts.Token));
        }

        public void Stop()
        {
            if (_cts == null) return;

            _cts.Cancel();
            try { _task?.Wait(2000); } catch { /* ignore */ }

            _task = null;
            _cts.Dispose();
            _cts = null;

            _stats.SetWorkerState("STOP");
        }

        private void RunLoop(CancellationToken ct)
        {
            try
            {
                _stats.SetWorkerState("RUN");

                // Format/paramètres visibles côté UI
                _stats.SetFormat(sampleRate: _sampleRate, fftSize: _fftSize, hop: _hop,
                 overlapRequested01: 1.0 - ((double)_hop / _fftSize));


                // Initialisation de l’index "nextHopEndAbs"    
                // Règle simple : on attend d’avoir au moins N samples écrits,
                // puis on positionne nextHopEndAbs = AbsWrite (arrondi sur un multiple de hop si besoin).
                while (!ct.IsCancellationRequested)
                {
                    long w = _ring.AbsWrite;
                    if (w >= _fftSize)
                    {
                        _nextHopEndAbs = w; // premier hop-end = "maintenant"
                        _stats.SetNextEndAbs(_nextHopEndAbs);
                        _stats.SetProgress01(1.0);
                        break;
                    }
                    _stats.SetWorkerState("WAIT");
                    Thread.Sleep(1);
                }

                _stats.SetWorkerState("RUN");
                
                _sw.Restart();
                _lastRateTime = 0;
                _framesAtLastRate = 0;

                while (!ct.IsCancellationRequested)
                {
                    long w = _ring.AbsWrite;

                    // Backfill: tant qu'on a de quoi calculer une fenêtre finissant à nextHopEndAbs
                    // Condition: w >= nextHopEndAbs
                    if (w < _nextHopEndAbs)
                    {
                        // Publie où on en est dans la "course" vers la prochaine FFT.
                        // lastEnd = end de la dernière FFT produite (ou end précédent logique)
                        long lastEnd = _nextHopEndAbs - _hop;

                        // Progression 0..1 : combien de samples du hop ont été acquis depuis lastEnd
                        double p01 = (double)(w - lastEnd) / _hop;

                        _stats.SetNextEndAbs(_nextHopEndAbs);
                        _stats.SetProgress01(p01);
                        _stats.SetWorkerState("WAIT");

                        Thread.Sleep(1);
                        continue;
                    }


                    _stats.SetWorkerState("RUN");

                    long absStart = _nextHopEndAbs - _fftSize;

                    bool ok = _ring.TryReadWindow(absStart, _fftSize, _winL, _winR);
                    if (ok)
                    {
                        // --- FFT réelle (Étape 3) ---
                        // Convertit float [-1..1] -> double, applique fenêtre, FFT Complex32, magnitude -> dBFS

                        int N = _fftSize;
                        int bins = N / 2 + 1;

                        var fftL = new MathNet.Numerics.Complex32[N];
                        var fftR = new MathNet.Numerics.Complex32[N];

                        for (int i = 0; i < N; i++)
                        {
                            double wl = _winL[i] * _window[i];
                            double wr = _winR[i] * _window[i];
                            fftL[i] = new MathNet.Numerics.Complex32((float)wl, 0f);
                            fftR[i] = new MathNet.Numerics.Complex32((float)wr, 0f);
                        }

                        Fourier.Forward(fftL, FourierOptions.Matlab);
                        Fourier.Forward(fftR, FourierOptions.Matlab);

                        // Scale dBFS : full-scale = 1.0
                        // Fenêtrage : on normalise comme SpectrumAnalyzer (2 / windowSum)
                        double scale = 2.0 / (_windowSum > 0.0 ? _windowSum : N);

                        var specDbL = new double[bins];
                        var specDbR = new double[bins];

                        for (int k = 0; k < bins; k++)
                        {
                            double ampL = fftL[k].Magnitude * scale;
                            double ampR = fftR[k].Magnitude * scale;

                            double dbL = (ampL <= 0.0) ? -200.0 : 20.0 * System.Math.Log10(ampL);
                            double dbR = (ampR <= 0.0) ? -200.0 : 20.0 * System.Math.Log10(ampR);

                            if (dbL < -200.0) dbL = -200.0; if (dbL > 0.0) dbL = 0.0;
                            if (dbR < -200.0) dbR = -200.0; if (dbR > 0.0) dbR = 0.0;

                            specDbL[k] = dbL;
                            specDbR[k] = dbR;
                        }

                        // Publication : on envoie un frame "prêt à afficher"
                        int fs = _stats.Snapshot().SampleRate;
                        var frame = new SpectrumFrame(fs, N, _nextHopEndAbs, specDbL, specDbR, _freqAxis);
                        _outQueue.Enqueue(frame);

                        _stats.IncFramesComputed();
                    }

                    else
                    {
                        // Fenêtre écrasée / incohérente
                        _stats.IncFramesSkipped();
                        _stats.SetLastError($"Window miss: [{absStart}..{_nextHopEndAbs}) w={w}");
                    }

                    // Avance exactement d'un hop
                    _nextHopEndAbs += _hop;
                    _stats.SetNextEndAbs(_nextHopEndAbs);
                    _stats.SetProgress01(0.0); // on repart pour le hop suivant

                    // (Option) petit yield
                    Thread.Yield();

                    // Diagnostics cadence (mise à jour toutes ~0.5s)
                    double t = _sw.Elapsed.TotalSeconds;
                    if (t - _lastRateTime > 0.5)
                    {
                        var snap = _stats.Snapshot();
                        long frames = snap.FramesComputed;
                        double dt = t - _lastRateTime;
                        long df = frames - _framesAtLastRate;
                        double fps = (dt > 1e-9) ? (df / dt) : 0.0;

                        // On stocke le fps dans LastError pour l’instant (temporaire)
                        _stats.SetLastError($"FFT/s~{fps:F1}  nextEnd={_nextHopEndAbs}  w={w}");

                        _framesAtLastRate = frames;
                        _lastRateTime = t;
                    }
                }
            }
            catch (Exception ex)
            {
                _stats.SetWorkerState("STOP");
                _stats.SetLastError(ex.ToString());
            }
        }

        public void Dispose() => Stop();
    }
}
