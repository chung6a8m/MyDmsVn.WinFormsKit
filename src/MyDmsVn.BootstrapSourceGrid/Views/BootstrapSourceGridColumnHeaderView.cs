using DevAge.Drawing;
using MyDmsVn.BootstrapSourceGrid.Internal;
using MyDmsVn.BootstrapSourceGrid.Theming;

namespace MyDmsVn.BootstrapSourceGrid.Views;

internal sealed class BootstrapSourceGridColumnHeaderView : SourceGrid.Cells.Views.ColumnHeader
{
    private readonly DevAge.Drawing.VisualElements.ColumnHeader _background;

    internal BootstrapSourceGridColumnHeaderView(
        BootstrapSourceGridThemeSnapshot snapshot,
        BootstrapSourceGridDpiMetrics dpiMetrics)
    {
        _background = new DevAge.Drawing.VisualElements.ColumnHeader
        {
            BackgroundColorStyle = BackgroundColorStyle.Solid,
        };
        Background = _background;
        ApplyTheme(snapshot, dpiMetrics);
    }

    internal void ApplyTheme(
        BootstrapSourceGridThemeSnapshot snapshot,
        BootstrapSourceGridDpiMetrics dpiMetrics)
    {
        BootstrapSourceGridHeaderStyle.Apply(this, _background, snapshot, dpiMetrics);
    }
}
