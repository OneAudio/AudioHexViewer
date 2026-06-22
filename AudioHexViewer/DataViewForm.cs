using System;
using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    public partial class DataViewForm : Form
    {
        // ── État RUN/STOP ─────────────────────────────────────────────────────
        private bool _isRunning = false;

        /// <summary>Appelé par Form1 quand RUN/STOP change dans DataView.</summary>
        public event EventHandler<bool>? RunStopChanged;

        // ── VU-mètre (constantes identiques à Form1.PaintVuBarGdi) ───────────
        private const int    VuLedCount  = 40;
        private const double VuDbMin     = -80.0;
        private const double VuBlueEnd   = -36.0;
        private const double VuGreenEnd  = -12.0;
        private const double VuOrangeEnd = -6.0;

        private int    _vuPctL, _vuPctR;
        private string _vuStrL = "-∞ dBFS", _vuStrR = "-∞ dBFS";

        // ── Constructeur ─────────────────────────────────────────────────────
        public DataViewForm()
        {
            InitializeComponent();

            // Close (= hide) on Escape
            KeyPreview = true;
            KeyDown   += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };

            // VU-mètre : double-buffering + Paint
            typeof(Control)
                .GetMethod("SetStyle",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)!
                .Invoke(pnlVu, new object[] {
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.DoubleBuffer, true });
            pnlVu.Paint += PnlVu_Paint;

            btnRunStop.Click += BtnRunStop_Click;

            FormClosing += (_, e) => { e.Cancel = true; Hide(); };
        }

        // ── API publique : mise à jour du VU depuis Form1 ─────────────────────
        public void SetVuLevels(double dbL, double dbR)
        {
            _vuPctL = DbToPct(dbL); _vuStrL = FmtDb(dbL);
            _vuPctR = DbToPct(dbR); _vuStrR = FmtDb(dbR);
            if (pnlVu.IsHandleCreated) pnlVu.Invalidate();
        }

        public void ResetVu()
        {
            _vuPctL = _vuPctR = 0;
            _vuStrL = _vuStrR = "-∞ dBFS";
            if (pnlVu.IsHandleCreated) pnlVu.Invalidate();
        }

        // ── RUN / STOP ────────────────────────────────────────────────────────
        private void BtnRunStop_Click(object? sender, EventArgs e)
        {
            _isRunning = !_isRunning;
            btnRunStop.Text      = _isRunning ? "■  STOP" : "▶  RUN";
            btnRunStop.BackColor = _isRunning ? Color.Firebrick : Color.SeaGreen;
            RunStopChanged?.Invoke(this, _isRunning);
            if (!_isRunning) ResetVu();
        }

        // ── Mise à jour des valeurs + police adaptée au mode ─────────────────
        public void UpdateValues(string leftVal, string rightVal, string mode)
        {
            if (!_isRunning) return;

            lblLeftMainTitle.Text  = mode;
            lblRightMainTitle.Text = mode;

            if (mode == "Binary (24b)")
            {
                // Passe en mode 4 lignes (MSB bits / MSB hex / LSB bits / LSB hex)
                var gL = leftVal.Split(' ');
                var gR = rightVal.Split(' ');

                lblLeftMainValue.Visible  = false;
                lblRightMainValue.Visible = false;

                if (gL.Length == 6)
                {
                    var f = new Font("Courier New", 9f, System.Drawing.FontStyle.Bold);
                    if (!lblLeftMsbBits.Font.Equals(f))
                    {
                        foreach (var l in new[]{lblLeftMsbBits,lblLeftMsbHex,lblLeftLsbBits,lblLeftLsbHex,
                                                lblRightMsbBits,lblRightMsbHex,lblRightLsbBits,lblRightLsbHex})
                            l.Font = f;
                    }
                    else f.Dispose();

                    lblLeftMsbBits.Text = "MSB  " + gL[0] + "." + gL[1] + "." + gL[2];
                    lblLeftMsbHex.Text  = HexLine(gL[0], gL[1], gL[2]);
                    lblLeftLsbBits.Text = "LSB  " + gL[3] + "." + gL[4] + "." + gL[5];
                    lblLeftLsbHex.Text  = HexLine(gL[3], gL[4], gL[5]);
                }
                if (gR.Length == 6)
                {
                    lblRightMsbBits.Text = "MSB  " + gR[0] + "." + gR[1] + "." + gR[2];
                    lblRightMsbHex.Text  = HexLine(gR[0], gR[1], gR[2]);
                    lblRightLsbBits.Text = "LSB  " + gR[3] + "." + gR[4] + "." + gR[5];
                    lblRightLsbHex.Text  = HexLine(gR[3], gR[4], gR[5]);
                }

                lblLeftMsbBits.Visible  = true;  lblLeftMsbHex.Visible   = true;
                lblLeftLsbBits.Visible  = true;  lblLeftLsbHex.Visible   = true;
                lblRightMsbBits.Visible = true;  lblRightMsbHex.Visible  = true;
                lblRightLsbBits.Visible = true;  lblRightLsbHex.Visible  = true;
            }
            else
            {
                var f = new Font("Segoe UI", 14f, System.Drawing.FontStyle.Bold);
                if (!lblLeftMainValue.Font.Equals(f))
                    { lblLeftMainValue.Font = f; lblRightMainValue.Font = f; }
                else f.Dispose();

                lblLeftMainValue.Text    = leftVal;
                lblRightMainValue.Text   = rightVal;
                lblLeftMainValue.Visible  = true;
                lblRightMainValue.Visible = true;

                lblLeftMsbBits.Visible  = false; lblLeftMsbHex.Visible   = false;
                lblLeftLsbBits.Visible  = false; lblLeftLsbHex.Visible   = false;
                lblRightMsbBits.Visible = false; lblRightMsbHex.Visible  = false;
                lblRightLsbBits.Visible = false; lblRightLsbHex.Visible  = false;
            }
        }

        // "MSB  xxxx.xxxx.xxxx" → hex line "      A    F    1"
        // (6 leading spaces → hex digit centred under each 4-bit group)
        private static string HexLine(string g0, string g1, string g2)
            => $"      {Nibble(g0)}    {Nibble(g1)}    {Nibble(g2)}";

        private static char Nibble(string g)
        {
            bool allX = true; int val = 0;
            foreach (char c in g)
            {
                val <<= 1;
                if (c == '1') { val |= 1; allX = false; }
                else if (c == '0') allX = false;
            }
            return allX ? '-' : "0123456789ABCDEF"[val];
        }

        private static (string bits, string hex) FormatBinaryWithHex(string rawBin)
        {
            if (string.IsNullOrWhiteSpace(rawBin)) return ("—", "");
            var groups = rawBin.Split(' ');
            if (groups.Length != 6) return (rawBin, "");

            string bits = string.Join(".", groups);

            // Hex ligne : chaque nibble centré sous son groupe de 4 bits
            // "  X    X    X    X    X    X" (pos 2,7,12,17,22,27)
            var sb = new System.Text.StringBuilder("  ");
            for (int i = 0; i < 6; i++)
            {
                string g = groups[i];
                bool allX = true;
                int val = 0;
                foreach (char ch in g)
                {
                    val <<= 1;
                    if (ch == '1') { val |= 1; allX = false; }
                    else if (ch == '0') allX = false;
                }
                sb.Append(allX ? '·' : "0123456789ABCDEF"[val]);
                if (i < 5) sb.Append("    ");
            }
            return (bits, sb.ToString());
        }

        public void UpdateDbfs(string leftDbfs, Color leftColor,
                               string rightDbfs, Color rightColor)
        {
            if (!_isRunning) return;
            lblLeftLevelDV.Text       = leftDbfs;
            lblLeftLevelDV.ForeColor  = leftColor;
            lblRightLevelDV.Text      = rightDbfs;
            lblRightLevelDV.ForeColor = rightColor;
        }

        public bool IsRunning => _isRunning;

        // ── Rendu VU-mètre ────────────────────────────────────────────────────
        private void PnlVu_Paint(object? sender, PaintEventArgs e)
        {
            var g  = e.Graphics;
            var sz = pnlVu.ClientSize;
            g.Clear(Color.Black);

            PaintBar(g, new Rectangle(22, 4,  sz.Width - 116, 16), _vuPctL);
            PaintBar(g, new Rectangle(22, 28, sz.Width - 116, 16), _vuPctR);

            using var fS = new Font("Segoe UI", 7f);
            TextRenderer.DrawText(g, "L", fS, new Rectangle(4, 4,  16, 16),
                Color.FromArgb(160, 160, 180), TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, "R", fS, new Rectangle(4, 28, 16, 16),
                Color.FromArgb(160, 160, 180), TextFormatFlags.VerticalCenter);

            using var fL = new Font("Segoe UI", 7.5f);
            int lx = sz.Width - 112;
            TextRenderer.DrawText(g, _vuStrL, fL, new Rectangle(lx, 4,  108, 16),
                Color.Lime, TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
            TextRenderer.DrawText(g, _vuStrR, fL, new Rectangle(lx, 28, 108, 16),
                Color.Lime, TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
        }

        private static void PaintBar(Graphics g, Rectangle r, int pct)
        {
            if (r.Width <= 4 || r.Height <= 4) return;
            int vm = Math.Max(1, r.Height / 6);
            int lh = r.Height - 2 * vm;
            double pB = (VuBlueEnd   - VuDbMin) / -VuDbMin * 100.0;
            double pG = (VuGreenEnd  - VuDbMin) / -VuDbMin * 100.0;
            double pO = (VuOrangeEnd - VuDbMin) / -VuDbMin * 100.0;
            for (int i = 0; i < VuLedCount; i++)
            {
                int x0 = r.X + (int)((double)i       / VuLedCount * r.Width);
                int x1 = r.X + (int)((double)(i + 1) / VuLedCount * r.Width);
                int w  = Math.Max(1, x1 - x0 - 2);
                double cp = (i + 0.5) / VuLedCount * 100.0;
                Color col = cp > pct     ? Color.FromArgb(30, 30, 30)
                          : cp <= pB    ? Color.FromArgb(0, 120, 215)
                          : cp <= pG   ? Color.Lime
                          : cp <= pO  ? Color.Gold
                                      : Color.Red;
                using var br = new SolidBrush(col);
                g.FillRectangle(br, x0, r.Y + vm, w, lh);
            }
        }

        private static int    DbToPct(double db) =>
            db <= VuDbMin ? 0 : db >= 0.0 ? 100 : (int)((db - VuDbMin) / -VuDbMin * 100.0);
        private static string FmtDb(double db)   =>
            double.IsNegativeInfinity(db) ? "-∞ dBFS" : $"{db:F1} dBFS";

        // ── Héritage de l'ancien AttachControls (garde compat si appelé) ──────
        public void AttachControls(params Control[] controls) { }
    }
}
