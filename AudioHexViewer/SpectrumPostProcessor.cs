using System;
using System.Linq;

namespace AudioHexViewer
{
    internal sealed class SpectrumPostProcessor
    {
        // --- Moving average (glissant sur N frames) en puissance ---
        private int _avgRingPos = 0;        // index courant dans l’anneau
        private int _avgFillCount = 0;      // 0..N (sature à N pour l'affichage)

        private double[]? _avgSumLeftPower = null;    // somme des puissances (linéaire)
        private double[]? _avgSumRightPower = null;

        private double[][]? _avgRingLeftPower = null;   // [N][bins]
        private double[][]? _avgRingRightPower = null;  // [N][bins]

        // --- Peak-hold (en dBFS) ---
        private double[]? _peakHoldLeftDb;
        private double[]? _peakHoldRightDb;

        public void ResetAveragingState()
        {
            _avgRingPos = 0;
            _avgFillCount = 0;

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
        /// Applique le même averaging que ton ApplyAveraging actuel.
        /// Sorties : currentLeft/currentRight + fillCount (pour afficher k/N).
        /// </summary>
        public void ApplyAveraging(
            double[] leftNewDb,
            double[]? rightNewDb,
            int avgTargetCount,
            out double[] currentLeftDb,
            out double[]? currentRightDb,
            out int fillCountForUi)
        {
            // None => pas de moyenne
            if (avgTargetCount <= 1)
            {
                ResetAveragingState();
                currentLeftDb = leftNewDb;
                currentRightDb = rightNewDb;
                fillCountForUi = 0;
                return;
            }

            int bins = leftNewDb.Length;

            // (Re)allocation si FFT bins change ou si N(avg) change
            if (_avgSumLeftPower == null || _avgSumLeftPower.Length != bins ||
                _avgRingLeftPower == null || _avgRingLeftPower.Length != avgTargetCount)
            {
                _avgSumLeftPower = new double[bins];
                _avgRingLeftPower = new double[avgTargetCount][];
                for (int i = 0; i < avgTargetCount; i++)
                    _avgRingLeftPower[i] = new double[bins];

                _avgRingPos = 0;
                _avgFillCount = 0;
            }

            bool hasRight = (rightNewDb != null);
            if (hasRight)
            {
                if (_avgSumRightPower == null || _avgSumRightPower.Length != bins ||
                    _avgRingRightPower == null || _avgRingRightPower.Length != avgTargetCount)
                {
                    _avgSumRightPower = new double[bins];
                    _avgRingRightPower = new double[avgTargetCount][];
                    for (int i = 0; i < avgTargetCount; i++)
                        _avgRingRightPower[i] = new double[bins];
                }
            }
            else
            {
                _avgSumRightPower = null;
                _avgRingRightPower = null;
            }

            // 1) Retirer la frame la plus vieille si la fenêtre est pleine
            if (_avgFillCount == avgTargetCount)
            {
                var oldL = _avgRingLeftPower![_avgRingPos];
                for (int i = 0; i < bins; i++)
                    _avgSumLeftPower![i] -= oldL[i];

                if (hasRight && _avgRingRightPower != null && _avgSumRightPower != null)
                {
                    var oldR = _avgRingRightPower[_avgRingPos];
                    for (int i = 0; i < bins; i++)
                        _avgSumRightPower[i] -= oldR[i];
                }
            }

            // 2) Convert dB(amplitude) -> puissance linéaire (power = 10^(dB/10)) et stocker dans l’anneau
            var slotL = _avgRingLeftPower![_avgRingPos];
            for (int i = 0; i < bins; i++)
            {
                double db = leftNewDb[i];
                double p = (db <= -200.0) ? 0.0 : Math.Pow(10.0, db / 10.0);
                slotL[i] = p;
                _avgSumLeftPower![i] += p;
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

            var avgLeftDb = new double[bins];
            for (int i = 0; i < bins; i++)
            {
                double pAvg = _avgSumLeftPower![i] * inv;
                double db = (pAvg <= 0.0) ? -200.0 : 10.0 * Math.Log10(pAvg);
                if (db < -200.0) db = -200.0;
                if (db > 0.0) db = 0.0;
                avgLeftDb[i] = db;
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

            if (currentLeftDb == null)
                return;

            int bins = currentLeftDb.Length;

            if (_peakHoldLeftDb == null || _peakHoldLeftDb.Length != bins)
                _peakHoldLeftDb = Enumerable.Repeat(-200.0, bins).ToArray();

            for (int i = 0; i < bins; i++)
            {
                double v = currentLeftDb[i];
                if (v > _peakHoldLeftDb[i]) _peakHoldLeftDb[i] = v;
            }

            if (currentRightDb != null)
            {
                int n = currentRightDb.Length;

                if (_peakHoldRightDb == null || _peakHoldRightDb.Length != n)
                    _peakHoldRightDb = Enumerable.Repeat(-200.0, n).ToArray();

                for (int i = 0; i < n; i++)
                {
                    double v = currentRightDb[i];
                    if (v > _peakHoldRightDb[i]) _peakHoldRightDb[i] = v;
                }
            }
        }
    }
}
