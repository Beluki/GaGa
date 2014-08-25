
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.ComponentModel;
using System.Windows.Forms;


namespace GaGa
{
    internal class GaGa : ApplicationContext
    {
        // streams file and loader:
        private readonly StreamsFile streamsFile;
        private readonly StreamsMenuLoader menuLoader;

        // gui components:
        private readonly Container container;
        private readonly NotifyIcon notifyIcon;
        private ContextMenu menu;

        // error items:
        private readonly MenuItem errorOpenItem;
        private readonly MenuItem errorReadItem;

        // constant items:
        private readonly MenuItem editItem;
        private readonly MenuItem exitItem;
        private readonly MenuItem volumeItem;

        // playing:
        private readonly Player player;

        /// <summary>
        /// GaGa implementation.
        /// </summary>
        /// <param name="streamsFilePath">
        /// Path for the streams file to use.
        /// </param>
        public GaGa(String streamsFilePath)
        {
            // streams file and loader:
            streamsFile = new StreamsFile(streamsFilePath, "GaGa.Resources.streams.ini");
            menuLoader = new StreamsMenuLoader(streamsFile);

            // gui components:
            container = new Container();

            notifyIcon = new NotifyIcon(container);
            notifyIcon.Visible = true;

            MenuRecreate();

            // error items:
            errorOpenItem = new MenuItem("Error opening streams file (click for details)", OnErrorOpenItemClick);
            errorReadItem = new MenuItem("Error reading streams file (click for details)", OnErrorReadItemClick);

            // constant items:
            editItem = new MenuItem("Edit streams file", OnEditItemClick);
            exitItem = new MenuItem("Exit", OnExitItemClick);

            // volume items:
            var volumeItems = new MenuItem[]
            {
                new MenuItem("10%", OnChangeVolumeClick),
                new MenuItem("25%", OnChangeVolumeClick),
                new MenuItem("50%", OnChangeVolumeClick),
                new MenuItem("75%", OnChangeVolumeClick),
                new MenuItem("100%", OnChangeVolumeClick)
            };
            volumeItem = new MenuItem("Change Volume", volumeItems);

            // playing:
            player = new Player(notifyIcon);
        }


        ///
        /// Reloading the menu.
        ///

        /// <summary>
        /// Clear the menu by deleting it and creating a new one from scratch.
        /// Avoids the problem of context menus caching their width.
        /// </summary>
        private void MenuRecreate()
        {
            menu = new ContextMenu();
            menu.Popup += OnMenuPopup;

            notifyIcon.ContextMenu = menu;
        }

        /// <summary>
        /// Re-create the context menu from the menuloader items.
        /// </summary>
        private void MenuReload()
        {
            MenuRecreate();
            menuLoader.LoadTo(menu, OnStreamItemClick);
            editItem.Enabled = true;
        }

        /// <summary>
        /// On read errors add a clickable error item.
        /// The item contains the raised exception in the .Tag property.
        /// </summary>
        /// <param name="exception">
        /// Error that happened when trying to read the streams file.
        /// </param>
        private void MenuLoadErrorRead(StreamsFileReadError exception)
        {
            MenuRecreate();
            menu.MenuItems.Add(errorReadItem);

            errorReadItem.Tag = exception;
            editItem.Enabled = true;
        }

        /// <summary>
        /// On open errors add a clickable error item and disable editing.
        /// The item contains the raised exception in the .Tag property.
        /// </summary>
        /// <param name="exception">
        /// Error that happened when trying to open the streams file.
        /// </param>
        private void MenuLoadErrorOpen(Exception exception)
        {
            MenuRecreate();
            menu.MenuItems.Add(errorOpenItem);

            errorOpenItem.Tag = exception;
            editItem.Enabled = false;
        }

        /// <summary>
        /// Recreate the context menu.
        /// Create alternate menus on errors.
        /// </summary>
        private void MenuUpdate()
        {
            try
            {
                MenuReload();
            }
            catch (StreamsFileReadError exception)
            {
                MenuLoadErrorRead(exception);
            }
            catch (Exception exception)
            {
                MenuLoadErrorOpen(exception);
            }

            menu.MenuItems.Add("-");
            menu.MenuItems.Add(editItem);
            menu.MenuItems.Add(volumeItem);
            menu.MenuItems.Add(exitItem);
        }

        /// <summary>
        /// When the menu is about to be opened, reload first if needed.
        /// </summary>
        private void OnMenuPopup(Object sender, EventArgs e)
        {
            if (menuLoader.MustReload)
            {
                MenuUpdate();
            }
        }

        ///
        /// Streams file actions.
        ///

        /// <summary>
        /// Open the streams file with the default program
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
        /// Clicking on menu items.
        ///

        /// <summary>
        /// Stream clicked, play it.
        /// </summary>
        private void OnStreamItemClick(Object sender, EventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            PlayerStream stream = new PlayerStream(item.Text, (Uri)item.Tag);
            player.Play(stream);
        }

        /// <summary>
        /// Opening error clicked, show details.
        /// </summary>
        private void OnErrorOpenItemClick(Object sender, EventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            Exception exception = (Exception)item.Tag;

            String text = exception.Message;
            String caption = "Error opening streams file";
            MessageBox.Show(text, caption, MessageBoxButtons.OK);
        }

        /// <summary>
        /// Reading error clicked, show details. Suggest editing.
        /// </summary>
        private void OnErrorReadItemClick(Object sender, EventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            StreamsFileReadError exception = (StreamsFileReadError)item.Tag;

            String text = String.Format(
                "{0} \n" +
                "Error at line {1}: {2} \n\n" +
                "{3} \n\n" +
                "Do you want to edit the streams file now?",
                exception.File.FilePath,
                exception.LineNumber,
                exception.Message,
                exception.Line
            );

            String caption = "Error reading streams file";
            if (Utils.MessageBoxYesNo(text, caption))
            {
                StreamsFileEdit();
            }
        }

        /// <summary>
        /// Edit clicked, open the streams file.
        /// </summary>
        private void OnEditItemClick(Object sender, EventArgs e)
        {
            StreamsFileEdit();
        }

        /// <summary>
        /// Exit clicked, stop playing, hide icon and exit.
        /// </summary>
        private void OnExitItemClick(Object sender, EventArgs e)
        {
            player.Stop();
            notifyIcon.Visible = false;
            Application.Exit();
        }

        /// <summary>
        /// Change Volume Clicked, 
        /// </summary>
        private void OnChangeVolumeClick(object sender, EventArgs eventArgs)
        {
            // HACK: Get rid of the percentage sign, use the Lambda Expression instead
            MenuItem item = (MenuItem)sender;
            string text = item.Text.Substring(0, item.Text.Length - 1);
            double amount = double.Parse(text) / 100;
            player.ChangeVolume(amount);
        }
    }
}

