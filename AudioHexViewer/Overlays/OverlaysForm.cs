using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace AudioHexViewer
{
    internal sealed partial class OverlaysForm : Form
    {
        private readonly SpectrumControl _owner;
        private readonly List<OverlayEntry> _entries = new List<OverlayEntry>();
        private bool _isRefreshing;

        public OverlaysForm(SpectrumControl owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            InitializeComponent();
            RefreshList();
        }

        private void RefreshList()
        {
            _isRefreshing = true;
            try
            {
                checkedListOverlays.Items.Clear();
                _entries.Clear();

                var entries = _owner.OverlayLibraryGetEntries();

                // On liste tout (Spectrum + Bode). L'affichage réel dépendra du Kind et de la vue active.
                _entries.AddRange(entries);

                foreach (var e in _entries)
                {
                    // Affichage minimal: juste le nom (comme ta spec)
                    checkedListOverlays.Items.Add(e.Name, isChecked: true);
                }
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        private OverlayEntry? GetSelectedEntry()
        {
            int idx = checkedListOverlays.SelectedIndex;
            if (idx < 0 || idx >= _entries.Count) return null;
            return _entries[idx];
        }

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
                ofd.Filter = "Overlay (*.overlay.json)|*.overlay.json|JSON (*.json)|*.json|All files (*.*)|*.*";
                ofd.Title = "Importer un overlay";

                if (ofd.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    _owner.OverlayLibraryImport(ofd.FileName);
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
            var entry = GetSelectedEntry();
            if (entry == null) return;

            using (var sfd = new SaveFileDialog())
            {
                // Nom par défaut: <NomOverlay>.overlay.json
                var baseName = MakeSafeFileName(entry.Name);
                sfd.FileName = baseName + ".overlay.json";
                sfd.Filter = "Overlay (*.overlay.json)|*.overlay.json|JSON (*.json)|*.json|All files (*.*)|*.*";
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
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            var entry = GetSelectedEntry();
            if (entry == null) return;

            var res = MessageBox.Show(
                this,
                $"Supprimer définitivement l'overlay \"{entry.Name}\" ?\r\n(Ceci efface le fichier de la bibliothèque.)",
                "Supprimer overlay",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (res != DialogResult.Yes)
                return;

            try
            {
                _owner.OverlayLibraryDelete(entry.FilePath);
                RefreshList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Suppression overlay", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void checkedListOverlays_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_isRefreshing) return;

            // Important: ItemCheck se déclenche avant que l'état soit appliqué
            BeginInvoke((Action)(() =>
            {
                if (e.Index < 0 || e.Index >= _entries.Count) return;
                var entry = _entries[e.Index];
                bool visible = checkedListOverlays.GetItemChecked(e.Index);

                _owner.OverlayLibrarySetVisible(entry.FilePath, visible);
            }));
        }

        private void checkedListOverlays_DoubleClick(object sender, EventArgs e)
        {
            var entry = GetSelectedEntry();
            if (entry == null) return;

            // Double-clic = renommer
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

                    cancel.Text = "Annuler";
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
