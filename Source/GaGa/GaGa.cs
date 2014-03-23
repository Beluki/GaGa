
// GaGa.
// A single icon radio player on the Windows notification area.


using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media;

namespace GaGa
{
    /// <summary>
    /// Actual implementation.
    /// </summary>
    public class GaGa : ApplicationContext
    {
        // resources:
        private Icon play_icon;
        private Icon stop_icon;

        // gui components:
        private Container container;
        private NotifyIcon icon;
        private ContextMenuStrip menu;

        // menu loader:
        private StreamsMenuLoader menuloader;

        // player instance:
        private MediaPlayer player;


        public GaGa()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize everything.
        /// </summary>
        private void Initialize()
        {
            // load resources:
            play_icon = Utils.LoadIconFromResource("GaGa.Resources.play.ico");
            stop_icon = Utils.LoadIconFromResource("GaGa.Resources.stop.ico");

            // load gui components:
            container = new Container();
            icon = new NotifyIcon(container);
            menu = new ContextMenuStrip();
            
            icon.Icon = play_icon;
            icon.ContextMenuStrip = menu;
            icon.Text = "GaGa";
            icon.Visible = true;

            menu.Opening += new CancelEventHandler(OnOpenMenu);

            // load dynamic menu:
            menuloader = new StreamsMenuLoader("streams.ini", "GaGa.Resources.streams.ini");

            // load media player instance:
            player = new MediaPlayer();            
        }

        private void OnOpenMenu(Object sender, CancelEventArgs e)
        {
            e.Cancel = false;
 
            menu.SuspendLayout();
            menu.Items.Clear();
            menuloader.ParseStreamsFile();
            ToolStripManager.Merge(menuloader.menu, menu);
            menu.ResumeLayout();
        }
    }
}
