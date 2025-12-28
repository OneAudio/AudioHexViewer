using System;
using System.Linq;

namespace AudioHexViewer
{
    internal sealed class SpectrumPostProcessor
    {
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
            bool hasLeft = (leftNewDb != null);
            bool hasRight = (rightNewDb != null);

            if (!hasLeft && !hasRight)
            {
                ResetAveragingState();
                currentLeftDb = null;
                currentRightDb = null;
                fillCountForUi = 0;
                return;
            }

            // None => pas de moyenne
            if (avgTargetCount <= 1)
            {
                ResetAveragingState();
                currentLeftDb = leftNewDb;
                currentRightDb = rightNewDb;
                fillCountForUi = 0;
                return;
            }

            // bins = longueur du 1er canal dispo
            int bins = hasLeft ? leftNewDb!.Length : rightNewDb!.Length;

            // Si le set de canaux change, ou bins change, ou N(avg) change => reset (sinon incohérences)
            bool needReset =
                (_avgRingLeftPower == null && hasLeft) ||
                (_avgRingRightPower == null && hasRight) ||
                (_avgHasLeft != hasLeft) ||
                (_avgHasRight != hasRight) ||
                (hasLeft && (_avgSumLeftPower == null || _avgSumLeftPower.Length != bins)) ||
                (hasRight && (_avgSumRightPower == null || _avgSumRightPower.Length != bins)) ||
                (hasLeft && (_avgRingLeftPower != null && _avgRingLeftPower.Length != avgTargetCount)) ||
                (hasRight && (_avgRingRightPower != null && _avgRingRightPower.Length != avgTargetCount));

            if (needReset)
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

            // 2) Convert dB(amplitude) -> puissance linéaire (power = 10^(dB/10)), stocker dans l’anneau
            if (hasLeft && leftNewDb != null && _avgRingLeftPower != null && _avgSumLeftPower != null)
            {
                var slotL = _avgRingLeftPower[_avgRingPos];
                for (int i = 0; i < bins; i++)
                {
                    double db = leftNewDb[i];
                    double p = (db <= -200.0) ? 0.0 : Math.Pow(10.0, db / 10.0);
                    slotL[i] = p;
                    _avgSumLeftPower[i] += p;
                }
            }

            if (hasRight && rightNewDb != null && _avgRingRightPower != null && _avgSumRightPower != null)
            {
                var slotR = _avgRingRightPower[_avgRingPos];
                for (int i = 0; i < bins; i++)
                {
                    double db = rightNewDb[i];
                    double p = (db <= -200.0) ? 0.0 : Math.Pow(10.0, db / 10.0);
                    slotR[i] = p;
                    _avgSumRightPower[i] += p;
                }
            }

            // 3) Avancer dans l’anneau
            _avgRingPos++;
            if (_avgRingPos >= avgTargetCount) _avgRingPos = 0;

            // 4) Warm-up : 0..N puis saturé à N
            if (_avgFillCount < avgTargetCount) _avgFillCount++;

            // 5) Construire le spectre moyen
            double inv = 1.0 / _avgFillCount;

            double[]? avgLeftDb = null;
            if (hasLeft && _avgSumLeftPower != null)
            {
                avgLeftDb = new double[bins];
                for (int i = 0; i < bins; i++)
                {
                    double pAvg = _avgSumLeftPower[i] * inv;
                    double db = (pAvg <= 0.0) ? -200.0 : 10.0 * Math.Log10(pAvg);
                    if (db < -200.0) db = -200.0;
                    if (db > 0.0) db = 0.0;
                    avgLeftDb[i] = db;
                }
            }

            double[]? avgRightDb = null;
            if (hasRight && _avgSumRightPower != null)
            {
                avgRightDb = new double[bins];
                for (int i = 0; i < bins; i++)
                {
                    double pAvg = _avgSumRightPower[i] * inv;
                    double db = (pAvg <= 0.0) ? -200.0 : 10.0 * Math.Log10(pAvg);
                    if (db < -200.0) db = -200.0;
                    if (db > 0.0) db = 0.0;
                    avgRightDb[i] = db;
                }
            }

            currentLeftDb = avgLeftDb;
            currentRightDb = avgRightDb;
            fillCountForUi = _avgFillCount;
        }

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
