// ═══════════════════════════════════════════════════════════════════════════
// Form1.FftTicker.cs  —  Ticker de progression FFT (UX)
//
// Responsabilités :
//   • Animation d'un label qui montre la progression du calcul FFT
//   • Deux modes : priming (pas encore de stats) et running (hop/Fs connus)
//   • Armement (ArmFftTicker*), tick (TickFftTickerUi), arrêt (StopFftTicker)
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
        //
        private void ResetFftTickerFromCurrentStats()
        {
            if (_lblFftTicker == null) return;

            // Si on n'a pas de paramètres valides, on masque (évite un "figé" trompeur)
            if (_tickerFs <= 0 || _tickerN <= 0 || _tickerHop <= 0)
            {
                _fftTickerEnabled = false;
                _lblFftTicker.Visible = false;
                _lblFftTicker.Text = "";
                return;
            }

            // Fin du priming : la 1ère FFT affichée marque le passage en steady
            if (_tickerPriming)
                _tickerPriming = false;

            // À chaque FFT affichée, on repart à 0% pour l'intervalle SUIVANT
            // Période en steady = hop/Fs
            _fftTickerPeriodSec = _tickerHop / (double)_tickerFs;

            // REW-like : si < 1 s, on cache
            if (_fftTickerPeriodSec < 1.0)
            {
                _fftTickerEnabled = false;
                _lblFftTicker.Visible = false;
                _lblFftTicker.Text = "";
                return;
            }

            _fftTickerEnabled = true;
            _fftTickerStartSec = _uiSw.Elapsed.TotalSeconds;

            _lblFftTicker.Visible = true;
            _lblFftTicker.Text = "0%";
        }

        //
        private void ArmFftTickerPriming(int fs, int fftSize, int hop, double overlap01)
        {
            if (_lblFftTicker == null) return;

            _tickerFs = fs;
            _tickerN = fftSize;
            _tickerHop = hop;
            _tickerOverlap01 = overlap01;

            _tickerPriming = true;

            // En priming, si overlap>0 : première FFT après N/Fs
            // Si overlap==0 : hop==N donc N/Fs==hop/Fs, pas besoin d'une règle spéciale.
            double periodSec = (overlap01 > 0.0) ? (fftSize / (double)fs)
                                                 : (hop / (double)fs);

            _fftTickerPeriodSec = periodSec;

            // REW-like : si trop rapide, on cache dès le départ
            if (_fftTickerPeriodSec < 1.0)
            {
                _fftTickerEnabled = false;
                _lblFftTicker.Visible = false;
                _lblFftTicker.Text = "";
                return;
            }

            _fftTickerEnabled = true;
            _fftTickerStartSec = _uiSw.Elapsed.TotalSeconds;
            _lblFftTicker.Visible = true;
            _lblFftTicker.Text = "0%";
        }

        //
        private void ArmFftTicker(int sampleRate, int hop)
        {
            if (_lblFftTicker == null)
                return;

            double periodSec = (sampleRate > 0 && hop > 0) ? (hop / (double)sampleRate) : 0.0;

            _fftTickerPeriodSec = periodSec;
            _fftTickerStartSec = _uiSw.Elapsed.TotalSeconds;

            // Règle : on n’affiche que si une FFT théorique prend >= 1 s
            _fftTickerEnabled = (periodSec >= 1.0);

            if (_fftTickerEnabled)
            {
                _lblFftTicker.Visible = true;
                _lblFftTicker.Text = "0%";
            }
            else
            {
                _lblFftTicker.Visible = false;
                _lblFftTicker.Text = "";
            }
        }

        //
        private void TickFftTickerUi()
        {
            if (_lblFftTicker == null) return;

            if (!_fftTickerEnabled)
            {
                // évite une valeur figée résiduelle
                _lblFftTicker.Text = "";
                _lblFftTicker.Visible = false;
                return;
            }

            double now = _uiSw.Elapsed.TotalSeconds;
            double dt = now - _fftTickerStartSec;

            if (_fftTickerPeriodSec <= 0.0) return;

            double p01 = dt / _fftTickerPeriodSec;
            if (p01 < 0.0) p01 = 0.0;
            if (p01 > 1.0) p01 = 1.0;

            int pct = (int)Math.Round(p01 * 100.0);

            // Affiche 0..100%. Si la FFT tarde (dt > period), on reste à 100%.
            _lblFftTicker.Text = pct.ToString() + "%";
        }

        //
        private void StopFftTicker()
        {
            _fftTickerEnabled = false;
            _fftTickerPeriodSec = 0.0;
            _fftTickerStartSec = 0.0;
            _tickerFs = 0;
            _tickerN = 0;
            _tickerHop = 0;
            _tickerOverlap01 = 0.0;
            _tickerPriming = true;

            if (_lblFftTicker != null)
            {
                _lblFftTicker.Visible = false;
                _lblFftTicker.Text = "";
            }
        }

    }
}
