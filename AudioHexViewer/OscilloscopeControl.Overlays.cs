// ═══════════════════════════════════════════════════════════════════════════
// OscilloscopeControl.Overlays.cs  —  Bibliothèque d'overlays Oscilloscope
//
// Mirroir de SpectrumControl.Overlays.cs, mais pour des traces temporelles
// (Kind=Oscilloscope). Bibliothèque/dossier séparés de ceux du Spectrum.
// ═══════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace AudioHexViewer
{
    internal partial class OscilloscopeControl : IOverlayHost
    {
        // ===== Overlays V3 (bibliothèque interne, dédiée Oscilloscope) =====
        private readonly OverlayLibrary _overlayLibrary = new OverlayLibrary(GetOscilloscopeOverlayDir());

        private OverlaysForm? _overlaysForm;

        private sealed class CachedOverlay
        {
            public OverlayEntry Entry;
            public OverlayFile Data;
            public bool Visible;

            public CachedOverlay(OverlayEntry entry, OverlayFile data, bool visible)
            {
                Entry = entry;
                Data = data;
                Visible = visible;
            }
        }

        private readonly List<CachedOverlay> _overlayCache = new List<CachedOverlay>();

        // Json options identiques à OverlayLibrary (enum en string)
        private readonly JsonSerializerOptions _overlayJsonOptions = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        };

        private static string GetOscilloscopeOverlayDir()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "AudioHexViewer", "Overlays", "Oscilloscope");
        }

        // Couleur d'un overlay selon son index de slot (0-based), palette partagée avec le Spectrum
        private Color GetOverlaySlotColor(int slotIndex)
        {
            var arr = _colors.OverlaySlotArgb;
            if (arr != null && slotIndex >= 0 && slotIndex < arr.Length)
                return NegApply(arr[slotIndex], _colors.NegativeOverlays);
            // fallback compat
            return NegApply(_colors.OverlayArgb, _colors.NegativeOverlays);
        }

        public Color OverlayGetSlotColor1Based(int slotNumber)
        {
            // slotNumber attendu: 1..8
            int idx0 = slotNumber - 1;
            if (idx0 < 0 || idx0 >= OverlayManager.MaxOverlays)
                idx0 = 0;

            return GetOverlaySlotColor(idx0);
        }

        public string OverlayLibraryGetFolderPath() => _overlayLibrary.LibraryDirectory;

        private int OverlayLibraryGetNextFreeSlotNumber()
        {
            bool[] used = new bool[OverlayManager.MaxOverlays + 1]; // index 1..8

            foreach (var c in _overlayCache)
            {
                int s = c.Data != null ? c.Data.SlotNumber : 0;
                if (s >= 1 && s <= OverlayManager.MaxOverlays)
                    used[s] = true;
            }

            for (int s = 1; s <= OverlayManager.MaxOverlays; s++)
                if (!used[s]) return s;

            throw new InvalidOperationException($"All overlay slots (1..{OverlayManager.MaxOverlays}) are already in use.");
        }

        private OverlayFile ReadOverlayFromDisk(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var dto = JsonSerializer.Deserialize<OverlayFile>(json, _overlayJsonOptions);
            if (dto == null)
                throw new InvalidDataException("Overlay invalide: JSON vide ou non conforme.");

            if (dto.Version != OverlayFile.CurrentVersion)
                throw new InvalidDataException($"Overlay invalide: Version={dto.Version}, attendu V{OverlayFile.CurrentVersion}.");

            return dto;
        }

        private void OverlayLibraryReloadCache()
        {
            _overlayCache.Clear();

            var entries = _overlayLibrary.LoadAll();
            foreach (var e in entries)
            {
                try
                {
                    var data = ReadOverlayFromDisk(e.FilePath);
                    bool visible = _overlayLibrary.GetOverlayVisible(e.FilePath, defaultValue: true);
                    _overlayCache.Add(new CachedOverlay(e, data, visible));
                }
                catch
                {
                    // ignore: overlay corrompu / non lisible
                }
            }

            _overlayCache.Sort((a, b) => b.Entry.CreatedUtc.CompareTo(a.Entry.CreatedUtc));
        }

        public List<OverlayEntry> OverlayLibraryGetEntries()
        {
            return _overlayCache.Select(c => c.Entry).ToList();
        }

        public void OverlayLibrarySetVisible(string filePath, bool visible)
        {
            var c = _overlayCache.FirstOrDefault(x => string.Equals(x.Entry.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            if (c != null)
            {
                c.Visible = visible;
                _overlayLibrary.SetOverlayVisible(c.Entry.FilePath, visible);
                pnlScope.Invalidate();
            }
        }

        public bool OverlayLibraryGetVisible(string libraryFilePath)
        {
            return _overlayLibrary.GetOverlayVisible(libraryFilePath, defaultValue: true);
        }

        public OverlayEntry OverlayLibraryImport(string sourcePath)
        {
            var entry = _overlayLibrary.ImportFromFile(sourcePath);
            OverlayLibraryReloadCache();
            _overlayLibrary.SetOverlayVisible(entry.FilePath, true);
            pnlScope.Invalidate();
            return entry;
        }

        public void OverlayLibraryExport(string libraryFilePath, string targetPath)
        {
            var c = _overlayCache.FirstOrDefault(x => string.Equals(x.Entry.FilePath, libraryFilePath, StringComparison.OrdinalIgnoreCase));
            if (c == null) throw new FileNotFoundException("Overlay introuvable dans le cache.", libraryFilePath);

            _overlayLibrary.ExportToFile(c.Entry, targetPath);
        }

        public void OverlayLibraryDelete(string libraryFilePath)
        {
            var c = _overlayCache.FirstOrDefault(x => string.Equals(x.Entry.FilePath, libraryFilePath, StringComparison.OrdinalIgnoreCase));
            if (c == null) return;

            _overlayLibrary.Delete(c.Entry);
            _overlayLibrary.RemoveOverlayVisibility(libraryFilePath);

            OverlayLibraryReloadCache();
            pnlScope.Invalidate();
        }

        public OverlayEntry OverlayLibraryRename(string libraryFilePath, string newName)
        {
            var c = _overlayCache.FirstOrDefault(x => string.Equals(x.Entry.FilePath, libraryFilePath, StringComparison.OrdinalIgnoreCase));
            if (c == null) throw new FileNotFoundException("Overlay introuvable dans le cache.", libraryFilePath);

            var updated = _overlayLibrary.Rename(c.Entry, newName);

            OverlayLibraryReloadCache();
            pnlScope.Invalidate();

            return updated;
        }

        public void OverlayLibraryRemoveFromList(string libraryFilePath)
        {
            // On masque d'abord l'overlay dans l'affichage live
            OverlayLibrarySetVisible(libraryFilePath, false);

            _overlayLibrary.HideFromList(libraryFilePath);
            OverlayLibraryReloadCache();
            pnlScope.Invalidate();
        }

        public void OverlayLibraryUnhideFromList(string libraryFilePath)
        {
            _overlayLibrary.UnhideFromList(libraryFilePath);
            OverlayLibraryReloadCache();
            pnlScope.Invalidate();
        }

        /// <summary>
        /// Capture le buffer complet actuellement en mémoire (_capL/_capR, comme affiché
        /// après le dernier Stop) et l'ajoute à la bibliothèque d'overlays Oscilloscope.
        /// </summary>
        public OverlayEntry OverlayLibraryCaptureActiveView()
        {
            if (_capL == null && _capR == null)
                throw new InvalidOperationException("No capture available (start then stop an acquisition first).");
            if (_capSR <= 0)
                throw new InvalidOperationException("Sample rate de capture inconnu.");

            int ch = GetCh();
            if (ch < 0)
                throw new InvalidOperationException("No active channel (Left/Right) to capture.");

            int n = (_capL?.Length ?? _capR!.Length);
            if (_capL != null) n = Math.Min(n, _capL.Length);
            if (_capR != null) n = Math.Min(n, _capR.Length);
            if (n < 2)
                throw new InvalidOperationException("Capture trop courte.");

            int slot = OverlayLibraryGetNextFreeSlotNumber();
            string defaultName = $"Overlay_{slot}";

            // Axe temps explicite (ms), t=0 = position du trigger (ou PreTrig si pas de trigger détecté)
            int pre = (int)(n * PreTrig);
            int tref = _capTrigIdx >= 0 ? _capTrigIdx : pre;
            double srMs = 1000.0 / _capSR;
            var timeMs = new double[n];
            for (int i = 0; i < n; i++)
                timeMs[i] = (i - tref) * srMs;

            int td = Math.Clamp(cmbTimediv.SelectedIndex, 0, TimeDivMs.Length - 1);

            var dto = new OverlayFile
            {
                Version = OverlayFile.CurrentVersion,
                Name = defaultName, // sera renommé par la bibliothèque
                SlotNumber = slot,
                CreatedUtc = DateTime.UtcNow,
                Kind = OverlayKind.Oscilloscope,

                SampleRate = _capSR,
                FftSize = 1,             // non pertinent pour Oscilloscope (champ requis >0 par compat)
                Window = "Rectangular",  // idem, non pertinent

                TimeMs = timeMs,
                LeftV = (ch == 0 || ch == 2) ? _capL?.Take(n).ToArray() : null,
                RightV = (ch == 1 || ch == 2) ? _capR?.Take(n).ToArray() : null,
                TimeDivMs = TimeDivMs[td],
                PreTrigPct = PreTrig * 100.0,
                Channel = ch
            };

            var entry = _overlayLibrary.AddCapturedOverlay(dto, $"{defaultName}_{DateTime.Now:yyyy-MM-dd HH-mm-ss}");

            OverlayLibraryReloadCache();
            _overlayLibrary.SetOverlayVisible(entry.FilePath, true);
            pnlScope.Invalidate();

            return entry;
        }

        // ── Fenêtre Overlays ──────────────────────────────────────────────
        private void OpenOverlaysWindow()
        {
            if (_overlaysForm == null || _overlaysForm.IsDisposed)
                _overlaysForm = new OverlaysForm(this);

            // CenterParent ne fonctionne pas pour Show() non-modal → centrage manuel
            var owner = FindForm();
            if (owner != null && !_overlaysForm.Visible)
            {
                _overlaysForm.StartPosition = FormStartPosition.Manual;
                _overlaysForm.Location = new Point(
                    owner.Left + (owner.Width  - _overlaysForm.Width)  / 2,
                    owner.Top  + (owner.Height - _overlaysForm.Height) / 2);
            }
            _overlaysForm.Show(owner ?? (IWin32Window)this);
            _overlaysForm.BringToFront();
            _overlaysForm.Activate();
        }

        private void btnOverlay_Click(object sender, EventArgs e)
        {
            OpenOverlaysWindow();
        }
    }
}
