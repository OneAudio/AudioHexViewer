// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

using System.Reflection;

namespace AudioHexViewer;

/// <summary>
/// Partial Form1 — Icon toolbar in panelTop.
/// </summary>
partial class Form1
{
    // ── Stateful button references ────────────────────────────────────────────
    private ToolbarButton? _tbGenerator;
    private ToolbarButton? _tbSidePanel;
    private ToolbarButton? _tbAlwaysOnTop;
    private ToolbarButton? _tbContextHelp;

    // ── Context help menu item (kept in sync with _tbContextHelp) ─────────────
    private ToolStripMenuItem? _menuContextHelp;

    // ── Side panel images (2 states) ─────────────────────────────────────────
    private Image? _imgShowPanel;
    private Image? _imgHidePanel;

    // ── Always-active tooltip for the ? button (independent of context help toggle) ──
    private static readonly ToolTip _alwaysOnTip = new ToolTip { Active = true, InitialDelay = 400, ReshowDelay = 200 };

    // ── Initialization ────────────────────────────────────────────────────────
    /// <summary>
    /// Creates all toolbar buttons and adds them to panelTop.
    /// Called from the Form1 constructor, after InitializeComponent().
    /// </summary>
    private void InitToolbar()
    {
        // Pre-load side panel images (used in SyncToolbarSidePanel)
        _imgShowPanel = LoadIcon("AudioHexViewer_ShowPanel");
        _imgHidePanel = LoadIcon("AudioHexViewer_HidePanel");

        // ── Group 1: Input / Output configuration ────────────────────────────
        var tbDevices = MakeButton("AudioHexViewer_Devices", "tbDevices");
        tbDevices.Click += (_, _) => menuDevices_Click(tbDevices, EventArgs.Empty);

        _tbGenerator = MakeButton("AudioHexViewer_Generator", "tbGenerator");
        _tbGenerator.Click        += (_, _) => menuGenerator_Click(_tbGenerator, EventArgs.Empty);
        _tbGenerator.RightClicked += TbGenerator_RightClicked;

        // ── Group 2: Display ─────────────────────────────────────────────────
        var tbColors = MakeButton("AudioHexViewer_Colors", "tbColors");
        tbColors.Click += (_, _) => ShowColorsDialog();

        var tbDataView = MakeButton("AudioHexViewer_DataView", "tbDataView");
        tbDataView.Click += (_, _) => menuDataView_Click(tbDataView, EventArgs.Empty);

        var tbAllFormats = MakeButton("AudioHexViewer_AllDataFormats", "tbAllFormats");
        tbAllFormats.Click += (_, _) => menuShowAllFormats_Click(tbAllFormats, EventArgs.Empty);

        // Side panel — 2-image toggle
        _tbSidePanel      = new ToolbarButton(_imgShowPanel) { Name = "tbSidePanel" };
        _tbSidePanel.Click += TbSidePanel_Click;

        // Always on top — 2-image toggle
        _tbAlwaysOnTop = new ToolbarButton(
            LoadIcon("AudioHexViewer_AlwaysontopOFF"),
            LoadIcon("AudioHexViewer_AlwaysontopON"),
            isToggle: false) // SyncToolbarAlwaysOnTop() owns IsActive
        {
            Name = "tbAlwaysOnTop"
        };
        _tbAlwaysOnTop.Click += TbAlwaysOnTop_Click;

        // ── Group 3: Export ───────────────────────────────────────────────────
        var tbCopy = MakeButton("AudioHexViewer_Copy", "tbCopy");
        tbCopy.Click += (_, _) => menuCopyClipboard_Click(tbCopy, EventArgs.Empty);

        var tbPng = MakeButton("AudioHexViewer_ExportPNG", "tbPng");
        tbPng.Click += (_, _) => ExportActiveGraphToPng();

        var tbCsv = MakeButton("AudioHexViewer_ExportCSV", "tbCsv");
        tbCsv.Click += (_, _) => menuExportCsv_Click(tbCsv, EventArgs.Empty);

        var tbPdf = MakeButton("AudioHexViewer_ExportPDF", "tbPdf");
        tbPdf.Click += (_, _) => menuExportPdf_Click(tbPdf, EventArgs.Empty);

        // ── Group 4: Help ─────────────────────────────────────────────────────
        _tbContextHelp = new ToolbarButton(
            LoadIcon("AudioHexViewer_ContextHelp"),
            null,
            isToggle: false)
        {
            Name = "tbContextHelp"
        };
        _tbContextHelp.Click += TbContextHelp_Click;

        // ── Layout in panelTop ───────────────────────────────────────────────
        panelTop.SuspendLayout();
        panelTop.Controls.Clear();

        const int BtnSize = 44;
        const int BtnGap  = 4;
        const int GrpGap  = 10;
        const int SepW    = 1;
        const int SepH    = 32;
        const int PadLeft = 8;
        const int BtnTop  = (52 - BtnSize) / 2;

        int x = PadLeft;

        PlaceButton(tbDevices,    x, BtnTop, BtnSize); x += BtnSize + BtnGap;
        PlaceButton(_tbGenerator, x, BtnTop, BtnSize); x += BtnSize + GrpGap;

        PlaceSeparator(x, (52 - SepH) / 2, SepH); x += SepW + GrpGap;

        PlaceButton(tbColors,       x, BtnTop, BtnSize); x += BtnSize + BtnGap;
        PlaceButton(tbDataView,     x, BtnTop, BtnSize); x += BtnSize + BtnGap;
        PlaceButton(tbAllFormats,   x, BtnTop, BtnSize); x += BtnSize + BtnGap;
        PlaceButton(_tbSidePanel,   x, BtnTop, BtnSize); x += BtnSize + BtnGap;
        PlaceButton(_tbAlwaysOnTop, x, BtnTop, BtnSize); x += BtnSize + GrpGap;

        PlaceSeparator(x, (52 - SepH) / 2, SepH); x += SepW + GrpGap;

        PlaceButton(tbCopy, x, BtnTop, BtnSize); x += BtnSize + BtnGap;
        PlaceButton(tbPng,  x, BtnTop, BtnSize); x += BtnSize + BtnGap;
        PlaceButton(tbCsv,  x, BtnTop, BtnSize); x += BtnSize + BtnGap;
        PlaceButton(tbPdf,  x, BtnTop, BtnSize); x += BtnSize + GrpGap;

        PlaceSeparator(x, (52 - SepH) / 2, SepH); x += SepW + GrpGap;

        PlaceButton(_tbContextHelp, x, BtnTop, BtnSize);

        panelTop.ResumeLayout(true);

        SyncToolbarSidePanel();
        SyncToolbarAlwaysOnTop();

        // ── Progression fichier (droite de panelTop) ──────────────────────
        InitProgressWidget();
    }

    // ── Context help: initialization(call from Form1_Load) ──────────────────
    /// <summary>
    /// Loads ContextHelp.json, applies tooltips to all controls, sets the
    /// toolbar button and Help menu item to the saved preference.
    /// <para>
    /// Call from <c>Form1_Load</c>:
    /// <code>InitContextHelp(helpToolStripMenuItem);</code>
    /// </para>
    /// </summary>
    internal void InitContextHelp(ToolStripMenuItem helpMenu)
    {
        // 1 — Load JSON (tolerates missing file silently)
        ContextHelpManager.Load(
            Path.Combine(AppContext.BaseDirectory, "ContextHelp.json"));

        // 3 — Apply tooltips to every named control in the form
        ContextHelpManager.ApplyTooltips(this.Controls);

        // 3b — ? button uses its own always-active tooltip (independent of toggle)
        ContextHelpManager.ToolTip.SetToolTip(_tbContextHelp, null);   // exclude from context tooltip
        if (ContextHelpManager.TryGetText("tbContextHelp", out var tipText))
            _alwaysOnTip.SetToolTip(_tbContextHelp, tipText);

        // 2 — Restore saved preference
        bool enabled = Properties.Settings.Default.ContextHelpEnabled;
        ContextHelpManager.IsEnabled = enabled;

      

        // 4 — Sync toolbar button visual state
        if (_tbContextHelp != null)
            _tbContextHelp.IsActive = enabled;

        // 5 — Add "Enable context help" item at top of Help menu
        _menuContextHelp = new ToolStripMenuItem("Enable context help")
        {
            CheckOnClick = true,
            Checked      = enabled
        };
        _menuContextHelp.Click += (_, _) =>
        {
            bool on = _menuContextHelp.Checked;
            ContextHelpManager.IsEnabled = on;
            if (_tbContextHelp != null) _tbContextHelp.IsActive = on;
            Properties.Settings.Default.ContextHelpEnabled = on;
            Properties.Settings.Default.Save();
            SyncMenuTooltips(on);
        };
        helpMenu.DropDownItems.Insert(0, _menuContextHelp);

        // 6 — Apply menu item tooltips (Overlap, Sample size…)
        SyncMenuTooltips(enabled);

#if DEBUG
        ContextHelpManager.ReportMissing(this.Controls);
#endif
    }

    // ── Context help: toolbar button click ───────────────────────────────────
    private void TbContextHelp_Click(object? sender, EventArgs e)
    {
        bool enabled = !ContextHelpManager.IsEnabled;   // calcule le nouvel état
        ContextHelpManager.IsEnabled = enabled;
        _tbContextHelp!.IsActive = enabled;  // pilotage visuel manuel
        if (_menuContextHelp != null)
            _menuContextHelp.Checked = enabled;
        Properties.Settings.Default.ContextHelpEnabled = enabled;
        Properties.Settings.Default.Save();
        SyncMenuTooltips(enabled);
    }

    // ── Layout helpers ────────────────────────────────────────────────────────
    private void PlaceButton(ToolbarButton btn, int x, int y, int size)
    {
        btn.Location = new Point(x, y);
        btn.Size     = new Size(size, size);
        panelTop.Controls.Add(btn);
    }

    private void PlaceSeparator(int x, int y, int height)
    {
        panelTop.Controls.Add(new Panel
        {
            Location  = new Point(x, y),
            Size      = new Size(1, height),
            BackColor = Color.FromArgb(0xB0, 0xB0, 0xB0)
        });
    }

    // ── Simple button factory ─────────────────────────────────────────────────
    /// <summary>
    /// Creates a <see cref="ToolbarButton"/> with the given icon and control Name.
    /// Tooltips are applied later by <see cref="ContextHelpManager.ApplyTooltips"/>.
    /// </summary>
    private static ToolbarButton MakeButton(string iconName, string name)
    {
        return new ToolbarButton(LoadIcon(iconName)) { Name = name };
    }

    // ── Icon loading from embedded resources ──────────────────────────────────
    private static Image? LoadIcon(string baseName)
    {
        string resourceName = $"AudioHexViewer.Resources.Icons.{baseName}.png";
        var stream = Assembly.GetExecutingAssembly()
                             .GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Toolbar] Resource not found: {resourceName}");
            return null;
        }
        return Image.FromStream(stream);
    }

    // ── Generator window creation (shared by left-click menu and right-click) ─
    /// <summary>
    /// Creates the GeneratorWindow if it does not exist yet, without showing it.
    /// Safe to call multiple times — no-op if the window already exists.
    /// </summary>
    private void EnsureGeneratorWindowCreated()
    {
        if (_generatorWindow != null && !_generatorWindow.IsDisposed) return;

        _generatorWindow = new GeneratorWindow(
            getOutputDevice:   () => _selectedOutputDevice,
            getInputDevice:    () => _selectedInputDevice,
            getOutputFormat:   () => _requestedOutputFormat,
            getAnalyzerSR:     () => currentFormat?.SampleRate ?? 0,
            getAnalyzerN:      () => spectrumControl?.CurrentFftSize ?? 0,
            getChannelMode:    () => spectrumControl?.SelectedChannelMode ?? "",
            onLoopbackRequest: HandleLoopbackRequest,
            settings:          _session,
            saveSettings:      SaveSessionSettingsSilently,
            onRunStateChanged: UpdateTitleBar);

        // Restore last position - but only if it is actually visible on some screen
        // (a minimized window reports Left/Top = -32000, which would hide it off-screen).
        _generatorWindow.StartPosition = FormStartPosition.Manual;
        if (_lastGeneratorX.HasValue && _lastGeneratorY.HasValue &&
            IsOnScreen(new Point(_lastGeneratorX.Value, _lastGeneratorY.Value), _generatorWindow.Size))
        {
            _generatorWindow.Location = new Point(_lastGeneratorX.Value, _lastGeneratorY.Value);
        }
        else
        {
            _generatorWindow.Location = new Point(Location.X + Width + 4, Location.Y);
        }
        _lastGeneratorX = _generatorWindow.Left;
        _lastGeneratorY = _generatorWindow.Top;

        // Save the position only in the normal state (ignore the -32000 reported while minimized).
        _generatorWindow.LocationChanged += (_, _) =>
        {
            if (_generatorWindow.WindowState != FormWindowState.Normal) return;
            _lastGeneratorX = _generatorWindow.Left;
            _lastGeneratorY = _generatorWindow.Top;
        };
        SubscribeGeneratorStateChange();
    }

    /// <summary>
    /// True if a usable part of a window placed at <paramref name="location"/> with the given
    /// <paramref name="size"/> lands on any screen's working area. Guards against off-screen
    /// restored positions (minimized windows report -32000, an unplugged second monitor, a
    /// resolution change, etc.). Shared by all windows that persist their position.
    /// </summary>
    internal static bool IsOnScreen(Point location, Size size)
    {
        var rect = new Rectangle(location, size);
        foreach (var screen in Screen.AllScreens)
        {
            var vis = Rectangle.Intersect(screen.WorkingArea, rect);
            if (vis.Width >= 100 && vis.Height >= 30) return true;  // enough of the title bar is reachable
        }
        return false;
    }

    // ── Generator button: right-click = start / stop (no dialog open) ─────────
    private void TbGenerator_RightClicked(object? sender, EventArgs e)
    {
        // Create the window silently if needed — do NOT show it
        EnsureGeneratorWindowCreated();

        if (_generatorWindow!.IsGenerating)
            _generatorWindow.StopGeneratorPublic();
        else
            _generatorWindow.StartGeneratorPublic();

        SyncToolbarGenerator();
    }

    /// <summary>
    /// Subscribe to IsGeneratingChanged so the button stays in sync
    /// even when the user starts/stops via the GeneratorWindow dialog's own button.
    /// Guard against double-subscription via Tag.
    /// </summary>
    private void SubscribeGeneratorStateChange()
    {
        if (_generatorWindow == null || _generatorWindow.IsDisposed) return;
        if (_generatorWindow.Tag is string t && t == "tb-sub") return;
        _generatorWindow.IsGeneratingChanged += _ => SyncToolbarGenerator();
        _generatorWindow.Tag = "tb-sub";
    }

    /// <summary>
    /// Called from menuGenerator_Click after the window is shown.
    /// Ensures toolbar subscription is established and state is reflected.
    /// </summary>
    internal void OnGeneratorWindowShown()
    {
        SubscribeGeneratorStateChange();
        SyncToolbarGenerator();
    }

    // ── Side panel toggle ─────────────────────────────────────────────────────
    private void TbSidePanel_Click(object? sender, EventArgs e)
    {
        _measPanelVisible = !_measPanelVisible;
        ApplyMeasPanelVisible();
        _session.ShowMeasPanel = _measPanelVisible;
        SaveSessionSettingsSilently();
    }

    // ── Always on top toggle ──────────────────────────────────────────────────
    private void TbAlwaysOnTop_Click(object? sender, EventArgs e)
    {
        alwaysOnTopToolStripMenuItem.Checked = !alwaysOnTopToolStripMenuItem.Checked;
        this.TopMost = alwaysOnTopToolStripMenuItem.Checked;
        if (_dataViewForm != null && !_dataViewForm.IsDisposed)
            _dataViewForm.TopMost = alwaysOnTopToolStripMenuItem.Checked;
        SyncToolbarAlwaysOnTop();
    }

    // ── State synchronization ─────────────────────────────────────────────────

    internal void SyncToolbarGenerator()
    {
        if (_tbGenerator == null) return;
        bool running = _generatorWindow?.IsGenerating == true;
        if (IsHandleCreated && InvokeRequired)
            BeginInvoke(() => _tbGenerator.IsActive = running);
        else
            _tbGenerator.IsActive = running;
    }

    internal void SyncToolbarSidePanel()
    {
        if (_tbSidePanel == null) return;
        _tbSidePanel.SetNormalImage(_measPanelVisible ? _imgShowPanel : _imgHidePanel);
    }

    internal void SyncToolbarAlwaysOnTop()
    {
        if (_tbAlwaysOnTop == null) return;
        _tbAlwaysOnTop.IsActive = alwaysOnTopToolStripMenuItem.Checked;
    }

    // ── Menu item tooltips (Overlap, Sample size…) ────────────────────────────
    private void SyncMenuTooltips(bool enabled)
    {
        menuStrip1.ShowItemToolTips = enabled;
        ContextHelpManager.ApplyMenuItemTooltips(menuStrip1.Items, enabled);
    }

    // -- File playback progress widget + T0 button
    // Shown whenever offline/post-process mode is active (playing OR paused).

    private Panel?              _offlinePanel;
    private FileProgressWidget? _progressWidget;
    private Button?             _btnT0;

    private void InitProgressWidget()
    {
        const int MARGIN_RIGHT = 10;
        const int BAR_H        = 16;   // same as FileProgressWidget.barH
        const int BAR_TOP      = 3;    // same as FileProgressWidget.barTop
        const int T0_SIZE      = BAR_H;   // square: 16 x 16
        const int GAP          = 3;
        const int PROGRESS_W   = 82;
        const int CONTAINER_W  = T0_SIZE + GAP + PROGRESS_W;   // 101

        _btnT0 = new Button
        {
            Text      = "",
            Size      = new Size(T0_SIZE, T0_SIZE),
            Location  = new Point(0, BAR_TOP),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(220, 220, 220),
            Cursor    = Cursors.Hand,
            TabStop   = false,
        };
        _btnT0.FlatAppearance.BorderColor = Color.FromArgb(150, 150, 150);
        _btnT0.FlatAppearance.BorderSize  = 1;
        _btnT0.Click += (_, _) => SeekFileToStart();

        // Draw |◄ icon in GDI+ matching the reference "back to start" button style:
        //   white background, medium-gray bar + triangle, clean proportions
        _btnT0.Paint += (s, pe) =>
        {
            var btn = (Button)s!;
            var g   = pe.Graphics;
            g.SmoothingMode     = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.PixelOffsetMode   = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

            int w = btn.Width, h = btn.Height;

            // Background — match icon reference (light neutral)
            g.Clear(Color.FromArgb(230, 230, 230));

            // Border
            using var borderPen = new Pen(Color.FromArgb(160, 160, 160), 1f);
            g.DrawRectangle(borderPen, 0, 0, w - 1, h - 1);

            // Icon color — same medium gray for bar and triangle
            var iconColor = Color.FromArgb(115, 115, 115);
            using var ink = new SolidBrush(iconColor);

            int m = 2;   // outer margin

            // Vertical bar |  — ~15 % of width, full usable height
            int barW = Math.Max(2, (int)(w * 0.15f));
            g.FillRectangle(ink, m, m, barW, h - 2 * m);

            // Left-pointing filled triangle ◄
            // tip: just to the right of bar  |  base: right margin
            int tipX  = m + barW + 1;
            int baseX = w - m;
            var tri = new System.Drawing.PointF[]
            {
                new(tipX,  h / 2f),      // tip pointing left
                new(baseX, m),           // top-right corner
                new(baseX, h - m),       // bottom-right corner
            };
            g.FillPolygon(ink, tri);
        };

        _progressWidget = new FileProgressWidget
        {
            Size     = new Size(PROGRESS_W, panelTop.Height),
            Location = new Point(T0_SIZE + GAP, 0),
        };

        _offlinePanel = new Panel
        {
            Size      = new Size(CONTAINER_W, panelTop.Height),
            Anchor    = AnchorStyles.Top | AnchorStyles.Right,
            Visible   = false,
            BackColor = SystemColors.Control,
        };
        _offlinePanel.Location = new Point(
            panelTop.Width - CONTAINER_W - MARGIN_RIGHT, 0);

        _offlinePanel.Controls.Add(_btnT0);
        _offlinePanel.Controls.Add(_progressWidget);
        panelTop.Controls.Add(_offlinePanel);
    }

    internal void HideProgressWidget()
    {
        if (_offlinePanel == null) return;
        void Hide() => _offlinePanel.Visible = false;
        if (InvokeRequired) BeginInvoke(Hide);
        else Hide();
    }

    internal void UpdateFileProgressWidget()
    {
        if (_offlinePanel == null || _progressWidget == null) return;

        // Visible as soon as OFFLINE mode is active (even before RUN)
        bool show = _postProcessMode;

        void DoRefresh()
        {
            _offlinePanel.Visible = show;
            if (!show) return;
            // Use zeros when no file is loaded yet
            _progressWidget.SetData(
                _audioFileReader?.CurrentTime ?? TimeSpan.Zero,
                _audioFileReader?.TotalTime   ?? TimeSpan.Zero,
                (_audioFileReader != null && _audioFileReader.Length > 0)
                    ? (double)_audioFileReader.Position / _audioFileReader.Length
                    : 0.0,
                _fileIsPaused);
            _progressWidget.Invalidate();
        }

        if (InvokeRequired) BeginInvoke(DoRefresh);
        else DoRefresh();
    }
}

internal sealed class FileProgressWidget : Control
{
    private double   _fraction;
    private TimeSpan _current;
    private TimeSpan _total;
    private bool     _isPaused;

    public FileProgressWidget()
    {
        SetStyle(ControlStyles.UserPaint
               | ControlStyles.AllPaintingInWmPaint
               | ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.ResizeRedraw, true);
    }

    public void SetData(TimeSpan current, TimeSpan total, double fraction, bool isPaused)
    {
        _fraction = Math.Clamp(fraction, 0.0, 1.0);
        _current  = current;
        _total    = total;
        _isPaused = isPaused;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        int W = Width, H = Height;
        g.Clear(SystemColors.Control);

        const int barH = 16, barTop = 3;
        var barRect = new Rectangle(2, barTop, W - 4, barH);

        using (var bg = new SolidBrush(Color.FromArgb(205, 205, 205)))
            g.FillRectangle(bg, barRect);

        int fillW = Math.Max(0, (int)(_fraction * (W - 4)));
        if (fillW > 0)
        {
            Color c1 = _isPaused ? Color.FromArgb(160, 160, 160) : Color.FromArgb(120, 140, 175);
            Color c2 = _isPaused ? Color.FromArgb(130, 130, 130) : Color.FromArgb( 90, 110, 150);
            using var grad = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(2, barTop, Math.Max(1, fillW), barH),
                c1, c2, System.Drawing.Drawing2D.LinearGradientMode.Vertical);
            g.FillRectangle(grad, 2, barTop, fillW, barH);
        }

        using (var border = new Pen(Color.FromArgb(150, 150, 150), 1f))
            g.DrawRectangle(border, barRect);

        if (_isPaused && fillW > 4)
        {
            using var pp = new Pen(Color.FromArgb(180, 255, 255, 255), 2f);
            int cx = 2 + fillW / 2;
            g.DrawLine(pp, cx - 3, barTop + 3, cx - 3, barTop + barH - 3);
            g.DrawLine(pp, cx + 1, barTop + 3, cx + 1, barTop + barH - 3);
        }

        using var font = new Font("Segoe UI", 9.75f);
        int lineH  = TextRenderer.MeasureText("0", font).Height;
        int textY1 = barTop + barH + 2;
        int textY2 = textY1 + lineH;

        string line1 = $"{(int)_current.TotalMinutes:00}mn {_current.Seconds:00}s";
        string line2 = $"{(int)_total.TotalMinutes:00}mn {_total.Seconds:00}s";

        var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding
                  | TextFormatFlags.PreserveGraphicsClipping;
        var bgc = SystemColors.Control;

        TextRenderer.DrawText(g, line1, font, new Rectangle(0, textY1, W, lineH + 2),
            _isPaused ? Color.FromArgb(130, 130, 130) : Color.FromArgb(50, 50, 50), bgc, flags);
        TextRenderer.DrawText(g, line2, font, new Rectangle(0, textY2, W, lineH + 2),
            Color.FromArgb(130, 130, 130), bgc, flags);
    }
}
