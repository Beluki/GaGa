
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.Drawing;
using System.Windows.Forms;


namespace GaGa.Controls
{
    internal class AeroColorTable : ProfessionalColorTable
    {
        private Color lastAeroColor;

        private Color menuItemSelected;
        private Color menuItemBorder;

        public AeroColorTable()
        {
            lastAeroColor = Color.Empty;
            menuItemSelected = Color.Empty;
            menuItemBorder = Color.Empty;
        }

        /// <summary>
        /// Update the colors to match the current aero theme.
        /// </summary>
        public void UpdateColors()
        {
            Color aeroColor = Util.AeroColor;

            // no change:
            if (aeroColor == lastAeroColor)
                return;

            // unable to get the aero color, fallback to default colors:
            if (aeroColor == Color.Empty)
            {
                menuItemSelected = base.MenuItemSelected;
                menuItemBorder = base.MenuItemBorder;
                return;
            }

            // present and changed, recalculate:
            RecalculateColors(aeroColor);
        }

        /// <summary>
        /// Recalculate our color values from a given aero color.
        /// </summary>
        private void RecalculateColors(Color aeroColor)
        {
            Double A = (Double) aeroColor.A;
            Double R = (Double) aeroColor.R;
            Double G = (Double) aeroColor.G;
            Double B = (Double) aeroColor.B;

            // aero tends to base light colors on low alpha
            // darken it a bit when too low to be clearly visible:
            if (A < 30)
            {
                A = 30;
            }

            // now remove alpha, we want an opaque color:
            // c = c * (alpha / 255) + (255 * (1 - (alpha / 255)))
            R = R * (A / 255) + (255 * (1 - (A / 255)));
            G = G * (A / 255) + (255 * (1 - (A / 255)));
            B = B * (A / 255) + (255 * (1 - (A / 255)));

            // we don't want colors too close to white
            // since those would be indistinguisable from the background:
            if ((R > 220) && (G > 220) && (B > 220))
            {
                R = 220; G = 220; B = 220;
            }

            Color color = Color.FromArgb((Int32) R, (Int32) G, (Int32) B);
            menuItemSelected = color;
            menuItemBorder = color;
        }
        
        /// <summary>
        /// Gets the solid color to use when a ToolStripMenuItem is selected.
        /// </summary>
        public override Color MenuItemSelected
        {
            get
            {
                return menuItemSelected;
            }
        }

        /// <summary>
        /// Gets the border color to use with ToolStripMenuItem.
        /// </summary>
        public override Color MenuItemBorder
        {
            get
            {
                return menuItemBorder;
            }
        }
    }

    internal class ToolStripAeroRenderer : ToolStripProfessionalRenderer
    {
        public ToolStripAeroRenderer() : base(new AeroColorTable())
        {

        }

        public void UpdateColors()
        {
            ((AeroColorTable) ColorTable).UpdateColors();
        }
    }
}

