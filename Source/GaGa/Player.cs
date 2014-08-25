
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;


namespace GaGa
{
    internal class Player
    {
        private readonly NotifyIcon notifyIcon;
        private readonly MediaPlayer player;

        private PlayerStream source;
        private bool IsIdle { get; set; }
        private bool IsMuted
        {
            get { return player.IsMuted; }
            set { player.IsMuted = value; }
        }

        private double Volume
        {
            get { return player.Volume; }
            set { player.Volume = Utils.Clamp(value, 0, 1); }
        }

        private readonly Icon idleIcon;
        private readonly Icon playingIcon;
        private readonly Icon playingMutedIcon;

        private readonly DispatcherTimer bufferingIconTimer;
        private readonly Icon[] bufferingIcons;
        private Int32 currentBufferingIcon;

        /// <summary>
        /// A media player that is controlled from a notify icon
        /// using the mouse. Takes control of the notifyicon icon,
        /// tooltip and balloon to display status.
        /// </summary>
        /// <param name="icon">
        /// The notify icon that controls playback.
        /// </param>
        public Player(NotifyIcon icon)
        {
            notifyIcon = icon;
            notifyIcon.MouseClick += OnMouseClick;

            player = new MediaPlayer();
            player.BufferingStarted += OnBufferingStarted;
            player.BufferingEnded += OnBufferingEnded;
            player.MediaEnded += OnMediaEnded;
            player.MediaFailed += OnMediaFailed;

            source = null;
            IsIdle = true;

            idleIcon = Utils.ResourceAsIcon("GaGa.Resources.idle.ico");
            playingIcon = Utils.ResourceAsIcon("GaGa.Resources.playing.ico");
            playingMutedIcon = Utils.ResourceAsIcon("GaGa.Resources.playing-muted.ico");

            bufferingIcons = new Icon[] {
                Utils.ResourceAsIcon("GaGa.Resources.buffering1.ico"),
                Utils.ResourceAsIcon("GaGa.Resources.buffering2.ico"),
                Utils.ResourceAsIcon("GaGa.Resources.buffering3.ico"),
                Utils.ResourceAsIcon("GaGa.Resources.buffering4.ico"),
            };

            bufferingIconTimer = new DispatcherTimer();
            bufferingIconTimer.Interval = TimeSpan.FromMilliseconds(300);
            bufferingIconTimer.Tick += bufferingIconTimer_Tick;

            currentBufferingIcon = 0;

            UpdateIcon();
        }

        ///
        /// Notify icon and text.
        ///

        /// <summary>
        /// Updates the notify icon icon and tooltip text
        /// depending on the current player state.
        /// </summary>
        private void UpdateIcon()
        {
            Icon icon;
            String text;
            const string separator = " | ";

            // player state:
            if (IsIdle)
            {
                icon = idleIcon;
                text = "Idle";
            }
            else if (IsMuted)
            {
                icon = playingMutedIcon;
                text = "Playing (muted)";
            }
            else
            {
                icon = playingIcon;
                text = "Playing";
            }


            // separator:
            text += separator;

            // source state:
            if (source != null)
            {
                text += source.Name;
            }
            else
            {
                text += "No stream selected";
            }


            // Volume:
            text += separator + Volume.ToString("P0");


            notifyIcon.Icon = icon;
            notifyIcon.SetToolTipText(text);
        }

        /// <summary>
        /// Override the current icon with an animation while buffering.
        /// </summary>
        private void bufferingIconTimer_Tick(Object sender, EventArgs e)
        {
            // the mute icon has priority over the buffering icons:
            if (IsMuted)
                notifyIcon.Icon = bufferingIcons[currentBufferingIcon];

            currentBufferingIcon++;
            if (currentBufferingIcon == bufferingIcons.Length)
                currentBufferingIcon = 0;
        }

        ///
        /// Unsafe private methods.
        ///

        /// <summary>
        /// Open and play the current source stream.
        /// Unmutes the player if currently muted.
        /// </summary>
        private void StartPlaying()
        {
            player.Open(source.Uri);
            player.Play();
            IsMuted = false;
            IsIdle = false;
            UpdateIcon();
        }

        /// <summary>
        /// Stop playing and close the current source stream.
        /// </summary>
        private void StopPlaying()
        {
            // HACK: This fixes the behaviour that when the player is closed reopened that the player starts with the default volume
            // TODO: Implement Volume Information into the config file
            double lastVolume = Volume;

            player.Stop();
            player.Close();

            Volume = lastVolume;

            bufferingIconTimer.Stop();
            currentBufferingIcon = 0;

            IsIdle = true;
            UpdateIcon();
        }

        /// <summary>
        /// Toggle between playing/stopped.
        /// </summary>
        private void TogglePlay()
        {
            if (IsIdle)
            {
                StartPlaying();
            }
            else
            {
                StopPlaying();
            }
        }

        /// <summary>
        /// Toggle between muted/unmuted.
        /// </summary>
        private void ToggleMute()
        {
            IsMuted = !IsMuted;
            UpdateIcon();
        }

        ///
        /// Media events.
        ///

        /// <summary>
        /// Start watching the buffering state to update our icon.
        /// </summary>
        private void OnBufferingStarted(Object sender, EventArgs e)
        {
            bufferingIconTimer.Start();
        }

        /// <summary>
        /// Stop watching the buffering state.
        /// </summary>
        private void OnBufferingEnded(Object sender, EventArgs e)
        {
            bufferingIconTimer.Stop();
            currentBufferingIcon = 0;
            UpdateIcon();
        }

        /// <summary>
        /// Update state when media ended.
        /// </summary>
        private void OnMediaEnded(Object sender, EventArgs e)
        {
            StopPlaying();
        }

        /// <summary>
        /// Update state when media failed. Show an error balloon with details.
        /// </summary>
        private void OnMediaFailed(Object sender, ExceptionEventArgs e)
        {
            StopPlaying();

            String title = "Unable to play: " + source.Name;
            String text = e.ErrorException.Message + "\n" + source.Uri;

            notifyIcon.ShowBalloonTip(10, title, text, ToolTipIcon.Error);
        }

        ///
        /// Safe external interface/mouse control.
        ///

        /// <summary>
        /// Open a stream and start playing it.
        /// </summary>
        /// <param name="target">Stream to play.</param>
        public void Play(PlayerStream stream)
        {
            source = stream;
            StartPlaying();
        }

        /// <summary>
        /// Stop playing the current stream.
        /// </summary>
        public void Stop()
        {
            StopPlaying();
        }

        /// <summary>
        /// The new Volume to set
        /// </summary>
        /// <param name="amount">the amount needs to be beetwen 0.0 and 1.0</param>
        public void ChangeVolume(double amount)
        {
            Volume = amount;
            UpdateIcon();
        }

        /// <summary>
        /// Toggle play with the left mouse button.
        /// When no stream has been selected, show the context menu instead.
        /// </summary>
        private void OnLeftMouseClick()
        {
            if (source == null)
            {
                notifyIcon.InvokeContextMenu();
            }
            else
            {
                TogglePlay();
            }
        }

        /// <summary>
        /// Toggle mute with the wheel button.
        /// When idle, show the context menu instead.
        /// </summary>
        private void OnMiddleMouseClick()
        {
            if (IsIdle)
            {
                notifyIcon.InvokeContextMenu();
            }
            else
            {
                ToggleMute();
            }
        }

        /// <summary>
        /// Allow control via mouse.
        /// </summary>
        private void OnMouseClick(Object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    OnLeftMouseClick();
                    break;

                case MouseButtons.Middle:
                    OnMiddleMouseClick();
                    break;

                case MouseButtons.Right:
                case MouseButtons.XButton1:
                case MouseButtons.XButton2:
                    break;
            }
        }

    }
}

