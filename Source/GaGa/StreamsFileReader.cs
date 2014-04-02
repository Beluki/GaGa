
// GaGa.
// A simple radio player running on the Windows notification area.


using System;
using System.Collections.Generic;
using System.Windows.Forms;

using mINI;


namespace GaGa
{
    internal class StreamsFileReader : INIReader
    {
        private StreamsFile file;
        private ContextMenuStrip menu;

        private ToolStripItemCollection currentSubmenu;
        private Dictionary<String, ToolStripMenuItem> seenSubmenues;

        private Int32 currentLineNumber;
        private String currentLine;

        /// <summary>
        /// An INI reader that adds submenus and items to a ContextMenuStrip
        /// from a given StreamsFile.
        /// </summary>
        /// <param name="file">StreamsFile to read.</param>
        /// <param name="menu">Context menu to add items to.</param>
        public StreamsFileReader(StreamsFile file, ContextMenuStrip menu)
        {
            this.file = file;
            this.menu = menu;

            this.currentSubmenu = menu.Items;
            this.seenSubmenues = new Dictionary<String, ToolStripMenuItem>();

            this.currentLineNumber = 0;
            this.currentLine = String.Empty;
        }

        /// <summary>
        /// Concise helper to throw StreamsFileReaderError
        /// exceptions during reading.
        /// </summary>
        /// <param name="message">Error message.</param>
        private void ThrowReadingError(String message)
        {
            throw new StreamsFileReaderError(
                message,
                file.FilePath,
                currentLine,
                currentLineNumber
            );
        }

        /// <summary>
        /// Do not accept menus (sections) with no name.
        /// </summary>
        protected override void OnSectionEmpty()
        {
            ThrowReadingError("Empty menu name.");
        }

        /// <summary>
        /// Do not accept submenus (subsections) with no name.
        /// </summary>
        protected override void OnSubSectionEmpty(String path)
        {
            ThrowReadingError("Empty submenu name.");
        }

        /// <summary>
        /// Do not accept streams (keys) with no name.
        /// </summary>
        protected override void OnKeyEmpty(String value)
        {
            ThrowReadingError("Empty stream name.");
        }

        /// <summary>
        /// Syntax errors.
        /// </summary>
        protected override void OnUnknown(String line)
        {
            ThrowReadingError("Invalid syntax.");
        }

        /// <summary>
        /// On an empty line, go back to the menu root.
        /// </summary>
        protected override void OnEmpty()
        {
            currentSubmenu = menu.Items;
        }

        /// <summary>
        /// On a section, go back to the menu root.
        /// </summary>
        protected override void OnSection(String section)
        {
            currentSubmenu = menu.Items;
        }

        /// <summary>
        /// Add subsections as submenus and descent into them.
        /// </summary>
        protected override void OnSubSection(String subsection, String path)
        {
            ToolStripMenuItem submenu = seenSubmenues.GetOrSet(
                path, () => new ToolStripMenuItem(subsection)
            );

            currentSubmenu.Add(submenu);
            currentSubmenu = submenu.DropDownItems;
        }

        /// <summary>
        /// Add key=value pairs as clickable menu items.
        /// The value is stored in the item tag.
        /// </summary>
        protected override void OnKeyValue(String key, String value)
        {
            ToolStripItem item = new ToolStripMenuItem(key);
            item.Tag = value;
            item.ToolTipText = value;

            currentSubmenu.Add(item);
        }

        /// <summary>
        /// Read all lines in our StreamsFile, adding menus
        /// and items to the ContextMenuStrip.
        /// </summary>
        public void Read()
        {
            foreach (String line in file.ReadLineByLine())
            {
                currentLineNumber++;
                currentLine = line;
                ReadLine(line);
            }
        }
    }
}

