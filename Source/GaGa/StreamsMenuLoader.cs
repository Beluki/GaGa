
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
        private Nullable<DateTime> lastUpdated;

        /// <summary>
        /// Can read a StreamsFile as an INI, monitor changes,
        /// and add all the sections and items to a ContextMenuStrip
        /// as submenus and clickable items.
        /// </summary>
        /// <param name="file">Streams file to read from.</param>
        public StreamsMenuLoader(StreamsFile file)
        {
            this.file = file;
            this.lastUpdated = null;
        }

        /// <summary>
        /// Determine whether we need to reload our StreamsFile.
        /// Returns true when the file does not exist or when it
        /// changed since the last read.
        /// </summary>
        public Boolean MustReload()
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
            file.CreateUnlessExists();

            // loading is instant for all practical purposes
            // but grab the current write time before proceeding:
            DateTime lastWriteTime = file.GetLastWriteTime();

            StreamsFileReader reader = new StreamsFileReader(file, menu);
            reader.Read();

            // the file exists and reading ok, update time:
            lastUpdated = lastWriteTime;
        }
    }
}

