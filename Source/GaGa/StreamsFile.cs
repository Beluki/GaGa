
// GaGa.
// A simple radio player running on the Windows notification area.


using System;
using System.Collections.Generic;
using System.IO;


namespace GaGa
{
    internal class StreamsFile
    {
        public readonly String FilePath;
        public readonly String Resource;

        /// <summary>
        /// Maintains an UTF8-encoded text file that can be recreated
        /// from an embedded resource when it doesn't exist.
        /// </summary>
        /// <param name="filepath">
        /// File path for the target file.
        /// </param>
        /// <param name="resource">
        /// Path to the resource used to recreate it.
        /// </param>
        public StreamsFile(String filepath, String resource)
        {
            this.FilePath = filepath;
            this.Resource = resource;
        }

        /// <summary>
        /// Returns the date and time this file was last written to.
        /// </summary>
        public DateTime GetLastWriteTime()
        {
            return File.GetLastWriteTime(FilePath);
        }

        /// <summary>
        /// Recreate the file from the embedded resouce unless it exists.
        /// </summary>
        public void CreateUnlessExists()
        {
            if (!File.Exists(FilePath))
                Utils.CopyResource(Resource, FilePath);
        }

        /// <summary>
        /// Iterate all lines from the file.
        /// </summary>
        public IEnumerable<String> ReadLineByLine()
        {
            return Utils.ReadLineByLine(FilePath);
        }
    }
}

