
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
        private Icon playIcon;
        private Icon stopIcon;

        // gui components:
        private Container container;
        private NotifyIcon icon;
        private ContextMenuStrip menu;

        // non-dynamic context menu items:
        private ToolStripMenuItem editItem;
        private ToolStripMenuItem errorItem;
        private ToolStripMenuItem exitItem;

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
            playIcon = Utils.LoadIconFromResource("GaGa.Resources.play.ico");
            stopIcon = Utils.LoadIconFromResource("GaGa.Resources.stop.ico");

            // load gui components:
            container = new Container();
            icon = new NotifyIcon(container);
            menu = new ContextMenuStrip();

            icon.ContextMenuStrip = menu;
            icon.Icon = playIcon;
            icon.Text = "GaGa";
            icon.Visible = true;

            menu.Opening += new CancelEventHandler(OnOpenMenu);

            // non-dynamic context menu items:
            editItem = new ToolStripMenuItem("Edit menu");
            errorItem = new ToolStripMenuItem("streams.ini error (click for details");
            exitItem = new ToolStripMenuItem("Exit");
                
            // load dynamic menu:
            menuloader = new StreamsMenuLoader("streams.ini", "GaGa.Resources.default-streams.ini");

            // load media player instance:
            player = new MediaPlayer();            
        }

        /// <summary>
        /// Recreate the context menu when needed.
        /// Create alternative menues on errors.
        /// </summary>
        private void MaybeRecreateMenu()
        {
            try
            {
                Boolean updated = menuloader.MaybeReload();
                if (updated)
                {
                    menu.Items.Clear();
                    menu.Items.AddRange(menuloader.Items);
                    menu.Items.Add(new ToolStripSeparator());
                    menu.Items.Add(editItem);
                    editItem.Enabled = true;
                }
            }

            catch (StreamsMenuLoaderParsingError ex)
            {
                menu.Items.Clear();
                menu.Items.Add(errorItem);
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(editItem);
                editItem.Enabled = true;
            }

            catch (Exception ex)
            {
                menu.Items.Clear();
                menu.Items.Add(errorItem);
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(editItem);
                editItem.Enabled = false;
            }
            finally
            {
                menu.Items.Add(exitItem);
            }
        }

        private void OnOpenMenu(Object sender, CancelEventArgs e)
        {
            e.Cancel = false;
            menu.SuspendLayout();
            MaybeRecreateMenu();
            menu.ResumeLayout();
        }
    }
}
