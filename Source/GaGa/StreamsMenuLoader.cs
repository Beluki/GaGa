
// GaGa.
// A single icon radio player on the Windows notification area.


using System;
using System.Windows.Forms;


namespace GaGa
{   
    /// <summary>
    /// Maintains the dynamic part of the context menu
    /// autoloading it from an INI file when it changes
    /// and using a resource file as a fallback.
    /// </summary>
    class StreamsMenuLoader
    {
        private String filepath;
        private String resource;

        public readonly ContextMenuStrip menu;

        public StreamsMenuLoader(String filepath, String resource)
        {
            this.filepath = filepath;
            this.resource = resource;

            this.menu = new ContextMenuStrip();
        }

        private void ParseError(String message, int linenumber)
        {
            throw new StreamsMenuParsingError(
                String.Format(
                    "{0} error at line: {1} : {2}",
                    filepath, linenumber.ToString(), message
                )
            );
        }

        /// <summary>
        /// Parse the streams file
        /// adding the items to the context menu.
        /// </summary>
        public void ParseStreamsFile()
        {
            ToolStripItemCollection root = menu.Items;
            int linenumber = 0;

            foreach (String line in Utils.ReadLines(filepath))
            {
                linenumber++;
                String text = line.Trim();

                // empty line, back to the menu root:
                if (String.IsNullOrEmpty(text))
                {
                    root = menu.Items;
                }

                // comment, skip:
                else if (text.StartsWith("#") || text.StartsWith(";"))
                {
                }

                // submenu, create it and change root:
                else if (text.StartsWith("[") && text.EndsWith("]"))
                {
                    String name = line.Substring(1, line.Length - 2).Trim();

                    // do not accept empty submenu names:
                    if (String.IsNullOrEmpty(name))
                        ParseError("Empty menu name.", linenumber);

                    ToolStripMenuItem submenu = new ToolStripMenuItem();
                    submenu.Text = name;

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
                        ParseError("Empty stream name.", linenumber);

                    // empty uri is ok, skipped, user can edit later:
                    if (String.IsNullOrEmpty(uri))
                        continue;

                    ToolStripMenuItem radio = new ToolStripMenuItem();
                    radio.Text = name;
                    radio.Tag = uri;
                    radio.ToolTipText = uri;

                    root.Add(radio);
                }

                // unknown:
                else
                {
                    ParseError("Invalid syntax.", linenumber);
                }
            }
        }
    }
}

