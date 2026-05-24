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
        private const float MEAS_W        = 165f;
        private const float MARGIN_LEFT   = 62f;
        private const float MARGIN_BOTTOM = 32f;
        private const float MARGIN_TOP    = 20f;  // zone pour la ligne d'info au-dessus de la grille
        private const float PRE_TRIG      = 0.20f;
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

        private readonly System.Windows.Forms.Timer _timer;

        private struct MeasResult { public bool Valid; public double Vpp, Vrms, Vdc, FreqHz; }

        // ─────────────────────────────────────────────────────────────────────
        public OscilloscopeControl()
        {
            InitializeComponent();

            // Bordure identique à l'onglet Spectrum
            BorderStyle = BorderStyle.FixedSingle;

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
            int pre=(int)(win*PRE_TRIG);
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
            int pre=(int)(win*PRE_TRIG);
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
            _trigPx=trigIdx>=0?trigIdx-dstart:(int)(win*PRE_TRIG);
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

            DrawMeasurements(g,wave);
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
                double tMs=IsRollMode?i*td:(i-PRE_TRIG*H_DIVS)*td;
                string lbl=(!IsRollMode&&Math.Abs(tMs)<td*0.001)?"0":FormatTime(tMs);
                var sf=new StringFormat{Alignment=StringAlignment.Center};
                g.DrawString(lbl,font,br,new RectangleF(x-38,w.Bottom+2,76,16),sf);
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
            // Rectangle : à droite du graphique, avec gap de 6px (comme Spectrum: outer.Right + 8)
            var rect = new RectangleF(w.Right + 6, w.Top, MEAS_W - 10, w.Height);

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
