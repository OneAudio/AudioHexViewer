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

        public void SaveToFile(string filePath,
                       string name,
                       int sampleRate,
                       int fftSize,
                       string window,
                       bool removeDc,
                       double[] freqHz,
                       double[]? leftDbFs,
                       double[]? rightDbFs)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("filePath vide");
            if (freqHz == null || freqHz.Length == 0) throw new ArgumentException("freqHz vide");
            if (leftDbFs == null && rightDbFs == null) throw new ArgumentException("pas de données spectre");

            if (leftDbFs != null && leftDbFs.Length != freqHz.Length)
                throw new ArgumentException("leftDbFs length != freqHz length");
            if (rightDbFs != null && rightDbFs.Length != freqHz.Length)
                throw new ArgumentException("rightDbFs length != freqHz length");

            var dto = new OverlayFile
            {
                Version = 2,
                Name = string.IsNullOrWhiteSpace(name) ? "Overlay" : name,
                CreatedUtc = DateTime.UtcNow,

                SampleRate = sampleRate,
                FftSize = fftSize,
                Window = window,
                RemoveDc = removeDc,

                FreqHz = freqHz,
                LeftDbFs = leftDbFs,
                RightDbFs = rightDbFs
            };

            var jsonOpt = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(filePath, JsonSerializer.Serialize(dto, jsonOpt));
        }


        public OverlayFile LoadFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("filePath vide");
            if (!File.Exists(filePath)) throw new FileNotFoundException("Fichier overlay introuvable", filePath);

            string json = File.ReadAllText(filePath);
            var dto = JsonSerializer.Deserialize<OverlayFile>(json);

            // Overlay V2 uniquement
            if (dto.Version != 2)
                throw new InvalidDataException("Overlay invalide: version non supportée (V2 uniquement).");

            if (dto.SampleRate <= 0 || dto.FftSize <= 0)
                throw new InvalidDataException("Overlay invalide: SampleRate/FftSize");

            if (dto.FreqHz == null || dto.FreqHz.Length == 0)
                throw new InvalidDataException("Overlay invalide: FreqHz manquant");

            if (dto.LeftDbFs == null && dto.RightDbFs == null)
                throw new InvalidDataException("Overlay invalide: pas de données spectre");

            if (dto.LeftDbFs != null && dto.LeftDbFs.Length != dto.FreqHz.Length)
                throw new InvalidDataException("Overlay invalide: LeftDbFs length != FreqHz length");

            if (dto.RightDbFs != null && dto.RightDbFs.Length != dto.FreqHz.Length)
                throw new InvalidDataException("Overlay invalide: RightDbFs length != FreqHz length");


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
