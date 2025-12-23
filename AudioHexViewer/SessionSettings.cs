using System;
using System.IO;
using System.Text.Json;


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
        public int BufferSize { get; set; } = 128 * 1024;
        public double Overlap { get; set; } = 0.0;
        public int SampleSize { get; set; } = 131072;
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
        public string Channel { get; set; } = "L+R";     // Left/Right/L+R
        public bool RemoveDC { get; set; } = true;       // DC checkbox
        public string Window { get; set; } = "Rectangular"; // Rectangular/Hann/Hamming/Blackman-Harris-4/Blackman-Harris-7
        public bool LogFreq { get; set; } = false;       // log freq
        public int Averaging { get; set; } = 0;          // 0 = None, sinon 2/4/8/...
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
                return JsonSerializer.Deserialize<SessionSettings>(json);
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
