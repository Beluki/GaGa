
// GaGa.
// A single icon radio player on the Windows notification area.


using System;
using System.Windows.Forms;


namespace GaGa
{
    public class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.Run(new GaGa());
        }
    }
}

