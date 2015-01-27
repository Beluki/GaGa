
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.Drawing;

using Microsoft.Win32;


namespace GaGa.Controls
{
    internal static class Util
    {
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
            // unable to read registry or invalid value:
            catch (Exception)
            {
                return Color.Empty;
            }
        }
    }
}

