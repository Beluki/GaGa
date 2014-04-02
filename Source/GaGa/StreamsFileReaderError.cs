
// GaGa.
// A simple radio player running on the Windows notification area.


using System;


namespace GaGa
{
    internal class StreamsFileReaderError : Exception
    {
        public readonly String FilePath;
        public readonly String Line;
        public readonly Int32 LineNumber;

        /// <summary>
        /// Raised by StreamsFileReader on a reading error.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="filepath">File path for the incorrect file.</param>
        /// <param name="line">Line text for the incorrect line.</param>
        /// <param name="linenumber">Line where the error happened.</param>
        public StreamsFileReaderError
            (String message, String filepath, String line, Int32 linenumber)
            : base(message)
        {
            this.FilePath = filepath;
            this.Line = line;
            this.LineNumber = linenumber;
        }
    }
}

