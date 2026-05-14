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
        /// Appelé par Form1 quand l'UI change de mode (Left/Right/L+R/Bode Plot).
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
        WindowType windowType, ConcurrentQueue<SpectrumFrame> outQueue)



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

            // Fenêtre : via WindowFactory (cache partagé, zéro allocation en chemin chaud)
            var winEntry = WindowFactory.Get(windowType, _fftSize);
            _window    = winEntry.Coeffs;
            _windowSum = winEntry.Sum;

            // Axe fréquence
            _freqAxis = AudioHexViewer.SpectrumAnalyzer.BuildFreqAxis(_sampleRate, _fftSize);

        }
        //
        public void Start()
        {
            if (_task != null) return;

            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            // LongRunning : thread dédié (pas le thread pool) → pas de délai scheduler
            // AboveNormal : priorité > UI (Normal) mais < WASAPI (Highest)
            // Objectif : garantir que SfftWorker reprend la main en < 15ms après
            // un Thread.Sleep(1), ce qui laisse >65ms de marge par hop à 384kHz/4Mpts.
            _task = new Task(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
                Thread.CurrentThread.Name = "SfftWorker";
                RunLoop(ct);
            }, ct, TaskCreationOptions.LongRunning);
            _task.Start();
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
                        // ── Diagnostic intégrité données ──────────────────────────────
                        // Sondage rapide : on vérifie 1 sample sur 256 (N/256 opérations)
                        int zeroCount = 0;
                        int jumpCount = 0;
                        int checkStep = Math.Max(1, _fftSize / 256);
                        float prevSample = _winL[0];
                        float sumAbs = 0f;

                        for (int ci = 0; ci < _fftSize; ci += checkStep)
                        {
                            float s = _winL[ci];
                            sumAbs += Math.Abs(s);
                            if (s == 0.0f) zeroCount++;
                            if (ci > 0 && Math.Abs(s - prevSample) > 0.5f) jumpCount++;
                            prevSample = s;
                        }

                        int checkCount = _fftSize / checkStep;
                        float avgAbs = sumAbs / checkCount;
                        float zeroFrac = (float)zeroCount / checkCount;
                        bool suspicious = zeroFrac > 0.01f || jumpCount > 2;

                        // Le frame est TOUJOURS calculé et publié, même si suspect.
                        // jumpCount > 2 est non-fiable : déclenche sur des signaux normaux
                        // (hauts niveaux, harmoniques, DC offset). On se contente de loguer.
                        if (suspicious)
                            _stats.SetLastError(
                                $"INTEG: zeros={zeroFrac:P1} sauts={jumpCount} avgAbs={avgAbs:F5}  " +
                                $"[absStart={absStart}]");
                        // ─────────────────────────────────────────────────────────────

                        // Mesure temps CPU de cette frame FFT (window fill + FFT + normalisation + enqueue)
                        long t0 = Stopwatch.GetTimestamp();

                        // Lecture des flags une seule fois (évite variations en plein calcul)
                        bool doL = (_needLeft != 0);
                        bool doR = (_needRight != 0);

                        int N = _fftSize;
                        int half = N / 2;

                        // Remplissage des buffers FFT réutilisés (uniquement voies actives)
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

                        MathNet.Numerics.Complex32[]? binsL = null;
                        MathNet.Numerics.Complex32[]? binsR = null;

                        if (doL) binsL = RentBins();
                        if (doR) binsR = RentBins();

                        try
                        {
                            float scale2 = (float)(2.0 / _windowSum);
                            float scale1 = (float)(1.0 / _windowSum);

                            for (int k = 0; k <= half; k++)
                            {
                                float s = (k == 0 || k == half) ? scale1 : scale2;
                                if (binsL != null) binsL[k] = _fftL[k] * s;
                                if (binsR != null) binsR[k] = _fftR[k] * s;
                            }

                            int fs = _stats.Snapshot().SampleRate;
                            var frame = new SpectrumFrame(fs, N, _nextHopEndAbs, binsL, binsR, _freqAxis, ReturnBins);
                            _outQueue.Enqueue(frame);

                            _stats.IncFramesComputed();

                            long t1 = Stopwatch.GetTimestamp();
                            long us = (long)((t1 - t0) * (1_000_000.0 / Stopwatch.Frequency));
                            _stats.ReportFftTimeUs(us);

                            binsL = null;
                            binsR = null;
                        }
                        finally
                        {
                            if (binsL != null) ReturnBins(binsL);
                            if (binsR != null) ReturnBins(binsR);
                        }
                    } // fin if (ok)
                    else
                    {
                        // Fenêtre écrasée : le write pointer a dépassé absStart
                        _stats.IncFramesSkipped();
                        _stats.SetLastError($"Window miss: [{absStart}..{_nextHopEndAbs}) w={w}");

                        // RESYNC : sans ça, le worker avance hop par hop pendant que
                        // le writer continue → cascade de miss irrécupérable.
                        // On saute directement au write pointer actuel (arrondi au hop sup)
                        // pour retrouver la synchro en une seule miss au lieu de des centaines.
                        long hopsToSkip = (w - _nextHopEndAbs + _hop - 1) / _hop;
                        if (hopsToSkip > 0)
                        {
                            _nextHopEndAbs += hopsToSkip * _hop;
                            _stats.SetNextEndAbs(_nextHopEndAbs);
                        }
                        else
                        {
                            _nextHopEndAbs += _hop; // avance minimale de sécurité
                        }
                        continue; // retour en tête de boucle, pas de double avance
                    }

                    // Avance exactement d'un hop (cas normal : ok == true)
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
