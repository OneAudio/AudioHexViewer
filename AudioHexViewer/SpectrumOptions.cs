namespace AudioHexViewer
{
    internal sealed class SpectrumOptions
    {
        public WindowType WindowType { get; set; } = WindowType.Rectangular;
        public bool RemoveDc { get; set; } = false;
    }
}
