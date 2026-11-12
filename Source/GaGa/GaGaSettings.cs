
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.IO;
using System.Runtime.Serialization;

using GaGa.NotifyIconPlayer;


namespace GaGa
{
    [Serializable]
    internal class GaGaSettings
    {
        /// <summary>
        /// Last balance value set in the audio menu.
        /// </summary>
        public Int32 LastBalanceTrackBarValue;

        /// <summary>
        /// Last volume value set in the audio menu.
        /// </summary>
        public Int32 LastVolumeTrackBarValue;

        /// <summary>
        /// Last stream played.
        /// </summary>
        public PlayerStream LastPlayerStream;

        /// <summary>
        /// Whether the enable auto play options is checked.
        /// </summary>
        public Boolean OptionsEnableAutoPlayChecked;

        /// <summary>
        /// Whether the multimedia keys option is checked.
        /// </summary>
        public Boolean OptionsEnableMultimediaKeysChecked;

        /// <summary>
        /// Whether the track change notifications option is checked.
        /// Optional: settings files from older versions don't have it.
        /// </summary>
        [OptionalField]
        public Boolean OptionsEnableTrackNotificationsChecked;

        /// <summary>
        /// Stores program settings.
        /// </summary>
        public GaGaSettings()
        {
            LastBalanceTrackBarValue = 0;
            LastVolumeTrackBarValue = 10;
            LastPlayerStream = null;
            OptionsEnableAutoPlayChecked = false;
            OptionsEnableMultimediaKeysChecked = true;
            OptionsEnableTrackNotificationsChecked = true;
        }

        /// <summary>
        /// Defaults for fields missing in older settings files.
        /// </summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            OptionsEnableTrackNotificationsChecked = true;
        }
    }
}

