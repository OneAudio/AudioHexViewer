// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

// ═══════════════════════════════════════════════════════════════════════════
// Form1.TestSignals.cs  —  Générateur de signaux de test
//
// Responsabilités :
//   • F9 : ouvre/met en avant TestSignalDialog (non-modal)
//   • StartTestSignal / StopTestSignal : injectent dans _pipeline
//   • Tous les types de TestSignalType sont gérés ici
//   • Indicateur "[TEST: …]" dans la barre de titre (via UpdateTitleBar)
// ═══════════════════════════════════════════════════════════════════════════
using AudioHexViewer.Core;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AudioHexViewer
{
    partial class Form1
    {
        // ── Dialogue (singleton non-modal) ───────────────────────────────────
        private TestSignalDialog? _testSignalDialog;

        // ── État courant ─────────────────────────────────────────────────────
        private bool           _isTestSignalRunning = false;
        private TestSignalType _currentTestType     = TestSignalType.Silence;

        // Compat backward : Form1.AudioCapture.cs vérifie encore ces champs.
        // Ils mappent simplement sur _isTestSignalRunning.
        private bool _useSineTest
        {
            get => _isTestSignalRunning;
            set { /* no-op — piloté uniquement par StartTestSignal/Stop */ }
        }
        private bool _useMetricsRefTest
        {
            get => false;   // F10 supprimé
            set { }
        }

        // ── Task du générateur ────────────────────────────────────────────────
        private Task?                  _sineTask; // nom gardé pour compat AudioCapture
        private CancellationTokenSource? _sineCts;

        // ─────────────────────────────────────────────────────────────────────
        // F9 — ouvre ou met en avant le dialogue
        // ─────────────────────────────────────────────────────────────────────
        private void OpenTestSignalDialog()
        {
            if (_testSignalDialog == null || _testSignalDialog.IsDisposed)
            {
                _testSignalDialog = new TestSignalDialog
                {
                    GetSampleRate = () => currentFormat?.SampleRate ?? 48000,
                    GetFftSize    = () => analysisSampleSize,
                };
                _testSignalDialog.RunRequested  += (_, type) => StartTestSignal(type);
                _testSignalDialog.StopRequested += (_, _)    => StopTestSignal();

                // Positionner à droite de la fenêtre principale
                _testSignalDialog.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
                _testSignalDialog.Location = new System.Drawing.Point(
                    Math.Max(0, Left + Width  - _testSignalDialog.Width  - 8),
                    Math.Max(0, Top  + Height / 2 - _testSignalDialog.Height / 2));
            }

            _testSignalDialog.SetRunningState(_isTestSignalRunning);
            _testSignalDialog.NotifyContextChanged();
            _testSignalDialog.Show(this);
            _testSignalDialog.BringToFront();
        }

        // ─────────────────────────────────────────────────────────────────────
        // ProcessCmdKey — F9 seulement (F10 supprimé)
        // ─────────────────────────────────────────────────────────────────────
        protected override bool ProcessCmdKey(ref System.Windows.Forms.Message msg,
                                              System.Windows.Forms.Keys keyData)
        {
            if (keyData == System.Windows.Forms.Keys.F8)
            {
                ToggleDiagLog();
                return true;
            }

            if (keyData == System.Windows.Forms.Keys.F9)
            {
                OpenTestSignalDialog();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ─────────────────────────────────────────────────────────────────────
        // StartTestSignal — dispatch selon le type
        // ─────────────────────────────────────────────────────────────────────
        private void StartTestSignal(TestSignalType type)
        {
            if (_pipeline?.Ring == null)
            {
                System.Windows.Forms.MessageBox.Show(
                    "Please start audio capture first, then run the test signal.",
                    "Test Signal",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information);
                return;
            }

            StopTestSignal();   // arrêt propre si déjà en cours

            int sr  = currentFormat?.SampleRate ?? 48000;
            int fft = analysisSampleSize;

            _currentTestType     = type;
            _isTestSignalRunning = true;
            _testSignalDialog?.SetRunningState(true);
            UpdateTitleBar();

            _sineCts  = new CancellationTokenSource();
            var ct    = _sineCts.Token;

            _sineTask = Task.Run(() =>
            {
                const int CHUNK = 1024;
                float[] l = new float[CHUNK];
                float[] r = new float[CHUNK];

                // ── État par signal ──────────────────────────────────────────
                double phase1  = 0.0;  // fondamentale / canal L
                double phase2  = 0.0;  // spur / 2e ton
                uint   rng     = 0x12345678U;
                double spare   = 0.0;  // Box-Muller spare
                bool   hasSpare = false;

                // ── Paramètres pré-calculés ──────────────────────────────────
                static int CoherentBin(double f, int s, int n) =>
                    Math.Max(1, Math.Min(n / 2 - 5, (int)Math.Round(f * n / s)));

                double Dphi(double f) => 2.0 * Math.PI * f / sr;

                // Fréquences cohérentes
                int    k0   = CoherentBin(1000,  sr, fft);
                double f0   = k0 * (double)sr / fft;
                int    kSp  = CoherentBin(19730, sr, fft);
                kSp         = Math.Max(k0 + 10, Math.Min(fft / 2 - 2, kSp));
                double fSp  = kSp * (double)sr / fft;
                int    k19  = CoherentBin(19000, sr, fft);
                int    k20  = CoherentBin(20000, sr, fft);
                double f19  = k19 * (double)sr / fft;
                double f20  = k20 * (double)sr / fft;

                double dphi1  = Dphi(f0);
                double dphi2  = Dphi(fSp);
                double dphi19 = Dphi(f19);
                double dphi20 = Dphi(f20);
                double dphiQFS = Dphi(f0);

                var refSpec = (type == TestSignalType.SineMultiHarmonic)
                    ? TestWaves.CreateReferenceSpec(sr, fft)
                    : default;

                // ── Boucle principale ────────────────────────────────────────
                var    sw    = Stopwatch.StartNew();
                long   total = 0;

                while (!ct.IsCancellationRequested)
                {
                    switch (type)
                    {
                        case TestSignalType.SinePure1kHz:
                            TestWaves.FillStereoSine(l, r, CHUNK,
                                0.25, 0.0, dphi1, ref phase1);
                            break;

                        case TestSignalType.SineMultiHarmonic:
                            TestWaves.FillStereoReferenceSignal(
                                l, r, CHUNK, refSpec,
                                ref phase1, ref phase2, ref rng);
                            break;

                        case TestSignalType.DcPure:
                            TestWaves.FillStereoDC(l, r, CHUNK, 0.5);
                            break;

                        case TestSignalType.SquareWave1kHz:
                            TestWaves.FillStereoSquare(l, r, CHUNK,
                                0.25, dphi1, ref phase1);
                            break;

                        case TestSignalType.TwoToneIMD:
                            TestWaves.FillStereoTwoTone(l, r, CHUNK,
                                0.25, 0.25, dphi19, dphi20,
                                ref phase1, ref phase2);
                            break;

                        case TestSignalType.GaussianNoise:
                            // −9 dBFS RMS (σ=0.355) : clipping < 0.5 % vs 4.55 % à −6 dBFS
                            TestWaves.FillStereoGaussianNoise(l, r, CHUNK,
                                0.355, ref rng, ref spare, ref hasSpare);
                            break;

                        case TestSignalType.GaussianNoiseLow:
                            TestWaves.FillStereoGaussianNoise(l, r, CHUNK,
                                1e-4, ref rng, ref spare, ref hasSpare);
                            break;

                        case TestSignalType.Sawtooth1kHz:
                            TestWaves.FillStereoSawtooth(l, r, CHUNK,
                                0.25, dphi1, ref phase1);
                            break;

                        case TestSignalType.QuasiFullScale:
                            TestWaves.FillStereoSine(l, r, CHUNK,
                                Math.Pow(10, -0.5 / 20.0), 0.0, dphiQFS, ref phase1);
                            break;

                        case TestSignalType.LoopbackLR:
                            // L et R identiques — H=1∠0° en Bode Plot
                            TestWaves.FillStereoSine(l, r, CHUNK,
                                0.25, 0.0, dphi1, ref phase1);
                            break;

                        case TestSignalType.Silence:
                        default:
                            TestWaves.FillStereoSilence(l, r, CHUNK);
                            break;
                    }

                    _pipeline!.PushAudio(l, r);

                    total += CHUNK;
                    double targetSec = total / (double)sr;
                    double sleepSec  = targetSec - sw.Elapsed.TotalSeconds;
                    if (sleepSec > 0.001)
                        Thread.Sleep((int)(sleepSec * 1000.0));
                    else
                        Thread.Yield();
                }
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // StopTestSignal
        // ─────────────────────────────────────────────────────────────────────
        private void StopTestSignal()
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

                _isTestSignalRunning = false;
                _testSignalDialog?.SetRunningState(false);
                if (InvokeRequired) Invoke(UpdateTitleBar);
                else UpdateTitleBar();
            }
        }

        // Conservé pour compatibilité Form1.AudioCapture.cs (line 296)
        private void StopSineTestSource() => StopTestSignal();

        // Conservé pour compatibilité Form1.AudioCapture.cs (line 181-182)
        private void StartSineTestSource(int sampleRate,
                                          double freqHz = 1000.0,
                                          double amp    = 0.25)
            => StartTestSignal(_currentTestType);

        // Conservé pour compatibilité Form1.AudioCapture.cs (line 179)
        private void StartMetricsReferenceTestSource(int sampleRate)
            => StartTestSignal(TestSignalType.SineMultiHarmonic);
    }
}
