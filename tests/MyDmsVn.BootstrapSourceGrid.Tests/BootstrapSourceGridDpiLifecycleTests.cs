using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using MyDmsVn.BootstrapSourceGrid.Internal;
using NUnit.Framework;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapSourceGridDpiLifecycleTests
{
    [OneTimeSetUp]
    public void ConfigureWinForms()
    {
        WinFormsTestGuard.Configure();
    }

    [TestCase(96, 4, 1, 2)]
    [TestCase(120, 5, 1, 3)]
    [TestCase(144, 6, 2, 3)]
    [TestCase(192, 8, 2, 4)]
    public void DefaultThemeMetricsUseFrameworkDpiScaling(
        int dpi,
        int expectedPadding,
        int expectedBorder,
        int expectedFocus)
    {
        var theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);

        var metrics = BootstrapSourceGridDpiMetrics.FromTheme(theme, dpi);

        Assert.That(metrics.CellPadding, Is.EqualTo(expectedPadding));
        Assert.That(metrics.CellBorderThickness, Is.EqualTo(expectedBorder));
        Assert.That(metrics.FocusThickness, Is.EqualTo(expectedFocus));
    }

    [Test]
    public void ConstructionBeforeHandleUsesDefaultDpi()
    {
        using (var grid = new BootstrapSourceGridControl())
        {
            Assert.That(grid.IsHandleCreated, Is.False);
            Assert.That(grid.CurrentDpi, Is.EqualTo(DpiScaler.DefaultDpi));
        }
    }

    [Test]
    public void FirstHandleCreationResynchronizesMetricsFromDeviceDpi()
    {
        using (var dpiProbe = new Control())
        using (var grid = new BootstrapSourceGridControl())
        {
            dpiProbe.CreateControl();
            var deviceDpi = dpiProbe.DeviceDpi;
            var poisonedDpi = deviceDpi <= 192 ? 384 : 96;
            var theme = BootstrapThemeManager.CurrentTheme;
            var expectedPadding = DpiScaler.Scale(theme.Metrics.SpacingXS, deviceDpi);
            var expectedBorder = DpiScaler.Scale(theme.Metrics.BorderWidth, deviceDpi);
            var expectedFocus = DpiScaler.Scale(theme.Metrics.FocusBorderWidth, deviceDpi);
            grid.RefreshDpiMetrics(poisonedDpi);

            Assert.That(grid.CurrentDpiMetrics.CellPadding, Is.Not.EqualTo(expectedPadding));
            Assert.That(grid.IsHandleCreated, Is.False);

            grid.CreateControl();

            Assert.That(grid.IsHandleCreated, Is.True);
            Assert.That(grid.CurrentDpi, Is.EqualTo(deviceDpi));
            Assert.That(grid.CurrentDpiMetrics.CellPadding, Is.EqualTo(expectedPadding));
            Assert.That(grid.CurrentDpiMetrics.CellBorderThickness, Is.EqualTo(expectedBorder));
            Assert.That(grid.CurrentDpiMetrics.FocusThickness, Is.EqualTo(expectedFocus));
        }
    }

    [Test]
    public void RefreshDpiMetricsDoesNotScaleSourceGridDimensions()
    {
        using (var grid = new BootstrapSourceGridControl())
        {
            grid.Redim(2, 2);
            grid[0, 0] = new SourceGrid.Cells.Cell("value");
            grid.Rows[0].Height = 37;
            grid.Columns[0].Width = 123;

            grid.RefreshDpiMetrics(144);

            Assert.That(grid.CurrentDpiMetrics.CellPadding, Is.EqualTo(6));
            Assert.That(grid.CurrentDpiMetrics.CellBorderThickness, Is.EqualTo(2));
            Assert.That(grid.CurrentDpiMetrics.FocusThickness, Is.EqualTo(3));
            Assert.That(grid.Rows[0].Height, Is.EqualTo(37));
            Assert.That(grid.Columns[0].Width, Is.EqualTo(123));

            grid.RefreshDpiMetrics(192);

            Assert.That(grid.CurrentDpiMetrics.CellPadding, Is.EqualTo(8));
            Assert.That(grid.CurrentDpiMetrics.CellBorderThickness, Is.EqualTo(2));
            Assert.That(grid.CurrentDpiMetrics.FocusThickness, Is.EqualTo(4));
            Assert.That(grid.Rows[0].Height, Is.EqualTo(37));
            Assert.That(grid.Columns[0].Width, Is.EqualTo(123));
        }
    }
}
