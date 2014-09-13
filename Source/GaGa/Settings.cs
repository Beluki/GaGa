
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.IO;

using GaGa.Playing;


namespace GaGa
{
    [Serializable]
    internal class Settings
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

