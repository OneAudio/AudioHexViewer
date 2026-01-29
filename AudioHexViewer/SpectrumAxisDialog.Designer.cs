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
            cmbYRange = new ComboBox();
            lblY2Max = new Label();
            lblY2Min = new Label();
            numY2Max = new NumericUpDown();
            cmbY2Range = new ComboBox();
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
            btnFitY2 = new Button();
            ((System.ComponentModel.ISupportInitialize)numYMax).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numY2Max).BeginInit();
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
            lblYMax.Size = new Size(85, 15);
            lblYMax.TabIndex = 0;
            lblYMax.Text = "Level max (dB)";
            // 
            // lblYMin
            // 
            lblYMin.AutoSize = true;
            lblYMin.Location = new Point(12, 50);
            lblYMin.Name = "lblYMin";
            lblYMin.Size = new Size(83, 15);
            lblYMin.TabIndex = 1;
            lblYMin.Text = "Level min (dB)";
            // 
            // numYMax
            // 
            numYMax.Increment = new decimal(new int[] { 5, 0, 0, 0 });
            numYMax.Location = new Point(114, 12);
            numYMax.Name = "numYMax";
            numYMax.Size = new Size(90, 23);
            numYMax.TabIndex = 2;
            // 
            // cmbYRange
            // 
            cmbYRange.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbYRange.FormattingEnabled = true;
            cmbYRange.Location = new Point(114, 47);
            cmbYRange.Name = "cmbYRange";
            cmbYRange.Size = new Size(90, 23);
            cmbYRange.TabIndex = 3;
            // 
            // lblY2Max
            // 
            lblY2Max.AutoSize = true;
            lblY2Max.Location = new Point(12, 85);
            lblY2Max.Name = "lblY2Max";
            lblY2Max.Size = new Size(80, 15);
            lblY2Max.TabIndex = 20;
            lblY2Max.Text = "Phase max (°)";
            // 
            // lblY2Min
            // 
            lblY2Min.AutoSize = true;
            lblY2Min.Location = new Point(12, 120);
            lblY2Min.Name = "lblY2Min";
            lblY2Min.Size = new Size(87, 15);
            lblY2Min.TabIndex = 21;
            lblY2Min.Text = "Phase range (°)";
            // 
            // numY2Max
            // 
            numY2Max.Increment = new decimal(new int[] { 15, 0, 0, 0 });
            numY2Max.Location = new Point(114, 82);
            numY2Max.Name = "numY2Max";
            numY2Max.Size = new Size(90, 23);
            numY2Max.TabIndex = 22;
            // 
            // cmbY2Range
            // 
            cmbY2Range.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbY2Range.FormattingEnabled = true;
            cmbY2Range.Location = new Point(114, 117);
            cmbY2Range.Name = "cmbY2Range";
            cmbY2Range.Size = new Size(90, 23);
            cmbY2Range.TabIndex = 23;
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
            tabMinMax.Size = new Size(188, 69);
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
            btnOk.Location = new Point(189, 253);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(75, 23);
            btnOk.TabIndex = 8;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = true;
            // 
            // btnDefault
            // 
            btnDefault.Location = new Point(16, 253);
            btnDefault.Name = "btnDefault";
            btnDefault.Size = new Size(75, 23);
            btnDefault.TabIndex = 6;
            btnDefault.Text = "Default";
            btnDefault.UseVisualStyleBackColor = true;
            // 
            // btnFitY
            // 
            btnFitY.Location = new Point(210, 12);
            btnFitY.Name = "btnFitY";
            btnFitY.Size = new Size(54, 58);
            btnFitY.TabIndex = 5;
            btnFitY.Text = "Auto Fit Level";
            btnFitY.UseVisualStyleBackColor = true;
            // 
            // btnFitX
            // 
            btnFitX.Location = new Point(210, 174);
            btnFitX.Name = "btnFitX";
            btnFitX.Size = new Size(54, 58);
            btnFitX.TabIndex = 7;
            btnFitX.Text = "Fit to Fs/2";
            btnFitX.UseVisualStyleBackColor = true;
            // 
            // btnFitY2
            // 
            btnFitY2.Location = new Point(210, 82);
            btnFitY2.Name = "btnFitY2";
            btnFitY2.Size = new Size(54, 58);
            btnFitY2.TabIndex = 24;
            btnFitY2.Text = "Auto Fit Phase";
            btnFitY2.UseVisualStyleBackColor = true;
            btnFitY2.Click += BtnFitY2_Click;
            // 
            // SpectrumAxisDialog
            // 
            AcceptButton = btnOk;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(271, 287);
            Controls.Add(btnFitY2);
            Controls.Add(btnOk);
            Controls.Add(btnFitX);
            Controls.Add(btnDefault);
            Controls.Add(btnFitY);
            Controls.Add(cmbY2Range);
            Controls.Add(numY2Max);
            Controls.Add(lblY2Min);
            Controls.Add(lblY2Max);
            Controls.Add(tabX);
            Controls.Add(cmbYRange);
            Controls.Add(numYMax);
            Controls.Add(lblYMin);
            Controls.Add(lblYMax);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SpectrumAxisDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Axis Parameters";
            ((System.ComponentModel.ISupportInitialize)numYMax).EndInit();
            ((System.ComponentModel.ISupportInitialize)numY2Max).EndInit();
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
        private ComboBox cmbYRange;

        private Label lblY2Max;
        private Label lblY2Min;
        private NumericUpDown numY2Max;
        private ComboBox cmbY2Range;

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
        private Button btnFitY2;
    }
}
