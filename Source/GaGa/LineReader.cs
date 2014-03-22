
// GaGa.

using System;
using System.IO;


namespace GaGa
{
    /// <summary>
    /// A simple stream reader that keeps track of the current line number.
    /// </summary>
    internal class LineReader
    {
        private StreamReader reader;
        private int current_line;

        public LineReader(Stream stream)
        {
            reader = new StreamReader(stream);
            current_line = 0;
        }

        /// <summary>
        /// Current line number.
        /// </summary>
        public int LineNumber()
        {
            return current_line;
        }

        /// <summary>
        /// Read a line from the stream.
        /// </summary>
        /// <returns>
        /// Line text as a string.
        /// </returns>
        public String ReadLine()
        {
            String line = reader.ReadLine();
            current_line++;
            return line;
        }
    }
}

