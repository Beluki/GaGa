
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.ComponentModel;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Media;


namespace GaGa
{
    internal class GaGa : ApplicationContext
    {
        // streams file and loader:
        private StreamsFile streamsFile;
        private StreamsMenuLoader menuLoader;

        // icons:
        private Icon playIcon;
        private Icon stopIcon;
        private Icon muteIcon;

        // gui components:
        private Container container;
        private NotifyIcon icon;
        private ContextMenu menu;

        // non-loader menu items:
        private MenuItem editItem;
        private MenuItem exitItem;
        private MenuItem errorOpenItem;
        private MenuItem errorReadItem;

        // player:
        private MediaPlayer player;
        private RadioStream playerStream;
        private Boolean playerPlaying;

        /// <summary>
        /// GaGa implementation.
        /// </summary>
        /// <param name="streamsFilePath">
        /// File path to use for the streams file.
        /// </param>
        public GaGa(String streamsFilePath)
        {
            // streams file and loader:
            streamsFile = new StreamsFile(streamsFilePath, "GaGa.Resources.default-streams.ini");
            menuLoader = new StreamsMenuLoader(streamsFile);

            // icons:
            playIcon = Utils.LoadIconFromResource("GaGa.Resources.play.ico");
            stopIcon = Utils.LoadIconFromResource("GaGa.Resources.stop.ico");
            muteIcon = Utils.LoadIconFromResource("GaGa.Resources.mute.ico");

            // gui components:
            container = new Container();

            icon = new NotifyIcon(container);
            IconSet(playIcon, "GaGa - Click to open menu");
            icon.Visible = true;
            icon.MouseClick += OnIconMouseClick;

            MenuRecreate();

            // non-loader menu items:
            editItem = new MenuItem("Edit streams file", OnEditItemClick);
            exitItem = new MenuItem("Exit", OnExitItemClick);
            errorOpenItem = new MenuItem("Error opening streams file (click for details)", OnErrorOpenItemClick);
            errorReadItem = new MenuItem("Error reading streams file (click for details)", OnErrorReadItemClick);

            // player state:
            player = new MediaPlayer();
            player.MediaEnded += OnPlayerMediaEnded;
            player.MediaFailed += OnPlayerMediaFailed;

            playerStream = null;
            playerPlaying = false;
        }

        ///
        /// Menu loading.
        ///

        /// <summary>
        /// Clear the menu by deleting it and creating a new one from scratch.
        /// Avoids the problem of menus caching their width.
        /// </summary>
        private void MenuRecreate()
        {
            menu = new ContextMenu();
            menu.Popup += OnMenuPopup;
            icon.ContextMenu = menu;
        }

        /// <summary>
        /// Re-create the context menu on changes from the menuloader.
        /// </summary>
        private void MenuMaybeReload()
        {
            if (menuLoader.MustReload())
            {
                MenuRecreate();
                menuLoader.LoadTo(menu, OnStreamItemClick);
                menu.MenuItems.Add("-");
                menu.MenuItems.Add(editItem);
                menu.MenuItems.Add(exitItem);

                editItem.Enabled = true;
            }
        }

        /// <summary>
        /// Create a context menu containing a clickable error item
        /// on opening errors. The item contains the raised exception
        /// in the .Tag property.
        /// </summary>
        /// <param name="exception">
        /// Error that happened when trying to open the streams file.
        /// </param>
        private void MenuLoadErrorOpen(Exception exception)
        {
            MenuRecreate();
            menu.MenuItems.Add(errorOpenItem);
            menu.MenuItems.Add("-");
            menu.MenuItems.Add(editItem);
            menu.MenuItems.Add(exitItem);

            errorOpenItem.Tag = exception;
            editItem.Enabled = false;
        }

        /// <summary>
        /// Create a context menu containing a clickable error item
        /// on reading errors. The item contains the raised exception
        /// in the .Tag property.
        /// </summary>
        /// <param name="exception">
        /// Error that happened when trying to read the streams file.
        /// </param>
        private void MenuLoadErrorRead(StreamsFileReadError exception)
        {
            MenuRecreate();
            menu.MenuItems.Add(errorReadItem);
            menu.MenuItems.Add("-");
            menu.MenuItems.Add(editItem);
            menu.MenuItems.Add(exitItem);

            errorReadItem.Tag = exception;
            editItem.Enabled = true;
        }

        /// <summary>
        /// Recreate the context menu when needed.
        /// Create alternate menus on read and open errors.
        /// </summary>
        private void MenuUpdate()
        {
            try
            {
                MenuMaybeReload();
            }
            catch (StreamsFileReadError exception)
            {
                MenuLoadErrorRead(exception);
            }
            catch (Exception exception)
            {
                MenuLoadErrorOpen(exception);
            }
        }

        ///
        /// Icon.
        ///

        /// <summary>
        /// Change the notify icon image and text.
        /// </summary>
        /// <param name="icon">Icon image.</param>
        /// <param name="text">Icon text.</param>
        private void IconSet(Icon icon, String text)
        {
            // maximum icon text length is 63 characters, cut when needed:
            if (text.Length > 63)
                text = text.Substring(0, 60) + "...";

            this.icon.Icon = icon;
            this.icon.Text = text;
        }

        ///
        /// Streams file.
        ///

        /// <summary>
        /// Edit the streams file with the default program
        /// associated to the extension.
        /// </summary>
        private void StreamsFileEdit()
        {
            try
            {
                streamsFile.Run();
            }
            catch (Exception exception)
            {
                String text = exception.Message;
                String caption = "Error running streams file";
                MessageBox.Show(text, caption);
            }
        }

        ///
        /// Player.
        /// Those methods require playerStream to be set.
        ///

        /// <summary>
        /// Like IconSet but adds the current stream name to the icon text.
        /// </summary>
        /// <param name="icon">Icon image.</param>
        /// <param name="text">Icon text</param>
        private void PlayerIconSet(Icon icon, String text)
        {
            IconSet(icon, text + " - " + playerStream.Name);
        }

        /// <summary>
        /// Play the current stream.
        /// </summary>
        private void PlayerPlay()
        {
            player.Open(playerStream.GetPlayerUri());
            player.Play();
            player.IsMuted = false;
            playerPlaying = true;

            PlayerIconSet(stopIcon, "Playing");
        }

        /// <summary>
        /// Stop playing.
        /// </summary>
        private void PlayerStop()
        {
            player.Stop();
            player.Close();
            playerPlaying = false;

            PlayerIconSet(playIcon, "Stopped");
        }

        /// <summary>
        /// Toggle between play/stop.
        /// </summary>
        private void PlayerTogglePlay()
        {
            playerPlaying.OnBool(PlayerStop, PlayerPlay);
        }

        /// <summary>
        /// Toggle between muted/unmuted.
        /// </summary>
        private void PlayerToggleMute()
        {
            if (playerPlaying)
            {
                player.IsMuted = !player.IsMuted;

                if (player.IsMuted)
                {
                    PlayerIconSet(muteIcon, "Playing (muted)");
                }
                else
                {
                    PlayerIconSet(stopIcon, "Playing");
                }
            }
        }

        ///
        /// Click events.
        ///

        /// <summary>
        /// Fired when the user clicks on a stream item in the menu.
        /// Change current stream to the new one and play it.
        /// </summary>
        private void OnStreamItemClick(Object sender, EventArgs e)
        {
            playerStream = (RadioStream) ((MenuItem) sender).Tag;
            PlayerPlay();
        }

        /// <summary>
        /// Fired when the user clicks on the open error details item.
        /// Show a MessageBox with the error.
        /// </summary>
        private void OnErrorOpenItemClick(Object sender, EventArgs e)
        {
            MenuItem item = (MenuItem) sender;
            Exception exception = (Exception) item.Tag;

            String text = exception.Message;
            String caption = "Error opening streams file";
            MessageBox.Show(text, caption);
        }

        /// <summary>
        /// Fired when the user clicks on the read error details item.
        /// Show a MessageBox with the error. Suggest editing.
        /// </summary>
        private void OnErrorReadItemClick(Object sender, EventArgs e)
        {
            MenuItem item = (MenuItem) sender;
            StreamsFileReadError exception = (StreamsFileReadError) item.Tag;

            String text = String.Format(
                "{0} \n" +
                "Error at line {1} \n" +
                "{2} \n\n" +
                "{3} \n\n" +
                "Do you want to edit the streams file now?",
                exception.File.FilePath,
                exception.LineNumber,
                exception.Message,
                exception.Line);

            String caption = "Error reading streams file";

            if (Utils.MessageBoxYesNo(text, caption))
                StreamsFileEdit();
        }

        /// <summary>
        /// Fired when the user clicks on the edit streams item.
        /// </summary>
        private void OnEditItemClick(Object sender, EventArgs e)
        {
            StreamsFileEdit();
        }

        /// <summary>
        /// Fired when the user clicks on the exit program item.
        /// </summary>
        private void OnExitItemClick(Object sender, EventArgs e)
        {
            icon.Visible = false;
            Application.Exit();
        }

        /// <summary>
        /// Fired when the user clicks on the icon.
        /// </summary>
        private void OnIconMouseClick(Object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                playerStream.OnNull(icon.InvokeContextMenu, PlayerTogglePlay);
            }

            else if (e.Button == MouseButtons.Middle)
            {
                playerStream.OnNull(icon.InvokeContextMenu, PlayerToggleMute);
            }
        }

        ///
        /// Automatic events.
        ///

        /// <summary>
        /// Fired when the context menu is about to be opened.
        /// </summary>
        private void OnMenuPopup(Object sender, EventArgs e)
        {
            MenuUpdate();
        }

        /// <summary>
        /// Fired when the current stream has no more data.
        /// </summary>
        private void OnPlayerMediaEnded(Object sender, EventArgs e)
        {
            PlayerStop();
        }

        /// <summary>
        /// Fired when the current stream can't be played.
        /// Show an error balloon with the reason as text.
        /// </summary>
        private void OnPlayerMediaFailed(Object sender, ExceptionEventArgs e)
        {
            PlayerStop();

            String name = playerStream.Name;
            String uri = playerStream.GetPlayerUri().ToString();

            String title = "Error playing stream: " + name;
            String text = e.ErrorException.Message + "\n" + uri;

            icon.ShowBalloonTip(10, title, text, ToolTipIcon.Error);
        }
    }
}

