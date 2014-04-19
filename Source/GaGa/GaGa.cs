
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
        // paths:
        private String streamsFilePath;
        private String streamsFileResourcePath;

        // resources:
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

        // menu file and loader:
        private StreamsFile streamsFile;
        private StreamsMenuLoader menuLoader;

        // player state:
        private MediaPlayer player;
        private RadioStream playerStream;
        private Boolean playerIsPlaying;

        /// <summary>
        /// Actual implementation.
        /// </summary>
        /// <param name="path">Streams file path.</param>
        public GaGa()
        {
            // paths:
            streamsFilePath = Path.Combine(Utils.ApplicationDirectory(), "streams.ini");
            streamsFileResourcePath = "GaGa.Resources.default-streams.ini";

            // resources:
            playIcon = Utils.LoadIconFromResource("GaGa.Resources.play.ico");
            stopIcon = Utils.LoadIconFromResource("GaGa.Resources.stop.ico");
            muteIcon = Utils.LoadIconFromResource("GaGa.Resources.mute.ico");

            // gui components:
            container = new Container();

            icon = new NotifyIcon(container);
            icon.MouseClick += OnIconMouseClick;
            IconSet(playIcon, "GaGa - Click to open menu");
            icon.Visible = true;

            MenuRecreate();

            // non-loader menu items:
            editItem = new MenuItem("Edit streams file");
            exitItem = new MenuItem("Exit");
            errorOpenItem = new MenuItem("Error opening streams file (click for details)");
            errorReadItem = new MenuItem("Error reading streams file (click for details)");

            editItem.Click += OnEditItemClick;
            exitItem.Click += OnExitItemClick;
            errorOpenItem.Click += OnErrorOpenItemClick;
            errorReadItem.Click += OnErrorReadItemClick;

            // menu file and loader:
            streamsFile = new StreamsFile(streamsFilePath, streamsFileResourcePath);
            menuLoader = new StreamsMenuLoader(streamsFile);

            // player state:
            player = new MediaPlayer();
            playerStream = null;
            playerIsPlaying = false;

            player.MediaEnded += OnPlayerMediaEnded;
            player.MediaFailed += OnPlayerMediaFailed;
        }

        ///
        /// Menu loading.
        ///

        /// <summary>
        /// Clear the menu by deleting it and creating a new one from scratch.
        /// Unlike menu.MenuItems.Clear() avoids the problem of menus
        /// caching their width.
        /// </summary>
        private void MenuRecreate()
        {
            menu = new ContextMenu();
            menu.Popup += new EventHandler(OnMenuPopup);
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
        /// Create an alternative menu on read and open errors.
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
        /// Change the notify icon text.
        /// Trims the text to 63 characters when longer.
        /// </summary>
        /// <param name="text">Icon text.</param>
        private void IconSet(Icon icon, String text)
        {
            // maximum icon text length is 63 characters
            // cut and add ... on longer names:
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
        /// Those methods expect playerStream to be set.
        ///

        /// <summary>
        /// Play the current stream.
        /// </summary>
        private void PlayerPlay()
        {
            player.Open(playerStream.GetPlayerUri());
            player.Play();
            player.IsMuted = false;
            playerIsPlaying = true;

            IconSet(stopIcon, "Playing - " + playerStream.Name);
        }

        /// <summary>
        /// Stop playing.
        /// </summary>
        private void PlayerStop()
        {
            player.Stop();
            player.Close();
            playerIsPlaying = false;

            IconSet(playIcon, "Stopped - " + playerStream.Name);
        }

        ///
        /// Toggle between play/stop.
        ///
        private void PlayerTogglePlayStop()
        {
            if (playerIsPlaying)
            {
                PlayerStop();
            }
            else
            {
                PlayerPlay();
            }
        }

        /// <summary>
        /// Toggle between muted/unmuted.
        /// </summary>
        private void PlayerToggleMuted()
        {
            if (playerIsPlaying)
            {
                player.IsMuted = !player.IsMuted;

                if (player.IsMuted)
                {
                    IconSet(muteIcon, "Playing (muted) - " + playerStream.Name);
                }
                else
                {
                    IconSet(stopIcon, "Playing - " + playerStream.Name);
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
        /// Fired when the user clicks on the error details item
        /// when the streams file can't be opened.
        /// Shows a MessageBox with the error.
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
        /// Fired when the user clicks on the error details item
        /// when the streams file can't be read.
        /// Shows a MessageBox with the error, suggests editing.
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
                // no stream yet, invoke the context menu instead:
                if (playerStream == null)
                {
                    icon.InvokeContextMenu();
                }
                else
                {
                    PlayerTogglePlayStop();
                }
            }

            else if (e.Button == MouseButtons.Middle)
            {
                // no stream yet, invoke the context menu instead:
                if (playerStream == null)
                {
                    icon.InvokeContextMenu();
                }
                else
                {
                    PlayerToggleMuted();
                }
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

