using System.Windows.Forms;

namespace AudioHexViewer
{
    partial class DataViewForm
    {
        private System.ComponentModel.IContainer components = null;
        private Panel panelHost;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            panelHost = new Panel();
            label3 = new Label();
            label2 = new Label();
            lblLeftLevelDV = new Label();
            lblRightLevelDV = new Label();
            lblLeftMainTitle = new Label();
            lblLeftMainValue = new Label();
            lblRightMainTitle = new Label();
            lblRightMainValue = new Label();
            label4 = new Label();
            comboDisplayMode = new ComboBox();
            panelHost.SuspendLayout();
            SuspendLayout();
            // 
            // panelHost
            // 
            panelHost.Controls.Add(label3);
            panelHost.Controls.Add(label2);
            panelHost.Controls.Add(lblLeftLevelDV);
            panelHost.Controls.Add(lblRightLevelDV);
            panelHost.Controls.Add(lblLeftMainTitle);
            panelHost.Controls.Add(lblLeftMainValue);
            panelHost.Controls.Add(lblRightMainTitle);
            panelHost.Controls.Add(lblRightMainValue);
            panelHost.Controls.Add(label4);
            panelHost.Controls.Add(comboDisplayMode);
            panelHost.Dock = DockStyle.Fill;
            panelHost.Location = new Point(0, 0);
            panelHost.Name = "panelHost";
            panelHost.Size = new Size(742, 129);
            panelHost.TabIndex = 0;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Tahoma", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3.ForeColor = SystemColors.HotTrack;
            label3.Location = new Point(379, 4);
            label3.Name = "label3";
            label3.Size = new Size(82, 25);
            label3.TabIndex = 68;
            label3.Text = "RIGHT";
            label3.TextAlign = ContentAlignment.TopCenter;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Tahoma", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.ForeColor = SystemColors.HotTrack;
            label2.Location = new Point(3, 4);
            label2.Name = "label2";
            label2.Size = new Size(62, 25);
            label2.TabIndex = 67;
            label2.Text = "LEFT";
            label2.TextAlign = ContentAlignment.TopCenter;
            // 
            // lblLeftLevelDV
            // 
            lblLeftLevelDV.BackColor = SystemColors.GradientInactiveCaption;
            lblLeftLevelDV.Font = new Font("Tahoma", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblLeftLevelDV.ForeColor = SystemColors.HotTrack;
            lblLeftLevelDV.Location = new Point(263, 1);
            lblLeftLevelDV.Name = "lblLeftLevelDV";
            lblLeftLevelDV.Size = new Size(100, 30);
            lblLeftLevelDV.TabIndex = 65;
            lblLeftLevelDV.Text = "-∞ dBFS";
            lblLeftLevelDV.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblRightLevelDV
            // 
            lblRightLevelDV.BackColor = SystemColors.GradientInactiveCaption;
            lblRightLevelDV.Font = new Font("Tahoma", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblRightLevelDV.ForeColor = SystemColors.HotTrack;
            lblRightLevelDV.Location = new Point(639, 1);
            lblRightLevelDV.Name = "lblRightLevelDV";
            lblRightLevelDV.Size = new Size(100, 30);
            lblRightLevelDV.TabIndex = 66;
            lblRightLevelDV.Text = "-∞ dBFS";
            lblRightLevelDV.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblLeftMainTitle
            // 
            lblLeftMainTitle.BackColor = SystemColors.ControlLight;
            lblLeftMainTitle.Font = new Font("Tahoma", 12F);
            lblLeftMainTitle.Location = new Point(3, 32);
            lblLeftMainTitle.Name = "lblLeftMainTitle";
            lblLeftMainTitle.Size = new Size(360, 30);
            lblLeftMainTitle.TabIndex = 69;
            lblLeftMainTitle.Text = "Float";
            lblLeftMainTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblLeftMainValue
            // 
            lblLeftMainValue.BackColor = SystemColors.InactiveCaption;
            lblLeftMainValue.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblLeftMainValue.Location = new Point(3, 62);
            lblLeftMainValue.Name = "lblLeftMainValue";
            lblLeftMainValue.Size = new Size(360, 35);
            lblLeftMainValue.TabIndex = 70;
            lblLeftMainValue.Text = "0.000000";
            lblLeftMainValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblRightMainTitle
            // 
            lblRightMainTitle.BackColor = SystemColors.ControlLight;
            lblRightMainTitle.Font = new Font("Tahoma", 12F);
            lblRightMainTitle.Location = new Point(379, 32);
            lblRightMainTitle.Name = "lblRightMainTitle";
            lblRightMainTitle.Size = new Size(360, 30);
            lblRightMainTitle.TabIndex = 71;
            lblRightMainTitle.Text = "Float";
            lblRightMainTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblRightMainValue
            // 
            lblRightMainValue.BackColor = SystemColors.InactiveCaption;
            lblRightMainValue.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblRightMainValue.Location = new Point(379, 62);
            lblRightMainValue.Name = "lblRightMainValue";
            lblRightMainValue.Size = new Size(360, 35);
            lblRightMainValue.TabIndex = 72;
            lblRightMainValue.Text = "0.000000";
            lblRightMainValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(447, 105);
            label4.Name = "label4";
            label4.Size = new Size(99, 15);
            label4.TabIndex = 64;
            label4.Text = "Main data format";
            // 
            // comboDisplayMode
            // 
            comboDisplayMode.DropDownStyle = ComboBoxStyle.DropDownList;
            comboDisplayMode.FormattingEnabled = true;
            comboDisplayMode.Location = new Point(552, 100);
            comboDisplayMode.Name = "comboDisplayMode";
            comboDisplayMode.Size = new Size(187, 23);
            comboDisplayMode.TabIndex = 63;
            // 
            // DataViewForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(742, 129);
            Controls.Add(panelHost);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Name = "DataViewForm";
            SizeGripStyle = SizeGripStyle.Hide;
            StartPosition = FormStartPosition.Manual;
            Text = "DataView";
            panelHost.ResumeLayout(false);
            panelHost.PerformLayout();
            ResumeLayout(false);
        }
        internal ComboBox comboDisplayMode;
        internal Label lblLeftLevelDV;
        internal Label lblRightLevelDV;
        internal Label lblLeftMainTitle;
        internal Label lblLeftMainValue;
        internal Label lblRightMainTitle;
        internal Label lblRightMainValue;
        internal Label label3;
        internal Label label2;
        internal Label label4;
    }
}
