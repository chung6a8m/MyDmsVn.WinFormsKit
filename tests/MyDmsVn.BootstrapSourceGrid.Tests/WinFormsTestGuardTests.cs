using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class WinFormsTestGuardTests
{
    [OneTimeSetUp]
    public void ConfigureWinForms()
    {
        WinFormsTestGuard.Configure();
    }

    [Test]
    public void ExceptionModeIsConfiguredBeforeGridHandleCreation()
    {
        Assert.That(WinFormsTestGuard.IsConfigured, Is.True);
        using (var grid = new BootstrapSourceGridControl())
        {
            Assert.That(grid.IsHandleCreated, Is.False);

            grid.CreateControl();

            Assert.That(grid.IsHandleCreated, Is.True);
        }
    }

    [Test]
    public void InvalidInitialEditorValueIsCapturedWithoutOpeningErrorDialog()
    {
        using (var form = new Form())
        using (var grid = new BootstrapSourceGridControl())
        using (var capture = WinFormsTestGuard.CaptureUserExceptions(grid))
        {
            var editor = new FailFirstSetTextBoxEditor();
            var cell = new SourceGrid.Cells.Cell("initial", editor);
            grid.Redim(1, 1);
            grid[0, 0] = cell;
            form.Controls.Add(grid);
            form.Show();
            var context = new SourceGrid.CellContext(
                grid,
                new SourceGrid.Position(0, 0),
                cell);

            context.StartEdit();
            try
            {
                Assert.That(capture.Count, Is.EqualTo(1));
                Assert.That(capture.Exception, Is.TypeOf<SourceGrid.EditingCellException>());
                Assert.That(cell.Editor.IsEditing, Is.True);
            }
            finally
            {
                context.EndEdit(true);
                form.Close();
            }
        }
    }

    private sealed class FailFirstSetTextBoxEditor : SourceGrid.Cells.Editors.TextBox
    {
        private bool _failNextSet = true;

        internal FailFirstSetTextBoxEditor()
            : base(typeof(string))
        {
        }

        public override void SetEditValue(object editValue)
        {
            if (_failNextSet)
            {
                _failNextSet = false;
                throw new System.FormatException("Deliberate editor setup failure.");
            }

            base.SetEditValue(editValue);
        }
    }
}
