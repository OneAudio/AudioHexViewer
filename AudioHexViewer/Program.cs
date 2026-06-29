// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Frex - OnE Audio Projects
// Part of AudioHexViewer - see LICENSE for details.

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

            // 3. Lance Form1 sur le même écran que le splash
            var mainForm = new Form1();
            var splashScreen = System.Windows.Forms.Screen.FromControl(splash);
            mainForm.StartPosition = FormStartPosition.Manual;
            var wa = splashScreen.WorkingArea;
            mainForm.Location = new System.Drawing.Point(
                wa.X + Math.Max(0, (wa.Width  - mainForm.Width)  / 2),
                wa.Y + Math.Max(0, (wa.Height - mainForm.Height) / 2));
            mainForm.Shown += (_, _) => splash.SignalReady();
            splash.FormClosed += (_, _) => splash.Dispose();

            Application.Run(mainForm);
        }
    }
}
