using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

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
            UpdateSwatches();

            // Wire clicks (buttons + swatches)
            btnPickFftL.Click += (_, __) => Pick(() => _colors.FftLeftArgb, v => _colors.FftLeftArgb = v, pbFftL);
            btnPickFftR.Click += (_, __) => Pick(() => _colors.FftRightArgb, v => _colors.FftRightArgb = v, pbFftR);

            btnPickOverlay.Click += (_, __) => Pick(() => _colors.OverlayArgb, v => _colors.OverlayArgb = v, pbOverlay);
            btnPickPeakL.Click += (_, __) => Pick(() => _colors.PeakHoldLeftArgb, v => _colors.PeakHoldLeftArgb = v, pbPeakL);
            btnPickPeakR.Click += (_, __) => Pick(() => _colors.PeakHoldRightArgb, v => _colors.PeakHoldRightArgb = v, pbPeakR);

            btnPickGrid.Click += (_, __) => Pick(() => _colors.GridArgb, v => _colors.GridArgb = v, pbGrid);
            btnPickBack.Click += (_, __) => Pick(() => _colors.BackgroundArgb, v => _colors.BackgroundArgb = v, pbBack);

            pbFftL.Click += (_, __) => btnPickFftL.PerformClick();
            pbFftR.Click += (_, __) => btnPickFftR.PerformClick();
            pbOverlay.Click += (_, __) => btnPickOverlay.PerformClick();
            pbPeakL.Click += (_, __) => btnPickPeakL.PerformClick();
            pbPeakR.Click += (_, __) => btnPickPeakR.PerformClick();
            pbGrid.Click += (_, __) => btnPickGrid.PerformClick();
            pbBack.Click += (_, __) => btnPickBack.PerformClick();

            btnDefault.Click += (_, __) =>
            {
                _colors = SpectrumColorSettings.Defaults();
                UpdateSwatches();
            };
        }

        private void Pick(Func<int> getArgb, Action<int> setArgb, PictureBox swatch)
        {
            using var cd = new ColorDialog
            {
                Color = Color.FromArgb(getArgb()),
                FullOpen = true
            };

            if (cd.ShowDialog(this) == DialogResult.OK)
            {
                setArgb(cd.Color.ToArgb());
                swatch.BackColor = cd.Color;
            }
        }

        private void UpdateSwatches()
        {
            pbFftL.BackColor = Color.FromArgb(_colors.FftLeftArgb);
            pbFftR.BackColor = Color.FromArgb(_colors.FftRightArgb);

            pbOverlay.BackColor = Color.FromArgb(_colors.OverlayArgb);
            pbPeakL.BackColor = Color.FromArgb(_colors.PeakHoldLeftArgb);
            pbPeakR.BackColor = Color.FromArgb(_colors.PeakHoldRightArgb);

            pbGrid.BackColor = Color.FromArgb(_colors.GridArgb);
            pbBack.BackColor = Color.FromArgb(_colors.BackgroundArgb);
        }

        private static SpectrumColorSettings Clone(SpectrumColorSettings src) => new SpectrumColorSettings
        {
            FftLeftArgb = src.FftLeftArgb,
            FftRightArgb = src.FftRightArgb,
            OverlayArgb = src.OverlayArgb,
            PeakHoldLeftArgb = src.PeakHoldLeftArgb,
            PeakHoldRightArgb = src.PeakHoldRightArgb,
            GridArgb = src.GridArgb,
            BackgroundArgb = src.BackgroundArgb
        };
    }
}
