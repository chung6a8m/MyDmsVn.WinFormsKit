using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using MyDmsVn.BootstrapSourceGrid.Demo;
using MyDmsVn.BootstrapSourceGrid.Editors;
using NUnit.Framework;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapSourceGridDemoTypographyTests
{
    private BootstrapTheme? _originalTheme;

    [SetUp]
    public void SetUp() => _originalTheme = BootstrapThemeManager.CurrentTheme;

    [TearDown]
    public void TearDown()
    {
        if (_originalTheme is not null)
        {
            BootstrapThemeManager.CurrentTheme = _originalTheme;
        }
    }

    [Test]
    public void MainFormExposesOrderedBaseFontProfiles()
    {
        using var form = new MyDmsVn.BootstrapSourceGrid.Demo.MainForm();
        var selector = Find<ComboBox>(form, "baseFontComboBox");

        Assert.That(selector.DropDownStyle, Is.EqualTo(ComboBoxStyle.DropDownList));
        Assert.That(selector.AccessibleName, Is.EqualTo("SourceGrid demo base font profile"));
        Assert.That(selector.Items.Cast<string>(), Is.EqualTo(new[]
        {
            "Default", "Base 14px", "Base 16px",
        }));
        Assert.That(selector.SelectedIndex, Is.EqualTo(0));
        Assert.That(Find<Label>(form, "baseFontLabel").Text, Is.EqualTo("Base font"));
        Assert.That(Find<CheckBox>(form, "reducedMotionCheckBox").Text,
            Is.EqualTo("Reduced motion"));
    }

    [Test]
    public void DefaultPresetUsesExactFrameworkTypographyObject()
    {
        Assert.That(DemoTypography.ForPreset(DemoTypographyPreset.Default),
            Is.SameAs(BootstrapThemeTypography.Default));
    }

    [TestCase(1, 10.5f, 9.1875f, 10.5f, 13.125f, 15.75f)]
    [TestCase(2, 12f, 10.5f, 12f, 15f, 18f)]
    public void ProfilesProvideExactSegoeUiTokens(
        int presetValue,
        float body,
        float bodySmall,
        float label,
        float headingSmall,
        float headingMedium)
    {
        var typography = DemoTypography.ForPreset((DemoTypographyPreset)presetValue);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(typography.Body.FontFamilyName, Is.EqualTo("Segoe UI"));
            Assert.That(typography.Body.SizeInPoints, Is.EqualTo(body));
            Assert.That(typography.Body.Style, Is.EqualTo(FontStyle.Regular));
            Assert.That(typography.BodySmall.SizeInPoints, Is.EqualTo(bodySmall));
            Assert.That(typography.BodySmall.Style, Is.EqualTo(FontStyle.Regular));
            Assert.That(typography.Label.SizeInPoints, Is.EqualTo(label));
            Assert.That(typography.Label.Style, Is.EqualTo(FontStyle.Bold));
            Assert.That(typography.HeadingSmall.SizeInPoints, Is.EqualTo(headingSmall));
            Assert.That(typography.HeadingSmall.Style, Is.EqualTo(FontStyle.Bold));
            Assert.That(typography.HeadingMedium.SizeInPoints, Is.EqualTo(headingMedium));
            Assert.That(typography.HeadingMedium.Style, Is.EqualTo(FontStyle.Bold));
        }));
    }

    [Test]
    public void InvalidPresetIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() =>
            DemoTypography.ForPreset((DemoTypographyPreset)999)));
    }

    [Test]
    public void FactoryPreservesCustomTypographyReference()
    {
        var custom = CustomTypography();

        var theme = DemoThemeFactory.Create(BootstrapThemeMode.Dark, custom, true);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(theme.Typography, Is.SameAs(custom));
            Assert.That(theme.Mode, Is.EqualTo(BootstrapThemeMode.Dark));
            Assert.That(theme.ReducedMotion, Is.True);
            Assert.That(theme.Colors.Surface,
                Is.EqualTo(BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Dark).Surface));
        }));
    }

    [Test]
    public void NumericallyMatchingCustomTypographyIsStillUnknown()
    {
        var known = DemoTypography.ForPreset(DemoTypographyPreset.Base14Px);
        var custom = new BootstrapThemeTypography(
            known.Body, known.BodySmall, known.Label,
            known.HeadingSmall, known.HeadingMedium);
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Light, custom, false);

        using var form = new MainForm();

        Assert.That(Find<ComboBox>(form, "baseFontComboBox").SelectedIndex, Is.EqualTo(-1));
        Assert.That(BootstrapThemeManager.CurrentTheme.Typography, Is.SameAs(custom));
    }

    [Test]
    public void ConstructionReflectsInstalledThemeWithoutPublishing()
    {
        var custom = CustomTypography();
        var installed = DemoThemeFactory.Create(BootstrapThemeMode.Dark, custom, true);
        BootstrapThemeManager.CurrentTheme = installed;
        var notifications = 0;
        EventHandler<BootstrapThemeChangedEventArgs> count = (_, _) => notifications++;
        BootstrapThemeManager.ThemeChanged += count;
        try
        {
            using var form = new MainForm();
            Assert.That(BootstrapThemeManager.CurrentTheme, Is.SameAs(installed));
            Assert.That(Find<ComboBox>(form, "baseFontComboBox").SelectedIndex, Is.EqualTo(-1));
            Assert.That(Find<CheckBox>(form, "reducedMotionCheckBox").Checked, Is.True);
            Assert.That(notifications, Is.Zero);
        }
        finally
        {
            BootstrapThemeManager.ThemeChanged -= count;
        }
    }

    [Test]
    public void UserSettingsComposeIndependentlyAndPublishOnceEach()
    {
        using var form = new MainForm();
        ShowForm(form);
        var profile = Find<ComboBox>(form, "baseFontComboBox");
        var reduced = Find<CheckBox>(form, "reducedMotionCheckBox");
        var dark = Find<Button>(form, "darkThemeButton");
        var light = Find<Button>(form, "lightThemeButton");
        var notifications = 0;
        EventHandler<BootstrapThemeChangedEventArgs> count = (_, _) => notifications++;
        BootstrapThemeManager.ThemeChanged += count;
        try
        {
            profile.SelectedIndex = 1;
            AssertTheme(BootstrapThemeMode.Light, 10.5f, false, 1);
            dark.PerformClick();
            AssertTheme(BootstrapThemeMode.Dark, 10.5f, false, 2);
            reduced.Checked = true;
            AssertTheme(BootstrapThemeMode.Dark, 10.5f, true, 3);
            profile.SelectedIndex = 2;
            AssertTheme(BootstrapThemeMode.Dark, 12f, true, 4);
            light.PerformClick();
            AssertTheme(BootstrapThemeMode.Light, 12f, true, 5);
            profile.SelectedIndex = 0;
            Assert.That(BootstrapThemeManager.CurrentTheme.Typography,
                Is.SameAs(BootstrapThemeTypography.Default));
            AssertTheme(BootstrapThemeMode.Light,
                BootstrapThemeTypography.Default.Body.SizeInPoints, true, 6);
        }
        finally
        {
            BootstrapThemeManager.ThemeChanged -= count;
        }

        void AssertTheme(BootstrapThemeMode mode, float bodySize, bool motion, int countExpected)
        {
            var theme = BootstrapThemeManager.CurrentTheme;
            Assert.Multiple((Action)(() =>
            {
                Assert.That(theme.Mode, Is.EqualTo(mode));
                Assert.That(theme.Typography.Body.SizeInPoints, Is.EqualTo(bodySize));
                Assert.That(theme.ReducedMotion, Is.EqualTo(motion));
                Assert.That(notifications, Is.EqualTo(countExpected));
            }));
        }
    }

    [Test]
    public void UnknownTypographySurvivesModeAndMotionUntilPresetIsChosen()
    {
        var custom = CustomTypography();
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Light, custom, false);
        using var form = new MainForm();
        ShowForm(form);
        var profile = Find<ComboBox>(form, "baseFontComboBox");
        Assert.That(profile.SelectedIndex, Is.EqualTo(-1));

        Find<Button>(form, "darkThemeButton").PerformClick();
        Assert.That(BootstrapThemeManager.CurrentTheme.Typography, Is.SameAs(custom));
        Assert.That(profile.SelectedIndex, Is.EqualTo(-1));

        Find<CheckBox>(form, "reducedMotionCheckBox").Checked = true;
        Assert.That(BootstrapThemeManager.CurrentTheme.Typography, Is.SameAs(custom));

        profile.SelectedIndex = 1;
        Assert.That(BootstrapThemeManager.CurrentTheme.Typography,
            Is.SameAs(DemoTypography.ForPreset(DemoTypographyPreset.Base14Px)));
    }

    [Test]
    public void ExternalThemeChangeSynchronizesSelectorWithoutRepublishing()
    {
        using var form = new MainForm();
        var replacement = DemoThemeFactory.Create(
            BootstrapThemeMode.Dark,
            DemoTypography.ForPreset(DemoTypographyPreset.Base16Px), true);
        var notifications = 0;
        EventHandler<BootstrapThemeChangedEventArgs> count = (_, _) => notifications++;
        BootstrapThemeManager.ThemeChanged += count;
        try
        {
            BootstrapThemeManager.CurrentTheme = replacement;

            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(BootstrapThemeManager.CurrentTheme, Is.SameAs(replacement));
            Assert.That(Find<ComboBox>(form, "baseFontComboBox").SelectedIndex, Is.EqualTo(2));
            Assert.That(Find<CheckBox>(form, "reducedMotionCheckBox").Checked, Is.True);
        }
        finally
        {
            BootstrapThemeManager.ThemeChanged -= count;
        }
    }

    [Test]
    public void LiveProfileChangesKeepGridEditorsAndConsumerFont()
    {
        using var form = new MainForm();
        ShowForm(form);
        var grid = Find<BootstrapSourceGridControl>(form, "BootstrapSourceGrid");
        var text = grid[1, 2].Editor;
        var formatted = grid[1, 3].Editor;
        var lookup = grid[1, 6].Editor;
        var view = grid[2, 9].View;
        var columnWidth = grid.Columns[2].Width;
        Find<Button>(form, "consumerFontButton").PerformClick();
        var consumerFont = grid.Font;

        var profile = Find<ComboBox>(form, "baseFontComboBox");
        profile.SelectedIndex = 1;
        profile.SelectedIndex = 2;
        Find<Button>(form, "darkThemeButton").PerformClick();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(Find<BootstrapSourceGridControl>(form, "BootstrapSourceGrid"), Is.SameAs(grid));
            Assert.That(grid[1, 2].Editor, Is.SameAs(text));
            Assert.That(grid[1, 3].Editor, Is.SameAs(formatted));
            Assert.That(grid[1, 6].Editor, Is.SameAs(lookup));
            Assert.That(grid[2, 9].View, Is.SameAs(view));
            Assert.That(grid.Columns[2].Width, Is.EqualTo(columnWidth));
            Assert.That(grid.Font, Is.SameAs(consumerFont));
            Assert.That(consumerFont.FontFamily.Name, Is.EqualTo(FontFamily.GenericMonospace.Name));
        }));
    }

    [Test]
    public void ShellFontChangesWithProfileAndReusesFontForModeOnlyChange()
    {
        using var form = new MainForm();
        ShowForm(form);
        var profile = Find<ComboBox>(form, "baseFontComboBox");
        var defaultFont = form.Font;
        profile.SelectedIndex = 1;
        var base14Font = form.Font;
        Assert.That(base14Font, Is.Not.SameAs(defaultFont));
        Assert.That(base14Font.SizeInPoints, Is.EqualTo(10.5f).Within(0.01f));
        Find<Button>(form, "darkThemeButton").PerformClick();
        Assert.That(form.Font, Is.SameAs(base14Font));
        profile.SelectedIndex = 2;
        Assert.That(form.Font.SizeInPoints, Is.EqualTo(12f).Within(0.01f));
        Assert.That(form.Font, Is.Not.SameAs(base14Font));
    }

    [Test]
    public void UnavailableRequestedFamilyDoesNotReplaceShellFontOnModeChange()
    {
        var missing = new BootstrapFontToken("DefinitelyMissingSourceGridFontFamily", 11f);
        var custom = new BootstrapThemeTypography(
            missing, missing, missing, missing, missing);
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Light, custom, false);
        using var form = new MainForm();
        var grid = Find<BootstrapSourceGridControl>(form, "BootstrapSourceGrid");
        Assert.That(grid.OwnedThemeFont, Is.Not.Null);
        Assert.DoesNotThrow((Action)(() => TextRenderer.MeasureText("grid", grid.Font)));
        foreach (Control child in grid.Controls)
        {
            Assert.DoesNotThrow((Action)(() =>
            {
                var handle = child.Font.ToHfont();
                DeleteObject(handle);
            }),
                child.GetType().Name);
        }
        ShowForm(form);
        var originalFont = form.Font;

        Find<Button>(form, "darkThemeButton").PerformClick();

        Assert.That(BootstrapThemeManager.CurrentTheme.Typography, Is.SameAs(custom));
        Assert.That(form.Font, Is.SameAs(originalFont));
    }

    [Test]
    public void DiagnosticsDescribeCurrentProfileAndMotion()
    {
        using var form = new MainForm();
        Find<ComboBox>(form, "baseFontComboBox").SelectedIndex = 1;
        Find<CheckBox>(form, "reducedMotionCheckBox").Checked = true;
        var diagnostics = Find<Label>(form, "diagnosticsLabel").Text;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(diagnostics, Does.Contain("Theme: Light"));
            Assert.That(diagnostics, Does.Contain("Profile: Base 14px"));
            Assert.That(diagnostics, Does.Contain("Body: 10.5pt"));
            Assert.That(diagnostics, Does.Contain("Reduced motion: True"));
            Assert.That(diagnostics, Does.Contain("DeviceDpi:"));
            Assert.That(diagnostics, Does.Contain("Font ownership:"));
        }));
    }

    [TestCase(0, 960, 640)]
    [TestCase(1, 960, 640)]
    [TestCase(2, 960, 640)]
    [TestCase(0, 760, 520)]
    [TestCase(1, 760, 520)]
    [TestCase(2, 760, 520)]
    public void DemoShellAndGridTextRemainUsableAtEachProfile(
        int presetIndex, int width, int height)
    {
        using var form = new MainForm { ClientSize = new Size(width, height) };
        ShowForm(form);
        Find<ComboBox>(form, "baseFontComboBox").SelectedIndex = presetIndex;
        form.PerformLayout();
        var toolbar = Find<FlowLayoutPanel>(form, "themeToolbar");
        var diagnostics = Find<Label>(form, "diagnosticsLabel");
        var help = Find<Label>(form, "interactionHelp");
        var grid = Find<BootstrapSourceGridControl>(form, "BootstrapSourceGrid");
        var headerTextHeight = TextRenderer.MeasureText("Bootstrap formatted", grid.Font).Height;
        var headerTextWidth = TextRenderer.MeasureText("Bootstrap formatted", grid.Font).Width;
        var rowTextHeight = TextRenderer.MeasureText("Item 01", grid.Font).Height;
        var rowTextWidth = TextRenderer.MeasureText("Item 01", grid.Font).Width;
        var padding = grid.CurrentDpiMetrics.CellPadding;

        Assert.Multiple((Action)(() =>
        {
            foreach (Control control in toolbar.Controls)
            {
                Assert.That(control.Right, Is.LessThanOrEqualTo(toolbar.ClientSize.Width),
                    $"{control.Name} extends past toolbar at {width}x{height}, profile {presetIndex}");
                Assert.That(control.Bottom, Is.LessThanOrEqualTo(toolbar.ClientSize.Height),
                    $"{control.Name} extends below toolbar at {width}x{height}, profile {presetIndex}");
            }

            Assert.That(diagnostics.Width, Is.GreaterThan(0));
            Assert.That(diagnostics.Height, Is.GreaterThan(0));
            Assert.That(help.Width, Is.GreaterThan(0));
            Assert.That(help.Height, Is.GreaterThan(0));
            Assert.That(grid.Width, Is.GreaterThan(0));
            Assert.That(grid.Height, Is.GreaterThan(0));
            Assert.That(grid.Rows[0].Height, Is.GreaterThanOrEqualTo(headerTextHeight));
            Assert.That(grid.Rows[1].Height,
                Is.GreaterThanOrEqualTo(rowTextHeight + 2 * padding));
            Assert.That(grid.Rows[1].Height,
                Is.GreaterThanOrEqualTo(((BootstrapTextBoxEditor)grid[1, 2].Editor!).Control.PreferredSize.Height));
            Assert.That(grid.Rows[1].Height,
                Is.GreaterThanOrEqualTo(((BootstrapFormattedTextBoxEditor)grid[1, 3].Editor!).Control.PreferredSize.Height));
            Assert.That(grid.Rows[1].Height,
                Is.GreaterThanOrEqualTo(((BootstrapLookupBoxEditor)grid[1, 6].Editor!).Control.PreferredSize.Height));
            Assert.That(grid.Columns[3].Width, Is.GreaterThanOrEqualTo(headerTextWidth + 2 * padding));
            for (var column = 1; column < grid.ColumnsCount; column++)
            {
                var position = new SourceGrid.Position(0, column);
                var headerRequiredWidth = grid[0, column].View.Measure(
                    new SourceGrid.CellContext(grid, position, grid[0, column]), Size.Empty).Width;
                Assert.That(grid.Columns[column].Width, Is.GreaterThanOrEqualTo(headerRequiredWidth),
                    $"Header {column} is clipped at {width}x{height}, profile {presetIndex}");
            }
            Assert.That(grid.Columns[1].Width, Is.GreaterThanOrEqualTo(rowTextWidth + 2 * padding));
            Assert.That(grid.Columns[2].Width, Is.EqualTo(150));
        }));
    }

    [Test]
    public void DemoOwnedHeaderWidthReturnsToBaselineAfterLargerProfileAndDpi()
    {
        using var normalDpiFont = new Font(FontFamily.GenericSansSerif, 9f);
        using var highDpiFont = new Font(FontFamily.GenericSansSerif, 18f);
        using var form = new MainForm();
        ShowForm(form);
        var grid = Find<BootstrapSourceGridControl>(form, "BootstrapSourceGrid");
        var profile = Find<ComboBox>(form, "baseFontComboBox");
        var baselineWidth = grid.Columns[3].Width;

        profile.SelectedIndex = 2;
        Assert.That(grid.Columns[3].Width, Is.GreaterThan(baselineWidth));
        profile.SelectedIndex = 0;
        Assert.That(grid.Columns[3].Width, Is.EqualTo(baselineWidth));

        var actualDpi = grid.CurrentDpi;
        grid.Font = normalDpiFont;
        form.ApplyDemoRowLayout();
        var normalDpiWidth = grid.Columns[3].Width;
        grid.Font = highDpiFont;
        grid.RefreshDpiMetrics(actualDpi * 2);
        form.ApplyDemoRowLayout();
        Assert.That(grid.Columns[3].Width, Is.GreaterThan(normalDpiWidth));
        grid.Font = normalDpiFont;
        grid.RefreshDpiMetrics(actualDpi);
        form.ApplyDemoRowLayout();
        Assert.That(grid.Columns[3].Width, Is.EqualTo(normalDpiWidth));

        grid.Columns[3].Width = baselineWidth + 80;
        profile.SelectedIndex = 2;
        profile.SelectedIndex = 0;
        Assert.That(grid.Columns[3].Width, Is.EqualTo(baselineWidth + 80));
    }

    [Test]
    public void DpiEventRecomputesDemoRowsAfterGridMetricsRefresh()
    {
        using var form = new MainForm();
        ShowForm(form);
        Find<ComboBox>(form, "baseFontComboBox").SelectedIndex = 2;
        var grid = Find<BootstrapSourceGridControl>(form, "BootstrapSourceGrid");
        var text = (BootstrapTextBoxEditor)grid[1, 2].Editor!;
        var formatted = (BootstrapFormattedTextBoxEditor)grid[1, 3].Editor!;
        var lookup = (BootstrapLookupBoxEditor)grid[1, 6].Editor!;
        var actualDpi = grid.CurrentDpi;
        grid.RefreshDpiMetrics(actualDpi * 2);
        form.ApplyDemoRowLayout();
        var previousHeight = grid.Rows[1].Height;

        var dpiCallback = typeof(BootstrapSourceGridControl).GetMethod(
            "OnDpiChangedAfterParent", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(dpiCallback, Is.Not.Null);
        dpiCallback!.Invoke(grid, new object[] { EventArgs.Empty });
        Application.DoEvents();

        var padding = grid.CurrentDpiMetrics.CellPadding;
        var rowTextHeight = TextRenderer.MeasureText("Item 01", grid.Font).Height;
        var expectedHeight = Math.Max(rowTextHeight + 2 * padding,
            Math.Max(text.Control.PreferredSize.Height,
                Math.Max(formatted.Control.PreferredSize.Height, lookup.Control.PreferredSize.Height)));
        Assert.That(previousHeight, Is.GreaterThan(expectedHeight));
        Assert.That(grid.Rows[1].Height, Is.EqualTo(expectedHeight));
        Assert.That(grid.Rows[0].Height,
            Is.EqualTo(Math.Max(32, TextRenderer.MeasureText("Bootstrap formatted", grid.Font).Height + 2 * padding)));
    }

    [Test]
    public void ProfileChangeKeepsActiveTextEditorAndLogicalValue()
    {
        using var form = new MainForm();
        ShowForm(form);
        var grid = Find<BootstrapSourceGridControl>(form, "BootstrapSourceGrid");
        var position = new SourceGrid.Position(1, 2);
        var cell = (SourceGrid.Cells.Cell)grid[position];
        var editor = (BootstrapTextBoxEditor)cell.Editor!;
        Assert.That(grid.Selection.Focus(position, true), Is.True);
        var context = new SourceGrid.CellContext(grid, position, cell);
        grid.GetCell(position).View.Measure(context, Size.Empty);
        context.StartEdit();
        editor.BootstrapControl.Text = "Profile change edit";

        Find<ComboBox>(form, "baseFontComboBox").SelectedIndex = 2;

        Assert.That(editor.IsEditing, Is.True);
        Assert.That(editor.BootstrapControl.Text, Is.EqualTo("Profile change edit"));
        Assert.That(context.EndEdit(false), Is.True);
        Assert.That(cell.Value, Is.EqualTo("Profile change edit"));
    }

    [Test]
    public void ProfileChangeKeepsLookupPopupAndSelectedValue()
    {
        using var form = new MainForm();
        ShowForm(form);
        var grid = Find<BootstrapSourceGridControl>(form, "BootstrapSourceGrid");
        var position = new SourceGrid.Position(1, 6);
        var cell = (SourceGrid.Cells.Cell)grid[position];
        var editor = (BootstrapLookupBoxEditor)cell.Editor!;
        Assert.That(grid.Selection.Focus(position, true), Is.True);
        var context = new SourceGrid.CellContext(grid, position, cell);
        grid.GetCell(position).View.Measure(context, Size.Empty);
        context.StartEdit();
        editor.BootstrapControl.OpenDropDown();
        Application.DoEvents();
        Assert.That(editor.BootstrapControl.IsDropDownOpen, Is.True);

        Find<ComboBox>(form, "baseFontComboBox").SelectedIndex = 2;
        Application.DoEvents();

        Assert.That(editor.IsEditing, Is.True);
        Assert.That(editor.BootstrapControl.IsDropDownOpen, Is.True);
        Assert.That(editor.BootstrapControl.SelectedValue, Is.EqualTo(1));
        Assert.That(cell.Value, Is.EqualTo(1));
    }

    private static BootstrapThemeTypography CustomTypography() => new BootstrapThemeTypography(
        new BootstrapFontToken("Segoe UI", 11f),
        new BootstrapFontToken("Segoe UI", 9f),
        new BootstrapFontToken("Segoe UI", 11f, FontStyle.Bold),
        new BootstrapFontToken("Segoe UI", 14f, FontStyle.Bold),
        new BootstrapFontToken("Segoe UI", 17f, FontStyle.Bold));

    private static T Find<T>(Control root, string name) where T : Control =>
        root.Controls.Find(name, true).OfType<T>().Single();

    private static void ShowForm(Form form)
    {
        form.Show();
        Application.DoEvents();
    }

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr handle);
}
