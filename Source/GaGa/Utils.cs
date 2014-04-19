
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;


namespace GaGa
{
    /// <summary>
    /// Stand-alone utilities.
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
        /// Copy an embedded resource to a file.
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
            using (Stream source = assembly.GetManifestResourceStream(resource))
            {
                FileMode mode = FileMode.Create;
                FileAccess access = FileAccess.Write;

                using (FileStream target = new FileStream(filepath, mode, access))
                {
                    source.CopyTo(target);
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
        /// Get the path for the directory that contains
        /// the application executable.
        /// </summary>
        public static String ApplicationDirectory()
        {
            return Path.GetDirectoryName(Application.ExecutablePath);
        }

        /// <summary>
        /// Show a MessageBox with Yes and No buttons.
        /// Return true when Yes was clicked, false otherwise.
        /// </summary>
        /// <param name="text">MessageBox text.</param>
        /// <param name="caption">MessageBox caption.</param>
        public static Boolean MessageBoxYesNo(String text, String caption)
        {
            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
            return MessageBox.Show(text, caption, buttons) == DialogResult.Yes;
        }
    }
}

