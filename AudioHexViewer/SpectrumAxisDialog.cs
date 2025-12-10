using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace AudioHexViewer
{
    public partial class SpectrumAxisDialog : Form
    {
        public SpectrumAxisDialog()
        {
            InitializeComponent();
        }

        // Y max (dBFS) – lecture/écriture, non sérialisé par le designer
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public double YMax
        {
            get => (double)numYMax.Value;
            set
            {
                if ((decimal)value < numYMax.Minimum) value = (double)numYMax.Minimum;
                if ((decimal)value > numYMax.Maximum) value = (double)numYMax.Maximum;
                numYMax.Value = (decimal)value;
            }
        }

        // Y min (dBFS)
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public double YMin
        {
            get => (double)numYMin.Value;
            set
            {
                if ((decimal)value < numYMin.Minimum) value = (double)numYMin.Minimum;
                if ((decimal)value > numYMin.Maximum) value = (double)numYMin.Maximum;
                numYMin.Value = (decimal)value;
            }
        }

        // F min en Hz
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public double FMinHz
        {
            get => (double)numFMin.Value;
            set
            {
                if ((decimal)value < numFMin.Minimum) value = (double)numFMin.Minimum;
                if ((decimal)value > numFMin.Maximum) value = (double)numFMin.Maximum;
                numFMin.Value = (decimal)value;
            }
        }

        // F max en Hz
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public double FMaxHz
        {
            get => (double)numFMax.Value;
            set
            {
                if ((decimal)value < numFMax.Minimum) value = (double)numFMax.Minimum;
                if ((decimal)value > numFMax.Maximum) value = (double)numFMax.Maximum;
                numFMax.Value = (decimal)value;
            }
        }
    }
}
