using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using MyDmsVn.BootstrapSourceGrid.Editors;
using NUnit.Framework;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapLookupBoxInteractionTests
{
    [OneTimeSetUp]
    public void ConfigureWinForms()
    {
        WinFormsTestGuard.Configure();
    }

    [TestCase(BootstrapThemeMode.Light)]
    [TestCase(BootstrapThemeMode.Dark)]
    public void ActiveLookupAccessoryPaintsChevronAcrossThemeWithoutEndingCellEdit(BootstrapThemeMode mode)
    {
        var previous = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode);
            using var fixture = new LookupInteractionFixture();
            using var errors = WinFormsTestGuard.CaptureUserExceptions(fixture.Grid);
            fixture.Editor.BootstrapControl.ResultsGrid.DataError += (_, args) => throw args.Exception ?? new InvalidOperationException("Unexpected lookup DataError.");
            var context = fixture.StartEdit();
            var lookup = fixture.Editor.BootstrapControl;
            var native = lookup.Controls.OfType<TextBoxBase>().Single();
            var handle = native.Handle;
            var accessory = lookup.Controls.Cast<Control>().Single(control => control.AccessibleName == "Open lookup results");
            AssertPainted();
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode == BootstrapThemeMode.Light ? BootstrapThemeMode.Dark : BootstrapThemeMode.Light);
            Application.DoEvents();
            AssertPainted();
            Assert.That(fixture.Editor.IsEditing, Is.True);
            Assert.That(native.Handle, Is.EqualTo(handle));
            Assert.That(native.Focused, Is.True);
            Assert.That(fixture.Cell.Value, Is.EqualTo(42));
            Assert.That(fixture.ValueChangedCount, Is.Zero);
            Assert.That(errors.Count, Is.Zero);
            Assert.That(context.EndEdit(true), Is.True);
            Assert.That(fixture.Cell.Value, Is.EqualTo(42));

            void AssertPainted()
            {
                using var bitmap = new Bitmap(accessory.Width, accessory.Height);
                accessory.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                var surface = BootstrapThemeManager.CurrentTheme.Colors.Surface.ToArgb();
                Assert.That(bitmap.GetPixel(1, 1).ToArgb(), Is.EqualTo(surface));
                var pixels = 0;
                for (var y = 2; y < bitmap.Height - 2; y++)
                    for (var x = 2; x < bitmap.Width - 2; x++)
                        if (bitmap.GetPixel(x, y).ToArgb() != surface) pixels++;
                Assert.That(pixels, Is.GreaterThan(5), "The active grid lookup accessory must render its chevron.");
            }
        }
        finally { BootstrapThemeManager.CurrentTheme = previous; }
    }
    [Test]
    public void OpeningPopupDoesNotEndSourceGridEdit()
    {
        using (var fixture = new LookupInteractionFixture())
        {
            fixture.StartEdit();

            fixture.Editor.BootstrapControl.OpenDropDown();
            Application.DoEvents();

            Assert.That(fixture.Editor.BootstrapControl.IsDropDownOpen, Is.True);
            Assert.That(fixture.Editor.IsEditing, Is.True);
            Assert.That(fixture.Cell.Value, Is.EqualTo(42));
            Assert.That(fixture.ValueChangedCount, Is.Zero);
        }
    }

    [Test]
    public void ResultGridFocusDoesNotCommitSourceGridEdit()
    {
        using (var fixture = new LookupInteractionFixture())
        {
            fixture.StartEdit();
            fixture.Editor.BootstrapControl.Text = string.Empty;
            fixture.Editor.BootstrapControl.OpenDropDown();
            Application.DoEvents();

            Assert.That(fixture.Editor.BootstrapControl.ResultsGrid.Focus(), Is.True);
            Application.DoEvents();

            Assert.That(fixture.Editor.BootstrapControl.IsDropDownOpen, Is.True);
            Assert.That(fixture.Editor.IsEditing, Is.True);
            Assert.That(fixture.Cell.Value, Is.EqualTo(42));
            Assert.That(fixture.ValueChangedCount, Is.Zero);
        }
    }

    [Test]
    public void ClosingPopupWithoutSelectionKeepsOriginalEditActive()
    {
        using (var fixture = new LookupInteractionFixture())
        {
            fixture.StartEdit();
            fixture.Editor.BootstrapControl.Text = string.Empty;
            fixture.Editor.BootstrapControl.OpenDropDown();
            Application.DoEvents();

            fixture.Editor.BootstrapControl.CloseDropDown();
            Application.DoEvents();

            Assert.That(fixture.Editor.BootstrapControl.IsDropDownOpen, Is.False);
            Assert.That(fixture.Editor.IsEditing, Is.True);
            Assert.That(fixture.Editor.BootstrapControl.SelectedValue, Is.EqualTo(42));
            Assert.That(fixture.Cell.Value, Is.EqualTo(42));
            Assert.That(fixture.ValueChangedCount, Is.Zero);
        }
    }

    [Test]
    public void EnterWithPopupClosedUsesSourceGridCommitPolicy()
    {
        using (var fixture = new LookupInteractionFixture())
        {
            fixture.StartEdit();
            fixture.Editor.BootstrapControl.SelectValue(7);

            Assert.That(fixture.DispatchEditorCommandKey(Keys.Enter), Is.True);

            Assert.That(fixture.Editor.IsEditing, Is.False);
            Assert.That(fixture.Cell.Value, Is.EqualTo(7));
            Assert.That(fixture.ValueChangedCount, Is.EqualTo(1));
        }
    }

    [Test]
    public void EnterWithHighlightedResultCommitsSelectedValueExactlyOnce()
    {
        using (var fixture = new LookupInteractionFixture())
        {
            fixture.StartEdit();
            fixture.Editor.BootstrapControl.Text = string.Empty;
            fixture.Editor.BootstrapControl.OpenDropDown();
            Assert.That(fixture.DispatchEditorCommandKey(Keys.Down), Is.True);
            Assert.That(((LookupItem)fixture.Editor.BootstrapControl.HighlightedItem!).Id, Is.EqualTo(7));

            Assert.That(fixture.DispatchEditorCommandKey(Keys.Enter), Is.True);
            Application.DoEvents();

            Assert.That(fixture.Editor.IsEditing, Is.False);
            Assert.That(fixture.Cell.Value, Is.EqualTo(7));
            Assert.That(fixture.ValueChangedCount, Is.EqualTo(1));
        }
    }

    [Test]
    public void EscapeWithPopupOpenCancelsPendingLookupOnly()
    {
        using (var fixture = new LookupInteractionFixture())
        {
            fixture.StartEdit();
            fixture.Editor.BootstrapControl.Text = "Cont";
            fixture.Editor.BootstrapControl.OpenDropDown();

            Assert.That(fixture.DispatchEditorCommandKey(Keys.Escape), Is.True);

            Assert.That(fixture.Editor.IsEditing, Is.True);
            Assert.That(fixture.Editor.BootstrapControl.IsDropDownOpen, Is.False);
            Assert.That(fixture.Editor.BootstrapControl.HasPendingText, Is.False);
            Assert.That(fixture.Editor.BootstrapControl.SelectedValue, Is.EqualTo(42));
            Assert.That(fixture.Cell.Value, Is.EqualTo(42));
            Assert.That(fixture.ValueChangedCount, Is.Zero);
        }
    }

    [Test]
    public void EscapeWithPopupClosedUsesSourceGridCancelPolicy()
    {
        using (var fixture = new LookupInteractionFixture())
        {
            fixture.StartEdit();
            fixture.Editor.BootstrapControl.SelectValue(7);

            Assert.That(fixture.DispatchEditorCommandKey(Keys.Escape), Is.True);

            Assert.That(fixture.Editor.IsEditing, Is.False);
            Assert.That(fixture.Cell.Value, Is.EqualTo(42));
            Assert.That(fixture.Editor.BootstrapControl.SelectedValue, Is.EqualTo(42));
            Assert.That(fixture.ValueChangedCount, Is.Zero);
        }
    }

    [Test]
    public void NativeEditorEnterWithPopupClosedCommitsSourceGridEdit()
    {
        using (var fixture = new LookupInteractionFixture())
        {
            fixture.StartEdit();
            fixture.Editor.BootstrapControl.SelectValue(7);

            var key = fixture.DispatchNativeEditorKeyDown(Keys.Enter);

            Assert.That(key.Handled, Is.True);
            Assert.That(key.SuppressKeyPress, Is.True);
            Assert.That(fixture.Editor.IsEditing, Is.False);
            Assert.That(fixture.Cell.Value, Is.EqualTo(7));
            Assert.That(fixture.ValueChangedCount, Is.EqualTo(1));
        }
    }

    [Test]
    public void NativeEditorEscapeWithPopupClosedCancelsSourceGridEdit()
    {
        using (var fixture = new LookupInteractionFixture())
        {
            fixture.StartEdit();
            fixture.Editor.BootstrapControl.SelectValue(7);

            var key = fixture.DispatchNativeEditorKeyDown(Keys.Escape);

            Assert.That(key.Handled, Is.True);
            Assert.That(key.SuppressKeyPress, Is.True);
            Assert.That(fixture.Editor.IsEditing, Is.False);
            Assert.That(fixture.Cell.Value, Is.EqualTo(42));
            Assert.That(fixture.Editor.BootstrapControl.SelectedValue, Is.EqualTo(42));
            Assert.That(fixture.ValueChangedCount, Is.Zero);
        }
    }

    [Test]
    public void TabCommitsOnceAndMovesToNextEditableCell()
    {
        using (var fixture = new LookupInteractionFixture())
        {
            fixture.StartEdit();
            fixture.Editor.BootstrapControl.SelectValue(7);

            Assert.That(fixture.DispatchEditorCommandKey(Keys.Tab), Is.True);

            Assert.That(fixture.Editor.IsEditing, Is.False);
            Assert.That(fixture.Cell.Value, Is.EqualTo(7));
            Assert.That(fixture.ValueChangedCount, Is.EqualTo(1));
            Assert.That(fixture.Grid.Selection.ActivePosition, Is.EqualTo(new SourceGrid.Position(1, 0)));
        }
    }

    [Test]
    public void ShiftTabCommitsOnceAndMovesToPreviousEditableCell()
    {
        using (var fixture = new LookupInteractionFixture())
        {
            fixture.StartEdit(1);
            fixture.Editor.BootstrapControl.SelectValue(42);

            Assert.That(fixture.DispatchEditorCommandKey(Keys.Shift | Keys.Tab), Is.True);

            Assert.That(fixture.Editor.IsEditing, Is.False);
            Assert.That(fixture.Cells[1].Value, Is.EqualTo(42));
            Assert.That(fixture.ValueChangedCount, Is.EqualTo(1));
            Assert.That(fixture.Grid.Selection.ActivePosition, Is.EqualTo(new SourceGrid.Position(0, 0)));
        }
    }

    [TestCase(Keys.Down, 7)]
    [TestCase(Keys.PageDown, 7)]
    public void PopupResultNavigationDoesNotMoveSourceGridActiveCell(Keys key, int highlightedId)
    {
        using (var fixture = new LookupInteractionFixture())
        {
            fixture.StartEdit();
            fixture.Editor.BootstrapControl.Text = string.Empty;
            fixture.Editor.BootstrapControl.OpenDropDown();

            Assert.That(fixture.DispatchEditorCommandKey(key), Is.True);

            Assert.That(((LookupItem)fixture.Editor.BootstrapControl.HighlightedItem!).Id, Is.EqualTo(highlightedId));
            Assert.That(fixture.Grid.Selection.ActivePosition, Is.EqualTo(new SourceGrid.Position(0, 0)));
            Assert.That(fixture.Editor.IsEditing, Is.True);
        }
    }

    [Test]
    public void SourceGridNavigationResumesAfterLookupCommit()
    {
        using (var fixture = new LookupInteractionFixture())
        {
            fixture.StartEdit();
            fixture.Editor.BootstrapControl.Text = string.Empty;
            fixture.Editor.BootstrapControl.OpenDropDown();
            fixture.DispatchEditorCommandKey(Keys.Down);
            fixture.DispatchEditorCommandKey(Keys.Enter);

            var navigation = fixture.Grid.DispatchSpecialKey(Keys.Down);

            Assert.That(navigation.Handled, Is.True);
            Assert.That(fixture.Grid.Selection.ActivePosition, Is.EqualTo(new SourceGrid.Position(1, 0)));
        }
    }

    [Test]
    public void MouseResultSelectionCommitsSelectedValueExactlyOnce()
    {
        using (var fixture = new LookupInteractionFixture())
        {
            fixture.StartEdit();
            fixture.Editor.BootstrapControl.Text = string.Empty;
            fixture.Editor.BootstrapControl.OpenDropDown();

            ClickResultRow(fixture.Editor.BootstrapControl.ResultsGrid, 1);

            Assert.That(fixture.Editor.IsEditing, Is.False);
            Assert.That(fixture.Editor.BootstrapControl.IsDropDownOpen, Is.False);
            Assert.That(fixture.Cell.Value, Is.EqualTo(7));
            Assert.That(fixture.ValueChangedCount, Is.EqualTo(1));
            Assert.That(fixture.Grid.Selection.ActivePosition, Is.EqualTo(new SourceGrid.Position(0, 0)));
        }
    }

    [Test]
    public void OutsideFocusCommitsOnceAndClosesPopup()
    {
        using (var fixture = new LookupInteractionFixture())
        {
            fixture.StartEdit();
            fixture.Editor.BootstrapControl.SelectValue(7);
            fixture.Editor.BootstrapControl.OpenDropDown();

            Assert.That(fixture.FocusTarget.Focus(), Is.True);
            Application.DoEvents();

            Assert.That(fixture.Editor.IsEditing, Is.False);
            Assert.That(fixture.Editor.BootstrapControl.IsDropDownOpen, Is.False);
            Assert.That(fixture.Cell.Value, Is.EqualTo(7));
            Assert.That(fixture.ValueChangedCount, Is.EqualTo(1));
        }
    }

    [Test]
    public void ApplicationDeactivationClosesPopupAndLeavesEditDefined()
    {
        using (var fixture = new LookupInteractionFixture())
        {
            fixture.StartEdit();
            fixture.Editor.BootstrapControl.OpenDropDown();
            var popup = GetLookupPopup(fixture.Editor.BootstrapControl);

            SendMessage(popup.Handle, 0x001C, IntPtr.Zero, IntPtr.Zero);
            Application.DoEvents();

            Assert.That(fixture.Editor.BootstrapControl.IsDropDownOpen, Is.False);
            Assert.That(fixture.Editor.IsEditing, Is.True);
            Assert.That(fixture.Cell.Value, Is.EqualTo(42));
            Assert.That(fixture.ValueChangedCount, Is.Zero);
        }
    }

    [Test]
    public void RestorePreviousSelectionPolicyKeepsOriginalLogicalValue()
    {
        using (var fixture = new LookupInteractionFixture())
        {
            fixture.StartEdit();
            fixture.Editor.BootstrapControl.UnmatchedTextBehavior =
                BootstrapLookupUnmatchedTextBehavior.RestorePreviousSelection;
            fixture.Editor.BootstrapControl.Text = "Unknown";

            Assert.That(fixture.DispatchEditorCommandKey(Keys.Tab), Is.True);

            Assert.That(fixture.Editor.IsEditing, Is.False);
            Assert.That(fixture.Cell.Value, Is.EqualTo(42));
            Assert.That(fixture.Editor.BootstrapControl.SelectedValue, Is.EqualTo(42));
            Assert.That(fixture.Editor.BootstrapControl.ValidationMessage, Is.Empty);
        }
    }

    [Test]
    public void KeepFocusPolicyBlocksSourceGridCommitForUnmatchedText()
    {
        using (var fixture = new LookupInteractionFixture())
        {
            fixture.StartEdit();
            fixture.Editor.BootstrapControl.UnmatchedTextBehavior =
                BootstrapLookupUnmatchedTextBehavior.KeepFocusWithValidationError;
            fixture.Editor.BootstrapControl.Text = "Unknown";

            Assert.That(fixture.DispatchEditorCommandKey(Keys.Tab), Is.True);

            Assert.That(fixture.Editor.IsEditing, Is.True);
            Assert.That(fixture.Editor.BootstrapControl.HasPendingText, Is.True);
            Assert.That(fixture.Editor.BootstrapControl.ValidationMessage, Is.Not.Empty);
            Assert.That(fixture.Editor.BootstrapControl.IsDropDownOpen, Is.True);
            Assert.That(fixture.Cell.Value, Is.EqualTo(42));
            Assert.That(fixture.ValueChangedCount, Is.Zero);
        }
    }

    [Test]
    public void CommitAndAddPolicyStoresCreatedSelectedValue()
    {
        using (var fixture = new LookupInteractionFixture())
        {
            fixture.StartEdit();
            fixture.Editor.BootstrapControl.UnmatchedTextBehavior =
                BootstrapLookupUnmatchedTextBehavior.CommitAndAdd;
            fixture.Editor.BootstrapControl.CreateItemFromText += (_, e) =>
                e.Item = new LookupItem(99, e.OriginalText.Trim());
            fixture.Editor.BootstrapControl.Text = "Adventure Works";

            Assert.That(fixture.DispatchEditorCommandKey(Keys.Tab), Is.True);

            Assert.That(fixture.Editor.IsEditing, Is.False);
            Assert.That(fixture.Cell.Value, Is.EqualTo(99));
            Assert.That(fixture.Editor.BootstrapControl.SelectedValue, Is.EqualTo(99));
            Assert.That(fixture.ValueChangedCount, Is.EqualTo(1));
        }
    }

    [Test]
    public void SourceGridValidationFailureKeepsLookupEditActive()
    {
        using (var fixture = new LookupInteractionFixture())
        {
#pragma warning disable CS0618
            fixture.Editor.Validating += (_, e) => e.Cancel = Equals(e.NewValue, 7);
#pragma warning restore CS0618
            fixture.StartEdit();
            fixture.Editor.BootstrapControl.SelectValue(7);

            Assert.That(fixture.DispatchEditorCommandKey(Keys.Enter), Is.True);

            Assert.That(fixture.Editor.IsEditing, Is.True);
            Assert.That(fixture.Cell.Value, Is.EqualTo(42));
            Assert.That(fixture.Editor.BootstrapControl.SelectedValue, Is.EqualTo(7));
            Assert.That(fixture.ValueChangedCount, Is.Zero);
        }
    }

    [Test]
    public void ThemeSwitchWhilePopupIsOpenKeepsLookupUsableWithoutViewInjection()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
            using (var fixture = new LookupInteractionFixture())
            {
                fixture.StartEdit();
                fixture.Editor.BootstrapControl.Text = string.Empty;
                fixture.Editor.BootstrapControl.OpenDropDown();
                var popupSurface = fixture.Editor.BootstrapControl.ResultsGrid.Parent!.Parent!.Parent!;

                var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
                BootstrapThemeManager.CurrentTheme = dark;
                Application.DoEvents();

                var nativeEditor = fixture.Editor.BootstrapControl.Controls
                    .OfType<TextBox>()
                    .Single();
                Assert.That(fixture.Editor.UseCellViewProperties, Is.False);
                Assert.That(fixture.Editor.IsEditing, Is.True);
                Assert.That(fixture.Editor.BootstrapControl.IsDropDownOpen, Is.True);
                Assert.That(nativeEditor.BackColor, Is.EqualTo(dark.Colors.Surface));
                Assert.That(nativeEditor.ForeColor, Is.EqualTo(dark.Colors.Text));
                Assert.That(popupSurface.BackColor, Is.EqualTo(dark.Colors.Surface));

                fixture.DispatchEditorCommandKey(Keys.Down);
                fixture.DispatchEditorCommandKey(Keys.Enter);

                Assert.That(fixture.Editor.IsEditing, Is.False);
                Assert.That(fixture.Cell.Value, Is.EqualTo(7));
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    private static void ClickResultRow(DataGridView resultsGrid, int rowIndex)
    {
        var onCellMouseClick = typeof(DataGridView).GetMethod(
            "OnCellMouseClick",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        onCellMouseClick.Invoke(resultsGrid, new object[]
        {
            new DataGridViewCellMouseEventArgs(
                0,
                rowIndex,
                4,
                4,
                new MouseEventArgs(MouseButtons.Left, 1, 4, 4, 0)),
        });
        Application.DoEvents();
    }

    private static Control GetLookupPopup(BootstrapLookupBox lookup)
    {
        var controllerField = typeof(BootstrapLookupBox).GetField(
            "_dropDownController",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var controller = controllerField.GetValue(lookup)!;
        var popupField = controller.GetType().GetField(
            "_dropDown",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (Control)popupField.GetValue(controller)!;
    }

    private sealed class LookupInteractionFixture : IDisposable
    {
        private readonly SourceGrid.Cells.Controllers.CustomEvents _events;

        internal LookupInteractionFixture()
        {
            Form = new Form
            {
                ClientSize = new Size(320, 150),
                ShowInTaskbar = false,
            };
            Grid = new LookupInteractionGrid
            {
                Dock = DockStyle.Top,
                Height = 80,
            };
            Editor = Grid.BootstrapEditors.CreateLookupBox(typeof(int));
            Editor.BootstrapControl.DisplayMember = nameof(LookupItem.Name);
            Editor.BootstrapControl.ValueMember = nameof(LookupItem.Id);
            Editor.BootstrapControl.DataSource = new BindingList<LookupItem>
            {
                new LookupItem(42, "Northwind"),
                new LookupItem(7, "Contoso"),
            };
            Editor.BootstrapControl.SearchDebounceMilliseconds = 0;
            Cell = new SourceGrid.Cells.Cell(42, typeof(int)) { Editor = Editor };
            Cells = new[]
            {
                Cell,
                new SourceGrid.Cells.Cell(7, typeof(int)) { Editor = Editor },
            };
            _events = new SourceGrid.Cells.Controllers.CustomEvents();
            _events.ValueChanged += OnValueChanged;
            Cells[0].AddController(_events);
            Cells[1].AddController(_events);
            Grid.Redim(2, 1);
            Grid.Rows[0].Height = 32;
            Grid.Rows[1].Height = 32;
            Grid[0, 0] = Cell;
            Grid[1, 0] = Cells[1];
            FocusTarget = new Button
            {
                Dock = DockStyle.Bottom,
                Text = "Outside",
            };
            Form.Controls.Add(Grid);
            Form.Controls.Add(FocusTarget);
            Form.Show();
            Grid.Focus();
            Application.DoEvents();
        }

        internal Form Form { get; }

        internal LookupInteractionGrid Grid { get; }

        internal Button FocusTarget { get; }

        internal BootstrapLookupBoxEditor Editor { get; }

        internal SourceGrid.Cells.Cell Cell { get; }

        internal SourceGrid.Cells.Cell[] Cells { get; }

        internal int ValueChangedCount { get; private set; }

        internal SourceGrid.CellContext StartEdit(int row = 0)
        {
            var position = new SourceGrid.Position(row, 0);
            Assert.That(Grid.Selection.Focus(position, true), Is.True);
            var context = new SourceGrid.CellContext(Grid, position, Cells[row]);
            Grid.GetCell(position).View.Measure(context, Size.Empty);
            context.StartEdit();
            Application.DoEvents();
            Assert.That(Editor.IsEditing, Is.True);
            return context;
        }

        internal bool DispatchEditorCommandKey(Keys keyData)
        {
            var message = new Message();
            var arguments = new object[] { message, keyData };
            var processCmdKey = typeof(MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapLookupBox).GetMethod(
                "ProcessCmdKey",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            var handledByLookup = (bool)processCmdKey.Invoke(Editor.BootstrapControl, arguments)!;
            return handledByLookup || Grid.DispatchCommandKey(keyData);
        }

        internal KeyEventArgs DispatchNativeEditorKeyDown(Keys keyData)
        {
            var arguments = new KeyEventArgs(keyData);
            var onEditorKeyDown = Editor.BootstrapControl.GetType().GetMethod(
                "OnEditorKeyDown",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            onEditorKeyDown.Invoke(Editor.BootstrapControl, new object[] { arguments });
            return arguments;
        }

        public void Dispose()
        {
            if (Editor.IsEditing)
            {
                Editor.EditCellContext.EndEdit(true);
            }

            Cells[0].RemoveController(_events);
            Cells[1].RemoveController(_events);
            Form.Close();
            Form.Dispose();
            Grid.Dispose();
        }

        private void OnValueChanged(object? sender, EventArgs e)
        {
            ValueChangedCount++;
        }
    }

    private sealed class LookupInteractionGrid : BootstrapSourceGridControl
    {
        internal bool DispatchCommandKey(Keys keys)
        {
            var message = new Message();
            return base.ProcessCmdKey(ref message, keys);
        }

        internal KeyEventArgs DispatchSpecialKey(Keys keys)
        {
            var args = new KeyEventArgs(keys);
            ProcessSpecialGridKey(args);
            return args;
        }
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

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(
        IntPtr handle,
        uint message,
        IntPtr wParam,
        IntPtr lParam);
}
