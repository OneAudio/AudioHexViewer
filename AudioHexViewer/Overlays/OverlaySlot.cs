namespace AudioHexViewer
{
    internal sealed class OverlaySlot
    {
        public int Index { get; }
        public bool Enabled { get; set; } = true;

        public string Name { get; set; } = "Overlay";
        public string? FilePath { get; set; }

        public OverlayFile? Data { get; set; }

        public OverlaySlot(int index)
        {
            Index = index;
        }
    }
}
