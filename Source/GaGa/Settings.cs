
// GaGa.
// A minimal radio player for the Windows Tray.


using System;


namespace GaGa
{
    [Serializable]
    internal class Settings
    {
        /// <summary>
        /// Last balance value set in the audio settings menu.
        /// </summary>
        public Int32 LastBalanceTrackBarValue;

        /// <summary>
        /// Last volume value set in the audio settings menu.
        /// </summary>
        public Int32 LastVolumeTrackBarValue;

        /// <summary>
        /// Last stream played.
        /// </summary>
        public PlayerStream LastPlayerStream;

        /// <summary>
        /// Stores program settings.
        /// </summary>
        public Settings()
        {
            LastBalanceTrackBarValue = 0;
            LastVolumeTrackBarValue = 10;
            LastPlayerStream = null;
        }
    }
}

