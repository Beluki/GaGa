
// GaGa.
// A minimal radio player for the Windows Tray.


using System;


namespace GaGa
{
    internal class StreamsFileReadError : Exception
    {
        /// <summary>
        /// Streams file that triggered the error.
        /// </summary>
        public readonly StreamsFile File;

        /// <summary>
        /// Line text for the incorrect line.
        /// </summary>
        public readonly String Line;

        /// <summary>
        /// Line where the error happened.
        /// </summary>
        public readonly Int32 LineNumber;

        /// <summary>
        /// Raised by StreamsFileReader on a reading error.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="file">Streams file that triggered the error.</param>
        /// <param name="line">Line text for the incorrect line.</param>
        /// <param name="linenumber">Line where the error happened.</param>
        public StreamsFileReadError(String message, StreamsFile file, String line, Int32 linenumber) : base(message)
        {
            File = file;
            Line = line;
            LineNumber = linenumber;
        }
    }
}

