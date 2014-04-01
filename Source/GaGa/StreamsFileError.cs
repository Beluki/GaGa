
// GaGa.
// A simple radio player running on the Windows notification area.


using System;


namespace GaGa
{
    internal class StreamsFileError : Exception
    {
        public readonly String FilePath;
        public readonly String Line;
        public readonly int LineNumber;

        /// <summary>
        /// Raised by StreamsFileReader on a reading error.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="filepath">File path for the incorrect file.</param>
        /// <param name="line">Line text for the incorrect line.</param>
        /// <param name="linenumber">Line number where the error happened.</param>
        public StreamsFileError(String message, String filepath, String line, int linenumber)
            : base(message)
        {
            this.FilePath = filepath;
            this.Line = line;
            this.LineNumber = linenumber;
        }
    }
}

