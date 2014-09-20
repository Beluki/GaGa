
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.IO;
using System.Reflection;


namespace GaGa.StreamsFile
{
    internal static class Util
    {
        ///
        /// Contants
        ///

        /// <summary>
        /// GetLastWriteTime() returns this when a file is not found.
        /// </summary>
        public static readonly DateTime FileNotFoundUtc = DateTime.FromFileTimeUtc(0);

        ///
        /// Resources
        ///

        /// <summary>
        /// Copy an embedded resource to a file.
        /// </summary>
        /// <param name="resource">Resource name, including namespace.</param>
        /// <param name="filepath">Destination path.</param>
        public static void ResourceCopy(String resource, String filepath)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream source = assembly.GetManifestResourceStream(resource))
            {
                using (FileStream target = new FileStream(filepath, FileMode.Create, FileAccess.Write))
                {
                    source.CopyTo(target);
                }
            }
        }
    }
}

