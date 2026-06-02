using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AudioHexViewer
{
    internal sealed class SplashScreen : Form
    {
        // ── Infos produit ─────────────────────────────────────────────────────
        public const string AppVersion = "V0.9";
        public const string AppDate    = "01/2026";
        public const string AppTitle   = "AudioHexViewer";
        public const string AppSubLine = "Digital audio data Analyzer";
        public const string AppAuthor  = "OnE Audio Projects";

        // ── Couleurs ──────────────────────────────────────────────────────────
        private static readonly Color BgTop    = Color.FromArgb(0,   0,  80);
        private static readonly Color BgBottom = Color.FromArgb(0,   0,  40);
        private static readonly Color AccentCol= Color.FromArgb(0, 180, 255);
        private static readonly Color SubCol   = Color.FromArgb(180, 200, 230);
        private static readonly Color DimCol   = Color.FromArgb(80,  110, 160);

        // ── Mode ─────────────────────────────────────────────────────────────
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

            FormBorderStyle = aboutMode ? FormBorderStyle.FixedDialog : FormBorderStyle.None;
            StartPosition   = FormStartPosition.CenterScreen;
            // ~30% plus petit : 480×300 → 340×212
            ClientSize      = new Size(340, aboutMode ? 230 : 212);
            BackColor       = Color.FromArgb(0, 0, 64);
            ShowInTaskbar   = false;
            TopMost         = !aboutMode;
            DoubleBuffered  = true;
            Text            = aboutMode ? "About" : "";
            MaximizeBox     = false;
            MinimizeBox     = false;

            if (!aboutMode)
            {
                // Coins arrondis (splash seulement)
                int rr = 14;
                var path = new GraphicsPath();
                path.AddArc(0, 0, rr*2, rr*2, 180, 90);
                path.AddArc(Width-rr*2, 0, rr*2, rr*2, 270, 90);
                path.AddArc(Width-rr*2, Height-rr*2, rr*2, rr*2, 0, 90);
                path.AddArc(0, Height-rr*2, rr*2, rr*2, 90, 90);
                path.CloseFigure();
                Region = new Region(path);
            }
            else
            {
                // Mode About : bouton OK
                var btn = new Button
                {
                    Text      = "OK",
                    DialogResult = DialogResult.OK,
                    Size      = new Size(80, 26),
                    Left      = (ClientSize.Width - 80) / 2,
                    Top       = ClientSize.Height - 38,
                    FlatStyle = FlatStyle.Flat
                };
                btn.FlatAppearance.BorderColor = AccentCol;
                btn.ForeColor = SubCol;
                btn.BackColor = Color.FromArgb(0, 0, 64);
                Controls.Add(btn);
                AcceptButton = btn;
            }

            Paint += OnPaint;

            if (!aboutMode)
            {
                _timer = new System.Windows.Forms.Timer { Interval = 30 };
                _timer.Tick += (_, _) =>
                {
                    if (_progress < 0.85f)
                        _progress = Math.Min(0.85f, _progress + 0.016f);
                    _glowPos = (_glowPos + 0.022f) % 1f;
                    _dots    = (_dots + 1) % (4 * 8);
                    Invalidate();
                    if (_readyToClose && _progress >= 0.85f)
                    {
                        _progress = 1f;
                        Invalidate();
                        _timer!.Stop();
                        var t = new System.Windows.Forms.Timer { Interval = 200 };
                        t.Tick += (_, _) => { t.Stop(); t.Dispose(); Close(); };
                        t.Start();
                    }
                };
                _timer.Start();
            }
        }

        public void SignalReady() => _readyToClose = true;

        // Listes mises en cache pendant le scan — réutilisées à chaque ouverture de Devices
        public static List<NAudio.CoreAudioApi.MMDevice> CachedInputs  { get; } = new();
        public static List<NAudio.CoreAudioApi.MMDevice> CachedOutputs { get; } = new();
        // Noms pré-cachés (FriendlyName déclenche un appel COM lent à chaque accès)
        public static Dictionary<string, string> CachedNames { get; } = new();

        /// <summary>Lance le scan audio pendant l'affichage du splash.</summary>
        public void RunAudioScan(int[] srOptions)
        {
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

            int total = all.Count * srOptions.Length;
            int done  = 0;

            foreach (var device in all)
            {
                // Pré-cache FriendlyName
                if (!CachedNames.ContainsKey(device.ID))
                    CachedNames[device.ID] = device.FriendlyName;

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
                        if (total > 0) _progress = Math.Min(0.95f, (float)done / total);
                        Application.DoEvents();
                    }
                }
            }
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

            // Fond dégradé
            using (var br = new LinearGradientBrush(rc, BgTop, BgBottom, 90f))
                g.FillRectangle(br, rc);

            // Halo central
            int cx = rc.Width / 2, cy = rc.Height / 2 - 14;
            using (var hPath = MakeCircle(cx, cy, 140))
            using (var hBr = new PathGradientBrush(hPath))
            {
                hBr.CenterColor    = Color.FromArgb(25, 0, 60, 140);
                hBr.SurroundColors = new[] { Color.Transparent };
                g.FillPath(hBr, hPath);
            }

            // "AHV" grand titre
            float ahvY = 22f;
            using var fAhv = new Font("Segoe UI", 62f, FontStyle.Bold, GraphicsUnit.Point);
            string ahv = "AHV";
            float ahvW = g.MeasureString(ahv, fAhv).Width;
            // Ombre
            using (var sh = new SolidBrush(Color.FromArgb(55, 0, 0, 0)))
                g.DrawString(ahv, fAhv, sh, rc.Width/2f - ahvW/2f + 2, ahvY + 2);
            // Texte dégradé
            var tRect = new RectangleF(rc.Width/2f - 160, ahvY, 320, 80);
            using (var tBr = new LinearGradientBrush(
                new PointF(0, ahvY), new PointF(0, ahvY + 70),
                Color.FromArgb(190, 215, 255), Color.White))
                g.DrawString(ahv, fAhv, tBr, rc.Width/2f - ahvW/2f, ahvY);

            // Ligne de séparation
            float sepY = 108f;
            using (var lBr = new LinearGradientBrush(
                new PointF(40, sepY), new PointF(rc.Width - 40, sepY),
                Color.Transparent, Color.Transparent))
            {
                lBr.InterpolationColors = new ColorBlend
                {
                    Colors    = new[] { Color.Transparent, AccentCol, Color.White, AccentCol, Color.Transparent },
                    Positions = new[] { 0f, 0.2f, 0.5f, 0.8f, 1f }
                };
                using var pen = new Pen(lBr, 1.2f);
                g.DrawLine(pen, 40, sepY, rc.Width - 40, sepY);
            }

            // Titre + version
            using var fSub = new Font("Segoe UI Light", 13f, FontStyle.Regular, GraphicsUnit.Point);
            using var bSub = new SolidBrush(SubCol);
            string titleVer = $"{AppTitle}  {AppVersion}  {AppDate}";
            float tw = g.MeasureString(titleVer, fSub).Width;
            g.DrawString(titleVer, fSub, bSub, rc.Width/2f - tw/2f, 114f);

            // Sous-titres
            using var fVer = new Font("Segoe UI", 8f, FontStyle.Regular, GraphicsUnit.Point);
            using var bVer = new SolidBrush(DimCol);
            foreach (var (txt, yOff) in new[] { (AppSubLine, 135f), (AppAuthor, 150f) })
            {
                float ww = g.MeasureString(txt, fVer).Width;
                g.DrawString(txt, fVer, bVer, rc.Width/2f - ww/2f, yOff);
            }

            // Barre de chargement (splash seulement)
            if (!_isAboutMode)
            {
                float bx = 40f, by = 170f, bw = rc.Width - 80f, bh = 4f;
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
                // "Loading..."
                int dc = (_dots / 8) % 4;
                string lt = "Loading" + new string('.', dc);
                using var fL = new Font("Segoe UI", 7.5f, FontStyle.Regular, GraphicsUnit.Point);
                using var bL = new SolidBrush(DimCol);
                float lw2 = g.MeasureString(lt, fL).Width;
                g.DrawString(lt, fL, bL, rc.Width/2f - lw2/2f, 180f);
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
