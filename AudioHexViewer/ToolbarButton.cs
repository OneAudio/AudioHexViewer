using System.Drawing.Drawing2D;

namespace AudioHexViewer;

/// <summary>
/// Circular icon button for the toolbar — transparent background, hover/active states.
/// </summary>
internal sealed class ToolbarButton : Control
{
    // ── Images ───────────────────────────────────────────────────────────────
    private Image? _normalImage;
    private Image? _activeImage;

    // ── States ───────────────────────────────────────────────────────────────
    private bool _isHovered;
    private bool _isActive;
    private bool _isPressed;

    // ── Colors ───────────────────────────────────────────────────────────────
    private static readonly Color ColNormal  = Color.FromArgb(0xE4, 0xE4, 0xE4);
    private static readonly Color ColHovered = Color.FromArgb(0xD0, 0xD0, 0xD0);
    private static readonly Color ColActive  = Color.FromArgb(0xC2, 0xE8, 0xAA);
    private static readonly Color ColPressed = Color.FromArgb(0xB8, 0xB8, 0xB8);

    // ── Events ───────────────────────────────────────────────────────────────
    /// <summary>Fired on right-click only, never on left-click.</summary>
    public event EventHandler? RightClicked;

    // ── Constructor ──────────────────────────────────────────────────────────
    public ToolbarButton(Image? normalImage, Image? activeImage = null, bool isToggle = false)
    {
        _normalImage = normalImage;
        _activeImage = activeImage;
        IsToggle     = isToggle;

        Size = new Size(44, 44);

        SetStyle(ControlStyles.UserPaint
               | ControlStyles.AllPaintingInWmPaint
               | ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.ResizeRedraw, true);
        // StandardClick remains true (default) so that WinForms raises Click
        // for left-clicks normally. We only filter it in OnClick for right-clicks.

        Cursor = Cursors.Hand;
        UpdateCircleRegion();
    }

    // ── Public properties ────────────────────────────────────────────────────
    public bool IsToggle { get; set; }

    public bool IsActive
    {
        get => _isActive;
        set { if (_isActive == value) return; _isActive = value; Invalidate(); }
    }

    public void SetNormalImage(Image? img) { _normalImage = img; Invalidate(); }

    // ── Painting ─────────────────────────────────────────────────────────────
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode     = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode   = PixelOffsetMode.HighQuality;

        Color bgColor = _isPressed ? ColPressed
                      : _isActive  ? ColActive
                      : _isHovered ? ColHovered
                                   : ColNormal;

        var rect = new Rectangle(1, 1, Width - 2, Height - 2);
        using (var brush = new SolidBrush(bgColor))
            g.FillEllipse(brush, rect);

        // The PNG already has its own Inkscape circle border — no DrawEllipse here.
        Image? icon = (_isActive && _activeImage != null) ? _activeImage : _normalImage;
        if (icon != null)
        {
            const int margin = 4;
            g.DrawImage(icon, new Rectangle(margin, margin, Width - margin * 2, Height - margin * 2));
        }
    }

    // ── Click filtering ───────────────────────────────────────────────────────
    /// <summary>
    /// WinForms raises OnClick for ALL mouse buttons when StandardClick is set.
    /// We block it here for anything that isn't a plain left-click.
    /// </summary>
    protected override void OnClick(EventArgs e)
    {
        if (e is MouseEventArgs me && me.Button != MouseButtons.Left)
            return;
        base.OnClick(e);
    }

    // ── Mouse handling ───────────────────────────────────────────────────────
    protected override void OnMouseEnter(EventArgs e)
    { _isHovered = true;  Invalidate(); base.OnMouseEnter(e); }

    protected override void OnMouseLeave(EventArgs e)
    { _isHovered = false; _isPressed = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left) { _isPressed = true; Invalidate(); }
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    { _isPressed = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            RightClicked?.Invoke(this, EventArgs.Empty);
            return;   // Click event already blocked in OnClick above
        }
        if (IsToggle && e.Button == MouseButtons.Left)
            IsActive = !IsActive;
        base.OnMouseClick(e);
    }

    // ── Circular region ──────────────────────────────────────────────────────
    private void UpdateCircleRegion()
    {
        using var path = new GraphicsPath();
        path.AddEllipse(0, 0, Width - 1, Height - 1);
        Region = new Region(path);
    }

    protected override void OnSizeChanged(EventArgs e)
    { base.OnSizeChanged(e); UpdateCircleRegion(); }

    // ── Disposal ─────────────────────────────────────────────────────────────
    protected override void Dispose(bool disposing)
    {
        if (disposing) { _normalImage?.Dispose(); _activeImage?.Dispose(); }
        base.Dispose(disposing);
    }
}
