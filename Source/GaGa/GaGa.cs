
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using GaGa.Controls;
using GaGa.NotifyIconPlayer;
using GaGa.StreamsFile;

using LowKey;


namespace GaGa
{
    internal class GaGa : ApplicationContext
    {
        // gui components:
        private readonly Container container;
        private readonly ToolStripAeroRenderer toolStripRenderer;
        private readonly NotifyIcon notifyIcon;

        // settings:
        private readonly String settingsFilepath;
        private readonly GaGaSettings settings;

        // streams:
        private readonly String streamsFilepath;
        private readonly StreamsFileLoader streamsFileLoader;

        // player:
        private readonly Player player;

        // constant menu items:
        private readonly ToolStripMenuItem dynamicMenuMarker;
        private readonly ToolStripMenuItem errorOpenItem;
        private readonly ToolStripMenuItem errorReadItem;
        private readonly ToolStripMenuItem editItem;
        private readonly ToolStripMenuItem exitItem;

        // audio submenu:
        private readonly ToolStripMenuItem audioMenuItem;
        private readonly ToolStripLabeledTrackBar volumeTrackBar;
        private readonly ToolStripLabeledTrackBar balanceTrackBar;

        // options submenu:
        private readonly ToolStripMenuItem optionsMenuItem;
        private readonly ToolStripMenuItem optionsEnableAutoPlayItem;
        private readonly ToolStripMenuItem optionsEnableMultimediaKeysItem;

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
            toolStripRenderer = new ToolStripAeroRenderer();

            notifyIcon = new NotifyIcon(container);
            notifyIcon.ContextMenuStrip = new ContextMenuStrip();
            notifyIcon.ContextMenuStrip.Renderer = toolStripRenderer;
            notifyIcon.Visible = true;

            // settings:
            this.settingsFilepath = settingsFilepath;
            settings = SettingsLoad();

            // streams:
            this.streamsFilepath = streamsFilepath;
            streamsFileLoader = new StreamsFileLoader(streamsFilepath);

            // player:
            player = new Player(notifyIcon);

            // constant menu items:
            dynamicMenuMarker = new ToolStripMenuItem();
            dynamicMenuMarker.Visible = false;

            errorOpenItem = new ToolStripMenuItem();
            errorOpenItem.Text = "Error opening streams file (click for details)";

            errorReadItem = new ToolStripMenuItem();
            errorReadItem.Text = "Error reading streams file (click for details)";

            editItem = new ToolStripMenuItem();
            editItem.Text = "&Edit streams file";

            exitItem = new ToolStripMenuItem();
            exitItem.Text = "E&xit";

            // audio submenu:
            audioMenuItem = new ToolStripMenuItem();
            audioMenuItem.Text = "Audio";

            balanceTrackBar = new ToolStripLabeledTrackBar();
            balanceTrackBar.Label.Text = "Balance";
            balanceTrackBar.TrackBar.Minimum = -10;
            balanceTrackBar.TrackBar.Maximum = 10;

            volumeTrackBar = new ToolStripLabeledTrackBar();
            volumeTrackBar.Label.Text = "Volume";
            volumeTrackBar.TrackBar.Minimum = 0;
            volumeTrackBar.TrackBar.Maximum = 20;

            // adjust the backcolor to the renderer:
            Color back = toolStripRenderer.ColorTable.ToolStripDropDownBackground;

            balanceTrackBar.BackColor = back;
            balanceTrackBar.Label.BackColor = back;
            balanceTrackBar.TrackBar.BackColor = back;
            volumeTrackBar.BackColor = back;
            volumeTrackBar.Label.BackColor = back;
            volumeTrackBar.TrackBar.BackColor = back;

            audioMenuItem.DropDownItems.Add(balanceTrackBar);
            audioMenuItem.DropDownItems.Add(volumeTrackBar);

            // options submenu:
            optionsMenuItem = new ToolStripMenuItem();
            optionsMenuItem.Text = "Options";

            optionsEnableAutoPlayItem = new ToolStripMenuItem();
            optionsEnableAutoPlayItem.Text = "Enable auto play on startup";

            optionsEnableMultimediaKeysItem = new ToolStripMenuItem();
            optionsEnableMultimediaKeysItem.Text = "Enable multimedia keys";

            optionsMenuItem.DropDownItems.Add(optionsEnableAutoPlayItem);
            optionsMenuItem.DropDownItems.Add(optionsEnableMultimediaKeysItem);

            // add multimedia keys:
            KeyboardHook.Hooker.Add("Toggle Play", Keys.MediaPlayPause);
            KeyboardHook.Hooker.Add("Stop", Keys.MediaStop);
            KeyboardHook.Hooker.Add("Toggle Mute", Keys.VolumeMute);
            KeyboardHook.Hooker.Add("Volume Up", Keys.VolumeUp);
            KeyboardHook.Hooker.Add("Volume Down", Keys.VolumeDown);

            // apply settings before wiring events:
            balanceTrackBar.TrackBar.Value = settings.LastBalanceTrackBarValue;
            volumeTrackBar.TrackBar.Value = settings.LastVolumeTrackBarValue;

            BalanceUpdate();
            VolumeUpdate();

            player.Select(settings.LastPlayerStream);

            optionsEnableAutoPlayItem.Checked = settings.OptionsEnableAutoPlayChecked;
            optionsEnableMultimediaKeysItem.Checked = settings.OptionsEnableMultimediaKeysChecked;

            // wire events:
            notifyIcon.ContextMenuStrip.Opening += OnMenuOpening;
            notifyIcon.MouseClick += OnIconMouseClick;

            errorOpenItem.Click += OnErrorOpenItemClick;
            errorReadItem.Click += OnErrorReadItemClick;
            editItem.Click += OnEditItemClick;
            exitItem.Click += OnExitItemClick;

            balanceTrackBar.TrackBar.ValueChanged += OnBalanceTrackBarChanged;
            volumeTrackBar.TrackBar.ValueChanged += OnVolumeTrackBarChanged;

            optionsEnableAutoPlayItem.CheckOnClick = true;
            optionsEnableMultimediaKeysItem.Click += OnEnableMultimediaKeysItemClicked;

            KeyboardHook.Hooker.HotkeyDown += OnHotkeyDown;

            // handle options:
            if (optionsEnableAutoPlayItem.Checked)
            {
                player.Play();
            }

            if (optionsEnableMultimediaKeysItem.Checked)
            {
                MultimediaKeysHook();
            }
        }

        ///
        /// Streams file
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
        /// Settings file
        ///

        /// <summary>
        /// Load the settings from our settings filepath if it exists.
        /// Return default settings otherwise.
        /// </summary>
        private GaGaSettings SettingsLoad()
        {
            try
            {
                if (File.Exists(settingsFilepath))
                {
                    return (GaGaSettings) Util.Deserialize(settingsFilepath);
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

            // unable to load or doesn't exist, use defaults:
            return new GaGaSettings();
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
        /// Clear the current menu items, disposing
        /// the dynamic items.
        /// </summary>
        private void MenuClear()
        {
            // collect dynamic items up to the marker:
            List<ToolStripItem> disposable = new List<ToolStripItem>();

            foreach (ToolStripItem item in notifyIcon.ContextMenuStrip.Items)
            {
                if (item == dynamicMenuMarker)
                {
                    break;
                }
                else
                {
                    disposable.Add(item);
                }
            }

            // dispose them:
            foreach (ToolStripItem item in disposable)
            {
                item.Dispose();
            }

            disposable.Clear();
            notifyIcon.ContextMenuStrip.Items.Clear();

            // at this point, all the menu items are dead
            // perform GC to make memory usage as deterministic/predictable
            // as possible:
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        /// <summary>
        /// Reload the context menu.
        /// </summary>
        private void MenuUpdate()
        {
            ContextMenuStrip menu = notifyIcon.ContextMenuStrip;

            try
            {
                MenuClear();
                streamsFileLoader.LoadTo(menu, OnStreamItemClick);
                menu.Items.Add(dynamicMenuMarker);

                editItem.Enabled = true;
            }
            catch (StreamsFileReadError exception)
            {
                MenuClear();
                menu.Items.Add(dynamicMenuMarker);
                menu.Items.Add(errorReadItem);

                errorReadItem.Tag = exception;
                editItem.Enabled = true;
            }
            catch (Exception exception)
            {
                MenuClear();
                menu.Items.Add(dynamicMenuMarker);
                menu.Items.Add(errorOpenItem);

                errorOpenItem.Tag = exception;
                editItem.Enabled = false;
            }

            menu.Items.Add(editItem);
            menu.Items.Add("-");
            menu.Items.Add(audioMenuItem);
            menu.Items.Add(optionsMenuItem);
            menu.Items.Add(exitItem);
        }

        /// <summary>
        /// When the menu is about to be opened, reload from the streams file
        /// and update the renderer colors when needed.
        /// </summary>
        private void OnMenuOpening(Object sender, CancelEventArgs e)
        {
            // get the mouse position *before* doing anything
            // because it can move while we are reloading the menu:
            Point position = Util.MousePosition;

            // suspend/resume layout before/after reloading:
            notifyIcon.ContextMenuStrip.SuspendLayout();

            if (streamsFileLoader.MustReload())
            {
                MenuUpdate();
            }

            toolStripRenderer.UpdateColors();

            notifyIcon.ContextMenuStrip.ResumeLayout();
            e.Cancel = false;
            notifyIcon.ShowContextMenuStrip(position);
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
        /// Multimedia keys hook
        ///

        /// <summary>
        /// Start the multimedia keys hook.
        /// </summary>
        private void MultimediaKeysHook()
        {
            try
            {
                KeyboardHook.Hooker.Hook();
                optionsEnableMultimediaKeysItem.Checked = true;
            }
            catch (KeyboardHookException exception)
            {
                optionsEnableMultimediaKeysItem.Checked = KeyboardHook.Hooker.IsHooked;

                String text = exception.Message;
                String caption = "Error hooking multimedia keys";
                MessageBox.Show(text, caption, MessageBoxButtons.OK);
            }
        }

        /// <summary>
        /// Stop the multimedia keys hook.
        /// </summary>
        /// <param name="quiet">Ignore exceptions instead of showing a message.</param>
        private void MultimediaKeysUnhook(Boolean quiet = false)
        {
            try
            {
                KeyboardHook.Hooker.Unhook();
                optionsEnableMultimediaKeysItem.Checked = false;
            }
            catch (KeyboardHookException exception)
            {
                optionsEnableMultimediaKeysItem.Checked = KeyboardHook.Hooker.IsHooked;

                if (!quiet)
                {
                    String text = exception.Message;
                    String caption = "Error unhooking multimedia keys";
                    MessageBox.Show(text, caption, MessageBoxButtons.OK);
                }
            }
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
                notifyIcon.ShowContextMenuStrip(Util.MousePosition);
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
                notifyIcon.ShowContextMenuStrip(Util.MousePosition);
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
        /// Events: multimedia keys
        ///

        private void OnHotkeyDown(Object sender, KeyboardHookEventArgs e)
        {
            switch (e.Name)
            {
                case "Toggle Play":
                {
                    player.TogglePlay();
                    break;
                }
                case "Stop":
                {
                    player.Stop();
                    break;
                }
                case "Toggle Mute":
                {
                    player.ToggleMute();
                    break;
                }
                case "Volume Up":
                {
                    Int32 volume = volumeTrackBar.TrackBar.Value;
                    Int32 volume_max = volumeTrackBar.TrackBar.Maximum;
                    Int32 volume_step = volumeTrackBar.TrackBar.SmallChange;
                    volumeTrackBar.TrackBar.Value = Math.Min(volume + volume_step, volume_max);
                    break;
                }
                case "Volume Down":
                {
                    Int32 volume = volumeTrackBar.TrackBar.Value;
                    Int32 volume_min = volumeTrackBar.TrackBar.Minimum;
                    Int32 volume_step = volumeTrackBar.TrackBar.SmallChange;
                    volumeTrackBar.TrackBar.Value = Math.Max(volume - volume_step, volume_min);
                    break;
                }
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
        /// Multimedia keys clicked, toggle on or off.
        /// </summary>
        private void OnEnableMultimediaKeysItemClicked(Object sender, EventArgs e)
        {
            if (optionsEnableMultimediaKeysItem.Checked)
            {
                MultimediaKeysUnhook();
            }
            else
            {
                MultimediaKeysHook();
            }
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
            settings.OptionsEnableAutoPlayChecked = optionsEnableAutoPlayItem.Checked;
            settings.OptionsEnableMultimediaKeysChecked = optionsEnableMultimediaKeysItem.Checked;
            SettingsSave();

            // unhook, but don't be annoying with error messages on shutdown:
            if (optionsEnableMultimediaKeysItem.Checked)
            {
                MultimediaKeysUnhook(true);
            }

            notifyIcon.Visible = false;
            Application.Exit();
        }
    }
}

