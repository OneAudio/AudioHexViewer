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
            btnCancel = new Button();
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
            lblFMin.Size = new Size(68, 15);
            lblFMin.TabIndex = 2;
            lblFMin.Text = "F min (Hz)";
            // 
            // lblFMax
            // 
            lblFMax.AutoSize = true;
            lblFMax.Location = new Point(12, 120);
            lblFMax.Name = "lblFMax";
            lblFMax.Size = new Size(70, 15);
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
            numFMin.DecimalPlaces = 0;
            numFMin.Location = new Point(110, 83);
            numFMin.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 }); // 1 000 000 Hz
            numFMin.Value = 0;
            numFMin.Name = "numFMin";
            numFMin.Size = new Size(90, 23);
            numFMin.TabIndex = 6;
            // 
            // numFMax
            // 
            numFMax.DecimalPlaces = 0;
            numFMax.Location = new Point(110, 118);
            numFMax.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            numFMax.Name = "numFMax";
            numFMax.Size = new Size(90, 23);
            numFMax.TabIndex = 7;
            numFMax.Value   = 20000;   // par ex. 20 kHz au départ
            // 
            // btnOk
            // 
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Location = new Point(125, 165);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(75, 23);
            btnOk.TabIndex = 8;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(12, 165);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 23);
            btnCancel.TabIndex = 9;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // SpectrumAxisDialog
            // 
            AcceptButton = btnOk;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(220, 200);
            Controls.Add(btnCancel);
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
        private System.Windows.Forms.Button btnCancel;
    }
}
