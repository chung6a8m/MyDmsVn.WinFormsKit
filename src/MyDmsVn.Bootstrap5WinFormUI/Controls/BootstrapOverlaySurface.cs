using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal sealed class BootstrapOverlaySurface : Panel
{
    private const float AntiAliasClipAllowance = 1f;

    private readonly BootstrapOverlayContentHost _contentHost;
    private BootstrapTheme _theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
    private Padding _logicalContentPadding = new Padding(12, 8, 12, 8);
    private int _logicalBorderRadius = -1;
    private int _dpi = DpiScaler.DefaultDpi;
    private int _borderWidth = 1;
    private int _radius = 6;
    private Region? _ownedRegion;
    private Region? _ownedContentHostRegion;

    public BootstrapOverlaySurface()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
        Margin = Padding.Empty;
        Padding = Padding.Empty;
        BackColor = _theme.Colors.Surface;
        _contentHost = new BootstrapOverlayContentHost
        {
            BackColor = _theme.Colors.Surface,
            Margin = Padding.Empty,
            TabStop = false,
            Visible = false
        };
        Controls.Add(_contentHost);
    }

    public Control? HostedContent { get; private set; }

    public Padding LogicalContentPadding
    {
        get => _logicalContentPadding;
        set
        {
            ValidatePadding(value);
            if (_logicalContentPadding == value)
            {
                return;
            }

            _logicalContentPadding = value;
            PerformLayout();
        }
    }

    public int LogicalBorderRadius
    {
        get => _logicalBorderRadius;
        set
        {
            if (value < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Border radius must be -1 or non-negative.");
            }

            if (_logicalBorderRadius == value)
            {
                return;
            }

            _logicalBorderRadius = value;
            ResolveMetrics();
            ReplaceRegion();
            ReplaceContentHostRegion();
            Invalidate();
        }
    }

    public void AttachContent(Control content)
    {
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (content.IsDisposed)
        {
            throw new ArgumentException("Overlay content cannot be disposed.", nameof(content));
        }

        if (HostedContent is not null && !ReferenceEquals(HostedContent, content))
        {
            throw new InvalidOperationException("Detach the existing overlay content before attaching another control.");
        }

        if (content.Parent is not null && !ReferenceEquals(content.Parent, _contentHost))
        {
            throw new InvalidOperationException("Overlay content must be unparented when attached.");
        }

        if (ReferenceEquals(HostedContent, content))
        {
            return;
        }

        HostedContent = content;
        _contentHost.AttachContent(content);
        _contentHost.Visible = true;
        PerformLayout();
    }

    public Control? DetachContent()
    {
        var content = HostedContent;
        if (content is null)
        {
            return null;
        }

        _contentHost.DetachContent(content);
        _contentHost.Visible = false;
        HostedContent = null;
        PerformLayout();
        return content;
    }

    public void ApplyTheme(BootstrapTheme theme, int dpi)
    {
        _theme = theme ?? throw new ArgumentNullException(nameof(theme));
        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi), dpi, "DPI must be positive.");
        }

        _dpi = dpi;
        BackColor = theme.Colors.Surface;
        _contentHost.BackColor = theme.Colors.Surface;
        ResolveMetrics();
        PerformLayout();
        ReplaceRegion();
        ReplaceContentHostRegion();
        Invalidate();
    }

    internal Padding GetEffectiveContentInsets(Size outerSize)
    {
        var callerPadding = DpiScaler.Scale(_logicalContentPadding, _dpi);
        if (outerSize.Width <= 0 || outerSize.Height <= 0)
        {
            return callerPadding;
        }

        var geometry = CreateGeometry(outerSize);
        return AddPadding(geometry.GetDecorationInsets(AntiAliasClipAllowance), callerPadding);
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var conservativeInsets = GetConservativeContentInsets();
        var candidate = MeasureCandidate(proposedSize, conservativeInsets);
        var conservativeCandidate = candidate;
        for (var pass = 0; pass < 4; pass++)
        {
            var insets = GetEffectiveContentInsets(candidate);
            var next = MeasureCandidate(proposedSize, insets);
            if (next == candidate)
            {
                return candidate;
            }

            candidate = next;
        }

        var finalInsets = GetEffectiveContentInsets(candidate);
        var finalContent = MeasureContent(proposedSize, finalInsets);
        if (candidate.Width - finalInsets.Horizontal >= finalContent.Width
            && candidate.Height - finalInsets.Vertical >= finalContent.Height)
        {
            return candidate;
        }

        return new Size(
            Math.Max(candidate.Width, conservativeCandidate.Width),
            Math.Max(candidate.Height, conservativeCandidate.Height));
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        var insets = GetEffectiveContentInsets(ClientSize);
        var hostInset = Math.Max(0, _borderWidth);
        var x = Math.Min(ClientSize.Width, hostInset);
        var y = Math.Min(ClientSize.Height, hostInset);
        var width = Math.Max(0, ClientSize.Width - x - hostInset);
        var height = Math.Max(0, ClientSize.Height - y - hostInset);
        _contentHost.Bounds = new Rectangle(x, y, width, height);
        _contentHost.Padding = new Padding(
            Math.Max(0, insets.Left - x),
            Math.Max(0, insets.Top - y),
            Math.Max(0, insets.Right - hostInset),
            Math.Max(0, insets.Bottom - hostInset));
        ReplaceContentHostRegion();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        ReplaceRegion();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var background = new SolidBrush(_theme.Colors.Surface);
        e.Graphics.FillRectangle(background, e.ClipRectangle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        BootstrapRoundedSurfacePainter.Paint(
            e.Graphics,
            CreateGeometry(ClientSize),
            _theme.Colors.Surface,
            _theme.Colors.Border);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _contentHost.Region = null;
            _ownedContentHostRegion?.Dispose();
            _ownedContentHostRegion = null;
            Region = null;
            _ownedRegion?.Dispose();
            _ownedRegion = null;
        }

        base.Dispose(disposing);
    }

    private void ResolveMetrics()
    {
        _borderWidth = Math.Max(0, DpiScaler.Scale(_theme.Metrics.BorderWidth, _dpi));
        var logicalRadius = _logicalBorderRadius < 0 ? _theme.Metrics.Radius : _logicalBorderRadius;
        _radius = Math.Max(0, DpiScaler.Scale(logicalRadius, _dpi));
    }

    private void ReplaceRegion()
    {
        Region? next = null;
        if (ClientSize.Width > 0 && ClientSize.Height > 0)
        {
            var geometry = CreateGeometry(ClientSize);
            using var paintPath = geometry.CreateOuterPath();
            using var envelopePath = geometry.CreateOuterEnvelopePath(AntiAliasClipAllowance);
            next = new Region(paintPath);
            if (envelopePath.PointCount > 0)
            {
                next.Union(envelopePath);
            }
            next.Intersect(ClientRectangle);
        }

        var previous = _ownedRegion;
        _ownedRegion = next;
        Region = next;
        previous?.Dispose();
    }

    private BootstrapRoundedSurfaceGeometry CreateGeometry(Size size)
    {
        return BootstrapRoundedSurfaceGeometry.Resolve(
            new RectangleF(0f, 0f, Math.Max(0, size.Width), Math.Max(0, size.Height)),
            new CornerRadius(_radius),
            _borderWidth);
    }

    private void ReplaceContentHostRegion()
    {
        Region? next = null;
        if (_contentHost.ClientSize.Width > 0 && _contentHost.ClientSize.Height > 0)
        {
            var geometry = CreateGeometry(ClientSize);
            using var path = geometry.CreateInnerPath();
            if (path.PointCount > 0)
            {
                using var transform = new Matrix();
                transform.Translate(-_contentHost.Left, -_contentHost.Top);
                path.Transform(transform);
                next = new Region(path);
            }
            else
            {
                next = new Region(Rectangle.Empty);
            }
        }

        var previous = _ownedContentHostRegion;
        _ownedContentHostRegion = next;
        _contentHost.Region = next;
        previous?.Dispose();
    }

    private static int SaturateSize(long value)
    {
        return value >= int.MaxValue ? int.MaxValue : (int)Math.Max(0L, value);
    }

    private Padding GetConservativeContentInsets()
    {
        var extent = Math.Max(1, SaturateSize(Math.Max(1L, (long)_radius * 2L)));
        var geometry = BootstrapRoundedSurfaceGeometry.Resolve(
            new RectangleF(0f, 0f, extent, extent),
            new CornerRadius(_radius),
            _borderWidth);
        return AddPadding(
            geometry.GetDecorationInsets(AntiAliasClipAllowance),
            DpiScaler.Scale(_logicalContentPadding, _dpi));
    }

    private Size MeasureCandidate(Size proposedSize, Padding insets)
    {
        var content = MeasureContent(proposedSize, insets);
        return new Size(
            SaturateSize((long)Math.Max(0, content.Width) + insets.Horizontal),
            SaturateSize((long)Math.Max(0, content.Height) + insets.Vertical));
    }

    private Size MeasureContent(Size proposedSize, Padding insets)
    {
        var contentProposal = new Size(
            proposedSize.Width > 0 ? Math.Max(0, proposedSize.Width - insets.Horizontal) : 0,
            proposedSize.Height > 0 ? Math.Max(0, proposedSize.Height - insets.Vertical) : 0);
        return HostedContent?.GetPreferredSize(contentProposal) ?? Size.Empty;
    }

    private static Padding AddPadding(Padding left, Padding right)
    {
        return new Padding(
            SaturateSize((long)left.Left + right.Left),
            SaturateSize((long)left.Top + right.Top),
            SaturateSize((long)left.Right + right.Right),
            SaturateSize((long)left.Bottom + right.Bottom));
    }

    private static void ValidatePadding(Padding padding)
    {
        if (padding.Left < 0 || padding.Top < 0 || padding.Right < 0 || padding.Bottom < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(padding), padding, "Overlay content padding cannot contain negative edges.");
        }
    }

    private sealed class BootstrapOverlayContentHost : Panel
    {
        private Control? _content;

        internal void AttachContent(Control content)
        {
            _content = content;
            Controls.Add(content);
            PerformLayout();
        }

        internal void DetachContent(Control content)
        {
            Controls.Remove(content);
            _content = null;
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (_content is null)
            {
                return;
            }

            var display = DisplayRectangle;
            _content.Bounds = new Rectangle(
                display.X,
                display.Y,
                Math.Max(0, display.Width),
                Math.Max(0, display.Height));
        }
    }
}
