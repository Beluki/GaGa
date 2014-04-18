
// GaGa.
// A simple radio player running on the Windows notification area.


using System;


namespace GaGa
{
    internal class RadioStream
    {
        public readonly String Name;
        public readonly String Link;

        /// <summary>
        /// Represents a playable stream with a name
        /// and a specified link that can be parsed to an Uri.
        /// </summary>
        /// <param name="name">Stream name.</param>
        /// <param name="link">Stream link.</param>
        public RadioStream(String name, String link)
        {
            this.Name = name;
            this.Link = link;
        }

        /// <summary>
        /// Parse the stream link and return an Uri
        /// suitable for MediaPlayer.
        /// </summary>
        public Uri GetUri()
        {
            // this does nothing right now, but it will be useful
            // if we add support for PLS or other formats:
            return new Uri(Link);
        }
    }
}

