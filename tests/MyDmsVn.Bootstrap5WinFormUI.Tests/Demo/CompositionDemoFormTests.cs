using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Demo;
using MyDmsVn.Bootstrap5WinFormUI.Tests.Infrastructure;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Demo;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class CompositionDemoFormTests
{
    private BootstrapTheme? _original;

    [SetUp]
    public void SetUp()
    {
        _original = BootstrapThemeManager.CurrentTheme;
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(BootstrapThemeMode.Light);
    }

    [TearDown]
    public void TearDown() => BootstrapThemeManager.CurrentTheme = _original!;

    [Test]
    public void FilterResults_UnavailableMessageFitsNarrowGridAndCountRemainsUnknown()
    {
        using var form = CreateList();
        form.ClientSize = new Size(480, 430);
        form.Show();
        Find<BootstrapComboBox>(form, "ScenarioSelector").SelectedIndex = 3;
        Application.DoEvents();
        var grid = Find<BootstrapDataGridView>(form, "ResultsGrid");
        using var scaledFont = new Font(grid.Font.FontFamily, grid.Font.SizeInPoints * 1.25f, grid.Font.Style, GraphicsUnit.Point);
        var textSize = TextRenderer.MeasureText(grid.EmptyStateText, scaledFont);
        Assert.That(textSize.Width, Is.LessThanOrEqualTo(grid.ClientSize.Width - grid.RowHeadersWidth - 24));
        Assert.That(Find<Label>(form, "ResultSummary").Text, Does.Contain("chưa biết"));
    }

    [Test]
    public void FilterResults_SearchExcludesStatusAndStatusFilterRemainsIndependent()
    {
        using var form = CreateList();
        form.Show();
        var search = Find<BootstrapTextBox>(form, "SearchInput");
        var status = Find<BootstrapComboBox>(form, "StatusFilter");
        var grid = Find<BootstrapDataGridView>(form, "ResultsGrid");
        search.Text = "hoạt động";
        Assert.That(grid.Rows.Count, Is.Zero);
        search.Text = "";
        status.SelectedIndex = 1;
        Assert.That(grid.Rows.Count, Is.EqualTo(2));
        status.SelectedIndex = 2;
        Assert.That(grid.Rows.Count, Is.EqualTo(2));
    }

    [Test]
    public void ProofViewportFitsLowWorkingAreaAndRejectsUnavailableSpace()
    {
        var type = typeof(MainForm).Assembly.GetType("MyDmsVn.Bootstrap5WinFormUI.Demo.CompositionDemoProof")!;
        var method = type.GetMethod("FitViewport", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        var fitted = (Size)method!.Invoke(null, new object[] { new Size(1280, 800), new Size(1366, 728), new Size(900, 600) })!;
        Assert.That(fitted, Is.EqualTo(new Size(1280, 728)));
        var small = (Size)method.Invoke(null, new object[] { new Size(900, 600), new Size(1366, 728), new Size(900, 600) })!;
        Assert.That(small, Is.EqualTo(new Size(900, 600)));
        Assert.Throws<System.Reflection.TargetInvocationException>((Action)(() => method.Invoke(null, new object[] { new Size(900, 600), new Size(800, 500), new Size(900, 600) })));
    }

    [Test]
    public void FilterResults_QueryChangesRowsCountWithoutResettingSearchBuffer()
    {
        using var form = CreateList();
        form.Show();
        var grid = Find<BootstrapDataGridView>(form, "ResultsGrid");
        DataGridViewTestGuard.FailOnDataError(grid);
        var search = Find<BootstrapTextBox>(form, "SearchInput");
        var editor = Descendants<TextBox>(search).Single();
        search.Text = "  Kho  ";
        editor.Select(3, 2);
        search.Focus();
        Application.DoEvents();
        Find<BootstrapComboBox>(form, "StatusFilter").SelectedIndex = 1;
        Application.DoEvents();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(grid.Rows.Count, Is.EqualTo(2));
            Assert.That(Find<Label>(form, "ResultSummary").Text, Does.Contain("2 / 4"));
            Assert.That(search.Text, Is.EqualTo("  Kho  "));
            Assert.That(editor.SelectionStart, Is.EqualTo(3));
            Assert.That(editor.SelectionLength, Is.EqualTo(2));
            Assert.That(search.ContainsFocus, Is.True);
            Assert.That(grid.ReadOnly, Is.True);
            Assert.That(grid.AllowUserToAddRows || grid.AllowUserToDeleteRows, Is.False);
        }));
        Find<BootstrapComboBox>(form, "ScenarioSelector").SelectedIndex = 3;
        Assert.That(Find<Label>(form, "ResultSummary").Text, Does.Not.Contain("0 /"));
        Assert.That(grid.Rows.Count, Is.Zero);
    }

    [Test]
    public void FilterResults_ClearAndEmptyStatesAreDistinct()
    {
        using var form = CreateList();
        form.Show();
        var grid = Find<BootstrapDataGridView>(form, "ResultsGrid");
        var search = Find<BootstrapTextBox>(form, "SearchInput");
        var scenario = Find<BootstrapComboBox>(form, "ScenarioSelector");
        var summary = Find<Label>(form, "ResultSummary");
        search.Text = "unmatched";
        var noMatches = grid.EmptyStateText;
        Assert.That(grid.Rows.Count, Is.Zero);
        Assert.That(summary.Text, Does.Contain("0 / 4"));
        Find<BootstrapButton>(form, "ClearFiltersButton").PerformClick();
        Assert.That(grid.Rows.Count, Is.EqualTo(4));
        scenario.SelectedIndex = 1;
        Assert.That(grid.EmptyStateText, Is.Not.EqualTo(noMatches));
        Assert.That(summary.Text, Does.Contain("0 / 0"));
        scenario.SelectedIndex = 2;
        Assert.That(grid.Loading, Is.True);
        Assert.That(summary.Text, Does.Not.Contain("0 /"));
        scenario.SelectedIndex = 0;
        Find<BootstrapButton>(form, "RefreshButton").PerformClick();
        Assert.That(grid.Rows.Count, Is.EqualTo(4));
        Assert.That(Descendants<BootstrapButton>(form).Select(x => x.Name), Is.EquivalentTo(new[] { "RefreshButton", "ClearFiltersButton" }));
    }

    [Test]
    public void FilterResults_AccessibilitySummaryExposesCurrentStateAndCount()
    {
        using var form = CreateList();
        form.Show();
        var summary = Find<Label>(form, "ResultSummary");
        var search = Find<BootstrapTextBox>(form, "SearchInput");
        var scenario = Find<BootstrapComboBox>(form, "ScenarioSelector");

        AssertSummaryExposesCurrentText(summary, "4 / 4");
        search.Text = "unmatched";
        AssertSummaryExposesCurrentText(summary, "0 / 4");
        scenario.SelectedIndex = 1;
        AssertSummaryExposesCurrentText(summary, "0 / 0");
        scenario.SelectedIndex = 2;
        AssertSummaryExposesCurrentText(summary, "Đang tải", "chưa biết");
        Assert.That(summary.AccessibilityObject.Name, Does.Not.Contain("0 /"));
        scenario.SelectedIndex = 3;
        AssertSummaryExposesCurrentText(summary, "Không khả dụng", "chưa biết");
        Assert.That(summary.AccessibilityObject.Name, Does.Not.Contain("0 /"));
    }

    [Test]
    public void FilterResults_NativePerCharacterEnterAndTabKeepQueryState()
    {
        using var form = CreateList();
        form.Show();
        form.Activate();
        var search = Find<BootstrapTextBox>(form, "SearchInput");
        var editor = Descendants<TextBox>(search).Single();
        var grid = Find<BootstrapDataGridView>(form, "ResultsGrid");
        search.Focus();
        var expected = "";
        foreach (var character in new[] { "K", "h", "o" })
        {
            SendKeys.SendWait(character);
            Application.DoEvents();
            expected += character;
            Assert.Multiple((Action)(() =>
            {
                Assert.That(search.Text, Is.EqualTo(expected));
                Assert.That(editor.SelectionStart, Is.EqualTo(expected.Length));
                Assert.That(editor.SelectionLength, Is.Zero);
                Assert.That(search.ContainsFocus, Is.True);
                Assert.That(grid.Rows.Count, Is.EqualTo(3));
                Assert.That(Find<Label>(form, "ResultSummary").Text, Does.Contain("3 / 4"));
            }));
        }
        SendKeys.SendWait("{ENTER}");
        Application.DoEvents();
        Assert.That(search.Text, Is.EqualTo("Kho"));
        Assert.That(editor.SelectionStart, Is.EqualTo(3));
        Assert.That(search.ContainsFocus, Is.True);
        SendKeys.SendWait("{TAB}");
        Application.DoEvents();
        Assert.That(Find<BootstrapComboBox>(form, "StatusFilter").ContainsFocus, Is.True);
        SendKeys.SendWait("+{TAB}");
        Application.DoEvents();
        Assert.That(search.ContainsFocus, Is.True);
        Assert.That(grid.Rows.Count, Is.EqualTo(3));
    }

    [Test]
    public void FilterResults_ThemeNarrowLayoutAndPageDisposalPreserveOwnership()
    {
        using var form = CreateList();
        form.ClientSize = new Size(480, 430);
        form.Show();
        var search = Find<BootstrapTextBox>(form, "SearchInput");
        var grid = Find<BootstrapDataGridView>(form, "ResultsGrid");
        search.Text = "Kho";
        grid.CurrentCell = grid.Rows[1].Cells[0];
        var key = grid.CurrentRow!.Cells[0].Value;
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(BootstrapThemeMode.Dark);
        Application.DoEvents();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(Find<BootstrapTextBox>(form, "SearchInput"), Is.SameAs(search));
            Assert.That(search.Text, Is.EqualTo("Kho"));
            Assert.That(grid.CurrentRow!.Cells[0].Value, Is.EqualTo(key));
            Assert.That(grid.ClientSize.Height, Is.GreaterThan(90));
            Assert.That(Find<BootstrapButton>(form, "ClearFiltersButton").Right, Is.LessThanOrEqualTo(480));
        }));
        using var main = new MainForm();
        main.Show();
        var sidebar = Descendants<BootstrapSidebar>(main).Single();
        sidebar.SelectedItem = sidebar.Items.Single(x => x.Text == "Compositions");
        var hosted = Descendants<Form>(main).Single(x => x.GetType().Name == "CompositionListDemoForm");
        var edit = Descendants<Form>(main).Single(x => x.GetType().Name == "CompositionEditDemoForm");
        var tabs = Descendants<TabControl>(main).Single();
        Assert.That(tabs.TabPages.Count, Is.EqualTo(2));
        tabs.SelectedIndex = 1;
        Application.DoEvents();
        Assert.That(edit.Visible, Is.True);
        sidebar.SelectedItem = sidebar.Items[0];
        Assert.That(hosted.IsDisposed, Is.True);
        Assert.That(edit.IsDisposed, Is.True);
        var prior = hosted.BackColor;
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(BootstrapThemeMode.Light);
        Assert.That(hosted.BackColor, Is.EqualTo(prior), "Disposed sample must unsubscribe from theme callbacks.");
    }

    private static Form CreateList()
    {
        var type = typeof(MainForm).Assembly.GetType("MyDmsVn.Bootstrap5WinFormUI.Demo.CompositionListDemoForm");
        Assert.That(type, Is.Not.Null, "CMP-T01 needs the runnable Filter Results recipe.");
        return (Form)Activator.CreateInstance(type!)!;
    }

    private static T Find<T>(Control root, string name) where T : Control => Descendants<T>(root).Single(x => x.Name == name);

    private static void AssertSummaryExposesCurrentText(Label summary, params string[] expectedFragments)
    {
        Assert.That(summary.AccessibleName, Is.Null);
        Assert.That(summary.AccessibilityObject.Name, Is.EqualTo(summary.Text));
        foreach (var fragment in expectedFragments) Assert.That(summary.AccessibilityObject.Name, Does.Contain(fragment));
    }

    private static IEnumerable<T> Descendants<T>(Control root) where T : Control
    {
        foreach (Control child in root.Controls)
        {
            if (child is T match) yield return match;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
}
