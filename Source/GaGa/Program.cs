
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GaGa());
        }
    }
}

