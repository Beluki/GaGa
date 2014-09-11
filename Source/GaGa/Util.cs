
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;

using Microsoft.Win32;


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
        /// Resources
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

        ///
        /// IO
        ///

        /// <summary>
        /// Serialize an object to a binary file.
        /// </summary>
        /// <param name="value">Object to serialize.</param>
        /// <param name="filepath">Destination path.</param>
        public static void Serialize(Object value, String filepath)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream fs = new FileStream(filepath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                formatter.Serialize(fs, value);
            }
        }

        /// <summary>
        /// Deserialize an object from a binary file.
        /// </summary>
        /// <param name="filepath">File path.</param>
        public static Object Deserialize(String filepath)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
            {
                return formatter.Deserialize(fs);
            }
        }

        ///
        /// Math
        ///

        /// <summary>
        /// Clamp a value inclusively between min and max.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="min">Minimum value.</param>
        /// <param name="max">Maximum value.</param>
        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0)
                return min;

            if (value.CompareTo(max) > 0)
                return max;

            return value;
        }

        ///
        /// MessageBoxes
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
        /// OS information
        ///

        /// <summary>
        /// Return the current Windows Aero colorization value
        /// or Color.Empty when Aero is not supported or unable to get it.
        /// </summary>
        public static Color GetCurrentAeroColor()
        {
            try
            {
                Object value = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM", "ColorizationColor", null);

                if (value == null)
                {
                    return Color.Empty;
                }
                else
                {
                    return Color.FromArgb((Int32) value);
                }
            }
            // unable to read registry or present but value not an Int32:
            catch (Exception)
            {
                return Color.Empty;
            }
        }

        /// <summary>
        /// Get the path for the directory that contains
        /// the current application executable.
        /// </summary>
        public static String ApplicationFolder
        {
            get
            {
                return Path.GetDirectoryName(Application.ExecutablePath);
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

