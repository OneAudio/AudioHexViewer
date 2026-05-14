// ═══════════════════════════════════════════════════════════════════════════
// SpectrumControl.cs  —  Shell du contrôle + état central + UpdateSpectrumChartAndStats
//
// La classe SpectrumControl est découpée en fichiers partiels (Phase 4) :
//   SpectrumControl.Pipeline.cs   — FeedExternalSpectrum, FeedExternalTimeStats24, Decimate
//   SpectrumControl.Rendering.cs  — Init chart, axes, thème, dialogs, export
//   SpectrumControl.PostPaint.cs  — Dessin GDI+ custom (PostPaint, stats panel)
//   SpectrumControl.Cursors.cs    — Curseurs fréquence + détection de pics
//   SpectrumControl.Overlays.cs   — Bibliothèque d'overlays
//   SpectrumControl.Bode.cs       — Mode FRF / Bode Plot
//   Core/DspMath.cs               — Maths DSP pures (ComplexBinsToDbFs, etc.)
// ═══════════════════════════════════════════════════════════════════════════
using AudioHexViewer.Core;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MathNet.Numerics.IntegralTransforms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using static AudioHexViewer.SpectrumControl;
using static AudioHexViewer.SpectrumPostProcessor;
using ChartSeries = System.Windows.Forms.DataVisualization.Charting.Series;


namespace AudioHexViewer
{
    public partial class SpectrumControl : UserControl
    {
        // --------------------------------------------------------------------
        // Overlay UI: "Axis" button (bottom-left of the chart)
        // Opens the same "Axis Parameters" dialog as the right-click on the chart.
        // IMPORTANT: UI-only (no FFT / no scheduling).
        // --------------------------------------------------------------------
        private Button? _btnAxis;
        private ToolTip? _toolTipAxis;


        // --------------------------------------------------------------------
        // THD UI (checkbox + button in the Spectrum tab)
        // --------------------------------------------------------------------
        public event Action<bool>? ThdEnabledChanged;
        public event Action? ThdSettingsRequested;

        // --------------------------------------------------------------------
        // Channel UI (mode Left/Right/L+R/Bode Plot) => informe l'extérieur (worker)
        // --------------------------------------------------------------------
        public event Action<string>? ChannelModeChanged;

        string phi = "\u03C6"; // 

        private bool _suppressThdUiEvents = false;
        internal bool IsXAxisLog() => _xAxisIsLog;

        internal int GetPlotPixelWidth()
        {
            // fallback simple : largeur du contrôle
            // si tu as un panel/zone dédiée au plot, remplace par sa largeur
            return Math.Max(100, this.Width);
        }
        //
        internal (double fMin, double fMax) GetVisibleFreqRangeHz()
        {
            // Si le chart n'est pas prêt, fallback bande entière
            double nyq = _sampleRate > 0 ? _sampleRate / 2.0 : 24000.0;

            if (chartSpectrum == null || chartSpectrum.ChartAreas.Count == 0)
                return (0.0, nyq);

            var area = chartSpectrum.ChartAreas[0];
            var ax = area.AxisX;

            // Cas zoom (ScaleView activé)
            if (ax.ScaleView != null && ax.ScaleView.IsZoomed)
            {
                double vmin = ax.ScaleView.ViewMinimum;
                double vmax = ax.ScaleView.ViewMaximum;
                if (vmax > vmin)
                    return (Math.Max(0.0, vmin), Math.Min(nyq, vmax));
            }

            // Sinon, la plage affichée courante (Minimum/Maximum)
            double min = double.IsNaN(ax.Minimum) ? 0.0 : ax.Minimum;
            double max = double.IsNaN(ax.Maximum) ? nyq : ax.Maximum;

            if (max <= min)
                return (0.0, nyq);

            return (Math.Max(0.0, min), Math.Min(nyq, max));
        }

        //
        // --- Unités volts pour l’affichage RMS/Peak ---
        private enum VoltUnit { V, mV, uV }
        private VoltUnit _vrmsUnitLeft = VoltUnit.mV;
        private VoltUnit _vrmsUnitRight = VoltUnit.mV;

        // Dernières unités utilisées (pour éviter de rafraîchir inutilement l’UI) 
        private VoltUnit _vpkUnitLeft = VoltUnit.mV;
        private VoltUnit _vpkUnitRight = VoltUnit.mV;
        private VoltUnit _vdcUnitLeft = VoltUnit.mV;
        private VoltUnit _vdcUnitRight = VoltUnit.mV;

        // Clamp utility for Threshold Peak detection
        private static double Clamp(double v, double min, double max)
        => (v < min) ? min : (v > max) ? max : v;


        /// <summary>
        /// Sync the THD checkbox UI without re-triggering events.
        /// </summary>
        public void SetThdEnabledUi(bool enabled)
        {
            if (chkEnableThd == null)
                return;

            _suppressThdUiEvents = true;
            try
            {
                chkEnableThd.Checked = enabled;
                // Visuel
                UpdateToggleButtonVisual(chkEnableThd);
            }
            finally
            {
                _suppressThdUiEvents = false;
            }
        }

        // --- Couleurs / thème du graphe (persistées via SessionSettings.Spectrum.Colors) ---
        private SpectrumColorSettings _colors = new SpectrumColorSettings();

        private Color FftLeftColor => Color.FromArgb(_colors.FftLeftArgb);
        private Color FftRightColor => Color.FromArgb(_colors.FftRightArgb);
        private Color PeakHoldLeftColor => Color.FromArgb(_colors.PeakHoldLeftArgb);
        private Color PeakHoldRightColor => Color.FromArgb(_colors.PeakHoldRightArgb);

        private Color GridMajorColor => Color.FromArgb(_colors.GridArgb);
        private Color GridMinorColor => Color.FromArgb(_colors.GridMinorArgb);
        private Color BackgroundColor => Color.FromArgb(_colors.BackgroundArgb);

        private Color LabelMajorColor => Color.FromArgb(_colors.LabelMajorArgb);
        private Color LabelMinorColor => Color.FromArgb(_colors.LabelMinorArgb);

        public Label FftTickerLabel => lblFftTicker;


        private Color InfoTextColor => Color.FromArgb(_colors.InfoTextArgb);

        // Couleur d’un overlay selon son index de slot
        //
        private ChartDashStyle GetOverlayDashStyle()
        {
            var v = (ChartDashStyle)_colors.OverlayDashStyle;
            if (!Enum.IsDefined(typeof(ChartDashStyle), v))
                return ChartDashStyle.Dot; // comportement par défaut actuel
            return v;
        }

        //
        private ChartDashStyle GetOverlayLineStyle()
        {
            var v = (ChartDashStyle)_colors.OverlayDashStyle;
            if (!Enum.IsDefined(typeof(ChartDashStyle), v))
                return ChartDashStyle.Dot;
            return v;
        }

        //
        private sealed class ExportRenderContext
        {
            public string? HeaderTitle { get; init; }
            public int HeaderPx { get; init; }
        }

        //
        public SpectrumColorSettings GetSpectrumColorSettingsSnapshot()
            => _colors ?? new SpectrumColorSettings();


        // Source de vérité : le mode sélectionné dans le contrôle Channel
        public string SelectedChannelMode => comboChannel.SelectedItem as string ?? "L+R";

        // FRF / Bode : Right = entrée, Left = sortie

        public bool IsLeftChannelActive =>
        SelectedChannelMode == "Left" || SelectedChannelMode == "L+R" || SelectedChannelMode == "Bode Plot";

        public bool IsRightChannelActive =>
        SelectedChannelMode == "Right"
        || SelectedChannelMode == "L+R"
        || SelectedChannelMode == "Bode Plot"; // IMPORTANT: FRF/H1 => besoin des 2 voies




        // Applique les couleurs du thème au contrôle Spectrum.
        public void ApplySpectrumColorSettings(SpectrumColorSettings? colors)
        {
            _colors = colors ?? SpectrumColorSettings.Defaults();
            _colors.Normalize();

            // Appliquer au Chart (fond + grilles) + éléments UI (curseurs, stats, etc.)
            ApplyChartTheme();
            ApplyCursorTheme();
            UpdateSpectrumChartAndStats();
        }




        //
        // 
        //private bool _cursorUiBindDone;
        //private Font? _cursorBtnRegular;
        //private Font? _cursorBtnBold;

        private int _sampleRate;
        // Pas de rafraîchissement en samples (hop). 0 => pas de hop (mise à jour = taille fenêtre)
        private int _hopSamples = 0;

        private readonly SpectrumPostProcessor _post = new SpectrumPostProcessor();

        // ===== Overlays V3 (bibliothèque interne) =====
        // Cache en mémoire pour éviter de relire les .overlay.json à chaque rafraîchissement du chart.
        private readonly OverlayLibrary _overlayLibrary = new OverlayLibrary();

        private OverlaysForm? _overlaysForm;

        private sealed class CachedOverlay
        {
            public OverlayEntry Entry;
            public OverlayFile Data;
            public bool Visible;

            public CachedOverlay(OverlayEntry entry, OverlayFile data, bool visible)
            {
                Entry = entry;
                Data = data;
                Visible = visible;
            }
        }

        private readonly List<CachedOverlay> _overlayCache = new List<CachedOverlay>();

        // Json options identiques à OverlayLibrary (enum en string)
        private readonly JsonSerializerOptions _overlayJsonOptions = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        };


        private readonly SpectrumMetricsOptions _metricsOpt = new SpectrumMetricsOptions();

        private SpectrumSettings.ThdSettings _thdUi = new SpectrumSettings.ThdSettings();

        private int lastFftSize;
        private int? _forcedFftSize = null;
        private WindowType lastWindow = WindowType.Rectangular;

        // Expose la fenêtre réellement sélectionnée (utile pour le worker externe SFFT)
        public WindowType SelectedWindowType => lastWindow;


        // Averaging
        private int avgTargetCount = 1;

        // --- Exponential averaging (magnitude-domain) ---
        private bool _avgIsExponential = false;
        private double _avgExpAlpha = 0.0;             // ex: 0.94

        // Spectres courants & axe fréquence
        private double[]? currentLeftSpectrum;
        private double[]? currentRightSpectrum;
        // --- FRF Phase (degrés) ---
        // Utilisé UNIQUEMENT en mode "Bode Plot" (Right=entrée, Left=sortie)
        private double[]? currentFrfPhaseDeg;

        // FRF (Bode) UI toggles
        private bool _frfPhaseEnabled = false;   // bouton "enable φ" : OFF par défaut
        private bool _frfPhaseUnwrap = false;    // bouton "unwrap φ" : OFF => wrap par défaut

        private double[]? freqAxis;

        // --- Données FULL (résolution complète) : utilisées pour métriques / curseurs / estimations ---
        //private double[]? freqAxis;                 // déjà présent plus bas dans ton fichier, garde-le tel quel
        //private double[]? currentLeftSpectrum;
        //private double[]? currentRightSpectrum;

        // --- Données PLOT (décimées) : utilisées UNIQUEMENT pour le tracé (chart) ---
        private double[]? _plotFreqAxis;
        private double[]? _plotLeftSpectrum;
        private double[]? _plotRightSpectrum;
        // Phase décimée pour le tracé (AxisY2) — uniquement en mode "Bode Plot"
        private double[]? _plotFrfPhaseDeg;

        // ------------------------------------------------------------------
        // < / > peak navigation threshold (based on visible noise floor)
        // Editable via right-click on < or > buttons.
        // ------------------------------------------------------------------
        private const double NoiseToPeakThresholdMinDb = 4.0;
        private const double NoiseToPeakThresholdMaxDb = 100.0;

        // Default = 12 dB (as requested)
        private double _noiseToPeakThresholdDb = 12.0;

        private double NoiseToPeakThresholdDb
        {
            get => _noiseToPeakThresholdDb;
            set
            {
                if (double.IsNaN(value) || double.IsInfinity(value))
                    return;

                _noiseToPeakThresholdDb = Math.Max(NoiseToPeakThresholdMinDb,
                                       Math.Min(NoiseToPeakThresholdMaxDb, value));
            }
        }


        private double[]? _plotPeakLeft;
        private double[]? _plotPeakRight;


        // --- Calibration niveau FFT (dBFS -> dBV) ---
        // Convention utilisée dans le projet :
        // - Les courbes FFT sont en "dBFS" avec 0 dBFS pour une sinusoïde full-scale (peak FS).
        // - Après calibration : dBV = dBFS + offset, où 0 dBV = 1 Vrms.
        private bool _levelCalValidLeft = false;
        private bool _levelCalValidRight = false;
        private double _levelCalOffsetDbLeft = 0.0;
        private double _levelCalOffsetDbRight = 0.0;

        private string _levelUnit = "dBFS"; // "dBFS" ou "dBV"

        // Dernières mesures RMS en dBFS (issues du buffer temps brut)
        private double? _lastRmsDbfsLeft = null;
        private double? _lastRmsDbfsRight = null;

        // Dernières mesures RMS en FS (AC, DC retiré) — base de calibration/volts linéaires
        private double? _lastRmsFsLeft = null;
        private double? _lastRmsFsRight = null;

        // Dernières mesures Peak en FS (DC compris)
        private double? _lastPeakFsLeft = null;
        private double? _lastPeakFsRight = null;

        // Gain linéaire Vrms / FS_rms (AC) — dérivé de la calibration
        private double _levelGainVrmsPerFsLeft = 0.0;
        private double _levelGainVrmsPerFsRight = 0.0;

        // Dernières composantes DC en volts (utile pour l'affichage dans le panneau mesures)
        private double? _lastVdcVoltsLeft = null;
        private double? _lastVdcVoltsRight = null;

        // Dernières mesures linéaires en volts (affichage)
        private double? _lastVrmsVoltsLeft = null;
        private double? _lastVrmsVoltsRight = null;
        private double? _lastVpkVoltsLeft = null;
        private double? _lastVpkVoltsRight = null;

        // --- Exposition des mesures en volts (pour Form1 / affichage principal) ---
        public bool VoltageCalibrationValidLeft => _levelCalValidLeft;
        public bool VoltageCalibrationValidRight => _levelCalValidRight;

        /// <summary>
        /// Retourne les dernières mesures en volts (linéaires) calculées par SpectrumControl.
        /// Valeurs null si non disponibles (ex: pas de calibration ou pas encore de mesure).
        /// Vrms = RMS AC (DC retiré), Vpeak = max(|sample|) sur la fenêtre (DC compris), Vdc = moyenne (DC).
        /// </summary>
        public void GetLastVoltages(
        out double? vrmsLeft, out double? vpeakLeft, out double? vdcLeft,
        out double? vrmsRight, out double? vpeakRight, out double? vdcRight)
        {
            vrmsLeft = _lastVrmsVoltsLeft;
            vpeakLeft = _lastVpkVoltsLeft;
            vdcLeft = _lastVdcVoltsLeft;

            vrmsRight = _lastVrmsVoltsRight;
            vpeakRight = _lastVpkVoltsRight;
            vdcRight = _lastVdcVoltsRight;
        }

        // --- Indicateur "a déjà fonctionné au moins une fois" ---
        private bool _hasEverRun = false;
        public bool HasEverRun => _hasEverRun;



        // Layout original des labels "Meas" (pour pouvoir insérer des lignes Volts en mode dBV)
        private bool _voltLayoutInit = false;
        private System.Drawing.Point _leftMeasOrigLoc;
        private System.Drawing.Size _leftMeasOrigSize;
        private System.Drawing.Point _rightMeasOrigLoc;
        private System.Drawing.Size _rightMeasOrigSize;

        private bool isLive = false;

        // Axes personnalisés (null = auto)
        private double? customYMin = null;
        private double? customYMax = null;
        private double? customFMin = null;   // en Hz
        private double? customFMax = null;   // en Hz

        private double? customPhaseMin = null; // deg
        private double? customPhaseMax = null; // deg

        // Derniers buffers temporels utilisés (pour les stats)
        private double[]? lastTimeLeft = null;
        private double[]? lastTimeRight = null;

        // ── Optimisation rendu (Phase 5) ──────────────────────────────────────────
        // Séries pré-allouées : évite new Series() + N×AddXY() par frame.
        // Initialisées paresseusement à la 1ère frame (EnsureSeriesInited).
        private Series? _serLeft;
        private Series? _serRight;
        private Series? _serPeakLeft;
        private Series? _serPeakRight;
        private Series? _serPhase;
        private bool    _seriesInited;

        // ── Cache axe (Phase 6) ────────────────────────────────────────────────────
        // Skip les 84 assignments area.Axis* si les paramètres n'ont pas changé.
        // Réinitialisé quand l'utilisateur change une limite, le SR, ou la taille du chart.
        private double _axCachedFmin   = double.NaN;
        private double _axCachedFmax   = double.NaN;
        private double _axCachedYmin   = double.NaN;
        private double _axCachedYmax   = double.NaN;
        private double _axCachedYminor = double.NaN;
        private bool   _axCachedUseLog = false;
        private System.Drawing.Size _axCachedChartSize;

        /// <summary>Invalide le cache d'axe — à appeler quand l'utilisateur change
        /// une limite custom, le mode log/lin, la résolution FFT ou la fréquence d'échantillonnage.</summary>
        // ── Buffers DSP pré-alloués pour FeedExternalSpectrum (Phase 7) ─────────────
        // Évite 4 × new double[N/2+1] par frame (jusqu'à 64 MB/frame à 384kHz/N=4M).
        // Réalloués uniquement si N change (changement de config).
        private double[]? _binsPowL;   // ComplexBinsToPowerInto output L
        private double[]? _binsPowR;   // ComplexBinsToPowerInto output R
        private double[]? _binsDbL;    // PowerToDbFsInto output L (= currentLeftSpectrum)
        private double[]? _binsDbR;    // PowerToDbFsInto output R (= currentRightSpectrum)

        private static double[] EnsureDspBuf(ref double[]? buf, int n)
        {
            if (buf == null || buf.Length != n)
                buf = new double[n];
            return buf;
        }

                internal void InvalidateAxisCache()
        {
            _axCachedFmin   = double.NaN;
            _axCachedYmin   = double.NaN;
        }


        private TextAnnotation? _overlayInfoAnno;

        // Annotations curseur
        private VerticalLineAnnotation? _cursorLineL;
        //private TextAnnotation? _cursorTextL;

        private VerticalLineAnnotation? _cursorLineR;
        //private TextAnnotation? _cursorTextR;

        // Labels (texte curseur) affichés en bas du chart
        private Label? _lblCursorLInfo;
        private Label? _lblCursorRInfo;
        private Label? _lblCursorDeltaInfo; // NEW



        // --- Etat curseurs (indépendants) ---
        private bool _cursorLockedL = false;
        private bool _cursorLockedR = false;

        // index bin courant (snap) pour chaque curseur
        private int _cursorIdxL = -1;
        private int _cursorIdxR = -1;

        // curseur "actif" (celui que l’on déplace quand les deux sont déverrouillés)
        private enum ActiveCursor { Left, Right }
        //private ActiveCursor _activeCursor = ActiveCursor.Left;

        // tolérance "clic sur curseur" en pixels
        private const int CursorHitTolPx = 10;

        private double _cursorFreqL = double.NaN;
        private double _cursorFreqR = double.NaN;

        // marques curseur visibles
        private bool _cursorMarkLVisible;
        private bool _cursorMarkRVisible;
        private double _cursorMarkLFreq, _cursorMarkLDb;
        private double _cursorMarkRFreq, _cursorMarkRDb;

        // true => la valeur Y est à lire/dessiner sur AxisY2 (ex: phase en mode Bode Plot)
        private bool _cursorMarkLUsesY2;
        private bool _cursorMarkRUsesY2;

        private double _cursorTargetHzL = 1000.0; // valeur init au choix
        private double _cursorTargetHzR = 1000.0;

        public event EventHandler? WindowTypeChangedForExternalFft;

        // État axe X logarithmique (pour redessin manuel des sous-divisions)
        private bool _xAxisIsLog = false;

        // Ligne d'info en bas (stats)
        private string _TopInfoLine = "";

        // Indicateur de restauration des settings (évite d’écrire dans les settings pendant la restauration)
        private bool _isRestoringSettings = false;

        // Pour le dessin manuel des sous-divisions Y
        private double _currentYMinor = 1.0;
        private double _currentYMajor = 5.0;

        // Événement Run demandé (par le bouton Run)
        public event Action<bool>? RunStateChanged; // true=RUN, false=STOP
        public event Action? RunRequested;

        // --- Options curseurs ---
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool CursorLeftEnabled
        {
            get => chkCursorL.Checked;
            set => chkCursorL.Checked = value;
        }
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool CursorRightEnabled
        {
            get => chkCursorR.Checked;
            set => chkCursorR.Checked = value;
        }

        public enum CursorTrackMode { Auto, Left, Right }

        private CursorTrackMode _c1TrackMode = CursorTrackMode.Auto;
        private CursorTrackMode _c2TrackMode = CursorTrackMode.Auto;
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public CursorTrackMode C1TrackMode
        {
            get => _c1TrackMode;
            set { _c1TrackMode = value; chartSpectrum?.Invalidate(); }
        }
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public CursorTrackMode C2TrackMode
        {
            get => _c2TrackMode;
            set { _c2TrackMode = value; chartSpectrum?.Invalidate(); }
        }


        private bool _statsShowLeft = true;
        private bool _statsShowRight = true;

        private bool _thdMeasuresEnabled = false;

        // ── Spectre issu du dernier cycle de moyennage cohérent (THD) ────────
        // Mis à jour à chaque fois que CohAvgCount atteint CohAvgTarget.
        // Utilisé à la place de currentLeftSpectrum/currentRightSpectrum pour
        // ComputeChannel() quand _thdUi.UseCohAvg est actif.
        private double[]? _cohAvgLeftDb;
        private double[]? _cohAvgRightDb;

        // --- Étape 2 : panneau stats dans le chart (zone dédiée à droite) ---
        private const int ChartStatsWidthPx = 200;      // largeur du panneau stats
        private const int ChartStatsMinPlotWidthPx = 420; // largeur mini du graphe (évite d’écraser le plot)

        //     
        // Formatage axe Y (dBFS), divisions secondaires
        //
        // Calcul du rectangle "plot" (interne) en pixels
        private static RectangleF GetPlotRectPixels(Chart chart, ChartArea area)
        {
            var cr = chart.ClientRectangle;
            if (cr.Width <= 0 || cr.Height <= 0) return RectangleF.Empty;

            // Rectangle externe de la ChartArea (en pixels)
            var pos = area.Position;
            RectangleF outer = new RectangleF(
                cr.Width * pos.X / 100f,
                cr.Height * pos.Y / 100f,
                cr.Width * pos.Width / 100f,
                cr.Height * pos.Height / 100f
            );

            // Rectangle interne (plot) de la ChartArea (en pixels)
            var ip = area.InnerPlotPosition;

            // Si Auto/valeurs non initialisées, fallback sur outer
            if (ip.Width <= 0f || ip.Height <= 0f)
                return outer;

            RectangleF inner = new RectangleF(
                outer.X + outer.Width * ip.X / 100f,
                outer.Y + outer.Height * ip.Y / 100f,
                outer.Width * ip.Width / 100f,
                outer.Height * ip.Height / 100f
            );

            return inner;
        }

        //
        // Échelle des labels secondaires X et Y (95% de la taille normale)
        private const float MinorLabelScale = 0.9f;
        /// <summary>Plancher absolu de fréquence en mode log (évite log(0)).
        /// Permet d'afficher jusqu'à ~0,001 Hz si la résolution FFT le permet.</summary>
        private const double LogXAbsMinHz = 1e-6;

        //
        // Choix du meilleur pas exact (sans ajustement de span)

        public SpectrumControl()
        {
            InitializeComponent();


            EnsureVoltLayoutInit();
            InitChartAppearance();
            this.Load += (_, __) =>
            {
                // 1) Première passe : réserve la place (ok même si layout pas final)
                ApplyChartStatsMargin();

                // 2) Deuxième passe après layout Windows : indispensable pour que le panneau s’affiche dès le démarrage en Bode
                if (IsHandleCreated)
                {
                    BeginInvoke(new Action(() =>
                    {
                        ApplyChartStatsMargin();
                        RebuildStatsRowsFromLabels();   // pour éviter un panneau “vide” selon l’état des labels
                        chartSpectrum.Invalidate();
                    }));
                }
            };

            chartSpectrum.Resize += (_, __) => ApplyChartStatsMargin();
            InitializeUiOptions();
            ApplyThdSettings(_thdUi);
            UpdateRunButtonAppearance();
            InitOverlayAnnotation();
            OverlayLibraryReloadCache();


            // "Axis" button in the bottom-left corner of the chart (UI helper).
            // Keeps the existing right-click access to the same dialog.
            InitAxisButtonUi();

            // Curseurs
            InitCursorBarUi();

            // FRF (Bode) extra buttons
            btnPhiEnable.Click += BtnPhiEnable_Click;
            btnPhiUnwrap.Click += BtnPhiUnwrap_Click;

            // état initial (phase OFF, wrap ON)
            ApplyFrfPhiButtonsUi();

            // Clic droit sur la zone du spectre -> ouverture de la boîte d'axes
            chartSpectrum.MouseUp += ChartSpectrum_MouseUp;

            // Dessin manuel des sous-divisions log
            chartSpectrum.FormatNumber += ChartSpectrum_FormatNumber;

            chartSpectrum.PostPaint -= ChartSpectrum_PostPaint;
            chartSpectrum.PostPaint += ChartSpectrum_PostPaint;


            InitCursors();
            chartSpectrum.MouseMove += ChartSpectrum_MouseMove;
            chartSpectrum.MouseDoubleClick += ChartSpectrum_MouseDoubleClick;
            chartSpectrum.MouseLeave += (s, e) => { /* optionnel : cacher texte */ };

            // IMPORTANT : au démarrage, aucune frame n'a encore été injectée via FeedExternalSpectrum().
            // Sans spectre initial, le chart reste "blanc" jusqu'à la première acquisition.
            InitializeEmptySpectrumDisplay();

            // Redessine une fois après le Load pour garantir un rendu même si la taille du contrôle
            // n'était pas encore valide pendant le constructeur.
            this.Load += (_, __) => UpdateSpectrumChartAndStats();


        }
        //
        /// <summary>
        /// Initialise un spectre "vide" (niveau plancher) pour forcer le rendu du graphe au démarrage.
        /// UI only : ne touche pas au worker FFT.
        /// </summary>



        private static double OffsetDbFromGain(double gainVrmsPerFs)
        {
            if (gainVrmsPerFs <= 0.0 || double.IsNaN(gainVrmsPerFs) || double.IsInfinity(gainVrmsPerFs))
                return 0.0;
            return 20.0 * Math.Log10(gainVrmsPerFs / Math.Sqrt(2.0));
        }

        private static double GainFromOffsetDb(double offsetDb)
        {
            return Math.Sqrt(2.0) * Math.Pow(10.0, offsetDb / 20.0);
        }

        // Affichage tension : V si >=1V, mV si [1mV..1V), µV si <1mV.
        // 4 chiffres significatifs (zéros conservés), sans notation scientifique.
        // Point d'entrée "simple" : choix d'unité sans hysteresis (stateless)
        private static string FormatVoltage(double volts)
        {
            VoltUnit dummy = VoltUnit.mV; // valeur initiale arbitraire
            return FormatVoltageCore(volts, ref dummy, useHysteresis: false);
        }

        // Point d'entrée "stable" : utilise l'état (unité précédente) pour hysteresis
        private static string FormatVoltage(double volts, ref VoltUnit unitState)
        {
            return FormatVoltageCore(volts, ref unitState, useHysteresis: true);
        }

        // Coeur unique : tout le formatage est ici (pas de duplication)
        private static string FormatVoltageCore(double volts, ref VoltUnit unitState, bool useHysteresis)
        {
            if (double.IsNaN(volts) || double.IsInfinity(volts)) return "—";

            // Zéro : format stable
            if (volts == 0.0) return "0.000 V";

            double abs = Math.Abs(volts);

            // --- Choix unité ---
            if (useHysteresis)
            {
                // Hystérésis autour de 1.0 V et 1 mV
                const double MV_TO_V_UP = 1.00;
                const double V_TO_MV_DN = 0.90;

                const double UV_TO_MV_UP = 1.00e-3;
                const double MV_TO_UV_DN = 0.90e-3;

                switch (unitState)
                {
                    case VoltUnit.V:
                        if (abs < V_TO_MV_DN) unitState = VoltUnit.mV;
                        break;

                    case VoltUnit.mV:
                        if (abs >= MV_TO_V_UP) unitState = VoltUnit.V;
                        else if (abs < MV_TO_UV_DN) unitState = VoltUnit.uV;
                        break;

                    case VoltUnit.uV:
                        if (abs >= UV_TO_MV_UP) unitState = VoltUnit.mV;
                        break;
                }
            }
            else
            {
                // Stateless : ancien comportement (seuils directs)
                if (abs >= 1.0) unitState = VoltUnit.V;
                else if (abs >= 1e-3) unitState = VoltUnit.mV;
                else unitState = VoltUnit.uV;
            }

            double scale;
            string unit;
            if (unitState == VoltUnit.V) { scale = 1.0; unit = "V"; }
            else if (unitState == VoltUnit.mV) { scale = 1e3; unit = "mV"; }
            else { scale = 1e6; unit = "µV"; }

            // --- Format "4 chiffres significatifs" (ton algo actuel) ---
            double v = volts * scale;
            double a = Math.Abs(v);

            int magnitude = (int)Math.Floor(Math.Log10(a));
            int decimals = 4 - 1 - magnitude;
            if (decimals < 0) decimals = 0;
            if (decimals > 6) decimals = 6;

            double rounded = Math.Round(v, decimals, MidpointRounding.AwayFromZero);
            string num = rounded.ToString("F" + decimals, CultureInfo.InvariantCulture);

            return $"{num} {unit}";
        }

        //
        //
        public void ApplyThdSettings(SpectrumSettings.ThdSettings s)
        {
            _thdUi = s ?? new SpectrumSettings.ThdSettings();

            // Bande
            _metricsOpt.MinFreqHz = _thdUi.UseHPF ? Math.Max(0.0, _thdUi.FlowHz) : 0.0;

            if (_thdUi.UseLPF)
            {
                double nyq = (_sampleRate > 0) ? (_sampleRate / 2.0) : 384000.0; // fallback
                _metricsOpt.MaxFreqHz = Math.Min(Math.Max(0.0, _thdUi.FhighHz), nyq);
            }
            else
            {
                _metricsOpt.MaxFreqHz = 0.0; // 0 => Nyquist
            }

            // Harmoniques (voir fix B : exclure le fondamental)
            int countExclFund = _thdUi.HarmonicsNumber;
            if (countExclFund < 1) countExclFund = 1;
            if (countExclFund > 19) countExclFund = 19;     // car on va ajouter +1 (fondamental) => max 20
            _metricsOpt.MaxHarmonics = countExclFund + 1;

            // Pas de recalcul FFT ici : le spectre vient du nouveau core (FeedExternalSpectrum).
            UpdateOverlayInfo();   // rafraîchit "Coherent Avg:" si UseCohAvg a changé
            RebuildStatsRowsFromLabels();
            chartSpectrum.Invalidate();

        }

        //
        public void SetThdMeasuresEnabled(bool enabled)
        {
            _thdMeasuresEnabled = enabled;

            UpdateOverlayInfo();   // rafraîchit "Coherent Avg:" / "Avg:" dans la ligne d'info
            RebuildStatsRowsFromLabels();
            chartSpectrum.Invalidate();
        }

        //
        private (double yMin, double yMax) GetCurrentYRangeGlobal()
        {
            if (freqAxis == null || freqAxis.Length == 0)
                return (-200.0, 0.0);

            if (chartSpectrum.ChartAreas.Count == 0)
                return (-200.0, 0.0);

            var area = chartSpectrum.ChartAreas["SpectrumArea"];

            // Plage X affichée (custom sinon axe)
            double fMin = customFMin ?? area.AxisX.Minimum;
            double fMax = customFMax ?? area.AxisX.Maximum;

            if (fMax <= fMin)
                return (-200.0, 0.0);

            // Trouver indices correspondant à la plage X
            int i0 = FindNearestFreqIndex(freqAxis, fMin);
            int i1 = FindNearestFreqIndex(freqAxis, fMax);

            if (i0 > i1) { int tmp = i0; i0 = i1; i1 = tmp; }

            // En log tu caches le bin 0 (k=0) (cf. startIndex=1) :contentReference[oaicite:2]{index=2}
            bool useLog = (chkLogX != null && chkLogX.Checked);
            if (useLog && i0 < 1) i0 = 1;

            // Sécurité bornes
            i0 = Math.Max(0, Math.Min(i0, freqAxis.Length - 1));
            i1 = Math.Max(0, Math.Min(i1, freqAxis.Length - 1));

            const double FloorDb = -199.9; // on ignore le plancher -200

            double min = double.PositiveInfinity;
            double max = double.NegativeInfinity;

            void Scan(double[]? arr)
            {
                if (arr == null) return;
                int n = Math.Min(arr.Length, freqAxis.Length);

                int a = Math.Min(i0, n - 1);
                int b = Math.Min(i1, n - 1);

                for (int i = a; i <= b; i++)
                {
                    double v = arr[i];
                    if (double.IsNaN(v) || double.IsInfinity(v)) continue;

                    // ignorer bins au plancher (sinon min reste collé à -200)
                    if (v <= FloorDb) continue;

                    if (v < min) min = v;
                    if (v > max) max = v;
                }
            }

            // Global sur 2 voies
            Scan(currentLeftSpectrum);
            Scan(currentRightSpectrum);

            // Si tout est au plancher (ou pas de data), fallback
            if (min == double.PositiveInfinity || max == double.NegativeInfinity)
                return (-200.0, 0.0);

            // Appliquer la même logique d'unité que l'affichage (dBFS / dBV)
            string modeForUnit = (comboChannel?.SelectedItem as string) ?? "L+R";
            bool useDbvAxis = UseDbvForMode(modeForUnit);

            if (useDbvAxis)
            {
                // Comme on peut afficher L et R avec des offsets différents, on couvre les 2
                var shifts = new List<double>();
                if (string.Equals(modeForUnit, "Left", StringComparison.OrdinalIgnoreCase) || string.Equals(modeForUnit, "L+R", StringComparison.OrdinalIgnoreCase))
                    shifts.Add(_levelCalOffsetDbLeft);
                if (string.Equals(modeForUnit, "Right", StringComparison.OrdinalIgnoreCase) || string.Equals(modeForUnit, "L+R", StringComparison.OrdinalIgnoreCase))
                    shifts.Add(_levelCalOffsetDbRight);

                if (shifts.Count > 0)
                {
                    min += shifts.Min();
                    max += shifts.Max();
                }
            }

            // Clamp final (on autorise jusqu'à +100 dB pour permettre l'affichage en dBV)
            const double YMIN_FLOOR = -300.0;
            const double YMAX_CAP = 100.0;

            min = Math.Max(YMIN_FLOOR, min);
            max = Math.Min(YMAX_CAP, max);

            return (min, max);
        }

        //

        public void UpdateSampleRate(int sampleRate)
        {
            if (sampleRate > 0)
                _sampleRate = sampleRate;

            UpdateWindowInfo();
        }

        // Appelé par Form1 quand le paramètre Overlap change.
        // hopSamples = nombre d'échantillons entre deux calculs (0 => pas de hop, on suit N).
        public void UpdateHopSamples(int hopSamples)
        {
            if (hopSamples < 0) hopSamples = 0;
            _hopSamples = hopSamples;

            UpdateOverlayInfo();
        }

        // Appelé par Form1 : force la taille FFT pour que Histogram + Spectrum utilisent la même fenêtre.
        public void SetGlobalSampleSize(int samples)
        {
            if (samples <= 0) return;
            _forcedFftSize = samples;
            lastFftSize = samples;

            // IMPORTANT (anti-artifacts)
            // Changer N change la resolution (deltaF) et la correspondance des bins (frequence <-> index).
            // On repart donc d'une moyenne vide pour eviter des artefacts visuels lors du prochain affichage.
            ResetAveragingState();
            UpdateWindowInfo();
            UpdateOverlayInfo();
        }



        //
        //private void UpdateAcDcButtonText()
        //{
        //    if (chkRemoveDC == null) return;
        //    // Convention : Checked = AC (remove DC activé), Unchecked = DC
        //    chkRemoveDC.Text = chkRemoveDC.Checked ? "AC" : "DC";
        //}
        //

        //
        // Reset level calibration (FFT) for both channel
        public void ResetLevelCalibration()
        {
            // Left
            _levelCalValidLeft = false;
            _levelCalOffsetDbLeft = 0.0;
            _levelGainVrmsPerFsLeft = 0.0;

            // Right
            _levelCalValidRight = false;
            _levelCalOffsetDbRight = 0.0;
            _levelGainVrmsPerFsRight = 0.0;

            // Remettre l’UI cohérente : dBV peut devenir indisponible
            UpdateLevelUnitUi();

            // Si tu es en mode "once" / non-live, forcer un refresh de l’affichage
            if (!isLive)
                UpdateSpectrumChartAndStats();
        }


        // ============================================================
        //  Level calibration (FFT) : dBFS <-> dBV
        // ============================================================
        private bool IsDbvAvailableForMode(string mode)
        {
            // Mode = "Left" / "Right" / "L+R"
            if (string.Equals(mode, "Left", StringComparison.OrdinalIgnoreCase))
                return _levelCalValidLeft;

            if (string.Equals(mode, "Right", StringComparison.OrdinalIgnoreCase))
                return _levelCalValidRight;

            // L+R => on affiche les 2 courbes, donc il faut les 2 calibrations
            return _levelCalValidLeft && _levelCalValidRight;
        }

        private bool IsDbvAvailableForCurrentMode()
        {
            string mode = (comboChannel?.SelectedItem as string) ?? "L+R";
            return IsDbvAvailableForMode(mode);
        }

        private bool UseDbvForMode(string mode)
        {
            return string.Equals(_levelUnit, "dBV", StringComparison.OrdinalIgnoreCase) && IsDbvAvailableForMode(mode);
        }

        private bool UseDbvForCurrentMode()
        {
            string mode = (comboChannel?.SelectedItem as string) ?? "L+R";
            return UseDbvForMode(mode);
        }

        private string CurrentLevelUnitLabel()
        {
            return UseDbvForCurrentMode() ? "dBV" : "dBFS";
        }

        private double DbfsToDisplay(double dbfs, bool isRightChannel, string mode)
        {
            if (!UseDbvForMode(mode))
                return dbfs;

            return dbfs + (isRightChannel ? _levelCalOffsetDbRight : _levelCalOffsetDbLeft);
        }
        //
        //
        // Grise/force certains contrôles quand le mode Bode Plot (FRF) est actif.

        //
        // Met à jour l'état de la ComboBox d'unité de niveau (dBFS/dBV)
        private void UpdateLevelUnitUi()
        {
            if (comboLevelUnit == null)
                return;

            // En mode Bode Plot (FRF), l'unité Y de la combo ne doit pas intervenir (pas de calibration ici).
            // On force donc dBFS et on grise la combo.
            if (IsFrfMode())
            {
                comboLevelUnit.Enabled = false;
                _levelUnit = "dBFS";
                if (comboLevelUnit.SelectedIndex != 0)
                    comboLevelUnit.SelectedIndex = 0;
                return;
            }

            bool canDbv = IsDbvAvailableForCurrentMode();

            // On grise complètement la ComboBox tant que dBV n'est pas dispo,
            // conformément au cahier des charges.
            comboLevelUnit.Enabled = canDbv;

            if (!canDbv)
            {
                _levelUnit = "dBFS";
                if (comboLevelUnit.SelectedIndex != 0)
                    comboLevelUnit.SelectedIndex = 0;
                return;
            }

            // Si dBV est dispo, on restaure la sélection cohérente
            if (string.Equals(_levelUnit, "dBV", StringComparison.OrdinalIgnoreCase))
            {
                if (comboLevelUnit.SelectedIndex != 1)
                    comboLevelUnit.SelectedIndex = 1;
            }
            else
            {
                if (comboLevelUnit.SelectedIndex != 0)
                    comboLevelUnit.SelectedIndex = 0;
            }
        }

        // Calibrage d'une voie (L ou R) en Vrms connu.




        //
        // ---------------- CURSORS ----------------
        //
        //
        public AudioHexViewer.SpectrumSettings GetSpectrumSettingsSnapshot()
        {
            var s = new AudioHexViewer.SpectrumSettings();

            // Channel
            if (comboChannel != null && comboChannel.SelectedItem is string ch)
                s.Channel = ch;
            else
                s.Channel = "L+R";

            //// DC
            //if (chkRemoveDC != null)
            //{
            //    s.RemoveDC = chkRemoveDC.Checked;
            //}

            // Window
            if (comboWindow != null && comboWindow.SelectedItem is string w)
                s.Window = w;
            else
                s.Window = "Rectangular";

            // Log freq
            if (chkLogX != null)
                s.LogFreq = chkLogX.Checked;

            // Averaging
            // comboAveraging contient "None" puis des nombres en string ("2","4","8"...)
            if (comboAveraging != null && comboAveraging.SelectedItem is string avgText)
            {
                if (string.Equals(avgText, "None", StringComparison.OrdinalIgnoreCase))
                    s.Averaging = 0;
                else if (int.TryParse(avgText, out int n))
                    s.Averaging = n;
                else
                    s.Averaging = 0;
            }
            else
            {
                s.Averaging = 0;
            }

            // Couleurs
            s.Colors = GetSpectrumColorSettingsSnapshot();

            // Niveau (FFT) : unité et calibration
            s.LevelUnit = _levelUnit;
            s.LevelCalValidLeft = _levelCalValidLeft;
            s.LevelCalOffsetDbLeft = _levelCalOffsetDbLeft;
            s.LevelCalValidRight = _levelCalValidRight;
            s.LevelCalOffsetDbRight = _levelCalOffsetDbRight;
            s.NoiseToPeakThresholdDb = _noiseToPeakThresholdDb;

            return s;
        }

        public void ApplySpectrumSettings(AudioHexViewer.SpectrumSettings s)
        {
            if (s == null)
                return;

            _isRestoringSettings = true;

            try
            {
                // Channel
                if (comboChannel != null)
                {
                    int idx = comboChannel.Items.IndexOf(s.Channel);
                    if (idx >= 0)
                        comboChannel.SelectedIndex = idx;
                }

                //// DC
                //if (chkRemoveDC != null)
                //{
                //    chkRemoveDC.Checked = s.RemoveDC;
                //}

                // Window
                if (comboWindow != null)
                {
                    int wIdx = comboWindow.Items.IndexOf(s.Window);
                    if (wIdx >= 0)
                        comboWindow.SelectedIndex = wIdx;
                    // IMPORTANT : ApplySpectrumSettings() inhibe les events UI (SelectedIndexChanged)
                    // via _isRestoringSettings. Donc on doit aussi synchroniser l'état interne
                    // utilisé par le worker externe (SelectedWindowType).
                    if (comboWindow.SelectedIndex >= 0)
                        lastWindow = (WindowType)comboWindow.SelectedIndex;

                }

                // Log freq
                if (chkLogX != null)
                {
                    chkLogX.Checked = s.LogFreq;
                }

                // Averaging
                if (comboAveraging != null)
                {
                    if (s.Averaging <= 0)
                    {
                        int noneIdx = comboAveraging.Items.IndexOf("None");
                        comboAveraging.SelectedIndex = noneIdx >= 0 ? noneIdx : 0;
                    }
                    else
                    {
                        string target = s.Averaging.ToString();
                        int aIdx = comboAveraging.Items.IndexOf(target);
                        if (aIdx >= 0)
                            comboAveraging.SelectedIndex = aIdx;
                    }
                    // IMPORTANT : idem que pour la fenêtre.
                    // Le handler SelectedIndexChanged n'est pas appelé pendant la restauration.
                    // On met donc à jour l'état d'averaging réellement utilisé par FeedExternalSpectrum().
                    _avgIsExponential = false;
                    _avgExpAlpha = 0.0;
                    avgTargetCount = (s.Averaging >= 2) ? s.Averaging : 1;
                    ResetAveragingState();
                    // --- Peak navigation threshold (Previous/Next Peak) ---
                    _noiseToPeakThresholdDb = Clamp(s.NoiseToPeakThresholdDb, 4.0, 100.0);


                }

                // Niveau (FFT) : unité + calibration (dBFS -> dBV)
                _levelUnit = string.IsNullOrWhiteSpace(s.LevelUnit) ? "dBFS" : s.LevelUnit;
                _levelCalValidLeft = s.LevelCalValidLeft;
                _levelCalOffsetDbLeft = s.LevelCalOffsetDbLeft;
                _levelCalValidRight = s.LevelCalValidRight;
                _levelCalOffsetDbRight = s.LevelCalOffsetDbRight;

                // Reconstituer le gain linéaire (Vrms/FS_rms) depuis l'offset dB persisté
                _levelGainVrmsPerFsLeft = _levelCalValidLeft ? GainFromOffsetDb(_levelCalOffsetDbLeft) : 0.0;
                _levelGainVrmsPerFsRight = _levelCalValidRight ? GainFromOffsetDb(_levelCalOffsetDbRight) : 0.0;

                UpdateLevelUnitUi();
                UpdateFrfUiLocks();   // <-- AJOUT (important si startup direct en FRF)

            }
            finally
            {
                _isRestoringSettings = false;
            }

            // Couleurs
            ApplySpectrumColorSettings(s.Colors);
            // IMPORTANT (bugfix UI)
            // Au démarrage, la ligne d'info (SR/N/ΔF/Overlap/Win/Avg/ACDC) a déjà pu être
            // initialisée par InitializeEmptySpectrumDisplay() avec des valeurs par défaut.
            // Comme ApplySpectrumSettings() inhibe les events UI pendant la restauration,
            // il faut forcer ici la mise à jour du texte affiché pour refléter la sélection réelle.
            UpdateWindowInfo();
            UpdateOverlayInfo();
            // IMPORTANT (startup en FRF) : ApplySpectrumSettings() inhibe les events UI,
            // donc on force ici la mise en page du chart (marges / largeur) selon le mode courant.
            ApplyChartStatsMargin(chartSpectrum, invalidate: true, reservedScale: IsFrfMode() ? 0f : 1f);
            SyncCursorUiFromModel();
            ApplyFrfPhiButtonsUi();



        }



        // --- Recherche de l’indice de fréquence le plus proche ---

        // 
        private static int FindNearestFreqIndex(double[] axis, double f, int minIndex)
        {
            if (axis == null || axis.Length == 0)
                return -1;

            minIndex = Math.Max(0, Math.Min(minIndex, axis.Length - 1));

            int lo = minIndex;
            int hi = axis.Length - 1;

            if (f <= axis[lo]) return lo;
            if (f >= axis[hi]) return hi;

            while (hi - lo > 1)
            {
                int mid = (lo + hi) / 2;
                if (axis[mid] < f) lo = mid;
                else hi = mid;
            }

            return (Math.Abs(axis[lo] - f) <= Math.Abs(axis[hi] - f)) ? lo : hi;
        }


        //

        private void ApplyChartStatsMargin(Chart chart, bool invalidate)
        {
            var area = chart.ChartAreas.FirstOrDefault(a => a.Name == "SpectrumArea");
            if (area == null) return;

            int w = chart.ClientSize.Width;
            if (w <= 0) return;

            // Panneau Stats : on réserve toujours une zone à droite, y compris en mode FRF (Bode Plot).
            // Important : l’axe Y2 (phase) reste dans le ChartArea ; le panneau est dessiné dans la zone réservée (hors plot).
            int reserved = ChartStatsWidthPx;


            // Si la fenêtre devient petite : on réduit la marge stats pour garder un plot utilisable
            int maxReserved = Math.Max(120, w - ChartStatsMinPlotWidthPx);
            reserved = Math.Min(reserved, maxReserved);

            float reservedPct = 100f * reserved / w;
            reservedPct = Math.Max(0f, Math.Min(45f, reservedPct)); // sécurité

            // On force une position manuelle pour libérer une zone vide à droite du chartArea
            area.Position.Auto = false;

            // Marges "safe" (en %) pour éviter de rogner titres/axes
            const float left = 3f;
            const float top = 3f;
            const float height = 94f;

            float width = 97f - reservedPct; // 97% - marge stats
            if (width < 10f) width = 10f;

            area.Position = new ElementPosition(left, top, width, height);

            if (invalidate)
                chart.Invalidate();
        }



        private Color _cursorMarkLColor = Color.DodgerBlue; // C1 (sera mis à jour par ApplyCursorTheme)
        private Color _cursorMarkRColor = Color.OrangeRed;  // C2 (sera mis à jour par ApplyCursorTheme)

        private enum ResolvedCurve
        {
            None,

            // Mode FFT normal
            Left,
            Right,

            // Mode FRF / Bode Plot
            Gain,   // courbe de gain (dB)
            Phase   // courbe de phase (deg) sur AxisY2
        }




        private Color CurveColor(ResolvedCurve c)
        {
            return c switch
            {
                ResolvedCurve.Left => FftLeftColor,
                ResolvedCurve.Right => FftRightColor,

                // FRF : on réutilise les mêmes couleurs (Gain=LeftColor, Phase=RightColor)
                ResolvedCurve.Gain => FftLeftColor,
                ResolvedCurve.Phase => FftRightColor,

                _ => Color.Gray
            };
        }


        //
        // Libellé affiché dans l'UI pour indiquer sur quelle "courbe" est le curseur.
        // - Mode normal : L / R
        // - Mode Bode Plot : Gain / Phase








        // =========================================================================================
        // PEAK SEARCH (Cursor "Peak", "Prev Peak", "Next Peak")
        //
        // Objectif : ne sélectionner que des "pics visibles" (tonalité, harmoniques, raies),
        // et ignorer les petites irrégularités dues au bruit.
        //
        // Définition pratique d’un "peak" (version simple, robuste, et testable) :
        //  1) Local maximum : le bin i doit être un maximum local (vs voisins immédiats).
        //  2) Prominence : le bin i doit dépasser ses voisins (±N bins) d’au moins X dB.
        //  3) Au-dessus du bruit : le bin i doit être au moins Y dB au-dessus d’un "noise floor"
        //     local (estimé par médiane sur une fenêtre autour du bin).
        //
        // Les 3 critères ensemble évitent de sauter sur du bruit.
        // =========================================================================================

        // ================= Peak search (cursor Next/Prev Peak) =================
        //
        // IMPORTANT : on travaille en dB (spectre affiché), sur la plage visible.
        // Un "peak" doit :
        //  1) être un max local (>= voisins)
        //  2) être "prominent" : dépasser les "épaules" (pas les bins collés au sommet)
        //  3) dépasser le bruit local (médiane) d'au moins X dB
        //
        // Tous les seuils sont regroupés ici pour pouvoir les ajuster facilement.
        //
        // (3) Bruit local : médiane d'une fenêtre glissante centrée autour du bin testé.
        private const int NOISE_FLOOR_HALF_WINDOW_BINS = 256;
        private const double PEAK_MIN_ABOVE_NOISE_DB = 6.0;

        // (2) Prominence : on prend une fenêtre ±W bins MAIS on ignore les bins trop proches
        // du sommet (lobe principal). Sinon, avec Hann/Blackman-Harris, les bins voisins
        // restent proches du max et la "prominence" devient artificiellement faible.
        private const int PEAK_PROMINENCE_HALF_WINDOW_BINS = 4;

        // Nombre de bins exclus autour du bin central (sommet) pour mesurer la "prominence"
        // sur les épaules du pic plutôt que sur le lobe principal.
        private const int PEAK_PROMINENCE_EXCLUDE_BINS = 2;


        private const double PEAK_MIN_PROMINENCE_DB = 3.0;


        // Petit epsilon pour éviter les égalités / instabilités.
        private const double PEAK_EPS = 1e-12;

        // ------------------------------------------------------------------
        // Simple peak detection based on visible noise floor (median)
        // Used ONLY for Next/Prev peak navigation (< > buttons)
        // ------------------------------------------------------------------





















        //
        // ---------------- PEAK HOLD ----------------

        //
        // --------------------------------------------------------------------
        // External Spectrum Feed (FFT calculée hors UI)
        // --------------------------------------------------------------------
        private bool _useExternalSpectrum = false;



        //
        /// <summary>
        /// Convertit un ratio de puissance (Left/Right) en dB : 10*log10(P_L / P_R).
        /// Utilisé par le mode "Bode Plot" (pré-Bode).
        /// </summary>

        //
        //
        // Alimente une frame de spectre externe (calculée hors UI)
        //
        // Construit les tableaux décimés uniquement pour le tracé (chart).
        // IMPORTANT : les métriques restent calculées sur currentLeftSpectrum/currentRightSpectrum + freqAxis (FULL).
        //
        /// <summary>
        /// Unwrap une phase en degrés supposée "wrapée" (typiquement [-180..180]) en phase continue.
        /// </summary>

        //
        // Décimation "tracé only" : conserve le max dans chaque bucket.
        // Entrées : freqFull (monotone), yFull (dBFS), plage visible, budget points, mode log/lin.
        // Sorties : freqOut/yOut de taille <= budget.

        // Variante : on a déjà un freqAxis plot (fréquences choisies), on interpole par "nearest bin" dans yFull.

        //
        /// Alimente SpectrumControl avec des stats temporelles (issues du core acquisition/analysis),
        /// pour calibration et affichage. Format attendu : int 24b signé (même que tes buffers d'analyse).
        /// Cette méthode est UI-only et doit être appelée sur le thread UI.


        //
        //
        /// <summary>
        /// Adds a small overlay button "Axis" in the bottom-left corner of the chart.
        /// This is a UI helper to open the Axis Parameters dialog without using right-click.
        /// The right-click behavior remains unchanged.
        /// </summary>

        /// <summary>
        /// Computes and applies the overlay button position inside the chart.
        /// Uses a fixed margin from the bottom-left corner.
        /// </summary>

        //
        // ------------------------------------------------------------------
        // UI: right-click on < or > buttons => edit NoiseToPeakThresholdDb
        // ------------------------------------------------------------------

        /// <summary>
        /// Small dialog for editing the Noise→Peak threshold (dB).
        /// - Editable ComboBox (direct input)
        /// - Range enforced: [minDb..maxDb]
        /// - OK/Cancel
        /// </summary>
        private sealed class NoiseToPeakThresholdDialog : Form
        {
            private readonly NumericUpDown _nud;
            private readonly Button _btnOk;
            private readonly Button _btnCancel;
            private readonly double _minDb;
            private readonly double _maxDb;

            public double ValueDb { get; private set; }

            public NoiseToPeakThresholdDialog(double currentValueDb, double minDb, double maxDb)
            {
                _minDb = minDb;
                _maxDb = maxDb;

                Text = "Noise→Peak Thres (dB)";
                FormBorderStyle = FormBorderStyle.FixedDialog;
                StartPosition = FormStartPosition.CenterParent;
                MinimizeBox = false;
                MaximizeBox = false;
                ShowInTaskbar = false;
                AutoSize = true;
                AutoSizeMode = AutoSizeMode.GrowAndShrink;
                Padding = new Padding(10);

                var lbl = new Label
                {
                    AutoSize = true,
                    Text = $"Threshold dB [{_minDb:0}..{_maxDb:0}] :",
                    Margin = new Padding(0, 0, 0, 6)
                };

                _nud = new NumericUpDown
                {
                    Width = 120,
                    Minimum = (decimal)minDb,
                    Maximum = (decimal)maxDb,

                    // Incréments de 2 dB demandés
                    Increment = 2m,

                    // Valeurs en dB : on reste en entier (si tu veux 0.5 dB un jour => DecimalPlaces=1 et Increment=0.5m)
                    DecimalPlaces = 0,

                    // Confort visuel
                    TextAlign = HorizontalAlignment.Right,
                };
                _nud.Value = (decimal)Math.Max(minDb, Math.Min(maxDb, currentValueDb));

                _btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
                _btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };

                AcceptButton = _btnOk;
                CancelButton = _btnCancel;

                var buttons = new FlowLayoutPanel
                {
                    FlowDirection = FlowDirection.RightToLeft,
                    AutoSize = true,
                    Dock = DockStyle.Bottom,
                    Margin = new Padding(0, 10, 0, 0)
                };
                buttons.Controls.Add(_btnCancel);
                buttons.Controls.Add(_btnOk);

                var panel = new FlowLayoutPanel
                {
                    FlowDirection = FlowDirection.TopDown,
                    AutoSize = true,
                    Dock = DockStyle.Fill
                };
                panel.Controls.Add(lbl); panel.Controls.Add(_nud);

                panel.Controls.Add(buttons);

                Controls.Add(panel);

                // Validation au OK
                _btnOk.Click += (_, __) =>
                {
                    double v = (double)_nud.Value;
                    ValueDb = v;
                    DialogResult = DialogResult.OK;
                    Close();

                    if (v < _minDb || v > _maxDb)
                    {
                        MessageBox.Show(this, $"Value must be between {_minDb:0} and {_maxDb:0} dB.",
                            "Out of range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        DialogResult = DialogResult.None;
                        return;
                    }

                    ValueDb = v;
                };
            }
        }

        //
        /// <summary>
        /// Applique le mode X (LIN/LOG) de manière atomique et sûre.
        /// Le Chart WinForms peut lever "logarithmic scale cannot be used" si min <= 0 en LOG.
        /// </summary>


        // Surcharge pratique : réutilise les derniers buffers temps connus
        private void UpdateSpectrumChartAndStats()
        {
            UpdateSpectrumChartAndStats(lastTimeLeft, lastTimeRight);

        }
        //
        // ════════════════════════════════════════════════════════════════════
        // Phase 5 — Helpers rendu haute performance
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Initialise les 5 séries fixes une seule fois (lazy).
        /// Ordre d'ajout = ordre de rendu (back → front).
        /// Les overlays (OV*) seront insérés en position 0 = sous tout le reste.
        /// </summary>
        private void EnsureSeriesInited()
        {
            if (_seriesInited) return;

            Series Mk(string name, Color color, int width,
                      ChartDashStyle dash = ChartDashStyle.Solid) => new Series(name)
            {
                ChartType       = SeriesChartType.FastLine,
                ChartArea       = "SpectrumArea",
                Color           = color,
                BorderWidth     = width,
                BorderDashStyle = dash,
            };

            _serPeakLeft  = Mk("LeftPeakHold",  PeakHoldLeftColor,  _colors.PeakHoldWidth, ChartDashStyle.Dash);
            _serPeakRight = Mk("RightPeakHold", PeakHoldRightColor, _colors.PeakHoldWidth, ChartDashStyle.Dash);
            _serPhase     = Mk("Phase",          FftRightColor,      _colors.FftWidth);
            _serPhase.YAxisType = AxisType.Secondary;
            _serLeft  = Mk("Left",  FftLeftColor,  _colors.FftWidth);
            _serRight = Mk("Right", FftRightColor, _colors.FftWidth);

            chartSpectrum.Series.Add(_serPeakLeft);
            chartSpectrum.Series.Add(_serPeakRight);
            chartSpectrum.Series.Add(_serPhase);
            chartSpectrum.Series.Add(_serLeft);
            chartSpectrum.Series.Add(_serRight);

            _seriesInited = true;
        }

        /// <summary>Supprime uniquement les séries overlay (OV*). Les séries fixes restent.</summary>
        private void RemoveOverlaySeries()
        {
            for (int i = chartSpectrum.Series.Count - 1; i >= 0; i--)
                if (chartSpectrum.Series[i].Name.StartsWith("OV", StringComparison.Ordinal))
                    chartSpectrum.Series.RemoveAt(i);
        }

        /// <summary>
        /// Remplit une série pré-allouée avec SuspendUpdates.
        /// Zéro allocation, zéro événement intermédiaire entre Clear et ResumeUpdates.
        /// </summary>
        private static void FillSeries(Series ser, double[]? fx, double[]? y,
                                        int startIdx, bool show, double offset)
        {
            ser.Points.SuspendUpdates();
            ser.Points.Clear();
            if (show && fx != null && y != null)
            {
                int n = Math.Min(fx.Length, y.Length);
                for (int i = startIdx; i < n; i++)
                    ser.Points.AddXY(fx[i], y[i] + offset);
            }
            ser.Points.ResumeUpdates();
        }

        /// <summary>
        /// Met à jour lblXxxRms/Peak depuis les champs _last* pré-calculés par
        /// FeedExternalTimeStats24. Corrige le bug latent : l'ancien bloc
        /// "if (timeL != null)" était du dead code (lastTimeLeft jamais assigné).
        /// </summary>
        private void UpdateStatsLabels(bool left, bool show, bool useDbv)
        {
            Label? lRms  = left ? lblLeftRms   : lblRightRms;
            Label? lPeak = left ? lblLeftPeak  : lblRightPeak;
            Label? lRmsV = left ? lblLeftRmsV  : lblRightRmsV;
            Label? lPkV  = left ? lblLeftPeakV : lblRightPeakV;

            if (!show)
            {
                if (lRms  != null) lRms.Text  = "RMS:";
                if (lPeak != null) lPeak.Text = "Peak:";
                if (lRmsV != null) lRmsV.Text = "";
                if (lPkV  != null) lPkV.Text  = "";
                return;
            }

            double? rmsDb  = left ? _lastRmsDbfsLeft  : _lastRmsDbfsRight;
            double? peakFs = left ? _lastPeakFsLeft   : _lastPeakFsRight;
            double  offset = left ? _levelCalOffsetDbLeft : _levelCalOffsetDbRight;
            string  unit   = useDbv ? "dBV" : "dBFS";

            if (lRms != null)
                lRms.Text = rmsDb.HasValue
                    ? $"RMS:  {(useDbv ? rmsDb.Value + offset : rmsDb.Value):F2} {unit}"
                    : "RMS:";

            if (lPeak != null && peakFs.HasValue && peakFs.Value > 0.0)
            {
                double pkDbfs = Math.Max(-200.0, 20.0 * Math.Log10(peakFs.Value));
                lPeak.Text = $"Peak: {(useDbv ? pkDbfs + offset : pkDbfs):F2} {unit}";
            }
            else if (lPeak != null) lPeak.Text = "Peak:";

            if (useDbv)
            {
                double? vrms = left ? _lastVrmsVoltsLeft  : _lastVrmsVoltsRight;
                double? vpk  = left ? _lastVpkVoltsLeft   : _lastVpkVoltsRight;
                if (lRmsV != null) lRmsV.Text = vrms.HasValue ? $"Vrms:  {FormatVoltage(vrms.Value)}" : "";
                if (lPkV  != null) lPkV.Text  = vpk.HasValue  ? $"Vpeak: {FormatVoltage(vpk.Value)}"  : "";
            }
            else
            {
                if (lRmsV != null) lRmsV.Text = "";
                if (lPkV  != null) lPkV.Text  = "";
            }
        }

        private void UpdateSpectrumChartAndStats(double[]? timeL, double[]? timeR)
        {
            if (chartSpectrum.ClientSize.Width <= 0 || chartSpectrum.ClientSize.Height <= 0)
                return;
            if (freqAxis == null || (currentLeftSpectrum == null && currentRightSpectrum == null))
                return;

            // Helper local : estimation rapide de F0 (pas de THD => pas de ComputeChannel)
            static double? EstimateFundamentalHz(double[] spectrum, double[] fx, double fMinHz, double fMaxHz, bool useLog)
            {
                if (spectrum == null || fx == null) return null;
                if (spectrum.Length != fx.Length || spectrum.Length < 3) return null;

                // bornes utiles
                double minPhys = useLog ? LogXAbsMinHz : 0.0;
                if (fMinHz < minPhys) fMinHz = minPhys;
                if (fMaxHz <= fMinHz) return null;

                // trouver l’index de départ/fin dans fx[]
                int i0 = 0;
                while (i0 < fx.Length && fx[i0] < fMinHz) i0++;

                int i1 = fx.Length - 1;
                while (i1 > i0 && fx[i1] > fMaxHz) i1--;

                // éviter DC/0Hz si besoin
                if (useLog && i0 < 1) i0 = 1;
                if (i1 <= i0) return null;

                int maxIdx = i0;
                double maxVal = spectrum[i0];

                for (int i = i0 + 1; i <= i1; i++)
                {
                    double v = spectrum[i];
                    if (v > maxVal)
                    {
                        maxVal = v;
                        maxIdx = i;
                    }
                }

                // La fréquence correcte du point sélectionné, même après décimation :
                return fx[maxIdx];
            }

            var area = chartSpectrum.ChartAreas["SpectrumArea"];

            // --- Axe X lin / log ---
            bool useLog = (chkLogX != null && chkLogX.Checked);
            // Garder l'info pour les autres fonctions
            _xAxisIsLog = useLog;

            // En log, on évite le bin 0
            int startIndex = useLog ? 1 : 0;

            // Valeurs AUTO (sans contrainte utilisateur)
            double fMaxAuto = _sampleRate > 0 ? _sampleRate / 2.0 : 1.0;
            double fMinAuto;

            if (useLog)
            {
                if (freqAxis != null && freqAxis.Length > startIndex)
                    fMinAuto = Math.Max(freqAxis[startIndex], LogXAbsMinHz);
                else
                    fMinAuto = LogXAbsMinHz;

                area.AxisX.IsMarginVisible = false;   // évite le “padding” à gauche
            }
            else
            {
                fMinAuto = 0.0;
                area.AxisX.IsMarginVisible = true;
            }



            // --- Application des limites X custom (si définies) ---
            double fMin = customFMin ?? fMinAuto;   // Hz
            double fMax = customFMax ?? fMaxAuto;   // Hz

            ApplyAxisXModeSafe(area, useLog, fMin, fMax);

            // Sécurités de base
            double xMinPhys = useLog ? LogXAbsMinHz : 0.0;
            if (fMin < xMinPhys) fMin = xMinPhys;
            double nyquist = _sampleRate / 2.0;

            // Sécurité fMax
            double uiMax = SpectrumAxisDialog.GetAllowedFmax(_sampleRate);
            if (fMax > uiMax)
                fMax = uiMax;

            // Guard "log" : évite l'exception Chart
            // "A logarithmic scale cannot be used for this axis".
            if (useLog)
            {
                if (double.IsNaN(fMin) || fMin <= 0.0)
                    fMin = LogXAbsMinHz;
                if (double.IsNaN(fMax) || fMax <= fMin)
                    fMax = fMin * 10.0;
            }

            // --- Axe Y (dBFS / dBV) avec limites custom éventuelles ---
            string modeForUnit = (comboChannel?.SelectedItem as string) ?? "L+R";
            bool useDbvAxis = UseDbvForMode(modeForUnit);

            double yMinAuto = -200.0;
            double yMaxAuto = 0.0;

            // Mode "Bode Plot" : on affiche un gain relatif (dB), pas un niveau dBFS.
            // On choisit une échelle par défaut centrée pour permettre des gains +/-.
            if (string.Equals(modeForUnit, "Bode Plot", StringComparison.OrdinalIgnoreCase))
            {
                //yMinAuto = -160.0;
                //yMaxAuto = 40.0;
                useDbvAxis = false; // pas de dBV sur un ratio
                area.AxisY.Title = "Gain (dB)";
            }

            // --- Axe Y2 : Phase (uniquement en mode Bode Plot) ---
            bool frfMode = string.Equals(modeForUnit, "Bode Plot", StringComparison.OrdinalIgnoreCase);

            area.AxisY2.Enabled = frfMode ? AxisEnabled.True : AxisEnabled.False;

            if (frfMode)
            {
                area.AxisY2.Title = "Phase (deg)";
                double pmin = customPhaseMin ?? -180.0;
                double pmax = customPhaseMax ?? 180.0;

                // Sécurité : clamp + cohérence
                pmin = Math.Max(-360.0, Math.Min(360.0, pmin));
                pmax = Math.Max(-360.0, Math.Min(360.0, pmax));
                if (pmax <= pmin) pmax = Math.Min(360.0, pmin + 1.0);

                area.AxisY2.Minimum = pmin;
                area.AxisY2.Maximum = pmax;

                // 6 zones visuelles => 6 intervalles => 7 labels majeurs
                double span = pmax - pmin;
                area.AxisY2.Interval = Math.Max(1.0, span / 4.0);

                // Grilles : on reste sobre (tu peux ajuster si tu veux)
                area.AxisY2.MajorGrid.Enabled = false;
                area.AxisY2.MinorGrid.Enabled = false;
            }

            // Limite supérieure autorisée (utile en dBV après calibration).
            // On garde une échelle par défaut de 200 dB (span = 200).
            const double YMAX_CAP = 100.0;
            const double DEFAULT_SPAN_DB = 200.0;

            if (string.Equals(modeForUnit, "Bode Plot", StringComparison.OrdinalIgnoreCase))
            {
                // Title déjà fixé plus haut.
            }
            else if (useDbvAxis)
            {
                // En dBV, on applique un décalage (offset) : dBV = dBFS + offset
                // En mode L+R, les offsets peuvent être différents => on prend un range qui couvre les 2
                var shifts = new List<double>();
                if (string.Equals(modeForUnit, "Left", StringComparison.OrdinalIgnoreCase) || string.Equals(modeForUnit, "L+R", StringComparison.OrdinalIgnoreCase))
                    shifts.Add(_levelCalOffsetDbLeft);
                if (string.Equals(modeForUnit, "Right", StringComparison.OrdinalIgnoreCase) || string.Equals(modeForUnit, "L+R", StringComparison.OrdinalIgnoreCase))
                    shifts.Add(_levelCalOffsetDbRight);

                if (shifts.Count > 0)
                {
                    yMinAuto += shifts.Min();
                    yMaxAuto += shifts.Max();
                }

                area.AxisY.Title = "Level (dBV)";
            }
            else
            {
                area.AxisY.Title = "Level (dBFS)";
            }

            // Cap sur le range auto pour éviter un axe Y qui parte trop haut
            if (yMaxAuto > YMAX_CAP)
            {
                yMaxAuto = YMAX_CAP;
                yMinAuto = yMaxAuto - DEFAULT_SPAN_DB;
            }

            double yMin = customYMin ?? yMinAuto;
            double yMax = customYMax ?? yMaxAuto;

            // Cap sur le YMax choisi. Si seul YMax est auto, on conserve le span par défaut.
            if (yMax > YMAX_CAP)
            {
                yMax = YMAX_CAP;
                if (customYMin == null)
                    yMin = yMax - DEFAULT_SPAN_DB;
            }

            if (yMin >= yMax)
                yMin = yMax - 10.0; // au moins 10 dB de span

            // ---------------- Grilles & divisions ----------------
            const int majorDivCount = 4;
            const int minorPerMajor = 5;

            double ySpan = yMax - yMin;
            if (ySpan <= 0) ySpan = 1.0;

            double rawMinor = ySpan / (majorDivCount * minorPerMajor);
            if (rawMinor <= 0) rawMinor = 1.0;

            double yMinor = rawMinor;

            double scaled = yMinor * 100.0;
            double scaledRound = Math.Round(scaled);
            bool hasMoreThan2Decimals = Math.Abs(scaled - scaledRound) > 1e-9;

            if (hasMoreThan2Decimals)
                yMinor = Math.Round(yMinor, 2, MidpointRounding.AwayFromZero);

            if (yMinor <= 0)
                yMinor = 0.01;

            double yMajor = minorPerMajor * yMinor;


            // ── Cache axe : skip les 120+ assignments si les paramètres n'ont pas changé ──
            bool _axChanged =
                double.IsNaN(_axCachedFmin)  ||
                double.IsNaN(_axCachedYmin)  ||
                Math.Abs(fMin    - _axCachedFmin)   > 0.01 ||
                Math.Abs(fMax    - _axCachedFmax)   > 0.01 ||
                Math.Abs(yMin    - _axCachedYmin)   > 0.01 ||
                Math.Abs(yMax    - _axCachedYmax)   > 0.01 ||
                Math.Abs(yMinor  - _axCachedYminor) > 1e-6 ||
                useLog != _axCachedUseLog            ||
                chartSpectrum.ClientSize != _axCachedChartSize;

            if (_axChanged)
            {
                area.AxisY.Minimum = yMin;
                area.AxisY.Maximum = yMax;

                _currentYMinor = yMinor;
                _currentYMajor = yMajor;

                area.AxisY.Interval = yMajor;
                area.AxisY.IntervalOffset = 0.0;

                area.AxisY.LabelStyle.Interval = yMajor;
                area.AxisY.LabelStyle.IntervalOffset = 0.0;

                area.AxisY.MajorTickMark.Enabled = true;
                area.AxisY.MajorTickMark.Interval = yMajor;
                area.AxisY.MajorTickMark.IntervalOffset = 0.0;

                area.AxisY.MajorGrid.Enabled = true;
                area.AxisY.MajorGrid.Interval = yMajor;
                area.AxisY.MajorGrid.IntervalOffset = 0.0;
                area.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Solid;

                area.AxisY.MinorGrid.Enabled = true;
                area.AxisY.MinorGrid.Interval = yMinor;
                area.AxisY.MinorGrid.IntervalOffset = 0.0;
                area.AxisY.MinorGrid.LineDashStyle = ChartDashStyle.Dot;
                area.AxisY.MinorGrid.LineColor = GridMinorColor;

                // Activer les ticks mineurs pour que LabelMinor soit visible.
                // Désactivé : on dessine nous-mêmes les ticks/labels mineurs.
                area.AxisY.MinorTickMark.Enabled = false;
                area.AxisY.MinorTickMark.Size = 1f;
                int yDecimals = 0;
                if (yMinor < 1.0)
                {
                    yDecimals = (int)Math.Ceiling(-Math.Log10(yMinor));
                    yDecimals = Math.Min(yDecimals, 2);
                }
                area.AxisY.LabelStyle.Format = "F" + yDecimals;

                // On remplace les labels majeurs du Chart par notre rendu PostPaint (deterministe)
                // => on masque ceux du Chart sans impacter le layout (ForeColor transparent).
                area.AxisY.LabelStyle.ForeColor = Color.Transparent;

                // --- Axe X : on fixe d'abord les bornes, puis on active le log si besoin ---
                if (useLog)
                {
                    if (area.AxisX.Minimum <= 0)
                        area.AxisX.Minimum = LogXAbsMinHz;
                    if (area.AxisX.Maximum <= area.AxisX.Minimum)
                        area.AxisX.Maximum = area.AxisX.Minimum * 10.0;

                    double minDecade = Math.Pow(10.0, Math.Floor(Math.Log10(area.AxisX.Minimum)));
                    area.AxisX.Minimum = Math.Max(LogXAbsMinHz, minDecade);

                    if (area.AxisX.Maximum <= area.AxisX.Minimum)
                        area.AxisX.Maximum = area.AxisX.Minimum * 10.0;

                    area.AxisX.IsLogarithmic = true;
                    area.AxisX.LogarithmBase = 10.0;

                    area.AxisX.Interval = 1.0;
                    area.AxisX.LabelStyle.Interval = 1.0;
                    area.AxisX.MajorTickMark.Interval = 1.0;

                    area.AxisX.MajorGrid.Enabled = true;
                    area.AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Solid;
                    area.AxisX.MajorGrid.Interval = 1.0;

                    area.AxisX.MinorGrid.Enabled = false;

                    ApplyLogXMinorGridStripLines(area.AxisX);

                    area.AxisX.MajorGrid.IntervalOffset = 0.0;
                    area.AxisX.MinorGrid.IntervalOffset = 0.0;
                    area.AxisX.MinorTickMark.IntervalOffset = 0.0;
                }
                else
                {
                    area.AxisX.IsLogarithmic = false;

                    area.AxisX.StripLines.Clear();

                    double xSpan = fMax - fMin;
                    if (xSpan <= 0) xSpan = 1.0;

                    int spanHz = (int)Math.Round(xSpan);
                    if (spanHz < 1) spanHz = 1;

                    var grid = ChooseBestStepWithSpanAdjustment(spanHz);
                    double xMajor = grid.StepHz;

                    double fMaxAdjusted = fMin + grid.SpanAdjustedHz;

                    area.AxisX.Minimum = fMin;
                    area.AxisX.Maximum = fMaxAdjusted;

                    area.AxisX.Interval = xMajor;
                    area.AxisX.IntervalOffset = 0.0;

                    area.AxisX.LabelStyle.Interval = xMajor;
                    area.AxisX.LabelStyle.IntervalOffset = 0.0;

                    area.AxisX.MajorTickMark.Enabled = true;
                    area.AxisX.MajorTickMark.Interval = xMajor;
                    area.AxisX.MajorTickMark.IntervalOffset = 0.0;

                    area.AxisX.MajorGrid.Enabled = true;
                    area.AxisX.MajorGrid.Interval = xMajor;
                    area.AxisX.MajorGrid.IntervalOffset = 0.0;

                    area.AxisX.MinorGrid.Enabled = true;
                    area.AxisX.MinorGrid.Interval = xMajor / 5.0;
                    area.AxisX.MinorGrid.IntervalOffset = 0.0;

                    area.AxisX.MajorTickMark.Size = 2f;

                    area.AxisX.MinorGrid.LineDashStyle = ChartDashStyle.Dot;
                    area.AxisX.MinorGrid.LineColor = GridMinorColor;
                }


                // Mettre en cache les paramètres utilisés
                _axCachedFmin      = fMin;
                _axCachedFmax      = fMax;
                _axCachedYmin      = yMin;
                _axCachedYmax      = yMax;
                _axCachedYminor    = yMinor;
                _axCachedUseLog    = useLog;
                _axCachedChartSize = chartSpectrum.ClientSize;
            } // end if (_axChanged)

            // --- Ce qu'on affiche en fonction du combo Channel ---
            string mode = comboChannel.SelectedItem as string ?? "L+R";

            // "Bode Plot" est une seule courbe portée par "Left" (currentLeftSpectrum = ratio en dB)
            bool showLeft = (mode == "Left" || mode == "L+R" || mode == "Bode Plot") && (currentLeftSpectrum != null);
            bool showRight = (mode == "Right" || mode == "L+R") && (currentRightSpectrum != null);


            bool peakHoldOn = (chkPeakHold != null && chkPeakHold.Checked);
            bool useDbvPlot = UseDbvForMode(mode);

            _statsShowLeft = showLeft;
            _statsShowRight = showRight;


            bool doThd = _thdMeasuresEnabled;

            // [Fix B] En LOG, si on ne pourra pas ajouter AU MOINS une série,
            // on ne touche pas au chart (on garde l'ancienne courbe) et on continue vers les mesures.
            bool useLog_now = (chkLogX != null && chkLogX.Checked);
            if (useLog_now)
            {
                // 1) Si aucune voie n'est affichable (Right sans spectre droit, etc.)
                if (!showLeft && !showRight)
                    goto MEASURES_AND_TEXT;

                // 2) Si les données PLOT ne sont pas prêtes (cas transitoire), on évite de vider le chart.
                //    Sinon on risque "LOG + 0 série" pendant un paint.
                if (_plotFreqAxis == null || _plotFreqAxis.Length < 2)
                    goto MEASURES_AND_TEXT;

                if (showLeft && (_plotLeftSpectrum == null || _plotLeftSpectrum.Length < 2))
                    goto MEASURES_AND_TEXT;

                if (showRight && (_plotRightSpectrum == null || _plotRightSpectrum.Length < 2))
                    goto MEASURES_AND_TEXT;
            }


            // >>> PLOT : peak-hold et spectres décimés (tracé uniquement)
            double[]? plotFx = _plotFreqAxis;
            double[]? plotL = _plotLeftSpectrum;
            double[]? plotR = _plotRightSpectrum;
            double[]? peakL = peakHoldOn ? _plotPeakLeft : null;
            double[]? peakR = peakHoldOn ? _plotPeakRight : null;

            // Si on n'a pas de données PLOT (cas transitoire), on ne trace rien (mais les métriques restent OK).
            if (plotFx == null || plotFx.Length == 0)
            {
                // On laisse la suite (stats, THD, etc.) travailler sur FULL, mais pas de courbe à afficher.
                // (Alternative possible : fallback sur FULL, mais ça peut saturer l'UI en points.)
            }

            // [Fix B] On ne clear le chart QUE si on va réellement reconstruire des séries.
            // Si on a sauté vers MEASURES_AND_TEXT (état transitoire invalide), cette ligne n'est jamais atteinte.
            EnsureSeriesInited();
            RemoveOverlaySeries();

            // --- Overlays (Library V3) ---
            OverlayKind activeKind = (mode == "Bode Plot") ? OverlayKind.Bode : OverlayKind.Spectrum;

            // Bornes visibles (axe actuel)
            double xMin = area.AxisX.Minimum;
            double xMax = area.AxisX.Maximum;

            // Troncature au Nyquist courant si SR connu
            double xMaxNyq = Math.Min(xMax, nyquist);

            // En LOG, on évite <= 0 Hz
            if (useLog) xMin = Math.Max(xMin, 1.0);

            // Helper local overlay : filtre xMin/xMaxNyq + SuspendUpdates + offset inline.
            // Supprime l'allocation .Select().ToArray() de l'ancienne version.
            void AddPoints(ChartSeries series, double[] fx, double[] yRaw, double yOffset = 0.0)
            {
                int n = Math.Min(fx.Length, yRaw.Length);
                series.Points.SuspendUpdates();
                series.Points.Clear();
                for (int i = 0; i < n; i++)
                {
                    double f = fx[i];
                    if (useLog && f <= 0) continue;
                    if (f < xMin) continue;
                    if (f > xMaxNyq) break;
                    series.Points.AddXY(f, yRaw[i] + yOffset);
                }
                series.Points.ResumeUpdates();
            }

            // Overlays insérés en position 0 = rendus sous le spectre live
            int ovInsert = 0;
            for (int ovIndex = 0; ovIndex < _overlayCache.Count; ovIndex++)
            {
                var ov = _overlayCache[ovIndex];
                if (!ov.Visible) continue;

                var data = ov.Data;
                if (data == null || data.Kind != activeKind) continue;

                int slotIndex = (data.SlotNumber >= 1 && data.SlotNumber <= OverlayManager.MaxOverlays)
                    ? (data.SlotNumber - 1) : (ovIndex % OverlayManager.MaxOverlays);
                var ovColor = GetOverlaySlotColor(slotIndex);

                if (activeKind == OverlayKind.Spectrum)
                {
                    if (showLeft && data.LeftDbFs != null && data.FreqHz != null)
                    {
                        var s = new ChartSeries($"OV{ovIndex + 1}_L")
                        {
                            ChartType = SeriesChartType.FastLine,
                            ChartArea = "SpectrumArea",
                            Color = ovColor,
                            BorderWidth = _colors.OverlayWidth,
                            BorderDashStyle = GetOverlayDashStyle(),
                        };
                        double ovLOff = useDbvPlot ? _levelCalOffsetDbLeft : 0.0;
                        AddPoints(s, data.FreqHz, data.LeftDbFs, ovLOff);
                        chartSpectrum.Series.Insert(ovInsert++, s);
                    }
                    if (showRight && data.RightDbFs != null && data.FreqHz != null)
                    {
                        var s = new ChartSeries($"OV{ovIndex + 1}_R")
                        {
                            ChartType = SeriesChartType.FastLine,
                            ChartArea = "SpectrumArea",
                            Color = ovColor,
                            BorderWidth = _colors.OverlayWidth,
                            BorderDashStyle = GetOverlayDashStyle(),
                        };
                        double ovROff = useDbvPlot ? _levelCalOffsetDbRight : 0.0;
                        AddPoints(s, data.FreqHz, data.RightDbFs, ovROff);
                        chartSpectrum.Series.Insert(ovInsert++, s);
                    }
                }
                else // Bode
                {
                    if (data.GainDb != null && data.FreqHz != null)
                    {
                        var sGain = new ChartSeries($"OV{ovIndex + 1}_Gain")
                        {
                            ChartType = SeriesChartType.FastLine,
                            ChartArea = "SpectrumArea",
                            Color = ovColor,
                            BorderDashStyle = GetOverlayDashStyle(),
                            BorderWidth = _colors.OverlayWidth,
                        };
                        AddPoints(sGain, data.FreqHz, data.GainDb);
                        chartSpectrum.Series.Insert(ovInsert++, sGain);
                    }
                    if (_frfPhaseEnabled && data.PhaseDeg != null && data.FreqHz != null)
                    {
                        var sPh = new ChartSeries($"OV{ovIndex + 1}_Phase")
                        {
                            ChartType = SeriesChartType.FastLine,
                            ChartArea = "SpectrumArea",
                            YAxisType = AxisType.Secondary,
                            Color = ovColor,
                            BorderDashStyle = GetOverlayDashStyle(),
                            BorderWidth = _colors.OverlayWidth,
                        };
                        AddPoints(sPh, data.FreqHz, data.PhaseDeg);
                        chartSpectrum.Series.Insert(ovInsert++, sPh);
                    }
                }
            }

            // ── Séries principales (pré-allouées, FastLine, SuspendUpdates) ───────────
            double leftOff  = useDbvPlot ? _levelCalOffsetDbLeft  : 0.0;
            double rightOff = useDbvPlot ? _levelCalOffsetDbRight : 0.0;

            FillSeries(_serLeft!,     plotFx, plotL,                      startIndex, showLeft,                  leftOff);
            FillSeries(_serRight!,    plotFx, plotR,                      startIndex, showRight,                 rightOff);
            FillSeries(_serPeakLeft!, plotFx, peakHoldOn ? peakL : null,  startIndex, showLeft  && peakHoldOn,  leftOff);
            FillSeries(_serPeakRight!,plotFx, peakHoldOn ? peakR : null,  startIndex, showRight && peakHoldOn,  rightOff);

            bool showPhase = mode == "Bode Plot" && _frfPhaseEnabled && _plotFrfPhaseDeg != null;
            FillSeries(_serPhase!, plotFx, showPhase ? _plotFrfPhaseDeg : null, startIndex, showPhase, 0.0);



            // ── Labels RMS / Peak ───────────────────────────────────────────────────────
            // Les champs _last* sont calculés par FeedExternalTimeStats24.
            // L'ancien bloc "if (timeL != null)" était du dead code :
            // lastTimeLeft n'est jamais assigné en dehors de null.
            bool showLeftVolts  = (_levelUnit == "dBV") && _levelCalValidLeft;
            bool showRightVolts = (_levelUnit == "dBV") && _levelCalValidRight;

            UpdateStatsLabels(left: true,  show: showLeft,  useDbv: showLeftVolts);
            UpdateStatsLabels(left: false, show: showRight, useDbv: showRightVolts);
            SetVoltLabelsVisible(showLeftVolts, showRightVolts);




        // =========================
        //   MESURES ANALYTIQUES
        // =========================
        MEASURES_AND_TEXT:
            ChannelMetrics? mL = null;
            ChannelMetrics? mR = null;

            bool singleChannel = (showLeft && !showRight) || (showRight && !showLeft);
            bool isMaximized = (this.FindForm()?.WindowState == FormWindowState.Maximized);

            // Les mesures THD (F0..SFDR + harmoniques) sont affichées seulement si
            // - une seule voie est active
            // - OU la fenêtre est maximisée
            bool allowThdMeasurementsUi = singleChannel || isMaximized;

            bool showHarmsInPanel = doThd && _thdUi.ShowHarmonicsValues && allowThdMeasurementsUi;

            // LEFT
            if (lblLeftMeas != null && currentLeftSpectrum != null && _sampleRate > 0 && lastFftSize > 0)
            {
                if (doThd && allowThdMeasurementsUi)
                {
                    // En mode cohérent : utilise le spectre moyenné complex (meilleur SNR).
                    // Guard sur la longueur : si une transition de fftSize vient de se produire,
                    // _cohAvgLeftDb peut avoir une taille différente → fallback sur currentLeftSpectrum.
                    int expectedBins = lastFftSize / 2 + 1;
                    double[]? specL = (_thdUi.UseCohAvg &&
                                       _cohAvgLeftDb != null &&
                                       _cohAvgLeftDb.Length == expectedBins)
                                      ? _cohAvgLeftDb
                                      : currentLeftSpectrum;

                    mL = SpectrumMetrics.ComputeChannel(specL, _sampleRate, lastFftSize, _metricsOpt);

                    if (mL.Valid)
                    {
                        string thdText;
                        string thdnText;

                        if (_thdUi.Unit == SpectrumSettings.ThdSettings.ThdUnit.Percent)
                        {
                            // THD ratio -> %
                            thdText = $"THD: {mL.Thd * 100.0:F5} %";

                            // THD+N : on convertit depuis le dB (ratio = 10^(dB/20))
                            double thdnPct = Math.Pow(10.0, mL.ThdNDb / 20.0) * 100.0;
                            thdnText = $"THD+N: {thdnPct:F5} %";
                        }
                        else
                        {
                            thdText = $"THD: {mL.ThdDb:F1} dB";
                            thdnText = $"THD+N: {mL.ThdNDb:F1} dB";
                        }

                        // --- F0 + niveaux (toujours affichés, même en mode THD) ---
                        // Bande d’analyse = bande affichée (cohérente avec LIN/LOG)
                        double f0Min = Math.Max(useLog ? 1.0 : 0.0, fMin);
                        double f0Max = fMax;
                        var f0 = EstimateFundamentalHz(currentLeftSpectrum, freqAxis!, f0Min, f0Max, useLog);

                        // Niveaux dBFS (issus des stats temporelles)
                        double? pkFs = _lastPeakFsLeft;
                        double? rmsDbfs = _lastRmsDbfsLeft;
                        double? pkDbfs = (pkFs.HasValue && pkFs.Value > 0.0)
                            ? Math.Max(-200.0, 20.0 * Math.Log10(pkFs.Value))
                            : null;

                        var sb = new System.Text.StringBuilder(512);
                sb.Append(f0.HasValue ? $"F0: {f0.Value:F1} Hz" : "F0: —");
                        sb.Append('\n').Append((pkDbfs.HasValue ? $"dBFS peak: {pkDbfs.Value:F2} dBFS" : "dBFS peak: —"));
                        sb.Append('\n').Append((rmsDbfs.HasValue ? $"dBFS rms:  {rmsDbfs.Value:F2} dBFS" : "dBFS rms:  —"));

                        if (_levelCalValidLeft)
                        {
                            double off = _levelCalOffsetDbLeft;
                            sb.Append('\n').Append((pkDbfs.HasValue ? $"dBV peak:  {(pkDbfs.Value + off):F2} dBV" : "dBV peak:  —"));
                            sb.Append('\n').Append((rmsDbfs.HasValue ? $"dBV rms:   {(rmsDbfs.Value + off):F2} dBV" : "dBV rms:   —"));
                        }

                        // --- Puis les mesures THD ---
                        sb.Append("\n----");
                        sb.Append('\n').Append(thdText);
                        sb.Append('\n').Append(thdnText);
                        sb.Append('\n').Append($"SINAD: {mL.SinadDb:F1} dB");
                        sb.Append('\n').Append($"SNR: {mL.SnrDb:F1} dB");
                        sb.Append('\n').Append($"SFDR: {mL.SfdrDb:F1} dB");

                        lblLeftMeas.Text = sb.ToString();


                        if (showHarmsInPanel && showLeft)
                        {
                            var list = mL.Harmonics;
                            if (list != null && list.Count > 0)
                            {
                                sb.Append("\n----");
                                foreach (var h in list)
                                {
                                    if (_thdUi.Unit == SpectrumSettings.ThdSettings.ThdUnit.Percent)
                                        sb.Append('\n').Append($"H{h.Order}: {h.RatioPercent:F5} %");
                                    else
                                        sb.Append('\n').Append($"H{h.Order}: {h.LevelDbc:F1} dB");
                                }
                            }
                        }


                    }
                    else
                    {
                        lblLeftMeas.Text = "—";
                    }
                }
                else
                {
                    // THD OFF => F0 uniquement (rapide)
                    // Bande d'analyse F0 : on respecte l'axe courant (custom ou auto)
                    // + on évite de descendre trop bas par défaut (rumble/offset)


                    // Bande d’analyse = bande affichée (cohérente avec LIN/LOG + limites custom)
                    double f0Min = Math.Max(useLog ? 1.0 : 0.0, fMin);
                    double f0Max = fMax;

                    var f0 = EstimateFundamentalHz(currentLeftSpectrum, freqAxis!, f0Min, f0Max, useLog);


                    string vdcPrefixL = (_levelUnit == "dBV" && _levelCalValidLeft && _lastVdcVoltsLeft.HasValue)
                        ? $"Vdc: {FormatVoltage(_lastVdcVoltsLeft.Value)}\n"
                        : "";

                    // On affiche aussi les mesures de niveau (peak/rms) en dBFS et dBV si calibré
                    double? pkFs = _lastPeakFsLeft;
                    double? rmsDbfs = _lastRmsDbfsLeft;

                    double? pkDbfs = (pkFs.HasValue && pkFs.Value > 0.0) ? Math.Max(-200.0, 20.0 * Math.Log10(pkFs.Value)) : null;

                    var sb = new System.Text.StringBuilder(512);
                sb.Append(f0.HasValue ? $"F0: {f0.Value:F1} Hz" : "F0: —");

                    sb.Append('\n').Append((pkDbfs.HasValue ? $"dBFS peak: {pkDbfs.Value:F2} dBFS" : "dBFS peak: —"));
                    sb.Append('\n').Append((rmsDbfs.HasValue ? $"dBFS rms:  {rmsDbfs.Value:F2} dBFS" : "dBFS rms:  —"));

                    if (_levelCalValidLeft)
                    {
                        double off = _levelCalOffsetDbLeft;
                        if (pkDbfs.HasValue) sb.Append('\n').Append($"dBV peak:  {(pkDbfs.Value + off):F2} dBV");
                        else sb.Append('\n').Append("dBV peak:  —");

                        if (rmsDbfs.HasValue) sb.Append('\n').Append($"dBV rms:   {(rmsDbfs.Value + off):F2} dBV");
                        else sb.Append('\n').Append("dBV rms:   —");
                    }

                    lblLeftMeas.Text = sb.ToString();

                }
            }
            else if (lblLeftMeas != null)
            {
                lblLeftMeas.Text = "";
            }

            // RIGHT
            if (lblRightMeas != null && currentRightSpectrum != null && _sampleRate > 0 && lastFftSize > 0)
            {
                if (doThd && allowThdMeasurementsUi)
                {
                    int expectedBins = lastFftSize / 2 + 1;
                    double[]? specR = (_thdUi.UseCohAvg &&
                                       _cohAvgRightDb != null &&
                                       _cohAvgRightDb.Length == expectedBins)
                                      ? _cohAvgRightDb
                                      : currentRightSpectrum;

                    mR = SpectrumMetrics.ComputeChannel(specR, _sampleRate, lastFftSize, _metricsOpt);

                    if (mR.Valid)
                    {
                        string thdText;
                        string thdnText;

                        if (_thdUi.Unit == SpectrumSettings.ThdSettings.ThdUnit.Percent)
                        {
                            thdText = $"THD: {mR.Thd * 100.0:F5} %";

                            double thdnPct = Math.Pow(10.0, mR.ThdNDb / 20.0) * 100.0;
                            thdnText = $"THD+N: {thdnPct:F5} %";
                        }
                        else
                        {
                            thdText = $"THD: {mR.ThdDb:F1} dB";
                            thdnText = $"THD+N: {mR.ThdNDb:F1} dB";
                        }

                        // --- F0 + niveaux (toujours affichés, même en mode THD) ---
                        double f0Min = Math.Max(useLog ? 1.0 : 0.0, fMin);
                        double f0Max = fMax;
                        var f0 = EstimateFundamentalHz(currentRightSpectrum, freqAxis!, f0Min, f0Max, useLog);

                        double? pkFs = _lastPeakFsRight;
                        double? rmsDbfs = _lastRmsDbfsRight;
                        double? pkDbfs = (pkFs.HasValue && pkFs.Value > 0.0)
                            ? Math.Max(-200.0, 20.0 * Math.Log10(pkFs.Value))
                            : null;

                        var sb = new System.Text.StringBuilder(512);
                sb.Append(f0.HasValue ? $"F0: {f0.Value:F1} Hz" : "F0: —");
                        sb.Append('\n').Append((pkDbfs.HasValue ? $"dBFS peak: {pkDbfs.Value:F2} dBFS" : "dBFS peak: —"));
                        sb.Append('\n').Append((rmsDbfs.HasValue ? $"dBFS rms:  {rmsDbfs.Value:F2} dBFS" : "dBFS rms:  —"));

                        if (_levelCalValidRight)
                        {
                            double off = _levelCalOffsetDbRight;
                            sb.Append('\n').Append((pkDbfs.HasValue ? $"dBV peak:  {(pkDbfs.Value + off):F2} dBV" : "dBV peak:  —"));
                            sb.Append('\n').Append((rmsDbfs.HasValue ? $"dBV rms:   {(rmsDbfs.Value + off):F2} dBV" : "dBV rms:   —"));
                        }

                        // --- Puis les mesures THD ---
                        sb.Append("\n----");
                        sb.Append('\n').Append(thdText);
                        sb.Append('\n').Append(thdnText);
                        sb.Append('\n').Append($"SINAD: {mR.SinadDb:F1} dB");
                        sb.Append('\n').Append($"SNR: {mR.SnrDb:F1} dB");
                        sb.Append('\n').Append($"SFDR: {mR.SfdrDb:F1} dB");

                        lblRightMeas.Text = sb.ToString();


                        if (showHarmsInPanel && showRight)
                        {
                            var list = mR.Harmonics;
                            if (list != null && list.Count > 0)
                            {
                                sb.Append("\n----");
                                foreach (var h in list)
                                {
                                    if (_thdUi.Unit == SpectrumSettings.ThdSettings.ThdUnit.Percent)
                                        sb.Append('\n').Append($"H{h.Order}: {h.RatioPercent:F5} %");
                                    else
                                        sb.Append('\n').Append($"H{h.Order}: {h.LevelDbc:F1} dB");
                                }
                            }
                        }

                    }
                    else
                    {
                        lblRightMeas.Text = "—";
                    }
                }
                else
                {
                    // THD OFF => F0 uniquement (rapide)
                    // Bande d'analyse F0 : on respecte l'axe courant (custom ou auto)
                    // + on évite de descendre trop bas par défaut (rumble/offset)

                    // Bande d’analyse = bande affichée (cohérente avec LIN/LOG + limites custom)
                    double f0Min = Math.Max(useLog ? 1.0 : 0.0, fMin);
                    double f0Max = fMax;

                    var f0 = EstimateFundamentalHz(currentRightSpectrum, freqAxis!, f0Min, f0Max, useLog);


                    string vdcPrefixR = (_levelUnit == "dBV" && _levelCalValidRight && _lastVdcVoltsRight.HasValue)
                        ? $"Vdc: {FormatVoltage(_lastVdcVoltsRight.Value)}\n"
                        : "";

                    // On affiche aussi les mesures de niveau (peak/rms) en dBFS et dBV si calibré
                    double? pkFs = _lastPeakFsRight;
                    double? rmsDbfs = _lastRmsDbfsRight;

                    double? pkDbfs = (pkFs.HasValue && pkFs.Value > 0.0) ? Math.Max(-200.0, 20.0 * Math.Log10(pkFs.Value)) : null;

                    var sb = new System.Text.StringBuilder(512);
                sb.Append(f0.HasValue ? $"F0: {f0.Value:F1} Hz" : "F0: —");

                    sb.Append('\n').Append((pkDbfs.HasValue ? $"dBFS peak: {pkDbfs.Value:F2} dBFS" : "dBFS peak: —"));
                    sb.Append('\n').Append((rmsDbfs.HasValue ? $"dBFS rms:  {rmsDbfs.Value:F2} dBFS" : "dBFS rms:  —"));

                    if (_levelCalValidRight)
                    {
                        double off = _levelCalOffsetDbRight;
                        if (pkDbfs.HasValue) sb.Append('\n').Append($"dBV peak:  {(pkDbfs.Value + off):F2} dBV");
                        else sb.Append('\n').Append("dBV peak:  —");

                        if (rmsDbfs.HasValue) sb.Append('\n').Append($"dBV rms:   {(rmsDbfs.Value + off):F2} dBV");
                        else sb.Append('\n').Append("dBV rms:   —");
                    }

                    lblRightMeas.Text = sb.ToString();

                }
            }
            else if (lblRightMeas != null)
            {
                lblRightMeas.Text = "";
            }

            RebuildStatsRowsFromLabels();
            RefreshCursorAfterRedraw();
        }



        //
        //
        // =====================
        // Overlays V3 - Library
        // =====================







        /// <summary>
        /// Recharge la bibliothèque interne en cache mémoire.
        /// Par défaut, tout overlay chargé est visible.
        /// </summary>


        /// <summary>
        /// Liste pour UI (future fenêtre Overlays).
        /// </summary>

        /// <summary>
        /// Visible/masqué (future fenêtre Overlays).
        /// </summary>

        //
        /// <summary>
        /// Capture la vue active (Spectrum ou Bode) et l’ajoute dans la bibliothèque.
        /// - Points décimés (plot)
        /// - Visible par défaut
        /// </summary>
        public OverlayEntry OverlayLibraryCaptureActiveView()
        {
            if (_plotFreqAxis == null || _plotFreqAxis.Length == 0)
                throw new InvalidOperationException("Pas de données plot (FreqHz) à capturer.");

            int slot = OverlayLibraryGetNextFreeSlotNumber();
            string defaultName = $"Overlay_{slot}";

            string mode = comboChannel.SelectedItem as string ?? "L+R";

            bool isBode = (mode == "Bode Plot");

            // Contexte (info)
            bool removeDc = false;                 // RemoveDC commenté dans ton UI actuellement
            string window = lastWindow.ToString(); // info fenêtre
            int sr = _sampleRate;
            int fftSize = lastFftSize;

            if (isBode)
            {
                // Bode: gain + phase requis
                if (_plotLeftSpectrum == null || _plotLeftSpectrum.Length == 0)
                    throw new InvalidOperationException("Pas de Gain (plot) à capturer.");
                if (_plotFrfPhaseDeg == null || _plotFrfPhaseDeg.Length == 0)
                    throw new InvalidOperationException("Pas de Phase (plot) à capturer.");

                // Longueur cohérente
                int n = Math.Min(_plotFreqAxis.Length, Math.Min(_plotLeftSpectrum.Length, _plotFrfPhaseDeg.Length));
                var fx = _plotFreqAxis.Take(n).ToArray();
                var gain = _plotLeftSpectrum.Take(n).ToArray();
                var phase = _plotFrfPhaseDeg.Take(n).ToArray();

                var dto = new OverlayFile
                {
                    Version = OverlayFile.CurrentVersion,
                    Name = defaultName, // sera renommé par la bibliothèque
                    SlotNumber = slot, // info
                    CreatedUtc = DateTime.UtcNow,
                    Kind = OverlayKind.Bode,

                    SampleRate = sr,
                    FftSize = fftSize,
                    Window = window,
                    RemoveDc = removeDc,

                    FreqHz = fx,
                    GainDb = gain,
                    PhaseDeg = phase
                };

                var entry = _overlayLibrary.AddCapturedOverlay(dto, $"{defaultName}_{DateTime.Now:yyyy-MM-dd HH-mm-ss}");

                // Mise à jour cache (visible par défaut)
                OverlayLibraryReloadCache();
                _overlayLibrary.SetOverlayVisible(entry.FilePath, true);
                UpdateSpectrumChartAndStats(lastTimeLeft, lastTimeRight);

                return entry;
            }
            else
            {
                // Spectrum: on capture ce qui est affiché
                // - si mode Left: Left uniquement
                // - si mode Right: Right uniquement
                // - si mode L+R: les deux
                bool captureLeft = (mode == "Left" || mode == "L+R") && _plotLeftSpectrum != null;
                bool captureRight = (mode == "Right" || mode == "L+R") && _plotRightSpectrum != null;

                if (!captureLeft && !captureRight)
                    throw new InvalidOperationException("Pas de spectre plot à capturer (Left/Right).");

                int n = _plotFreqAxis.Length;
                if (captureLeft) n = Math.Min(n, _plotLeftSpectrum!.Length);
                if (captureRight) n = Math.Min(n, _plotRightSpectrum!.Length);

                var fx = _plotFreqAxis.Take(n).ToArray();

                var dto = new OverlayFile
                {
                    Version = OverlayFile.CurrentVersion,
                    Name = defaultName,
                    SlotNumber = slot,
                    CreatedUtc = DateTime.UtcNow,
                    Kind = OverlayKind.Spectrum,

                    SampleRate = sr,
                    FftSize = fftSize,
                    Window = window,
                    RemoveDc = removeDc,

                    FreqHz = fx,
                    LeftDbFs = captureLeft ? _plotLeftSpectrum!.Take(n).ToArray() : null,
                    RightDbFs = captureRight ? _plotRightSpectrum!.Take(n).ToArray() : null
                };

                var entry = _overlayLibrary.AddCapturedOverlay(dto, $"{defaultName}_{DateTime.Now:yyyy-MM-dd HH-mm-ss}");   

                OverlayLibraryReloadCache();
                UpdateSpectrumChartAndStats(lastTimeLeft, lastTimeRight);

                return entry;
            }
        }



        //
        public OverlayKind OverlayGetActiveKind()
        {
            string mode = comboChannel.SelectedItem as string ?? "L+R";
            bool isBode = (mode.IndexOf("bode", StringComparison.OrdinalIgnoreCase) >= 0);
            return isBode ? OverlayKind.Bode : OverlayKind.Spectrum;
        }

        //
        // Mémorise la fenêtre choisie hors FRF, pour la restaurer quand on sort du mode Bode Plot.
        private int _windowIndexBeforeFrf = -1;

        


        //
        // ===== Stats panel (table) =====
        private enum StatEmphasis { Normal, Medium, Strong }

        private sealed class StatRow
        {
            public string Label { get; init; } = "";
            public string Value { get; init; } = "";
            public StatEmphasis Emphasis { get; init; } = StatEmphasis.Normal;

            // Nouveau : ligne de séparation visuelle
            public bool IsSeparator { get; init; } = false;

            public bool IsBottomGroup { get; init; } = false;

        }


        private readonly List<StatRow> _rowsLeft = new();
        private readonly List<StatRow> _rowsRight = new();

        //

        //
        // Injecte les lignes Vrms/Vpeak/Vdc au début des mesures existantes


        //


        private static void BuildRowsForChannel(List<StatRow> dst, string? rms, string? peak, string? rmsV, string? peakV, string? meas)
        {
            void AddLine(string? line)
            {
                if (string.IsNullOrWhiteSpace(line)) return;
                line = line.Trim();
                if (line == "—") return;

                // Marqueur de séparation (inséré par UpdateSpectrumChartAndStats)
                if (line == "----")
                {
                    dst.Add(new StatRow { IsSeparator = true });
                    return;
                }

                int idx = line.IndexOf(':');
                if (idx <= 0 || idx >= line.Length - 1) return;

                string label = line[..idx].Trim();
                string value = line[(idx + 1)..].Trim();

                if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(value))
                    return;

                var emph = StatEmphasis.Normal;

                if (label.Equals("F0", StringComparison.OrdinalIgnoreCase) ||
                    label.Equals("Vrms", StringComparison.OrdinalIgnoreCase) ||
                    label.Equals("dBFS peak", StringComparison.OrdinalIgnoreCase))
                {
                    emph = StatEmphasis.Strong;
                }

                if (label.Equals("RMS", StringComparison.OrdinalIgnoreCase) ||
                    label.Equals("F0", StringComparison.OrdinalIgnoreCase))
                    emph = StatEmphasis.Strong;
                else if (label.Equals("Peak", StringComparison.OrdinalIgnoreCase))
                    emph = StatEmphasis.Medium;

                dst.Add(new StatRow { Label = label, Value = value, Emphasis = emph });
            }

            AddLine(rms);
            AddLine(peak);
            AddLine(rmsV);
            AddLine(peakV);

            if (!string.IsNullOrWhiteSpace(meas) && meas.Trim() != "—")
            {
                foreach (var ln in meas.Replace("\r\n", "\n").Split('\n'))
                    AddLine(ln);
            }
        }

        //
        //
        private void ApplyChartStatsMargin(Chart chart, bool invalidate, float reservedScale)
        {
            var area = chart.ChartAreas.FirstOrDefault(a => a.Name == "SpectrumArea");
            if (area == null) return;

            int w = chart.ClientSize.Width;
            if (w <= 0) return;

            int reserved = (int)(ChartStatsWidthPx * reservedScale);

            int maxReserved = Math.Max(120, w - ChartStatsMinPlotWidthPx);
            reserved = Math.Min(reserved, maxReserved);

            float reservedPct = 100f * reserved / w;
            reservedPct = Math.Max(0f, Math.Min(60f, reservedPct)); // tu peux monter à 60% si besoin

            area.Position.Auto = false;

            const float left = 3f;
            const float top = 3f;
            const float height = 94f;

            float width = 97f - reservedPct;
            if (width < 10f) width = 10f;

            area.Position = new ElementPosition(left, top, width, height);

            if (invalidate)
                chart.Invalidate();
        }

        //
        // --------------------------------------------------------------------
        // THD UI handlers (checkbox + button in the Spectrum tab)
        // --------------------------------------------------------------------



    }
}
