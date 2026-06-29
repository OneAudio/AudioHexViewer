// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

// ═══════════════════════════════════════════════════════════════════════════
// Form1.FftTicker.cs  —  Ticker de progression FFT (UX)
//
// Responsabilités :
//   • Pilote SetAcqProgress() sur SpectrumControl (GDI+ owner-drawn)
//   • Deux modes : priming (N/Fs) et running (hop/Fs)
//   • Masqué si période < 1 s
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
        // ── Helper central ────────────────────────────────────────────────────
        private void PushAcqProgress(int pct, bool visible)
        {
            if (_spectrumCtlTicker == null) return;
            string timeStr = (_tickerFs > 0 && _tickerN > 0)
                ? (_fftTickerPeriodSec < 1.0
                    ? $"≈ {_fftTickerPeriodSec * 1000:F0} ms"
                    : $"≈ {_fftTickerPeriodSec:F2} s")
                : "";
            _spectrumCtlTicker.SetAcqProgress(pct, visible, timeStr);
        }

        // ── ResetFftTickerFromCurrentStats ────────────────────────────────────
        private void ResetFftTickerFromCurrentStats()
        {
            if (_spectrumCtlTicker == null) return;

            if (_tickerFs <= 0 || _tickerN <= 0 || _tickerHop <= 0)
            {
                _fftTickerEnabled = false;
                PushAcqProgress(0, false);
                return;
            }

            if (_tickerPriming) _tickerPriming = false;

            _fftTickerPeriodSec = _tickerHop / (double)_tickerFs;

            if (_fftTickerPeriodSec < 1.0)
            {
                _fftTickerEnabled = false;
                PushAcqProgress(0, false);
                return;
            }

            _fftTickerEnabled  = true;
            _fftTickerStartSec = _uiSw.Elapsed.TotalSeconds;
            PushAcqProgress(0, true);
        }

        // ── ArmFftTickerPriming ───────────────────────────────────────────────
        private void ArmFftTickerPriming(int fs, int fftSize, int hop, double overlap01)
        {
            if (_spectrumCtlTicker == null) return;

            _tickerFs        = fs;
            _tickerN         = fftSize;
            _tickerHop       = hop;
            _tickerOverlap01 = overlap01;
            _tickerPriming   = true;

            double periodSec = (overlap01 > 0.0) ? (fftSize / (double)fs)
                                                 : (hop    / (double)fs);
            _fftTickerPeriodSec = periodSec;

            if (_fftTickerPeriodSec < 1.0)
            {
                _fftTickerEnabled = false;
                PushAcqProgress(0, false);
                return;
            }

            _fftTickerEnabled  = true;
            _fftTickerStartSec = _uiSw.Elapsed.TotalSeconds;
            PushAcqProgress(0, true);
        }

        // ── ArmFftTicker ──────────────────────────────────────────────────────
        private void ArmFftTicker(int sampleRate, int hop)
        {
            if (_spectrumCtlTicker == null) return;

            double periodSec = (sampleRate > 0 && hop > 0) ? (hop / (double)sampleRate) : 0.0;
            _fftTickerPeriodSec = periodSec;
            _fftTickerStartSec  = _uiSw.Elapsed.TotalSeconds;
            _fftTickerEnabled   = (periodSec >= 1.0);

            PushAcqProgress(0, _fftTickerEnabled);
        }

        // ── TickFftTickerUi ───────────────────────────────────────────────────
        private void TickFftTickerUi()
        {
            if (_spectrumCtlTicker == null) return;

            if (!_fftTickerEnabled)
            {
                PushAcqProgress(0, false);
                return;
            }

            double now = _uiSw.Elapsed.TotalSeconds;
            double dt  = now - _fftTickerStartSec;
            if (_fftTickerPeriodSec <= 0.0) return;

            double p01 = Math.Clamp(dt / _fftTickerPeriodSec, 0.0, 1.0);
            int    pct = (int)Math.Round(p01 * 100.0);
            PushAcqProgress(pct, true);
        }

        // ── StopFftTicker ─────────────────────────────────────────────────────
        private void StopFftTicker()
        {
            _fftTickerEnabled   = false;
            _fftTickerPeriodSec = 0.0;
            _fftTickerStartSec  = 0.0;
            _tickerFs           = 0;
            _tickerN            = 0;
            _tickerHop          = 0;
            _tickerOverlap01    = 0.0;
            _tickerPriming      = true;
            PushAcqProgress(0, false);
        }
    }
}
