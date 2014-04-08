
// GaGa.
// A simple radio player running on the Windows notification area.


using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;


namespace GaGa
{
    internal class GaGa : ApplicationContext
    {
        // resources:
        private Icon playIcon;
        private Icon stopIcon;

        // gui components:
        private Container container;
        private ContextMenuStrip menu;
        private NotifyIcon icon;

        // non-loader menu items:
        private ToolStripMenuItem editItem;
        private ToolStripMenuItem exitItem;
        private ToolStripMenuItem errorOpeningItem;
        private ToolStripMenuItem errorReadingItem;

        // menu file and loader:
        private StreamsFile streamsFile;
        private StreamsMenuLoader menuLoader;

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

            menu = new ContextMenuStrip();
            menu.Opening += new CancelEventHandler(OnMenuOpening);

            icon = new NotifyIcon(container);
            icon.Icon = playIcon;
            icon.Text = "GaGa";
            icon.Visible = true;
            icon.ContextMenuStrip = menu;

            // non-loader menu items:
            editItem = new ToolStripMenuItem("Edit streams file");
            exitItem = new ToolStripMenuItem("Exit");
            errorOpeningItem = new ToolStripMenuItem("Unable to open streams file (click for details)");
            errorReadingItem = new ToolStripMenuItem("Unable to read streams file (click for details)");

            errorOpeningItem.Click += new EventHandler(OnErrorOpeningItemClick);
            errorReadingItem.Click += new EventHandler(OnErrorReadingItemClick);

            // menu file and loader:
            streamsFile = new StreamsFile("streams.ini", "GaGa.Resources.default-streams.ini");
            menuLoader = new StreamsMenuLoader(streamsFile);
        }

        /// <summary>
        /// Re-create the context menu on changes from the menuloader.
        /// </summary>
        private void ReloadContextMenuOnChanges()
        {
            if (menuLoader.HasChanged())
            {
                menu.Items.Clear();
                menuLoader.LoadTo(menu);
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(editItem);
                menu.Items.Add(exitItem);
                editItem.Enabled = true;
            }
        }

        /// <summary>
        /// Create a context menu containing a clickable error item.
        /// The item contains the raised exception in the Tag property.
        /// </summary>
        /// <param name="exception">
        /// Error that happened when trying to load the menu.
        /// </param>
        private void LoadErrorContextMenu(Exception exception)
        {
            menu.Items.Clear();

            // on parsing errors, allow editing:
            if (exception is StreamsFileReaderError)
            {
                errorReadingItem.Tag = exception;
                menu.Items.Add(errorReadingItem);
                editItem.Enabled = true;
            }
            // otherwise it's an IO error, the file may not even exist:
            else
            {
                errorOpeningItem.Tag = exception;
                menu.Items.Add(errorOpeningItem);
                editItem.Enabled = false;
            }

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(editItem);
            menu.Items.Add(exitItem);
        }

        /// <summary>
        /// Recreate the context menu when needed.
        /// Create an alternative menu on errors.
        /// </summary>
        private void UpdateMenu()
        {
            try
            {
                ReloadContextMenuOnChanges();
            }
            catch (Exception exception)
            {
                LoadErrorContextMenu(exception);
            }
        }

        /// <summary>
        /// Fired when the user clicks on the error details
        /// when the streams file can't be opened.
        /// Shows a MessageBox with the error.
        /// </summary>
        private void OnErrorOpeningItemClick(Object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem) sender;
            Exception exception = (Exception) item.Tag;

            String caption = "Error opening streams file";
            String text = exception.Message;

            MessageBox.Show(text, caption);
        }

        /// <summary>
        /// Fired when the user clicks on the error details
        /// when the streams file can't be read.
        /// Shows a MessageBox with the error, suggests editing.
        /// </summary>
        private void OnErrorReadingItemClick(Object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem) sender;
            StreamsFileReaderError exception = (StreamsFileReaderError) item.Tag;

            // Example (without padding newlines):
            // streams.ini error at line 15
            // Invalid syntax.
            // Line text
            // Do you want to edit the streams file now?

            String text = String.Format(
                "{0} error at line {1} \n{2} \n\n{3}\n\n" +
                "Do you want to edit the streams file now?",
                exception.FilePath,
                exception.LineNumber,
                exception.Message,
                exception.Line);

            String caption = "Error reading streams file";
            DialogResult result = MessageBox.Show(text, caption, MessageBoxButtons.YesNo);
        }

        /// <summary>
        /// Fired when the context menu is opening.
        /// </summary>
        private void OnMenuOpening(Object sender, CancelEventArgs e)
        {
            Point cursorPosition = Control.MousePosition;

            e.Cancel = false;
            menu.SuspendLayout();
            UpdateMenu();
            menu.ResumeLayout();

            // positioning workaround
            // .NET tends to get confused on size changes:
            menu.Show(cursorPosition.X - menu.Width, cursorPosition.Y - menu.Height);
        }
    }
}

