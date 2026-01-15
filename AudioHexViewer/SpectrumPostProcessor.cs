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
    }
}
