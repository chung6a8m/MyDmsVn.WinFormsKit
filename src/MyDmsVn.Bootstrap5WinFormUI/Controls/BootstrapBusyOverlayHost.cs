using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Compatibility;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Owns one content region and displays delayed, token-owned busy presentations.</summary>
public sealed class BootstrapBusyOverlayHost : Panel
{
    private readonly int _uiThread = Thread.CurrentThread.ManagedThreadId;
    private readonly BootstrapBusyOverlayController _controller;
    private readonly BootstrapBusyLayeredBackdrop _backdrop;
    private readonly BootstrapBusyOverlayInputFilter _inputFilter;
    private readonly Panel _messageViewport;
    private readonly Panel _card;
    private readonly BootstrapSpinner _spinner;
    private readonly Label _message;
    private readonly Button _cancel;
    private double _opacity = .35;
    private bool _disposing, _handleDestroying, _hasCancel;

    /// <summary>Creates a host and takes ownership of an unparented content control.</summary>
    public BootstrapBusyOverlayHost(Control content) : this(content, new BootstrapBusyDelay()) { }

    internal BootstrapBusyOverlayHost(Control content, IBootstrapBusyDelay delay) : this(content, delay, BootstrapBusyLayeredWindow.Instance) { }

    internal BootstrapBusyOverlayHost(Control content, IBootstrapBusyDelay delay, IBootstrapBusyLayeredWindow native)
    {
        if (content is null) throw new ArgumentNullException(nameof(content));
        if (content.Parent is not null) throw new ArgumentException("Content must be unparented.", nameof(content));
        if (delay is null) throw new ArgumentNullException(nameof(delay));
        Content = content;
        _backdrop = new BootstrapBusyLayeredBackdrop(native) { Dock = DockStyle.Fill, Visible = false };
        _inputFilter = new BootstrapBusyOverlayInputFilter(content);
        _messageViewport = new Panel { AutoScroll = true, TabStop = false };
        _card = new Panel { Visible = false, TabStop = false };
        _spinner = new BootstrapSpinner { Spinning = false, TabStop = false, AccessibleName = "Busy" };
        _message = new Label { AutoSize = false, UseMnemonic = false, TextAlign = ContentAlignment.MiddleCenter, AccessibleRole = AccessibleRole.StaticText };
        _cancel = new Button { Text = "Cancel", Visible = false, AccessibleRole = AccessibleRole.PushButton };
        _cancel.Click += OnCancel;
        _messageViewport.Controls.Add(_message);
        _card.Controls.AddRange(new Control[] { _spinner, _messageViewport, _cancel });
        _controller = new BootstrapBusyOverlayController(delay, CanPresent, Present, HidePresentation);
        content.Dock = DockStyle.Fill;
        Controls.AddRange(new[] { content, _backdrop, _card });
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme();
    }

    /// <summary>Gets the content whose lifetime is owned by this host.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Control Content { get; }

    /// <summary>Gets or sets backdrop opacity from zero to one; the foreground stays opaque.</summary>
    [DefaultValue(.35)]
    public double BackdropOpacity
    {
        get => _opacity;
        set
        {
            AssertUiThread();
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0 || value > 1)
                throw new ArgumentOutOfRangeException(nameof(value));
            if (_opacity == value) return;
            _opacity = value;
            if (RenderMode != BootstrapBusyOverlayRenderMode.None) UpdateBackdrop();
        }
    }

    /// <summary>Gets the actual visible rendering mode, or None while hidden or pending.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public BootstrapBusyOverlayRenderMode RenderMode { get; private set; }

    /// <summary>Occurs after the actual rendering mode changes.</summary>
    public event EventHandler? RenderModeChanged;

    /// <summary>Begins a busy operation; dispose its token on the owning UI thread to finish it.</summary>
    public IDisposable Begin(BusyPresentation presentation)
    {
        if (IsDisposed || _disposing) throw new ObjectDisposedException(nameof(BootstrapBusyOverlayHost));
        AssertUiThread();
        return _controller.Begin(presentation);
    }

    internal static byte ToAlpha(double opacity) => (byte)Math.Round(255 * opacity, MidpointRounding.AwayFromZero);

    /// <inheritdoc />
    protected override void OnVisibleChanged(EventArgs e) { base.OnVisibleChanged(e); _controller?.RefreshVisibility(); }
    /// <inheritdoc />
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); _handleDestroying = false; _controller?.RefreshVisibility(); }
    /// <inheritdoc />
    protected override void OnHandleDestroyed(EventArgs e) { _handleDestroying = true; _controller?.RefreshVisibility(); base.OnHandleDestroyed(e); }
    /// <inheritdoc />
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        if (_card is null) return;
        var layout = BootstrapBusyOverlayLayoutLogic.Calculate(ClientSize, Font, _message.Text, _cancel.Text, _hasCancel, DeviceDpi, BootstrapThemeManager.CurrentTheme.Metrics);
        _card.Bounds = layout.Card;
        _spinner.Bounds = layout.Spinner;
        _spinner.Visible = layout.Spinner.Width > 0 && layout.Spinner.Height > 0;
        _messageViewport.Bounds = layout.Viewport;
        _message.Bounds = new Rectangle(Point.Empty, layout.Message);
        _cancel.Bounds = layout.Cancel;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposing)
        {
            AssertUiThread();
            _disposing = true;
            BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
            _cancel.Click -= OnCancel;
            try
            {
                _controller.Dispose();
            }
            finally
            {
                base.Dispose(disposing);
            }
            return;
        }
        base.Dispose(disposing);
    }

    private bool CanPresent() => !_disposing && !_handleDestroying && !IsDisposed && IsHandleCreated && Visible && ClientSize.Width > 0 && ClientSize.Height > 0 && !Content.IsDisposed;
    private void Present(BusyPresentation presentation)
    {
        if (!CanPresent()) return;
        _message.Text = presentation.Message;
        _message.AccessibleName = presentation.Message;
        _hasCancel = presentation.CancelRequested is not null;
        _cancel.Visible = _hasCancel;
        PerformLayout();
        var mode = ConfigureBackdrop();
        _inputFilter.Install();
        _backdrop.Visible = true;
        _card.Visible = true;
        _backdrop.BringToFront();
        _card.BringToFront();
        _spinner.Start();
        SetMode(mode);
        // No work after the event: its handler may hide/dispose/reenter Begin.
    }
    private void HidePresentation()
    {
        if (_card is null) return;
        _inputFilter.Dispose();
        _spinner.Stop();
        _card.Visible = false;
        _backdrop.Visible = false;
        SetMode(BootstrapBusyOverlayRenderMode.None);
    }
    private void SetMode(BootstrapBusyOverlayRenderMode mode)
    {
        if (RenderMode == mode) return;
        RenderMode = mode;
        RenderModeChanged?.Invoke(this, EventArgs.Empty);
    }
    private void OnCancel(object? sender, EventArgs e) => _controller.RequestCancel();
    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e) { if (!_disposing) ApplyTheme(); }
    private void ApplyTheme()
    {
        var colors = BootstrapThemeManager.CurrentTheme.Colors;
        _card.BackColor = Color.FromArgb(255, colors.Surface);
        _message.ForeColor = colors.Text;
        _cancel.BackColor = Color.FromArgb(255, colors.Surface);
        _cancel.ForeColor = colors.Text;
        PerformLayout();
        if (RenderMode != BootstrapBusyOverlayRenderMode.None) UpdateBackdrop();
    }
    internal int BlockedMouseMessages => _inputFilter.Blocked;
    internal bool InputFilterInstalled => _inputFilter.Installed;
    private BootstrapBusyOverlayRenderMode ConfigureBackdrop()
    {
        var colors = BootstrapThemeManager.CurrentTheme.Colors;
        return _backdrop.Configure(ToAlpha(_opacity), colors.Dark, colors.SurfaceSecondary);
    }
    private void UpdateBackdrop()
    {
        var mode = ConfigureBackdrop();
        _backdrop.BringToFront(); _card.BringToFront();
        SetMode(mode);
    }
    /// <inheritdoc />
    protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); if (_card is not null) PerformLayout(); }
    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); PerformLayout(); }
    private void AssertUiThread()
    {
        if (Thread.CurrentThread.ManagedThreadId != _uiThread)
            throw new InvalidOperationException("Busy overlay must be managed on the owning UI thread.");
    }
}
