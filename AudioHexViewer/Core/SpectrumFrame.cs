// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

using MathNet.Numerics;
using System;

namespace AudioHexViewer.Core
{
    internal sealed class SpectrumFrame : IDisposable
    {
        public int SampleRate { get; }
        public int FftSize { get; }
        public long AbsEnd { get; }

        // FFT complexe (RFFT) : bins [0 .. N/2], longueur = N/2+1
        // NOTE: ces tableaux peuvent provenir d'un pool => ne pas les conserver hors de la frame.
        public Complex32[]? LeftBins => _leftBins;
        public Complex32[]? RightBins => _rightBins;

        // Hz, longueur = N/2+1
        public double[] FreqAxisHz { get; }

        // --- Ownership / pooling ---
        private Complex32[]? _leftBins;
        private Complex32[]? _rightBins;

        // Callback (worker) pour rendre un buffer au pool.
        private readonly Action<Complex32[]>? _returnToPool;

        // Protection anti double-dispose
        private bool _disposed;

        public SpectrumFrame(
            int sampleRate,
            int fftSize,
            long absEnd,
            Complex32[]? leftBins,
            Complex32[]? rightBins,
            double[] freqAxisHz,
            Action<Complex32[]>? returnToPool = null)
        {
            SampleRate = sampleRate;
            FftSize = fftSize;
            AbsEnd = absEnd;
            _leftBins = leftBins;
            _rightBins = rightBins;
            FreqAxisHz = freqAxisHz;
            _returnToPool = returnToPool;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // IMPORTANT:
            // - On rend au pool uniquement si un callback a été fourni.
            // - On "null" les refs pour casser les chaînes de rétention mémoire côté UI.
            if (_returnToPool != null)
            {
                if (_leftBins != null) _returnToPool(_leftBins);
                if (_rightBins != null) _returnToPool(_rightBins);
            }

            _leftBins = null;
            _rightBins = null;
        }
    }
}
