using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AudioHexViewer.Core;

namespace AudioHexViewer
{
    internal partial class OscilloscopeControl : UserControl
    {
        // ── Callbacks fournis par Form1 ───────────────────────────────────────
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Func<RingBufferStereoFloat?>? GetRing       { get; set; }
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Func<int>?                    GetSampleRate { get; set; }
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Func<(bool vL, double oL, bool vR, double oR)>? GetInputCalibration { get; set; }

        // ── Événements ────────────────────────────────────────────────────────
        public event Action<bool>? RunStateChanged;
        public event Action?       RunRequested;

        // ── Couleurs ──────────────────────────────────────────────────────────
        private SpectrumColorSettings _colors = SpectrumColorSettings.Defaults();
        private Color BgColor       => Color.FromArgb(_colors.BackgroundArgb);
        private Color GridMajColor  => Color.FromArgb(_colors.GridArgb);
        private Color GridMinColor  => Color.FromArgb(_colors.GridMinorArgb);
        private Color TraceColorL   => Color.FromArgb(_colors.FftLeftArgb);
        private Color TraceColorR   => Color.FromArgb(_colors.FftRightArgb);
        private Color LabelColor    => Color.FromArgb(_colors.LabelMajorArgb);
        private Color InfoTextColor => Color.FromArgb(_colors.InfoTextArgb);

        // ── Constantes ────────────────────────────────────────────────────────
        private const int   H_DIVS        = 10;
        private const int   V_DIVS        = 8;
        private const float MEAS_W        = 132f;
        private const float MARGIN_LEFT   = 54f;
        private const float MARGIN_BOTTOM = 22f;
        private const float MARGIN_TOP    = 4f;
        private const float PRE_TRIG      = 0.20f;   // trigger à div 2

        // ── Tables ────────────────────────────────────────────────────────────
        private static readonly double[] TimeDivMs =
        {
            0.01, 0.02, 0.05, 0.1, 0.2, 0.5,
            1.0,  2.0,  5.0, 10.0, 20.0, 50.0,
            100.0, 200.0, 500.0, 1000.0, 2000.0, 5000.0
        };
        private static readonly float[] ZoomY = { 1f, 2f, 5f, 10f, 20f };
        private float CurrentZoom =>
            ZoomY[Math.Clamp(cmbVdiv.SelectedIndex, 0, ZoomY.Length - 1)];

        // ── État d'exécution ──────────────────────────────────────────────────
        private bool _isRunning;
        private bool _isArmed;      // Normal/Single en attente de trigger

        // ── Données affichées (fenêtre active) ────────────────────────────────
        private float[]?  _dispL, _dispR;
        private int       _trigPx;
        private bool      _trigFound;
        private MeasResult _measL, _measR;
        private int       _lastSR;
        private string    _infoLine = "";

        // ── Buffer complet pour navigation post-stop ──────────────────────────
        private float[]? _capL, _capR;    // buffer complet (searchSamples)
        private int      _capSR;
        private int      _capTrigIdx;     // position du trigger dans _capL/R
        private int      _panOffset;      // décalage de pan en samples

        private readonly System.Windows.Forms.Timer _timer;

        private struct MeasResult
        {
            public bool Valid;
            public double Vpp, VrmsAc, Vdc, FreqHz;
        }

        // ─────────────────────────────────────────────────────────────────────
        public OscilloscopeControl()
        {
            InitializeComponent();

            _timer = new System.Windows.Forms.Timer { Interval = 33 };
            _timer.Tick += Timer_Tick;

            btnRun.Click += BtnRun_Click;

            // Timebase : recalcul de la vue quand arrêté
            cmbTimediv.SelectedIndexChanged += (_, _) =>
            {
                if (_isRunning) pnlScope.Invalidate();
                else            RefreshStoppedView();
            };

            cmbChannel.SelectedIndexChanged  += (_, _) => pnlScope.Invalidate();
            cmbTrigMode.SelectedIndexChanged += (_, _) => { };
            cmbTrigEdge.SelectedIndexChanged += (_, _) => { };
            cmbTrigSrc.SelectedIndexChanged  += (_, _) => { };
            nudTrigLevel.ValueChanged        += (_, _) => pnlScope.Invalidate();
            cmbVdiv.SelectedIndexChanged     += (_, _) => pnlScope.Invalidate();

            pnlScope.Paint      += PnlScope_Paint;
            pnlScope.Resize     += (_, _) => pnlScope.Invalidate();

            // Pan à la molette quand arrêté
            pnlScope.MouseEnter += (_, _) => { if (!_isRunning) pnlScope.Focus(); };
            pnlScope.MouseWheel += PnlScope_MouseWheel;

            SetRunState(false);
        }

        // ─────────────────────────────────────────────────────────────────────
        public void ApplySpectrumColorSettings(SpectrumColorSettings? colors)
        {
            _colors = colors ?? SpectrumColorSettings.Defaults();
            _colors.Normalize();
            pnlScope.Invalidate();
        }

        public void UpdateSampleRate(int sr) { _lastSR = sr; UpdateInfoLine(); }

        // ─────────────────────────────────────────────────────────────────────
        private bool IsAutoMode   => cmbTrigMode.SelectedIndex == 0;
        private bool IsNormalMode => cmbTrigMode.SelectedIndex == 1;
        private bool IsSingleMode => cmbTrigMode.SelectedIndex == 2;

        private void BtnRun_Click(object? s, EventArgs e)
        {
            if (_isRunning) StopOscillo();
            else            StartOscillo();
        }

        private void StartOscillo()
        {
            _panOffset = 0;
            RunRequested?.Invoke();
            SetRunState(true);
            SetArmedState(false);
            _timer.Start();
        }

        private void StopOscillo()
        {
            _timer.Stop();
            SetRunState(false);
            RunStateChanged?.Invoke(false);
        }

        private void SetRunState(bool on)
        {
            _isRunning       = on;
            btnRun.BackColor = on ? Color.Firebrick : Color.SeaGreen;
            btnRun.ForeColor = Color.White;
            btnRun.Text      = on ? "■  STOP" : "▶  RUN";
            if (on) RunStateChanged?.Invoke(true);
        }

        /// <summary>
        /// Passe le bouton en orange "ARMED" quand on attend le trigger
        /// (Normal / Single), ou repasse en rouge "STOP" quand on tourne normalement.
        /// </summary>
        private void SetArmedState(bool armed)
        {
            if (!_isRunning) return;
            _isArmed = armed;
            if (armed)
            {
                btnRun.BackColor = Color.DarkOrange;
                btnRun.Text      = "◉  ARMED";
            }
            else
            {
                btnRun.BackColor = Color.Firebrick;
                btnRun.Text      = "■  STOP";
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Molette : pan quand arrêté
        // ─────────────────────────────────────────────────────────────────────
        private void PnlScope_MouseWheel(object? s, MouseEventArgs e)
        {
            if (_isRunning || _capL == null || _capSR <= 0) return;

            int tdIdx = Math.Clamp(cmbTimediv.SelectedIndex, 0, TimeDivMs.Length - 1);
            int samplesPerDiv = Math.Max(1, (int)(TimeDivMs[tdIdx] / 1000.0 * _capSR));

            // Molette avant = vers la gauche (temps plus ancien)
            _panOffset += e.Delta > 0 ? -samplesPerDiv : samplesPerDiv;
            RefreshStoppedView();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Re-découpe le buffer capturé avec la base de temps et le pan courants
        // ─────────────────────────────────────────────────────────────────────
        private void RefreshStoppedView()
        {
            if (_capL == null || _capR == null || _capSR <= 0) return;

            int tdIdx         = Math.Clamp(cmbTimediv.SelectedIndex, 0, TimeDivMs.Length - 1);
            int windowSamples = Math.Max(8, (int)(TimeDivMs[tdIdx] * H_DIVS / 1000.0 * _capSR));
            int preTrigSamp   = (int)(windowSamples * PRE_TRIG);

            int trigRef      = _capTrigIdx >= 0 ? _capTrigIdx : preTrigSamp;
            int displayStart = trigRef - preTrigSamp + _panOffset;

            int bufLen = _capL.Length;
            displayStart = Math.Clamp(displayStart, 0, Math.Max(0, bufLen - windowSamples));
            windowSamples = Math.Min(windowSamples, bufLen - displayStart);
            if (windowSamples < 2) return;

            _dispL = new float[windowSamples];
            _dispR = new float[windowSamples];
            Array.Copy(_capL, displayStart, _dispL, 0, windowSamples);
            Array.Copy(_capR, displayStart, _dispR, 0, windowSamples);

            int relTrig = trigRef - displayStart;
            _trigPx    = (relTrig >= 0 && relTrig < windowSamples) ? relTrig : -1;
            _trigFound = _trigPx >= 0;

            _measL = ComputeMeasurements(_dispL, _capSR);
            _measR = ComputeMeasurements(_dispR, _capSR);
            _lastSR = _capSR;
            UpdateInfoLine();
            pnlScope.Invalidate();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Timer tick
        // ─────────────────────────────────────────────────────────────────────
        private void Timer_Tick(object? s, EventArgs e)
        {
            var ring = GetRing?.Invoke();
            int sr   = GetSampleRate?.Invoke() ?? _lastSR;
            if (ring == null || sr <= 0) { pnlScope.Invalidate(); return; }
            _lastSR = sr;

            int tdIdx         = Math.Clamp(cmbTimediv.SelectedIndex, 0, TimeDivMs.Length - 1);
            int windowSamples = Math.Max(8, (int)(TimeDivMs[tdIdx] * H_DIVS / 1000.0 * sr));
            int preTrigSamp   = (int)(windowSamples * PRE_TRIG);
            int searchSamples = Math.Min(windowSamples * 3 + preTrigSamp + 4, ring.Capacity - 1);

            long absEnd   = ring.AbsWrite;
            long absStart = absEnd - searchSamples;
            if (absStart < 0) { pnlScope.Invalidate(); return; }

            var rawL = new float[searchSamples];
            var rawR = new float[searchSamples];

            if (!ring.TryReadWindow(absStart, searchSamples, rawL, rawR))
            { pnlScope.Invalidate(); return; }

            var   trigSrc = (cmbTrigSrc.SelectedIndex == 1) ? rawR : rawL;
            float tLvl    = (float)nudTrigLevel.Value;
            bool  rising  = cmbTrigEdge.SelectedIndex == 0;

            float vmax = float.MinValue, vmin = float.MaxValue;
            for (int i = 0; i < searchSamples; i++)
            { if (trigSrc[i] > vmax) vmax = trigSrc[i]; if (trigSrc[i] < vmin) vmin = trigSrc[i]; }
            float hyst = Math.Max(0.002f, (vmax - vmin) * 0.05f);

            bool trigOk = FindTrigger(trigSrc, searchSamples, tLvl, rising, hyst, out int trigIdx);

            int displayStart;
            if (trigOk)
            {
                displayStart = trigIdx - preTrigSamp;
                _trigFound   = true;
                SetArmedState(false);
            }
            else if (IsAutoMode)
            {
                displayStart = searchSamples - windowSamples;
                _trigFound   = false;
                SetArmedState(false);
            }
            else
            {
                // Normal / Single : armé, on attend
                SetArmedState(true);
                pnlScope.Invalidate();
                return;
            }

            displayStart = Math.Max(0, Math.Min(displayStart, searchSamples - windowSamples));
            if (displayStart + windowSamples > searchSamples) { pnlScope.Invalidate(); return; }

            // ── Stocker le buffer complet pour navigation post-stop ───────────
            _capL        = rawL;
            _capR        = rawR;
            _capSR       = sr;
            _capTrigIdx  = trigOk ? trigIdx : -1;

            // ── Extraire la fenêtre d'affichage ───────────────────────────────
            _dispL = new float[windowSamples];
            _dispR = new float[windowSamples];
            Array.Copy(rawL, displayStart, _dispL, 0, windowSamples);
            Array.Copy(rawR, displayStart, _dispR, 0, windowSamples);

            _trigPx = trigOk ? (trigIdx - displayStart) : preTrigSamp;
            _measL  = ComputeMeasurements(_dispL, sr);
            _measR  = ComputeMeasurements(_dispR, sr);
            UpdateInfoLine();

            // ── Mode Single : stopper après capture ───────────────────────────
            if (IsSingleMode && trigOk)
            {
                pnlScope.Invalidate();
                StopOscillo();
                return;
            }

            pnlScope.Invalidate();
        }

        // ─────────────────────────────────────────────────────────────────────
        private static bool FindTrigger(Span<float> src, int len, float level,
            bool rising, float hyst, out int idx)
        {
            idx = -1;
            float arm = rising ? level - hyst : level + hyst;
            bool armed = false;
            for (int i = 1; i < len - 1; i++)
            {
                if (!armed) armed = rising ? src[i] <= arm : src[i] >= arm;
                if (armed)
                {
                    bool cross = rising
                        ? (src[i] < level && src[i+1] >= level)
                        : (src[i] > level && src[i+1] <= level);
                    if (cross) { idx = i; return true; }
                }
            }
            return false;
        }

        private static MeasResult ComputeMeasurements(float[] s, int sr)
        {
            if (s.Length < 4) return default;
            double sum = 0, sumSq = 0;
            float vmax = s[0], vmin = s[0];
            for (int i = 0; i < s.Length; i++)
            { sum += s[i]; if (s[i]>vmax) vmax=s[i]; if (s[i]<vmin) vmin=s[i]; }
            double vdc = sum / s.Length;
            for (int i = 0; i < s.Length; i++) { double ac=s[i]-vdc; sumSq+=ac*ac; }
            return new MeasResult
            {
                Valid  = true,
                Vpp    = vmax - vmin,
                VrmsAc = Math.Sqrt(sumSq / s.Length),
                Vdc    = vdc,
                FreqHz = DetectFrequency(s, sr, vdc, vmax - vmin)
            };
        }

        private static double DetectFrequency(float[] s, int sr, double vdc, double vpp)
        {
            if (vpp < 0.005f) return 0;
            float hyst = (float)(vpp*0.05), arm = (float)(vdc-hyst), trig = (float)(vdc+hyst);
            bool armed = false; int prev = -1, count = 0; long sumI = 0;
            for (int i = 1; i < s.Length; i++)
            {
                if (!armed && s[i] <= arm) armed = true;
                if (armed && s[i-1] < trig && s[i] >= trig)
                { if (prev>=0) { sumI+=i-prev; count++; } prev=i; armed=false; }
            }
            if (count < 1) return 0;
            double per = (double)sumI / count;
            return per > 0 ? sr / per : 0;
        }

        private void UpdateInfoLine()
        {
            int tdIdx = Math.Clamp(cmbTimediv.SelectedIndex, 0, TimeDivMs.Length-1);
            double td = TimeDivMs[tdIdx];
            string srStr  = _lastSR > 0 ? $"{_lastSR/1000.0:G3} kHz" : "—";
            string chStr  = cmbChannel.SelectedIndex switch { 0=>"Left", 1=>"Right", _=>"L+R" };
            float  zoom   = CurrentZoom;
            string zStr   = zoom > 1f ? $"   ×{zoom:G0}" : "";
            string panStr = (!_isRunning && _panOffset != 0 && _capSR > 0)
                ? $"   pan {FormatTime((double)_panOffset / _capSR * 1000.0)}"
                : "";
            _infoLine = $"Channel:{chStr}   SR:{srStr}   {FormatTimediv(td)}   Window:{FormatTime(td*H_DIVS)}{zStr}{panStr}";
        }

        private static string FormatTime(double ms)
        {
            if (ms == 0) return "0";
            double a = Math.Abs(ms);
            if (a >= 1000) return $"{ms/1000:G3} s";
            if (a >= 1)    return $"{ms:G3} ms";
            return $"{ms*1000:G3} µs";
        }

        private static string FormatTimediv(double ms)
        {
            if (ms >= 1000) return $"{ms/1000:G3} s/div";
            if (ms >= 1)    return $"{ms:G3} ms/div";
            return $"{ms*1000:G3} µs/div";
        }

        // ─────────────────────────────────────────────────────────────────────
        // Rendu GDI+
        // ─────────────────────────────────────────────────────────────────────
        private void PnlScope_Paint(object? sender, PaintEventArgs e)
        {
            var g  = e.Graphics;
            var sz = pnlScope.ClientSize;
            if (sz.Width < 40 || sz.Height < 40) return;

            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var wave = new RectangleF(MARGIN_LEFT, MARGIN_TOP,
                sz.Width - MARGIN_LEFT - MEAS_W, sz.Height - MARGIN_TOP - MARGIN_BOTTOM);
            if (wave.Width < 20 || wave.Height < 20) return;

            using (var br = new SolidBrush(BgColor)) g.FillRectangle(br, 0, 0, sz.Width, sz.Height);
            DrawGrid(g, wave);
            DrawAxisLabels(g, wave);

            int ch = cmbChannel.SelectedIndex;
            if (_dispL != null && (ch == 0 || ch == 2)) DrawWaveform(g, wave, _dispL, TraceColorL, _colors.FftWidth);
            if (_dispR != null && (ch == 1 || ch == 2)) DrawWaveform(g, wave, _dispR, TraceColorR, _colors.FftWidth);
            if (_dispL != null || _dispR != null)
                DrawTriggerMarkers(g, wave, _trigPx, (_dispL ?? _dispR)!.Length);

            DrawMeasurements(g, wave);
        }

        private void DrawGrid(Graphics g, RectangleF w)
        {
            using (var br = new SolidBrush(BgColor)) g.FillRectangle(br, w);
            using var penMaj = new Pen(GridMajColor, 1f);
            using var penMin = new Pen(GridMinColor, 1f) { DashStyle = DashStyle.Dot };
            for (int i = 0; i <= H_DIVS; i++)
            {
                float x = w.Left + i * w.Width / H_DIVS;
                g.DrawLine(i==0||i==H_DIVS?penMaj:penMin, x, w.Top, x, w.Bottom);
                if (i < H_DIVS) g.DrawLine(penMin, w.Left+(i+.5f)*w.Width/H_DIVS, w.Top, w.Left+(i+.5f)*w.Width/H_DIVS, w.Bottom);
            }
            for (int i = 0; i <= V_DIVS; i++)
            {
                float y = w.Top + i * w.Height / V_DIVS;
                g.DrawLine(i==0||i==V_DIVS?penMaj:penMin, w.Left, y, w.Right, y);
                if (i < V_DIVS) g.DrawLine(penMin, w.Left, w.Top+(i+.5f)*w.Height/V_DIVS, w.Right, w.Top+(i+.5f)*w.Height/V_DIVS);
            }
            using var penZ = new Pen(Color.FromArgb(100, GridMajColor), 1.2f);
            g.DrawLine(penZ, w.Left, w.Top+w.Height/2f, w.Right, w.Top+w.Height/2f);
            g.DrawLine(penZ, w.Left+PRE_TRIG*w.Width, w.Top, w.Left+PRE_TRIG*w.Width, w.Bottom);
            g.DrawRectangle(penMaj, w.Left, w.Top, w.Width, w.Height);
        }

        private void DrawAxisLabels(Graphics g, RectangleF w)
        {
            using var font = new Font("Segoe UI", 7.5f);
            using var br   = new SolidBrush(LabelColor);
            int    tdIdx = Math.Clamp(cmbTimediv.SelectedIndex, 0, TimeDivMs.Length-1);
            double td    = TimeDivMs[tdIdx];
            float  zoom  = CurrentZoom;

            // X : t=0 au trigger (div PRE_TRIG × H_DIVS = div 2)
            for (int i = 0; i <= H_DIVS; i += 2)
            {
                float  x   = w.Left + i * w.Width / H_DIVS;
                double tMs = (i - PRE_TRIG * H_DIVS) * td;
                string lbl = Math.Abs(tMs) < td * 0.001 ? "0" : FormatTime(tMs);
                var sf = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString(lbl, font, br, new RectangleF(x-32, w.Bottom+2, 64, MARGIN_BOTTOM-2), sf);
            }

            // Y : ±1/zoom aux extrêmes, labels tous les 2 divisions
            for (int i = 0; i <= V_DIVS; i += 2)
            {
                float  y   = w.Top + i * w.Height / V_DIVS;
                double val = (1.0/zoom)*(1.0 - 2.0*i/V_DIVS);
                string lbl = Math.Abs(val) < 1e-9 ? "0" : $"{val:+0.00;-0.00}";
                var sf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
                g.DrawString(lbl, font, br, new RectangleF(0, y-10, MARGIN_LEFT-3, 20), sf);
            }

            if (!string.IsNullOrEmpty(_infoLine))
            {
                using var brI = new SolidBrush(InfoTextColor);
                g.DrawString(_infoLine, font, brI, new PointF(w.Left+4, 1));
            }
        }

        private void DrawWaveform(Graphics g, RectangleF w, float[] samples, Color color, int lw)
        {
            int dispW = (int)w.Width;
            if (dispW < 2 || samples.Length < 2) return;
            float yMid = w.Top + w.Height/2f, yScale = w.Height/2f * CurrentZoom;
            var pts = new PointF[dispW];
            for (int px = 0; px < dispW; px++)
            {
                float t = (float)px / dispW * (samples.Length-1);
                int   lo = (int)t, hi = Math.Min(lo+1, samples.Length-1);
                float v  = samples[lo]*(1f-(t-lo)) + samples[hi]*(t-lo);
                pts[px]  = new PointF(w.Left+px, Math.Clamp(yMid-v*yScale, w.Top, w.Bottom));
            }
            using var pen = new Pen(color, Math.Max(1, lw));
            g.DrawLines(pen, pts);
        }

        private void DrawTriggerMarkers(Graphics g, RectangleF w, int trigIdx, int winSamples)
        {
            float yMid = w.Top+w.Height/2f, yScale = w.Height/2f*CurrentZoom;
            float yTrig = Math.Clamp(yMid-(float)nudTrigLevel.Value*yScale, w.Top, w.Bottom);
            int   src  = cmbTrigSrc.SelectedIndex;
            Color tCol = Color.FromArgb(180, src==0 ? TraceColorL : TraceColorR);
            using var penH = new Pen(tCol, 1f) { DashStyle = DashStyle.Dash };
            g.DrawLine(penH, w.Left, yTrig, w.Right, yTrig);
            if (_trigFound && trigIdx >= 0 && trigIdx < winSamples)
            {
                float xT = w.Left + (float)trigIdx/winSamples*w.Width;
                using var penV = new Pen(tCol, 1f) { DashStyle = DashStyle.Dash };
                g.DrawLine(penV, xT, w.Top, xT, w.Bottom);
                float ay = w.Top+4;
                using var brA = new SolidBrush(tCol);
                bool rising = cmbTrigEdge.SelectedIndex == 0;
                g.FillPolygon(brA, rising
                    ? new[]{ new PointF(xT,ay+8), new PointF(xT-4,ay), new PointF(xT+4,ay) }
                    : new[]{ new PointF(xT,ay),   new PointF(xT-4,ay+8), new PointF(xT+4,ay+8) });
            }
        }

        private void DrawMeasurements(Graphics g, RectangleF w)
        {
            var r = new RectangleF(w.Right, w.Top, MEAS_W-4, w.Height);
            using (var br = new SolidBrush(Color.FromArgb(200,
                       Math.Max(0,BgColor.R-8), Math.Max(0,BgColor.G-8), Math.Max(0,BgColor.B-8))))
                g.FillRectangle(br, r);
            using (var pen = new Pen(GridMajColor, 1f)) g.DrawRectangle(pen, r.Left, r.Top, r.Width, r.Height);

            float y = r.Top+6;
            int ch = cmbChannel.SelectedIndex;
            bool calOk = false; double offL=0, offR=0;
            if (GetInputCalibration != null)
            { var (vL,oL,_,oR)=GetInputCalibration(); calOk=vL; offL=oL; offR=oR; }

            if (ch==0||ch==2) y = DrawMeasSection(g,"LEFT",  TraceColorL, _measL, r, y, calOk, offL);
            if (ch==1||ch==2) y = DrawMeasSection(g,"RIGHT", TraceColorR, _measR, r, y, calOk, offR);

            if (!_isRunning && _capL != null)
            {
                DrawSmallLabel(g, "🖱 scroll to pan", Color.Gray, r, ref y);
                if (_panOffset != 0)
                    DrawSmallLabel(g, "← →", Color.DodgerBlue, r, ref y);
            }
            else if (_isArmed)
                DrawSmallLabel(g, "ARMED", Color.DarkOrange, r, ref y);
            else if (!_isRunning)
                DrawSmallLabel(g, "STOPPED", Color.Gray, r, ref y);
        }

        private float DrawMeasSection(Graphics g, string title, Color color,
            MeasResult m, RectangleF r, float y, bool cal, double offDb)
        {
            using var fT=new Font("Segoe UI",8.5f,FontStyle.Bold); using var fV=new Font("Segoe UI",8f);
            using var brT=new SolidBrush(color); using var brV=new SolidBrush(LabelColor); using var brG=new SolidBrush(Color.Gray);
            float x = r.Left+6;
            g.DrawString(title,fT,brT,x,y); y+=16;
            if (!m.Valid||!_isRunning&&_dispL==null)
            { foreach(var l in new[]{"Vrms  ---","Vpp   ---","Vdc   ---","Freq  ---","Per   ---"}) { g.DrawString(l,fV,brG,x,y); y+=14; } return y+4; }
            double sc = cal ? Math.Pow(10,offDb/20) : 1.0; string u = cal?"V":"";
            string Fv(double v) => cal ? FormatVoltage(v*sc) : $"{v:F4}";
            g.DrawString($"Vrms  {Fv(m.VrmsAc)}{u}",fV,brV,x,y); y+=14;
            g.DrawString($"Vpp   {Fv(m.Vpp)}{u}",   fV,brV,x,y); y+=14;
            g.DrawString($"Vdc   {Fv(m.Vdc)}{u}",   fV,brV,x,y); y+=14;
            if (m.FreqHz>0)
            {
                string fStr = m.FreqHz>=1000 ? $"{m.FreqHz/1000:G4} kHz" : $"{m.FreqHz:G4} Hz";
                g.DrawString($"Freq  {fStr}",              fV,brV,x,y); y+=14;
                g.DrawString($"Per   {1000/m.FreqHz:G4} ms",fV,brV,x,y); y+=14;
            }
            else { g.DrawString("Freq  ---",fV,brG,x,y); y+=14; g.DrawString("Per   ---",fV,brG,x,y); y+=14; }
            return y+4;
        }

        private static string FormatVoltage(double v)
        { double a=Math.Abs(v); return a>=10?$"{v:F2}":a>=1?$"{v:F3}":a>=0.1?$"{v:F4}":$"{v:G4}"; }

        private static void DrawSmallLabel(Graphics g, string text, Color c, RectangleF r, ref float y)
        { using var f=new Font("Segoe UI",7.5f,FontStyle.Italic); using var br=new SolidBrush(c); g.DrawString(text,f,br,r.Left+6,y); y+=14; }

        // ─────────────────────────────────────────────────────────────────────
        public OscilloscopeSettings GetSettingsSnapshot() => new OscilloscopeSettings
        {
            ChannelIndex=cmbChannel.SelectedIndex, TimeDivIndex=cmbTimediv.SelectedIndex,
            TrigModeIndex=cmbTrigMode.SelectedIndex, TrigEdgeIndex=cmbTrigEdge.SelectedIndex,
            TrigSrcIndex=cmbTrigSrc.SelectedIndex, TrigLevel=(double)nudTrigLevel.Value,
            ZoomYIndex=cmbVdiv.SelectedIndex
        };

        public void ApplySettings(OscilloscopeSettings? s)
        {
            if (s==null) return;
            cmbChannel.SelectedIndex  = Math.Clamp(s.ChannelIndex,  0,2);
            cmbTimediv.SelectedIndex  = Math.Clamp(s.TimeDivIndex,  0,TimeDivMs.Length-1);
            cmbTrigMode.SelectedIndex = Math.Clamp(s.TrigModeIndex, 0,2);
            cmbTrigEdge.SelectedIndex = Math.Clamp(s.TrigEdgeIndex, 0,1);
            cmbTrigSrc.SelectedIndex  = Math.Clamp(s.TrigSrcIndex,  0,1);
            nudTrigLevel.Value        = (decimal)Math.Clamp(s.TrigLevel,-1.0,1.0);
            cmbVdiv.SelectedIndex     = Math.Clamp(s.ZoomYIndex,    0,ZoomY.Length-1);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        { _timer.Stop(); _timer.Dispose(); base.OnHandleDestroyed(e); }
    }

    // ── Panel double-buffered ─────────────────────────────────────────────────
    internal sealed class ScopePanel : Panel
    {
        public ScopePanel()
        {
            DoubleBuffered = true;
            TabStop = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint, true);
        }
    }

    // ── Session settings ──────────────────────────────────────────────────────
    public sealed class OscilloscopeSettings
    {
        public int    ChannelIndex  { get; set; } = 2;
        public int    TimeDivIndex  { get; set; } = 6;
        public int    TrigModeIndex { get; set; } = 0;
        public int    TrigEdgeIndex { get; set; } = 0;
        public int    TrigSrcIndex  { get; set; } = 0;
        public double TrigLevel     { get; set; } = 0.0;
        public int    ZoomYIndex    { get; set; } = 0;
    }
}
