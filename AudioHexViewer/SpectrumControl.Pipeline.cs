// ═══════════════════════════════════════════════════════════════════════════
// SpectrumControl.Pipeline.cs  —  Ingestion des données FFT
//
// Responsabilités :
//   • Recevoir les frames FFT (FeedExternalSpectrum)
//   • Appliquer le moyennage Welch / exponentiel / cohérent
//   • Décimation pour l'affichage (BuildPlotDataFromFull / Decimate*)
//   • Stats temporelles Vrms/Vpeak/Vdc (FeedExternalTimeStats24)
// ═══════════════════════════════════════════════════════════════════════════
using AudioHexViewer.Core;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace AudioHexViewer
{
    partial class SpectrumControl
    {
        internal void EnableExternalSpectrumMode(bool enabled)
        {
            _useExternalSpectrum = enabled;
            // Plus de timer FFT interne : rien à stopper ici.
        }





        internal void FeedExternalSpectrum(AudioHexViewer.Core.SpectrumFrame frame)
        {
            if (frame == null) return;

            // Important : cette méthode doit être appelée sur le thread UI
            _useExternalSpectrum = true;

            // ── Détection du changement de SR ou de N (taille FFT) ─────────────────
            // Quand SR ou N change, ΔF = SR/N change → le minimum physique de l'axe
            // X en mode LOG change aussi. Si customFMin était réglé pour l'ancien ΔF,
            // il peut être devenu inférieur au nouveau ΔF → axe LOG illisible.
            // Solution : dès que SR ou N change, invalider customFMin s'il est en
            // dessous du nouveau ΔF. Cela force ApplyChartAxes à recalculer fMinAuto.
            bool _srOrNChanged = (lastFftSize != frame.FftSize || _sampleRate != frame.SampleRate);
            lastFftSize = frame.FftSize;
            _sampleRate = frame.SampleRate;

            if (_srOrNChanged && customFMin.HasValue && frame.FftSize > 0 && frame.SampleRate > 0)
            {
                double newDeltaF = (double)frame.SampleRate / frame.FftSize;
                if (customFMin.Value <= 0 || customFMin.Value < newDeltaF)
                    customFMin = null;   // → ApplyChartAxes utilisera fMinAuto = nouveau ΔF
            }

            // Conversion bins complexes → puissance (Phase 7: buffers pré-alloués, zéro alloc)
            double[]? powL = null;
            if (frame.LeftBins != null)
            {
                powL = EnsureDspBuf(ref _binsPowL, frame.LeftBins.Length);
                DspMath.ComplexBinsToPowerInto(frame.LeftBins, powL);
                // Conserver les bins complexes pour la phase des harmoniques (ComputeChannel)
                _complexBinsLeft = frame.LeftBins;
            }
            double[]? powR = null;
            if (frame.RightBins != null)
            {
                powR = EnsureDspBuf(ref _binsPowR, frame.RightBins.Length);
                DspMath.ComplexBinsToPowerInto(frame.RightBins, powR);
                _complexBinsRight = frame.RightBins;
            }


            if (powL == null && powR == null)
            {
                ClearSpectrum();
                return;
            }

            // ── Cohérent actif ? (calculé avant le Welch pour le bypass CPU) ─────
            bool doCohAvg = _thdMeasuresEnabled &&
                            _thdUi.UseCohAvg &&
                            !string.Equals(SelectedChannelMode, "Bode Plot",
                                           StringComparison.OrdinalIgnoreCase);

            // Averaging (POWER) : Count / Exponential / None
            // Quand la moyenne cohérente est active, le Welch n'est ni affiché ni
            // utilisé pour le THD → on passe None pour éviter le calcul inutile.
            SpectrumPostProcessor.AveragingMode mode;
            if (doCohAvg)
                mode = SpectrumPostProcessor.AveragingMode.None;
            else if (_avgIsExponential)
                mode = SpectrumPostProcessor.AveragingMode.Exponential;
            else if (avgTargetCount <= 1)
                mode = SpectrumPostProcessor.AveragingMode.None;
            else
                mode = SpectrumPostProcessor.AveragingMode.Count;

            _post.ApplyAveragingPower(powL, powR, mode, avgTargetCount, _avgExpAlpha,
                out double[]? outPowL,
                out double[]? outPowR,
                out int avgFill);

            // ── Moyennage cohérent pour THD ──────────────────────────────────────
            // Accumule les bins complexes bruts (avant conversion en puissance).
            // La moyenne complexe réduit le bruit incohérent de 10·log10(N) dB.
            // Le nombre de frames cible est pris dans la combo "Averaging" de l'UI.
            if (doCohAvg)
            {
                _post.AccumulateCohAvg(frame.LeftBins, frame.RightBins, avgTargetCount);

                // Running average : mise à jour à chaque frame (comportement REW).
                // L'accumulation continue indéfiniment ; le bouton Clear remet à zéro.
                // Le plancher de bruit descend de 10·log10(count) dB en continu.
                _post.TryGetCohAvgDbFs(out var cohRunL, out var cohRunR, out _);
                if (cohRunL != null) _cohAvgLeftDb  = cohRunL;
                if (cohRunR != null) _cohAvgRightDb = cohRunR;
            }
            else
            {
                // Nettoyage si le mode cohérent vient d'être désactivé
                if (_cohAvgLeftDb != null || _cohAvgRightDb != null)
                {
                    _cohAvgLeftDb  = null;
                    _cohAvgRightDb = null;
                    _post.ResetCohAvgState();
                }
            }

            // Conversion POWER -> dB pour l’affichage/peak-hold
            // - Modes classiques : dBFS = 10*log10(power), clamp [-200..0]
            // - Mode "Bode Plot" : gain relatif = 10*log10(P_L/P_R), clamp large [-200..200]
            if (string.Equals(SelectedChannelMode, "Bode Plot", StringComparison.OrdinalIgnoreCase))
            {
                // -----------------------------------------------------------------
                // BODE / FRF (H1)
                // Right = entrée X, Left = sortie Y
                // Calcul entièrement délégué au SpectrumPostProcessor
                // -----------------------------------------------------------------

                if (frame.RightBins != null && frame.LeftBins != null)
                {
                    _post.ApplyFrfH1Welch(
                        frame.RightBins,     // X (entrée)
                        frame.LeftBins,      // Y (sortie)
                        mode,                // Averaging mode (EXISTANT)
                        avgTargetCount,      // Target count (EXISTANT)
                        _avgExpAlpha,        // Alpha exponentiel (EXISTANT)
                        out var gainDb,      //  Gain dB (EXISTANT)
                        out var phaseDeg,    // <-- NOUVEAU (Phase)
                        out avgFill);        // Fill count UI (EXISTANT)


                    currentLeftSpectrum = gainDb;   // une seule courbe (gain)
                    //currentFrfPhaseDeg = phaseDeg;   // <-- NEW : stocke la phase (FULL) pour le plot / curseurs
                    currentFrfPhaseDeg = _frfPhaseUnwrap ? DspMath.UnwrapPhaseDegrees(phaseDeg) : phaseDeg;

                    currentRightSpectrum = null;
                }
                else
                {
                    currentLeftSpectrum = null;
                    currentRightSpectrum = null;
                }
            }
            else
            {
                // ---- Chemin EXISTANT, INCHANGÉ ----
                // Conversion puissance → dBFS (Phase 7: buffers pré-alloués)
                if (outPowL != null)
                {
                    var dbBuf = EnsureDspBuf(ref _binsDbL, outPowL.Length);
                    DspMath.PowerToDbFsInto(outPowL, dbBuf);
                    currentLeftSpectrum = dbBuf;
                }
                else currentLeftSpectrum = null;

                if (outPowR != null)
                {
                    var dbBuf = EnsureDspBuf(ref _binsDbR, outPowR.Length);
                    DspMath.PowerToDbFsInto(outPowR, dbBuf);
                    currentRightSpectrum = dbBuf;
                }
                else currentRightSpectrum = null;
            }

            // ── Affichage du spectre cohérent ────────────────────────────────
            // Quand un cycle complet est disponible, on remplace le spectre Welch
            // par le spectre cohérent dans le graphe : le plancher de bruit descend
            // de 10·log10(N) dB, rendant le résultat visuellement visible.
            if (doCohAvg)
            {
                int expBins = frame.FftSize / 2 + 1;
                if (_cohAvgLeftDb  != null && _cohAvgLeftDb.Length  == expBins)
                    currentLeftSpectrum  = _cohAvgLeftDb;
                if (_cohAvgRightDb != null && _cohAvgRightDb.Length == expBins)
                    currentRightSpectrum = _cohAvgRightDb;
            }

            // Progression UI :
            // – Mode cohérent actif  : k/N (k plafonné à N une fois la cible atteinte)
            // – Mode Count Welch     : k/N (comportement existant)
            // – Autres               : efface
            if (doCohAvg)
                UpdateAvgProgressUi(
                    Math.Min(_post.CohAvgCount, avgTargetCount),
                    avgTargetCount);
            else if (mode != SpectrumPostProcessor.AveragingMode.Count || avgTargetCount <= 1)
                UpdateAvgProgressUi(0, 0);
            else
                UpdateAvgProgressUi(avgFill, avgTargetCount);

            // Peak-hold : on le calculera sur la version PLOT (décimée) pour éviter un coût O(Nbins) inutile.
            bool peakEnabled = (chkPeakHold != null && chkPeakHold.Checked);

            // Axe fréquence FULL (servira aux métriques)
            freqAxis = frame.FreqAxisHz;

            // Construit la version "PLOT" décimée (tracé only), à partir des tableaux dB (FULL).
            BuildPlotDataFromFull();

            // Peak-hold : calcul uniquement sur les tableaux PLOT (décimés).
            // => même nombre de points que la courbe affichée, coût CPU maîtrisé.
            _post.UpdatePeakHold(_plotLeftSpectrum, _plotRightSpectrum, peakEnabled);

            // Peak PLOT = directement l'état du post-processor (mêmes longueurs que _plot*Spectrum)
            _plotPeakLeft = peakEnabled ? _post.GetPeakLeft() : null;
            _plotPeakRight = peakEnabled ? _post.GetPeakRight() : null;

            // Puis UI update (stats + chart)
            UpdateSpectrumChartAndStats(lastTimeLeft, lastTimeRight);


        }
        private void BuildPlotDataFromFull()
        {
            _plotFreqAxis    = null;
            _plotLeftSpectrum  = null;
            _plotRightSpectrum = null;
            _plotFrfPhaseDeg   = null;
            _plotPeakLeft  = null;
            _plotPeakRight = null;

            if (freqAxis == null) return;

            var (fMin, fMax) = GetVisibleFreqRangeHz();
            int px     = GetPlotPixelWidth();
            int budget = Math.Min(4096, Math.Max(512, px * 2));
            bool useLogX = IsXAxisLog();

            // ─────────────────────────────────────────────────────────────────────
            // Phase 7b FIX DÉFINITIF
            // On construit l'axe fréquentiel UNE SEULE FOIS, de façon entièrement
            // indépendante des données des deux voies. Les deux voies sont ensuite
            // décimées avec EXACTEMENT le même algorithme et les mêmes bornes de
            // bucket → résultat identique quelle que soit la combinaison de voies.
            //
            // Ancienne approche : axe construit depuis la voie primaire (Left) avec
            // des positions de bin-max data-dépendantes, puis voie secondaire (Right)
            // redécimée avec un algorithme différent → plancher de bruit asymétrique.
            // ─────────────────────────────────────────────────────────────────────

            // 1) Construire l'axe commun (bornes de buckets uniformes, sans données)
            double[]? sharedFreqAxis = BuildSharedFreqAxis(
                freqAxis, fMin, fMax, budget, useLogX,
                out double[]? bucketLo, out double[]? bucketHi);

            if (sharedFreqAxis == null || bucketLo == null || bucketHi == null) return;

            _plotFreqAxis = sharedFreqAxis;

            // 2) Décimer les deux voies avec les MÊMES bornes
            if (currentLeftSpectrum != null)
                _plotLeftSpectrum = DecimateWithBuckets(
                    freqAxis, currentLeftSpectrum, bucketLo, bucketHi);

            if (currentFrfPhaseDeg != null)
                _plotFrfPhaseDeg = DecimateWithBuckets(
                    freqAxis, currentFrfPhaseDeg, bucketLo, bucketHi);

            if (currentRightSpectrum != null)
                _plotRightSpectrum = DecimateWithBuckets(
                    freqAxis, currentRightSpectrum, bucketLo, bucketHi);

            _plotPeakLeft  = null;
            _plotPeakRight = null;
        }

        /// <summary>
        /// Construit un axe fréquentiel commun (<paramref name="freqOut"/>) et ses bornes
        /// de buckets (<paramref name="bucketLo"/>, <paramref name="bucketHi"/>),
        /// entièrement indépendants des données audio.
        /// </summary>
        private static double[]? BuildSharedFreqAxis(
            double[] freqFull,
            double fMin, double fMax,
            int maxPoints, bool useLogX,
            out double[]? bucketLo, out double[]? bucketHi)
        {
            bucketLo = null;
            bucketHi = null;

            int nIn = freqFull.Length;
            if (nIn == 0 || fMin >= fMax || maxPoints <= 0) return null;

            // Clamp fMin/fMax aux bornes réelles du spectre
            double freqFirst = freqFull[0];
            double freqLast  = freqFull[nIn - 1];
            fMin = Math.Max(fMin, freqFirst);
            fMax = Math.Min(fMax, freqLast);
            if (fMin >= fMax) return null;

            // Nombre de bins dans la plage visible
            int i0 = Array.BinarySearch(freqFull, fMin);
            if (i0 < 0) i0 = ~i0;
            int i1 = Array.BinarySearch(freqFull, fMax);
            if (i1 < 0) i1 = ~i1;
            else         i1++;           // inclusif → exclusif
            i0 = Math.Max(0, i0);
            i1 = Math.Min(nIn, i1);
            int span = i1 - i0;
            if (span <= 0) return null;

            // Pas de décimation nécessaire : 1 point = 1 bin
            if (span <= maxPoints)
            {
                int n = span;
                var fa = new double[n];
                var lo = new double[n];
                var hi = new double[n];
                for (int j = 0; j < n; j++)
                {
                    fa[j] = freqFull[i0 + j];
                    lo[j] = (j == 0)     ? double.MinValue : (freqFull[i0+j-1] + freqFull[i0+j]) * 0.5;
                    hi[j] = (j == n - 1) ? double.MaxValue : (freqFull[i0+j]   + freqFull[i0+j+1]) * 0.5;
                }
                bucketLo = lo;
                bucketHi = hi;
                return fa;
            }

            int nOut = maxPoints;
            var freqOut = new double[nOut];
            var bLo     = new double[nOut];
            var bHi     = new double[nOut];

            if (useLogX && fMin > 0 && fMax > fMin)
            {
                // LOG : buckets uniformes en log10(f)
                double logMin  = Math.Log10(fMin);
                double logMax  = Math.Log10(fMax);
                double logStep = (logMax - logMin) / nOut;

                for (int j = 0; j < nOut; j++)
                {
                    double fa2 = Math.Pow(10.0, logMin + j       * logStep);
                    double fb2 = Math.Pow(10.0, logMin + (j + 1) * logStep);
                    bLo[j]     = fa2;
                    bHi[j]     = fb2;
                    // Centre géométrique = milieu en espace log (affiché sur l'axe X)
                    freqOut[j] = Math.Pow(10.0, logMin + (j + 0.5) * logStep);
                }
            }
            else
            {
                // LIN : buckets uniformes en index de bin
                double step = (double)span / nOut;
                for (int j = 0; j < nOut; j++)
                {
                    int a = i0 + (int)Math.Floor(j       * step);
                    int b = i0 + (int)Math.Floor((j + 1) * step);
                    b = Math.Min(b, i1);
                    if (b <= a) b = a + 1;
                    int center = (a + Math.Min(b, i1) - 1) / 2;
                    center = Math.Max(0, Math.Min(nIn - 1, center));
                    bLo[j]     = freqFull[Math.Max(0, a)];
                    bHi[j]     = (b < i1) ? freqFull[b] : double.MaxValue;
                    freqOut[j] = freqFull[center];
                }
            }

            bucketLo = bLo;
            bucketHi = bHi;
            return freqOut;
        }

        /// <summary>
        /// Décime <paramref name="yFull"/> sur les buckets fournis par
        /// <see cref="BuildSharedFreqAxis"/> en appliquant un <b>max-hold</b>.
        /// Toutes les voies appellent cette même fonction avec les mêmes bornes
        /// → résultats strictement comparables.
        /// </summary>
        private static double[] DecimateWithBuckets(
            double[] freqFull,
            double[] yFull,
            double[] bucketLo,
            double[] bucketHi)
        {
            int nIn  = Math.Min(freqFull.Length, yFull.Length);
            int nOut = bucketLo.Length;
            var yOut = new double[nOut];

            for (int j = 0; j < nOut; j++)
            {
                double lo = bucketLo[j];
                double hi = bucketHi[j];

                int kLo = Array.BinarySearch(freqFull, lo);
                if (kLo < 0) kLo = ~kLo;

                int kHi = Array.BinarySearch(freqFull, hi);
                if (kHi < 0) kHi = ~kHi;     // premier bin >= hi → exclusif

                kLo = Math.Max(0,        Math.Min(nIn - 1, kLo));
                kHi = Math.Max(kLo + 1,  Math.Min(nIn,     kHi));  // exclusif

                // Max-hold dans le bucket [kLo, kHi)
                double yMax = yFull[kLo];
                for (int k = kLo + 1; k < kHi; k++)
                    if (yFull[k] > yMax) yMax = yFull[k];

                yOut[j] = yMax;
            }
            return yOut;
        }


        internal void FeedExternalTimeStats24(int[] left24, int[] right24)
        {
            const double fullScale = SpectrumAnalyzer.Max24Pos; // 0x7FFFFF

            // Phase 7 : algorithme de Welford — 1 seule boucle au lieu de 2.
            // Calcule peak, mean (DC) et variance (AC RMS) en une passe.
            // En mode AC (_removeDcStats=true) : peak = max(|sample - prevMean|)
            // utilisant la moyenne du frame précédent (latence 1 frame, coût nul).
            void ComputeOne(int[] x24, double prevMean, out double? rmsFsAc, out double? peakFs, out double? meanFs)
            {
                rmsFsAc = null;
                peakFs  = null;
                meanFs  = null;
                if (x24 == null || x24.Length == 0) return;

                int    n      = x24.Length;
                int    pk24   = 0;
                double mean   = 0.0;
                double M2     = 0.0;
                double invScale = 1.0 / fullScale;

                // En mode AC, le peak est calculé par rapport à prevMean (frame N-1).
                // En mode DC, prevMean = 0.0 → peak = max(|sample|) comme avant.
                for (int i = 0; i < n; i++)
                {
                    double v = x24[i] * invScale;   // en FS [-1..1]

                    // Peak AC : soustrait la moyenne du frame précédent
                    double av = v - prevMean;
                    if (av < 0) av = -av;
                    if (av * fullScale > pk24) pk24 = (int)(av * fullScale);

                    // Welford incremental mean + variance (toujours sur signal brut)
                    double delta  = v - mean;
                    mean         += delta / (i + 1);
                    M2           += delta * (v - mean);   // = delta * delta2
                }

                double pk = pk24 * invScale;
                double rms = n > 1 ? Math.Sqrt(M2 / n) : 0.0;   // population std dev = RMS AC

                rmsFsAc = (rms > 0.0) ? rms : null;
                peakFs  = (pk  > 0.0) ? pk  : null;
                meanFs  = mean;
            }

            // prevMean = 0 en mode DC, moyenne frame précédent en mode AC
            double prevL = _removeDcStats ? _prevMeanFsLeft  : 0.0;
            double prevR = _removeDcStats ? _prevMeanFsRight : 0.0;

            ComputeOne(left24,  prevL, out var rmsL, out var pkL, out var meanL);
            ComputeOne(right24, prevR, out var rmsR, out var pkR, out var meanR);

            // Mémoriser les moyennes pour le prochain frame (utilisées pour le peak AC)
            if (meanL.HasValue) _prevMeanFsLeft  = meanL.Value;
            if (meanR.HasValue) _prevMeanFsRight = meanR.Value;

            _lastRmsFsLeft = rmsL;
            _lastRmsFsRight = rmsR;
            _lastPeakFsLeft = pkL;
            _lastPeakFsRight = pkR;

            // dBFS RMS (déjà utilisé dans le dialogue CAL V)
            _lastRmsDbfsLeft = (rmsL.HasValue) ? Math.Max(-200.0, 20.0 * Math.Log10(rmsL.Value * Math.Sqrt(2.0))) : null;
            _lastRmsDbfsRight = (rmsR.HasValue) ? Math.Max(-200.0, 20.0 * Math.Log10(rmsR.Value * Math.Sqrt(2.0))) : null;

            // --- VOLTS (si calibré) ---
            // Vrms = RMS AC (DC retiré) * gain
            // Vpeak = peak FS (DC compris) * gain
            // Vdc = mean FS * gain
            if (_levelCalValidLeft && _levelGainVrmsPerFsLeft > 0.0)
            {
                _lastVrmsVoltsLeft = rmsL.HasValue ? rmsL.Value * _levelGainVrmsPerFsLeft : null;
                _lastVpkVoltsLeft = pkL.HasValue ? pkL.Value * _levelGainVrmsPerFsLeft : null;
                _lastVdcVoltsLeft = meanL.HasValue ? meanL.Value * _levelGainVrmsPerFsLeft : null;
            }
            else
            {
                _lastVrmsVoltsLeft = null;
                _lastVpkVoltsLeft = null;
                _lastVdcVoltsLeft = null;
            }

            if (_levelCalValidRight && _levelGainVrmsPerFsRight > 0.0)
            {
                _lastVrmsVoltsRight = rmsR.HasValue ? rmsR.Value * _levelGainVrmsPerFsRight : null;
                _lastVpkVoltsRight = pkR.HasValue ? pkR.Value * _levelGainVrmsPerFsRight : null;
                _lastVdcVoltsRight = meanR.HasValue ? meanR.Value * _levelGainVrmsPerFsRight : null;
            }
            else
            {
                _lastVrmsVoltsRight = null;
                _lastVpkVoltsRight = null;
                _lastVdcVoltsRight = null;
            }
        }

    }
}
