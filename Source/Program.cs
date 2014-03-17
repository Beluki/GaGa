
// GaGa.

using System;
using System.Windows.Forms;

namespace GaGa
{
    public class Run
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run();
        }
    }
}

