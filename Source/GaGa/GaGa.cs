
// GaGa.


using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;


namespace GaGa
{
    /// <summary>
    /// Actual implementation.
    /// </summary>
    public class GaGa : ApplicationContext
    {
        private Container container;
        private NotifyIcon icon;

        private Icon play_icon;
        private Icon stop_icon;

        public GaGa()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            play_icon = Utils.LoadIconFromResource("GaGa.Resources.play.ico");
            stop_icon = Utils.LoadIconFromResource("GaGa.Resources.stop.ico");

            container = new Container();

            icon = new NotifyIcon(container);
            icon.Icon = play_icon;
            icon.Text = "GaGa";
            icon.Visible = true;
        }
    }
}

