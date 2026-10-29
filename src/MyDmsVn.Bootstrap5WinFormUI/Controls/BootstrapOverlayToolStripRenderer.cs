using System.Drawing;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal sealed class BootstrapOverlayToolStripRenderer : ToolStripProfessionalRenderer
{
    internal BootstrapOverlayToolStripRenderer(Color backColor)
    {
        BackColor = backColor;
    }

    internal Color BackColor { get; set; }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var background = new SolidBrush(BackColor);
        e.Graphics.FillRectangle(background, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
    }
}
