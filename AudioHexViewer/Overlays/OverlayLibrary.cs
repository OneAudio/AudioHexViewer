using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AudioHexViewer
{
    /// <summary>
    /// Bibliothèque locale d'overlays.
    ///
    /// - Stockage: un fichier JSON par overlay dans AppData.
    /// - Format: OverlayFile (V3), extension *.overlay.json
    /// - AUCUNE dépendance UI: ce composant fait uniquement du stockage / IO / validation légère.
    ///
    /// NB: La notion "visible / masqué" est volontairement hors-fichier pour le moment.
    ///     La fenêtre Overlays pilotera l'affichage via une liste en mémoire (puis on pourra persister l'état si souhaité).
    /// </summary>
    internal sealed class OverlayLibrary
    {
        private readonly string _libraryDir;
        private readonly JsonSerializerOptions _jsonOptions;

        public string LibraryDirectory => _libraryDir;
        public int SlotNumber { get; }

        public OverlayLibrary(string? libraryDir = null)
        {
            _libraryDir = libraryDir ?? GetDefaultLibraryDir();
            Directory.CreateDirectory(_libraryDir);

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
            };
            _jsonOptions.Converters.Add(new JsonStringEnumConverter());
        }

        /// <summary>
        /// Charge tous les overlays présents dans la bibliothèque.
        /// Les fichiers illisibles / invalides sont ignorés (mais on remonte une trace via exceptions si tu préfères plus strict).
        /// </summary>
        public List<OverlayEntry> LoadAll()
        {
            Directory.CreateDirectory(_libraryDir);
            var hidden = LoadHiddenFileNames();

            var entries = new List<OverlayEntry>();

            foreach (var path in Directory.EnumerateFiles(_libraryDir, "*.overlay.json", SearchOption.TopDirectoryOnly))
            {
                var fileName = Path.GetFileName(path);
                if (hidden.Contains(fileName))
                continue;

                try
                {
                    var overlay = ReadOverlayFile(path);
                    entries.Add(OverlayEntry.FromFile(path, overlay));
                }
                catch
                {
                    // Choix volontaire: on ignore silencieusement pour ne pas rendre la bibliothèque inutilisable
                    // à cause d'un fichier corrompu.
                    // Si tu veux du strict: remplace par "throw;" ou log.
                }
            }

            // Tri stable: plus récent en premier (utile pour l'UI)
            entries.Sort((a, b) => b.CreatedUtc.CompareTo(a.CreatedUtc));
            return entries;
        }
        //
        private string HiddenListPath => Path.Combine(_libraryDir, "_hidden_overlays.txt");

        private HashSet<string> LoadHiddenFileNames()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                if (!File.Exists(HiddenListPath))
                    return set;

                foreach (var line in File.ReadAllLines(HiddenListPath))
                {
                    var s = (line ?? "").Trim();
                    if (s.Length == 0) continue;
                    set.Add(s);
                }
            }
            catch
            {
                // ignore : si fichier illisible, on ne bloque pas
            }

            return set;
        }
        //
        public void UnhideFromList(string libraryFilePath)
        {
            if (string.IsNullOrWhiteSpace(libraryFilePath)) return;

            var fileName = Path.GetFileName(libraryFilePath);
            if (string.IsNullOrWhiteSpace(fileName)) return;

            var hidden = LoadHiddenFileNames();
            if (hidden.Remove(fileName))
                SaveHiddenFileNames(hidden);
        }

        //
        public void HideFromList(string libraryFilePath)
        {
            if (string.IsNullOrWhiteSpace(libraryFilePath)) return;

            var fileName = Path.GetFileName(libraryFilePath);
            if (string.IsNullOrWhiteSpace(fileName)) return;

            var hidden = LoadHiddenFileNames();
            if (hidden.Add(fileName))
                SaveHiddenFileNames(hidden);
        }

        //
        private void SaveHiddenFileNames(HashSet<string> hidden)
        {
            try
            {
                Directory.CreateDirectory(_libraryDir);
                File.WriteAllLines(HiddenListPath, hidden.OrderBy(x => x).ToArray());
            }
            catch
            {
                // ignore : on évite de casser l’app si on ne peut pas écrire
            }
        }

        ///
        /// <summary>
        /// Ajoute un overlay déjà construit en mémoire (typiquement une capture de la vue active).
        /// Écrit immédiatement sur disque dans la bibliothèque.
        ///
        /// fileNameBase: si null, on génère "Overlay_YYYY-MM-DD HH-mm-ss".
        /// Retourne l'entrée créée.
        /// </summary>
        public OverlayEntry AddCapturedOverlay(OverlayFile overlay, string? fileNameBase = null)
        {
            if (overlay == null) throw new ArgumentNullException(nameof(overlay));
            EnsureOverlayIsV3(overlay);
            EnsureOverlayHasData(overlay);

            Directory.CreateDirectory(_libraryDir);

            if (string.IsNullOrWhiteSpace(fileNameBase))
                fileNameBase = $"Overlay_{DateTime.Now:yyyy-MM-dd HH-mm-ss}";

            var safeBase = MakeSafeFileName(fileNameBase);
            var filePath = GetUniquePath(Path.Combine(_libraryDir, safeBase + ".overlay.json"));

            WriteOverlayFile(filePath, overlay);

            return OverlayEntry.FromFile(filePath, overlay);
        }

        /// <summary>
        /// Import: ajoute un overlay depuis un fichier .overlay.json externe.
        /// Stratégie: on lit/valide, puis on copie dans la bibliothèque sous un nom unique.
        /// </summary>
        public OverlayEntry ImportFromFile(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath)) throw new ArgumentException("sourcePath null/empty", nameof(sourcePath));
            if (!File.Exists(sourcePath)) throw new FileNotFoundException("Fichier overlay introuvable.", sourcePath);

            var overlay = ReadOverlayFile(sourcePath);
            EnsureOverlayIsV3(overlay);
            EnsureOverlayHasData(overlay);

            Directory.CreateDirectory(_libraryDir);

            // Nom de destination = nom du fichier importé (sans (2)/(3) automatique)
            var baseName = Path.GetFileName(sourcePath);

            // Si l'utilisateur importe un .json générique, on force l'extension .overlay.json
            if (!baseName.EndsWith(".overlay.json", StringComparison.OrdinalIgnoreCase))
            {
                var noExt = Path.GetFileNameWithoutExtension(baseName);
                baseName = noExt + ".overlay.json";
            }

            var destPath = Path.Combine(_libraryDir, baseName);


            // On ré-écrit (plutôt qu'un File.Copy brut) pour normaliser le JSON (indentation, enum string, etc.)
            WriteOverlayFile(destPath, overlay);
            // Si l'overlay avait été "Remove" auparavant, on le ré-affiche explicitement.
            UnhideFromList(destPath);
            return OverlayEntry.FromFile(destPath, overlay);

        }

        /// <summary>
        /// Export: écrit l'overlay vers un fichier cible.
        /// Le caller passe typiquement un chemin choisi par SaveFileDialog.
        /// </summary>
        public void ExportToFile(OverlayEntry entry, string targetPath)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (string.IsNullOrWhiteSpace(targetPath)) throw new ArgumentException("targetPath null/empty", nameof(targetPath));

            var overlay = ReadOverlayFile(entry.FilePath);
            EnsureOverlayIsV3(overlay);
            EnsureOverlayHasData(overlay);

            WriteOverlayFile(targetPath, overlay);
        }

        /// <summary>
        /// Supprime définitivement le fichier de l'overlay dans la bibliothèque.
        /// </summary>
        public void Delete(OverlayEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (File.Exists(entry.FilePath))
                File.Delete(entry.FilePath);
        }

        /// <summary>
        /// Renomme un overlay (nom interne + nom de fichier).
        /// On garde le contenu identique sauf Name.
        /// </summary>
        public OverlayEntry Rename(OverlayEntry entry, string newName)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (string.IsNullOrWhiteSpace(newName)) throw new ArgumentException("Nom vide.", nameof(newName));

            var overlay = ReadOverlayFile(entry.FilePath);
            EnsureOverlayIsV3(overlay);
            EnsureOverlayHasData(overlay);

            overlay.Name = newName.Trim();

            var safeBase = MakeSafeFileName(overlay.Name);
            var newPath = GetUniquePath(Path.Combine(_libraryDir, safeBase + ".overlay.json"));

            WriteOverlayFile(newPath, overlay);

            // Suppression de l'ancien fichier
            if (File.Exists(entry.FilePath))
                File.Delete(entry.FilePath);

            return OverlayEntry.FromFile(newPath, overlay);
        }

        // ===================== Helpers IO / Validation =====================

        private OverlayFile ReadOverlayFile(string path)
        {
            var json = File.ReadAllText(path);
            var dto = JsonSerializer.Deserialize<OverlayFile>(json, _jsonOptions);
            if (dto == null) throw new InvalidDataException("Overlay invalide: JSON vide ou non conforme.");
            return dto;
        }

        private void WriteOverlayFile(string path, OverlayFile dto)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(dto, _jsonOptions);
            File.WriteAllText(path, json);
        }

        private static void EnsureOverlayIsV3(OverlayFile dto)
        {
            if (dto.Version != OverlayFile.CurrentVersion)
                throw new InvalidDataException($"Overlay invalide: Version={dto.Version}, attendu V{OverlayFile.CurrentVersion}.");
        }

        private static void EnsureOverlayHasData(OverlayFile dto)
        {
            if (dto.FreqHz == null || dto.FreqHz.Length == 0)
                throw new InvalidDataException("Overlay invalide: FreqHz vide.");

            switch (dto.Kind)
            {
                case OverlayKind.Spectrum:
                    if (dto.LeftDbFs == null && dto.RightDbFs == null)
                        throw new InvalidDataException("Overlay invalide (Spectrum): pas de LeftDbFs/RightDbFs.");
                    if (dto.LeftDbFs != null && dto.LeftDbFs.Length != dto.FreqHz.Length)
                        throw new InvalidDataException("Overlay invalide (Spectrum): LeftDbFs length != FreqHz length.");
                    if (dto.RightDbFs != null && dto.RightDbFs.Length != dto.FreqHz.Length)
                        throw new InvalidDataException("Overlay invalide (Spectrum): RightDbFs length != FreqHz length.");
                    break;

                case OverlayKind.Bode:
                    // Spec: on veut gain + phase.
                    if (dto.GainDb == null || dto.PhaseDeg == null)
                        throw new InvalidDataException("Overlay invalide (Bode): GainDb et PhaseDeg requis.");
                    if (dto.GainDb.Length != dto.FreqHz.Length)
                        throw new InvalidDataException("Overlay invalide (Bode): GainDb length != FreqHz length.");
                    if (dto.PhaseDeg.Length != dto.FreqHz.Length)
                        throw new InvalidDataException("Overlay invalide (Bode): PhaseDeg length != FreqHz length.");
                    break;

                default:
                    throw new InvalidDataException("Overlay invalide: Kind inconnu.");
            }
        }

        private static string GetDefaultLibraryDir()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "AudioHexViewer", "Overlays");
        }

        private static string MakeSafeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var safe = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray()).Trim();
            if (string.IsNullOrWhiteSpace(safe))
                safe = "Overlay";
            return safe;
        }

        private static string GetUniquePath(string desiredPath)
        {
            if (!File.Exists(desiredPath))
                return desiredPath;

            var dir = Path.GetDirectoryName(desiredPath)!;
            var baseName = Path.GetFileNameWithoutExtension(desiredPath);
            var ext = Path.GetExtension(desiredPath);

            for (int i = 2; i < 10000; i++)
            {
                var candidate = Path.Combine(dir, $"{baseName} ({i}){ext}");
                if (!File.Exists(candidate))
                    return candidate;
            }

            throw new IOException("Impossible de générer un nom de fichier unique (trop de collisions).");
        }
    }

    /// <summary>
    /// Métadonnées prêtes pour affichage UI (liste des overlays).
    /// </summary>
    public sealed class OverlayEntry
    {
        public string FilePath { get; }
        public string Name { get; }
        public OverlayKind Kind { get; }
        public DateTime CreatedUtc { get; }
        public int SampleRate { get; }
        public int SlotNumber { get; }


        private OverlayEntry(string filePath, string name, OverlayKind kind, DateTime createdUtc, int sampleRate, int slotNumber)
        {
            FilePath = filePath;
            Name = name;
            Kind = kind;
            CreatedUtc = createdUtc;
            SampleRate = sampleRate;
            SlotNumber = slotNumber;
        }

        public static OverlayEntry FromFile(string filePath, OverlayFile dto)
        {
            return new OverlayEntry(
                filePath,
                string.IsNullOrWhiteSpace(dto.Name) ? Path.GetFileNameWithoutExtension(filePath) : dto.Name,
                dto.Kind,
                dto.CreatedUtc,
                dto.SampleRate,
                dto.SlotNumber
            );
        }
    }
}
