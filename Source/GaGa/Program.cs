
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
            // use a "streams.ini" file located in the same folder
            // as the executable for this application:
            String filepath = Path.Combine(Util.ExeFolder, "streams.ini");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GaGa(filepath));
        }
    }
}

