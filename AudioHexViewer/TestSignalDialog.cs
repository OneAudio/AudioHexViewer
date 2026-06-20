using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using AudioHexViewer.Core;

namespace AudioHexViewer
{
    /// <summary>
    /// Dialogue non-modal "Test Signal Generator".
    /// Ouvert par F9 ; reste visible pendant que le signal tourne.
    /// </summary>
    internal sealed class TestSignalDialog : Form
    {
        // ── Callbacks fournis par Form1 ──────────────────────────────────────
        public Func<int>?              GetSampleRate;
        public Func<int>?              GetFftSize;
        public event EventHandler<TestSignalType>? RunRequested;
        public event EventHandler?     StopRequested;

        // ── Contrôles ────────────────────────────────────────────────────────
        private readonly ComboBox   _combo;
        private readonly RichTextBox _rtb;
        private readonly Button     _btnRunStop;

        // ── État ─────────────────────────────────────────────────────────────
        private bool _isRunning;

        // ── Constantes de mise en page ────────────────────────────────────────
        private const int PAD = 10;

        // ─────────────────────────────────────────────────────────────────────
        public TestSignalDialog()
        {
            // ── Form ─────────────────────────────────────────────────────────
            Text            = "Test Signal Generator";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            ShowInTaskbar   = false;
            ClientSize      = new Size(530, 385);
            StartPosition   = FormStartPosition.Manual;   // positionné par Form1

            // ── Label Signal ──────────────────────────────────────────────────
            var lblSignal = new Label
            {
                Text      = "Signal:",
                Location  = new Point(PAD, PAD + 3),
                AutoSize  = true,
                Font      = new Font(Font.FontFamily, 9f, FontStyle.Bold),
            };
            Controls.Add(lblSignal);

            // ── ComboBox ──────────────────────────────────────────────────────
            _combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location      = new Point(60, PAD),
                Size          = new Size(ClientSize.Width - 60 - PAD, 24),
                Font          = new Font(Font.FontFamily, 9f),
            };
            foreach (TestSignalType t in Enum.GetValues(typeof(TestSignalType)))
                _combo.Items.Add(TestSignalInfo.GetDisplayName(t));
            _combo.SelectedIndex   = 0;
            _combo.SelectedIndexChanged += (_, _) => RefreshDescription();
            Controls.Add(_combo);

            // ── RichTextBox description ───────────────────────────────────────
            _rtb = new RichTextBox
            {
                Location    = new Point(PAD, PAD + 34),
                Size        = new Size(ClientSize.Width - 2 * PAD, 295),
                ReadOnly    = true,
                BackColor   = Color.FromArgb(20, 20, 30),
                ForeColor   = Color.FromArgb(200, 220, 240),
                Font        = new Font("Consolas", 8.25f),
                BorderStyle = BorderStyle.None,
                ScrollBars  = RichTextBoxScrollBars.Vertical,
                WordWrap    = false,
            };
            Controls.Add(_rtb);

            // ── Séparateur ────────────────────────────────────────────────────
            var sep = new Label
            {
                Location  = new Point(PAD, PAD + 34 + 295 + 4),
                Size      = new Size(ClientSize.Width - 2 * PAD, 1),
                BackColor = Color.FromArgb(80, 80, 100),
            };
            Controls.Add(sep);

            // ── Bouton RUN/STOP ───────────────────────────────────────────────
            _btnRunStop = new Button
            {
                Size      = new Size(140, 34),
                Font      = new Font(Font.FontFamily, 10f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
            };
            _btnRunStop.Location = new Point(
                (ClientSize.Width - _btnRunStop.Width) / 2,
                PAD + 34 + 295 + 14);
            SetRunningVisual(false);
            _btnRunStop.Click += BtnRunStop_Click;
            Controls.Add(_btnRunStop);

            FormClosing += (_, _) =>
            {
                if (_isRunning)
                    StopRequested?.Invoke(this, EventArgs.Empty);
            };

            RefreshDescription();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Appelé par Form1 après chaque changement de SR ou N.</summary>
        public void NotifyContextChanged() => RefreshDescription();

        /// <summary>Synchronise l'état visuel RUN/STOP depuis Form1.</summary>
        public void SetRunningState(bool running)
        {
            _isRunning = running;
            SetRunningVisual(running);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private
        // ─────────────────────────────────────────────────────────────────────

        private TestSignalType SelectedType =>
            (TestSignalType)Math.Max(0, _combo.SelectedIndex);

        private void BtnRunStop_Click(object? sender, EventArgs e)
        {
            if (_isRunning)
            {
                StopRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                RunRequested?.Invoke(this, SelectedType);
            }
        }

        private void SetRunningVisual(bool running)
        {
            if (running)
            {
                _btnRunStop.Text      = "■  STOP";
                _btnRunStop.BackColor = Color.FromArgb(180, 40, 40);
                _btnRunStop.ForeColor = Color.White;
                _btnRunStop.FlatAppearance.BorderColor = Color.FromArgb(220, 60, 60);
            }
            else
            {
                _btnRunStop.Text      = "▶  RUN";
                _btnRunStop.BackColor = Color.FromArgb(30, 140, 60);
                _btnRunStop.ForeColor = Color.White;
                _btnRunStop.FlatAppearance.BorderColor = Color.FromArgb(50, 180, 80);
            }
        }

        private void RefreshDescription()
        {
            int sr = GetSampleRate?.Invoke() ?? 48000;
            int n  = GetFftSize ?.Invoke() ?? 65536;
            _rtb.Text = BuildDescription(SelectedType, sr, n);
            _rtb.SelectionStart  = 0;
            _rtb.SelectionLength = 0;
        }

        // ── Descriptions par signal ──────────────────────────────────────────

        private static string BuildDescription(TestSignalType t, int sr, int n)
        {
            var ic = CultureInfo.InvariantCulture;

            // Helpers communs
            static string dBFS(double linAmp) =>
                $"{20.0 * Math.Log10(Math.Max(1e-15, Math.Abs(linAmp))):F2} dBFS";
            static int  CoherentBin(double targetHz, int sr, int n) =>
                Math.Max(1, Math.Min(n / 2 - 5, (int)Math.Round(targetHz * n / sr)));
            static double BinFreq(int k, int sr, int n) => k * (double)sr / n;

            double df = (double)sr / n;

            switch (t)
            {
                // ── 1. Sinus pur 1 kHz ────────────────────────────────────────
                case TestSignalType.SinePure1kHz:
                {
                    int    k0 = CoherentBin(1000, sr, n);
                    double f0 = BinFreq(k0, sr, n);
                    const double amp = 0.25;
                    double rms = amp / Math.Sqrt(2);
                    return
$@"PURE SINE — {f0:F2} Hz,  {dBFS(amp)} peak
{"".PadRight(55,'─')}

Signal composition:
  Frequency   : {f0:F2} Hz  (coherent bin #{k0},  ΔF = {df:F4} Hz)
  Peak ampl.  : {dBFS(amp)}  ({amp:F4} FS)
  RMS level   : {dBFS(rms)}  ({rms:F4} FS)
  DC offset   : none  (0.0000 FS)

Expected spectrum measurements:
  Peak        : {dBFS(amp)}  @  {f0:F2} Hz
  THD + N     : < −150 dBc  (digital ideal — no quantisation noise)
  Noise floor : < −200 dBFS (no noise added)

Expected stats panel:
  Vrms (AC)   : depends on calibration
  Vdc         : 0.000 FS";
                }

                // ── 2. Signal multi-harmoniques ───────────────────────────────
                case TestSignalType.SineMultiHarmonic:
                {
                    int    k0    = CoherentBin(1000,  sr, n);
                    double f0    = BinFreq(k0,  sr, n);
                    int    kSpur = CoherentBin(19730, sr, n);
                    kSpur        = Math.Max(k0 + 10, Math.Min(n / 2 - 2, kSpur));
                    double fSpur = BinFreq(kSpur, sr, n);

                    const double fundDbfs = -12.0;
                    const double h2Dbfs   = -90.0;
                    const double h3Dbfs   = -100.0;
                    const double h4Dbfs   = -110.0;
                    const double h5Dbfs   = -120.0;
                    const double spurDbfs = -70.0;

                    static double P(double db) => Math.Pow(10, db / 20.0);
                    double aFund = P(fundDbfs);
                    double thdRms = Math.Sqrt(P(h2Dbfs)*P(h2Dbfs) + P(h3Dbfs)*P(h3Dbfs)
                                            + P(h4Dbfs)*P(h4Dbfs) + P(h5Dbfs)*P(h5Dbfs)) / aFund;
                    double thdDbc = 20.0 * Math.Log10(thdRms);

                    return
$@"MULTI-HARMONIC REFERENCE SIGNAL
{"".PadRight(55,'─')}

Signal composition (all bins coherent, FFT-bin-locked):
  Fundamental : {f0:F2} Hz  (bin #{k0})   {fundDbfs:F2} dBFS
  H2          : {f0*2:F2} Hz              {h2Dbfs:F2} dBFS  ({h2Dbfs-fundDbfs:F2} dBc)
  H3          : {f0*3:F2} Hz             {h3Dbfs:F2} dBFS  ({h3Dbfs-fundDbfs:F2} dBc)
  H4          : {f0*4:F2} Hz             {h4Dbfs:F2} dBFS  ({h4Dbfs-fundDbfs:F2} dBc)
  H5          : {f0*5:F2} Hz             {h5Dbfs:F2} dBFS  ({h5Dbfs-fundDbfs:F2} dBc)
  Spur        : {fSpur:F2} Hz  (bin #{kSpur})  {spurDbfs:F2} dBFS  ({spurDbfs-fundDbfs:F2} dBc)
  White noise : −140.00 dBFS RMS  (negligible)

Expected measurements:
  Peak level  : {fundDbfs:F2} dBFS  @ {f0:F2} Hz
  THD (H2–H5): {thdDbc:F1} dBc  (theorical)
  SFDR        : {spurDbfs - fundDbfs:F1} dBc  (spur @ {fSpur:F1} Hz)";
                }

                // ── 3. DC pur ─────────────────────────────────────────────────
                case TestSignalType.DcPure:
                {
                    const double dcLevel = 0.5;
                    return
$@"PURE DC SIGNAL
{"".PadRight(55,'─')}

Signal composition:
  DC level    : {dBFS(dcLevel)}  ({dcLevel:F4} FS)
  AC content  : none  (0.0000 FS)

Expected measurements:
  Spectrum    : single peak at 0 Hz = {dBFS(dcLevel)}
  RMS         : {dBFS(dcLevel)}  (= DC level, no AC)
  Vdc         : {dcLevel:F4} FS × calibration factor
  AC RMS      : 0.0000 FS  (below noise floor)

Note: Enable DC mode (AC/DC → DC) to see the 0 Hz bin.
      In AC mode the DC component is suppressed.";
                }

                // ── 4. Onde carrée 1 kHz ──────────────────────────────────────
                case TestSignalType.SquareWave1kHz:
                {
                    int    k0  = CoherentBin(1000, sr, n);
                    double f0  = BinFreq(k0, sr, n);
                    const double amp = 0.25;
                    // H1 = amp, H3 = amp/3, H5 = amp/5 ...
                    double thd = Math.Sqrt(Math.PI * Math.PI / 8.0 - 1.0); // ~48.34 %
                    return
$@"SQUARE WAVE — {f0:F2} Hz,  {dBFS(amp)} peak
{"".PadRight(55,'─')}

Signal composition (odd harmonics only):
  H1  : {f0,9:F2} Hz   {dBFS(amp)}        ({amp:F4} FS)
  H3  : {f0*3,9:F2} Hz   {dBFS(amp/3):>10}   (−9.54 dBc)
  H5  : {f0*5,9:F2} Hz   {dBFS(amp/5):>10}  (−13.98 dBc)
  H7  : {f0*7,9:F2} Hz   {dBFS(amp/7):>10}  (−16.90 dBc)
  H9  : {f0*9,9:F2} Hz   {dBFS(amp/9):>10}  (−19.08 dBc)
  ···  all odd harmonics up to Nyquist

Expected measurements:
  Peak level  : {dBFS(amp)}
  RMS         : {dBFS(amp)}  (RMS = peak for ideal square)
  THD (theory): {thd * 100:F2} %  =  {20*Math.Log10(thd):F2} dBc  (vs H1)
  H3/H1 ratio :  −9.54 dBc
  H5/H1 ratio : −13.98 dBc
  H7/H1 ratio : −16.90 dBc";
                }

                // ── 5. Deux tons IMD ──────────────────────────────────────────
                case TestSignalType.TwoToneIMD:
                {
                    int    k1 = CoherentBin(19000, sr, n);
                    int    k2 = CoherentBin(20000, sr, n);
                    double f1 = BinFreq(k1, sr, n);
                    double f2 = BinFreq(k2, sr, n);
                    const double amp = 0.25; // each tone
                    double combinedRms = amp; // sqrt(A^2/2 + A^2/2) = A
                    double combinedPeak = amp * 2;
                    return
$@"TWO-TONE IMD — {f1:F2} Hz + {f2:F2} Hz
{"".PadRight(55,'─')}

Signal composition:
  Tone 1 (f1) : {f1:F2} Hz  (bin #{k1})   {dBFS(amp)} peak
  Tone 2 (f2) : {f2:F2} Hz  (bin #{k2})   {dBFS(amp)} peak
  Combined RMS: {dBFS(combinedRms)}  (two equal tones)
  Combined peak: {dBFS(combinedPeak)} (max constructive sum)

Expected IMD products (for a non-linear system under test):
  IM2 (f2−f1) : {f2-f1:F2} Hz   ← 2nd-order
  IM2 (f2+f1) : {f2+f1:F2} Hz   ← 2nd-order (may exceed Nyquist)
  IM3 (2f1−f2): {2*f1-f2:F2} Hz   ← 3rd-order (most critical in audio)
  IM3 (2f2−f1): {2*f2-f1:F2} Hz   ← 3rd-order

For an ideal digital generator:
  All IM products: < −200 dBFS (no distortion in software)
  Use with loopback to measure ADC/DAC non-linearity.";
                }

                // ── 6. Bruit blanc gaussien ───────────────────────────────────
                case TestSignalType.GaussianNoise:
                {
                    const double rms = 0.355; // 10^(-9/20) ≈ 0.355 FS  → clipping < 0.5 %
                    // PSD per bin: amplitude ≈ rms / sqrt(N/2)
                    double perBinAmp = rms / Math.Sqrt(n / 2.0);
                    double perBinDb  = 20 * Math.Log10(perBinAmp);
                    return
$@"GAUSSIAN WHITE NOISE — {dBFS(rms)} RMS  (−9 dBFS)
{"".PadRight(55,'─')}

Signal properties:
  Target RMS  : {rms:F4} FS  ({dBFS(rms)})
  Distribution: Gaussian  (Box-Muller algorithm)
  Bandwidth   : 0 to {sr/2000.0:F3} kHz  (full Nyquist)
  Spectral PSD: ≈ {perBinDb:F1} dBFS / bin  (N={n}, ΔF={df:F4} Hz)

Expected measurements:
  Spectrum    : flat noise floor ≈ {perBinDb:F1} dBFS  (on average)
  Histogram   : Gaussian bell curve, σ ≈ {rms:F4} FS
  RMS         : {dBFS(rms)}  (±0.5 dB with many averages)
  Peak (crest): {20*Math.Log10(rms) - 12:F0} to {20*Math.Log10(rms) - 18:F0} dBFS  (3–4 σ)

Note: Amplitude reduced to −9 dBFS (vs −6 dBFS) to avoid clipping.
      At −6 dBFS (σ=0.5), ~4.55 % of samples exceed ±1.0 FS and are
      clipped, producing artefact spikes at ±FS in the histogram.";
                }

                // ── 7. Dents de scie 1 kHz ───────────────────────────────────
                case TestSignalType.Sawtooth1kHz:
                {
                    int    k0  = CoherentBin(1000, sr, n);
                    double f0  = BinFreq(k0, sr, n);
                    const double amp = 0.25;
                    double h1Amp  = 2 * amp / Math.PI;  // = 0.1592 FS
                    double rmsAmp = amp / Math.Sqrt(3);  // = 0.1443 FS
                    double thd    = Math.Sqrt(Math.PI * Math.PI / 6.0 - 1.0); // ~80.3 %
                    return
$@"SAWTOOTH (RAMP) — {f0:F2} Hz,  {dBFS(amp)} peak
{"".PadRight(55,'─')}

Signal composition (all harmonics, amplitude ∝ 1/n):
  H1  : {f0,9:F2} Hz   {dBFS(h1Amp):>10}   (2A/π = {h1Amp:F4} FS)
  H2  : {f0*2,9:F2} Hz   {dBFS(h1Amp/2):>10}   (−6.02 dBc  vs H1)
  H3  : {f0*3,9:F2} Hz   {dBFS(h1Amp/3):>10}   (−9.54 dBc)
  H4  : {f0*4,9:F2} Hz   {dBFS(h1Amp/4):>10}   (−12.04 dBc)
  H5  : {f0*5,9:F2} Hz   {dBFS(h1Amp/5):>10}   (−13.98 dBc)
  ···  all harmonics (odd AND even) up to Nyquist

Expected measurements:
  Peak level  : {dBFS(amp)}  (= A)
  H1 amplitude: {dBFS(h1Amp)}  (= 2A/π)
  RMS (theory): {dBFS(rmsAmp)}  (A/√3)
  THD vs H1   : {thd * 100:F2} %  =  {20*Math.Log10(thd):F2} dBc  (√(π²/6 − 1))";
                }

                // ── 8. Quasi pleine échelle ───────────────────────────────────
                case TestSignalType.QuasiFullScale:
                {
                    int    k0  = CoherentBin(1000, sr, n);
                    double f0  = BinFreq(k0, sr, n);
                    double amp = Math.Pow(10, -0.5 / 20.0);  // ≈ 0.9441 FS
                    double rms = amp / Math.Sqrt(2);
                    return
$@"QUASI FULL-SCALE SINE — {f0:F2} Hz,  {dBFS(amp)} peak
{"".PadRight(55,'─')}

Signal composition:
  Frequency   : {f0:F2} Hz  (coherent bin #{k0})
  Peak ampl.  : {dBFS(amp)}  ({amp:F4} FS)
  RMS level   : {dBFS(rms)}  ({rms:F4} FS)
  Headroom    : {-20*Math.Log10(Math.Max(1e-15,amp)):F2} dB  before digital clipping

Expected measurements:
  Peak        : {dBFS(amp)}  @ {f0:F2} Hz
  THD + N     : < −150 dBc  (if no clipping in the audio chain)
  Noise floor : < −200 dBFS (no noise added)

Usage:
  Reveals any non-linearity at high drive levels.
  If THD > −60 dBc: clipping or non-linearity detected.
  Clipping threshold: ±1.0000 FS (hard integer limit).";
                }

                // ── 9. Loopback L = R ─────────────────────────────────────────
                case TestSignalType.LoopbackLR:
                {
                    int    k0  = CoherentBin(1000, sr, n);
                    double f0  = BinFreq(k0, sr, n);
                    const double amp = 0.25;
                    return
$@"LOOPBACK L = R — Bode Plot baseline
{"".PadRight(55,'─')}

Signal composition (both channels identical):
  Sine        : {f0:F2} Hz (coherent, bin #{k0})
  Peak ampl.  : {dBFS(amp)}  ({amp:F4} FS)
  L channel   : reference (X — Input)
  R channel   : identical copy (Y = X — Output)

Expected Bode Plot measurements (Norm OFF, AC coupling):
  Gain        :   0.00 dB  (flat, all frequencies)
  Phase       :   0.00 °   (flat, all frequencies)
  Coherence γ²: 100.0 %   (flat, all frequencies)

This validates:
  ✓  H1 estimator accuracy    (H = Y/X = 1∠0°)
  ✓  Coherence calculation    (γ² = 1 everywhere)
  ✓  Phase polarity           (0° = no inversion)
  ✓  Normalisation (Cap Ref): H_corr = 1∠0° everywhere
  ✓  Cross-spectrum convention (Y·X* / X·X*)";
                }

                // ── 11. Bruit blanc faible niveau ────────────────────────────
                case TestSignalType.GaussianNoiseLow:
                {
                    const double rms = 1e-4; // 10^(-80/20) = 0.0001 FS
                    double perBinAmp = rms / Math.Sqrt(n / 2.0);
                    double perBinDb  = 20 * Math.Log10(Math.Max(1e-15, perBinAmp));
                    return
$@"GAUSSIAN WHITE NOISE (LOW LEVEL) — {dBFS(rms)} RMS
{"".PadRight(55,'─')}

Signal properties:
  Target RMS  : {rms:E2} FS  ({dBFS(rms)})
  Distribution: Gaussian  (Box-Muller algorithm)
  Bandwidth   : 0 to {sr/2000.0:F3} kHz  (full Nyquist)
  Spectral PSD: ≈ {perBinDb:F1} dBFS / bin  (N={n}, ΔF={df:F4} Hz)

Expected measurements:
  Spectrum    : flat noise floor ≈ {perBinDb:F1} dBFS  (on average)
  Histogram   : Gaussian bell curve, σ ≈ {rms:E2} FS  (very narrow)
  RMS         : {dBFS(rms)}  (±1 dB with many averages)

Usage:
  Low-level stimulus to measure analyser noise floor without
  saturating the input. Typical use: verify that THD+N floor
  is limited by the analyser, not the generator.";
                }

                // ── 10. Silence ───────────────────────────────────────────────
                case TestSignalType.Silence:
                default:
                {
                    return
$@"SILENCE — digital zeros (0.0000 FS)
{"".PadRight(55,'─')}

Signal properties:
  All samples : 0.0000 FS  (exact, no noise added)
  Bandwidth   : 0 to {sr/2000.0:F3} kHz

Expected measurements:
  Spectrum    : below −300 dBFS  (analyser numeric floor)
  RMS         : 0.0000 FS  (−∞ dBFS)
  Vdc         : 0.000 FS
  Histogram   : single bin at code 0 only

Usage:
  Checks for signal leakage into the measurement path.
  In AC-coupled mode: any measured DC indicates an
  interface issue or DC-blocking filter artefact.
  Useful to baseline the analyser's own noise floor.";
                }
            }
        }
    }
}
