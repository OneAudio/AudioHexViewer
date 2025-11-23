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
            label3 = new Label();
            DeviceDatabox = new RichTextBox();
            label4 = new Label();
            btnRecord = new Button();
            lblSampleRate = new Label();
            comboSampleRate = new ComboBox();
            label5 = new Label();
            lblSatLeft = new Label();
            ledSatLeft = new Label();
            lblSatRight = new Label();
            ledSatRight = new Label();
            barLeftLevel = new ProgressBar();
            barRightLevel = new ProgressBar();
            lblLeftLevel = new Label();
            lblRightLevel = new Label();
            SuspendLayout();
            // 
            // comboDevices
            // 
            comboDevices.FormattingEnabled = true;
            comboDevices.Location = new Point(201, 49);
            comboDevices.Name = "comboDevices";
            comboDevices.Size = new Size(203, 23);
            comboDevices.TabIndex = 0;
            // 
            // btnStart
            // 
            btnStart.Location = new Point(440, 87);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(80, 23);
            btnStart.TabIndex = 1;
            btnStart.Text = "READ";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;
            // 
            // txtLeft
            // 
            txtLeft.Font = new Font("Consolas", 9F);
            txtLeft.Location = new Point(189, 165);
            txtLeft.Name = "txtLeft";
            txtLeft.ReadOnly = true;
            txtLeft.ScrollBars = RichTextBoxScrollBars.None;
            txtLeft.Size = new Size(331, 78);
            txtLeft.TabIndex = 5;
            txtLeft.TabStop = false;
            txtLeft.Text = "";
            // 
            // txtRight
            // 
            txtRight.Font = new Font("Consolas", 9F);
            txtRight.Location = new Point(189, 305);
            txtRight.Name = "txtRight";
            txtRight.ReadOnly = true;
            txtRight.ScrollBars = RichTextBoxScrollBars.None;
            txtRight.Size = new Size(331, 78);
            txtRight.TabIndex = 7;
            txtRight.TabStop = false;
            txtRight.Text = "";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(201, 31);
            label1.Name = "label1";
            label1.Size = new Size(139, 15);
            label1.TabIndex = 5;
            label1.Text = "Select input audio device";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(189, 129);
            label2.Name = "label2";
            label2.Size = new Size(120, 15);
            label2.TabIndex = 6;
            label2.Text = "LEFT CHANNEL DATA";
            label2.TextAlign = ContentAlignment.TopCenter;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(189, 266);
            label3.Name = "label3";
            label3.Size = new Size(129, 15);
            label3.TabIndex = 7;
            label3.Text = "RIGHT CHANNEL DATA";
            // 
            // DeviceDatabox
            // 
            DeviceDatabox.Location = new Point(12, 49);
            DeviceDatabox.Name = "DeviceDatabox";
            DeviceDatabox.Size = new Size(171, 335);
            DeviceDatabox.TabIndex = 8;
            DeviceDatabox.Text = "";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 31);
            label4.Name = "label4";
            label4.Size = new Size(105, 15);
            label4.TabIndex = 9;
            label4.Text = "Audio device infos";
            // 
            // btnRecord
            // 
            btnRecord.Location = new Point(324, 87);
            btnRecord.Name = "btnRecord";
            btnRecord.Size = new Size(80, 23);
            btnRecord.TabIndex = 11;
            btnRecord.Text = "RECORD";
            btnRecord.UseVisualStyleBackColor = true;
            btnRecord.Click += btnRecord_Click;
            // 
            // lblSampleRate
            // 
            lblSampleRate.AutoSize = true;
            lblSampleRate.Location = new Point(440, 31);
            lblSampleRate.Name = "lblSampleRate";
            lblSampleRate.Size = new Size(69, 15);
            lblSampleRate.TabIndex = 10;
            lblSampleRate.Text = "Sample rate";
            // 
            // comboSampleRate
            // 
            comboSampleRate.FormattingEnabled = true;
            comboSampleRate.Location = new Point(440, 49);
            comboSampleRate.Name = "comboSampleRate";
            comboSampleRate.Size = new Size(80, 23);
            comboSampleRate.TabIndex = 11;
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
            // lblSatLeft
            // 
            lblSatLeft.AutoSize = true;
            lblSatLeft.Location = new Point(408, 129);
            lblSatLeft.Name = "lblSatLeft";
            lblSatLeft.Size = new Size(38, 15);
            lblSatLeft.TabIndex = 13;
            lblSatLeft.Text = "SAT L:";
            lblSatLeft.Click += LblSatLeft_Click;
            // 
            // ledSatLeft
            // 
            ledSatLeft.BackColor = Color.Gray;
            ledSatLeft.BorderStyle = BorderStyle.FixedSingle;
            ledSatLeft.Location = new Point(431, 147);
            ledSatLeft.Name = "ledSatLeft";
            ledSatLeft.Size = new Size(15, 15);
            ledSatLeft.TabIndex = 14;
            ledSatLeft.Click += ledSatLeft_Click;
            // 
            // lblSatRight
            // 
            lblSatRight.AutoSize = true;
            lblSatRight.Location = new Point(408, 266);
            lblSatRight.Name = "lblSatRight";
            lblSatRight.Size = new Size(39, 15);
            lblSatRight.TabIndex = 15;
            lblSatRight.Text = "SAT R:";
            // 
            // ledSatRight
            // 
            ledSatRight.BackColor = Color.Gray;
            ledSatRight.BorderStyle = BorderStyle.FixedSingle;
            ledSatRight.Location = new Point(431, 284);
            ledSatRight.Name = "ledSatRight";
            ledSatRight.Size = new Size(15, 15);
            ledSatRight.TabIndex = 16;
            // 
            // barLeftLevel
            // 
            barLeftLevel.Location = new Point(189, 147);
            barLeftLevel.Name = "barLeftLevel";
            barLeftLevel.Size = new Size(236, 15);
            barLeftLevel.TabIndex = 0;
            // 
            // barRightLevel
            // 
            barRightLevel.Location = new Point(189, 284);
            barRightLevel.Name = "barRightLevel";
            barRightLevel.Size = new Size(236, 16);
            barRightLevel.TabIndex = 0;
            barRightLevel.Click += barRightLevel_Click;
            // 
            // lblLeftLevel
            // 
            lblLeftLevel.AutoSize = true;
            lblLeftLevel.Location = new Point(469, 147);
            lblLeftLevel.Name = "lblLeftLevel";
            lblLeftLevel.Size = new Size(51, 15);
            lblLeftLevel.TabIndex = 0;
            lblLeftLevel.Text = "-∞ dBFS";
            lblLeftLevel.Click += lblLeftLevel_Click;
            // 
            // lblRightLevel
            // 
            lblRightLevel.AutoSize = true;
            lblRightLevel.Location = new Point(469, 284);
            lblRightLevel.Name = "lblRightLevel";
            lblRightLevel.Size = new Size(51, 15);
            lblRightLevel.TabIndex = 0;
            lblRightLevel.Text = "-∞ dBFS";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(528, 398);
            Controls.Add(label5);
            Controls.Add(comboSampleRate);
            Controls.Add(lblSampleRate);
            Controls.Add(label4);
            Controls.Add(DeviceDatabox);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(btnStart);
            Controls.Add(comboDevices);
            Controls.Add(btnRecord);
            Controls.Add(lblSatLeft);
            Controls.Add(ledSatLeft);
            Controls.Add(lblSatRight);
            Controls.Add(ledSatRight);
            Controls.Add(txtLeft);
            Controls.Add(txtRight);
            Controls.Add(barLeftLevel);
            Controls.Add(lblLeftLevel);
            Controls.Add(barRightLevel);
            Controls.Add(lblRightLevel);
            HelpButton = true;
            Name = "Form1";
            Text = "AudioHexViewer";
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
        private Label label3;
        private RichTextBox DeviceDatabox;
        private Label label4;
        private Button btnRecord;
        private Label lblSampleRate;
        private ComboBox comboSampleRate;
        private Label label5;
        private Label lblSatLeft;
        private Label lblSatRight;
        private Label ledSatLeft;
        private Label ledSatRight;
        private ProgressBar barLeftLevel;
        private ProgressBar barRightLevel;
        private Label lblLeftLevel;
        private Label lblRightLevel;


    }
}
