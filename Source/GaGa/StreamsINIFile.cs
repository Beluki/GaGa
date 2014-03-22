
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace GaGa
{
    class StreamsINIFile
    {
        private String filepath;
        private String resource;
        private DateTime last_modified;

        public StreamsINIFile(String filepath, String resource)
        {
            this.filepath = filepath;
            this.resource = resource;
        }

        /// <summary>
        /// Check that the streams filepath exists
        /// or create it from the embedded resource otherwise.
        /// </summary>
        public void EnsureExists()
        {
            String path = Path.Combine(Application.StartupPath, filepath);

            if (!File.Exists(path))
                Utils.CopyResource(resource, path);
        }

        /// <summary>
        /// Check if the streams file has been updated.
        /// </summary>
        public Boolean IsOutdated()
        {
            DateTime modified = File.GetLastWriteTime(filepath);

            if (last_modified == modified)
                return false;
            
            return true;
        }

        /// <summary>
        /// Parse the streams file and add all its items to
        /// the given context menu.
        /// </summary>
        public void AddToContextMenu(ContextMenuStrip menu)
        {
            ToolStripItemCollection root = menu.Items;

            foreach (String line in Utils.ReadLines(filepath))
            {
                String text = line.Trim();

                // empty line, back to the menu root:
                if (text == String.Empty)
                    root = menu.Items;

                // comment, skip:
                else if (text.StartsWith("#") || text.StartsWith(";"))
                    continue;

                // submenu, add to current and change root:
                else if (text.StartsWith("[") && text.EndsWith("]"))
                {
                    ToolStripMenuItem submenu = new ToolStripMenuItem();
                    submenu.Text = text.Substring(1, text.Length - 2);
                    root.Add(submenu);
                    root = submenu.DropDownItems;
                }

                // stream link, add to current root:
                else if (text.Contains("="))
                {
                    int separator = text.IndexOf('=');

                    String key = text.Substring(0, separator - 1).Trim();
                    String value = text.Substring(separator + 1).Trim();

                    ToolStripMenuItem radio = new ToolStripMenuItem();
                    radio.Text = key;
                    radio.Tag = value;
                    radio.ToolTipText = value;
                    root.Add(radio);
                }
            }
        }
    }
}
