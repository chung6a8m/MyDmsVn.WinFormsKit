using DevAge.Drawing;
using MyDmsVn.BootstrapSourceGrid.Internal;
using MyDmsVn.BootstrapSourceGrid.Theming;

namespace MyDmsVn.BootstrapSourceGrid.Views;

internal sealed class BootstrapSourceGridHeaderView : SourceGrid.Cells.Views.Header
{
    private readonly DevAge.Drawing.VisualElements.Header _background;

    internal BootstrapSourceGridHeaderView(
        BootstrapSourceGridThemeSnapshot snapshot,
        BootstrapSourceGridDpiMetrics dpiMetrics)
    {
        _background = new DevAge.Drawing.VisualElements.Header
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
