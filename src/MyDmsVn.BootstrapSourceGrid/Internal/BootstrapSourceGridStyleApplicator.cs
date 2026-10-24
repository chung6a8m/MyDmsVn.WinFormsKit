using MyDmsVn.BootstrapSourceGrid.Theming;
using MyDmsVn.BootstrapSourceGrid.Views;

namespace MyDmsVn.BootstrapSourceGrid.Internal;

internal sealed class BootstrapSourceGridStyleApplicator
{
    private readonly BootstrapSourceGridCellView _cellView;
    private readonly BootstrapSourceGridHeaderView _headerView;
    private readonly BootstrapSourceGridColumnHeaderView _columnHeaderView;
    private readonly BootstrapSourceGridRowHeaderView _rowHeaderView;

    internal BootstrapSourceGridStyleApplicator(
        BootstrapSourceGridThemeSnapshot snapshot,
        BootstrapSourceGridDpiMetrics dpiMetrics)
    {
        _cellView = new BootstrapSourceGridCellView(snapshot, dpiMetrics);
        _headerView = new BootstrapSourceGridHeaderView(snapshot, dpiMetrics);
        _columnHeaderView = new BootstrapSourceGridColumnHeaderView(snapshot, dpiMetrics);
        _rowHeaderView = new BootstrapSourceGridRowHeaderView(snapshot, dpiMetrics);
    }

    internal void ApplyDefaultView(SourceGrid.Cells.ICellVirtual? cell)
    {
        if (cell is null ||
            ReferenceEquals(cell.View, _cellView) ||
            ReferenceEquals(cell.View, _headerView) ||
            ReferenceEquals(cell.View, _columnHeaderView) ||
            ReferenceEquals(cell.View, _rowHeaderView))
        {
            return;
        }

        if (ReferenceEquals(cell.View, SourceGrid.Cells.Views.ColumnHeader.Default))
        {
            cell.View = _columnHeaderView;
        }
        else if (ReferenceEquals(cell.View, SourceGrid.Cells.Views.RowHeader.Default))
        {
            cell.View = _rowHeaderView;
        }
        else if (ReferenceEquals(cell.View, SourceGrid.Cells.Views.Header.Default))
        {
            cell.View = _headerView;
        }
        else if (ReferenceEquals(cell.View, SourceGrid.Cells.Views.Cell.Default))
        {
            cell.View = _cellView;
        }
    }

    internal void ApplyTheme(
        BootstrapSourceGridThemeSnapshot snapshot,
        BootstrapSourceGridDpiMetrics dpiMetrics)
    {
        _cellView.ApplyTheme(snapshot, dpiMetrics);
        _headerView.ApplyTheme(snapshot, dpiMetrics);
        _columnHeaderView.ApplyTheme(snapshot, dpiMetrics);
        _rowHeaderView.ApplyTheme(snapshot, dpiMetrics);
    }
}
