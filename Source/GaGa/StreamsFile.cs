
// GaGa.
// A simple radio player running on the Windows notification area.


using System;
using System.Collections.Generic;
using System.IO;


namespace GaGa
{
    internal class StreamsFile
    {
        public readonly String filepath;
        public readonly String resource;

        /// <summary>
        /// Maintains an UTF-8 encoded text file that is recreated
        /// from an embedded resource upon reading when it doesn't exist.
        /// </summary>
        /// <param name="filepath">File path for the target file.</param>
        /// <param name="resource">Resource path to recreate it.</param>
        public StreamsFile(String filepath, String resource)
        {
            this.filepath = filepath;
            this.resource = resource;
        }

        /// <summary>
        /// Returns the date and time this file was last written to.
        /// </summary>
        public DateTime GetLastWriteTime()
        {
            return File.GetLastWriteTime(filepath);
        }

        /// <summary>
        /// Iterate all lines from the file.
        /// </summary>
        public IEnumerable<String> ReadLineByLine()
        {
            if (!File.Exists(filepath))
                Utils.CopyResource(resource, filepath);

            return Utils.ReadLineByLine(filepath);
        }
    }
}

