
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.Drawing;
using System.Windows.Forms;


namespace GaGa.Controls
{
    [System.ComponentModel.DesignerCategory("")]
    internal class MyToolStripLabel : ToolStripControlHost
    {
        public Label Label;

        public MyToolStripLabel() : base(new Label())
        {
            Label = (Label) this.Control;
        }
    }
}

