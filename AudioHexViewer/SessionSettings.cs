using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms.DataVisualization.Charting;


namespace AudioHexViewer
{
    public sealed class SessionSettings
    {
        public int Version { get; set; } = 1;

        public UiSettings Ui { get; set; } = new UiSettings();
        public AnalysisSettings Analysis { get; set; } = new AnalysisSettings();
        public HistogramSettings Histogram { get; set; } = new HistogramSettings();
        public SpectrumSettings Spectrum { get; set; } = new SpectrumSettings();

        public OscilloscopeSettings Oscilloscope { get; set; } = new OscilloscopeSettings();

        // ── Calibration de niveau de sortie du générateur (dBFS → Vrms) ──────
        // VrmsFullScale = tension mesurée en sortie à 0 dBFS (pleine échelle)
        // Vrms_out = VrmsFullScale * 10^(dBFS/20)
        public bool   GenCalValidLeft           { get; set; } = false;
        public double GenCalVrmsFullScaleLeft   { get; set; } = 1.0;
        public bool   GenCalValidRight          { get; set; } = false;
        public double GenCalVrmsFullScaleRight  { get; set; } = 1.0;

        // Global UI state — shared across all tabs
        public bool ShowMeasPanel { get; set; } = true;
    }

    public sealed class UiSettings
    {
        public string? DeviceId             { get; set; }
        public bool    CaptureOutput        { get; set; }
        public string? MainDataFormat       { get; set; }
        // Périphérique de sortie du générateur
        public string? OutputDeviceId       { get; set; }
        public int     RequestedOutputSR    { get; set; } = 0;   // 0 = Auto (Shared)
        // ── Post-Process (lecture de fichier audio) ──────────────────────────
        public bool    PostProcessMode     { get; set; } = false;
        public string? PostProcessFilePath { get; set; }
        public bool    PostProcessLoop     { get; set; } = false;
    }

    public sealed class AnalysisSettings
    {
        public int BufferSize  { get; set; } = 128 * 1024;
        public double Overlap  { get; set; } = 0.0;
        public int SampleSize  { get; set; } = 131072;

        /// <summary>
        /// Sample rate demandé en mode Exclusif (Hz).
        /// 0 = Auto (Windows Shared Mode, valeur par défaut).
        /// </summary>
        public int RequestedSampleRate { get; set; } = 0;
    }

    public sealed class HistogramSettings
    {
        public bool AC { get; set; } = true;
        public string Channel { get; set; } = "Both";
        public int RangeLsb { get; set; } = 1024;
        public int BinLsb { get; set; } = 1;
        public int RecordSamples { get; set; } = 131072;
        public int SplitterDistance { get; set; } = 740;
    }

    public sealed class SpectrumSettings
    {
        public SpectrumColorSettings? DefaultColors { get; set; }

        public string Channel { get; set; } = "L+R";     // Left/Right/L+R
        public bool RemoveDC { get; set; } = true;       // DC checkbox
        public string Window { get; set; } = "Rectangular"; // Rectangular/Hann/Hamming/Blackman-Harris-4/Blackman-Harris-7
        public bool LogFreq { get; set; } = false;       // log freq
        public int Averaging { get; set; } = 0;          // 0 = None, sinon 2/4/8/...

        // Seuil (dB) utilisé par la navigation Previous/Next Peak (< >)
        // Interprétation : Peak si niveau >= NoiseFloorVisible + NoiseToPeakThresholdDb
        // Plage UI : 3..100 dB (12 dB par défaut)
        public double NoiseToPeakThresholdDb { get; set; } = 12.0;

        // Unité d'affichage de l'axe Y (FFT)
        // - "dBFS" : niveau relatif au full-scale numérique (actuel)
        // - "dBV"  : niveau absolu RMS, 0 dBV = 1 Vrms (disponible si calibration faite)
        public string LevelUnit { get; set; } = "dBFS";

        // Calibration niveau (conversion dBFS -> dBV) : dBV = dBFS + Offset
        public bool LevelCalValidLeft { get; set; } = false;
        public double LevelCalOffsetDbLeft { get; set; } = 0.0;

        public bool LevelCalValidRight { get; set; } = false;
        public double LevelCalOffsetDbRight { get; set; } = 0.0;

        public bool EnableThdMeasures { get; set; } = false;

        // Couleurs du graphe (FFT/overlays/grille/fond)
        public SpectrumColorSettings Colors { get; set; } = new SpectrumColorSettings();

        // --- Commit 11 : THD settings ---
        public ThdSettings Thd { get; set; } = new ThdSettings();



        public sealed class ThdSettings
        {
            // Bande de mesure
            public bool UseHPF { get; set; } = true;          // Flow
            public double FlowHz { get; set; } = 20.0;

            public bool UseLPF { get; set; } = true;          // Fhigh
            public double FhighHz { get; set; } = 20000.0;    // clamp à Fs/2 à l’application

            // Bande de mesure SFDR (indépendante de la bande THD)
            // SfdrUseHPF=false / SfdrFlowHz=0  → ΔF (premier bin utile)
            // SfdrUseLPF=false / SfdrFhighHz=0 → Fs/2 (Nyquist)
            public bool SfdrUseHPF { get; set; } = false;
            public double SfdrFlowHz { get; set; } = 0.0;
            public bool SfdrUseLPF { get; set; } = false;
            public double SfdrFhighHz { get; set; } = 0.0;

            // Harmoniques
            public int HarmonicsNumber { get; set; } = 5;     // 2..20

            // UI
            public bool ShowHarmonicsValues { get; set; } = true;
            public bool ShowHarmonicsPhase  { get; set; } = false;   // phase relative (°), off par défaut

            // THD en dB ou %
            public ThdUnit Unit { get; set; } = ThdUnit.Db;

            // ── Moyennage cohérent (accumulation complexe des bins FFT) ──────────
            // Le nombre de frames est pris dans la combo "Averaging" de l'analyseur.
            public bool UseCohAvg { get; set; } = false;

            public enum ThdUnit
            {
                Db = 0,
                Percent = 1
            }
        }
    }
}
