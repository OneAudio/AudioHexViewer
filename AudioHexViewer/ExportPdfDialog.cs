using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AudioHexViewer
{
    internal sealed class ExportPdfDialog : Form
    {
        private ToolTip? _contextTip;

        private readonly TextBox       _tbPath      = new();
        private readonly Button        _btnBrowse   = new() { Text = "Browse…", Width = 72, Height = 23 };
        private readonly CheckBox      _chkHisto    = new() { Text = "Histogram",    AutoSize = true };
        private readonly CheckBox      _chkSpec     = new() { Text = "Spectrum",     AutoSize = true };
        private readonly CheckBox      _chkScope    = new() { Text = "Oscilloscope", AutoSize = true };
        private readonly NumericUpDown _nudScale    = new();
        private readonly CheckBox      _chkOpen     = new() { Text = "Open", AutoSize = true };
        private readonly TextBox       _tbTitle     = new();
        private readonly TextBox       _tbNotes     = new();
        private readonly Button        _btnOk       = new() { Text = "EXPORT PDF", Width = 96, Height = 26 };
        private readonly Button        _btnCancel   = new() { Text = "CANCEL",     Width = 80, Height = 26 };

        private static string _lastFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        public string FilePath         => _tbPath.Text.Trim();
        public int    ExpScale         => (int)_nudScale.Value;
        public bool   IncludeHistogram => _chkHisto.Checked;
        public bool   IncludeSpectrum  => _chkSpec.Checked;
        public bool   IncludeOscillo   => _chkScope.Checked;
        public bool   OpenAfter        => _chkOpen.Checked;
        public string GetTitleText()    => _tbTitle.Text.Trim();
        public string GetSubtitleText() => _tbNotes.Text.Trim();

        public void SetAvailableTabs(bool hasSpectrum, bool hasHistogram, bool hasOscillo)
        {
            _chkSpec .Enabled = hasSpectrum;  _chkSpec .Checked = hasSpectrum;
            _chkHisto.Enabled = hasHistogram; _chkHisto.Checked = hasHistogram;
            _chkScope.Enabled = hasOscillo;   _chkScope.Checked = hasOscillo;
            RefreshName(); UpdateOk();
        }

        public ExportPdfDialog()
        {
            Text            = "Export to PDF";
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            ClientSize  = new Size(460, 222);
            Font        = new Font("Segoe UI", 9f);

            const int LX = 8, CX = 72, RW = 310, BY = 184;

            // Row 1 — File
            Controls.Add(Lbl("File :", LX, 16));
            _tbPath.SetBounds(CX, 12, RW, 23);
            _btnBrowse.SetBounds(CX + RW + 4, 12, 72, 23);
            _btnBrowse.Click += (_, _) => Browse();
            Controls.Add(_tbPath); Controls.Add(_btnBrowse);

            // Row 2 — Pages
            Controls.Add(Lbl("Pages :", LX, 48));
            var pagesFlow = new FlowLayoutPanel
            {
                Left = CX, Top = 44, Width = 360, Height = 22,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false
            };
            foreach (var c in new[] { _chkHisto, _chkSpec, _chkScope })
                c.Margin = new Padding(0, 2, 14, 0);
            _chkHisto.CheckedChanged += (_, _) => { RefreshName(); UpdateOk(); };
            _chkSpec .CheckedChanged += (_, _) => { RefreshName(); UpdateOk(); };
            _chkScope.CheckedChanged += (_, _) => { RefreshName(); UpdateOk(); };
            pagesFlow.Controls.AddRange(new Control[] { _chkHisto, _chkSpec, _chkScope });
            Controls.Add(pagesFlow);

            // Row 3 — Scale + Open
            Controls.Add(Lbl("Scale :", LX, 80));
            _nudScale.SetBounds(CX, 76, 52, 23);
            _nudScale.Minimum = 1; _nudScale.Maximum = 4; _nudScale.Value = 2;
            var hint = new Label { Text = "  (×2 recommended)", AutoSize = true,
                ForeColor = SystemColors.GrayText, Font = new Font("Segoe UI", 7.5f),
                Left = CX + 58, Top = 79 };
            _chkOpen.SetBounds(CX + RW - 10, 79, 60, 20);
            Controls.Add(_nudScale); Controls.Add(hint); Controls.Add(_chkOpen);

            // Row 4 — Title
            Controls.Add(Lbl("Title :", LX, 114));
            _tbTitle.SetBounds(CX, 110, RW + 76, 23);
            _tbTitle.PlaceholderText = "Document title (shown on each page)";
            _tbTitle.MaxLength = 100;
            Controls.Add(_tbTitle);

            // Row 5 — Notes
            Controls.Add(Lbl("Notes :", LX, 148));
            _tbNotes.SetBounds(CX, 144, RW + 76, 23);
            _tbNotes.PlaceholderText = "Optional sub-title / notes";
            _tbNotes.MaxLength = 80;
            Controls.Add(_tbNotes);

            // Row 6 — Buttons
            _btnCancel.SetBounds(ClientSize.Width - 88,        BY, 80, 26);
            _btnOk    .SetBounds(ClientSize.Width - 88 - 104,  BY, 96, 26);
            _btnOk.FlatStyle = FlatStyle.Flat; _btnOk.FlatAppearance.BorderSize = 0;
            _btnOk.BackColor = Color.FromArgb(0, 122, 204); _btnOk.ForeColor = Color.White;
            _btnOk.Click     += (_, _) => OnOk();
            _btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(_btnOk); Controls.Add(_btnCancel);


            // ── Names for context-sensitive tooltips ──────────────────────────
            _tbPath.Name      = "tbPath";
            _btnBrowse.Name   = "btnBrowse";
            _chkHisto.Name    = "chkExportHisto";
            _chkSpec.Name     = "chkExportSpec";
            _chkScope.Name    = "chkExportScope";
            _nudScale.Name    = "nudScale";
            _chkOpen.Name     = "chkOpen";
            _tbTitle.Name     = "tbTitle";
            _tbNotes.Name     = "tbNotes";
            _btnOk.Name       = "btnExportPdf";
            _btnCancel.Name   = "btnCancel";
            _contextTip = ContextHelpManager.CreateFormToolTip();
            Load += (_, _) => ContextHelpManager.ApplyTooltips(this.Controls, _contextTip);
            AcceptButton = _btnOk; CancelButton = _btnCancel;

            // Init all 3 checked by default
            _chkHisto.Checked = _chkSpec.Checked = _chkScope.Checked = true;
            RefreshName(); UpdateOk();
        }

        private static Label Lbl(string t, int x, int y)
            => new() { Text = t, Left = x, Top = y, AutoSize = true };

        // ── Nommage automatique AHVxxx_XX.pdf ────────────────────────────────
        private void RefreshName()
        {
            // Ordre fixe : Histo → Spec → Scope
            string suffix = (_chkHisto.Checked ? "Histo" : "")
                          + (_chkSpec .Checked ? "Spec"  : "")
                          + (_chkScope.Checked ? "Scope" : "");
            if (suffix == "") { _tbPath.Text = ""; return; }
            string prefix = "AHV" + suffix;
            int    idx    = ExportCsvDialog.NextAvailableIndex(_lastFolder, prefix, "pdf");
            _tbPath.Text  = Path.Combine(_lastFolder, $"{prefix}_{idx:D2}.pdf");
        }

        private void UpdateOk()
        {
            _btnOk.Enabled = _chkHisto.Checked || _chkSpec.Checked || _chkScope.Checked;
        }

        private void Browse()
        {
            using var sfd = new SaveFileDialog
            {
                Title            = "Export to PDF",
                Filter           = "PDF document (*.pdf)|*.pdf",
                InitialDirectory = Path.GetDirectoryName(_tbPath.Text.Trim()) ?? _lastFolder,
                FileName         = Path.GetFileNameWithoutExtension(_tbPath.Text.Trim()),
                DefaultExt       = "pdf"
            };
            if (sfd.ShowDialog(this) != DialogResult.OK) return;
            string path = sfd.FileName;
            if (string.IsNullOrWhiteSpace(Path.GetExtension(path))) path += ".pdf";
            _tbPath.Text = path;
            _lastFolder  = Path.GetDirectoryName(path) ?? _lastFolder;
        }

        private void OnOk()
        {
            if (string.IsNullOrWhiteSpace(FilePath))
            { MessageBox.Show(this, "Please choose an output file.", Text,
                  MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            string? dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                try { Directory.CreateDirectory(dir); } catch { return; }
            if (!string.IsNullOrWhiteSpace(dir)) _lastFolder = dir;
            DialogResult = DialogResult.OK;
            Close();
        }

        public static void TryOpenFile(string filePath)
        {
            try { System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true }); }
            catch { }
        }
    }
}
