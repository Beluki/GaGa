
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
    }
}

