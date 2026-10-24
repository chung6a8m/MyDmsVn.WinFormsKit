using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using MyDmsVn.BootstrapSourceGrid.Editors;
using NUnit.Framework;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapSourceGridDemoTests
{
    [Test]
    public void MainFormExposesTheCompleteMvpReviewSurface()
    {
        using (var form = new MyDmsVn.BootstrapSourceGrid.Demo.MainForm())
        {
            var grid = FindControl<BootstrapSourceGridControl>(form, "BootstrapSourceGrid");
            var help = FindControl<Label>(form, "interactionHelp");

            Assert.That(FindControl<Button>(form, "lightThemeButton"), Is.Not.Null);
            Assert.That(FindControl<Button>(form, "darkThemeButton"), Is.Not.Null);
            Assert.That(FindControl<Button>(form, "resetGridButton"), Is.Not.Null);
            Assert.That(FindControl<Button>(form, "consumerFontButton"), Is.Not.Null);
            Assert.That(help.Text, Does.Contain("Shift+Tab"));
            Assert.That(help.Text, Does.Contain("PageUp/PageDown"));
            Assert.That(help.Text, Does.Contain("Esc"));
            Assert.That(help.Text, Does.Contain("sort"));

            Assert.That(grid.RowsCount, Is.GreaterThanOrEqualTo(32));
            Assert.That(grid.ColumnsCount, Is.GreaterThanOrEqualTo(10));
            Assert.That(grid.Selection.EnableMultiSelection, Is.True);
            Assert.That(((SourceGrid.Cells.ColumnHeader)grid[0, 1]).AutomaticSortEnabled, Is.True);
            Assert.That(((SourceGrid.Cells.Cell)grid[1, 7]).Editor, Is.Null);
            Assert.That(((SourceGrid.Cells.Cell)grid[1, 9]).Editor!.EnableEdit, Is.False);
            Assert.That(grid[3, 8].ColumnSpan, Is.EqualTo(2));
            Assert.That(grid[2, 9].View, Is.Not.SameAs(SourceGrid.Cells.Views.Cell.Default));
        }
    }

    [Test]
    public void MainFormUsesOneConfiguredBootstrapEditorPerDemoColumn()
    {
        using (var form = new MyDmsVn.BootstrapSourceGrid.Demo.MainForm())
        {
            var grid = FindControl<BootstrapSourceGridControl>(form, "BootstrapSourceGrid");
            var help = FindControl<Label>(form, "interactionHelp");
            var text = (BootstrapTextBoxEditor)grid[1, 2].Editor!;
            var formatted = (BootstrapFormattedTextBoxEditor)grid[1, 3].Editor!;
            var lookup = (BootstrapLookupBoxEditor)grid[1, 6].Editor!;

            Assert.Multiple((Action)(() =>
            {
                Assert.That(grid[39, 2].Editor, Is.SameAs(text));
                Assert.That(grid[39, 3].Editor, Is.SameAs(formatted));
                Assert.That(grid[39, 6].Editor, Is.SameAs(lookup));
                Assert.That(text.BootstrapControl.PlaceholderText, Is.EqualTo("Customer name"));
                Assert.That(text.BootstrapControl.ShowClearButton, Is.True);
                Assert.That(formatted.BootstrapControl.FormatMode, Is.EqualTo(BootstrapInputFormatMode.Numeral));
                Assert.That(formatted.BootstrapControl.NumeralOptions.DecimalScale, Is.EqualTo(2));
                Assert.That(grid[1, 3].Value, Is.TypeOf<decimal>());
                Assert.That(lookup.BootstrapControl.DataSource, Is.Not.Null);
                Assert.That(lookup.BootstrapControl.DisplayMember, Is.EqualTo("Name"));
                Assert.That(lookup.BootstrapControl.ValueMember, Is.EqualTo("Id"));
                Assert.That(lookup.BootstrapControl.Columns, Has.Count.EqualTo(3));
                Assert.That(lookup.BootstrapControl.SearchMembers, Is.EquivalentTo(new[] { "Name", "Region" }));
                Assert.That(grid[1, 6].Value, Is.TypeOf<int>());
                Assert.That(help.Text, Does.Contain("shared per column/configuration"));
                Assert.That(help.Text, Does.Contain("outside click"));
                Assert.That(help.Text, Does.Contain("deactivation"));
            }));
        }
    }

    [Test]
    public void ThemeSwitchPreservesTheGridAndConsumerView()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            using (var form = new MyDmsVn.BootstrapSourceGrid.Demo.MainForm())
            {
                ShowForm(form);
                var grid = FindControl<BootstrapSourceGridControl>(form, "BootstrapSourceGrid");
                var consumerView = grid[2, 9].View;

                FindControl<Button>(form, "darkThemeButton").PerformClick();

                Assert.That(FindControl<BootstrapSourceGridControl>(form, "BootstrapSourceGrid"), Is.SameAs(grid));
                Assert.That(grid[2, 9].View, Is.SameAs(consumerView));
                Assert.That(BootstrapThemeManager.CurrentTheme.Mode, Is.EqualTo(BootstrapThemeMode.Dark));
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [Test]
    public void ResetRepopulatesDataWithoutReplacingTheGrid()
    {
        using (var form = new MyDmsVn.BootstrapSourceGrid.Demo.MainForm())
        {
            ShowForm(form);
            var grid = FindControl<BootstrapSourceGridControl>(form, "BootstrapSourceGrid");
            grid[1, 1].Value = "Changed";

            FindControl<Button>(form, "resetGridButton").PerformClick();

            Assert.That(FindControl<BootstrapSourceGridControl>(form, "BootstrapSourceGrid"), Is.SameAs(grid));
            Assert.That(grid[1, 1].Value, Is.EqualTo("Item 01"));
        }
    }

    [Test]
    public void ResetReusesTheExistingSharedBootstrapEditors()
    {
        using (var form = new MyDmsVn.BootstrapSourceGrid.Demo.MainForm())
        {
            ShowForm(form);
            var grid = FindControl<BootstrapSourceGridControl>(form, "BootstrapSourceGrid");
            var text = grid[1, 2].Editor;
            var formatted = grid[1, 3].Editor;
            var lookup = grid[1, 6].Editor;

            FindControl<Button>(form, "resetGridButton").PerformClick();

            Assert.Multiple((Action)(() =>
            {
                Assert.That(grid[1, 2].Editor, Is.SameAs(text));
                Assert.That(grid[1, 3].Editor, Is.SameAs(formatted));
                Assert.That(grid[1, 6].Editor, Is.SameAs(lookup));
            }));
        }
    }

    [Test]
    public void ResetAfterSortingSpannedRowsRepopulatesWithoutAnOverlap()
    {
        using var form = new MyDmsVn.BootstrapSourceGrid.Demo.MainForm();
        ShowForm(form);
        var grid = FindControl<BootstrapSourceGridControl>(form, "BootstrapSourceGrid");
        var text = grid[1, 2].Editor;
        var formatted = grid[1, 3].Editor;
        var lookup = grid[1, 6].Editor;

        ((SourceGrid.Cells.ColumnHeader)grid[0, 6]).Sort(true);
        FindControl<Button>(form, "resetGridButton").PerformClick();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(FindControl<BootstrapSourceGridControl>(form, "BootstrapSourceGrid"), Is.SameAs(grid));
            Assert.That(grid[1, 1].Value, Is.EqualTo("Item 01"));
            Assert.That(grid[3, 8].ColumnSpan, Is.EqualTo(2));
            Assert.That(grid[1, 2].Editor, Is.SameAs(text));
            Assert.That(grid[1, 3].Editor, Is.SameAs(formatted));
            Assert.That(grid[1, 6].Editor, Is.SameAs(lookup));
        }));
    }

    [Test]
    public void LookupDisplayColumnTracksOnlyTheCommittedLogicalValue()
    {
        using (var form = new MyDmsVn.BootstrapSourceGrid.Demo.MainForm())
        {
            ShowForm(form);
            var grid = FindControl<BootstrapSourceGridControl>(form, "BootstrapSourceGrid");
            var position = new SourceGrid.Position(1, 6);
            var lookupCell = (SourceGrid.Cells.Cell)grid[position];
            var displayCell = (SourceGrid.Cells.Cell)grid[1, 7];
            var editor = (BootstrapLookupBoxEditor)lookupCell.Editor!;

            Assert.Multiple((Action)(() =>
            {
                Assert.That(((SourceGrid.Cells.ColumnHeader)grid[0, 7]).Value, Is.EqualTo("Lookup display"));
                Assert.That(lookupCell.Value, Is.EqualTo(1));
                Assert.That(displayCell.Value, Is.EqualTo("Northwind Traders"));
                Assert.That(displayCell.Editor, Is.Null);
            }));

            Assert.That(grid.Selection.Focus(position, true), Is.True);
            var context = new SourceGrid.CellContext(grid, position, lookupCell);
            grid.GetCell(position).View.Measure(context, Size.Empty);
            context.StartEdit();
            editor.BootstrapControl.SelectValue(3);

            Assert.That(displayCell.Value, Is.EqualTo("Northwind Traders"));
            Assert.That(context.EndEdit(false), Is.True);
            Assert.That(lookupCell.Value, Is.EqualTo(3));
            Assert.That(displayCell.Value, Is.EqualTo("Adventure Works"));
        }
    }

    [Test]
    public void DiagnosticsRefreshAfterSourceGridCommitsTheEditedValue()
    {
        using (var form = new MyDmsVn.BootstrapSourceGrid.Demo.MainForm())
        {
            ShowForm(form);
            var grid = FindControl<BootstrapSourceGridControl>(form, "BootstrapSourceGrid");
            var diagnostics = FindControl<Label>(form, "diagnosticsLabel");
            var position = new SourceGrid.Position(1, 2);
            var cell = (SourceGrid.Cells.Cell)grid[position];
            var editor = (BootstrapTextBoxEditor)cell.Editor!;
            Assert.That(grid.Selection.Focus(position, true), Is.True);
            var context = new SourceGrid.CellContext(grid, position, cell);
            grid.GetCell(position).View.Measure(context, Size.Empty);
            context.StartEdit();
            editor.BootstrapControl.Text = "Committed diagnostics";

            Assert.That(context.EndEdit(false), Is.True);
            Application.DoEvents();

            Assert.That(cell.Value, Is.EqualTo("Committed diagnostics"));
            Assert.That(diagnostics.Text, Does.Contain("Committed diagnostics (String)"));
        }
    }

    [Test]
    public void ConsumerFontButtonOptsOutOfLaterThemeFontReplacement()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            using (var form = new MyDmsVn.BootstrapSourceGrid.Demo.MainForm())
            {
                ShowForm(form);
                var grid = FindControl<BootstrapSourceGridControl>(form, "BootstrapSourceGrid");
                FindControl<Button>(form, "consumerFontButton").PerformClick();
                var consumerFont = grid.Font;

                BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

                Assert.That(grid.Font, Is.SameAs(consumerFont));
                Assert.That(grid.Font.FontFamily.Name, Is.EqualTo(FontFamily.GenericMonospace.Name));
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    private static T FindControl<T>(Control root, string name)
        where T : Control
    {
        var controls = root.Controls.Find(name, true);
        Assert.That(controls, Has.Length.EqualTo(1), $"Expected one control named '{name}'.");
        return (T)controls[0];
    }

    private static void ShowForm(Form form)
    {
        form.Show();
        Application.DoEvents();
    }
}
