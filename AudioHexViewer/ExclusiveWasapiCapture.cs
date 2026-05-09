using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AudioHexViewer
{
    public sealed class ExclusiveWasapiCapture : IWaveIn
    {
        private readonly WaveFormat _driverFormat;
        private readonly WaveFormat _outputFormat;
        private readonly MMDevice _device;

        private AudioClient? _audioClient;
        private AudioCaptureClient? _captureClient;
        private AutoResetEvent? _bufferReady;      // signalé par le driver
        private CancellationTokenSource? _cts;
        private Task? _captureTask;

        public event EventHandler<WaveInEventArgs>? DataAvailable;
        public event EventHandler<StoppedEventArgs>? RecordingStopped;

        /// <summary>Toujours float32 — format attendu par Form1.cs</summary>
        public WaveFormat WaveFormat
        {
            get => _outputFormat;
            set { }
        }

        public ExclusiveWasapiCapture(MMDevice device, WaveFormat driverFormat)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
            _driverFormat = driverFormat ?? throw new ArgumentNullException(nameof(driverFormat));
            _outputFormat = WaveFormat.CreateIeeeFloatWaveFormat(
                                _driverFormat.SampleRate, _driverFormat.Channels);
        }

        public void StartRecording()
        {
            if (_captureTask != null)
                throw new InvalidOperationException("Capture déjà démarrée.");

            _audioClient = _device.AudioClient;

            // Période native du device
            long period = _audioClient.DefaultDevicePeriod;

            // Mode Exclusif + EventCallback
            // hnsPeriodicity DOIT être == hnsBufferDuration en Exclusif (sinon E_INVALIDARG)
            _audioClient.Initialize(
                AudioClientShareMode.Exclusive,
                AudioClientStreamFlags.EventCallback,   // driver signale lui-même chaque buffer
                period,   // hnsBufferDuration
                period,   // hnsPeriodicity — identique obligatoirement en Exclusif
                _driverFormat,
                Guid.Empty);

            _captureClient = _audioClient.AudioCaptureClient;

            // Enregistre l'événement avant Start()
            _bufferReady = new AutoResetEvent(false);
            _audioClient.SetEventHandle(_bufferReady.SafeWaitHandle.DangerousGetHandle());

            _audioClient.Start();

            _cts = new CancellationTokenSource();
            _captureTask = Task.Run(() => CaptureLoop(_cts.Token));
        }

        private void CaptureLoop(CancellationToken token)
        {
            Exception? error = null;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    // Attend le signal du driver (timeout 500ms de sécurité)
                    if (_bufferReady == null || !_bufferReady.WaitOne(500))
                        continue;

                    if (token.IsCancellationRequested)
                        break;

                    // Vide tous les paquets disponibles à ce tick
                    while (_captureClient != null &&
                           _captureClient.GetNextPacketSize() > 0 &&
                           !token.IsCancellationRequested)
                    {
                        IntPtr buf = _captureClient.GetBuffer(
                            out int numFrames,
                            out AudioClientBufferFlags flags,
                            out long devPos,
                            out long qpcPos);

                        byte[] outputData = ConvertToFloat32(buf, numFrames, flags);

                        _captureClient.ReleaseBuffer(numFrames);

                        DataAvailable?.Invoke(this, new WaveInEventArgs(outputData, outputData.Length));
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                try { _audioClient?.Stop(); } catch { }
                RecordingStopped?.Invoke(this, new StoppedEventArgs(error));
            }
        }

        private byte[] ConvertToFloat32(IntPtr buf, int numFrames, AudioClientBufferFlags flags)
        {
            int channels = _driverFormat.Channels;
            int totalSamps = numFrames * channels;
            float[] floats = new float[totalSamps];

            if ((flags & AudioClientBufferFlags.Silent) == 0)
            {
                bool isFloat = IsDriverFloat();
                int bps = _driverFormat.BitsPerSample;

                for (int i = 0; i < totalSamps; i++)
                {
                    int byteOffset = i * (bps / 8);

                    if (isFloat && bps == 32)
                    {
                        floats[i] = Marshal.PtrToStructure<float>(buf + byteOffset);
                    }
                    else if (bps == 32)
                    {
                        int s = Marshal.ReadInt32(buf, byteOffset);
                        floats[i] = s / 2_147_483_648.0f;
                    }
                    else if (bps == 24)
                    {
                        int b0 = Marshal.ReadByte(buf, byteOffset);
                        int b1 = Marshal.ReadByte(buf, byteOffset + 1);
                        int b2 = Marshal.ReadByte(buf, byteOffset + 2);
                        int s = b0 | (b1 << 8) | (b2 << 16);
                        if ((s & 0x80_0000) != 0) s |= unchecked((int)0xFF00_0000);
                        floats[i] = s / 8_388_608.0f;
                    }
                    else if (bps == 16)
                    {
                        short s = Marshal.ReadInt16(buf, byteOffset);
                        floats[i] = s / 32_768.0f;
                    }
                }
            }

            byte[] result = new byte[totalSamps * sizeof(float)];
            Buffer.BlockCopy(floats, 0, result, 0, result.Length);
            return result;
        }

        private bool IsDriverFloat()
        {
            if (_driverFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                return true;
            if (_driverFormat is WaveFormatExtensible ext)
                return ext.SubFormat == new Guid("00000003-0000-0010-8000-00aa00389b71");
            return false;
        }

        public void StopRecording()
        {
            _cts?.Cancel();
            _bufferReady?.Set();           // débloque le WaitOne si en attente
            _captureTask?.Wait(2000);
            _captureTask = null;
        }

        public void Dispose()
        {
            StopRecording();
            _bufferReady?.Dispose();
            _bufferReady = null;
            _cts?.Dispose();
            _cts = null;
            _captureClient = null;
            _audioClient?.Dispose();
            _audioClient = null;
        }
    }
}