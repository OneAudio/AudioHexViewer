using System;
using System.Collections.Generic;

namespace AudioHexViewer
{
    internal sealed class SpectrumMetricsOptions
    {
        // Bande d'analyse globale
        public double MinFreqHz { get; set; } = 20.0;
        public double MaxFreqHz { get; set; } = 24000.0; // 0 => Nyquist

        // Largeur demi-lobe principale pour SFDR
        public int SfdrFundHalfWidthBins { get; set; } = 16;


        // Détection / intégration
        public bool ExcludeDc { get; set; } = true;

        // Largeur demi-lobe principale pour intégration fondamentale et harmoniques
        // Largeur utilisée pour intégrer/exclure la fondamentale (mainlobe + skirts)
        // Doit dépendre de la fenêtre (BH7 > Hann > Rect)
        public int FundamentalHalfWidthBins { get; set; } = 8;

        // Largeur utilisée pour mesurer un spur pour SFDR (sinon on "mesure le bruit")
        public int ToneHalfWidthBins { get; set; } = 8;


        // Largeur utilisée pour mesurer une harmonique (sinon on "mesure le bruit")
        public int HarmonicHalfWidthBins { get; set; } = 3;

        public int MaxHarmonics { get; set; } = 7;      // 2..N

        // Optionnel: ignorer les harmoniques sous une fréquence minimale
        public double MinHarmonicFreqHz { get; set; } = 20.0;
    }

    internal readonly struct HarmonicInfo
    {
        public int Order { get; init; }           // 2..N
        public double FrequencyHz { get; init; }  // Hz
        public double LevelDbc { get; init; }     // dBc (relatif à la fondamentale)
        public double RatioPercent { get; init; } // % (relatif à la fondamentale)
    }

    internal sealed class ChannelMetrics
    {
        public bool Valid { get; init; }

        public double FundamentalHz { get; init; }
        public double FundamentalDbFs { get; init; }

        public double FundamentalPower { get; init; }      // somme P fondamentale (linéaire)
        public double HarmonicsPower { get; init; }        // somme P harmoniques (linéaire)
        public double NoisePower { get; init; }            // P bruit (excl. fondamentale + harmoniques)
        public double DistortionPlusNoisePower { get; init; } // P total - P fondamentale

        // Ratios (linéaires) de type amplitude (sqrt(power ratio))
        public double Thd { get; init; }      // sqrt(P_harm / P_fund)
        public double ThdN { get; init; }     // sqrt((P_total-P_fund)/P_fund)

        // En dB
        public double ThdDb { get; init; }    // 20*log10(THD) == 10*log10(P_harm/P_fund)
        public double ThdNDb { get; init; }   // 20*log10(THD+N)
        public double SinadDb { get; init; }  // 10*log10(P_fund/(P_total-P_fund))
        public double SnrDb { get; init; }    // 10*log10(P_fund/P_noise)
        public double SfdrDb { get; init; }   // dBFSfund - dBFSspur

        // Infos SFDR
        public double LargestSpurHz { get; init; }
        public double LargestSpurDbFs { get; init; }

        // Debug
        public int FundamentalBin { get; init; }

        // Harmoniques détaillées (optionnel)
        public IReadOnlyList<HarmonicInfo> Harmonics { get; init; } = Array.Empty<HarmonicInfo>();

    }

    internal static class SpectrumMetrics
    {
        public static ChannelMetrics ComputeChannel(double[] spectrumDbFs, int sampleRate, int fftSize, SpectrumMetricsOptions opt)
        {
            if (spectrumDbFs == null) throw new ArgumentNullException(nameof(spectrumDbFs));
            if (sampleRate <= 0 || fftSize <= 0) return new ChannelMetrics { Valid = false };

            int bins = spectrumDbFs.Length;
            double nyquist = sampleRate / 2.0;

            double fMin = Math.Max(opt.MinFreqHz, 0.0);
            double fMax = (opt.MaxFreqHz <= 0.0) ? nyquist : Math.Min(opt.MaxFreqHz, nyquist);

            int kMin = FreqToBinCeil(fMin, sampleRate, fftSize);
            int kMax = FreqToBinFloor(fMax, sampleRate, fftSize);

            if (opt.ExcludeDc && kMin < 1) kMin = 1;
            if (kMax >= bins) kMax = bins - 1;
            if (kMin >= kMax) return new ChannelMetrics { Valid = false };

            // 1) Convert dBFS(amplitude) -> puissance linéaire (amp^2) par bin
            //    db = 20log10(amp) == 10log10(amp^2) => P = 10^(db/10)
            var p = new double[bins];
            for (int i = 0; i < bins; i++)
            {
                double db = spectrumDbFs[i];
                if (db <= -200.0) p[i] = 0.0;
                else p[i] = Math.Pow(10.0, db / 10.0);
            }

            // 2) Trouver la fondamentale = bin de puissance max dans [kMin..kMax]
            int kFund = kMin;
            double pFundPeak = p[kMin];
            for (int k = kMin + 1; k <= kMax; k++)
            {
                if (p[k] > pFundPeak)
                {
                    pFundPeak = p[k];
                    kFund = k;
                }
            }

            // Fréquence affinée (parabole sur puissance), optionnelle
            double fundHz = BinToFreq(kFund, sampleRate, fftSize);
            if (kFund > 0 && kFund < bins - 1)
            {
                double a = p[kFund - 1];
                double b = p[kFund];
                double c = p[kFund + 1];

                double denom = (a - 2.0 * b + c);
                if (Math.Abs(denom) > 1e-30)
                {
                    double delta = 0.5 * (a - c) / denom; // [-0.5 ; +0.5] typiquement
                    double kInterp = kFund + delta;
                    fundHz = (kInterp * sampleRate) / (double)fftSize;
                }
            }

            // 3) Marquer les bins exclus (fondamentale + harmoniques) pour calcul bruit
            var excluded = new bool[bins];

            // Exclure fondamentale (±W)
            double pFund = IntegrateTonePowerAndMark(excluded, p, kFund, opt.FundamentalHalfWidthBins, kMin, kMax);

            // 4) Harmoniques
            var harmonics = new List<HarmonicInfo>();

            double pHarm = 0.0;
            int maxH = Math.Max(0, opt.MaxHarmonics);

            for (int h = 2; h <= maxH; h++)
            {
                double fh = h * fundHz;
                if (fh < opt.MinHarmonicFreqHz) continue;
                if (fh >= nyquist) break;

                int kH = (int)Math.Round(fh * fftSize / (double)sampleRate);
                if (kH < kMin || kH > kMax) continue;

                double pThis = IntegrateTonePowerAndMark(excluded, p, kH, opt.HarmonicHalfWidthBins, kMin, kMax);
                pHarm += pThis;

                if (pFund > 0.0 && pThis > 0.0)
                {
                    double ratioAmp = Math.Sqrt(pThis / pFund);            // amplitude ratio
                    double levelDbc = 20.0 * SafeLog10(ratioAmp);          // dBc (négatif)
                    harmonics.Add(new HarmonicInfo
                    {
                        Order = h,
                        FrequencyHz = fh,
                        LevelDbc = ClampDb(levelDbc),
                        RatioPercent = ratioAmp * 100.0
                    });
                }
                else
                {
                    harmonics.Add(new HarmonicInfo
                    {
                        Order = h,
                        FrequencyHz = fh,
                        LevelDbc = -200.0,
                        RatioPercent = 0.0
                    });
                }
            }


            // 5) Puissance totale dans bande (pour THD+N / SINAD)
            double pTotal = 0.0;
            for (int k = kMin; k <= kMax; k++)
                pTotal += p[k];

            double pDistPlusNoise = Math.Max(0.0, pTotal - pFund);

            // 6) Bruit = total - (fund + harm)
            double pNoise = Math.Max(0.0, pTotal - pFund - pHarm);

            // 7) SFDR "spur-only" : on exclut fondamentale + harmoniques (THD) puis on cherche le plus gros spur restant.
            // Important : pour être stable, on centre les harmoniques en BINS (kFund*h) plutôt qu'en Hz interpolé.
            double spurDb = -200.0;
            double spurHz = 0.0;

            // Masque d'exclusion SFDR
            var excludedSfdr = new bool[bins];

            // Exclure fondamentale (largeur dédiée SFDR)
            MarkBins(excludedSfdr, kFund, opt.SfdrFundHalfWidthBins, kMin, kMax);

            // Exclure harmoniques H2..Hmax (même largeur que l'intégration tonalité, ou une largeur dédiée si tu ajoutes une option)
            for (int h = 2; h <= maxH; h++)
            {
                int kH = kFund * h;
                if (kH > kMax) break;

                // Respecter MinHarmonicFreqHz
                double fh = BinToFreq(kH, sampleRate, fftSize);
                if (fh < opt.MinHarmonicFreqHz) continue;
                if (fh >= nyquist) break;

                MarkBins(excludedSfdr, kH, opt.ToneHalfWidthBins, kMin, kMax);
            }

            int kSpur = -1;
            double pSpurPeak = 0.0;

            // 1er passage : trouver le bin max hors exclusions
            for (int k = kMin; k <= kMax; k++)
            {
                if (excludedSfdr[k]) continue;
                if (p[k] > pSpurPeak) { pSpurPeak = p[k]; kSpur = k; }
            }

            // 2e passage : intégrer autour du spur trouvé (plus robuste qu'un seul bin, surtout avec fenêtrage)
            double pSpur = 0.0;
            if (kSpur >= 0 && pSpurPeak > 0.0)
            {
                pSpur = IntegrateTonePowerAndMark(new bool[bins], p, kSpur, opt.ToneHalfWidthBins, kMin, kMax);
                spurDb = 10.0 * Math.Log10(pSpur);
                spurHz = BinToFreq(kSpur, sampleRate, fftSize);
            }



            // 8) Ratios et dB
            bool valid = (pFund > 0.0);

            double thd = valid ? Math.Sqrt(SafeRatio(pHarm, pFund)) : 0.0;
            double thdn = valid ? Math.Sqrt(SafeRatio(pDistPlusNoise, pFund)) : 0.0;

            double thdDb = valid ? 20.0 * SafeLog10(thd) : -200.0;
            double thdnDb = valid ? 20.0 * SafeLog10(thdn) : -200.0;

            double sinadDb = valid ? 10.0 * SafeLog10(SafeRatio(pFund, pDistPlusNoise)) : -200.0;
            double snrDb = valid ? 10.0 * SafeLog10(SafeRatio(pFund, pNoise)) : -200.0;

            double fundDb = (pFund > 0.0) ? 10.0 * Math.Log10(pFund) : -200.0; // cohérent avec dBFS amplitude
            double sfdrDb = (pSpur > 0.0) ? (fundDb - spurDb) : 0.0;

            // clamp dB bas
            fundDb = ClampDb(fundDb);
            spurDb = ClampDb(spurDb);

            return new ChannelMetrics
            {
                Valid = valid,

                FundamentalBin = kFund,
                FundamentalHz = fundHz,
                FundamentalDbFs = fundDb,

                FundamentalPower = pFund,
                HarmonicsPower = pHarm,
                NoisePower = pNoise,
                DistortionPlusNoisePower = pDistPlusNoise,

                Thd = thd,
                ThdN = thdn,

                ThdDb = ClampDb(thdDb),
                ThdNDb = ClampDb(thdnDb),
                SinadDb = ClampDb(sinadDb),
                SnrDb = ClampDb(snrDb),
                SfdrDb = sfdrDb,

                LargestSpurHz = spurHz,
                LargestSpurDbFs = spurDb,
                Harmonics = harmonics

            };
        }

        // ---- Helpers ----

        private static double IntegrateTonePowerAndMark(bool[] excluded, double[] p, int kCenter, int halfWidth, int kMin, int kMax)
        {
            int a = Math.Max(kMin, kCenter - halfWidth);
            int b = Math.Min(kMax, kCenter + halfWidth);

            double sum = 0.0;
            for (int k = a; k <= b; k++)
            {
                excluded[k] = true;
                sum += p[k];
            }
            return sum;
        }

        private static void MarkBins(bool[] excluded, int kCenter, int halfWidth, int kMin, int kMax)
        {
            int a = Math.Max(kMin, kCenter - halfWidth);
            int b = Math.Min(kMax, kCenter + halfWidth);
            for (int k = a; k <= b; k++)
                excluded[k] = true;
        }

        private static int FreqToBinCeil(double freqHz, int sampleRate, int fftSize)
        {
            double k = freqHz * fftSize / (double)sampleRate;
            return (int)Math.Ceiling(k);
        }

        private static int FreqToBinFloor(double freqHz, int sampleRate, int fftSize)
        {
            double k = freqHz * fftSize / (double)sampleRate;
            return (int)Math.Floor(k);
        }

        private static double BinToFreq(int k, int sampleRate, int fftSize)
        {
            return (double)k * sampleRate / (double)fftSize;
        }

        private static double SafeRatio(double num, double den)
        {
            if (den <= 0.0) return 0.0;
            if (num <= 0.0) return 0.0;
            return num / den;
        }

        private static double SafeLog10(double x)
        {
            if (x <= 0.0) return -10.0; // évite -Inf (valeur arbitraire, sera clampée)
            return Math.Log10(x);
        }

        private static double ClampDb(double db)
        {
            if (double.IsNaN(db) || double.IsInfinity(db)) return -200.0;
            if (db < -200.0) return -200.0;
            // pas de clamp haut ici (THD peut être >0 dB si très mauvais), sauf si tu veux
            return db;
        }
    }
}
