
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

using GaGa.Controls;


namespace GaGa
{
    internal class GaGa : ApplicationContext
    {
        // gui components:
        private readonly Container container;
        private readonly NotifyIcon notifyIcon;
        private readonly ContextMenuStrip menu;

        // streams menu:
        private readonly String streamsFilepath;
        private readonly StreamsFileLoader streamsFileLoader;

        // streams menu constant items:
        private readonly ToolStripMenuItem errorOpenItem;
        private readonly ToolStripMenuItem errorReadItem;
        private readonly ToolStripMenuItem editItem;

        // audio settings:
        private readonly ToolStripMenuItem audioSettingsItem;
        private readonly Label balanceLabel;
        private readonly TrackBar balanceTrackBar;
        private readonly Label volumeLabel;
        private readonly TrackBar volumeTrackBar;

        // other menu items:
        private readonly ToolStripMenuItem exitItem;

        // player:
        private readonly Player player;

        /// <summary>
        /// GaGa implementation.
        /// </summary>
        /// <param name="filepath">Path to the streams file to use.</param>
        public GaGa(String filepath)
        {
            // gui components:
            container = new Container();

            notifyIcon = new NotifyIcon(container);
            notifyIcon.ContextMenuStrip = new ContextMenuStrip();
            notifyIcon.ContextMenuStrip.Opening += OnMenuOpening;
            notifyIcon.Icon = Util.ResourceAsIcon("GaGa.Resources.idle.ico");
            notifyIcon.MouseClick += OnIconMouseClick;
            notifyIcon.Visible = true;

            menu = notifyIcon.ContextMenuStrip;

            // streams menu:
            streamsFilepath = filepath;
            streamsFileLoader = new StreamsFileLoader(filepath, "GaGa.Resources.streams.ini");

            // streams menu constant items:
            errorOpenItem = new ToolStripMenuItem();
            errorOpenItem.Text = "Error opening streams file";
            errorOpenItem.Click += OnErrorOpenItemClick;

            errorReadItem = new ToolStripMenuItem();
            errorReadItem.Text = "Error reading streams file";
            errorReadItem.Click += OnErrorReadItemClick;

            editItem = new ToolStripMenuItem();
            editItem.Text = "Edit streams file";
            editItem.Click += OnEditItemClick;

            // audio settings:
            audioSettingsItem = new ToolStripMenuItem();
            audioSettingsItem.Text = "Audio settings";

            MyToolStripLabel balanceStripLabel = new MyToolStripLabel();
            MyToolStripTrackBar balanceStripTrackBar = new MyToolStripTrackBar();
            MyToolStripLabel volumeStripLabel = new MyToolStripLabel();
            MyToolStripTrackBar volumeStripTrackBar = new MyToolStripTrackBar();

            balanceLabel = balanceStripLabel.Label;
            balanceLabel.Text = "Balance";

            balanceTrackBar = balanceStripTrackBar.TrackBar;
            balanceTrackBar.Minimum = -10;
            balanceTrackBar.Maximum = 10;
            balanceTrackBar.Value = 0;
            balanceTrackBar.ValueChanged += OnBalanceTrackBarChanged;

            volumeLabel = volumeStripLabel.Label;
            volumeLabel.Text = "Volume";

            volumeTrackBar = volumeStripTrackBar.TrackBar;
            volumeTrackBar.Minimum = 0;
            volumeTrackBar.Maximum = 20;
            volumeTrackBar.Value = 10;
            volumeTrackBar.ValueChanged += OnVolumeTrackBarChanged;

            // change back color depending on the current renderer:
            if (menu.Renderer is ToolStripProfessionalRenderer)
            {
                ProfessionalColorTable colors = new ProfessionalColorTable();
                Color back = colors.ToolStripDropDownBackground;

                balanceLabel.BackColor = back;
                balanceTrackBar.BackColor = back;
                volumeLabel.BackColor = back;
                volumeTrackBar.BackColor = back;
            }

            audioSettingsItem.DropDownItems.Add(balanceStripLabel);
            audioSettingsItem.DropDownItems.Add(balanceStripTrackBar);
            audioSettingsItem.DropDownItems.Add(volumeStripLabel);
            audioSettingsItem.DropDownItems.Add(volumeStripTrackBar);

            // other items:
            exitItem = new ToolStripMenuItem();
            exitItem.Text = "Exit";
            exitItem.Click += OnExitItemClick;

            // player:
            player = new Player(notifyIcon);

            // update everything:
            BalanceUpdate();
            VolumeUpdate();
            MenuUpdate();
        }

        ///
        /// Helpers
        ///

        /// <summary>
        /// Open the streams file with the default program
        /// associated to the extension.
        /// </summary>
        private void StreamsFileOpen()
        {
            try
            {
                Process.Start(streamsFilepath);
            }
            catch (Exception exception)
            {
                String text = exception.Message;
                String caption = "Error openning streams file";
                MessageBox.Show(text, caption);
            }
        }

        ///
        /// Menu updating
        ///

        /// <summary>
        /// Reload the context menu.
        /// </summary>
        private void MenuUpdate()
        {
            try
            {
                menu.Items.Clear();
                streamsFileLoader.LoadTo(menu, OnStreamItemClick);
                editItem.Enabled = true;
            }
            catch (StreamsFileReadError exception)
            {
                menu.Items.Clear();
                menu.Items.Add(errorReadItem);
                errorReadItem.Tag = exception;
                editItem.Enabled = true;
            }
            catch (Exception exception)
            {
                menu.Items.Clear();
                menu.Items.Add(errorOpenItem);
                errorOpenItem.Tag = exception;
                editItem.Enabled = false;
            }

            menu.Items.Add(editItem);
            menu.Items.Add("-");
            menu.Items.Add(audioSettingsItem);
            menu.Items.Add(exitItem);
        }

        /// <summary>
        /// When the menu is about to be opened, reload first if needed.
        /// </summary>
        private void OnMenuOpening(Object sender, CancelEventArgs e)
        {
            if (streamsFileLoader.MustReload())
            {
                menu.SuspendLayout();
                MenuUpdate();
                menu.ResumeLayout();
            }

            e.Cancel = false;
        }

        ///
        /// Balance and volume updating
        ///

        /// <summary>
        /// Update the balance label and trackbar
        /// and send the current value to the player.
        /// </summary>
        private void BalanceUpdate()
        {
            Double current = (Double) balanceTrackBar.Value;
            Double maximum = balanceTrackBar.Maximum;

            Double balance = current / maximum;
            Double percent = balance * 100;

            balanceLabel.Text = "Balance  " + percent.ToString();
            player.Balance = balance;
        }

        /// <summary>
        /// Update the volume label and trackbar
        /// and send the current value to the player.
        /// </summary>
        private void VolumeUpdate()
        {
            Double current = (Double) volumeTrackBar.Value;
            Double maximum = volumeTrackBar.Maximum;

            Double volume = current / maximum;
            Double percent = volume * 100;

            volumeLabel.Text = "Volume  " + percent.ToString();
            player.Volume = volume;
        }

        ///
        /// Events: icon
        /// 

        /// <summary>
        /// Toggle play with the left mouse button.
        /// When no stream has been selected, show the context menu instead.
        /// </summary>
        private void OnIconLeftMouseClick()
        {
            if (player.Source == null)
            {
                notifyIcon.InvokeContextMenu();
            }
            else
            {
                player.TogglePlay();
            }
        }

        /// <summary>
        /// Toggle mute with the wheel button.
        /// When not playing, show the context menu instead.
        /// </summary>
        private void OnIconMiddleMouseClick()
        {
            if (player.IsIdle)
            {
                notifyIcon.InvokeContextMenu();
            }
            else
            {
                player.ToggleMute();
            }
        }

        /// <summary>
        /// Allow control via mouse.
        /// </summary>
        private void OnIconMouseClick(Object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    OnIconLeftMouseClick();
                    break;

                case MouseButtons.Middle:
                    OnIconMiddleMouseClick();
                    break;

                case MouseButtons.Right:
                case MouseButtons.XButton1:
                case MouseButtons.XButton2:
                    break;
            }
        }

        ///
        /// Events: menu
        ///

        /// <summary>
        /// Stream clicked, play it.
        /// </summary>
        private void OnStreamItemClick(Object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem) sender;

            PlayerStream stream = new PlayerStream(item.Text, (Uri) item.Tag);
            player.Play(stream);
        }

        /// <summary>
        /// Opening error clicked, show details.
        /// </summary>
        private void OnErrorOpenItemClick(Object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem) sender;
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
            ToolStripMenuItem item = (ToolStripMenuItem) sender;
            StreamsFileReadError exception = (StreamsFileReadError) item.Tag;

            String text = String.Format(
                "{0} \n" +
                "Error at line {1}: {2} \n\n" +
                "{3} \n\n" +
                "Do you want to open the streams file now?",
                exception.FilePath,
                exception.LineNumber,
                exception.Message,
                exception.Line
            );

            String caption = "Error reading streams file";
            if (Util.MessageBoxYesNo(text, caption))
            {
                StreamsFileOpen();
            }
        }

        /// <summary>
        /// Edit clicked, open the streams file.
        /// </summary>
        private void OnEditItemClick(Object sender, EventArgs e)
        {
            StreamsFileOpen();
        }


        /// <summary>
        /// Balance changed.
        /// </summary>
        private void OnBalanceTrackBarChanged(Object sender, EventArgs e)
        {
            BalanceUpdate();
        }

        /// <summary>
        /// Volume changed.
        /// </summary>
        private void OnVolumeTrackBarChanged(Object sender, EventArgs e)
        {
            VolumeUpdate();
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
    }
}

