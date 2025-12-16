namespace AudioHexViewer
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());

            //Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            //Application.ThreadException += (s, e) =>
            //{
            //    MessageBox.Show(e.Exception.ToString(), "ThreadException");
            //};

            //AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            //{
            //    MessageBox.Show(e.ExceptionObject?.ToString() ?? "Unknown", "UnhandledException");
            //};

        }
    }
}