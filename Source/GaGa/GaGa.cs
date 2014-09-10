
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

using GaGa.Controls;
using GlobalHotkeys;


namespace GaGa
{
    internal class GaGa : ApplicationContext
    {
        // gui components:
        private readonly Container container;
        private readonly NotifyIcon notifyIcon;
        private readonly ToolStripAeroRenderer toolStripRenderer;
        private readonly ContextMenuStrip menu;

        // player:
        private readonly Player player;

        // streams menu:
        private readonly String streamsFilepath;
        private readonly StreamsFileLoader streamsFileLoader;

        // streams menu constant items:
        private readonly ToolStripMenuItem errorOpenItem;
        private readonly ToolStripMenuItem errorReadItem;
        private readonly ToolStripMenuItem editItem;

        // audio settings:
        private readonly ToolStripMenuItem audioSettingsItem;
        private readonly ToolStripLabeledTrackBar volumeTrackBar;
        private readonly ToolStripLabeledTrackBar balanceTrackBar;

        // other menu items:
        private readonly ToolStripMenuItem exitItem;

        // Hotkeys
        private HotKeyManager hotkeyManager;

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
            notifyIcon.Icon = Util.ResourceAsIcon("GaGa.Resources.idle.ico");
            notifyIcon.MouseClick += OnIconMouseClick;
            notifyIcon.Visible = true;

            toolStripRenderer = new ToolStripAeroRenderer();

            menu = notifyIcon.ContextMenuStrip;
            menu.Opening += OnMenuOpening;
            menu.Renderer = toolStripRenderer;

            // player:
            player = new Player(notifyIcon);

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

            balanceTrackBar = new ToolStripLabeledTrackBar();
            balanceTrackBar.Label.Text = "Balance";
            balanceTrackBar.TrackBar.Minimum = -10;
            balanceTrackBar.TrackBar.Maximum = 10;
            balanceTrackBar.TrackBar.Value = 0;
            balanceTrackBar.TrackBar.ValueChanged += OnBalanceTrackBarChanged;

            volumeTrackBar = new ToolStripLabeledTrackBar();
            volumeTrackBar.Label.Text = "Volume";
            volumeTrackBar.TrackBar.Minimum = 0;
            volumeTrackBar.TrackBar.Maximum = 20;
            volumeTrackBar.TrackBar.Value = 10;
            volumeTrackBar.TrackBar.ValueChanged += OnVolumeTrackBarChanged;

            // change back color to the renderer color:
            Color back = toolStripRenderer.ColorTable.ToolStripDropDownBackground;

            balanceTrackBar.BackColor = back;
            balanceTrackBar.Label.BackColor = back;
            balanceTrackBar.TrackBar.BackColor = back;

            volumeTrackBar.BackColor = back;
            volumeTrackBar.Label.BackColor = back;
            volumeTrackBar.TrackBar.BackColor = back;

            audioSettingsItem.DropDownItems.Add(balanceTrackBar);
            audioSettingsItem.DropDownItems.Add(volumeTrackBar);

            // other items:
            exitItem = new ToolStripMenuItem();
            exitItem.Text = "Exit";
            exitItem.Click += OnExitItemClick;

            // Hotkeys - TODO: Add config in which user can define these
            hotkeyManager = new HotKeyManager();
            hotkeyManager.RegisterHotKey(ModifierKeys.Alt, Keys.P);
            hotkeyManager.RegisterHotKey(ModifierKeys.Alt, Keys.O);

            // Mediakeys - Alternative implementation for some keyboards
            hotkeyManager.RegisterHotKey(ModifierKeys.None, Keys.VolumeMute);
            hotkeyManager.RegisterHotKey(ModifierKeys.None, Keys.VolumeDown);
            hotkeyManager.RegisterHotKey(ModifierKeys.None, Keys.VolumeUp);
            hotkeyManager.RegisterHotKey(ModifierKeys.None, Keys.MediaPlayPause);
            hotkeyManager.RegisterHotKey(ModifierKeys.None, Keys.Play);
            hotkeyManager.RegisterHotKey(ModifierKeys.None, Keys.Pause);

            // Register Hot&Media key events
            hotkeyManager.HotKeyPressed += OnHotKeyPressed;
            hotkeyManager.MediaKeyPressed += OnMediaKeyPressed;

            // update everything:
            BalanceUpdate();
            VolumeUpdate();
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
                MenuUpdate();
            }

            toolStripRenderer.UpdateColors();

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
            Double current = (Double)balanceTrackBar.TrackBar.Value;
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
            Double current = (Double)volumeTrackBar.TrackBar.Value;
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
            ToolStripMenuItem item = (ToolStripMenuItem)sender;

            PlayerStream stream = new PlayerStream(item.Text, (Uri)item.Tag);
            player.Play(stream);
        }

        /// <summary>
        /// Opening error clicked, show details.
        /// </summary>
        private void OnErrorOpenItemClick(Object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
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
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            StreamsFileReadError exception = (StreamsFileReadError)item.Tag;

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

        #region Hotkeys & Mediakeys
        /// <summary>
        /// A Global Hotkey was pressed, process it
        /// </summary>
        /// <param name="e">contains the key & modifier that was pressed</param>
        private void OnHotKeyPressed(object sender, HotKeyPressedEventArgs e)
        {
            // TODO: Read these from a config file
            if (e.Modifier == ModifierKeys.Alt && e.Key == Keys.P)
            {
                player.TogglePlay();
            }
            else if (e.Modifier == ModifierKeys.Alt && e.Key == Keys.O)
            {
                player.ToggleMute();
            }

            // Media Keys
            else if (e.Key == Keys.VolumeMute) { player.ToggleMute(); }
            else if (e.Key == Keys.VolumeDown) { Volume -= 0.10; ; }
            else if (e.Key == Keys.VolumeUp) { Volume += 0.10; }
            else if (e.Key == Keys.MediaPlayPause) { player.TogglePlay(); }
            else if (e.Key == Keys.Play) { player.Play(); }
            else if (e.Key == Keys.Pause) { player.Stop(); }
        }

        /// <summary>
        /// A Media Key  was pressed, process it
        /// </summary>
        /// <param name="e">contains the Application Command that was send</param>
        private void OnMediaKeyPressed(object sender, MediaKeyPressedEventArgs e)
        {
            // TODO: Read these from a config file
            // TODO: Think about which one of these you want to use
            // TODO: [GlobalHotkeys] Think about providing individual events to subscribe to, instead of evaluating the ApplicationCommand on the consumer
            switch (e.Command)
            {
                case ApplicationCommand.VolumeMute:
                player.ToggleMute();
                break;
                case ApplicationCommand.VolumeDown:
                Volume -= 0.10;
                break;
                case ApplicationCommand.VolumeUp:
                Volume += 0.10;
                break;
                case ApplicationCommand.MediaNexttrack:
                // TODO: Read in the Streams in a list so we can cycle them instead of having only access to one stream
                break;
                case ApplicationCommand.MediaPrevioustrack:
                // TODO: Read in the Streams in a list so we can cycle them instead of having only access to one stream
                break;
                case ApplicationCommand.MediaStop:
                player.Stop();
                break;
                case ApplicationCommand.MediaPlayPause:
                player.TogglePlay();
                break;
                case ApplicationCommand.Close:
                Application.Exit();
                break;
                case ApplicationCommand.MediaPlay:
                player.Play();
                break;
                case ApplicationCommand.MediaPause:
                player.Stop();
                break;
                case ApplicationCommand.MediaFastForward:
                break;
                case ApplicationCommand.MediaRewind:
                break;
            }
        }

        /// <summary>
        /// Uses the range 0.0 - 1.0 and maps it to the Sliderbar
        /// TODO: Quick and dirty replace with something better
        /// TODO: I don't like that sliderbar and player use different volume scales, need to think about a better way of doing this 
        /// </summary>
        public double Volume
        {
            get { return (double)volumeTrackBar.TrackBar.Value / volumeTrackBar.TrackBar.Maximum; }
            set { volumeTrackBar.TrackBar.Value = (int)(value * volumeTrackBar.TrackBar.Maximum); }
        }
        #endregion
    }
}

