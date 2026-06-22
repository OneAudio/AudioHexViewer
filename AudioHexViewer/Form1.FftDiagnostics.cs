// ═══════════════════════════════════════════════════════════════════════════
// Form1.FftDiagnostics.cs  —  Log de diagnostic FFT (touche F8)
//
// Responsabilités :
//   • Activation / désactivation du log CSV (ToggleDiagLog)
//   • Écriture d'une ligne par frame FFT (WriteDiagLogLine)
//   • Fermeture propre du fichier (CloseDiagLog)
// ═══════════════════════════════════════════════════════════════════════════
using AudioHexViewer.Core;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace AudioHexViewer
{
    partial class Form1
    {
        private void ToggleDiagLog()
        {
            lock (_diagLogLock)
            {
                if (_diagLogEnabled)
                {
                    // ── Arrêt du log ──────────────────────────────────────────
                    _diagLogEnabled = false;
                    _diagLogWriter?.Flush();
                    _diagLogWriter?.Close();
                    _diagLogWriter = null;

                    string path = _diagLogPath ?? "(unknown)";
                    BeginInvoke((Action)(() =>
                        MessageBox.Show($"Log stopped.\nFile: {path}",
                            "Diagnostic Log", MessageBoxButtons.OK, MessageBoxIcon.Information)));
                }
                else
                {
                    // ── Démarrage du log ──────────────────────────────────────
                    string folder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "AudioHexViewer", "logs");
                    Directory.CreateDirectory(folder);
                    _diagLogPath   = Path.Combine(folder,
                        $"AudioHexViewer_Diag_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

                    _diagLogWriter = new StreamWriter(_diagLogPath, append: false,
                        encoding: Encoding.UTF8) { AutoFlush = false };

                    // En-tête lisible
                    var snap   = _pipeline!.Stats.Snapshot();
                    string mode = (_requestedCaptureFormat != null) ? "Exclusive" : "Shared";
                    _diagLogWriter.WriteLine("# AudioHexViewer - FFT Diagnostic Log");
                    _diagLogWriter.WriteLine($"# Started    : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                    _diagLogWriter.WriteLine($"# SR={snap.SampleRate} Hz | N={snap.FftSize} | " +
                                             $"Hop={snap.Hop} | Overlap={snap.Overlap01 * 100:F1}% | Mode={mode}");
                    _diagLogWriter.WriteLine("#");
                    _diagLogWriter.WriteLine("# HopActual  = AbsEnd[n] - AbsEnd[n-1]");
                    _diagLogWriter.WriteLine("# HopError   = HopActual - HopExpected  (>0 = gap = discontinuity)");
                    _diagLogWriter.WriteLine("# Drained    = frames drained by UI this tick (>1 = multiple drain, normal)");
                    _diagLogWriter.WriteLine("# Drops      = audio packets lost (ring full) since last FFT");
                    _diagLogWriter.WriteLine("# LockWait   = max wait on _pipeline?.Ring since last FFT");
                    _diagLogWriter.WriteLine("# FrmSkipped = cumulative FramesSkipped in SfftWorker (real TryReadWindow misses)");
                    _diagLogWriter.WriteLine("# Anomaly    = UI_DRAIN if drain>1 normal | HOP⚠(REAL) if true gap | DROP if loss");
                    _diagLogWriter.WriteLine("#");
                    _diagLogWriter.WriteLine(
                        "Timestamp;FFT#;AbsEnd;HopActual;HopExpected;HopError_ms;Drained;Drops;LockWait_ms;FrmSkipped;Anomaly;FftPerSec;FftAvgMs;DebugBarInfo");

                    // Reset des compteurs spécifiques au log
                    _diagLogFftIndex    = 0;
                    _diagLogAbsEndPrev  = -1;
                    Interlocked.Exchange(ref _diagDropForLog, 0);
                    _pipeline?.ResetLockStats();

                    _diagLogEnabled = true;

                    string path = _diagLogPath;
                    BeginInvoke((Action)(() =>
                        MessageBox.Show($"Log started.\nFile: {path}\n\nPress F8 to stop.",
                            "Diagnostic Log", MessageBoxButtons.OK, MessageBoxIcon.Information)));
                }
            }
        }

        private void WriteDiagLogLine(SpectrumFrame frame, int drained)
        {
            if (!_diagLogEnabled) return; // check rapide sans lock

            lock (_diagLogLock)
            {
                if (!_diagLogEnabled || _diagLogWriter == null) return;

                _diagLogFftIndex++;

                long absEnd      = frame.AbsEnd;
                int  sr          = frame.SampleRate;
                var  snap        = _pipeline!.Stats.Snapshot();
                long hopAttendu  = snap.Hop;

                // Drops depuis la FFT précédente (reset atomique)
                long drops = Interlocked.Exchange(ref _diagDropForLog, 0);

                // Hop réellement observé entre deux FFTs consécutives.
                // Pour la 1ère FFT (_diagLogAbsEndPrev < 0), on ne peut pas calculer
                // de hop : on marque INIT et on ignore l'erreur (faux positif FFT#1).
                bool isFirstFft = (_diagLogAbsEndPrev < 0);
                long hopActuel = isFirstFft
                    ? hopAttendu                 // force hopActuel = hopAttendu → HopErreur = 0
                    : absEnd - _diagLogAbsEndPrev;

                // Erreur en ms (>0 = samples manquants = gap)
                double hopErreurMs = (sr > 0 && !isFirstFft)
                    ? (hopActuel - hopAttendu) * 1000.0 / sr
                    : 0.0;

                // LockWait depuis la FFT précédente puis reset
                double lockWaitMs = _pipeline?.LockWaitMaxMs ?? 0.0;
                _pipeline?.ResetLockStats();

                // FramesSkipped dans SfftWorker (vrais TryReadWindow miss ou frames suspects)
                long framesSkipped = _pipeline!.Stats.Snapshot().FramesSkipped;

                // ── Détection d'anomalie réelle ───────────────────────────────────
                // drained=2 + HopActuel=2×hop = drain UI normal (SfftWorker OK)
                // drained=1 + HopActuel=2×hop = VRAI gap (résync SfftWorker)
                // → anomalie seulement si HopActuel > drained×HopAttendu
                string anomalie;
                if (isFirstFft)
                {
                    anomalie = "INIT";
                }
                else
                {
                    anomalie = "";
                    if (drops > 0)
                        anomalie += $"DROP={drops} ";
                    if (!isFirstFft && hopActuel > (long)drained * hopAttendu + hopAttendu / 2)
                        anomalie += "HOP⚠(REAL) ";
                    else if (drained > 1)
                        anomalie += $"UI_DRAIN={drained} ";
                    if (anomalie.Length == 0)
                        anomalie = "OK";
                }

                _diagLogWriter.WriteLine(
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff};" +
                    $"{_diagLogFftIndex};" +
                    $"{absEnd};" +
                    $"{hopActuel};" +
                    $"{hopAttendu};" +
                    $"{hopErreurMs:F2};" +
                    $"{drained};" +
                    $"{drops};" +
                    $"{lockWaitMs:F3};" +
                    $"{framesSkipped};" +
                    $"{anomalie};" +
                    $"{snap.FftPerSec:F2};" +
                    $"{snap.FftAvgMs:F3};" +
                    $"{_lastDebugBarText.Replace(';', '|')}");

                // Flush toutes les 20 FFTs pour ne pas perdre de données
                // sans sur-solliciter le disque (20 FFTs ≈ 6s à 3 FFT/s)
                if (_diagLogFftIndex % 20 == 0)
                    _diagLogWriter.Flush();

                _diagLogAbsEndPrev = absEnd;
            }
        }

        private void CloseDiagLog()
        {
            lock (_diagLogLock)
            {
                if (!_diagLogEnabled) return;
                _diagLogEnabled = false;
                _diagLogWriter?.Flush();
                _diagLogWriter?.Close();
                _diagLogWriter = null;
            }
        }

    }
}
