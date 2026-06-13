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
        // -- Palette "Bleu nuit" --
        private static readonly Color AhvTop    = Color.FromArgb(40, 70, 130);
        private static readonly Color AhvBottom = Color.FromArgb(10, 25, 60);
        private static readonly Color AhvShadow = Color.FromArgb(40, 255, 255, 255);
        private static readonly Color AccentCol = Color.FromArgb(0, 120, 200);
        private static readonly Color SubCol    = Color.FromArgb(70, 90, 130);
        private static readonly Color DimCol    = Color.FromArgb(130, 145, 170);
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
                    System.IO.Path.GetDirectoryName(
                        System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "",
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
            BackColor       = Color.FromArgb(0, 0, 64);
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
                catch (Exception ex)
                {
                    return false;
                }
            }
            catch (Exception ex)
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
            using var borderPen = new Pen(Color.FromArgb(255, 255, 255, 255), 4f);
            g.DrawPath(borderPen, borderPath);

            // "AHV" — Calibri Gras +30%
            float ahvY = 5f;
            using var fAhv = new Font("Calibri", 75f, FontStyle.Bold, GraphicsUnit.Point);
            string ahv = "AHV";
            // MeasureString surestime la hauteur — on utilise GetHeight pour la hauteur réelle
            float ahvW    = g.MeasureString(ahv, fAhv).Width;
            float ahvH    = fAhv.GetHeight(g);   // hauteur de ligne réelle, sans padding GDI+
            // Ombre
            using (var sh = new SolidBrush(AhvShadow))
                g.DrawString(ahv, fAhv, sh, rc.Width/2f - ahvW/2f + 2, ahvY + 2);
            // Texte dégradé
            using (var tBr = new LinearGradientBrush(
                new PointF(0, ahvY), new PointF(0, ahvY + ahvH),
                AhvTop, AhvBottom))
                g.DrawString(ahv, fAhv, tBr, rc.Width/2f - ahvW/2f, ahvY);

            // "AudioHexViewer" — Century Gothic 17pt, collé juste sous AHV
            using var fSub = new Font("Century Gothic", 17f, FontStyle.Regular, GraphicsUnit.Point);
            using var bSub = new SolidBrush(SubCol);
            float titleW  = g.MeasureString(AppTitle, fSub).Width;
            float titleH  = fSub.GetHeight(g);
            float titleY  = ahvY + ahvH - 28f;   // quasi collé
            g.DrawString(AppTitle, fSub, bSub, rc.Width/2f - titleW/2f, titleY);

            // Ligne — entre AudioHexViewer et OnE Audio Projects
            float sepY = titleY + titleH + 4f;
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
            float authorY = sepY + 5f;
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

            // Barre de progression
            float barY = authorY + authorH + 8f;

            // Version — sous Loading, alignée à droite
            using var fVn = new Font("Calibri", 9f, FontStyle.Regular, GraphicsUnit.Point);
            using var bVn = new SolidBrush(SubCol);
            float vw = g.MeasureString(AppVersion, fVn).Width;
            // Version dessinée dans le bloc if (!_isAboutMode) sous Loading
            // (en mode About, affichée séparément ci-dessous)
            if (_isAboutMode)
            {
                g.DrawString(AppVersion, fVn, bVn, rc.Width - vw - 6f, rc.Height - 18f);
                string copy = $"© {DateTime.Now.Year} OnE Audio Projects";
                using var bC = new SolidBrush(SubCol);
                g.DrawString(copy, fVn, bC, 6f, rc.Height - 18f);
            }

            if (!_isAboutMode)
            {
                float bx = 40f, by = barY, bw = rc.Width - 80f, bh = 4f;
                // Fond
                using var bgP = RoundRect(bx, by, bw, bh, 2f);
                using var bgBr = new SolidBrush(Color.FromArgb(45, 100, 130, 180));
                g.FillPath(bgBr, bgP);
                // Remplissage
                float filled = bw * _progress;
                if (filled > bh)
                {
                    using var fP = RoundRect(bx, by, filled, bh, 2f);
                    using var fBr = new LinearGradientBrush(
                        new PointF(bx, by), new PointF(bx + filled, by),
                        Color.FromArgb(0, 100, 200), AccentCol);
                    g.FillPath(fBr, fP);
                    // Glow
                    float gx2 = bx + _glowPos * filled;
                    if (gx2 > bx && gx2 < bx + filled)
                    {
                        using var gP = MakeCircle((int)gx2, (int)(by + bh/2), 12);
                        using var gBr = new PathGradientBrush(gP)
                        {
                            CenterColor    = Color.FromArgb(100, 180, 230, 255),
                            SurroundColors = new[] { Color.Transparent }
                        };
                        g.FillPath(gBr, gP);
                    }
                }
                // Message de statut — centré sous la barre
                using var fL = new Font("Calibri", 9f, FontStyle.Regular, GraphicsUnit.Point);
                using var bL = new SolidBrush(SubCol);
                float lw2 = g.MeasureString(_statusMessage, fL).Width;
                float loadingY = by + bh + 5f;
                g.DrawString(_statusMessage, fL, bL, rc.Width/2f - lw2/2f, loadingY);
                // Version sous le message, alignée à droite
                // Version et copyright calés en bas comme en mode About
                g.DrawString(AppVersion, fVn, bVn, rc.Width - vw - 6f, rc.Height - 18f);
                string copy = $"© {DateTime.Now.Year} OnE Audio Projects";
                using var bC = new SolidBrush(SubCol);
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

        protected override void Dispose(bool disposing)
        { if (disposing) _timer?.Dispose(); base.Dispose(disposing); }
    }
}
