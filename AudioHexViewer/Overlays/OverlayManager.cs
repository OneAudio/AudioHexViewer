using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AudioHexViewer
{
    internal sealed class OverlayManager
    {
        public const int MaxOverlays = 8;

        private readonly List<OverlaySlot> _slots = new();

        public IReadOnlyList<OverlaySlot> Slots => _slots;

        public OverlayManager()
        {
            for (int i = 0; i < MaxOverlays; i++)
                _slots.Add(new OverlaySlot(i));
        }

        public void ClearSlot(int index)
        {
            var s = GetSlot(index);
            s.Data = null;
            s.FilePath = null;
            s.Name = $"Overlay {index + 1}";
            s.Enabled = true;
        }

        public int CountLoaded()
        {
            int n = 0;
            foreach (var s in _slots)
                if (s.Data != null) n++;
            return n;
        }

        public int CountVisible()
        {
            int n = 0;
            foreach (var s in _slots)
                if (s.Enabled && s.Data != null) n++;
            return n;
        }

        public void SaveToFile(string filePath, SpectrumResult current, string name, SpectrumOptions options)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("filePath vide");

            var dto = new OverlayFile
            {
                Version = 1,
                Name = string.IsNullOrWhiteSpace(name) ? "Overlay" : name,
                CreatedUtc = DateTime.UtcNow,
                SampleRate = current.SampleRate,
                FftSize = current.FftSize,
                Window = options.WindowType.ToString(),
                RemoveDc = options.RemoveDc,
                LeftDbFs = current.LeftDbFs,
                RightDbFs = current.RightDbFs
            };

            var jsonOpt = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(dto, jsonOpt);
            File.WriteAllText(filePath, json);
        }

        public OverlayFile LoadFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("filePath vide");
            if (!File.Exists(filePath)) throw new FileNotFoundException("Fichier overlay introuvable", filePath);

            string json = File.ReadAllText(filePath);
            var dto = JsonSerializer.Deserialize<OverlayFile>(json);

            if (dto == null)
                throw new InvalidDataException("Overlay invalide: JSON vide");

            // petite validation minimale
            if (dto.FftSize <= 0 || dto.SampleRate <= 0)
                throw new InvalidDataException("Overlay invalide: SampleRate/FftSize");

            if (dto.LeftDbFs == null && dto.RightDbFs == null)
                throw new InvalidDataException("Overlay invalide: pas de données spectre");

            return dto;
        }

        public void LoadIntoSlot(int index, string filePath)
        {
            var dto = LoadFromFile(filePath);

            var s = GetSlot(index);
            s.Data = dto;
            s.FilePath = filePath;
            s.Name = dto.Name;
            s.Enabled = true;
        }

        public bool IsCompatibleStrict(OverlayFile overlay, SpectrumResult current)
        {
            if (overlay.SampleRate != current.SampleRate) return false;
            if (overlay.FftSize != current.FftSize) return false;
            return true;
        }

        private OverlaySlot GetSlot(int index)
        {
            if (index < 0 || index >= _slots.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return _slots[index];
        }
    }
}
