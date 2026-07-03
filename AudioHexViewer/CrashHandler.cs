// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AudioHexViewer
{
    /// <summary>
    /// Global last-chance exception handler. Catches unhandled exceptions from the UI thread,
    /// background threads and faulted tasks, writes a crash log to
    /// %LOCALAPPDATA%\AudioHexViewer\logs\crash_*.log, keeps only the most recent files,
    /// and shows the user a short message pointing to that log.
    /// <para>Call <see cref="Install"/> once, at the very start of Main (before any form).</para>
    /// </summary>
    internal static class CrashHandler
    {
        private const int MaxLogFiles = 20;

        private static string LogDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AudioHexViewer", "logs");

        /// <summary>Wire up the global handlers. Safe to call once at startup.</summary>
        public static void Install()
        {
            // UI-thread exceptions are routed to Application.ThreadException instead of killing the app.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.ThreadException += (_, e) =>
                Handle(e.Exception, "UI thread (Application.ThreadException)", fatal: false);

            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                Handle(e.ExceptionObject as Exception, "AppDomain.UnhandledException", fatal: e.IsTerminating);

            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                Handle(e.Exception, "TaskScheduler.UnobservedTaskException", fatal: false);
                e.SetObserved(); // prevent escalation to a process-killing exception
            };
        }

        private static void Handle(Exception? ex, string source, bool fatal)
        {
            string? path = null;
            try { path = WriteLog(ex, source); } catch { /* logging must never throw */ }

            try
            {
                string where = path != null ? $"\n\nA crash log was saved to:\n{path}" : string.Empty;
                string kind  = fatal ? "A fatal error occurred and the application must close."
                                     : "An unexpected error occurred.";
                MessageBox.Show(
                    $"{kind}{where}",
                    "AudioHexViewer",
                    MessageBoxButtons.OK,
                    fatal ? MessageBoxIcon.Error : MessageBoxIcon.Warning);
            }
            catch { /* never throw from the handler */ }
        }

        private static string WriteLog(Exception? ex, string source)
        {
            Directory.CreateDirectory(LogDir);
            string file = Path.Combine(LogDir, $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.log");

            var sb = new StringBuilder();
            sb.AppendLine($"AudioHexViewer crash log");
            sb.AppendLine($"UTC     : {DateTime.UtcNow:o}");
            sb.AppendLine($"Version : {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
            sb.AppendLine($"OS      : {Environment.OSVersion}");
            sb.AppendLine($"64-bit  : {Environment.Is64BitProcess}");
            sb.AppendLine($"Source  : {source}");
            sb.AppendLine(new string('-', 60));
            sb.AppendLine(ex?.ToString() ?? "(no exception object)");

            File.WriteAllText(file, sb.ToString());
            PruneOldLogs();
            return file;
        }

        private static void PruneOldLogs()
        {
            try
            {
                var files = new DirectoryInfo(LogDir)
                    .GetFiles("crash_*.log")
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .Skip(MaxLogFiles);
                foreach (var f in files)
                {
                    try { f.Delete(); } catch { /* ignore */ }
                }
            }
            catch { /* ignore */ }
        }
    }
}
