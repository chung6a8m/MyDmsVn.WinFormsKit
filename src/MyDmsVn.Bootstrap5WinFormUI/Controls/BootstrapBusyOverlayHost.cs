using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Owns one content region and displays delayed, token-owned busy presentations.</summary>
public sealed class BootstrapBusyOverlayHost : Panel
{
    private readonly int _uiThread = Thread.CurrentThread.ManagedThreadId;
    private readonly BootstrapBusyOverlayController _controller;
    private readonly Panel _backdrop;
    private readonly Panel _card;
    private readonly BootstrapSpinner _spinner;
    private readonly Label _message;
    private readonly Button _cancel;
    private double _opacity = .35;
    private bool _disposing, _handleDestroying;

    /// <summary>Creates a host and takes ownership of an unparented content control.</summary>
    public BootstrapBusyOverlayHost(Control content) : this(content, new BootstrapBusyDelay()) { }

    internal BootstrapBusyOverlayHost(Control content, IBootstrapBusyDelay delay)
    {
        if (content is null) throw new ArgumentNullException(nameof(content));
        if (content.Parent is not null) throw new ArgumentException("Content must be unparented.", nameof(content));
        if (delay is null) throw new ArgumentNullException(nameof(delay));
        Content = content;
        _backdrop = new Panel { Dock = DockStyle.Fill, Visible = false, TabStop = false };
        _card = new Panel { Visible = false, TabStop = false };
        _spinner = new BootstrapSpinner { Spinning = false, TabStop = false, AccessibleName = "Busy" };
        _message = new Label { AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, AccessibleRole = AccessibleRole.StaticText };
        _cancel = new Button { Text = "Cancel", Visible = false, AccessibleRole = AccessibleRole.PushButton };
        _cancel.Click += OnCancel;
        _card.Controls.AddRange(new Control[] { _spinner, _message, _cancel });
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
            _backdrop.Invalidate();
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
        // BUS-02 scaffold only. BUS-03 supplies DPI-aware constrained card geometry.
        var width = Math.Max(0, Math.Min(480, ClientSize.Width));
        var height = Math.Max(0, Math.Min(160, ClientSize.Height));
        _card.Bounds = new Rectangle((ClientSize.Width - width) / 2, (ClientSize.Height - height) / 2, width, height);
        _spinner.Bounds = new Rectangle(Math.Max(0, (width - 24) / 2), 8, 24, Math.Min(24, height));
        _message.Bounds = new Rectangle(0, Math.Min(40, height), width, Math.Max(0, height - 80));
        _cancel.Bounds = new Rectangle(Math.Max(0, (width - 96) / 2), Math.Max(0, height - 32), Math.Min(96, width), Math.Min(32, height));
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
        _cancel.Visible = presentation.CancelRequested is not null;
        _backdrop.Visible = true;
        _card.Visible = true;
        _backdrop.BringToFront();
        _card.BringToFront();
        _spinner.Start();
        // Explicit opaque scaffold until BUS-03 installs and verifies the native renderer.
        SetMode(BootstrapBusyOverlayRenderMode.OpaqueFallback);
        // No work after the event: its handler may hide/dispose/reenter Begin.
    }
    private void HidePresentation()
    {
        if (_card is null) return;
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
        _backdrop.BackColor = colors.SurfaceSecondary;
        _card.BackColor = colors.Surface;
        _message.ForeColor = colors.Text;
        _cancel.BackColor = colors.Surface;
        _cancel.ForeColor = colors.Text;
    }
    private void AssertUiThread()
    {
        if (Thread.CurrentThread.ManagedThreadId != _uiThread)
            throw new InvalidOperationException("Busy overlay must be managed on the owning UI thread.");
    }
}
