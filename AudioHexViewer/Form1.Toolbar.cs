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

    // ── Side panel images (2 states) ─────────────────────────────────────────
    private Image? _imgShowPanel;
    private Image? _imgHidePanel;

    // ── Shared tooltip component ──────────────────────────────────────────────
    private readonly ToolTip _toolbarTip = new ToolTip { InitialDelay = 400, ReshowDelay = 200 };

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
        var tbDevices = MakeButton("AudioHexViewer_Devices", "Audio devices");
        tbDevices.Click += (_, _) => menuDevices_Click(tbDevices, EventArgs.Empty);

        _tbGenerator = MakeButton("AudioHexViewer_Generator",
            "Generator\nLeft-click: open dialog  |  Right-click: start / stop");
        _tbGenerator.Click        += (_, _) => menuGenerator_Click(_tbGenerator, EventArgs.Empty);
        _tbGenerator.RightClicked += TbGenerator_RightClicked;

        // ── Group 2: Display ─────────────────────────────────────────────────
        var tbColors = MakeButton("AudioHexViewer_Colors", "Spectrum colors");
        tbColors.Click += (_, _) => ShowColorsDialog();

        var tbDataView = MakeButton("AudioHexViewer_DataView", "DataView — numeric values");
        tbDataView.Click += (_, _) => menuDataView_Click(tbDataView, EventArgs.Empty);

        var tbAllFormats = MakeButton("AudioHexViewer_AllDataFormats", "Show all data formats");
        tbAllFormats.Click += (_, _) => menuShowAllFormats_Click(tbAllFormats, EventArgs.Empty);

        // Side panel — 2-image toggle
        _tbSidePanel = new ToolbarButton(_imgShowPanel);
        _toolbarTip.SetToolTip(_tbSidePanel, "Show / hide measurement panel");
        _tbSidePanel.Click += TbSidePanel_Click;

        // Always on top — 2-image toggle
        _tbAlwaysOnTop = new ToolbarButton(
            LoadIcon("AudioHexViewer_AlwaysontopOFF"),
            LoadIcon("AudioHexViewer_AlwaysontopON"),
            isToggle: true);
        _toolbarTip.SetToolTip(_tbAlwaysOnTop, "Always on top");
        _tbAlwaysOnTop.Click += TbAlwaysOnTop_Click;

        // ── Group 3: Export ───────────────────────────────────────────────────
        var tbCopy = MakeButton("AudioHexViewer_Copy", "Copy graph to clipboard");
        tbCopy.Click += (_, _) => menuCopyClipboard_Click(tbCopy, EventArgs.Empty);

        var tbPng = MakeButton("AudioHexViewer_ExportPNG", "Export as PNG…");
        tbPng.Click += (_, _) => ExportActiveGraphToPng();

        var tbCsv = MakeButton("AudioHexViewer_ExportCSV", "Export data to CSV…");
        tbCsv.Click += (_, _) => menuExportCsv_Click(tbCsv, EventArgs.Empty);

        var tbPdf = MakeButton("AudioHexViewer_ExportPDF", "Export to PDF…");
        tbPdf.Click += (_, _) => menuExportPdf_Click(tbPdf, EventArgs.Empty);

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
        PlaceButton(tbPdf,  x, BtnTop, BtnSize);

        panelTop.ResumeLayout(true);

        SyncToolbarSidePanel();
        SyncToolbarAlwaysOnTop();
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
    private ToolbarButton MakeButton(string iconName, string tooltipText)
    {
        var btn = new ToolbarButton(LoadIcon(iconName));
        _toolbarTip.SetToolTip(btn, tooltipText);
        return btn;
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

        _generatorWindow.Location = new Point(Location.X + Width + 4, Location.Y);
        SubscribeGeneratorStateChange();
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
}
