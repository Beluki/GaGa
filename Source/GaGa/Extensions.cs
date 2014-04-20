
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.Reflection;
using System.Windows.Forms;


namespace GaGa
{
    /// <summary>
    /// Extension methods.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// GetLastWriteTime() returns this when a file is not found.
        ///
        /// MSDN:
        /// If the file described in the path parameter does not exist
        /// this method returns 12:00 midnight, January 1, 1601 A.D. (C.E.)
        /// Coordinated Universal Time (UTC), adjusted to local time.
        /// </summary>
        private static readonly Int64 fileNotFoundUtc = new DateTime(1601, 1, 1).ToFileTimeUtc();

        /// <summary>
        /// Determine whether this date is equivalent to the date
        /// that GetLastWriteTime() returns when a file is not found.
        /// </summary>
        public static Boolean IsFileNotFound(this DateTime datetime)
        {
            return datetime.ToFileTimeUtc() == fileNotFoundUtc;
        }

        /// <summary>
        /// Show the context menu for the icon.
        /// </summary>
        public static void InvokeContextMenu(this NotifyIcon icon)
        {
            MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            mi.Invoke(icon, null);
        }

        /// <summary>
        /// Choose an action depending on whether this value is true.
        /// </summary>
        /// <param name="action1">Action to execute when true.</param>
        /// <param name="action2">Action to execute when false.</param>
        public static void OnBool(this Boolean value, Action action1, Action action2)
        {
            if (value)
            {
                action1();
            }
            else
            {
                action2();
            }
        }

        /// <summary>
        /// Choose an action depending on whether this value is null.
        /// </summary>
        /// <param name="action1">Action to execute when null.</param>
        /// <param name="action2">Action to execute when not null.</param>
        public static void OnNull(this Object value, Action action1, Action action2)
        {
            if (value == null)
            {
                action1();
            }
            else
            {
                action2();
            }
        }
    }
}

