// ═══════════════════════════════════════════════════════════════════════════
// Form1.TestSignals.cs  —  Sources de signal de test
//
// Responsabilités :
//   • F9  : sinus 1 kHz injecté dans FftPipelineController (StartSineTestSource)
//   • F10 : signal de référence multi-harmoniques (StartMetricsReferenceTestSource)
//   • Arrêt des sources de test (StopSineTestSource)
//   • Dispatch des touches F8/F9/F10 (ProcessCmdKey)
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
        private void StartSineTestSource(int sampleRate, double freqHz = 1000.0, double amp = 0.25)
        {
            if (_pipeline?.Ring == null) return;
            if (_sineTask != null) return;

            _sineCts = new CancellationTokenSource();
            var ct = _sineCts.Token;

            _sineTask = Task.Run(() =>
            {
                // écrit par paquets pour réduire la charge
                int chunk = 1024;
                var l = new float[chunk];
                var r = new float[chunk];

                double phase = 0.0;
                double dphi = 2.0 * Math.PI * freqHz / sampleRate;

                var sw = Stopwatch.StartNew();
                long total = 0;

                while (!ct.IsCancellationRequested)
                {
                    TestWaves.FillStereoSine(l, r, chunk, amp,0.05, dphi, ref phase); // ajoute offset DC 0.05 pour test mesures DC

                    // Écrit dans le ring (remplace l'entrée carte son)
                    _pipeline!.PushAudio(l, r);

                    // En mode F9, on doit aussi alimenter la fenêtre d'analyse "temps" (int24)
                    // sinon RMS/Peak/Calibration ne voient rien.

                    total += chunk;

                    // Throttle pour se caler sur le temps réel (~Fs)
                    // Temps théorique écoulé pour total samples
                    double targetSec = total / (double)sampleRate;
                    double nowSec = sw.Elapsed.TotalSeconds;
                    double sleepSec = targetSec - nowSec;

                    if (sleepSec > 0.001)
                        Thread.Sleep((int)(sleepSec * 1000.0));
                    else
                        Thread.Yield();
                }
            });
        }

        //
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F8)
            {
                ToggleDiagLog();
                return true;
            }

            if (keyData == Keys.F9)
            {
                _useSineTest = !_useSineTest;

                // évite d’avoir 2 modes en même temps
                if (_useSineTest) _useMetricsRefTest = false;

                MessageBox.Show(_useSineTest
                    ? "Sine test ON (1 kHz). Redémarre READ."
                    : "Sine test OFF (carte son). Redémarre READ.");
                return true;
            }

            if (keyData == Keys.F10)
            {
                _useMetricsRefTest = !_useMetricsRefTest;

                // évite d’avoir 2 modes en même temps
                if (_useMetricsRefTest) _useSineTest = false;

                MessageBox.Show(_useMetricsRefTest
                    ? "Ref mesures ON (coherent FFT + harmoniques + spur + bruit). Redémarre READ."
                    : "Ref mesures OFF (carte son). Redémarre READ.");
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        //
        private void StartMetricsReferenceTestSource(int sampleRate)
        {
            if (_pipeline?.Ring == null) return;
            if (_sineTask != null) return; // on réutilise la même task/cts que le mode F9 (un seul générateur)

            // FFT size utilisée par ton pipeline (celle choisie dans l'UI)
            // -> cohérent avec RestartSfftFromUi() qui redémarre _pipeliner sur analysisSampleSize
            int fftSize = analysisSampleSize;

            _sineCts = new CancellationTokenSource();
            var ct = _sineCts.Token;

            _sineTask = Task.Run(() =>
            {
                int chunk = 1024;
                var l = new float[chunk];
                var r = new float[chunk];

                // Specification du signal (voir TestWaves.CreateReferenceSpec pour le detail)
                var spec = TestWaves.CreateReferenceSpec(
                    fs: sampleRate,
                    fftSize: fftSize);

                double phaseFund = 0.0;
                double phaseSpur = 0.0;
                uint rngState = 0x12345678;

                var sw = Stopwatch.StartNew();
                long total = 0;

                while (!ct.IsCancellationRequested)
                {
                    // Génère le chunk (Bode Plot identiques) via ton vrai générateur
                    TestWaves.FillStereoReferenceSignal(
                        l, r, chunk,
                        spec,
                        ref phaseFund,
                        ref phaseSpur,
                        ref rngState
                    );

                    // Écrit dans le ring (remplace l'entrée carte son)
                    _pipeline!.PushAudio(l, r);

                    // Alimente aussi la fenêtre d'analyse "temps" (int24)
                    // pour que RMS/Peak/Calibration fonctionnent comme en mode carte son.

                    total += chunk;

                    // Throttle pour se caler sur le temps réel (~Fs)
                    double targetSec = total / (double)sampleRate;
                    double nowSec = sw.Elapsed.TotalSeconds;
                    double sleepSec = targetSec - nowSec;

                    if (sleepSec > 0.001)
                        Thread.Sleep((int)(sleepSec * 1000.0));
                    else
                        Thread.Yield();
                }
            });
        }

        //
        private void StopSineTestSource()
        {
            try
            {
                _sineCts?.Cancel();
                _sineTask?.Wait(500);
            }
            catch { /* ignore */ }
            finally
            {
                _sineTask = null;
                _sineCts?.Dispose();
                _sineCts = null;
            }
        }

    }
}
