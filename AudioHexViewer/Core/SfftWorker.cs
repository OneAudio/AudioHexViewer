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

        // ---- Active channels (évite de calculer des FFT inutiles) ----
        // 1 = actif, 0 = inactif. Volatile pour lecture/écriture cross-thread.
        private volatile int _needLeft = 1;
        private volatile int _needRight = 1;

        /// <summary>
        /// Définit quelles voies doivent être calculées.
        /// Appelé par Form1 quand l'UI change de mode (Left/Right/L+R/L÷R).
        /// </summary>
        public void SetActiveChannels(bool needLeft, bool needRight)
        {
            _needLeft = needLeft ? 1 : 0;
            _needRight = needRight ? 1 : 0;
        }

        private CancellationTokenSource? _cts;
        private Task? _task;

        // Fenêtres de travail (réutilisées pour éviter allocations en boucle)
        private readonly float[] _winL;
        private readonly float[] _winR;

        // Buffers FFT (réutilisés) : évite 2 allocations de taille N à chaque hop
        private readonly MathNet.Numerics.Complex32[] _fftL;
        private readonly MathNet.Numerics.Complex32[] _fftR;

        // =============================================================
        // Pool local (taille exacte = N/2+1) pour éviter new Complex32[] à chaque hop.
        // IMPORTANT: taille fixe car fftSize fixe pour cette instance.
        // =============================================================
        private readonly ConcurrentBag<MathNet.Numerics.Complex32[]> _binsPool
            = new ConcurrentBag<MathNet.Numerics.Complex32[]>();

        private readonly int _binsLen; // = (fftSize/2)+1

        private MathNet.Numerics.Complex32[] RentBins()
        {
            return _binsPool.TryTake(out var a) ? a : new MathNet.Numerics.Complex32[_binsLen];
        }

        private void ReturnBins(MathNet.Numerics.Complex32[] a)
        {
            // Ici pas de Clear(): on réécrit toujours [0..half], donc inutile.
            _binsPool.Add(a);
        }


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
        object windowTypeObj, ConcurrentQueue<SpectrumFrame> outQueue)



        {
            if (fftSize <= 0) throw new ArgumentOutOfRangeException(nameof(fftSize));
            if (hop <= 0) throw new ArgumentOutOfRangeException(nameof(hop));
            if (hop > fftSize) throw new ArgumentException("hop must be <= fftSize");

            _ring = ring ?? throw new ArgumentNullException(nameof(ring));
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _sampleRate = sampleRate;

            _fftSize = fftSize;
            _hop = hop;

            _binsLen = (_fftSize / 2) + 1;

            _winL = new float[_fftSize];
            _winR = new float[_fftSize];

            _fftL = new MathNet.Numerics.Complex32[_fftSize];
            _fftR = new MathNet.Numerics.Complex32[_fftSize];

            _outQueue = outQueue ?? throw new ArgumentNullException(nameof(outQueue));

            // Fenêtre : dépend de la sélection UI (Windowing)
            _window = BuildWindow(_fftSize, windowTypeObj, out _windowSum);

            // Axe fréquence
            _freqAxis = AudioHexViewer.SpectrumAnalyzer.BuildFreqAxis(_sampleRate, _fftSize);

        }
        // 7-term Blackman-Harris window
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
        // 4-term Blackman-Harris window
        private static double[] BuildBlackmanHarris4(int N, out double sum)
        {
            sum = 0.0;
            var win = new double[N];
            if (N <= 1)
            {
                if (N == 1) { win[0] = 1.0; sum = 1.0; }
                return win;
            }

            // Coefficients BH4 (4-term Blackman-Harris, "classic" Nuttall / BH4)
            // w[n] = a0 - a1 cos(w) + a2 cos(2w) - a3 cos(3w)
            const double a0 = 0.35875;
            const double a1 = 0.48829;
            const double a2 = 0.14128;
            const double a3 = 0.01168;

            double denom = N - 1;
            for (int n = 0; n < N; n++)
            {
                double w = 2.0 * System.Math.PI * n / denom;
                double v = a0
                         - a1 * System.Math.Cos(w)
                         + a2 * System.Math.Cos(2.0 * w)
                         - a3 * System.Math.Cos(3.0 * w);

                win[n] = v;
                sum += v;
            }

            return win;
        }

        //
        private static double[] BuildWindow(int N, object windowTypeObj, out double sum)
        {
            string name = windowTypeObj?.ToString() ?? "BlackmanHarris7";

            switch (name)
            {
                case "Rectangular":
                    return BuildRectangular(N, out sum);

                case "Hann":
                    return BuildHann(N, out sum);

                case "Hamming":
                    return BuildHamming(N, out sum);

                case "BlackmanHarris4":
                    return BuildBlackmanHarris4(N, out sum);

                case "BlackmanHarris7":
                default:
                    return BuildBlackmanHarris7(N, out sum);
            }

        }

        private static double[] BuildRectangular(int N, out double sum)
        {
            sum = 0.0;
            var win = new double[N];
            for (int i = 0; i < N; i++)
            {
                win[i] = 1.0;
                sum += 1.0;
            }
            return win;
        }

        private static double[] BuildHann(int N, out double sum)
        {
            sum = 0.0;
            var win = new double[N];
            if (N <= 1)
            {
                if (N == 1) { win[0] = 1.0; sum = 1.0; }
                return win;
            }

            double denom = N - 1;
            for (int n = 0; n < N; n++)
            {
                double v = 0.5 * (1.0 - System.Math.Cos(2.0 * System.Math.PI * n / denom));
                win[n] = v;
                sum += v;
            }
            return win;
        }

        private static double[] BuildHamming(int N, out double sum)
        {
            sum = 0.0;
            var win = new double[N];
            if (N <= 1)
            {
                if (N == 1) { win[0] = 1.0; sum = 1.0; }
                return win;
            }

            double denom = N - 1;
            for (int n = 0; n < N; n++)
            {
                double v = 0.54 - 0.46 * System.Math.Cos(2.0 * System.Math.PI * n / denom);
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
                        _stats.SetAbsWrite(w);                        
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
                    _stats.SetAbsWrite(w); // publié en continu pour l’UI/diagnostics


                    // Backfill: tant qu'on a de quoi calculer une fenêtre finissant à nextHopEndAbs
                    // Condition: w >= nextHopEndAbs
                    if (w < _nextHopEndAbs)
                    {
                        // Progression entre 2 FFT successives (sur le HOP)
                        long lastEnd = _nextHopEndAbs - _hop;
                        double p01 = (double)(w - lastEnd) / _hop;

                        // clamp 0..1
                        if (p01 < 0.0) p01 = 0.0;
                        if (p01 > 1.0) p01 = 1.0;

                        _stats.SetNextEndAbs(_nextHopEndAbs);
                        _stats.SetProgress01(p01);
                        _stats.SetWorkerState("WAIT");

                        Thread.Sleep(1);
                        continue;
                    }




                    _stats.SetWorkerState("RUN");

                    long absStart = _nextHopEndAbs - _fftSize;
                    _stats.SetNextEndAbs(_nextHopEndAbs);
                    _stats.SetProgress01(1.0); // on a atteint le point où la FFT est "due"


                    bool ok = _ring.TryReadWindow(absStart, _fftSize, _winL, _winR);
                    if (ok)
                    {
                        // Mesure temps CPU de cette frame FFT (window fill + FFT + normalisation + enqueue)
                        long t0 = Stopwatch.GetTimestamp();

                        // Lecture des flags une seule fois (évite variations en plein calcul)
                        bool doL = (_needLeft != 0);
                        bool doR = (_needRight != 0);

                        int N = _fftSize;
                        int half = N / 2;

                        // Remplissage des buffers FFT réutilisés (uniquement voies actives)
                        // NOTE: on évite la voie inactive => gain CPU direct (boucle + FFT)
                        for (int i = 0; i < N; i++)
                        {
                            double wcoef = _window[i];

                            if (doL)
                            {
                                double wl = _winL[i] * wcoef;
                                _fftL[i] = new MathNet.Numerics.Complex32((float)wl, 0f);
                            }

                            if (doR)
                            {
                                double wr = _winR[i] * wcoef;
                                _fftR[i] = new MathNet.Numerics.Complex32((float)wr, 0f);
                            }
                        }

                        // FFT uniquement sur les voies demandées
                        if (doL) Fourier.Forward(_fftL, FourierOptions.Matlab);
                        if (doR) Fourier.Forward(_fftR, FourierOptions.Matlab);

                        // Bins complexes poolés UNIQUEMENT si nécessaires
                        MathNet.Numerics.Complex32[]? binsL = null;
                        MathNet.Numerics.Complex32[]? binsR = null;

                        if (doL) binsL = RentBins();
                        if (doR) binsR = RentBins();

                        try
                        {
                            // Normalisation amplitude (inchangée)
                            float scale2 = (float)(2.0 / _windowSum);
                            float scale1 = (float)(1.0 / _windowSum);

                            for (int k = 0; k <= half; k++)
                            {
                                float s = (k == 0 || k == half) ? scale1 : scale2;

                                if (binsL != null) binsL[k] = _fftL[k] * s;
                                if (binsR != null) binsR[k] = _fftR[k] * s;
                            }

                            // Publication frame (LeftBins/RightBins peuvent être null)
                            int fs = _stats.Snapshot().SampleRate;

                            var frame = new SpectrumFrame(fs, N, _nextHopEndAbs, binsL, binsR, _freqAxis, ReturnBins);
                            _outQueue.Enqueue(frame);

                            _stats.IncFramesComputed();

                            // Fin mesure (µs)
                            long t1 = Stopwatch.GetTimestamp();
                            long us = (long)((t1 - t0) * (1_000_000.0 / Stopwatch.Frequency));
                            _stats.ReportFftTimeUs(us);

                            // Ownership transféré à la frame => ne pas rendre ici
                            binsL = null;
                            binsR = null;
                        }
                        finally
                        {
                            // Si exception avant enqueue, rendre au pool seulement ce qui a été loué
                            if (binsL != null) ReturnBins(binsL);
                            if (binsR != null) ReturnBins(binsR);
                        }
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

                        // IMPORTANT : LastError redevient "réservé aux erreurs".
                        // La cadence FFT (télémétrie) vit dans SfftStats.
                        _stats.SetFftPerSec(fps);

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
