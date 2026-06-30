// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;


namespace AudioHexViewer
{
    internal sealed partial class OverlaysForm : Form
    {
        private ToolTip? _contextTip;

        private readonly IOverlayHost _owner;
        // File extension for this host's overlay library (".spec.json" or ".scop.json")
        private string Ext       => _owner.OverlayLibraryGetFileExtension();
        private string ExtGlob   => "*" + Ext;
        private string ExtFilter => $"Overlay ({ExtGlob})|{ExtGlob}|JSON (*.json)|*.json|All files (*.*)|*.*";
        private readonly List<OverlayEntry> _entries = new List<OverlayEntry>();
        private bool _isRefreshing;

        public OverlaysForm(IOverlayHost owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            InitializeComponent();
            _contextTip = ContextHelpManager.CreateFormToolTip();
            Load += (_, _) => ContextHelpManager.ApplyTooltips(this.Controls, _contextTip);
            InitColorIcons();
            RefreshList();

            // Close on Escape
            KeyPreview = true;
            KeyDown   += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
        }

        private void RefreshList()
        {
            _isRefreshing = true;
            try
            {
                listOverlays.BeginUpdate();
                listOverlays.Items.Clear();
                _entries.Clear();

                var entries = _owner.OverlayLibraryGetEntries();
                _entries.AddRange(entries);

                foreach (var e in _entries)
                {
                    // Tag = index dans _entries (stable pendant la durée d’affichage)
                    string key = (e.SlotNumber >= 1 && e.SlotNumber <= OverlayManager.MaxOverlays) ? $"slot{e.SlotNumber}" : "slot0";
                    var item = new ListViewItem(e.Name);
                    item.ImageKey = key;
                    // Colonne "Type"
                    item.SubItems.Add(KindToShortText(e.Kind));
                    // Colonne "SR"
                    item.SubItems.Add(e.SampleRate > 0 ? e.SampleRate.ToString() : "");
                    // Range (fmin...fmax)
                    if (TryGetRangeTextFromOverlayFile(e.FilePath, e.Kind, out var rangeText))
                        item.SubItems.Add(rangeText);
                    else
                        item.SubItems.Add("");
                    // Binding
                    item.Tag = e;
                    
                    item.Checked = _owner.OverlayLibraryGetVisible(e.FilePath);


                    listOverlays.Items.Add(item);

                }
            }
            finally
            {
                listOverlays.EndUpdate();
                _isRefreshing = false;
            }
        }
        //
        /// <summary>
        /// Calcule la plage affichée dans la colonne "Range" du ListView, en fonction du Kind :
        ///   - Spectrum/Bode     : plage de fréquences (FreqHz[])
        ///   - Oscilloscope      : plage temporelle (TimeMs[]), t=0 = trigger
        /// </summary>
        private bool TryGetRangeTextFromOverlayFile(string filePath, OverlayKind kind, out string rangeText)
        {
            rangeText = "";

            try
            {
                var json = OverlaySafety.ReadOverlayJson(filePath);  // ← était File.ReadAllText(filePath)
                var dto = System.Text.Json.JsonSerializer.Deserialize<OverlayFile>(json, new System.Text.Json.JsonSerializerOptions
                {
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                });

                if (dto == null) return false;
                OverlaySafety.CheckBounds(dto);                      // ← ajouter

                if (kind == OverlayKind.Oscilloscope)
                {
                    if (dto.TimeMs == null || dto.TimeMs.Length == 0) return false;

                    double tmin = dto.TimeMs[0];
                    double tmax = dto.TimeMs[dto.TimeMs.Length - 1];
                    if (tmax < tmin)
                    {
                        double mn = dto.TimeMs.Min();
                        double mx = dto.TimeMs.Max();
                        tmin = mn; tmax = mx;
                    }

                    rangeText = FormatTimeRange(tmin, tmax);
                    return true;
                }
                else
                {
                    if (dto.FreqHz == null || dto.FreqHz.Length == 0) return false;

                    double fmin = dto.FreqHz[0];
                    double fmax = dto.FreqHz[dto.FreqHz.Length - 1];
                    if (fmax < fmin)
                    {
                        double mn = dto.FreqHz.Min();
                        double mx = dto.FreqHz.Max();
                        fmin = mn; fmax = mx;
                    }

                    rangeText = FormatRange(fmin, fmax);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        //
        private List<OverlayEntry> GetSelectedEntries()
        {
            var selected = new List<OverlayEntry>();

            foreach (ListViewItem it in listOverlays.SelectedItems)
            {
                if (it.Tag is OverlayEntry e)
                    selected.Add(e);
            }

            return selected;
        }

        private OverlayEntry? GetSingleSelectedEntryOrNull()
        {
            var list = GetSelectedEntries();
            return list.Count == 1 ? list[0] : null;
        }
        //
        private void InitColorIcons()
        {
            imgOverlayColors.Images.Clear();

            // 8 slots (1..8)
            for (int slot = 1; slot <= OverlayManager.MaxOverlays; slot++)
            {
                var c = _owner.OverlayGetSlotColor1Based(slot);
                var bmp = CreateColorDotBitmap(c, 14, 14);
                imgOverlayColors.Images.Add($"slot{slot}", bmp);
            }

            // fallback
            imgOverlayColors.Images.Add("slot0", CreateColorDotBitmap(System.Drawing.Color.Gray, 14, 14));
        }

        private static System.Drawing.Bitmap CreateColorDotBitmap(System.Drawing.Color color, int w, int h)
        {
            var bmp = new System.Drawing.Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            using (var b = new System.Drawing.SolidBrush(color))
            using (var p = new System.Drawing.Pen(System.Drawing.Color.FromArgb(220, 0, 0, 0), 1f))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.Transparent);

                // petit rond centré
                int d = Math.Min(w, h) - 2;
                var rect = new System.Drawing.Rectangle(1, 1, d, d);
                g.FillEllipse(b, rect);
                g.DrawEllipse(p, rect);
            }
            return bmp;
        }

        //
        private void btnCapture_Click(object sender, EventArgs e)
        {
            try
            {
                _owner.OverlayLibraryCaptureActiveView();
                RefreshList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Capture overlay", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = ExtFilter;
                ofd.Title = "Import an overlay";
                ofd.Multiselect = true;

                if (ofd.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    string libraryDir = _owner.OverlayLibraryGetFolderPath();

                    foreach (var file in ofd.FileNames)
                    {
                        string baseName = Path.GetFileName(file);

                        // If importing a generic .json file, force the correct extension
                        if (!baseName.EndsWith(Ext, StringComparison.OrdinalIgnoreCase))
                            baseName = Path.GetFileNameWithoutExtension(baseName) + Ext;

                        string destPath = Path.Combine(libraryDir, baseName);

                        // CAS IMPORTANT: l'utilisateur "importe" un fichier qui est déjà dans le dossier overlays.
                        // Dans ce cas, on ne copie pas, on ne demande pas confirmation.
                        // On veut surtout le "ré-afficher" s'il était masqué (Remove).
                        string srcFull = Path.GetFullPath(file);
                        string dstFull = Path.GetFullPath(destPath);

                        if (string.Equals(srcFull, dstFull, StringComparison.OrdinalIgnoreCase))
                        {
                            _owner.OverlayLibraryUnhideFromList(destPath);
                            continue;
                        }

                        // Si un fichier existe déjà, on demande confirmation avant overwrite
                        if (File.Exists(destPath))
                        {
                            var r = MessageBox.Show(
                                this,
                                $"An overlay named \"{baseName}\" already exists in the library.\r\nDo you want to overwrite it?",
                                "Import overlay",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question);

                            if (r != DialogResult.Yes)
                                continue;
                        }

                        _owner.OverlayLibraryImport(file);
                    }

                    RefreshList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Import overlay", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }



        private void btnExport_Click(object sender, EventArgs e)
        {
            var selection = GetSelectedEntries();
            if (selection.Count == 0) return;

            // Export 1 overlay -> SaveFileDialog (choix fichier)
            if (selection.Count == 1)
            {
                var entry = selection[0];

                using (var sfd = new SaveFileDialog())
                {
                    var baseName = MakeSafeFileName(entry.Name);
                    sfd.FileName = baseName + Ext;
                    sfd.Filter = ExtFilter;
                    sfd.Title = "Exporter l'overlay";

                    if (sfd.ShowDialog(this) != DialogResult.OK)
                        return;

                    try
                    {
                        _owner.OverlayLibraryExport(entry.FilePath, sfd.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, ex.Message, "Export overlay", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                return;
            }

            // Export multiple -> FolderBrowserDialog (choix dossier)
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Choose the overlay export folder";
                if (fbd.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    foreach (var entry in selection)
                    {
                        var baseName = MakeSafeFileName(entry.Name);
                        var target = Path.Combine(fbd.SelectedPath, baseName + Ext);
                        // Eviter d'écraser: si existe, on ajoute (2), (3)...
                        target = GetUniquePath(target);

                        _owner.OverlayLibraryExport(entry.FilePath, target);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Export overlay (multiple)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        //
        private static string GetUniquePath(string desiredPath)
        {
            if (!File.Exists(desiredPath))
                return desiredPath;

            var dir = Path.GetDirectoryName(desiredPath);
            if (string.IsNullOrWhiteSpace(dir))
                return desiredPath;

            var baseName = Path.GetFileNameWithoutExtension(desiredPath);
            var ext = Path.GetExtension(desiredPath);

            for (int i = 2; i < 10000; i++)
            {
                var candidate = Path.Combine(dir, $"{baseName} ({i}){ext}");
                if (!File.Exists(candidate))
                    return candidate;
            }

            return desiredPath;
        }
        //
        private void btnDelete_Click(object sender, EventArgs e)
        {
            var selection = GetSelectedEntries();
            if (selection.Count == 0) return;

            string msg = selection.Count == 1
                ? $"Permanently delete the overlay \"{selection[0].Name}\"?\r\n(This removes the file from the library.)"
                : $"Permanently delete {selection.Count} selected overlays?\r\n(This removes the files from the library.)";

            var res = MessageBox.Show(this, msg, "Supprimer overlay(s)", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (res != DialogResult.Yes)
                return;

            try
            {
                foreach (var entry in selection)
                    _owner.OverlayLibraryDelete(entry.FilePath);

                RefreshList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Suppression overlay", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //
        private void btnRemove_Click(object sender, EventArgs e)
        {
            var selection = GetSelectedEntries();
            if (selection.Count == 0) return;         

            try
            {
                foreach (var entry in selection)
                    _owner.OverlayLibraryRemoveFromList(entry.FilePath);

                RefreshList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Remove overlay", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //
        private void btnOpenFolder_Click(object sender, EventArgs e)
        {
            try
            {
                var dir = _owner.OverlayLibraryGetFolderPath();
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                Process.Start("explorer.exe", dir);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Overlay folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //
        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void listOverlays_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (_isRefreshing) return;

            if (e.Item.Tag is OverlayEntry entry)
            {
                _owner.OverlayLibrarySetVisible(entry.FilePath, e.Item.Checked);
            }
        }


        private void listOverlays_DoubleClick(object sender, EventArgs e)
        {
            var entry = GetSingleSelectedEntryOrNull();
            if (entry == null) return;

            string? newName = SimpleInputDialog.Show(this, "Renommer overlay", "Nom :", entry.Name);
            if (string.IsNullOrWhiteSpace(newName)) return;

            try
            {
                _owner.OverlayLibraryRename(entry.FilePath, newName.Trim());
                RefreshList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Renommer overlay", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //
        private static string FormatFreq(double hz)
        {
            if (hz >= 1000.0)
                return (hz / 1000.0).ToString(hz >= 10000.0 ? "0" : "0.#") + "kHz";
            return hz.ToString(hz >= 100.0 ? "0" : "0.#") + "Hz";
        }

        private static string FormatRange(double fmin, double fmax)
        {
            if (fmin < 0) fmin = 0;
            if (fmax < 0) fmax = 0;
            return $"{FormatFreq(fmin)}...{FormatFreq(fmax)}";
        }

        //
        /// <summary>Formate une durée en ms (signée), avec passage automatique en µs/s selon l'amplitude.</summary>
        private static string FormatTimeMs(double ms)
        {
            double abs = Math.Abs(ms);
            string sign = ms < 0 ? "-" : "";

            if (abs >= 1000.0)
                return sign + (abs / 1000.0).ToString(abs >= 10000.0 ? "0" : "0.##") + "s";
            if (abs >= 1.0)
                return sign + abs.ToString(abs >= 100.0 ? "0" : abs >= 10.0 ? "0.#" : "0.##") + "ms";
            if (abs == 0.0)
                return "0ms";
            return sign + (abs * 1000.0).ToString(abs * 1000.0 >= 100.0 ? "0" : "0.#") + "\u00b5s";
        }

        private static string FormatTimeRange(double tMinMs, double tMaxMs)
            => $"{FormatTimeMs(tMinMs)}...{FormatTimeMs(tMaxMs)}";

        //
        private static string KindToShortText(OverlayKind kind)
        {
            switch (kind)
            {
                case OverlayKind.Spectrum: return "Spectrum";
                case OverlayKind.Bode: return "Bode";
                case OverlayKind.Oscilloscope: return "Oscillo";
                default: return kind.ToString();
            }
        }
        //
        private static string MakeSafeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var safe = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray()).Trim();
            return string.IsNullOrWhiteSpace(safe) ? "Overlay" : safe;
        }

        // Petit dialogue d'entrée texte (évite une dépendance Microsoft.VisualBasic)
        private static class SimpleInputDialog
        {
            public static string? Show(IWin32Window owner, string title, string label, string initial)
            {
                using (var f = new Form())
                using (var tb = new TextBox())
                using (var lbl = new Label())
                using (var ok = new Button())
                using (var cancel = new Button())
                {
                    f.Text = title;
                    f.FormBorderStyle = FormBorderStyle.FixedDialog;
                    f.MinimizeBox = false;
                    f.MaximizeBox = false;
                    f.StartPosition = FormStartPosition.CenterParent;
                    f.Width = 420;
                    f.Height = 150;

                    lbl.Text = label;
                    lbl.Left = 12;
                    lbl.Top = 12;
                    lbl.Width = 380;

                    tb.Left = 12;
                    tb.Top = 36;
                    tb.Width = 380;
                    tb.Text = initial ?? "";

                    ok.Text = "OK";
                    ok.Left = 232;
                    ok.Top = 70;
                    ok.Width = 75;
                    ok.DialogResult = DialogResult.OK;

                    cancel.Text = "Cancel";
                    cancel.Left = 317;
                    cancel.Top = 70;
                    cancel.Width = 75;
                    cancel.DialogResult = DialogResult.Cancel;

                    f.Controls.Add(lbl);
                    f.Controls.Add(tb);
                    f.Controls.Add(ok);
                    f.Controls.Add(cancel);

                    f.AcceptButton = ok;
                    f.CancelButton = cancel;

                    return f.ShowDialog(owner) == DialogResult.OK ? tb.Text : null;
                }
            }
        }
    }
}
