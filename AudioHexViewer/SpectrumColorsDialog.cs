using System;
using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    public partial class SpectrumColorsDialog : Form
    {
        private SpectrumColorSettings _colors;

        public SpectrumColorSettings SelectedColors => Clone(_colors);

        public SpectrumColorsDialog(SpectrumColorSettings initial)
        {
            InitializeComponent();

            _colors = Clone(initial ?? SpectrumColorSettings.Defaults());
            _colors.Normalize();

            // Click handlers (color swatches)
            btnFftL.Click += (_, __) => Pick(() => _colors.FftLeftArgb, v => _colors.FftLeftArgb = v, btnFftL);
            btnFftR.Click += (_, __) => Pick(() => _colors.FftRightArgb, v => _colors.FftRightArgb = v, btnFftR);

            btnPkL.Click += (_, __) => Pick(() => _colors.PeakHoldLeftArgb, v => _colors.PeakHoldLeftArgb = v, btnPkL);
            btnPkR.Click += (_, __) => Pick(() => _colors.PeakHoldRightArgb, v => _colors.PeakHoldRightArgb = v, btnPkR);

            btnOv1.Click += (_, __) => PickOverlay(0, btnOv1);
            btnOv2.Click += (_, __) => PickOverlay(1, btnOv2);
            btnOv3.Click += (_, __) => PickOverlay(2, btnOv3);
            btnOv4.Click += (_, __) => PickOverlay(3, btnOv4);
            btnOv5.Click += (_, __) => PickOverlay(4, btnOv5);
            btnOv6.Click += (_, __) => PickOverlay(5, btnOv6);
            btnOv7.Click += (_, __) => PickOverlay(6, btnOv7);
            btnOv8.Click += (_, __) => PickOverlay(7, btnOv8);

            btnGridMaj.Click += (_, __) => Pick(() => _colors.GridArgb, v => _colors.GridArgb = v, btnGridMaj);
            btnGridMin.Click += (_, __) => Pick(() => _colors.GridMinorArgb, v => _colors.GridMinorArgb = v, btnGridMin);
            btnBack.Click += (_, __) => Pick(() => _colors.BackgroundArgb, v => _colors.BackgroundArgb = v, btnBack);
            btnLabMaj.Click += (_, __) => Pick(() => _colors.LabelMajorArgb, v => _colors.LabelMajorArgb = v, btnLabMaj);
            btnLabMin.Click += (_, __) => Pick(() => _colors.LabelMinorArgb, v => _colors.LabelMinorArgb = v, btnLabMin);

            // Thickness
            nudFftWidth.Value = ClampToRange(_colors.FftWidth);
            nudPkWidth.Value = ClampToRange(_colors.PeakHoldWidth);
            nudOvWidth.Value = ClampToRange(_colors.OverlayWidth);

            nudFftWidth.ValueChanged += (_, __) => _colors.FftWidth = (int)nudFftWidth.Value;
            nudPkWidth.ValueChanged += (_, __) => _colors.PeakHoldWidth = (int)nudPkWidth.Value;
            nudOvWidth.ValueChanged += (_, __) => _colors.OverlayWidth = (int)nudOvWidth.Value;

            // Default
            btnDefault.Click += (_, __) =>
            {
                _colors = SpectrumColorSettings.Defaults();
                UpdateUiFromSettings();
            };

            UpdateUiFromSettings();
        }

        private static decimal ClampToRange(int v)
        {
            if (v < 1) v = 1;
            if (v > 3) v = 3;
            return v;
        }

        private void PickOverlay(int idx, Button swatch)
        {
            _colors.Normalize();
            int[] arr = _colors.OverlaySlotArgb;

            int current = (idx >= 0 && idx < arr.Length) ? arr[idx] : _colors.OverlayArgb;
            using var cd = new ColorDialog { Color = Color.FromArgb(current), FullOpen = true };
            if (cd.ShowDialog(this) == DialogResult.OK)
            {
                if (idx >= 0 && idx < arr.Length)
                    arr[idx] = cd.Color.ToArgb();
                swatch.BackColor = cd.Color;
            }
        }

        private static void Pick(Func<int> getArgb, Action<int> setArgb, Button swatch)
        {
            using var cd = new ColorDialog { Color = Color.FromArgb(getArgb()), FullOpen = true };
            if (cd.ShowDialog() == DialogResult.OK)
            {
                setArgb(cd.Color.ToArgb());
                swatch.BackColor = cd.Color;
            }
        }

        private void UpdateUiFromSettings()
        {
            _colors.Normalize();

            btnFftL.BackColor = Color.FromArgb(_colors.FftLeftArgb);
            btnFftR.BackColor = Color.FromArgb(_colors.FftRightArgb);

            btnPkL.BackColor = Color.FromArgb(_colors.PeakHoldLeftArgb);
            btnPkR.BackColor = Color.FromArgb(_colors.PeakHoldRightArgb);

            btnGridMaj.BackColor = Color.FromArgb(_colors.GridArgb);
            btnGridMin.BackColor = Color.FromArgb(_colors.GridMinorArgb);
            btnBack.BackColor = Color.FromArgb(_colors.BackgroundArgb);
            btnLabMaj.BackColor = Color.FromArgb(_colors.LabelMajorArgb);
            btnLabMin.BackColor = Color.FromArgb(_colors.LabelMinorArgb);

            var ov = _colors.OverlaySlotArgb;
            btnOv1.BackColor = Color.FromArgb(ov[0]);
            btnOv2.BackColor = Color.FromArgb(ov[1]);
            btnOv3.BackColor = Color.FromArgb(ov[2]);
            btnOv4.BackColor = Color.FromArgb(ov[3]);
            btnOv5.BackColor = Color.FromArgb(ov[4]);
            btnOv6.BackColor = Color.FromArgb(ov[5]);
            btnOv7.BackColor = Color.FromArgb(ov[6]);
            btnOv8.BackColor = Color.FromArgb(ov[7]);

            nudFftWidth.Value = ClampToRange(_colors.FftWidth);
            nudPkWidth.Value = ClampToRange(_colors.PeakHoldWidth);
            nudOvWidth.Value = ClampToRange(_colors.OverlayWidth);
        }


        // --- UI helpers (called from Designer) ---
        private static void SetupColorButton(Button b)
        {
            b.Size = new Size(18, 18);
            b.Margin = new Padding(3, 6, 3, 6);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;
            b.Text = "";
            b.TabStop = false;
            b.Cursor = Cursors.Hand;
            b.UseVisualStyleBackColor = false;
        }

        private static void SetupWidthNud(NumericUpDown nud)
        {
            nud.Minimum = 1;
            nud.Maximum = 3;
            nud.Value = 2;
            nud.DecimalPlaces = 0;
            nud.Increment = 1;
            nud.Size = new Size(44, 22);
            // Keep vertical rhythm aligned with swatches
            nud.Margin = new Padding(3, 6, 3, 6);
            nud.TextAlign = HorizontalAlignment.Center;
            nud.Anchor = AnchorStyles.Left;
        }

        private static Label MakeLabel(string text)
        {
            var l = new Label();
            l.Text = text;
            l.Dock = DockStyle.Fill;

            // Align text close to the color square (requested UI alignment)
            l.TextAlign = ContentAlignment.MiddleRight;
            l.Padding = new Padding(0, 0, 6, 0);

            return l;
        }

        private static Button MakeColorButton()
        {
            var b = new Button();
            SetupColorButton(b);
            return b;
        }

        private static SpectrumColorSettings Clone(SpectrumColorSettings src)
        {
            // clone profond (array overlay)
            var dst = new SpectrumColorSettings
            {
                FftLeftArgb = src.FftLeftArgb,
                FftRightArgb = src.FftRightArgb,
                FftWidth = src.FftWidth,

                PeakHoldLeftArgb = src.PeakHoldLeftArgb,
                PeakHoldRightArgb = src.PeakHoldRightArgb,
                PeakHoldWidth = src.PeakHoldWidth,

                OverlayArgb = src.OverlayArgb,
                OverlayWidth = src.OverlayWidth,

                GridArgb = src.GridArgb,
                GridMinorArgb = src.GridMinorArgb,
                BackgroundArgb = src.BackgroundArgb,

                LabelMajorArgb = src.LabelMajorArgb,
                LabelMinorArgb = src.LabelMinorArgb
            };

            if (src.OverlaySlotArgb != null)
            {
                dst.OverlaySlotArgb = new int[src.OverlaySlotArgb.Length];
                Array.Copy(src.OverlaySlotArgb, dst.OverlaySlotArgb, src.OverlaySlotArgb.Length);
            }
            else
            {
                dst.OverlaySlotArgb = SpectrumColorSettings.Defaults().OverlaySlotArgb;
            }

            dst.Normalize();
            return dst;
        }
    }
}