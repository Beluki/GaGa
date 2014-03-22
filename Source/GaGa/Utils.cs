
// GaGa.


using System;
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
    }
}

