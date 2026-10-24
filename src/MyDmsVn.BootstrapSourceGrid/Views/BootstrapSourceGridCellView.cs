using DevAge.Drawing;
using MyDmsVn.BootstrapSourceGrid.Internal;
using MyDmsVn.BootstrapSourceGrid.Theming;

namespace MyDmsVn.BootstrapSourceGrid.Views;

internal sealed class BootstrapSourceGridCellView : SourceGrid.Cells.Views.Cell
{
    private BootstrapSourceGridThemeSnapshot _snapshot;
    private RectangleBorder _border;
    private Padding _padding;

    internal BootstrapSourceGridCellView(
        BootstrapSourceGridThemeSnapshot snapshot,
        BootstrapSourceGridDpiMetrics dpiMetrics)
    {
        _snapshot = snapshot;
        ApplyTheme(snapshot, dpiMetrics);
    }

    internal void ApplyTheme(
        BootstrapSourceGridThemeSnapshot snapshot,
        BootstrapSourceGridDpiMetrics dpiMetrics)
    {
        _snapshot = snapshot;
        _border = new RectangleBorder(
            new BorderLine(snapshot.BorderColor, dpiMetrics.CellBorderThickness));
        _padding = new Padding(dpiMetrics.CellPadding);

        ForeColor = snapshot.CellForeColor;
        Border = _border;
        Padding = _padding;
        Font = null;
    }

    protected override void PrepareView(SourceGrid.CellContext context)
    {
        var snapshot = _snapshot;
        BackColor = (context.Position.Row & 1) == 0
            ? snapshot.CellBackColor
            : snapshot.AlternateCellBackColor;
        ForeColor = context.Grid.Enabled
            ? snapshot.CellForeColor
            : snapshot.DisabledColor;
        Border = _border;
        Padding = _padding;

        base.PrepareView(context);
    }
}
