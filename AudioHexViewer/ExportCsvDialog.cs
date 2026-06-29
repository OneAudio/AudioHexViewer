// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AudioHexViewer
{
    internal sealed class ExportCsvDialog : Form
    {
        private ToolTip? _contextTip;

        private readonly RadioButton _rbHisto = new() { Text = "Histogram",    AutoSize = true };
        private readonly RadioButton _rbSpec  = new() { Text = "Spectrum",     AutoSize = true };
        private readonly RadioButton _rbScope = new() { Text = "Oscilloscope", AutoSize = true };
        private readonly TextBox     _tbPath  = new();
        private readonly Button      _btnBrowse = new() { Text = "Browse…", Width = 72, Height = 23 };
        private readonly ComboBox    _cboSep     = new() { Width = 118, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox    _cboDecimal = new() { Width = 80,  DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly CheckBox    _chkHeader  = new() { Text = "Include header", AutoSize = true, Checked = true };
        private readonly Button      _btnOk     = new() { Text = "EXPORT", Width = 80, Height = 26 };
        private readonly Button      _btnCancel = new() { Text = "CANCEL", Width = 80, Height = 26 };

        private static string _lastFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private static int    _lastSepIdx = 1;
        private static int    _lastDecIdx = 0;   // 0=point, 1=virgule
        private static int    _lastTabIdx = 0;

        public string FilePath      => _tbPath.Text.Trim();
        public bool   IncludeHeader => _chkHeader.Checked;
        public char   Separator       => _cboSep.SelectedIndex switch { 0 => ',', 2 => '\t', _ => ';' };
        public char   DecimalSeparator => _cboDecimal.SelectedIndex == 1 ? ',' : '.';
        public string SelectedTab   => _rbHisto.Checked ? "Histogram"
                                     : _rbSpec.Checked  ? "Spectrum" : "Oscilloscope";

        public void PreSelectTab(string tab)
        {
            _rbHisto.Checked = tab == "Histogram";
            _rbSpec .Checked = tab == "Spectrum";
            _rbScope.Checked = tab == "Oscilloscope";
        }

        public ExportCsvDialog()
        {
            Text            = "Export data to CSV";
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            ClientSize  = new Size(460, 212);
            Font        = new Font("Segoe UI", 9f);

            const int LX = 8, CX = 80, RW = 295, BY = 174;

            // Row 1 — Source
            Controls.Add(Lbl("Source :", LX, 16));
            var srcFlow = new FlowLayoutPanel
            {
                Left = CX, Top = 12, Width = 360, Height = 22,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false
            };
            foreach (var rb in new[] { _rbHisto, _rbSpec, _rbScope })
                rb.Margin = new Padding(0, 2, 14, 0);
            srcFlow.Controls.AddRange(new Control[] { _rbHisto, _rbSpec, _rbScope });
            Controls.Add(srcFlow);

            // Row 2 — File
            Controls.Add(Lbl("File :", LX, 46));
            _tbPath.SetBounds(CX, 42, RW, 23);
            _btnBrowse.SetBounds(CX + RW + 4, 42, 72, 23);
            _btnBrowse.Click += (_, _) => Browse();
            Controls.Add(_tbPath); Controls.Add(_btnBrowse);

            // Row 3 — Separator
            Controls.Add(Lbl("Separator :", LX, 78));
            _cboSep.SetBounds(CX, 74, 118, 23);
            _cboSep.Items.AddRange(new object[] { "Comma  ,", "Semicolon  ;", "Tab  \\t" });
            _cboSep.SelectedIndex = _lastSepIdx;
            // Auto-force ; si décimal = virgule
            _cboSep.SelectedIndexChanged += (_, _) => OnSepChanged();
            Controls.Add(_cboSep);

            // Row 4 — Decimal separator + Include header
            Controls.Add(Lbl("Decimal :", LX, 110));
            _cboDecimal.SetBounds(CX, 106, 80, 23);
            _cboDecimal.Items.AddRange(new object[] { "Point  .", "Comma  ," });
            _cboDecimal.SelectedIndex = _lastDecIdx;
            _cboDecimal.SelectedIndexChanged += (_, _) => OnDecimalChanged();
            _chkHeader.SetBounds(CX + 90, 108, 160, 20);
            Controls.Add(_cboDecimal); Controls.Add(_chkHeader);

            // Row 4 — Buttons
            _btnCancel.SetBounds(ClientSize.Width - 88, BY, 80, 26);
            _btnOk    .SetBounds(ClientSize.Width - 172, BY, 80, 26);
            _btnOk.FlatStyle = FlatStyle.Flat; _btnOk.FlatAppearance.BorderSize = 0;
            _btnOk.BackColor = Color.FromArgb(0, 122, 204); _btnOk.ForeColor = Color.White;
            _btnOk.Click     += (_, _) => OnOk();
            _btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(_btnOk); Controls.Add(_btnCancel);


            // ── Names for context-sensitive tooltips ──────────────────────────
            _rbHisto.Name    = "rbExportHisto";
            _rbSpec.Name     = "rbExportSpec";
            _rbScope.Name    = "rbExportScope";
            _tbPath.Name     = "tbPath";
            _btnBrowse.Name  = "btnBrowse";
            _cboSep.Name     = "cboSep";
            _cboDecimal.Name = "cboDecimal";
            _chkHeader.Name  = "chkHeader";
            _btnOk.Name      = "btnExportCsv";
            _btnCancel.Name  = "btnCancel";
            _contextTip = ContextHelpManager.CreateFormToolTip();
            Load += (_, _) => ContextHelpManager.ApplyTooltips(this.Controls, _contextTip);
            AcceptButton = _btnOk; CancelButton = _btnCancel;

            // Restore + wire source change
            _rbHisto.Checked = _lastTabIdx == 0;
            _rbSpec .Checked = _lastTabIdx == 1;
            _rbScope.Checked = _lastTabIdx == 2;
            _rbHisto.CheckedChanged += (_, _) => { if (_rbHisto.Checked) RefreshName(); };
            _rbSpec .CheckedChanged += (_, _) => { if (_rbSpec .Checked) RefreshName(); };
            _rbScope.CheckedChanged += (_, _) => { if (_rbScope.Checked) RefreshName(); };
            RefreshName();
        }

        private static Label Lbl(string t, int x, int y)
            => new() { Text = t, Left = x, Top = y, AutoSize = true };

        private void OnDecimalChanged()
        {
            // Si décimal = virgule, le séparateur de colonnes doit être ;
            if (_cboDecimal.SelectedIndex == 1 && _cboSep.SelectedIndex != 1)
                _cboSep.SelectedIndex = 1;   // force ;
        }

        private void OnSepChanged()
        {
            // Si on passe à virgule comme séparateur, force décimal = point
            if (_cboSep.SelectedIndex == 0 && _cboDecimal.SelectedIndex == 1)
                _cboDecimal.SelectedIndex = 0;
        }

        private void RefreshName()
        {
            string prefix = _rbHisto.Checked ? "AHVHisto"
                          : _rbSpec .Checked ? "AHVSpec" : "AHVScope";
            int idx = NextAvailableIndex(_lastFolder, prefix, "csv");
            _tbPath.Text = Path.Combine(_lastFolder, $"{prefix}_{idx:D2}.csv");
        }

        private void Browse()
        {
            using var sfd = new SaveFileDialog
            {
                Title            = "Export CSV",
                Filter           = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                InitialDirectory = Path.GetDirectoryName(_tbPath.Text) ?? _lastFolder,
                FileName         = Path.GetFileNameWithoutExtension(_tbPath.Text),
                DefaultExt       = "csv"
            };
            if (sfd.ShowDialog(this) != DialogResult.OK) return;
            _tbPath.Text = sfd.FileName;
            _lastFolder  = Path.GetDirectoryName(sfd.FileName) ?? _lastFolder;
        }

        private void OnOk()
        {
            if (string.IsNullOrWhiteSpace(FilePath))
            { MessageBox.Show(this, "Please choose a file.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            _lastFolder = Path.GetDirectoryName(FilePath) ?? _lastFolder;
            _lastSepIdx = _cboSep.SelectedIndex;
            _lastDecIdx = _cboDecimal.SelectedIndex;
            _lastTabIdx = _rbHisto.Checked ? 0 : _rbSpec.Checked ? 1 : 2;
            DialogResult = DialogResult.OK;
            Close();
        }

        // Utilisé aussi par ExportPdfDialog
        internal static int NextAvailableIndex(string folder, string prefix, string ext)
        {
            for (int i = 0; i <= 99; i++)
                if (!File.Exists(Path.Combine(folder, $"{prefix}_{i:D2}.{ext}"))) return i;
            return 0;
        }
    }
}
