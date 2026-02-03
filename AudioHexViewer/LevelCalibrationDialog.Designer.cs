#nullable enable
using System.Drawing;
using System.Windows.Forms;

namespace AudioHexViewer
{
    internal sealed partial class LevelCalibrationDialog
    {
        private System.ComponentModel.IContainer? components = null;

        private Label _lblInfo = null!;
        private Label _lblLeftCaption = null!;
        private Label _lblRightCaption = null!;

        private TextBox _txtLeft = null!;
        private TextBox _txtRight = null!;

        private Button _btnCalLeft = null!;
        private Button _btnCalRight = null!;

        private Label _lblLeftStatus = null!;
        private Label _lblRightStatus = null!;

        private Button _btnClose = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LevelCalibrationDialog));
            _lblInfo = new Label();
            _lblLeftCaption = new Label();
            _txtLeft = new TextBox();
            _btnCalLeft = new Button();
            _lblLeftStatus = new Label();
            _lblRightCaption = new Label();
            _txtRight = new TextBox();
            _btnCalRight = new Button();
            _lblRightStatus = new Label();
            _btnClose = new Button();
            ResetCAL = new Button();
            SuspendLayout();
            // 
            // _lblInfo
            // 
            _lblInfo.Location = new Point(10, 9);
            _lblInfo.Name = "_lblInfo";
            _lblInfo.Size = new Size(324, 69);
            _lblInfo.TabIndex = 0;
            _lblInfo.Text = resources.GetString("_lblInfo.Text");
            // 
            // _lblLeftCaption
            // 
            _lblLeftCaption.AutoSize = true;
            _lblLeftCaption.Location = new Point(12, 175);
            _lblLeftCaption.Name = "_lblLeftCaption";
            _lblLeftCaption.Size = new Size(68, 15);
            _lblLeftCaption.TabIndex = 1;
            _lblLeftCaption.Text = "Left (Vrms):";
            // 
            // _txtLeft
            // 
            _txtLeft.Location = new Point(92, 172);
            _txtLeft.Name = "_txtLeft";
            _txtLeft.Size = new Size(110, 23);
            _txtLeft.TabIndex = 2;
            // 
            // _btnCalLeft
            // 
            _btnCalLeft.Location = new Point(212, 172);
            _btnCalLeft.Name = "_btnCalLeft";
            _btnCalLeft.Size = new Size(90, 25);
            _btnCalLeft.TabIndex = 3;
            _btnCalLeft.Text = "Calibrate";
            // 
            // _lblLeftStatus
            // 
            _lblLeftStatus.Location = new Point(102, 200);
            _lblLeftStatus.Name = "_lblLeftStatus";
            _lblLeftStatus.Size = new Size(200, 60);
            _lblLeftStatus.TabIndex = 4;
            _lblLeftStatus.TextAlign = ContentAlignment.MiddleLeft;
            _lblLeftStatus.Click += _lblLeftStatus_Click;
            // 
            // _lblRightCaption
            // 
            _lblRightCaption.AutoSize = true;
            _lblRightCaption.Location = new Point(10, 84);
            _lblRightCaption.Name = "_lblRightCaption";
            _lblRightCaption.Size = new Size(76, 15);
            _lblRightCaption.TabIndex = 5;
            _lblRightCaption.Text = "Right (Vrms):";
            // 
            // _txtRight
            // 
            _txtRight.Location = new Point(92, 81);
            _txtRight.Name = "_txtRight";
            _txtRight.Size = new Size(110, 23);
            _txtRight.TabIndex = 6;
            // 
            // _btnCalRight
            // 
            _btnCalRight.Location = new Point(212, 81);
            _btnCalRight.Name = "_btnCalRight";
            _btnCalRight.Size = new Size(90, 25);
            _btnCalRight.TabIndex = 7;
            _btnCalRight.Text = "Calibrate";
            // 
            // _lblRightStatus
            // 
            _lblRightStatus.Location = new Point(102, 109);
            _lblRightStatus.Name = "_lblRightStatus";
            _lblRightStatus.Size = new Size(200, 60);
            _lblRightStatus.TabIndex = 8;
            _lblRightStatus.TextAlign = ContentAlignment.MiddleLeft;
            _lblRightStatus.Click += _lblRightStatus_Click;
            // 
            // _btnClose
            // 
            _btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _btnClose.Location = new Point(210, 269);
            _btnClose.Name = "_btnClose";
            _btnClose.Size = new Size(90, 28);
            _btnClose.TabIndex = 9;
            _btnClose.Text = "Close";
            // 
            // ResetCAL
            // 
            ResetCAL.Location = new Point(12, 269);
            ResetCAL.Name = "ResetCAL";
            ResetCAL.Size = new Size(74, 28);
            ResetCAL.TabIndex = 10;
            ResetCAL.Text = "RESET CAL";
            ResetCAL.UseVisualStyleBackColor = true;
            ResetCAL.Click += _ResetCalibration_Click;
            // 
            // LevelCalibrationDialog
            // 
            ClientSize = new Size(312, 309);
            Controls.Add(ResetCAL);
            Controls.Add(_lblInfo);
            Controls.Add(_lblLeftCaption);
            Controls.Add(_txtLeft);
            Controls.Add(_btnCalLeft);
            Controls.Add(_lblLeftStatus);
            Controls.Add(_lblRightCaption);
            Controls.Add(_txtRight);
            Controls.Add(_btnCalRight);
            Controls.Add(_lblRightStatus);
            Controls.Add(_btnClose);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "LevelCalibrationDialog";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Calibrate Level (dBFS → dBV)";
            ResumeLayout(false);
            PerformLayout();
        }

        private Button ResetCAL;
    }
}
#nullable restore
