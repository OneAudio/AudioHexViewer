using System;
using System.Runtime.InteropServices;
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AudioHexViewer
{
    public sealed class ExclusiveWasapiCapture : IWaveIn
    {
        // ── P/Invoke Windows Multimedia ──────────────────────────────────────
        [DllImport("winmm.dll")] private static extern uint timeBeginPeriod(uint p);
        [DllImport("winmm.dll")] private static extern uint timeEndPeriod(uint p);

        // Thread multimédia haute priorité (comme les DAWs professionnels)
        [DllImport("avrt.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr AvSetMmThreadCharacteristics(string TaskName, ref uint TaskIndex);
        [DllImport("avrt.dll")]
        private static extern bool AvRevertMmThreadCharacteristics(IntPtr handle);
        // ─────────────────────────────────────────────────────────────────────

        private readonly WaveFormat _driverFormat;
        private readonly WaveFormat _outputFormat;
        private readonly MMDevice _device;

        private AudioClient? _audioClient;
        private AudioCaptureClient? _captureClient;
        private AutoResetEvent? _bufferReady;
        private Thread? _captureThread;
        private volatile bool _running;
        private bool _timerPeriodSet;

        // Buffers pré-alloués (zéro GC dans la boucle de capture)
        private byte[]? _rawBytes;
        private float[]? _conversionBuf;
        private byte[]? _outputBytes;

        public event EventHandler<WaveInEventArgs>? DataAvailable;
        public event EventHandler<StoppedEventArgs>? RecordingStopped;

        /// <summary>Toujours float32 — format attendu par Form1.cs</summary>
        public WaveFormat WaveFormat { get => _outputFormat; set { } }

        public ExclusiveWasapiCapture(MMDevice device, WaveFormat driverFormat)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
            _driverFormat = driverFormat ?? throw new ArgumentNullException(nameof(driverFormat));
            _outputFormat = WaveFormat.CreateIeeeFloatWaveFormat(
                                _driverFormat.SampleRate, _driverFormat.Channels);
        }

        public void StartRecording()
        {
            if (_running) throw new InvalidOperationException("Capture déjà démarrée.");

            // ── CORRECTIF PRINCIPAL ─────────────────────────────────────────
            // Sans timeBeginPeriod(1), le scheduler Windows a une résolution de
            // 15,6ms. Notre période audio est de ~10ms → callbacks manquées.
            // NAudio's WasapiCapture fait ceci en interne ; nous aussi désormais.
            timeBeginPeriod(1);
            _timerPeriodSet = true;
            // ───────────────────────────────────────────────────────────────

            _audioClient = _device.AudioClient;
            long period = _audioClient.DefaultDevicePeriod;

            _audioClient.Initialize(
                AudioClientShareMode.Exclusive,
                AudioClientStreamFlags.EventCallback,
                period, period,
                _driverFormat, Guid.Empty);

            // Pré-allocation des buffers (taille fixe en exclusif)
            int framesPerPeriod = _audioClient.BufferSize;
            int samplesPerPeriod = framesPerPeriod * _driverFormat.Channels;
            _rawBytes = new byte[framesPerPeriod * _driverFormat.BlockAlign];
            _conversionBuf = new float[samplesPerPeriod];
            _outputBytes = new byte[samplesPerPeriod * sizeof(float)];

            _captureClient = _audioClient.AudioCaptureClient;

            _bufferReady = new AutoResetEvent(false);
            _audioClient.SetEventHandle(_bufferReady.SafeWaitHandle.DangerousGetHandle());

            _running = true;
            _captureThread = new Thread(CaptureLoop)
            {
                IsBackground = true,
                Priority = ThreadPriority.Highest,
                Name = "ExclusiveWasapiCapture"
            };
            _captureThread.Start();

            _audioClient.Start();
        }

        private void CaptureLoop()
        {
            // ── Thread multimédia "Pro Audio" ────────────────────────────────
            // Donne au thread la priorité MMCSS (au-dessus de Highest),
            // garantit le scheduling dans les délais audio.
            uint taskIndex = 0;
            IntPtr avrtHandle = IntPtr.Zero;
            try { avrtHandle = AvSetMmThreadCharacteristics("Pro Audio", ref taskIndex); }
            catch { /* avrt.dll absent sur certains Windows Embedded — non critique */ }
            // ─────────────────────────────────────────────────────────────────

            Exception? error = null;
            try
            {
                while (_running)
                {
                    if (_bufferReady == null || !_bufferReady.WaitOne(500))
                        continue;

                    if (!_running) break;

                    // Mode exclusif event : 1 événement = 1 buffer hardware.
                    // Une boucle while provoquait une double lecture sur certains drivers
                    // (GetNextPacketSize() > 0 une 2ème fois avec les mêmes données).
                    // La spec WASAPI exclusive event recommande de lire exactement 1 paquet
                    // par événement.
                    if (_captureClient == null || !_running) continue;
                    uint pktSize = (uint)_captureClient.GetNextPacketSize();
                    if (pktSize == 0) continue;

                    {
                        IntPtr buf = _captureClient.GetBuffer(
                            out int numFrames,
                            out AudioClientBufferFlags flags,
                            out long devPos,
                            out long qpcPos);

                        int rawByteCount = numFrames * _driverFormat.BlockAlign;
                        if (_rawBytes != null && rawByteCount <= _rawBytes.Length)
                            Marshal.Copy(buf, _rawBytes, 0, rawByteCount);

                        _captureClient.ReleaseBuffer(numFrames);

                        int outSampleCount = ConvertToFloat32InPlace(
                            numFrames, flags, _rawBytes!, _conversionBuf!);

                        int outByteCount = outSampleCount * sizeof(float);
                        Buffer.BlockCopy(_conversionBuf!, 0, _outputBytes!, 0, outByteCount);

                        DataAvailable?.Invoke(this, new WaveInEventArgs(_outputBytes!, outByteCount));
                    }
                }
            }
            catch (Exception ex) { error = ex; }
            finally
            {
                if (avrtHandle != IntPtr.Zero)
                    try { AvRevertMmThreadCharacteristics(avrtHandle); } catch { }

                try { _audioClient?.Stop(); } catch { }
                RecordingStopped?.Invoke(this, new StoppedEventArgs(error));
            }
        }

        private int ConvertToFloat32InPlace(
            int numFrames, AudioClientBufferFlags flags,
            byte[] rawBytes, float[] floats)
        {
            int channels = _driverFormat.Channels;
            int totalSamps = Math.Min(numFrames * channels, floats.Length);

            if ((flags & AudioClientBufferFlags.Silent) != 0)
            {
                Array.Clear(floats, 0, totalSamps);
                return totalSamps;
            }

            bool isFloat = IsDriverFloat();
            int bps = _driverFormat.BitsPerSample;
            int bpsBytes = bps / 8;

            for (int i = 0; i < totalSamps; i++)
            {
                int off = i * bpsBytes;
                if (off + bpsBytes > rawBytes.Length) break;

                if (isFloat && bps == 32)
                    floats[i] = BitConverter.ToSingle(rawBytes, off);
                else if (bps == 32)
                    floats[i] = BitConverter.ToInt32(rawBytes, off) / 2_147_483_648.0f;
                else if (bps == 24)
                {
                    int s = rawBytes[off] | (rawBytes[off + 1] << 8) | (rawBytes[off + 2] << 16);
                    if ((s & 0x80_0000) != 0) s |= unchecked((int)0xFF00_0000);
                    floats[i] = s / 8_388_608.0f;
                }
                else if (bps == 16)
                    floats[i] = BitConverter.ToInt16(rawBytes, off) / 32_768.0f;
            }
            return totalSamps;
        }

        private bool IsDriverFloat()
        {
            if (_driverFormat.Encoding == WaveFormatEncoding.IeeeFloat) return true;
            if (_driverFormat is WaveFormatExtensible ext)
                return ext.SubFormat == new Guid("00000003-0000-0010-8000-00aa00389b71");
            return false;
        }

        public void StopRecording()
        {
            if (!_running) return;
            _running = false;
            _bufferReady?.Set();
            _captureThread?.Join(2000);

            if (_timerPeriodSet) { timeEndPeriod(1); _timerPeriodSet = false; }
        }

        public void Dispose()
        {
            StopRecording();
            _bufferReady?.Dispose();
            _bufferReady = null;
            _rawBytes = null;
            _outputBytes = null;
            _conversionBuf = null;
            _captureClient = null;
            _audioClient?.Dispose();
            _audioClient = null;
        }
    }
}