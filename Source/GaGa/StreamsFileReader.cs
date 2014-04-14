
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
        private ContextMenu menu;
        private Menu.MenuItemCollection currentMenuItems;
        private Dictionary<String, MenuItem> seenSubmenues;

        private Int32 currentLineNumber;
        private String currentLine;

        /// <summary>
        /// An INI reader that adds sections and key=value pairs to
        /// a ContextMenu as submenus and clickable items.
        /// </summary>
        /// <param name="menu">Target context menu.</param>
        public StreamsFileReader(ContextMenu menu)
        {
            this.menu = menu;
            this.currentMenuItems = menu.MenuItems;
            this.seenSubmenues = new Dictionary<String, MenuItem>();

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
        /// The value is stored in the item .Tag property.
        /// </summary>
        protected override void OnKeyValue(String key, String value)
        {
            MenuItem item = new MenuItem(key);
            item.Tag = value;

            currentMenuItems.Add(item);
        }

        /// <summary>
        /// Read lines adding submenus and items to the context menu.
        /// </summary>
        public void ReadLines(IEnumerable<String> lines)
        {
            foreach (String line in lines)
            {
                currentLineNumber++;
                currentLine = line;
                ReadLine(line);
            }
        }
    }
}

