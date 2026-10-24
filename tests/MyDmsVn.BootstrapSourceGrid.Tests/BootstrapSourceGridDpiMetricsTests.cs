using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using MyDmsVn.BootstrapSourceGrid.Internal;
using NUnit.Framework;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture]
public sealed class BootstrapSourceGridDpiMetricsTests
{
    [TestCase(96)]
    [TestCase(120)]
    [TestCase(144)]
    [TestCase(192)]
    public void ScalesOnlyIntegrationOwnedMetrics(int dpi)
    {
        var theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);

        var metrics = BootstrapSourceGridDpiMetrics.FromTheme(theme, dpi);

        Assert.That(metrics.CellPadding,
            Is.EqualTo(DpiScaler.Scale(theme.Metrics.SpacingXS, dpi)));
        Assert.That(metrics.CellBorderThickness,
            Is.EqualTo(DpiScaler.Scale(theme.Metrics.BorderWidth, dpi)));
        Assert.That(metrics.FocusThickness,
            Is.EqualTo(DpiScaler.Scale(theme.Metrics.FocusBorderWidth, dpi)));
    }
}
