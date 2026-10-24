using DevAge.Drawing;
using MyDmsVn.BootstrapSourceGrid.Internal;
using MyDmsVn.BootstrapSourceGrid.Theming;

namespace MyDmsVn.BootstrapSourceGrid.Views;

internal sealed class BootstrapSourceGridRowHeaderView : SourceGrid.Cells.Views.RowHeader
{
    private readonly DevAge.Drawing.VisualElements.RowHeader _background;

    internal BootstrapSourceGridRowHeaderView(
        BootstrapSourceGridThemeSnapshot snapshot,
        BootstrapSourceGridDpiMetrics dpiMetrics)
    {
        _background = new DevAge.Drawing.VisualElements.RowHeader
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
