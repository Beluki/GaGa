
// GaGa.
// A simple radio player running on the Windows notification area.


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
        // resources:
        private Icon playIcon;
        private Icon stopIcon;

        // gui components:
        private Container container;
        private NotifyIcon icon;
        private ContextMenu menu;

        // non-loader menu items:
        private MenuItem editItem;
        private MenuItem exitItem;
        private MenuItem errorOpenItem;
        private MenuItem errorReadItem;

        // paths:
        private String streamsFilePath;
        private String streamsFileResourcePath;

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
        public GaGa()
        {
            // resources:
            playIcon = Utils.LoadIconFromResource("GaGa.Resources.play.ico");
            stopIcon = Utils.LoadIconFromResource("GaGa.Resources.stop.ico");

            // gui components:
            container = new Container();

            icon = new NotifyIcon(container);
            icon.Icon = playIcon;
            icon.Visible = true;
            icon.MouseClick += new MouseEventHandler(OnIconMouseClick);

            IconSetNoStream();
            MenuRecreate();

            // non-loader menu items:
            editItem = new MenuItem("Edit streams file");
            exitItem = new MenuItem("Exit");
            errorOpenItem = new MenuItem("Error opening streams file (click for details)");
            errorReadItem = new MenuItem("Error reading streams file (click for details)");

            editItem.Click += new EventHandler(OnEditItemClick);
            exitItem.Click += new EventHandler(OnExitItemClick);
            errorOpenItem.Click += new EventHandler(OnErrorOpenItemClick);
            errorReadItem.Click += new EventHandler(OnErrorReadItemClick);

            // paths:
            streamsFilePath = Path.Combine(Utils.ApplicationDirectory(), "streams.ini");
            streamsFileResourcePath = "GaGa.Resources.default-streams.ini";

            // menu file and loader:
            streamsFile = new StreamsFile(streamsFilePath, streamsFileResourcePath);
            menuLoader = new StreamsMenuLoader(streamsFile);

            // player state:
            player = new MediaPlayer();
            playerStream = null;
            playerIsPlaying = false;
        }

        ///
        /// Menu loading.
        /// 

        /// <summary>
        /// Clear the menu by deleting it and creating a new one from scratch.
        /// Unlike menu.MenuItems.Clear() this avoids the problem of menus
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
        /// Streams file.
        /// 

        /// <summary>
        /// Edit the streams file with the default program
        /// associated to the INI extension.
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
        /// Icon switching.
        /// 

        /// <summary>
        /// Change the icon to no stream selected yet.
        /// </summary>
        private void IconSetNoStream()
        {
            icon.Icon = playIcon;
            icon.Text = "No stream selected\n"
                      + "Click to open the menu";
        }

        /// <summary>
        /// Change the icon state to playing.
        /// </summary>
        private void IconSetPlay()
        {
            icon.Icon = stopIcon;
            icon.Text = "Playing\n"
                      + "Left click: stop\n"
                      + "Right click: menu";
        }

        /// <summary>
        /// Change the icon state to stopped.
        /// </summary>
        private void IconSetStop()
        {
            icon.Icon = playIcon;
            icon.Text = "Stopped\n"
                      + "Left click: play\n"
                      + "Right click: menu";
        }

        ///
        /// Player.
        /// Those methods expect playerStream to be set.
        ///

        /// <summary>
        /// Open and play the currently selected stream.
        /// </summary>
        private void PlayerPlay()
        {
            try
            {
                Uri uri = playerStream.GetUri();
                player.Open(uri);
                player.Play();

                playerIsPlaying = true;
                IconSetPlay();
            }

            catch (UriFormatException exception)
            {
                PlayerStop();

                String name = playerStream.Name;
                String link = playerStream.Link;

                String title = "Unable to open stream: " + name;
                String text = link + "\n\n" + exception.Message;

                icon.ShowBalloonTip(10, title, text, ToolTipIcon.Error);
            }
        }

        /// <summary>
        /// Stop playing.
        /// </summary>
        private void PlayerStop()
        {
            player.Stop();
            player.Close();
            playerIsPlaying = false;
            IconSetStop();
        }

        ///
        /// Click events.
        /// 

        /// <summary>
        /// Fired when the user clicks on a stream item in the menu.
        /// Change selected stream to the new one and play it.
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
                if (playerIsPlaying)
                {
                    PlayerStop();
                }
                else
                {
                    // no stream yet, invoke context menu instead
                    // so that the user can choose one:
                    if (playerStream == null)
                    {
                        icon.InvokeContextMenu();
                    }
                    else
                    {
                        PlayerPlay();
                    }
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
    }
}

