
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.IO;

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
        /// Stores program settings.
        /// </summary>
        public GaGaSettings()
        {
            LastBalanceTrackBarValue = 0;
            LastVolumeTrackBarValue = 10;
            LastPlayerStream = null;
            OptionsEnableAutoPlayChecked = false;
            OptionsEnableMultimediaKeysChecked = true;
        }
    }
}

