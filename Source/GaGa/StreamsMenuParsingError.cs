
// GaGa.
// A single icon radio player on the Windows notification area.


using System;


namespace GaGa
{
    /// <summary>
    /// Raised on parsing errors.
    /// </summary>
    internal class StreamsMenuParsingError : Exception
    {
        public StreamsMenuParsingError()
        {
        }

        public StreamsMenuParsingError(String message) : base(message)
        {
        }
    }
}

