// ═══════════════════════════════════════════════════════════════════════════
// Form1.AudioFile.cs  —  Post-Process mode: audio file playback
//
// States:
//   _isFilePlayback = true  → file task is running (reading samples)
//   _fileIsPaused   = true  → file loaded, task stopped (reader keeps position)
//   _audioFileReader != null → file is open (playing or paused)
//
// EnsureReadRunning  → StartFilePlayback (first time) or ResumeFilePlayback
// EnsureReadStopped  → PauseFilePlayback  (keeps position)
// StopFilePlayback   → full cleanup (disposes reader, resets all flags)
// SeekFileToStart    → _audioFileReader.Position = 0  (T0 button)
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
        // ── State ────────────────────────────────────────────────────────────
        private bool      _isFilePlayback  = false;   // task is running
        private bool      _fileIsPaused    = false;   // file open, task stopped
        private string?   _audioFileName;

        // Metadata shown in title bar
        private int      _fileInfoSR;
        private int      _fileInfoBits;
        private int      _fileInfoChannels;
        private TimeSpan _fileInfoDuration;

        // ── Internal ─────────────────────────────────────────────────────────
        private AudioFileReader?         _audioFileReader;
        private Task?                    _fileTask;
        private CancellationTokenSource? _fileCts;

        // ─────────────────────────────────────────────────────────────────────
        // StartFilePlayback — open file and start reading task from scratch
        // ─────────────────────────────────────────────────────────────────────
        private void StartFilePlayback(string path)
        {
            StopFilePlayback();   // full cleanup if already active

            AudioFileReader reader;
            try { reader = new AudioFileReader(path); }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"Cannot open audio file:\n{ex.Message}",
                    "Post-Process", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _audioFileName    = Path.GetFileName(path);
            _fileInfoSR       = reader.WaveFormat.SampleRate;
            _fileInfoChannels = reader.WaveFormat.Channels;
            _fileInfoDuration = reader.TotalTime;
            _fileInfoBits     = GetOriginalBitDepth(path, reader);
            _audioFileReader  = reader;

            currentFormat = WaveFormat.CreateIeeeFloatWaveFormat(_fileInfoSR, 2);
            _pipeline ??= new Core.FftPipelineController();
            RestartSfftFromUi();
            RecomputeHopAndTimer();
            analysisTimer?.Start();

            if (spectrumControl != null)
            {
                spectrumControl.FilePlaybackActive = true;
                if ((spectrumControl.SelectedChannelMode ?? "") == "Bode Plot")
                    spectrumControl.SetChannelModeFromCode("L+R");
            }

            _isFilePlayback = true;
            _fileIsPaused   = false;
            UpdateTitleBar();
            UpdateFileProgressWidget();

            LaunchReadingTask(reader, _fileInfoSR, _fileInfoChannels);
        }

        // ─────────────────────────────────────────────────────────────────────
        // ResumeFilePlayback — restart task from current reader position
        // ─────────────────────────────────────────────────────────────────────
        internal void ResumeFilePlayback()
        {
            if (!_fileIsPaused || _audioFileReader == null) return;

            _pipeline ??= new Core.FftPipelineController();
            RestartSfftFromUi();
            RecomputeHopAndTimer();
            analysisTimer?.Start();

            _isFilePlayback = true;
            _fileIsPaused   = false;
            UpdateTitleBar();
            UpdateFileProgressWidget();

            LaunchReadingTask(_audioFileReader, _fileInfoSR, _fileInfoChannels);
        }

        // ─────────────────────────────────────────────────────────────────────
        // PauseFilePlayback — stop task but keep reader at current position
        // ─────────────────────────────────────────────────────────────────────
        internal void PauseFilePlayback()
        {
            if (!_isFilePlayback && _fileTask == null) return;

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

            _isFilePlayback = false;
            _fileIsPaused   = true;

            analysisTimer?.Stop();
            _pipeline?.Stop();

            if (InvokeRequired) Invoke(UpdateTitleBar);
            else UpdateTitleBar();
            UpdateFileProgressWidget();
        }

        // ─────────────────────────────────────────────────────────────────────
        // StopFilePlayback — full cleanup (disposes reader, resets all flags)
        // ─────────────────────────────────────────────────────────────────────
        internal void StopFilePlayback()
        {
            if (!_isFilePlayback && !_fileIsPaused && _fileTask == null)
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
            _fileIsPaused   = false;
            _audioFileName  = null;
            currentFormat   = null;

            if (spectrumControl != null)
                spectrumControl.FilePlaybackActive = false;

            analysisTimer?.Stop();
            _pipeline?.Stop();

            // Explicitly hide the progress widget
            HideProgressWidget();

            if (InvokeRequired) Invoke(UpdateTitleBar);
            else UpdateTitleBar();
        }

        // ─────────────────────────────────────────────────────────────────────
        // SeekFileToStart — T0 button: seek to beginning
        // ─────────────────────────────────────────────────────────────────────
        internal void SeekFileToStart()
        {
            if (_audioFileReader == null) return;

            if (_isFilePlayback)
            {
                // Pause → seek → resume
                PauseFilePlayback();
                _audioFileReader.Position = 0;
                ResumeFilePlayback();
            }
            else
            {
                // Just seek (stays paused)
                _audioFileReader.Position = 0;
                UpdateFileProgressWidget();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // LaunchReadingTask — shared task for Start and Resume
        // ─────────────────────────────────────────────────────────────────────
        private void LaunchReadingTask(AudioFileReader reader, int fileSR, int fileChannels)
        {
            _fileCts  = new CancellationTokenSource();
            var ct    = _fileCts.Token;

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
                            reader.Position = 0;
                            totalSamples    = 0;
                            sw.Restart();
                            continue;
                        }
                        break;   // EOF without loop → pause at end
                    }

                    int frames = samplesRead / Math.Max(1, fileChannels);

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

                    // Real-time throttle
                    totalSamples += frames;
                    double targetSec = totalSamples / (double)fileSR;
                    double sleepSec  = targetSec - sw.Elapsed.TotalSeconds;
                    if (sleepSec > 0.001)
                        Thread.Sleep((int)(sleepSec * 1000.0));
                    else
                        Thread.Yield();
                }

                // EOF without loop → pause at end position (not full stop)
                if (!ct.IsCancellationRequested)
                    Invoke(() => PauseFilePlayback());
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────
        private static int GetOriginalBitDepth(string path, AudioFileReader decoded)
        {
            try
            {
                using var wfr = new WaveFileReader(path);
                if (wfr.WaveFormat.BitsPerSample > 0)
                    return wfr.WaveFormat.BitsPerSample;
            }
            catch { }

            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".mp3") return 16;

            return decoded.WaveFormat.BitsPerSample > 0
                ? decoded.WaveFormat.BitsPerSample
                : 32;
        }
    }
}
