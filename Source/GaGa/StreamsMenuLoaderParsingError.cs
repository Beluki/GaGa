
// GaGa.
// A simple radio player running on the Windows notification area.


using System;


namespace GaGa
{
    internal class StreamsMenuLoaderParsingError : Exception
    {
        public readonly String Message;
        public readonly String Path;
        public readonly String Line;
        public readonly int LineNumber;

        /// <summary>
        /// Raised by the StreamsMenuLoader on a parsing error.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="path">File path for the incorrect file.</param>
        /// <param name="line">Line text for the incorrect line.</param>
        /// <param name="linenumber">Line number where the error happened.</param>
        public StreamsMenuLoaderParsingError(String message, String path, String line, int linenumber)
        {
            this.Message = message;
            this.Path = path;
            this.Line = line;
            this.LineNumber = linenumber;
        }
    }
}

