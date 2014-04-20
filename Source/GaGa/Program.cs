
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.IO;
using System.Windows.Forms;


namespace GaGa
{
    internal class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            // by default use an streams.ini file located
            // in the same folder as the executable:
            String streamsFilePath = Path.Combine(Utils.ApplicationDirectory(), "streams.ini");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GaGa(streamsFilePath));
        }
    }
}

