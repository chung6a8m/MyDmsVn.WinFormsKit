using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

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
                if (surface.ToArgb() == border.ToArgb())
                {
                    using var brush = new SolidBrush(surface);
                    graphics.FillPath(brush, outerPath);
                }
                else
                {
                    using var innerPath = geometry.CreateInnerPath();
                    PaintAdjacentContours(graphics, geometry.OuterBounds, outerPath, innerPath, surface, border);
                }
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

    private static void PaintAdjacentContours(Graphics graphics, RectangleF bounds, GraphicsPath outerPath,
        GraphicsPath innerPath, Color surface, Color border)
    {
        var pixels = Rectangle.FromLTRB((int)Math.Floor(bounds.Left), (int)Math.Floor(bounds.Top),
            (int)Math.Ceiling(bounds.Right), (int)Math.Ceiling(bounds.Bottom));
        using var outerMask = CreateCoverageMask(pixels, outerPath);
        using var innerMask = CreateCoverageMask(pixels, innerPath);
        var outer = ReadPixels(outerMask);
        var inner = ReadPixels(innerMask);
        var combined = new byte[outer.Length];
        // Complementary AA coverage must be combined before SourceOver, otherwise the shared
        // inner edge leaks backdrop. Premultiplied colors also keep translucent interiors single-pass.
        for (var offset = 0; offset < combined.Length; offset += 4)
        {
            var innerCoverage = Math.Min(outer[offset + 3], inner[offset + 3]) / 255f;
            var borderCoverage = (outer[offset + 3] / 255f) - innerCoverage;
            var surfaceAlpha = surface.A * innerCoverage;
            var borderAlpha = border.A * borderCoverage;
            combined[offset] = (byte)Math.Round(((surface.B * surfaceAlpha) + (border.B * borderAlpha)) / 255f);
            combined[offset + 1] = (byte)Math.Round(((surface.G * surfaceAlpha) + (border.G * borderAlpha)) / 255f);
            combined[offset + 2] = (byte)Math.Round(((surface.R * surfaceAlpha) + (border.R * borderAlpha)) / 255f);
            combined[offset + 3] = (byte)Math.Round(surfaceAlpha + borderAlpha);
        }

        using var layer = new Bitmap(pixels.Width, pixels.Height, PixelFormat.Format32bppPArgb);
        var data = layer.LockBits(new Rectangle(Point.Empty, layer.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
        try { Marshal.Copy(combined, 0, data.Scan0, combined.Length); }
        finally { layer.UnlockBits(data); }
        graphics.DrawImageUnscaled(layer, pixels.Location);
    }

    private static Bitmap CreateCoverageMask(Rectangle bounds, GraphicsPath path)
    {
        var mask = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        try
        {
            using var graphics = Graphics.FromImage(mask);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            graphics.TranslateTransform(-bounds.Left, -bounds.Top);
            using var brush = new SolidBrush(Color.White);
            graphics.FillPath(brush, path);
            return mask;
        }
        catch { mask.Dispose(); throw; }
    }

    private static byte[] ReadPixels(Bitmap bitmap)
    {
        var data = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            // Newly allocated 32-bpp bitmaps have positive, width*4 stride.
            var pixels = new byte[checked(bitmap.Width * bitmap.Height * 4)];
            Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
            return pixels;
        }
        finally { bitmap.UnlockBits(data); }
    }
}
