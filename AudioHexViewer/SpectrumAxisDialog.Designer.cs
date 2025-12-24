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

            tabX = new TabControl();
            tabMinMax = new TabPage();
            tabCenterSpan = new TabPage();

            lblFMin = new Label();
            lblFMax = new Label();
            numFMin = new NumericUpDown();
            numFMax = new NumericUpDown();

            lblCenter = new Label();
            lblSpan = new Label();
            numCenter = new NumericUpDown();
            numSpan = new NumericUpDown();

            btnOk = new Button();
            btnDefault = new Button();
            btnFitY = new Button();
            btnFitX = new Button();

            ((System.ComponentModel.ISupportInitialize)numYMax).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numYMin).BeginInit();
            tabX.SuspendLayout();
            tabMinMax.SuspendLayout();
            tabCenterSpan.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numFMin).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numFMax).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numCenter).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numSpan).BeginInit();
            SuspendLayout();

            // lblYMax
            lblYMax.AutoSize = true;
            lblYMax.Location = new Point(12, 15);
            lblYMax.Name = "lblYMax";
            lblYMax.Size = new Size(77, 15);
            lblYMax.TabIndex = 0;
            lblYMax.Text = "Y max (dBFS)";

            // lblYMin
            lblYMin.AutoSize = true;
            lblYMin.Location = new Point(12, 50);
            lblYMin.Name = "lblYMin";
            lblYMin.Size = new Size(75, 15);
            lblYMin.TabIndex = 1;
            lblYMin.Text = "Y min (dBFS)";

            // numYMax
            numYMax.DecimalPlaces = 1;
            numYMax.Location = new Point(110, 13);
            numYMax.Maximum = new decimal(new int[] { 0, 0, 0, 0 });
            numYMax.Minimum = new decimal(new int[] { 300, 0, 0, int.MinValue });
            numYMax.Name = "numYMax";
            numYMax.Size = new Size(90, 23);
            numYMax.TabIndex = 2;

            // numYMin
            numYMin.DecimalPlaces = 1;
            numYMin.Location = new Point(110, 48);
            numYMin.Maximum = new decimal(new int[] { 0, 0, 0, 0 });
            numYMin.Minimum = new decimal(new int[] { 300, 0, 0, int.MinValue });
            numYMin.Name = "numYMin";
            numYMin.Size = new Size(90, 23);
            numYMin.TabIndex = 3;
            numYMin.Value = new decimal(new int[] { 200, 0, 0, int.MinValue });

            // tabX
            tabX.Controls.Add(tabMinMax);
            tabX.Controls.Add(tabCenterSpan);
            tabX.Location = new Point(12, 80);
            tabX.Name = "tabX";
            tabX.SelectedIndex = 0;
            tabX.Size = new Size(188, 92);
            tabX.TabIndex = 4;

            // tabMinMax
            tabMinMax.Controls.Add(lblFMin);
            tabMinMax.Controls.Add(numFMin);
            tabMinMax.Controls.Add(lblFMax);
            tabMinMax.Controls.Add(numFMax);
            tabMinMax.Location = new Point(4, 24);
            tabMinMax.Name = "tabMinMax";
            tabMinMax.Padding = new Padding(3);
            tabMinMax.Size = new Size(180, 64);
            tabMinMax.TabIndex = 0;
            tabMinMax.Text = "Mini/Maxi";
            tabMinMax.UseVisualStyleBackColor = true;

            // lblFMin
            lblFMin.AutoSize = true;
            lblFMin.Location = new Point(6, 9);
            lblFMin.Name = "lblFMin";
            lblFMin.Size = new Size(62, 15);
            lblFMin.TabIndex = 0;
            lblFMin.Text = "F min (Hz)";

            // numFMin
            numFMin.Location = new Point(80, 7);
            numFMin.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            numFMin.Name = "numFMin";
            numFMin.Size = new Size(90, 23);
            numFMin.TabIndex = 1;

            // lblFMax
            lblFMax.AutoSize = true;
            lblFMax.Location = new Point(6, 38);
            lblFMax.Name = "lblFMax";
            lblFMax.Size = new Size(64, 15);
            lblFMax.TabIndex = 2;
            lblFMax.Text = "F max (Hz)";

            // numFMax
            numFMax.Location = new Point(80, 36);
            numFMax.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            numFMax.Name = "numFMax";
            numFMax.Size = new Size(90, 23);
            numFMax.TabIndex = 3;
            numFMax.Value = new decimal(new int[] { 20000, 0, 0, 0 });

            // tabCenterSpan
            tabCenterSpan.Controls.Add(lblCenter);
            tabCenterSpan.Controls.Add(numCenter);
            tabCenterSpan.Controls.Add(lblSpan);
            tabCenterSpan.Controls.Add(numSpan);
            tabCenterSpan.Location = new Point(4, 24);
            tabCenterSpan.Name = "tabCenterSpan";
            tabCenterSpan.Padding = new Padding(3);
            tabCenterSpan.Size = new Size(180, 64);
            tabCenterSpan.TabIndex = 1;
            tabCenterSpan.Text = "Center/Span";
            tabCenterSpan.UseVisualStyleBackColor = true;

            // lblCenter
            lblCenter.AutoSize = true;
            lblCenter.Location = new Point(6, 9);
            lblCenter.Name = "lblCenter";
            lblCenter.Size = new Size(69, 15);
            lblCenter.TabIndex = 0;
            lblCenter.Text = "Center (Hz)";

            // numCenter
            numCenter.Location = new Point(80, 7);
            numCenter.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            numCenter.Name = "numCenter";
            numCenter.Size = new Size(90, 23);
            numCenter.TabIndex = 1;

            // lblSpan
            lblSpan.AutoSize = true;
            lblSpan.Location = new Point(6, 38);
            lblSpan.Name = "lblSpan";
            lblSpan.Size = new Size(60, 15);
            lblSpan.TabIndex = 2;
            lblSpan.Text = "Span (Hz)";

            // numSpan
            numSpan.Location = new Point(80, 36);
            numSpan.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            numSpan.Name = "numSpan";
            numSpan.Size = new Size(90, 23);
            numSpan.TabIndex = 3;

            // btnFitY
            btnFitY.Location = new Point(12, 183);
            btnFitY.Name = "btnFitY";
            btnFitY.Size = new Size(75, 23);
            btnFitY.TabIndex = 5;
            btnFitY.Text = "Fit Y";
            btnFitY.UseVisualStyleBackColor = true;

            // btnDefault
            btnDefault.Location = new Point(125, 183);
            btnDefault.Name = "btnDefault";
            btnDefault.Size = new Size(75, 23);
            btnDefault.TabIndex = 6;
            btnDefault.Text = "Default";
            btnDefault.UseVisualStyleBackColor = true;

            // btnFitX
            btnFitX.Location = new Point(12, 212);
            btnFitX.Name = "btnFitX";
            btnFitX.Size = new Size(75, 23);
            btnFitX.TabIndex = 7;
            btnFitX.Text = "Fit X";
            btnFitX.UseVisualStyleBackColor = true;

            // btnOk
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Location = new Point(125, 212);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(75, 23);
            btnOk.TabIndex = 8;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = true;

            // SpectrumAxisDialog
            AcceptButton = btnOk;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(220, 247);
            Controls.Add(btnOk);
            Controls.Add(btnFitX);
            Controls.Add(btnDefault);
            Controls.Add(btnFitY);
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
            tabCenterSpan.ResumeLayout(false);
            tabCenterSpan.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numFMin).EndInit();
            ((System.ComponentModel.ISupportInitialize)numFMax).EndInit();
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
