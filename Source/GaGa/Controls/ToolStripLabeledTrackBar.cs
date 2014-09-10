
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.Drawing;
using System.Windows.Forms;


namespace GaGa.Controls
{
    [System.ComponentModel.DesignerCategory("")]
    internal class LabeledTrackBar : Panel
    {
        public Label label;
        public TrackBar trackbar;

        public LabeledTrackBar()
        {
            label = new Label();
            label.Location = new Point(0, 0);

            trackbar = new TrackBar();
            trackbar.Location = new Point(0, label.Bottom);
            
            trackbar.AutoSize = false;
            trackbar.TickStyle = TickStyle.None;
            trackbar.AutoSize = false;
            trackbar.Height = (Int32) (trackbar.PreferredSize.Height * 0.65);
            trackbar.Width = (Int32) (trackbar.PreferredSize.Width * 1.2);

            Controls.Add(label);
            Controls.Add(trackbar);

            Width = Math.Max(label.Width, trackbar.Width);
        }
    }

    [System.ComponentModel.DesignerCategory("")]
    internal class ToolStripLabeledTrackBar : ToolStripControlHost
    {
        public Label Label;
        public TrackBar TrackBar;

        public ToolStripLabeledTrackBar() : base(new LabeledTrackBar())
        {
            LabeledTrackBar control = (LabeledTrackBar) this.Control;
            
            Label = control.label;
            TrackBar = control.trackbar;
        }
    }
}

