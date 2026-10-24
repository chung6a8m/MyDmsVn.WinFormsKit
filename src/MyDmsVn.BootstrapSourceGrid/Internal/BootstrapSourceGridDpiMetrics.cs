using System;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.BootstrapSourceGrid.Internal;

internal readonly struct BootstrapSourceGridDpiMetrics
{
    public BootstrapSourceGridDpiMetrics(
        int cellPadding,
        int cellBorderThickness,
        int focusThickness)
    {
        CellPadding = cellPadding;
        CellBorderThickness = cellBorderThickness;
        FocusThickness = focusThickness;
    }

    public int CellPadding { get; }

    public int CellBorderThickness { get; }

    public int FocusThickness { get; }

    public static BootstrapSourceGridDpiMetrics FromTheme(BootstrapTheme theme, int dpi)
    {
        if (theme is null)
        {
            throw new ArgumentNullException(nameof(theme));
        }

        // These are the only Stage 1 metrics introduced and owned by the integration.
        return new BootstrapSourceGridDpiMetrics(
            DpiScaler.Scale(theme.Metrics.SpacingXS, dpi),
            DpiScaler.Scale(theme.Metrics.BorderWidth, dpi),
            DpiScaler.Scale(theme.Metrics.FocusBorderWidth, dpi));
    }
}
