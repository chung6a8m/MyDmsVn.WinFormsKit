using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapSourceGridSelectionStyleTests
{
    [OneTimeSetUp]
    public void ConfigureWinForms()
    {
        WinFormsTestGuard.Configure();
    }

    [Test]
    public void ConstructionAppliesBootstrapSelectionColors()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        var theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

        try
        {
            BootstrapThemeManager.CurrentTheme = theme;
            using (var grid = new BootstrapSourceGridControl())
            {
                var selection = SelectionOf(grid);
                Assert.That(
                    selection.BackColor,
                    Is.EqualTo(Color.FromArgb(75, theme.Colors.Primary)));
                Assert.That(selection.FocusBackColor, Is.EqualTo(Color.Transparent));
                Assert.That(selection.Border.Top.Color, Is.EqualTo(theme.Colors.Focus));
                Assert.That(
                    selection.Border.Top.Width,
                    Is.EqualTo(DpiScaler.Scale(theme.Metrics.FocusBorderWidth, grid.DeviceDpi)));
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void ThemeChangeUpdatesIntegrationOwnedSelectionStyle()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        var light = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

        try
        {
            BootstrapThemeManager.CurrentTheme = light;
            using (var grid = new BootstrapSourceGridControl())
            {
                var selection = SelectionOf(grid);
                BootstrapThemeManager.CurrentTheme = dark;

                Assert.That(
                    selection.BackColor,
                    Is.EqualTo(Color.FromArgb(75, dark.Colors.Primary)));
                Assert.That(selection.Border.Top.Color, Is.EqualTo(dark.Colors.Focus));
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void ConsumerChangedSelectionBackColorIsNotOverwrittenLater()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        var light = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        var consumerColor = Color.Magenta;

        try
        {
            BootstrapThemeManager.CurrentTheme = light;
            using (var grid = new BootstrapSourceGridControl())
            {
                var selection = SelectionOf(grid);
                selection.BackColor = consumerColor;

                BootstrapThemeManager.CurrentTheme = dark;

                Assert.That(selection.BackColor, Is.EqualTo(consumerColor));
                Assert.That(selection.Border.Top.Color, Is.EqualTo(dark.Colors.Focus));
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void ConsumerChangedFocusAndBorderAreNotOverwrittenLater()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        var light = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        var consumerFocus = Color.FromArgb(40, Color.Yellow);
        var consumerBorder = new DevAge.Drawing.RectangleBorder(
            new DevAge.Drawing.BorderLine(Color.Lime, 7));

        try
        {
            BootstrapThemeManager.CurrentTheme = light;
            using (var grid = new BootstrapSourceGridControl())
            {
                var selection = SelectionOf(grid);
                selection.FocusBackColor = consumerFocus;
                selection.Border = consumerBorder;

                BootstrapThemeManager.CurrentTheme = dark;

                Assert.That(selection.FocusBackColor, Is.EqualTo(consumerFocus));
                Assert.That(selection.Border, Is.EqualTo(consumerBorder));
                Assert.That(
                    selection.BackColor,
                    Is.EqualTo(Color.FromArgb(75, dark.Colors.Primary)));
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void SelectionModeChangeImmediatelyStylesRecreatedSelection()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        var light = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

        try
        {
            BootstrapThemeManager.CurrentTheme = light;
            using (var grid = new BootstrapSourceGridControl())
            {
                var originalSelection = grid.Selection;
                var decoratorCount = grid.Decorators.Count;
                grid.SelectionMode = SourceGrid.GridSelectionMode.Row;
                Assert.That(grid.Selection, Is.Not.SameAs(originalSelection));
                Assert.That(grid.Decorators.Count, Is.EqualTo(decoratorCount));

                var selection = SelectionOf(grid);
                Assert.That(
                    selection.BackColor,
                    Is.EqualTo(Color.FromArgb(75, light.Colors.Primary)));
                Assert.That(selection.Border.Top.Color, Is.EqualTo(light.Colors.Focus));

                BootstrapThemeManager.CurrentTheme = dark;

                Assert.That(
                    selection.BackColor,
                    Is.EqualTo(Color.FromArgb(75, dark.Colors.Primary)));
                Assert.That(selection.Border.Top.Color, Is.EqualTo(dark.Colors.Focus));
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void ThemeChangePreservesActivePositionAndSelectedRanges()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        var light = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

        try
        {
            BootstrapThemeManager.CurrentTheme = light;
            using (var form = new Form())
            {
                var grid = CreatePopulatedGrid();
                form.Controls.Add(grid);
                form.Show();
                var active = new SourceGrid.Position(1, 1);
                var selected = new SourceGrid.Range(1, 0, 2, 2);
                Assert.That(grid.Selection.Focus(active, true), Is.True);
                grid.Selection.SelectRange(selected, true);
                var before = grid.Selection.GetSelectionRegion().GetCellsPositions();

                BootstrapThemeManager.CurrentTheme = dark;

                var after = grid.Selection.GetSelectionRegion().GetCellsPositions();
                Assert.That(grid.Selection.ActivePosition, Is.EqualTo(active));
                Assert.That(after.Count, Is.EqualTo(before.Count));
                for (var index = 0; index < before.Count; index++)
                {
                    Assert.That(after[index], Is.EqualTo(before[index]));
                }

                form.Close();
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    private static BootstrapSourceGridControl CreatePopulatedGrid()
    {
        var grid = new BootstrapSourceGridControl();
        grid.Redim(3, 3);
        for (var row = 0; row < 3; row++)
        {
            for (var column = 0; column < 3; column++)
            {
                grid[row, column] = new SourceGrid.Cells.Cell($"{row},{column}");
            }
        }

        return grid;
    }

    private static SourceGrid.Selection.SelectionBase SelectionOf(BootstrapSourceGridControl grid)
    {
        return (SourceGrid.Selection.SelectionBase)grid.Selection;
    }
}
