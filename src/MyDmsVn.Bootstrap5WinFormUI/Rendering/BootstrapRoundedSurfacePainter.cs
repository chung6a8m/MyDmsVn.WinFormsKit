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

            var radius = geometry.OuterRadius;
            var halfBorder = geometry.BorderWidth / 2f;
            if (CollapsesRoundedCorner(radius.TopLeft, halfBorder) ||
                CollapsesRoundedCorner(radius.TopRight, halfBorder) ||
                CollapsesRoundedCorner(radius.BottomLeft, halfBorder) ||
                CollapsesRoundedCorner(radius.BottomRight, halfBorder))
            {
                // A rectangular centerline with a miter join would square a rounded outer corner.
                // Keep the border out of the interior: themes may use translucent semantic colors.
                using var borderBrush = new SolidBrush(border);
                using var innerPath = geometry.CreateInnerPath();
                using var borderBand = new GraphicsPath(FillMode.Alternate);
                borderBand.AddPath(outerPath, false);
                borderBand.AddPath(innerPath, false);
                graphics.FillPath(borderBrush, borderBand);
                using var innerBrush = new SolidBrush(surface);
                graphics.FillPath(innerBrush, innerPath);
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

    private static bool CollapsesRoundedCorner(float radius, float halfBorder) => radius > 0f && radius <= halfBorder;
}
