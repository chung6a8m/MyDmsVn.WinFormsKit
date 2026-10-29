using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal static class BootstrapTabHeaderPainter
{
    internal static void DrawHeaderFallback(DrawItemEventArgs e, TabAlignment alignment,
        bool rightToLeft, BootstrapTabHeaderMetrics metrics, BootstrapTabHeaderPalette palette,
        string text, Image? image, Rectangle closeBounds, bool enabled, bool selected, bool showFocus)
    {
        var bounds = e.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        var graphics = e.Graphics;
        var state = graphics.Save();
        try
        {
            graphics.SetClip(bounds, CombineMode.Intersect);
            using (var background = new SolidBrush(palette.Background))
                graphics.FillRectangle(background, bounds);
            if (selected)
            {
                using var accent = new Pen(palette.Accent, Math.Max(1, metrics.UnderlineHeight));
                if (alignment == TabAlignment.Left)
                    graphics.DrawLine(accent, bounds.Right - 1, bounds.Top + 2, bounds.Right - 1, bounds.Bottom - 2);
                else if (alignment == TabAlignment.Right)
                    graphics.DrawLine(accent, bounds.Left, bounds.Top + 2, bounds.Left, bounds.Bottom - 2);
                else
                    graphics.DrawLine(accent, bounds.Left + 2, bounds.Bottom - 1, bounds.Right - 2, bounds.Bottom - 1);
            }

            var vertical = alignment == TabAlignment.Left || alignment == TabAlignment.Right;
            var close = Rectangle.Intersect(bounds, closeBounds);
            var imageBounds = Rectangle.Empty;
            if (image is not null)
            {
                var rtlImageRight = close.IsEmpty ? bounds.Right - 5 : close.Left - 3;
                imageBounds = vertical
                    ? new Rectangle(bounds.Left + Math.Max(0, (bounds.Width - image.Width) / 2), bounds.Top + 3,
                        Math.Min(image.Width, bounds.Width), Math.Min(image.Height, Math.Max(0, bounds.Height - 6)))
                    : new Rectangle(rightToLeft ? rtlImageRight - image.Width : bounds.Left + 5,
                        bounds.Top + Math.Max(0, (bounds.Height - image.Height) / 2),
                        Math.Min(image.Width, bounds.Width), Math.Min(image.Height, bounds.Height));
                imageBounds = Rectangle.Intersect(bounds, imageBounds);
                if (rightToLeft && !vertical && !close.IsEmpty)
                    imageBounds = Rectangle.Intersect(imageBounds,
                        Rectangle.FromLTRB(bounds.Left, bounds.Top, Math.Max(bounds.Left, close.Left - 3), bounds.Bottom));
                if (!imageBounds.IsEmpty)
                {
                    if (enabled) graphics.DrawImage(image, imageBounds);
                    else ControlPaint.DrawImageDisabled(graphics, image, imageBounds.X, imageBounds.Y, palette.Background);
                }
            }

            if (!string.IsNullOrEmpty(text))
            {
                if (vertical)
                {
                    var start = imageBounds.IsEmpty ? 3 : imageBounds.Bottom - bounds.Top + 3;
                    var end = close.IsEmpty ? bounds.Height - 3 : close.Top - bounds.Top - 3;
                    var length = Math.Max(0, end - start);
                    if (length > 0)
                    {
                        var textState = graphics.Save();
                        try
                        {
                            if (alignment == TabAlignment.Left)
                            {
                                graphics.TranslateTransform(bounds.Left, bounds.Bottom);
                                graphics.RotateTransform(-90);
                            }
                            else
                            {
                                graphics.TranslateTransform(bounds.Right, bounds.Top);
                                graphics.RotateTransform(90);
                            }
                            using var foreground = new SolidBrush(palette.Foreground);
                            using var format = new StringFormat(StringFormatFlags.NoWrap)
                            {
                                Alignment = StringAlignment.Center,
                                LineAlignment = StringAlignment.Center,
                                Trimming = StringTrimming.EllipsisCharacter
                            };
                            graphics.DrawString(text, e.Font ?? Control.DefaultFont, foreground,
                                new RectangleF(start, 0, length, bounds.Width), format);
                        }
                        finally { graphics.Restore(textState); }
                    }
                }
                else
                {
                    var left = bounds.Left + 4;
                    var right = bounds.Right - 4;
                    if (!imageBounds.IsEmpty)
                    {
                        if (rightToLeft) right = Math.Min(right, imageBounds.Left - 3);
                        else left = Math.Max(left, imageBounds.Right + 3);
                    }
                    if (!close.IsEmpty)
                    {
                        right = Math.Min(right, close.Left - 3);
                    }
                    if (right > left)
                        TextRenderer.DrawText(graphics, text, e.Font,
                            Rectangle.FromLTRB(left, bounds.Top, right, bounds.Bottom), palette.Foreground,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis |
                            (rightToLeft ? TextFormatFlags.RightToLeft : 0));
                }
            }
            if (!close.IsEmpty)
            {
                using var pen = new Pen(palette.Foreground, Math.Max(1, metrics.BorderWidth));
                var inset = Math.Max(3, close.Width / 4);
                graphics.DrawLine(pen, close.Left + inset, close.Top + inset, close.Right - inset, close.Bottom - inset);
                graphics.DrawLine(pen, close.Right - inset, close.Top + inset, close.Left + inset, close.Bottom - inset);
            }
            if (selected && showFocus && enabled)
                DrawFocus(graphics, Rectangle.Inflate(bounds, -3, -3), metrics.FocusBorderWidth, palette.Focus);
        }
        finally { graphics.Restore(state); }
    }

    internal static void DrawCompactChrome(Graphics graphics, BootstrapTabChromeSnapshot snapshot,
        Rectangle dirtyBounds, BootstrapTheme theme)
    {
        if (graphics is null) throw new ArgumentNullException(nameof(graphics));
        if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));
        if (theme is null) throw new ArgumentNullException(nameof(theme));
        var dirty = Rectangle.Intersect(snapshot.ClientBounds, dirtyBounds);
        if (dirty.IsEmpty) return;

        var state = graphics.Save();
        try
        {
            using var region = new Region(dirty);
            if (!snapshot.PageDisplayBounds.IsEmpty) region.Exclude(snapshot.PageDisplayBounds);
            foreach (var item in snapshot.HeaderItemBounds) region.Exclude(item);
            foreach (var child in snapshot.NativeChildExclusions) region.Exclude(child);
            graphics.SetClip(region, CombineMode.Intersect);
            using var surface = new SolidBrush(theme.Colors.Surface);
            graphics.FillRectangle(surface, dirty);
        }
        finally { graphics.Restore(state); }
    }

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
                    TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);

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
