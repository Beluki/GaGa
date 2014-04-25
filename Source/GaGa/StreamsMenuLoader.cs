
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
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
            reader = new StreamsFileReader();

            lastUpdated = null;
        }

        /// <summary>
        /// Determine whether the menu must be updated.
        /// True when the file is inaccesible or changed since the last update.
        /// </summary>
        public Boolean MustReload
        {
            get
            {
                try
                {
                    return lastUpdated != file.GetLastWriteTime();
                }

                // GetLastWriteTime can raise exceptions
                // despite returning a date when the file does not exist:
                catch (Exception)
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Recreate the streams file if it doesn't exist
        /// and get the last write time.
        /// </summary>
        private DateTime RecreatingGetLastWriteTime()
        {
            try
            {
                file.RecreateUnlessExists();
                DateTime lastWriteTime = file.GetLastWriteTime();

                // Corner case:
                //
                // RecreateUnlessExists() didn't raise an exception
                // but the file was deleted before calling GetLastWriteTime()

                if (lastWriteTime.ToFileTimeUtc() == Utils.FileNotFoundUtc)
                    throw new IOException("Streams file deleted after creating it.");

                return lastWriteTime;
            }

            // reset time on exceptions during recreation:
            catch (Exception)
            {
                lastUpdated = null;
                throw;
            }
        }

        /// <summary>
        /// Read the streams file again, adding submenus and items
        /// to the given context menu. The file will be recreated first
        /// when it doesn't exist.
        /// </summary>
        /// <param name="menu">Target context menu.</param>
        /// <param name="onClick">Click event to attach to menu items.</param>
        public void LoadTo(ContextMenu menu, EventHandler onClick)
        {
            DateTime lastWriteTime = RecreatingGetLastWriteTime();

            try
            {
                reader.Read(file, menu, onClick);
                lastUpdated = lastWriteTime;
            }

            // update time on parsing errors:
            catch (StreamsFileReadError)
            {
                lastUpdated = lastWriteTime;
                throw;
            }

            // reset time on non-parsing exceptions (IO) while reading:
            catch (Exception)
            {
                lastUpdated = null;
                throw;
            }
        }
    }
}

