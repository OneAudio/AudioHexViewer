using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AudioHexViewer.Core;

namespace AudioHexViewer
{
    internal partial class OscilloscopeControl : UserControl
    {
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Func<RingBufferStereoFloat?>? GetRing       { get; set; }
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Func<int>?                    GetSampleRate { get; set; }
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Func<(bool vL, double oL, bool vR, double oR)>? GetInputCalibration { get; set; }

        public event Action<bool>? RunStateChanged;
        public event Action?       RunRequested;

        private SpectrumColorSettings _colors = SpectrumColorSettings.Defaults();
        private Color BgColor       => Color.FromArgb(_colors.BackgroundArgb);
        private Color GridMajColor  => Color.FromArgb(_colors.GridArgb);
        private Color GridMinColor  => Color.FromArgb(_colors.GridMinorArgb);
        private Color TraceColorL   => Color.FromArgb(_colors.FftLeftArgb);
        private Color TraceColorR   => Color.FromArgb(_colors.FftRightArgb);
        private Color LabelColor    => Color.FromArgb(_colors.LabelMajorArgb);
        private Color InfoTextColor => Color.FromArgb(_colors.InfoTextArgb);

        private const int   H_DIVS        = 10;
        private const int   V_DIVS        = 8;
        private const float MEAS_W        = 132f;
        private const float MARGIN_LEFT   = 54f;
        private const float MARGIN_BOTTOM = 22f;
        private const float MARGIN_TOP    = 4f;
        private const float PRE_TRIG      = 0.20f;

        private static readonly double[] TimeDivMs =
        {
            0.01, 0.02, 0.05, 0.1, 0.2, 0.5,
            1.0,  2.0,  5.0, 10.0, 20.0, 50.0,
            100.0, 200.0, 500.0, 1000.0, 2000.0, 5000.0
        };
        // Roll mode disponible pour timebase >= INDEX_ROLL_MIN (10 ms/div)
        private const int INDEX_ROLL_MIN = 9;

        private static readonly float[] ZoomY = { 1f, 2f, 5f, 10f, 20f, 50f, 100f, 200f, 500f, 1000f };

        // ── AC coupling : EMA persistante entre les ticks ─────────────────────
        // On ne recalcule PAS la moyenne à chaque tick depuis le buffer courant
        // (variance → oscillation visible). On filtre lentement vers la vraie DC.
        private float _dcEstL, _dcEstR;   // estimation DC courante (EMA adaptative)

        // ── WAIT / TRIG'D indicator ───────────────────────────────────────────
        private int  _blinkCounter;
        private bool _blinkState;
        private bool _lastTrigOk;
        private float CurrentZoom =>
            ZoomY[Math.Clamp(cmbVdiv.SelectedIndex, 0, ZoomY.Length - 1)];

        private bool IsAcCoupling => cmbCoupling.SelectedIndex == 1;
        private bool IsRollMode   => chkRoll.Checked && chkRoll.Enabled;

        private bool _isRunning;
        private bool _isArmed;

        private float[]?   _dispL, _dispR;
        private int        _trigPx;
        private bool       _trigFound;
        private MeasResult _measL, _measR;
        private int        _lastSR;
        private string     _infoLine = "";

        // Buffer complet pour navigation post-stop
        private float[]? _capL, _capR;
        private int      _capSR;
        private int      _capTrigIdx;
        private int      _panOffset;

        private readonly System.Windows.Forms.Timer _timer;

        private struct MeasResult
        {
            public bool Valid;
            public double Vpp, VrmsAc, Vdc, FreqHz;
        }

        public OscilloscopeControl()
        {
            InitializeComponent();

            _timer = new System.Windows.Forms.Timer { Interval = 33 };
            _timer.Tick += Timer_Tick;

            btnRun.Click += BtnRun_Click;

            // ── Owner-draw du bouton RUN ──────────────────────────────────────
            // FlatStyle.Flat avec BackColor personnalisé bascule vers le rendu natif
            // Windows (WM_PAINT non-managé) quand un ComboBox dropdown s'ouvre,
            // affichant alors uniquement l'indicateur de focus (carré blanc) sans texte.
            // On court-circuite complètement le rendu natif via l'événement Paint.
            btnRun.Paint += (s, e) =>
            {
                var btn = (Button)s!;
                e.Graphics.Clear(btn.BackColor);
                TextRenderer.DrawText(
                    e.Graphics, btn.Text, btn.Font, btn.ClientRectangle,
                    btn.ForeColor,
                    System.Windows.Forms.TextFormatFlags.HorizontalCenter |
                    System.Windows.Forms.TextFormatFlags.VerticalCenter   |
                    System.Windows.Forms.TextFormatFlags.SingleLine);
            };

            cmbTimediv.SelectedIndexChanged += (_, _) =>
            {
                UpdateRollCheckbox();
                if (_isRunning) pnlScope.Invalidate();
                else            RefreshStoppedView();
            };

            cmbChannel.SelectedIndexChanged  += (_, _) => pnlScope.Invalidate();
            cmbCoupling.SelectedIndexChanged += (_, _) =>
            { if (!_isRunning) RefreshStoppedView(); else pnlScope.Invalidate(); };
            cmbTrigMode.SelectedIndexChanged += (_, _) => { };
            cmbTrigEdge.SelectedIndexChanged += (_, _) => { };
            cmbTrigSrc.SelectedIndexChanged  += (_, _) => { };
            nudTrigLevel.ValueChanged        += (_, _) => pnlScope.Invalidate();
            cmbVdiv.SelectedIndexChanged     += (_, _) => pnlScope.Invalidate();
            chkRoll.CheckedChanged           += (_, _) => { };

            pnlScope.Paint      += PnlScope_Paint;
            pnlScope.Resize     += (_, _) => pnlScope.Invalidate();
            pnlScope.MouseEnter += (_, _) => { if (!_isRunning) pnlScope.Focus(); };
            pnlScope.MouseWheel += PnlScope_MouseWheel;

            UpdateRollCheckbox();
            SetRunState(false);
        }

        public void ApplySpectrumColorSettings(SpectrumColorSettings? colors)
        {
            _colors = colors ?? SpectrumColorSettings.Defaults();
            _colors.Normalize();
            pnlScope.Invalidate();
        }

        public void UpdateSampleRate(int sr) { _lastSR = sr; UpdateInfoLine(); }

        // ── Mode trigger ──────────────────────────────────────────────────────
        private bool IsAutoMode   => cmbTrigMode.SelectedIndex == 0;
        private bool IsNormalMode => cmbTrigMode.SelectedIndex == 1;
        private bool IsSingleMode => cmbTrigMode.SelectedIndex == 2;

        private void UpdateRollCheckbox()
        {
            bool canRoll = cmbTimediv.SelectedIndex >= INDEX_ROLL_MIN;
            chkRoll.Enabled = canRoll;
            if (!canRoll) chkRoll.Checked = false;
        }

        // ── Run / Stop ────────────────────────────────────────────────────────
        private void BtnRun_Click(object? s, EventArgs e)
        {
            if (_isRunning) StopOscillo();
            else            StartOscillo();
        }

        private void StartOscillo()
        {
            _panOffset    = 0;
            _dcEstL       = 0f;
            _dcEstR       = 0f;
            _blinkCounter = 0;
            _blinkState   = false;
            _lastTrigOk   = false;
            RunRequested?.Invoke();
            SetRunState(true);
            SetArmedState(false);
            _timer.Start();
        }

        private void StopOscillo()
        {
            _timer.Stop();
            SetRunState(false);
            lblTrigStatus.Text = "";
            RunStateChanged?.Invoke(false);
        }

        private void SetRunState(bool on)
        {
            _isRunning = on;
            // Toutes les propriétés visuelles explicites pour éviter les artefacts
            // lors des repaints déclenchés par les combos enfants du panelTop.
            btnRun.BackColor  = on ? Color.Firebrick : Color.SeaGreen;
            btnRun.ForeColor  = Color.White;
            btnRun.Text       = on ? "■  STOP" : "▶  RUN";
            btnRun.TextAlign  = System.Drawing.ContentAlignment.MiddleCenter;
            btnRun.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Empty;
            btnRun.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Empty;
            if (on) RunStateChanged?.Invoke(true);
        }

        private void SetArmedState(bool armed)
        {
            if (!_isRunning) return;
            _isArmed = armed;
            // Bouton uniquement rouge ou vert — pas d'orange
            btnRun.BackColor  = Color.Firebrick;
            btnRun.ForeColor  = Color.White;
            btnRun.Text       = "■  STOP";
            btnRun.TextAlign  = System.Drawing.ContentAlignment.MiddleCenter;
            btnRun.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Empty;
            btnRun.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Empty;
            UpdateTrigStatus();
        }

        private void UpdateTrigStatus()
        {
            if (!_isRunning)
            {
                lblTrigStatus.Text      = "";
                lblTrigStatus.ForeColor = Color.Gray;
                return;
            }
            if (_isArmed)
            {
                // Clignotement : change toutes les 15 ticks (~0.5 s à 30 Hz)
                _blinkCounter++;
                if (_blinkCounter >= 15) { _blinkCounter = 0; _blinkState = !_blinkState; }
                lblTrigStatus.Text      = "WAIT";
                lblTrigStatus.ForeColor = _blinkState ? Color.Lime : Color.FromArgb(0, 80, 0);
            }
            else if (_lastTrigOk)
            {
                lblTrigStatus.Text      = "TRIG'D";
                lblTrigStatus.ForeColor = Color.OrangeRed;
            }
            else
            {
                lblTrigStatus.Text      = "";
                lblTrigStatus.ForeColor = Color.Gray;
            }
        }

        // ── Pan molette ───────────────────────────────────────────────────────
        private void PnlScope_MouseWheel(object? s, MouseEventArgs e)
        {
            if (_isRunning || _capL == null || _capSR <= 0) return;
            int tdIdx = Math.Clamp(cmbTimediv.SelectedIndex, 0, TimeDivMs.Length - 1);
            int sampPerDiv = Math.Max(1, (int)(TimeDivMs[tdIdx] / 1000.0 * _capSR));
            _panOffset += e.Delta > 0 ? -sampPerDiv : sampPerDiv;
            RefreshStoppedView();
        }

        // ── Navigation post-stop ──────────────────────────────────────────────
        private void RefreshStoppedView()
        {
            if (_capL == null || _capR == null || _capSR <= 0) return;

            int tdIdx         = Math.Clamp(cmbTimediv.SelectedIndex, 0, TimeDivMs.Length - 1);
            int windowSamples = Math.Max(8, (int)(TimeDivMs[tdIdx] * H_DIVS / 1000.0 * _capSR));
            int preTrigSamp   = (int)(windowSamples * PRE_TRIG);
            int trigRef       = _capTrigIdx >= 0 ? _capTrigIdx : preTrigSamp;
            int displayStart  = trigRef - preTrigSamp + _panOffset;

            int bufLen = _capL.Length;
            displayStart  = Math.Clamp(displayStart, 0, Math.Max(0, bufLen - windowSamples));
            windowSamples = Math.Min(windowSamples, bufLen - displayStart);
            if (windowSamples < 2) return;

            float[] sl = new float[windowSamples], sr2 = new float[windowSamples];
            Array.Copy(_capL, displayStart, sl, 0, windowSamples);
            Array.Copy(_capR, displayStart, sr2, 0, windowSamples);

            // Couplage AC : utilise l'EMA stockée (stable, pas de recalcul)
            if (IsAcCoupling)
            {
                for (int i = 0; i < windowSamples; i++) { sl[i] -= _dcEstL; sr2[i] -= _dcEstR; }
            }

            _dispL = sl; _dispR = sr2;
            int relTrig = trigRef - displayStart;
            _trigPx = (relTrig >= 0 && relTrig < windowSamples) ? relTrig : -1;
            _trigFound = _trigPx >= 0;
            _measL = ComputeMeasurements(_dispL, _capSR);
            _measR = ComputeMeasurements(_dispR, _capSR);
            _lastSR = _capSR;
            UpdateInfoLine();
            pnlScope.Invalidate();
        }

        // ── Timer tick ────────────────────────────────────────────────────────
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

            long absEnd = ring.AbsWrite, absStart = absEnd - searchSamples;
            if (absStart < 0) { pnlScope.Invalidate(); return; }

            var rawL = new float[searchSamples];
            var rawR = new float[searchSamples];
            if (!ring.TryReadWindow(absStart, searchSamples, rawL, rawR))
            { pnlScope.Invalidate(); return; }

            // ── Couplage AC : EMA à alpha adaptatif ──────────────────────────
            // α = min(1, searchSamples / (SR × τ))
            //   · petite fenêtre (1ms/div)  → α ≈ 0.1  → EMA lente → pas d'oscillation
            //   · grande fenêtre (50ms/div) → α ≈ 1.0  → moyenne exacte → DC résiduel = 0
            if (IsAcCoupling)
            {
                const float DC_TAU = 0.3f;
                float alpha = Math.Min(1.0f, (float)searchSamples / (sr * DC_TAU));
                float sumL = 0, sumR = 0;
                for (int i = 0; i < searchSamples; i++) { sumL += rawL[i]; sumR += rawR[i]; }
                _dcEstL = _dcEstL * (1f - alpha) + (sumL / searchSamples) * alpha;
                _dcEstR = _dcEstR * (1f - alpha) + (sumR / searchSamples) * alpha;
                for (int i = 0; i < searchSamples; i++) { rawL[i] -= _dcEstL; rawR[i] -= _dcEstR; }
            }

            // ── Mode Roll : affiche toujours les samples les plus récents ─────
            if (IsRollMode)
            {
                SetArmedState(false);
                int displayStart = searchSamples - windowSamples;
                StoreAndDisplay(rawL, rawR, searchSamples, displayStart, windowSamples, sr, -1);
                pnlScope.Invalidate();
                return;
            }

            // ── Recherche du trigger ──────────────────────────────────────────
            var trigSrc  = (cmbTrigSrc.SelectedIndex == 1) ? rawR : rawL;
            float tLvl   = (float)nudTrigLevel.Value;
            bool  rising = cmbTrigEdge.SelectedIndex == 0;

            // Hysteresis = 5% du Vpp
            float vmax = float.MinValue, vmin = float.MaxValue;
            for (int i = 0; i < searchSamples; i++)
            { if (trigSrc[i]>vmax) vmax=trigSrc[i]; if (trigSrc[i]<vmin) vmin=trigSrc[i]; }
            float hyst = Math.Max(0.002f, (vmax - vmin) * 0.05f);

            // ── Scan BACKWARD dans la plage valide ───────────────────────────
            bool trigOk = FindTriggerBackward(trigSrc, searchSamples, tLvl, rising, hyst,
                                              windowSamples, preTrigSamp, out int trigIdx);

            int dispStart;
            if (trigOk)
            {
                dispStart = trigIdx - preTrigSamp;
                _trigFound = true;
                _lastTrigOk = true;
                SetArmedState(false);
            }
            else if (IsAutoMode)
            {
                dispStart = searchSamples - windowSamples;
                _trigFound = false;
                _lastTrigOk = false;
                SetArmedState(false);
            }
            else
            {
                _lastTrigOk = false;
                SetArmedState(true);
                pnlScope.Invalidate();
                return;
            }

            dispStart = Math.Max(0, Math.Min(dispStart, searchSamples - windowSamples));
            if (dispStart + windowSamples > searchSamples) { pnlScope.Invalidate(); return; }

            StoreAndDisplay(rawL, rawR, searchSamples, dispStart, windowSamples, sr,
                            trigOk ? trigIdx : -1);
            UpdateTrigStatus();

            if (IsSingleMode && trigOk)
            { pnlScope.Invalidate(); StopOscillo(); return; }

            pnlScope.Invalidate();
        }

        private void StoreAndDisplay(float[] rawL, float[] rawR, int searchSamples,
            int dispStart, int windowSamples, int sr, int trigIdx)
        {
            _capL       = rawL;
            _capR       = rawR;
            _capSR      = sr;
            _capTrigIdx = trigIdx;

            _dispL = new float[windowSamples];
            _dispR = new float[windowSamples];
            Array.Copy(rawL, dispStart, _dispL, 0, windowSamples);
            Array.Copy(rawR, dispStart, _dispR, 0, windowSamples);

            _trigPx = (trigIdx >= 0) ? trigIdx - dispStart : (int)(windowSamples * PRE_TRIG);
            _measL  = ComputeMeasurements(_dispL, sr);
            _measR  = ComputeMeasurements(_dispR, sr);
            UpdateInfoLine();
        }

        // ── Couplage AC ───────────────────────────────────────────────────────
        private static void ApplyAcCoupling(float[] s)
        {
            if (s.Length == 0) return;
            double mean = 0;
            for (int i = 0; i < s.Length; i++) mean += s[i];
            mean /= s.Length;
            float m = (float)mean;
            for (int i = 0; i < s.Length; i++) s[i] -= m;
        }

        // ── Trigger : scan BACKWARD borné pour un affichage parfaitement stable ──
        /// <summary>
        /// Cherche le crossing LE PLUS RÉCENT dans la plage de validité :
        ///   minI = preTrigSamp        (assez de données avant le trig)
        ///   maxI = len - postSamples  (assez de données après le trig)
        /// Cela garantit que dispStart = trigIdx - preTrigSamp n'est jamais clampé,
        /// donc _trigPx = preTrigSamp en permanence → signal immobile sur l'écran.
        /// </summary>
        private static bool FindTriggerBackward(float[] src, int len, float level,
            bool rising, float hyst, int windowSamples, int preTrigSamp, out int idx)
        {
            idx = -1;
            float armThresh = rising ? level - hyst : level + hyst;

            int postSamples = windowSamples - preTrigSamp;  // samples après le trig
            int maxI = len - postSamples - 1;               // borne haute
            int minI = preTrigSamp + 1;                     // borne basse
            if (maxI <= minI) return false;
            maxI = Math.Min(maxI, len - 2);

            for (int i = maxI; i >= minI; i--)
            {
                bool cross = rising
                    ? (src[i] < level && src[i+1] >= level)
                    : (src[i] > level && src[i+1] <= level);
                if (!cross) continue;

                // Vérifier que le signal était "armé" avant ce crossing
                // (look-back sur 1 windowSamples complet, ou jusqu'à minI)
                int lookbackLimit = Math.Max(0, i - windowSamples);
                for (int j = i - 1; j >= lookbackLimit; j--)
                {
                    if (rising ? src[j] <= armThresh : src[j] >= armThresh)
                    { idx = i; return true; }
                }
            }
            return false;
        }

        private static MeasResult ComputeMeasurements(float[] s, int sr)
        {
            if (s.Length < 4) return default;
            double sum=0, sumSq=0; float vmax=s[0], vmin=s[0];
            for (int i=0; i<s.Length; i++)
            { sum+=s[i]; if(s[i]>vmax)vmax=s[i]; if(s[i]<vmin)vmin=s[i]; }
            double vdc=sum/s.Length;
            for (int i=0; i<s.Length; i++) { double ac=s[i]-vdc; sumSq+=ac*ac; }
            return new MeasResult { Valid=true, Vpp=vmax-vmin,
                VrmsAc=Math.Sqrt(sumSq/s.Length), Vdc=vdc,
                FreqHz=DetectFrequency(s,sr,vdc,vmax-vmin) };
        }

        private static double DetectFrequency(float[] s, int sr, double vdc, double vpp)
        {
            if (vpp<0.005f) return 0;
            float hyst=(float)(vpp*0.05), arm=(float)(vdc-hyst), trig=(float)(vdc+hyst);
            bool armed=false; int prev=-1, count=0; long sumI=0;
            for (int i=1; i<s.Length; i++)
            {
                if (!armed&&s[i]<=arm) armed=true;
                if (armed&&s[i-1]<trig&&s[i]>=trig)
                { if(prev>=0){sumI+=i-prev;count++;} prev=i; armed=false; }
            }
            if (count<1) return 0;
            double per=(double)sumI/count;
            return per>0?sr/per:0;
        }

        private void UpdateInfoLine()
        {
            int tdIdx=Math.Clamp(cmbTimediv.SelectedIndex,0,TimeDivMs.Length-1);
            double td=TimeDivMs[tdIdx];
            string srStr  = _lastSR>0 ? $"{_lastSR/1000.0:G3} kHz" : "—";
            string chStr  = cmbChannel.SelectedIndex switch{0=>"Left",1=>"Right",_=>"L+R"};
            float  zoom   = CurrentZoom;
            string zStr   = zoom>1f ? $"   ×{zoom:G0}" : "";
            string panStr = (!_isRunning&&_panOffset!=0&&_capSR>0)
                ? $"   pan {FormatTime((double)_panOffset/_capSR*1000.0)}" : "";
            string acStr  = IsAcCoupling ? "  AC" : "";
            string rollStr= IsRollMode   ? "  ROLL" : "";
            _infoLine = $"Channel:{chStr}   SR:{srStr}   {FormatTimediv(td)}{acStr}{rollStr}{zStr}{panStr}";
        }

        private static string FormatTime(double ms)
        {
            if (ms==0) return "0";
            double a=Math.Abs(ms);
            if (a>=1000) return $"{ms/1000:G3} s";
            if (a>=1)    return $"{ms:G3} ms";
            return $"{ms*1000:G3} µs";
        }
        private static string FormatTimediv(double ms)
        {
            if (ms>=1000) return $"{ms/1000:G3} s/div";
            if (ms>=1)    return $"{ms:G3} ms/div";
            return $"{ms*1000:G3} µs/div";
        }

        // ── Rendu ─────────────────────────────────────────────────────────────
        private void PnlScope_Paint(object? sender, PaintEventArgs e)
        {
            var g=e.Graphics; var sz=pnlScope.ClientSize;
            if (sz.Width<40||sz.Height<40) return;
            g.SmoothingMode=SmoothingMode.AntiAlias;
            g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            var wave=new RectangleF(MARGIN_LEFT,MARGIN_TOP,sz.Width-MARGIN_LEFT-MEAS_W,sz.Height-MARGIN_TOP-MARGIN_BOTTOM);
            if (wave.Width<20||wave.Height<20) return;
            using(var br=new SolidBrush(BgColor)) g.FillRectangle(br,0,0,sz.Width,sz.Height);
            DrawGrid(g,wave); DrawAxisLabels(g,wave);
            int ch=cmbChannel.SelectedIndex;
            if (_dispL!=null&&(ch==0||ch==2)) DrawWaveform(g,wave,_dispL,TraceColorL,_colors.FftWidth);
            if (_dispR!=null&&(ch==1||ch==2)) DrawWaveform(g,wave,_dispR,TraceColorR,_colors.FftWidth);
            if (!IsRollMode&&(_dispL!=null||_dispR!=null))
                DrawTriggerMarkers(g,wave,_trigPx,(_dispL??_dispR)!.Length);
            DrawMeasurements(g,wave);
        }

        private void DrawGrid(Graphics g, RectangleF w)
        {
            using(var br=new SolidBrush(BgColor)) g.FillRectangle(br,w);
            using var pM=new Pen(GridMajColor,1f); using var pm=new Pen(GridMinColor,1f){DashStyle=DashStyle.Dot};
            for(int i=0;i<=H_DIVS;i++){float x=w.Left+i*w.Width/H_DIVS;g.DrawLine(i==0||i==H_DIVS?pM:pm,x,w.Top,x,w.Bottom);if(i<H_DIVS)g.DrawLine(pm,w.Left+(i+.5f)*w.Width/H_DIVS,w.Top,w.Left+(i+.5f)*w.Width/H_DIVS,w.Bottom);}
            for(int i=0;i<=V_DIVS;i++){float y=w.Top+i*w.Height/V_DIVS;g.DrawLine(i==0||i==V_DIVS?pM:pm,w.Left,y,w.Right,y);if(i<V_DIVS)g.DrawLine(pm,w.Left,w.Top+(i+.5f)*w.Height/V_DIVS,w.Right,w.Top+(i+.5f)*w.Height/V_DIVS);}
            using var pZ=new Pen(Color.FromArgb(100,GridMajColor),1.2f);
            g.DrawLine(pZ,w.Left,w.Top+w.Height/2f,w.Right,w.Top+w.Height/2f);
            if(!IsRollMode) g.DrawLine(pZ,w.Left+PRE_TRIG*w.Width,w.Top,w.Left+PRE_TRIG*w.Width,w.Bottom);
            g.DrawRectangle(pM,w.Left,w.Top,w.Width,w.Height);
        }

        private void DrawAxisLabels(Graphics g, RectangleF w)
        {
            using var font=new Font("Segoe UI",7.5f); using var br=new SolidBrush(LabelColor);
            int    tdIdx=Math.Clamp(cmbTimediv.SelectedIndex,0,TimeDivMs.Length-1);
            double td=TimeDivMs[tdIdx]; float zoom=CurrentZoom;
            for(int i=0;i<=H_DIVS;i+=2)
            {
                float  x=w.Left+i*w.Width/H_DIVS;
                double tMs= IsRollMode ? i*td : (i-PRE_TRIG*H_DIVS)*td;
                string lbl=(!IsRollMode&&Math.Abs(tMs)<td*0.001)?"0":FormatTime(tMs);
                var sf=new StringFormat{Alignment=StringAlignment.Center};
                g.DrawString(lbl,font,br,new RectangleF(x-32,w.Bottom+2,64,MARGIN_BOTTOM-2),sf);
            }
            for(int i=0;i<=V_DIVS;i+=2)
            {
                float  y=w.Top+i*w.Height/V_DIVS;
                double val=(1.0/zoom)*(1.0-2.0*i/V_DIVS);
                string lbl=Math.Abs(val)<1e-9?"0":$"{val:+0.00;-0.00}";
                var sf=new StringFormat{Alignment=StringAlignment.Far,LineAlignment=StringAlignment.Center};
                g.DrawString(lbl,font,br,new RectangleF(0,y-10,MARGIN_LEFT-3,20),sf);
            }
            if(!string.IsNullOrEmpty(_infoLine))
            { using var brI=new SolidBrush(InfoTextColor); g.DrawString(_infoLine,font,brI,new PointF(w.Left+4,1)); }
        }

        private void DrawWaveform(Graphics g, RectangleF w, float[] s, Color color, int lw)
        {
            int dw=(int)w.Width; if(dw<2||s.Length<2) return;
            float ym=w.Top+w.Height/2f, ys=w.Height/2f*CurrentZoom;
            var pts=new PointF[dw];
            for(int px=0;px<dw;px++){float t=(float)px/dw*(s.Length-1);int lo=(int)t,hi=Math.Min(lo+1,s.Length-1);float v=s[lo]*(1f-(t-lo))+s[hi]*(t-lo);pts[px]=new PointF(w.Left+px,Math.Clamp(ym-v*ys,w.Top,w.Bottom));}
            using var pen=new Pen(color,Math.Max(1,lw)); g.DrawLines(pen,pts);
        }

        private void DrawTriggerMarkers(Graphics g, RectangleF w, int trigIdx, int winSamples)
        {
            float ym=w.Top+w.Height/2f, ys=w.Height/2f*CurrentZoom;
            float yT=Math.Clamp(ym-(float)nudTrigLevel.Value*ys,w.Top,w.Bottom);
            int src=cmbTrigSrc.SelectedIndex; Color tCol=Color.FromArgb(180,src==0?TraceColorL:TraceColorR);
            using var pH=new Pen(tCol,1f){DashStyle=DashStyle.Dash}; g.DrawLine(pH,w.Left,yT,w.Right,yT);
            if(_trigFound&&trigIdx>=0&&trigIdx<winSamples)
            {
                float xT=w.Left+(float)trigIdx/winSamples*w.Width;
                using var pV=new Pen(tCol,1f){DashStyle=DashStyle.Dash}; g.DrawLine(pV,xT,w.Top,xT,w.Bottom);
                float ay=w.Top+4; using var brA=new SolidBrush(tCol);
                bool rising=cmbTrigEdge.SelectedIndex==0;
                g.FillPolygon(brA,rising?new[]{new PointF(xT,ay+8),new PointF(xT-4,ay),new PointF(xT+4,ay)}:new[]{new PointF(xT,ay),new PointF(xT-4,ay+8),new PointF(xT+4,ay+8)});
            }
        }

        private void DrawMeasurements(Graphics g, RectangleF w)
        {
            var r=new RectangleF(w.Right,w.Top,MEAS_W-4,w.Height);
            using(var br=new SolidBrush(Color.FromArgb(200,Math.Max(0,BgColor.R-8),Math.Max(0,BgColor.G-8),Math.Max(0,BgColor.B-8)))) g.FillRectangle(br,r);
            using(var pen=new Pen(GridMajColor,1f)) g.DrawRectangle(pen,r.Left,r.Top,r.Width,r.Height);
            float y=r.Top+6; int ch=cmbChannel.SelectedIndex;
            bool calOk=false; double offL=0, offR=0;
            if(GetInputCalibration!=null){var(vL,oL,_,oR)=GetInputCalibration();calOk=vL;offL=oL;offR=oR;}
            if(ch==0||ch==2) y=DrawMeasSection(g,"LEFT", TraceColorL,_measL,r,y,calOk,offL);
            if(ch==1||ch==2) y=DrawMeasSection(g,"RIGHT",TraceColorR,_measR,r,y,calOk,offR);
            if(!_isRunning&&_capL!=null)   DrawSmallLabel(g,"🖱 scroll to pan",Color.Gray,r,ref y);
            else if(_isArmed)               DrawSmallLabel(g,"ARMED",Color.DarkOrange,r,ref y);
            else if(IsRollMode)             DrawSmallLabel(g,"ROLL",Color.DodgerBlue,r,ref y);
            else if(!_isRunning)            DrawSmallLabel(g,"STOPPED",Color.Gray,r,ref y);
        }

        private float DrawMeasSection(Graphics g,string title,Color color,MeasResult m,RectangleF r,float y,bool cal,double offDb)
        {
            using var fT=new Font("Segoe UI",8.5f,FontStyle.Bold);using var fV=new Font("Segoe UI",8f);
            using var brT=new SolidBrush(color);using var brV=new SolidBrush(LabelColor);using var brG=new SolidBrush(Color.Gray);
            float x=r.Left+6; g.DrawString(title,fT,brT,x,y); y+=16;
            if(!m.Valid){foreach(var l in new[]{"Vrms  ---","Vpp   ---","Vdc   ---","Freq  ---","Per   ---"}){g.DrawString(l,fV,brG,x,y);y+=14;}return y+4;}
            double sc=cal?Math.Pow(10,offDb/20):1.0;string u=cal?"V":"";
            string Fv(double v)=>cal?FormatVoltage(v*sc):$"{v:F4}";
            g.DrawString($"Vrms  {Fv(m.VrmsAc)}{u}",fV,brV,x,y);y+=14;
            g.DrawString($"Vpp   {Fv(m.Vpp)}{u}",   fV,brV,x,y);y+=14;
            g.DrawString($"Vdc   {Fv(m.Vdc)}{u}",   fV,brV,x,y);y+=14;
            if(m.FreqHz>0){string fStr=m.FreqHz>=1000?$"{m.FreqHz/1000:G4} kHz":$"{m.FreqHz:G4} Hz";g.DrawString($"Freq  {fStr}",fV,brV,x,y);y+=14;g.DrawString($"Per   {1000/m.FreqHz:G4} ms",fV,brV,x,y);y+=14;}
            else{g.DrawString("Freq  ---",fV,brG,x,y);y+=14;g.DrawString("Per   ---",fV,brG,x,y);y+=14;}
            return y+4;
        }

        private static string FormatVoltage(double v){double a=Math.Abs(v);return a>=10?$"{v:F2}":a>=1?$"{v:F3}":a>=0.1?$"{v:F4}":$"{v:G4}";}
        private static void DrawSmallLabel(Graphics g,string text,Color c,RectangleF r,ref float y){using var f=new Font("Segoe UI",7.5f,FontStyle.Italic);using var br=new SolidBrush(c);g.DrawString(text,f,br,r.Left+6,y);y+=14;}

        public OscilloscopeSettings GetSettingsSnapshot()=>new OscilloscopeSettings{ChannelIndex=cmbChannel.SelectedIndex,TimeDivIndex=cmbTimediv.SelectedIndex,TrigModeIndex=cmbTrigMode.SelectedIndex,TrigEdgeIndex=cmbTrigEdge.SelectedIndex,TrigSrcIndex=cmbTrigSrc.SelectedIndex,TrigLevel=(double)nudTrigLevel.Value,ZoomYIndex=cmbVdiv.SelectedIndex,AcCouplingIndex=cmbCoupling.SelectedIndex,RollMode=chkRoll.Checked};

        public void ApplySettings(OscilloscopeSettings? s)
        {
            if(s==null)return;
            cmbChannel.SelectedIndex =Math.Clamp(s.ChannelIndex, 0,2);
            cmbTimediv.SelectedIndex =Math.Clamp(s.TimeDivIndex, 0,TimeDivMs.Length-1);
            cmbTrigMode.SelectedIndex=Math.Clamp(s.TrigModeIndex,0,2);
            cmbTrigEdge.SelectedIndex=Math.Clamp(s.TrigEdgeIndex,0,1);
            cmbTrigSrc.SelectedIndex =Math.Clamp(s.TrigSrcIndex, 0,1);
            nudTrigLevel.Value=(decimal)Math.Clamp(s.TrigLevel,-1.0,1.0);
            cmbVdiv.SelectedIndex    =Math.Clamp(s.ZoomYIndex,   0,ZoomY.Length-1);
            cmbCoupling.SelectedIndex=Math.Clamp(s.AcCouplingIndex,0,1);
            UpdateRollCheckbox();
            if(chkRoll.Enabled) chkRoll.Checked=s.RollMode;
        }

        protected override void OnHandleDestroyed(EventArgs e){_timer.Stop();_timer.Dispose();base.OnHandleDestroyed(e);}
    }

    internal sealed class ScopePanel : Panel
    {
        public ScopePanel(){DoubleBuffered=true;TabStop=true;SetStyle(ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint,true);}
    }

    public sealed class OscilloscopeSettings
    {
        public int    ChannelIndex      { get; set; } = 2;
        public int    TimeDivIndex      { get; set; } = 6;
        public int    TrigModeIndex     { get; set; } = 0;
        public int    TrigEdgeIndex     { get; set; } = 0;
        public int    TrigSrcIndex      { get; set; } = 0;
        public double TrigLevel         { get; set; } = 0.0;
        public int    ZoomYIndex        { get; set; } = 0;
        public int    AcCouplingIndex   { get; set; } = 0;
        public bool   RollMode          { get; set; } = false;
    }
}
