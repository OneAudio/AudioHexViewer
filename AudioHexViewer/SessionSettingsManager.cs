// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

using System;
using System.IO;
using System.Text.Json;

namespace AudioHexViewer
{
    public static class SessionSettingsManager
    {
        // ── Chemin de sauvegarde ──────────────────────────────────────────────
        // %APPDATA%\AudioHexViewer\SessionSettings.json
        // (ex: C:\Users\<user>\AppData\Roaming\AudioHexViewer\SessionSettings.json)
        //
        // Ancienne valeur (avant Phase 1) : répertoire de l'exécutable.
        // Le chemin AppData est nécessaire pour éviter les erreurs d'accès
        // quand l'application est installée dans Program Files.
        public static string SettingsFilePath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AudioHexViewer",
                "SessionSettings.json");

        // Chemin de l'ancien fichier (répertoire de l'exe) — utilisé pour la migration.
        private static string LegacySettingsFilePath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SessionSettings.json");

        // ── Chargement ────────────────────────────────────────────────────────
        public static SessionSettings? LoadOrNull()
        {
            try
            {
                // Migration : si un fichier existe dans l'ancien emplacement (répertoire exe)
                // et pas encore dans le nouvel emplacement, on le copie et on supprime l'original.
                MigrateLegacySettingsIfNeeded();

                if (!File.Exists(SettingsFilePath))
                    return null;

                string json = File.ReadAllText(SettingsFilePath);
                var s = JsonSerializer.Deserialize<SessionSettings>(json);

                // Normalise les valeurs suite à l'ajout de nouveaux champs
                // (compat avec des fichiers JSON écrits par des versions antérieures)
                s?.Spectrum?.Colors?.Normalize();
                s?.Spectrum?.DefaultColors?.Normalize();

                return s;
            }
            catch (Exception ex)
            {
                // Utile en debug, silencieux en release
                System.Diagnostics.Debug.WriteLine("Load SessionSettings failed: " + ex);
                return null;
            }
        }

        // ── Sauvegarde ────────────────────────────────────────────────────────
        public static bool Save(SessionSettings settings, out string? error)
        {
            error = null;
            try
            {
                settings?.Spectrum?.Colors?.Normalize();
                settings?.Spectrum?.DefaultColors?.Normalize();

                // Crée le répertoire si nécessaire (%APPDATA%\AudioHexViewer\)
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

        // ── Migration depuis l'ancien emplacement (one-shot) ──────────────────
        private static void MigrateLegacySettingsIfNeeded()
        {
            try
            {
                string legacy = LegacySettingsFilePath;
                string current = SettingsFilePath;

                if (!File.Exists(legacy) || File.Exists(current))
                    return; // rien à faire

                // Crée le répertoire cible si besoin
                var dir = Path.GetDirectoryName(current);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.Copy(legacy, current);

                // Suppression de l'ancien fichier en best-effort (pas bloquant)
                try { File.Delete(legacy); }
                catch { /* ignore */ }

                System.Diagnostics.Debug.WriteLine(
                    $"SessionSettings migrated: {legacy} → {current}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SessionSettings migration failed: " + ex.Message);
            }
        }
    }
}
