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

        // ── Couleurs ──────────────────────────────────────────────────────────
        private SpectrumColorSettings _colors = SpectrumColorSettings.Defaults();
        private Color BgColor       => Color.FromArgb(_colors.BackgroundArgb);
        private Color GridMajColor  => Color.FromArgb(_colors.GridArgb);
        private Color GridMinColor  => Color.FromArgb(_colors.GridMinorArgb);
        private Color TraceColorL   => Color.FromArgb(_colors.FftLeftArgb);
        private Color TraceColorR   => Color.FromArgb(_colors.FftRightArgb);
        private Color InfoTextColor => Color.FromArgb(_colors.InfoTextArgb);

        // ── Constantes ────────────────────────────────────────────────────────
        private const int   H_DIVS        = 10;
        private const int   V_DIVS        = 8;
        private const float MEAS_W        = 200f;   // identique ChartStatsWidthPx = 200 (Spectrum)
        private const float MARGIN_LEFT   = 62f;
        private const float MARGIN_BOTTOM = 32f;
        private const float MARGIN_TOP    = 20f;  // zone pour la ligne d'info au-dessus de la grille
        private const float PRE_TRIG_DEFAULT = 0.20f;

        /// Position horizontale du trigger : fraction [0,1] du début de la fenêtre.
        /// Lue depuis nudTrigPos (en divisions / H_DIVS).
        private float PreTrig => nudTrigPos != null
            ? Math.Clamp((float)nudTrigPos.Value / H_DIVS, 0.05f, 0.95f)
            : PRE_TRIG_DEFAULT;
        private const float DC_TAU        = 0.3f;

        private static readonly double[] TimeDivMs =
        {
            0.01, 0.02, 0.05, 0.1, 0.2, 0.5,
            1.0,  2.0,  5.0, 10.0, 20.0, 50.0,
            100.0, 200.0, 500.0, 1000.0, 2000.0, 5000.0
        };
        private const int INDEX_ROLL_MIN = 9;

        // Zoom ×N
        private static readonly float[]  ZoomY = { 1f,2f,5f,10f,20f,50f,100f,200f,500f,1000f };
        // V/div (m = 1-2-5 séquence)
        private static readonly double[] VdivValues = { 0.001,0.002,0.005,0.01,0.02,0.05,0.1,0.2,0.5,1.0,2.0,5.0,10.0 };
        private static readonly string[] VdivStrings = { "1 mV/div","2 mV/div","5 mV/div","10 mV/div","20 mV/div","50 mV/div","100 mV/div","200 mV/div","500 mV/div","1 V/div","2 V/div","5 V/div","10 V/div" };
        private static readonly string[] ZoomStrings = { "×1","×2","×5","×10","×20","×50","×100","×200","×500","×1000" };

        // ── Mode V/div et zoom effectif ───────────────────────────────────────
        private bool  _vdivMode;
        private float _effZoomL = 1f, _effZoomR = 1f;

        // ── Couplage et zoom par voie ─────────────────────────────────────────
        private bool AcL => cmbCouplingL.SelectedIndex == 1;
        private bool AcR => cmbCouplingR.SelectedIndex == 1;
        private int  GridMode => Math.Clamp(cmbGrid.SelectedIndex, 0, 2); // 0=Full 1=Axes 2=None

        // ── État ──────────────────────────────────────────────────────────────
        private bool _isRunning, _isArmed, _lastTrigOk;
        private int  _blinkCounter; private bool _blinkState;

        // ── Données affichées ─────────────────────────────────────────────────
        private float[]?  _dispL, _dispR;
        private int       _trigPx;
        private bool      _trigFound;
        private MeasResult _measL, _measR;
        private int       _lastSR;
        private string    _infoLine = "";

        // ── Buffer post-stop ──────────────────────────────────────────────────
        private float[]? _capL, _capR;
        private int _capSR, _capTrigIdx, _panOffset;
        private float _dcEstL, _dcEstR;

        // ── Curseurs C1 / C2 ─────────────────────────────────────────────────
        private bool  _c1Active, _c2Active;
        private bool  _c1Frozen, _c2Frozen;          // figé par double-clic
        private float _c1Frac = 1f/3, _c2Frac = 2f/3; // position [0,1] dans la fenêtre
        private int   _c1Chan = 0, _c2Chan = 1;       // 0=Left 1=Right 2=Auto
        private int   _cursorDragging;                 // 0=none 1=C1 2=C2
        private static readonly Color CC1 = Color.OrangeRed;
        private static readonly Color CC2 = Color.DodgerBlue;

        private readonly System.Windows.Forms.Timer _timer;

        private struct MeasResult { public bool Valid; public double Vpp, Vrms, Vdc, FreqHz; }

        // ─────────────────────────────────────────────────────────────────────
        public OscilloscopeControl()
        {
            InitializeComponent();

            // Pas de BorderStyle sur le UserControl (comme SpectrumControl) :
            // la bordure est dessinée dans PnlScope_Paint sans réduire ClientSize.

            _timer = new System.Windows.Forms.Timer { Interval = 33 };
            _timer.Tick += Timer_Tick;

            btnRun.Click += BtnRun_Click;
            btnRun.Paint += (s, e) =>
            {
                var b = (Button)s!;
                e.Graphics.Clear(b.BackColor);
                TextRenderer.DrawText(e.Graphics, b.Text, b.Font, b.ClientRectangle, b.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            };

            cmbTimediv.SelectedIndexChanged += (_, _) =>
            { UpdateRollCheckbox(); if (_isRunning) pnlScope.Invalidate(); else RefreshStoppedView(); };

            cmbChannel.SelectedIndexChanged   += (_, _) => pnlScope.Invalidate();
            cmbTrigMode.SelectedIndexChanged  += (_, _) => { };
            cmbTrigEdge.SelectedIndexChanged  += (_, _) => { };
            cmbTrigSrc.SelectedIndexChanged   += (_, _) => { };
            nudTrigLevel.ValueChanged         += (_, _) => pnlScope.Invalidate();
            nudTrigPos.ValueChanged           += (_, _) => { if (!_isRunning) RefreshStoppedView(); else pnlScope.Invalidate(); };
            cmbVdivL.SelectedIndexChanged     += (_, _) => pnlScope.Invalidate();
            cmbVdivR.SelectedIndexChanged     += (_, _) => pnlScope.Invalidate();
            cmbCouplingL.SelectedIndexChanged += (_, _) => { if (!_isRunning) RefreshStoppedView(); };
            cmbCouplingR.SelectedIndexChanged += (_, _) => { if (!_isRunning) RefreshStoppedView(); };
            cmbGrid.SelectedIndexChanged      += (_, _) => pnlScope.Invalidate();
            chkRoll.CheckedChanged            += (_, _) => { };
            chkVdiv.CheckedChanged            += ChkVdiv_Changed;

            pnlScope.Paint      += PnlScope_Paint;
            pnlScope.Resize     += (_, _) => pnlScope.Invalidate();
            pnlScope.MouseEnter += (_, _) => { if (!_isRunning) pnlScope.Focus(); };
            pnlScope.MouseWheel += PnlScope_MouseWheel;

            // ── Curseurs ─────────────────────────────────────────────────────
            btnC1.Click     += (_, _) => { _c1Active=!_c1Active; UpdateCursorButtons(); pnlScope.Invalidate(); };
            btnC2.Click     += (_, _) => { _c2Active=!_c2Active; UpdateCursorButtons(); pnlScope.Invalidate(); };
            btnC1Chan.Click += (_, _) => { _c1Chan=(_c1Chan+1)%3; UpdateCursorButtons(); pnlScope.Invalidate(); };
            btnC2Chan.Click += (_, _) => { _c2Chan=(_c2Chan+1)%3; UpdateCursorButtons(); pnlScope.Invalidate(); };

            pnlScope.MouseDown        += PnlScope_CursorMouseDown;
            pnlScope.MouseMove        += PnlScope_CursorMouseMove;
            pnlScope.MouseUp          += (_, _) => _cursorDragging = 0;
            pnlScope.MouseDoubleClick += PnlScope_CursorDblClick;

            UpdateCursorButtons();

            UpdateRollCheckbox();
            SetRunState(false);
        }

        // ─────────────────────────────────────────────────────────────────────
        public void ApplySpectrumColorSettings(SpectrumColorSettings? c)
        { _colors = c ?? SpectrumColorSettings.Defaults(); _colors.Normalize(); pnlScope.Invalidate(); }
        public void UpdateSampleRate(int sr) { _lastSR = sr; UpdateInfoLine(); }

        private bool IsAutoMode   => cmbTrigMode.SelectedIndex == 0;
        private bool IsSingleMode => cmbTrigMode.SelectedIndex == 2;
        private bool IsRollMode   => chkRoll.Checked && chkRoll.Enabled;

        private void UpdateRollCheckbox()
        {
            bool can = cmbTimediv.SelectedIndex >= INDEX_ROLL_MIN;
            chkRoll.Enabled = can;
            if (!can) chkRoll.Checked = false;
        }

        // ── Helpers curseurs ──────────────────────────────────────────────────
        private void UpdateCursorButtons()
        {
            string[] cn = {"Left","Right","Auto"};
            btnC1.BackColor = _c1Active ? CC1              : SystemColors.Control;
            btnC1.ForeColor = _c1Active ? Color.White      : SystemColors.ControlText;
            btnC2.BackColor = _c2Active ? CC2              : SystemColors.Control;
            btnC2.ForeColor = _c2Active ? Color.White      : SystemColors.ControlText;
            btnC1Chan.Text  = cn[_c1Chan];
            btnC2Chan.Text  = cn[_c2Chan];
        }

        private RectangleF GetWaveRect()
        {
            var sz = pnlScope.ClientSize;
            if (sz.Width<60||sz.Height<60) return RectangleF.Empty;
            return new RectangleF(MARGIN_LEFT, MARGIN_TOP,
                sz.Width-MARGIN_LEFT-MEAS_W, sz.Height-MARGIN_TOP-MARGIN_BOTTOM);
        }

        /// Retourne les données du curseur selon son réglage de voie.
        private float[]? GetCursorData(int cursorIdx)
        {
            int ch   = cmbChannel.SelectedIndex;
            bool hasL= _dispL!=null && (ch==0||ch==2);
            bool hasR= _dispR!=null && (ch==1||ch==2);
            int chan = cursorIdx==1 ? _c1Chan : _c2Chan;
            return chan switch
            {
                0 => hasL ? _dispL : null,
                1 => hasR ? _dispR : null,
                _ => cursorIdx==1 ? (hasL?_dispL:_dispR) : (hasR?_dispR:_dispL)
            };
        }

        private float GetCursorZoom(int cursorIdx)
        {
            int ch   = cmbChannel.SelectedIndex;
            bool hasL= ch==0||ch==2;
            bool hasR= ch==1||ch==2;
            int chan = cursorIdx==1 ? _c1Chan : _c2Chan;
            return chan switch
            {
                0 => _effZoomL,
                1 => _effZoomR,
                _ => cursorIdx==1 ? (hasL?_effZoomL:_effZoomR) : (hasR?_effZoomR:_effZoomL)
            };
        }

        private (bool calOk, double offDb) GetCursorCal(int cursorIdx)
        {
            if (GetInputCalibration == null) return (false, 0);
            var (vL, oL, vR, oR) = GetInputCalibration();
            int chan = cursorIdx==1 ? _c1Chan : _c2Chan;
            int ch   = cmbChannel.SelectedIndex;
            bool hasL= ch==0||ch==2;
            bool hasR= ch==1||ch==2;
            return chan switch
            {
                0 => (vL, oL),
                1 => (vR, oR),
                _ => cursorIdx==1 ? (hasL?(vL,oL):(vR,oR)) : (hasR?(vR,oR):(vL,oL))
            };
        }

        private (double timeMs, float normVal, bool valid) EvalCursor(float frac, int cursorIdx)
        {
            int tdIdx    = Math.Clamp(cmbTimediv.SelectedIndex, 0, TimeDivMs.Length-1);
            double tMs   = (frac - PreTrig) * TimeDivMs[tdIdx] * H_DIVS;
            float[]? dat = GetCursorData(cursorIdx);
            if (dat==null||dat.Length<2) return (tMs, 0f, false);
            float t=frac*(dat.Length-1); int lo=(int)t, hi=Math.Min(lo+1,dat.Length-1);
            return (tMs, dat[lo]*(1f-(t-lo))+dat[hi]*(t-lo), true);
        }

        // ── Souris curseurs ───────────────────────────────────────────────────
        private void PnlScope_CursorMouseDown(object? s, MouseEventArgs e)
        {
            if (e.Button!=MouseButtons.Left) return;
            var w=GetWaveRect(); if(w.IsEmpty) return;
            float mx=e.X, thresh=10f;
            if (_c1Active&&!_c1Frozen&&Math.Abs(mx-(w.Left+_c1Frac*w.Width))<=thresh) _cursorDragging=1;
            else if(_c2Active&&!_c2Frozen&&Math.Abs(mx-(w.Left+_c2Frac*w.Width))<=thresh) _cursorDragging=2;
        }

        private void PnlScope_CursorMouseMove(object? s, MouseEventArgs e)
        {
            var w=GetWaveRect(); if(w.IsEmpty) return;
            float mx=e.X;
            if (_cursorDragging>0)
            {
                float f=Math.Clamp((mx-w.Left)/w.Width,0f,1f);
                if(_cursorDragging==1) _c1Frac=f; else _c2Frac=f;
                pnlScope.Invalidate(); return;
            }
            float c1x=w.Left+_c1Frac*w.Width, c2x=w.Left+_c2Frac*w.Width;
            bool near=(_c1Active&&!_c1Frozen&&Math.Abs(mx-c1x)<=10f)
                    ||(_c2Active&&!_c2Frozen&&Math.Abs(mx-c2x)<=10f);
            pnlScope.Cursor=near?Cursors.SizeWE:Cursors.Default;
        }

        private void PnlScope_CursorDblClick(object? s, MouseEventArgs e)
        {
            var w=GetWaveRect(); if(w.IsEmpty) return;
            float mx=e.X, thresh=10f;
            if (_c1Active&&Math.Abs(mx-(w.Left+_c1Frac*w.Width))<=thresh) { _c1Frozen=!_c1Frozen; pnlScope.Invalidate(); }
            else if(_c2Active&&Math.Abs(mx-(w.Left+_c2Frac*w.Width))<=thresh) { _c2Frozen=!_c2Frozen; pnlScope.Invalidate(); }
        }

        // ── Dessin des curseurs ───────────────────────────────────────────────
        private void DrawCursors(Graphics g, RectangleF w)
        {
            // Dessiner C2 d'abord (C1 par-dessus)
            if (_c2Active) DrawOneCursor(g, w, _c2Frac, 2, _c2Frozen, CC2, "C2");
            if (_c1Active) DrawOneCursor(g, w, _c1Frac, 1, _c1Frozen, CC1, "C1");
        }

        private void DrawOneCursor(Graphics g, RectangleF w, float frac, int cursorIdx,
            bool frozen, Color color, string label)
        {
            float cx = w.Left + frac * w.Width;

            // Ligne verticale (tiretée si mobile, pleine si figée)
            using var pen = new Pen(Color.FromArgb(210, color), 1f)
                { DashStyle = frozen ? DashStyle.Solid : DashStyle.Dash };
            g.DrawLine(pen, cx, w.Top, cx, w.Bottom);

            // Marqueur diamant sur la courbe
            float[]? dat = GetCursorData(cursorIdx);
            if (dat != null && dat.Length >= 2)
            {
                float zoom = GetCursorZoom(cursorIdx);
                float t = frac*(dat.Length-1); int lo=(int)t,hi=Math.Min(lo+1,dat.Length-1);
                float val = dat[lo]*(1f-(t-lo))+dat[hi]*(t-lo);
                float ym  = w.Top+w.Height/2f, ys=w.Height/2f*zoom;
                float yPos= Math.Clamp(ym-val*ys, w.Top, w.Bottom);
                float d   = 6f;
                var pts   = new[]{new PointF(cx,yPos-d),new PointF(cx+d,yPos),
                                  new PointF(cx,yPos+d),new PointF(cx-d,yPos)};
                using var br = new SolidBrush(Color.FromArgb(220, color));
                g.FillPolygon(br, pts);
            }

            // Étiquette + icône figé
            using var font = new Font("Segoe UI", 8f, FontStyle.Bold);
            using var brL  = new SolidBrush(color);
            string lbl = label + (frozen ? " ⏸" : "");
            g.DrawString(lbl, font, brL, cx+3, w.Top+2);
        }

        // ── Mode V/div ────────────────────────────────────────────────────────
        private void ChkVdiv_Changed(object? s, EventArgs e)
        {
            _vdivMode = chkVdiv.Checked;
            UpdateGainCombos();
            pnlScope.Invalidate();
        }

        private void UpdateGainCombos()
        {
            // ── Calcul des indices équivalents avant de changer les items ────────
            // On convertit l'indice courant vers le mode CIBLE pour conserver
            // la même plage d'amplitude réelle autant que possible.
            int newIdxL = SmartConvertGainIdx(cmbVdivL.SelectedIndex, !_vdivMode, 0);
            int newIdxR = SmartConvertGainIdx(cmbVdivR.SelectedIndex, !_vdivMode, 1);

            cmbVdivL.Items.Clear(); cmbVdivR.Items.Clear();
            object[] items = _vdivMode ? VdivStrings : ZoomStrings;
            cmbVdivL.Items.AddRange(items); cmbVdivR.Items.AddRange(items);
            cmbVdivL.SelectedIndex = Math.Clamp(newIdxL, 0, items.Length-1);
            cmbVdivR.SelectedIndex = Math.Clamp(newIdxR, 0, items.Length-1);
            lblGainHdr.Text = "GAIN";
        }

        /// <summary>
        /// Convertit un indice de la gamme SOURCE vers la gamme CIBLE en préservant
        /// la plage d'amplitude réelle. Utilise l'échelle logarithmique.
        /// sourceIsZoom = true  → on vient de ×N, on va vers V/div
        /// sourceIsZoom = false → on vient de V/div, on va vers ×N
        /// </summary>
        private int SmartConvertGainIdx(int srcIdx, bool sourceIsZoom, int ch)
        {
            if (GetInputCalibration == null) return 0;
            var (vL, oL, vR, oR) = GetInputCalibration();
            bool calOk  = ch == 1 ? vR : vL;
            double offDb= ch == 1 ? oR : oL;
            if (!calOk) return 0;

            // calScale : facteur de conversion normalisé → Volts (incluant ×√2)
            double calScale = Math.Pow(10, offDb / 20.0) * Math.Sqrt(2.0);

            if (sourceIsZoom) // ×N → V/div : FS normalisé = ±1/zoom → FS en V
            {
                float  zoom         = ZoomY[Math.Clamp(srcIdx, 0, ZoomY.Length-1)];
                double halfRangeV   = calScale / zoom;                // amplitude max en V
                double vdivDesired  = halfRangeV / (V_DIVS / 2.0);   // V/div équivalent
                return FindClosestLog(VdivValues, vdivDesired);
            }
            else // V/div → ×N
            {
                double vd           = VdivValues[Math.Clamp(srcIdx, 0, VdivValues.Length-1)];
                double halfRangeV   = vd * (V_DIVS / 2.0);           // amplitude max en V
                double zoomDesired  = calScale / halfRangeV;          // zoom équivalent
                var zoomD = new double[]{ 1,2,5,10,20,50,100,200,500,1000 };
                return FindClosestLog(zoomD, zoomDesired);
            }
        }

        /// Trouve l'indice dans 'arr' dont la valeur log est la plus proche de log(target).
        private static int FindClosestLog(double[] arr, double target)
        {
            if (target <= 0) return 0;
            double logT = Math.Log(target);
            int best = 0; double bestDist = double.MaxValue;
            for (int i = 0; i < arr.Length; i++)
            {
                double d = Math.Abs(Math.Log(arr[i]) - logT);
                if (d < bestDist) { bestDist = d; best = i; }
            }
            return best;
        }

        /// Calcule les zooms effectifs (×N ou V/div) et met à jour _effZoomL/R.
        private void RefreshEffectiveZooms()
        {
            _effZoomL = GetEffectiveZoom(0);
            _effZoomR = GetEffectiveZoom(1);

            // Activer la case V/div seulement si calibration disponible
            if (GetInputCalibration != null)
            {
                var (vL, _, vR, _) = GetInputCalibration();
                chkVdiv.Enabled = vL || vR;
            }
        }

        private float GetEffectiveZoom(int ch)
        {
            var cmb = ch == 1 ? cmbVdivR : cmbVdivL;
            int idx = cmb.SelectedIndex;
            if (_vdivMode && GetInputCalibration != null)
            {
                var (vL, oL, vR, oR) = GetInputCalibration();
                bool calOk = ch == 1 ? vR : vL;
                double offDb = ch == 1 ? oR : oL;
                if (calOk)
                {
                    double sc = Math.Pow(10, offDb/20.0) * Math.Sqrt(2.0);
                    double vd = VdivValues[Math.Clamp(idx, 0, VdivValues.Length-1)];
                    return (float)Math.Max(0.001, sc / (vd * (V_DIVS/2.0)));
                }
            }
            return ZoomY[Math.Clamp(idx, 0, ZoomY.Length-1)];
        }

        private double GetVdivForCh(int ch)
        {
            var cmb = ch == 1 ? cmbVdivR : cmbVdivL;
            return VdivValues[Math.Clamp(cmb.SelectedIndex, 0, VdivValues.Length-1)];
        }

        // ── Run/Stop ──────────────────────────────────────────────────────────
        private void BtnRun_Click(object? s, EventArgs e)
        { if (_isRunning) StopOscillo(); else StartOscillo(); }

        private void StartOscillo()
        {
            _panOffset=0; _dcEstL=0f; _dcEstR=0f;
            _blinkCounter=0; _blinkState=false; _lastTrigOk=false;
            RunRequested?.Invoke();
            SetRunState(true); SetArmedState(false);
            _timer.Start();
        }

        private void StopOscillo()
        { _timer.Stop(); SetRunState(false); lblTrigStatus.Text=""; RunStateChanged?.Invoke(false); }

        private void SetRunState(bool on)
        {
            _isRunning=on;
            btnRun.BackColor=on?Color.Firebrick:Color.SeaGreen;
            btnRun.ForeColor=Color.White;
            btnRun.Text=on?"■  STOP":"▶  RUN";
            btnRun.TextAlign=ContentAlignment.MiddleCenter;
            if(on) RunStateChanged?.Invoke(true);
        }

        private void SetArmedState(bool armed)
        {
            if(!_isRunning) return;
            _isArmed=armed;
            btnRun.BackColor=Color.Firebrick; btnRun.ForeColor=Color.White;
            btnRun.Text="■  STOP"; btnRun.TextAlign=ContentAlignment.MiddleCenter;
            UpdateTrigStatus();
        }

        private void UpdateTrigStatus()
        {
            if(!_isRunning){lblTrigStatus.Text="";return;}
            if(_isArmed)
            {
                _blinkCounter++; if(_blinkCounter>=15){_blinkCounter=0;_blinkState=!_blinkState;}
                lblTrigStatus.Text="WAIT";
                lblTrigStatus.ForeColor=_blinkState?Color.Lime:Color.FromArgb(0,80,0);
            }
            else if(_lastTrigOk){ lblTrigStatus.Text="TRIG'D"; lblTrigStatus.ForeColor=Color.OrangeRed; }
            else lblTrigStatus.Text="";
        }

        // ── Molette / navigation post-stop ────────────────────────────────────
        private void PnlScope_MouseWheel(object? s, MouseEventArgs e)
        {
            if(_isRunning||_capL==null||_capSR<=0) return;
            int td=Math.Clamp(cmbTimediv.SelectedIndex,0,TimeDivMs.Length-1);
            int spd=Math.Max(1,(int)(TimeDivMs[td]/1000.0*_capSR));
            _panOffset+=e.Delta>0?-spd:spd;
            RefreshStoppedView();
        }

        private void RefreshStoppedView()
        {
            if(_capL==null||_capR==null||_capSR<=0) return;
            int td=Math.Clamp(cmbTimediv.SelectedIndex,0,TimeDivMs.Length-1);
            int win=Math.Max(8,(int)(TimeDivMs[td]*H_DIVS/1000.0*_capSR));
            int pre=(int)(win*PreTrig);
            int tref=_capTrigIdx>=0?_capTrigIdx:pre;
            int ds=Math.Clamp(tref-pre+_panOffset,0,Math.Max(0,_capL.Length-win));
            win=Math.Min(win,_capL.Length-ds); if(win<2) return;
            float[] sl=new float[win],sr2=new float[win];
            Array.Copy(_capL,ds,sl,0,win); Array.Copy(_capR,ds,sr2,0,win);
            if(AcL) for(int i=0;i<win;i++) sl[i]-=_dcEstL;
            if(AcR) for(int i=0;i<win;i++) sr2[i]-=_dcEstR;
            _dispL=sl; _dispR=sr2;
            int rel=tref-ds; _trigPx=(rel>=0&&rel<win)?rel:-1; _trigFound=_trigPx>=0;
            _measL=ComputeMeasurements(_dispL,_capSR);
            _measR=ComputeMeasurements(_dispR,_capSR);
            _lastSR=_capSR; UpdateInfoLine(); pnlScope.Invalidate();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Timer tick
        // ─────────────────────────────────────────────────────────────────────
        private void Timer_Tick(object? s, EventArgs e)
        {
            var ring=GetRing?.Invoke(); int sr=GetSampleRate?.Invoke()??_lastSR;
            if(ring==null||sr<=0){pnlScope.Invalidate();return;}
            _lastSR=sr;

            int td=Math.Clamp(cmbTimediv.SelectedIndex,0,TimeDivMs.Length-1);
            int win=Math.Max(8,(int)(TimeDivMs[td]*H_DIVS/1000.0*sr));
            int pre=(int)(win*PreTrig);
            int search=Math.Min(win*3+pre+4,ring.Capacity-1);

            long absEnd=ring.AbsWrite;
            if(absEnd-search<0){pnlScope.Invalidate();return;}
            var rawL=new float[search]; var rawR=new float[search];
            if(!ring.TryReadWindow(absEnd-search,search,rawL,rawR)){pnlScope.Invalidate();return;}

            float alpha=Math.Min(1.0f,(float)search/(sr*DC_TAU));
            if(AcL){float s2=0; for(int i=0;i<search;i++)s2+=rawL[i];_dcEstL=_dcEstL*(1f-alpha)+(s2/search)*alpha;for(int i=0;i<search;i++)rawL[i]-=_dcEstL;}
            if(AcR){float s2=0; for(int i=0;i<search;i++)s2+=rawR[i];_dcEstR=_dcEstR*(1f-alpha)+(s2/search)*alpha;for(int i=0;i<search;i++)rawR[i]-=_dcEstR;}

            if(IsRollMode){SetArmedState(false);StoreAndDisplay(rawL,rawR,search,search-win,win,sr,-1);pnlScope.Invalidate();return;}

            var trigSrc=(cmbTrigSrc.SelectedIndex==1)?rawR:rawL;
            float tLvl=(float)nudTrigLevel.Value; bool rising=cmbTrigEdge.SelectedIndex==0;
            float vmax=float.MinValue,vmin=float.MaxValue;
            for(int i=0;i<search;i++){if(trigSrc[i]>vmax)vmax=trigSrc[i];if(trigSrc[i]<vmin)vmin=trigSrc[i];}
            float hyst=Math.Max(0.002f,(vmax-vmin)*0.05f);

            bool trigOk=FindTriggerBackward(trigSrc,search,tLvl,rising,hyst,win,pre,out int trigIdx);

            int dstart;
            if(trigOk){dstart=trigIdx-pre;_trigFound=true;_lastTrigOk=true;SetArmedState(false);}
            else if(IsAutoMode){dstart=search-win;_trigFound=false;_lastTrigOk=false;SetArmedState(false);}
            else{_lastTrigOk=false;SetArmedState(true);pnlScope.Invalidate();return;}

            dstart=Math.Max(0,Math.Min(dstart,search-win));
            if(dstart+win>search){pnlScope.Invalidate();return;}
            StoreAndDisplay(rawL,rawR,search,dstart,win,sr,trigOk?trigIdx:-1);
            UpdateTrigStatus();
            if(IsSingleMode&&trigOk){pnlScope.Invalidate();StopOscillo();return;}
            pnlScope.Invalidate();
        }

        private void StoreAndDisplay(float[] rawL,float[] rawR,int search,int dstart,int win,int sr,int trigIdx)
        {
            _capL=rawL;_capR=rawR;_capSR=sr;_capTrigIdx=trigIdx;
            _dispL=new float[win];_dispR=new float[win];
            Array.Copy(rawL,dstart,_dispL,0,win);Array.Copy(rawR,dstart,_dispR,0,win);
            _trigPx=trigIdx>=0?trigIdx-dstart:(int)(win*PreTrig);
            _measL=ComputeMeasurements(_dispL,sr);_measR=ComputeMeasurements(_dispR,sr);
            UpdateInfoLine();
        }

        private static bool FindTriggerBackward(float[] src,int len,float level,bool rising,float hyst,int win,int pre,out int idx)
        {
            idx=-1;float arm=rising?level-hyst:level+hyst;
            int post=win-pre,maxI=len-post-1,minI=pre+1;
            if(maxI<=minI) return false;
            maxI=Math.Min(maxI,len-2);
            for(int i=maxI;i>=minI;i--)
            {
                bool cross=rising?(src[i]<level&&src[i+1]>=level):(src[i]>level&&src[i+1]<=level);
                if(!cross) continue;
                int lb=Math.Max(0,i-win);
                for(int j=i-1;j>=lb;j--)
                    if(rising?src[j]<=arm:src[j]>=arm){idx=i;return true;}
            }
            return false;
        }

        // ─────────────────────────────────────────────────────────────────────
        private static MeasResult ComputeMeasurements(float[] s,int sr)
        {
            if(s.Length<4) return default;
            double sum=0,sumSq=0;float vmax=s[0],vmin=s[0];
            for(int i=0;i<s.Length;i++){sum+=s[i];sumSq+=(double)s[i]*s[i];if(s[i]>vmax)vmax=s[i];if(s[i]<vmin)vmin=s[i];}
            double vdc=sum/s.Length;
            return new MeasResult{Valid=true,Vpp=vmax-vmin,Vrms=Math.Sqrt(sumSq/s.Length),Vdc=vdc,FreqHz=DetectFrequency(s,sr,vdc,vmax-vmin)};
        }
        private static double DetectFrequency(float[] s,int sr,double vdc,double vpp)
        {
            if(vpp<0.005f) return 0;
            float hyst=(float)(vpp*0.05),arm=(float)(vdc-hyst),trig=(float)(vdc+hyst);
            bool armed=false;int prev=-1,count=0;long sumI=0;
            for(int i=1;i<s.Length;i++){if(!armed&&s[i]<=arm)armed=true;if(armed&&s[i-1]<trig&&s[i]>=trig){if(prev>=0){sumI+=i-prev;count++;}prev=i;armed=false;}}
            if(count<1) return 0;double per=(double)sumI/count;return per>0?sr/per:0;
        }

        private void UpdateInfoLine()
        {
            int td=Math.Clamp(cmbTimediv.SelectedIndex,0,TimeDivMs.Length-1);
            int ch=cmbChannel.SelectedIndex;
            string sr=_lastSR>0?$"{_lastSR/1000.0:G3} kHz":"—";
            string ch2=ch switch{0=>"Left",1=>"Right",_=>"L+R"};
            string pan=(!_isRunning&&_panOffset!=0&&_capSR>0)?$"   pan {FormatTime((double)_panOffset/_capSR*1000.0)}":"";
            string roll=IsRollMode?"  ROLL":"";
            string zoom="";
            if(!_vdivMode)
            {
                float zL=_effZoomL,zR=_effZoomR;
                if(ch==0&&zL>1f)zoom=$"   L×{zL:G0}";
                else if(ch==1&&zR>1f)zoom=$"   R×{zR:G0}";
                else if(ch==2){if(zL==zR&&zL>1f)zoom=$"   ×{zL:G0}";else if(zL>1f||zR>1f)zoom=$"   L×{zL:G0} R×{zR:G0}";}
            }
            string coup="";
            if(ch==0&&AcL)coup="  AC";else if(ch==1&&AcR)coup="  AC";
            else if(ch==2){if(AcL&&AcR)coup="  AC";else if(AcL)coup="  L:AC";else if(AcR)coup="  R:AC";}
            _infoLine=$"Channel:{ch2}   SR:{sr}   {FormatTimediv(TimeDivMs[td])}{zoom}{coup}{roll}{pan}";
        }

        private static string FormatTime(double ms)
        {
            if(ms==0) return "0";double a=Math.Abs(ms);
            if(a>=1000) return $"{ms/1000:G3} s";if(a>=1)return $"{ms:G3} ms";return $"{ms*1000:G3} µs";
        }
        private static string FormatTimediv(double ms)
        {
            if(ms>=1000)return $"{ms/1000:G3} s/div";if(ms>=1)return $"{ms:G3} ms/div";return $"{ms*1000:G3} µs/div";
        }
        private static string FormatV(double v)
        { double a=Math.Abs(v); return a>=10?$"{v:F2}":a>=1?$"{v:F3}":a>=0.1?$"{v:F4}":$"{v:G4}"; }

        private static string FormatVoltLabel(double v)
        {
            if(Math.Abs(v)<1e-9) return "0";
            double a=Math.Abs(v);string sg=v>=0?"+":"-";
            if(a>=1.0) return $"{sg}{a:G3}V";
            if(a>=0.001) return $"{sg}{a*1000:G3}mV";
            return $"{sg}{a*1e6:G3}µV";
        }

        // ─────────────────────────────────────────────────────────────────────
        // Rendu
        // ─────────────────────────────────────────────────────────────────────
        private void PnlScope_Paint(object? sender, PaintEventArgs e)
        {
            RefreshEffectiveZooms();  // met à jour _effZoomL/R et active chkVdiv
            var g=e.Graphics; var sz=pnlScope.ClientSize;
            if(sz.Width<60||sz.Height<60) return;
            g.SmoothingMode=SmoothingMode.AntiAlias;
            g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var wave=new RectangleF(MARGIN_LEFT,MARGIN_TOP,sz.Width-MARGIN_LEFT-MEAS_W,sz.Height-MARGIN_TOP-MARGIN_BOTTOM);
            if(wave.Width<20||wave.Height<20) return;

            g.FillRectangle(SystemBrushes.Control,0,0,sz.Width,sz.Height);

            // ── Ligne d'info : même style que l'onglet Spectrum ──────────────
            // Segoe UI 8.25pt, couleur InfoTextColor (DarkBlue par défaut),
            // positionnée au-dessus du graphique dans MARGIN_TOP (comme dans PostPaint Spectrum)
            if (!string.IsNullOrEmpty(_infoLine))
            {
                using var fontInfo = new Font("Segoe UI", 8.25f);
                using var brInfo   = new SolidBrush(InfoTextColor);
                g.DrawString(_infoLine, fontInfo, brInfo,
                    new PointF(MARGIN_LEFT + 2, 3));
            }

            DrawGrid(g,wave);
            DrawAxisLabels(g,wave);

            int ch=cmbChannel.SelectedIndex;
            if(_dispL!=null&&(ch==0||ch==2)) DrawWaveform(g,wave,_dispL,TraceColorL,_colors.FftWidth,_effZoomL);
            if(_dispR!=null&&(ch==1||ch==2)) DrawWaveform(g,wave,_dispR,TraceColorR,_colors.FftWidth,_effZoomR);
            if(!IsRollMode&&(_dispL!=null||_dispR!=null))
                DrawTriggerMarkers(g,wave,_trigPx,(_dispL??_dispR)!.Length);

            // Curseurs (par-dessus les traces et le trigger)
            if (_c1Active || _c2Active) DrawCursors(g, wave);

            DrawMeasurements(g,wave);

            // Bordure noire 1px autour de pnlScope — dessinée EN DERNIER pour ne jamais
            // être recouverte. Technique identique au chart DataVisualization du Spectrum
            // (BorderlineColor + Width), mais sans réduire le ClientSize du contrôle.
            g.SmoothingMode = SmoothingMode.None;
            g.DrawRectangle(Pens.Black, 0, 0, sz.Width-1, sz.Height-1);
        }

        // ── Grille 3 modes ────────────────────────────────────────────────────
        private void DrawGrid(Graphics g, RectangleF w)
        {
            using var br=new SolidBrush(BgColor); g.FillRectangle(br,w);
            using var penMaj=new Pen(GridMajColor,1f);
            using var penMin=new Pen(GridMinColor,1f){DashStyle=DashStyle.Dot};

            // Croix centrale : CENTRE PHYSIQUE de l'affichage (H_DIVS/2, V_DIVS/2)
            float cx = w.Left + w.Width/2f;    // 5e division (centre sur 10)
            float cy = w.Top  + w.Height/2f;   // 4e division (centre sur 8)

            if(GridMode==2) // None — fond uniquement
            {
                g.DrawRectangle(penMaj,w.Left,w.Top,w.Width,w.Height);
                return;
            }

            // Tick marks communs aux modes Full et Axis
            float tickH = 5f;
            int hSub = H_DIVS*5, vSub = V_DIVS*5;
            using var penCtr = new Pen(GridMajColor, 1.2f);

            if(GridMode==1) // Axis — croix centrale + tick marks seulement
            {
                g.DrawLine(penMaj, w.Left, cy, w.Right, cy);   // axe H central
                g.DrawLine(penMaj, cx, w.Top, cx, w.Bottom);   // axe V central
                // Tick marks horizontaux sur l'axe Y=0
                for(int si=0;si<=hSub;si++)
                {
                    float x=w.Left+si*w.Width/hSub;
                    float tl=(si%5==0)?tickH*1.6f:tickH;
                    g.DrawLine(penCtr,x,cy-tl/2,x,cy+tl/2);
                }
                // Tick marks verticaux sur l'axe X=centre
                for(int si=0;si<=vSub;si++)
                {
                    float y=w.Top+si*w.Height/vSub;
                    float tl=(si%5==0)?tickH*1.6f:tickH;
                    g.DrawLine(penCtr,cx-tl/2,y,cx+tl/2,y);
                }
                g.DrawRectangle(penMaj,w.Left,w.Top,w.Width,w.Height);
                return;
            }

            // Full — grille complète + croix centrale avec tick marks
            for(int i=0;i<=H_DIVS;i++){float x=w.Left+i*w.Width/H_DIVS;g.DrawLine(i==0||i==H_DIVS?penMaj:penMin,x,w.Top,x,w.Bottom);}
            for(int i=0;i<=V_DIVS;i++){float y=w.Top+i*w.Height/V_DIVS;g.DrawLine(i==0||i==V_DIVS?penMaj:penMin,w.Left,y,w.Right,y);}

            // Croix centrale en surimpression
            g.DrawLine(penCtr,w.Left,cy,w.Right,cy);
            g.DrawLine(penCtr,cx,w.Top,cx,w.Bottom);
            // Tick marks
            for(int si=0;si<=hSub;si++)
            {
                float x=w.Left+si*w.Width/hSub;
                float tl=(si%5==0)?tickH*1.6f:tickH;
                g.DrawLine(penCtr,x,cy-tl/2,x,cy+tl/2);
            }
            for(int si=0;si<=vSub;si++)
            {
                float y=w.Top+si*w.Height/vSub;
                float tl=(si%5==0)?tickH*1.6f:tickH;
                g.DrawLine(penCtr,cx-tl/2,y,cx+tl/2,y);
            }
            g.DrawRectangle(penMaj,w.Left,w.Top,w.Width,w.Height);
        }

        // ── Labels des axes + légendes ────────────────────────────────────────
        private void DrawAxisLabels(Graphics g, RectangleF w)
        {
            using var font    = new Font("Segoe UI",9f,FontStyle.Regular);
            using var fontLeg = new Font("Segoe UI",8.5f,FontStyle.Italic);
            using var br      = new SolidBrush(SystemColors.ControlText);
            int ch=cmbChannel.SelectedIndex;
            int tdIdx=Math.Clamp(cmbTimediv.SelectedIndex,0,TimeDivMs.Length-1);
            double td=TimeDivMs[tdIdx];

            // ── Axe X (Temps) ─────────────────────────────────────────────────
            for(int i=0;i<=H_DIVS;i+=2)
            {
                float x=w.Left+i*w.Width/H_DIVS;
                double tMs=IsRollMode?i*td:(i-PreTrig*H_DIVS)*td;
                string lbl=(!IsRollMode&&Math.Abs(tMs)<td*0.001)?"0":FormatTime(tMs);
                var sf=new StringFormat{Alignment=StringAlignment.Center};
                g.DrawString(lbl,font,br,new RectangleF(x-38,w.Bottom+2,76,16),sf);
            }
            // Label "0" à la position exacte du trigger si elle tombe entre deux labels pairs
            if(!IsRollMode)
            {
                float trigDiv = PreTrig * H_DIVS;
                bool isAtEven = Math.Abs(trigDiv - Math.Round(trigDiv/2)*2) < 0.05f;
                if(!isAtEven)
                {
                    float xT = w.Left + PreTrig * w.Width;
                    var sfT = new StringFormat{Alignment=StringAlignment.Center};
                    g.DrawString("0",font,br,new RectangleF(xT-20,w.Bottom+2,40,16),sfT);
                }
            }

            // Légende "Time" en bas au centre
            using var sfC=new StringFormat{Alignment=StringAlignment.Center};
            g.DrawString("Time",fontLeg,br,new RectangleF(w.Left,w.Bottom+18,w.Width,14),sfC);

            // ── Axe Y (Amplitude) ─────────────────────────────────────────────
            for(int i=0;i<=V_DIVS;i+=2)
            {
                float y=w.Top+i*w.Height/V_DIVS;
                string lbl;
                if(_vdivMode)
                {
                    double vd=GetVdivForCh(ch==1?1:0);
                    double val_V=vd*(V_DIVS/2.0-i);
                    lbl=FormatVoltLabel(val_V);
                }
                else
                {
                    float zoom=ch==1?_effZoomR:_effZoomL;
                    double val=(1.0/zoom)*(1.0-2.0*i/V_DIVS);
                    lbl=Math.Abs(val)<1e-9?"0":$"{val:+0.000;-0.000}";
                }
                var sf=new StringFormat{Alignment=StringAlignment.Far,LineAlignment=StringAlignment.Center};
                g.DrawString(lbl,font,br,new RectangleF(0,y-11,MARGIN_LEFT-4,22),sf);
            }

            // Légende "Amplitude" verticale (rotée -90°) à gauche
            var state=g.Save();
            g.TranslateTransform(10f,w.Top+w.Height/2f);
            g.RotateTransform(-90f);
            using var sfA=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center};
            g.DrawString("Amplitude",fontLeg,br,new PointF(0,0),sfA);
            g.Restore(state);
        }

        private static void DrawWaveform(Graphics g,RectangleF w,float[] s,Color color,int lw,float zoom)
        {
            int dw=(int)w.Width;if(dw<2||s.Length<2) return;
            float ym=w.Top+w.Height/2f,ys=w.Height/2f*zoom;
            var pts=new PointF[dw];
            for(int px=0;px<dw;px++){float t=(float)px/dw*(s.Length-1);int lo=(int)t,hi=Math.Min(lo+1,s.Length-1);float v=s[lo]*(1f-(t-lo))+s[hi]*(t-lo);pts[px]=new PointF(w.Left+px,Math.Clamp(ym-v*ys,w.Top,w.Bottom));}
            using var pen=new Pen(color,Math.Max(1,lw));g.DrawLines(pen,pts);
        }

        private void DrawTriggerMarkers(Graphics g,RectangleF w,int trigIdx,int winSamples)
        {
            int trigCh=cmbTrigSrc.SelectedIndex;
            float zoom=trigCh==1?_effZoomR:_effZoomL;
            float ym=w.Top+w.Height/2f,ys=w.Height/2f*zoom;
            float yT=Math.Clamp(ym-(float)nudTrigLevel.Value*ys,w.Top,w.Bottom);
            Color tCol=Color.FromArgb(180,trigCh==0?TraceColorL:TraceColorR);
            using var pH=new Pen(tCol,1f){DashStyle=DashStyle.Dash};g.DrawLine(pH,w.Left,yT,w.Right,yT);
            if(_trigFound&&trigIdx>=0&&trigIdx<winSamples)
            {
                float xT=w.Left+(float)trigIdx/winSamples*w.Width;
                using var pV=new Pen(tCol,1f){DashStyle=DashStyle.Dash};g.DrawLine(pV,xT,w.Top,xT,w.Bottom);
                float ay=w.Top+4;using var brA=new SolidBrush(tCol);
                bool rising=cmbTrigEdge.SelectedIndex==0;
                g.FillPolygon(brA,rising?new[]{new PointF(xT,ay+8),new PointF(xT-4,ay),new PointF(xT+4,ay)}:new[]{new PointF(xT,ay),new PointF(xT-4,ay+8),new PointF(xT+4,ay+8)});
            }
        }

        // Panneau de mesures : style identique au DrawChartStatsPanel du Spectrum
        private void DrawMeasurements(Graphics g, RectangleF w)
        {
            // Rectangle du panneau — formule identique au DrawChartStatsPanel du Spectrum :
            //   panelLeft  = outer.Right + 8   → w.Right + 8
            //   panelRight = cr.Right - 6       → (w.Right + MEAS_W) - 6
            //   → largeur = MEAS_W - 14 ≈ 186 px (pour une fenêtre de 1138 px)
            var rect = RectangleF.FromLTRB(
                w.Right + 8,
                w.Top,
                w.Right + MEAS_W - 6,
                w.Bottom);

            // Fond blanc + bordure LightGray (identique Spectrum)
            g.FillRectangle(Brushes.White, rect);
            using var penBorder = new Pen(Color.LightGray, 1f);
            g.DrawRectangle(penBorder, rect.X, rect.Y, rect.Width, rect.Height);

            float y = rect.Y + 8f;
            int ch = cmbChannel.SelectedIndex;

            bool calOkL=false, calOkR=false; double offL=0, offR=0;
            if (GetInputCalibration != null)
            { var (vL,oL,vR,oR)=GetInputCalibration(); calOkL=vL;offL=oL;calOkR=vR;offR=oR; }

            if (ch==0||ch==2) DrawMeasSection(g,"LEFT",  Color.DodgerBlue, _measL, rect, ref y, calOkL, offL);
            if (ch==1||ch==2) DrawMeasSection(g,"RIGHT", Color.OrangeRed,  _measR, rect, ref y, calOkR, offR);

            // Section curseurs (si au moins un actif)
            if (_c1Active || _c2Active)
                y = DrawCursorMeasSection(g, rect, y, calOkL, offL, calOkR, offR);

            // Statut centré en bas du panneau, italic gris
            string statTxt=""; Color statCol=Color.Gray;
            if      (_isArmed)                     { statTxt="WAIT";          statCol=Color.DarkOrange; }
            else if (!_isRunning && _capL != null) { statTxt="🖱 scroll/pan"; statCol=Color.Gray; }
            else if (IsRollMode)                   { statTxt="ROLL";          statCol=Color.DodgerBlue; }
            else if (!_isRunning)                  { statTxt="STOPPED";       statCol=Color.Gray; }
            if (statTxt != "")
            {
                using var fStat  = new Font("Segoe UI", 7.5f, FontStyle.Italic);
                using var brStat = new SolidBrush(statCol);
                using var sfC    = new StringFormat{Alignment=StringAlignment.Center};
                g.DrawString(statTxt,fStat,brStat,
                    new RectangleF(rect.X,rect.Bottom-18,rect.Width,16),sfC);
            }
        }

        /// Panneau section canal : titre centré coloré + séparateur Gainsboro + lignes label|valeur.
        /// Polices identiques Spectrum : base=8.25pt Regular, header=Bold, strong=9.75pt Bold.
        private static void DrawMeasSection(Graphics g, string title, Color titleColor,
            MeasResult m, RectangleF rect, ref float y, bool cal, double offDb)
        {
            using var fontBase   = new Font("Segoe UI", 8.25f, FontStyle.Regular);
            using var fontHeader = new Font("Segoe UI", 8.25f, FontStyle.Bold);
            using var fontStrong = new Font("Segoe UI", 9.75f, FontStyle.Bold);
            using var brushText  = new SolidBrush(Color.DimGray);
            using var penSep     = new Pen(Color.Gainsboro, 1f);
            using var fmtCenter  = new StringFormat{Alignment=StringAlignment.Center};
            using var fmtRight   = new StringFormat{Alignment=StringAlignment.Far,
                                                     LineAlignment=StringAlignment.Near};

            float pad  = 8f;
            float xL   = rect.X + pad;
            float lineH= fontBase.GetHeight(g) + 4f;

            // Titre centré coloré
            var headerRect = new RectangleF(rect.X+pad, y, rect.Width-2*pad, fontHeader.GetHeight(g));
            using (var bTitle = new SolidBrush(titleColor))
                g.DrawString(title, fontHeader, bTitle, headerRect, fmtCenter);
            y += headerRect.Height + 4f;

            // Séparateur Gainsboro sous le titre (identique Spectrum)
            g.DrawLine(penSep, rect.X+pad, y, rect.Right-pad, y);
            y += 8f;

            // Largeur max labels pour aligner les valeurs
            float maxLW = 0f;
            foreach (string l in new[]{"Vrms","Vpp","Vdc","Freq","Per"})
            { float w2=g.MeasureString(l,fontBase).Width; if(w2>maxLW) maxLW=w2; }
            float valueLeft = xL + maxLW + 8f;
            float valueW    = rect.Right - pad - valueLeft;

            if (!m.Valid)
            {
                foreach (string lbl in new[]{"Vrms","Vpp","Vdc","Freq","Per"})
                {
                    g.DrawString(lbl, fontBase, brushText, xL, y);
                    g.DrawString("---", fontBase, brushText,
                        new RectangleF(valueLeft,y,valueW,lineH), fmtRight);
                    y += lineH;
                }
                y += 4f; return;
            }

            double sc = cal ? Math.Pow(10,offDb/20.0)*Math.Sqrt(2.0) : 1.0;
            string u  = cal ? "V" : "";
            string Fv(double v) => cal ? FormatV(v*sc)+u : $"{v:F4}";

            // Vrms en fontStrong (valeur principale mise en valeur, comme Spectrum)
            float strongH = Math.Max(fontBase.GetHeight(g), fontStrong.GetHeight(g));
            g.DrawString("Vrms", fontBase, brushText, xL, y);
            g.DrawString(Fv(m.Vrms), fontStrong, brushText,
                new RectangleF(valueLeft,y,valueW,strongH), fmtRight);
            y += strongH + 4f;

            // Autres valeurs en fontBase
            (string lbl, string val)[] rows = {
                ("Vpp",  Fv(m.Vpp)),
                ("Vdc",  Fv(m.Vdc)),
                ("Freq", m.FreqHz>0?(m.FreqHz>=1000?$"{m.FreqHz/1000:G4} kHz":$"{m.FreqHz:G4} Hz"):"---"),
                ("Per",  m.FreqHz>0?$"{1000/m.FreqHz:G4} ms":"---")
            };
            foreach (var (lbl, val) in rows)
            {
                g.DrawString(lbl, fontBase, brushText, xL, y);
                g.DrawString(val, fontBase, brushText,
                    new RectangleF(valueLeft,y,valueW,lineH), fmtRight);
                y += lineH;
            }
            y += 4f;
        }

        /// Section curseurs dans le panneau de mesures (retourne le y mis à jour).
        private float DrawCursorMeasSection(Graphics g, RectangleF rect, float y,
            bool calOkL, double offL, bool calOkR, double offR)
        {
            using var fontBase   = new Font("Segoe UI", 8.25f);
            using var fontHeader = new Font("Segoe UI", 8.25f, FontStyle.Bold);
            using var brushText  = new SolidBrush(Color.DimGray);
            using var penSep     = new Pen(Color.Gainsboro, 1f);
            using var fmtCenter  = new StringFormat{Alignment=StringAlignment.Center};
            using var fmtRight   = new StringFormat{Alignment=StringAlignment.Far,
                                                     LineAlignment=StringAlignment.Near};

            float pad   = 8f;
            float xL    = rect.X + pad;
            float lineH = fontBase.GetHeight(g) + 4f;
            float maxLW = Math.Max(g.MeasureString("Time", fontBase).Width,
                                   g.MeasureString("Level",fontBase).Width);
            float valLeft = xL + maxLW + 6f;
            float valW    = rect.Right - pad - valLeft;

            // Ligne de séparation avant les curseurs
            g.DrawLine(penSep, rect.X+pad, y, rect.Right-pad, y);
            y += 8f;

            // Évalue les deux curseurs
            var (t1,v1,ok1) = _c1Active ? EvalCursor(_c1Frac,1) : (0.0,0f,false);
            var (t2,v2,ok2) = _c2Active ? EvalCursor(_c2Frac,2) : (0.0,0f,false);

            // Calibration par curseur
            var (cal1,off1) = GetCursorCal(1);
            var (cal2,off2) = GetCursorCal(2);

            void DrawCurRow(string title, Color titleColor, double tMs, float norm,
                bool ok, bool active, bool frozen, bool calOk, double offDb)
            {
                if (!active) return;
                // Titre centré
                var hr = new RectangleF(rect.X+pad, y, rect.Width-2*pad, fontHeader.GetHeight(g));
                using var bT = new SolidBrush(titleColor);
                string titleTxt = title + (frozen?" ⏸":"");
                g.DrawString(titleTxt, fontHeader, bT, hr, fmtCenter);
                y += hr.Height + 4f;

                // Time
                g.DrawString("Time",  fontBase, brushText, xL, y);
                g.DrawString(ok ? FormatTime(tMs) : "---", fontBase, brushText,
                    new RectangleF(valLeft,y,valW,lineH), fmtRight);
                y += lineH;

                // Level
                string levStr = "---";
                if (ok)
                    levStr = calOk
                        ? FormatV(norm * Math.Pow(10,offDb/20.0) * Math.Sqrt(2.0)) + "V"
                        : $"{norm:+0.0000;-0.0000}";
                g.DrawString("Level", fontBase, brushText, xL, y);
                g.DrawString(levStr,  fontBase, brushText,
                    new RectangleF(valLeft,y,valW,lineH), fmtRight);
                y += lineH + 4f;
            }

            DrawCurRow("Cursor 1", CC1, t1, v1, ok1, _c1Active, _c1Frozen, cal1, off1);
            DrawCurRow("Cursor 2", CC2, t2, v2, ok2, _c2Active, _c2Frozen, cal2, off2);

            // Section ΔDelta
            if (_c1Active && _c2Active && ok1 && ok2)
            {
                var hr = new RectangleF(rect.X+pad, y, rect.Width-2*pad, fontHeader.GetHeight(g));
                using var bD = new SolidBrush(Color.DimGray);
                g.DrawString("ΔDelta", fontHeader, bD, hr, fmtCenter);
                y += hr.Height + 4f;
                g.DrawLine(penSep, rect.X+pad, y-2, rect.Right-pad, y-2);

                double dt = t2 - t1;
                g.DrawString("ΔTime",  fontBase, brushText, xL, y);
                g.DrawString(FormatTime(dt), fontBase, brushText,
                    new RectangleF(valLeft,y,valW,lineH), fmtRight);
                y += lineH;

                string dvStr;
                if (cal1 && cal2)
                {
                    double dv = v2*Math.Pow(10,off2/20.0)*Math.Sqrt(2.0)
                              - v1*Math.Pow(10,off1/20.0)*Math.Sqrt(2.0);
                    dvStr = FormatV(dv) + "V";
                }
                else dvStr = $"{(v2-v1):+0.0000;-0.0000}";
                g.DrawString("ΔLevel", fontBase, brushText, xL, y);
                g.DrawString(dvStr, fontBase, brushText,
                    new RectangleF(valLeft,y,valW,lineH), fmtRight);
                y += lineH;
            }
            return y;
        }

        // ─────────────────────────────────────────────────────────────────────
        public OscilloscopeSettings GetSettingsSnapshot()=>new OscilloscopeSettings
        {
            ChannelIndex=cmbChannel.SelectedIndex,TimeDivIndex=cmbTimediv.SelectedIndex,
            TrigModeIndex=cmbTrigMode.SelectedIndex,TrigEdgeIndex=cmbTrigEdge.SelectedIndex,
            TrigSrcIndex=cmbTrigSrc.SelectedIndex,TrigLevel=(double)nudTrigLevel.Value,
            ZoomYIndexL=cmbVdivL.SelectedIndex,ZoomYIndexR=cmbVdivR.SelectedIndex,
            AcCouplingL=cmbCouplingL.SelectedIndex,AcCouplingR=cmbCouplingR.SelectedIndex,
            RollMode=chkRoll.Checked,VdivMode=_vdivMode,GridModeIdx=cmbGrid.SelectedIndex
        };

        public void ApplySettings(OscilloscopeSettings? s)
        {
            if(s==null)return;
            _vdivMode=s.VdivMode; UpdateGainCombos();
            cmbChannel.SelectedIndex =Math.Clamp(s.ChannelIndex,0,2);
            cmbTimediv.SelectedIndex =Math.Clamp(s.TimeDivIndex,0,TimeDivMs.Length-1);
            cmbTrigMode.SelectedIndex=Math.Clamp(s.TrigModeIndex,0,2);
            cmbTrigEdge.SelectedIndex=Math.Clamp(s.TrigEdgeIndex,0,1);
            cmbTrigSrc.SelectedIndex =Math.Clamp(s.TrigSrcIndex,0,1);
            nudTrigLevel.Value=(decimal)Math.Clamp(s.TrigLevel,-1.0,1.0);
            int maxZ=_vdivMode?VdivValues.Length-1:ZoomY.Length-1;
            cmbVdivL.SelectedIndex=Math.Clamp(s.ZoomYIndexL,0,maxZ);
            cmbVdivR.SelectedIndex=Math.Clamp(s.ZoomYIndexR,0,maxZ);
            cmbCouplingL.SelectedIndex=Math.Clamp(s.AcCouplingL,0,1);
            cmbCouplingR.SelectedIndex=Math.Clamp(s.AcCouplingR,0,1);
            cmbGrid.SelectedIndex=Math.Clamp(s.GridModeIdx,0,2);
            chkVdiv.Checked=_vdivMode;
            UpdateRollCheckbox();
            if(chkRoll.Enabled)chkRoll.Checked=s.RollMode;
        }

        protected override void OnHandleDestroyed(EventArgs e){_timer.Stop();_timer.Dispose();base.OnHandleDestroyed(e);}
    }

    internal sealed class ScopePanel : Panel
    {
        public ScopePanel(){DoubleBuffered=true;TabStop=true;SetStyle(ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint,true);}
    }

    public sealed class OscilloscopeSettings
    {
        public int    ChannelIndex  { get; set; } = 2;
        public int    TimeDivIndex  { get; set; } = 6;
        public int    TrigModeIndex { get; set; } = 0;
        public int    TrigEdgeIndex { get; set; } = 0;
        public int    TrigSrcIndex  { get; set; } = 0;
        public double TrigLevel     { get; set; } = 0.0;
        public int    ZoomYIndexL   { get; set; } = 0;
        public int    ZoomYIndexR   { get; set; } = 0;
        public int    AcCouplingL   { get; set; } = 0;
        public int    AcCouplingR   { get; set; } = 0;
        public bool   RollMode      { get; set; } = false;
        public bool   VdivMode      { get; set; } = false;
        public int    GridModeIdx   { get; set; } = 0;
    }
}
