// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

using System;
using System.IO;

namespace AudioHexViewer
{
    /// <summary>
    /// Defensive limits for loading overlay files. Overlays can be shared between users,
    /// so a corrupt or malicious file must not be able to exhaust memory. The file size is
    /// capped BEFORE reading (the primary guard, since array sizes are bounded by file size),
    /// and the number of points per array is capped AFTER deserialization (belt-and-suspenders).
    /// </summary>
    internal static class OverlaySafety
    {
        /// <summary>Maximum accepted overlay file size. Legitimate overlays are KB to a few MB.</summary>
        public const long MaxFileBytes = 64L * 1024 * 1024;   // 64 MB

        /// <summary>Maximum number of points accepted in any single array.</summary>
        public const int MaxPoints = 5_000_000;

        /// <summary>
        /// Read an overlay JSON file after checking its size. Throws if the file is missing
        /// or larger than <see cref="MaxFileBytes"/> (checked before any bytes are read).
        /// </summary>
        public static string ReadOverlayJson(string path)
        {
            var info = new FileInfo(path);
            if (!info.Exists)
                throw new FileNotFoundException("Overlay file not found.", path);

            if (info.Length > MaxFileBytes)
                throw new InvalidDataException(
                    $"Overlay rejected: file too large ({info.Length / (1024 * 1024)} MB, limit {MaxFileBytes / (1024 * 1024)} MB).");

            return File.ReadAllText(path);
        }

        /// <summary>
        /// Reject an overlay whose arrays exceed <see cref="MaxPoints"/>.
        /// No-op when <paramref name="dto"/> is null (the caller's own validation handles that).
        /// </summary>
        public static void CheckBounds(OverlayFile? dto)
        {
            if (dto == null) return;
            Check(dto.FreqHz?.Length,    "FreqHz");
            Check(dto.LeftDbFs?.Length,  "LeftDbFs");
            Check(dto.RightDbFs?.Length, "RightDbFs");
            Check(dto.GainDb?.Length,    "GainDb");
            Check(dto.PhaseDeg?.Length,  "PhaseDeg");
            Check(dto.TimeMs?.Length,    "TimeMs");
            Check(dto.LeftV?.Length,     "LeftV");
            Check(dto.RightV?.Length,    "RightV");
        }

        private static void Check(int? length, string field)
        {
            if (length is int n && n > MaxPoints)
                throw new InvalidDataException(
                    $"Overlay rejected: {field} has {n} points (limit {MaxPoints}).");
        }
    }
}
