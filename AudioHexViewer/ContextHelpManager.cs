// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

using System.Text.Json;

namespace AudioHexViewer;

/// <summary>
/// Manages context-sensitive tooltips loaded from <c>ContextHelp.json</c>.
/// Form1 uses the shared <see cref="ToolTip"/> instance.
/// Secondary forms each get their own ToolTip via <see cref="CreateFormToolTip"/>.
/// <see cref="AlwaysOnToolTip"/> is a permanently active ToolTip for controls
/// that should always show their tooltip regardless of <see cref="IsEnabled"/>.
/// </summary>
internal static class ContextHelpManager
{
    private static Dictionary<string, string> _helpTexts = new();

    // ── Main form tooltip (Form1 controls) ───────────────────────────────────
    public static readonly ToolTip ToolTip = new()
    {
        InitialDelay   = 400,
        ReshowDelay    = 200,
        AutomaticDelay = 400,
    };

    // ── Always-active tooltip (e.g. the ? toolbar button) ────────────────────
    /// <summary>
    /// Permanently active tooltip — never affected by <see cref="IsEnabled"/>.
    /// Use for controls that should always show help regardless of context help state.
    /// </summary>
    public static readonly ToolTip AlwaysOnToolTip = new()
    {
        Active         = true,
        InitialDelay   = 400,
        ReshowDelay    = 200,
        AutomaticDelay = 400,
    };

    // ── Secondary form tooltips (one per secondary Form) ──────────────────────
    private static readonly List<WeakReference<ToolTip>> _secondaryToolTips = new();

    // ── Toggle (controls all instances except AlwaysOnToolTip) ───────────────
    public static bool IsEnabled
    {
        get => ToolTip.Active;
        set
        {
            ToolTip.Active = value;

            _secondaryToolTips.RemoveAll(wr =>
            {
                if (!wr.TryGetTarget(out var tt)) return true;
                try   { tt.Active = value; return false; }
                catch (ObjectDisposedException) { return true; }
            });
        }
    }

    // ── Loading ───────────────────────────────────────────────────────────────
    public static void Load(string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            System.Diagnostics.Debug.WriteLine($"[ContextHelp] File not found: {jsonPath}");
            return;
        }
        try
        {
            var json = File.ReadAllText(jsonPath, System.Text.Encoding.UTF8);
            var opts = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };
            _helpTexts = JsonSerializer.Deserialize<Dictionary<string, string>>(json, opts) ?? new();
            System.Diagnostics.Debug.WriteLine(
                $"[ContextHelp] Loaded {_helpTexts.Count} entries from {Path.GetFileName(jsonPath)}.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ContextHelp] Load error: {ex.Message}");
        }
    }

    // ── Text lookup ───────────────────────────────────────────────────────────
    /// <summary>Returns the tooltip text for a given control Name key.</summary>
    public static bool TryGetText(string name, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out string text)
        => _helpTexts.TryGetValue(name, out text);

    // ── Factory for secondary forms ───────────────────────────────────────────
    public static ToolTip CreateFormToolTip()
    {
        var tt = new ToolTip
        {
            InitialDelay   = 400,
            ReshowDelay    = 200,
            AutomaticDelay = 400,
            Active         = IsEnabled,
        };
        _secondaryToolTips.Add(new WeakReference<ToolTip>(tt));
        return tt;
    }

    // ── Apply to Control tree ─────────────────────────────────────────────────
    /// <summary>
    /// Recursively assigns tooltips to all named controls found in the JSON.
    /// Pass <paramref name="toolTip"/> for secondary forms; omit for Form1.
    /// </summary>
    public static void ApplyTooltips(Control.ControlCollection controls, ToolTip? toolTip = null)
    {
        var tt = toolTip ?? ToolTip;
        foreach (Control ctrl in controls)
        {
            if (!string.IsNullOrEmpty(ctrl.Name) &&
                _helpTexts.TryGetValue(ctrl.Name, out var text))
            {
                tt.SetToolTip(ctrl, text);
            }
            if (ctrl.HasChildren)
                ApplyTooltips(ctrl.Controls, tt);
        }
    }

    // ── Apply to ToolStrip menu items ─────────────────────────────────────────
    /// <summary>
    /// Sets or clears <see cref="ToolStripItem.ToolTipText"/> for items whose
    /// <see cref="ToolStripItem.Name"/> is found in the JSON.
    /// Call with <paramref name="enable"/> = true to show, false to hide.
    /// Also recurse into sub-menus.
    /// </summary>
    public static void ApplyMenuItemTooltips(ToolStripItemCollection items, bool enable)
    {
        foreach (ToolStripItem item in items)
        {
            if (!string.IsNullOrEmpty(item.Name) &&
                _helpTexts.TryGetValue(item.Name, out var text))
            {
                item.ToolTipText = enable ? text : string.Empty;
            }
            if (item is ToolStripMenuItem mi && mi.HasDropDownItems)
                ApplyMenuItemTooltips(mi.DropDownItems, enable);
        }
    }

    // ── Diagnostic ────────────────────────────────────────────────────────────
#if DEBUG
    public static void ReportMissing(Control.ControlCollection controls)
    {
        var names   = CollectNames(controls);
        var missing = names.Where(n => !_helpTexts.ContainsKey(n)).OrderBy(n => n).ToList();
        if (missing.Count == 0)
            System.Diagnostics.Debug.WriteLine("[ContextHelp] All named controls have a tooltip entry.");
        else
            System.Diagnostics.Debug.WriteLine(
                $"[ContextHelp] {missing.Count} control(s) without tooltip: {string.Join(", ", missing)}");
    }

    private static List<string> CollectNames(Control.ControlCollection controls)
    {
        var result = new List<string>();
        foreach (Control ctrl in controls)
        {
            if (!string.IsNullOrEmpty(ctrl.Name)) result.Add(ctrl.Name);
            if (ctrl.HasChildren) result.AddRange(CollectNames(ctrl.Controls));
        }
        return result;
    }
#endif
}
