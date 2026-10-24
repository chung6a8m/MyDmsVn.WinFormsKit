using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.BootstrapSourceGrid.Editors;
using NUnit.Framework;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapLookupBoxEditorTests
{
    [OneTimeSetUp]
    public void ConfigureWinForms()
    {
        WinFormsTestGuard.Configure();
    }

    [Test]
    public void SetEditValueSelectsBySelectedValue()
    {
        using (var grid = new BootstrapSourceGridControl())
        {
            var editor = CreateEditor(grid, typeof(int));

            editor.SetEditValue(42);

            Assert.That(editor.BootstrapControl.SelectedValue, Is.EqualTo(42));
            Assert.That(((LookupItem)editor.BootstrapControl.SelectedItem!).Id, Is.EqualTo(42));
            Assert.That(editor.BootstrapControl.Text, Is.EqualTo("Northwind"));
        }
    }

    [Test]
    public void GetEditedValueReturnsSelectedValue()
    {
        using (var grid = new BootstrapSourceGridControl())
        {
            var editor = CreateEditor(grid, typeof(int));
            editor.BootstrapControl.SelectValue(42);

            Assert.That(editor.GetEditedValue(), Is.EqualTo(42));
        }
    }

    [Test]
    public void DisplayTextIsNotLogicalValue()
    {
        using (var grid = new BootstrapSourceGridControl())
        {
            var editor = CreateEditor(grid, typeof(int));
            editor.BootstrapControl.SelectValue(42);

            Assert.That(editor.BootstrapControl.Text, Is.EqualTo("Northwind"));
            Assert.That(editor.GetEditedValue(), Is.Not.EqualTo("Northwind"));
            Assert.That(editor.GetEditedValue(), Is.EqualTo(42));
        }
    }

    [Test]
    public void CancelRestoresOriginalSelectedValue()
    {
        using (var fixture = new LookupEditorFixture(42, 7))
        {
            var context = fixture.StartEdit(0);
            fixture.Editor.BootstrapControl.SelectValue(7);

            Assert.That(context.EndEdit(true), Is.True);
            Assert.That(fixture.Cells[0].Value, Is.EqualTo(42));
            Assert.That(fixture.Editor.BootstrapControl.SelectedValue, Is.EqualTo(42));
            Assert.That(fixture.Editor.BootstrapControl.Text, Is.EqualTo("Northwind"));
        }
    }

    [Test]
    public void CommitPassesSelectedValueThroughSourceGridConversion()
    {
        using (var fixture = new LookupEditorFixture(
            0,
            7,
            typeof(int),
            new LookupItem(0, "Original"),
            new LookupItem("42", "Northwind")))
        {
            var context = fixture.StartEdit(0);
            Assert.That(fixture.Editor.BootstrapControl.SelectValue("42"), Is.True);

            Assert.That(context.EndEdit(false), Is.True);
            Assert.That(fixture.Cells[0].Value, Is.TypeOf<int>());
            Assert.That(fixture.Cells[0].Value, Is.EqualTo(42));
        }
    }

    [Test]
    public void SendFirstCharacterCreatesLookupPendingText()
    {
        using (var fixture = new LookupEditorFixture(42, 7))
        {
            var context = fixture.StartEdit(0);

            try
            {
                fixture.Editor.SendCharToEditor('B');

                Assert.That(fixture.Editor.BootstrapControl.Text, Is.EqualTo("B"));
                Assert.That(fixture.Editor.BootstrapControl.HasPendingText, Is.True);
                Assert.That(fixture.Editor.BootstrapControl.SelectedValue, Is.EqualTo(42));
            }
            finally
            {
                context.EndEdit(true);
            }
        }
    }

    [Test]
    public void SharedEditorInitializesSelectionFromEachCell()
    {
        using (var fixture = new LookupEditorFixture(42, 42))
        {
            var firstContext = fixture.StartEdit(0);
            fixture.Editor.BootstrapControl.SelectValue(7);
            Assert.That(firstContext.EndEdit(false), Is.True);

            var secondContext = fixture.StartEdit(1);
            try
            {
                Assert.That(fixture.Editor.BootstrapControl.SelectedValue, Is.EqualTo(42));
                Assert.That(fixture.Editor.BootstrapControl.Text, Is.EqualTo("Northwind"));
                Assert.That(fixture.Cells[0].Value, Is.EqualTo(7));
                Assert.That(fixture.Cells[1].Value, Is.EqualTo(42));
                Assert.That(fixture.Cells[0].Editor, Is.SameAs(fixture.Editor));
                Assert.That(fixture.Cells[1].Editor, Is.SameAs(fixture.Editor));
            }
            finally
            {
                secondContext.EndEdit(true);
            }
        }
    }

    [Test]
    public void NullCellValueDoesNotCommitWhileEditorIsInitializing()
    {
        using (var fixture = new LookupEditorFixture(null!, 7))
        {
            var context = fixture.StartEdit(0);
            try
            {
                Assert.That(fixture.Editor.IsEditing, Is.True);
                Assert.That(fixture.Editor.BootstrapControl.SelectedValue, Is.Null);
                Assert.That(fixture.Cells[0].Value, Is.Null);
            }
            finally
            {
                context.EndEdit(true);
            }
        }
    }

    [Test]
    public void ProgrammaticClearSelectionDoesNotCommitActiveEdit()
    {
        using (var fixture = new LookupEditorFixture(
            "42",
            "7",
            typeof(string),
            new LookupItem("42", "Northwind"),
            new LookupItem("7", "Contoso")))
        {
            var context = fixture.StartEdit(0);
            fixture.Editor.BootstrapControl.ClearSelection();

            try
            {
                Assert.That(fixture.Editor.IsEditing, Is.True);
                Assert.That(fixture.Editor.BootstrapControl.SelectedValue, Is.Null);
                Assert.That(fixture.Cells[0].Value, Is.EqualTo("42"));
            }
            finally
            {
                context.EndEdit(true);
            }
        }
    }

    private static BootstrapLookupBoxEditor CreateEditor(BootstrapSourceGridControl grid, Type valueType)
    {
        var editor = grid.BootstrapEditors.CreateLookupBox(valueType);
        editor.BootstrapControl.DisplayMember = nameof(LookupItem.Name);
        editor.BootstrapControl.ValueMember = nameof(LookupItem.Id);
        editor.BootstrapControl.DataSource = new BindingList<LookupItem>
        {
            new LookupItem(42, "Northwind"),
            new LookupItem(7, "Contoso"),
        };
        editor.BootstrapControl.SearchDebounceMilliseconds = 0;
        return editor;
    }

    private sealed class LookupEditorFixture : IDisposable
    {
        internal LookupEditorFixture(
            object firstValue,
            object secondValue,
            Type? valueType = null,
            params LookupItem[] source)
        {
            Form = new Form { ClientSize = new Size(260, 100) };
            Grid = new BootstrapSourceGridControl { Dock = DockStyle.Fill };
            Editor = Grid.BootstrapEditors.CreateLookupBox(valueType ?? typeof(int));
            Editor.BootstrapControl.DisplayMember = nameof(LookupItem.Name);
            Editor.BootstrapControl.ValueMember = nameof(LookupItem.Id);
            Editor.BootstrapControl.DataSource = new BindingList<LookupItem>(
                source.Length == 0
                    ? new[] { new LookupItem(42, "Northwind"), new LookupItem(7, "Contoso") }
                    : source);
            Editor.BootstrapControl.SearchDebounceMilliseconds = 0;
            Cells = new[]
            {
                new SourceGrid.Cells.Cell(firstValue, valueType ?? typeof(int)) { Editor = Editor },
                new SourceGrid.Cells.Cell(secondValue, valueType ?? typeof(int)) { Editor = Editor },
            };
            Grid.Redim(2, 1);
            Grid.Rows[0].Height = 30;
            Grid.Rows[1].Height = 30;
            Grid[0, 0] = Cells[0];
            Grid[1, 0] = Cells[1];
            Form.Controls.Add(Grid);
            Form.Show();
            Grid.Focus();
        }

        internal Form Form { get; }

        internal BootstrapSourceGridControl Grid { get; }

        internal BootstrapLookupBoxEditor Editor { get; }

        internal SourceGrid.Cells.Cell[] Cells { get; }

        internal SourceGrid.CellContext StartEdit(int row)
        {
            var position = new SourceGrid.Position(row, 0);
            Assert.That(Grid.Selection.Focus(position, true), Is.True);
            var context = new SourceGrid.CellContext(Grid, position, Cells[row]);
            Grid.GetCell(position).View.Measure(context, Size.Empty);
            context.StartEdit();
            Assert.That(Editor.IsEditing, Is.True);
            return context;
        }

        public void Dispose()
        {
            if (Editor.IsEditing)
            {
                Editor.EditCellContext.EndEdit(true);
            }

            Form.Close();
            Form.Dispose();
            Grid.Dispose();
        }
    }

    private sealed class LookupItem
    {
        internal LookupItem(object id, string name)
        {
            Id = id;
            Name = name;
        }

        public object Id { get; }

        public string Name { get; }
    }
}
