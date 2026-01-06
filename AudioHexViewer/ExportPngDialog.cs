using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;


public sealed class ExportPngDialog : Form
{
    private readonly TextBox _tbPath = new TextBox();
    private readonly Button _btnBrowse = new Button();
    private readonly NumericUpDown _nudScale = new NumericUpDown();
    private readonly RichTextBox _tbTitle = new RichTextBox();

    private readonly Label _lblHint = new Label();
    private readonly Button _btnOk = new Button();
    private readonly Button _btnCancel = new Button();

    public string FilePath => _tbPath.Text.Trim();

    // Export Scale: 1 to 4 
    public int ExpScale => (int)_nudScale.Value;
    public string GetTitleText()
    {
        return _tbTitle.Text;
    }

    public void SetTitleText(string value)
    {
        _tbTitle.Text = value ?? string.Empty;
        EnforceTitleLimits();
    }
    //
    private bool _inEnforce;
    private const int MaxCharsTotal = 150;
    private const int MaxCharsPerLine = 50;

    //
    public ExportPngDialog()
    {
        Text = "Export PNG or PDF";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(360, 260);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 6,
            Padding = new Padding(12),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 65));// labels
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));// inputs
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));// browse button

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); // path
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10)); // spacer
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); // scale
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10)); // spacer
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // title
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));  // buttons

        // Path
        layout.Controls.Add(new Label { Text = "File :", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 0);
        _tbPath.Dock = DockStyle.Fill;
        layout.Controls.Add(_tbPath, 1, 0);

        _btnBrowse.Text = "Browse…";
        _btnBrowse.Dock = DockStyle.Fill;   
        _btnBrowse.Click += (_, __) => Browse();
        layout.Controls.Add(_btnBrowse, 2, 0);

        // Scale
        layout.Controls.Add(new Label { Text = "Scale :", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 2);
        _nudScale.Minimum = 1;
        _nudScale.Maximum = 4;
        _nudScale.Value = 2;
        _nudScale.Dock = DockStyle.Left;
        _nudScale.Width = 80;
        layout.Controls.Add(_nudScale, 1, 2);

        // Title
        // Title (layout robuste: label + hint + textbox)
        var titleLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        titleLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));      // titre label
        titleLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));      // hint / compteur
        titleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));      // zone de texte

        var titleLabel = new Label
        {
            Text = "Main Title (3 lines, 50 characters/lines maxi) :",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _lblHint.Dock = DockStyle.Fill;
        _lblHint.TextAlign = ContentAlignment.MiddleLeft;
        _lblHint.ForeColor = SystemColors.GrayText;

        // IMPORTANT: _tbTitle remplit la 3e ligne
        _tbTitle.Dock = DockStyle.Fill;
        _tbTitle.BorderStyle = BorderStyle.FixedSingle;
        _tbTitle.DetectUrls = false;
        _tbTitle.ScrollBars = RichTextBoxScrollBars.Vertical;
        _tbTitle.WordWrap = true;
        // (_tbTitle.Multiline / AcceptsReturn : NE PAS mettre si RichTextBox)

        titleLayout.Controls.Add(titleLabel, 0, 0);
        titleLayout.Controls.Add(_lblHint, 0, 1);
        titleLayout.Controls.Add(_tbTitle, 0, 2);

        layout.Controls.Add(titleLayout, 0, 4);
        layout.SetColumnSpan(titleLayout, 3);

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0),
        };

        _btnOk.Text = "EXPORT";
        _btnOk.Width = 90;
        _btnOk.Height = 27;
        _btnOk.Click += (_, __) => OnOk();

        _btnCancel.Text = "CANCEL";
        _btnCancel.Width = 90;
        _btnCancel.Height = 27;
        _btnCancel.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };

        btnPanel.Controls.Add(_btnOk);
        btnPanel.Controls.Add(_btnCancel);

        layout.Controls.Add(btnPanel, 0, 5);
        layout.SetColumnSpan(btnPanel, 3);

        Controls.Add(layout);

        EnforceTitleLimits();

        // Events for title textbox
        _tbTitle.TextChanged += (_, __) =>
        {
            EnforceTitleLimits();
            CenterTitleText();
        };
        //
        // Enter et Shift+Enter = même comportement (retour à la ligne Windows)
        _tbTitle.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                // Insère un retour ligne à la position du curseur
                _tbTitle.SelectedText = Environment.NewLine;
                e.SuppressKeyPress = true;
            }
        };

        // Bloque la saisie si la ligne courante a déjà 50 caractères (sauf si on peut auto-wrap)
        _tbTitle.KeyPress += (s, e) =>
        {
            if (char.IsControl(e.KeyChar))
                return;

            // on ne laisse pas RichTextBox gérer Enter (KeyDown s'en charge)
            if (e.KeyChar == '\r' || e.KeyChar == '\n')
            {
                e.Handled = true;
                return;
            }

            if (IsCurrentLineFull(_tbTitle))
            {
                // Si on a encore de la place en lignes ( < 3 ), on auto-wrap: newline + char
                if (GetLineCount(_tbTitle.Text) < 3)
                {
                    _tbTitle.SelectedText = Environment.NewLine + e.KeyChar;
                    e.Handled = true;
                    return;
                }

                // sinon on bloque
                e.Handled = true;
            }
        };

        // Initial centering
        CenterTitleText();


    }

    private void Browse()
    {
        using var sfd = new SaveFileDialog
        {
            Filter = "PNG (*.png)|*.png|PDF (*.pdf)|*.pdf",
            FilterIndex = 1,

            // On gère l'extension nous-mêmes (plus fiable quand on propose plusieurs formats)
            AddExtension = false,

            FileName = "Graph_"
        };

        if (sfd.ShowDialog(this) != DialogResult.OK)
            return;

        string path = sfd.FileName;

        // Si l'utilisateur n'a pas mis d'extension, on la déduit du filtre choisi
        if (string.IsNullOrWhiteSpace(Path.GetExtension(path)))
            path += (sfd.FilterIndex == 2) ? ".pdf" : ".png";

        _tbPath.Text = path;
    }

    //
    private void CenterTitleText()
    {
        int selStart = _tbTitle.SelectionStart;
        int selLen = _tbTitle.SelectionLength;

        _tbTitle.SelectAll();
        _tbTitle.SelectionAlignment = HorizontalAlignment.Center;

        _tbTitle.Select(selStart, selLen);
    }
    //
    private static int GetContentLength(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        // compte sans \r ni \n
        int n = 0;
        foreach (char c in text)
            if (c != '\r' && c != '\n')
                n++;
        return n;
    }

    private static string NormalizeToWrappedLines(string text)
    {
        // Normalise les retours à la ligne
        string t = (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');

        // Split en lignes utilisateur
        var lines = t.Split('\n').ToList();

        // Max 3 lignes (on jette le surplus)
        if (lines.Count > 3)
            lines = lines.Take(3).ToList();

        // Clamp 50 caractères par ligne
        for (int i = 0; i < lines.Count; i++)
            if (lines[i].Length > MaxCharsPerLine)
                lines[i] = lines[i].Substring(0, MaxCharsPerLine);

        // Reconstruit
        string rebuilt = string.Join("\n", lines);

        // Clamp total 150 (utile surtout en cas de collage + retours)
        // Ici on clamp sur le contenu "sans \n" pour rester cohérent avec 3x50.
        int contentLen = rebuilt.Count(c => c != '\n');
        if (contentLen <= MaxCharsTotal)
            return rebuilt;

        // Si jamais ça dépasse (cas étrange), on coupe le contenu tout en gardant les \n
        var sb = new System.Text.StringBuilder(rebuilt.Length);
        int kept = 0;
        foreach (char c in rebuilt)
        {
            if (c == '\n')
            {
                sb.Append(c);
                continue;
            }

            if (kept >= MaxCharsTotal) break;
            sb.Append(c);
            kept++;
        }

        return sb.ToString();
    }


    //
    private void EnforceTitleLimits()
    {
        if (_inEnforce) return;

        try
        {
            _inEnforce = true;

            // Sauve la position du curseur en "position contenu" (sans retours ligne)
            int selStart = _tbTitle.SelectionStart;
            string current = _tbTitle.Text;

            int caretContentPos = 0;
            for (int i = 0; i < current.Length && i < selStart; i++)
                if (current[i] != '\r' && current[i] != '\n')
                    caretContentPos++;

            // Normalise en 3 lignes de 50 max, retours auto, clamp à 150
            string normalized = NormalizeToWrappedLines(current);

            // Réécrit seulement si nécessaire (évite loops)
            string normalizedWin = normalized.Replace("\n", Environment.NewLine);
            if (_tbTitle.Text != normalizedWin)
                _tbTitle.Text = normalizedWin;

            // Restaure le curseur simplement (on clamp si besoin)
            int newSel = selStart;
            if (newSel > _tbTitle.TextLength) newSel = _tbTitle.TextLength;
            _tbTitle.SelectionStart = newSel;
            _tbTitle.SelectionLength = 0;


            // Compteur
            int used = GetContentLength(_tbTitle.Text);
            int remaining = MaxCharsTotal - used;
            if (remaining < 0) remaining = 0;

            // Lignes réelles
            int lines = 1;
            foreach (char c in _tbTitle.Text)
                if (c == '\n') lines++;

            _lblHint.Text = $"{lines}/3 lines — {used}/150 characters — remain {remaining}";
        }
        finally
        {
            _inEnforce = false;
        }
    }
    //
    private static int GetLineCount(string text)
    {
        if (string.IsNullOrEmpty(text)) return 1;
        // normalise puis compte
        string t = text.Replace("\r\n", "\n").Replace('\r', '\n');
        return 1 + t.Count(c => c == '\n');
    }

    private static bool IsCurrentLineFull(RichTextBox rtb)
    {
        int lineIndex = rtb.GetLineFromCharIndex(rtb.SelectionStart);
        if (lineIndex < 0) lineIndex = 0;

        string[] lines = rtb.Lines; // RichTextBox découpe déjà en lignes
        if (lines.Length == 0) return false;
        if (lineIndex >= lines.Length) lineIndex = lines.Length - 1;

        // longueur de la ligne courante
        return lines[lineIndex].Length >= MaxCharsPerLine;
    }

    //
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
