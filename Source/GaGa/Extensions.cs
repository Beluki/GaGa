
// GaGa.
// A simple radio player running on the Windows notification area.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;


namespace GaGa
{
    /// <summary>
    /// Extension methods.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Apply an action to each element.
        /// The action receives the current collection index as parameter.
        /// </summary>
        /// <param name="source">Collection to iterate.</param>
        /// <param name="action">Action to apply on each element.</param>
        public static void EachWithIndex<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            int index = 0;
            foreach (T element in source)
                action(element, index++);
        }

        /// <summary>
        /// Try to get the value associated with the specified key.
        /// When the key has no value, compute action, associate
        /// the key with the result and return the result.
        /// </summary>
        /// <param name="key">The key to lookup.</param>
        /// <param name="action">Action that determines the key value when not found.</param>
        public static TValue GetOrSet<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key, Func<TValue> action)
        {
            if (d.ContainsKey(key))
                return d[key];
            else
                return d[key] = action();
        }

        /// <summary>
        /// Determine whether the beginning of this string
        /// matches any of the specified arguments.
        /// </summary>
        /// <param name="s">Strings to test.</param>
        public static Boolean StartsWithAny(this String source, params String[] matches)
        {
            return matches.Any((match) => source.StartsWith(match));
        }

        /// <summary>
        /// Determine whether the string contains characters.
        /// </summary>
        public static Boolean IsEmpty(this String source)
        {
            return source == String.Empty;
        }

        /// <summary>
        /// Determine whether the beginning and ending of this string
        /// matches the specified strings.
        /// </summary>
        /// <param name="s1">Beginning string.</param>
        /// <param name="s2">Ending string</param>
        public static Boolean IsSurroundedBy(this String source, String s1, String s2)
        {
            return source.StartsWith(s1) && source.EndsWith(s2);
        }

        /// <summary>
        /// Separate the string in two parts
        /// using the given character as delimiter.
        /// </summary>
        /// <param name="s1">Substring before the delimiter.</param>
        /// <param name="s2">Substring after the delimiter.</param>
        /// <param name="separators">Characters considered delimiters.</param>
        public static void SplitTo(this String source, out String s1, out String s2, params Char[] separators)
        {
            String[] substrings = source.Split(separators, 2);

            if (substrings.Length < 2)
                throw new ArgumentException("String doesn't contain any of the delimiters.");

            s1 = substrings.First();
            s2 = substrings.Last();
        }
    }
}

