
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using GaGa.Controls;


namespace GaGa
{
    internal class GaGa : ApplicationContext
    {
        // gui components:
        private readonly Container container;
        private readonly NotifyIcon notifyIcon;
        private readonly ToolStripAeroRenderer toolStripRenderer;

        // settings/streams files:
        private readonly String settingsFilepath;
        private readonly Settings settings;
        private readonly String streamsFilepath;
        private readonly StreamsFileLoader streamsFileLoader;

        // player:
        private readonly Player player;

        // streams menu constant items:
        private readonly ToolStripMenuItem errorOpenItem;
        private readonly ToolStripMenuItem errorReadItem;
        private readonly ToolStripMenuItem editItem;

        // audio settings items:
        private readonly ToolStripMenuItem audioSettingsItem;
        private readonly ToolStripLabeledTrackBar volumeTrackBar;
        private readonly ToolStripLabeledTrackBar balanceTrackBar;

        // other menu items:
        private readonly ToolStripMenuItem exitItem;

        /// <summary>
        /// GaGa implementation.
        /// </summary>
        /// <param name="settingsFilepath">
        /// Path to the settings file to use.
        /// </param>
        /// <param name="streamsFilepath">
        /// Path to the streams file to use.
        /// </param>
        public GaGa(String settingsFilepath, String streamsFilepath)
        {
            // gui components:
            container = new Container();

            notifyIcon = new NotifyIcon(container);
            notifyIcon.ContextMenuStrip = new ContextMenuStrip();
            notifyIcon.ContextMenuStrip.Opening += OnMenuOpening;
            notifyIcon.Icon = Util.ResourceAsIcon("GaGa.Resources.Idle.ico");
            notifyIcon.MouseClick += OnIconMouseClick;
            notifyIcon.Visible = true;

            toolStripRenderer = new ToolStripAeroRenderer();
            notifyIcon.ContextMenuStrip.Renderer = toolStripRenderer;

            // settings/streams files:
            this.settingsFilepath = settingsFilepath;
            settings = SettingsLoad();
            this.streamsFilepath = streamsFilepath;
            streamsFileLoader = new StreamsFileLoader(streamsFilepath, "GaGa.Resources.Streams.ini");

            // player:
            player = new Player(notifyIcon);
            player.Select(settings.LastPlayerStream);

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

            // audio settings items:
            audioSettingsItem = new ToolStripMenuItem();
            audioSettingsItem.Text = "Audio settings";

            balanceTrackBar = new ToolStripLabeledTrackBar();
            balanceTrackBar.Label.Text = "Balance";
            balanceTrackBar.TrackBar.Minimum = -10;
            balanceTrackBar.TrackBar.Maximum = 10;
            balanceTrackBar.TrackBar.Value = settings.LastBalanceTrackBarValue;
            balanceTrackBar.TrackBar.ValueChanged += OnBalanceTrackBarChanged;

            volumeTrackBar = new ToolStripLabeledTrackBar();
            volumeTrackBar.Label.Text = "Volume";
            volumeTrackBar.TrackBar.Minimum = 0;
            volumeTrackBar.TrackBar.Maximum = 20;
            volumeTrackBar.TrackBar.Value = settings.LastVolumeTrackBarValue;
            volumeTrackBar.TrackBar.ValueChanged += OnVolumeTrackBarChanged;

            // adjust the backcolor to the renderer:
            Color back = toolStripRenderer.ColorTable.ToolStripDropDownBackground;

            balanceTrackBar.BackColor = back;
            balanceTrackBar.Label.BackColor = back;
            balanceTrackBar.TrackBar.BackColor = back;
            volumeTrackBar.BackColor = back;
            volumeTrackBar.Label.BackColor = back;
            volumeTrackBar.TrackBar.BackColor = back;

            audioSettingsItem.DropDownItems.Add(balanceTrackBar);
            audioSettingsItem.DropDownItems.Add(volumeTrackBar);

            BalanceUpdate();
            VolumeUpdate();

            // other items:
            exitItem = new ToolStripMenuItem();
            exitItem.Text = "Exit";
            exitItem.Click += OnExitItemClick;
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
        /// Settings
        ///

        /// <summary>
        /// Load the settings from our streams filepath if it exists.
        /// Return default settings otherwise.
        /// </summary>
        private Settings SettingsLoad()
        {
            try
            {
                if (File.Exists(settingsFilepath))
                {
                    return (Settings) Util.Deserialize(settingsFilepath);
                }
            }
            catch (Exception exception)
            {
                String text = String.Format(
                    "Unable to load settings: \n" +
                    "{0} \n\n" +
                    "This usually means that the file is corrupt, empty \n" +
                    "or incompatible with the current GaGa version. \n\n" +
                    "Exception message: \n" +
                    "{1} \n",
                    settingsFilepath,
                    exception.Message
                );

                String caption = "Error loading settings file";
                MessageBox.Show(text, caption);
            }

            // unable to load or doesn't exist:
            return new Settings();
        }

        /// <summary>
        /// Save the current settings.
        /// </summary>
        private void SettingsSave()
        {
            try
            {
                Util.Serialize(settings, settingsFilepath);
            }
            catch (Exception exception)
            {
                String text = String.Format(
                    "Unable to save settings: \n" +
                    "{0} \n\n" +
                    "Exception message: \n" +
                    "{1} \n",
                    settingsFilepath,
                    exception.Message
                );

                String caption = "Error saving settings file";
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
            ContextMenuStrip menu = notifyIcon.ContextMenuStrip;

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
        /// When the menu is about to be opened, reload from the streams file
        /// and update the renderer colors when needed.
        /// </summary>
        private void OnMenuOpening(Object sender, CancelEventArgs e)
        {
            ContextMenuStrip menu = notifyIcon.ContextMenuStrip;
            menu.SuspendLayout();

            if (streamsFileLoader.MustReload())
            {
                MenuUpdate();
            }

            toolStripRenderer.UpdateColors();

            menu.ResumeLayout();
            e.Cancel = false;
            notifyIcon.InvokeContextMenu();
        }

        ///
        /// Balance and volume updating
        ///

        /// <summary>
        /// Update the balance label
        /// and send the current value to the player.
        /// </summary>
        private void BalanceUpdate()
        {
            Double current = (Double) balanceTrackBar.TrackBar.Value;
            Double maximum = balanceTrackBar.TrackBar.Maximum;

            Double balance = current / maximum;
            Double percent = balance * 100;

            balanceTrackBar.Label.Text = "Balance  " + percent.ToString();
            player.Balance = balance;
        }

        /// <summary>
        /// Update the volume label
        /// and send the current value to the player.
        /// </summary>
        private void VolumeUpdate()
        {
            Double current = (Double) volumeTrackBar.TrackBar.Value;
            Double maximum = volumeTrackBar.TrackBar.Maximum;

            Double volume = current / maximum;
            Double percent = volume * 100;

            volumeTrackBar.Label.Text = "Volume  " + percent.ToString();
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
        /// Balance changed, update label and send new value to the player.
        /// </summary>
        private void OnBalanceTrackBarChanged(Object sender, EventArgs e)
        {
            BalanceUpdate();
        }

        /// <summary>
        /// Volume changed, update label and send new value to the player.
        /// </summary>
        private void OnVolumeTrackBarChanged(Object sender, EventArgs e)
        {
            VolumeUpdate();
        }

        /// <summary>
        /// Exit clicked, stop playing, save settings, hide icon and exit.
        /// </summary>
        private void OnExitItemClick(Object sender, EventArgs e)
        {
            player.Stop();

            settings.LastBalanceTrackBarValue = balanceTrackBar.TrackBar.Value;
            settings.LastVolumeTrackBarValue = volumeTrackBar.TrackBar.Value;
            settings.LastPlayerStream = player.Source;
            SettingsSave();

            notifyIcon.Visible = false;
            Application.Exit();
        }
    }
}

