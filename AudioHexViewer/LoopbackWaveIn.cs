// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

using System;
using NAudio.Wave;

namespace AudioHexViewer
{
    internal sealed class LoopbackWaveIn : IWaveIn
    {
        private readonly IWaveProvider          _source;
        private readonly int                    _bytesPerTick;
        private          System.Threading.Timer? _timer;
        private volatile bool                   _running;

        // IWaveIn exige un setter (même vide)
        public WaveFormat WaveFormat { get => _source.WaveFormat; set { } }

        public event EventHandler<WaveInEventArgs>?  DataAvailable;
        public event EventHandler<StoppedEventArgs>? RecordingStopped;

        public LoopbackWaveIn(IWaveProvider source, int callbackIntervalMs = 10)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));

            int sr       = source.WaveFormat.SampleRate;
            int channels = source.WaveFormat.Channels;
            int bpSample = source.WaveFormat.BitsPerSample / 8;

            int samplesPerTick = Math.Max(1, (int)(sr * callbackIntervalMs / 1000.0));
            _bytesPerTick = samplesPerTick * channels * bpSample;
        }

        public void StartRecording()
        {
            if (_running) return;
            _running = true;
            _timer   = new System.Threading.Timer(Tick, null, 0, 10);
        }

        private void Tick(object? state)
        {
            if (!_running) return;
            var buf  = new byte[_bytesPerTick];
            int read = _source.Read(buf, 0, _bytesPerTick);
            if (read > 0)
                DataAvailable?.Invoke(this, new WaveInEventArgs(buf, read));
        }

        public void StopRecording()
        {
            if (!_running) return;
            _running = false;
            _timer?.Change(System.Threading.Timeout.Infinite,
                           System.Threading.Timeout.Infinite);
            RecordingStopped?.Invoke(this, new StoppedEventArgs());
        }

        public void Dispose()
        {
            StopRecording();
            _timer?.Dispose();
            _timer = null;
        }
    }
}
