// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

namespace AudioHexViewer;

/// <summary>
/// One-shot migration that runs on the first launch after updating to the new
/// file-location layout.  Each migration step is independent: a partial failure
/// in one step does not prevent the others from running.
///
/// Migration map:
///   AudioHexViewer.cal          : &lt;exe&gt;\  →  %LOCALAPPDATA%\AudioHexViewer\
///   HistogramSettings.json      : &lt;exe&gt;\  →  %APPDATA%\AudioHexViewer\
///   *.overlay.json (Spectrum)   : Overlays\  →  Overlays\Spectrum\  (renamed *.spec.json)
///   _overlay_visibility.txt     : Overlays\  →  Overlays\Spectrum\
///   *.overlay.json (Oscilloscope): Overlays\Oscilloscope\  (renamed *.scop.json, same dir)
/// </summary>
internal static class FileLocationMigration
{
    public static void RunOnce()
    {
        MigrateCalibration();
        MigrateHistogramSettings();
        MigrateSpectrumOverlays();
        MigrateOscilloscopeOverlays();
    }

    // ── Calibration file ──────────────────────────────────────────────────────
    private static void MigrateCalibration()
    {
        try
        {
            string oldPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AudioHexViewer.cal");
            if (!File.Exists(oldPath)) return;

            string newDir  = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AudioHexViewer");
            string newPath = Path.Combine(newDir, "AudioHexViewer.cal");
            if (File.Exists(newPath)) return;          // already migrated

            Directory.CreateDirectory(newDir);
            File.Move(oldPath, newPath);
            System.Diagnostics.Debug.WriteLine($"[Migration] Cal: {oldPath} → {newPath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Migration] Cal failed: {ex.Message}");
        }
    }

    // ── HistogramSettings.json ────────────────────────────────────────────────
    private static void MigrateHistogramSettings()
    {
        try
        {
            string oldPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HistogramSettings.json");
            if (!File.Exists(oldPath)) return;

            string newDir  = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AudioHexViewer");
            string newPath = Path.Combine(newDir, "HistogramSettings.json");
            if (File.Exists(newPath)) return;

            Directory.CreateDirectory(newDir);
            File.Move(oldPath, newPath);
            System.Diagnostics.Debug.WriteLine($"[Migration] HistogramSettings: {oldPath} → {newPath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Migration] HistogramSettings failed: {ex.Message}");
        }
    }

    // ── Spectrum overlays ─────────────────────────────────────────────────────
    // Step 1 (previous): %APPDATA%\AudioHexViewer\Overlays\*.overlay.json
    //         → %APPDATA%\AudioHexViewer\Overlays\Spectrum\*.spec.json
    // Step 2 (this):    %APPDATA%\AudioHexViewer\Overlays\Spectrum\*.spec.json
    //         → Documents\AudioHexViewer\Overlays\Spectrum\*.spec.json
    private static void MigrateSpectrumOverlays()
    {
        try
        {
            string appData  = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string docs     = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // Legacy 1: %APPDATA%\...\Overlays\*.overlay.json  (pre-rename)
            string legacy1  = Path.Combine(appData, "AudioHexViewer", "Overlays");
            // Legacy 2: %APPDATA%\...\Overlays\Spectrum\*.spec.json  (post-rename, pre-move)
            string legacy2  = Path.Combine(appData, "AudioHexViewer", "Overlays", "Spectrum");
            // Final:    Documents\...\Overlays\Spectrum\*.spec.json
            string newDir   = Path.Combine(docs, "AudioHexViewer", "Overlays", "Spectrum");

            Directory.CreateDirectory(newDir);

            // Step 1: rename *.overlay.json → *.spec.json (in legacy1)
            if (Directory.Exists(legacy1))
            {
                foreach (var src in Directory.EnumerateFiles(legacy1, "*.overlay.json", SearchOption.TopDirectoryOnly))
                {
                    string baseName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(src));
                    string dest = Path.Combine(newDir, baseName + ".spec.json");
                    if (!File.Exists(dest)) { File.Move(src, dest); }
                }
                MigrateVisibilityFile(
                    Path.Combine(legacy1, "_overlay_visibility.txt"),
                    Path.Combine(newDir,  "_overlay_visibility.txt"),
                    ".overlay.json", ".spec.json");
            }

            // Step 2: move *.spec.json from legacy2 → newDir
            if (Directory.Exists(legacy2) && legacy2 != newDir)
            {
                foreach (var src in Directory.EnumerateFiles(legacy2, "*.spec.json", SearchOption.TopDirectoryOnly))
                {
                    string dest = Path.Combine(newDir, Path.GetFileName(src));
                    if (!File.Exists(dest)) { File.Move(src, dest); }
                }
                MigrateVisibilityFile(
                    Path.Combine(legacy2, "_overlay_visibility.txt"),
                    Path.Combine(newDir,  "_overlay_visibility.txt"),
                    null, null);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Migration] Spectrum overlays failed: {ex.Message}");
        }
    }

    // ── Oscilloscope overlays ─────────────────────────────────────────────────
    // Step 1: %APPDATA%\...\Overlays\Oscilloscope\*.overlay.json → *.scop.json (same dir)
    // Step 2: %APPDATA%\...\Overlays\Oscilloscope\*.scop.json
    //       → Documents\...\Overlays\Oscilloscope\*.scop.json
    private static void MigrateOscilloscopeOverlays()
    {
        try
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string docs    = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string oldDir  = Path.Combine(appData, "AudioHexViewer", "Overlays", "Oscilloscope");
            string newDir  = Path.Combine(docs,    "AudioHexViewer", "Overlays", "Oscilloscope");

            if (!Directory.Exists(oldDir)) return;

            Directory.CreateDirectory(newDir);

            // Step 1: rename *.overlay.json → *.scop.json then move
            foreach (var src in Directory.EnumerateFiles(oldDir, "*.overlay.json", SearchOption.TopDirectoryOnly))
            {
                string baseName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(src));
                string dest = Path.Combine(newDir, baseName + ".scop.json");
                if (!File.Exists(dest)) { File.Move(src, dest); }
            }

            // Step 2: move *.scop.json
            foreach (var src in Directory.EnumerateFiles(oldDir, "*.scop.json", SearchOption.TopDirectoryOnly))
            {
                string dest = Path.Combine(newDir, Path.GetFileName(src));
                if (!File.Exists(dest)) { File.Move(src, dest); }
            }

            MigrateVisibilityFile(
                Path.Combine(oldDir, "_overlay_visibility.txt"),
                Path.Combine(newDir, "_overlay_visibility.txt"),
                ".overlay.json", ".scop.json");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Migration] Oscilloscope overlays failed: {ex.Message}");
        }
    }

    // ── Shared helper ─────────────────────────────────────────────────────────
    private static void MigrateVisibilityFile(string src, string dest, string? oldExt, string? newExt)
    {
        if (!File.Exists(src)) return;
        try
        {
            var lines = File.ReadAllLines(src);
            if (oldExt != null && newExt != null)
                lines = lines.Select(l => l.Replace(oldExt, newExt)).ToArray();
            if (!File.Exists(dest))
                File.WriteAllLines(dest, lines);
            File.Delete(src);
        }
        catch { /* non-critical */ }
    }
}
