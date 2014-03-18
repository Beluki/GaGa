
// GaGa.


using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;


namespace GaGa
{
    public class GaGa : ApplicationContext
    {
        Container container;
        NotifyIcon icon;

        Icon play_icon;
        Icon stop_icon;

        public GaGa()
        {
            initializeComponents();
        }

        private void initializeComponents()
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

