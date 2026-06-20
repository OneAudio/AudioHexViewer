// ═══════════════════════════════════════════════════════════════════════════
// Form1.AudioFile.cs  —  Mode Post-Process : lecture de fichiers audio
//
// Démarré par EnsureReadRunning() quand _postProcessMode=true.
// Injecte les samples dans _pipeline.PushAudio() — identique aux test signals.
// Supporte le loop (rechargement en début de fichier sur EOF si _postProcessLoop).
// ═══════════════════════════════════════════════════════════════════════════
using NAudio.Wave;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AudioHexViewer
{
    partial class Form1
    {
        // ── État public (lu par UpdateTitleBar) ──────────────────────────────
        private bool      _isFilePlayback  = false;
        private string?   _audioFileName;

        // Métadonnées affichées dans la barre de titre
        private int      _fileInfoSR;
        private int      _fileInfoBits;
        private int      _fileInfoChannels;
        private TimeSpan _fileInfoDuration;

        // ── Privé ────────────────────────────────────────────────────────────
        private AudioFileReader?         _audioFileReader;
        private Task?                    _fileTask;
        private CancellationTokenSource? _fileCts;

        // ─────────────────────────────────────────────────────────────────────
        // StartFilePlayback — ouverture + pipeline + task temps-réel
        // ─────────────────────────────────────────────────────────────────────
        private void StartFilePlayback(string path)
        {
            StopFilePlayback();   // arrêt propre si déjà actif

            // 1. Ouvrir le fichier
            AudioFileReader reader;
            try { reader = new AudioFileReader(path); }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"Cannot open audio file:\n{ex.Message}",
                    "Post-Process", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 2. Collecter les métadonnées pour la barre de titre
            _audioFileName    = Path.GetFileName(path);
            _fileInfoSR       = reader.WaveFormat.SampleRate;
            _fileInfoChannels = reader.WaveFormat.Channels;
            _fileInfoDuration = reader.TotalTime;
            _fileInfoBits     = GetOriginalBitDepth(path, reader);
            _audioFileReader  = reader;

            // 3. Initialiser le pipeline avec le SR du fichier
            currentFormat = WaveFormat.CreateIeeeFloatWaveFormat(_fileInfoSR, 2);
            _pipeline ??= new Core.FftPipelineController();
            RestartSfftFromUi();

            // 4. Démarrer le timer d'analyse (normalement conditionné à capture != null)
            RecomputeHopAndTimer();
            analysisTimer?.Start();

            // 5. Désactiver Bode Plot
            if (spectrumControl != null)
            {
                spectrumControl.FilePlaybackActive = true;
                if ((spectrumControl.SelectedChannelMode ?? "") == "Bode Plot")
                    spectrumControl.SetChannelModeFromCode("L+R");
            }

            // 6. Marquer et mettre à jour le titre
            _isFilePlayback = true;
            UpdateTitleBar();

            // 7. Lancer la task
            _fileCts  = new CancellationTokenSource();
            var ct    = _fileCts.Token;
            int fileSR       = _fileInfoSR;
            int fileChannels = _fileInfoChannels;

            _fileTask = Task.Run(() =>
            {
                const int CHUNK = 1024;
                int   interleaved = CHUNK * fileChannels;
                float[] raw  = new float[interleaved];
                float[] left = new float[CHUNK];
                float[] rght = new float[CHUNK];

                var  sw           = Stopwatch.StartNew();
                long totalSamples = 0;

                while (!ct.IsCancellationRequested)
                {
                    int samplesRead = reader.Read(raw, 0, interleaved);

                    if (samplesRead == 0)
                    {
                        // EOF
                        if (_postProcessLoop && !ct.IsCancellationRequested)
                        {
                            reader.Position = 0;   // retour au début
                            continue;
                        }
                        break;   // fin sans loop → auto-arrêt
                    }

                    int frames = samplesRead / Math.Max(1, fileChannels);

                    // Dé-interleaving  (mono → dupliqué L et R)
                    for (int i = 0; i < frames; i++)
                    {
                        left[i] = raw[i * fileChannels];
                        rght[i] = fileChannels > 1
                            ? raw[i * fileChannels + 1]
                            : raw[i * fileChannels];
                    }

                    _pipeline?.PushAudio(
                        new ReadOnlySpan<float>(left, 0, frames),
                        new ReadOnlySpan<float>(rght, 0, frames));

                    // Throttle temps-réel
                    totalSamples += frames;
                    double targetSec = totalSamples / (double)fileSR;
                    double sleepSec  = targetSec - sw.Elapsed.TotalSeconds;
                    if (sleepSec > 0.001)
                        Thread.Sleep((int)(sleepSec * 1000.0));
                    else
                        Thread.Yield();
                }

                // EOF sans loop → mise à jour UI depuis le thread principal
                if (!ct.IsCancellationRequested)
                    Invoke(() => StopFilePlayback());
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // StopFilePlayback
        // ─────────────────────────────────────────────────────────────────────
        internal void StopFilePlayback()
        {
            if (!_isFilePlayback && _fileTask == null)
                return;

            try
            {
                _fileCts?.Cancel();
                _fileTask?.Wait(500);
            }
            catch { }
            finally
            {
                _fileTask = null;
                _fileCts?.Dispose();
                _fileCts = null;
            }

            _audioFileReader?.Dispose();
            _audioFileReader = null;

            _isFilePlayback = false;
            _audioFileName  = null;
            currentFormat   = null;

            if (spectrumControl != null)
                spectrumControl.FilePlaybackActive = false;

            analysisTimer?.Stop();
            _pipeline?.Stop();

            if (InvokeRequired) Invoke(UpdateTitleBar);
            else UpdateTitleBar();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Tente de lire la résolution d'origine du fichier.
        /// AudioFileReader convertit tout en float 32 bits — on re-lit l'en-tête
        /// WAV séparément pour obtenir le vrai BitsPerSample du fichier source.
        /// </summary>
        private static int GetOriginalBitDepth(string path, AudioFileReader decoded)
        {
            // WAV : relire directement l'en-tête
            try
            {
                using var wfr = new WaveFileReader(path);
                if (wfr.WaveFormat.BitsPerSample > 0)
                    return wfr.WaveFormat.BitsPerSample;
            }
            catch { }

            // MP3 : 16 bits par convention (MPEG Layer 3)
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".mp3") return 16;

            // Autres formats (WMA, AIFF…) : format décodé par NAudio = float 32
            return decoded.WaveFormat.BitsPerSample > 0
                ? decoded.WaveFormat.BitsPerSample
                : 32;
        }
    }
}
