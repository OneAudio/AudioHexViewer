// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

namespace AudioHexViewer
{
    internal sealed class SpectrumOptions
    {
        public WindowType WindowType { get; set; } = WindowType.Rectangular;
        public bool RemoveDc { get; set; } = false;

        // Commit 4 : préparer Bode / mesures (phase, ratio complexe)
        public bool NeedComplexFft { get; set; } = false;
    }
}

