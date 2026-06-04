// ═══════════════════════════════════════════════════════════════════════════
// SpectrumControl.Overlays.cs  —  Bibliothèque d'overlays spectraux
//
// Responsabilités :
//   • Import / export / suppression / renommage d'overlays
//   • Cache en mémoire (OverlayLibraryReloadCache)
//   • Visibilité par slot (OverlayLibrarySetVisible)
//   • Ouverture de la fenêtre de gestion (OpenOverlaysWindow)
// ═══════════════════════════════════════════════════════════════════════════
using AudioHexViewer.Core;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Text.Json;
using System.Globalization;

namespace AudioHexViewer
{
    partial class SpectrumControl
    {
        private Color GetOverlaySlotColor(int slotIndex)
        {
            var arr = _colors.OverlaySlotArgb;
            if (arr != null && slotIndex >= 0 && slotIndex < arr.Length)
                return NegO(arr[slotIndex]);
            // fallback compat
            return Color.FromArgb(_colors.OverlayArgb);
        }

        //
        private void InitOverlayAnnotation()
        {
            // éviter de créer plusieurs fois
            if (_overlayInfoAnno != null)
                return;

            // Vérifier qu'on a bien un ChartArea
            if (chartSpectrum.ChartAreas.Count == 0)
                return;

            // Si une annotation du même nom existe déjà (designer / reload), on la réutilise
            const string annoName = "OverlayInfo";
            foreach (var a in chartSpectrum.Annotations)
            {
                if (a is TextAnnotation ta && ta.Name == annoName)
                {
                    _overlayInfoAnno = ta;
                    return;
                }
            }

            var anno = new TextAnnotation
            {
                Name = annoName,
                Text = "",
                Visible = false,

                // Très important : coordonnées relatives (0..100)
                IsSizeAlwaysRelative = true,

                // On clippe à la zone SpectrumArea pour éviter qu'il déborde
                ClipToChartArea = "SpectrumArea",

                // Style
                BackColor = Color.FromArgb(160, 230, 230, 230),
                ForeColor = Color.FromArgb(230, 30, 30, 30),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),

                // Alignement du texte dans le bloc
                Alignment = ContentAlignment.TopRight
            };

            chartSpectrum.Annotations.Add(anno);
            _overlayInfoAnno = anno;
        }

        //
        private void UpdateOverlayInfo()
        {
            if (_overlayInfoAnno == null)
                return;

            if (_sampleRate <= 0 || lastFftSize <= 0)
            {
                _TopInfoLine = "";
                _overlayInfoAnno.Visible = false;
                chartSpectrum.Invalidate();
                return;
            }

            // ---- Texte ----
            string srText = _sampleRate >= 1000
                ? $"{_sampleRate / 1000.0:F0} kHz"
                : $"{_sampleRate} Hz";

            int N = lastFftSize;

            int hop = (_hopSamples > 0) ? _hopSamples : N;
            hop = Math.Max(1, Math.Min(hop, N));
            double overlapPct = 100.0 * (1.0 - (double)hop / N);
            if (overlapPct < 0) overlapPct = 0;

            // --- FFT Window ---
            string winText = lastWindow switch
            {
                WindowType.Rectangular => "Rect",
                WindowType.Hann => "Hann",
                WindowType.Hamming => "Hamming",
                WindowType.BlackmanHarris4 => "Blackman-Harris4",
                WindowType.BlackmanHarris7 => "Blackman-Harris7",
                _ => "?"
            };

            // --- Averaging ---
            // Quand la moyenne cohérente est active, on le mentionne explicitement.
            bool cohAvgActive = _thdMeasuresEnabled && _thdUi.UseCohAvg;
            string avgText;
            if (cohAvgActive)
            {
                // "Coherent Avg: N"  ou  "Coherent Avg: Exp α"
                if (_avgIsExponential)
                    avgText = $"Coherent Avg: Exp {_avgExpAlpha.ToString("0.##", CultureInfo.InvariantCulture)}";
                else
                    avgText = avgTargetCount <= 1
                        ? "Coherent Avg: 1"
                        : $"Coherent Avg: {avgTargetCount}";
            }
            else if (_avgIsExponential)
            {
                avgText = $"Avg: Exp {_avgExpAlpha.ToString("0.##", CultureInfo.InvariantCulture)}";
            }
            else
            {
                avgText = avgTargetCount <= 1
                    ? "Avg: No"
                    : $"Avg:{avgTargetCount}";
            }

            //// --- AC / DC ---
            //string acdcText = (chkRemoveDC != null && chkRemoveDC.Checked)
            //    ? "AC mode"
            //    : "DC mode";

            // --- Ligne finale ---
            // --- Résolution FFT : ΔF = SR / N ---
            double df = (double)_sampleRate / N;

            // Format lisible
            string dfText = (df >= 1000.0)
                ? $"{df / 1000.0:F2} kHz"
                : (df >= 10.0 ? $"{df:F1} Hz" : $"{df:F2} Hz");

            // --- Channel mode (pour affichage) ---
            // --- Channel mode (pour affichage) ---
            string channelText = SelectedChannelMode switch
            {
                "Left" => "Left",
                "Right" => "Right",
                "Bode Plot" => "Bode Plot",
                "L+R" => "Both",
                _ => SelectedChannelMode // fallback (affiche la valeur brute si inattendue)
            };


            _TopInfoLine =
                $"Channel:{channelText}    " +
                $"SR:{srText}    " +
                $"N={N:N0}    " +
                $"ΔF={dfText}    " +
                $"Overlap:{overlapPct:F1}%     " +
                $"Win:{winText}    " +
                $"{avgText}    ";
            //$"{acdcText}";


            _overlayInfoAnno.Visible = false;   // on ne l’utilise plus
            chartSpectrum.Invalidate();         // force un repaint pour voir la ligne mise à jour

        }

        //
        public void OverlayLibraryUnhideFromList(string libraryFilePath)
        {
            _overlayLibrary.UnhideFromList(libraryFilePath);
            OverlayLibraryReloadCache();
            UpdateSpectrumChartAndStats(lastTimeLeft, lastTimeRight);
        }

        //
        public void OverlayLibraryRemoveFromList(string libraryFilePath)
        {
            // On masque d'abord l'overlay dans l'affichage live
            OverlayLibrarySetVisible(libraryFilePath, false);

            _overlayLibrary.HideFromList(libraryFilePath);
            OverlayLibraryReloadCache();
            UpdateSpectrumChartAndStats(lastTimeLeft, lastTimeRight);
        }

        public string OverlayLibraryGetFolderPath()
        {
            return _overlayLibrary.LibraryDirectory;
        }

        //
        public System.Drawing.Color OverlayGetSlotColor1Based(int slotNumber)
        {
            // slotNumber attendu: 1..8
            int idx0 = slotNumber - 1;
            if (idx0 < 0 || idx0 >= OverlayManager.MaxOverlays)
                idx0 = 0;

            return GetOverlaySlotColor(idx0);
        }

        //
        private int OverlayLibraryGetNextFreeSlotNumber()
        {
            // Slots utilisés = ceux présents dans la bibliothèque (quel que soit Kind)
            bool[] used = new bool[OverlayManager.MaxOverlays + 1]; // index 1..8

            foreach (var c in _overlayCache)
            {
                int s = c.Data != null ? c.Data.SlotNumber : 0;
                if (s >= 1 && s <= OverlayManager.MaxOverlays)
                    used[s] = true;
            }

            for (int s = 1; s <= OverlayManager.MaxOverlays; s++)
                if (!used[s]) return s;

            throw new InvalidOperationException($"Tous les slots overlays (1..{OverlayManager.MaxOverlays}) sont déjà utilisés.");
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
            // Conserver les visibles actuels par FilePath
            var prevVisible = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in _overlayCache)
                prevVisible[c.Entry.FilePath] = c.Visible;

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

            // Tri: plus récent en premier (déjà trié côté OverlayLibrary, mais on garde une sécurité)
            _overlayCache.Sort((a, b) => b.Entry.CreatedUtc.CompareTo(a.Entry.CreatedUtc));
        }

        public List<OverlayEntry> OverlayLibraryGetEntries()
        {
            // Retourne la liste "UI-friendly" (métadonnées)
            return _overlayCache.Select(c => c.Entry).ToList();
        }

        public void OverlayLibrarySetVisible(string filePath, bool visible)
        {
            var c = _overlayCache.FirstOrDefault(x => string.Equals(x.Entry.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            if (c != null)
            {
                c.Visible = visible;

                // Persist
                _overlayLibrary.SetOverlayVisible(c.Entry.FilePath, visible);

                UpdateSpectrumChartAndStats(lastTimeLeft, lastTimeRight);
            }
        }

        //
        public bool OverlayLibraryGetVisible(string libraryFilePath)
        {
            return _overlayLibrary.GetOverlayVisible(libraryFilePath, defaultValue: true);
        }

        //
        public OverlayEntry OverlayLibraryImport(string sourcePath)
        {
            var entry = _overlayLibrary.ImportFromFile(sourcePath);
            OverlayLibraryReloadCache();
            _overlayLibrary.SetOverlayVisible(entry.FilePath, true);
            UpdateSpectrumChartAndStats(lastTimeLeft, lastTimeRight);
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
            UpdateSpectrumChartAndStats(lastTimeLeft, lastTimeRight);
        }

        public OverlayEntry OverlayLibraryRename(string libraryFilePath, string newName)
        {
            var c = _overlayCache.FirstOrDefault(x => string.Equals(x.Entry.FilePath, libraryFilePath, StringComparison.OrdinalIgnoreCase));
            if (c == null) throw new FileNotFoundException("Overlay introuvable dans le cache.", libraryFilePath);

            var updated = _overlayLibrary.Rename(c.Entry, newName);

            OverlayLibraryReloadCache();
            UpdateSpectrumChartAndStats(lastTimeLeft, lastTimeRight);

            return updated;
        }

        //
        private void OpenOverlaysWindow()
        {
            if (_overlaysForm == null || _overlaysForm.IsDisposed)
                _overlaysForm = new OverlaysForm(this);

            // CenterParent ne fonctionne pas pour Show() non-modal → centrage manuel
            var owner = FindForm();
            if (owner != null && !_overlaysForm.Visible)
            {
                _overlaysForm.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
                _overlaysForm.Location = new System.Drawing.Point(
                    owner.Left + (owner.Width  - _overlaysForm.Width)  / 2,
                    owner.Top  + (owner.Height - _overlaysForm.Height) / 2);
            }
            _overlaysForm.Show(owner ?? (System.Windows.Forms.IWin32Window)this);
            _overlaysForm.BringToFront();
            _overlaysForm.Activate();
        }

        //
        private void btnOverlay_Click(object sender, EventArgs e)
        {
            OpenOverlaysWindow();
        }

    }
}
