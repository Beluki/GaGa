
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.Drawing;
using System.Windows.Forms;


namespace GaGa.Controls
{
    [System.ComponentModel.DesignerCategory("")]
    internal class MyToolStripTrackBar : ToolStripControlHost
    {
        public TrackBar TrackBar;

        public MyToolStripTrackBar() : base(new TrackBar())
        {
            TrackBar = (TrackBar) this.Control;

            // disable ticks and adjust size:
            TrackBar.TickStyle = TickStyle.None;
            TrackBar.AutoSize = false;
            TrackBar.Height = (Int32) (TrackBar.PreferredSize.Height * 0.65);
            TrackBar.Width = (Int32) (TrackBar.PreferredSize.Width * 1.2);
        }
    }
}

