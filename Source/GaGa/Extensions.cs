
// GaGa.
// A simple radio player running on the Windows notification area.


using System;
using System.Collections.Generic;


namespace GaGa
{
    /// <summary>
    /// Extension methods.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Try to get the value associated with the specified key.
        /// When the key has no value, compute action, associate
        /// the key with the result and return the result.
        /// </summary>
        /// <param name="key">
        /// The key to lookup.
        /// </param>
        /// <param name="action">
        /// Action that determines the key value when not found.
        /// </param>
        public static TValue GetOrSet<TKey, TValue>
            (this IDictionary<TKey, TValue> d, TKey key, Func<TValue> action)
        {
            if (d.ContainsKey(key))
                return d[key];
            else
                return d[key] = action();
        }
    }
}

