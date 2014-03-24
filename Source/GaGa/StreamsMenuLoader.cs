
// GaGa.
// A simple radio player running on the Windows notification area.


using System;
using System.Windows.Forms;


namespace GaGa
{
    internal class StreamsMenuLoader
    {
        private StreamsFile file;
        private ContextMenuStrip menu;
        private DateTime lastUpdated;

        /// <summary>
        /// Maintains a dynamic ToolStripItemCollection of clickable
        /// radio streams, autoloaded on changes from a StreamsFile.
        /// </summary>
        /// <param name="file">Streams file to read from.</param>
        public StreamsMenuLoader(StreamsFile file)
        {
            this.file = file;
            this.menu = new ContextMenuStrip();
            this.lastUpdated = DateTime.MinValue;
        }

        /// <summary>
        /// Get all the items.
        /// </summary>
        public ToolStripItemCollection Items
        {
            get { return menu.Items; }
        }

        /// <summary>
        /// Reload the items if the streams file changed since the last update.
        /// Returns true or false depending on whether a reload was needed.
        /// </summary>
        public Boolean MaybeReload()
        {
            DateTime file_last_modified = file.GetLastWriteTime();
            if (lastUpdated == file_last_modified)
                return false;

            Reload();
            lastUpdated = file_last_modified;
            return true;
        }

        /// <summary>
        /// Throw a StreamsMenuLoaderParsingError for the current file.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="line">Line text for the incorrect line.</param>
        /// <param name="linenumber">Line number where the error happened.</param>
        private void ThrowParsingError(String message, String line, int linenumber)
        {
            throw new StreamsMenuLoaderParsingError(message, file.filepath, line, linenumber);
        }

        /// <summary>
        /// Reload the items from the streams file.
        /// </summary>
        private void Reload()
        {
            menu.Items.Clear();
            ToolStripItemCollection root = menu.Items;
            int linenumber = 0;

            foreach (String line in file.ReadLineByLine())
            {
                linenumber++;
                String text = line.Trim();

                // empty line, back to the menu root:
                if (String.IsNullOrEmpty(text))
                    root = menu.Items;

                // comment, skip:
                else if (text.StartsWith("#") || text.StartsWith(";"))
                    continue;

                // submenu, create it and change root:
                else if (text.StartsWith("[") && text.EndsWith("]"))
                {
                    String name = text.Substring(1, text.Length - 2).Trim();

                    // do not accept empty submenu names:
                    if (String.IsNullOrEmpty(name))
                        ThrowParsingError("Empty menu name.", line, linenumber);

                    ToolStripMenuItem submenu = new ToolStripMenuItem(name);
                    root.Add(submenu);
                    root = submenu.DropDownItems;
                }

                // stream link, add to the current root:
                else if (text.Contains("="))
                {
                    String[] pair = text.Split(new char[] { '=' }, 2);
                    String name = pair[0].Trim();
                    String uri = pair[1].Trim();

                    // do not accept empty names:
                    if (String.IsNullOrEmpty(name))
                        ThrowParsingError("Empty stream name.", line, linenumber);

                    // empty uri is ok, skipped, user can edit later:
                    if (String.IsNullOrEmpty(uri))
                        continue;

                    ToolStripMenuItem radio = new ToolStripMenuItem(name);
                    radio.Tag = uri;
                    radio.ToolTipText = uri;
                    root.Add(radio);
                }

                // unknown:
                else ThrowParsingError("Invalid syntax.", line, linenumber);
            }
        }
    }
}

