
// GaGa.
// A simple radio player running on the Windows notification area.


using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;


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
        /// Determine whether we need to reload our streams file.
        /// </summary>
        public Boolean MustReload()
        {
            return lastUpdated != file.GetLastWriteTime();
        }

        /// <summary>
        /// Read the streams file again, adding submenus and items
        /// to the given ContextMenu.
        /// </summary>
        /// <param name="menu">Target context menu.</param>
        public void LoadTo(ContextMenu menu)
        {
            file.CreateUnlessExists();

            // the file could have been deleted right after creation:
            DateTime lastWriteTime = file.GetLastWriteTime();

            if (lastWriteTime.ToFileTimeUtc() == Utils.fileNotFoundUtc)
                throw new IOException("Unable to create streams file.");

            try
            {
                StreamsFileReader reader = new StreamsFileReader(menu);
                reader.ReadLines(file.ReadLineByLine());
                lastUpdated = lastWriteTime;
            }

            // update our time on StreamFileReadErrors too
            // because the file will still be wrong until it changes:
            catch (StreamsFileReadError)
            {
                lastUpdated = lastWriteTime;
                throw;
            }
        }
    }
}

