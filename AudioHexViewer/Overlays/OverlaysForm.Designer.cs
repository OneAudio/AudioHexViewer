#nullable enable
using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    partial class OverlaysForm
    {
        private System.ComponentModel.IContainer components = null;
        private Button btnCapture, btnImport, btnExport, btnRemove;
        private Button btnDelete, btnOpenFolder, btnClose;
        private ListView listOverlays;
        private ImageList imgOverlayColors;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            var std9 = new Font("Segoe UI", 9f);
            Color blue = Color.FromArgb(0, 90, 160);

            imgOverlayColors = new ImageList(components)
            {
                ImageSize   = new Size(14, 14),
                ColorDepth  = ColorDepth.Depth32Bit,
                TransparentColor = Color.Transparent
            };

            // ── Top action bar ────────────────────────────────────────────────
            btnCapture = new Button { Text = "📌 Record",    Location = new Point(12, 12),  Size = new Size(90, 27), Font = std9, TabIndex = 0, UseVisualStyleBackColor = true };
            btnImport  = new Button { Text = "📂 Import",    Location = new Point(108, 12), Size = new Size(90, 27), Font = std9, TabIndex = 1, UseVisualStyleBackColor = true };
            btnExport  = new Button { Text = "💾 Export",    Location = new Point(204, 12), Size = new Size(90, 27), Font = std9, TabIndex = 2, UseVisualStyleBackColor = true };
            btnRemove  = new Button { Text = "Remove",       Location = new Point(300, 12), Size = new Size(90, 27), Font = std9, TabIndex = 3, UseVisualStyleBackColor = true };

            btnCapture.Click += btnCapture_Click;
            btnImport.Click  += btnImport_Click;
            btnExport.Click  += btnExport_Click;
            btnRemove.Click  += btnRemove_Click;

            // ── ListView ──────────────────────────────────────────────────────
            listOverlays = new ListView
            {
                Anchor          = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                CheckBoxes      = true,
                FullRowSelect   = true,
                HideSelection   = false,
                MultiSelect     = true,
                Location        = new Point(12, 47),
                Size            = new Size(378, 274),
                SmallImageList  = imgOverlayColors,
                TabIndex        = 4,
                Font            = std9,
                UseCompatibleStateImageBehavior = false,
                View            = View.Details
            };
            listOverlays.Columns.Add("Nom",   145);
            listOverlays.Columns.Add("Type",   65);
            listOverlays.Columns.Add("SR",     60);
            listOverlays.Columns.Add("Range", 100);
            listOverlays.ItemChecked  += listOverlays_ItemChecked;
            listOverlays.DoubleClick  += listOverlays_DoubleClick;

            // ── Bottom bar ────────────────────────────────────────────────────
            btnDelete = new Button
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Text   = "🗑 Delete", Location = new Point(12, 332), Size = new Size(90, 27),
                Font   = std9, TabIndex = 6, UseVisualStyleBackColor = true
            };
            btnOpenFolder = new Button
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Text   = "Overlay folder", Location = new Point(108, 332), Size = new Size(110, 27),
                Font   = std9, TabIndex = 7, UseVisualStyleBackColor = true
            };
            btnClose = new Button
            {
                Anchor      = AnchorStyles.Bottom | AnchorStyles.Right,
                Text        = "Close", Location = new Point(300, 332), Size = new Size(90, 27),
                Font        = std9, TabIndex = 5,
                BackColor   = blue, ForeColor = Color.White,
                FlatStyle   = FlatStyle.Flat, UseVisualStyleBackColor = false
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnDelete.Click     += btnDelete_Click;
            btnOpenFolder.Click += btnOpenFolder_Click;
            btnClose.Click      += btnClose_Click;

            // ── Form ──────────────────────────────────────────────────────────
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode       = AutoScaleMode.Font;
            ClientSize          = new Size(402, 371);
            Font                = std9;
            FormBorderStyle     = FormBorderStyle.FixedToolWindow;
            Text                = "Overlays";
            Controls.AddRange(new Control[]
            {
                btnCapture, btnImport, btnExport, btnRemove,
                listOverlays,
                btnDelete, btnOpenFolder, btnClose
            });
            ResumeLayout(false);
        }
    }
}
#nullable restore
