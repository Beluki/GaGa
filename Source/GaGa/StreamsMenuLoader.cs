
// GaGa.
// A simple radio player running on the Windows notification area.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;


namespace GaGa
{
    internal class StreamsMenuLoader
    {
        private StreamsFile file;
        private DateTime lastUpdated;

        /// <summary>
        /// Can parse a StreamsFile as an INI, monitor changes,
        /// and add all the sections and items to a ContextMenuStrip.
        /// </summary>
        /// <param name="file">Streams file to read from.</param>
        public StreamsMenuLoader(StreamsFile file)
        {
            this.file = file;
            this.lastUpdated = DateTime.MinValue;
        }

        /// <summary>
        /// Returns true or false depending on whether the
        /// streams file has changed since last parsed.
        /// </summary>
        public Boolean HasChanged()
        {
            return lastUpdated != file.GetLastWriteTime();
        }

        /// <summary>
        /// Internal helper to throw StreamsMenuLoaderParsingError
        /// exceptions during parsing.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="line">Line text for the incorrect line.</param>
        /// <param name="linenumber">Line number where the error happened.</param>
        private void ThrowParsingError(String message, String line, int linenumber)
        {
            throw new StreamsMenuLoaderParsingError(message, file.FilePath, line, linenumber);
        }

        /// <summary>
        /// Parse the streams file again, adding menues and items
        /// to the given ContextMenuStrip.
        /// </summary>
        /// <param name="menu">Target ContextMenuStrip.</param>
        public void LoadTo(ContextMenuStrip menu)
        {
            // current root, submenues seen so far:
            ToolStripItemCollection root = menu.Items;
            Dictionary<String, ToolStripMenuItem> seen_submenues = new Dictionary<String, ToolStripMenuItem>();

            int linenumber = 0;
            foreach (String line in file.ReadLineByLine())
            {
                linenumber++;
                String text = line.Trim();

                // empty line, back to the menu root:
                if (text == String.Empty)
                    root = menu.Items;

                // comment, skip line:
                else if (text.StartsWith("#") || text.StartsWith(";"))
                    continue;

                // menu, possibly nested:
                else if (text.StartsWith("[") && text.EndsWith("]"))
                {
                    // split into submenues and trim:
                    String[] submenu_names = text.Substring(1, text.Length - 2)
                                                 .Split('/')
                                                 .Select(name => name.Trim())
                                                 .ToArray();

                    // iterate, creating each submenu, from the index:
                    root = menu.Items;
                    submenu_names.EachIndex((name, index) =>
                    {
                        // do not accept empty menu names:
                        if (name == String.Empty)
                            ThrowParsingError("Empty menu name", line, linenumber);

                        String fullname = String.Join("/", submenu_names, 0, index + 1);

                        // add and change root:
                        ToolStripMenuItem submenu;
                        submenu = seen_submenues.GetOrSet(fullname,
                            () => new ToolStripMenuItem(name)
                        );

                        root.Add(submenu);
                        root = submenu.DropDownItems;
                    });                      
                }

                // stream link, add to the current root:
                else if (text.Contains("="))
                {
                    String[] pair = text.Split(new char[] { '=' }, 2);
                    String name = pair[0].Trim();
                    String uri = pair[1].Trim();

                    // do not accept empty names:
                    if (name == String.Empty)
                        ThrowParsingError("Empty stream name", line, linenumber);

                    // empty uri is ok, skipped, user can edit later:
                    if (uri == String.Empty)
                        continue;

                    ToolStripItem radio = new ToolStripMenuItem(name);
                    radio.Tag = uri;
                    radio.ToolTipText = uri;
                    root.Add(radio);
                }

                // unknown:
                else ThrowParsingError("Invalid syntax.", line, linenumber);
            }

            lastUpdated = file.GetLastWriteTime();
        }
    }
}

