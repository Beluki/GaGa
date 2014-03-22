
// GaGa.
// A lightweight radio player for the Windows tray.


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
        // embedded icons:
        private Icon play_icon;
        private Icon stop_icon;

        // gui components:
        private Container container;
        private ContextMenuStrip menu;
        private NotifyIcon icon;

        // actual player instance:
        private MediaPlayer player;
        private StreamsINIFile streams;

        public GaGa()
        {
            InitializeComponents();
        }

        /// <summary>
        /// Initialize the GUI.
        /// </summary>
        private void InitializeComponents()
        {
            // load resources:
            play_icon = Utils.LoadIconFromResource("GaGa.Resources.play.ico");
            stop_icon = Utils.LoadIconFromResource("GaGa.Resources.stop.ico");

            // load gui components:
            container = new Container();
            menu = new ContextMenuStrip();

            icon = new NotifyIcon(container);
            icon.Icon = play_icon;
            icon.ContextMenuStrip = menu;
            icon.Text = "GaGa";
            icon.Visible = true;
            icon.MouseClick += new MouseEventHandler(OnIconMouseClick);

            // media player and streams file instance:
            player = new MediaPlayer();
            streams = new StreamsINIFile("streams.ini", "GaGa.Resources.streams.ini");
        }

        /// <summary>
        /// Show an error balloon for about 5 seconds.
        /// </summary>
        /// <param name="title">
        /// Balloon title as a string.
        /// </param>
        /// <param name="message">
        /// Error message.
        /// </param>
        private void ErrorBalloon(String title, String message)
        {
            icon.ShowBalloonTip(5, title, message, ToolTipIcon.Error);
        }

        private void OnIconMouseRightClick()
        {
            streams.EnsureExists();

            if (streams.IsOutdated())
            {
                menu.Items.Clear();
                streams.AddToContextMenu(menu);
            }

            menu.Show();
        }

        private void OnIconMouseClick(Object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
               OnIconMouseRightClick();
        }
    }
}
