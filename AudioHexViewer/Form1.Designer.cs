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
            lblSampleRate = new Label();
            comboSampleRate = new ComboBox();
            SuspendLayout();
            // 
            // comboDevices
            // 
            comboDevices.FormattingEnabled = true;
            comboDevices.Location = new Point(394, 48);
            comboDevices.Name = "comboDevices";
            comboDevices.Size = new Size(203, 23);
            comboDevices.TabIndex = 0;
            // 
            // btnStart
            // 
            btnStart.Location = new Point(635, 87);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(75, 23);
            btnStart.TabIndex = 1;
            btnStart.Text = "READ";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;
            // 
            // txtLeft
            // 
            txtLeft.Font = new Font("Consolas", 9F);
            txtLeft.Location = new Point(394, 135);
            txtLeft.Name = "txtLeft";
            txtLeft.ReadOnly = true;
            txtLeft.ScrollBars = RichTextBoxScrollBars.None;
            txtLeft.Size = new Size(362, 78);
            txtLeft.TabIndex = 5;
            txtLeft.TabStop = false;
            txtLeft.Text = "";
            // 
            // txtRight
            // 
            txtRight.Font = new Font("Consolas", 9F);
            txtRight.Location = new Point(394, 249);
            txtRight.Name = "txtRight";
            txtRight.ReadOnly = true;
            txtRight.ScrollBars = RichTextBoxScrollBars.None;
            txtRight.Size = new Size(362, 78);
            txtRight.TabIndex = 7;
            txtRight.TabStop = false;
            txtRight.Text = "";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(394, 28);
            label1.Name = "label1";
            label1.Size = new Size(139, 15);
            label1.TabIndex = 5;
            label1.Text = "Select input audio device";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(394, 117);
            label2.Name = "label2";
            label2.Size = new Size(120, 15);
            label2.TabIndex = 6;
            label2.Text = "LEFT CHANNEL DATA";
            label2.TextAlign = ContentAlignment.TopCenter;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(394, 231);
            label3.Name = "label3";
            label3.Size = new Size(129, 15);
            label3.TabIndex = 7;
            label3.Text = "RIGHT CHANNEL DATA";
            // 
            // DeviceDatabox
            // 
            DeviceDatabox.Location = new Point(12, 48);
            DeviceDatabox.Name = "DeviceDatabox";
            DeviceDatabox.Size = new Size(362, 409);
            DeviceDatabox.TabIndex = 8;
            DeviceDatabox.Text = "";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 28);
            label4.Name = "label4";
            label4.Size = new Size(71, 15);
            label4.TabIndex = 9;
            label4.Text = "Device infos";
            // 
            // lblSampleRate
            // 
            lblSampleRate.AutoSize = true;
            lblSampleRate.Location = new Point(635, 31);
            lblSampleRate.Name = "lblSampleRate";
            lblSampleRate.Size = new Size(69, 15);
            lblSampleRate.TabIndex = 10;
            lblSampleRate.Text = "Sample rate";
            // 
            // comboSampleRate
            // 
            comboSampleRate.FormattingEnabled = true;
            comboSampleRate.Location = new Point(635, 48);
            comboSampleRate.Name = "comboSampleRate";
            comboSampleRate.Size = new Size(75, 23);
            comboSampleRate.TabIndex = 11;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(757, 469);
            Controls.Add(comboSampleRate);
            Controls.Add(lblSampleRate);
            Controls.Add(label4);
            Controls.Add(DeviceDatabox);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(txtRight);
            Controls.Add(txtLeft);
            Controls.Add(btnStart);
            Controls.Add(comboDevices);
            HelpButton = true;
            Name = "Form1";
            Text = "OnE Audio Projects - ADC Data reader";
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
        private Label lblSampleRate;
        private ComboBox comboSampleRate;
    }
}
