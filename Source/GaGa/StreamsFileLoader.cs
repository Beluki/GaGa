
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.IO;
using System.Windows.Forms;


namespace GaGa
{
    public class StreamsFileLoader
    {
        private readonly String filepath;
        private readonly String resourcepath;

        private readonly StreamsFileReader reader;
        private DateTime lastUpdated;

        /// <summary>
        /// Can read or recreate a streams file, monitor changes,
        /// and add all the sections and items to a context menu.
        /// </summary>
        /// <param name="filepath">Path to the streams file to read from.</param>
        /// <param name="resourcepath">Path to the resource used to recreate it.</param>
        public StreamsFileLoader(String filepath, String resourcepath)
        {
            this.filepath = filepath;
            this.resourcepath = resourcepath;

            reader = new StreamsFileReader();
            lastUpdated = DateTime.MinValue;
        }

        /// <summary>
        /// Determine whether the menu must be updated.
        /// True when the file is inaccesible or changed since the last update.
        /// </summary>
        public Boolean MustReload()
        {
            try
            {
                return lastUpdated != File.GetLastWriteTimeUtc(filepath);
            }
            // GetLastWriteTime() can raise exceptions
            // despite returning a date when the file does not exist:
            catch (Exception)
            {
                return true;
            }
        }

        /// <summary>
        /// Read the streams file again, adding submenus and items
        /// to the given context menu. The file will be recreated first
        /// when it doesn't exist.
        /// </summary>
        /// <param name="menu">Target context menu.</param>
        /// <param name="onClick">Click event to attach to menu items.</param>
        public void LoadTo(ContextMenuStrip menu, EventHandler onClick)
        {
            // reset so that unhandled exceptions invalidate the current time:
            lastUpdated = DateTime.MinValue;

            // make a single attempt to recreate the file when needed:
            if (!File.Exists(filepath))
            {
                Util.ResourceCopy(resourcepath, filepath);
            }

            DateTime lastWriteTime = File.GetLastWriteTimeUtc(filepath);

            // corner case:
            // the file may be deleted before calling GetLastWriteTimeUtc()
            if (lastWriteTime == Util.FileNotFoundUtc)
                throw new IOException("Streams file deleted after creating it.");

            try
            {
                reader.Read(filepath, menu, onClick);
                lastUpdated = lastWriteTime;
            }

            // update time on parsing errors
            // the syntax will still be wrong until the file changes:
            catch (StreamsFileReadError)
            {
                lastUpdated = lastWriteTime;
                throw;
            }
        }
    }
}

