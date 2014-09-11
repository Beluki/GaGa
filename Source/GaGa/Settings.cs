
// GaGa.
// A minimal radio player for the Windows Tray.


using System;


namespace GaGa
{
    [Serializable]
    internal class Settings
    {
        public Int32 LastBalanceTrackBarValue;
        public Int32 LastVolumeTrackBarValue;
        public PlayerStream LastPlayerStream;

        public Settings()
        {
            LastBalanceTrackBarValue = 0;
            LastVolumeTrackBarValue = 10;
            LastPlayerStream = null;
        }
    }
}

