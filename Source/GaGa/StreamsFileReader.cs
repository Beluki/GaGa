
// GaGa.
// A minimal radio player for the Windows Tray.


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
        /// An INIReader that reads lines from a streams file
        /// adding sections and key=value pairs to a context menu
        /// as submenus and clickable items.
        /// </summary>
        public StreamsFileReader()
        {
            file = null;
            menu = null;
            onClick = null;

            currentMenuItems = null;
            seenSubmenues = new Dictionary<String, MenuItem>();

            currentLineNumber = 0;
            currentLine = String.Empty;
        }

        /// <summary>
        /// Clear internal state.
        /// </summary>
        private void ResetState()
        {
            file = null;
            menu = null;
            onClick = null;

            currentMenuItems = null;
            seenSubmenues.Clear();

            currentLineNumber = 0;
            currentLine = String.Empty;
        }

        /// <summary>
        /// Concise helper to throw StreamsFileReadError exceptions.
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
            ThrowReadError("Empty submenu name.");
        }

        /// <summary>
        /// Do not accept streams with no name.
        /// </summary>
        protected override void OnKeyEmpty(String value)
        {
            ThrowReadError("Empty stream name.");
        }

        /// <summary>
        /// Do not accept streams with no url.
        /// </summary>
        protected override void OnValueEmpty(String key)
        {
            ThrowReadError("Empty stream url.");
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
            // otherwise it's a duplicate and has already been added:
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
        /// The uri is stored in the item .Tag property.
        /// </summary>
        protected override void OnKeyValue(String key, String value)
        {
            MenuItem item = new MenuItem(key, onClick);

            try
            {
                item.Tag = new Uri(value);
            }
            catch (UriFormatException exception)
            {
                ThrowReadError(exception.Message);
            }

            currentMenuItems.Add(item);
        }

        /// <summary>
        /// Read a streams file adding submenus items to a context menu.
        /// </summary>
        /// <param name="file">Streams file to read lines from.</param>
        /// <param name="menu">Target context menu.</param>
        /// <param name="onClick">Click event to attach to menu items.</param>
        public void Read(StreamsFile file, ContextMenu menu, EventHandler onClick)
        {
            this.file = file;
            this.menu = menu;
            this.onClick = onClick;

            // start at root:
            currentMenuItems = menu.MenuItems;

            try
            {
                foreach (String line in file.ReadLines())
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

