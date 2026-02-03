namespace AudioHexViewer
{
    partial class OverlaysForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Button btnCapture;
        private System.Windows.Forms.Button btnImport;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.ListView listOverlays; 
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.ImageList imgOverlayColors;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Button btnOpenFolder;


        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            btnCapture = new Button();
            btnImport = new Button();
            btnExport = new Button();
            btnDelete = new Button();
            btnClose = new Button();
            btnRemove = new Button();
            btnOpenFolder = new Button();
            listOverlays = new ListView();
            imgOverlayColors = new ImageList(components);
            SuspendLayout();
            // 
            // btnCapture
            // 
            btnCapture.Location = new Point(12, 12);
            btnCapture.Name = "btnCapture";
            btnCapture.Size = new Size(90, 27);
            btnCapture.TabIndex = 0;
            btnCapture.Text = "📌 Record";
            btnCapture.UseVisualStyleBackColor = true;
            btnCapture.Click += btnCapture_Click;
            // 
            // btnImport
            // 
            btnImport.Location = new Point(108, 12);
            btnImport.Name = "btnImport";
            btnImport.Size = new Size(90, 27);
            btnImport.TabIndex = 1;
            btnImport.Text = "📂 Import";
            btnImport.UseVisualStyleBackColor = true;
            btnImport.Click += btnImport_Click;
            // 
            // btnExport
            // 
            btnExport.Location = new Point(204, 12);
            btnExport.Name = "btnExport";
            btnExport.Size = new Size(90, 27);
            btnExport.TabIndex = 2;
            btnExport.Text = "💾 Export";
            btnExport.UseVisualStyleBackColor = true;
            btnExport.Click += btnExport_Click;
            // 
            // btnDelete
            // 
            btnDelete.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnDelete.Location = new Point(12, 332);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(90, 27);
            btnDelete.TabIndex = 6;
            btnDelete.Text = "🗑 Delete";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += btnDelete_Click;
            // 
            // btnClose
            // 
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClose.Location = new Point(300, 332);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(90, 27);
            btnClose.TabIndex = 5;
            btnClose.Text = "Close";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click;
            // 
            // btnRemove
            // 
            btnRemove.Location = new Point(300, 12);
            btnRemove.Name = "btnRemove";
            btnRemove.Size = new Size(90, 27);
            btnRemove.TabIndex = 3;
            btnRemove.Text = "Remove";
            btnRemove.UseVisualStyleBackColor = true;
            btnRemove.Click += btnRemove_Click;
            // 
            // btnOpenFolder
            // 
            this.btnOpenFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnOpenFolder.Location = new System.Drawing.Point(108, 332);
            this.btnOpenFolder.Name = "btnOpenFolder";
            this.btnOpenFolder.Size = new System.Drawing.Size(110, 27);
            this.btnOpenFolder.TabIndex = 7;
            this.btnOpenFolder.Text = "Overlay folder";
            this.btnOpenFolder.UseVisualStyleBackColor = true;
            this.btnOpenFolder.Click += new System.EventHandler(this.btnOpenFolder_Click);

            // 
            // listOverlays
            // 
            listOverlays.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listOverlays.CheckBoxes = true;
            listOverlays.FullRowSelect = true;
            listOverlays.Location = new Point(12, 47);
            listOverlays.Name = "listOverlays";
            listOverlays.Size = new Size(378, 274);
            listOverlays.SmallImageList = imgOverlayColors;
            listOverlays.TabIndex = 4;
            listOverlays.UseCompatibleStateImageBehavior = false;
            listOverlays.View = View.Details;
            listOverlays.ItemChecked += listOverlays_ItemChecked;
            listOverlays.DoubleClick += listOverlays_DoubleClick;
            //
            // 
            this.listOverlays = new System.Windows.Forms.ListView();
            this.imgOverlayColors = new System.Windows.Forms.ImageList();
            this.imgOverlayColors.ImageSize = new System.Drawing.Size(14, 14);
            this.imgOverlayColors.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this.listOverlays.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listOverlays.CheckBoxes = true;
            this.listOverlays.FullRowSelect = true;
            this.listOverlays.HideSelection = false;
            this.listOverlays.Location = new System.Drawing.Point(12, 47);
            this.listOverlays.MultiSelect = true;
            this.listOverlays.Name = "listOverlays";
            this.listOverlays.Size = new System.Drawing.Size(378, 274);
            this.listOverlays.TabIndex = 4;
            this.listOverlays.UseCompatibleStateImageBehavior = false;
            this.listOverlays.View = System.Windows.Forms.View.Details;
            this.listOverlays.Columns.Add("Nom", 145);
            this.listOverlays.Columns.Add("Type", 65);
            this.listOverlays.Columns.Add("SR", 60);
            this.listOverlays.Columns.Add("Range", 100);
            this.listOverlays.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.listOverlays_ItemChecked);
            this.listOverlays.DoubleClick += new System.EventHandler(this.listOverlays_DoubleClick);
            this.listOverlays.SmallImageList = this.imgOverlayColors;
            // 
            // imgOverlayColors
            // 
            imgOverlayColors.ColorDepth = ColorDepth.Depth32Bit;
            imgOverlayColors.ImageSize = new Size(14, 14);
            imgOverlayColors.TransparentColor = Color.Transparent;
            // 
            // OverlaysForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(402, 371);
            Controls.Add(btnClose);
            Controls.Add(listOverlays);
            Controls.Add(btnDelete);
            Controls.Add(btnExport);
            Controls.Add(btnImport);
            Controls.Add(btnCapture);
            Controls.Add(btnRemove);
            Controls.Add(btnOpenFolder);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Name = "OverlaysForm";
            Text = "Overlays";
            ResumeLayout(false);
        }
    }
}
