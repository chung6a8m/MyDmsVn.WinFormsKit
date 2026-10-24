using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapSourceGridThemeStressTests
{
    [OneTimeSetUp]
    public void ConfigureWinForms()
    {
        WinFormsTestGuard.Configure();
    }

    [Test]
    public void RepeatedThemeChangesPreserveApplicationOwnedGridState()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        var light = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

        try
        {
            BootstrapThemeManager.CurrentTheme = light;
            using (var consumerFont = new Font(SystemFonts.DefaultFont.FontFamily, 11f))
            using (var form = new Form())
            using (var grid = new BootstrapSourceGridControl())
            {
                grid.Redim(6, 5);
                var ordinary = new SourceGrid.Cells.Cell("ordinary");
                var customView = new SourceGrid.Cells.Views.Cell();
                var custom = new SourceGrid.Cells.Cell("custom") { View = customView };
                var spanned = new SourceGrid.Cells.Cell("spanned")
                {
                    RowSpan = 2,
                    ColumnSpan = 2,
                };

                grid[1, 1] = ordinary;
                grid[2, 3] = custom;
                grid[3, 1] = spanned;
                grid.Rows[1].Height = 37;
                grid.Columns[1].Width = 123;
                grid.Font = consumerFont;

                form.Controls.Add(grid);
                form.Show();
                var active = new SourceGrid.Position(1, 1);
                var selectedRange = new SourceGrid.Range(1, 1, 2, 2);
                Assert.That(grid.Selection.Focus(active, true), Is.True);
                grid.Selection.SelectRange(selectedRange, true);
                var selectedBefore = grid.Selection.GetSelectionRegion().GetCellsPositions();
                var spanBefore = grid.PositionToCellRange(new SourceGrid.Position(4, 2));

                for (var iteration = 0; iteration < 20; iteration++)
                {
                    BootstrapThemeManager.CurrentTheme = (iteration & 1) == 0 ? dark : light;

                    Assert.That(grid.RowsCount, Is.EqualTo(6));
                    Assert.That(grid.ColumnsCount, Is.EqualTo(5));
                    Assert.That(ordinary.Value, Is.EqualTo("ordinary"));
                    Assert.That(custom.Value, Is.EqualTo("custom"));
                    Assert.That(spanned.Value, Is.EqualTo("spanned"));
                    Assert.That(grid.Rows[1].Height, Is.EqualTo(37));
                    Assert.That(grid.Columns[1].Width, Is.EqualTo(123));
                    Assert.That(grid.Font, Is.SameAs(consumerFont));
                    Assert.That(grid.GetCell(2, 3).View, Is.SameAs(customView));
                    Assert.That(grid.GetCell(4, 2), Is.SameAs(spanned));
                    Assert.That(
                        grid.PositionToCellRange(new SourceGrid.Position(4, 2)),
                        Is.EqualTo(spanBefore));
                    Assert.That(grid.Selection.ActivePosition, Is.EqualTo(active));
                    AssertSelectedPositionsEqual(grid, selectedBefore);
                }

                form.Close();
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    private static void AssertSelectedPositionsEqual(
        BootstrapSourceGridControl grid,
        SourceGrid.PositionCollection expected)
    {
        var actual = grid.Selection.GetSelectionRegion().GetCellsPositions();
        Assert.That(actual.Count, Is.EqualTo(expected.Count));
        for (var index = 0; index < expected.Count; index++)
        {
            Assert.That(actual[index], Is.EqualTo(expected[index]));
        }
    }
}
