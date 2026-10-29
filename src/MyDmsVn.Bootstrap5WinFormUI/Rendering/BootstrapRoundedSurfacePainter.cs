using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace MyDmsVn.Bootstrap5WinFormUI.Rendering;

internal static class BootstrapRoundedSurfacePainter
{
    internal static void Paint(
        Graphics graphics,
        BootstrapRoundedSurfaceGeometry geometry,
        Color surface,
        Color border)
    {
        if (graphics is null)
        {
            throw new ArgumentNullException(nameof(graphics));
        }

        var state = graphics.Save();
        try
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            using var outerPath = geometry.CreateOuterPath();
            if (outerPath.PointCount == 0)
            {
                return;
            }

            if (!geometry.HasUsableInterior && geometry.BorderWidth > 0f)
            {
                using var fallbackBrush = new SolidBrush(border);
                graphics.FillPath(fallbackBrush, outerPath);
                return;
            }

            using (var surfaceBrush = new SolidBrush(surface))
            {
                graphics.FillPath(surfaceBrush, outerPath);
            }

            if (geometry.BorderWidth <= 0f)
            {
                return;
            }

            using var strokePath = geometry.CreateStrokePath();
            if (strokePath.PointCount == 0)
            {
                using var fallbackBrush = new SolidBrush(border);
                graphics.FillPath(fallbackBrush, outerPath);
                return;
            }

            using var borderPen = new Pen(border, geometry.BorderWidth);
            graphics.DrawPath(borderPen, strokePath);
        }
        finally
        {
            graphics.Restore(state);
        }
    }
}
