using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using MyDmsVn.BootstrapSourceGrid.Tests.EditorProbes;
using MyDmsVn.BootstrapSourceGrid.Views;
using NUnit.Framework;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapSourceGridEditorTests
{
    [OneTimeSetUp]
    public void ConfigureWinForms()
    {
        WinFormsTestGuard.Configure();
    }

    [Test]
    public void DefaultTextEditorUsesResolvedBootstrapCellViewAppearance()
    {
        using (var form = new Form())
        using (var grid = new BootstrapSourceGridControl())
        {
            var cell = new SourceGrid.Cells.Cell("before", typeof(string));
            grid.Redim(1, 1);
            grid[0, 0] = cell;
            form.Controls.Add(grid);
            form.Show();

            var resolvedCell = grid.GetCell(0, 0);
            Assert.That(resolvedCell.View, Is.TypeOf<BootstrapSourceGridCellView>());

            var context = new SourceGrid.CellContext(
                grid,
                new SourceGrid.Position(0, 0),
                cell);
            cell.View.Measure(context, Size.Empty);
            context.StartEdit();

            try
            {
                var editor = (SourceGrid.Cells.Editors.EditorControlBase)cell.Editor;
                Assert.That(editor.IsEditing, Is.True);
                Assert.That(editor.UseCellViewProperties, Is.True);
                Assert.That(editor.Control.BackColor, Is.EqualTo(cell.View.BackColor));
                Assert.That(editor.Control.ForeColor, Is.EqualTo(cell.View.ForeColor));
                Assert.That(editor.Control.Font, Is.EqualTo(grid.Font));
            }
            finally
            {
                context.EndEdit(true);
                form.Close();
            }
        }
    }

    [Test]
    public void ThemeChangeRefreshesActiveEditorWithoutChangingEditStateOrValue()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        var light = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

        try
        {
            BootstrapThemeManager.CurrentTheme = light;
            using (var form = new Form())
            using (var grid = new BootstrapSourceGridControl())
            {
                var cell = new SourceGrid.Cells.Cell("before", typeof(string));
                grid.Redim(1, 1);
                grid[0, 0] = cell;
                form.Controls.Add(grid);
                form.Show();

                var position = new SourceGrid.Position(0, 0);
                var context = new SourceGrid.CellContext(grid, position, cell);
                grid.GetCell(position).View.Measure(context, Size.Empty);
                context.StartEdit();
                var editor = (SourceGrid.Cells.Editors.TextBox)cell.Editor;

                try
                {
                    editor.SetEditValue("pending");
                    BootstrapThemeManager.CurrentTheme = dark;

                    Assert.That(cell.Editor, Is.SameAs(editor));
                    Assert.That(editor.IsEditing, Is.True);
                    Assert.That(editor.GetEditedValue(), Is.EqualTo("pending"));
                    Assert.That(cell.Value, Is.EqualTo("before"));
                    Assert.That(editor.Control.BackColor, Is.EqualTo(dark.Colors.Surface));
                    Assert.That(editor.Control.ForeColor, Is.EqualTo(dark.Colors.Text));
                    Assert.That(editor.Control.Font, Is.EqualTo(grid.Font));
                    Assert.That(grid.Selection.ActivePosition, Is.EqualTo(position));
                }
                finally
                {
                    context.EndEdit(true);
                    form.Close();
                }
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [Test]
    public void ThemeChangePreservesActiveEditorAppearanceWhenViewPropertiesAreDisabled()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        var light = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        var customBackColor = Color.MediumPurple;
        var customForeColor = Color.Honeydew;

        try
        {
            BootstrapThemeManager.CurrentTheme = light;
            using (var form = new Form())
            using (var grid = new BootstrapSourceGridControl())
            {
                var cell = new SourceGrid.Cells.Cell("before", typeof(string));
                var editor = (SourceGrid.Cells.Editors.TextBox)cell.Editor;
                editor.UseCellViewProperties = false;
                editor.Control.BackColor = customBackColor;
                editor.Control.ForeColor = customForeColor;
                grid.Redim(1, 1);
                grid[0, 0] = cell;
                form.Controls.Add(grid);
                form.Show();

                var context = new SourceGrid.CellContext(
                    grid,
                    new SourceGrid.Position(0, 0),
                    cell);
                grid.GetCell(0, 0).View.Measure(context, Size.Empty);
                context.StartEdit();

                try
                {
                    BootstrapThemeManager.CurrentTheme = dark;

                    Assert.That(editor.IsEditing, Is.True);
                    Assert.That(editor.Control.BackColor, Is.EqualTo(customBackColor));
                    Assert.That(editor.Control.ForeColor, Is.EqualTo(customForeColor));
                }
                finally
                {
                    context.EndEdit(true);
                    form.Close();
                }
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [Test]
    public void ThemeChangeKeepsBootstrapProbeVisualsOwnedByBootstrapControl()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        var light = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

        try
        {
            BootstrapThemeManager.CurrentTheme = light;
            using (var cellFont = new Font(SystemFonts.DefaultFont.FontFamily, 18f, FontStyle.Bold))
            using (var form = new Form())
            using (var grid = new BootstrapSourceGridControl())
            {
                var editor = grid.EditorRegistry.Register(new BootstrapTextBoxProbeEditor());
                var customView = new SourceGrid.Cells.Views.Cell
                {
                    BackColor = Color.MediumPurple,
                    ForeColor = Color.Honeydew,
                    Font = cellFont,
                };
                var cell = new SourceGrid.Cells.Cell("before")
                {
                    Editor = editor,
                    View = customView,
                };
                grid.Redim(1, 1);
                grid[0, 0] = cell;
                form.Controls.Add(grid);
                form.Show();

                var context = new SourceGrid.CellContext(
                    grid,
                    new SourceGrid.Position(0, 0),
                    cell);
                customView.Measure(context, Size.Empty);
                context.StartEdit();

                try
                {
                    BootstrapThemeManager.CurrentTheme = dark;

                    Assert.That(editor.UseCellViewProperties, Is.False);
                    Assert.That(editor.BootstrapControl.BackColor, Is.Not.EqualTo(customView.BackColor));
                    Assert.That(editor.BootstrapControl.ForeColor, Is.Not.EqualTo(customView.ForeColor));
                    Assert.That(editor.BootstrapControl.Font, Is.Not.SameAs(cellFont));
                    Assert.That(editor.BootstrapControl.Font.Name, Is.EqualTo(dark.Typography.Body.FontFamilyName));
                    Assert.That(editor.BootstrapControl.Font.SizeInPoints, Is.EqualTo(dark.Typography.Body.SizeInPoints).Within(0.01f));
                }
                finally
                {
                    context.EndEdit(true);
                    form.Close();
                }
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [Test]
    public void FactoryTextEditorSupportsAppearanceCommitAndCancel()
    {
        AssertEditorLifecycle(
            new SourceGrid.Cells.Cell("before", typeof(string)),
            "after",
            "cancelled",
            typeof(SourceGrid.Cells.Editors.TextBox));
    }

    [Test]
    public void FactoryNumericEditorSupportsAppearanceCommitAndCancel()
    {
        AssertEditorLifecycle(
            new SourceGrid.Cells.Cell(10, typeof(int)),
            20,
            30,
            typeof(SourceGrid.Cells.Editors.TextBox));
    }

    [Test]
    public void FactoryDateTimeEditorSupportsAppearanceCommitAndCancel()
    {
        AssertEditorLifecycle(
            new SourceGrid.Cells.Cell(new System.DateTime(2026, 9, 7), typeof(System.DateTime)),
            new System.DateTime(2026, 9, 8),
            new System.DateTime(2026, 9, 9),
            typeof(SourceGrid.Cells.Editors.TextBoxUITypeEditor));
    }

    [Test]
    public void FactoryBooleanEditorSupportsAppearanceCommitAndCancel()
    {
        AssertEditorLifecycle(
            new SourceGrid.Cells.Cell(false, typeof(bool)),
            true,
            false,
            typeof(SourceGrid.Cells.Editors.ComboBox));
    }

    [Test]
    public void FactoryEnumEditorSupportsAppearanceCommitAndCancel()
    {
        AssertEditorLifecycle(
            new SourceGrid.Cells.Cell(EditorChoice.Alpha, typeof(EditorChoice)),
            EditorChoice.Beta,
            EditorChoice.Alpha,
            typeof(SourceGrid.Cells.Editors.ComboBox));
    }

    [Test]
    public void DateTimePickerEditorSupportsAppearanceCommitAndCancel()
    {
        AssertEditorLifecycle(
            new SourceGrid.Cells.Cell(
                new System.DateTime(2026, 9, 7),
                new SourceGrid.Cells.Editors.DateTimePicker()),
            new System.DateTime(2026, 9, 8),
            new System.DateTime(2026, 9, 9),
            typeof(SourceGrid.Cells.Editors.DateTimePicker));
    }

    private static void AssertEditorLifecycle(
        SourceGrid.Cells.Cell cell,
        object committedValue,
        object cancelledValue,
        System.Type expectedEditorType)
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        var light = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        try
        {
            BootstrapThemeManager.CurrentTheme = light;
            using (var form = new Form())
            using (var grid = new BootstrapSourceGridControl())
            {
                grid.Redim(1, 1);
                grid[0, 0] = cell;
                form.Controls.Add(grid);
                form.Show();

                var context = new SourceGrid.CellContext(
                    grid,
                    new SourceGrid.Position(0, 0),
                    cell);
                grid.GetCell(0, 0).View.Measure(context, Size.Empty);
                var editor = (SourceGrid.Cells.Editors.EditorControlBase)cell.Editor;
                Assert.That(editor, Is.TypeOf(expectedEditorType));

                context.StartEdit();
                try
                {
                    Assert.That(editor.IsEditing, Is.True);
                    Assert.That(editor.Control.Parent, Is.SameAs(grid));
                    Assert.That(editor.Control.Visible, Is.True);
                    Assert.That(editor.Control.BackColor, Is.EqualTo(cell.View.BackColor));
                    Assert.That(editor.Control.ForeColor, Is.EqualTo(cell.View.ForeColor));
                    Assert.That(editor.Control.Font, Is.EqualTo(grid.Font));

                    BootstrapThemeManager.CurrentTheme = dark;
                    Assert.That(editor.IsEditing, Is.True);
                    Assert.That(editor.Control.BackColor, Is.EqualTo(dark.Colors.Surface));
                    Assert.That(editor.Control.ForeColor, Is.EqualTo(dark.Colors.Text));
                    Assert.That(editor.Control.Font, Is.EqualTo(grid.Font));

                    editor.SetEditValue(committedValue);
                    Assert.That(context.EndEdit(false), Is.True);
                    Assert.That(cell.Value, Is.EqualTo(committedValue));

                    context.StartEdit();
                    editor.SetEditValue(cancelledValue);
                    Assert.That(context.EndEdit(true), Is.True);
                    Assert.That(cell.Value, Is.EqualTo(committedValue));
                }
                finally
                {
                    context.EndEdit(true);
                    form.Close();
                }
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    private enum EditorChoice
    {
        Alpha,
        Beta,
    }
}
