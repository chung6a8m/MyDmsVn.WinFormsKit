using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides a Bootstrap-inspired native WinForms tab control while preserving <see cref="TabPage"/> composition and selection behavior.
/// </summary>
[DefaultEvent(nameof(SelectedIndexChanged))]
public class BootstrapTabControl : TabControl
{
    private BootstrapTabStyle _tabStyle = BootstrapTabStyle.Tabs;
    private BootstrapVariant _variant = BootstrapVariant.Primary;
    private bool _fill;
    private int _borderRadius = -1;
    private readonly BootstrapTabInteractionController _interaction;
    private bool _themeSubscribed;
    private bool _settingThemeFont;
    private bool _useThemeFont = true;
    private bool _updatingItemSize;
    private Size[] _sizedTabImageSizes = Array.Empty<Size>();
    private Font? _themeFont;
    private readonly BootstrapTabCloseController _close;

    /// <summary>Requests that the owner close a page. The collection and page lifetime remain owner controlled.</summary>
    public event EventHandler<TabCloseRequestedEventArgs>? TabCloseRequested;

    /// <summary>Enables or disables the optional close affordance for one native page.</summary>
    public void SetTabClosable(TabPage page, bool closable)
    {
        if (page is null) throw new ArgumentNullException(nameof(page));
        if (!TabPages.Contains(page)) throw new ArgumentException("The page must belong to this tab control.", nameof(page));
        _close.SetClosable(page, closable);
        ApplyHeaderItemSize();
        Invalidate();
    }

    /// <summary>Gets whether an owned page can request a close.</summary>
    public bool IsTabClosable(TabPage page) => _close.IsClosable(page);

    /// <summary>Raises a close request for an enabled, closable page without removing or selecting it.</summary>
    public bool RequestTabClose(TabPage page, TabCloseReason reason = TabCloseReason.Programmatic)
    {
        if (!Enabled || page is null || !page.Enabled || !IsTabClosable(page)) return false;
        TabCloseRequested?.Invoke(this, new TabCloseRequestedEventArgs(page, reason));
        return true;
    }

    /// <summary>
    /// Initializes a designer-safe Bootstrap-inspired tab control using native WinForms tab pages and selection behavior.
    /// </summary>
    public BootstrapTabControl()
    {
        _interaction = new BootstrapTabInteractionController(this, () => DrawMode == TabDrawMode.OwnerDrawFixed);
        _close = new BootstrapTabCloseController(this, RequestTabClose);
        DrawMode = TabDrawMode.OwnerDrawFixed;
        SizeMode = TabSizeMode.Fixed;

        DrawItem += OnDrawTabItem;
        ControlAdded += OnTabControlAdded;
        ControlRemoved += OnTabControlRemoved;
        FontChanged += OnTabFontChanged;
        SizeChanged += OnTabSizeChanged;
        DpiChangedAfterParent += OnTabDpiChangedAfterParent;
        MouseMove += OnTabMouseMove;
        MouseLeave += OnTabMouseLeave;
        GotFocus += OnTabFocusChanged;
        LostFocus += OnTabFocusChanged;
        EnabledChanged += OnTabEnabledChanged;
        SelectedIndexChanged += OnTabSelectedIndexChanged;
        HandleCreated += OnTabHandleCreated;
        HandleDestroyed += OnTabHandleDestroyed;

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyThemeFont();
        ApplyHeaderItemSize();
    }

    /// <summary>
    /// Gets or sets the Bootstrap-inspired visual treatment used for tab headers.
    /// </summary>
    [Category("Appearance")]
    [Description("Selects Tabs, Pills, or Underline presentation for native tab headers.")]
    [DefaultValue(BootstrapTabStyle.Tabs)]
    public BootstrapTabStyle TabStyle
    {
        get => _tabStyle;
        set
        {
            BootstrapTabControlRenderLogic.ValidateStyle(value);
            if (_tabStyle == value)
            {
                return;
            }

            _tabStyle = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the semantic Bootstrap-inspired accent variant used by selected tab headers.
    /// </summary>
    [Category("Appearance")]
    [Description("Selects the semantic Bootstrap-inspired accent color used by selected tab headers.")]
    [DefaultValue(BootstrapVariant.Primary)]
    public BootstrapVariant Variant
    {
        get => _variant;
        set
        {
            BootstrapTabControlRenderLogic.ValidateVariant(value);
            if (_variant == value)
            {
                return;
            }

            _variant = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets whether the fixed-width native tab headers share the available client width evenly.
    /// </summary>
    [Category("Layout")]
    [Description("Makes all fixed-width tab headers share the available client width evenly while retaining native overflow behavior.")]
    [DefaultValue(false)]
    public bool Fill
    {
        get => _fill;
        set
        {
            if (_fill == value)
            {
                return;
            }

            _fill = value;
            ApplyHeaderItemSize();
            Invalidate();
            _interaction.RefreshFromPointer();
        }
    }

    /// <summary>
    /// Gets or sets a uniform logical corner radius for Tabs and Pills. Use -1 to select the current theme radius.
    /// </summary>
    [Category("Appearance")]
    [Description("Sets a uniform logical tab-header corner radius, or -1 to use the current theme radius.")]
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

            DrawItem -= OnDrawTabItem;
            ControlAdded -= OnTabControlAdded;
            ControlRemoved -= OnTabControlRemoved;
            FontChanged -= OnTabFontChanged;
            SizeChanged -= OnTabSizeChanged;
            DpiChangedAfterParent -= OnTabDpiChangedAfterParent;
            MouseMove -= OnTabMouseMove;
            MouseLeave -= OnTabMouseLeave;
            GotFocus -= OnTabFocusChanged;
            LostFocus -= OnTabFocusChanged;
            EnabledChanged -= OnTabEnabledChanged;
            SelectedIndexChanged -= OnTabSelectedIndexChanged;
            HandleCreated -= OnTabHandleCreated;
            HandleDestroyed -= OnTabHandleDestroyed;
            _interaction.Dispose();

            foreach (TabPage page in TabPages)
            {
                DetachTabPage(page);
            }

            _close.Dispose();

            DisposeThemeFont();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            return _interaction is null ? parameters : FilterTabStyle(parameters);
        }
    }

    private CreateParams FilterTabStyle(CreateParams parameters)
    {
        parameters.Style = _interaction.FilterNativeStyle(parameters.Style);
        return parameters;
    }

    /// <inheritdoc />
    protected override bool ProcessCmdKey(ref Message message, Keys keyData)
    {
        if (_close.TryProcessShortcut(keyData)) return true;
        return base.ProcessCmdKey(ref message, keyData);
    }

    /// <inheritdoc />
    protected override void WndProc(ref Message message)
    {
        if (message.Msg == 0x0201)
        {
            var value = message.LParam.ToInt64();
            var location = new Point(unchecked((short)(value & 0xffff)), unchecked((short)((value >> 16) & 0xffff)));
            if (_close.TryHandleMouseDown(location))
            {
                message.Result = IntPtr.Zero;
                return;
            }
        }
        if (message.Msg == 0x0202 && _close.TryHandleMouseUp())
        {
            message.Result = IntPtr.Zero;
            return;
        }
        base.WndProc(ref message);
    }

    /// <summary>Returns the DPI-scaled close hit target within a native header.</summary>
    protected Rectangle GetTabCloseButtonBounds(int index) => _close.GetButtonBounds(index);

    private void OnDrawTabItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= TabPages.Count)
        {
            return;
        }

        if (!_fill && NativeImageSizingChanged())
        {
            ApplyHeaderItemSize();
        }

        var page = TabPages[e.Index];
        var theme = BootstrapThemeManager.CurrentTheme;
        var metrics = BootstrapTabControlRenderLogic.ResolveMetrics(theme.Metrics, GetCurrentDpi(), _borderRadius);
        var image = ResolveTabImage(page);
        var textWidth = MeasureTabTextWidth(page.Text, metrics.Height);
        var imageSize = image?.Size ?? Size.Empty;
        var closeBounds = IsTabClosable(page) ? GetTabCloseButtonBounds(e.Index) : Rectangle.Empty;
        var layout = BootstrapTabControlRenderLogic.CalculateLayout(
            e.Bounds,
            _tabStyle,
            metrics,
            textWidth,
            imageSize,
            image is not null,
            closeBounds);
        var enabled = Enabled && page.Enabled;
        var palette = BootstrapTabControlRenderLogic.ResolvePalette(
            theme.Colors,
            _variant,
            _tabStyle,
            e.Index == SelectedIndex,
            enabled,
            HotTrack && e.Index == _interaction.HoveredIndex);

        BootstrapTabHeaderPainter.DrawHeader(e, layout, metrics, palette, page.Text ?? string.Empty, image, closeBounds, enabled, e.Index == SelectedIndex, Focused && ShowFocusCues);
    }

    private void OnTabControlAdded(object? sender, ControlEventArgs e)
    {
        if (e.Control is TabPage page)
        {
            AttachTabPage(page);
            ApplyHeaderItemSize();
            Invalidate();
            _interaction.RefreshFromPointer();
        }
    }

    private void OnTabControlRemoved(object? sender, ControlEventArgs e)
    {
        if (e.Control is TabPage page)
        {
            DetachTabPage(page);
            _close.PageRemoved(page);
            ApplyHeaderItemSize();
            _interaction.Reset();
            _interaction.RefreshFromPointer();
            Invalidate();
        }
    }

    private void AttachTabPage(TabPage page)
    {
        page.TextChanged -= OnTabPagePresentationChanged;
        page.EnabledChanged -= OnTabPagePresentationChanged;
        page.TextChanged += OnTabPagePresentationChanged;
        page.EnabledChanged += OnTabPagePresentationChanged;
    }

    private void DetachTabPage(TabPage page)
    {
        page.TextChanged -= OnTabPagePresentationChanged;
        page.EnabledChanged -= OnTabPagePresentationChanged;
    }

    private void OnTabPagePresentationChanged(object? sender, EventArgs e)
    {
        if (sender is TabPage page) _close.PagePresentationChanged(page);
        ApplyHeaderItemSize();
        Invalidate();
        _interaction.RefreshFromPointer();
    }

    private void OnTabFontChanged(object? sender, EventArgs e)
    {
        if (!_settingThemeFont)
        {
            _useThemeFont = false;
            DisposeThemeFont();
        }

        ApplyHeaderItemSize();
        Invalidate();
        _interaction.RefreshFromPointer();
    }

    private void OnTabSizeChanged(object? sender, EventArgs e)
    {
        if (_fill)
        {
            ApplyHeaderItemSize();
        }

        Invalidate();
        _interaction.RefreshFromPointer();
    }

    private void OnTabDpiChangedAfterParent(object? sender, EventArgs e)
    {
        ApplyHeaderItemSize();
        Invalidate();
        _interaction.RefreshFromPointer();
    }

    private void OnTabMouseMove(object? sender, MouseEventArgs e)
    {
        _interaction.MouseMoved(e.Location);
    }

    private void OnTabMouseLeave(object? sender, EventArgs e)
    {
        _interaction.MouseLeft();
    }

    private void OnTabFocusChanged(object? sender, EventArgs e)
    {
        InvalidateSelectedHeader();
    }

    private void OnTabEnabledChanged(object? sender, EventArgs e)
    {
        Invalidate();
        _interaction.RefreshFromPointer();
    }

    private void OnTabSelectedIndexChanged(object? sender, EventArgs e)
    {
        _close.SelectionChanged();
        Invalidate();
        _interaction.RefreshFromPointer();
    }

    private void OnTabHandleCreated(object? sender, EventArgs e) => _interaction.RefreshFromPointer();

    private void OnTabHandleDestroyed(object? sender, EventArgs e) => _interaction.Reset();

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

        ApplyHeaderItemSize();
        Invalidate();
        _interaction.RefreshFromPointer();
    }

    private void ApplyHeaderItemSize()
    {
        if (_updatingItemSize || IsDisposed)
        {
            return;
        }

        var theme = BootstrapThemeManager.CurrentTheme;
        var metrics = BootstrapTabControlRenderLogic.ResolveMetrics(theme.Metrics, GetCurrentDpi(), _borderRadius);
        var preferredWidths = new int[TabPages.Count];
        for (var index = 0; index < TabPages.Count; index++)
        {
            preferredWidths[index] = MeasurePreferredContentWidth(TabPages[index], metrics);
        }

        var width = BootstrapTabControlRenderLogic.CalculateUniformItemWidth(
            TabPages.Count,
            ClientSize.Width,
            preferredWidths,
            metrics,
            _fill);
        var next = new Size(Math.Max(1, width), Math.Max(1, metrics.Height));
        CaptureNativeImageSizing();
        if (ItemSize == next)
        {
            return;
        }

        _updatingItemSize = true;
        try
        {
            ItemSize = next;
        }
        finally
        {
            _updatingItemSize = false;
        }
    }

    private int MeasurePreferredContentWidth(TabPage page, BootstrapTabHeaderMetrics metrics)
    {
        var textWidth = MeasureTabTextWidth(page.Text, metrics.Height);
        var image = ResolveTabImage(page);
        var closeWidth = IsTabClosable(page) ? DpiScaler.Scale(24, GetCurrentDpi()) : 0;
        if (image is null)
        {
            return textWidth + closeWidth;
        }

        var spacing = textWidth > 0 ? metrics.ContentSpacing : 0;
        return image.Width + spacing + textWidth + closeWidth;
    }

    private int MeasureTabTextWidth(string? text, int height)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        return TextRenderer.MeasureText(
            text,
            Font,
            new Size(int.MaxValue, Math.Max(1, height)),
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding).Width;
    }

    private Image? ResolveTabImage(TabPage page)
    {
        var images = ImageList?.Images;
        if (images is null || images.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(page.ImageKey) && images.ContainsKey(page.ImageKey))
        {
            return images[page.ImageKey];
        }

        var imageIndex = page.ImageIndex;
        return imageIndex >= 0 && imageIndex < images.Count ? images[imageIndex] : null;
    }

    private bool NativeImageSizingChanged()
    {
        if (_sizedTabImageSizes.Length != TabPages.Count)
        {
            return true;
        }

        for (var index = 0; index < TabPages.Count; index++)
        {
            var currentSize = ResolveTabImage(TabPages[index])?.Size ?? Size.Empty;
            if (currentSize != _sizedTabImageSizes[index])
            {
                return true;
            }
        }

        return false;
    }

    private void CaptureNativeImageSizing()
    {
        var sizes = new Size[TabPages.Count];
        for (var index = 0; index < TabPages.Count; index++)
        {
            sizes[index] = ResolveTabImage(TabPages[index])?.Size ?? Size.Empty;
        }

        _sizedTabImageSizes = sizes;
    }

    private void InvalidateSelectedHeader()
    {
        InvalidateHeader(SelectedIndex);
    }

    private void InvalidateHeader(int index)
    {
        if (index < 0 || index >= TabPages.Count || !IsHandleCreated)
        {
            return;
        }

        Invalidate(GetTabRect(index));
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
}
