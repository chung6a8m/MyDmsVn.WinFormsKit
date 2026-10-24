using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.BootstrapSourceGrid.Tests.EditorProbes;
using MyDmsVn.BootstrapSourceGrid.Editors;
using NUnit.Framework;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapEditorSizingTests
{
    [OneTimeSetUp]
    public void ConfigureWinForms()
    {
        WinFormsTestGuard.Configure();
    }

    [Test]
    public void EditorMinimumSizeUsesBootstrapControlPreferredSize()
    {
        var editor = new BootstrapTextBoxProbeEditor();
        try
        {
            var preferredSize = editor.BootstrapControl.GetPreferredSize(Size.Empty);

            Assert.That(preferredSize.Width, Is.GreaterThan(0));
            Assert.That(preferredSize.Height, Is.GreaterThan(0));
            Assert.That(editor.GetMinimumSize(SourceGrid.CellContext.Empty), Is.EqualTo(preferredSize));
        }
        finally
        {
            editor.Control.Dispose();
            editor.Dispose();
        }
    }

    [Test]
    public void CompactRowPlacesEditorWithoutChangingRowHeight()
    {
        using (var form = new Form())
        using (var grid = new BootstrapSourceGridControl())
        {
            var editor = grid.EditorRegistry.Register(new BootstrapTextBoxProbeEditor());
            var cell = new SourceGrid.Cells.Cell("before") { Editor = editor };
            grid.Redim(1, 1);
            grid.Rows[0].Height = 12;
            grid[0, 0] = cell;
            form.ClientSize = new Size(240, 80);
            form.Controls.Add(grid);
            grid.Dock = DockStyle.Fill;
            form.Show();

            var configuredRowHeight = grid.Rows[0].Height;
            var preferredHeight = editor.BootstrapControl.GetPreferredSize(Size.Empty).Height;
            var context = new SourceGrid.CellContext(grid, new SourceGrid.Position(0, 0), cell);
            grid.GetCell(0, 0).View.Measure(context, Size.Empty);
            context.StartEdit();

            try
            {
                Assert.That(preferredHeight, Is.GreaterThan(configuredRowHeight));
                Assert.That(grid.Rows[0].Height, Is.EqualTo(configuredRowHeight));
                Assert.That(editor.BootstrapControl.Height, Is.LessThanOrEqualTo(configuredRowHeight));
            }
            finally
            {
                context.EndEdit(true);
                form.Close();
            }
        }
    }

    [Test]
    public void BootstrapTextBoxEditorStaysWithinCompactRowWithoutChangingRowHeight()
    {
        using (var form = new Form())
        using (var grid = new BootstrapSourceGridControl())
        {
            var editor = grid.BootstrapEditors.CreateTextBox(typeof(string));
            var cell = new SourceGrid.Cells.Cell("before") { Editor = editor };
            grid.Redim(1, 1);
            grid.Rows[0].Height = 12;
            grid[0, 0] = cell;
            form.ClientSize = new Size(240, 80);
            form.Controls.Add(grid);
            grid.Dock = DockStyle.Fill;
            form.Show();
            Assert.That(grid.Selection.Focus(new SourceGrid.Position(0, 0), true), Is.True);

            var configuredRowHeight = grid.Rows[0].Height;
            var preferredHeight = editor.BootstrapControl.GetPreferredSize(Size.Empty).Height;
            var context = new SourceGrid.CellContext(grid, new SourceGrid.Position(0, 0), cell);
            grid.GetCell(0, 0).View.Measure(context, Size.Empty);
            context.StartEdit();

            try
            {
                Assert.That(preferredHeight, Is.GreaterThan(configuredRowHeight));
                Assert.That(grid.Rows[0].Height, Is.EqualTo(configuredRowHeight));
                Assert.That(editor.BootstrapControl.Height, Is.LessThanOrEqualTo(configuredRowHeight));
            }
            finally
            {
                context.EndEdit(true);
                form.Close();
            }
        }
    }

    [Test]
    public void BootstrapFormattedTextBoxEditorStaysWithinCompactRowWithoutChangingRowHeight()
    {
        using (var form = new Form())
        using (var grid = new BootstrapSourceGridControl())
        {
            var editor = grid.BootstrapEditors.CreateFormattedTextBox(typeof(string));
            var cell = new SourceGrid.Cells.Cell("before") { Editor = editor };
            grid.Redim(1, 1);
            grid.Rows[0].Height = 12;
            grid[0, 0] = cell;
            form.ClientSize = new Size(240, 80);
            form.Controls.Add(grid);
            grid.Dock = DockStyle.Fill;
            form.Show();
            Assert.That(grid.Selection.Focus(new SourceGrid.Position(0, 0), true), Is.True);

            var configuredRowHeight = grid.Rows[0].Height;
            var preferredHeight = editor.BootstrapControl.GetPreferredSize(Size.Empty).Height;
            var context = new SourceGrid.CellContext(grid, new SourceGrid.Position(0, 0), cell);
            grid.GetCell(0, 0).View.Measure(context, Size.Empty);
            context.StartEdit();

            try
            {
                Assert.That(preferredHeight, Is.GreaterThan(configuredRowHeight));
                Assert.That(grid.Rows[0].Height, Is.EqualTo(configuredRowHeight));
                Assert.That(editor.BootstrapControl.Height, Is.LessThanOrEqualTo(configuredRowHeight));
            }
            finally
            {
                context.EndEdit(true);
                form.Close();
            }
        }
    }

    [Test]
    public void BootstrapLookupBoxEditorStaysWithinCompactRowWithoutChangingRowHeight()
    {
        using (var form = new Form())
        using (var grid = new BootstrapSourceGridControl())
        {
            var editor = grid.BootstrapEditors.CreateLookupBox(typeof(string));
            var cell = new SourceGrid.Cells.Cell("before") { Editor = editor };
            grid.Redim(1, 1);
            grid.Rows[0].Height = 12;
            grid[0, 0] = cell;
            form.ClientSize = new Size(240, 80);
            form.Controls.Add(grid);
            grid.Dock = DockStyle.Fill;
            form.Show();
            Assert.That(grid.Selection.Focus(new SourceGrid.Position(0, 0), true), Is.True);

            var configuredRowHeight = grid.Rows[0].Height;
            var preferredHeight = editor.BootstrapControl.GetPreferredSize(Size.Empty).Height;
            var context = new SourceGrid.CellContext(grid, new SourceGrid.Position(0, 0), cell);
            grid.GetCell(0, 0).View.Measure(context, Size.Empty);
            context.StartEdit();

            try
            {
                Assert.That(preferredHeight, Is.GreaterThan(configuredRowHeight));
                Assert.That(grid.Rows[0].Height, Is.EqualTo(configuredRowHeight));
                Assert.That(editor.BootstrapControl.Height, Is.LessThanOrEqualTo(configuredRowHeight));
            }
            finally
            {
                context.EndEdit(true);
                form.Close();
            }
        }
    }

    [TestCase(144)]
    [TestCase(192)]
    public void DpiMetricRefreshDoesNotResizeRowsOrOverrideControlPreferredSize(int dpi)
    {
        using (var grid = new BootstrapSourceGridControl())
        {
            var editor = grid.EditorRegistry.Register(new BootstrapTextBoxProbeEditor());
            grid.Redim(1, 1);
            grid.Rows[0].Height = 17;
            var configuredRowHeight = grid.Rows[0].Height;

            grid.RefreshDpiMetrics(dpi);

            Assert.That(grid.Rows[0].Height, Is.EqualTo(configuredRowHeight));
            Assert.That(
                editor.GetMinimumSize(SourceGrid.CellContext.Empty),
                Is.EqualTo(editor.BootstrapControl.GetPreferredSize(Size.Empty)));
        }
    }
}
