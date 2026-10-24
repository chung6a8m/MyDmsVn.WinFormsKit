using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapSourceGridRuntimeViewThemeTests
{
    [OneTimeSetUp]
    public void ConfigureWinForms()
    {
        WinFormsTestGuard.Configure();
    }

    [Test]
    public void ThemeChangeUpdatesOwnedViewsWithoutMutatingGridState()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        var light = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

        try
        {
            BootstrapThemeManager.CurrentTheme = light;
            using (var form = new Form())
            {
                var grid = new BootstrapSourceGridControl();
                grid.Redim(6, 5);
                var columnHeader = new SourceGrid.Cells.ColumnHeader("Name");
                var rowHeader = new SourceGrid.Cells.RowHeader("1");
                var ordinary = new SourceGrid.Cells.Cell("ordinary");
                var customView = new SourceGrid.Cells.Views.Cell();
                var custom = new SourceGrid.Cells.Cell("custom") { View = customView };
                var spanned = new SourceGrid.Cells.Cell("spanned")
                {
                    RowSpan = 2,
                    ColumnSpan = 2,
                };
                grid[0, 1] = columnHeader;
                grid[1, 0] = rowHeader;
                grid[1, 1] = ordinary;
                grid[2, 1] = custom;
                grid[3, 1] = spanned;

                var ordinaryView = grid.GetCell(1, 1).View;
                var columnHeaderView = grid.GetCell(0, 1).View;
                var rowHeaderView = grid.GetCell(1, 0).View;
                var spanView = grid.GetCell(3, 1).View;

                form.Controls.Add(grid);
                form.Show();
                var active = new SourceGrid.Position(1, 1);
                var selected = new SourceGrid.Range(1, 1, 2, 2);
                Assert.That(grid.Selection.Focus(active, true), Is.True);
                grid.Selection.SelectRange(selected, true);
                var selectedBefore = grid.Selection.GetSelectionRegion().GetCellsPositions();

                BootstrapThemeManager.CurrentTheme = dark;

                Assert.That(grid.GetCell(1, 1).View, Is.SameAs(ordinaryView));
                Assert.That(grid.GetCell(0, 1).View, Is.SameAs(columnHeaderView));
                Assert.That(grid.GetCell(1, 0).View, Is.SameAs(rowHeaderView));
                Assert.That(grid.GetCell(2, 1).View, Is.SameAs(customView));
                Assert.That(grid.GetCell(3, 1).View, Is.SameAs(spanView));
                Assert.That(grid.GetCell(4, 2), Is.SameAs(spanned));
                Assert.That(spanned.RowSpan, Is.EqualTo(2));
                Assert.That(spanned.ColumnSpan, Is.EqualTo(2));
                Assert.That(ordinary.Value, Is.EqualTo("ordinary"));
                Assert.That(custom.Value, Is.EqualTo("custom"));
                Assert.That(spanned.Value, Is.EqualTo("spanned"));
                Assert.That(ordinaryView.ForeColor, Is.EqualTo(dark.Colors.Text));
                AssertHeaderTheme(columnHeaderView, rowHeaderView, dark);
                AssertSelectionUnchanged(grid, active, selectedBefore);

                var newlyInserted = new SourceGrid.Cells.Cell("new");
                grid[2, 4] = newlyInserted;
                Assert.That(grid.GetCell(2, 4).View, Is.SameAs(ordinaryView));
                ((SourceGrid.Cells.Views.Cell)ordinaryView).Measure(
                    new SourceGrid.CellContext(grid, new SourceGrid.Position(2, 4)),
                    Size.Empty);
                Assert.That(
                    ((SourceGrid.Cells.Views.Cell)ordinaryView).BackColor,
                    Is.EqualTo(dark.Colors.Surface));

                form.Close();
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void ThemeChangeKeepsOneSharedViewForMaterializedCellsInLargeSparseGrid()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        var light = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

        try
        {
            BootstrapThemeManager.CurrentTheme = light;
            using (var grid = new BootstrapSourceGridControl())
            {
                grid.Redim(2000, 20);
                var positions = new[]
                {
                    new SourceGrid.Position(0, 0),
                    new SourceGrid.Position(37, 3),
                    new SourceGrid.Position(999, 10),
                    new SourceGrid.Position(1999, 19),
                };

                foreach (var position in positions)
                {
                    grid[position.Row, position.Column] = new SourceGrid.Cells.Cell(position.ToString());
                }

                var sharedView = grid.GetCell(positions[0]).View;
                BootstrapThemeManager.CurrentTheme = dark;

                foreach (var position in positions)
                {
                    Assert.That(grid.GetCell(position).View, Is.SameAs(sharedView));
                }

                Assert.That(grid.GetCell(500, 5), Is.Null);
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void GenericHeaderViewTracksLightDarkLightThemeChanges()
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
                grid[0, 0] = new SourceGrid.Cells.Header(string.Empty);

                var view = (SourceGrid.Cells.Views.Header)grid.GetCell(0, 0).View;
                AssertGenericHeaderTheme(view, light);

                BootstrapThemeManager.CurrentTheme = dark;

                Assert.That(grid.GetCell(0, 0).View, Is.SameAs(view));
                AssertGenericHeaderTheme(view, dark);

                BootstrapThemeManager.CurrentTheme = light;

                Assert.That(grid.GetCell(0, 0).View, Is.SameAs(view));
                AssertGenericHeaderTheme(view, light);
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void CustomThemeTokensFlowIntoCellsHeadersAndSelection()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        var custom = CreateCustomTheme();

        try
        {
            BootstrapThemeManager.CurrentTheme = custom;
            using (var grid = new BootstrapSourceGridControl())
            {
                grid.Redim(2, 2);
                grid[0, 0] = new SourceGrid.Cells.ColumnHeader("Header");
                grid[1, 1] = new SourceGrid.Cells.Cell("Cell");
                var headerView = (SourceGrid.Cells.Views.ColumnHeader)grid.GetCell(0, 0).View;
                var cellView = (SourceGrid.Cells.Views.Cell)grid.GetCell(1, 1).View;

                cellView.Measure(
                    new SourceGrid.CellContext(grid, new SourceGrid.Position(1, 1)),
                    Size.Empty);

                Assert.That(cellView.BackColor, Is.EqualTo(Color.Lavender));
                Assert.That(cellView.ForeColor, Is.EqualTo(Color.DarkSlateBlue));
                Assert.That(
                    ((DevAge.Drawing.VisualElements.ColumnHeader)headerView.Background).BackColor,
                    Is.EqualTo(Color.Lavender));
                var selection = (SourceGrid.Selection.SelectionBase)grid.Selection;
                Assert.That(selection.BackColor, Is.EqualTo(Color.FromArgb(75, Color.Crimson)));
                Assert.That(selection.Border.Top.Color, Is.EqualTo(Color.DarkOrange));
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    private static void AssertHeaderTheme(
        SourceGrid.Cells.Views.IView columnHeaderView,
        SourceGrid.Cells.Views.IView rowHeaderView,
        BootstrapTheme theme)
    {
        var columnBackground = (DevAge.Drawing.VisualElements.ColumnHeader)
            ((SourceGrid.Cells.Views.ColumnHeader)columnHeaderView).Background;
        var rowBackground = (DevAge.Drawing.VisualElements.RowHeader)
            ((SourceGrid.Cells.Views.RowHeader)rowHeaderView).Background;
        Assert.That(columnBackground.BackColor, Is.EqualTo(theme.Colors.SurfaceSecondary));
        Assert.That(rowBackground.BackColor, Is.EqualTo(theme.Colors.SurfaceSecondary));
        Assert.That(columnHeaderView.ForeColor, Is.EqualTo(theme.Colors.Text));
        Assert.That(rowHeaderView.ForeColor, Is.EqualTo(theme.Colors.Text));
    }

    private static void AssertGenericHeaderTheme(
        SourceGrid.Cells.Views.Header view,
        BootstrapTheme theme)
    {
        var background = (DevAge.Drawing.VisualElements.Header)view.Background;
        Assert.That(background.BackColor, Is.EqualTo(theme.Colors.SurfaceSecondary));
        Assert.That(background.Border.Top.Color, Is.EqualTo(theme.Colors.Border));
        Assert.That(view.ForeColor, Is.EqualTo(theme.Colors.Text));
    }

    private static void AssertSelectionUnchanged(
        BootstrapSourceGridControl grid,
        SourceGrid.Position active,
        SourceGrid.PositionCollection selectedBefore)
    {
        var selectedAfter = grid.Selection.GetSelectionRegion().GetCellsPositions();
        Assert.That(grid.Selection.ActivePosition, Is.EqualTo(active));
        Assert.That(selectedAfter.Count, Is.EqualTo(selectedBefore.Count));
        for (var index = 0; index < selectedBefore.Count; index++)
        {
            Assert.That(selectedAfter[index], Is.EqualTo(selectedBefore[index]));
        }
    }

    private static BootstrapTheme CreateCustomTheme()
    {
        var defaults = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var colors = defaults.Colors;
        return new BootstrapTheme(
            BootstrapThemeMode.Light,
            new BootstrapThemeColors(
                Color.Crimson,
                colors.Secondary,
                colors.Success,
                colors.Danger,
                colors.Warning,
                colors.Info,
                colors.Light,
                colors.Dark,
                Color.Cornsilk,
                Color.Cornsilk,
                Color.Lavender,
                Color.Sienna,
                Color.DarkSlateBlue,
                colors.MutedText,
                Color.Gray,
                Color.DarkOrange,
                colors.Hover,
                colors.Active),
            defaults.Metrics,
            defaults.Typography);
    }
}
