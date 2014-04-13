
// GaGa.
// A simple radio player running on the Windows notification area.


using System;


namespace GaGa
{
    internal class StreamsFileReadError : Exception
    {
        public readonly String Line;
        public readonly Int32 LineNumber;

        /// <summary>
        /// Raised by StreamsFileReader on a reading error.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="line">Line text for the incorrect line.</param>
        /// <param name="linenumber">Line where the error happened.</param>
        public StreamsFileReadError
            (String message, String line, Int32 linenumber)
            : base(message)
        {
            this.Line = line;
            this.LineNumber = linenumber;
        }
    }
}

