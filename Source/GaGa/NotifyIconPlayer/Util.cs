
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;


namespace GaGa.NotifyIconPlayer
{
    internal static class Util
    {
        ///
        /// Resources
        ///

        /// <summary>
        /// Load an embedded resource as an icon.
        /// </summary>
        /// <param name="resource">Resource name, including namespace.</param>
        public static Icon ResourceAsIcon(String resource)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resource))
            {
                return new Icon(stream);
            }
        }

        ///
        /// NotifyIcon extensions
        ///

        /// <summary>
        /// Safely change the icon tooltip text.
        /// Strings longer than 63 characters are trimmed to 60 characters
        /// with a "..." suffix.
        /// </summary>
        /// <param name="text">Tooltip text to display.</param>
        public static void SetToolTipText(this NotifyIcon notifyIcon, String text)
        {
            if (text.Length > 63)
            {
                notifyIcon.Text = text.Substring(0, 60) + "...";
            }
            else
            {
                notifyIcon.Text = text;
            }
        }
    }
}

