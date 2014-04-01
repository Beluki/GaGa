
using System;
using System.Collections.Generic;
using System.Windows.Forms;

using mINI;

namespace GaGa
{
    internal class StreamsFileReader : INIReader
    {
        private StreamsFile File;

        private ContextMenuStrip Menu;
        private ToolStripItemCollection CurrentSubmenu;
        private Dictionary<String, ToolStripMenuItem> SeenSubmenues;

        private Int32 CurrentLineNumber;
        private String CurrentLine;

        /// <summary>
        /// An INI reader that adds sections and items to a ContextMenuStrip
        /// from a given StreamsFile.
        /// </summary>
        /// <param name="file">StreamsFile to read.</param>
        /// <param name="menu">Context menu to the add items to.</param>
        public StreamsFileReader(StreamsFile file, ContextMenuStrip menu)
        {
            this.File = file;

            this.Menu = menu;
            this.CurrentSubmenu = menu.Items;
            this.SeenSubmenues = new Dictionary<String, ToolStripMenuItem>();

            this.CurrentLineNumber = 0;
            this.CurrentLine = String.Empty;
        }

        /// <summary>
        /// Concise helper to throw StreamsFileError exceptions during reading.
        /// </summary>
        /// <param name="message">Error message.</param>
        private void ThrowReadingError(String message)
        {
            throw new StreamsFileError(message, File.FilePath, CurrentLine, CurrentLineNumber);
        }

        protected override void OnSectionEmpty()
        {
            ThrowReadingError("Empty menu name");
        }

        protected override void OnSubSectionEmpty(String path)
        {
            ThrowReadingError("Empty submenu name");
        }

        protected override void OnKeyEmpty(String value)
        {
            ThrowReadingError("Empty stream name");
        }

        /// <summary>
        /// On an empty line, go back to the menu root.
        /// </summary>
        protected override void OnEmpty()
        {
            CurrentSubmenu = Menu.Items;
        }

        /// <summary>
        /// On a section, go back to the menu root.
        /// </summary>
        protected override void OnSection(String section)
        {
            CurrentSubmenu = Menu.Items;
        }

        /// <summary>
        /// On subsections, add them as submenues and set as root.
        /// </summary>
        protected override void OnSubSection(String subsection, String path)
        {
            ToolStripMenuItem submenu = SeenSubmenues.GetOrSet(
               path, () => new ToolStripMenuItem(subsection)
            );

            CurrentSubmenu.Add(submenu);
            CurrentSubmenu = submenu.DropDownItems;
        }

        /// <summary>
        /// On key=value pairs, add them as clickable menu items
        /// to the current root. The value is stored in the item tag.
        /// </summary>
        protected override void OnKeyValue(String key, String value)
        {
            ToolStripItem item = new ToolStripMenuItem(key);
            item.Tag = value;
            item.ToolTipText = value;

            CurrentSubmenu.Add(item);
        }

        /// <summary>
        /// Read all lines in our StreamsFile, adding the
        /// sections and items to the menu.
        /// </summary>
        public void Read()
        {
            foreach (String line in File.ReadLineByLine())
            {
                CurrentLineNumber++;
                CurrentLine = line;
                ReadLine(line);
            }
        }
    }
}

