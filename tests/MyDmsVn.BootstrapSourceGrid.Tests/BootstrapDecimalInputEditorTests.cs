using System;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;
using MyDmsVn.BootstrapSourceGrid.Editors;
using MyDmsVn.BootstrapSourceGrid.Editors.Internal;
using NUnit.Framework;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapDecimalInputEditorTests
{
    private static readonly DecimalInputProfile UsProfile =
        new DecimalInputProfile(CultureInfo.GetCultureInfo("en-US"), false, 4, 0m, 999999999999999.9999m);

    [OneTimeSetUp]
    public void ConfigureWinForms() => WinFormsTestGuard.Configure();

    [Test]
    public void InvalidCandidateDoesNotCommitOrLosePendingText()
    {
        using (var fixture = new DecimalEditorFixture(12m, 4m, UsProfile))
        {
            var errors = 0;
            fixture.Editor.EditException += (_, _) => errors++;
            var context = fixture.StartEdit(0);
            fixture.Editor.BootstrapControl.Text = "1,234";

            Assert.That(context.EndEdit(false), Is.False);
            Assert.That(fixture.Editor.IsEditing, Is.True);
            Assert.That(fixture.Editor.BootstrapControl.PendingText, Is.EqualTo("1,234"));
            Assert.That(fixture.Cells[0].Value, Is.EqualTo(12m));
            Assert.That(errors, Is.EqualTo(1));
        }
    }

    [Test]
    public void ValidDecimalSyntaxWithExcessPrecisionStillCannotCommit()
    {
        using (var fixture = new DecimalEditorFixture(12m, 4m, UsProfile))
        {
            var errors = 0;
            fixture.Editor.EditException += (_, _) => errors++;
            var context = fixture.StartEdit(0);
            fixture.Editor.BootstrapControl.Text = "1.00001";

            Assert.That(context.EndEdit(false), Is.False);
            Assert.That(fixture.Editor.BootstrapControl.PendingText, Is.EqualTo("1.00001"));
            Assert.That(fixture.Cells[0].Value, Is.EqualTo(12m));
            Assert.That(errors, Is.EqualTo(1));
        }
    }

    [Test]
    public void ValidCandidateAppliesModelOnlyOnceAndProjectsAcceptedValue()
    {
        using (var fixture = new DecimalEditorFixture(12m, 4m, UsProfile))
        {
            var model = new CountingValueModel(12m);
            fixture.Cells[0].Model.ValueModel = model;
            var context = fixture.StartEdit(0);
            fixture.Editor.BootstrapControl.Text = "1234.5";

            Assert.That(context.EndEdit(false), Is.True);
            Assert.That(model.Value, Is.EqualTo(1234.5m));
            Assert.That(model.WriteCount, Is.EqualTo(1));
            Assert.That(fixture.Editor.BootstrapControl.Value, Is.EqualTo(1234.5m));
        }
    }

    [Test]
    public void AcceptedProjectionDoesNotRaiseAnotherPendingChange()
    {
        using (var fixture = new DecimalEditorFixture(12m, 4m, UsProfile))
        {
            var context = fixture.StartEdit(0);
            var pendingChanges = 0;
            fixture.Editor.BootstrapControl.PendingTextChanged += (_, _) => pendingChanges++;
            fixture.Editor.BootstrapControl.Text = "1234.5";
            Assert.That(pendingChanges, Is.EqualTo(1));

            Assert.That(context.EndEdit(false), Is.True);
            Assert.That(pendingChanges, Is.EqualTo(1));
        }
    }

    [Test]
    public void GetEditedValueParsesWithoutProjectingOrWritingModel()
    {
        using (var fixture = new DecimalEditorFixture(12m, 4m, UsProfile))
        {
            var context = fixture.StartEdit(0);
            fixture.Editor.BootstrapControl.Text = "1234.5";

            Assert.That(fixture.Editor.GetEditedValue(), Is.EqualTo(1234.5m));
            Assert.That(fixture.Editor.BootstrapControl.Value, Is.EqualTo(12m));
            Assert.That(fixture.Editor.BootstrapControl.PendingText, Is.EqualTo("1234.5"));
            Assert.That(fixture.Cells[0].Value, Is.EqualTo(12m));
            context.EndEdit(true);
        }
    }

    [Test]
    public void CancelProjectsCurrentModelRatherThanEditStartSnapshot()
    {
        using (var fixture = new DecimalEditorFixture(12m, 4m, UsProfile))
        {
            var context = fixture.StartEdit(0);
            fixture.Editor.BootstrapControl.Text = "888";
            fixture.Cells[0].Value = 25m;

            Assert.That(context.EndEdit(true), Is.True);
            Assert.That(fixture.Cells[0].Value, Is.EqualTo(25m));
            Assert.That(fixture.Editor.BootstrapControl.Value, Is.EqualTo(25m));
            Assert.That(fixture.Editor.BootstrapControl.PendingText, Is.EqualTo("25"));
        }
    }

    [Test]
    public void FirstCharacterReplacesSelectedInitialValue()
    {
        using (var fixture = new DecimalEditorFixture(1234m, 4m, UsProfile))
        {
            var context = fixture.StartEdit(0);
            fixture.Editor.SendCharToEditor('9');

            Assert.That(fixture.Editor.BootstrapControl.PendingText, Is.EqualTo("9"));
            Assert.That(((BootstrapSourceGridDecimalInputControl)fixture.Editor.Control).NativeEditor.SelectionStart, Is.EqualTo(1));
            Assert.That(fixture.Cells[0].Value, Is.EqualTo(1234m));
            context.EndEdit(true);
        }
    }

    [Test]
    public void NativeDecimalEditorKeepsRightAlignmentAndVerticalCenterWithoutChangingRowHeight()
    {
        using (var fixture = new DecimalEditorFixture(1234m, 4m, UsProfile))
        {
            var rowHeight = fixture.Grid.Rows[0].Height;
            var context = fixture.StartEdit(0);
            var control = (BootstrapSourceGridDecimalInputControl)fixture.Editor.Control;
            var native = control.NativeEditor;
            var preferred = Math.Max(native.PreferredHeight,
                TextRenderer.MeasureText("123", native.Font).Height);

            Assert.That(native.TextAlign, Is.EqualTo(HorizontalAlignment.Right));
            Assert.That(native.AutoSize, Is.False);
            Assert.That(native.Top, Is.EqualTo(Math.Max(0, (control.ClientSize.Height - preferred) / 2)));
            Assert.That(fixture.Grid.Rows[0].Height, Is.EqualTo(rowHeight));
            Assert.That(control.Height, Is.LessThanOrEqualTo(rowHeight));
            context.EndEdit(true);
        }
    }

    [Test]
    public void ReusedEditorResetsCandidateAndAcceptsNewCellValue()
    {
        using (var fixture = new DecimalEditorFixture(12m, 4m, UsProfile))
        {
            var first = fixture.StartEdit(0);
            fixture.Editor.BootstrapControl.Text = "invalid";
            Assert.That(first.EndEdit(true), Is.True);

            var second = fixture.StartEdit(1);
            Assert.That(fixture.Editor.BootstrapControl.PendingText, Is.EqualTo("4"));
            Assert.That(fixture.Editor.BootstrapControl.Value, Is.EqualTo(4m));
            ((BootstrapSourceGridDecimalInputControl)fixture.Editor.Control)
                .ProcessDecimalEditCommand(Keys.Control | Keys.Z);
            Assert.That(fixture.Editor.BootstrapControl.PendingText, Is.EqualTo("4"));
            second.EndEdit(true);
        }
    }

    [Test]
    public void ProfileIsFrozenDuringEditAndCanChangeBeforeReusedCell()
    {
        var vietnamese = new DecimalInputProfile(
            CultureInfo.GetCultureInfo("vi-VN"), false, 2, 0m, 100m);
        using (var fixture = new DecimalEditorFixture(12m, 4.5m, UsProfile))
        {
            var first = fixture.StartEdit(0);
            fixture.Editor.BootstrapControl.Text = "invalid";
            Assert.That((Action)(() => fixture.Editor.Profile = vietnamese),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(fixture.Editor.Profile, Is.SameAs(UsProfile));
            Assert.That(fixture.Editor.BootstrapControl.PendingText, Is.EqualTo("invalid"));
            Assert.That(first.EndEdit(true), Is.True);

            fixture.Editor.Profile = vietnamese;
            var second = fixture.StartEdit(1);
            Assert.That(fixture.Editor.BootstrapControl.PendingText, Is.EqualTo("4,5"));
            Assert.That(fixture.Editor.BootstrapControl.Value, Is.EqualTo(4.5m));
            fixture.Editor.BootstrapControl.Text = "5,25";
            Assert.That(second.EndEdit(false), Is.True);
            Assert.That(fixture.Cells[1].Value, Is.EqualTo(5.25m));
        }
    }

    [Test]
    public void NullableBlankCommitsNullAndRequiredBlankStaysPending()
    {
        var nullable = new DecimalInputProfile(CultureInfo.InvariantCulture, true, 4, 0m, 100m);
        using (var fixture = new DecimalEditorFixture(12m, 4m, nullable))
        {
            var first = fixture.StartEdit(0);
            fixture.Editor.BootstrapControl.Text = string.Empty;
            Assert.That(first.EndEdit(false), Is.True);
            Assert.That(fixture.Cells[0].Value, Is.Null);

            fixture.Editor.Profile = UsProfile;
            var second = fixture.StartEdit(1);
            fixture.Editor.BootstrapControl.Text = string.Empty;
            Assert.That(second.EndEdit(false), Is.False);
            Assert.That(fixture.Cells[1].Value, Is.EqualTo(4m));
            Assert.That(fixture.Editor.BootstrapControl.PendingText, Is.Empty);
        }
    }

    [Test]
    public void RegistryRejectsUnsupportedTypeAndNullProfileBeforeAllocatingControl()
    {
        using (var grid = new BootstrapSourceGridControl())
        {
            Assert.That((Action)(() => grid.BootstrapEditors.CreateDecimalInput(null!, UsProfile)),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("valueType"));
            Assert.That((Action)(() => grid.BootstrapEditors.CreateDecimalInput(typeof(decimal), null!)),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("profile"));
            Assert.That((Action)(() => grid.BootstrapEditors.CreateDecimalInput(typeof(int), UsProfile)),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("valueType"));
        }
    }

    [Test]
    public void RegistryOwnsEvenNeverStartedDecimalControl()
    {
        var grid = new BootstrapSourceGridControl();
        var editor = grid.BootstrapEditors.CreateDecimalInput(typeof(decimal), UsProfile);
        Assert.That(editor.Grid, Is.Null);
        Assert.That(editor.UseCellViewProperties, Is.False);
        grid.Dispose();
        Assert.That(editor.BootstrapControl.IsDisposed, Is.True);
    }

    private sealed class DecimalEditorFixture : IDisposable
    {
        internal DecimalEditorFixture(decimal first, decimal second, DecimalInputProfile profile)
        {
            Form = new Form { ClientSize = new Size(260, 100) };
            Grid = new BootstrapSourceGridControl { Dock = DockStyle.Fill };
            Editor = Grid.BootstrapEditors.CreateDecimalInput(typeof(decimal), profile);
            Cells = new[]
            {
                new SourceGrid.Cells.Cell(first, typeof(decimal)) { Editor = Editor },
                new SourceGrid.Cells.Cell(second, typeof(decimal)) { Editor = Editor },
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
        internal BootstrapDecimalInputEditor Editor { get; }
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
            if (Editor.IsEditing) Editor.EditCellContext.EndEdit(true);
            Form.Close();
            Form.Dispose();
            Grid.Dispose();
        }
    }

    private sealed class CountingValueModel : SourceGrid.Cells.Models.IValueModel
    {
        internal CountingValueModel(decimal value) => Value = value;
        internal object Value { get; private set; }
        internal int WriteCount { get; private set; }
        public object GetValue(SourceGrid.CellContext context) => Value;
        public void SetValue(SourceGrid.CellContext context, object value)
        {
            WriteCount++;
            Value = value;
        }
        public string GetDisplayText(SourceGrid.CellContext context) => Convert.ToString(Value, CultureInfo.InvariantCulture) ?? string.Empty;
    }
}
