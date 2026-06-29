// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

#nullable enable
using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace AudioHexViewer
{
    public sealed class CursorFrequencyDialog : Form
    {
        private readonly TextBox _txt;
        private double _value;
        private readonly double _min, _max;
        private readonly Action<double>? _onPreview;
        public double SelectedHz { get; private set; }

        private void SetValue(double v)
        {
            _value = Math.Max(_min, Math.Min(_max, v));
            _txt.Text = _value.ToString("N1", CultureInfo.CurrentCulture);
            _onPreview?.Invoke(_value);
        }

        private void CommitText()
        {
            // Retire séparateurs de milliers (espace insécable ou espace normal)
            string raw = _txt.Text.Replace("\u00a0", "").Replace(" ", "");
            if (double.TryParse(raw, NumberStyles.Any, CultureInfo.CurrentCulture, out double v))
                SetValue(v);
            else
                SetValue(_value); // revert
        }

        public CursorFrequencyDialog(string title, double minHz, double maxHz,
            double? initialHz, Action<double>? onPreview = null)
        {
            _min = minHz; _max = maxHz; _onPreview = onPreview;

            var std    = new Font("Segoe UI", 9f);
            var fStep  = new Font("Segoe UI", 8.5f);
            var fBig   = new Font("Segoe UI", 13f, FontStyle.Bold);
            var fUnit  = new Font("Segoe UI", 8f);
            var blue   = Color.FromArgb(0, 90, 160);
            var bdrClr = Color.FromArgb(180, 180, 180);

            Text            = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition   = FormStartPosition.CenterParent;
            MaximizeBox     = false; MinimizeBox = false;
            ShowInTaskbar   = false;
            ClientSize      = new Size(300, 100);
            Font            = std;
            BackColor       = SystemColors.Control;

            // ── Value display panel ───────────────────────────────────────────
            var pnlVal = new Panel
            {
                Location    = new Point(100, 8),
                Size        = new Size(100, 46),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor   = SystemColors.Window
            };
            _txt = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font        = fBig,
                TextAlign   = HorizontalAlignment.Center,
                Location    = new Point(2, 4),
                Size        = new Size(96, 24),
                BackColor   = SystemColors.Window
            };
            var lblUnit = new Label
            {
                Text      = "Hz",
                Font      = fUnit,
                ForeColor = SystemColors.GrayText,
                TextAlign = ContentAlignment.MiddleCenter,
                Location  = new Point(0, 29),
                Size      = new Size(98, 14),
                BackColor = SystemColors.Window
            };
            pnlVal.Controls.Add(_txt);
            pnlVal.Controls.Add(lblUnit);
            Controls.Add(pnlVal);

            // ── Step buttons helper ───────────────────────────────────────────
            Button MakeBtn(string label, int x, Action act)
            {
                var b = new Button
                {
                    Text      = label,
                    Location  = new Point(x, 8),
                    Size      = new Size(40, 46),
                    Font      = fStep,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = SystemColors.Control
                };
                b.FlatAppearance.BorderColor = bdrClr;
                b.Click += (_, __) => { CommitText(); act(); };
                Controls.Add(b);
                return b;
            }

            // ÷10  ÷2  [display]  ×2  ×10
            MakeBtn("÷10",   8, () => SetValue(_value / 10.0));
            MakeBtn("÷2",   52, () => SetValue(_value / 2.0));
            MakeBtn("×2",  204, () => SetValue(_value * 2.0));
            MakeBtn("×10", 248, () => SetValue(_value * 10.0));

            // ── OK / Cancel ───────────────────────────────────────────────────
            var btnCancel = new Button
            {
                Text = "Cancel", DialogResult = DialogResult.Cancel,
                Location = new Point(10, 64), Size = new Size(78, 27),
                Font = std, UseVisualStyleBackColor = true
            };
            var btnOk = new Button
            {
                Text = "OK", DialogResult = DialogResult.OK,
                Location = new Point(212, 64), Size = new Size(78, 27),
                Font = std, BackColor = blue, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, UseVisualStyleBackColor = false
            };
            btnOk.FlatAppearance.BorderSize = 0;
            // Pas d'AcceptButton : Enter valide sans fermer
            CancelButton = btnCancel;
            Controls.Add(btnCancel); Controls.Add(btnOk);

            // ── Init value ────────────────────────────────────────────────────
            double init = initialHz ?? Math.Min(Math.Max(1000.0, minHz), maxHz);
            // Set initial value WITHOUT preview (cursor is already there)
            _value = Math.Max(_min, Math.Min(_max, init));
            _txt.Text = _value.ToString("N1", CultureInfo.CurrentCulture);

            _txt.Leave += (_, __) => CommitText();
            _txt.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    CommitText();          // valide + preview
                    e.SuppressKeyPress = true; // empêche le bip
                    e.Handled = true;      // n'atteint pas AcceptButton
                }
            };

            btnOk.Click += (_, __) => { CommitText(); SelectedHz = _value; };
        }
    }
}
#nullable restore
