using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal static class BootstrapTabHeaderPainter
{
    internal static void DrawHeader(
        DrawItemEventArgs e, BootstrapTabHeaderLayout layout,
        BootstrapTabHeaderMetrics metrics, BootstrapTabHeaderPalette palette,
        string text, Image? image, Rectangle closeBounds,
        bool enabled, bool selected, bool showFocus)
    {
        var graphics = e.Graphics;
        var state = graphics.Save();
        try
        {
            DrawSurface(graphics, layout, metrics, palette, selected);
            if (image is not null && layout.ImageBounds.Width > 0 && layout.ImageBounds.Height > 0)
            {
                if (enabled) graphics.DrawImage(image, layout.ImageBounds);
                else ControlPaint.DrawImageDisabled(graphics, image, layout.ImageBounds.X, layout.ImageBounds.Y, palette.Background);
            }

            if (layout.TextBounds.Width > 0 && layout.TextBounds.Height > 0)
                TextRenderer.DrawText(graphics, text, e.Font, layout.TextBounds, palette.Foreground,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

            if (!closeBounds.IsEmpty)
            {
                using var pen = new Pen(palette.Foreground, Math.Max(1, metrics.BorderWidth));
                var inset = Math.Max(3, closeBounds.Width / 4);
                graphics.DrawLine(pen, closeBounds.Left + inset, closeBounds.Top + inset,
                    closeBounds.Right - inset, closeBounds.Bottom - inset);
                graphics.DrawLine(pen, closeBounds.Right - inset, closeBounds.Top + inset,
                    closeBounds.Left + inset, closeBounds.Bottom - inset);
            }

            if (selected && showFocus && enabled)
                DrawFocus(graphics, layout.FocusBounds, metrics.FocusBorderWidth, palette.Focus);
        }
        finally { graphics.Restore(state); }
    }

    private static void DrawSurface(Graphics graphics, BootstrapTabHeaderLayout layout,
        BootstrapTabHeaderMetrics metrics, BootstrapTabHeaderPalette palette, bool selected)
    {
        if (layout.SurfaceBounds.Width <= 0 || layout.SurfaceBounds.Height <= 0) return;
        using (var neutral = new SolidBrush(BootstrapThemeManager.CurrentTheme.Colors.Surface))
            graphics.FillRectangle(neutral, layout.SurfaceBounds);

        if (layout.Style == BootstrapTabStyle.Underline)
        {
            using (var background = new SolidBrush(palette.Background))
                graphics.FillRectangle(background, layout.SurfaceBounds);
            if (selected && layout.UnderlineBounds.Width > 0 && layout.UnderlineBounds.Height > 0)
            {
                using var accent = new SolidBrush(palette.Accent);
                graphics.FillRectangle(accent, layout.UnderlineBounds);
            }
            return;
        }

        var borderWidth = Math.Max(0f, metrics.BorderWidth);
        var borderInset = borderWidth / 2f;
        var bounds = new RectangleF(layout.SurfaceBounds.X + borderInset, layout.SurfaceBounds.Y + borderInset,
            Math.Max(0f, layout.SurfaceBounds.Width - borderWidth),
            Math.Max(0f, layout.SurfaceBounds.Height - borderWidth));
        if (bounds.Width <= 0f || bounds.Height <= 0f) return;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundedPath.Create(bounds, layout.CornerRadius);
        using var surface = new SolidBrush(palette.Background);
        graphics.FillPath(surface, path);
        if ((layout.Style == BootstrapTabStyle.Tabs || selected) && metrics.BorderWidth > 0)
        {
            using var border = new Pen(palette.Border, borderWidth);
            graphics.DrawPath(border, path);
        }
    }

    private static void DrawFocus(Graphics graphics, Rectangle bounds, int width, Color color)
    {
        if (width <= 0 || bounds.Width <= 0 || bounds.Height <= 0) return;
        var focus = bounds;
        focus.Width = Math.Max(0, focus.Width - 1);
        focus.Height = Math.Max(0, focus.Height - 1);
        if (focus.Width <= 0 || focus.Height <= 0) return;
        using var pen = new Pen(color, width);
        graphics.DrawRectangle(pen, focus);
    }
}
