using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapSourceGridEditLifecycleTests
{
    [OneTimeSetUp]
    public void ConfigureWinForms()
    {
        WinFormsTestGuard.Configure();
    }

    [Test]
    public void EndEditWithoutCancelCommitsTextEditorValue()
    {
        using (var fixture = new EditorFixture("before"))
        {
            fixture.Context.StartEdit();
            try
            {
                fixture.Editor.SetEditValue("after");

                Assert.That(fixture.Context.EndEdit(false), Is.True);
                Assert.That(fixture.Cell.Value, Is.EqualTo("after"));
                Assert.That(fixture.Editor.IsEditing, Is.False);
            }
            finally
            {
                fixture.Context.EndEdit(true);
            }
        }
    }

    [Test]
    public void EndEditWithCancelPreservesOriginalCellValue()
    {
        using (var fixture = new EditorFixture("before"))
        {
            fixture.Context.StartEdit();
            try
            {
                fixture.Editor.SetEditValue("after");

                Assert.That(fixture.Context.EndEdit(true), Is.True);
                Assert.That(fixture.Cell.Value, Is.EqualTo("before"));
                Assert.That(fixture.Editor.IsEditing, Is.False);
            }
            finally
            {
                fixture.Context.EndEdit(true);
            }
        }
    }

    [Test]
    public void ThemeChangeDoesNotCommitOrCancelPendingEdit()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
            using (var fixture = new EditorFixture("before"))
            {
                fixture.Context.StartEdit();
                try
                {
                    fixture.Editor.SetEditValue("pending");
                    BootstrapThemeManager.CurrentTheme =
                        BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

                    Assert.That(fixture.Editor.IsEditing, Is.True);
                    Assert.That(fixture.Editor.GetEditedValue(), Is.EqualTo("pending"));
                    Assert.That(fixture.Cell.Value, Is.EqualTo("before"));
                }
                finally
                {
                    fixture.Context.EndEdit(true);
                }
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [Test]
    public void DisabledEditorCannotEnterEditStateAfterTheming()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            using (var fixture = new EditorFixture("before"))
            {
                fixture.Editor.EnableEdit = false;

                BootstrapThemeManager.CurrentTheme =
                    BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
                fixture.Context.StartEdit();

                Assert.That(fixture.Editor.EnableEdit, Is.False);
                Assert.That(fixture.Editor.IsEditing, Is.False);
                Assert.That(fixture.Cell.Value, Is.EqualTo("before"));
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [Test]
    public void DemoSpannedCellIsExplicitlyEditable()
    {
        using (var form = new MyDmsVn.BootstrapSourceGrid.Demo.MainForm())
        {
            var controls = form.Controls.Find("BootstrapSourceGrid", true);
            Assert.That(controls, Has.Length.EqualTo(1));
            var grid = (BootstrapSourceGridControl)controls[0];
            var cell = (SourceGrid.Cells.Cell)grid[3, 8];

            Assert.That(cell.ColumnSpan, Is.EqualTo(2));
            Assert.That(cell.Editor, Is.Not.Null);
            var editableMode = cell.Editor.EditableMode;
            Assert.That(
                editableMode & SourceGrid.EditableMode.F2Key,
                Is.EqualTo(SourceGrid.EditableMode.F2Key));
            Assert.That(
                editableMode & SourceGrid.EditableMode.AnyKey,
                Is.EqualTo(SourceGrid.EditableMode.AnyKey));
            Assert.That(
                editableMode & SourceGrid.EditableMode.DoubleClick,
                Is.EqualTo(SourceGrid.EditableMode.DoubleClick));
        }
    }

    [Test]
    public void CoveredSpanF2ActivationUsesVisibleCanonicalEditorAndSupportsCommitAndCancel()
    {
        using (var fixture = new SpannedEditorFixture())
        {
            fixture.FocusThroughCoveredPortion();
            fixture.Grid.DispatchKeyDown(Keys.F2);

            AssertActiveEditor(fixture);
            fixture.Editor.SetEditValue("committed");
            Assert.That(fixture.Context.EndEdit(false), Is.True);
            Assert.That(fixture.Cell.Value, Is.EqualTo("committed"));

            fixture.FocusThroughCoveredPortion();
            fixture.Grid.DispatchKeyDown(Keys.F2);
            fixture.Editor.SetEditValue("cancelled");
            Assert.That(fixture.Context.EndEdit(true), Is.True);
            Assert.That(fixture.Cell.Value, Is.EqualTo("committed"));
        }
    }

    [Test]
    public void CoveredSpanAnyKeyActivationUsesVisibleCanonicalEditor()
    {
        using (var fixture = new SpannedEditorFixture())
        {
            fixture.FocusThroughCoveredPortion();
            fixture.Grid.DispatchKeyDown(Keys.X);
            fixture.Grid.DispatchKeyPress('x');

            AssertActiveEditor(fixture);
        }
    }

    [Test]
    public void CoveredSpanDoubleClickActivationUsesVisibleCanonicalEditor()
    {
        using (var fixture = new SpannedEditorFixture())
        {
            fixture.FocusThroughCoveredPortion();
            fixture.Grid.DispatchMouseDoubleClick(fixture.CoveredPoint);

            AssertActiveEditor(fixture);
        }
    }

    private static void AssertActiveEditor(SpannedEditorFixture fixture)
    {
        Assert.That(fixture.Editor.IsEditing, Is.True);
        Assert.That(fixture.Editor.Control.Visible, Is.True);
        Assert.That(fixture.Editor.EditPosition, Is.EqualTo(fixture.Canonical));
        Assert.That(fixture.Context.IsEditing(), Is.True);
    }

    private sealed class EditLifecycleTestGrid : BootstrapSourceGridControl
    {
        internal void DispatchKeyDown(Keys keys)
        {
            base.OnKeyDown(new KeyEventArgs(keys));
        }

        internal void DispatchKeyPress(char keyChar)
        {
            base.OnKeyPress(new KeyPressEventArgs(keyChar));
        }

        internal void DispatchMouseDown(Point point)
        {
            base.OnMouseDown(new MouseEventArgs(
                MouseButtons.Left,
                1,
                point.X,
                point.Y,
                0));
        }

        internal void DispatchMouseUp(Point point)
        {
            base.OnMouseUp(new MouseEventArgs(
                MouseButtons.Left,
                1,
                point.X,
                point.Y,
                0));
        }

        internal void DispatchMouseDoubleClick(Point point)
        {
            var originalPosition = Cursor.Position;
            try
            {
                Cursor.Position = PointToScreen(point);
                base.OnMouseDoubleClick(new MouseEventArgs(
                    MouseButtons.Left,
                    2,
                    point.X,
                    point.Y,
                    0));
            }
            finally
            {
                Cursor.Position = originalPosition;
            }
        }
    }

    private sealed class SpannedEditorFixture : System.IDisposable
    {
        internal SpannedEditorFixture()
        {
            Form = new Form
            {
                ClientSize = new Size(260, 80),
            };
            Grid = new EditLifecycleTestGrid
            {
                Dock = DockStyle.Fill,
            };
            Cell = new SourceGrid.Cells.Cell("before", typeof(string))
            {
                ColumnSpan = 2,
            };
            Grid.Redim(1, 2);
            Grid.Columns[0].Width = 100;
            Grid.Columns[1].Width = 100;
            Grid.Rows[0].Height = 30;
            Grid[0, 0] = Cell;
            Form.Controls.Add(Grid);
            Form.Show();
            Grid.Focus();

            Canonical = new SourceGrid.Position(0, 0);
            Context = new SourceGrid.CellContext(Grid, Canonical, Cell);
            Editor = (SourceGrid.Cells.Editors.TextBox)Cell.Editor;
            CoveredPoint = new Point(
                Grid.Columns.GetLeft(1) + (Grid.Columns.GetWidth(1) / 2),
                Grid.Rows.GetTop(0) + (Grid.Rows.GetHeight(0) / 2));
        }

        internal Form Form { get; }

        internal EditLifecycleTestGrid Grid { get; }

        internal SourceGrid.Cells.Cell Cell { get; }

        internal SourceGrid.Position Canonical { get; }

        internal SourceGrid.CellContext Context { get; }

        internal SourceGrid.Cells.Editors.TextBox Editor { get; }

        internal Point CoveredPoint { get; }

        internal void FocusThroughCoveredPortion()
        {
            Assert.That(Grid.PositionAtPoint(CoveredPoint), Is.EqualTo(Canonical));
            Grid.DispatchMouseDown(CoveredPoint);
            Grid.DispatchMouseUp(CoveredPoint);
            Assert.That(Grid.Selection.ActivePosition, Is.EqualTo(Canonical));
        }

        public void Dispose()
        {
            Context.EndEdit(true);
            Form.Close();
            Form.Dispose();
            Grid.Dispose();
        }
    }

    private sealed class EditorFixture : System.IDisposable
    {
        internal EditorFixture(string value)
        {
            Form = new Form();
            Grid = new BootstrapSourceGridControl();
            Cell = new SourceGrid.Cells.Cell(value, typeof(string));
            Grid.Redim(1, 1);
            Grid[0, 0] = Cell;
            Form.Controls.Add(Grid);
            Form.Show();
            Context = new SourceGrid.CellContext(
                Grid,
                new SourceGrid.Position(0, 0),
                Cell);
            Grid.GetCell(0, 0);
            Editor = (SourceGrid.Cells.Editors.TextBox)Cell.Editor;
        }

        internal Form Form { get; }

        internal BootstrapSourceGridControl Grid { get; }

        internal SourceGrid.Cells.Cell Cell { get; }

        internal SourceGrid.CellContext Context { get; }

        internal SourceGrid.Cells.Editors.TextBox Editor { get; }

        public void Dispose()
        {
            Context.EndEdit(true);
            Form.Close();
            Form.Dispose();
            Grid.Dispose();
        }
    }
}
