using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.BootstrapSourceGrid.Demo;

internal static class DemoThemeFactory
{
    internal static BootstrapTheme Create(
        BootstrapThemeMode mode,
        BootstrapThemeTypography typography,
        bool reducedMotion)
    {
        return new BootstrapTheme(
            mode,
            BootstrapThemeColors.CreateDefault(mode),
            BootstrapThemeMetrics.Default,
            typography,
            reducedMotion);
    }
}
