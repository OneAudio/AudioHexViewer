namespace AudioHexViewer
{
    internal sealed class SpectrumOptions
    {
        public WindowType WindowType { get; set; } = WindowType.Rectangular;
        public bool RemoveDc { get; set; } = false;

        // Commit 4 : préparer Bode / mesures (phase, ratio complexe)
        public bool NeedComplexFft { get; set; } = false;
    }
}

