
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

using mINI;


namespace GaGa.Streams
{
    internal class StreamsFileReader : INIReader
    {
        private String filepath;
        private ContextMenuStrip menu;
        private EventHandler onClick;

        private List<ToolStripMenuItem> currentMenuItems;
        private ToolStripItemCollection currentMenuItemCollection;
        private Dictionary<String, ToolStripMenuItem> seenSubmenues;

        private Int32 currentLineNumber;
        private String currentLine;

        /// <summary>
        /// An INIReader that reads lines from a streams file
        /// adding sections and key=value pairs to a context menu
        /// as submenus and clickable items.
        /// </summary>
        public StreamsFileReader()
        {
            filepath = null;
            menu = null;
            onClick = null;

            currentMenuItems = new List<ToolStripMenuItem>();
            currentMenuItemCollection = null;
            seenSubmenues = new Dictionary<String, ToolStripMenuItem>();

            currentLineNumber = 0;
            currentLine = String.Empty;
        }

        /// <summary>
        /// Clear internal state.
        /// </summary>
        private void ResetState()
        {
            filepath = null;
            menu = null;
            onClick = null;

            currentMenuItems.Clear();
            currentMenuItemCollection = null;
            seenSubmenues.Clear();

            currentLineNumber = 0;
            currentLine = String.Empty;
        }

        /// <summary>
        /// Add the collected items to the current menu.
        /// </summary>
        private void AddCurrentMenuItems()
        {
            currentMenuItemCollection.AddRange(currentMenuItems.ToArray());
            currentMenuItems.Clear();
        }

        /// <summary>
        /// Concise helper to create StreamsFileReadError exceptions.
        /// </summary>
        /// <param name="message">Error message.</param>
        private StreamsFileReadError ReadError(String message)
        {
            return new StreamsFileReadError(
                message,
                filepath,
                currentLine,
                currentLineNumber
            );
        }

        /// <summary>
        /// Do not accept menus (sections) with no name.
        /// </summary>
        protected override void OnSectionEmpty()
        {
            throw ReadError("Empty menu name.");
        }

        /// <summary>
        /// Do not accept submenus (subsections) with no name.
        /// </summary>
        protected override void OnSubSectionEmpty(String path)
        {
            throw ReadError("Empty submenu name.");
        }

        /// <summary>
        /// Do not accept streams with no name.
        /// </summary>
        protected override void OnKeyEmpty(String value)
        {
            throw ReadError("Empty stream name.");
        }

        /// <summary>
        /// Do not accept streams with no URI.
        /// </summary>
        protected override void OnValueEmpty(String key)
        {
            throw ReadError("Empty stream URI.");
        }

        /// <summary>
        /// Syntax errors.
        /// </summary>
        protected override void OnUnknown(String line)
        {
            throw ReadError("Invalid syntax.");
        }

        /// <summary>
        /// On an empty line, add collected items
        /// and go back to the menu root.
        /// </summary>
        protected override void OnEmpty()
        {
            AddCurrentMenuItems();
            currentMenuItemCollection = menu.Items;
        }

        /// <summary>
        /// On a new section, add collected items
        /// and go back to the menu root.
        /// </summary>
        protected override void OnSection(String section)
        {
            AddCurrentMenuItems();
            currentMenuItemCollection = menu.Items;
        }

        /// <summary>
        /// On subsections, add collected item
        /// and descend into them.
        /// </summary>
        protected override void OnSubSection(String subsection, String path)
        {
            ToolStripMenuItem submenu;
            seenSubmenues.TryGetValue(path, out submenu);

            // not seen, create and add as a submenu to the current menu
            // otherwise it's a duplicate and has already been added:
            if (submenu == null)
            {
                submenu = new ToolStripMenuItem(subsection);
                seenSubmenues.Add(path, submenu);
                currentMenuItems.Add(submenu);
            }

            AddCurrentMenuItems();
            currentMenuItemCollection = submenu.DropDownItems;
        }

        /// <summary>
        /// Add key=value pairs as clickable menu items.
        /// The URI is stored in the item .Tag property.
        /// </summary>
        protected override void OnKeyValue(String key, String value)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(key, null, onClick);

            try
            {
                item.Tag = new Uri(value);
            }
            catch (UriFormatException exception)
            {
                throw ReadError(exception.Message);
            }

            currentMenuItems.Add(item);
        }

        /// <summary>
        /// Read a streams file adding submenus and items to a context menu.
        /// </summary>
        /// <param name="filepath">Path to the streams file to read lines from.</param>
        /// <param name="menu">Target context menu.</param>
        /// <param name="onClick">Click event to attach to menu items.</param>
        public void Read(String filepath, ContextMenuStrip menu, EventHandler onClick)
        {
            this.filepath = filepath;
            this.menu = menu;
            this.onClick = onClick;

            try
            {
                // start at the menu root:
                currentMenuItemCollection = menu.Items;

                foreach (String line in File.ReadLines(filepath))
                {
                    currentLineNumber++;
                    currentLine = line;
                    ReadLine(line);
                }

                // add pending items for the last submenu:
                AddCurrentMenuItems();
            }
            finally
            {
                ResetState();
            }
        }
    }
}

