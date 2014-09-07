
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
        private Boolean isIdle;

        private readonly Icon idleIcon;
        private readonly Icon playingIcon;
        private readonly Icon playingMutedIcon;
        private readonly Icon[] bufferingIcons;

        private readonly DispatcherTimer bufferingIconTimer;
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
            isIdle = true;

            idleIcon = Util.ResourceAsIcon("GaGa.Resources.idle.ico");
            playingIcon = Util.ResourceAsIcon("GaGa.Resources.playing.ico");
            playingMutedIcon = Util.ResourceAsIcon("GaGa.Resources.playing-muted.ico");

            bufferingIcons = new Icon[] {
                Util.ResourceAsIcon("GaGa.Resources.buffering1.ico"),
                Util.ResourceAsIcon("GaGa.Resources.buffering2.ico"),
                Util.ResourceAsIcon("GaGa.Resources.buffering3.ico"),
                Util.ResourceAsIcon("GaGa.Resources.buffering4.ico"),
            };

            bufferingIconTimer = new DispatcherTimer();
            bufferingIconTimer.Interval = TimeSpan.FromMilliseconds(300);
            bufferingIconTimer.Tick += OnBufferingIconTimerTick;
            currentBufferingIcon = 0;

            UpdateIcon();
        }

        ///
        /// Icon handling
        ///

        /// <summary>
        /// Updates the notify icon icon and tooltip text
        /// depending on the current player state.
        /// </summary>
        private void UpdateIcon()
        {
            Icon icon;
            String text;

            // player state
            if (isIdle)
            {
                icon = idleIcon;
                text = "Idle";
            }
            else if (player.IsMuted)
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
            text += " - ";

            // source state:
            if (source == null)
            {
                text += "No stream selected";
            }
            else
            {
                text += source.Name;
            }

            notifyIcon.Icon = icon;
            notifyIcon.SetToolTipText(text);
        }

        ///
        /// Player
        ///

        /// <summary>
        /// Open and play the current source stream.
        /// Unmutes the player.
        /// </summary>
        public void Play()
        {
            // do nothing if there is no source:
            if (source == null)
                return;

            player.Open(source.Uri);
            player.Play();
            player.IsMuted = false;

            isIdle = false;
            UnMute();
            UpdateIcon();
        }

        /// <summary>
        /// Stop playing and close the current stream.
        /// Unmutes the player.
        /// </summary>
        public void Stop()
        {
            // do nothing if there is no source or already idle:
            if ((source == null) || isIdle)
                return;

            // corner case:
            // if we only call .Stop(), the player continues downloading
            // from online streams, but .Close() calls _mediaState.Init()
            // changing the volume, so save and restore it:
            Double volume = player.Volume;

            player.Stop();
            player.Close();
            player.IsMuted = false;

            player.Volume = volume;

            bufferingIconTimer.Stop();
            currentBufferingIcon = 0;

            isIdle = true;
            UpdateIcon();
        }

        /// <summary>
        /// Set a given stream as current and play it.
        /// Unmutes the player.
        /// </summary>
        /// <param name="stream">Source stream to play.</param>
        public void Play(PlayerStream stream)
        {
            source = stream;
            Play();
        }

        /// <summary>
        /// Mute the player.
        /// </summary>
        public void Mute()
        {
            // do nothing if idle or already muted:
            if (isIdle || player.IsMuted)
                return;

            player.IsMuted = true;
            UpdateIcon();
        }

        /// <summary>
        /// Unmute the player.
        /// </summary>
        public void UnMute()
        {
            // do nothing if idle or not muted:
            if (isIdle || !player.IsMuted)
                return;

            player.IsMuted = false;
            UpdateIcon();
        }

        /// <summary>
        /// Toggle between idle/playing.
        /// </summary>
        public void TogglePlay()
        {
            if (isIdle)
            {
                Play();
            }
            else
            {
                Stop();
            }
        }

        /// <summary>
        /// Toggle between muted/unmuted.
        /// </summary>
        private void ToggleMute()
        {
            if (player.IsMuted)
            {
                UnMute();
            }
            else
            {
                Mute();
            }
        }

        /// <summary>
        /// Change the player balance.
        /// </summary>
        public void SetBalance(Double balance)
        {
            player.Balance = balance;
        }

        /// <summary>
        /// Change the player volume.
        /// </summary>
        public void SetVolume(Double volume)
        {
            player.Volume = volume;
        }

        ///
        /// Buffering animation
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
        /// Override the current icon with an animation while buffering.
        /// </summary>
        private void OnBufferingIconTimerTick(Object sender, EventArgs e)
        {
            // only change the icon when NOT muted
            // the mute icon has priority over the buffering icons:
            if (!player.IsMuted)
            {
                notifyIcon.Icon = bufferingIcons[currentBufferingIcon];
            }

            // but keep the animation always running:
            currentBufferingIcon++;
            if (currentBufferingIcon == bufferingIcons.Length)
            {
                currentBufferingIcon = 0;
            }
        }

        ///
        /// Media events
        ///

        /// <summary>
        /// Update state when media ended.
        /// </summary>
        private void OnMediaEnded(Object sender, EventArgs e)
        {
            Stop();
        }

        /// <summary>
        /// Update state when media failed. Show an error balloon with details.
        /// </summary>
        private void OnMediaFailed(Object sender, ExceptionEventArgs e)
        {
            Stop();

            String title = "Unable to play: " + source.Name;
            String text = e.ErrorException.Message + "\n" + source.Uri;

            notifyIcon.ShowBalloonTip(10, title, text, ToolTipIcon.Error);
        }

        ///
        /// Mouse control
        ///

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
        /// When not playing, show the context menu instead.
        /// </summary>
        private void OnMiddleMouseClick()
        {
            if (isIdle)
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

