using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Demo;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Demo;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class CompositionEditDemoFormTests
{
    private BootstrapTheme _originalTheme = null!;

    [SetUp]
    public void SetUp() => _originalTheme = BootstrapThemeManager.CurrentTheme;

    [TearDown]
    public void TearDown() => BootstrapThemeManager.CurrentTheme = _originalTheme;

    [Test]
    public void EditForm_InvalidSubmitKeepsDraftAndDoesNotSave()
    {
        using var form = CreateForm();
        Show(form);
        var code = Find<BootstrapTextBox>(form, "CodeInput");
        var name = Find<BootstrapTextBox>(form, "NameInput");
        var notes = Find<BootstrapTextBox>(form, "NotesInput");
        code.Text = "  ";
        name.Text = string.Empty;
        notes.Text = "Bản nháp chưa lưu vẫn giữ nguyên";
        var feedback = Find<Label>(form, "SaveFeedback").Text;
        Find<BootstrapButton>(form, "SaveButton").PerformClick();
        Pump();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(code.Text, Is.EqualTo("  "));
            Assert.That(notes.Text, Is.EqualTo("Bản nháp chưa lưu vẫn giữ nguyên"));
            Assert.That(code.ValidationState, Is.EqualTo(BootstrapValidationState.Invalid));
            Assert.That(name.ValidationState, Is.EqualTo(BootstrapValidationState.Invalid));
            Assert.That(code.ContainsFocus, Is.True, "First invalid native input receives focus.");
            Assert.That(Find<Label>(form, "SaveFeedback").Text, Is.EqualTo(feedback));
            Assert.That(Find<Label>(form, "ValidationSummary").Text, Does.Contain("mã").And.Contain("tên"));
            AssertDynamicMessageIsExposed(Find<Label>(form, "CodeError"));
            AssertDynamicMessageIsExposed(Find<Label>(form, "NameError"));
            AssertDynamicMessageIsExposed(Find<Label>(form, "ValidationSummary"));
        }));

        code.Text = "MỚI";
        Assert.That(code.ValidationState, Is.EqualTo(BootstrapValidationState.None));
        Assert.That(Find<Label>(form, "CodeError").Visible, Is.False);
        Assert.That(name.ValidationState, Is.EqualTo(BootstrapValidationState.Invalid));
    }

    [Test]
    public void EditForm_AccessibilityErrorsTrackValidationAndClearOnCancel()
    {
        using var form = CreateForm();
        Show(form);
        var code = Find<BootstrapTextBox>(form, "CodeInput");
        var name = Find<BootstrapTextBox>(form, "NameInput");
        var codeError = Find<Label>(form, "CodeError");
        var nameError = Find<Label>(form, "NameError");
        var summary = Find<Label>(form, "ValidationSummary");
        code.Text = "  ";
        name.Text = string.Empty;
        Find<BootstrapButton>(form, "SaveButton").PerformClick();
        Pump();

        Assert.Multiple((Action)(() =>
        {
            AssertDynamicMessageIsExposed(codeError);
            AssertDynamicMessageIsExposed(nameError);
            AssertDynamicMessageIsExposed(summary);
            Assert.That(code.ContainsFocus, Is.True);
        }));

        code.Text = "MỚI";
        Pump();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(codeError.Visible, Is.False);
            Assert.That(codeError.Text, Is.Empty);
            Assert.That(nameError.Visible, Is.True);
            AssertDynamicMessageIsExposed(nameError);
            AssertDynamicMessageIsExposed(summary);
            Assert.That(summary.AccessibilityObject.Name, Does.Not.Contain("mã nhận diện và tên"));
        }));

        Find<BootstrapButton>(form, "CancelButton").PerformClick();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(codeError.Visible, Is.False);
            Assert.That(codeError.Text, Is.Empty);
            Assert.That(nameError.Visible, Is.False);
            Assert.That(nameError.Text, Is.Empty);
            Assert.That(summary.Visible, Is.False);
            Assert.That(summary.Text, Is.Empty);
        }));
    }

    [Test]
    public void EditForm_AccessibilityFeedbackReflectsCommittedSave()
    {
        using var form = CreateForm();
        Show(form);
        var code = Find<BootstrapTextBox>(form, "CodeInput");
        var name = Find<BootstrapTextBox>(form, "NameInput");
        var feedback = Find<Label>(form, "SaveFeedback");
        AssertDynamicMessageIsExposed(feedback);
        code.Text = "MÃ-ĐÃ-LƯU";
        name.Text = "Tên đã lưu";
        Find<BootstrapButton>(form, "SaveButton").PerformClick();
        AssertDynamicMessageIsExposed(feedback);
        Assert.That(feedback.AccessibilityObject.Name, Does.Contain("lần 1"));

        var committedFeedback = feedback.Text;
        code.Text = string.Empty;
        Find<BootstrapButton>(form, "SaveButton").PerformClick();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(code.Text, Is.Empty, "Invalid save preserves the draft.");
            Assert.That(feedback.Text, Is.EqualTo(committedFeedback));
            AssertDynamicMessageIsExposed(feedback);
        }));
    }

    [Test]
    public void EditForm_ValidSaveAndCancelUseCommittedSnapshot()
    {
        using var form = CreateForm();
        Show(form);
        var code = Find<BootstrapTextBox>(form, "CodeInput");
        var name = Find<BootstrapTextBox>(form, "NameInput");
        var group = Find<BootstrapComboBox>(form, "GroupInput");
        var active = Find<BootstrapCheckBox>(form, "ActiveInput");
        var notes = Find<BootstrapTextBox>(form, "NotesInput");
        code.Text = "MÃ-ĐÃ-LƯU";
        name.Text = "Tên tiếng Việt đã lưu";
        group.SelectedIndex = 1;
        active.Checked = false;
        notes.Text = "Ghi chú đã lưu";
        Find<BootstrapButton>(form, "SaveButton").PerformClick();
        Assert.That(Find<Label>(form, "SaveFeedback").Text, Does.Contain("1"));
        var savedFeedback = Find<Label>(form, "SaveFeedback").Text;
        code.Text = "";
        name.Text = "";
        group.SelectedIndex = 0;
        active.Checked = true;
        notes.Text = "Nháp tiếp theo";
        Find<BootstrapButton>(form, "SaveButton").PerformClick();
        Assert.That(Find<Label>(form, "SaveFeedback").Text, Is.EqualTo(savedFeedback));
        Find<BootstrapButton>(form, "CancelButton").PerformClick();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(code.Text, Is.EqualTo("MÃ-ĐÃ-LƯU"));
            Assert.That(name.Text, Is.EqualTo("Tên tiếng Việt đã lưu"));
            Assert.That(group.SelectedIndex, Is.EqualTo(1));
            Assert.That(active.Checked, Is.False);
            Assert.That(notes.Text, Is.EqualTo("Ghi chú đã lưu"));
            Assert.That(code.ValidationState, Is.EqualTo(BootstrapValidationState.None));
            Assert.That(name.ValidationState, Is.EqualTo(BootstrapValidationState.None));
            Assert.That(Find<Label>(form, "ValidationSummary").Visible, Is.False);
        }));
        Find<BootstrapButton>(form, "SaveButton").PerformClick();
        Assert.That(Find<Label>(form, "SaveFeedback").Text, Does.Contain("2"));
    }

    [TestCase(BootstrapThemeMode.Light)]
    [TestCase(BootstrapThemeMode.Dark)]
    public void EditForm_ThemeAndNarrowLayoutPreserveInputAndReachableCommands(BootstrapThemeMode mode)
    {
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode == BootstrapThemeMode.Light ? BootstrapThemeMode.Dark : BootstrapThemeMode.Light);
        using var form = CreateForm();
        form.ClientSize = new Size(360, 320);
        Show(form);
        var name = Find<BootstrapTextBox>(form, "NameInput");
        var native = Descendants(name).OfType<TextBox>().Single();
        name.Text = "Nội dung dài đang nhập và chưa được lưu";
        native.Select(3, 5);
        var handle = native.Handle;
        var group = Find<BootstrapComboBox>(form, "GroupInput");
        group.SelectedIndex = 1;
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode);
        Pump();
        var save = Find<BootstrapButton>(form, "SaveButton");
        var cancel = Find<BootstrapButton>(form, "CancelButton");
        Assert.Multiple((Action)(() =>
        {
            Assert.That(Find<BootstrapTextBox>(form, "NameInput"), Is.SameAs(name));
            Assert.That(native.Handle, Is.EqualTo(handle));
            Assert.That(name.Text, Is.EqualTo("Nội dung dài đang nhập và chưa được lưu"));
            Assert.That(native.SelectionStart, Is.EqualTo(3));
            Assert.That(native.SelectionLength, Is.EqualTo(5));
            Assert.That(group.SelectedIndex, Is.EqualTo(1));
            Assert.That(form.ClientRectangle.Contains(form.RectangleToClient(save.RectangleToScreen(save.ClientRectangle))), Is.True);
            Assert.That(form.ClientRectangle.Contains(form.RectangleToClient(cancel.RectangleToScreen(cancel.ClientRectangle))), Is.True);
            Assert.That(Find<Panel>(form, "EditorScroll").AutoScroll, Is.True);
        }));
        var disposedColor = form.BackColor;
        form.Dispose();
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode == BootstrapThemeMode.Light ? BootstrapThemeMode.Dark : BootstrapThemeMode.Light);
        Assert.That(form.BackColor, Is.EqualTo(disposedColor), "Disposed page no longer receives theme callback.");
    }

    [Test]
    public void EditForm_NativeKeysCorrectValidateSaveAndCancel()
    {
        using var form = CreateForm();
        Show(form);
        var code = Find<BootstrapTextBox>(form, "CodeInput");
        var name = Find<BootstrapTextBox>(form, "NameInput");
        code.Text = "";
        name.Text = "";
        Find<BootstrapButton>(form, "SaveButton").PerformClick();
        Pump();
        Assert.That(code.ContainsFocus, Is.True);
        var native = Descendants(code).OfType<TextBox>().Single();
        foreach (var character in "ABC")
        {
            SendKeys.SendWait(character.ToString());
            Pump();
            Assert.That(native.SelectionStart, Is.EqualTo(code.Text.Length));
            Assert.That(code.ContainsFocus, Is.True);
        }
        Assert.That(code.Text, Is.EqualTo("ABC"));
        SendKeys.SendWait("{TAB}");
        Pump();
        Assert.That(name.ContainsFocus, Is.True);
        SendKeys.SendWait("Name");
        Pump();
        SendKeys.SendWait("+{TAB}");
        Pump();
        Assert.That(code.ContainsFocus, Is.True);
        Find<BootstrapButton>(form, "SaveButton").PerformClick();
        Assert.That(Find<Label>(form, "SaveFeedback").Text, Does.Contain("1"));
        code.Text = "changed";
        Find<BootstrapButton>(form, "CancelButton").PerformClick();
        Assert.That(code.Text, Is.EqualTo("ABC"));
    }

    private static Form CreateForm()
    {
        var type = typeof(MainForm).Assembly.GetType("MyDmsVn.Bootstrap5WinFormUI.Demo.CompositionEditDemoForm");
        Assert.That(type, Is.Not.Null, "UIC-02 sample has not been implemented.");
        return (Form)Activator.CreateInstance(type!)!;
    }

    private static void Show(Form form)
    {
        form.Show();
        form.Activate();
        Pump();
    }

    private static void Pump()
    {
        for (var index = 0; index < 4; index++) Application.DoEvents();
    }

    private static T Find<T>(Control root, string name) where T : Control => Descendants(root).OfType<T>().Single(c => c.Name == name);

    private static void AssertDynamicMessageIsExposed(Label label)
    {
        Assert.That(label.AccessibleName, Is.Null);
        Assert.That(label.Text, Is.Not.Empty);
        Assert.That(label.AccessibilityObject.Name, Is.EqualTo(label.Text));
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
}
