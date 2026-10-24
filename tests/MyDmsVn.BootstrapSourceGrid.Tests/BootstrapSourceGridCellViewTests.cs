using System.Threading;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapSourceGridCellViewTests
{
    [OneTimeSetUp]
    public void ConfigureWinForms()
    {
        WinFormsTestGuard.Configure();
    }

    [Test]
    public void GetCell_ReplacesViewsCellDefaultWithSharedIntegrationView()
    {
        using (var grid = new BootstrapSourceGridControl())
        {
            grid.Redim(3, 1);
            grid[0, 0] = new SourceGrid.Cells.Cell("even");
            grid[1, 0] = new SourceGrid.Cells.Cell("odd");

            var even = grid.GetCell(0, 0);
            var odd = grid.GetCell(1, 0);

            Assert.That(even.View, Is.Not.SameAs(SourceGrid.Cells.Views.Cell.Default));
            Assert.That(odd.View, Is.SameAs(even.View));
            Assert.That(even.View.Font, Is.Null);
        }
    }

    [Test]
    public void GetCell_DoesNotReplaceCustomCellView()
    {
        using (var grid = new BootstrapSourceGridControl())
        {
            grid.Redim(1, 1);
            var customView = new SourceGrid.Cells.Views.Cell();
            var cell = new SourceGrid.Cells.Cell("custom") { View = customView };
            grid[0, 0] = cell;

            Assert.That(grid.GetCell(0, 0).View, Is.SameAs(customView));
        }
    }

    [Test]
    public void SharedCellView_PreparesBackgroundFromCurrentRowPosition()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        var theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);

        try
        {
            BootstrapThemeManager.CurrentTheme = theme;
            using (var grid = new BootstrapSourceGridControl())
            {
                grid.Redim(2, 1);
                grid[0, 0] = new SourceGrid.Cells.Cell("even");
                grid[1, 0] = new SourceGrid.Cells.Cell("odd");
                var view = (SourceGrid.Cells.Views.Cell)grid.GetCell(0, 0).View;

                view.Measure(new SourceGrid.CellContext(grid, new SourceGrid.Position(0, 0)), Size.Empty);
                Assert.That(view.BackColor, Is.EqualTo(theme.Colors.Surface));

                view.Measure(new SourceGrid.CellContext(grid, new SourceGrid.Position(1, 0)), Size.Empty);
                Assert.That(view.BackColor, Is.EqualTo(theme.Colors.SurfaceSecondary));
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void SharedCellView_PreparesDisabledForegroundWithoutChangingCellBehavior()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        var theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);

        try
        {
            BootstrapThemeManager.CurrentTheme = theme;
            using (var grid = new BootstrapSourceGridControl())
            {
                grid.Redim(1, 1);
                var cell = new SourceGrid.Cells.Cell("disabled", typeof(string));
                grid[0, 0] = cell;
                var editor = cell.Editor;
                var view = (SourceGrid.Cells.Views.Cell)grid.GetCell(0, 0).View;
                grid.Enabled = false;

                view.Measure(new SourceGrid.CellContext(grid, new SourceGrid.Position(0, 0)), Size.Empty);

                Assert.That(view.ForeColor, Is.EqualTo(theme.Colors.Disabled));
                Assert.That(cell.Editor, Is.SameAs(editor));
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void IndexerGetBeforeGetCell_DoesNotRequireBootstrapWrapperType()
    {
        using (var grid = new BootstrapSourceGridControl())
        {
            grid.Redim(2, 2);
            var cell = new SourceGrid.Cells.Cell("A");
            grid[0, 0] = cell;

            Assert.That(grid[0, 0], Is.SameAs(cell));
            Assert.That(grid.GetCell(0, 0), Is.SameAs(cell));
        }
    }

    [Test]
    public void ThemeChange_DoesNotRequireReassigningCells()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        var light = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

        try
        {
            BootstrapThemeManager.CurrentTheme = light;
            using (var grid = new BootstrapSourceGridControl())
            {
                grid.Redim(1, 1);
                var cell = new SourceGrid.Cells.Cell("themed");
                grid[0, 0] = cell;
                var themedView = grid.GetCell(0, 0).View;

                BootstrapThemeManager.CurrentTheme = dark;

                Assert.That(grid.GetCell(0, 0), Is.SameAs(cell));
                Assert.That(grid.GetCell(0, 0).View, Is.SameAs(themedView));
                Assert.That(themedView.ForeColor, Is.EqualTo(dark.Colors.Text));
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }
}
