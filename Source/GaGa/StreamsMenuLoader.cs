
// GaGa.
// A single icon radio player on the Windows notification area.


using System;
using System.IO;
using System.Windows.Forms;


namespace GaGa
{
    /// <summary>
    /// Raised on a parsing error.
    /// </summary>
    public class StreamsMenuLoaderParsingError : Exception
    {
        public StreamsMenuLoaderParsingError() {}
        public StreamsMenuLoaderParsingError(String message) : base(message) {}
    }

    /// <summary>
    /// Maintains a dynamic ContextMenuStrip, autoloaded
    /// on changes from an INI file, with an embedded resource as a fallback.
    /// </summary>
    internal class StreamsMenuLoader
    {
        private String filepath;
        private String resource;

        private ContextMenuStrip menu;
        private DateTime last_update;

        public StreamsMenuLoader(String filepath, String resource)
        {
            this.filepath = filepath;
            this.resource = resource;
            this.menu = new ContextMenuStrip();
        }

        /// <summary>
        /// Get all the items in the menu.
        /// </summary>
        public ToolStripItemCollection Items {
            get { return menu.Items; }
        }

        /// <summary>
        /// Reload the menu when the INI file changed since the last update.
        /// Returns true or false depending on whether a reload was needed.
        /// 
        /// When the INI file does not exist, it's recreated first
        /// from the embedded resource.
        /// </summary>
        public Boolean MaybeReload()
        {
            if (!File.Exists(filepath))
                Utils.CopyResource(resource, filepath);

            DateTime last_modified = File.GetLastWriteTime(filepath);
            if (last_update == last_modified)
                return false;

            Reload();
            last_update = last_modified;
            return true;
        }

        /// <summary>
        /// Recreate the menu from the INI file.
        /// </summary>
        private void Reload()
        {
            menu.Items.Clear();
            ToolStripItemCollection root = menu.Items;
            int linenumber = 0;

            foreach (String line in Utils.ReadLines(filepath))
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
                    String name = line.Substring(1, line.Length - 2).Trim();

                    // do not accept empty submenu names:
                    if (String.IsNullOrEmpty(name))
                        ParseError(linenumber, "Empty menu name.", line);

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
                        ParseError(linenumber, "Empty stream name.", line);

                    // empty uri is ok, skipped, user can edit later:
                    if (String.IsNullOrEmpty(uri))
                        continue;

                    ToolStripMenuItem radio = new ToolStripMenuItem(name);
                    radio.Tag = uri;
                    radio.ToolTipText = uri;
                    root.Add(radio);
                }

                // unknown:
                else ParseError(linenumber, "Invalid syntax.", line);
            }
        }

        /// <summary>
        /// Raise an StreamsMenuParsingError.
        /// </summary>
        /// <param name="linenumber">
        /// Line number the error happened at.
        /// </param>
        /// <param name="message">
        /// Error message.
        /// </param>
        /// <param name="line">
        /// Complete text for the line where the error happened.
        /// </param>
        private void ParseError(int linenumber, String message, String line)
        {
            // Example:
            // streams.ini 10: Invalid syntax.
            // Line text.
            String error_message = String.Format(
                "{0} {1}:  {2} \n\n {3}",
                filepath,
                linenumber.ToString(),
                message,
                line);

            throw new StreamsMenuLoaderParsingError(error_message);
        }
    }
}

