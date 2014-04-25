
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;


namespace GaGa
{
    internal static class Utils
    {
        ///
        /// Constants.
        ///

        /// <summary>
        /// GetLastWriteTime() returns this when a file is not found.
        ///
        /// MSDN:
        /// If the file described in the path parameter does not exist
        /// this method returns 12:00 midnight, January 1, 1601 A.D. (C.E.)
        /// Coordinated Universal Time (UTC), adjusted to local time.
        /// </summary>
        public static readonly Int64 FileNotFoundUtc = new DateTime(1601, 1, 1).ToFileTimeUtc();

        ///
        /// Resource files.
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

        ///
        /// Message boxes.
        ///

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
        /// OS information.
        ///

        /// <summary>
        /// Get the path for the directory that contains
        /// the current application executable.
        /// </summary>
        public static String ExeFolder
        {
            get { return Path.GetDirectoryName(Application.ExecutablePath); }
        }

        /// <summary>
        /// Get the name of the current logged user.
        /// </summary>
        public static String Username
        {
            get { return Environment.UserName; }
        }
    }
}

