
// GaGa.
// A lightweight radio player for the Windows tray.


using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;


namespace GaGa
{
    /// <summary>
    /// Standalone utilities for the GaGa main class.
    /// </summary>
    internal static class Utils
    {
        /// <summary>
        /// Load an Icon from a given assembly embedded resource name.
        /// </summary>
        /// <param name="resource">
        /// The embedded resource full path as a string, including namespace.
        /// </param>
        public static Icon LoadIconFromResource(String resource)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resource))
            {
                return new Icon(stream);
            }
        }

        /// <summary>
        /// Copy an embedded resource to the local filesystem.
        /// </summary>
        /// <param name="resource">
        /// The embedded resource full path as a string, including namespace.
        /// </param>
        /// <param name="filepath">
        /// Local path to copy the file to as a string.
        /// </param>
        public static void CopyResource(String resource, String filepath)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resource))
            {
                using (FileStream target = new FileStream(filepath, FileMode.Create, FileAccess.Write))
                {
                    stream.CopyTo(target);
                }
            }
        }

        /// <summary>
        /// Iterate all lines from an UTF-8 encoded text file.
        /// </summary>
        /// <param name="filename">
        /// File path as a string.
        /// </param>
        public static IEnumerable<string> ReadLines(String filename)
        {
            String line;
            using (StreamReader reader = File.OpenText(filename))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }
    }
}

