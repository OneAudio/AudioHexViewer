using System;

namespace AudioHexViewer
{
    internal sealed class OverlayFile
    {
        public int Version { get; set; } = 1;

        public string Name { get; set; } = "Overlay";
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        // Contexte de calcul (utile pour compatibilité)
        public int SampleRate { get; set; }
        public int FftSize { get; set; }
        public string Window { get; set; } = "Rectangular";
        public bool RemoveDc { get; set; }

        // Données spectre (dBFS)
        public double[]? LeftDbFs { get; set; }
        public double[]? RightDbFs { get; set; }
    }
}
