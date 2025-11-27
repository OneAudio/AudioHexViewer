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
            comboDisplayMode = new ComboBox();
            menuStrip1 = new MenuStrip();
            menuDeviceInfo = new ToolStripMenuItem();
            menuHelp = new ToolStripMenuItem();
            menuAbout = new ToolStripMenuItem();
            menuOpenCal = new ToolStripMenuItem();
            menuOpenLastRecording = new ToolStripMenuItem();
            label6 = new Label();
            label7 = new Label();
            label3 = new Label();
            lblLeftMainTitle = new Label();
            lblLeftMainValue = new Label();
            lblRightMainTitle = new Label();
            lblRightMainValue = new Label();
            chkShowDetails = new CheckBox();
            label4 = new Label();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // comboDevices
            // 
            comboDevices.FormattingEnabled = true;
            comboDevices.Location = new Point(547, 159);
            comboDevices.Name = "comboDevices";
            comboDevices.Size = new Size(186, 23);
            comboDevices.TabIndex = 0;
            // 
            // btnStart
            // 
            btnStart.BackColor = Color.SeaGreen;
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.FlatStyle = FlatStyle.Flat;
            btnStart.ForeColor = Color.White;
            btnStart.Location = new Point(739, 67);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(116, 47);
            btnStart.TabIndex = 1;
            btnStart.Text = "READ";
            btnStart.UseVisualStyleBackColor = false;
            btnStart.Click += btnStart_Click;
            // 
            // txtLeft
            // 
            txtLeft.Font = new Font("Consolas", 9F);
            txtLeft.Location = new Point(11, 190);
            txtLeft.Name = "txtLeft";
            txtLeft.ReadOnly = true;
            txtLeft.ScrollBars = RichTextBoxScrollBars.None;
            txtLeft.Size = new Size(351, 98);
            txtLeft.TabIndex = 5;
            txtLeft.TabStop = false;
            txtLeft.Text = "";
            // 
            // txtRight
            // 
            txtRight.Font = new Font("Consolas", 9F);
            txtRight.Location = new Point(383, 190);
            txtRight.Name = "txtRight";
            txtRight.ReadOnly = true;
            txtRight.ScrollBars = RichTextBoxScrollBars.None;
            txtRight.Size = new Size(350, 97);
            txtRight.TabIndex = 7;
            txtRight.TabStop = false;
            txtRight.Text = "";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(490, 164);
            label1.Name = "label1";
            label1.Size = new Size(42, 15);
            label1.TabIndex = 5;
            label1.Text = "Device";
            label1.Click += label1_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Tahoma", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.ForeColor = SystemColors.HotTrack;
            label2.Location = new Point(12, 55);
            label2.Name = "label2";
            label2.Size = new Size(62, 25);
            label2.TabIndex = 6;
            label2.Text = "LEFT";
            label2.TextAlign = ContentAlignment.TopCenter;
            // 
            // btnRecord
            // 
            btnRecord.BackColor = Color.Firebrick;
            btnRecord.FlatAppearance.BorderSize = 0;
            btnRecord.FlatStyle = FlatStyle.Flat;
            btnRecord.ForeColor = Color.White;
            btnRecord.Location = new Point(796, 38);
            btnRecord.Name = "btnRecord";
            btnRecord.Size = new Size(59, 30);
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
            // barLeftLevel
            // 
            barLeftLevel.BackColor = Color.FromArgb(32, 32, 32);
            barLeftLevel.BorderStyle = BorderStyle.FixedSingle;
            barLeftLevel.Location = new Point(12, 38);
            barLeftLevel.Name = "barLeftLevel";
            barLeftLevel.Size = new Size(350, 15);
            barLeftLevel.TabIndex = 31;
            // 
            // barRightLevel
            // 
            barRightLevel.BackColor = Color.FromArgb(32, 32, 32);
            barRightLevel.BorderStyle = BorderStyle.FixedSingle;
            barRightLevel.Location = new Point(383, 38);
            barRightLevel.Name = "barRightLevel";
            barRightLevel.Size = new Size(350, 16);
            barRightLevel.TabIndex = 32;
            // 
            // lblLeftLevel
            // 
            lblLeftLevel.AutoSize = true;
            lblLeftLevel.Font = new Font("Tahoma", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblLeftLevel.ForeColor = SystemColors.HotTrack;
            lblLeftLevel.Location = new Point(285, 55);
            lblLeftLevel.Name = "lblLeftLevel";
            lblLeftLevel.Size = new Size(77, 19);
            lblLeftLevel.TabIndex = 0;
            lblLeftLevel.Text = "-∞ dBFS";
            // 
            // lblRightLevel
            // 
            lblRightLevel.AutoSize = true;
            lblRightLevel.Font = new Font("Tahoma", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblRightLevel.ForeColor = SystemColors.HotTrack;
            lblRightLevel.Location = new Point(656, 56);
            lblRightLevel.Name = "lblRightLevel";
            lblRightLevel.Size = new Size(77, 19);
            lblRightLevel.TabIndex = 0;
            lblRightLevel.Text = "-∞ dBFS";
            // 
            // btnCalV
            // 
            btnCalV.BackColor = Color.SteelBlue;
            btnCalV.FlatAppearance.BorderSize = 0;
            btnCalV.FlatStyle = FlatStyle.Flat;
            btnCalV.ForeColor = Color.White;
            btnCalV.Location = new Point(739, 38);
            btnCalV.Name = "btnCalV";
            btnCalV.Size = new Size(59, 30);
            btnCalV.TabIndex = 12;
            btnCalV.Text = "CAL V";
            btnCalV.UseVisualStyleBackColor = false;
            btnCalV.Click += btnCalV_Click;
            // 
            // rbInput
            // 
            rbInput.AutoSize = true;
            rbInput.Checked = true;
            rbInput.Location = new Point(437, 164);
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
            rbOutput.Location = new Point(383, 163);
            rbOutput.Name = "rbOutput";
            rbOutput.Size = new Size(48, 19);
            rbOutput.TabIndex = 21;
            rbOutput.TabStop = true;
            rbOutput.Text = "OUT";
            rbOutput.UseVisualStyleBackColor = true;
            rbOutput.CheckedChanged += rbOutput_CheckedChanged;
            // 
            // comboDisplayMode
            // 
            comboDisplayMode.DropDownStyle = ComboBoxStyle.DropDownList;
            comboDisplayMode.FormattingEnabled = true;
            comboDisplayMode.Location = new Point(739, 136);
            comboDisplayMode.Name = "comboDisplayMode";
            comboDisplayMode.Size = new Size(116, 23);
            comboDisplayMode.TabIndex = 30;
            // 
            // menuStrip1
            // 
            menuStrip1.AutoSize = false;
            menuStrip1.Items.AddRange(new ToolStripItem[] { menuDeviceInfo, menuHelp });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(867, 25);
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
            menuHelp.DropDownItems.AddRange(new ToolStripItem[] { menuAbout, menuOpenCal, menuOpenLastRecording });
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
            // menuOpenLastRecording
            // 
            menuOpenLastRecording.Name = "menuOpenLastRecording";
            menuOpenLastRecording.Size = new Size(181, 22);
            menuOpenLastRecording.Text = "Open last recording";
            menuOpenLastRecording.Click += menuOpenLastRecording_Click;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Segoe UI", 7F);
            label6.Location = new Point(164, 56);
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
            label7.Location = new Point(534, 56);
            label7.Name = "label7";
            label7.Size = new Size(52, 12);
            label7.TabIndex = 18;
            label7.Text = "dBFS Level";
            label7.Click += label7_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Tahoma", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3.ForeColor = SystemColors.HotTrack;
            label3.Location = new Point(383, 55);
            label3.Name = "label3";
            label3.Size = new Size(82, 25);
            label3.TabIndex = 19;
            label3.Text = "RIGHT";
            label3.TextAlign = ContentAlignment.TopCenter;
            // 
            // lblLeftMainTitle
            // 
            lblLeftMainTitle.BackColor = SystemColors.ControlLight;
            lblLeftMainTitle.Font = new Font("Tahoma", 12F);
            lblLeftMainTitle.Location = new Point(12, 85);
            lblLeftMainTitle.Name = "lblLeftMainTitle";
            lblLeftMainTitle.Size = new Size(350, 30);
            lblLeftMainTitle.TabIndex = 33;
            lblLeftMainTitle.Text = "Float";
            lblLeftMainTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblLeftMainValue
            // 
            lblLeftMainValue.BackColor = SystemColors.InactiveCaption;
            lblLeftMainValue.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblLeftMainValue.Location = new Point(12, 115);
            lblLeftMainValue.Name = "lblLeftMainValue";
            lblLeftMainValue.Size = new Size(350, 35);
            lblLeftMainValue.TabIndex = 34;
            lblLeftMainValue.Text = "0.000000";
            lblLeftMainValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblRightMainTitle
            // 
            lblRightMainTitle.BackColor = SystemColors.ControlLight;
            lblRightMainTitle.Font = new Font("Tahoma", 12F);
            lblRightMainTitle.Location = new Point(383, 85);
            lblRightMainTitle.Name = "lblRightMainTitle";
            lblRightMainTitle.Size = new Size(350, 30);
            lblRightMainTitle.TabIndex = 35;
            lblRightMainTitle.Text = "Float";
            lblRightMainTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblRightMainValue
            // 
            lblRightMainValue.BackColor = SystemColors.InactiveCaption;
            lblRightMainValue.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblRightMainValue.Location = new Point(383, 115);
            lblRightMainValue.Name = "lblRightMainValue";
            lblRightMainValue.Size = new Size(350, 35);
            lblRightMainValue.TabIndex = 36;
            lblRightMainValue.Text = "0.000000";
            lblRightMainValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // chkShowDetails
            // 
            chkShowDetails.AutoSize = true;
            chkShowDetails.Location = new Point(763, 165);
            chkShowDetails.Name = "chkShowDetails";
            chkShowDetails.Size = new Size(92, 19);
            chkShowDetails.TabIndex = 31;
            chkShowDetails.Text = "Show details";
            chkShowDetails.UseVisualStyleBackColor = true;
            chkShowDetails.CheckedChanged += chkShowDetails_CheckedChanged;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(771, 118);
            label4.Name = "label4";
            label4.Size = new Size(84, 15);
            label4.TabIndex = 37;
            label4.Text = "Display format";
            label4.Click += label4_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(867, 295);
            Controls.Add(label4);
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
            Controls.Add(comboDisplayMode);
            Controls.Add(btnRecord);
            Controls.Add(txtLeft);
            Controls.Add(txtRight);
            Controls.Add(barLeftLevel);
            Controls.Add(lblLeftLevel);
            Controls.Add(barRightLevel);
            Controls.Add(lblRightLevel);
            Controls.Add(btnCalV);
            Controls.Add(lblLeftMainTitle);
            Controls.Add(lblLeftMainValue);
            Controls.Add(lblRightMainTitle);
            Controls.Add(lblRightMainValue);
            Controls.Add(chkShowDetails);
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
        private ComboBox comboDisplayMode;
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
        private Label lblLeftMainTitle;
        private Label lblLeftMainValue;
        private Label lblRightMainTitle;
        private Label lblRightMainValue;
        private CheckBox chkShowDetails;
        private Label label4;
        private ToolStripMenuItem menuOpenLastRecording;
    }
}
