namespace AudioHexViewer
{
    partial class CursorSetValueDialog
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblC1;
        private System.Windows.Forms.Label lblC2;

        private System.Windows.Forms.NumericUpDown nudC1;
        private System.Windows.Forms.NumericUpDown nudC2;

        private System.Windows.Forms.Button btnPeakC1;
        private System.Windows.Forms.Button btnPeakC2;

        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            lblC1 = new Label();
            lblC2 = new Label();
            nudC1 = new NumericUpDown();
            nudC2 = new NumericUpDown();
            btnPeakC1 = new Button();
            btnPeakC2 = new Button();
            btnOk = new Button();
            btnCancel = new Button();
            ((System.ComponentModel.ISupportInitialize)nudC1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudC2).BeginInit();
            SuspendLayout();
            // 
            // lblC1
            // 
            lblC1.AutoSize = true;
            lblC1.Location = new Point(3, 14);
            lblC1.Name = "lblC1";
            lblC1.Size = new Size(53, 15);
            lblC1.TabIndex = 0;
            lblC1.Text = "C1 f (Hz)";
            // 
            // lblC2
            // 
            lblC2.AutoSize = true;
            lblC2.Location = new Point(3, 46);
            lblC2.Name = "lblC2";
            lblC2.Size = new Size(53, 15);
            lblC2.TabIndex = 3;
            lblC2.Text = "C2 f (Hz)";
            // 
            // nudC1
            // 
            nudC1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            nudC1.DecimalPlaces = 2;
            nudC1.Location = new Point(62, 12);
            nudC1.Maximum = new decimal(new int[] { 10000000, 0, 0, 0 });
            nudC1.Name = "nudC1";
            nudC1.Size = new Size(106, 23);
            nudC1.TabIndex = 1;
            // 
            // nudC2
            // 
            nudC2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            nudC2.DecimalPlaces = 2;
            nudC2.Location = new Point(62, 44);
            nudC2.Maximum = new decimal(new int[] { 10000000, 0, 0, 0 });
            nudC2.Name = "nudC2";
            nudC2.Size = new Size(106, 23);
            nudC2.TabIndex = 4;
            // 
            // btnPeakC1
            // 
            btnPeakC1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPeakC1.Location = new Point(174, 12);
            btnPeakC1.Name = "btnPeakC1";
            btnPeakC1.Size = new Size(75, 25);
            btnPeakC1.TabIndex = 2;
            btnPeakC1.Text = "Set to Peak";
            btnPeakC1.UseVisualStyleBackColor = true;
            // 
            // btnPeakC2
            // 
            btnPeakC2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPeakC2.Location = new Point(174, 44);
            btnPeakC2.Name = "btnPeakC2";
            btnPeakC2.Size = new Size(75, 25);
            btnPeakC2.TabIndex = 5;
            btnPeakC2.Text = "Set to Peak";
            btnPeakC2.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Location = new Point(159, 86);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(90, 27);
            btnOk.TabIndex = 6;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(18, 86);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(90, 27);
            btnCancel.TabIndex = 7;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // CursorSetValueDialog
            // 
            AcceptButton = btnOk;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(261, 125);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
            Controls.Add(btnPeakC2);
            Controls.Add(nudC2);
            Controls.Add(lblC2);
            Controls.Add(btnPeakC1);
            Controls.Add(nudC1);
            Controls.Add(lblC1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "CursorSetValueDialog";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Set Cursor Frequency";
            ((System.ComponentModel.ISupportInitialize)nudC1).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudC2).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
