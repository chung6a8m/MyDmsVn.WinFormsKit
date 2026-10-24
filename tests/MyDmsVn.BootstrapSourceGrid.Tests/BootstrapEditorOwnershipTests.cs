using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.BootstrapSourceGrid.Tests.EditorProbes;
using MyDmsVn.BootstrapSourceGrid.Editors;
using NUnit.Framework;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapEditorOwnershipTests
{
    [OneTimeSetUp]
    public void ConfigureWinForms()
    {
        WinFormsTestGuard.Configure();
    }

    [Test]
    public void BootstrapControlIsCreatedBeforeAnyGridEditStarts()
    {
        var editor = new BootstrapTextBoxProbeEditor();
        try
        {
            Assert.That(editor.BootstrapControl, Is.Not.Null);
            Assert.That(editor.Control, Is.SameAs(editor.BootstrapControl));
            Assert.That(editor.Grid, Is.Null);
            Assert.That(editor.IsEditing, Is.False);
        }
        finally
        {
            editor.Control.Dispose();
            editor.Dispose();
        }
    }

    [Test]
    public void OneEditorEditsMultipleCellsSequentiallyInOneGrid()
    {
        using (var form = new Form())
        using (var grid = new BootstrapSourceGridControl())
        {
            var editor = new BootstrapTextBoxProbeEditor();
            var control = editor.BootstrapControl;
            var firstCell = new SourceGrid.Cells.Cell("first") { Editor = editor };
            var secondCell = new SourceGrid.Cells.Cell("second") { Editor = editor };
            grid.Redim(2, 1);
            grid.Rows[0].Height = 30;
            grid.Rows[1].Height = 30;
            grid[0, 0] = firstCell;
            grid[1, 0] = secondCell;
            form.ClientSize = new Size(240, 100);
            form.Controls.Add(grid);
            grid.Dock = DockStyle.Fill;
            form.Show();

            try
            {
                EditAndCommit(grid, firstCell, new SourceGrid.Position(0, 0), editor, "first updated");
                EditAndCommit(grid, secondCell, new SourceGrid.Position(1, 0), editor, "second updated");

                Assert.That(firstCell.Value, Is.EqualTo("first updated"));
                Assert.That(secondCell.Value, Is.EqualTo("second updated"));
                Assert.That(editor.BootstrapControl, Is.SameAs(control));
                Assert.That(editor.Grid, Is.SameAs(grid));
            }
            finally
            {
                form.Close();
                editor.Control.Dispose();
                editor.Dispose();
            }
        }
    }

    [Test]
    public void NormalEditStartSelectsAllText()
    {
        using (var form = new Form())
        using (var grid = new BootstrapSourceGridControl())
        {
            var editor = grid.EditorRegistry.Register(new BootstrapTextBoxProbeEditor());
            var cell = new SourceGrid.Cells.Cell("before") { Editor = editor };
            grid.Redim(1, 1);
            grid[0, 0] = cell;
            form.Controls.Add(grid);
            form.Show();
            var context = new SourceGrid.CellContext(grid, new SourceGrid.Position(0, 0), cell);
            grid.GetCell(0, 0).View.Measure(context, Size.Empty);

            context.StartEdit();

            try
            {
                Assert.That(editor.SelectionStart, Is.Zero);
                Assert.That(editor.SelectionLength, Is.EqualTo("before".Length));
            }
            finally
            {
                context.EndEdit(true);
                form.Close();
            }
        }
    }

    [Test]
    public void SetEditValueSelectsAllTextBeforeControlIsShown()
    {
        var editor = new BootstrapTextBoxProbeEditor();
        try
        {
            editor.SetEditValue("replacement");

            Assert.That(editor.SelectionStart, Is.Zero);
            Assert.That(editor.SelectionLength, Is.EqualTo("replacement".Length));
        }
        finally
        {
            editor.Control.Dispose();
            editor.Dispose();
        }
    }

    [Test]
    public void TypedCharacterReplacesTextAndPlacesCaretAfterCharacter()
    {
        using (var form = new Form())
        using (var grid = new BootstrapSourceGridControl())
        {
            var editor = grid.EditorRegistry.Register(new BootstrapTextBoxProbeEditor());
            var cell = new SourceGrid.Cells.Cell("before") { Editor = editor };
            grid.Redim(1, 1);
            grid[0, 0] = cell;
            form.Controls.Add(grid);
            form.Show();
            var context = new SourceGrid.CellContext(grid, new SourceGrid.Position(0, 0), cell);
            grid.GetCell(0, 0).View.Measure(context, Size.Empty);
            context.StartEdit();

            try
            {
                editor.SendCharToEditor('x');

                Assert.That(editor.BootstrapControl.Text, Is.EqualTo("x"));
                Assert.That(editor.SelectionStart, Is.EqualTo(1));
                Assert.That(editor.SelectionLength, Is.Zero);
            }
            finally
            {
                context.EndEdit(true);
                form.Close();
            }
        }
    }

    [Test]
    public void RegisteredEditorUsedByCell_IsDisposedWithGrid()
    {
        var form = new Form();
        var grid = new BootstrapSourceGridControl();
        var editor = grid.EditorRegistry.Register(new BootstrapTextBoxProbeEditor());
        var cell = new SourceGrid.Cells.Cell("before") { Editor = editor };
        grid.Redim(1, 1);
        grid[0, 0] = cell;
        form.Controls.Add(grid);
        form.Show();

        var context = new SourceGrid.CellContext(grid, new SourceGrid.Position(0, 0), cell);
        grid.GetCell(0, 0).View.Measure(context, Size.Empty);
        context.StartEdit();
        Assert.That(context.EndEdit(true), Is.True);

        grid.Dispose();

        Assert.That(editor.IsControlDisposed, Is.True);
        Assert.That(editor.IsEditorDisposed, Is.True);
        form.Dispose();
    }

    [Test]
    public void RegisteredEditorNeverStarted_IsDisposedWithGrid()
    {
        var grid = new BootstrapSourceGridControl();
        var editor = grid.EditorRegistry.Register(new BootstrapTextBoxProbeEditor());

        Assert.That(editor.Grid, Is.Null);
        grid.Dispose();

        Assert.That(editor.IsControlDisposed, Is.True);
        Assert.That(editor.IsEditorDisposed, Is.True);
    }

    [Test]
    public void RegisteredBootstrapTextBoxEditorNeverStarted_IsDisposedWithGrid()
    {
        var grid = new BootstrapSourceGridControl();
        var editor = grid.BootstrapEditors.CreateTextBox(typeof(string));

        Assert.That(editor.Grid, Is.Null);
        grid.Dispose();

        Assert.That(editor.BootstrapControl.IsDisposed, Is.True);
    }

    [Test]
    public void RegisteredBootstrapTextBoxEditorUsedByCell_IsDisposedWithGrid()
    {
        var form = new Form();
        var grid = new BootstrapSourceGridControl();
        var editor = grid.BootstrapEditors.CreateTextBox(typeof(string));
        var cell = new SourceGrid.Cells.Cell("before") { Editor = editor };
        grid.Redim(1, 1);
        grid[0, 0] = cell;
        form.Controls.Add(grid);
        form.Show();
        Assert.That(grid.Selection.Focus(new SourceGrid.Position(0, 0), true), Is.True);
        var context = new SourceGrid.CellContext(grid, new SourceGrid.Position(0, 0), cell);
        grid.GetCell(0, 0).View.Measure(context, Size.Empty);
        context.StartEdit();
        Assert.That(context.EndEdit(true), Is.True);

        grid.Dispose();

        Assert.That(editor.BootstrapControl.IsDisposed, Is.True);
        form.Dispose();
    }

    [Test]
    public void RegisteredBootstrapFormattedTextBoxEditorNeverStarted_IsDisposedWithGrid()
    {
        var grid = new BootstrapSourceGridControl();
        var editor = grid.BootstrapEditors.CreateFormattedTextBox(typeof(string));

        Assert.That(editor.Grid, Is.Null);
        grid.Dispose();

        Assert.That(editor.BootstrapControl.IsDisposed, Is.True);
    }

    [Test]
    public void RegisteredBootstrapFormattedTextBoxEditorUsedByCell_IsDisposedWithGrid()
    {
        var form = new Form();
        var grid = new BootstrapSourceGridControl();
        var editor = grid.BootstrapEditors.CreateFormattedTextBox(typeof(string));
        var cell = new SourceGrid.Cells.Cell("before") { Editor = editor };
        grid.Redim(1, 1);
        grid[0, 0] = cell;
        form.Controls.Add(grid);
        form.Show();
        Assert.That(grid.Selection.Focus(new SourceGrid.Position(0, 0), true), Is.True);
        var context = new SourceGrid.CellContext(grid, new SourceGrid.Position(0, 0), cell);
        grid.GetCell(0, 0).View.Measure(context, Size.Empty);
        context.StartEdit();
        Assert.That(context.EndEdit(true), Is.True);

        grid.Dispose();

        Assert.That(editor.BootstrapControl.IsDisposed, Is.True);
        form.Dispose();
    }

    [Test]
    public void RegisteredBootstrapLookupBoxEditorNeverStarted_IsDisposedWithGrid()
    {
        var grid = new BootstrapSourceGridControl();
        var editor = grid.BootstrapEditors.CreateLookupBox(typeof(int));

        Assert.That(editor.Grid, Is.Null);
        grid.Dispose();

        Assert.That(editor.BootstrapControl.IsDisposed, Is.True);
    }

    [Test]
    public void RegisteredBootstrapLookupBoxEditorWithPopup_IsFullyDisposedWithGrid()
    {
        var form = new Form();
        var grid = new BootstrapSourceGridControl();
        var editor = grid.BootstrapEditors.CreateLookupBox(typeof(int));
        editor.BootstrapControl.DisplayMember = nameof(LookupItem.Name);
        editor.BootstrapControl.ValueMember = nameof(LookupItem.Id);
        editor.BootstrapControl.DataSource = new BindingList<LookupItem>
        {
            new LookupItem(42, "Northwind"),
        };
        var cell = new SourceGrid.Cells.Cell(42, typeof(int)) { Editor = editor };
        grid.Redim(1, 1);
        grid[0, 0] = cell;
        form.Controls.Add(grid);
        form.Show();
        Assert.That(grid.Selection.Focus(new SourceGrid.Position(0, 0), true), Is.True);
        var context = new SourceGrid.CellContext(grid, new SourceGrid.Position(0, 0), cell);
        grid.GetCell(0, 0).View.Measure(context, Size.Empty);
        context.StartEdit();
        editor.BootstrapControl.OpenDropDown();
        Application.DoEvents();
        var controller = GetLookupController(editor.BootstrapControl);
        var popup = GetPrivateField<Control>(controller, "_dropDown");
        editor.BootstrapControl.CloseDropDown();

        grid.Dispose();

        Assert.That(editor.BootstrapControl.IsDisposed, Is.True);
        Assert.That(popup.IsDisposed, Is.True);
        Assert.That(GetPrivateField<bool>(controller, "_messageFilterInstalled"), Is.False);
        form.Dispose();
    }

    [Test]
    public void RegisterRejectsNullEditor()
    {
        using (var grid = new BootstrapSourceGridControl())
        {
            System.Action registerNull = () =>
            {
                grid.EditorRegistry.Register<BootstrapTextBoxProbeEditor>(null!);
            };

            Assert.That(
                registerNull,
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("editor"));
        }
    }

    [Test]
    public void DisposedRegistryRejectsInternalRegistration()
    {
        using (var grid = new BootstrapSourceGridControl())
        {
            var editor = new BootstrapTextBoxProbeEditor();
            try
            {
                grid.BootstrapEditors.Dispose();

                Assert.That(
                    (System.Action)(() => grid.EditorRegistry.Register(editor)),
                    Throws.TypeOf<System.ObjectDisposedException>()
                        .With.Property("ObjectName")
                        .EqualTo(nameof(BootstrapSourceGridEditorRegistry)));
            }
            finally
            {
                editor.Control.Dispose();
                editor.Dispose();
            }
        }
    }

    [Test]
    public void LargeAssignmentDisposesUsedAndUnusedRegistryControlsExactlyOnce()
    {
        var grid = new BootstrapSourceGridControl();
        var used = grid.BootstrapEditors.CreateTextBox(typeof(string));
        var unusedFormatted = grid.BootstrapEditors.CreateFormattedTextBox(typeof(decimal));
        var unusedLookup = grid.BootstrapEditors.CreateLookupBox(typeof(int));
        var controls = new[]
        {
            used.Control,
            unusedFormatted.Control,
            unusedLookup.Control,
        };
        var disposeCounts = new int[controls.Length];
        for (var index = 0; index < controls.Length; index++)
        {
            var capturedIndex = index;
            controls[index].Disposed += (_, _) => disposeCounts[capturedIndex]++;
        }

        grid.Redim(10_000, 1);
        for (var row = 0; row < grid.RowsCount; row++)
        {
            grid[row, 0] = new SourceGrid.Cells.Cell(row.ToString()) { Editor = used };
        }

        grid.Dispose();
        grid.Dispose();

        Assert.Multiple((System.Action)(() =>
        {
            Assert.That(controls[0].IsDisposed, Is.True);
            Assert.That(controls[1].IsDisposed, Is.True);
            Assert.That(controls[2].IsDisposed, Is.True);
            Assert.That(disposeCounts, Is.EqualTo(new[] { 1, 1, 1 }));
        }));
    }

    private static void EditAndCommit(
        BootstrapSourceGridControl grid,
        SourceGrid.Cells.Cell cell,
        SourceGrid.Position position,
        BootstrapTextBoxProbeEditor editor,
        string editedValue)
    {
        var context = new SourceGrid.CellContext(grid, position, cell);
        grid.GetCell(position).View.Measure(context, Size.Empty);
        context.StartEdit();
        Assert.That(editor.IsEditing, Is.True);
        editor.BootstrapControl.Text = editedValue;
        Assert.That(context.EndEdit(false), Is.True);
        Assert.That(editor.IsEditing, Is.False);
    }

    private static object GetLookupController(
        MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapLookupBox lookup)
    {
        return typeof(MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapLookupBox)
            .GetField("_dropDownController", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(lookup)!;
    }

    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        return (T)instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(instance)!;
    }

    private sealed class LookupItem
    {
        internal LookupItem(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }

        public string Name { get; }
    }
}
