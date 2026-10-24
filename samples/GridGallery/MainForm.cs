using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using MyDmsVn.BootstrapSourceGrid.Editors;

namespace MyDmsVn.BootstrapSourceGrid.Demo;

public sealed partial class MainForm : Form
{
    private Font? _consumerFont;
    private Font? _ownedBodyFont;
    private BootstrapFontToken? _ownedBodyFontToken;
    private bool _synchronizingSettings;
    private readonly BootstrapTextBoxEditor _textEditor;
    private readonly BootstrapFormattedTextBoxEditor _formattedEditor;
    private readonly BootstrapLookupBoxEditor _lookupEditor;
    private readonly DemoLookupItem[] _lookupItems;
    private readonly SourceGrid.Cells.Controllers.CustomEvents _lookupValueEvents = new();
    private readonly DemoGridContent _demoGridContent = new();

    public MainForm()
    {
        InitializeComponent();
        _textEditor = _grid.BootstrapEditors.CreateTextBox(typeof(string));
        _textEditor.BootstrapControl.PlaceholderText = "Customer name";
        _textEditor.BootstrapControl.ShowClearButton = true;

        _formattedEditor = _grid.BootstrapEditors.CreateFormattedTextBox(typeof(decimal));
        _formattedEditor.BootstrapControl.FormatMode = BootstrapInputFormatMode.Numeral;
        _formattedEditor.BootstrapControl.NumeralOptions.DecimalScale = 2;
        _formattedEditor.BootstrapControl.NumeralOptions.Prefix = "$";

        _lookupItems = DemoLookupItem.CreateSampleData();
        _lookupValueEvents.ValueChanged += OnLookupValueChanged;
        _lookupEditor = _grid.BootstrapEditors.CreateLookupBox(typeof(int));
        ConfigureLookup(_lookupEditor.BootstrapControl, _lookupItems);

        _demoGridContent.Populate(
            _grid,
            _textEditor,
            _formattedEditor,
            _lookupEditor,
            _lookupItems,
            _lookupValueEvents);
        _textEditor.Control.Validated += OnEditorValidated;
        _formattedEditor.Control.Validated += OnEditorValidated;
        _lookupEditor.Control.Validated += OnEditorValidated;
        _grid.DpiChangedAfterParent += OnGridDpiChangedAfterParent;
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        var theme = BootstrapThemeManager.CurrentTheme;
        SynchronizeSettings(theme);
        ApplyShellTheme(theme);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        ApplyDemoRowLayout();
        UpdateDiagnostics();
        _grid.Selection.Focus(new SourceGrid.Position(1, 1), true);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
            _grid.DpiChangedAfterParent -= OnGridDpiChangedAfterParent;
            _textEditor.Control.Validated -= OnEditorValidated;
            _formattedEditor.Control.Validated -= OnEditorValidated;
            _lookupEditor.Control.Validated -= OnEditorValidated;
            _lookupValueEvents.ValueChanged -= OnLookupValueChanged;
            _components.Dispose();
            _consumerFont?.Dispose();
            _consumerFont = null;
        }

        base.Dispose(disposing);

        if (disposing)
        {
            _ownedBodyFont?.Dispose();
            _ownedBodyFont = null;
            _ownedBodyFontToken = null;
        }
    }

    private void OnResetGridClick(object? sender, EventArgs e)
    {
        _demoGridContent.Populate(
            _grid,
            _textEditor,
            _formattedEditor,
            _lookupEditor,
            _lookupItems,
            _lookupValueEvents);
        ApplyDemoRowLayout();
        _grid.Selection.Focus(new SourceGrid.Position(1, 1), true);
        UpdateDiagnostics();
    }

    private void OnConsumerFontClick(object? sender, EventArgs e)
    {
        if (_consumerFont is not null)
        {
            return;
        }

        _consumerFont = new Font(FontFamily.GenericMonospace, _grid.Font.SizeInPoints);
        _grid.Font = _consumerFont;
        _consumerFontButton.Enabled = false;
        _consumerFontButton.Text = "Consumer font active";
        ApplyDemoRowLayout();
        UpdateDiagnostics();
    }

    private void OnLightThemeClick(object? sender, EventArgs e)
    {
        PublishTheme(BootstrapThemeMode.Light, BootstrapThemeManager.CurrentTheme.Typography,
            BootstrapThemeManager.CurrentTheme.ReducedMotion);
    }

    private void OnDarkThemeClick(object? sender, EventArgs e)
    {
        PublishTheme(BootstrapThemeMode.Dark, BootstrapThemeManager.CurrentTheme.Typography,
            BootstrapThemeManager.CurrentTheme.ReducedMotion);
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        SynchronizeSettings(e.NewTheme);
        ApplyShellTheme(e.NewTheme);
    }

    private void OnBaseFontSelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_synchronizingSettings || _baseFontComboBox.SelectedIndex < 0)
        {
            return;
        }

        var current = BootstrapThemeManager.CurrentTheme;
        PublishTheme(current.Mode,
            DemoTypography.ForPreset((DemoTypographyPreset)_baseFontComboBox.SelectedIndex),
            current.ReducedMotion);
    }

    private void OnReducedMotionCheckedChanged(object? sender, EventArgs e)
    {
        if (_synchronizingSettings)
        {
            return;
        }

        var current = BootstrapThemeManager.CurrentTheme;
        PublishTheme(current.Mode, current.Typography, _reducedMotionCheckBox.Checked);
    }

    private static void PublishTheme(
        BootstrapThemeMode mode,
        BootstrapThemeTypography typography,
        bool reducedMotion)
    {
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(mode, typography, reducedMotion);
    }

    private void SynchronizeSettings(BootstrapTheme theme)
    {
        _synchronizingSettings = true;
        try
        {
            _baseFontComboBox.SelectedIndex = DemoTypography.TryGetPreset(theme.Typography, out var preset)
                ? (int)preset
                : -1;
            _reducedMotionCheckBox.Checked = theme.ReducedMotion;
        }
        finally
        {
            _synchronizingSettings = false;
        }
    }

    private void ApplyShellTheme(BootstrapTheme theme)
    {
        ApplyBodyTypography(theme.Typography.Body);
        ApplyDemoRowLayout();
        _baseFontLabel.BackColor = theme.Colors.Surface;
        _baseFontLabel.ForeColor = theme.Colors.Text;
        _baseFontComboBox.BackColor = theme.Colors.Surface;
        _baseFontComboBox.ForeColor = theme.Colors.Text;
        _reducedMotionCheckBox.BackColor = theme.Colors.Surface;
        _reducedMotionCheckBox.ForeColor = theme.Colors.Text;
        UpdateDiagnostics();
    }

    internal void ApplyDemoRowLayout() => _demoGridContent.ApplyTypographyLayout(
        _grid, _textEditor, _formattedEditor, _lookupEditor);

    private void ApplyBodyTypography(BootstrapFontToken token)
    {
        if (_ownedBodyFont is not null && _ownedBodyFontToken is not null &&
            DemoTypography.TokensMatch(_ownedBodyFontToken, token))
        {
            return;
        }

        if (_ownedBodyFont is not null && DemoTypography.FontMatchesToken(_ownedBodyFont, token))
        {
            _ownedBodyFontToken = token;
            return;
        }

        var replacement = DemoTypography.CreateFont(token);
        if (_ownedBodyFont is not null && DemoTypography.FontsEquivalent(_ownedBodyFont, replacement))
        {
            replacement.Dispose();
            _ownedBodyFontToken = token;
            return;
        }

        var previous = _ownedBodyFont;
        Font = replacement;
        if (!ReferenceEquals(Font, replacement))
        {
            replacement.Dispose();
            _ownedBodyFontToken = token;
            return;
        }

        _ownedBodyFont = replacement;
        _ownedBodyFontToken = token;
        previous?.Dispose();
    }

    private void OnGridDpiChangedAfterParent(object? sender, EventArgs e)
    {
        if (IsDisposed || Disposing || !IsHandleCreated)
        {
            return;
        }

        // The grid refreshes its DPI metrics after raising DpiChangedAfterParent.
        BeginInvoke((MethodInvoker)ApplyDemoDpiLayout);
    }

    private void ApplyDemoDpiLayout()
    {
        if (IsDisposed || Disposing || _grid.IsDisposed)
        {
            return;
        }

        ApplyDemoRowLayout();
        UpdateDiagnostics();
    }

    private void OnLookupValueChanged(object? sender, EventArgs e)
    {
        if (sender is not SourceGrid.CellContext context || context.Position.Row <= 0)
        {
            return;
        }

        var lookupValue = _grid[context.Position.Row, 6].Value;
        var displayValue = string.Empty;
        foreach (var item in _lookupItems)
        {
            if (Equals(item.Id, lookupValue))
            {
                displayValue = item.Name;
                break;
            }
        }

        _grid[context.Position.Row, 7].Value = displayValue;
    }

    private void OnEditorValidated(object? sender, EventArgs e)
    {
        if (IsDisposed || Disposing || !IsHandleCreated)
        {
            return;
        }

        BeginInvoke((MethodInvoker)UpdateDiagnostics);
    }

    private void UpdateDiagnostics()
    {
        if (_diagnostics.IsDisposed || _grid.IsDisposed)
        {
            return;
        }

        var theme = BootstrapThemeManager.CurrentTheme;
        var metrics = _grid.CurrentDpiMetrics;
        var targetFramework = AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName ?? "unknown TFM";
        var fontMode = _grid.OwnedThemeFont is null ? "consumer font" : "theme font";
        var profile = DemoTypography.TryGetPreset(theme.Typography, out var preset)
            ? _baseFontComboBox.Items[(int)preset]?.ToString() ?? "Custom"
            : "Custom";
        var activeValue = GetActiveLogicalValue();
        _diagnostics.Text =
            $"Runtime: {targetFramework}; CLR {Environment.Version} | " +
            $"DeviceDpi: {_grid.CurrentDpi} | Theme: {theme.Mode} | " +
            $"Profile: {profile} | Body: {theme.Typography.Body.SizeInPoints}pt | " +
            $"Reduced motion: {theme.ReducedMotion} | " +
            $"Padding: {metrics.CellPadding}; Border: {metrics.CellBorderThickness}; " +
            $"Focus: {metrics.FocusThickness} | Font ownership: {fontMode} | " +
            $"Active logical value: {activeValue}";
        _diagnostics.BackColor = theme.Colors.Surface;
        _diagnostics.ForeColor = theme.Colors.Text;
        _interactionHelp.BackColor = theme.Colors.SurfaceSecondary;
        _interactionHelp.ForeColor = theme.Colors.Text;
        _toolbar.BackColor = theme.Colors.Surface;
        BackColor = theme.Colors.Surface;
    }

    private string GetActiveLogicalValue()
    {
        var active = _grid.Selection.ActivePosition;
        if (active.IsEmpty())
        {
            return "none";
        }

        var value = _grid[active.Row, active.Column].Value;
        return value is null ? "null" : $"{value} ({value.GetType().Name})";
    }

    private static void ConfigureLookup(BootstrapLookupBox lookup, DemoLookupItem[] lookupItems)
    {
        lookup.DisplayMember = nameof(DemoLookupItem.Name);
        lookup.ValueMember = nameof(DemoLookupItem.Id);
        lookup.DataSource = lookupItems;
        lookup.Columns.Add(new BootstrapLookupColumnDefinition
        {
            DataPropertyName = nameof(DemoLookupItem.Id),
            HeaderText = "ID",
            Width = 55,
        });
        lookup.Columns.Add(new BootstrapLookupColumnDefinition
        {
            DataPropertyName = nameof(DemoLookupItem.Name),
            HeaderText = "Customer",
            Width = 150,
        });
        lookup.Columns.Add(new BootstrapLookupColumnDefinition
        {
            DataPropertyName = nameof(DemoLookupItem.Region),
            HeaderText = "Region",
            Width = 90,
        });
        lookup.SearchMembers.Add(nameof(DemoLookupItem.Name));
        lookup.SearchMembers.Add(nameof(DemoLookupItem.Region));
        lookup.MinimumSearchLength = 0;
        lookup.SearchDebounceMilliseconds = 0;
        lookup.DropDownWidth = 320;
    }
}
