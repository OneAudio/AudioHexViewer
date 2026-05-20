using System;
using System.Runtime.InteropServices;
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AudioHexViewer
{
    /// <summary>
    /// Lecture WASAPI en mode exclusif — symétrique de ExclusiveWasapiCapture.
    /// La source (IWaveProvider) fournit du float32 stéréo.
    /// Ce classe convertit vers le format driver (float32 / int32 / int24 / int16)
    /// et alimente le render client WASAPI avec le scheduling MMCSS haute priorité.
    /// </summary>
    public sealed class ExclusiveWasapiPlayback : IDisposable
    {
        // ── P/Invoke ──────────────────────────────────────────────────────────
        [DllImport("winmm.dll")] private static extern uint timeBeginPeriod(uint p);
        [DllImport("winmm.dll")] private static extern uint timeEndPeriod(uint p);

        [DllImport("avrt.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr AvSetMmThreadCharacteristics(string TaskName, ref uint TaskIndex);
        [DllImport("avrt.dll")]
        private static extern bool AvRevertMmThreadCharacteristics(IntPtr handle);

        // ── Champs ────────────────────────────────────────────────────────────
        private readonly MMDevice     _device;
        private readonly WaveFormat   _driverFormat;
        private readonly IWaveProvider _source;       // float32 stéréo, même SR que driver

        private AudioClient?       _audioClient;
        private AudioRenderClient? _renderClient;
        private AutoResetEvent?    _bufferReady;
        private Thread?            _renderThread;
        private volatile bool      _running;
        private bool               _timerPeriodSet;

        public event EventHandler<StoppedEventArgs>? PlaybackStopped;

        // ── Construction ──────────────────────────────────────────────────────
        /// <param name="device">Périphérique de sortie.</param>
        /// <param name="driverFormat">Format négocié (int32/int24/int16/float32) — résultat de FindSupportedExclusiveFormat.</param>
        /// <param name="source">Fournisseur audio float32 stéréo (SineProvider). SR doit correspondre à driverFormat.SampleRate.</param>
        public ExclusiveWasapiPlayback(MMDevice device, WaveFormat driverFormat, IWaveProvider source)
        {
            _device       = device       ?? throw new ArgumentNullException(nameof(device));
            _driverFormat = driverFormat ?? throw new ArgumentNullException(nameof(driverFormat));
            _source       = source       ?? throw new ArgumentNullException(nameof(source));
        }

        // ── Démarrage ─────────────────────────────────────────────────────────
        public void Play()
        {
            if (_running) return;

            timeBeginPeriod(1);
            _timerPeriodSet = true;

            _audioClient = _device.AudioClient;
            long period  = _audioClient.DefaultDevicePeriod;

            // Certains drivers exigent MinimumDevicePeriod en exclusif
            try
            {
                _audioClient.Initialize(
                    AudioClientShareMode.Exclusive,
                    AudioClientStreamFlags.EventCallback,
                    period, period,
                    _driverFormat, Guid.Empty);
            }
            catch
            {
                // Retry avec la période minimale
                period = _audioClient.MinimumDevicePeriod;
                _audioClient.Initialize(
                    AudioClientShareMode.Exclusive,
                    AudioClientStreamFlags.EventCallback,
                    period, period,
                    _driverFormat, Guid.Empty);
            }

            _renderClient = _audioClient.AudioRenderClient;

            _bufferReady = new AutoResetEvent(false);
            _audioClient.SetEventHandle(_bufferReady.SafeWaitHandle.DangerousGetHandle());

            _running = true;
            _renderThread = new Thread(RenderLoop)
            {
                IsBackground = true,
                Priority     = ThreadPriority.Highest,
                Name         = "ExclusiveWasapiPlayback"
            };
            _renderThread.Start();

            _audioClient.Start();
        }

        // ── Boucle de rendu ───────────────────────────────────────────────────
        private void RenderLoop()
        {
            uint    taskIndex  = 0;
            IntPtr  avrtHandle = IntPtr.Zero;
            try { avrtHandle = AvSetMmThreadCharacteristics("Pro Audio", ref taskIndex); }
            catch { /* avrt.dll absent sur certains Windows Embedded */ }

            int framesPerPeriod  = _audioClient!.BufferSize;
            int channels         = _driverFormat.Channels;       // 2
            int srcBytesPerPeriod = framesPerPeriod * channels * sizeof(float);  // float32 src
            int dstBytesPerPeriod = framesPerPeriod * _driverFormat.BlockAlign;  // driver fmt

            byte[] srcBuf = new byte[srcBytesPerPeriod];
            byte[] dstBuf = new byte[dstBytesPerPeriod];

            Exception? error = null;
            try
            {
                while (_running)
                {
                    if (_bufferReady == null || !_bufferReady.WaitOne(500))
                        continue;
                    if (!_running) break;

                    // 1) Lire le sinus depuis la source (float32)
                    int bytesRead = _source.Read(srcBuf, 0, srcBytesPerPeriod);

                    // 2) Convertir float32 → format driver
                    ConvertFloat32ToDriver(srcBuf, bytesRead, dstBuf, framesPerPeriod);

                    // 3) Copier dans le buffer driver WASAPI
                    IntPtr renderBuf = _renderClient!.GetBuffer(framesPerPeriod);
                    Marshal.Copy(dstBuf, 0, renderBuf, dstBytesPerPeriod);
                    _renderClient.ReleaseBuffer(framesPerPeriod, AudioClientBufferFlags.None);
                }
            }
            catch (Exception ex) { error = ex; }
            finally
            {
                if (avrtHandle != IntPtr.Zero)
                    try { AvRevertMmThreadCharacteristics(avrtHandle); } catch { }
                try { _audioClient?.Stop(); } catch { }
                PlaybackStopped?.Invoke(this, new StoppedEventArgs(error));
            }
        }

        // ── Conversion float32 → format driver ───────────────────────────────
        private void ConvertFloat32ToDriver(byte[] src, int srcBytes, byte[] dst, int frames)
        {
            int   channels  = _driverFormat.Channels;
            int   bps       = _driverFormat.BitsPerSample;
            bool  isFloat   = IsDriverFloat();
            int   totalSamp = frames * channels;

            var srcSpan = MemoryMarshal.Cast<byte, float>(new Span<byte>(src, 0, srcBytes));

            if (isFloat && bps == 32)
            {
                // float32 → float32 : copie directe
                Buffer.BlockCopy(src, 0, dst, 0, Math.Min(srcBytes, dst.Length));
                return;
            }

            int dstOff = 0;
            for (int i = 0; i < totalSamp && i < srcSpan.Length; i++)
            {
                float s = Math.Clamp(srcSpan[i], -1f, 1f);

                if (bps == 32)
                {
                    int v = (int)(s * 2_147_483_647f);
                    dst[dstOff]     = (byte)v;
                    dst[dstOff + 1] = (byte)(v >> 8);
                    dst[dstOff + 2] = (byte)(v >> 16);
                    dst[dstOff + 3] = (byte)(v >> 24);
                    dstOff += 4;
                }
                else if (bps == 24)
                {
                    int v = (int)(s * 8_388_607f);
                    dst[dstOff]     = (byte)v;
                    dst[dstOff + 1] = (byte)(v >> 8);
                    dst[dstOff + 2] = (byte)(v >> 16);
                    dstOff += 3;
                }
                else // 16 bits
                {
                    short v = (short)(s * 32_767f);
                    dst[dstOff]     = (byte)v;
                    dst[dstOff + 1] = (byte)(v >> 8);
                    dstOff += 2;
                }
            }
        }

        private bool IsDriverFloat()
        {
            if (_driverFormat.Encoding == WaveFormatEncoding.IeeeFloat) return true;
            if (_driverFormat is WaveFormatExtensible ext)
                return ext.SubFormat == new Guid("00000003-0000-0010-8000-00aa00389b71");
            return false;
        }

        // ── Arrêt / nettoyage ─────────────────────────────────────────────────
        public void Stop()
        {
            if (!_running) return;
            _running = false;
            _bufferReady?.Set();
            _renderThread?.Join(2000);
        }

        public void Dispose()
        {
            Stop();
            if (_timerPeriodSet) { timeEndPeriod(1); _timerPeriodSet = false; }
            _bufferReady?.Dispose();
            _bufferReady   = null;
            _renderClient  = null;
            _audioClient?.Dispose();
            _audioClient   = null;
        }
    }
}
