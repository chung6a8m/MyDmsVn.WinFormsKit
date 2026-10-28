using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapTextBoxMultilineTests
{
    [Test]
    public void MultilineUsesCallerHeightAndReturningToSingleLineRestoresGeometry()
    {
        using var input = new BootstrapTextBox { Size = new Size(280, 116) };
        var native = NativeEditor(input);
        input.PerformLayout();
        var singleLineBounds = native.Bounds;

        input.Multiline = true;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.Height, Is.EqualTo(116), "Multiline must retain the caller's outer height.");
            Assert.That(native.Height, Is.GreaterThan(native.PreferredHeight * 2));
            Assert.That(native.Top, Is.LessThan(singleLineBounds.Top));
            Assert.That(native.Left, Is.EqualTo(singleLineBounds.Left));
            Assert.That(native.Right, Is.EqualTo(singleLineBounds.Right));
            Assert.That(input.ClientRectangle.Contains(native.Bounds), Is.True);
        }));

        input.Multiline = false;

        Assert.That(native.Bounds, Is.EqualTo(singleLineBounds));
        Assert.That(input.Height, Is.EqualTo(116));
    }

    [TestCase(typeof(BootstrapTextBox))]
    [TestCase(typeof(BootstrapFormattedTextBox))]
    [TestCase(typeof(BootstrapDecimalInput))]
    [TestCase(typeof(BootstrapLookupBox))]
    public void ExistingInputFamiliesKeepTheirSingleLineEditorAndKeyboardDefaults(Type inputType)
    {
        using var input = (BootstrapTextBox)Activator.CreateInstance(inputType)!;
        input.Size = new Size(280, 116);
        input.PerformLayout();
        var native = NativeEditor(input);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.Multiline, Is.False);
            Assert.That(native.Height, Is.EqualTo(native.PreferredHeight));
            Assert.That(native.Top, Is.EqualTo((input.ClientSize.Height - native.Height) / 2));
            Assert.That(native.AcceptsTab, Is.False);
            Assert.That(native.AcceptsReturn, Is.False);
            Assert.That(native.TabStop, Is.False);
            Assert.That(input.TabStop, Is.True);
        }));
    }

    [Test]
    public void MultilineResizeAndFontChangesKeepTheEditorInsideTheAvailableArea()
    {
        using var input = new BootstrapTextBox { Multiline = true, Size = new Size(280, 116) };
        var native = NativeEditor(input);
        var originalHeight = native.Height;
        using var font = new Font(input.Font.FontFamily, 16f);

        input.Font = font;
        input.Size = new Size(190, 164);
        input.PerformLayout();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.Height, Is.GreaterThan(originalHeight));
            Assert.That(native.Height, Is.GreaterThan(native.PreferredHeight * 2));
            Assert.That(native.Top, Is.GreaterThan(0));
            Assert.That(native.Bottom, Is.LessThan(input.ClientSize.Height));
            Assert.That(input.ClientRectangle.Contains(native.Bounds), Is.True);
        }));
    }

    [Test]
    public void MultilinePlaceholderAndAccessoriesLeaveTheNativeEditorUncovered()
    {
        using var input = new BootstrapTextBox
        {
            Multiline = true,
            Size = new Size(320, 116),
            PlaceholderText = "Description",
            Icon = IconDescriptor.Framework(FrameworkIconGlyph.Check),
            TrailingIcon = IconDescriptor.Framework(FrameworkIconGlyph.Close),
            ShowClearButton = true
        };
        var accessory = new Button();
        input.SetFrameworkTrailingAccessory(accessory);
        var native = NativeEditor(input);
        var placeholder = input.Controls.OfType<Label>().Single();
        var clear = input.Controls.OfType<Button>().Single(control => !ReferenceEquals(control, accessory));
        input.PerformLayout();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(placeholder.TextAlign, Is.EqualTo(ContentAlignment.TopLeft));
            Assert.That(placeholder.Bounds, Is.EqualTo(native.Bounds));
            Assert.That(native.Left, Is.GreaterThan(0));
            Assert.That(native.Right, Is.LessThan(accessory.Left));
        }));

        input.Text = "First line\r\nSecond line";
        Assert.That(placeholder.Visible, Is.False);
        Assert.That(clear.Visible, Is.True);
        Assert.That(native.Right, Is.LessThanOrEqualTo(clear.Left));
        Assert.That(clear.Right, Is.LessThanOrEqualTo(accessory.Left));

        input.ReadOnly = true;
        Assert.That(clear.Visible, Is.False);
        Assert.That(native.Right, Is.LessThan(accessory.Left));
        input.Multiline = false;
        Assert.That(placeholder.TextAlign, Is.EqualTo(ContentAlignment.MiddleLeft));
    }

    [Test]
    public void NativeWrappingAndScrollbarStylesFollowThePublicMultilineSettings()
    {
        using var input = new BootstrapTextBox
        {
            Multiline = true,
            Size = new Size(180, 88),
            ScrollBars = ScrollBars.Both,
            Text = string.Concat(Enumerable.Repeat("long permission description ", 20)) + "\r\nLast line"
        };
        var native = NativeEditor(input);

        Assert.That(SendMessage(native.Handle, EmGetLineCount, IntPtr.Zero, IntPtr.Zero).ToInt32(), Is.GreaterThan(2));
        Assert.That(GetWindowLong(native.Handle, GwlStyle) & WsHScroll, Is.Zero);

        input.WordWrap = false;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(SendMessage(native.Handle, EmGetLineCount, IntPtr.Zero, IntPtr.Zero).ToInt32(), Is.EqualTo(2));
            Assert.That(GetWindowLong(native.Handle, GwlStyle) & WsHScroll, Is.Not.Zero);
            Assert.That(GetWindowLong(native.Handle, GwlStyle) & WsVScroll, Is.Not.Zero);
        }));

        input.ScrollBars = ScrollBars.Vertical;
        Assert.That(GetWindowLong(native.Handle, GwlStyle) & WsHScroll, Is.Zero);
        Assert.That(GetWindowLong(native.Handle, GwlStyle) & WsVScroll, Is.Not.Zero);
        Assert.Throws<InvalidEnumArgumentException>((Action)(() => input.ScrollBars = (ScrollBars)99));
        Assert.That(input.ScrollBars, Is.EqualTo(ScrollBars.Vertical));
    }

    [Test]
    public void ReadOnlyMultilineKeepsNativeFocusSelectionCopyAndScrollThroughThemeChanges()
    {
        var previousTheme = BootstrapThemeManager.CurrentTheme;
        var previousClipboard = Clipboard.GetDataObject();
        try
        {
            using var form = new Form { ClientSize = new Size(360, 180) };
            using var input = new BootstrapTextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Size = new Size(320, 116),
                Text = string.Join("\r\n", Enumerable.Range(1, 60).Select(index => "Permission " + index))
            };
            using var after = new Button { Text = "After", Top = 130, TabIndex = 1 };
            form.Controls.AddRange(new Control[] { input, after });
            form.Show();
            form.Activate();
            input.Focus();
            Application.DoEvents();
            var native = NativeEditor(input);
            var handle = native.Handle;
            var originalText = input.Text;
            native.Select(2, 5);
            Assert.That(native.Focused, Is.True);

            SendMessage(handle, WmCopy, IntPtr.Zero, IntPtr.Zero);
            Assert.That(Clipboard.GetText(), Is.EqualTo("rmiss"));
            SendMessage(handle, WmChar, new IntPtr('x'), new IntPtr(1));
            Assert.That(input.Text, Is.EqualTo(originalText));
            SendMessage(handle, WmVScroll, new IntPtr(SbBottom), IntPtr.Zero);
            var firstVisibleLine = SendMessage(handle, EmGetFirstVisibleLine, IntPtr.Zero, IntPtr.Zero).ToInt32();
            Assert.That(firstVisibleLine, Is.GreaterThan(0));

            foreach (var mode in new[] { BootstrapThemeMode.Dark, BootstrapThemeMode.Light })
            {
                BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode);
                Application.DoEvents();
                Assert.Multiple((Action)(() =>
                {
                    Assert.That(native.Handle, Is.EqualTo(handle));
                    Assert.That(native.Focused, Is.True);
                    Assert.That(native.SelectionStart, Is.EqualTo(2));
                    Assert.That(native.SelectionLength, Is.EqualTo(5));
                    Assert.That(input.Text, Is.EqualTo(originalText));
                    Assert.That(native.BackColor, Is.EqualTo(BootstrapThemeManager.CurrentTheme.Colors.SurfaceSecondary));
                    Assert.That(native.ForeColor, Is.EqualTo(BootstrapThemeManager.CurrentTheme.Colors.Text));
                    Assert.That(SendMessage(handle, EmGetFirstVisibleLine, IntPtr.Zero, IntPtr.Zero).ToInt32(), Is.EqualTo(firstVisibleLine));
                }));
            }

            var tab = Message.Create(handle, WmKeyDown, new IntPtr((int)Keys.Tab), IntPtr.Zero);
            native.PreProcessMessage(ref tab);
            Assert.That(after.Focused, Is.True, "Read-only multiline text must not trap Tab inside its native editor.");
            form.Close();
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = previousTheme;
            if (previousClipboard is not null) Clipboard.SetDataObject(previousClipboard, true);
            else Clipboard.Clear();
        }
    }

    [Test]
    public void NativeAccessibilityRequestsUseWrapperMetadataAndKeepNativeReadOnlyState()
    {
        using var input = new BootstrapTextBox
        {
            Multiline = true,
            ReadOnly = true,
            AccessibleName = "Permission details",
            AccessibleDescription = "Selected permission definition",
            Text = "Read-only definition"
        };
        var native = NativeEditor(input);
        RequestNativeAccessibility(native);
        var accessible = native.AccessibilityObject;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(accessible.Name, Is.EqualTo("Permission details"));
            Assert.That(accessible.Description, Is.EqualTo("Selected permission definition"));
            Assert.That(accessible.Role, Is.EqualTo(AccessibleRole.Text));
            Assert.That(accessible.State.HasFlag(AccessibleStates.ReadOnly), Is.True);
        }));

        input.AccessibleName = "Effective permission explanation";
        input.AccessibleDescription = "Loaded decision sources";
        RequestNativeAccessibility(native);

        Assert.That(native.AccessibilityObject, Is.SameAs(accessible), "Metadata forwarding must retain the native accessibility provider.");
        Assert.That(accessible.Name, Is.EqualTo("Effective permission explanation"));
        Assert.That(accessible.Description, Is.EqualTo("Loaded decision sources"));
    }

    [Test]
    public void NativeFocusRefreshesMetadataAssignedAfterHandleCreation()
    {
        using var form = new Form();
        using var input = new BootstrapTextBox { Multiline = true, ReadOnly = true };
        using var other = new Button { Top = 150 };
        form.Controls.AddRange(new Control[] { input, other });
        form.Show();
        other.Focus();
        Application.DoEvents();
        var native = NativeEditor(input);
        _ = native.Handle;

        input.AccessibleName = "Loaded permission explanation";
        input.AccessibleDescription = "Selection can be copied";
        native.Focus();
        Application.DoEvents();

        Assert.That(native.Focused, Is.True);
        Assert.That(native.AccessibilityObject.Name, Is.EqualTo("Loaded permission explanation"));
        Assert.That(native.AccessibilityObject.Description, Is.EqualTo("Selection can be copied"));
        form.Close();
    }

    [Test]
    public void AccessibilityForwardingSurvivesNativeStyleHandleRecreation()
    {
        using var input = new BootstrapTextBox { Multiline = true, AccessibleName = "Original details" };
        var native = NativeEditor(input);
        RequestNativeAccessibility(native);

        input.WordWrap = false;
        input.ScrollBars = ScrollBars.Both;
        _ = native.Handle;
        input.AccessibleName = "Details after style changes";
        RequestNativeAccessibility(native);

        Assert.That(NativeEditor(input), Is.SameAs(native));
        Assert.That(native.AccessibilityObject.Name, Is.EqualTo("Details after style changes"));
        input.Multiline = false;
        _ = native.Handle;
        input.AccessibleName = "Single-line details";
        RequestNativeAccessibility(native);
        Assert.That(native.AccessibilityObject.Name, Is.EqualTo("Single-line details"));
    }

    [Test]
    public void AbsentWrapperMetadataPreservesExplicitNativeSearchAccessibility()
    {
        using var input = new BootstrapSelectSearchTextBox();
        var native = NativeEditor(input);
        RequestNativeAccessibility(native);
        Assert.That(native.AccessibilityObject.Name, Is.EqualTo("Search"));
        Assert.That(native.AccessibilityObject.Description, Is.EqualTo("Filters BootstrapSelect results."));

        input.AccessibleName = "Permission search";
        input.AccessibleDescription = "Search loaded permissions";
        RequestNativeAccessibility(native);
        Assert.That(native.AccessibilityObject.Name, Is.EqualTo("Permission search"));
        Assert.That(native.AccessibilityObject.Description, Is.EqualTo("Search loaded permissions"));

        input.AccessibleName = null;
        input.AccessibleDescription = null;
        RequestNativeAccessibility(native);
        Assert.That(native.AccessibilityObject.Name, Is.EqualTo("Search"));
        Assert.That(native.AccessibilityObject.Description, Is.EqualTo("Filters BootstrapSelect results."));
    }

    [Test]
    public void DesignerSerializesOnlyNonDefaultMultilineSettings()
    {
        using var input = new BootstrapTextBox();
        var properties = TypeDescriptor.GetProperties(input);
        foreach (var name in new[] { nameof(BootstrapTextBox.Multiline), nameof(BootstrapTextBox.WordWrap), nameof(BootstrapTextBox.ScrollBars) })
            Assert.That(properties[name]!.ShouldSerializeValue(input), Is.False, name);

        input.Multiline = true;
        input.WordWrap = false;
        input.ScrollBars = ScrollBars.Vertical;

        foreach (var name in new[] { nameof(BootstrapTextBox.Multiline), nameof(BootstrapTextBox.WordWrap), nameof(BootstrapTextBox.ScrollBars) })
        {
            Assert.That(properties[name]!.ShouldSerializeValue(input), Is.True, name);
            properties[name]!.ResetValue(input);
            Assert.That(properties[name]!.ShouldSerializeValue(input), Is.False, name);
        }
    }

    private static TextBox NativeEditor(BootstrapTextBox input) => input.Controls.OfType<TextBox>().Single();

    private static void RequestNativeAccessibility(TextBox native)
    {
        var iid = new Guid("618736E0-3C3D-11CF-810C-00AA00389B71");
        object? accessible = null;
        try
        {
            Marshal.ThrowExceptionForHR(AccessibleObjectFromWindow(native.Handle, unchecked((uint)-4), ref iid, out accessible));
        }
        finally
        {
            if (accessible is not null && Marshal.IsComObject(accessible)) Marshal.ReleaseComObject(accessible);
        }
    }

    private const int WmKeyDown = 0x0100;
    private const int WmChar = 0x0102;
    private const int WmVScroll = 0x0115;
    private const int WmCopy = 0x0301;
    private const int EmGetLineCount = 0x00BA;
    private const int EmGetFirstVisibleLine = 0x00CE;
    private const int GwlStyle = -16;
    private const int WsHScroll = 0x00100000;
    private const int WsVScroll = 0x00200000;
    private const int SbBottom = 7;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(IntPtr handle, int index);

    [DllImport("oleacc.dll")]
    private static extern int AccessibleObjectFromWindow(IntPtr handle, uint objectId, ref Guid interfaceId,
        [MarshalAs(UnmanagedType.Interface)] out object? accessible);
}
