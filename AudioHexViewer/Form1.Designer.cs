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
            comboDevices = new ComboBox();
            btnStart = new Button();
            txtLeft = new RichTextBox();
            txtRight = new RichTextBox();
            label1 = new Label();
            label2 = new Label();
            btnRecord = new Button();
            label5 = new Label();
            barLeftLevel = new Panel();
            barRightLevel = new Panel();
            lblLeftLevel = new Label();
            lblRightLevel = new Label();
            btnCalV = new Button();
            rbInput = new RadioButton();
            rbOutput = new RadioButton();
            menuStrip1 = new MenuStrip();
            menuDeviceInfo = new ToolStripMenuItem();
            menuHelp = new ToolStripMenuItem();
            menuAbout = new ToolStripMenuItem();
            menuOpenCal = new ToolStripMenuItem();
            label6 = new Label();
            label7 = new Label();
            label3 = new Label();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // comboDevices
            // 
            comboDevices.FormattingEnabled = true;
            comboDevices.Location = new Point(59, 205);
            comboDevices.Name = "comboDevices";
            comboDevices.Size = new Size(188, 23);
            comboDevices.TabIndex = 0;
            // 
            // btnStart
            // 
            btnStart.BackColor = Color.SeaGreen;
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.FlatStyle = FlatStyle.Flat;
            btnStart.ForeColor = Color.White;
            btnStart.Location = new Point(626, 205);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(80, 23);
            btnStart.TabIndex = 1;
            btnStart.Text = "READ";
            btnStart.UseVisualStyleBackColor = false;
            btnStart.Click += btnStart_Click;
            // 
            // txtLeft
            // 
            txtLeft.Font = new Font("Consolas", 9F);
            txtLeft.Location = new Point(11, 98);
            txtLeft.Name = "txtLeft";
            txtLeft.ReadOnly = true;
            txtLeft.ScrollBars = RichTextBoxScrollBars.None;
            txtLeft.Size = new Size(331, 98);
            txtLeft.TabIndex = 5;
            txtLeft.TabStop = false;
            txtLeft.Text = "";
            // 
            // txtRight
            // 
            txtRight.Font = new Font("Consolas", 9F);
            txtRight.Location = new Point(375, 98);
            txtRight.Name = "txtRight";
            txtRight.ReadOnly = true;
            txtRight.ScrollBars = RichTextBoxScrollBars.None;
            txtRight.Size = new Size(331, 97);
            txtRight.TabIndex = 7;
            txtRight.TabStop = false;
            txtRight.Text = "";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(11, 209);
            label1.Name = "label1";
            label1.Size = new Size(42, 15);
            label1.TabIndex = 5;
            label1.Text = "Device";
            label1.Click += label1_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Tahoma", 15.75F);
            label2.Location = new Point(207, 37);
            label2.Name = "label2";
            label2.Size = new Size(57, 25);
            label2.TabIndex = 6;
            label2.Text = "LEFT";
            label2.TextAlign = ContentAlignment.TopCenter;
            label2.Click += label2_Click_1;
            // 
            // btnRecord
            // 
            btnRecord.BackColor = Color.Firebrick;
            btnRecord.FlatAppearance.BorderSize = 0;
            btnRecord.FlatStyle = FlatStyle.Flat;
            btnRecord.ForeColor = Color.White;
            btnRecord.Location = new Point(509, 206);
            btnRecord.Name = "btnRecord";
            btnRecord.Size = new Size(80, 23);
            btnRecord.TabIndex = 11;
            btnRecord.Text = "● REC";
            btnRecord.UseVisualStyleBackColor = false;
            btnRecord.Click += btnRecord_Click;
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
            //
            // barLeftLevel
            barLeftLevel.BackColor = Color.FromArgb(32, 32, 32);   // fond gris très sombre
            barLeftLevel.BorderStyle = BorderStyle.FixedSingle;
            barLeftLevel.Location = new Point(11, 77);
            barLeftLevel.Name = "barLeftLevel";
            barLeftLevel.Size = new Size(279, 15);

            // barRightLevel
            barRightLevel.BackColor = Color.FromArgb(32, 32, 32);
            barRightLevel.BorderStyle = BorderStyle.FixedSingle;
            barRightLevel.Location = new Point(375, 77);
            barRightLevel.Name = "barRightLevel";
            barRightLevel.Size = new Size(279, 16);
            // lblLeftLevel
            // 
            lblLeftLevel.AutoSize = true;
            lblLeftLevel.Location = new Point(291, 77);
            lblLeftLevel.Name = "lblLeftLevel";
            lblLeftLevel.Size = new Size(51, 15);
            lblLeftLevel.TabIndex = 0;
            lblLeftLevel.Text = "-∞ dBFS";
            lblLeftLevel.Click += lblLeftLevel_Click;
            // 
            // lblRightLevel
            // 
            lblRightLevel.AutoSize = true;
            lblRightLevel.Location = new Point(655, 76);
            lblRightLevel.Name = "lblRightLevel";
            lblRightLevel.Size = new Size(51, 15);
            lblRightLevel.TabIndex = 0;
            lblRightLevel.Text = "-∞ dBFS";
            // 
            // btnCalV
            // 
            btnCalV.BackColor = Color.SteelBlue;
            btnCalV.FlatAppearance.BorderSize = 0;
            btnCalV.FlatStyle = FlatStyle.Flat;
            btnCalV.ForeColor = Color.White;
            btnCalV.Location = new Point(444, 206);
            btnCalV.Name = "btnCalV";
            btnCalV.Size = new Size(47, 23);
            btnCalV.TabIndex = 12;
            btnCalV.Text = "CAL V";
            btnCalV.UseVisualStyleBackColor = false;
            btnCalV.Click += btnCalV_Click;
            // 
            // rbInput
            // 
            rbInput.AutoSize = true;
            rbInput.Checked = true;
            rbInput.Location = new Point(253, 207);
            rbInput.Name = "rbInput";
            rbInput.Size = new Size(37, 19);
            rbInput.TabIndex = 20;
            rbInput.TabStop = true;
            rbInput.Text = "IN";
            rbInput.UseVisualStyleBackColor = true;
            rbInput.CheckedChanged += rbInput_CheckedChanged;
            // 
            // rbOutput
            // 
            rbOutput.AutoSize = true;
            rbOutput.Location = new Point(296, 207);
            rbOutput.Name = "rbOutput";
            rbOutput.Size = new Size(48, 19);
            rbOutput.TabIndex = 21;
            rbOutput.TabStop = true;
            rbOutput.Text = "OUT";
            rbOutput.UseVisualStyleBackColor = true;
            rbOutput.CheckedChanged += rbOutput_CheckedChanged;
            // 
            // menuStrip1
            // 
            menuStrip1.AutoSize = false;
            menuStrip1.Items.AddRange(new ToolStripItem[] { menuDeviceInfo, menuHelp });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(717, 25);
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
            menuHelp.DropDownItems.AddRange(new ToolStripItem[] { menuAbout, menuOpenCal });
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
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Segoe UI", 7F);
            label6.Location = new Point(11, 48);
            label6.Name = "label6";
            label6.Size = new Size(52, 12);
            label6.TabIndex = 17;
            label6.Text = "dBFS Level";
            label6.Click += label6_Click;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Segoe UI", 7F);
            label7.Location = new Point(375, 48);
            label7.Name = "label7";
            label7.Size = new Size(52, 12);
            label7.TabIndex = 18;
            label7.Text = "dBFS Level";
            label7.Click += label7_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Tahoma", 15.75F);
            label3.Location = new Point(555, 37);
            label3.Name = "label3";
            label3.Size = new Size(73, 25);
            label3.TabIndex = 19;
            label3.Text = "RIGHT";
            label3.TextAlign = ContentAlignment.TopCenter;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(717, 240);
            Controls.Add(label3);
            Controls.Add(label7);
            Controls.Add(label6);
            Controls.Add(menuStrip1);
            Controls.Add(label5);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(btnStart);
            Controls.Add(rbInput);
            Controls.Add(rbOutput);
            Controls.Add(comboDevices);
            Controls.Add(btnRecord);
            Controls.Add(txtLeft);
            Controls.Add(txtRight);
            Controls.Add(barLeftLevel);
            Controls.Add(lblLeftLevel);
            Controls.Add(barRightLevel);
            Controls.Add(lblRightLevel);
            Controls.Add(btnCalV);
            HelpButton = true;
            MainMenuStrip = menuStrip1;
            Name = "Form1";
            Text = "AudioHexViewer";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ComboBox comboDevices;
        private Button btnStart;
        private RichTextBox txtLeft;
        private RichTextBox txtRight;
        private Label label1;
        private Label label2;
        private Button btnRecord;
        private Label label5;
        private Panel barLeftLevel;
        private Panel barRightLevel;
        private Label lblLeftLevel;
        private Label lblRightLevel;
        private Button btnCalV;
        private RadioButton rbInput;
        private RadioButton rbOutput;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem menuDeviceInfo;
        private ToolStripMenuItem menuHelp;
        private ToolStripMenuItem menuAbout;
        private ToolStripMenuItem menuOpenCal;
        private Label label6;
        private Label label7;
        private Label label3;
    }
}
