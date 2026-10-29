using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides a Bootstrap-inspired presentation for the native WinForms <see cref="ComboBox"/> while preserving native item, binding, selection, editing, keyboard, auto-complete, and drop-down behavior.
/// </summary>
[DefaultEvent(nameof(SelectedIndexChanged))]
public class BootstrapComboBox : ComboBox
{
    private const int WmPaint = 0x000F;
    private const int WmNcPaint = 0x0085;
    private const int CbGetItemHeight = 0x0154;
    private const int CbSetItemHeight = 0x0153;
    private static readonly IIconRenderer DefaultIconRenderer = BootstrapIconRenderer.CreateDefault();

    private BootstrapValidationState _validationState = BootstrapValidationState.None;
    private int _borderRadius = -1;
    private IconDescriptor? _leadingIcon;
    private IIconRenderer _iconRenderer = DefaultIconRenderer;
    private bool _themeSubscribed;
    private bool _settingThemeFont;
    private bool _useThemeFont = true;
    private bool _settingShellRegion;
    private bool _manageShellRegion = true;
    private Font? _themeFont;
    private Region? _shellRegion;
    private bool _sizingInitialized;
    private bool _applyingSizeMetrics;

    /// <summary>
    /// Initializes a designer-safe native-backed Bootstrap combo box.
    /// </summary>
    public BootstrapComboBox()
    {
        base.DrawMode = DrawMode.OwnerDrawFixed;
        FlatStyle = FlatStyle.Flat;
        IntegralHeight = true;

        FontChanged += OnComboBoxFontChanged;
        DpiChangedAfterParent += OnComboBoxDpiChangedAfterParent;
        EnabledChanged += OnComboBoxPresentationChanged;
        GotFocus += OnComboBoxPresentationChanged;
        LostFocus += OnComboBoxPresentationChanged;
        DropDown += OnComboBoxPresentationChanged;
        DropDownClosed += OnComboBoxPresentationChanged;
        SizeChanged += OnComboBoxSizeChanged;
        RegionChanged += OnComboBoxRegionChanged;
        DropDownStyleChanged += OnComboBoxSizingPolicyChanged;

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;

        ApplyThemeFont();
        ApplyThemePresentation();
        _sizingInitialized = true;
        ApplyOwnerDrawMetrics();
    }

    /// <summary>
    /// Gets or sets the validation presentation state without changing native selection or data-binding state.
    /// </summary>
    [Category("Appearance")]
    [Description("Selects neutral, valid, or invalid border presentation for the combo box.")]
    [DefaultValue(BootstrapValidationState.None)]
    public BootstrapValidationState ValidationState
    {
        get => _validationState;
        set
        {
            BootstrapTextBoxRenderLogic.ValidateState(value);
            if (_validationState == value)
            {
                return;
            }

            _validationState = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the logical uniform shell radius, or -1 to use the current theme radius.
    /// </summary>
    [Category("Appearance")]
    [Description("Sets a uniform logical combo-box shell radius, or -1 to use the current theme radius.")]
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
            ApplyShellRegion();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets an optional decorative icon shown in the owner-drawn closed selection area when WinForms exposes that drawing context.
    /// </summary>
    [Category("Appearance")]
    [Description("Specifies an optional source-neutral leading icon for the closed combo-box selection area.")]
    [DefaultValue(null)]
    public IconDescriptor? LeadingIcon
    {
        get => _leadingIcon;
        set
        {
            if (ReferenceEquals(_leadingIcon, value))
            {
                return;
            }

            _leadingIcon = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the source-neutral renderer used for <see cref="LeadingIcon"/>.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IIconRenderer IconRenderer
    {
        get => _iconRenderer;
        set
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (ReferenceEquals(_iconRenderer, value))
            {
                return;
            }

            _iconRenderer = value;
            Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        DrawBootstrapItem(e);
        base.OnDrawItem(e);
    }

    /// <inheritdoc />
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        ApplyThemePresentation();
        ApplyOwnerDrawMetrics();
        ApplyShellRegion();
    }

    /// <inheritdoc />
    protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
    {
        if (!_sizingInitialized || _applyingSizeMetrics || !UsesClosedOwnerDrawSizingPolicy())
        {
            base.SetBoundsCore(x, y, width, height, specified);
            return;
        }

        var sizing = ResolveSizing(ResolveNativeChromeHeight());
        base.SetBoundsCore(x, y, width, sizing.OuterHeight, specified | BoundsSpecified.Height);
        if (!IsHandleCreated)
        {
            UpdateBounds(Left, Top, Width, sizing.OuterHeight);
        }
    }

    /// <inheritdoc />
    public override Size GetPreferredSize(Size proposedSize)
    {
        var preferred = base.GetPreferredSize(proposedSize);
        if (!_sizingInitialized || !UsesClosedOwnerDrawSizingPolicy())
        {
            return preferred;
        }

        return new Size(preferred.Width, ResolveSizing(ResolveNativeChromeHeight()).OuterHeight);
    }

    /// <inheritdoc />
    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        if ((m.Msg == WmPaint || m.Msg == WmNcPaint) && IsHandleCreated && !IsDisposed)
        {
            DrawShellBorder();
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

            FontChanged -= OnComboBoxFontChanged;
            DpiChangedAfterParent -= OnComboBoxDpiChangedAfterParent;
            EnabledChanged -= OnComboBoxPresentationChanged;
            GotFocus -= OnComboBoxPresentationChanged;
            LostFocus -= OnComboBoxPresentationChanged;
            DropDown -= OnComboBoxPresentationChanged;
            DropDownClosed -= OnComboBoxPresentationChanged;
            SizeChanged -= OnComboBoxSizeChanged;
            RegionChanged -= OnComboBoxRegionChanged;
            DropDownStyleChanged -= OnComboBoxSizingPolicyChanged;

            DisposeShellRegion();
            DisposeThemeFont();
        }

        base.Dispose(disposing);
    }

    private void DrawBootstrapItem(DrawItemEventArgs e)
    {
        if (e.Bounds.Width <= 0 || e.Bounds.Height <= 0)
        {
            return;
        }

        var theme = BootstrapThemeManager.CurrentTheme;
        var metrics = ResolveMetrics(theme);
        var palette = BootstrapComboBoxRenderLogic.ResolvePalette(
            theme.Colors,
            _validationState,
            Focused || ContainsFocus,
            Enabled);
        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var comboBoxEdit = (e.State & DrawItemState.ComboBoxEdit) == DrawItemState.ComboBoxEdit;
        var background = selected ? palette.SelectedBackground : palette.Background;
        var foreground = selected ? palette.SelectedForeground : palette.Foreground;

        using (var backgroundBrush = new SolidBrush(background))
        {
            e.Graphics.FillRectangle(backgroundBrush, e.Bounds);
        }

        var showLeadingIcon = comboBoxEdit && _leadingIcon is not null;
        var layout = BootstrapComboBoxRenderLogic.CalculateItemLayout(
            e.Bounds,
            metrics,
            showLeadingIcon,
            trailingReserve: 0);

        if (showLeadingIcon && layout.IconBounds.Width > 0 && layout.IconBounds.Height > 0)
        {
            _iconRenderer.TryRender(e.Graphics, _leadingIcon!, layout.IconBounds, foreground);
        }

        var text = ResolveItemText(e.Index);
        if (layout.TextBounds.Width > 0 && layout.TextBounds.Height > 0 && text.Length > 0)
        {
            TextRenderer.DrawText(
                e.Graphics,
                text,
                Font,
                layout.TextBounds,
                foreground,
                TextFormatFlags.Left |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix);
        }

        if (!comboBoxEdit && (e.State & DrawItemState.Focus) == DrawItemState.Focus && ShowFocusCues)
        {
            e.DrawFocusRectangle();
        }
    }

    private string ResolveItemText(int index)
    {
        if (index >= 0 && index < Items.Count)
        {
            return GetItemText(Items[index]) ?? string.Empty;
        }

        return Text ?? string.Empty;
    }

    private void DrawShellBorder()
    {
        var bounds = ClientRectangle;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var theme = BootstrapThemeManager.CurrentTheme;
        var metrics = ResolveMetrics(theme);
        var containsFocus = Focused || ContainsFocus;
        var palette = BootstrapComboBoxRenderLogic.ResolvePalette(
            theme.Colors,
            _validationState,
            containsFocus,
            Enabled);
        var borderWidth = containsFocus ? metrics.FocusBorderWidth : metrics.BorderWidth;
        if (borderWidth <= 0f)
        {
            return;
        }

        using var graphics = Graphics.FromHwnd(Handle);
        var state = graphics.Save();
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        try
        {
            var geometry = BootstrapRoundedSurfaceGeometry.Resolve(
                new RectangleF(bounds.Left, bounds.Top, bounds.Width, bounds.Height),
                new CornerRadius(metrics.Radius),
                borderWidth);
            using var path = geometry.CreateStrokePath();
            using var pen = new Pen(palette.Border, borderWidth);
            graphics.DrawPath(pen, path);
        }
        finally
        {
            graphics.Restore(state);
        }
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

        ApplyThemePresentation();
        ApplyOwnerDrawMetrics();
        ApplyShellRegion();
        Invalidate();
    }

    private void OnComboBoxFontChanged(object? sender, EventArgs e)
    {
        if (!_settingThemeFont)
        {
            _useThemeFont = false;
            DisposeThemeFont();
        }

        ApplyOwnerDrawMetrics();
        Invalidate();
    }

    private void OnComboBoxDpiChangedAfterParent(object? sender, EventArgs e)
    {
        ApplyOwnerDrawMetrics();
        ApplyShellRegion();
        Invalidate();
    }

    private void OnComboBoxPresentationChanged(object? sender, EventArgs e)
    {
        ApplyThemePresentation();
        Invalidate();
    }

    private void OnComboBoxSizeChanged(object? sender, EventArgs e)
    {
        ApplyShellRegion();
    }

    private void OnComboBoxSizingPolicyChanged(object? sender, EventArgs e)
    {
        ApplyOwnerDrawMetrics();
        ApplyShellRegion();
        Invalidate();
    }

    private void OnComboBoxRegionChanged(object? sender, EventArgs e)
    {
        if (_settingShellRegion)
        {
            return;
        }

        _manageShellRegion = false;
        _shellRegion?.Dispose();
        _shellRegion = null;
    }

    private void ApplyThemePresentation()
    {
        if (IsDisposed)
        {
            return;
        }

        var theme = BootstrapThemeManager.CurrentTheme;
        var palette = BootstrapComboBoxRenderLogic.ResolvePalette(
            theme.Colors,
            _validationState,
            Focused || ContainsFocus,
            Enabled);
        BackColor = palette.Background;
        ForeColor = palette.Foreground;
    }

    private void ApplyOwnerDrawMetrics()
    {
        if (IsDisposed || !_sizingInitialized || _applyingSizeMetrics || !UsesClosedOwnerDrawSizingPolicy())
        {
            return;
        }

        var parent = Parent;
        parent?.SuspendLayout();
        _applyingSizeMetrics = true;
        try
        {
            var nativeChromeHeight = ResolveNativeChromeHeight();
            var sizing = ResolveSizing(nativeChromeHeight);
            if (!IsHandleCreated)
            {
                base.SetBoundsCore(Left, Top, Width, sizing.OuterHeight, BoundsSpecified.Height);
                UpdateBounds(Left, Top, Width, sizing.OuterHeight);
                return;
            }

            ApplyNativeHeights(sizing);
            UpdateBounds();
            var correction = sizing.OuterHeight - Height;
            if (correction != 0 && sizing.SelectionHeight + correction > 0)
            {
                var corrected = new BootstrapComboBoxSizing(
                    sizing.OuterHeight,
                    sizing.SelectionHeight + correction,
                    sizing.ItemHeight + correction);
                ApplyNativeHeights(corrected);
                UpdateBounds();
            }
        }
        finally
        {
            _applyingSizeMetrics = false;
            parent?.ResumeLayout(performLayout: true);
        }
    }

    private void ApplyNativeHeights(BootstrapComboBoxSizing sizing)
    {
        if (ItemHeight != sizing.ItemHeight)
        {
            ItemHeight = sizing.ItemHeight;
        }

        if (IsHandleCreated && GetNativeSelectionHeight() != sizing.SelectionHeight)
        {
            SendMessage(Handle, CbSetItemHeight, new IntPtr(-1), new IntPtr(sizing.SelectionHeight));
        }
    }

    private BootstrapComboBoxSizing ResolveSizing(int nativeChromeHeight)
    {
        var metrics = ResolveMetrics(BootstrapThemeManager.CurrentTheme);
        return BootstrapComboBoxSizingLogic.Resolve(
            metrics.OuterHeight,
            nativeChromeHeight,
            metrics.MinimumFieldHeight);
    }

    private int ResolveNativeChromeHeight()
    {
        if (IsHandleCreated)
        {
            var selectionHeight = GetNativeSelectionHeight();
            var outerHeight = GetNativeOuterHeight();
            if (selectionHeight > 0 && outerHeight >= selectionHeight)
            {
                return outerHeight - selectionHeight;
            }
        }

        return Math.Max(0, SystemInformation.FixedFrameBorderSize.Height * 2);
    }

    private int GetNativeSelectionHeight()
    {
        if (!IsHandleCreated)
        {
            return 0;
        }

        return SendMessage(Handle, CbGetItemHeight, new IntPtr(-1), IntPtr.Zero).ToInt32();
    }

    private int GetNativeOuterHeight()
    {
        if (!IsHandleCreated || !GetWindowRect(Handle, out var bounds))
        {
            return Height;
        }

        return Math.Max(0, bounds.Bottom - bounds.Top);
    }

    private bool UsesClosedOwnerDrawSizingPolicy()
    {
        return DrawMode == DrawMode.OwnerDrawFixed && DropDownStyle != ComboBoxStyle.Simple;
    }

    private void ApplyShellRegion()
    {
        if (!_manageShellRegion || IsDisposed || !IsHandleCreated || Width <= 0 || Height <= 0)
        {
            return;
        }

        var metrics = ResolveMetrics(BootstrapThemeManager.CurrentTheme);
        Region? nextRegion = null;
        if (metrics.Radius > 0f)
        {
            var geometry = BootstrapRoundedSurfaceGeometry.Resolve(
                new RectangleF(0f, 0f, Width, Height),
                new CornerRadius(metrics.Radius),
                Math.Max(metrics.BorderWidth, metrics.FocusBorderWidth));
            using var path = geometry.CreateOuterPath();
            nextRegion = new Region(path);
        }

        var previousRegion = _shellRegion;
        _settingShellRegion = true;
        try
        {
            Region = nextRegion;
            _shellRegion = nextRegion;
            nextRegion = null;
        }
        finally
        {
            _settingShellRegion = false;
            nextRegion?.Dispose();
            previousRegion?.Dispose();
        }
    }

    private void DisposeShellRegion()
    {
        var region = _shellRegion;
        _shellRegion = null;
        if (region is null)
        {
            return;
        }

        _settingShellRegion = true;
        try
        {
            if (ReferenceEquals(Region, region))
            {
                Region = null;
            }
        }
        finally
        {
            _settingShellRegion = false;
        }

        region.Dispose();
    }

    private BootstrapComboBoxMetrics ResolveMetrics(BootstrapTheme theme)
    {
        return BootstrapComboBoxRenderLogic.ResolveMetrics(
            theme.Metrics,
            Math.Max(1, Font.Height),
            GetCurrentDpi(),
            _borderRadius);
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
    }

    private bool ThemeFontMatches(BootstrapFontToken token)
    {
        return _themeFont is not null &&
            string.Equals(_themeFont.Name, token.FontFamilyName, StringComparison.OrdinalIgnoreCase) &&
            Math.Abs(_themeFont.SizeInPoints - token.SizeInPoints) < 0.01f &&
            _themeFont.Style == token.Style;
    }

    private void DisposeThemeFont()
    {
        var font = _themeFont;
        _themeFont = null;
        font?.Dispose();
    }

    private int GetCurrentDpi()
    {
        return DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect bounds);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
