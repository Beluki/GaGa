
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

        /// <summary>
        /// A color table that tries to match the current aero theme
        /// when rendering selected menu items.
        /// </summary>
        public AeroColorTable()
        {
            lastAeroColor = Color.Empty;
            menuItemSelected = base.MenuItemSelected;
            menuItemBorder = base.MenuItemBorder;
        }

        /// <summary>
        /// Update the colors to match the current aero theme.
        /// </summary>
        public void UpdateColors()
        {
            Color aeroColor = Util.GetCurrentAeroColor();

            // unable to read it, fallback to default colors:
            if (aeroColor == Color.Empty)
            {
                menuItemSelected = base.MenuItemSelected;
                menuItemBorder = base.MenuItemBorder;
            }
            else
            {
                // recalculate when needed:
                if (aeroColor != lastAeroColor)
                {
                    lastAeroColor = aeroColor;
                    RecalculateColors(aeroColor);
                }
            }
        }

        /// <summary>
        /// Recalculate our color values from a given base color.
        /// </summary>
        private void RecalculateColors(Color baseColor)
        {
            Double A = (Double) baseColor.A;
            Double R = (Double) baseColor.R;
            Double G = (Double) baseColor.G;
            Double B = (Double) baseColor.B;

            // too low alpha for clear visibility, darken it:
            if (A < 30)
            {
                A = 30;
            }

            // we want an opaque color, so remove alpha
            // but keep the current color value:
            // c = c * (alpha / 255) + (255 * (1 - (alpha / 255)))
            R = R * (A / 255) + (255 * (1 - (A / 255)));
            G = G * (A / 255) + (255 * (1 - (A / 255)));
            B = B * (A / 255) + (255 * (1 - (A / 255)));

            // we don't want colors too close to white
            // those would be indistinguishable from the background:
            if ((R > 220) && (G > 220) && (B > 220))
            {
                R = G = B = 220;
            }

            // we don't want color too close to black
            // those would obscure the text:
            if ((R < 50) && (G < 50) && (B < 50))
            {
                R = G = B = 50;
            }

            R = Util.Clamp(R, 0, 255);
            G = Util.Clamp(G, 0, 255);
            B = Util.Clamp(B, 0, 255);

            Color color = Color.FromArgb((Int32) R, (Int32) G, (Int32) B);

            // right now, both the selected menu and the border use
            // the same color but we could also darken the border a bit:
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
        /// <summary>
        /// A renderer that tries to match the current aero theme
        /// when drawing selected menu items.
        /// </summary>
        public ToolStripAeroRenderer() : base(new AeroColorTable())
        {

        }

        /// <summary>
        /// Update the colors to match the current aero theme.
        /// </summary>
        public void UpdateColors()
        {
            ((AeroColorTable) ColorTable).UpdateColors();
        }
    }
}

