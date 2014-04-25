
// GaGa.
// A minimal radio player for the Windows Tray.


using System;


namespace GaGa
{
    internal class PlayerStream
    {
        /// <summary>
        /// Stream name.
        /// </summary>
        public readonly String Name;

        /// <summary>
        /// Streaming uri.
        /// </summary>
        public readonly Uri Uri;

        /// <summary>
        /// Represents a playable media stream. 
        /// </summary>
        /// <param name="name">Stream name.</param>
        /// <param name="uri">Streaming uri.</param>
        public PlayerStream(String name, Uri uri)
        {
            Name = name;
            Uri = uri;
        }
    }
}

