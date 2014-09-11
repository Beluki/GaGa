
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
            // default path for the settings and the streams file:
            String currentFolder = Util.ApplicationFolder;
            String settingsFilepath = Path.Combine(currentFolder, "GaGa.dat");
            String streamsFilepath = Path.Combine(currentFolder, "Streams.ini");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GaGa(settingsFilepath, streamsFilepath));
        }
    }
}

