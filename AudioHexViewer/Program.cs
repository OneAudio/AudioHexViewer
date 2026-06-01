using System;
using System.Windows.Forms;

namespace AudioHexViewer
{
    internal static class Program
    {
        internal static readonly int[] SampleRateOptions =
        {
            44100, 48000, 88200, 96000,
            176400, 192000, 352800, 384000,
            705600, 768000
        };

        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // 1. Affiche le splash
            var splash = new SplashScreen();
            splash.Show();
            Application.DoEvents();

            // 2. Scan audio intégré dans le splash (barre de progression = avancement réel)
            splash.RunAudioScan(SampleRateOptions);

            // 3. Lance Form1
            var mainForm = new Form1();
            mainForm.Shown += (_, _) => splash.SignalReady();
            splash.FormClosed += (_, _) => splash.Dispose();

            Application.Run(mainForm);
        }
    }
}
