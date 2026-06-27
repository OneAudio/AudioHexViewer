using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AudioHexViewer
{
    internal sealed class SplashScreen : Form
    {
        // ── Infos produit ─────────────────────────────────────────────────────
        public static string AppVersion
        {
            get
            {
                var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string ver = v != null ? $"V {v.Major}.{v.Minor}.{v.Build}" : "V 0.0.0";
                return $"{ver}  build {BuildInfo.Timestamp}";
            }
        }
        public const string AppDate    = "06/2026";
        public const string AppTitle   = "AudioHexViewer";
        public const string AppSubLine = "Digital audio data Analyzer";
        public const string AppAuthor  = "OnE Audio Projects";

        // ── Couleurs du texte / accents ──────────────────────────────────────
        // Regroupées ici pour pouvoir essayer rapidement d'autres teintes :
        // modifier les valeurs ci-dessous puis recompiler (le build se renumérote seul).
        //
        // Rôles :
        //   AhvTop / AhvBottom : dégradé du "AHV" (titre principal, de haut en bas)
        //   AhvShadow          : ombre portée derrière "AHV"
        //   AccentCol          : ligne séparatrice + barre de progression
        //   SubCol             : "AudioHexViewer", "OnE Audio Projects", version, copyright, statut
        //   DimCol             : réservé (actuellement inutilisé)
        //   BgTop / BgBottom   : dégradé de fond, UNIQUEMENT si l'image de fond est absente
        // private static readonly Color AhvTop    = Color.FromArgb(190, 215, 255);
        // private static readonly Color AhvBottom = Color.White;
        // private static readonly Color AhvShadow = Color.FromArgb(55, 0, 0, 0);
        // private static readonly Color AccentCol = Color.FromArgb(0, 180, 255);
        // private static readonly Color SubCol    = Color.FromArgb(180, 200, 230);
        // private static readonly Color DimCol    = Color.FromArgb(80,  110, 160);
        private static readonly Color BgTop     = Color.FromArgb(0,   0,  80);
        private static readonly Color BgBottom  = Color.FromArgb(0,   0,  40);

        // ── Palettes "texte sombre sur fond clair" à essayer ──────────────────
        // Pour tester : commente le bloc ci-dessus (de AhvTop à DimCol) et
        // décommente UNE des palettes ci-dessous, puis recompile.
        //
        // -- Palette "Nuit orange" --
        private static readonly Color AhvTop    = Color.FromArgb(245, 248, 255);  // blanc légèrement bleuté
        private static readonly Color AhvBottom = Color.FromArgb(185, 195, 210);  // gris-blanc pour effet 3D
        private static readonly Color AhvShadow = Color.FromArgb(160, 0, 0, 0);   // ombre sombre (fond noir)
        private static readonly Color AccentCol = Color.FromArgb(255, 251, 112,   1); // R=251 V=112 B=1 A=100%
        private static readonly Color SubCol    = Color.White;                         // RVBA ffffffff
        private static readonly Color DimCol    = Color.FromArgb(155, 160, 170);       // gris clair (copyright/version)
        //
        // -- Palette "Gris ardoise" --
        // private static readonly Color AhvTop    = Color.FromArgb(60, 65, 75);
        // private static readonly Color AhvBottom = Color.FromArgb(20, 25, 35);
        // private static readonly Color AhvShadow = Color.FromArgb(40, 255, 255, 255);
        // private static readonly Color AccentCol = Color.FromArgb(0, 140, 180);
        // private static readonly Color SubCol    = Color.FromArgb(90, 100, 115);
        // private static readonly Color DimCol    = Color.FromArgb(140, 150, 165);

        // ── Image de fond ─────────────────────────────────────────────────────
        private static readonly System.Drawing.Image? _bgImage = LoadBgImage();
        private static System.Drawing.Image? LoadBgImage()
        {
            try
            {
                string path = System.IO.Path.Combine(
                    AppContext.BaseDirectory,
                    "splash_bg.png");
                if (System.IO.File.Exists(path))
                    return System.Drawing.Image.FromFile(path);
                return null;
            }
            catch { return null; }
        }

        // ── Messages de statut ────────────────────────────────────────────────
        private string _statusMessage = "Initializing...";
        public void SetStatus(string message)
        {
            _statusMessage = message;
            Invoke(() => Invalidate());
        }
        private readonly bool _isAboutMode;   // true = boîte About (pas de barre de chargement)

        // ── Animation (mode splash seulement) ────────────────────────────────
        private float  _progress = 0f;
        private int    _dots     = 0;
        private float  _glowPos  = 0f;
        private bool   _readyToClose = false;
        private System.Windows.Forms.Timer? _timer;

        // ── Constructeur ─────────────────────────────────────────────────────
        public SplashScreen(bool aboutMode = false)
        {
            _isAboutMode = aboutMode;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition   = FormStartPosition.CenterScreen;
            ClientSize      = new Size(340, 230);
            BackColor       = Color.FromArgb(8, 10, 20);
            ShowInTaskbar   = false;
            TopMost         = true;
            DoubleBuffered  = true;
            Text            = "";
            MaximizeBox     = false;
            MinimizeBox     = false;

            // Coins arrondis pour les deux modes
            int rr = 14;
            var path = new GraphicsPath();
            path.AddArc(0, 0, rr*2, rr*2, 180, 90);
            path.AddArc(Width-rr*2, 0, rr*2, rr*2, 270, 90);
            path.AddArc(Width-rr*2, Height-rr*2, rr*2, rr*2, 0, 90);
            path.AddArc(0, Height-rr*2, rr*2, rr*2, 90, 90);
            path.CloseFigure();
            Region = new Region(path);

            if (aboutMode)
            {
                // Fermeture au clic ou Echap
                MouseClick += (_, _) => Close();
                KeyPreview  = true;
                KeyDown    += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
            }

            Paint += OnPaint;

            if (!aboutMode)
            {
                _timer = new System.Windows.Forms.Timer { Interval = 30 };
                _timer.Tick += (_, _) =>
                {
                    if (!_readyToClose && _progress < 0.85f)
                        _progress = Math.Min(0.85f, _progress + 0.016f);
                    else if (_readyToClose && _progress < 1f)
                        _progress = Math.Min(1f, _progress + 0.022f);
                    _glowPos = (_glowPos + 0.022f) % 1f;
                    _dots    = (_dots + 1) % (4 * 8);
                    Invalidate();
                    if (_readyToClose && _progress >= 1f)
                    {
                        _timer!.Stop();
                        var t = new System.Windows.Forms.Timer { Interval = 200 };
                        t.Tick += (_, _) => { t.Stop(); t.Dispose(); Close(); };
                        t.Start();
                    }
                };
                _timer.Start();
            }
        }

        public void SignalReady()
        {
            _statusMessage = "Ready !";
            Invalidate();
            _readyToClose = true;
        }

        // Listes mises en cache pendant le scan — réutilisées à chaque ouverture de Devices
        public static List<NAudio.CoreAudioApi.MMDevice> CachedInputs  { get; } = new();
        public static List<NAudio.CoreAudioApi.MMDevice> CachedOutputs { get; } = new();
        // Noms pré-cachés (FriendlyName déclenche un appel COM lent à chaque accès)
        public static Dictionary<string, string> CachedNames { get; } = new();

        /// <summary>Lance le scan audio pendant l'affichage du splash.</summary>
        public void RunAudioScan(int[] srOptions)
        {
            _statusMessage = "Detecting audio devices...";
            Invalidate();
            Application.DoEvents();

            CachedInputs.Clear();
            CachedOutputs.Clear();
            List<NAudio.CoreAudioApi.MMDevice> all = new();
            try
            {
                var en = new NAudio.CoreAudioApi.MMDeviceEnumerator();
                var inputs  = en.EnumerateAudioEndPoints(
                    NAudio.CoreAudioApi.DataFlow.Capture,
                    NAudio.CoreAudioApi.DeviceState.Active);
                var outputs = en.EnumerateAudioEndPoints(
                    NAudio.CoreAudioApi.DataFlow.Render,
                    NAudio.CoreAudioApi.DeviceState.Active);
                CachedInputs .AddRange(inputs);
                CachedOutputs.AddRange(outputs);
                all.AddRange(inputs);
                all.AddRange(outputs);
            }
            catch { return; }

            _statusMessage = $"Scanning audio formats ({all.Count} devices)...";
            Invalidate();
            Application.DoEvents();

            int total = all.Count * srOptions.Length;
            int done  = 0;

            foreach (var device in all)
            {
                // Pré-cache FriendlyName
                if (!CachedNames.ContainsKey(device.ID))
                    CachedNames[device.ID] = device.FriendlyName;

                string shortName = device.FriendlyName.Length > 30
                    ? device.FriendlyName[..30] + "…"
                    : device.FriendlyName;
                _statusMessage = $"Scanning: {shortName}";
                Invalidate();
                Application.DoEvents();

                // ── Phase 1 : IsFormatSupported pour chaque SR ──────────────
                int confirmedPhase1 = 0;
                foreach (int sr in srOptions)
                {
                    var key = (device.ID, sr);
                    if (!AudioHexViewer.DevicesDialog.SrCache.ContainsKey(key))
                    {
                        bool ok = TestAudioFormat(device, sr);
                        AudioHexViewer.DevicesDialog.SrCache[key] = ok;
                        if (ok) confirmedPhase1++;
                    }
                    else if (AudioHexViewer.DevicesDialog.SrCache[(device.ID, sr)])
                        confirmedPhase1++;
                    done++;
                    if (total > 0) _progress = Math.Min(0.95f, (float)done / total);
                    Application.DoEvents();
                }

                // ── Phase 2 : si Phase 1 ne confirme RIEN pour ce device ────
                // Certains drivers (ex: MKADC1) répondent toujours "non supporté"
                // à IsFormatSupported même pour leurs SR natifs (bug driver).
                // On sonde alors via Initialize réel, plus fiable.
                if (confirmedPhase1 == 0)
                {

                    // Récupère le MixFormat pour l'utiliser comme template de format
                    NAudio.Wave.WaveFormat? mixFmt = null;
                    try { mixFmt = device.AudioClient.MixFormat; } catch { }

                    foreach (int sr in srOptions)
                    {
                        var key = (device.ID, sr);
                        bool ok = false;

                        // Candidat 1 : même encoding/bits que le MixFormat, SR variable
                        if (!ok && mixFmt != null)
                        {
                            var fmt = mixFmt.Encoding == NAudio.Wave.WaveFormatEncoding.IeeeFloat
                                ? NAudio.Wave.WaveFormat.CreateIeeeFloatWaveFormat(sr, mixFmt.Channels)
                                : (NAudio.Wave.WaveFormat)new NAudio.Wave.WaveFormatExtensible(
                                    sr, mixFmt.BitsPerSample, mixFmt.Channels);
                            ok = ProbeExclusiveInit(device, fmt);
                        }
                        // Candidat 2 : 32-bit PCM extensible
                        if (!ok) ok = ProbeExclusiveInit(device,
                            new NAudio.Wave.WaveFormatExtensible(sr, 32, 2));
                        // Candidat 3 : IEEE Float non-extensible
                        if (!ok) ok = ProbeExclusiveInit(device,
                            NAudio.Wave.WaveFormat.CreateIeeeFloatWaveFormat(sr, 2));

                        AudioHexViewer.DevicesDialog.SrCache[key] = ok;
                        done++;
                        if (total > 0) _progress = Math.Min(0.85f, (float)done / total);
                        Application.DoEvents();
                    }
                }
            }
            _statusMessage = "Loading interface...";
            Invalidate();
            Application.DoEvents();
        }

        // Pas de réflexion — on crée un MMDevice FRAIS via GetDevice(id)
        // pour éviter tout état corrompu du cache AudioClient

        private static bool TestAudioFormat(NAudio.CoreAudioApi.MMDevice device, int sampleRate)
        {
            NAudio.Wave.WaveFormat[] cands =
            {
                NAudio.Wave.WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2),
                new NAudio.Wave.WaveFormatExtensible(sampleRate, 32, 2),
                new NAudio.Wave.WaveFormatExtensible(sampleRate, 24, 2),
                new NAudio.Wave.WaveFormatExtensible(sampleRate, 16, 2),
                // Formats PCM non-extensibles (tag 0x0001) — requis par certains
                // drivers embarqués (ex: STM32 USB Audio, ST libs)
                new NAudio.Wave.WaveFormat(sampleRate, 32, 2),
                new NAudio.Wave.WaveFormat(sampleRate, 24, 2),
                new NAudio.Wave.WaveFormat(sampleRate, 16, 2),
            };

            // Phase 1 : IsFormatSupported
            // AUDCLNT_E_UNSUPPORTED_FORMAT (0x88890008) = réponse propre "non supporté"
            // Autre exception COM = driver bugué (ex: MKADC1) → tente la phase 2
            bool allWereComErrors = true;
            foreach (var wf in cands)
            {
                try
                {
                    if (device.AudioClient.IsFormatSupported(
                            NAudio.CoreAudioApi.AudioClientShareMode.Exclusive, wf))
                        return true;
                    allWereComErrors = false;  // réponse propre "non supporté"
                }
                catch (System.Runtime.InteropServices.COMException ex)
                {
                    if (unchecked((uint)ex.HResult) == 0x88890008u)
                        allWereComErrors = false;  // AUDCLNT_E_UNSUPPORTED_FORMAT → propre
                    // Autre erreur COM → allWereComErrors reste true
                }
                catch { /* allWereComErrors reste true */ }
            }

            // Phase 2 : Si tous les appels ont levé des erreurs COM non-format
            // (driver ne supporte pas IsFormatSupported), sonde via Initialize
            if (allWereComErrors)
            {
                foreach (var wf in cands)
                    if (ProbeExclusiveInit(device, wf)) return true;
            }

            return false;
        }

        private static bool ProbeExclusiveInit(NAudio.CoreAudioApi.MMDevice device,
                                               NAudio.Wave.WaveFormat fmt)
        {
            const uint E_NOT_ALIGNED = 0x88890019u;

            // Crée un MMDevice FRAIS via GetDevice(id) — évite tout état corrompu
            // du cache AudioClient (le champ privé NAudio n'est pas accessible en .NET 10)
            NAudio.CoreAudioApi.MMDevice? fresh = null;
            NAudio.CoreAudioApi.AudioClient? ac  = null;
            try
            {
                fresh = new NAudio.CoreAudioApi.MMDeviceEnumerator().GetDevice(device.ID);
                if (fresh == null) return false;
                ac = fresh.AudioClient;
                long period = ac.DefaultDevicePeriod;


                try
                {
                    ac.Initialize(
                        NAudio.CoreAudioApi.AudioClientShareMode.Exclusive,
                        NAudio.CoreAudioApi.AudioClientStreamFlags.EventCallback,
                        period, period, fmt, Guid.Empty);
                    return true;
                }
                catch (System.Runtime.InteropServices.COMException ex)
                    when (unchecked((uint)ex.HResult) == E_NOT_ALIGNED)
                {
                    int bufSize;
                    try { bufSize = ac.BufferSize; } catch { return false; }
                    long aligned = (long)Math.Round(10_000_000.0 * bufSize / fmt.SampleRate);
                    ac.Dispose();    ac    = null;
                    fresh.Dispose(); fresh = null;
                    fresh = new NAudio.CoreAudioApi.MMDeviceEnumerator().GetDevice(device.ID);
                    if (fresh == null) return false;
                    ac = fresh.AudioClient;
                    ac.Initialize(
                        NAudio.CoreAudioApi.AudioClientShareMode.Exclusive,
                        NAudio.CoreAudioApi.AudioClientStreamFlags.EventCallback,
                        aligned, aligned, fmt, Guid.Empty);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                if (ac    != null) try { ac.Dispose();    } catch { }
                if (fresh != null) try { fresh.Dispose(); } catch { }
            }
        }

        // ── Rendu ─────────────────────────────────────────────────────────────
        private void OnPaint(object? sender, PaintEventArgs e)
        {
            var g  = e.Graphics;
            var rc = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            // Image de fond
            if (_bgImage != null)
                g.DrawImage(_bgImage, rc);
            else
            {
                using var br = new LinearGradientBrush(rc, BgTop, BgBottom, 90f);
                g.FillRectangle(br, rc);
            }

            // Contour lumineux — épais, calé sur le bord extérieur pour masquer le débordement de l'image
            int rr2 = 14;
            using var borderPath = new GraphicsPath();
            borderPath.AddArc(0, 0, rr2*2, rr2*2, 180, 90);
            borderPath.AddArc(rc.Width-rr2*2, 0, rr2*2, rr2*2, 270, 90);
            borderPath.AddArc(rc.Width-rr2*2, rc.Height-rr2*2, rr2*2, rr2*2, 0, 90);
            borderPath.AddArc(0, rc.Height-rr2*2, rr2*2, rr2*2, 90, 90);
            borderPath.CloseFigure();
            // using var borderPen = new Pen(Color.FromArgb(180, 140, 200, 255), 4f);
            using var borderPen = new Pen(Color.FromArgb(55, 255, 255, 255), 2f);
            g.DrawPath(borderPen, borderPath);

            // "AH" blanc dégradé  +  "V" orange — Calibri Gras +30%
            float ahvY = 2f;
            using var fAhv = new Font("Calibri", 75f, FontStyle.Bold, GraphicsUnit.Point);

            // Largeur totale "AHV" pour le centrage (y compris padding GDI+)
            float ahvW = g.MeasureString("AHV", fAhv).Width;
            float ahvH = fAhv.GetHeight(g);
            float startX = rc.Width / 2f - ahvW / 2f;

            // Position de "V" : largeur typographique serrée de "AH" (sans padding)
            float ahTight = g.MeasureString("AH", fAhv, Point.Empty,
                                            StringFormat.GenericTypographic).Width;
            float vStartX = startX + ahTight;

            // Ombres
            using (var sh = new SolidBrush(AhvShadow))
            {
                g.DrawString("AH", fAhv, sh, startX  + 2, ahvY + 2);
                g.DrawString("V",  fAhv, sh, vStartX + 2, ahvY + 2);
            }
            // "AH" avec dégradé blanc → gris-blanc (effet légèrement 3D)
            using (var tBr = new LinearGradientBrush(
                new PointF(0, ahvY), new PointF(0, ahvY + ahvH),
                AhvTop, AhvBottom))
                g.DrawString("AH", fAhv, tBr, startX, ahvY);
            // "V" plein orange
            using (var oBr = new SolidBrush(AccentCol))
                g.DrawString("V", fAhv, oBr, vStartX, ahvY);

            // "AudioHexViewer" — Century Gothic 17pt, collé juste sous AHV
            using var fSub = new Font("Century Gothic", 17f, FontStyle.Regular, GraphicsUnit.Point);
            using var bSub = new SolidBrush(SubCol);
            float titleW  = g.MeasureString(AppTitle, fSub).Width;
            float titleH  = fSub.GetHeight(g);
            float titleY  = ahvY + ahvH - 28f;   // quasi collé
            g.DrawString(AppTitle, fSub, bSub, rc.Width/2f - titleW/2f, titleY);

            // Ligne — entre AudioHexViewer et OnE Audio Projects
            float sepY = titleY + titleH + 2f;
            using (var lBr = new LinearGradientBrush(
                new PointF(20, sepY), new PointF(rc.Width - 20, sepY),
                Color.Transparent, Color.Transparent))
            {
                lBr.InterpolationColors = new ColorBlend
                {
                    Colors    = new[] { Color.Transparent, AccentCol, Color.White, AccentCol, Color.Transparent },
                    Positions = new[] { 0f, 0.2f, 0.5f, 0.8f, 1f }
                };
                using var pen = new Pen(lBr, 1.2f);
                g.DrawLine(pen, 20, sepY, rc.Width - 20, sepY);
            }

            // "OnE Audio Projects" — Calibri 13pt
            using var fVer = new Font("Calibri", 13f, FontStyle.Regular, GraphicsUnit.Point);
            using var bVer = new SolidBrush(SubCol);
            float authorH = fVer.GetHeight(g);
            float authorY = sepY + 2f;
            float wAuthor = g.MeasureString(AppAuthor, fVer).Width;
            g.DrawString(AppAuthor, fVer, bVer, rc.Width/2f - wAuthor/2f, authorY);

            // "Digital audio data Analyzer" — même police/couleur, sous "OnE Audio Projects".
            // Visible uniquement dans la boîte About (pas sur le splash avec barre de progression).
            if (_isAboutMode)
            {
                float subLineY = authorY + authorH + 2f;
                float wSubLine = g.MeasureString(AppSubLine, fVer).Width;
                g.DrawString(AppSubLine, fVer, bVer, rc.Width/2f - wSubLine/2f, subLineY);
            }

            // Barre de progression / décorative
            float barY = _isAboutMode
                ? authorY + authorH + 48f   // About : espace pour AppSubLine + marge avant les pics
                : authorY + authorH + 32f;  // Splash : remonté pour laisser de la place en bas

            // Version — sous Loading, alignée à droite
            using var fVn = new Font("Calibri", 9f, FontStyle.Regular, GraphicsUnit.Point);
            using var bVn = new SolidBrush(DimCol);
            float vw = g.MeasureString(AppVersion, fVn).Width;
            // Version dessinée dans le bloc if (!_isAboutMode) sous Loading
            // (en mode About, affichée séparément ci-dessous)
            if (_isAboutMode)
            {
                DrawSpectrumBar(g, rc.Width, barY, 1.0f);   // barre entièrement orange en mode About
                g.DrawString(AppVersion, fVn, bVn, rc.Width - vw - 6f, rc.Height - 18f);
                string copy = $"© {DateTime.Now.Year} OnE Audio Projects";
                using var bC = new SolidBrush(DimCol);
                g.DrawString(copy, fVn, bC, 6f, rc.Height - 18f);
            }

            if (!_isAboutMode)
            {
                DrawSpectrumBar(g, rc.Width, barY, _progress);

                // Position bas de la barre → texte de statut
                const float svgH2 = 129.89961f, svgB2 = 109.29961f, svgW2 = 1210.1934f;
                float barBottom = barY + (svgH2 - svgB2) * ((rc.Width - 40f) / svgW2);
                using var fL = new Font("Calibri", 9f, FontStyle.Regular, GraphicsUnit.Point);
                using var bL = new SolidBrush(SubCol);
                float lw2 = g.MeasureString(_statusMessage, fL).Width;
                g.DrawString(_statusMessage, fL, bL, rc.Width / 2f - lw2 / 2f, barBottom + 5f);

                // Version et copyright en bas
                g.DrawString(AppVersion, fVn, bVn, rc.Width - vw - 6f, rc.Height - 18f);
                string copy = $"© {DateTime.Now.Year} OnE Audio Projects";
                using var bC = new SolidBrush(DimCol);
                g.DrawString(copy, fVn, bC, 6f, rc.Height - 18f);
            }
        }

        private static GraphicsPath MakeCircle(int cx, int cy, int r)
        { var p = new GraphicsPath(); p.AddEllipse(cx-r, cy-r, r*2, r*2); return p; }

        private static GraphicsPath RoundRect(float x, float y, float w, float h, float r)
        {
            var p = new GraphicsPath();
            if (w < 1) { p.AddLine(x,y,x,y+h); return p; }
            p.AddArc(x,      y,      r*2, r*2, 180,  90);
            p.AddArc(x+w-r*2,y,      r*2, r*2, 270,  90);
            p.AddArc(x+w-r*2,y+h-r*2,r*2,r*2,   0,  90);
            p.AddArc(x,      y+h-r*2,r*2,r*2,  90,  90);
            p.CloseFigure();
            return p;
        }

        /// <summary>Dessine la barre spectre (silhouette SVG scalée).
        /// progress = 0..1  (0 = tout gris, 1 = tout orange).</summary>
        private static void DrawSpectrumBar(Graphics g, int rcWidth, float barY, float progress)
        {
            const float svgW = 1210.1934f;
            const float svgH = 129.89961f;
            const float svgB = 109.29961f;
            float bx = 20f, bw = rcWidth - 40f;
            float scale  = bw / svgW;
            float filled = bw * progress;
            float tx = bx, ty = barY - svgB * scale;

            using var barPath = BuildBarShape();
            using var mx = new System.Drawing.Drawing2D.Matrix(scale, 0f, 0f, scale, tx, ty);
            barPath.Transform(mx);

            using (var grayBr = new SolidBrush(Color.FromArgb(112, 122, 133)))
                g.FillPath(grayBr, barPath);

            if (filled > 0f)
            {
                using var orangeBr = new SolidBrush(AccentCol);
                var savedClip = g.Clip;
                using var clipRgn = new System.Drawing.Region(
                    new RectangleF(tx - 1f, ty - 2f, filled + 1f, svgH * scale + 4f));
                g.IntersectClip(clipRgn);
                g.FillPath(orangeBr, barPath);
                g.Clip = savedClip;
                savedClip.Dispose();
            }
        }

        /// <summary>Silhouette de la barre de progression (5 pics + base arrondie)
        /// dans le repère SVG 1210×129.9 — appliquer une Matrix pour redimensionner.</summary>
        private static GraphicsPath BuildBarShape()
        {
            var p = new GraphicsPath();
            const float B = 109.29961f;   // y de la ligne de base SVG

            // Ligne de base gauche
            p.AddLine(11f, B, 321f, B);

            // ── Pic 1 (le plus haut, ~109 u) ─────────────────────────────────
            p.AddBezier(321f, B,       325f, B,       328f, B-3f,    329f, B-10f);
            p.AddLine  (329f, B-10f,   331f, 12f);
            p.AddBezier(331f,  12f,    331f,  4f,     338f,  0f,     344f,  0f);
            p.AddBezier(344f,   0f,    350f,  0f,     357f,  4f,     357f, 12f);
            p.AddLine  (357f,  12f,    359f, B-10f);
            p.AddBezier(359f, B-10f,   360f, B-3f,   363f,  B,      367f,  B);
            p.AddLine  (367f, B, 443f, B);      // espace 1 → 2

            // ── Pic 2 (~72 u) ────────────────────────────────────────────────
            p.AddBezier(443f,  B,      447f,  B,      450f, B-3f,    451f, B-10f);
            p.AddLine  (451f, B-10f,   453f, 48.9f);
            p.AddBezier(453f, 48.9f,   453f, 40.9f,   460f, 36.9f,   466f, 36.9f);
            p.AddBezier(466f, 36.9f,   472f, 36.9f,   479f, 40.9f,   479f, 48.9f);
            p.AddLine  (479f, 48.9f,   481f, B-10f);
            p.AddBezier(481f, B-10f,   482f, B-3f,   485f,  B,      489f,  B);
            p.AddLine  (489f, B, 543f, B);      // espace 2 → 3

            // ── Pic 3 (~56 u) ────────────────────────────────────────────────
            p.AddBezier(543f,  B,      547f,  B,      550.6f, B-3f,   551f, B-10f);
            p.AddLine  (551f, B-10f,   553f, 64.28f);
            p.AddBezier(553f, 64.28f,  553.4f,57.22f, 560f,  53.28f, 566f, 53.28f);
            p.AddBezier(566f, 53.28f,  572f,  53.28f, 579.6f,57.22f, 580f, 64.28f);
            p.AddLine  (580f, 64.28f,  582f, B-10f);
            p.AddBezier(582f, B-10f,   582.4f,B-3f,  586f,  B,      590f,  B);
            p.AddLine  (590f, B, 641f, B);      // espace 3 → 4

            // ── Pic 4 (~40 u) ────────────────────────────────────────────────
            p.AddBezier(641f,  B,      645f,  B,      648f,  B-3f,   649f, B-8f);
            p.AddLine  (649f, B-8f,    651f, 79.09f);
            p.AddBezier(651f, 79.09f,  652f, 73.09f,  658f,  69.09f, 664f, 69.09f);
            p.AddBezier(664f, 69.09f,  670f, 69.09f,  676f,  73.09f, 677f, 79.09f);
            p.AddLine  (677f, 79.09f,  679f, B-8f);
            p.AddBezier(679f, B-8f,    680f, B-3f,   683f,  B,      687f,  B);
            p.AddLine  (687f, B, 721f, B);      // espace 4 → 5

            // ── Pic 5 (petit, ~14 u) ─────────────────────────────────────────
            p.AddBezier(721f,  B,      725f,  B,      728f,  B-2f,   729f, B-5f);
            p.AddLine  (729f, B-5f,    730f, 102.3f);
            p.AddBezier(730f, 102.3f,  732f, 97.3f,   737f,  95.3f,  742f, 95.3f);
            p.AddBezier(742f,  95.3f,  747f, 95.3f,   752f,  97.3f,  754f, 102.3f);
            p.AddLine  (754f, 102.3f,  755f, B-5f);
            p.AddBezier(755f, B-5f,    756f, B-2f,   759f,  B,      763f,  B);

            // ── Queue droite + rectangle bas arrondi ──────────────────────────
            p.AddLine  (763f,       B,          1200.41f, 108.85f);
            p.AddBezier(1200.41f, 108.85f,   1206.41f, 108.84f, 1210.19f, 114.3f, 1210.19f, 119.3f);
            p.AddBezier(1210.19f, 119.3f,    1210.19f, 124.3f,  1206.06f, 129.9f, 1200.06f, 129.9f);
            p.AddLine  (1200.06f, 129.9f,    11f,       129.3f);
            p.AddBezier(11f,       129.3f,   5f,        129.3f,  0f,       124.3f, 0f,       119.3f);
            p.AddBezier(0f,        119.3f,   0f,        114.3f,  5f,       109.3f, 11f,      109.3f);
            p.CloseFigure();
            return p;
        }

        protected override void Dispose(bool disposing)
        { if (disposing) _timer?.Dispose(); base.Dispose(disposing); }
    }
}
