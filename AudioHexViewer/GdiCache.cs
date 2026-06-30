// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

using System;
using System.Collections.Generic;
using System.Drawing;

namespace AudioHexViewer
{
    /// <summary>
    /// Caches GDI+ pens, brushes and fonts by their parameters so paint hot paths can reuse
    /// them instead of allocating and disposing one per frame.
    /// <para>
    /// Safe by construction: the cache key includes every parameter that affects the resource
    /// (colour, width, font family/size/style), so a theme change simply yields a different
    /// cached instance — there is no risk of a stale colour. Old entries from previous themes
    /// linger harmlessly until <see cref="Clear"/> or <see cref="Dispose"/>.
    /// </para>
    /// <para>
    /// IMPORTANT: returned resources are owned by the cache. NEVER wrap them in <c>using</c>
    /// and never call Dispose() on them. UI-thread use only (no locking).
    /// </para>
    /// </summary>
    internal sealed class GdiCache : IDisposable
    {
        private readonly Dictionary<(int argb, float width), Pen> _pens = new();
        private readonly Dictionary<int, SolidBrush> _brushes = new();
        private readonly Dictionary<(string family, float size, FontStyle style), Font> _fonts = new();

        /// <summary>Cached solid pen for the given colour and width.</summary>
        public Pen Pen(Color color, float width = 1f)
        {
            var key = (color.ToArgb(), width);
            if (!_pens.TryGetValue(key, out var p))
            {
                p = new Pen(color, width);
                _pens[key] = p;
            }
            return p;
        }

        /// <summary>Cached solid brush for the given colour.</summary>
        public SolidBrush Brush(Color color)
        {
            int key = color.ToArgb();
            if (!_brushes.TryGetValue(key, out var b))
            {
                b = new SolidBrush(color);
                _brushes[key] = b;
            }
            return b;
        }

        /// <summary>Cached font for the given family, size (points) and style.</summary>
        public Font Font(FontFamily family, float sizePt, FontStyle style)
        {
            var key = (family.Name, sizePt, style);
            if (!_fonts.TryGetValue(key, out var f))
            {
                f = new Font(family, sizePt, style);
                _fonts[key] = f;
            }
            return f;
        }

        /// <summary>Cached font for the given family name, size (points) and style.</summary>
        public Font Font(string familyName, float sizePt, FontStyle style)
        {
            var key = (familyName, sizePt, style);
            if (!_fonts.TryGetValue(key, out var f))
            {
                f = new Font(familyName, sizePt, style);
                _fonts[key] = f;
            }
            return f;
        }

        /// <summary>Cached font copying a prototype's family/size/unit but with a new style.</summary>
        public Font Font(Font prototype, FontStyle style)
        {
            var key = (prototype.FontFamily.Name, prototype.Size, style);
            if (!_fonts.TryGetValue(key, out var f))
            {
                f = new Font(prototype, style);
                _fonts[key] = f;
            }
            return f;
        }

        /// <summary>Convenience overload deriving from an existing font's family.</summary>
        public Font Font(Font baseFont, float sizePt, FontStyle style)
            => Font(baseFont.FontFamily, sizePt, style);

        /// <summary>Dispose and drop all cached resources (e.g. on a deliberate theme reset).</summary>
        public void Clear()
        {
            foreach (var p in _pens.Values) p.Dispose();
            foreach (var b in _brushes.Values) b.Dispose();
            foreach (var f in _fonts.Values) f.Dispose();
            _pens.Clear();
            _brushes.Clear();
            _fonts.Clear();
        }

        public void Dispose() => Clear();
    }
}
