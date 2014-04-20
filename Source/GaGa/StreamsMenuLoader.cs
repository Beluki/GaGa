
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;


namespace GaGa
{
    internal class StreamsMenuLoader
    {
        private readonly StreamsFile file;
        private readonly StreamsFileReader reader;
        private Nullable<DateTime> lastUpdated;

        /// <summary>
        /// Can read or recreate a streams file, monitor changes,
        /// and add all the sections and items to a context menu.
        /// </summary>
        /// <param name="file">Streams file to read from.</param>
        public StreamsMenuLoader(StreamsFile file)
        {
            this.file = file;
            this.reader = new StreamsFileReader();
            this.lastUpdated = null;
        }

        /// <summary>
        /// Determine whether we need to reload the streams file.
        ///
        /// Returns true when the file does not exist
        /// or when it changed since the last update.
        /// </summary>
        public Boolean MustReload()
        {
            return lastUpdated != file.GetLastWriteTime();
        }

        /// <summary>
        /// Read the streams file again, adding submenus and items
        /// to the given context menu.
        /// </summary>
        /// <param name="menu">Target context menu.</param>
        /// <param name="onClick">Click event to attach to menu items.</param>
        public void LoadTo(ContextMenu menu, EventHandler onClick)
        {
            file.CreateUnlessExists();
            DateTime lastWriteTime = file.GetLastWriteTime();

            // Corner case:
            //
            // We made an attempt to recreate the file when needed
            // but it could be deleted right before calling GetLastWriteTime().
            //
            // In this case, raise an exception now, don't touch lastUpdated.
            // lastUpdated should only contain *valid* dates or null, so that
            // when the file doesn't exist, MustReload() returns true.

            if (lastWriteTime.IsFileNotFound())
                throw new IOException("Streams file deleted during load.");

            try
            {
                reader.Read(file, menu, onClick);
                lastUpdated = lastWriteTime;
            }

            // update our time on StreamFileReadError too
            // because the file will still be wrong until it changes:
            catch (StreamsFileReadError)
            {
                lastUpdated = lastWriteTime;
                throw;
            }
        }
    }
}

