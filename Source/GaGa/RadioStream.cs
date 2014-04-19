
// GaGa.
// A minimal radio player for the Windows Tray.


using System;


namespace GaGa
{
    internal class RadioStream
    {
        public readonly String Name;
        private Uri Uri;

        /// <summary>
        /// Represents a playable radio stream.
        /// </summary>
        /// <param name="name">Stream name.</param>
        /// <param name="uri">Stream uri.</param>
        public RadioStream(String name, Uri uri)
        {
            this.Name = name;
            this.Uri = uri;
        }

        /// <summary>
        /// Return an Uri suitable for MediaPlayer playback.
        /// </summary>
        public Uri GetPlayerUri()
        {
            // does nothing right now
            // it can be used to preprocess the uri when adding
            // support for PLS or other formats:
            return Uri;
        }
    }
}

