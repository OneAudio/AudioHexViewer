// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

// ══════════════════════════════════════════════════════════════════════════════
//  ExportPngDialog  v2
//
//  Nouveautés vs v1 :
//   • Mémorisation du dernier dossier utilisé (statique → survit à la réouverture)
//   • Nom de fichier horodaté pré-rempli (passé via SetDefaultFileName)
//   • Champ "Subtitle / notes" (1 ligne, 80 car.) ajouté sous le titre principal
//   • Case "Open after export" pour ouvrir automatiquement le fichier
//   • Browse() restaure le dossier mémorisé dans le SaveFileDialog
//   • Layout légèrement agrandi pour accueillir les nouveaux contrôles
// ══════════════════════════════════════════════════════════════════════════════
public sealed class ExportPngDialog : Form
{
    // ── Contrôles ─────────────────────────────────────────────────────────────
    private readonly TextBox        _tbPath      = new();
    private readonly Button         _btnBrowse   = new();
    private readonly NumericUpDown  _nudScale    = new();
    private readonly RichTextBox    _tbTitle     = new();
    private readonly TextBox        _tbSubtitle  = new();
    private readonly CheckBox       _chkOpen     = new();
    private readonly Label          _lblHint     = new();
    private readonly Button         _btnOk       = new();
    private readonly Button         _btnCancel   = new();

    // ── État statique (mémorisé entre ouvertures) ─────────────────────────────
    private static string _lastFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    // ── Propriétés publiques ──────────────────────────────────────────────────
    public string  FilePath    => _tbPath.Text.Trim();
    public int     ExpScale    => (int)_nudScale.Value;
    public bool    OpenAfter   => _chkOpen.Checked;

    public string GetTitleText()    => _tbTitle.Text;
    public string GetSubtitleText() => _tbSubtitle.Text.Trim();

    public void SetTitleText(string value)
    {
        _tbTitle.Text = value ?? string.Empty;
        EnforceTitleLimits();
    }

    /// <summary>
    /// Pré-remplit le champ chemin avec dossier mémorisé + nom horodaté,
    /// seulement si le champ est encore vide.
    /// </summary>
    public void SetDefaultFileName(string baseNameNoExt)
    {
        if (!string.IsNullOrWhiteSpace(_tbPath.Text)) return;
        _tbPath.Text = Path.Combine(_lastFolder, baseNameNoExt + ".png");
    }

    // ── Constantes ────────────────────────────────────────────────────────────
    private bool       _inEnforce;
    private const int  MaxCharsTotal   = 150;
    private const int  MaxCharsPerLine = 50;
    private const int  MaxSubtitle     = 80;

    // ══════════════════════════════════════════════════════════════════════════
    public ExportPngDialog()
    {
        Text              = "Export PNG / PDF";
        StartPosition     = FormStartPosition.CenterParent;
        FormBorderStyle   = FormBorderStyle.FixedDialog;
        MaximizeBox       = false;
        MinimizeBox       = false;
        ShowInTaskbar     = false;
        ClientSize        = new Size(480, 320);

        // ── Layout principal ─────────────────────────────────────────────────
        var layout = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 3,
            RowCount    = 9,
            Padding     = new Padding(12, 10, 12, 10),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));   // labels
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100));  // inputs
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));   // browse / extras

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));  // 0 — file path
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));   // 1 — spacer
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));  // 2 — scale + open after
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));   // 3 — spacer
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));  // 4 — title label
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));  // 5 — hint
        layout.RowStyles.Add(new RowStyle(SizeType.Percent,  100)); // 6 — title textbox
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));  // 7 — subtitle
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));  // 8 — buttons

        // ── Row 0 : File path ─────────────────────────────────────────────────
        layout.Controls.Add(MkLabel("File :"), 0, 0);

        _tbPath.Dock = DockStyle.Fill;
        layout.Controls.Add(_tbPath, 1, 0);

        _btnBrowse.Text  = "Browse…";
        _btnBrowse.Dock  = DockStyle.Fill;
        _btnBrowse.Click += (_, _) => Browse();
        layout.Controls.Add(_btnBrowse, 2, 0);

        // ── Row 2 : Scale + Open after ────────────────────────────────────────
        layout.Controls.Add(MkLabel("Scale :"), 0, 2);

        var scaleRow = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = false,
            Padding       = new Padding(0),
        };

        _nudScale.Minimum = 1;
        _nudScale.Maximum = 4;
        _nudScale.Value   = 2;
        _nudScale.Width   = 55;
        _nudScale.Height  = 24;

        var lblScaleHint = new Label
        {
            Text      = "  (×1 = screen size,  ×2 = recommended,  ×4 = high-res)",
            AutoSize  = true,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = SystemColors.GrayText,
            Font      = new Font("Segoe UI", 7.5f),
            Padding   = new Padding(0, 4, 0, 0),
        };

        scaleRow.Controls.Add(_nudScale);
        scaleRow.Controls.Add(lblScaleHint);
        layout.Controls.Add(scaleRow, 1, 2);

        _chkOpen.Text     = "Open after export";
        _chkOpen.AutoSize = true;
        _chkOpen.Dock     = DockStyle.Fill;
        _chkOpen.TextAlign = ContentAlignment.MiddleCenter;
        layout.Controls.Add(_chkOpen, 2, 2);

        // ── Row 4-6 : Title ───────────────────────────────────────────────────
        var titleLabel = new Label
        {
            Text      = "Title  (3 lines max, 50 characters / line) :",
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        layout.Controls.Add(titleLabel, 0, 4);
        layout.SetColumnSpan(titleLabel, 3);

        _lblHint.Dock      = DockStyle.Fill;
        _lblHint.TextAlign = ContentAlignment.MiddleLeft;
        _lblHint.ForeColor = SystemColors.GrayText;
        _lblHint.Font      = new Font("Segoe UI", 7.5f);
        layout.Controls.Add(_lblHint, 0, 5);
        layout.SetColumnSpan(_lblHint, 3);

        _tbTitle.Dock        = DockStyle.Fill;
        _tbTitle.BorderStyle = BorderStyle.FixedSingle;
        _tbTitle.DetectUrls  = false;
        _tbTitle.ScrollBars  = RichTextBoxScrollBars.Vertical;
        _tbTitle.WordWrap    = true;
        layout.Controls.Add(_tbTitle, 0, 6);
        layout.SetColumnSpan(_tbTitle, 3);

        // ── Row 7 : Subtitle ─────────────────────────────────────────────────
        layout.Controls.Add(MkLabel("Notes :"), 0, 7);

        _tbSubtitle.Dock          = DockStyle.Fill;
        _tbSubtitle.MaxLength     = MaxSubtitle;
        _tbSubtitle.PlaceholderText = $"Optional sub-title / notes  (max {MaxSubtitle} characters)";
        layout.Controls.Add(_tbSubtitle, 1, 7);
        layout.SetColumnSpan(_tbSubtitle, 2);

        // ── Row 8 : Buttons ───────────────────────────────────────────────────
        var btnPanel = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding       = new Padding(0, 4, 0, 0),
        };

        _btnOk.Text   = "EXPORT";
        _btnOk.Width  = 90;
        _btnOk.Height = 27;
        _btnOk.Click += (_, _) => OnOk();

        _btnCancel.Text   = "CANCEL";
        _btnCancel.Width  = 90;
        _btnCancel.Height = 27;
        _btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        btnPanel.Controls.Add(_btnOk);
        btnPanel.Controls.Add(_btnCancel);
        layout.Controls.Add(btnPanel, 0, 8);
        layout.SetColumnSpan(btnPanel, 3);

        Controls.Add(layout);

        // ── Events ────────────────────────────────────────────────────────────
        EnforceTitleLimits();

        _tbTitle.TextChanged += (_, _) => { EnforceTitleLimits(); CenterTitleText(); };

        _tbTitle.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                _tbTitle.SelectedText = Environment.NewLine;
                e.SuppressKeyPress = true;
            }
        };

        _tbTitle.KeyPress += (_, e) =>
        {
            if (char.IsControl(e.KeyChar)) return;
            if (e.KeyChar is '\r' or '\n') { e.Handled = true; return; }
            if (IsCurrentLineFull(_tbTitle))
            {
                if (GetLineCount(_tbTitle.Text) < 3)
                { _tbTitle.SelectedText = Environment.NewLine + e.KeyChar; e.Handled = true; return; }
                e.Handled = true;
            }
        };

        // Mémorisation du dossier quand le chemin change manuellement
        _tbPath.Leave += (_, _) =>
        {
            string dir = Path.GetDirectoryName(_tbPath.Text.Trim()) ?? "";
            if (Directory.Exists(dir)) _lastFolder = dir;
        };

        CenterTitleText();
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Browse
    // ══════════════════════════════════════════════════════════════════════════
    private void Browse()
    {
        // Récupère le dossier et nom courants du champ (s'ils existent)
        string curPath = _tbPath.Text.Trim();
        string initDir  = Directory.Exists(Path.GetDirectoryName(curPath) ?? "") 
                          ? Path.GetDirectoryName(curPath)! 
                          : _lastFolder;
        string initName = string.IsNullOrWhiteSpace(curPath)
                          ? $"AudioHexViewer_{DateTime.Now:yyyyMMdd_HHmmss}"
                          : Path.GetFileNameWithoutExtension(curPath);

        using var sfd = new SaveFileDialog
        {
            Title            = "Export — choose destination",
            Filter           = "PNG image (*.png)|*.png|PDF document (*.pdf)|*.pdf",
            FilterIndex      = curPath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? 2 : 1,
            AddExtension     = false,
            InitialDirectory = initDir,
            FileName         = initName,
        };

        if (sfd.ShowDialog(this) != DialogResult.OK) return;

        string path = sfd.FileName;
        if (string.IsNullOrWhiteSpace(Path.GetExtension(path)))
            path += (sfd.FilterIndex == 2) ? ".pdf" : ".png";

        _tbPath.Text = path;
        _lastFolder  = Path.GetDirectoryName(path) ?? _lastFolder;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Validation
    // ══════════════════════════════════════════════════════════════════════════
    private void OnOk()
    {
        string path = FilePath;

        if (string.IsNullOrWhiteSpace(path))
        {
            MessageBox.Show(this, "Please choose an output file.", "Export",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _tbPath.Focus();
            return;
        }

        // Crée le dossier si besoin
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
        {
            try   { Directory.CreateDirectory(dir); }
            catch { MessageBox.Show(this, $"Cannot create folder:\n{dir}", "Export",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        }

        // Mémorise le dossier
        if (!string.IsNullOrWhiteSpace(dir)) _lastFolder = dir;

        DialogResult = DialogResult.OK;
        Close();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Open-after-export helper (appelé depuis Form1 après export réussi)
    // ══════════════════════════════════════════════════════════════════════════
    public static void TryOpenFile(string filePath)
    {
        try { Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true }); }
        catch { /* viewer non disponible — on ignore silencieusement */ }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Title text helpers (inchangés vs v1)
    // ══════════════════════════════════════════════════════════════════════════
    private void CenterTitleText()
    {
        int s = _tbTitle.SelectionStart, l = _tbTitle.SelectionLength;
        _tbTitle.SelectAll();
        _tbTitle.SelectionAlignment = HorizontalAlignment.Center;
        _tbTitle.Select(s, l);
    }

    private static int GetContentLength(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        int n = 0;
        foreach (char c in text) if (c != '\r' && c != '\n') n++;
        return n;
    }

    private static string NormalizeToWrappedLines(string text)
    {
        string t = (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = t.Split('\n').ToList();
        if (lines.Count > 3) lines = lines.Take(3).ToList();
        for (int i = 0; i < lines.Count; i++)
            if (lines[i].Length > MaxCharsPerLine)
                lines[i] = lines[i][..MaxCharsPerLine];
        string rebuilt = string.Join("\n", lines);
        int contentLen = rebuilt.Count(c => c != '\n');
        if (contentLen <= MaxCharsTotal) return rebuilt;
        var sb = new System.Text.StringBuilder(rebuilt.Length);
        int kept = 0;
        foreach (char c in rebuilt)
        {
            if (c == '\n') { sb.Append(c); continue; }
            if (kept >= MaxCharsTotal) break;
            sb.Append(c); kept++;
        }
        return sb.ToString();
    }

    private void EnforceTitleLimits()
    {
        if (_inEnforce) return;
        try
        {
            _inEnforce = true;
            int selStart = _tbTitle.SelectionStart;
            string normalized = NormalizeToWrappedLines(_tbTitle.Text);
            string normalizedWin = normalized.Replace("\n", Environment.NewLine);
            if (_tbTitle.Text != normalizedWin) _tbTitle.Text = normalizedWin;
            int newSel = Math.Min(selStart, _tbTitle.TextLength);
            _tbTitle.SelectionStart  = newSel;
            _tbTitle.SelectionLength = 0;

            int used = GetContentLength(_tbTitle.Text);
            int lines = 1;
            foreach (char c in _tbTitle.Text) if (c == '\n') lines++;
            _lblHint.Text = $"{lines}/3 lines  —  {used}/{MaxCharsTotal} characters  —  {MaxCharsTotal - used} remaining";
        }
        finally { _inEnforce = false; }
    }

    private static int GetLineCount(string text)
    {
        if (string.IsNullOrEmpty(text)) return 1;
        return 1 + text.Replace("\r\n", "\n").Replace('\r', '\n').Count(c => c == '\n');
    }

    private static bool IsCurrentLineFull(RichTextBox rtb)
    {
        int idx = rtb.GetLineFromCharIndex(rtb.SelectionStart);
        if (idx < 0) idx = 0;
        string[] lines = rtb.Lines;
        if (lines.Length == 0) return false;
        if (idx >= lines.Length) idx = lines.Length - 1;
        return lines[idx].Length >= MaxCharsPerLine;
    }

    // ── Factory helper pour les labels de colonne 0 ───────────────────────────
    private static Label MkLabel(string text) => new()
    {
        Text      = text,
        TextAlign = ContentAlignment.MiddleRight,
        Dock      = DockStyle.Fill,
    };
}
