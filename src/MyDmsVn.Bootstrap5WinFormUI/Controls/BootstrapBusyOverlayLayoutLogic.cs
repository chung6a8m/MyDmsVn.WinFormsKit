using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;
internal readonly struct BootstrapBusyOverlayLayout
{
    internal readonly Rectangle Card, Spinner, Viewport, Cancel;
    internal readonly Size Message;
    internal BootstrapBusyOverlayLayout(Rectangle card, Rectangle spinner, Rectangle viewport, Rectangle cancel, Size message) { Card = card; Spinner = spinner; Viewport = viewport; Cancel = cancel; Message = message; }
}
internal static class BootstrapBusyOverlayLayoutLogic
{
    internal static BootstrapBusyOverlayLayout Calculate(Size client, Font font, string message, string cancelText, bool cancel, int dpi, BootstrapThemeMetrics metrics)
    {
        var cw = Math.Max(0, client.Width); var ch = Math.Max(0, client.Height);
        var margin = DpiScaler.Scale(metrics.SpacingLG, dpi);
        var padding = margin; var gap = DpiScaler.Scale(metrics.SpacingSM, dpi);
        var buttonHeight = cancel ? DpiScaler.Scale(metrics.ControlHeightSmall, dpi) : 0;
        if (ch < buttonHeight + 2 * margin + 2 * padding + gap) { margin = 0; padding = 0; gap = 0; }
        if (cw < DpiScaler.Scale(120, dpi)) { margin = 0; padding = 0; }
        var width = Math.Max(0, Math.Min(DpiScaler.Scale(480, dpi), cw - 2 * margin));
        var innerWidth = Math.Max(0, width - 2 * padding);
        var text = innerWidth == 0 ? Size.Empty : TextRenderer.MeasureText(message, font, new Size(innerWidth, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.TextBoxControl);
        var spinner = DpiScaler.Scale(24, dpi);
        var available = Math.Max(0, ch - 2 * margin);
        if (available < 2 * padding + spinner + gap + buttonHeight + gap + font.Height) spinner = 0;
        var top = padding + (spinner > 0 ? spinner + gap : 0);
        var desired = top + text.Height + (cancel ? gap + buttonHeight : 0) + padding;
        var height = Math.Min(available, desired);
        var bh = Math.Min(buttonHeight, Math.Max(0, height - 2 * padding));
        var by = Math.Max(0, height - padding - bh);
        var viewportHeight = Math.Max(0, (cancel ? by - gap : height - padding) - top);
        top = Math.Min(top, Math.Max(0, cancel ? by : height));
        var buttonWidth = Math.Min(innerWidth, Math.Max(DpiScaler.Scale(80, dpi), TextRenderer.MeasureText(cancelText, font).Width + DpiScaler.Scale(24, dpi)));
        var card = new Rectangle((cw - width) / 2, (ch - height) / 2, width, height);
        return new BootstrapBusyOverlayLayout(card,
            new Rectangle(Math.Max(0, (width - spinner) / 2), Math.Min(padding, height), Math.Min(spinner, width), Math.Min(spinner, Math.Max(0, height - padding))),
            new Rectangle(Math.Min(padding, width), top, innerWidth, viewportHeight),
            new Rectangle(Math.Max(0, (width - buttonWidth) / 2), by, buttonWidth, bh), new Size(innerWidth, Math.Max(text.Height, viewportHeight)));
    }
}
