
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
        private int line;

        public LineReader(Stream stream)
        {
            reader = new StreamReader(stream);
            line = 0;
        }

        /// <summary>
        /// Current line number.
        /// </summary>
        public int LineNumber()
        {
            return line;
        }

        /// <summary>
        /// Read a line from the stream.
        /// </summary>
        /// <returns>
        /// Line text as a string.
        /// </returns>
        public String ReadLine()
        {
            line++;
            return reader.ReadLine();
        }
    }
}

