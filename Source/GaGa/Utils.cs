
// GaGa.
// A simple radio player running on the Windows notification area.


using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;


namespace GaGa
{
    /// <summary>
    /// Stand-alone utilities.
    /// </summary>
    internal static class Utils
    {
        /// <summary>
        /// Load an Icon from an embedded resource.
        /// </summary>
        /// <param name="resource">
        /// The resource path as a string, including namespace.
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
        /// The resource path as a string, including namespace.
        /// </param>
        /// <param name="filepath">
        /// Destination path as a string.
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
        /// <param name="filepath">
        /// File path as a string.
        /// </param>
        public static IEnumerable<String> ReadLineByLine(String filepath)
        {
            String line;
            using (StreamReader reader = File.OpenText(filepath))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }
    }
}

