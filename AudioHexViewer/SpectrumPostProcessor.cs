using MathNet.Numerics;
using System;
using System.Linq;

namespace AudioHexViewer
{
    internal sealed class SpectrumPostProcessor
    {
        internal enum AveragingMode
        {
            None = 0,
            Count = 1,        // moyenne sur N frames (fenêtre glissante)
            Exponential = 2   // Y = aY + (1-a)X
        }

        // --- Exponential averaging state (en puissance linéaire) ---
        private bool _expInit = false;
        private double _expAlpha = 0.0;
        private double[]? _expLeftPower = null;
        private double[]? _expRightPower = null;


        // --- Moving average (glissant sur N frames) en puissance ---
        private int _avgRingPos = 0;
        private int _avgFillCount = 0;

        private bool _avgHasLeft = false;
        private bool _avgHasRight = false;

        private double[]? _avgSumLeftPower = null;
        private double[]? _avgSumRightPower = null;

        private double[][]? _avgRingLeftPower = null;
        private double[][]? _avgRingRightPower = null;

        // --- Peak-hold (en dBFS) ---
        private double[]? _peakHoldLeftDb;
        private double[]? _peakHoldRightDb;

        // ------------------------------------------------------------------
        // --- FRF / Bode (Welch cross-spectrum) state ---
        // Right = entrée X, Left = sortie Y
        // Gxx = <X * conj(X)>   (réel, positif)
        // Gxy = <X * conj(Y)>   (complex)
        // ------------------------------------------------------------------

        private bool _frfInit = false;
        private AveragingMode _frfMode = AveragingMode.None;
        private int _frfAvgTarget = 0;
        private double _frfExpAlpha = 0.0;

        private double[]? _frfGxx;          // auto-spectrum entrée
        private System.Numerics.Complex[]? _frfGxy; // cross-spectrum
        private int _frfFillCount = 0;

        // ════════════════════════════════════════════════════════════════════
        // Coherent averaging pour THD — suivi de phase mesuré frame-à-frame
        //
        // PRINCIPE (v4 — phase tracking)
        // ────────────────────────────────
        // Au lieu de calculer la correction analytiquement à partir de f0_estimé
        // (qui accumule une erreur de phase ε·n → oscillation après quelques secondes),
        // on MESURE la phase réelle du bin dominant à chaque frame et on accumule
        // le drift observé directement :
        //
        //   Δφ_n  = arg(bins_n[k0]) − arg(bins_{n-1}[k0])   (unwrap ∈ [−π, π])
        //   Φ_n   = Φ_{n-1} + Δφ_n   (phase cumulée de la fondamentale)
        //
        // Correction pour l'harmonique h :  exp(−j·h·Φ_n)
        //
        // Avantages :
        //   • Aucune dépendance à f0_est → zéro erreur d'estimation
        //   • Fonctionne même si f0 dérive lentement (oscillateur imparfait)
        //   • API simplifiée : plus besoin de absEnd/fftSize/sampleRate
        //   • Harmoniques parfaitement alignés pour N quelconque
        // ════════════════════════════════════════════════════════════════════
        private System.Numerics.Complex[]? _cohSumL;
        private System.Numerics.Complex[]? _cohSumR;
        private bool   _cohHasL;
        private bool   _cohHasR;
        private int    _cohCount;
        private int    _cohTarget;

        // Phase tracking (mis à jour chaque frame)
        private int    _cohK0;          // bin dominant détecté sur la frame 0 (fixe)
        private double _cohK0real;      // position fractionnaire du bin (pour le groupage)
        private double _cohPrevPhase;   // phase mesurée à la frame précédente
        private double _cohCumPhase;    // phase cumulée de la fondamentale (Φ_n)

        /// <summary>Nombre de frames déjà accumulées dans le cycle courant.</summary>
        public int CohAvgCount => _cohCount;

        /// <summary>Nombre de frames cible configuré.</summary>
        public int CohAvgTarget => _cohTarget;

        /// <summary>True si le nombre cible est atteint (l'accumulation continue au-delà).</summary>
        public bool CohAvgComplete => (_cohTarget > 0 && _cohCount >= _cohTarget);

        // ── Reset ─────────────────────────────────────────────────────────
        public void ResetCohAvgState()
        {
            _cohSumL       = null;
            _cohSumR       = null;
            _cohCount      = 0;
            _cohTarget     = 0;
            _cohHasL       = false;
            _cohHasR       = false;
            _cohK0         = 0;
            _cohK0real     = 0.0;
            _cohPrevPhase  = 0.0;
            _cohCumPhase   = 0.0;
        }

        /// <summary>
        /// Accumule les bins complexes d'une frame FFT avec correction de phase
        /// basée sur le suivi de la phase mesurée du bin fondamental.
        ///
        /// Algorithme :
        ///   Frame 0 : détecte le bin dominant k0 ; initialise le tracking de phase.
        ///   Frame n : mesure Δφ = arg(bins_n[k0]) − arg(bins_{n-1}[k0])  [unwrappé]
        ///             accumule Φ += Δφ  (phase cumulée réelle du signal)
        ///             pour chaque harmonique h : rotation = exp(−j·h·Φ)
        ///             → somme cohérente exacte, indépendante de f0_estimé
        /// </summary>
        public void AccumulateCohAvg(
            MathNet.Numerics.Complex32[]? leftBins,
            MathNet.Numerics.Complex32[]? rightBins,
            int target)
        {
            bool hasL = (leftBins  != null);
            bool hasR = (rightBins != null);
            int bins  = hasL ? leftBins!.Length : hasR ? rightBins!.Length : 0;
            if (bins == 0 || target <= 0) return;

            // ── Reset si la configuration a changé ──────────────────────────
            bool needReset =
                (_cohSumL == null && hasL) ||
                (_cohSumR == null && hasR) ||
                (hasL && _cohSumL != null && _cohSumL.Length != bins) ||
                (hasR && _cohSumR != null && _cohSumR.Length != bins) ||
                _cohTarget != target ||
                _cohHasL   != hasL  ||
                _cohHasR   != hasR;

            if (needReset)
            {
                _cohSumL      = hasL ? new System.Numerics.Complex[bins] : null;
                _cohSumR      = hasR ? new System.Numerics.Complex[bins] : null;
                _cohCount     = 0;
                _cohTarget    = target;
                _cohHasL      = hasL;
                _cohHasR      = hasR;
                _cohK0        = 0;
                _cohK0real    = 0.0;
                _cohPrevPhase = 0.0;
                _cohCumPhase  = 0.0;
            }

            // ── Canal de référence pour la mesure de phase ──────────────────
            var refBins = hasL ? leftBins! : rightBins!;

            // ── Frame 0 : initialisation ─────────────────────────────────────
            if (_cohCount == 0)
            {
                // Détecter le bin dominant
                _cohK0 = CohFindDominantBin(refBins, 1, bins - 2);

                // Position fractionnaire k0_real pour le groupage harmonique
                _cohK0real = CohInterpolateK0Real(refBins, _cohK0);

                // Phase initiale = référence (Φ_0 = 0)
                _cohPrevPhase = Math.Atan2(refBins[_cohK0].Imaginary, refBins[_cohK0].Real);
                _cohCumPhase  = 0.0;
                // Frame 0 accumulée avec Φ=0 (pas de rotation)
            }
            else
            {
                // ── Frame n>0 : mesure du drift de phase ─────────────────────
                double phiNow = Math.Atan2(refBins[_cohK0].Imaginary, refBins[_cohK0].Real);

                // Différence de phase (unwrap dans [−π, π])
                double deltaPhi = phiNow - _cohPrevPhase;
                if      (deltaPhi >  Math.PI) deltaPhi -= 2.0 * Math.PI;
                else if (deltaPhi < -Math.PI) deltaPhi += 2.0 * Math.PI;

                _cohCumPhase  += deltaPhi;
                _cohPrevPhase  = phiNow;
            }

            // ── Accumulation avec correction de phase ────────────────────────
            if (hasL && _cohSumL != null)
                CohAccumulateChannel(_cohSumL, leftBins!, bins, _cohCumPhase, _cohK0real);

            if (hasR && _cohSumR != null)
                CohAccumulateChannel(_cohSumR, rightBins!, bins, _cohCumPhase, _cohK0real);

            _cohCount++;
        }

        // ── Accumulation d'un canal avec correction par groupe harmonique ────
        private static void CohAccumulateChannel(
            System.Numerics.Complex[]    sum,
            MathNet.Numerics.Complex32[] bins,
            int    binCount,
            double cumPhase,    // Φ_n : phase cumulée mesurée de la fondamentale
            double k0real)      // bin fractionnaire pour les limites de groupes
        {
            // Groupe harmonique h couvre les bins k ∈ [kLow_h, kHigh_h].
            // Tous les bins d'un même groupe reçoivent la MÊME rotation.
            // → 1 seul cos/sin par harmonique, quelle que soit la taille de N.
            //
            // Correction : exp(−j·h·Φ_n)
            // Φ_n est la phase cumulée MESURÉE → exacte, sans erreur d'estimation.

            int kCursor = 0;
            int h = 1;

            while (kCursor < binCount)
            {
                // Limites du groupe h (h=1 part de k=0 pour couvrir les basses fréquences)
                int kLow  = (h == 1) ? 0 : (int)Math.Round((h - 0.5) * k0real);
                int kHigh = (int)Math.Round((h + 0.5) * k0real) - 1;
                kLow  = Math.Max(kCursor, kLow);
                kHigh = Math.Min(binCount - 1, kHigh);

                if (kLow > kHigh) { h++; continue; }

                // Rotation : exp(−j·h·Φ_n)
                // cumPhase est borné automatiquement car c'est une somme de Δφ ∈ [−π,π]
                double angle = -(double)h * cumPhase;
                double cosA  = Math.Cos(angle);
                double sinA  = Math.Sin(angle);

                for (int k = kLow; k <= kHigh; k++)
                {
                    float re = bins[k].Real;
                    float im = bins[k].Imaginary;
                    sum[k] += new System.Numerics.Complex(
                        re * cosA - im * sinA,
                        re * sinA + im * cosA);
                }

                kCursor = kHigh + 1;
                h++;
            }
        }

        // ── Détection du bin dominant (max |bin|²) en excluant DC et Nyquist ─
        private static int CohFindDominantBin(
            MathNet.Numerics.Complex32[] bins, int kMin, int kMax)
        {
            int    kBest   = kMin;
            double magBest = 0.0;
            for (int k = kMin; k <= kMax; k++)
            {
                double m = (double)bins[k].Real * bins[k].Real
                         + (double)bins[k].Imaginary * bins[k].Imaginary;
                if (m > magBest) { magBest = m; kBest = k; }
            }
            return kBest;
        }

        // ── Interpolation parabolique : position fractionnaire du bin dominant ─
        // Retourne k0 + δ ∈ [k0−0.5, k0+0.5].
        // N'a pas besoin de sampleRate/fftSize — sert uniquement au groupage.
        private static double CohInterpolateK0Real(
            MathNet.Numerics.Complex32[] bins, int k0)
        {
            if (k0 <= 0 || k0 >= bins.Length - 1) return k0;

            double a = (double)bins[k0-1].Real * bins[k0-1].Real
                     + (double)bins[k0-1].Imaginary * bins[k0-1].Imaginary;
            double b = (double)bins[k0  ].Real * bins[k0  ].Real
                     + (double)bins[k0  ].Imaginary * bins[k0  ].Imaginary;
            double c = (double)bins[k0+1].Real * bins[k0+1].Real
                     + (double)bins[k0+1].Imaginary * bins[k0+1].Imaginary;

            double denom = a - 2.0 * b + c;
            if (Math.Abs(denom) < 1e-30) return k0;

            double delta = 0.5 * (a - c) / denom;   // ∈ [−0.5, +0.5]
            return k0 + delta;
        }


        public void ResetAveragingState()
        {
            _avgRingPos = 0;
            _avgFillCount = 0;

            _avgHasLeft = false;
            _avgHasRight = false;

            _avgSumLeftPower = null;
            _avgSumRightPower = null;

            _avgRingLeftPower = null;
            _avgRingRightPower = null;
            _expInit = false;
            _expAlpha = 0.0;
            _expLeftPower = null;
            _expRightPower = null;

        }

        // Reset spécifique FRF (Bode / H1)
        public void ResetFrfState()
        {
            _frfInit = false;
            _frfFillCount = 0;
            _frfGxx = null;
            _frfGxy = null;
        }


        /// <summary>
        /// Retourne le spectre dBFS issu du moyennage cohérent courant.
        /// Peut être appelé avant que le cycle ne soit complet (snapshot intermédiaire).
        /// Le spectre est calculé par : dBFS[k] = 20·log10( |mean_coh[k]| )
        /// ce qui est cohérent avec <see cref="ComplexBinsToDbFs"/> dans SpectrumControl.
        /// </summary>
        /// <param name="countUsed">Nombre de frames utilisées dans la moyenne.</param>
        public bool TryGetCohAvgDbFs(
            out double[]? leftDb,
            out double[]? rightDb,
            out int countUsed)
        {
            countUsed = _cohCount;
            leftDb  = null;
            rightDb = null;

            if (_cohCount == 0) return false;

            double invN = 1.0 / _cohCount;

            if (_cohSumL != null)
            {
                int n = _cohSumL.Length;
                leftDb = new double[n];
                for (int k = 0; k < n; k++)
                {
                    double mag = (_cohSumL[k] * invN).Magnitude;
                    double db  = (mag <= 1e-15) ? -200.0 : 20.0 * Math.Log10(mag);
                    leftDb[k]  = Math.Max(-200.0, db);
                }
            }

            if (_cohSumR != null)
            {
                int n = _cohSumR.Length;
                rightDb = new double[n];
                for (int k = 0; k < n; k++)
                {
                    double mag = (_cohSumR[k] * invN).Magnitude;
                    double db  = (mag <= 1e-15) ? -200.0 : 20.0 * Math.Log10(mag);
                    rightDb[k] = Math.Max(-200.0, db);
                }
            }

            return true;
        }

        public void ResetPeakHoldState()
        {
            _peakHoldLeftDb = null;
            _peakHoldRightDb = null;
        }

        public double[]? GetPeakLeft() => _peakHoldLeftDb;
        public double[]? GetPeakRight() => _peakHoldRightDb;

        /// <summary>
        /// Averaging supportant Left-only / Right-only / Both.
        /// Entrées nullable. Sorties nullable.
        /// </summary>
        public void ApplyAveraging(
            double[]? leftNewDb,
            double[]? rightNewDb,
            int avgTargetCount,
            out double[]? currentLeftDb,
            out double[]? currentRightDb,
            out int fillCountForUi)
        {
            ApplyAveragingAdvanced(leftNewDb, rightNewDb,
            AveragingMode.Count, avgTargetCount, 0.0,
            out currentLeftDb, out currentRightDb, out fillCountForUi);

        }
        //
        // ---- NOUVEAU : averaging "avancé" en puissance linéaire (Welch-ready) ----
        public void ApplyAveragingPower(
            double[]? leftNewPower,
            double[]? rightNewPower,
            AveragingMode mode,
            int avgTargetCount,
            double expAlpha,
            out double[]? currentLeftPower,
            out double[]? currentRightPower,
            out int fillCountForUi)
        {
            bool hasLeft = (leftNewPower != null);
            bool hasRight = (rightNewPower != null);

            if (!hasLeft && !hasRight)
            {
                ResetAveragingState();
                currentLeftPower = null;
                currentRightPower = null;
                fillCountForUi = 0;
                return;
            }

            if (mode == AveragingMode.None || avgTargetCount <= 1 && mode == AveragingMode.Count)
            {
                // pas de moyenne
                ResetAveragingState();
                currentLeftPower = leftNewPower;
                currentRightPower = rightNewPower;
                fillCountForUi = 0;
                return;
            }

            // bins = longueur du 1er canal dispo
            int bins = hasLeft ? leftNewPower!.Length : rightNewPower!.Length;

            if (mode == AveragingMode.Exponential)
            {
                // Clamp alpha
                if (expAlpha < 0.0) expAlpha = 0.0;
                if (expAlpha > 1.0) expAlpha = 1.0;

                // Reset si alpha change ou longueur change ou set de canaux change
                bool needReset =
                    (!_expInit) ||
                    (System.Math.Abs(_expAlpha - expAlpha) > 1e-12) ||
                    (_avgHasLeft != hasLeft) ||
                    (_avgHasRight != hasRight) ||
                    (hasLeft && (_expLeftPower == null || _expLeftPower.Length != bins)) ||
                    (hasRight && (_expRightPower == null || _expRightPower.Length != bins));

                if (needReset)
                {
                    ResetAveragingState();
                    _avgHasLeft = hasLeft;
                    _avgHasRight = hasRight;

                    _expAlpha = expAlpha;
                    _expInit = false;

                    if (hasLeft) _expLeftPower = new double[bins];
                    if (hasRight) _expRightPower = new double[bins];
                }

                // init: Y = X
                if (!_expInit)
                {
                    if (hasLeft && _expLeftPower != null && leftNewPower != null)
                        Array.Copy(leftNewPower, _expLeftPower, bins);

                    if (hasRight && _expRightPower != null && rightNewPower != null)
                        Array.Copy(rightNewPower, _expRightPower, bins);

                    _expInit = true;
                }
                else
                {
                    double a = _expAlpha;
                    double b = 1.0 - a;

                    if (hasLeft && _expLeftPower != null && leftNewPower != null)
                    {
                        for (int i = 0; i < bins; i++)
                            _expLeftPower[i] = a * _expLeftPower[i] + b * leftNewPower[i];
                    }

                    if (hasRight && _expRightPower != null && rightNewPower != null)
                    {
                        for (int i = 0; i < bins; i++)
                            _expRightPower[i] = a * _expRightPower[i] + b * rightNewPower[i];
                    }
                }

                currentLeftPower = hasLeft ? _expLeftPower : null;
                currentRightPower = hasRight ? _expRightPower : null;
                fillCountForUi = 0; // pas de "fill" en exponentiel
                return;
            }

            // ---- mode COUNT (fenêtre glissante N) ----
            // Si le set de canaux change, ou bins change, ou N(avg) change => reset
            bool needResetCount =
                (_avgRingLeftPower == null && hasLeft) ||
                (_avgRingRightPower == null && hasRight) ||
                (_avgHasLeft != hasLeft) ||
                (_avgHasRight != hasRight) ||
                (hasLeft && (_avgSumLeftPower == null || _avgSumLeftPower.Length != bins)) ||
                (hasRight && (_avgSumRightPower == null || _avgSumRightPower.Length != bins)) ||
                (hasLeft && (_avgRingLeftPower != null && _avgRingLeftPower.Length != avgTargetCount)) ||
                (hasRight && (_avgRingRightPower != null && _avgRingRightPower.Length != avgTargetCount));

            if (needResetCount)
            {
                ResetAveragingState();
                _avgHasLeft = hasLeft;
                _avgHasRight = hasRight;
            }

            // Allocate si besoin
            if (hasLeft && (_avgSumLeftPower == null || _avgRingLeftPower == null))
            {
                _avgSumLeftPower = new double[bins];
                _avgRingLeftPower = new double[avgTargetCount][];
                for (int i = 0; i < avgTargetCount; i++)
                    _avgRingLeftPower[i] = new double[bins];

                _avgRingPos = 0;
                _avgFillCount = 0;
            }

            if (hasRight && (_avgSumRightPower == null || _avgRingRightPower == null))
            {
                _avgSumRightPower = new double[bins];
                _avgRingRightPower = new double[avgTargetCount][];
                for (int i = 0; i < avgTargetCount; i++)
                    _avgRingRightPower[i] = new double[bins];

                _avgRingPos = 0;
                _avgFillCount = 0;
            }

            // 1) Retirer la frame la plus vieille si la fenêtre est pleine
            if (_avgFillCount == avgTargetCount)
            {
                if (hasLeft && _avgRingLeftPower != null && _avgSumLeftPower != null)
                {
                    var oldL = _avgRingLeftPower[_avgRingPos];
                    for (int i = 0; i < bins; i++)
                        _avgSumLeftPower[i] -= oldL[i];
                }

                if (hasRight && _avgRingRightPower != null && _avgSumRightPower != null)
                {
                    var oldR = _avgRingRightPower[_avgRingPos];
                    for (int i = 0; i < bins; i++)
                        _avgSumRightPower[i] -= oldR[i];
                }
            }

            // 2) Stocker la nouvelle frame POWER dans l’anneau + somme
            if (hasLeft && leftNewPower != null && _avgRingLeftPower != null && _avgSumLeftPower != null)
            {
                var slotL = _avgRingLeftPower[_avgRingPos];
                for (int i = 0; i < bins; i++)
                {
                    double p = leftNewPower[i];
                    slotL[i] = p;
                    _avgSumLeftPower[i] += p;
                }
            }

            if (hasRight && rightNewPower != null && _avgRingRightPower != null && _avgSumRightPower != null)
            {
                var slotR = _avgRingRightPower[_avgRingPos];
                for (int i = 0; i < bins; i++)
                {
                    double p = rightNewPower[i];
                    slotR[i] = p;
                    _avgSumRightPower[i] += p;
                }
            }

            // 3) Avancer dans l’anneau
            _avgRingPos++;
            if (_avgRingPos >= avgTargetCount) _avgRingPos = 0;

            // 4) Warm-up
            if (_avgFillCount < avgTargetCount) _avgFillCount++;

            // 5) Sortie = moyenne POWER
            double inv = 1.0 / _avgFillCount;

            currentLeftPower = null;
            if (hasLeft && _avgSumLeftPower != null)
            {
                var outL = new double[bins];
                for (int i = 0; i < bins; i++)
                    outL[i] = _avgSumLeftPower[i] * inv;
                currentLeftPower = outL;
            }

            currentRightPower = null;
            if (hasRight && _avgSumRightPower != null)
            {
                var outR = new double[bins];
                for (int i = 0; i < bins; i++)
                    outR[i] = _avgSumRightPower[i] * inv;
                currentRightPower = outR;
            }

            fillCountForUi = _avgFillCount;
        }

        // ---- NOUVEAU : wrapper compat (entrée dB amplitude -> sortie dB amplitude) ----
        public void ApplyAveragingAdvanced(
            double[]? leftNewDb,
            double[]? rightNewDb,
            AveragingMode mode,
            int avgTargetCount,
            double expAlpha,
            out double[]? currentLeftDb,
            out double[]? currentRightDb,
            out int fillCountForUi)
        {
            // Convert dB(amplitude) -> power
            double[]? leftPower = null;
            if (leftNewDb != null)
            {
                int n = leftNewDb.Length;
                leftPower = new double[n];
                for (int i = 0; i < n; i++)
                {
                    double db = leftNewDb[i];
                    leftPower[i] = (db <= -200.0) ? 0.0 : Math.Pow(10.0, db / 10.0);
                }
            }

            double[]? rightPower = null;
            if (rightNewDb != null)
            {
                int n = rightNewDb.Length;
                rightPower = new double[n];
                for (int i = 0; i < n; i++)
                {
                    double db = rightNewDb[i];
                    rightPower[i] = (db <= -200.0) ? 0.0 : Math.Pow(10.0, db / 10.0);
                }
            }

            ApplyAveragingPower(leftPower, rightPower, mode, avgTargetCount, expAlpha,
                out var outLeftPower, out var outRightPower, out fillCountForUi);

            // Convert power -> dB(amplitude) : db = 10log10(power)
            currentLeftDb = null;
            if (outLeftPower != null)
            {
                int n = outLeftPower.Length;
                var outDb = new double[n];
                for (int i = 0; i < n; i++)
                {
                    double p = outLeftPower[i];
                    double db = (p <= 0.0) ? -200.0 : 10.0 * Math.Log10(p);
                    if (db < -200.0) db = -200.0;
                    if (db > 0.0) db = 0.0;
                    outDb[i] = db;
                }
                currentLeftDb = outDb;
            }

            currentRightDb = null;
            if (outRightPower != null)
            {
                int n = outRightPower.Length;
                var outDb = new double[n];
                for (int i = 0; i < n; i++)
                {
                    double p = outRightPower[i];
                    double db = (p <= 0.0) ? -200.0 : 10.0 * Math.Log10(p);
                    if (db < -200.0) db = -200.0;
                    if (db > 0.0) db = 0.0;
                    outDb[i] = db;
                }
                currentRightDb = outDb;
            }
        }

        //
        public void UpdatePeakHold(double[]? currentLeftDb, double[]? currentRightDb, bool enabled)
        {
            if (!enabled)
                return;

            if (currentLeftDb != null)
            {
                int bins = currentLeftDb.Length;
                if (_peakHoldLeftDb == null || _peakHoldLeftDb.Length != bins)
                    _peakHoldLeftDb = Enumerable.Repeat(-200.0, bins).ToArray();

                for (int i = 0; i < bins; i++)
                {
                    double v = currentLeftDb[i];
                    if (v > _peakHoldLeftDb[i]) _peakHoldLeftDb[i] = v;
                }
            }

            if (currentRightDb != null)
            {
                int bins = currentRightDb.Length;
                if (_peakHoldRightDb == null || _peakHoldRightDb.Length != bins)
                    _peakHoldRightDb = Enumerable.Repeat(-200.0, bins).ToArray();

                for (int i = 0; i < bins; i++)
                {
                    double v = currentRightDb[i];
                    if (v > _peakHoldRightDb[i]) _peakHoldRightDb[i] = v;
                }
            }
        }
        /// <summary>
        /// Estimation FRF H1 (Welch) :
        /// H1(f) = Gxy(f) / Gxx(f)
        /// Right = entrée X, Left = sortie Y
        /// Sortie : gain en dB (20*log10(|H1|))
        /// </summary>
        public void ApplyFrfH1Welch(
            Complex32[] rightBins,
            Complex32[] leftBins,
            AveragingMode mode,
            int avgTargetCount,
            double expAlpha,
            out double[] gainDb,
            out double[] phaseDeg,
            out int fillCountForUi)

        {
            int bins = Math.Min(rightBins.Length, leftBins.Length);

            // Détection changement de configuration → reset
            bool needReset =
                (!_frfInit) ||
                (_frfMode != mode) ||
                (_frfAvgTarget != avgTargetCount) ||
                (Math.Abs(_frfExpAlpha - expAlpha) > 1e-12) ||
                (_frfGxx == null || _frfGxx.Length != bins) ||
                (_frfGxy == null || _frfGxy.Length != bins);

            if (needReset)
            {
                ResetFrfState();
                _frfMode = mode;
                _frfAvgTarget = avgTargetCount;
                _frfExpAlpha = expAlpha;

                _frfGxx = new double[bins];
                _frfGxy = new System.Numerics.Complex[bins];
                _frfInit = true;
            }

            // --- Calcul instantané ---
            for (int k = 0; k < bins; k++)
            {
                var X = rightBins[k];
                var Y = leftBins[k];

                // Gxx = X * conj(X) = |X|^2 (réel)
                double gxxInst = (double)X.Real * X.Real + (double)X.Imaginary * X.Imaginary;

                // Gxy = X * conj(Y) (complex)
                var yConj = new Complex32(Y.Real, -Y.Imaginary);
                var gxy32 = X * yConj;

                // on stocke en Complex(double) pour l'accumulation (plus stable numériquement)
                var gxyInst = new System.Numerics.Complex(gxy32.Real, gxy32.Imaginary);


                if (mode == AveragingMode.Exponential)
                {
                    double a = expAlpha;
                    double b = 1.0 - a;

                    _frfGxx[k] = a * _frfGxx[k] + b * gxxInst;
                    _frfGxy[k] = a * _frfGxy[k] + b * gxyInst;
                }
                else
                {
                    // Mode COUNT (accumulation simple)
                    _frfGxx[k] += gxxInst;
                    _frfGxy[k] += gxyInst;
                }
            }

            // Gestion du fill count (COUNT uniquement)
            if (mode == AveragingMode.Count)
            {
                if (_frfFillCount < avgTargetCount)
                    _frfFillCount++;
            }
            else
            {
                _frfFillCount = 0; // exponentiel : pas de warm-up
            }

            fillCountForUi = _frfFillCount;

            // --- Calcul du gain ---
            gainDb = new double[bins];
            const double EPS = 1e-30;
            phaseDeg = new double[bins];

            for (int k = 0; k < bins; k++)
            {
                double gxx = _frfGxx[k];
                if (gxx < EPS)
                {
                    gainDb[k] = -200.0;
                    continue;
                }

                System.Numerics.Complex H = _frfGxy[k] / gxx;

                // --- Gain ---
                double mag = H.Magnitude;
                double db = (mag <= EPS) ? -200.0 : 20.0 * Math.Log10(mag);
                if (db < -200.0) db = -200.0;
                if (db > 200.0) db = 200.0;
                gainDb[k] = db;

                // --- Phase (brute, non unwrap) ---
                double phaseRad = Math.Atan2(H.Imaginary, H.Real);
                double deg = phaseRad * (180.0 / Math.PI);

                // clamp sécurité (évite NaN visuels)
                if (deg < -180.0) deg = -180.0;
                if (deg > 180.0) deg = 180.0;

                phaseDeg[k] = deg;

            }
        }

    }

}
