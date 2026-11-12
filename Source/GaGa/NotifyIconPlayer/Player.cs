
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;


namespace GaGa.NotifyIconPlayer
{
    internal class Player
    {
        private readonly NotifyIcon notifyIcon;
        private readonly MediaPlayer player;

        private PlayerStream source;
        private Boolean isIdle;
        private HttpsRelay relay;
        private String title;
        private readonly Dispatcher dispatcher;

        private readonly Icon idleIcon;
        private readonly Icon playingIcon;
        private readonly Icon playingMutedIcon;
        private readonly Icon[] bufferingIcons;

        private readonly DispatcherTimer bufferingIconTimer;
        private Int32 currentBufferingIcon;

        /// <summary>
        /// A media player that takes control of a NotifyIcon icon,
        /// tooltip and balloon to display status.
        /// </summary>
        /// <param name="icon">
        /// The notify icon to use to display status.
        /// </param>
        public Player(NotifyIcon icon)
        {
            notifyIcon = icon;
            dispatcher = Dispatcher.CurrentDispatcher;

            player = new MediaPlayer();
            player.BufferingStarted += OnBufferingStarted;
            player.BufferingEnded += OnBufferingEnded;
            player.MediaEnded += OnMediaEnded;
            player.MediaFailed += OnMediaFailed;

            idleIcon = Util.ResourceAsIcon("GaGa.NotifyIconPlayer.Resources.Idle.ico");
            playingIcon = Util.ResourceAsIcon("GaGa.NotifyIconPlayer.Resources.Playing.ico");
            playingMutedIcon = Util.ResourceAsIcon("GaGa.NotifyIconPlayer.Resources.Playing-muted.ico");

            bufferingIcons = new Icon[] {
                Util.ResourceAsIcon("GaGa.NotifyIconPlayer.Resources.Buffering1.ico"),
                Util.ResourceAsIcon("GaGa.NotifyIconPlayer.Resources.Buffering2.ico"),
                Util.ResourceAsIcon("GaGa.NotifyIconPlayer.Resources.Buffering3.ico"),
                Util.ResourceAsIcon("GaGa.NotifyIconPlayer.Resources.Buffering4.ico"),
            };

            bufferingIconTimer = new DispatcherTimer(DispatcherPriority.Background);
            bufferingIconTimer.Interval = TimeSpan.FromMilliseconds(300);
            bufferingIconTimer.Tick += OnBufferingIconTimerTick;
            currentBufferingIcon = 0;

            source = null;
            isIdle = true;

            UpdateIcon();
        }

        ///
        /// Icon handling
        ///

        /// <summary>
        /// Updates the NotifyIcon icon and tooltip text
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

            // current track, when the stream announces it:
            if (!isIdle && title != null)
            {
                text += "\n" + title;
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
            // do nothing if there is no current source:
            if (source == null)
                return;

            // the WPF MediaPlayer can't open https:// itself,
            // so https streams go through a local relay:
            CloseRelay();
            SetTitle(null);
            if (source.Uri.Scheme == Uri.UriSchemeHttps)
            {
                HttpsRelay newRelay = new HttpsRelay(source.Uri);
                newRelay.TitleChanged += (String newTitle) =>
                    dispatcher.BeginInvoke(new Action(() => OnRelayTitleChanged(newRelay, newTitle)));

                relay = newRelay;
                player.Open(relay.LocalUri);
            }
            else
            {
                player.Open(source.Uri);
            }
            player.Play();
            player.IsMuted = false;

            isIdle = false;
            UpdateIcon();
        }

        /// <summary>
        /// Stop the https relay of the previous stream, if any.
        /// </summary>
        private void CloseRelay()
        {
            if (relay != null)
            {
                relay.Dispose();
                relay = null;
            }
        }

        ///
        /// Track title
        ///

        /// <summary>
        /// Raised (on the UI thread) when the title of the
        /// track playing on the current stream changes.
        /// </summary>
        public event EventHandler TitleChanged;

        /// <summary>
        /// Title of the track playing on the current stream,
        /// or null when unknown or idle.
        /// </summary>
        public String Title
        {
            get
            {
                return title;
            }
        }

        private void SetTitle(String newTitle)
        {
            if (newTitle == title)
                return;

            title = newTitle;
            UpdateIcon();

            EventHandler handler = TitleChanged;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private void OnRelayTitleChanged(HttpsRelay sender, String newTitle)
        {
            // ignore late titles from a stream that was already closed:
            if (sender != relay || isIdle)
                return;

            SetTitle(newTitle);
        }

        /// <summary>
        /// Stop playing and close the current stream.
        /// Unmutes the player.
        /// </summary>
        public void Stop()
        {
            // do nothing if already idle:
            if (isIdle)
                return;

            // corner case:
            // if we only call .Stop(), the player continues downloading
            // from online streams, but .Close() calls _mediaState.Init()
            // changing the balance/volume, so save and restore them:
            Double balance = player.Balance;
            Double volume = player.Volume;

            player.Stop();
            player.Close();
            player.IsMuted = false;
            CloseRelay();

            player.Balance = balance;
            player.Volume = volume;

            bufferingIconTimer.Stop();
            currentBufferingIcon = 0;

            isIdle = true;
            UpdateIcon();
            SetTitle(null);
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
        /// Stop playing and set a given stream as current.
        /// Unmutes the player.
        /// </summary>
        /// <param name="stream">Source stream to set as current.</param>
        public void Select(PlayerStream stream)
        {
            Stop();
            source = stream;
            UpdateIcon();
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
        public void ToggleMute()
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
        /// Determine whether the player is currently idle.
        /// </summary>
        public Boolean IsIdle
        {
            get
            {
                return isIdle;
            }
        }

        /// <summary>
        /// Get the current player stream.
        /// </summary>
        public PlayerStream Source
        {
            get
            {
                return source;
            }
        }

        /// <summary>
        /// Get or set the player balance.
        /// </summary>
        public Double Balance
        {
            get
            {
                return player.Balance;
            }
            set
            {
                player.Balance = value;
            }
        }

        /// <summary>
        /// Get or set the player volume.
        /// </summary>
        public Double Volume
        {
            get
            {
                return player.Volume;
            }
            set
            {
                player.Volume = value;
            }
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
            // change the icon when NOT muted
            // the mute icon has priority over the buffering icons:
            if (!player.IsMuted)
            {
                notifyIcon.Icon = bufferingIcons[currentBufferingIcon];
            }

            // keep the animation always running:
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
            // for https streams the real reason is known by the relay:
            String reason = (relay != null && relay.LastError != null) ? relay.LastError : e.ErrorException.Message;

            Stop();

            String title = "Unable to play: " + Source.Name;
            String text = reason + "\n" + Source.Uri;

            notifyIcon.ShowBalloonTip(10, title, text, ToolTipIcon.Error);
        }
    }
}

