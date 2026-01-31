using System;
using System.Windows.Forms;

namespace AudioHexViewer
{
    public partial class DataViewForm : Form
    {
        public DataViewForm()
        {
            InitializeComponent();

            // On évite de détruire la fenêtre : on la cache à la fermeture
            FormClosing += DataViewForm_FormClosing;
        }

        /// <summary>
        /// Déplace des contrôles existants dans la fenêtre DataView.
        /// Important: on ne duplique rien, on re-parent => rendu IDENTIQUE.
        /// </summary>
        public void AttachControls(params Control[] controls)
        {
            if (controls == null) return;

            // SuspendLayout limite les flickers / recalculs
            panelHost.SuspendLayout();
            try
            {
                foreach (var c in controls)
                {
                    if (c == null) continue;

                    // Re-parenting : le contrôle quitte automatiquement son ancien parent
                    c.Parent = panelHost;
                    c.Visible = true;
                }
            }
            finally
            {
                panelHost.ResumeLayout(true);
            }
        }

        private void DataViewForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // On ne détruit pas (sinon les contrôles seraient disposés)
            e.Cancel = true;
            Hide();
        }
    }
}
