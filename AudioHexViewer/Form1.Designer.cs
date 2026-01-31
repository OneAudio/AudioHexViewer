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
            menuDeviceInfo = new ToolStripMenuItem();
            menuHelp = new ToolStripMenuItem();
            menuAbout = new ToolStripMenuItem();
            menuOpenCal = new ToolStripMenuItem();
            menuOpenRecordsFolder = new ToolStripMenuItem();
            alwaysOnTopToolStripMenuItem = new ToolStripMenuItem();
            menuData = new ToolStripMenuItem();
            menuShowAllFormats = new ToolStripMenuItem();
            menuDataView = new ToolStripMenuItem();
            menuSampleSize = new ToolStripMenuItem();
            menuOverlap = new ToolStripMenuItem();
            menuBuffer = new ToolStripMenuItem();
            exportToolStripMenuItem = new ToolStripMenuItem();
            menuExportPng = new ToolStripMenuItem();
            panelTop = new Panel();
            lblFormatInfo = new Label();
            label7 = new Label();
            label6 = new Label();
            barLeftLevel = new Panel();
            barRightLevel = new Panel();
            label1 = new Label();
            btnStart = new Button();
            rbInput = new RadioButton();
            rbOutput = new RadioButton();
            comboDevices = new ComboBox();
            btnRecord = new Button();
            btnCalV = new Button();
            tabAnalysis = new TabControl();
            tabHistogram = new TabPage();
            panelHistogram = new Panel();
            tabSpectrum = new TabPage();
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
            menuStrip1.Items.AddRange(new ToolStripItem[] { menuDeviceInfo, menuHelp, menuData, menuSampleSize, menuOverlap, menuBuffer, exportToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1138, 25);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // menuDeviceInfo
            // 
            menuDeviceInfo.Alignment = ToolStripItemAlignment.Right;
            menuDeviceInfo.Name = "menuDeviceInfo";
            menuDeviceInfo.Size = new Size(78, 21);
            menuDeviceInfo.Text = "Device info";
            menuDeviceInfo.Click += menuDeviceInfo_Click;
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
            menuData.DropDownItems.AddRange(new ToolStripItem[] { menuShowAllFormats, menuDataView });
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
            panelTop.Controls.Add(lblFormatInfo);
            panelTop.Controls.Add(label7);
            panelTop.Controls.Add(label6);
            panelTop.Controls.Add(barLeftLevel);
            panelTop.Controls.Add(barRightLevel);
            panelTop.Controls.Add(label1);
            panelTop.Controls.Add(btnStart);
            panelTop.Controls.Add(rbInput);
            panelTop.Controls.Add(rbOutput);
            panelTop.Controls.Add(comboDevices);
            panelTop.Controls.Add(btnRecord);
            panelTop.Controls.Add(btnCalV);
            panelTop.Dock = DockStyle.Top;
            panelTop.Location = new Point(0, 25);
            panelTop.Name = "panelTop";
            panelTop.Size = new Size(1138, 160);
            panelTop.TabIndex = 38;
            // 
            // lblFormatInfo
            // 
            lblFormatInfo.AutoSize = true;
            lblFormatInfo.BackColor = SystemColors.Highlight;
            lblFormatInfo.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblFormatInfo.ForeColor = SystemColors.ControlLightLight;
            lblFormatInfo.Location = new Point(765, 85);
            lblFormatInfo.Name = "lblFormatInfo";
            lblFormatInfo.Size = new Size(132, 20);
            lblFormatInfo.TabIndex = 63;
            lblFormatInfo.Text = "Format: (unknown)";
            lblFormatInfo.Click += lblFormatInfo_Click;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Segoe UI", 7F);
            label7.Location = new Point(414, 28);
            label7.Name = "label7";
            label7.Size = new Size(52, 12);
            label7.TabIndex = 55;
            label7.Text = "dBFS Level";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Segoe UI", 7F);
            label6.Location = new Point(40, 28);
            label6.Name = "label6";
            label6.Size = new Size(52, 12);
            label6.TabIndex = 54;
            label6.Text = "dBFS Level";
            // 
            // barLeftLevel
            // 
            barLeftLevel.BackColor = Color.FromArgb(32, 32, 32);
            barLeftLevel.BorderStyle = BorderStyle.FixedSingle;
            barLeftLevel.Location = new Point(8, 42);
            barLeftLevel.Name = "barLeftLevel";
            barLeftLevel.Size = new Size(360, 15);
            barLeftLevel.TabIndex = 57;
            // 
            // barRightLevel
            // 
            barRightLevel.BackColor = Color.FromArgb(32, 32, 32);
            barRightLevel.BorderStyle = BorderStyle.FixedSingle;
            barRightLevel.Location = new Point(384, 42);
            barRightLevel.Name = "barRightLevel";
            barRightLevel.Size = new Size(360, 16);
            barRightLevel.TabIndex = 58;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(765, 39);
            label1.Name = "label1";
            label1.Size = new Size(42, 15);
            label1.TabIndex = 42;
            label1.Text = "Device";
            // 
            // btnStart
            // 
            btnStart.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnStart.BackColor = Color.SeaGreen;
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.FlatStyle = FlatStyle.Flat;
            btnStart.ForeColor = Color.White;
            btnStart.Location = new Point(965, 83);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(161, 70);
            btnStart.TabIndex = 41;
            btnStart.Text = "READ";
            btnStart.UseVisualStyleBackColor = false;
            btnStart.Click += btnStart_Click;
            // 
            // rbInput
            // 
            rbInput.AutoSize = true;
            rbInput.Checked = true;
            rbInput.Location = new Point(914, 37);
            rbInput.Name = "rbInput";
            rbInput.Size = new Size(37, 19);
            rbInput.TabIndex = 45;
            rbInput.TabStop = true;
            rbInput.Text = "IN";
            rbInput.UseVisualStyleBackColor = true;
            // 
            // rbOutput
            // 
            rbOutput.AutoSize = true;
            rbOutput.Location = new Point(860, 37);
            rbOutput.Name = "rbOutput";
            rbOutput.Size = new Size(48, 19);
            rbOutput.TabIndex = 46;
            rbOutput.TabStop = true;
            rbOutput.Text = "OUT";
            rbOutput.UseVisualStyleBackColor = true;
            // 
            // comboDevices
            // 
            comboDevices.FormattingEnabled = true;
            comboDevices.Location = new Point(765, 58);
            comboDevices.Name = "comboDevices";
            comboDevices.Size = new Size(186, 23);
            comboDevices.TabIndex = 40;
            // 
            // btnRecord
            // 
            btnRecord.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRecord.BackColor = Color.Firebrick;
            btnRecord.FlatAppearance.BorderSize = 0;
            btnRecord.FlatStyle = FlatStyle.Flat;
            btnRecord.ForeColor = Color.White;
            btnRecord.Location = new Point(1044, 39);
            btnRecord.Name = "btnRecord";
            btnRecord.Size = new Size(82, 44);
            btnRecord.TabIndex = 43;
            btnRecord.Text = "● REC";
            btnRecord.UseVisualStyleBackColor = false;
            btnRecord.Click += btnRecord_Click;
            // 
            // btnCalV
            // 
            btnCalV.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCalV.BackColor = Color.SteelBlue;
            btnCalV.FlatAppearance.BorderSize = 0;
            btnCalV.FlatStyle = FlatStyle.Flat;
            btnCalV.ForeColor = Color.White;
            btnCalV.Location = new Point(965, 39);
            btnCalV.Name = "btnCalV";
            btnCalV.Size = new Size(82, 44);
            btnCalV.TabIndex = 44;
            btnCalV.Text = "CAL V";
            btnCalV.UseVisualStyleBackColor = false;
            btnCalV.Click += btnCalV_Click;
            // 
            // tabAnalysis
            // 
            tabAnalysis.Controls.Add(tabHistogram);
            tabAnalysis.Controls.Add(tabSpectrum);
            tabAnalysis.Dock = DockStyle.Fill;
            tabAnalysis.Location = new Point(0, 185);
            tabAnalysis.Name = "tabAnalysis";
            tabAnalysis.SelectedIndex = 0;
            tabAnalysis.Size = new Size(1138, 564);
            tabAnalysis.TabIndex = 0;
            // 
            // tabHistogram
            // 
            tabHistogram.Controls.Add(panelHistogram);
            tabHistogram.Location = new Point(4, 24);
            tabHistogram.Name = "tabHistogram";
            tabHistogram.Size = new Size(1130, 536);
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
            panelHistogram.Size = new Size(1130, 536);
            panelHistogram.TabIndex = 0;
            // 
            // tabSpectrum
            // 
            tabSpectrum.Location = new Point(4, 24);
            tabSpectrum.Name = "tabSpectrum";
            tabSpectrum.Size = new Size(1130, 557);
            tabSpectrum.TabIndex = 1;
            tabSpectrum.Text = "Spectrum";
            tabSpectrum.UseVisualStyleBackColor = true;
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
        private ToolStripMenuItem menuDeviceInfo;
        private ToolStripMenuItem menuHelp;
        private ToolStripMenuItem menuAbout;
        private ToolStripMenuItem menuOpenCal;
        private ToolStripMenuItem menuOpenRecordsFolder;
        private ToolStripMenuItem alwaysOnTopToolStripMenuItem;
        private SplitContainer splitMain;
        private Panel panelTop;
        private Label label7;
        private Label label6;
        private Panel barLeftLevel;
        private Panel barRightLevel;
        private Label label1;
        private Button btnStart;
        private RadioButton rbInput;
        private RadioButton rbOutput;
        private ComboBox comboDevices;
        private Button btnRecord;
        private Button btnCalV;
        // Nouveaux contrôles pour la zone d'analyse
        private TabControl tabAnalysis;
        private TabPage tabHistogram;
        private TabPage tabSpectrum;
        //
        private Panel panelHistogram;
        private Label lblFormatInfo;
        private ToolStripMenuItem menuData;
        private ToolStripMenuItem menuShowAllFormats;
        private ToolStripMenuItem menuDataView;
        private System.Windows.Forms.RichTextBox txtLeft;
        private System.Windows.Forms.RichTextBox txtRight;
        private ToolStripMenuItem menuSampleSize;
        private ToolStripMenuItem menuOverlap;
        private ToolStripMenuItem menuBuffer;
        private ToolStripMenuItem exportToolStripMenuItem;
        private ToolStripMenuItem menuExportPng;
    }
}
