using System;
using System.Drawing;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AudioHexViewer
{
    internal sealed class DeviceInfoDialog : Form
    {
        private readonly MMDevice?   _inputDevice;
        private readonly MMDevice?   _outputDevice;
        private readonly WaveFormat? _activeFormat;
        private readonly bool        _isCapturing;
        private readonly bool        _isLoopback;
        private readonly int[]       _srOptions;

        // Fonts partagés
        private static readonly Font FSection = new("Segoe UI Semibold", 8.5f);
        private static readonly Font FLabel   = new("Segoe UI", 8.5f);
        private static readonly Font FValue   = new("Segoe UI", 8.5f);
        private static readonly Font FSmall   = new("Segoe UI", 7.5f);
        private static readonly Font FBold    = new("Segoe UI", 8.5f, FontStyle.Bold);

        public DeviceInfoDialog(
            MMDevice?   inputDevice,
            MMDevice?   outputDevice,
            WaveFormat? activeFormat,
            bool        isCapturing,
            bool        isLoopback,
            int[]       srOptions)
        {
            _inputDevice  = inputDevice;
            _outputDevice = outputDevice;
            _activeFormat = activeFormat;
            _isCapturing  = isCapturing;
            _isLoopback   = isLoopback;
            _srOptions    = srOptions;

            Text            = "Device info";
            StartPosition   = FormStartPosition.CenterParent;
            Size            = new Size(680, 440);
            MinimumSize     = new Size(600, 360);
            Font            = FValue;
            BackColor       = SystemColors.Window;

            var tab = new TabControl { Dock = DockStyle.Fill };
            tab.TabPages.Add(BuildPage("  Input  ",  _inputDevice,  _activeFormat, _isCapturing, _isLoopback, true));
            tab.TabPages.Add(BuildPage("  Output  ", _outputDevice, null,           false,        false,       false));

            var pnlBtn = new Panel { Dock = DockStyle.Bottom, Height = 44 };
            var btnClose = new Button
            {
                Text         = "Close",
                Size         = new Size(88, 27),
                DialogResult = DialogResult.OK,
                Anchor       = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnClose.Location = new Point(pnlBtn.Width - 98, 9);
            btnClose.Anchor   = AnchorStyles.Bottom | AnchorStyles.Right;
            pnlBtn.Controls.Add(btnClose);

            Controls.Add(tab);
            Controls.Add(pnlBtn);
            AcceptButton = btnClose;
        }

        // ── Construction d'un onglet ──────────────────────────────────────────
        private TabPage BuildPage(string title, MMDevice? dev, WaveFormat? activeFmt,
                                  bool isCapturing, bool isLoopback, bool isInput)
        {
            var page = new TabPage(title);

            if (dev == null)
            {
                page.Controls.Add(new Label
                {
                    Text      = "No device selected.",
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = SystemColors.GrayText
                });
                return page;
            }

            // Layout 2 colonnes
            var table = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 1,
                Padding     = new Padding(8, 8, 8, 4)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            page.Controls.Add(table);

            // ── Colonne gauche : Identification + Windows format ──────────────
            var left = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoScroll    = false,
                Padding       = new Padding(4, 4, 8, 4)
            };
            table.Controls.Add(left, 0, 0);

            string name = SplashScreen.CachedNames.TryGetValue(dev.ID, out var n) ? n : dev.FriendlyName;

            AddSection(left, "Identification");
            AddRow(left, "Name",   name, bold: true);
            AddRow(left, "ID",     dev.ID, small: true);
            AddRow(left, "Type",   isInput ? "Input (Capture)" : "Output (Render)");
            AddRow(left, "State",  dev.State.ToString(),
                color: dev.State == DeviceState.Active ? Color.FromArgb(0, 140, 0) : Color.OrangeRed);

            AddSection(left, "Windows Shared Format");
            try
            {
                var fmt = dev.AudioClient.MixFormat;
                string enc = fmt.Encoding == WaveFormatEncoding.IeeeFloat ? "IEEE Float" : "PCM";
                AddRow(left, "Encoding",    enc);
                AddRow(left, "Sample rate", $"{fmt.SampleRate:N0} Hz  ({fmt.SampleRate/1000.0:0.###} kHz)");
                AddRow(left, "Bit depth",   $"{fmt.BitsPerSample} bits");
                AddRow(left, "Channels",    $"{fmt.Channels}");
                AddRow(left, "Block align", $"{fmt.BlockAlign} bytes");
                AddRow(left, "Byte rate",   $"{fmt.AverageBytesPerSecond/1000:N0} kB/s");
            }
            catch (Exception ex) { AddRow(left, "Error", ex.Message, color: Color.OrangeRed); }

            // ── Colonne droite : Exclusive SR + Capture status ────────────────
            var right = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoScroll    = false,
                Padding       = new Padding(8, 4, 4, 4)
            };
            table.Controls.Add(right, 1, 0);

            AddSection(right, "Exclusive Mode — Supported Sample Rates");
            int supported = 0;
            foreach (int sr in _srOptions)
            {
                var key = (dev.ID, sr);
                if (DevicesDialog.SrCache.TryGetValue(key, out bool ok) && ok)
                {
                    AddRow(right, "", $"✓   {sr/1000.0:0.###} kHz",
                        color: Color.FromArgb(0, 140, 0));
                    supported++;
                }
            }
            if (supported == 0)
                AddRow(right, "", "None confirmed (shared mode only)", color: Color.Gray);

            if (isInput)
            {
                AddSection(right, "Capture Status");
                AddRow(right, "State",
                    isCapturing ? "● Active" : "○ Idle",
                    color: isCapturing ? Color.FromArgb(0, 160, 0) : Color.Gray,
                    bold: isCapturing);

                if (isCapturing)
                    AddRow(right, "Mode", isLoopback ? "Loopback (output)" : "Direct input");

                if (activeFmt != null)
                {
                    string enc = activeFmt.Encoding == WaveFormatEncoding.IeeeFloat ? "Float" : "PCM";
                    AddRow(right, "Active format",
                        $"{activeFmt.BitsPerSample}-bit {enc}  ·  " +
                        $"{activeFmt.SampleRate:N0} Hz  ·  {activeFmt.Channels} ch");
                }
            }

            return page;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void AddSection(FlowLayoutPanel flow, string title)
        {
            // Marge avant le titre
            flow.Controls.Add(new Panel { Width = 500, Height = 6,
                Margin = new Padding(0) });

            // Titre en bleu
            flow.Controls.Add(new Label
            {
                Text      = title.ToUpperInvariant(),
                Font      = FSection,
                ForeColor = Color.FromArgb(0, 90, 160),
                AutoSize  = false,
                Width     = 280,
                Height    = 18,
                Margin    = new Padding(0, 0, 0, 0)
            });

            // Ligne de séparation SOUS le titre (pas entre le titre et le texte)
            flow.Controls.Add(new Panel
            {
                Width     = 280,
                Height    = 1,
                BackColor = Color.FromArgb(180, 210, 240),
                Margin    = new Padding(0, 1, 0, 5)
            });
        }

        private static void AddRow(FlowLayoutPanel flow, string label, string value,
            bool small = false, bool bold = false, Color? color = null)
        {
            var row = new Panel
            {
                Width  = 280,
                Height = small ? 16 : 20,
                Margin = new Padding(0, 0, 0, 1)
            };

            if (!string.IsNullOrEmpty(label))
            {
                row.Controls.Add(new Label
                {
                    Text      = label + " :",
                    Font      = small ? FSmall : FLabel,
                    ForeColor = SystemColors.GrayText,
                    Location  = new Point(0, 2),
                    Width     = 90,
                    Height    = 18
                });
            }

            int valX = string.IsNullOrEmpty(label) ? 14 : 94;
            row.Controls.Add(new Label
            {
                Text         = value,
                Font         = bold ? FBold : (small ? FSmall : FValue),
                ForeColor    = color ?? SystemColors.ControlText,
                Location     = new Point(valX, 2),
                Width        = 280 - valX - 2,
                Height       = 18,
                AutoEllipsis = true
            });

            flow.Controls.Add(row);
        }
    }
}
