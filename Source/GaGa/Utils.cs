
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
    /// Stand-alone utilities and extension methods.
    /// </summary>
    internal static class Utils
    {
        /// <summary>
        /// Load an Icon from an embedded resource.
        /// </summary>
        /// <param name="resource">
        /// Resource path as a string, including namespace.
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
        /// Resource path as a string, including namespace.
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
        /// Iterate all lines from an UTF8-encoded text file.
        /// </summary>
        /// <param name="filepath">File path as a string.</param>
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

        /// <summary>
        /// Apply an action to each element. The action receives
        /// the current collection index as parameter.
        /// </summary>
        /// <param name="ie">Collection to iterate.</param>
        /// <param name="action">Action to apply on each element.</param>
        public static void EachIndex<T>(this IEnumerable<T> ie, Action<T, int> action)
        {
            int index = 0;
            foreach (T e in ie)
                action(e, index++);
        }

        /// <summary>
        /// Try to get the value associated with the specified key.
        /// When the key has no value, compute action, associate
        /// the key with the result and return the result.
        /// </summary>
        /// <param name="key">The key to lookup.</param>
        /// <param name="action">Action that determines the key value when not found.</param>
        public static TValue GetOrSet<TKey, TValue>(this Dictionary<TKey, TValue> d, TKey key, Func<TValue> action)
        {
            if (d.ContainsKey(key))
                return d[key];
            else
                return d[key] = action();
        }
    }
}

