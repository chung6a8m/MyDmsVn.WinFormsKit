using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
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
    private bool _disposing, _handleDestroying, _hasCancel, _hasPositiveClientSize;
    private long _presentationGeneration;
    private readonly List<Control> _ancestors = new();
    private Form? _owner;
    private Control? _savedFocus;
    private bool _contentHandleLost;

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
        _hasPositiveClientSize = ClientSize.Width > 0 && ClientSize.Height > 0;
        content.Dock = DockStyle.Fill;
        Controls.AddRange(new[] { content, _backdrop, _card });
        content.HandleDestroyed += OnContentHandleDestroyed;
        content.HandleCreated += OnContentHandleCreated;
        content.Disposed += OnContentDisposed;
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
    protected override void OnParentChanged(EventArgs e)
    {
        base.OnParentChanged(e);
        if (_controller is null || _disposing) return;
        TrackAncestors();
        _controller.RefreshVisibility();
    }
    /// <inheritdoc />
    protected override void OnClientSizeChanged(EventArgs e)
    {
        base.OnClientSizeChanged(e);
        var positive = ClientSize.Width > 0 && ClientSize.Height > 0;
        if (positive == _hasPositiveClientSize) return;
        // Commit before callbacks: a mode subscriber may resize/Begin again.
        _hasPositiveClientSize = positive;
        _controller?.RefreshVisibility();
    }
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
            UntrackAncestors();
            Content.HandleDestroyed -= OnContentHandleDestroyed;
            Content.HandleCreated -= OnContentHandleCreated;
            Content.Disposed -= OnContentDisposed;
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

    private bool CanPresent() => !_disposing && !_handleDestroying && !_contentHandleLost && !IsDisposed && IsHandleCreated && Visible && ClientSize.Width > 0 && ClientSize.Height > 0 && !Content.IsDisposed && !Content.Disposing && (_owner is null || (!_owner.IsDisposed && !_owner.Disposing && _owner.WindowState != FormWindowState.Minimized));
    private void Present(BusyPresentation presentation)
    {
        if (!CanPresent()) return;
        var generation = ++_presentationGeneration;
        if (!_inputFilter.Installed)
        {
            var focused = Control.FromChildHandle(GetFocus());
            _savedFocus = focused == Content || (focused is not null && Content.Contains(focused)) ? focused : null;
        }
        _message.Text = presentation.Message;
        if (!IsCurrentPresentation(generation)) return;
        _message.AccessibleName = presentation.Message;
        _hasCancel = presentation.CancelRequested is not null;
        if (!_hasCancel && _cancel.ContainsFocus) RestoreFocus();
        if (!IsCurrentPresentation(generation)) return;
        _cancel.Visible = _hasCancel;
        PerformLayout();
        if (!IsCurrentPresentation(generation)) return;
        var mode = ConfigureBackdrop();
        if (!IsCurrentPresentation(generation)) return;
        _inputFilter.Install();
        if (!IsCurrentPresentation(generation)) return;
        _backdrop.Visible = true;
        if (!IsCurrentPresentation(generation)) return;
        _card.Visible = true;
        if (!IsCurrentPresentation(generation)) return;
        _backdrop.BringToFront();
        _card.BringToFront();
        if (!IsCurrentPresentation(generation)) return;
        _spinner.Start();
        SetMode(mode);
        // No work after the event: its handler may hide/dispose/reenter Begin.
    }
    private bool IsCurrentPresentation(long generation) => generation == _presentationGeneration && CanPresent();
    private void HidePresentation()
    {
        if (_card is null) return;
        var generation = ++_presentationGeneration;
        _inputFilter.Dispose();
        _spinner.Stop();
        RestoreFocus();
        // Focus events may pump messages and present a new operation while hiding.
        if (generation != _presentationGeneration) return;
        _savedFocus = null;
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
    private void RestoreFocus()
    {
        if (!_card.ContainsFocus || _disposing || _owner is null || !_owner.IsHandleCreated || GetForegroundWindow() != _owner.Handle) return;
        var target = _savedFocus;
        if (target is not null && !target.IsDisposed && (target == Content || Content.Contains(target)) && target.CanSelect && target.Visible && target.Enabled)
            target.Focus();
        else
            _owner.SelectNextControl(this, true, true, true, true);
    }
    private void OnContentHandleDestroyed(object? sender, EventArgs e) { _contentHandleLost = true; _controller.RefreshVisibility(); }
    private void OnContentHandleCreated(object? sender, EventArgs e) { _contentHandleLost = false; _controller.RefreshVisibility(); }
    private void OnContentDisposed(object? sender, EventArgs e) => _controller.RefreshVisibility();
    private void OnAncestorChanged(object? sender, EventArgs e) { TrackAncestors(); _controller.RefreshVisibility(); }
    private void OnOwnerStateChanged(object? sender, EventArgs e) => _controller.RefreshVisibility();
    private void TrackAncestors()
    {
        UntrackAncestors();
        for (var parent = Parent; parent is not null; parent = parent.Parent)
        {
            _ancestors.Add(parent);
            parent.ParentChanged += OnAncestorChanged;
        }
        _owner = FindForm();
        if (_owner is null) return;
        _owner.Resize += OnOwnerStateChanged;
        _owner.Disposed += OnOwnerStateChanged;
    }
    private void UntrackAncestors()
    {
        foreach (var parent in _ancestors) parent.ParentChanged -= OnAncestorChanged;
        _ancestors.Clear();
        if (_owner is not null)
        {
            _owner.Resize -= OnOwnerStateChanged;
            _owner.Disposed -= OnOwnerStateChanged;
            _owner = null;
        }
    }
    [DllImport("user32.dll")] private static extern IntPtr GetFocus();
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
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
