
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
        /// On read errors add a clickable error item and disable editing.
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
        /// On open errors add a clickable error item and enable editing.
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
        /// Clicking on menu items.
        /// 

        /// <summary>
        /// Stream clicked, play it.
        /// </summary>
        private void OnStreamItemClick(Object sender, EventArgs e)
        {
            PlayerStream stream = (PlayerStream) ((MenuItem) sender).Tag;
            player.Play(stream);
        }

        /// <summary>
        /// Opening error clicked, show details.
        /// </summary>
        private void OnErrorOpenItemClick(Object sender, EventArgs e)
        {
            MenuItem item = (MenuItem) sender;
            Exception exception = (Exception) item.Tag;

            String text = exception.Message;
            String caption = "Error opening streams file";
            MessageBox.Show(text, caption, MessageBoxButtons.OK);
        }

        /// <summary>
        /// Reading error clicked, show details. Suggest editing.
        /// </summary>
        private void OnErrorReadItemClick(Object sender, EventArgs e)
        {
            MenuItem item = (MenuItem) sender;
            StreamsFileReadError exception = (StreamsFileReadError) item.Tag;

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
        /// Edit item, open the streams file.
        /// </summary>
        private void OnEditItemClick(Object sender, EventArgs e)
        {
            StreamsFileEdit();
        }

        /// <summary>
        /// Exit item, hide icon and exit.
        /// </summary>
        private void OnExitItemClick(Object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            Application.Exit();
        }
    }
}

