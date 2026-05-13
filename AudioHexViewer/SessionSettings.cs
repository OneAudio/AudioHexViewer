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
    }

    public sealed class UiSettings
    {
        public string? DeviceId { get; set; }
        public bool CaptureOutput { get; set; }
        public string? MainDataFormat { get; set; }
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

            // Harmoniques
            public int HarmonicsNumber { get; set; } = 5;     // 2..20

            // UI
            public bool ShowHarmonicsValues { get; set; } = true;

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

public sealed class SpectrumColorSettings
{
    // Stockage ARGB (Color.ToArgb()) pour sérialisation JSON simple

    // --- FFT ---
    public int FftLeftArgb { get; set; } = Color.DodgerBlue.ToArgb();
    public int FftRightArgb { get; set; } = Color.OrangeRed.ToArgb();
    public int FftWidth { get; set; } = 2;                 // 1..3

    // --- PeakHold ---
    public int PeakHoldLeftArgb { get; set; } = Color.DodgerBlue.ToArgb();
    public int PeakHoldRightArgb { get; set; } = Color.OrangeRed.ToArgb();
    public int PeakHoldWidth { get; set; } = 1;             // 1..3

    // --- Overlays ---
    // Ancien réglage (compat): couleur unique pour tous les overlays
    public int OverlayArgb { get; set; } = Color.Gray.ToArgb();

    // Nouveau: une couleur par slot (Overlay1=Slot1, ... Overlay8=Slot8)
    public int[] OverlaySlotArgb { get; set; } = DefaultOverlayRainbowArgb();
    public int OverlayWidth { get; set; } = 1;              // 1..3
    
    // Style de ligne des overlays (ChartDashStyle)
    public int OverlayDashStyle { get; set; } = (int)ChartDashStyle.Dot;

    // --- Chart (fond / grilles / labels) ---
    // Grille Major (compat: ancien GridArgb)
    public int GridArgb { get; set; } = Color.LightGray.ToArgb();

    // Nouveau: Grille Minor
    public int GridMinorArgb { get; set; } = Color.Gainsboro.ToArgb();

    public int BackgroundArgb { get; set; } = Color.White.ToArgb();

    // Labels (Major/Minor). Axis line + ticks = LabelMajor
    public int LabelMajorArgb { get; set; } = Color.Black.ToArgb();
    public int LabelMinorArgb { get; set; } = Color.DimGray.ToArgb();
    public int InfoTextArgb { get; set; } = Color.DarkBlue.ToArgb();

    public static SpectrumColorSettings Defaults()
    {
        var s = new SpectrumColorSettings();
        s.Normalize();
        return s;
    }

    /// <summary>
    /// Normalise les valeurs (taille overlays, bornes épaisseur).
    /// Appeler après chargement JSON pour garantir une config cohérente.
    /// </summary>
    public void Normalize()
    {
        // Overlay array
        if (OverlaySlotArgb == null || OverlaySlotArgb.Length != 8)
            OverlaySlotArgb = DefaultOverlayRainbowArgb();

        // Clamp widths
        FftWidth = Clamp(FftWidth, 1, 3);
        PeakHoldWidth = Clamp(PeakHoldWidth, 1, 3);
        OverlayWidth = Clamp(OverlayWidth, 1, 3);

        // If someone is upgrading from older settings and explicitly set OverlayArgb,
        // and OverlaySlotArgb is still default, they can later customize per-slot.
        // (On ne force pas OverlaySlotArgb à OverlayArgb pour ne pas écraser le rainbow par défaut.)
    }

    private static int Clamp(int v, int min, int max)
        => v < min ? min : (v > max ? max : v);

    private static int[] DefaultOverlayRainbowArgb()
    {
        // Palette arc-en-ciel (lisible sur fond clair/sombre).
        // Overlay1..Overlay8
        Color[] colors = new[]
        {
            Color.Red,
            Color.Orange,
            Color.Goldenrod,
            Color.LimeGreen,
            Color.Cyan,
            Color.DodgerBlue,
            Color.BlueViolet,
            Color.Magenta
        };

        var arr = new int[8];
        for (int i = 0; i < 8; i++)
            arr[i] = colors[i].ToArgb();
        return arr;
    }
}


public static class SessionSettingsManager
    {
        public static string SettingsFilePath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SessionSettings.json");

        public static SessionSettings? LoadOrNull()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                    return null;

                string json = File.ReadAllText(SettingsFilePath);
                var s = JsonSerializer.Deserialize<SessionSettings>(json);
                // Normalize newly added settings (compat with older SessionSettings.json)
                s?.Spectrum?.Colors?.Normalize();
                s?.Spectrum?.DefaultColors?.Normalize();

                return s;
            }
            catch (Exception ex)
            {
                // utile en debug, silencieux en release
                System.Diagnostics.Debug.WriteLine("Load SessionSettings failed: " + ex);
                return null;
            }
        }



        public static bool Save(SessionSettings settings, out string? error)
        {
            error = null;
            try
            {
                settings?.Spectrum?.Colors?.Normalize();
                settings?.Spectrum?.DefaultColors?.Normalize();

                var dir = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(SettingsFilePath, json);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}