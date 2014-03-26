
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
        /// and add all the sections and items to a ContextMenuStrip
        /// as submenues and menuitems.
        /// </summary>
        /// <param name="file">Streams file to read from.</param>
        public StreamsMenuLoader(StreamsFile file)
        {
            this.file = file;
            this.lastUpdated = DateTime.MinValue;
        }

        /// <summary>
        /// Determines whether the streams file has changed since last parsed.
        /// </summary>
        public Boolean HasChanged()
        {
            return lastUpdated != file.GetLastWriteTime();
        }

        /// Parser, as a set of methods that act upon
        /// a parsing state that is kept internal:

        private class ParsingState
        {
            public ContextMenuStrip Menu;
            public ToolStripItemCollection CurrentSubmenu;
            public Dictionary<String, ToolStripMenuItem> SeenSubmenues;
            public int CurrentLineNumber;
            public String CurrentLine;

            /// <summary>
            /// Initialize a new parser state.
            /// </summary>
            /// <param name="menu">Target menu to add items to.</param>
            public ParsingState(ContextMenuStrip menu)
            {
                this.Menu = menu;
                this.CurrentSubmenu = menu.Items;
                this.SeenSubmenues = new Dictionary<String, ToolStripMenuItem>();
                this.CurrentLineNumber = 0;
                this.CurrentLine = String.Empty;
            }
        }

        /// <summary>
        /// Concise helper to throw StreamsMenuLoaderParsingError
        /// exceptions during parsing for our StreamsFile.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="state">Current parser state.</param>
        private void ThrowParsingError(String message, ParsingState state)
        {
            throw new StreamsMenuLoaderParsingError(
                message,
                file.FilePath,
                state.CurrentLine,
                state.CurrentLineNumber
            );
        }

        /// <summary>
        /// Try to parse an empty line.
        /// </summary>
        /// <param name="state">Current parser state.</param>
        private Boolean ParseEmpty(ParsingState state)
        {
            if (!state.CurrentLine.IsEmpty())
                return false;

            state.CurrentSubmenu = state.Menu.Items;
            return true;
        }

        /// <summary>
        /// Try to parse a comment.
        /// </summary>
        /// <param name="state">Current parser state.</param>
        private Boolean ParseComment(ParsingState state)
        {
            return state.CurrentLine.StartsWithAny("#", ";");
        }

        /// <summary>
        /// Try to parse a (possibly nested) submenu.
        /// </summary>
        /// <param name="state">Current parser state.</param>
        private Boolean ParseSubmenu(ParsingState state)
        {
            String line = state.CurrentLine;
            if (!line.IsSurroundedBy("[", "]"))
                return false;

            // split into submenu names and trim each name:
            String[] names = line.Substring(1, line.Length - 2)
                                 .Split('/')
                                 .Select(name => name.Trim())
                                 .ToArray();

            // go back to the root to avoid duplicates:
            state.CurrentSubmenu = state.Menu.Items;

            // create all the nested submenues:
            names.EachWithIndex((name, index) =>
            {
                // do not accept empty menu names:
                if (name.IsEmpty())
                    ThrowParsingError("Empty menu name", state);

                String fullname = String.Join("/", names, 0, index + 1);

                // lookup or add new:
                ToolStripMenuItem submenu = state.SeenSubmenues.GetOrSet(
                    fullname, () => new ToolStripMenuItem(name)
                );

                // add to the current root and descend into it:
                state.CurrentSubmenu.Add(submenu);
                state.CurrentSubmenu = submenu.DropDownItems;
            });

            return true;
        }

        /// <summary>
        /// Try to parse a menu item.
        /// </summary>
        /// <param name="state">Current parser state.</param>
        private Boolean ParseMenuItem(ParsingState state)
        {
            if (!state.CurrentLine.Contains("="))
                return false;

            String name, uri;
            state.CurrentLine.SplitTo(out name, out uri, '=');

            // do not accept empty names:
            if (name.IsEmpty())
                ThrowParsingError("Empty stream name", state);

            ToolStripItem item = new ToolStripMenuItem(name);
            item.Tag = uri;
            item.ToolTipText = uri;

            // empty uri is ok, disable item:
            if (uri.IsEmpty())
                item.Enabled = false;

            state.CurrentSubmenu.Add(item);
            return true;
        }

        /// <summary>
        /// Try to parse a line.
        /// </summary>
        /// <param name="line">Line to parse.</param>
        /// <param name="state">Current parser state.</param>
        private Boolean ParseLine(String line, ParsingState state)
        {
            state.CurrentLineNumber++;
            state.CurrentLine = line.Trim();

            return ParseEmpty(state)
                || ParseComment(state)
                || ParseSubmenu(state)
                || ParseMenuItem(state);
        }

        /// <summary>
        /// Parse the streams file again, adding menues and items
        /// to the given ContextMenuStrip.
        /// </summary>
        /// <param name="menu">Target ContextMenuStrip.</param>
        public void LoadTo(ContextMenuStrip menu)
        {
            ParsingState state = new ParsingState(menu);

            foreach (String line in file.ReadLineByLine())
                if (!ParseLine(line, state))
                    ThrowParsingError("Invalid syntax", state);

            lastUpdated = file.GetLastWriteTime();
        }
    }
}

