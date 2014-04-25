
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;
using System.Threading;

namespace GaGa
{
    internal class Player
    {
        private readonly NotifyIcon notifyIcon;
        private readonly MediaPlayer player;

        private PlayerStream source;
        private Boolean playing;

        private readonly Icon playIcon;
        private readonly Icon stopIcon;
        private readonly Icon muteIcon;

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
            playing = false;

            playIcon = Utils.ResourceAsIcon("GaGa.Resources.play.ico");
            stopIcon = Utils.ResourceAsIcon("GaGa.Resources.stop.ico");
            muteIcon = Utils.ResourceAsIcon("GaGa.Resources.mute.ico");

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
        /// Updates the notify icon and tooltip text
        /// depending on the current player state.
        /// </summary>
        private void UpdateIcon()
        {
            String text;
            Icon icon;

            // add player state:
            if (playing)
            {
                text = "Playing";
                icon = stopIcon;

                if (player.IsMuted)
                {
                    text += " (muted) ";
                    icon = muteIcon;
                }
            }
            else
            {
                text = "Idle";
                icon = playIcon;
            }

            // add separator:
            text += " - ";

            // add source state:
            if (source != null)
            {
                text += source.Name;
            }
            else
            {
                text += "No stream selected";
            }

            notifyIcon.Icon = icon;
            notifyIcon.SetToolTipText(text);
        }

        /// <summary>
        /// Animates the icon while buffering.
        /// </summary>
        private void bufferingIconTimer_Tick(Object sender, EventArgs e)
        {
            // the mute icon has priority, do not change:
            if (!player.IsMuted)
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
            player.IsMuted = false;

            playing = true;
            UpdateIcon();
        }

        /// <summary>
        /// Stop playing and close the current source stream.
        /// </summary>
        private void StopPlaying()
        {
            player.Stop();
            player.Close();

            bufferingIconTimer.Stop();
            currentBufferingIcon = 0;

            playing = false;
            UpdateIcon();
        }

        /// <summary>
        /// Toggle between playing/stopped.
        /// </summary>
        private void TogglePlay()
        {
            if (playing)
            {
                StopPlaying();
            }
            else
            {
                StartPlaying();
            }
        }

        /// <summary>
        /// Toggle between muted/unmuted.
        /// </summary>
        private void ToggleMute()
        {
            player.IsMuted = !player.IsMuted;
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
        /// Toggle play with the left mouse button when there is a stream.
        /// Show the context menu otherwise.
        /// </summary>
        private void OnLeftMouseClick()
        {
            if (source != null)
            {
                TogglePlay();
            }
            else
            {
                notifyIcon.InvokeContextMenu();
            }
        }

        /// <summary>
        /// Toggle mute with the wheel button when playing.
        /// Show the context menu otherwise.
        /// </summary>
        private void OnMiddleMouseClick()
        {
            if (playing)
            {
                ToggleMute();
            }
            else
            {
                notifyIcon.InvokeContextMenu();
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

