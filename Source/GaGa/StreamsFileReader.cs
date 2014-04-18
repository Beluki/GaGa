
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
        private ContextMenu menu;
        private EventHandler onClick;

        private Menu.MenuItemCollection currentMenuItems;
        private Dictionary<String, MenuItem> seenSubmenues;

        private Int32 currentLineNumber;
        private String currentLine;

        /// <summary>
        /// An INI reader that reads lines from a streams file
        /// adding sections and key=value pairs to a ContextMenu
        /// as submenus and clickable items.
        /// </summary>
        public StreamsFileReader()
        {
            this.file = null;
            this.menu = null;
            this.onClick = null;

            this.currentMenuItems = null;
            this.seenSubmenues = new Dictionary<String, MenuItem>();

            this.currentLineNumber = 0;
            this.currentLine = String.Empty;
        }

        /// <summary>
        /// Clear internal state.
        /// </summary>
        private void ResetState()
        {
            this.file = null;
            this.menu = null;
            this.onClick = null;

            this.currentMenuItems = null;
            this.seenSubmenues.Clear();

            this.currentLineNumber = 0;
            this.currentLine = String.Empty;
        }

        /// <summary>
        /// Concise helper to throw StreamsFileReadError
        /// exceptions during reading.
        /// </summary>
        /// <param name="message">Error message.</param>
        private void ThrowReadError(String message)
        {
            throw new StreamsFileReadError(
                message,
                file,
                currentLine,
                currentLineNumber
            );
        }

        /// <summary>
        /// Do not accept menus (sections) with no name.
        /// </summary>
        protected override void OnSectionEmpty()
        {
            ThrowReadError("Empty menu name.");
        }

        /// <summary>
        /// Do not accept submenus (subsections) with no name.
        /// </summary>
        protected override void OnSubSectionEmpty(String path)
        {
            ThrowReadError("Empty submenu name, at path: " + path);
        }

        /// <summary>
        /// Do not accept streams (keys) with no name.
        /// </summary>
        protected override void OnKeyEmpty(String value)
        {
            ThrowReadError("Empty stream name.");
        }

        /// <summary>
        /// Syntax errors.
        /// </summary>
        protected override void OnUnknown(String line)
        {
            ThrowReadError("Invalid syntax.");
        }

        /// <summary>
        /// On an empty line, go back to the menu root.
        /// </summary>
        protected override void OnEmpty()
        {
            currentMenuItems = menu.MenuItems;
        }

        /// <summary>
        /// On a new section, go back to the menu root.
        /// </summary>
        protected override void OnSection(String section)
        {
            currentMenuItems = menu.MenuItems;
        }

        /// <summary>
        /// Add subsections as submenus and descend into them.
        /// </summary>
        protected override void OnSubSection(String subsection, String path)
        {
            MenuItem submenu;
            seenSubmenues.TryGetValue(path, out submenu);

            // not seen, create and add to the current menu
            // otherwise it's a duplicate and has already been added
            if (submenu == null)
            {
                submenu = new MenuItem(subsection);
                seenSubmenues.Add(path, submenu);
                currentMenuItems.Add(submenu);
            }

            currentMenuItems = submenu.MenuItems;
        }

        /// <summary>
        /// Add key=value pairs as clickable menu items.
        /// The radio stream is stored in the item .Tag property.
        /// </summary>
        protected override void OnKeyValue(String key, String value)
        {
            MenuItem item = new MenuItem(key);

            item.Click += onClick;
            item.Tag = new RadioStream(key, value);

            currentMenuItems.Add(item);
        }

        /// <summary>
        /// Read lines from a streams file adding submenus and items
        /// to a context menu.
        /// <param name="file">Streams file to read lines from.</param>
        /// <param name="menu">Target context menu.</param>
        /// <param name="onClick">Click event to attach to menu items.</param>
        /// </summary>
        public void Read(StreamsFile file, ContextMenu menu, EventHandler onClick)
        {
            this.file = file;
            this.menu = menu;
            this.onClick = onClick;

            // start at root:
            currentMenuItems = menu.MenuItems;

            try
            {
                foreach (String line in file.ReadLineByLine())
                {
                    currentLineNumber++;
                    currentLine = line;
                    ReadLine(line);
                }
            }
            finally
            {
                ResetState();
            }
        }
    }
}

