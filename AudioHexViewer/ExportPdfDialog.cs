using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AudioHexViewer
{
    // ══════════════════════════════════════════════════════════════════════════
    //  ExportPdfDialog — export multi-onglets en PDF multi-pages
    //
    //  Permet de choisir :
    //   • Le fichier PDF de destination
    //   • Les onglets à inclure (Spectrum, Histogram, Oscilloscope)
    //   • Le facteur d'échelle (résolution des pages)
    //   • Un titre global (apparaît en bandeau sur chaque page)
    //   • Un sous-titre / notes (ligne supplémentaire)
    //   • Ouvrir le PDF après génération
    // ══════════════════════════════════════════════════════════════════════════
    internal sealed class ExportPdfDialog : Form
    {
        // ── Contrôles ─────────────────────────────────────────────────────────
        private readonly TextBox       _tbPath     = new();
        private readonly Button        _btnBrowse  = new();
        private readonly CheckBox      _chkSpectrum = new();
        private readonly CheckBox      _chkHistogram = new();
        private readonly CheckBox      _chkOscillo  = new();
        private readonly NumericUpDown _nudScale    = new();
        private readonly TextBox       _tbTitle     = new();
        private readonly TextBox       _tbSubtitle  = new();
        private readonly CheckBox      _chkOpen     = new();
        private readonly Button        _btnOk       = new();
        private readonly Button        _btnCancel   = new();
        private readonly Label         _lblValidation = new();

        // ── État statique ─────────────────────────────────────────────────────
        private static string _lastFolder =
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        // ── Propriétés publiques ──────────────────────────────────────────────
        public string FilePath        => _tbPath.Text.Trim();
        public int    ExpScale        => (int)_nudScale.Value;
        public bool   IncludeSpectrum  => _chkSpectrum.Checked;
        public bool   IncludeHistogram => _chkHistogram.Checked;
        public bool   IncludeOscillo   => _chkOscillo.Checked;
        public bool   OpenAfter        => _chkOpen.Checked;
        public string GetTitleText()    => _tbTitle.Text.Trim();
        public string GetSubtitleText() => _tbSubtitle.Text.Trim();

        // ── Pré-sélection des onglets disponibles ─────────────────────────────
        public void SetAvailableTabs(bool hasSpectrum, bool hasHistogram, bool hasOscillo)
        {
            _chkSpectrum .Enabled = hasSpectrum;
            _chkSpectrum .Checked = hasSpectrum;
            _chkHistogram.Enabled = hasHistogram;
            _chkHistogram.Checked = hasHistogram;
            _chkOscillo  .Enabled = hasOscillo;
            _chkOscillo  .Checked = hasOscillo;
            UpdateValidation();
        }

        // ══════════════════════════════════════════════════════════════════════
        public ExportPdfDialog()
        {
            Text            = "Export to PDF";
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            ShowInTaskbar   = false;
            ClientSize      = new Size(500, 330);

            var layout = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 3,
                RowCount    = 9,
                Padding     = new Padding(12, 10, 12, 10),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 75));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));  // 0 file
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));   // 1 spacer
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));  // 2 pages
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));   // 3 spacer
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));  // 4 scale + open
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));   // 5 spacer
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));  // 6 title
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));  // 7 subtitle
            layout.RowStyles.Add(new RowStyle(SizeType.Percent,  100)); // 8 buttons + validation

            // ── Row 0 : Fichier PDF ───────────────────────────────────────────
            layout.Controls.Add(MkLabel("File :"), 0, 0);
            _tbPath.Dock = DockStyle.Fill;
            _tbPath.Text = Path.Combine(_lastFolder,
                $"AudioHexViewer_AllTabs_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            layout.Controls.Add(_tbPath, 1, 0);

            _btnBrowse.Text  = "Browse…";
            _btnBrowse.Dock  = DockStyle.Fill;
            _btnBrowse.Click += (_, _) => Browse();
            layout.Controls.Add(_btnBrowse, 2, 0);

            // ── Row 2 : Sélection des pages ───────────────────────────────────
            layout.Controls.Add(MkLabel("Pages :"), 0, 2);

            var pagesRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, Padding = new Padding(0),
            };

            _chkSpectrum .Text = "Spectrum";  _chkSpectrum .AutoSize = true;
            _chkHistogram.Text = "Histogram"; _chkHistogram.AutoSize = true;
            _chkOscillo  .Text = "Oscilloscope"; _chkOscillo.AutoSize = true;

            // Padding entre les cases
            _chkSpectrum .Margin = new Padding(0, 4, 14, 0);
            _chkHistogram.Margin = new Padding(0, 4, 14, 0);
            _chkOscillo  .Margin = new Padding(0, 4,  0, 0);

            _chkSpectrum .CheckedChanged += (_, _) => UpdateValidation();
            _chkHistogram.CheckedChanged += (_, _) => UpdateValidation();
            _chkOscillo  .CheckedChanged += (_, _) => UpdateValidation();

            pagesRow.Controls.Add(_chkSpectrum);
            pagesRow.Controls.Add(_chkHistogram);
            pagesRow.Controls.Add(_chkOscillo);
            layout.Controls.Add(pagesRow, 1, 2);
            layout.SetColumnSpan(pagesRow, 2);

            // ── Row 4 : Scale + Open after ────────────────────────────────────
            layout.Controls.Add(MkLabel("Scale :"), 0, 4);

            var scaleRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, Padding = new Padding(0),
            };
            _nudScale.Minimum = 1; _nudScale.Maximum = 4; _nudScale.Value = 2;
            _nudScale.Width   = 55; _nudScale.Height = 24;
            var scaleHint = new Label
            {
                Text = "  (×2 recommended)", AutoSize = true,
                ForeColor = SystemColors.GrayText, Font = new Font("Segoe UI", 7.5f),
                Padding = new Padding(0, 4, 0, 0),
            };
            scaleRow.Controls.Add(_nudScale);
            scaleRow.Controls.Add(scaleHint);
            layout.Controls.Add(scaleRow, 1, 4);

            _chkOpen.Text = "Open after export"; _chkOpen.AutoSize = true;
            _chkOpen.Dock = DockStyle.Fill; _chkOpen.TextAlign = ContentAlignment.MiddleCenter;
            layout.Controls.Add(_chkOpen, 2, 4);

            // ── Row 6 : Titre ─────────────────────────────────────────────────
            layout.Controls.Add(MkLabel("Title :"), 0, 6);
            _tbTitle.Dock        = DockStyle.Fill;
            _tbTitle.MaxLength   = 100;
            _tbTitle.PlaceholderText = "Document title  (shown on each page)";
            layout.Controls.Add(_tbTitle, 1, 6);
            layout.SetColumnSpan(_tbTitle, 2);

            // ── Row 7 : Sous-titre ────────────────────────────────────────────
            layout.Controls.Add(MkLabel("Notes :"), 0, 7);
            _tbSubtitle.Dock        = DockStyle.Fill;
            _tbSubtitle.MaxLength   = 80;
            _tbSubtitle.PlaceholderText = "Optional sub-title / notes";
            layout.Controls.Add(_tbSubtitle, 1, 7);
            layout.SetColumnSpan(_tbSubtitle, 2);

            // ── Row 8 : Boutons + message validation ──────────────────────────
            var bottomRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                Margin = new Padding(0), Padding = new Padding(0, 4, 0, 0),
            };
            bottomRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bottomRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));

            _lblValidation.Dock      = DockStyle.Fill;
            _lblValidation.ForeColor = Color.Firebrick;
            _lblValidation.Font      = new Font("Segoe UI", 7.5f);
            _lblValidation.TextAlign = ContentAlignment.MiddleLeft;
            bottomRow.Controls.Add(_lblValidation, 0, 0);

            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0),
            };
            _btnOk.Text  = "EXPORT PDF"; _btnOk.Width = 100; _btnOk.Height = 27;
            _btnOk.Click += (_, _) => OnOk();
            _btnCancel.Text = "CANCEL"; _btnCancel.Width = 80; _btnCancel.Height = 27;
            _btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
            btnPanel.Controls.Add(_btnOk);
            btnPanel.Controls.Add(_btnCancel);
            bottomRow.Controls.Add(btnPanel, 1, 0);

            layout.Controls.Add(bottomRow, 0, 8);
            layout.SetColumnSpan(bottomRow, 3);

            Controls.Add(layout);
            AcceptButton = _btnOk;
            CancelButton = _btnCancel;

            UpdateValidation();
        }

        // ══════════════════════════════════════════════════════════════════════
        private void Browse()
        {
            string curPath = _tbPath.Text.Trim();
            string initDir = Directory.Exists(Path.GetDirectoryName(curPath) ?? "")
                             ? Path.GetDirectoryName(curPath)! : _lastFolder;
            string initName = string.IsNullOrWhiteSpace(curPath)
                              ? $"AudioHexViewer_AllTabs_{DateTime.Now:yyyyMMdd_HHmmss}"
                              : Path.GetFileNameWithoutExtension(curPath);

            using var sfd = new SaveFileDialog
            {
                Title = "Export to PDF — choose PDF destination",
                Filter = "PDF document (*.pdf)|*.pdf",
                AddExtension = false,
                InitialDirectory = initDir,
                FileName = initName,
            };
            if (sfd.ShowDialog(this) != DialogResult.OK) return;

            string path = sfd.FileName;
            if (string.IsNullOrWhiteSpace(Path.GetExtension(path))) path += ".pdf";
            _tbPath.Text = path;
            _lastFolder  = Path.GetDirectoryName(path) ?? _lastFolder;
        }

        private void UpdateValidation()
        {
            bool anyPage = _chkSpectrum.Checked || _chkHistogram.Checked || _chkOscillo.Checked;
            _btnOk.Enabled     = anyPage;
            _lblValidation.Text = anyPage ? "" : "Select at least one page.";
        }

        private void OnOk()
        {
            if (string.IsNullOrWhiteSpace(FilePath))
            {
                MessageBox.Show(this, "Please choose an output file.", "Export to PDF",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string? dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                try   { Directory.CreateDirectory(dir); }
                catch { MessageBox.Show(this, $"Cannot create folder:\n{dir}", "Export to PDF",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            }
            if (!string.IsNullOrWhiteSpace(dir)) _lastFolder = dir;
            DialogResult = DialogResult.OK;
            Close();
        }

        private static Label MkLabel(string text) => new()
        {
            Text = text, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill,
        };
        /// <summary>Ouvre le fichier avec l'application par défaut du système.</summary>
        public static void TryOpenFile(string filePath)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true }); }
            catch { /* viewer non disponible */ }
        }

    }
}
