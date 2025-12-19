namespace AudioHexViewer
{
    partial class SpectrumAxisDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            lblYMax = new Label();
            lblYMin = new Label();
            lblFMin = new Label();
            lblFMax = new Label();
            numYMax = new NumericUpDown();
            numYMin = new NumericUpDown();
            numFMin = new NumericUpDown();
            numFMax = new NumericUpDown();
            btnOk = new Button();
            btnDefault = new Button();
            btnFitY = new Button();
            btnFitX = new Button();
            ((System.ComponentModel.ISupportInitialize)numYMax).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numYMin).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numFMin).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numFMax).BeginInit();
            SuspendLayout();
            // 
            // lblYMax
            // 
            lblYMax.AutoSize = true;
            lblYMax.Location = new Point(12, 15);
            lblYMax.Name = "lblYMax";
            lblYMax.Size = new Size(77, 15);
            lblYMax.TabIndex = 0;
            lblYMax.Text = "Y max (dBFS)";
            // 
            // lblYMin
            // 
            lblYMin.AutoSize = true;
            lblYMin.Location = new Point(12, 50);
            lblYMin.Name = "lblYMin";
            lblYMin.Size = new Size(75, 15);
            lblYMin.TabIndex = 1;
            lblYMin.Text = "Y min (dBFS)";
            // 
            // lblFMin
            // 
            lblFMin.AutoSize = true;
            lblFMin.Location = new Point(12, 85);
            lblFMin.Name = "lblFMin";
            lblFMin.Size = new Size(62, 15);
            lblFMin.TabIndex = 2;
            lblFMin.Text = "F min (Hz)";
            // 
            // lblFMax
            // 
            lblFMax.AutoSize = true;
            lblFMax.Location = new Point(12, 120);
            lblFMax.Name = "lblFMax";
            lblFMax.Size = new Size(64, 15);
            lblFMax.TabIndex = 3;
            lblFMax.Text = "F max (Hz)";
            // 
            // numYMax
            // 
            numYMax.DecimalPlaces = 1;
            numYMax.Location = new Point(110, 13);
            numYMax.Maximum = new decimal(new int[] { 0, 0, 0, 0 });
            numYMax.Minimum = new decimal(new int[] { 300, 0, 0, int.MinValue });
            numYMax.Name = "numYMax";
            numYMax.Size = new Size(90, 23);
            numYMax.TabIndex = 4;
            // 
            // numYMin
            // 
            numYMin.DecimalPlaces = 1;
            numYMin.Location = new Point(110, 48);
            numYMin.Maximum = new decimal(new int[] { 0, 0, 0, 0 });
            numYMin.Minimum = new decimal(new int[] { 300, 0, 0, int.MinValue });
            numYMin.Name = "numYMin";
            numYMin.Size = new Size(90, 23);
            numYMin.TabIndex = 5;
            numYMin.Value = new decimal(new int[] { 200, 0, 0, int.MinValue });
            // 
            // numFMin
            // 
            numFMin.Location = new Point(110, 83);
            numFMin.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            numFMin.Name = "numFMin";
            numFMin.Size = new Size(90, 23);
            numFMin.TabIndex = 6;
            // 
            // numFMax
            // 
            numFMax.Location = new Point(110, 118);
            numFMax.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            numFMax.Name = "numFMax";
            numFMax.Size = new Size(90, 23);
            numFMax.TabIndex = 7;
            numFMax.Value = new decimal(new int[] { 20000, 0, 0, 0 });
            // 
            // btnOk
            // 
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Location = new Point(125, 196);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(75, 23);
            btnOk.TabIndex = 8;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = true;
            // 
            // btnDefault
            // 
            btnDefault.Location = new Point(125, 157);
            btnDefault.Name = "btnDefault";
            btnDefault.Size = new Size(75, 23);
            btnDefault.TabIndex = 8;
            btnDefault.Text = "Default";
            btnDefault.UseVisualStyleBackColor = true;
            // 
            // btnFitY
            // 
            btnFitY.Location = new Point(12, 157);
            btnFitY.Name = "btnFitY";
            btnFitY.Size = new Size(75, 23);
            btnFitY.TabIndex = 9;
            btnFitY.Text = "Fit Y";
            btnFitY.UseVisualStyleBackColor = true;
            // 
            // btnFitX
            // 
            btnFitX.Location = new Point(12, 196);
            btnFitX.Name = "btnFitX";
            btnFitX.Size = new Size(75, 23);
            btnFitX.TabIndex = 10;
            btnFitX.Text = "Fit X";
            btnFitX.UseVisualStyleBackColor = true;
            // 
            // SpectrumAxisDialog
            // 
            AcceptButton = btnOk;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(220, 231);
            Controls.Add(btnFitX);
            Controls.Add(btnFitY);
            Controls.Add(btnDefault);
            Controls.Add(btnOk);
            Controls.Add(numFMax);
            Controls.Add(numFMin);
            Controls.Add(numYMin);
            Controls.Add(numYMax);
            Controls.Add(lblFMax);
            Controls.Add(lblFMin);
            Controls.Add(lblYMin);
            Controls.Add(lblYMax);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SpectrumAxisDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "FFT Axis Limits";
            ((System.ComponentModel.ISupportInitialize)numYMax).EndInit();
            ((System.ComponentModel.ISupportInitialize)numYMin).EndInit();
            ((System.ComponentModel.ISupportInitialize)numFMin).EndInit();
            ((System.ComponentModel.ISupportInitialize)numFMax).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblYMax;
        private System.Windows.Forms.Label lblYMin;
        private System.Windows.Forms.Label lblFMin;
        private System.Windows.Forms.Label lblFMax;
        private System.Windows.Forms.NumericUpDown numYMax;
        private System.Windows.Forms.NumericUpDown numYMin;
        private System.Windows.Forms.NumericUpDown numFMin;
        private System.Windows.Forms.NumericUpDown numFMax;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnDefault;
        private System.Windows.Forms.Button btnFitY;
        private System.Windows.Forms.Button btnFitX;

    }
}
