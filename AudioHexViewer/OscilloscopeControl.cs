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
        private static Color NegApply(int argb, bool inv)
        { var col = Color.FromArgb(argb); return inv ? Color.FromArgb(col.A,255-col.R,255-col.G,255-col.B) : col; }
        private Color BgColor       => NegApply(_colors.BackgroundArgb, _colors.NegativeChart);
        private Color GridMajColor  => NegApply(_colors.GridArgb, _colors.NegativeChart);
        private Color GridMinColor  => NegApply(_colors.GridMinorArgb, _colors.NegativeChart);
        private Color TraceColorL   => NegApply(_colors.FftLeftArgb,  _colors.NegativeTraces);
        private Color TraceColorR   => NegApply(_colors.FftRightArgb, _colors.NegativeTraces);
        private Color InfoTextColor => NegApply(_colors.InfoTextArgb, _colors.NegativeChart);

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
        private const float HPF_FC         = 10f;   // fréquence de coupure AC (-3 dB) en Hz

        private static readonly double[] TimeDivMs =
        {
            0.001, 0.002, 0.005,        // idx  0- 2 : 1µs, 2µs, 5µs
            0.01,  0.02,  0.05,         // idx  3- 5 : 10µs, 20µs, 50µs
            0.1,   0.2,   0.5,          // idx  6- 8 : 100µs, 200µs, 500µs
            1.0,   2.0,   5.0,          // idx  9-11 : 1ms, 2ms, 5ms
            10.0,  20.0,  50.0,         // idx 12-14 : 10ms, 20ms, 50ms
            100.0, 200.0, 500.0,        // idx 15-17 : 100ms, 200ms, 500ms
            1000.0                      // idx 18    : 1 s
        };
        private const int INDEX_ROLL_MIN = 12;  // 10ms/div et plus → Roll disponible

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
        private void NudTrigLevel_KeyDown(object? s, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)   { TrigLevelValue += _trigInc; e.Handled = true; }
            if (e.KeyCode == Keys.Down) { TrigLevelValue -= _trigInc; e.Handled = true; }
            if (e.KeyCode == Keys.Enter){ TrigLevelValue  = TrigLevelValue; pnlScope.Invalidate(); e.Handled = true; e.SuppressKeyPress = true; }
        }

        private void NudTrigLevel_Leave(object? s, EventArgs e)
        {
            TrigLevelValue = TrigLevelValue;   // reformat + clamp
            pnlScope.Invalidate();
        }

        private bool AcL => cmbCouplingL.SelectedIndex == 1;
        private bool AcR => cmbCouplingR.SelectedIndex == 1;

        // ── Trigger level TextBox helpers ──────────────────────────────────
        private decimal _trigMin = -1m, _trigMax = 1m, _trigInc = 0.001m;
        private int     _trigDp  = 3;

        private decimal TrigLevelValue
        {
            get
            {
                if (decimal.TryParse(nudTrigLevel.Text,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.CurrentCulture, out decimal v))
                    return Math.Clamp(v, _trigMin, _trigMax);
                if (decimal.TryParse(nudTrigLevel.Text,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal v2))
                    return Math.Clamp(v2, _trigMin, _trigMax);
                return 0m;
            }
            set
            {
                decimal clamped = Math.Clamp(value, _trigMin, _trigMax);
                nudTrigLevel.Text = clamped.ToString("F" + _trigDp,
                    System.Globalization.CultureInfo.CurrentCulture);
            }
        }

        private void SetTrigRange(decimal min, decimal max, decimal inc, int dp)
        {
            _trigMin = min; _trigMax = max; _trigInc = inc; _trigDp = dp;
        }

        private int  GridMode => Math.Clamp(cmbGrid.SelectedIndex, 0, 2); // 0=Full 1=Axes 2=None

        // ── État ──────────────────────────────────────────────────────────────
        private bool _isRunning, _isArmed, _lastTrigOk;
        private bool _rollActive = false, _rollEnabled = false;
        private long _holdoffEndAbs = 0;   // AbsWrite position jusqu'où le hold-off est actif

        // ── Acquisition progress (AcqButtonPainter) ───────────────────────────
        private int    _acqPct     = 0;
        private bool   _acqVisible = false;
        private string _acqTimeStr = "";

        public void SetAcqProgress(int pct, bool visible, string timeStr)
        {
            _acqPct     = pct;
            _acqVisible = visible;
            _acqTimeStr = timeStr;
            btnRun.Invalidate();
        }

        private readonly System.Diagnostics.Stopwatch _acqSw = new();
        private System.Windows.Forms.Timer?           _acqUiTimer;
        private double _acqPeriodSec = 0.0;

        private void StartAcqTicker()
        {
            int td = Math.Clamp(cmbTimediv.SelectedIndex, 0, TimeDivMs.Length - 1);
            double periodSec = TimeDivMs[td] * H_DIVS / 1000.0;
            _acqPeriodSec = periodSec;
            _acqSw.Restart();
            bool show = periodSec >= 1.0;
            string timeStr = show ? $"≈ {periodSec:F2} s" : $"≈ {periodSec * 1000:F0} ms";
            SetAcqProgress(0, show, timeStr);

            if (_acqUiTimer == null)
            {
                _acqUiTimer = new System.Windows.Forms.Timer { Interval = 50 };
                _acqUiTimer.Tick += (_, _) => TickAcqProgress();
            }
            _acqUiTimer.Start();
        }

        private void StopAcqTicker()
        {
            _acqUiTimer?.Stop();
            _acqSw.Reset();
            SetAcqProgress(100, false, _acqTimeStr);
        }

        private void TickAcqProgress()
        {
            if (_acqPeriodSec <= 0) return;
            double p = Math.Clamp(_acqSw.Elapsed.TotalSeconds / _acqPeriodSec, 0.0, 1.0);
            int pct = (int)Math.Round(p * 100.0);
            SetAcqProgress(pct, _acqVisible, _acqTimeStr);
            if (pct >= 100) _acqSw.Restart();
        }
        private bool _isExporting;   // true pendant DrawToBitmap → fond blanc
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
        // État du filtre passe-haut RII 1er ordre (couplage AC)
        // y[n] = α × (y[n-1] + x[n] - x[n-1])   avec α = τ/(τ+1/sr)
        private double _hpXprevL, _hpXprevR;   // x[n-1]
        private double _hpYprevL, _hpYprevR;   // y[n-1]

        // ── Curseurs X / Y ───────────────────────────────────────────────────
        // Canal  : 0=aucun  1=L (couleur voie L)  2=R (couleur voie R)
        private int  _cursorChan  = 1;  // synced with _scaleChannel (1=L, 2=R)
        private bool _xActive     = true;    // lignes verticales X1/X2
        private bool _yActive     = false;   // lignes horizontales Y1/Y2
        private bool _linkedX     = false;   // X1+X2 déplacés ensemble
        private bool _linkedY     = false;   // Y1+Y2 déplacés ensemble
        private bool _lblX        = false;   // étiquettes ΔX sur le graphe
        private bool _lblY        = false;   // étiquettes ΔY sur le graphe
        // Positions X [0,1] dans la fenêtre temporelle
        private float _x1Frac = 1f/3f, _x2Frac = 2f/3f;
        // Positions Y [0,1] dans la fenêtre verticale (0=bas, 1=haut)
        private float _y1Frac = 1f/3f, _y2Frac = 2f/3f;
        // Drag : 0=aucun 1=X1 2=X2 3=Y1 4=Y2 5=lbl-X1 6=lbl-X2 7=lbl-ΔX 8=lbl-Y1 9=lbl-Y2 10=lbl-ΔY
        private int _cursorDragging = 0;
        private bool _yTrack      = false;  // Y cursors track signal at X positions
        // Positions des étiquettes (fraction le long de l'axe du curseur)
        private float _x1LblFracY=0.85f, _x2LblFracY=0.85f, _dxLblFracX=0.50f;  // ΔX moves horizontally
        private float _y1LblFracX=0.04f, _y2LblFracX=0.04f, _dyLblFracY=0.50f;  // ΔY moves vertically
        // Rectangles d'étiquettes mis à jour au dessin (hit detection)
        private RectangleF _x1LblRect, _x2LblRect, _dxLblRect;
        private RectangleF _y1LblRect, _y2LblRect, _dyLblRect;
        // Voie curseur visible à l'écran (couplage non OFF)
        private bool CursorChanVisible =>
            _cursorChan==1 ? cmbCouplingL.SelectedIndex<2 :
            _cursorChan==2 ? cmbCouplingR.SelectedIndex<2 : false;
        // Couleur selon voie active
        private Color CursorColor
            => _cursorChan == 1 ? TraceColorL : _cursorChan == 2 ? TraceColorR : Color.Gray;

        // ── Décalage vertical des courbes ────────────────────────────────────
        // offset en unités normalisées : 0 = centre, +1 = monte d'une demi-hauteur × zoom
        private float _offsetL = 0f, _offsetR = 0f;
        private float _hystEma = 0.002f;  // EMA hystérésis → stabilise la position du trigger
        private long  _lastTrigAbsPos = -1; // position absolue du dernier déclenchement affiché
        private int   _draggingOffset    = 0;   // 0=aucun 1=L 2=R
        private bool  _draggingTrigger   = false;
        private bool  _draggingTrigPos   = false;
        private bool  _draggingMinimap   = false;
        private int   _scaleChannel      = 0;   // 0=L  1=R  → quelle voie commande l'échelle Y
        private int   _minimapClickOffset= 0;   // offset du clic dans la barre bleue (en samples)

        private readonly System.Windows.Forms.Timer _timer;

        private struct MeasResult { public bool Valid; public double Vpp, Vrms, Vdc, FreqHz; }

        // ─────────────────────────────────────────────────────────────────────
        public OscilloscopeControl()
        {
            InitializeComponent();

            // Bordure noire 1px autour de TOUT l'onglet (commandes + graphique + bouton RUN).
            // Technique du 24 mai : BackColor=Black visible dans le Padding=1 non couvert
            // par les enfants Dock. panelTop.BackColor=Control évite que la barre de contrôles
            // devienne noire.
            BackColor = Color.Black;
            Padding   = new Padding(1);
            // ── Bordure blanche 2px ───────────────────────────────────
            { var bc = Color.White;
              panelTop.Controls.Add(new Panel { Dock = DockStyle.Top,    Height = 2, BackColor = bc });
              panelTop.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 2, BackColor = bc });
              panelTop.Controls.Add(new Panel { Dock = DockStyle.Left,   Width  = 2, BackColor = bc });
              panelTop.Controls.Add(new Panel { Dock = DockStyle.Right,  Width  = 2, BackColor = bc }); }


            _timer = new System.Windows.Forms.Timer { Interval = 33 };
            _timer.Tick += Timer_Tick;

            btnRun.Click += BtnRun_Click;
            btnRun.Paint += (s, e) =>
                AcqButtonPainter.Paint(e.Graphics, (Button)s!, _acqPct, _acqVisible, _acqTimeStr);

            cmbTimediv.SelectedIndexChanged += (_, _) =>
            { UpdateRollCheckbox(); if (_isRunning) { pnlScope.Invalidate(); StartAcqTicker(); } else RefreshStoppedView(); };

            // Couplage (DC/AC/OFF) → redraw uniquement, PAS de UpdateGainCombos
            // (UpdateGainCombos réinitialise les indices gain → bug ×500)
            cmbCouplingL.SelectedIndexChanged += (_, _) => { UpdateCursorUI(); if (!_isRunning) RefreshStoppedView(); else pnlScope.Invalidate(); };
            cmbCouplingR.SelectedIndexChanged += (_, _) => { UpdateCursorUI(); if (!_isRunning) RefreshStoppedView(); else pnlScope.Invalidate(); };
            cmbTrigMode.SelectedIndexChanged  += (_, _) => { };
            cmbTrigEdge.SelectedIndexChanged  += (_, _) => { };
            cmbTrigSrc.SelectedIndexChanged   += (_, _) => { if (_vdivMode) UpdateTrigLevelRange(); };
            nudTrigLevel.TextChanged          += (_, _) => pnlScope.Invalidate();
            nudTrigLevel.KeyDown              += NudTrigLevel_KeyDown;
            nudTrigLevel.Leave                += NudTrigLevel_Leave;
            nudTrigPos.ValueChanged           += (_, _) => { if (!_isRunning) RefreshStoppedView(); else pnlScope.Invalidate(); };
            cmbVdivL.SelectedIndexChanged     += (_, _) => { if (_vdivMode) UpdateTrigLevelRange(); pnlScope.Invalidate(); };
            cmbVdivR.SelectedIndexChanged     += (_, _) => { if (_vdivMode) UpdateTrigLevelRange(); pnlScope.Invalidate(); };
            cmbGrid.SelectedIndexChanged      += (_, _) => pnlScope.Invalidate();
            btnRoll.Click += (_, _) => { if(_rollEnabled){_rollActive=!_rollActive;UpdateRollButton();}  };
            btnVdiv.Click += (_, _) => { _vdivMode=!_vdivMode; UpdateVdivButton(); ChkVdivLogic(); };

            pnlScope.Paint      += PnlScope_Paint;
            pnlScope.Resize     += (_, _) => pnlScope.Invalidate();
            pnlScope.MouseEnter += (_, _) => { if (!_isRunning) pnlScope.Focus(); };
            pnlScope.MouseWheel += PnlScope_MouseWheel;

            // ── Curseurs X / Y ────────────────────────────────────────────────
            btnCurX.Click    += (_, _) => { _xActive=!_xActive; if(!(_xActive&&_yActive))_yTrack=false; UpdateCursorUI(); pnlScope.Invalidate(); };
            btnCurY.Click    += (_, _) => { _yActive=!_yActive; if(!(_xActive&&_yActive))_yTrack=false; UpdateCursorUI(); pnlScope.Invalidate(); };
            btnLinkedX.Click += (_, _) => { _linkedX=!_linkedX; UpdateCursorUI(); };
            btnLinkedY.Click += (_, _) => { _linkedY=!_linkedY; UpdateCursorUI(); };
            btnSetX.Click    += (_, _) => OpenSetCursorDialog(isX: true);
            btnSetY.Click    += (_, _) => OpenSetCursorDialog(isX: false);
            // Labels = show/hide both ΔX and ΔY labels together
            btnLblX.Click    += (_, _) => { _lblX=!_lblX; _lblY=_lblX; UpdateCursorUI(); pnlScope.Invalidate(); };
            // Y track = Y cursors follow signal amplitude at X positions
            btnLblY.Click    += (_, _) =>
            { if(!(_xActive&&_yActive&&_cursorChan>0)) return;
              _yTrack=!_yTrack; UpdateCursorUI(); pnlScope.Invalidate(); };

            pnlScope.MouseDown        += PnlScope_CursorMouseDown;
            pnlScope.MouseDown        += PnlScope_OffsetMouseDown;
            pnlScope.MouseMove        += PnlScope_CursorMouseMove;
            pnlScope.MouseMove        += PnlScope_OffsetMouseMove;
            pnlScope.MouseUp          += (_, _) => { _cursorDragging = 0; _draggingOffset = 0; _draggingMinimap = false; _draggingTrigger = false; _draggingTrigPos = false; };
            pnlScope.MouseDown        += PnlScope_TrigHandleMouseDown;
            pnlScope.MouseMove        += PnlScope_TrigHandleMouseMove;
            pnlScope.MouseDown        += PnlScope_TrigPosMouseDown;
            pnlScope.MouseMove        += PnlScope_TrigPosMouseMove;
            pnlScope.MouseDown        += PnlScope_MinimapMouseDown;
            pnlScope.MouseMove        += PnlScope_MinimapMouseMove;
            pnlScope.MouseDoubleClick += PnlScope_OffsetDblClick;

            btnScaleL.Click += (_, _) => { _scaleChannel=0; _cursorChan=1; UpdateScaleButtons(); UpdateCursorUI(); pnlScope.Invalidate(); };
            btnScaleR.Click += (_, _) => { _scaleChannel=1; _cursorChan=2; UpdateScaleButtons(); UpdateCursorUI(); pnlScope.Invalidate(); };

            UpdateCursorUI();
            UpdateScaleButtons();
            UpdateRollButton();
            UpdateVdivButton();

            UpdateRollCheckbox();
            // Séparateurs visuels entre groupes (1px silver dans les gaps)
            foreach (int sx in new[] { 244, 363, 563, 680, 934 })
                panelTop.Controls.Add(new Label {
                    Location  = new System.Drawing.Point(sx, 6),
                    Size      = new System.Drawing.Size(1, 78),
                    BackColor = System.Drawing.Color.Silver,
                    Text      = ""
                });
            // Cursor separator: centered between SetX(right=894) and Labels(left=907)
            // Center = (894+907)/2 = 900, gap = 6px each side
            // Height = button height only: y=26 (top row1) to y=80 (bottom row2) → h=54
            panelTop.Controls.Add(new Label {
                Location  = new System.Drawing.Point(855, 26),
                Size      = new System.Drawing.Size(1, 54),
                BackColor = System.Drawing.Color.Silver,
                Text      = ""
            });
            panelTop.Controls.Remove(lblTrigStatus);  // shown via canvas only
            SetRunState(false);
        }

        // ─────────────────────────────────────────────────────────────────────
        public void ApplySpectrumColorSettings(SpectrumColorSettings? c)
        { _colors = c ?? SpectrumColorSettings.Defaults(); _colors.Normalize(); UpdateCursorUI(); UpdateScaleButtons(); pnlScope.Invalidate(); }
        public void UpdateSampleRate(int sr) { _lastSR = sr; UpdateInfoLine(); }

        private bool IsAutoMode   => cmbTrigMode.SelectedIndex == 0;
        private bool IsSingleMode => cmbTrigMode.SelectedIndex == 2;
        private bool IsRollMode   => _rollActive && _rollEnabled;

        private void UpdateRollCheckbox()
        {
            bool can = cmbTimediv.SelectedIndex >= INDEX_ROLL_MIN;
            _rollEnabled = can;
            if (!can) _rollActive = false;
            UpdateRollButton();
        }
        private void UpdateRollButton()
        {
            btnRoll.Enabled   = _rollEnabled;
            btnRoll.BackColor = _rollActive ? Color.LightGreen : SystemColors.Control;
            btnRoll.ForeColor = _rollActive ? Color.Black     : SystemColors.ControlText;
            btnRoll.UseVisualStyleBackColor = !_rollActive;
        }
        private void UpdateVdivButton()
        {
            btnVdiv.BackColor = _vdivMode ? Color.LightGreen : SystemColors.Control;
            btnVdiv.ForeColor = _vdivMode ? Color.Black     : SystemColors.ControlText;
            btnVdiv.UseVisualStyleBackColor = !_vdivMode;
        }

        // ── Helpers curseurs ──────────────────────────────────────────────────
        // Voie(s) actives déduites des combos COUP. : DC(0)/AC(1) = actif, OFF(2) = éteint
        // Retourne 0=Left, 1=Right, 2=L+R  (compatible avec l'ancien GetCh())
        // Renvoie le niveau de trigger en unités normalisées (-1…+1)
        // Si V/div actif : convertit la valeur en Volts affichée dans le NUD → normalisé
        private float GetTrigLevelNormalized()
        {
            if (!_vdivMode) return (float)TrigLevelValue;
            // Samples dans le ring buffer = float IEEE ±1 (pleine échelle dBFS).
            // calScale = Volts correspondant à sample=1.0
            // Donc : trigNorm = trigV / calScale
            int ch = cmbTrigSrc.SelectedIndex;
            if (GetInputCalibration == null) return 0f;
            var (vL, oL, vR, oR) = GetInputCalibration();
            bool calOk = ch == 1 ? vR : vL;
            if (!calOk) return 0f;
            double offDb    = ch == 1 ? oR : oL;
            double calScale = Math.Pow(10, offDb / 20.0) * Math.Sqrt(2.0);
            return calScale > 0 ? (float)((double)TrigLevelValue / calScale) : 0f;
        }

        // Met à jour plage/incrément/valeur du NUD trigger selon le mode V/div
        // isConversion=true : bascule entre modes (checkbox V/div cochée/décochée)
        //                     → convertit la valeur avec calScale
        // isConversion=false : reste dans le même mode, juste reclamper
        // isConversion=true  : bascule V/div ON↔OFF
        //   norm→V : trigV    = trigNorm * calScale
        //   V→norm : trigNorm = trigV    / calScale
        // isConversion=false : même mode, reclamper seulement
        private void UpdateTrigLevelRange(bool isConversion = false)
        {
            int    ch        = cmbTrigSrc.SelectedIndex;
            double vd        = GetVdivForCh(ch);
            double halfRange = vd * (V_DIVS / 2.0);

            if (_vdivMode)
            {
                if (halfRange < 1e-9) return;
                int dp  = halfRange >= 1.0 ? 3 : halfRange >= 0.01 ? 4 : 5;
                decimal inc = (decimal)Math.Max(halfRange / 200.0, 0.00001);
                SetTrigRange(-(decimal)halfRange, (decimal)halfRange, inc, dp);

                decimal newVal;
                if (isConversion) {
                    double cs = GetCalScaleForTrigCh(ch);
                    newVal = cs > 0
                        ? Math.Clamp((decimal)((double)TrigLevelValue * cs), _trigMin, _trigMax)
                        : Math.Clamp(TrigLevelValue * (decimal)halfRange, _trigMin, _trigMax);
                } else {
                    newVal = Math.Clamp(TrigLevelValue, _trigMin, _trigMax);
                }
                TrigLevelValue = newVal;
            }
            else
            {
                SetTrigRange(-1m, 1m, 0.001m, 3);
                decimal norm;
                if (isConversion) {
                    double cs = GetCalScaleForTrigCh(ch);
                    norm = cs > 0
                        ? Math.Clamp((decimal)((double)TrigLevelValue / cs), -1m, 1m)
                        : Math.Clamp(TrigLevelValue / (decimal)halfRange, -1m, 1m);
                } else {
                    norm = Math.Clamp(TrigLevelValue, -1m, 1m);
                }
                TrigLevelValue = norm;
            }
        }

        private double GetCalScaleForTrigCh(int ch)
        {
            if (GetInputCalibration == null) return 0;
            var (vL, oL, vR, oR) = GetInputCalibration();
            bool calOk = ch == 1 ? vR : vL;
            if (!calOk) return 0;
            double offDb = ch == 1 ? oR : oL;
            return Math.Pow(10, offDb / 20.0) * Math.Sqrt(2.0);
        }

                private int GetCh()
        {
            bool l = cmbCouplingL.SelectedIndex < 2;
            bool r = cmbCouplingR.SelectedIndex < 2;
            if (l && r) return 2;
            if (l)      return 0;
            if (r)      return 1;
            return -1; // les deux OFF → rien à afficher (aucune condition ch==0/1/2 ne matche)
        }

                private void UpdateScaleButtons()
        {
            // Style identique Spectrum/C1/C2 : vert si actif, gris si inactif
            btnScaleL.BackColor               = _scaleChannel==0 ? TraceColorL   : SystemColors.Control;
            btnScaleL.ForeColor               = _scaleChannel==0 ? Color.White   : SystemColors.ControlText;
            btnScaleL.UseVisualStyleBackColor  = _scaleChannel!=0;
            btnScaleR.BackColor               = _scaleChannel==1 ? TraceColorR   : SystemColors.Control;
            btnScaleR.ForeColor               = _scaleChannel==1 ? Color.White   : SystemColors.ControlText;
            btnScaleR.UseVisualStyleBackColor  = _scaleChannel!=1;
        }

        private void UpdateCursorUI()
        {
            void StyleBtn(Button b, bool on, Color onBack)
            { b.BackColor=on?onBack:SystemColors.Control;
              b.ForeColor=on?(onBack==Color.LightGreen?Color.Black:Color.White):SystemColors.ControlText;
              b.UseVisualStyleBackColor=!on;
              if(b.FlatStyle==FlatStyle.Flat)
                b.FlatAppearance.BorderColor=on?Color.FromArgb(80,160,80):SystemColors.ControlDark; }
            // CursorChan is driven by btnScaleL/R (no separate L/R cursor buttons)
            StyleBtn(btnCurX, _xActive, Color.LightGreen);
            StyleBtn(btnCurY, _yActive, Color.LightGreen);
            StyleBtn(btnLinkedX, _linkedX, Color.LightGreen);
            StyleBtn(btnLinkedY, _linkedY, Color.LightGreen);
            StyleBtn(btnLblX, _lblX, Color.LightGreen);   // Labels
            StyleBtn(btnLblY, _yTrack, Color.LightGreen); // Y track
            bool hasChan = _cursorChan > 0;
            btnCurX.Enabled = btnCurY.Enabled = hasChan;
            btnLinkedX.Enabled = hasChan && _xActive;
            btnLinkedY.Enabled = hasChan && _yActive;
            btnSetX.Enabled    = hasChan && _xActive;
            btnSetY.Enabled    = hasChan && _yActive;
            // Labels: enabled if any cursor active
            btnLblX.Enabled    = hasChan && (_xActive||_yActive);
            // Y track: only when BOTH X and Y cursors are active
            btnLblY.Enabled    = hasChan && _xActive && _yActive;
            if (!(_xActive&&_yActive)) _yTrack=false;
        }

        private RectangleF GetWaveRect()
        {
            var sz = pnlScope.ClientSize;
            if (sz.Width<60||sz.Height<60) return RectangleF.Empty;
            return new RectangleF(MARGIN_LEFT, MARGIN_TOP,
                sz.Width-MARGIN_LEFT-MEAS_W, sz.Height-MARGIN_TOP-MARGIN_BOTTOM);
        }

        // ── Helpers curseurs X/Y ─────────────────────────────────────────────
        private float[]?             GetActiveCursorData()
            => _cursorChan==1?_dispL : _cursorChan==2?_dispR : null;
        private float                GetActiveZoom()
            => _cursorChan==1?_effZoomL : _cursorChan==2?_effZoomR : 1f;
        private float                GetActiveOffset()
            => _cursorChan==1?_offsetL : _cursorChan==2?_offsetR : 0f;
        private (bool ok, double db) GetActiveCal()
        {
            if (GetInputCalibration==null) return (false,0);
            var (vL,oL,vR,oR)=GetInputCalibration();
            if(_cursorChan==1) return (vL,oL);
            if(_cursorChan==2) return (vR,oR);
            return (false,0);
        }
        // Frac X [0,1] → temps ms relatif au trigger
        private double XFracToMs(float frac)
        { int td=Math.Clamp(cmbTimediv.SelectedIndex,0,TimeDivMs.Length-1);
          return (frac-PreTrig)*TimeDivMs[td]*H_DIVS; }
        // Temps ms → frac X
        private float MsToXFrac(double tMs)
        { int td=Math.Clamp(cmbTimediv.SelectedIndex,0,TimeDivMs.Length-1);
          double range=TimeDivMs[td]*H_DIVS;
          return (float)Math.Clamp(tMs/range+PreTrig,0.0,1.0); }
        // Frac Y [0,1] → valeur normalisée (0=bas, 1=haut, tient compte offset)
        private float YFracToNorm(float frac)
        { float zoom=GetActiveZoom(); float off=GetActiveOffset();
          return (2f*frac-1f-off)/zoom; }
        // Valeur normalisée → frac Y
        private float NormToYFrac(float normVal)
        { float zoom=GetActiveZoom(); float off=GetActiveOffset();
          return Math.Clamp((1f+off+normVal*zoom)/2f,0f,1f); }
        // Évalue un curseur X : temps + niveau signal interpolé
        private (double timeMs, float normVal, bool valid) EvalXCursor(float frac)
        {
            double tMs=XFracToMs(frac);
            float[]? dat=GetActiveCursorData();
            if(dat==null||dat.Length<2) return (tMs,0f,false);
            float t=frac*(dat.Length-1); int lo=(int)t,hi=Math.Min(lo+1,dat.Length-1);
            return (tMs, dat[lo]*(1f-(t-lo))+dat[hi]*(t-lo), true);
        }

        // ── Souris curseurs X/Y ───────────────────────────────────────────────
        private void PnlScope_CursorMouseDown(object? s, MouseEventArgs e)
        {
            if (e.Button!=MouseButtons.Left || _cursorChan==0) return;
            var w=GetWaveRect(); if(w.IsEmpty) return;
            const float T=10f;
            if (_xActive)
            {
                float x1px=w.Left+_x1Frac*w.Width, x2px=w.Left+_x2Frac*w.Width;
                if(Math.Abs(e.X-x1px)<=T){_cursorDragging=1;return;}
                if(Math.Abs(e.X-x2px)<=T){_cursorDragging=2;return;}
            }
            if (_yActive && !_yTrack)  // Y drag blocked in track mode
            {
                float y1py=w.Bottom-_y1Frac*w.Height, y2py=w.Bottom-_y2Frac*w.Height;
                if(Math.Abs(e.Y-y1py)<=T){_cursorDragging=3;return;}
                if(Math.Abs(e.Y-y2py)<=T){_cursorDragging=4;return;}
            }
            // Label drag detection (bounding rects updated during draw)
            if(_xActive){if(_x1LblRect.Contains(e.Location)){_cursorDragging=5;return;}
                          if(_x2LblRect.Contains(e.Location)){_cursorDragging=6;return;}
                          if(_dxLblRect.Contains(e.Location)){_cursorDragging=7;return;}}
            if(_yActive){if(_y1LblRect.Contains(e.Location)){_cursorDragging=8;return;}
                          if(_y2LblRect.Contains(e.Location)){_cursorDragging=9;return;}
                          if(_dyLblRect.Contains(e.Location)){_cursorDragging=10;return;}}
        }

        private void PnlScope_CursorMouseMove(object? s, MouseEventArgs e)
        {
            var w=GetWaveRect(); if(w.IsEmpty) return;
            if (_cursorDragging>0)
            {
                if (_cursorDragging<=2)
                {
                    float f=Math.Clamp((e.X-w.Left)/w.Width,0f,1f);
                    if(_cursorDragging==1){float d=f-_x1Frac;_x1Frac=f;if(_linkedX)_x2Frac=Math.Clamp(_x2Frac+d,0f,1f);}
                    else                 {float d=f-_x2Frac;_x2Frac=f;if(_linkedX)_x1Frac=Math.Clamp(_x1Frac+d,0f,1f);}
                }
                else if(_cursorDragging<=4)  // Y cursor lines
                {
                    float f=Math.Clamp((w.Bottom-e.Y)/w.Height,0f,1f);
                    if(_cursorDragging==3){float d=f-_y1Frac;_y1Frac=f;if(_linkedY)_y2Frac=Math.Clamp(_y2Frac+d,0f,1f);}
                    else                 {float d=f-_y2Frac;_y2Frac=f;if(_linkedY)_y1Frac=Math.Clamp(_y1Frac+d,0f,1f);}
                }
                else if(_cursorDragging<=7)  // labels X (5=x1lbl, 6=x2lbl, 7=ΔX horiz)
                {
                    float f=_cursorDragging==7
                        ? Math.Clamp((e.X-w.Left)/w.Width,0f,1f)  // ΔX: horizontal
                        : Math.Clamp((e.Y-w.Top)/w.Height,0f,1f); // x1/x2 labels: vertical
                    if(_cursorDragging==5)_x1LblFracY=f;
                    else if(_cursorDragging==6)_x2LblFracY=f;
                    else _dxLblFracX=f;
                }
                else                          // labels Y (8=y1lbl, 9=y2lbl, 10=ΔY vert)
                {
                    float f=_cursorDragging==10
                        ? Math.Clamp((e.Y-w.Top)/w.Height,0f,1f)  // ΔY: vertical
                        : Math.Clamp((e.X-w.Left)/w.Width,0f,1f); // y1/y2 labels: horizontal
                    if(_cursorDragging==8)_y1LblFracX=f;
                    else if(_cursorDragging==9)_y2LblFracX=f;
                    else _dyLblFracY=f;
                }
                pnlScope.Invalidate(); return;
            }
            const float T=10f;
            bool nearX=_xActive&&_cursorChan>0&&
                (Math.Abs(e.X-(w.Left+_x1Frac*w.Width))<=T||Math.Abs(e.X-(w.Left+_x2Frac*w.Width))<=T);
            bool nearY=_yActive&&_cursorChan>0&&
                (Math.Abs(e.Y-(w.Bottom-_y1Frac*w.Height))<=T||Math.Abs(e.Y-(w.Bottom-_y2Frac*w.Height))<=T);
            bool nearXLbl=_xActive&&(_x1LblRect.Contains(e.Location)||_x2LblRect.Contains(e.Location)||_dxLblRect.Contains(e.Location));
            bool nearYLbl=_yActive&&(_y1LblRect.Contains(e.Location)||_y2LblRect.Contains(e.Location)||_dyLblRect.Contains(e.Location));
            bool nearDxLbl=_xActive&&_dxLblRect.Contains(e.Location);
            bool nearDyLbl=_yActive&&_dyLblRect.Contains(e.Location);
            bool nearXLbl2=_xActive&&(_x1LblRect.Contains(e.Location)||_x2LblRect.Contains(e.Location));
            bool nearYLbl2=_yActive&&(_y1LblRect.Contains(e.Location)||_y2LblRect.Contains(e.Location));
            pnlScope.Cursor=nearX?Cursors.SizeWE:nearY?Cursors.SizeNS
                :nearDxLbl?Cursors.SizeWE:nearDyLbl?Cursors.SizeNS
                :nearXLbl2?Cursors.SizeNS:nearYLbl2?Cursors.SizeWE:Cursors.Default;
        }

        // ── Dessin des curseurs ───────────────────────────────────────────────
        private void DrawCursors(Graphics g, RectangleF w)
        {
            if (_cursorChan==0 || !CursorChanVisible) return;
            // Y track: move Y cursors to signal amplitude at X positions
            if (_yTrack && _xActive && _yActive)
            {
                float[]? td=GetActiveCursorData();
                if(td!=null&&td.Length>=2)
                {
                    float t1=_x1Frac*(td.Length-1); int l1=(int)t1,h1=Math.Min(l1+1,td.Length-1);
                    float t2=_x2Frac*(td.Length-1); int l2=(int)t2,h2=Math.Min(l2+1,td.Length-1);
                    float v1=td[l1]*(1f-(t1-l1))+td[h1]*(t1-l1);
                    float v2=td[l2]*(1f-(t2-l2))+td[h2]*(t2-l2);
                    _y1Frac=NormToYFrac(v1); _y2Frac=NormToYFrac(v2);
                }
            }
            Color col=CursorColor;
            using var penX=new Pen(Color.FromArgb(210,col),1.5f){DashStyle=DashStyle.Custom};
            penX.DashPattern=new float[]{8f,4f};   // X: longs tirets
            using var penY=new Pen(Color.FromArgb(210,col),1.5f){DashStyle=DashStyle.Custom};
            penY.DashPattern=new float[]{1.5f,3f}; // Y: pointillés courts
            using var font=new Font("Segoe UI",8f,FontStyle.Bold);
            using var brL =new SolidBrush(col);
            if (_xActive)
            {
                DrawXCursorLine(g,w,_x1Frac,"X1",col,penX,font,brL,_x1LblFracY,out _x1LblRect);
                DrawXCursorLine(g,w,_x2Frac,"X2",col,penX,font,brL,_x2LblFracY,out _x2LblRect);
                if (_lblX)
                {
                    double t1=XFracToMs(_x1Frac),t2=XFracToMs(_x2Frac),dt=t2-t1;
                    string dts=FormatTime(Math.Abs(dt));
                    string fs="---";
                    if(Math.Abs(dt)>1e-9){double hz=1000.0/Math.Abs(dt);fs=hz>=1000?$"{hz/1000:G4} kHz":$"{hz:G4} Hz";}
                    using var fDx=new Font("Segoe UI",9f,FontStyle.Bold);
                    string txt=$"\u0394X= {dts}   1/\u0394X= {fs}";
                    var sz=g.MeasureString(txt,fDx);
                    float x1px=w.Left+_x1Frac*w.Width, x2px=w.Left+_x2Frac*w.Width;
                    float lx=Math.Clamp(x1px+_dxLblFracX*(x2px-x1px)-sz.Width/2f, w.Left, w.Right-sz.Width);
                    float ly=w.Top+18f;  // just below acquisition minimap
                    _dxLblRect=new RectangleF(lx-2,ly,sz.Width+4,sz.Height+2);
                    using var brBgDx=new SolidBrush(Color.FromArgb(210, BgColor));
                    g.FillRectangle(brBgDx,_dxLblRect);
                    g.DrawString(txt,fDx,brL,lx,ly);
                }
            }
            if (_yActive)
            {
                DrawYCursorLine(g,w,_y1Frac,"Y1",col,penY,font,brL,_y1LblFracX,out _y1LblRect);
                DrawYCursorLine(g,w,_y2Frac,"Y2",col,penY,font,brL,_y2LblFracX,out _y2LblRect);
                if (_lblY)
                {
                    float n1=YFracToNorm(_y1Frac),n2=YFracToNorm(_y2Frac);
                    var (cok,odb)=GetActiveCal();
                    string dy;
                    if(cok){double sc=Math.Pow(10,odb/20.0)*Math.Sqrt(2.0);dy=FormatV(Math.Abs((double)(n2-n1))*sc);}
                    else dy=$"{Math.Abs(n2-n1):G4}";
                    using var fDy=new Font("Segoe UI",9f,FontStyle.Bold);
                    string txt=$"\u0394Y= {dy}";
                    var sz=g.MeasureString(txt,fDy);
                    float y1py=w.Bottom-_y1Frac*w.Height, y2py=w.Bottom-_y2Frac*w.Height;
                    float lx=w.Left+8f;  // fixed near left
                    float ly=Math.Clamp(y2py+_dyLblFracY*(y1py-y2py)-sz.Height/2f, w.Top, w.Bottom-sz.Height);
                    _dyLblRect=new RectangleF(lx-2,ly,sz.Width+4,sz.Height+2);
                    using var brBgDy=new SolidBrush(Color.FromArgb(210, BgColor));
                    g.FillRectangle(brBgDy,_dyLblRect);
                    g.DrawString(txt,fDy,brL,lx,ly);
                }
            }
        }

        private void DrawXCursorLine(Graphics g,RectangleF w,float frac,string lbl,
            Color col,Pen pen,Font font,SolidBrush brL,float lblFracY,out RectangleF lblRect)
        {
            float cx=w.Left+frac*w.Width;
            g.DrawLine(pen,cx,w.Top,cx,w.Bottom);
            float[]? dat=GetActiveCursorData();
            if(dat!=null&&dat.Length>=2)
            {
                float zoom=GetActiveZoom(),off=GetActiveOffset();
                float t=frac*(dat.Length-1);int lo=(int)t,hi=Math.Min(lo+1,dat.Length-1);
                float val=dat[lo]*(1f-(t-lo))+dat[hi]*(t-lo);
                float ym=w.Top+w.Height/2f,ys=w.Height/2f*zoom;
                float yRef=ym-off*w.Height/2f;
                float yPos=Math.Clamp(yRef-val*ys,w.Top,w.Bottom);
                float d=6f, dh=d+3.5f;
                // Halo semi-transparent BgColor for visibility
                var ptsH=new[]{new PointF(cx,yPos-dh),new PointF(cx+dh,yPos),new PointF(cx,yPos+dh),new PointF(cx-dh,yPos)};
                using var brHalo=new SolidBrush(Color.FromArgb(180,BgColor)); g.FillPolygon(brHalo,ptsH);
                var pts=new[]{new PointF(cx,yPos-d),new PointF(cx+d,yPos),new PointF(cx,yPos+d),new PointF(cx-d,yPos)};
                using var br=new SolidBrush(Color.FromArgb(240,col)); g.FillPolygon(br,pts);
            }
            if (_lblX)  // Labels button controls all cursor labels
            {
                string timeTxt=lbl+"="+FormatTime(XFracToMs(frac));
                var szL=g.MeasureString(timeTxt,font);
                float ly=Math.Clamp(w.Top+lblFracY*w.Height-szL.Height/2f, w.Top, w.Bottom-szL.Height);
                lblRect=new RectangleF(cx+2,ly,szL.Width+4,szL.Height);
                using var brBgX=new SolidBrush(Color.FromArgb(210, BgColor));
                g.FillRectangle(brBgX,lblRect);
                g.DrawString(timeTxt,font,brL,cx+3,ly);
            } else lblRect=RectangleF.Empty;
        }

        private void DrawYCursorLine(Graphics g,RectangleF w,float frac,string lbl,
            Color col,Pen pen,Font font,SolidBrush brL,float lblFracX,out RectangleF lblRect)
        {
            float cy=w.Bottom-frac*w.Height;
            g.DrawLine(pen,w.Left,cy,w.Right,cy);
            float norm=YFracToNorm(frac);
            var (cok,odb)=GetActiveCal();
            if (_lblY)  // Labels button controls all cursor labels
            {
                string valTxt;
                if(cok){double sc=Math.Pow(10,odb/20.0)*Math.Sqrt(2.0);valTxt=lbl+"="+FormatV((double)norm*sc);}
                else valTxt=$"{lbl}={norm:+0.000;-0.000}";
                var szL=g.MeasureString(valTxt,font);
                float lx=Math.Clamp(w.Left+lblFracX*w.Width, w.Left, w.Right-szL.Width);
                lblRect=new RectangleF(lx-2,cy+2,szL.Width+4,szL.Height);
                using var brBgY=new SolidBrush(Color.FromArgb(210, BgColor));
                g.FillRectangle(brBgY,lblRect);
                g.DrawString(valTxt,font,brL,lx,cy+2);
            } else lblRect=RectangleF.Empty;
        }

        // ── Dialogue Set X / Set Y ────────────────────────────────────────────
        private void OpenSetCursorDialog(bool isX)
        {
            if (isX)
            {
                double t1=XFracToMs(_x1Frac), t2=XFracToMs(_x2Frac);
                using var dlg=new CursorSetDialog(isX:true, v1:t1, v2:t2, unit:"ms");
                if(dlg.ShowDialog(this)!=DialogResult.OK) return;
                _x1Frac=MsToXFrac(dlg.Value1); _x2Frac=MsToXFrac(dlg.Value2);
            }
            else
            {
                var (cok,odb)=GetActiveCal();
                double sc=cok?Math.Pow(10,odb/20.0)*Math.Sqrt(2.0):1.0;
                double yv1=(double)YFracToNorm(_y1Frac)*sc, yv2=(double)YFracToNorm(_y2Frac)*sc;
                string unit=cok?"V":"norm";
                using var dlg=new CursorSetDialog(isX:false, v1:yv1, v2:yv2, unit:unit);
                if(dlg.ShowDialog(this)!=DialogResult.OK) return;
                _y1Frac=NormToYFrac((float)(dlg.Value1/sc));
                _y2Frac=NormToYFrac((float)(dlg.Value2/sc));
            }
            pnlScope.Invalidate();
        }

        private void ChkVdiv_Changed(object? s, EventArgs e)
        { _vdivMode = _vdivMode; ChkVdivLogic(); }  // kept for compat

        private void ChkVdivLogic()
        {
            UpdateGainCombos();
            UpdateTrigLevelRange(isConversion: true);
            lblTrigLvl.Text = _vdivMode ? "LEVEL (V)" : "LEVEL";
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
                btnVdiv.Enabled = vL || vR;
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
            _panOffset=0;
            _hpXprevL=0; _hpXprevR=0; _hpYprevL=0; _hpYprevR=0;
            _blinkCounter=0; _blinkState=false; _lastTrigOk=false; _lastTrigAbsPos=-1; _holdoffEndAbs=0;
            RunRequested?.Invoke();
            SetRunState(true); SetArmedState(false);
            _timer.Start();
            StartAcqTicker();
        }

        private void StopOscillo()
        { _timer.Stop(); SetRunState(false); lblTrigStatus.Text=""; RunStateChanged?.Invoke(false); StopAcqTicker(); }

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
            if(!_isRunning){lblTrigStatus.Text=""; lblTrigStatus.Visible=true; return;}
            if(_isArmed)
            {
                _blinkCounter++; if(_blinkCounter>=15){_blinkCounter=0;_blinkState=!_blinkState;}
                lblTrigStatus.Text="WAIT"; lblTrigStatus.ForeColor=Color.SeaGreen;
                lblTrigStatus.Visible=_blinkState;  // éteint = complètement invisible
            }
            else if(_lastTrigOk){ lblTrigStatus.Text="TRIG'D"; lblTrigStatus.ForeColor=Color.Crimson;
                                  lblTrigStatus.Visible=_blinkState; }
            else { lblTrigStatus.Text=""; lblTrigStatus.Visible=true; }
        }

        // ── Molette / navigation post-stop ────────────────────────────────────
        private void PnlScope_MouseWheel(object? s, MouseEventArgs e)
        {
            if(_isRunning||_capL==null||_capSR<=0) return;
            int td=Math.Clamp(cmbTimediv.SelectedIndex,0,TimeDivMs.Length-1);
            int spd=Math.Max(1,(int)(TimeDivMs[td]/1000.0*_capSR));
            // Shift + roulette = déplacement rapide (×10)
            if((Control.ModifierKeys & Keys.Shift) != 0) spd *= 10;
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
            // ── Butée : resynchroniser _panOffset sur ds réel pour éviter
            //    l'accumulation silencieuse au-delà des limites ──────────────
            _panOffset=ds-(tref-pre);
            win=Math.Min(win,_capL.Length-ds); if(win<2) return;
            float[] sl=new float[win],sr2=new float[win];
            Array.Copy(_capL,ds,sl,0,win); Array.Copy(_capR,ds,sr2,0,win);
            // AC coupling applied upstream via HPF filter (no post-subtraction needed)
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
            // Cap à 2M samples pour éviter les allocations massives aux grandes bases de temps
            const int MAX_SEARCH = 2_000_000;
            int search=Math.Min(win*3+pre+4, Math.Min(ring.Capacity-1, MAX_SEARCH));

            long absEnd=ring.AbsWrite;
            if(absEnd-search<0){pnlScope.Invalidate();return;}
            // Buffer trop petit pour cette base de temps ?
            int minCap=win+(win-pre)+2;  // pre + post minimum
            if(ring.Capacity < minCap)
            {
                lblTrigStatus.Text=$"BUFFER < {minCap/sr:.0}s";
                lblTrigStatus.ForeColor=Color.Orange;
                pnlScope.Invalidate(); return;
            }
            var rawL=new float[search]; var rawR=new float[search];
            if(!ring.TryReadWindow(absEnd-search,search,rawL,rawR)){pnlScope.Invalidate();return;}

            // ── Couplage AC : filtre passe-haut RII 1er ordre (simule condensateur CR) ──
            // H(z) = α(1 - z⁻¹) / (1 - αz⁻¹)  avec α = τ/(τ + T),  fc = HPF_FC Hz
            if (AcL || AcR)
            {
                double tau = 1.0 / (2.0 * Math.PI * HPF_FC);
                double T   = 1.0 / sr;
                double a   = tau / (tau + T);   // ≈ 0.9994 à 768 kHz, 10 Hz

                if (AcL)
                {
                    double xp = _hpXprevL, yp = _hpYprevL;
                    for (int i = 0; i < search; i++)
                    {
                        double x = rawL[i];
                        double y = a * (yp + x - xp);
                        rawL[i] = (float)y;
                        xp = x; yp = y;
                    }
                    _hpXprevL = xp; _hpYprevL = yp;
                }
                if (AcR)
                {
                    double xp = _hpXprevR, yp = _hpYprevR;
                    for (int i = 0; i < search; i++)
                    {
                        double x = rawR[i];
                        double y = a * (yp + x - xp);
                        rawR[i] = (float)y;
                        xp = x; yp = y;
                    }
                    _hpXprevR = xp; _hpYprevR = yp;
                }
            }

            if(IsRollMode){SetArmedState(false);
                int rollStart=search-win;
                if(rollStart<0){pnlScope.Invalidate();return;} // buffer trop petit pour cette base de temps
                StoreAndDisplay(rawL,rawR,search,rollStart,win,sr,-1);pnlScope.Invalidate();return;}

            var trigSrc=(cmbTrigSrc.SelectedIndex==1)?rawR:rawL;
            float tLvl=GetTrigLevelNormalized(); bool rising=cmbTrigEdge.SelectedIndex==0;
            float vmax=float.MinValue,vmin=float.MaxValue;
            for(int i=0;i<search;i++){if(trigSrc[i]>vmax)vmax=trigSrc[i];if(trigSrc[i]<vmin)vmin=trigSrc[i];}
            // EMA sur l'hystérésis : évite les sauts brusques de armLevel/fireLevel
            // qui causaient des décalages X à la fin d'une acquisition
            float hystInst=Math.Max(0.002f,(vmax-vmin)*0.05f);
            _hystEma = _hystEma*0.85f + hystInst*0.15f;
            float hyst=_hystEma;

            bool trigOk=FindTriggerBackward(trigSrc,search,tLvl,rising,hyst,win,pre,out int trigIdx);

            int dstart;
            if(trigOk){dstart=trigIdx-pre;_trigFound=true;_lastTrigOk=true;SetArmedState(false);

                long trigAbsPos = absEnd - (search - trigIdx);

                // ── Hold-off : ignore les triggers trop proches du précédent ─
                if (_holdoffEndAbs > 0 && trigAbsPos < _holdoffEndAbs)
                { UpdateInfoLine(); pnlScope.Invalidate(); return; }

                // ── Anti-redéclenchement sur le même événement ────────────────
                bool sameEvent = _lastTrigAbsPos >= 0 &&
                    Math.Abs(trigAbsPos - _lastTrigAbsPos) < (long)(sr / 2);
                if (sameEvent) { UpdateInfoLine(); pnlScope.Invalidate(); return; }
                _lastTrigAbsPos = trigAbsPos;

                // Arme le hold-off pour le prochain trigger
                double holdoffMs = (double)nudHoldoff.Value;
                if (holdoffMs > 0)
                    _holdoffEndAbs = trigAbsPos + (long)(holdoffMs / 1000.0 * sr);
                else
                    _holdoffEndAbs = 0;
            }
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

        /// Détection du trigger standard (logique oscilloscope matériel).
        ///
        /// Scan avant (chronologique) dans [minI..maxI] :
        /// 1) attend armement (signal sous armLevel pour rising)
        /// 2) dès que fireLevel est franchi → premier trigger valide, stop.
        private static bool FindTriggerBackward(
            float[] src, int len, float level, bool rising, float hyst,
            int win, int pre, out int idx)
        {
            idx = -1;
            int post = win - pre;
            int maxI = len - post - 1;
            int minI = pre + 1;
            if (maxI <= minI) return false;

            float armLevel  = level + (rising ? -hyst :  hyst);
            float fireLevel = level + (rising ?  hyst : -hyst);

            bool armed = false;
            for (int i = minI; i <= maxI; i++)
            {
                if (!armed)
                {
                    if (rising ? src[i] <= armLevel : src[i] >= armLevel)
                        armed = true;
                }
                else
                {
                    if (rising ? src[i] >= fireLevel : src[i] <= fireLevel)
                    { idx = i; return true; }   // premier front valide → stop
                }
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
            int ch=GetCh();
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
            int winN=Math.Max(8,(int)(TimeDivMs[td]*H_DIVS/1000.0*(_lastSR>0?_lastSR:1)));
            string nPts=$"   N={winN:N0}";
            _infoLine=$"Channel:{ch2}   SR:{sr}   {FormatTimediv(TimeDivMs[td])}{zoom}{coup}{roll}{nPts}{pan}";
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
        {
            if (Math.Abs(v) < 1e-12) return "0 V";
            double a = Math.Abs(v);
            if (a >= 1.0)   return Sig4(v)    + " V";
            if (a >= 0.001) return Sig4(v * 1000) + " mV";
            return                  Sig4(v * 1e6)  + " µV";
        }

        // 4 chiffres significatifs avec zéros trailing conservés
        private static string Sig4(double v)
        {
            double a = Math.Abs(v);
            if (a == 0)      return "0,000";
            int dec = 3 - (int)Math.Floor(Math.Log10(a));
            if (dec < 0) dec = 0;
            if (dec > 6) dec = 6;
            return v.ToString("F" + dec,
                System.Globalization.CultureInfo.CurrentCulture);
        }

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

            // Fond : blanc en export (pour PDF), gris système en live
            using var bgBrush = new SolidBrush(_isExporting ? Color.White : SystemColors.Control);
            g.FillRectangle(bgBrush, 0, 0, sz.Width, sz.Height);

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

            // Trigger status right-aligned in info line area
            if (!string.IsNullOrEmpty(lblTrigStatus.Text))
            {
                using var fSt=new Font("Segoe UI",8.25f,FontStyle.Bold);
                using var brSt=new SolidBrush(lblTrigStatus.ForeColor);
                using var sfR=new StringFormat{Alignment=StringAlignment.Far};
                var r=new RectangleF(MARGIN_LEFT,0,wave.Width,MARGIN_TOP-2);
                g.DrawString(lblTrigStatus.Text,fSt,brSt,r,sfR);
            }
            DrawGrid(g,wave);
            DrawAxisLabels(g,wave);

            int ch=GetCh();
            int traceMode=cmbTraceMode.SelectedIndex;  // 0=Segment 1=Mixed 2=Dot
            if(_dispL!=null&&(ch==0||ch==2)) DrawWaveform(g,wave,_dispL,TraceColorL,_colors.FftWidth,_effZoomL,_offsetL,traceMode);
            if(_dispR!=null&&(ch==1||ch==2)) DrawWaveform(g,wave,_dispR,TraceColorR,_colors.FftWidth,_effZoomR,_offsetR,traceMode);
            if(!IsRollMode&&(_dispL!=null||_dispR!=null))
                DrawTriggerMarkers(g,wave,_trigPx,(_dispL??_dispR)!.Length);

            // Curseurs X/Y (par-dessus les traces et le trigger)
            DrawCursors(g, wave);

            // Marqueurs de référence verticale (dans la marge gauche)
            DrawOffsetMarkers(g, wave);

            // Mini-carte d'acquisition (top-right, dans MARGIN_TOP)
            DrawAcqMinimap(g, wave);

            DrawMeasurements(g,wave);

            // Minimap mémoire (quand arrêté et buffer disponible)
            // (pas de DrawRectangle ici — la bordure de l'onglet vient de BackColor+Padding)
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
            int ch=GetCh();
            int tdIdx=Math.Clamp(cmbTimediv.SelectedIndex,0,TimeDivMs.Length-1);
            double td=TimeDivMs[tdIdx];

            // ── Axe X : label sur chaque division (11 au total : 0 à H_DIVS) ─────
            for(int i=0;i<=H_DIVS;i++)
            {
                float x=w.Left+i*w.Width/H_DIVS;
                double tMs=IsRollMode?i*td:(i-PreTrig*H_DIVS)*td;
                string lbl=(!IsRollMode&&Math.Abs(tMs)<td*0.001)?"0":FormatTime(tMs);
                var sf=new StringFormat{Alignment=StringAlignment.Center};
                g.DrawString(lbl,font,br,new RectangleF(x-38,w.Bottom+2,76,16),sf);
            }

            // Légende "Time" en bas au centre
            using var sfC=new StringFormat{Alignment=StringAlignment.Center};
            g.DrawString("Time",fontLeg,br,new RectangleF(w.Left,w.Bottom+18,w.Width,14),sfC);

            // ── Axe Y : voie active (_scaleChannel) avec offset corrigé ──────────
            // yRef = ym - offset × w.Height/2  (référence 0V de la voie active)
            // Mode V/div  : val_V = vd × V_DIVS × (yRef - yPos) / w.Height  (SANS zoom)
            // Mode ×N     : val   = (yRef - yPos) / ys_                       (AVEC zoom)
            float activeOffset = _scaleChannel==0 ? _offsetL : _offsetR;
            float activeZoom   = _scaleChannel==0 ? _effZoomL : _effZoomR;
            float ym_          = w.Top + w.Height/2f;
            float ys_          = w.Height/2f * activeZoom;
            float yRef_        = ym_ - activeOffset * w.Height/2f;
            Color axisColor    = _scaleChannel==0 ? TraceColorL : TraceColorR;
            using var brAxis   = new SolidBrush(axisColor);
            var sfY = new StringFormat{Alignment=StringAlignment.Far,LineAlignment=StringAlignment.Center};

            for(int i=0;i<=V_DIVS;i++)
            {
                float yPos = w.Top + i * w.Height / V_DIVS;
                string lbl;
                if(_vdivMode)
                {
                    // Formule indépendante du zoom :
                    // val_V = vd × V_DIVS × (yRef - yPos) / w.Height
                    double vd    = GetVdivForCh(_scaleChannel);
                    double val_V = vd * V_DIVS * (yRef_ - yPos) / w.Height;
                    lbl = FormatVoltLabel(val_V);
                }
                else
                {
                    double val = ys_ > 0.001f ? (double)(yRef_ - yPos) / ys_ : 0.0;
                    if(Math.Abs(val)<1e-9) lbl="0";
                    else{double a=Math.Abs(val);lbl=val.ToString(a>=0.01?"+0.000;-0.000":"+0.0000;-0.0000");}
                }
                g.DrawString(lbl,font,brAxis,new RectangleF(0,yPos-11,MARGIN_LEFT-4,22),sfY);
            }

            // Légende "Amplitude" verticale (couleur de la voie active)
            var state=g.Save();
            g.TranslateTransform(10f,w.Top+w.Height/2f);
            g.RotateTransform(-90f);
            using var sfA=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center};
            g.DrawString("Amplitude",fontLeg,brAxis,new PointF(0,0),sfA);
            g.Restore(state);
        }

        // offset : fraction de la demi-hauteur écran (SANS zoom).
        // Changer le zoom ne déplace pas la référence 0V — comme un vrai oscilloscope.
        // offset > 0 → courbe monte ; offset < 0 → courbe descend.
        // traceMode: 0=Segment  1=Mixed(segments+dots)  2=Dot(dots seuls)
        // Peak Detect automatique quand s.Length > dw (indépendant du mode trace).
        private static void DrawWaveform(Graphics g,RectangleF w,float[] s,Color color,int lw,float zoom,float offset=0f,int traceMode=0)
        {
            int dw=(int)w.Width;if(dw<2||s.Length<2) return;
            float ym=w.Top+w.Height/2f, ys=w.Height/2f*zoom;
            float yRef=ym-offset*w.Height/2f;
            using var pen=new Pen(color,Math.Max(1,lw));

            if(s.Length > dw)
            {
                // ── Peak Detect automatique ───────────────────────────────────
                float spx=(float)(s.Length-1)/(dw-1);
                for(int px=0;px<dw;px++)
                {
                    int lo=Math.Max(0,(int)(px*spx));
                    int hi=Math.Min(s.Length-1,(int)((px+1)*spx));
                    float vMin=s[lo],vMax=s[lo];
                    for(int k=lo+1;k<=hi;k++){if(s[k]<vMin)vMin=s[k];if(s[k]>vMax)vMax=s[k];}
                    float y1=Math.Clamp(yRef-vMax*ys,w.Top,w.Bottom);
                    float y2=Math.Clamp(yRef-vMin*ys,w.Top,w.Bottom);
                    if(y2<y1+1f)y2=y1+1f;
                    g.DrawLine(pen,w.Left+px,y1,w.Left+px,y2);
                }
            }
            else
            {
                // ── Mode Segment ou Mixed : lignes interpolées pixel/pixel ────
                if(traceMode <= 1)
                {
                    var pts=new PointF[dw];
                    for(int px=0;px<dw;px++)
                    {
                        float t=(float)px/dw*(s.Length-1);
                        int lo=(int)t,hi=Math.Min(lo+1,s.Length-1);
                        float v=s[lo]*(1f-(t-lo))+s[hi]*(t-lo);
                        pts[px]=new PointF(w.Left+px,Math.Clamp(yRef-v*ys,w.Top,w.Bottom));
                    }
                    g.DrawLines(pen,pts);
                }

                // ── Mode Mixed ou Dot : point à chaque position d'échantillon ─
                if(traceMode >= 1)
                {
                    float scale=dw>1?(dw-1f)/(s.Length-1):1f;
                    float r=Math.Max(2.5f,lw+0.5f);
                    using var brush=new SolidBrush(color);
                    for(int si=0;si<s.Length;si++)
                    {
                        float px=si*scale;
                        float y=Math.Clamp(yRef-s[si]*ys,w.Top,w.Bottom);
                        g.FillEllipse(brush,w.Left+px-r,y-r,r*2f,r*2f);
                    }
                }
            }
        }

        // ── Marqueurs de référence verticale (dans la marge gauche) ──────────
        private void DrawOffsetMarkers(Graphics g, RectangleF w)
        {
            int ch=GetCh();
            if(ch==0||ch==2) DrawOffsetMarker(g,w,_offsetL,_effZoomL,TraceColorL,"L");
            if(ch==1||ch==2) DrawOffsetMarker(g,w,_offsetR,_effZoomR,TraceColorR,"R");
        }

        private static void DrawOffsetMarker(Graphics g,RectangleF w,float offset,float zoom,Color color,string lbl)
        {
            float ym=w.Top+w.Height/2f;
            // Position du marqueur indépendante du zoom (offset en fraction de demi-hauteur)
            float yM=Math.Clamp(ym-offset*w.Height/2f, w.Top, w.Bottom);
            float tip=w.Left-2f;
            float bas=tip-10f;
            float h=7f;
            // Triangle pointant à droite
            PointF[] tri={new(bas,yM-h),new(tip,yM),new(bas,yM+h)};
            using var brFill=new SolidBrush(Color.FromArgb(220,color));
            g.FillPolygon(brFill,tri);
            using var penOut=new Pen(Color.FromArgb(160,color),1f);
            g.DrawPolygon(penOut,tri);
            // Étiquette de voie
            using var font=new Font("Segoe UI",7f,FontStyle.Bold);
            using var brLbl=new SolidBrush(color);
            g.DrawString(lbl,font,brLbl,bas-13f,yM-7f);
        }

        // ── Minimap mémoire ───────────────────────────────────────────────────
        // Affiche (en haut à droite de la zone graphique) un rectangle représentant
        // la totalité du buffer capturé (_capL) et la portion actuellement visible.
        // ── Handle trigger level (côté droit, triangle T) ───────────────────
        private float GetTrigYPixel(RectangleF w)
        {
            int trigCh  = cmbTrigSrc.SelectedIndex;
            float zoom  = trigCh == 1 ? _effZoomR : _effZoomL;
            float offset= trigCh == 1 ? _offsetR  : _offsetL;
            float ym    = w.Top + w.Height / 2f;
            float ys    = w.Height / 2f * zoom;
            float yRef  = ym - offset * w.Height / 2f;
            return Math.Clamp(yRef - GetTrigLevelNormalized() * ys, w.Top, w.Bottom);
        }

        private void PnlScope_TrigHandleMouseDown(object? s, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            var w = GetWaveRect(); if (w.IsEmpty) return;
            // Zone de hit : de w.Right à w.Right+20, ±12px autour de yT
            if (e.X < w.Right || e.X > w.Right + 20) return;
            float yT = GetTrigYPixel(w);
            if (Math.Abs(e.Y - yT) <= 12f) _draggingTrigger = true;
        }

        private void PnlScope_TrigHandleMouseMove(object? s, MouseEventArgs e)
        {
            if (!_draggingTrigger) return;
            var w = GetWaveRect(); if (w.IsEmpty) return;

            int   trigCh = cmbTrigSrc.SelectedIndex;
            float zoom   = trigCh == 1 ? _effZoomR : _effZoomL;
            float offset = trigCh == 1 ? _offsetR  : _offsetL;
            float ym     = w.Top + w.Height / 2f;
            float ys     = w.Height / 2f * zoom;
            float yRef   = ym - offset * w.Height / 2f;

            // Convertit pixels → normalisé
            float normVal = ys > 0 ? (yRef - Math.Clamp(e.Y, w.Top, w.Bottom)) / ys : 0f;

            if (_vdivMode)
            {
                // Convertit normalisé → Volts via calScale
                double cs = GetCalScaleForTrigCh(trigCh);
                double trigV = cs > 0 ? normVal * cs : normVal;
                TrigLevelValue = Math.Clamp((decimal)trigV, _trigMin, _trigMax);
            }
            else
            {
                TrigLevelValue = Math.Clamp((decimal)normVal, _trigMin, _trigMax);
            }
            pnlScope.Invalidate();
        }

        // ── Drag horizontal de la position trigger (T POS) ──────────────────
        private void PnlScope_TrigPosMouseDown(object? s, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            var w = GetWaveRect(); if (w.IsEmpty) return;
            float xTrig = w.Left + PreTrig * w.Width;
            // Zone de hit : triangle en haut (y ≤ w.Top + 10) ±8px autour de xTrig
            if (e.Y > w.Top + 10) return;
            if (Math.Abs(e.X - xTrig) <= 8f) _draggingTrigPos = true;
        }

        private void PnlScope_TrigPosMouseMove(object? s, MouseEventArgs e)
        {
            if (!_draggingTrigPos) return;
            var w = GetWaveRect(); if (w.IsEmpty || w.Width < 1f) return;

            // Convertit X pixel → divisions (0 … H_DIVS)
            float frac = Math.Clamp((e.X - w.Left) / w.Width, 0.05f / H_DIVS * H_DIVS, 1f);
            decimal divVal = Math.Clamp(
                (decimal)Math.Round(frac * H_DIVS * 2f) / 2m,   // arrondi au 0.5 div
                nudTrigPos.Minimum, nudTrigPos.Maximum);

            if (nudTrigPos.Value != divVal)
            {
                nudTrigPos.Value = divVal;
                // nudTrigPos.ValueChanged s'occupe du Invalidate
            }
        }

        private void PnlScope_OffsetMouseDown(object? s, MouseEventArgs e)
        {
            if(e.Button!=MouseButtons.Left) return;
            var w=GetWaveRect();if(w.IsEmpty)return;
            float tip=w.Left-2f,bas=tip-10f;
            if(e.X<bas-14f||e.X>tip+2f)return;
            float ym=w.Top+w.Height/2f;
            int ch=GetCh();
            if((ch==0||ch==2)&&Math.Abs(e.Y-(ym-_offsetL*w.Height/2f))<=12f)_draggingOffset=1;
            else if((ch==1||ch==2)&&Math.Abs(e.Y-(ym-_offsetR*w.Height/2f))<=12f)_draggingOffset=2;
        }

        private void PnlScope_OffsetMouseMove(object? s, MouseEventArgs e)
        {
            if(_draggingOffset==0)return;
            var w=GetWaveRect();if(w.IsEmpty)return;
            float ym=w.Top+w.Height/2f;
            float halfH=w.Height/2f;
            if(halfH<1f)return;
            float yC=Math.Clamp(e.Y,w.Top,w.Bottom);
            float newOff=(ym-yC)/halfH;   // offset en fraction de demi-hauteur, sans zoom
            // Aimantation magnétique : accroche sur les divisions de grille si ±15% d'une division
            float divUnit=2f/V_DIVS;
            float snapped=(float)Math.Round(newOff/divUnit)*divUnit;
            if(Math.Abs(newOff-snapped)<divUnit*0.15f) newOff=snapped;
            if(_draggingOffset==1)_offsetL=newOff;else _offsetR=newOff;
            pnlScope.Invalidate();
        }

        private void DrawAcqMinimap(Graphics g, RectangleF w)
        {
            if (_capL == null || _capL.Length < 2 || _lastSR <= 0) return;

            int td    = Math.Clamp(cmbTimediv.SelectedIndex, 0, TimeDivMs.Length-1);
            int win   = Math.Max(8, (int)(TimeDivMs[td]*H_DIVS/1000.0*_lastSR));
            int pre   = (int)(win * PreTrig);
            int tref  = _capTrigIdx>=0 ? _capTrigIdx : pre;
            int ds    = Math.Clamp(tref-pre+_panOffset, 0, Math.Max(0, _capL.Length-win));
            win       = Math.Min(win, _capL.Length-ds);
            int total = _capL.Length;

            // ── Géométrie : barre centrée en haut du graphe (à l'intérieur) ──
            const float MM_W = 200f, MM_H = 12f;
            float mmX = w.Left + (w.Width - MM_W) / 2f;
            float mmY = w.Top + 3f;

            // Fond blanc semi-opaque + bordure grise
            using var brBg  = new SolidBrush(Color.FromArgb(200, Color.White));
            using var penBd = new Pen(Color.FromArgb(160, Color.DimGray), 1f);
            g.FillRectangle(brBg,  mmX, mmY, MM_W, MM_H);
            g.DrawRectangle(penBd, mmX, mmY, MM_W-1f, MM_H-1f);

            // ── Fenêtre visible (bleu) — clampée à la barre ───────────────────
            // widthFrac et startFrac toujours entre 0 et 1
            float startFrac = total > 0 ? Math.Clamp((float)ds  / total, 0f, 1f) : 0f;
            float widthFrac = total > 0 ? Math.Clamp((float)win / total, 0f, 1f) : 1f;
            float vx  = mmX + startFrac * MM_W;
            float vw  = Math.Max(2f, widthFrac * MM_W);
            // S'assurer que le rectangle ne dépasse pas à droite
            vw = Math.Min(vw, mmX + MM_W - vx);
            using var brWin = new SolidBrush(Color.FromArgb(140, Color.DodgerBlue));
            g.FillRectangle(brWin, vx, mmY+1f, vw, MM_H-2f);

            // ── Marqueur trigger (trait orange) ───────────────────────────────
            if (_capTrigIdx >= 0 && _capTrigIdx < total)
            {
                float tx = mmX + Math.Clamp((float)_capTrigIdx/total, 0f, 1f)*MM_W;
                using var penT = new Pen(Color.FromArgb(210, Color.OrangeRed), 1.5f);
                g.DrawLine(penT, tx, mmY, tx, mmY+MM_H);
            }

            // ── Labels : durée totale à gauche / durée visible à droite ───────
            using var fnt  = new Font("Segoe UI", 6.5f);
            using var brL  = new SolidBrush(Color.FromArgb(180, Color.Black));
            double totalMs   = (double)total/_lastSR*1000.0;
            double visibleMs = (double)win  /_lastSR*1000.0;
            var sfL = new StringFormat{Alignment=StringAlignment.Near,  LineAlignment=StringAlignment.Center};
            var sfR = new StringFormat{Alignment=StringAlignment.Far,   LineAlignment=StringAlignment.Center};
            var barRect = new RectangleF(mmX+2, mmY, MM_W-4, MM_H);
            g.DrawString(FormatTime(totalMs),   fnt, brL, barRect, sfL);
            g.DrawString(FormatTime(visibleMs), fnt, brL, barRect, sfR);
        }


        // ── Fraction effective du curseur (prend en compte le verrouillage sample) ─
        private RectangleF GetMinimapScreenRect()
        {
            var w = GetWaveRect();
            const float MM_W=200f, MM_H=12f;
            return new RectangleF(w.Left+(w.Width-MM_W)/2f, w.Top+3f, MM_W, MM_H);
        }

        private void PnlScope_MinimapMouseDown(object? s, MouseEventArgs e)
        {
            if(_isRunning||_capL==null||_capSR<=0) return;
            var mm=GetMinimapScreenRect();
            if(!mm.Contains(e.Location)) return;

            // Calcul du ds courant pour mémoriser l'offset du clic dans la barre
            int td =Math.Clamp(cmbTimediv.SelectedIndex,0,TimeDivMs.Length-1);
            int win=Math.Max(8,(int)(TimeDivMs[td]*H_DIVS/1000.0*_capSR));
            int pre=(int)(win*PreTrig);
            int tref=_capTrigIdx>=0?_capTrigIdx:pre;
            int ds =Math.Clamp(tref-pre+_panOffset,0,Math.Max(0,_capL.Length-win));
            int total=_capL.Length;

            float clickFrac=(e.X-mm.Left)/mm.Width;
            int clickSample=(int)(clickFrac*total);
            _minimapClickOffset=clickSample-ds;  // offset clic dans la barre bleue
            _draggingMinimap=true;
            ApplyMinimapPan(e.X, mm);
        }

        private void PnlScope_MinimapMouseMove(object? s, MouseEventArgs e)
        {
            if(!_draggingMinimap||_capL==null||_capSR<=0) return;
            ApplyMinimapPan(e.X, GetMinimapScreenRect());
        }

        private void ApplyMinimapPan(float mx, RectangleF mm)
        {
            if(_capL==null||_capSR<=0) return;
            int td  =Math.Clamp(cmbTimediv.SelectedIndex,0,TimeDivMs.Length-1);
            int win =Math.Max(8,(int)(TimeDivMs[td]*H_DIVS/1000.0*_capSR));
            int pre =(int)(win*PreTrig);
            int tref=_capTrigIdx>=0?_capTrigIdx:pre;
            int total=_capL.Length;

            float frac=(mx-mm.Left)/mm.Width;
            int targetDs=(int)(frac*total)-_minimapClickOffset;
            targetDs=Math.Clamp(targetDs,0,Math.Max(0,total-win));
            _panOffset=targetDs-(tref-pre);
            RefreshStoppedView();
            pnlScope.Invalidate();
        }

        private void PnlScope_OffsetDblClick(object? s, MouseEventArgs e)        {
            var w=GetWaveRect();if(w.IsEmpty)return;
            float tip=w.Left-2f,bas=tip-10f;
            if(e.X<bas-14f||e.X>tip+2f)return;
            float ym=w.Top+w.Height/2f;
            int ch=GetCh();
            if((ch==0||ch==2)&&Math.Abs(e.Y-(ym-_offsetL*w.Height/2f))<=12f){_offsetL=0f;pnlScope.Invalidate();}
            else if((ch==1||ch==2)&&Math.Abs(e.Y-(ym-_offsetR*w.Height/2f))<=12f){_offsetR=0f;pnlScope.Invalidate();}
        }

        private void DrawTriggerMarkers(Graphics g,RectangleF w,int trigIdx,int winSamples)
        {
            int trigCh=cmbTrigSrc.SelectedIndex;
            float zoom=trigCh==1?_effZoomR:_effZoomL;
            // Décalage vertical de la voie source du trigger (même formule que DrawWaveform)
            float offset=trigCh==1?_offsetR:_offsetL;
            float ym=w.Top+w.Height/2f;
            float ys=w.Height/2f*zoom;
            float yRef=ym-offset*w.Height/2f;   // référence 0V décalée comme la courbe
            float yT=Math.Clamp(yRef-GetTrigLevelNormalized()*ys,w.Top,w.Bottom);
            Color tCol=Color.FromArgb(180, trigCh==0?TraceColorL:TraceColorR);

            // Ligne horizontale du niveau de trigger (toujours visible)
            using var pH=new Pen(tCol,1f){DashStyle=DashStyle.Dash};
            g.DrawLine(pH,w.Left,yT,w.Right,yT);

            // Handle "T" côté droit — triangle pointant à gauche + lettre T
            float th = 7f;   // demi-hauteur du triangle
            float tx = w.Right + 2f;     // pointe à gauche (bord droit du plot)
            float tw = tx + 10f;          // base du triangle
            PointF[] tTri = { new(tx, yT), new(tw, yT - th), new(tw, yT + th) };
            using var brT = new SolidBrush(Color.FromArgb(220, tCol));
            g.FillPolygon(brT, tTri);
            using var penT = new Pen(Color.FromArgb(160, tCol), 1f);
            g.DrawPolygon(penT, tTri);
            using var fontT = new Font("Segoe UI", 7f, FontStyle.Bold);
            using var brLblT = new SolidBrush(tCol);
            g.DrawString("T", fontT, brLblT, tw + 2f, yT - 7f);

            // Ligne verticale t=0 TOUJOURS visible à la position PreTrig
            float xTrig=w.Left+PreTrig*w.Width;
            using var pV=new Pen(Color.FromArgb(160,tCol),1f){DashStyle=DashStyle.Dash};
            g.DrawLine(pV,xTrig,w.Top,xTrig,w.Bottom);

            // Repère draggable en HAUT de la ligne de position trigger (triangle vers le bas)
            {
                float hx = xTrig;
                float hy = w.Top - 1f;
                using var brH = new SolidBrush(tCol);
                g.FillPolygon(brH, new PointF[]
                {
                    new(hx,        hy + 9f),   // pointe vers le bas
                    new(hx - 5f,   hy),
                    new(hx + 5f,   hy)
                });
            }

            // Flèche indicatrice uniquement quand le trigger a déclenché
            if(_trigFound&&trigIdx>=0&&trigIdx<winSamples)
            {
                float ay=w.Top+4; using var brA=new SolidBrush(tCol);
                bool rising=cmbTrigEdge.SelectedIndex==0;
                g.FillPolygon(brA,rising
                    ?new[]{new PointF(xTrig,ay+8),new PointF(xTrig-4,ay),new PointF(xTrig+4,ay)}
                    :new[]{new PointF(xTrig,ay),new PointF(xTrig-4,ay+8),new PointF(xTrig+4,ay+8)});
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
            int ch = GetCh();

            bool calOkL=false, calOkR=false; double offL=0, offR=0;
            if (GetInputCalibration != null)
            { var (vL,oL,vR,oR)=GetInputCalibration(); calOkL=vL;offL=oL;calOkR=vR;offR=oR; }

            if (ch==0||ch==2) DrawMeasSection(g,"LEFT",  TraceColorL, _measL, rect, ref y, calOkL, offL);
            if (ch==1||ch==2) DrawMeasSection(g,"RIGHT", TraceColorR, _measR, rect, ref y, calOkR, offR);

            // Section curseurs X/Y
            if (_cursorChan>0 && (_xActive||_yActive))
                y = DrawCursorMeasSection(g, rect, y);

            // Statut centré en bas du panneau, italic gris
            string statTxt=""; Color statCol=Color.Gray;
            if      (_isArmed)                     { statTxt="WAIT";          statCol=Color.SeaGreen; }
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

        /// Panneau section canal : titre + lignes label|valeur.
        /// Vrms et Freq mis en valeur (gras, plus gros, fond bleu) comme le panneau Spectrum.
        private static void DrawMeasSection(Graphics g, string title, Color titleColor,
            MeasResult m, RectangleF rect, ref float y, bool cal, double offDb)
        {
            using var fontBase   = new Font("Segoe UI", 9f,    FontStyle.Regular);
            using var fontHeader = new Font("Segoe UI", 9f,    FontStyle.Bold);
            using var fontStrong = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            using var brushText  = new SolidBrush(Color.DimGray);
            using var penSep     = new Pen(Color.Gainsboro, 1f);
            using var fmtCenter  = new StringFormat { Alignment = StringAlignment.Center };
            using var fmtRight   = new StringFormat { Alignment = StringAlignment.Far,
                                                      LineAlignment = StringAlignment.Near };

            float pad    = 8f;
            float xL     = rect.X + pad;
            float lineH  = fontBase.GetHeight(g) + 4f;
            float strongH= fontStrong.GetHeight(g) + 4f;

            // Titre centré coloré
            var headerRect = new RectangleF(rect.X + pad, y, rect.Width - 2 * pad, fontHeader.GetHeight(g));
            using (var bTitle = new SolidBrush(titleColor))
                g.DrawString(title, fontHeader, bTitle, headerRect, fmtCenter);
            y += headerRect.Height + 4f;
            g.DrawLine(penSep, rect.X + pad, y, rect.Right - pad, y);
            y += 8f;

            // Largeur labels
            float maxLW = 0f;
            foreach (string l in new[] { "Vrms", "Vpp", "Vdc", "Freq", "Per" })
            { float w2 = g.MeasureString(l, fontBase).Width; if (w2 > maxLW) maxLW = w2; }
            float valueLeft = xL + maxLW + 8f;
            float valueW    = rect.Right - pad - valueLeft;

            if (!m.Valid)
            {
                foreach (string lbl in new[] { "Vrms", "Vpp", "Vdc", "Freq", "Per" })
                {
                    g.DrawString(lbl, fontBase, brushText, xL, y);
                    g.DrawString("---", fontBase, brushText,
                        new RectangleF(valueLeft, y, valueW, lineH), fmtRight);
                    y += lineH;
                }
                y += 4f; return;
            }

            double sc = cal ? Math.Pow(10, offDb / 20.0) * Math.Sqrt(2.0) : 1.0;
            string Fv(double v) => cal ? FormatV(v * sc) : $"{v:G4}";

            // ── Helper : dessine une ligne, retourne la nouvelle valeur de y ─────
            float DrawRow(float cy, string lbl, string val, Font fVal, bool highlight)
            {
                float rh = Math.Max(fontBase.GetHeight(g), fVal.GetHeight(g)) + 4f;
                var valRect = new RectangleF(valueLeft, cy, valueW, rh);
                g.DrawString(lbl, fontBase, brushText, xL, cy);
                if (highlight)
                {
                    using var fmtRange = (StringFormat)fmtRight.Clone();
                    fmtRange.SetMeasurableCharacterRanges(new[] { new CharacterRange(0, val.Length) });
                    var regions = g.MeasureCharacterRanges(val, fVal, valRect, fmtRange);
                    var bounds  = regions[0].GetBounds(g);
                    bounds.Inflate(4f, 0.5f);
                    using var fill = new SolidBrush(Color.FromArgb(18, 30, 144, 255));
                    g.FillRectangle(fill, bounds);
                }
                g.DrawString(val, fVal, brushText, valRect, fmtRight);
                return cy + rh;
            }

            y = DrawRow(y, "Vrms", Fv(m.Vrms), fontStrong, highlight: true);
            y = DrawRow(y, "Vpp",  Fv(m.Vpp),  fontBase,   highlight: false);
            y = DrawRow(y, "Vdc",  Fv(m.Vdc),  fontBase,   highlight: false);

            string freqStr = m.FreqHz > 0
                ? (m.FreqHz >= 1e6  ? $"{m.FreqHz / 1e6:G4} MHz"
                 : m.FreqHz >= 1000 ? $"{m.FreqHz / 1000:G4} kHz"
                                   : $"{m.FreqHz:G4} Hz")
                : "---";
            y = DrawRow(y, "Freq", freqStr, m.FreqHz > 0 ? fontStrong : fontBase, highlight: m.FreqHz > 0);

            string perStr = m.FreqHz > 0
                ? (m.FreqHz < 1    ? $"{1000 / m.FreqHz:G4} s"
                 : m.FreqHz < 1000 ? $"{1000 / m.FreqHz:G4} ms"
                                   : $"{1e6 / m.FreqHz:G4} µs")
                : "---";
            y = DrawRow(y, "Per", perStr, fontBase, highlight: false);

            y += 4f;
        }

        /// Section curseurs X/Y dans le panneau de mesures (retourne y mis à jour).
        private float DrawCursorMeasSection(Graphics g, RectangleF rect, float y)
        {
            using var fontBase  =new Font("Segoe UI",8.25f);
            using var fontHeader=new Font("Segoe UI",8.25f,FontStyle.Bold);
            using var brushText =new SolidBrush(Color.DimGray);
            using var penSep    =new Pen(Color.Gainsboro,1f);
            using var fmtCenter =new StringFormat{Alignment=StringAlignment.Center};
            using var fmtRight  =new StringFormat{Alignment=StringAlignment.Far,LineAlignment=StringAlignment.Near};
            var (calOk,offDb)=GetActiveCal();
            Color curCol=CursorColor;
            float pad=8f, xL=rect.X+pad;
            float lineH=fontBase.GetHeight(g)+4f;
            string[] lbls={"X1","X2","\u0394X","1/\u0394X","Y1","Y2","\u0394Y"};
            float maxLW=0f;
            foreach(var l in lbls){float w2=g.MeasureString(l,fontBase).Width;if(w2>maxLW)maxLW=w2;}
            float valLeft=xL+maxLW+6f, valW=rect.Right-pad-valLeft;
            g.DrawLine(penSep,rect.X+pad,y,rect.Right-pad,y); y+=8f;
            void DrawRow(string l,string v){
                g.DrawString(l,fontBase,brushText,xL,y);
                g.DrawString(v,fontBase,brushText,new RectangleF(valLeft,y,valW,lineH),fmtRight);
                y+=lineH;}
            if (_xActive)
            {
                var hr=new RectangleF(rect.X+pad,y,rect.Width-2*pad,fontHeader.GetHeight(g));
                using var bH=new SolidBrush(curCol);
                g.DrawString("Cursors X",fontHeader,bH,hr,fmtCenter); y+=hr.Height+4f;
                double t1=XFracToMs(_x1Frac),t2=XFracToMs(_x2Frac),dt=t2-t1;
                DrawRow("X1",FormatTime(t1)); DrawRow("X2",FormatTime(t2));
                DrawRow("\u0394X",FormatTime(dt));
                string df="---";
                if(Math.Abs(dt)>1e-9){double hz=1000.0/Math.Abs(dt);df=hz>=1000?$"{hz/1000:G4} kHz":$"{hz:G4} Hz";}
                DrawRow("1/\u0394X",df); y+=4f;
            }
            if (_yActive)
            {
                var hr=new RectangleF(rect.X+pad,y,rect.Width-2*pad,fontHeader.GetHeight(g));
                using var bH=new SolidBrush(curCol);
                g.DrawString("Cursors Y",fontHeader,bH,hr,fmtCenter); y+=hr.Height+4f;
                float n1=YFracToNorm(_y1Frac),n2=YFracToNorm(_y2Frac);
                string FmtY(float n){
                    if(calOk){double sc=Math.Pow(10,offDb/20.0)*Math.Sqrt(2.0);return FormatV((double)n*sc);}
                    return $"{n:+0.0000;-0.0000}";}
                DrawRow("Y1",FmtY(n1)); DrawRow("Y2",FmtY(n2));
                DrawRow("\u0394Y",FmtY(n2-n1)); y+=4f;
            }
            return y;
        }

        // ─────────────────────────────────────────────────────────────────────
        public OscilloscopeSettings GetSettingsSnapshot()=>new OscilloscopeSettings
        {
            ChannelIndex=GetCh(),TimeDivIndex=cmbTimediv.SelectedIndex,
            TrigModeIndex=cmbTrigMode.SelectedIndex,TrigEdgeIndex=cmbTrigEdge.SelectedIndex,
            TrigSrcIndex=cmbTrigSrc.SelectedIndex,TrigLevel=(double)TrigLevelValue,
            ZoomYIndexL=cmbVdivL.SelectedIndex,ZoomYIndexR=cmbVdivR.SelectedIndex,
            AcCouplingL=cmbCouplingL.SelectedIndex,AcCouplingR=cmbCouplingR.SelectedIndex,
            RollMode=_rollActive,VdivMode=_vdivMode,GridModeIdx=cmbGrid.SelectedIndex
        };

        public void ApplySettings(OscilloscopeSettings? s)
        {
            if(s==null)return;
            _vdivMode=s.VdivMode; UpdateGainCombos();
            cmbTimediv.SelectedIndex =Math.Clamp(s.TimeDivIndex,0,TimeDivMs.Length-1);
            cmbTrigMode.SelectedIndex=Math.Clamp(s.TrigModeIndex,0,2);
            cmbTrigEdge.SelectedIndex=Math.Clamp(s.TrigEdgeIndex,0,1);
            cmbTrigSrc.SelectedIndex =Math.Clamp(s.TrigSrcIndex,0,1);
            // Restaure le niveau trigger dans la bonne plage selon le mode actif
            if(_vdivMode){
                double vd=GetVdivForCh(cmbTrigSrc.SelectedIndex);
                double hr=vd*(V_DIVS/2.0);
                SetTrigRange(-(decimal)hr, (decimal)hr, (decimal)Math.Max(hr/200.0, 0.00001), hr>=1.0?3:hr>=0.01?4:5);
                TrigLevelValue=(decimal)Math.Clamp(s.TrigLevel,-hr,hr);
            } else {
                TrigLevelValue=(decimal)Math.Clamp(s.TrigLevel,-1.0,1.0);
            }
            int maxZ=_vdivMode?VdivValues.Length-1:ZoomY.Length-1;
            cmbVdivL.SelectedIndex=Math.Clamp(s.ZoomYIndexL,0,maxZ);
            cmbVdivR.SelectedIndex=Math.Clamp(s.ZoomYIndexR,0,maxZ);
            cmbCouplingL.SelectedIndex=Math.Clamp(s.AcCouplingL,0,2);
            cmbCouplingR.SelectedIndex=Math.Clamp(s.AcCouplingR,0,2);
            cmbGrid.SelectedIndex=Math.Clamp(s.GridModeIdx,0,2);
            UpdateVdivButton();
            _cursorChan = _scaleChannel + 1;  // sync with scale channel at load
            UpdateRollCheckbox();
            if(_rollEnabled){_rollActive=s.RollMode;UpdateRollButton();}
        }

        protected override void OnHandleDestroyed(EventArgs e){_timer.Stop();_timer.Dispose();base.OnHandleDestroyed(e);}

        /// <summary>Retourne le panel de rendu pour copie presse-papier (résolution écran).</summary>
        public System.Windows.Forms.Control GetPanelForClipboard() => pnlScope;

        // ── Export PNG ────────────────────────────────────────────────────────
        // ── Export CSV ───────────────────────────────────────────────────────
        public System.Text.StringBuilder GetCsvData(char sep, bool header, char decSep = '.')
        {
            var sb  = new System.Text.StringBuilder();
            var ic2 = System.Globalization.CultureInfo.InvariantCulture;
            // Formate un double avec le séparateur décimal choisi
            string Fmt(double v, string fmt) {
                var s2 = v.ToString(fmt, ic2);
                return decSep == ',' ? s2.Replace('.', ',') : s2;
            }
            string s = sep.ToString();

            // Exporte toutes les voies qui ont des données affichées
            bool hasL = _dispL != null && _dispL.Length > 0;
            bool hasR = _dispR != null && _dispR.Length > 0;
            if (!hasL && !hasR) return sb;

            int n = Math.Max(hasL ? _dispL!.Length : 0, hasR ? _dispR!.Length : 0);
            double dt = _capSR > 0 ? 1.0 / _capSR : 0;

            // Calibration → conversion en Volts si disponible
            double calL = 1.0, calR = 1.0;
            bool voltMode = _vdivMode && GetInputCalibration != null;
            if (voltMode)
            {
                var (vL, oL, vR, oR) = GetInputCalibration!();
                if (vL) calL = Math.Pow(10, oL / 20.0) * Math.Sqrt(2.0);
                if (vR) calR = Math.Pow(10, oR / 20.0) * Math.Sqrt(2.0);
            }
            string unitL = voltMode ? "V" : "norm";
            string unitR = voltMode ? "V" : "norm";

            if (header)
            {
                sb.Append("Time_s");
                if (hasL) sb.Append($"{s}Ch_L_{unitL}");
                if (hasR) sb.Append($"{s}Ch_R_{unitR}");
                sb.AppendLine();
            }

            for (int i = 0; i < n; i++)
            {
                sb.Append(Fmt(i * dt, "G6"));
                if (hasL) sb.Append($"{s}{(i < _dispL!.Length ? Fmt(_dispL[i] * calL, "G6") : "")}");
                if (hasR) sb.Append($"{s}{(i < _dispR!.Length ? Fmt(_dispR[i] * calR, "G6") : "")}");
                sb.AppendLine();
            }
            return sb;
        }

        public void ExportPng(string filePath, int scale = 2, string? headerTitle = null, string? footerText = null)
        {
            _isExporting = true;
            using var capture = ChartExport.CaptureControl(pnlScope, scale);
            _isExporting = false;
            int headerPx  = ChartExport.HeaderPixels(headerTitle, scale);

            // Le graphique oscillo n'a pas de marge blanche en bas.
            // On en ajoute une pour que le footer soit visible hors du graphique.
            int footerMargin = string.IsNullOrWhiteSpace(footerText) ? 0 : (int)(28 * scale);

            if (footerMargin > 0)
            {
                using var expanded = new System.Drawing.Bitmap(
                    capture.Width, capture.Height + footerMargin);
                using (var g = System.Drawing.Graphics.FromImage(expanded))
                {
                    g.Clear(System.Drawing.Color.White);
                    g.DrawImage(capture, 0, 0);
                }
                ChartExport.SaveChartPng(
                    expanded, filePath, headerPx, headerTitle,
                    scale, dpi: 300, footerText: footerText);
            }
            else
            {
                ChartExport.SaveChartPng(
                    capture, filePath, headerPx, headerTitle,
                    scale, dpi: 300, footerText: footerText);
            }
        }

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
