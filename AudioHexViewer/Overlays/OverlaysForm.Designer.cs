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


        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.btnCapture = new System.Windows.Forms.Button();
            this.btnImport = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnCapture
            // 
            this.btnCapture.Location = new System.Drawing.Point(12, 12);
            this.btnCapture.Name = "btnCapture";
            this.btnCapture.Size = new System.Drawing.Size(90, 27);
            this.btnCapture.TabIndex = 0;
            this.btnCapture.Text = "📌 Capturer";
            this.btnCapture.UseVisualStyleBackColor = true;
            this.btnCapture.Click += new System.EventHandler(this.btnCapture_Click);
            // 
            // btnImport
            // 
            this.btnImport.Location = new System.Drawing.Point(108, 12);
            this.btnImport.Name = "btnImport";
            this.btnImport.Size = new System.Drawing.Size(90, 27);
            this.btnImport.TabIndex = 1;
            this.btnImport.Text = "📂 Import";
            this.btnImport.UseVisualStyleBackColor = true;
            this.btnImport.Click += new System.EventHandler(this.btnImport_Click);
            // 
            // btnExport
            // 
            this.btnExport.Location = new System.Drawing.Point(204, 12);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(90, 27);
            this.btnExport.TabIndex = 2;
            this.btnExport.Text = "💾 Export";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // btnDelete
            // 
            this.btnDelete.Location = new System.Drawing.Point(300, 12);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(90, 27);
            this.btnDelete.TabIndex = 3;
            this.btnDelete.Text = "🗑 Supprimer";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // listOverlays
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
            this.listOverlays.Columns.Add("Nom", 240);
            this.listOverlays.Columns.Add("Type", 70);
            this.listOverlays.Columns.Add("SR", 60);
            this.listOverlays.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.listOverlays_ItemChecked);
            this.listOverlays.DoubleClick += new System.EventHandler(this.listOverlays_DoubleClick);
            this.listOverlays.SmallImageList = this.imgOverlayColors;
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.Location = new System.Drawing.Point(300, 332);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(90, 27);
            this.btnClose.TabIndex = 5;
            this.btnClose.Text = "Fermer";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // OverlaysForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(402, 371);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.listOverlays);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.btnImport);
            this.Controls.Add(this.btnCapture);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "OverlaysForm";
            this.Text = "Overlays";
            this.ResumeLayout(false);
        }
    }
}
