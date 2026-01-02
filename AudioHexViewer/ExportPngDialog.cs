using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

public sealed class ExportPngDialog : Form
{
    private readonly TextBox _tbPath = new TextBox();
    private readonly Button _btnBrowse = new Button();
    private readonly NumericUpDown _nudScale = new NumericUpDown();
    private readonly TextBox _tbTitle = new TextBox();
    private readonly Label _lblHint = new Label();
    private readonly Button _btnOk = new Button();
    private readonly Button _btnCancel = new Button();

    public string FilePath => _tbPath.Text.Trim();
    public int Scale => (int)_nudScale.Value;
    public string TitleText => _tbTitle.Text;

    public ExportPngDialog()
    {
        Text = "Export PNG";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(520, 260);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 6,
            Padding = new Padding(12),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); // path
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); // scale
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // title
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));  // buttons

        // Path
        layout.Controls.Add(new Label { Text = "Fichier :", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 0);
        _tbPath.Dock = DockStyle.Fill;
        layout.Controls.Add(_tbPath, 1, 0);

        _btnBrowse.Text = "Parcourir…";
        _btnBrowse.Dock = DockStyle.Fill;
        _btnBrowse.Click += (_, __) => Browse();
        layout.Controls.Add(_btnBrowse, 2, 0);

        // Scale
        layout.Controls.Add(new Label { Text = "Scale :", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 2);
        _nudScale.Minimum = 1;
        _nudScale.Maximum = 4;
        _nudScale.Value = 2;
        _nudScale.Dock = DockStyle.Left;
        _nudScale.Width = 80;
        layout.Controls.Add(_nudScale, 1, 2);

        // Title
        var titlePanel = new Panel { Dock = DockStyle.Fill };
        var titleLabel = new Label { Text = "Titre (max 3 lignes, ~50 caractères/ligne) :", Dock = DockStyle.Top, Height = 18 };
        _tbTitle.Multiline = true;
        _tbTitle.AcceptsReturn = true;
        _tbTitle.ScrollBars = ScrollBars.Vertical;
        _tbTitle.Dock = DockStyle.Fill;
        _tbTitle.TextChanged += (_, __) => EnforceTitleLimits();

        _lblHint.Dock = DockStyle.Bottom;
        _lblHint.Height = 18;
        _lblHint.ForeColor = SystemColors.GrayText;

        titlePanel.Controls.Add(_tbTitle);
        titlePanel.Controls.Add(_lblHint);
        titlePanel.Controls.Add(titleLabel);

        layout.Controls.Add(titlePanel, 0, 4);
        layout.SetColumnSpan(titlePanel, 3);

        // Buttons
        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0),
        };

        _btnOk.Text = "Exporter";
        _btnOk.Width = 110;
        _btnOk.Click += (_, __) => OnOk();

        _btnCancel.Text = "Annuler";
        _btnCancel.Width = 110;
        _btnCancel.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };

        btnPanel.Controls.Add(_btnOk);
        btnPanel.Controls.Add(_btnCancel);

        layout.Controls.Add(btnPanel, 0, 5);
        layout.SetColumnSpan(btnPanel, 3);

        Controls.Add(layout);

        EnforceTitleLimits();
    }

    private void Browse()
    {
        using var sfd = new SaveFileDialog
        {
            Filter = "PNG (*.png)|*.png",
            DefaultExt = "png",
            AddExtension = true,
            FileName = "AudioHexViewer_Spectrum.png"
        };

        if (sfd.ShowDialog(this) == DialogResult.OK)
            _tbPath.Text = sfd.FileName;
    }

    private void EnforceTitleLimits()
    {
        // max 3 lignes, ~50 caractères / ligne
        var raw = _tbTitle.Text.Replace("\r\n", "\n");
        var lines = raw.Split('\n');

        var clamped = lines
            .Take(3)
            .Select(l => l.Length > 50 ? l.Substring(0, 50) : l)
            .ToArray();

        var newText = string.Join(Environment.NewLine, clamped);

        if (_tbTitle.Text != newText)
        {
            int sel = _tbTitle.SelectionStart;
            _tbTitle.Text = newText;
            _tbTitle.SelectionStart = Math.Min(sel, _tbTitle.Text.Length);
        }

        _lblHint.Text = $"{clamped.Length}/3 lignes — {clamped.Sum(l => l.Length)}/150 caractères (approx.)";
    }

    private void OnOk()
    {
        if (string.IsNullOrWhiteSpace(FilePath))
        {
            MessageBox.Show(this, "Choisis un fichier de sortie.", "Export PNG", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
