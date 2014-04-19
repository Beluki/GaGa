
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;


namespace GaGa
{
    internal class StreamsFile
    {
        public readonly String FilePath;
        public readonly String Resource;

        /// <summary>
        /// Maintains an UTF8-encoded text file that can be recreated
        /// from an embedded resource.
        /// </summary>
        /// <param name="filepath">
        /// File path for the target file.
        /// </param>
        /// <param name="resource">
        /// Path to the resource used to recreate it, including namespace.
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
        /// Recreate the file from the embedded resource unless it exists.
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

        /// <summary>
        /// Execute the file as a process.
        /// </summary>
        public void Run()
        {
            Process.Start(FilePath);
        }
    }
}

