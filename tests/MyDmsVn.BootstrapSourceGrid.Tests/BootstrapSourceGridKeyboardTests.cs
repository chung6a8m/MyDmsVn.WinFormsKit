using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapSourceGridKeyboardTests
{
    [OneTimeSetUp]
    public void ConfigureWinForms()
    {
        WinFormsTestGuard.Configure();
    }

    [Test]
    public void SelectionFocusRemainsUsableAfterThemeSwitch()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            using (var fixture = new KeyboardFixture())
            {
                var active = new SourceGrid.Position(2, 1);
                Assert.That(fixture.Grid.Selection.Focus(active, true), Is.True);

                BootstrapThemeManager.CurrentTheme =
                    BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

                Assert.That(fixture.Grid.Selection.ActivePosition, Is.EqualTo(active));
                PressSpecialKey(fixture.Grid, Keys.Right);
                Assert.That(
                    fixture.Grid.Selection.ActivePosition,
                    Is.EqualTo(new SourceGrid.Position(2, 2)));
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [Test]
    public void ArrowAndTabKeysRetainSourceGridNavigation()
    {
        using (var fixture = new KeyboardFixture())
        {
            Assert.That(
                fixture.Grid.Selection.Focus(new SourceGrid.Position(2, 1), true),
                Is.True);

            PressSpecialKey(fixture.Grid, Keys.Right);
            AssertActivePosition(fixture.Grid, 2, 2);
            PressSpecialKey(fixture.Grid, Keys.Left);
            AssertActivePosition(fixture.Grid, 2, 1);
            PressSpecialKey(fixture.Grid, Keys.Down);
            AssertActivePosition(fixture.Grid, 3, 1);
            PressSpecialKey(fixture.Grid, Keys.Up);
            AssertActivePosition(fixture.Grid, 2, 1);
            PressSpecialKey(fixture.Grid, Keys.Tab);
            AssertActivePosition(fixture.Grid, 2, 2);
            PressSpecialKey(fixture.Grid, Keys.Tab | Keys.Shift);
            AssertActivePosition(fixture.Grid, 2, 1);
        }
    }

    [Test]
    public void PageKeysMoveWithinGridAndPreserveActiveColumn()
    {
        using (var fixture = new KeyboardFixture())
        {
            Assert.That(
                fixture.Grid.Selection.Focus(new SourceGrid.Position(0, 1), true),
                Is.True);

            var pageDown = PressSpecialKey(fixture.Grid, Keys.PageDown);
            var afterPageDown = fixture.Grid.Selection.ActivePosition;
            Assert.That(pageDown.Handled, Is.True);
            Assert.That(afterPageDown.Row, Is.GreaterThan(0));
            Assert.That(afterPageDown.Column, Is.EqualTo(1));

            var pageUp = PressSpecialKey(fixture.Grid, Keys.PageUp);
            var afterPageUp = fixture.Grid.Selection.ActivePosition;
            Assert.That(pageUp.Handled, Is.True);
            Assert.That(afterPageUp.Row, Is.LessThan(afterPageDown.Row));
            Assert.That(afterPageUp.Column, Is.EqualTo(1));
        }
    }

    [Test]
    public void F2StartsEditingAndEscapeCancelsThroughSourceGridControllers()
    {
        using (var fixture = new KeyboardFixture())
        {
            var position = new SourceGrid.Position(1, 1);
            Assert.That(fixture.Grid.Selection.Focus(position, true), Is.True);
            var cell = (SourceGrid.Cells.Cell)fixture.Grid.GetCell(position);
            var context = new SourceGrid.CellContext(fixture.Grid, position, cell);
            var editor = (SourceGrid.Cells.Editors.TextBox)cell.Editor;
            var f2 = new KeyEventArgs(Keys.F2);

            fixture.Grid.Controller.OnKeyDown(context, f2);
            try
            {
                Assert.That(f2.Handled, Is.True);
                Assert.That(editor.IsEditing, Is.True);
                editor.SetEditValue("pending");

                var escape = PressSpecialKey(fixture.Grid, Keys.Escape);

                Assert.That(escape.Handled, Is.True);
                Assert.That(editor.IsEditing, Is.False);
                Assert.That(cell.Model.ValueModel.GetValue(context), Is.EqualTo("1:1"));
            }
            finally
            {
                context.EndEdit(true);
            }
        }
    }

    [Test]
    public void ShiftTabMovesBackwardWithoutExtendingSelection()
    {
        using (var fixture = new KeyboardFixture())
        {
            Assert.That(
                fixture.Grid.Selection.Focus(new SourceGrid.Position(2, 1), true),
                Is.True);

            fixture.Grid.DispatchCommandKey(Keys.Shift | Keys.ShiftKey);
            Assert.That(fixture.Grid.DispatchCommandKey(Keys.Shift | Keys.Tab), Is.True);

            var active = new SourceGrid.Position(2, 0);
            var selected = fixture.Grid.Selection
                .GetSelectionRegion()
                .GetCellsPositions();
            Assert.That(fixture.Grid.Selection.ActivePosition, Is.EqualTo(active));
            Assert.That(selected.Count, Is.EqualTo(1));
            Assert.That(selected[0], Is.EqualTo(active));
        }
    }

    [Test]
    public void ShiftTabRetainsSourceGridSpecialKeyDispatch()
    {
        using (var fixture = new KeyboardFixture())
        {
            Assert.That(
                fixture.Grid.Selection.Focus(new SourceGrid.Position(2, 1), true),
                Is.True);

            Assert.That(
                fixture.Grid.DispatchCommandKey(Keys.Shift | Keys.Tab),
                Is.True);

            Assert.That(fixture.Grid.ShiftTabDispatchCount, Is.EqualTo(1));
            AssertActivePosition(fixture.Grid, 2, 0);
        }
    }

    [Test]
    public void HomeAndEndMoveToFirstAndLastFocusableVisibleCellInRow()
    {
        using (var fixture = new KeyboardFixture())
        {
            fixture.Grid.Columns[0].Visible = false;
            Assert.That(
                fixture.Grid.Selection.Focus(new SourceGrid.Position(2, 2), true),
                Is.True);

            Assert.That(fixture.Grid.DispatchCommandKey(Keys.Home), Is.True);
            AssertActivePosition(fixture.Grid, 2, 1);

            Assert.That(fixture.Grid.DispatchCommandKey(Keys.End), Is.True);
            AssertActivePosition(fixture.Grid, 2, 2);
        }
    }

    [Test]
    public void HomeAndEndRetainSourceGridSpecialKeyDispatch()
    {
        using (var fixture = new KeyboardFixture())
        {
            Assert.That(
                fixture.Grid.Selection.Focus(new SourceGrid.Position(2, 1), true),
                Is.True);

            Assert.That(fixture.Grid.DispatchCommandKey(Keys.Home), Is.True);
            Assert.That(fixture.Grid.DispatchCommandKey(Keys.End), Is.True);

            Assert.That(fixture.Grid.RowBoundaryDispatchCount, Is.EqualTo(2));
        }
    }

    [Test]
    public void EndFocusesCanonicalStartOfSpannedLastCell()
    {
        using (var fixture = new KeyboardFixture())
        {
            fixture.Grid[2, 2] = null;
            fixture.Grid[2, 1] = new SourceGrid.Cells.Cell(
                "spanned",
                typeof(string))
            {
                ColumnSpan = 2,
            };
            Assert.That(
                fixture.Grid.Selection.Focus(new SourceGrid.Position(2, 0), true),
                Is.True);

            Assert.That(fixture.Grid.DispatchCommandKey(Keys.End), Is.True);

            AssertActivePosition(fixture.Grid, 2, 1);
        }
    }

    [Test]
    public void NonEditKeyPreservesSelectionWithCoveredSpanActivePosition()
    {
        using (var fixture = new KeyboardFixture())
        {
            fixture.Grid[2, 2] = null;
            fixture.Grid[2, 1] = new SourceGrid.Cells.Cell(
                "spanned",
                typeof(string))
            {
                ColumnSpan = 2,
            };
            var covered = new SourceGrid.Position(2, 2);
            Assert.That(fixture.Grid.Selection.Focus(covered, true), Is.True);
            fixture.Grid.Selection.SelectCell(new SourceGrid.Position(1, 0), true);
            var selectedBefore = fixture.Grid.Selection
                .GetSelectionRegion()
                .GetCellsPositions();
            Assert.That(selectedBefore.Count, Is.GreaterThan(1));

            fixture.Grid.DispatchKeyDown(Keys.F5);

            var selectedAfter = fixture.Grid.Selection
                .GetSelectionRegion()
                .GetCellsPositions();
            Assert.That(fixture.Grid.Selection.ActivePosition, Is.EqualTo(covered));
            Assert.That(selectedAfter, Is.EquivalentTo(selectedBefore));
        }
    }

    [Test]
    public void ShiftTabCommitsActiveEditBeforeMovingBackward()
    {
        using (var fixture = new KeyboardFixture())
        {
            var position = new SourceGrid.Position(2, 1);
            Assert.That(fixture.Grid.Selection.Focus(position, true), Is.True);
            var cell = (SourceGrid.Cells.Cell)fixture.Grid.GetCell(position);
            var context = new SourceGrid.CellContext(fixture.Grid, position, cell);
            var editor = (SourceGrid.Cells.Editors.TextBox)cell.Editor;
            context.StartEdit();

            try
            {
                editor.SetEditValue("committed by Shift+Tab");
                fixture.Grid.DispatchCommandKey(Keys.Shift | Keys.ShiftKey);

                Assert.That(
                    fixture.Grid.DispatchCommandKey(Keys.Shift | Keys.Tab),
                    Is.True);
                Assert.That(editor.IsEditing, Is.False);
                Assert.That(cell.Value, Is.EqualTo("committed by Shift+Tab"));
                AssertActivePosition(fixture.Grid, 2, 0);
                Assert.That(
                    fixture.Grid.Selection.GetSelectionRegion().GetCellsPositions().Count,
                    Is.EqualTo(1));
            }
            finally
            {
                context.EndEdit(true);
            }
        }
    }

    [Test]
    public void HomeAndEndDoNotMoveActiveCellWhileEditing()
    {
        using (var fixture = new KeyboardFixture())
        {
            var position = new SourceGrid.Position(2, 1);
            Assert.That(fixture.Grid.Selection.Focus(position, true), Is.True);
            var context = new SourceGrid.CellContext(fixture.Grid, position);
            context.StartEdit();

            try
            {
                fixture.Grid.DispatchCommandKey(Keys.Home);
                Assert.That(fixture.Grid.Selection.ActivePosition, Is.EqualTo(position));
                Assert.That(context.IsEditing(), Is.True);

                fixture.Grid.DispatchCommandKey(Keys.End);
                Assert.That(fixture.Grid.Selection.ActivePosition, Is.EqualTo(position));
                Assert.That(context.IsEditing(), Is.True);
            }
            finally
            {
                context.EndEdit(true);
            }
        }
    }

    private static KeyEventArgs PressSpecialKey(
        BootstrapSourceGridControl grid,
        Keys keys)
    {
        var args = new KeyEventArgs(keys);
        grid.ProcessSpecialGridKey(args);
        return args;
    }

    private static void AssertActivePosition(
        BootstrapSourceGridControl grid,
        int row,
        int column)
    {
        Assert.That(
            grid.Selection.ActivePosition,
            Is.EqualTo(new SourceGrid.Position(row, column)));
    }

    private sealed class KeyboardFixture : System.IDisposable
    {
        internal KeyboardFixture()
        {
            Form = new Form
            {
                ClientSize = new Size(260, 110),
            };
            Grid = new KeyboardTestGrid
            {
                Dock = DockStyle.Fill,
            };
            Grid.Redim(5, 3);
            for (var row = 0; row < Grid.RowsCount; row++)
            {
                Grid.Rows[row].Height = 28;
                for (var column = 0; column < Grid.ColumnsCount; column++)
                {
                    Grid[row, column] = new SourceGrid.Cells.Cell(
                        $"{row}:{column}",
                        typeof(string));
                    Grid.GetCell(row, column);
                }
            }

            Form.Controls.Add(Grid);
            Form.Show();
            Grid.Focus();
        }

        internal Form Form { get; }

        internal KeyboardTestGrid Grid { get; }

        public void Dispose()
        {
            var active = Grid.Selection.ActivePosition;
            if (!active.IsEmpty())
            {
                new SourceGrid.CellContext(Grid, active).EndEdit(true);
            }

            Form.Close();
            Form.Dispose();
            Grid.Dispose();
        }
    }

    private sealed class KeyboardTestGrid : BootstrapSourceGridControl
    {
        internal int ShiftTabDispatchCount { get; private set; }

        internal int RowBoundaryDispatchCount { get; private set; }

        internal bool DispatchCommandKey(Keys keys)
        {
            var message = new Message();
            return base.ProcessCmdKey(ref message, keys);
        }

        internal void DispatchKeyDown(Keys keys)
        {
            base.OnKeyDown(new KeyEventArgs(keys));
        }

        public override void ProcessSpecialGridKey(KeyEventArgs e)
        {
            if (e.KeyData == (Keys.Shift | Keys.Tab))
            {
                ShiftTabDispatchCount++;
            }

            if (e.KeyData == Keys.Home || e.KeyData == Keys.End)
            {
                RowBoundaryDispatchCount++;
            }

            base.ProcessSpecialGridKey(e);
        }
    }
}
