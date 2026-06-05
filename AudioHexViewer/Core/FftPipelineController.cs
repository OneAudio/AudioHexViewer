using System;
using System.Buffers;
using System.Collections.Concurrent;

namespace AudioHexViewer.Core
{
    /// <summary>
    /// Contrôleur du pipeline FFT temps-réel.
    ///
    /// Responsabilités :
    ///   1. Posséder le <see cref="RingBufferStereoFloat"/> (source unique de vérité audio).
    ///   2. Gérer le cycle de vie du <see cref="SfftWorker"/> (Start / Stop / reconfiguration).
    ///   3. Exposer la queue de frames FFT pour SpectrumControl.
    ///   4. Fournir des snapshots temporels (float) depuis le ring, pour le Histogram
    ///      et les statistiques RMS/Peak/Vdc de SpectrumControl.
    ///
    /// Ce contrôleur remplace les champs dispersés dans Form1 :
    ///   _floatRing, _sfftWorker, _sfftStats, _sfftFrames,
    ///   _analysisWinL/R et leur infrastructure (lock, indices, snapshot buffers).
    /// </summary>
    internal sealed class FftPipelineController : IDisposable
    {
        // ── Composants internes ───────────────────────────────────────────────
        private RingBufferStereoFloat? _ring;
        private SfftWorker?            _worker;
        private readonly SfftStats     _stats = new SfftStats();
        private readonly ConcurrentQueue<SpectrumFrame> _frames = new ConcurrentQueue<SpectrumFrame>();

        // Buffers float pré-alloués pour le snapshot temporel (réalloués uniquement si N change)
        private float[] _snapshotL = Array.Empty<float>();
        private float[] _snapshotR = Array.Empty<float>();

        // ── Propriétés publiques ──────────────────────────────────────────────

        /// <summary>Ring buffer stéréo float (lecture seule pour les consommateurs).</summary>
        public RingBufferStereoFloat? Ring => _ring;

        /// <summary>Statistiques du worker FFT (thread-safe via Snapshot()).</summary>
        public SfftStats Stats => _stats;

        /// <summary>Queue de frames FFT produites par SfftWorker, consommées par SpectrumControl.</summary>
        public ConcurrentQueue<SpectrumFrame> Frames => _frames;

        /// <summary>True si un worker est actuellement actif.</summary>
        public bool IsRunning => _worker != null;

        // ── Cycle de vie ──────────────────────────────────────────────────────

        /// <summary>
        /// (Re)démarre le pipeline avec les paramètres donnés.
        /// Arrête et recrée le worker si un était déjà actif.
        /// </summary>
        public void Start(int sampleRate, int fftSize, int hop, WindowType windowType)
        {
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
            if (fftSize   <= 0) throw new ArgumentOutOfRangeException(nameof(fftSize));
            if (hop       <= 0) throw new ArgumentOutOfRangeException(nameof(hop));

            // 1) Arrêter le worker existant
            StopWorker();

            // 2) (Re)créer le ring — capacité = max(35 secondes, 6×N)
            //    35s couvre le pire cas scope : 1s/div × 10 divs × 3 (search) ≈ 32s à n'importe quel fs.
            //    6×N garantit que le worker FFT ne se bloque jamais en attente de données.
            int ringCapacity = Math.Max(sampleRate * 35, fftSize * 6);
            _ring = new RingBufferStereoFloat(ringCapacity);

            // 3) Vider la queue (évite le mélange ancien/nouveau N)
            DrainFrameQueue();

            // 4) Publier le format immédiatement (UI n'attend pas la 1ère FFT)
            _stats.ResetCounters();
            _stats.SetFormat(sampleRate, fftSize, hop,
                             overlapRequested01: 1.0 - ((double)hop / fftSize));

            // 5) Démarrer le nouveau worker
            _worker = new SfftWorker(_ring, _stats, sampleRate, fftSize, hop, windowType, _frames);
            _worker.Start();
        }

        /// <summary>
        /// Arrête le worker et vide la queue.
        /// Le ring buffer est conservé (les données restent lisibles jusqu'au prochain Start).
        /// </summary>
        public void Stop()
        {
            StopWorker();
            DrainFrameQueue();
            _stats.SetWorkerState("STOP");
        }

        // ── Ingestion audio ───────────────────────────────────────────────────

        /// <summary>
        /// Pousse un bloc stéréo float [-1..1] dans le ring buffer.
        /// Appelé depuis le thread de capture (ProcessBuffer) ou le générateur de test.
        /// </summary>
        public void PushAudio(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
        {
            _ring?.PushBlock(left, right);
        }

        // ── Snapshot temporel ─────────────────────────────────────────────────

        /// <summary>
        /// Lit les <paramref name="n"/> derniers samples float depuis le ring et les
        /// convertit en int24 (échelle 0x7FFFFF) dans <paramref name="outL"/> /
        /// <paramref name="outR"/>.
        ///
        /// Remplace le double pipeline _analysisWinL/R (int24 ring parallèle) :
        /// une seule source de vérité = <see cref="Ring"/>.
        ///
        /// Retourne <c>false</c> si le ring n'a pas encore accumulé assez de données.
        /// </summary>
        public bool TrySnapshotAsInt24(int n, out int[] outL, out int[] outR)
        {
            outL = Array.Empty<int>();
            outR = Array.Empty<int>();

            var ring = _ring;
            if (ring == null || n <= 0) return false;

            long absEnd   = ring.AbsWrite;
            long absStart = absEnd - n;
            if (absStart < 0) return false;   // pas encore assez de données

            // Réallouer uniquement si N change (changement de config par l'utilisateur)
            if (_snapshotL.Length != n)
            {
                _snapshotL = new float[n];
                _snapshotR = new float[n];
            }

            if (!ring.TryReadWindow(absStart, n, _snapshotL, _snapshotR))
                return false;

            // Conversion float → int24
            var l = new int[n];
            var r = new int[n];
            const float scale    =  8388608f;   // 2^23 (même convention que ProcessBuffer : LSB = 1/2^23)
            const int   max24pos =  8388607;
            const int   min24neg = -8388608;

            for (int i = 0; i < n; i++)
            {
                int lv = (int)(_snapshotL[i] * scale);
                int rv = (int)(_snapshotR[i] * scale);

                if (lv > max24pos) lv = max24pos;
                if (lv < min24neg) lv = min24neg;
                if (rv > max24pos) rv = max24pos;
                if (rv < min24neg) rv = min24neg;

                l[i] = lv;
                r[i] = rv;
            }

            outL = l;
            outR = r;
            return true;
        }

        // ── Contrôle des voies actives ────────────────────────────────────────

        /// <summary>
        /// Informe le worker des voies à calculer (optimisation CPU).
        /// Appel thread-safe.
        /// </summary>
        public void SetActiveChannels(bool needLeft, bool needRight)
        {
            _worker?.SetActiveChannels(needLeft, needRight);
        }

        /// <summary>
        /// Active ou désactive le retrait de la composante DC avant FFT (mode AC/DC).
        /// Délègue à <see cref="SfftWorker.SetRemoveDc"/> ET vide la queue de frames
        /// pour que l'affichage reflète le nouveau mode sans délai.
        /// </summary>
        /// <remarks>
        /// Sans le drain, les frames déjà calculées avec l'ancien mode restent en queue
        /// et continuent à s'afficher plusieurs secondes. Le drain les supprime
        /// immédiatement ; le worker remplit la queue avec des frames au nouveau mode
        /// dès le prochain hop.
        /// </remarks>
        public void SetRemoveDc(bool remove)
        {
            _worker?.SetRemoveDc(remove);
            DrainFrameQueue();   // vide les frames périmées → affichage immédiat
        }

        // ── Diagnostics ring ─────────────────────────────────────────────────

        public double LockWaitMaxMs  => _ring?.LockWaitMaxMs ?? 0.0;
        public double LockWaitAvgMs  => _ring?.LockWaitAvgMs ?? 0.0;
        public long   LockWaitCount  => _ring?.LockWaitCount ?? 0;

        public void ResetLockStats() => _ring?.ResetLockStats();

        // ── IDisposable ───────────────────────────────────────────────────────
        public void Dispose()
        {
            Stop();
            _ring = null;
        }

        // ── Helpers privés ────────────────────────────────────────────────────
        private void StopWorker()
        {
            if (_worker == null) return;
            _worker.Stop();
            _worker = null;
        }

        private void DrainFrameQueue()
        {
            while (_frames.TryDequeue(out var f))
                f.Dispose();
        }
    }
}
