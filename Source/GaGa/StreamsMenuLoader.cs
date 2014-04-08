
// GaGa.
// A simple radio player running on the Windows notification area.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using mINI;


namespace GaGa
{
    internal class StreamsMenuLoader
    {
        private StreamsFile file;
        private DateTime lastUpdated;

        /// <summary>
        /// Can read a StreamsFile as an INI, monitor changes,
        /// and add all the sections and items to a ContextMenuStrip
        /// as submenus and clickable items.
        /// </summary>
        /// <param name="file">Streams file to read from.</param>
        public StreamsMenuLoader(StreamsFile file)
        {
            this.file = file;
            this.lastUpdated = DateTime.MinValue;
        }

        /// <summary>
        /// Determines whether the streams file has changed since last read.
        /// </summary>
        public Boolean HasChanged()
        {
            return lastUpdated != file.GetLastWriteTime();
        }

        /// <summary>
        /// Read the StreamsFile again, adding submenus and items
        /// to the given ContextMenuStrip.
        /// </summary>
        /// <param name="menu">Target ContextMenuStrip.</param>
        public void LoadTo(ContextMenuStrip menu)
        {
            StreamsFileReader reader = new StreamsFileReader(file, menu);
            reader.Read();
            lastUpdated = file.GetLastWriteTime();
        }
    }
}

