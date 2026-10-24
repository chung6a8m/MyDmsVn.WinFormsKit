using DevAge.Drawing;
using MyDmsVn.BootstrapSourceGrid.Theming;

namespace MyDmsVn.BootstrapSourceGrid.Internal;

internal static class BootstrapSourceGridHeaderStyle
{
    internal static void Apply(
        SourceGrid.Cells.Views.Header view,
        DevAge.Drawing.VisualElements.Header background,
        BootstrapSourceGridThemeSnapshot snapshot,
        BootstrapSourceGridDpiMetrics dpiMetrics)
    {
        ApplyView(view, snapshot, dpiMetrics, out var border);
        background.BackColor = snapshot.HeaderBackColor;
        background.Border = border;
    }

    internal static void Apply(
        SourceGrid.Cells.Views.ColumnHeader view,
        DevAge.Drawing.VisualElements.ColumnHeader background,
        BootstrapSourceGridThemeSnapshot snapshot,
        BootstrapSourceGridDpiMetrics dpiMetrics)
    {
        ApplyView(view, snapshot, dpiMetrics, out var border);
        background.BackColor = snapshot.HeaderBackColor;
        background.Border = border;
    }

    internal static void Apply(
        SourceGrid.Cells.Views.RowHeader view,
        DevAge.Drawing.VisualElements.RowHeader background,
        BootstrapSourceGridThemeSnapshot snapshot,
        BootstrapSourceGridDpiMetrics dpiMetrics)
    {
        ApplyView(view, snapshot, dpiMetrics, out var border);
        background.BackColor = snapshot.HeaderBackColor;
        background.Border = border;
    }

    private static void ApplyView(
        SourceGrid.Cells.Views.ViewBase view,
        BootstrapSourceGridThemeSnapshot snapshot,
        BootstrapSourceGridDpiMetrics dpiMetrics,
        out RectangleBorder border)
    {
        border = new RectangleBorder(
            new BorderLine(snapshot.BorderColor, dpiMetrics.CellBorderThickness));
        view.ForeColor = snapshot.HeaderForeColor;
        view.Padding = new Padding(dpiMetrics.CellPadding);
        view.Font = null;
    }
}
