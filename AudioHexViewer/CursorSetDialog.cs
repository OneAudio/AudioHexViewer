// ═══════════════════════════════════════════════════════════════════════════
// CursorSetDialog.cs  —  Cursor position entry (Set X / Set Y)
// Style AHV compact — matches THD Settings / other project dialogs.
// ═══════════════════════════════════════════════════════════════════════════
#nullable enable
using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace AudioHexViewer
{
    internal sealed class CursorSetDialog : Form
    {
        public double Value1 { get; private set; }
        public double Value2 { get; private set; }

        private readonly TextBox     _tbV1, _tbV2, _tbDelta;
        private readonly RadioButton _rbFix1, _rbFix2;
        private bool   _updating;

        public CursorSetDialog(bool isX, double v1, double v2, string unit)
        {
            string lbl1 = isX ? "X1"           : "Y1";
            string lbl2 = isX ? "X2"           : "Y2";
            string lblD = isX ? "X2 \u2212 X1" : "Y2 \u2212 Y1";

            // ── Form ─────────────────────────────────────────────────────
            Text            = isX ? "Set X Cursors" : "Set Y Cursors";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false; MinimizeBox = false;
            StartPosition   = FormStartPosition.CenterParent;
            Font            = new Font("Segoe UI", 9f);
            BackColor       = SystemColors.Control;
            AutoScaleMode   = AutoScaleMode.Font;
            AutoScaleDimensions = new SizeF(7f, 15f);

            var gS   = new Font("Segoe UI", 7.5f);
            var bold = new Font("Segoe UI", 9f, FontStyle.Bold);
            var blue = Color.FromArgb(0, 102, 204);

            // ── Section header ────────────────────────────────────────────
            int y = 8;
            var lblSection = new Label
            { Text = isX ? "CURSOR X POSITIONS" : "CURSOR Y POSITIONS",
              Font = bold, ForeColor = blue,
              AutoSize = true, Location = new Point(12, y) };
            y += 20;

            // Separator
            var sep1 = new Label
            { BackColor = Color.FromArgb(200,200,200),
              Location = new Point(12, y), Size = new Size(230, 1) };
            y += 6;

            // ── Row builder ───────────────────────────────────────────────
            const int LX = 12, TX = 78, TW = 100, UX = 182;

            Label MkLbl(string t, int yy) => new Label
            { Text=t, Font=Font, AutoSize=true, Location=new Point(LX, yy+3) };
            TextBox MkBox(double val, int yy) => new TextBox
            { Text=Fmt(val), Font=Font, Location=new Point(TX, yy),
              Size=new Size(TW, 23), TextAlign=HorizontalAlignment.Right,
              BorderStyle=BorderStyle.FixedSingle };
            Label MkUnit(string t, int yy) => new Label
            { Text=t, Font=Font, AutoSize=true, Location=new Point(UX, yy+3),
              ForeColor=SystemColors.GrayText };

            var l1=MkLbl(lbl1, y); _tbV1=MkBox(v1, y); var u1=MkUnit(unit, y); y+=28;
            var l2=MkLbl(lbl2, y); _tbV2=MkBox(v2, y); var u2=MkUnit(unit, y); y+=28;

            // Second separator before delta
            var sep2 = new Label
            { BackColor = Color.FromArgb(200,200,200),
              Location = new Point(12, y), Size = new Size(230, 1) };
            y += 6;

            var lD=MkLbl(lblD, y); _tbDelta=MkBox(v2-v1, y); var uD=MkUnit(unit, y); y+=30;

            // ── Reference row ─────────────────────────────────────────────
            var lblRef = new Label
            { Text="Reference:", Font=gS, AutoSize=true,
              Location=new Point(12, y+3), ForeColor=SystemColors.GrayText };
            _rbFix1 = new RadioButton
            { Text=lbl1+" fixed", Font=Font, Checked=true,
              AutoSize=true, Location=new Point(78, y) };
            _rbFix2 = new RadioButton
            { Text=lbl2+" fixed", Font=Font,
              AutoSize=true, Location=new Point(158, y) };
            y += 26;

            // ── Buttons ───────────────────────────────────────────────────
            var btnOK = new Button
            { Text="OK", DialogResult=DialogResult.OK, Font=bold,
              Location=new Point(82, y), Size=new Size(72, 26),
              BackColor=Color.FromArgb(0,102,204), ForeColor=Color.White,
              FlatStyle=FlatStyle.Flat, UseVisualStyleBackColor=false };
            btnOK.FlatAppearance.BorderSize = 0;
            var btnCancel = new Button
            { Text="Cancel", DialogResult=DialogResult.Cancel, Font=Font,
              Location=new Point(162, y), Size=new Size(72, 26) };

            ClientSize = new Size(254, y + 42);

            Controls.AddRange(new Control[]
            { lblSection, sep1, l1, _tbV1, u1, l2, _tbV2, u2,
              sep2, lD, _tbDelta, uD, lblRef, _rbFix1, _rbFix2,
              btnOK, btnCancel });

            AcceptButton = btnOK;
            CancelButton = btnCancel;

            // ── Events ────────────────────────────────────────────────────
            _tbV1.TextChanged    += (_,_) => OnV1Changed();
            _tbV2.TextChanged    += (_,_) => OnV2Changed();
            _tbDelta.TextChanged += (_,_) => OnDeltaChanged();

            btnOK.Click += (_,_) =>
            {
                if (!TryParse(_tbV1.Text, out double r1) ||
                    !TryParse(_tbV2.Text, out double r2))
                { MessageBox.Show("Invalid value.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                  DialogResult = DialogResult.None; return; }
                Value1 = r1; Value2 = r2;
            };
        }

        private void OnV1Changed()
        { if (_updating) return;
          if (!TryParse(_tbV1.Text, out double a)) return;
          if (!TryParse(_tbV2.Text, out double b)) return;
          _updating=true; _tbDelta.Text=Fmt(b-a); _updating=false; }

        private void OnV2Changed()
        { if (_updating) return;
          if (!TryParse(_tbV1.Text, out double a)) return;
          if (!TryParse(_tbV2.Text, out double b)) return;
          _updating=true; _tbDelta.Text=Fmt(b-a); _updating=false; }

        private void OnDeltaChanged()
        { if (_updating) return;
          if (!TryParse(_tbDelta.Text, out double d)) return;
          _updating=true;
          if (_rbFix1.Checked)
          { if (TryParse(_tbV1.Text, out double a)) _tbV2.Text=Fmt(a+d); }
          else
          { if (TryParse(_tbV2.Text, out double b)) _tbV1.Text=Fmt(b-d); }
          _updating=false; }

        private static string Fmt(double v) =>
            v.ToString("G6", CultureInfo.CurrentCulture);
        private static bool TryParse(string s, out double v) =>
            double.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out v) ||
            double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out v);
    }
}
#nullable restore
