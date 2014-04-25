
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
        /// <summary>
        /// File path in the filesystem.
        /// </summary>
        public readonly String FilePath;

        /// <summary>
        /// Resource name used to recreate it, including namespace.
        /// </summary>
        public readonly String Resource;

        /// <summary>
        /// Maintains an UTF8-encoded text file that can be recreated
        /// from an embedded resource.
        /// </summary>
        /// <param name="filepath">
        /// File path in the filesystem.
        /// </param>
        /// <param name="resource">
        /// Resource name used to recreate it, including namespace.
        /// </param>
        public StreamsFile(String filepath, String resource)
        {
            FilePath = filepath;
            Resource = resource;
        }

        /// <summary>
        /// Recreate the file from the embedded resource unless it exists.
        /// </summary>
        public void RecreateUnlessExists()
        {
            if (!File.Exists(FilePath))
                Utils.ResourceCopy(Resource, FilePath);
        }

        /// <summary>
        /// Return the date and time the file was last written to.
        /// </summary>
        public DateTime GetLastWriteTime()
        {
            return File.GetLastWriteTime(FilePath);
        }

        /// <summary>
        /// Iterate all lines from the file.
        /// </summary>
        public IEnumerable<String> ReadLines()
        {
            String line;
            using (StreamReader reader = File.OpenText(FilePath))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
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

