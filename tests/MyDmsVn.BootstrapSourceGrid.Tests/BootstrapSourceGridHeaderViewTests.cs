using System.Threading;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapSourceGridHeaderViewTests
{
    [OneTimeSetUp]
    public void ConfigureWinForms()
    {
        WinFormsTestGuard.Configure();
    }

    [TestCase(BootstrapThemeMode.Light)]
    [TestCase(BootstrapThemeMode.Dark)]
    public void GetCell_ReplacesDefaultGenericHeaderViewAndAppliesTheme(
        BootstrapThemeMode mode)
    {
        var original = BootstrapThemeManager.CurrentTheme;
        var theme = BootstrapTheme.CreateDefault(mode);

        try
        {
            BootstrapThemeManager.CurrentTheme = theme;
            using (var grid = new BootstrapSourceGridControl())
            {
                grid.Redim(1, 1);
                grid[0, 0] = new SourceGrid.Cells.Header(string.Empty);

                var resolved = grid.GetCell(0, 0);

                Assert.That(resolved.View, Is.Not.SameAs(SourceGrid.Cells.Views.Header.Default));
                Assert.That(resolved.View, Is.InstanceOf<SourceGrid.Cells.Views.Header>());
                var view = (SourceGrid.Cells.Views.Header)resolved.View;
                Assert.That(view.Background, Is.TypeOf<DevAge.Drawing.VisualElements.Header>());
                var background = (DevAge.Drawing.VisualElements.Header)view.Background;
                Assert.That(
                    background.BackgroundColorStyle,
                    Is.EqualTo(DevAge.Drawing.BackgroundColorStyle.Solid));
                Assert.That(background.BackColor, Is.EqualTo(theme.Colors.SurfaceSecondary));
                Assert.That(background.Border.Top.Color, Is.EqualTo(theme.Colors.Border));
                Assert.That(view.ForeColor, Is.EqualTo(theme.Colors.Text));
                Assert.That(view.Font, Is.Null);
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void GetCell_DoesNotReplaceCustomGenericHeaderView()
    {
        using (var grid = new BootstrapSourceGridControl())
        {
            grid.Redim(1, 1);
            var customView = new SourceGrid.Cells.Views.Header();
            var header = new SourceGrid.Cells.Header(string.Empty) { View = customView };
            grid[0, 0] = header;

            Assert.That(grid.GetCell(0, 0).View, Is.SameAs(customView));
        }
    }

    [Test]
    public void GetCell_ReplacesDefaultColumnHeaderViewAndPreservesSortingContracts()
    {
        using (var grid = new BootstrapSourceGridControl())
        {
            grid.Redim(1, 1);
            var header = new SourceGrid.Cells.ColumnHeader("Name");
            grid[0, 0] = header;

            var resolved = grid.GetCell(0, 0);

            Assert.That(resolved.View, Is.Not.SameAs(SourceGrid.Cells.Views.ColumnHeader.Default));
            Assert.That(resolved.View, Is.InstanceOf<SourceGrid.Cells.Views.ColumnHeader>());
            var view = (SourceGrid.Cells.Views.ColumnHeader)resolved.View;
            Assert.That(view.Background, Is.TypeOf<DevAge.Drawing.VisualElements.ColumnHeader>());
            Assert.That(
                ((DevAge.Drawing.VisualElements.ColumnHeader)view.Background).BackgroundColorStyle,
                Is.EqualTo(DevAge.Drawing.BackgroundColorStyle.Solid));
            Assert.That(
                header.Model.FindModel(typeof(SourceGrid.Cells.Models.SortableHeader)),
                Is.Not.Null);
            Assert.That(
                header.FindController(typeof(SourceGrid.Cells.Controllers.SortableHeader)),
                Is.SameAs(SourceGrid.Cells.Controllers.SortableHeader.Default));
        }
    }

    [Test]
    public void GetCell_DoesNotReplaceCustomColumnHeaderView()
    {
        using (var grid = new BootstrapSourceGridControl())
        {
            grid.Redim(1, 1);
            var customView = new SourceGrid.Cells.Views.ColumnHeader();
            var header = new SourceGrid.Cells.ColumnHeader("Name") { View = customView };
            grid[0, 0] = header;

            Assert.That(grid.GetCell(0, 0).View, Is.SameAs(customView));
        }
    }

    [Test]
    public void GetCell_ReplacesDefaultRowHeaderView()
    {
        using (var grid = new BootstrapSourceGridControl())
        {
            grid.Redim(1, 1);
            grid[0, 0] = new SourceGrid.Cells.RowHeader("1");

            var resolved = grid.GetCell(0, 0);

            Assert.That(resolved.View, Is.Not.SameAs(SourceGrid.Cells.Views.RowHeader.Default));
            Assert.That(resolved.View, Is.InstanceOf<SourceGrid.Cells.Views.RowHeader>());
            var view = (SourceGrid.Cells.Views.RowHeader)resolved.View;
            Assert.That(view.Background, Is.TypeOf<DevAge.Drawing.VisualElements.RowHeader>());
            Assert.That(
                ((DevAge.Drawing.VisualElements.RowHeader)view.Background).BackgroundColorStyle,
                Is.EqualTo(DevAge.Drawing.BackgroundColorStyle.Solid));
        }
    }

    [Test]
    public void GetCell_DoesNotReplaceCustomRowHeaderView()
    {
        using (var grid = new BootstrapSourceGridControl())
        {
            grid.Redim(1, 1);
            var customView = new SourceGrid.Cells.Views.RowHeader();
            var header = new SourceGrid.Cells.RowHeader("1") { View = customView };
            grid[0, 0] = header;

            Assert.That(grid.GetCell(0, 0).View, Is.SameAs(customView));
        }
    }
}
