
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;


namespace GaGa
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
        /// Functions
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

        /// <summary>
        /// Show a MessageBox with Yes and No buttons.
        /// Return true when Yes is clicked, false otherwise.
        /// </summary>
        /// <param name="text">MessageBox text.</param>
        /// <param name="caption">MessageBox caption.</param>
        public static Boolean MessageBoxYesNo(String text, String caption)
        {
            return MessageBox.Show(text, caption, MessageBoxButtons.YesNo) == DialogResult.Yes;
        }

        ///
        /// Properties
        ///

        /// <summary>
        /// Get the path for the directory that contains
        /// the current application executable.
        /// </summary>
        public static String ExeFolder
        {
            get { return Path.GetDirectoryName(Application.ExecutablePath); }
        }

        ///
        /// Extensions
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
                text = text.Substring(0, 60) + "...";
            }

            notifyIcon.Text = text;
        }

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
    }
}

