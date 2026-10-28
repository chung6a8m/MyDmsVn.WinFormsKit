using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides a Bootstrap-inspired themed text input while delegating text editing to a native WinForms <see cref="TextBox"/>.
/// </summary>
[DefaultProperty(nameof(Text))]
[DefaultEvent(nameof(TextChanged))]
public class BootstrapTextBox : UserControl, IBootstrapConnectedControl
{
    private static readonly IIconRenderer DefaultIconRenderer = BootstrapIconRenderer.CreateDefault();
    private static readonly IconDescriptor ClearIcon = IconDescriptor.Framework(FrameworkIconGlyph.Close);

    private readonly TextBox _editor = new TextBox();
    private readonly EditorAccessibilityWindow _editorAccessibilityWindow;
    private readonly Label _placeholder = new Label();
    private readonly Button _clearButton = new Button();
    private Control? _frameworkTrailingAccessory;
    private bool _frameworkTrailingAccessoryCompactHitArea;
    private string _placeholderText = string.Empty;
    private BootstrapValidationState _applicationValidationState = BootstrapValidationState.None;
    private BootstrapValidationState? _transientValidationState;
    private IconDescriptor? _icon;
    private IconDescriptor? _trailingIcon;
    private IIconRenderer _iconRenderer = DefaultIconRenderer;
    private bool _showClearButton;
    private bool _editorHasFocus;
    private int _borderRadius = -1;
    private CornerRadius? _connectedCornerRadius;
    private BootstrapConnectedControlSize? _connectedSizeOverride;
    private bool _themeSubscribed;
    private bool _settingThemeFont;
    private bool _useThemeFont = true;
    private Font? _themeFont;
    private BootstrapFontToken? _themeFontToken;
    private bool _synchronizingEditorAccessibility;
    private string? _forwardedAccessibleName;
    private string? _previousEditorAccessibleName;
    private string? _forwardedAccessibleDescription;
    private string? _previousEditorAccessibleDescription;

    /// <summary>
    /// Initializes a designer-safe text box using the current application theme.
    /// </summary>
    public BootstrapTextBox()
    {
        _editorAccessibilityWindow = new EditorAccessibilityWindow(this);
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.Selectable,
            true);

        BackColor = Color.Transparent;
        TabStop = true;
        AccessibleRole = AccessibleRole.Text;
        AccessibleDescription = "Bootstrap-inspired text input.";

        _editor.BorderStyle = BorderStyle.None;
        _editor.TabStop = false;
        _editor.Margin = Padding.Empty;
        _editor.HandleCreated += OnEditorHandleCreated;
        _editor.HandleDestroyed += OnEditorHandleDestroyed;
        _editor.TextChanged += HandleEditorTextChanged;
        _editor.GotFocus += OnEditorGotFocus;
        _editor.LostFocus += OnEditorLostFocus;
        _editor.KeyDown += HandleEditorKeyDown;
        _editor.KeyPress += OnEditorKeyPress;
        _editor.KeyUp += OnEditorKeyUp;
        _editor.PreviewKeyDown += OnEditorPreviewKeyDown;

        _placeholder.AutoSize = false;
        _placeholder.TabStop = false;
        _placeholder.UseMnemonic = false;
        _placeholder.TextAlign = ContentAlignment.MiddleLeft;
        _placeholder.AccessibleRole = AccessibleRole.StaticText;
        _placeholder.Click += (_, _) => FocusEditor();

        _clearButton.AutoSize = false;
        _clearButton.TabStop = false;
        _clearButton.FlatStyle = FlatStyle.Flat;
        _clearButton.FlatAppearance.BorderSize = 0;
        _clearButton.FlatAppearance.MouseDownBackColor = Color.Transparent;
        _clearButton.FlatAppearance.MouseOverBackColor = Color.Transparent;
        _clearButton.UseVisualStyleBackColor = false;
        _clearButton.Text = string.Empty;
        _clearButton.Visible = false;
        _clearButton.AccessibleName = "Clear text";
        _clearButton.AccessibleDescription = "Clears the current text value.";
        _clearButton.Paint += OnClearButtonPaint;
        _clearButton.Click += (_, _) =>
        {
            _editor.Clear();
            FocusEditor();
        };

        Controls.Add(_editor);
        Controls.Add(_placeholder);
        Controls.Add(_clearButton);

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyThemeFont();
        ApplyTheme();

        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        Size = new Size(240, DpiScaler.Scale(theme.Metrics.ControlHeight, dpi));
        UpdatePlaceholderVisibility();
        UpdateClearButtonVisibility();
    }

    /// <summary>
    /// Gets the native text editor used by this composite control.
    /// </summary>
    protected TextBox Editor => _editor;

    internal void SetFrameworkTrailingAccessory(Control? accessory, bool compactHitArea = false)
    {
        if (ReferenceEquals(_frameworkTrailingAccessory, accessory))
        {
            return;
        }

        var previous = _frameworkTrailingAccessory;
        _frameworkTrailingAccessory = null;
        _frameworkTrailingAccessoryCompactHitArea = compactHitArea;
        if (previous is not null)
        {
            Controls.Remove(previous);
            previous.Dispose();
        }

        if (accessory is not null)
        {
            accessory.TabStop = false;
            _frameworkTrailingAccessory = accessory;
            Controls.Add(accessory);
            accessory.BringToFront();
            ApplyTheme();
        }

        PerformLayout();
        Invalidate();
    }

    internal void SetTransientValidationStateOverride(BootstrapValidationState? state)
    {
        if (state.HasValue)
        {
            BootstrapTextBoxRenderLogic.ValidateState(state.Value);
        }

        if (_transientValidationState == state)
        {
            return;
        }

        _transientValidationState = state;
        Invalidate();
    }

    /// <inheritdoc />
    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [Category("Appearance")]
    [Description("Gets or sets the text edited by the native inner TextBox.")]
    [DefaultValue("")]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.AllowNull]
#endif
    public override string Text
    {
        get => _editor.Text;
        set => _editor.Text = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets placeholder text displayed while the input is empty.
    /// </summary>
    [Category("Appearance")]
    [Description("Displays muted placeholder text while the input is empty.")]
    [DefaultValue("")]
    public string PlaceholderText
    {
        get => _placeholderText;
        set
        {
            var normalized = value ?? string.Empty;
            if (string.Equals(_placeholderText, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _placeholderText = normalized;
            _placeholder.Text = normalized;
            UpdatePlaceholderVisibility();
        }
    }

    /// <summary>
    /// Gets or sets the validation state used to color the input border.
    /// </summary>
    [Category("Appearance")]
    [Description("Selects neutral, valid, or invalid validation border presentation.")]
    [DefaultValue(BootstrapValidationState.None)]
    public BootstrapValidationState ValidationState
    {
        get => _transientValidationState ?? _applicationValidationState;
        set
        {
            BootstrapTextBoxRenderLogic.ValidateState(value);
            if (_applicationValidationState == value)
            {
                return;
            }

            _applicationValidationState = value;
            if (!_transientValidationState.HasValue)
            {
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets an optional source-neutral leading icon.
    /// </summary>
    [Category("Appearance")]
    [Description("Specifies an optional icon rendered before the native editor.")]
    [DefaultValue(null)]
    public IconDescriptor? Icon
    {
        get => _icon;
        set
        {
            if (ReferenceEquals(_icon, value))
            {
                return;
            }

            _icon = value;
            PerformLayout();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets an optional source-neutral trailing icon.
    /// </summary>
    [Category("Appearance")]
    [Description("Specifies an optional icon rendered after the native editor.")]
    [DefaultValue(null)]
    public IconDescriptor? TrailingIcon
    {
        get => _trailingIcon;
        set
        {
            if (ReferenceEquals(_trailingIcon, value))
            {
                return;
            }

            _trailingIcon = value;
            PerformLayout();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the renderer used by <see cref="Icon"/> and <see cref="TrailingIcon"/>.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IIconRenderer IconRenderer
    {
        get => _iconRenderer;
        set
        {
            _iconRenderer = value ?? throw new ArgumentNullException(nameof(value));
            _clearButton.Invalidate();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets whether a clear affordance is shown when editable text is present.
    /// </summary>
    [Category("Behavior")]
    [Description("Shows a clear button while the input contains editable text.")]
    [DefaultValue(false)]
    public bool ShowClearButton
    {
        get => _showClearButton;
        set
        {
            if (_showClearButton == value)
            {
                return;
            }

            _showClearButton = value;
            UpdateClearButtonVisibility();
            PerformLayout();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets whether the native editor is read-only.
    /// </summary>
    [Category("Behavior")]
    [Description("Prevents user edits while preserving text selection and copy behavior.")]
    [DefaultValue(false)]
    public bool ReadOnly
    {
        get => _editor.ReadOnly;
        set
        {
            if (_editor.ReadOnly == value)
            {
                return;
            }

            _editor.ReadOnly = value;
            UpdateClearButtonVisibility();
            ApplyTheme();
            PerformLayout();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets whether the native editor displays multiple lines in the caller-sized control.
    /// </summary>
    [Category("Behavior")]
    [Description("Displays multiple native text lines while retaining the caller's control height.")]
    [DefaultValue(false)]
    public bool Multiline
    {
        get => _editor.Multiline;
        set
        {
            if (_editor.Multiline == value)
            {
                return;
            }

            _editor.Multiline = value;
            PerformLayout();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets whether the native multiline editor wraps text to the available width.
    /// </summary>
    [Category("Behavior")]
    [Description("Wraps multiline text. Native horizontal scrollbars are hidden while wrapping is enabled.")]
    [DefaultValue(true)]
    public bool WordWrap
    {
        get => _editor.WordWrap;
        set
        {
            if (_editor.WordWrap == value)
            {
                return;
            }

            _editor.WordWrap = value;
            PerformLayout();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the native scrollbars used when <see cref="Multiline"/> is enabled.
    /// </summary>
    [Category("Behavior")]
    [Description("Selects native multiline scrollbars; horizontal scrolling also requires WordWrap = false.")]
    [DefaultValue(ScrollBars.None)]
    public ScrollBars ScrollBars
    {
        get => _editor.ScrollBars;
        set
        {
            if (_editor.ScrollBars == value)
            {
                return;
            }

            _editor.ScrollBars = value;
            PerformLayout();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets whether the native editor uses the system password character.
    /// </summary>
    [Category("Behavior")]
    [Description("Masks entered text using the system password character.")]
    [DefaultValue(false)]
    public bool UseSystemPasswordChar
    {
        get => _editor.UseSystemPasswordChar;
        set => _editor.UseSystemPasswordChar = value;
    }

    /// <summary>
    /// Gets or sets a uniform logical corner radius. Use -1 to select the current theme radius.
    /// </summary>
    [Category("Appearance")]
    [Description("Sets a uniform logical corner radius, or -1 to use the current theme radius.")]
    [DefaultValue(-1)]
    public int BorderRadius
    {
        get => _borderRadius;
        set
        {
            if (value < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Border radius must be -1 or a non-negative value.");
            }

            if (_borderRadius == value)
            {
                return;
            }

            _borderRadius = value;
            Invalidate();
        }
    }

    /// <summary>Clears all text from the native editor.</summary>
    public void Clear()
    {
        _editor.Clear();
    }

    /// <summary>Selects all text in the native editor.</summary>
    public void SelectAll()
    {
        _editor.SelectAll();
    }

    CornerRadius? IBootstrapConnectedControl.ConnectedCornerRadius
    {
        get => _connectedCornerRadius;
        set
        {
            _connectedCornerRadius = value;
            Invalidate();
        }
    }

    BootstrapConnectedControlSize? IBootstrapConnectedControl.ConnectedSizeOverride
    {
        get => _connectedSizeOverride;
        set
        {
            _connectedSizeOverride = value;
            PerformLayout();
            Invalidate();
        }
    }

    int IBootstrapConnectedControl.GetConnectedSafeMinimumHeight(BootstrapConnectedControlSize size, int dpi)
    {
        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi));
        }

        var target = DpiScaler.Scale(GetLogicalHeight(BootstrapThemeManager.CurrentTheme.Metrics, size), dpi);
        return Math.Max(target, _editor.PreferredHeight + Math.Max(2, DpiScaler.Scale(2, dpi)));
    }

    /// <inheritdoc />
    protected override void OnEnter(EventArgs e)
    {
        base.OnEnter(e);
        FocusEditor();
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (Enabled)
        {
            FocusEditor();
        }
    }

    /// <inheritdoc />
    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        UpdateClearButtonVisibility();
        ApplyTheme();
        PerformLayout();
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (!_settingThemeFont)
        {
            _useThemeFont = false;
            DisposeThemeFont();
        }

        ApplyChildFonts();
        PerformLayout();
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        LayoutChildren();
    }

    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        PerformLayout();
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var borderWidth = Math.Max(1f, DpiScaler.Scale((float)(ContainsFocus ? theme.Metrics.FocusBorderWidth : theme.Metrics.BorderWidth), dpi));
        var inset = borderWidth / 2f;
        var bounds = new RectangleF(
            inset,
            inset,
            Math.Max(0f, ClientSize.Width - borderWidth),
            Math.Max(0f, ClientSize.Height - borderWidth));
        if (bounds.Width <= 0f || bounds.Height <= 0f)
        {
            return;
        }

        var logicalRadius = _borderRadius >= 0 ? _borderRadius : GetLogicalRadius(theme.Metrics);
        var radius = _connectedCornerRadius.HasValue
            ? ScaleCornerRadius(_connectedCornerRadius.Value, dpi)
            : new CornerRadius(DpiScaler.Scale((float)logicalRadius, dpi));
        var surface = Enabled && !ReadOnly ? theme.Colors.Surface : theme.Colors.SurfaceSecondary;
        var borderColor = BootstrapTextBoxRenderLogic.ResolveBorderColor(
            theme.Colors,
            _transientValidationState ?? _applicationValidationState,
            ContainsFocus,
            Enabled);

        var graphics = e.Graphics;
        var previousSmoothing = graphics.SmoothingMode;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        try
        {
            using var path = RoundedPath.Create(bounds, radius);
            using var surfaceBrush = new SolidBrush(surface);
            using var borderPen = new Pen(borderColor, borderWidth);
            graphics.FillPath(surfaceBrush, path);
            graphics.DrawPath(borderPen, path);
            PaintIcons(graphics, theme, dpi);
        }
        finally
        {
            graphics.SmoothingMode = previousSmoothing;
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_themeSubscribed)
            {
                BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
                _themeSubscribed = false;
            }

            _editor.HandleCreated -= OnEditorHandleCreated;
            _editor.HandleDestroyed -= OnEditorHandleDestroyed;
            _editorAccessibilityWindow.ReleaseHandle();
            DisposeThemeFont();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Processes a text change raised by the native editor.
    /// </summary>
    /// <param name="e">The event data.</param>
    protected virtual void OnEditorTextChanged(EventArgs e)
    {
        UpdatePlaceholderVisibility();
        UpdateClearButtonVisibility();
        PerformLayout();
        OnTextChanged(e);
    }

    /// <summary>
    /// Processes a key-down event raised by the native editor.
    /// </summary>
    /// <param name="e">The event data.</param>
    protected virtual void OnEditorKeyDown(KeyEventArgs e)
    {
        OnKeyDown(e);
    }

    private void HandleEditorTextChanged(object? sender, EventArgs e)
    {
        OnEditorTextChanged(e);
    }

    private void OnEditorGotFocus(object? sender, EventArgs e)
    {
        SynchronizeEditorAccessibility();
        _editorHasFocus = true;
        UpdatePlaceholderVisibility();
        Invalidate();
    }

    private void OnEditorLostFocus(object? sender, EventArgs e)
    {
        _editorHasFocus = false;
        UpdatePlaceholderVisibility();
        Invalidate();
    }

    private void HandleEditorKeyDown(object? sender, KeyEventArgs e)
    {
        OnEditorKeyDown(e);
    }

    private void OnEditorKeyPress(object? sender, KeyPressEventArgs e)
    {
        OnKeyPress(e);
    }

    private void OnEditorKeyUp(object? sender, KeyEventArgs e)
    {
        OnKeyUp(e);
    }

    private void OnEditorPreviewKeyDown(object? sender, PreviewKeyDownEventArgs e)
    {
        OnPreviewKeyDown(e);
    }

    private void OnClearButtonPaint(object? sender, PaintEventArgs e)
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var inset = Math.Max(2, DpiScaler.Scale(theme.Metrics.SpacingXS, dpi));
        var bounds = Rectangle.Inflate(_clearButton.ClientRectangle, -inset, -inset);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        _iconRenderer.TryRender(e.Graphics, ClearIcon, bounds, _clearButton.ForeColor);
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (_useThemeFont)
        {
            ApplyThemeFont();
        }

        ApplyTheme();
        PerformLayout();
        Invalidate();
    }

    private void ApplyTheme()
    {
        SynchronizeEditorAccessibility();
        var colors = BootstrapThemeManager.CurrentTheme.Colors;
        var surface = Enabled && !ReadOnly ? colors.Surface : colors.SurfaceSecondary;
        var foreground = Enabled ? colors.Text : colors.MutedText;

        _editor.BackColor = surface;
        _editor.ForeColor = foreground;
        _placeholder.BackColor = surface;
        _placeholder.ForeColor = colors.Disabled;
        _clearButton.BackColor = surface;
        _clearButton.ForeColor = colors.MutedText;
        _clearButton.Invalidate();
        if (_frameworkTrailingAccessory is not null)
        {
            _frameworkTrailingAccessory.BackColor = surface;
            _frameworkTrailingAccessory.ForeColor = foreground;
            _frameworkTrailingAccessory.Invalidate();
        }
    }

    private void ApplyThemeFont()
    {
        var token = BootstrapThemeManager.CurrentTheme.Typography.Body;
        if (ThemeFontMatches(token))
        {
            return;
        }

        var nextFont = new Font(token.FontFamilyName, token.SizeInPoints, token.Style);
        var previous = _themeFont;
        _themeFont = nextFont;
        _themeFontToken = token;
        _settingThemeFont = true;
        try
        {
            Font = nextFont;
        }
        finally
        {
            _settingThemeFont = false;
        }

        previous?.Dispose();
        ApplyChildFonts();
    }

    private bool ThemeFontMatches(BootstrapFontToken token)
    {
        return _themeFont is not null &&
            _themeFontToken is not null &&
            string.Equals(_themeFontToken.FontFamilyName, token.FontFamilyName, StringComparison.OrdinalIgnoreCase) &&
            Math.Abs(_themeFontToken.SizeInPoints - token.SizeInPoints) < 0.01f &&
            _themeFontToken.Style == token.Style;
    }

    private void ApplyChildFonts()
    {
        _editor.Font = Font;
        _placeholder.Font = Font;
        _clearButton.Font = Font;
    }

    private void DisposeThemeFont()
    {
        var font = _themeFont;
        _themeFont = null;
        _themeFontToken = null;
        font?.Dispose();
    }

    private void FocusEditor()
    {
        SynchronizeEditorAccessibility();
        if (Enabled && !_editor.Focused)
        {
            _editor.Focus();
        }
    }

    private void UpdatePlaceholderVisibility()
    {
        _placeholder.Text = _placeholderText;
        _placeholder.Visible = !_editorHasFocus && _editor.TextLength == 0 && _placeholderText.Length > 0;
        if (_placeholder.Visible)
        {
            _placeholder.BringToFront();
        }
    }

    private void UpdateClearButtonVisibility()
    {
        _clearButton.Visible = _showClearButton && Enabled && !ReadOnly && _editor.TextLength > 0;
        if (_clearButton.Visible)
        {
            _clearButton.BringToFront();
        }
    }

    private void LayoutChildren()
    {
        SynchronizeEditorAccessibility();
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var horizontalPadding = DpiScaler.Scale(GetLogicalHorizontalPadding(theme.Metrics), dpi);
        var spacing = DpiScaler.Scale(theme.Metrics.SpacingXS, dpi);
        var iconExtent = DpiScaler.Scale(theme.Metrics.SpacingLG, dpi);
        var accessoryExtent = _frameworkTrailingAccessoryCompactHitArea
            ? DpiScaler.Scale(theme.Metrics.ControlHeightSmall, dpi)
            : iconExtent;

        var left = horizontalPadding;
        if (_icon is not null)
        {
            left += iconExtent + spacing;
        }

        var right = Math.Max(left, ClientSize.Width - horizontalPadding);
        if (_frameworkTrailingAccessory is not null)
        {
            var accessoryLeft = Math.Max(left, right - accessoryExtent);
            var accessoryHeight = Math.Min(accessoryExtent, Math.Max(1, ClientSize.Height - 2));
            _frameworkTrailingAccessory.Bounds = new Rectangle(accessoryLeft, (ClientSize.Height - accessoryHeight) / 2, accessoryExtent, accessoryHeight);
            right = Math.Max(left, accessoryLeft - spacing);
        }

        if (_clearButton.Visible)
        {
            var clearLeft = Math.Max(left, right - iconExtent);
            _clearButton.Bounds = new Rectangle(clearLeft, (ClientSize.Height - iconExtent) / 2, iconExtent, iconExtent);
            right = Math.Max(left, clearLeft - spacing);
        }
        else if (_trailingIcon is not null)
        {
            right = Math.Max(left, right - iconExtent - spacing);
        }

        var editorHeight = Math.Min(_editor.PreferredHeight, Math.Max(1, ClientSize.Height - 2));
        var editorTop = Math.Max(0, (ClientSize.Height - editorHeight) / 2);
        if (_editor.Multiline)
        {
            var verticalPadding = Math.Max(1, DpiScaler.Scale(theme.Metrics.SpacingXS, dpi));
            editorTop = Math.Min(verticalPadding, Math.Max(0, (ClientSize.Height - 1) / 2));
            editorHeight = Math.Max(1, ClientSize.Height - (editorTop * 2));
        }

        var editorWidth = Math.Max(1, right - left);
        var editorBounds = new Rectangle(left, editorTop, editorWidth, editorHeight);
        _editor.Bounds = editorBounds;
        _placeholder.Bounds = editorBounds;
        _placeholder.TextAlign = _editor.Multiline ? ContentAlignment.TopLeft : ContentAlignment.MiddleLeft;

        if (_placeholder.Visible)
        {
            _placeholder.BringToFront();
        }

        if (_clearButton.Visible)
        {
            _clearButton.BringToFront();
        }

        _frameworkTrailingAccessory?.BringToFront();
    }

    private void PaintIcons(Graphics graphics, BootstrapTheme theme, int dpi)
    {
        var iconExtent = DpiScaler.Scale(theme.Metrics.SpacingLG, dpi);
        var horizontalPadding = DpiScaler.Scale(GetLogicalHorizontalPadding(theme.Metrics), dpi);
        var y = Math.Max(0, (ClientSize.Height - iconExtent) / 2);
        var foreground = Enabled ? theme.Colors.MutedText : theme.Colors.Disabled;

        if (_icon is not null)
        {
            _iconRenderer.TryRender(
                graphics,
                _icon,
                new Rectangle(horizontalPadding, y, iconExtent, iconExtent),
                foreground);
        }

        if (_trailingIcon is not null && !_clearButton.Visible)
        {
            var right = _frameworkTrailingAccessory is null
                ? ClientSize.Width - horizontalPadding
                : _frameworkTrailingAccessory.Left - DpiScaler.Scale(theme.Metrics.SpacingXS, dpi);
            var x = Math.Max(horizontalPadding, right - iconExtent);
            _iconRenderer.TryRender(
                graphics,
                _trailingIcon,
                new Rectangle(x, y, iconExtent, iconExtent),
                foreground);
        }
    }

    private int GetLogicalHorizontalPadding(BootstrapThemeMetrics metrics)
    {
        if (_connectedSizeOverride == BootstrapConnectedControlSize.Small)
        {
            return metrics.SpacingXS;
        }

        return _connectedSizeOverride == BootstrapConnectedControlSize.Large
            ? metrics.SpacingMD
            : metrics.SpacingSM;
    }

    private int GetLogicalRadius(BootstrapThemeMetrics metrics)
    {
        if (_connectedSizeOverride == BootstrapConnectedControlSize.Small)
        {
            return metrics.RadiusSmall;
        }

        return _connectedSizeOverride == BootstrapConnectedControlSize.Large
            ? metrics.RadiusLarge
            : metrics.Radius;
    }

    private static int GetLogicalHeight(BootstrapThemeMetrics metrics, BootstrapConnectedControlSize size)
    {
        return size == BootstrapConnectedControlSize.Small
            ? metrics.ControlHeightSmall
            : (size == BootstrapConnectedControlSize.Large ? metrics.ControlHeightLarge : metrics.ControlHeight);
    }

    private static CornerRadius ScaleCornerRadius(CornerRadius radius, int dpi)
    {
        return new CornerRadius(
            DpiScaler.Scale(radius.TopLeft, dpi),
            DpiScaler.Scale(radius.TopRight, dpi),
            DpiScaler.Scale(radius.BottomRight, dpi),
            DpiScaler.Scale(radius.BottomLeft, dpi));
    }

    private void OnEditorHandleCreated(object? sender, EventArgs e)
    {
        SynchronizeEditorAccessibility();
        _editorAccessibilityWindow.AssignHandle(_editor.Handle);
    }

    private void OnEditorHandleDestroyed(object? sender, EventArgs e)
    {
        _editorAccessibilityWindow.ReleaseHandle();
    }

    private void SynchronizeEditorAccessibility()
    {
        if (_synchronizingEditorAccessibility)
        {
            return;
        }

        _synchronizingEditorAccessibility = true;
        try
        {
            _editor.AccessibleName = ResolveEditorAccessibilityValue(
                AccessibleName, _editor.AccessibleName,
                ref _forwardedAccessibleName, ref _previousEditorAccessibleName);
            _editor.AccessibleDescription = ResolveEditorAccessibilityValue(
                AccessibleDescription, _editor.AccessibleDescription,
                ref _forwardedAccessibleDescription, ref _previousEditorAccessibleDescription);
        }
        finally
        {
            _synchronizingEditorAccessibility = false;
        }
    }

    private static string? ResolveEditorAccessibilityValue(
        string? ownerValue, string? editorValue, ref string? forwardedValue, ref string? previousEditorValue)
    {
        if (ownerValue is not null)
        {
            if (forwardedValue is null || !string.Equals(editorValue, forwardedValue, StringComparison.Ordinal))
            {
                previousEditorValue = editorValue;
            }

            forwardedValue = ownerValue;
            return ownerValue;
        }

        // A derived control may name its native editor independently, as Select's search editor does.
        if (forwardedValue is null)
        {
            return editorValue;
        }

        var restoredValue = string.Equals(editorValue, forwardedValue, StringComparison.Ordinal)
            ? previousEditorValue
            : editorValue;
        forwardedValue = null;
        previousEditorValue = null;
        return restoredValue;
    }

    private sealed class EditorAccessibilityWindow : NativeWindow
    {
        private const int WmSetFocus = 0x0007;
        private const int WmGetObject = 0x003D;
        private readonly BootstrapTextBox _owner;

        internal EditorAccessibilityWindow(BootstrapTextBox owner)
        {
            _owner = owner;
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == WmSetFocus || message.Msg == WmGetObject)
            {
                // Synchronize metadata before the native provider handles the request; never replace it.
                _owner.SynchronizeEditorAccessibility();
            }

            base.WndProc(ref message);
        }
    }
}
