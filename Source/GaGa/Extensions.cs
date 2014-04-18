
// GaGa.
// A simple radio player running on the Windows notification area.


using System;
using System.Reflection;
using System.Windows.Forms;


namespace GaGa
{
    internal static class Extensions
    {
        /// <summary>
        /// Show the icon context menu.
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

