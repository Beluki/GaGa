
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.Reflection;
using System.Windows.Forms;


namespace GaGa
{
    internal static class Extensions
    {
        ///
        /// NotifyIcon extensions.
        ///

        /// <summary>
        /// Show the context menu for the icon.
        /// </summary>
        public static void InvokeContextMenu(this NotifyIcon notifyIcon)
        {
            MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            mi.Invoke(notifyIcon, null);
        }

        /// <summary>
        /// Safely change the icon tooltip text.
        /// Strings longer than 63 characters are trimmed to 60 characters
        /// with a "..." suffix.
        /// </summary>
        /// <param name="text">Tooltip text to display.</param>
        public static void SetToolTipText(this NotifyIcon notifyIcon, String text)
        {
            if (text.Length > 63)
                text = text.Substring(0, 60) + "...";

            notifyIcon.Text = text;
        }
    }
}

