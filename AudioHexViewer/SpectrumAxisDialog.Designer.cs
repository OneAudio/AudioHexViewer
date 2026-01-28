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
            numYMax = new NumericUpDown();
            numYMin = new NumericUpDown();
            lblY2Max = new Label();
            lblY2Min = new Label();
            cmbY2Max = new ComboBox();
            cmbY2Min = new ComboBox();
            tabX = new TabControl();
            tabMinMax = new TabPage();
            lblFMin = new Label();
            numFMin = new NumericUpDown();
            lblFMax = new Label();
            numFMax = new NumericUpDown();
            tabCenterSpan = new TabPage();
            lblCenter = new Label();
            numCenter = new NumericUpDown();
            lblSpan = new Label();
            numSpan = new NumericUpDown();
            btnOk = new Button();
            btnDefault = new Button();
            btnFitY = new Button();
            btnFitX = new Button();
            ((System.ComponentModel.ISupportInitialize)numYMax).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numYMin).BeginInit();
            tabX.SuspendLayout();
            tabMinMax.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numFMin).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numFMax).BeginInit();
            tabCenterSpan.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numCenter).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numSpan).BeginInit();
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
            // numYMax
            // 
            numYMax.DecimalPlaces = 1;
            numYMax.Location = new Point(114, 12);
            numYMax.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            numYMax.Minimum = new decimal(new int[] { 300, 0, 0, int.MinValue });
            numYMax.Name = "numYMax";
            numYMax.Size = new Size(90, 23);
            numYMax.TabIndex = 2;
            // 
            // numYMin
            // 
            numYMin.DecimalPlaces = 1;
            numYMin.Location = new Point(114, 48);
            numYMin.Maximum = new decimal(new int[] { 0, 0, 0, 0 });
            numYMin.Minimum = new decimal(new int[] { 300, 0, 0, int.MinValue });
            numYMin.Name = "numYMin";
            numYMin.Size = new Size(90, 23);
            numYMin.TabIndex = 3;
            numYMin.Value = new decimal(new int[] { 200, 0, 0, int.MinValue });
            //
            //
            // 
            // lblY2Max
            // 
            lblY2Max.AutoSize = true;
            lblY2Max.Location = new Point(12, 85);
            lblY2Max.Name = "lblY2Max";
            lblY2Max.Size = new Size(86, 15);
            lblY2Max.TabIndex = 20;
            lblY2Max.Text = "Y2 max (deg)";

            // 
            // lblY2Min
            // 
            lblY2Min.AutoSize = true;
            lblY2Min.Location = new Point(12, 120);
            lblY2Min.Name = "lblY2Min";
            lblY2Min.Size = new Size(84, 15);
            lblY2Min.TabIndex = 21;
            lblY2Min.Text = "Y2 min (deg)";

            // 
            // cmbY2Max
            // 
            cmbY2Max.FormattingEnabled = true;
            cmbY2Max.Location = new Point(114, 82);
            cmbY2Max.Name = "cmbY2Max";
            cmbY2Max.Size = new Size(90, 23);
            cmbY2Max.TabIndex = 22;

            // 
            // cmbY2Min
            // 
            cmbY2Min.FormattingEnabled = true;
            cmbY2Min.Location = new Point(114, 117);
            cmbY2Min.Name = "cmbY2Min";
            cmbY2Min.Size = new Size(90, 23);
            cmbY2Min.TabIndex = 23;

            //
            // tabX
            // 
            tabX.Controls.Add(tabMinMax);
            tabX.Controls.Add(tabCenterSpan);
            tabX.Location = new Point(12, 150);
            tabX.Name = "tabX";
            tabX.SelectedIndex = 0;
            tabX.Size = new Size(196, 97);
            tabX.TabIndex = 4;
            // 
            // tabMinMax
            // 
            tabMinMax.Controls.Add(lblFMin);
            tabMinMax.Controls.Add(numFMin);
            tabMinMax.Controls.Add(lblFMax);
            tabMinMax.Controls.Add(numFMax);
            tabMinMax.Location = new Point(4, 24);
            tabMinMax.Name = "tabMinMax";
            tabMinMax.Padding = new Padding(3);
            tabMinMax.Size = new Size(188, 64);
            tabMinMax.TabIndex = 0;
            tabMinMax.Text = "Mini/Maxi";
            tabMinMax.UseVisualStyleBackColor = true;
            // 
            // lblFMin
            // 
            lblFMin.AutoSize = true;
            lblFMin.Location = new Point(6, 9);
            lblFMin.Name = "lblFMin";
            lblFMin.Size = new Size(62, 15);
            lblFMin.TabIndex = 0;
            lblFMin.Text = "F min (Hz)";
            // 
            // numFMin
            // 
            numFMin.Location = new Point(80, 7);
            numFMin.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            numFMin.Name = "numFMin";
            numFMin.Size = new Size(90, 23);
            numFMin.TabIndex = 1;
            // 
            // lblFMax
            // 
            lblFMax.AutoSize = true;
            lblFMax.Location = new Point(6, 38);
            lblFMax.Name = "lblFMax";
            lblFMax.Size = new Size(64, 15);
            lblFMax.TabIndex = 2;
            lblFMax.Text = "F max (Hz)";
            // 
            // numFMax
            // 
            numFMax.Location = new Point(80, 36);
            numFMax.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            numFMax.Name = "numFMax";
            numFMax.Size = new Size(90, 23);
            numFMax.TabIndex = 3;
            numFMax.Value = new decimal(new int[] { 20000, 0, 0, 0 });
            // 
            // tabCenterSpan
            // 
            tabCenterSpan.Controls.Add(lblCenter);
            tabCenterSpan.Controls.Add(numCenter);
            tabCenterSpan.Controls.Add(lblSpan);
            tabCenterSpan.Controls.Add(numSpan);
            tabCenterSpan.Location = new Point(4, 24);
            tabCenterSpan.Name = "tabCenterSpan";
            tabCenterSpan.Padding = new Padding(3);
            tabCenterSpan.Size = new Size(188, 69);
            tabCenterSpan.TabIndex = 1;
            tabCenterSpan.Text = "Center/Span";
            tabCenterSpan.UseVisualStyleBackColor = true;
            // 
            // lblCenter
            // 
            lblCenter.AutoSize = true;
            lblCenter.Location = new Point(6, 9);
            lblCenter.Name = "lblCenter";
            lblCenter.Size = new Size(67, 15);
            lblCenter.TabIndex = 0;
            lblCenter.Text = "Center (Hz)";
            // 
            // numCenter
            // 
            numCenter.Location = new Point(80, 7);
            numCenter.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            numCenter.Name = "numCenter";
            numCenter.Size = new Size(90, 23);
            numCenter.TabIndex = 1;
            // 
            // lblSpan
            // 
            lblSpan.AutoSize = true;
            lblSpan.Location = new Point(6, 38);
            lblSpan.Name = "lblSpan";
            lblSpan.Size = new Size(58, 15);
            lblSpan.TabIndex = 2;
            lblSpan.Text = "Span (Hz)";
            // 
            // numSpan
            // 
            numSpan.Location = new Point(80, 36);
            numSpan.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            numSpan.Name = "numSpan";
            numSpan.Size = new Size(90, 23);
            numSpan.TabIndex = 3;
            // 
            // btnOk
            // 
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Location = new Point(129, 282);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(75, 23);
            btnOk.TabIndex = 8;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = true;
            // 
            // btnDefault
            // 
            btnDefault.Location = new Point(129, 253);
            btnDefault.Name = "btnDefault";
            btnDefault.Size = new Size(75, 23);
            btnDefault.TabIndex = 6;
            btnDefault.Text = "Default";
            btnDefault.UseVisualStyleBackColor = true;
            // 
            // btnFitY
            // 
            btnFitY.Location = new Point(12, 253);
            btnFitY.Name = "btnFitY";
            btnFitY.Size = new Size(75, 23);
            btnFitY.TabIndex = 5;
            btnFitY.Text = "Fit Y";
            btnFitY.UseVisualStyleBackColor = true;
            // 
            // btnFitX
            // 
            btnFitX.Location = new Point(12, 282);
            btnFitX.Name = "btnFitX";
            btnFitX.Size = new Size(75, 23);
            btnFitX.TabIndex = 7;
            btnFitX.Text = "Fit X";
            btnFitX.UseVisualStyleBackColor = true;
            // 
            // SpectrumAxisDialog
            // 
            AcceptButton = btnOk;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(220, 317);
            Controls.Add(btnOk);
            Controls.Add(btnFitX);
            Controls.Add(btnDefault);
            Controls.Add(btnFitY);
            Controls.Add(cmbY2Min);
            Controls.Add(cmbY2Max);
            Controls.Add(lblY2Min);
            Controls.Add(lblY2Max);
            Controls.Add(tabX);
            Controls.Add(numYMin);
            Controls.Add(numYMax);
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
            tabX.ResumeLayout(false);
            tabMinMax.ResumeLayout(false);
            tabMinMax.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numFMin).EndInit();
            ((System.ComponentModel.ISupportInitialize)numFMax).EndInit();
            tabCenterSpan.ResumeLayout(false);
            tabCenterSpan.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numCenter).EndInit();
            ((System.ComponentModel.ISupportInitialize)numSpan).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblYMax;
        private Label lblYMin;
        private NumericUpDown numYMax;
        private NumericUpDown numYMin;

        private Label lblY2Max;
        private Label lblY2Min;
        private ComboBox cmbY2Max;
        private ComboBox cmbY2Min;

        private TabControl tabX;
        private TabPage tabMinMax;
        private TabPage tabCenterSpan;

        private Label lblFMin;
        private Label lblFMax;
        private NumericUpDown numFMin;
        private NumericUpDown numFMax;

        private Label lblCenter;
        private Label lblSpan;
        private NumericUpDown numCenter;
        private NumericUpDown numSpan;

        private Button btnOk;
        private Button btnDefault;
        private Button btnFitY;
        private Button btnFitX;
    }
}
