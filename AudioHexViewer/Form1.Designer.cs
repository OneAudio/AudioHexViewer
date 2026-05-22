using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label5 = new Label();
            menuStrip1 = new MenuStrip();
            menuDevices = new ToolStripMenuItem();
            menuHelp = new ToolStripMenuItem();
            menuAbout = new ToolStripMenuItem();
            menuOpenCal = new ToolStripMenuItem();
            menuOpenRecordsFolder = new ToolStripMenuItem();
            alwaysOnTopToolStripMenuItem = new ToolStripMenuItem();
            menuData = new ToolStripMenuItem();
            menuShowAllFormats = new ToolStripMenuItem();
            menuDataView = new ToolStripMenuItem();
            menuDebugInfo = new ToolStripMenuItem();
            panelDebugInfo = new Panel();
            lblDebugInfo = new Label();
            menuSampleSize = new ToolStripMenuItem();
            menuOverlap = new ToolStripMenuItem();
            menuBuffer = new ToolStripMenuItem();
            exportToolStripMenuItem = new ToolStripMenuItem();
            menuExportPng = new ToolStripMenuItem();
            panelTop = new Panel();
            lblRightLevelMain = new Label();
            lblLeftLevelMain = new Label();
            label6 = new Label();
            barLeftLevel = new Panel();
            lblFormatInfo = new Label();
            barRightLevel = new Panel();
            label7 = new Label();
            btnRecord = new Button();
            btnCalV = new Button();
            tabAnalysis = new TabControl();
            tabHistogram = new TabPage();
            panelHistogram = new Panel();
            tabSpectrum = new TabPage();
            tabOscilloscope = new TabPage();
            txtLeft = new RichTextBox();
            txtRight = new RichTextBox();
            menuStrip1.SuspendLayout();
            panelTop.SuspendLayout();
            tabAnalysis.SuspendLayout();
            tabHistogram.SuspendLayout();
            SuspendLayout();
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            label5.Location = new Point(12, 4);
            label5.Name = "label5";
            label5.Size = new Size(156, 21);
            label5.TabIndex = 12;
            label5.Text = "OnE Audio Projects";
            // 
            // menuStrip1
            // 
            menuStrip1.AutoSize = false;
            menuStrip1.Items.AddRange(new ToolStripItem[] { menuDevices, menuHelp, menuData, menuSampleSize, menuOverlap, menuBuffer, exportToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1138, 25);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // menuDevices
            menuDevices.Alignment = ToolStripItemAlignment.Right;
            menuDevices.Name      = "menuDevices";
            menuDevices.Size      = new Size(59, 21);
            menuDevices.Text      = "Devices";
            menuDevices.Click    += menuDevices_Click;
            // 
            // menuHelp
            // 
            menuHelp.Alignment = ToolStripItemAlignment.Right;
            menuHelp.DropDownItems.AddRange(new ToolStripItem[] { menuAbout, menuOpenCal, menuOpenRecordsFolder, alwaysOnTopToolStripMenuItem });
            menuHelp.Name = "menuHelp";
            menuHelp.Size = new Size(44, 21);
            menuHelp.Text = "Help";
            // 
            // menuAbout
            // 
            menuAbout.Name = "menuAbout";
            menuAbout.Size = new Size(181, 22);
            menuAbout.Text = "About";
            menuAbout.Click += menuAbout_Click;
            // 
            // menuOpenCal
            // 
            menuOpenCal.Name = "menuOpenCal";
            menuOpenCal.Size = new Size(181, 22);
            menuOpenCal.Text = "Open calibration file";
            menuOpenCal.Click += menuOpenCal_Click;
            // 
            // menuOpenRecordsFolder
            // 
            menuOpenRecordsFolder.Name = "menuOpenRecordsFolder";
            menuOpenRecordsFolder.Size = new Size(181, 22);
            menuOpenRecordsFolder.Text = "Open records folder";
            menuOpenRecordsFolder.Click += menuOpenRecordsFolder_Click;
            // 
            // alwaysOnTopToolStripMenuItem
            // 
            alwaysOnTopToolStripMenuItem.CheckOnClick = true;
            alwaysOnTopToolStripMenuItem.Name = "alwaysOnTopToolStripMenuItem";
            alwaysOnTopToolStripMenuItem.Size = new Size(181, 22);
            alwaysOnTopToolStripMenuItem.Text = "Always on top";
            alwaysOnTopToolStripMenuItem.Click += alwaysOnTopToolStripMenuItem_Click;
            // 
            // menuData
            // 
            menuData.Alignment = ToolStripItemAlignment.Right;
            menuData.DropDownItems.AddRange(new ToolStripItem[] { menuShowAllFormats, menuDataView, menuDebugInfo });
            menuData.Name = "menuData";
            menuData.Size = new Size(44, 21);
            menuData.Text = "View";
            // 
            // menuShowAllFormats
            // 
            menuShowAllFormats.Name = "menuShowAllFormats";
            menuShowAllFormats.Size = new Size(188, 22);
            menuShowAllFormats.Text = "Show all data formats";
            menuShowAllFormats.Click += menuShowAllFormats_Click;
            // 
            // menuDataView
            // 
            menuDataView.Name = "menuDataView";
            menuDataView.Size = new Size(188, 22);
            menuDataView.Text = "DataView";
            menuDataView.Click += menuDataView_Click;
            // 
            // menuDebugInfo
            // 
            menuDebugInfo.CheckOnClick = true;
            menuDebugInfo.Name = "menuDebugInfo";
            menuDebugInfo.Size = new Size(188, 22);
            menuDebugInfo.Text = "Debug info";
            menuDebugInfo.Click += menuDebugInfo_Click;
            // 
            // menuSampleSize
            // 
            menuSampleSize.Alignment = ToolStripItemAlignment.Right;
            menuSampleSize.Name = "menuSampleSize";
            menuSampleSize.Size = new Size(80, 21);
            menuSampleSize.Text = "Sample size";
            // 
            // menuOverlap
            // 
            menuOverlap.Alignment = ToolStripItemAlignment.Right;
            menuOverlap.Name = "menuOverlap";
            menuOverlap.Size = new Size(60, 21);
            menuOverlap.Text = "Overlap";
            // 
            // menuBuffer
            // 
            menuBuffer.Alignment = ToolStripItemAlignment.Right;
            menuBuffer.Name = "menuBuffer";
            menuBuffer.Size = new Size(51, 21);
            menuBuffer.Text = "Buffer";
            // 
            // exportToolStripMenuItem
            // 
            exportToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { menuExportPng });
            exportToolStripMenuItem.Name = "exportToolStripMenuItem";
            exportToolStripMenuItem.Size = new Size(53, 21);
            exportToolStripMenuItem.Text = "Export";
            // 
            // menuExportPng
            // 
            menuExportPng.Name = "menuExportPng";
            menuExportPng.Size = new Size(209, 22);
            menuExportPng.Text = "Export graph to PNG/PDF";
            menuExportPng.Click += menuExportPng_Click;
            // 
            // panelTop
            // 
            panelTop.Controls.Add(lblRightLevelMain);
            panelTop.Controls.Add(lblLeftLevelMain);
            panelTop.Controls.Add(label6);
            panelTop.Controls.Add(barLeftLevel);
            panelTop.Controls.Add(lblFormatInfo);
            panelTop.Controls.Add(barRightLevel);
            panelTop.Controls.Add(label7);
            panelTop.Controls.Add(btnRecord);
            panelTop.Controls.Add(btnCalV);
            panelTop.Dock = DockStyle.Top;
            panelTop.Location = new Point(0, 25);
            panelTop.Name = "panelTop";
            panelTop.Size = new Size(1138, 73);
            panelTop.TabIndex = 38;
            // 
            // panelDebugInfo  (barre de debug sous panelTop, cachée par défaut)
            // 
            panelDebugInfo.Controls.Add(lblDebugInfo);
            panelDebugInfo.Dock = DockStyle.Top;
            panelDebugInfo.Name = "panelDebugInfo";
            panelDebugInfo.Size = new Size(1138, 18);
            panelDebugInfo.BackColor = Color.FromArgb(25, 25, 25);
            panelDebugInfo.Visible = false;
            panelDebugInfo.TabIndex = 39;
            // 
            // lblDebugInfo
            // 
            lblDebugInfo.Dock = DockStyle.Fill;
            lblDebugInfo.Font = new Font("Consolas", 8.25F);
            lblDebugInfo.ForeColor = Color.FromArgb(144, 238, 144);  // LightGreen
            lblDebugInfo.BackColor = Color.Transparent;
            lblDebugInfo.Name = "lblDebugInfo";
            lblDebugInfo.Text = "";
            lblDebugInfo.AutoEllipsis = false;
            lblDebugInfo.AutoSize = false;
            lblDebugInfo.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblRightLevelMain
            // 
            lblRightLevelMain.BackColor = SystemColors.GradientInactiveCaption;
            lblRightLevelMain.Font = new Font("Tahoma", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblRightLevelMain.ForeColor = SystemColors.HotTrack;
            lblRightLevelMain.Location = new Point(449, 47);
            lblRightLevelMain.Name = "lblRightLevelMain";
            lblRightLevelMain.Size = new Size(70, 23);
            lblRightLevelMain.TabIndex = 67;
            lblRightLevelMain.Text = "-∞ dBFS";
            lblRightLevelMain.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblLeftLevelMain
            // 
            lblLeftLevelMain.BackColor = SystemColors.GradientInactiveCaption;
            lblLeftLevelMain.Font = new Font("Tahoma", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblLeftLevelMain.ForeColor = SystemColors.HotTrack;
            lblLeftLevelMain.Location = new Point(449, 15);
            lblLeftLevelMain.Name = "lblLeftLevelMain";
            lblLeftLevelMain.Size = new Size(70, 23);
            lblLeftLevelMain.TabIndex = 66;
            lblLeftLevelMain.Text = "-∞ dBFS";
            lblLeftLevelMain.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Segoe UI", 7F);
            label6.Location = new Point(126, 4);
            label6.Name = "label6";
            label6.Size = new Size(110, 12);
            label6.TabIndex = 54;
            label6.Text = "dBFS Level Left Channel";
            // 
            // barLeftLevel
            // 
            barLeftLevel.BackColor = Color.FromArgb(32, 32, 32);
            barLeftLevel.BorderStyle = BorderStyle.FixedSingle;
            barLeftLevel.Location = new Point(83, 19);
            barLeftLevel.Name = "barLeftLevel";
            barLeftLevel.Size = new Size(360, 15);
            barLeftLevel.TabIndex = 57;
            // 
            // lblFormatInfo
            // 
            lblFormatInfo.BackColor = SystemColors.Highlight;
            lblFormatInfo.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblFormatInfo.ForeColor = SystemColors.ControlLightLight;
            lblFormatInfo.Location = new Point(948, 44);
            lblFormatInfo.Name = "lblFormatInfo";
            lblFormatInfo.Size = new Size(186, 23);
            lblFormatInfo.TabIndex = 63;
            lblFormatInfo.Text = "Format: (unknown)";
            lblFormatInfo.TextAlign = ContentAlignment.TopCenter;
            lblFormatInfo.Click += lblFormatInfo_Click;
            // 
            // barRightLevel
            // 
            barRightLevel.BackColor = Color.FromArgb(32, 32, 32);
            barRightLevel.BorderStyle = BorderStyle.FixedSingle;
            barRightLevel.Location = new Point(83, 52);
            barRightLevel.Name = "barRightLevel";
            barRightLevel.Size = new Size(360, 15);
            barRightLevel.TabIndex = 58;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Segoe UI", 7F);
            label7.Location = new Point(126, 37);
            label7.Name = "label7";
            label7.Size = new Size(117, 12);
            label7.TabIndex = 55;
            label7.Text = "dBFS Level Right Channel";
            // 

            // 

            // 

            // 

            // 
            // btnRecord
            // 
            btnRecord.BackColor = Color.Firebrick;
            btnRecord.FlatAppearance.BorderSize = 0;
            btnRecord.FlatStyle = FlatStyle.Flat;
            btnRecord.ForeColor = Color.White;
            btnRecord.Location = new Point(12, 11);
            btnRecord.Name = "btnRecord";
            btnRecord.Size = new Size(60, 23);
            btnRecord.TabIndex = 43;
            btnRecord.Text = "● REC";
            btnRecord.UseVisualStyleBackColor = false;
            btnRecord.Click += btnRecord_Click;
            // 
            // btnCalV
            // 
            btnCalV.BackColor = Color.SteelBlue;
            btnCalV.FlatAppearance.BorderSize = 0;
            btnCalV.FlatStyle = FlatStyle.Flat;
            btnCalV.ForeColor = Color.White;
            btnCalV.Location = new Point(12, 44);
            btnCalV.Name = "btnCalV";
            btnCalV.Size = new Size(60, 23);
            btnCalV.TabIndex = 44;
            btnCalV.Text = "CAL V";
            btnCalV.UseVisualStyleBackColor = false;
            btnCalV.Click += btnCalV_Click;
            // 
            // tabAnalysis
            // 
            tabAnalysis.Controls.Add(tabHistogram);
            tabAnalysis.Controls.Add(tabSpectrum);
            tabAnalysis.Controls.Add(tabOscilloscope);
            tabAnalysis.Dock = DockStyle.Fill;
            tabAnalysis.Location = new Point(0, 98);
            tabAnalysis.Name = "tabAnalysis";
            tabAnalysis.SelectedIndex = 0;
            tabAnalysis.Size = new Size(1138, 651);
            tabAnalysis.TabIndex = 0;
            // 
            // tabHistogram
            // 
            tabHistogram.Controls.Add(panelHistogram);
            tabHistogram.Location = new Point(4, 24);
            tabHistogram.Name = "tabHistogram";
            tabHistogram.Size = new Size(1130, 623);
            tabHistogram.TabIndex = 0;
            tabHistogram.Text = "Histogram";
            tabHistogram.UseVisualStyleBackColor = true;
            // 
            // panelHistogram
            // 
            panelHistogram.BorderStyle = BorderStyle.FixedSingle;
            panelHistogram.Dock = DockStyle.Fill;
            panelHistogram.Location = new Point(0, 0);
            panelHistogram.Name = "panelHistogram";
            panelHistogram.Size = new Size(1130, 623);
            panelHistogram.TabIndex = 0;
            // 
            // tabSpectrum
            // 
            tabSpectrum.Location = new Point(4, 24);
            tabSpectrum.Name = "tabSpectrum";
            tabSpectrum.Size = new Size(1130, 623);
            tabSpectrum.TabIndex = 1;
            tabSpectrum.Text = "Spectrum";
            tabSpectrum.UseVisualStyleBackColor = true;
            // tabOscilloscope
            tabOscilloscope.Location = new Point(4, 24);
            tabOscilloscope.Name = "tabOscilloscope";
            tabOscilloscope.Size = new Size(1130, 623);
            tabOscilloscope.TabIndex = 2;
            tabOscilloscope.Text = "Oscilloscope";
            tabOscilloscope.UseVisualStyleBackColor = true;
            // 
            // txtLeft
            // 
            txtLeft.Font = new Font("Consolas", 9F);
            txtLeft.Location = new Point(0, 0);
            txtLeft.Name = "txtLeft";
            txtLeft.ReadOnly = true;
            txtLeft.ScrollBars = RichTextBoxScrollBars.None;
            txtLeft.Size = new Size(100, 96);
            txtLeft.TabIndex = 0;
            txtLeft.Text = "";
            txtLeft.Visible = false;
            // 
            // txtRight
            // 
            txtRight.Font = new Font("Consolas", 9F);
            txtRight.Location = new Point(0, 0);
            txtRight.Name = "txtRight";
            txtRight.ReadOnly = true;
            txtRight.ScrollBars = RichTextBoxScrollBars.None;
            txtRight.Size = new Size(100, 96);
            txtRight.TabIndex = 0;
            txtRight.Text = "";
            txtRight.Visible = false;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1138, 749);
            Controls.Add(tabAnalysis);
            Controls.Add(panelDebugInfo);
            Controls.Add(panelTop);
            Controls.Add(menuStrip1);
            Controls.Add(label5);
            HelpButton = true;
            MainMenuStrip = menuStrip1;
            Name = "Form1";
            Text = "AudioHexViewer";
            Load += Form1_Load;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            tabAnalysis.ResumeLayout(false);
            tabHistogram.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Label label5;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem menuDevices;
        private ToolStripMenuItem menuHelp;
        private ToolStripMenuItem menuAbout;
        private ToolStripMenuItem menuOpenCal;
        private ToolStripMenuItem menuOpenRecordsFolder;
        private ToolStripMenuItem alwaysOnTopToolStripMenuItem;
        private Panel panelTop;
        private Label label7;
        private Label label6;
        private Panel barLeftLevel;
        private Panel barRightLevel;
        private Button btnRecord;
        private Button btnCalV;
        // Nouveaux contrôles pour la zone d'analyse
        private TabControl tabAnalysis;
        private TabPage tabHistogram;
        private TabPage tabSpectrum;
        private TabPage tabOscilloscope;
        //
        private Panel panelHistogram;
        private Label lblFormatInfo;
        private ToolStripMenuItem menuData;
        private ToolStripMenuItem menuShowAllFormats;
        private ToolStripMenuItem menuDataView;
        private ToolStripMenuItem menuDebugInfo;
        private Panel panelDebugInfo;
        private Label lblDebugInfo;
        private System.Windows.Forms.RichTextBox txtLeft;
        private System.Windows.Forms.RichTextBox txtRight;
        private ToolStripMenuItem menuSampleSize;
        private ToolStripMenuItem menuOverlap;
        private ToolStripMenuItem menuBuffer;
        private ToolStripMenuItem exportToolStripMenuItem;
        private ToolStripMenuItem menuExportPng;
        internal Label lblLeftLevelMain;
        internal Label lblRightLevelMain;
    }
}
