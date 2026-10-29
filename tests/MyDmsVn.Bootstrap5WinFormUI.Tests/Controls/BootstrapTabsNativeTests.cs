using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapTabsNativeTests
{
    [Test]
    public void PrintClient_UsesSuppliedDcAndPreservesClipPageAndDerivedDrawOrder()
    {
        using var host = new Form { ClientSize = new Size(400, 240), StartPosition = FormStartPosition.Manual, Location = new Point(70, 70) };
        using var tabs = new ProbeTabs { Bounds = new Rectangle(20, 20, 300, 160) };
        tabs.TabPages.Add(new TabPage("Trang in"));
        tabs.DrawItem += (_, _) => tabs.Order.Add("user");
        host.Controls.Add(tabs);
        host.Show();
        Application.DoEvents();

        using var bitmap = new Bitmap(tabs.Width + 40, tabs.Height + 40);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Fuchsia);
        var hdc = graphics.GetHdc();
        try
        {
            var saved = SaveDC(hdc);
            try
            {
                SetViewportOrgEx(hdc, 17, 11, out _);
                IntersectClipRect(hdc, 17, 11, tabs.Width + 17, tabs.Height + 11);
                tabs.Order.Clear();
                SendMessage(tabs.Handle, 0x0318, hdc, (IntPtr)4);
                Assert.That(tabs.Order, Does.Contain("before"));
                Assert.That(tabs.Order, Does.Contain("user"));
                Assert.That(tabs.Order, Does.Contain("after"));
                Assert.That(tabs.Order.IndexOf("before"), Is.LessThan(tabs.Order.IndexOf("user")));
                Assert.That(tabs.Order.IndexOf("user"), Is.LessThan(tabs.Order.IndexOf("after")));
            }
            finally { RestoreDC(hdc, saved); }
        }
        finally { graphics.ReleaseHdc(hdc); }

        var header = tabs.GetTabRect(0);
        var changed = 0;
        for (var y = Math.Max(11, header.Top + 11); y < Math.Min(bitmap.Height, header.Bottom + 11); y++)
            for (var x = Math.Max(17, header.Left + 17); x < Math.Min(bitmap.Width, header.Right + 17); x++)
                if (bitmap.GetPixel(x, y).ToArgb() != Color.Fuchsia.ToArgb()) changed++;
        Assert.That(changed, Is.GreaterThan(10));
        Assert.That(bitmap.GetPixel(20, 100).ToArgb(), Is.EqualTo(Color.Fuchsia.ToArgb()));
        Assert.That(bitmap.GetPixel(bitmap.Width - 1, bitmap.Height - 1).ToArgb(), Is.EqualTo(Color.Fuchsia.ToArgb()));
    }

    [Test]
    public void DrawToBitmapAndNativeFallbackKeepOwnerDrawAndCallerProperties()
    {
        using var host = new Form { ClientSize = new Size(350, 220) };
        using var tabs = new ProbeTabs { Dock = DockStyle.Fill };
        tabs.TabPages.Add(new TabPage("Một"));
        tabs.TabPages.Add(new TabPage("Hai"));
        host.Controls.Add(tabs);
        host.Show();
        Application.DoEvents();

        foreach (var alignment in new[] { TabAlignment.Top, TabAlignment.Bottom, TabAlignment.Left, TabAlignment.Right })
        {
            tabs.Alignment = alignment;
            using var bitmap = new Bitmap(tabs.Width, tabs.Height);
            tabs.Order.Clear();
            tabs.DrawToBitmap(bitmap, tabs.ClientRectangle);
            Assert.That(tabs.Alignment, Is.EqualTo(alignment));
            Assert.That(tabs.DrawMode, Is.EqualTo(TabDrawMode.OwnerDrawFixed));
            Assert.That(tabs.Order, Does.Contain("user").Or.Contain("after").Or.Contain("before"));
        }
        tabs.Alignment = TabAlignment.Top;
        tabs.DrawMode = TabDrawMode.Normal;
        using (var bitmap = new Bitmap(tabs.Width, tabs.Height)) tabs.DrawToBitmap(bitmap, tabs.ClientRectangle);
        Assert.That(tabs.DrawMode, Is.EqualTo(TabDrawMode.Normal));
        Assert.That(tabs.TabPages.Count, Is.EqualTo(2));
    }

    [Test]
    public void CompactChrome_PaintsOnlyUnownedChromeAndPreservesPageItemsAndChild()
    {
        var snapshot = new BootstrapTabChromeSnapshot(
            new Rectangle(0, 0, 120, 80),
            new Rectangle(4, 26, 112, 50),
            new Rectangle(0, 0, 120, 26),
            new[] { new Rectangle(4, 2, 50, 24) },
            new[] { new Rectangle(95, 2, 20, 20) }, 7);
        using var bitmap = new Bitmap(120, 80);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Fuchsia);

        BootstrapTabHeaderPainter.DrawCompactChrome(graphics, snapshot,
            new Rectangle(0, 0, 120, 80), BootstrapTheme.CreateDefault(BootstrapThemeMode.Light));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(bitmap.GetPixel(70, 10).ToArgb(), Is.Not.EqualTo(Color.Fuchsia.ToArgb()));
            Assert.That(bitmap.GetPixel(10, 10).ToArgb(), Is.EqualTo(Color.Fuchsia.ToArgb()));
            Assert.That(bitmap.GetPixel(100, 10).ToArgb(), Is.EqualTo(Color.Fuchsia.ToArgb()));
            Assert.That(bitmap.GetPixel(30, 45).ToArgb(), Is.EqualTo(Color.Fuchsia.ToArgb()));
        }));
    }

    [Test]
    public void CompactChrome_RespectsDirtyClipAndEmptyTabs()
    {
        var snapshot = new BootstrapTabChromeSnapshot(
            new Rectangle(0, 0, 100, 70),
            new Rectangle(4, 24, 92, 42),
            new Rectangle(0, 0, 100, 24),
            Array.Empty<Rectangle>(), Array.Empty<Rectangle>(), 8);
        using var bitmap = new Bitmap(100, 70);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Fuchsia);

        BootstrapTabHeaderPainter.DrawCompactChrome(graphics, snapshot,
            new Rectangle(0, 0, 40, 20), BootstrapTheme.CreateDefault(BootstrapThemeMode.Light));

        Assert.That(bitmap.GetPixel(20, 10).ToArgb(), Is.Not.EqualTo(Color.Fuchsia.ToArgb()));
        Assert.That(bitmap.GetPixel(60, 10).ToArgb(), Is.EqualTo(Color.Fuchsia.ToArgb()));
        Assert.That(bitmap.GetPixel(20, 40).ToArgb(), Is.EqualTo(Color.Fuchsia.ToArgb()));
    }

    [Test]
    public void VerticalAndRtlFallback_KeepDrawingInsideHeaderAndRestoreGraphicsState()
    {
        var theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var metrics = BootstrapTabControlRenderLogic.ResolveMetrics(theme.Metrics, 96, 0);
        var palette = BootstrapTabControlRenderLogic.ResolvePalette(theme.Colors,
            BootstrapVariant.Primary, BootstrapTabStyle.Underline, true, true, false);
        using var bitmap = new Bitmap(150, 140);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Fuchsia);
        using var font = new Font("Segoe UI", 9f);
        var left = new Rectangle(10, 10, 28, 110);
        var right = new Rectangle(60, 10, 28, 110);
        var rtl = new Rectangle(95, 10, 45, 30);

        BootstrapTabHeaderPainter.DrawHeaderFallback(
            new DrawItemEventArgs(graphics, font, left, 0, DrawItemState.Selected),
            TabAlignment.Left, false, metrics, palette, "Trang dài", null,
            Rectangle.Empty, true, true, true);
        BootstrapTabHeaderPainter.DrawHeaderFallback(
            new DrawItemEventArgs(graphics, font, right, 1, DrawItemState.Selected),
            TabAlignment.Right, false, metrics, palette, "Trang dài", null,
            Rectangle.Empty, true, true, true);
        BootstrapTabHeaderPainter.DrawHeaderFallback(
            new DrawItemEventArgs(graphics, font, rtl, 2, DrawItemState.Selected),
            TabAlignment.Top, true, metrics, palette, "Tiếng Việt", null,
            Rectangle.Empty, true, true, true);

        Assert.That(graphics.Transform.IsIdentity, Is.True);
        Assert.That(bitmap.GetPixel(20, 50).ToArgb(), Is.Not.EqualTo(Color.Fuchsia.ToArgb()));
        Assert.That(bitmap.GetPixel(70, 50).ToArgb(), Is.Not.EqualTo(Color.Fuchsia.ToArgb()));
        Assert.That(bitmap.GetPixel(110, 20).ToArgb(), Is.Not.EqualTo(Color.Fuchsia.ToArgb()));
        Assert.That(bitmap.GetPixel(45, 60).ToArgb(), Is.EqualTo(Color.Fuchsia.ToArgb()));
        Assert.That(bitmap.GetPixel(145, 100).ToArgb(), Is.EqualTo(Color.Fuchsia.ToArgb()));
    }

    [Test]
    public void RtlFallback_ImageAndCaptionStayLeftOfCloseHitTarget()
    {
        var theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var metrics = BootstrapTabControlRenderLogic.ResolveMetrics(theme.Metrics, 96, 0);
        var palette = BootstrapTabControlRenderLogic.ResolvePalette(theme.Colors,
            BootstrapVariant.Primary, BootstrapTabStyle.Underline, false, true, false);
        var bounds = new Rectangle(0, 0, 160, 32);
        var close = new Rectangle(132, 6, 20, 20);
        using var icon = new Bitmap(12, 12);
        using (var iconGraphics = Graphics.FromImage(icon)) iconGraphics.Clear(Color.Blue);
        using var font = new Font("Segoe UI", 9f);
        using var withCaption = new Bitmap(160, 32);
        using var withoutCaption = new Bitmap(160, 32);
        using (var graphics = Graphics.FromImage(withCaption))
            BootstrapTabHeaderPainter.DrawHeaderFallback(
                new DrawItemEventArgs(graphics, font, bounds, 0, DrawItemState.None),
                TabAlignment.Top, true, metrics, palette, "Tiếng Việt", icon,
                close, true, false, false);
        using (var graphics = Graphics.FromImage(withoutCaption))
            BootstrapTabHeaderPainter.DrawHeaderFallback(
                new DrawItemEventArgs(graphics, font, bounds, 0, DrawItemState.None),
                TabAlignment.Top, true, metrics, palette, string.Empty, icon,
                close, true, false, false);

        var imagePixels = 0;
        var closeImagePixels = 0;
        var captionPixels = 0;
        for (var y = 0; y < bounds.Height; y++)
        for (var x = 0; x < bounds.Width; x++)
        {
            var color = withCaption.GetPixel(x, y).ToArgb();
            if (x >= 115 && x < 127 && color == Color.Blue.ToArgb()) imagePixels++;
            if (close.Contains(x, y) && color == Color.Blue.ToArgb()) closeImagePixels++;
            if (x < 110 && color != withoutCaption.GetPixel(x, y).ToArgb()) captionPixels++;
        }
        Assert.Multiple((Action)(() =>
        {
            Assert.That(imagePixels, Is.GreaterThan(0));
            Assert.That(closeImagePixels, Is.Zero);
            Assert.That(captionPixels, Is.GreaterThan(0));
        }));
    }

    [Test]
    public void LeftFallback_LongCaptionDoesNotOverwriteIcon()
    {
        var theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var metrics = BootstrapTabControlRenderLogic.ResolveMetrics(theme.Metrics, 96, 0);
        var palette = BootstrapTabControlRenderLogic.ResolvePalette(theme.Colors,
            BootstrapVariant.Primary, BootstrapTabStyle.Underline, false, true, false);
        var bounds = new Rectangle(4, 4, 32, 100);
        var iconBounds = new Rectangle(12, 7, 16, 16);
        using var icon = new Bitmap(16, 16);
        using (var brush = Graphics.FromImage(icon)) brush.Clear(Color.Blue);
        using var font = new Font("Segoe UI", 12f, FontStyle.Bold);
        using var caption = new Bitmap(40, 110);
        using var blank = new Bitmap(40, 110);
        using (var graphics = Graphics.FromImage(caption))
            BootstrapTabHeaderPainter.DrawHeaderFallback(
                new DrawItemEventArgs(graphics, font, bounds, 0, DrawItemState.None),
                TabAlignment.Left, false, metrics, palette,
                "ABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZ", icon,
                Rectangle.Empty, true, false, false);
        using (var graphics = Graphics.FromImage(blank))
            BootstrapTabHeaderPainter.DrawHeaderFallback(
                new DrawItemEventArgs(graphics, font, bounds, 0, DrawItemState.None),
                TabAlignment.Left, false, metrics, palette, string.Empty, icon,
                Rectangle.Empty, true, false, false);

        var changedIconPixels = 0;
        var captionPixels = 0;
        for (var y = bounds.Top; y < bounds.Bottom; y++)
        for (var x = bounds.Left; x < bounds.Right; x++)
        {
            if (caption.GetPixel(x, y).ToArgb() == blank.GetPixel(x, y).ToArgb()) continue;
            if (iconBounds.Contains(x, y)) changedIconPixels++;
            else captionPixels++;
        }
        Assert.That(captionPixels, Is.GreaterThan(0), "The long caption must actually be rendered.");
        Assert.That(changedIconPixels, Is.Zero, "Rotated Left text must not enter the icon rectangle.");
    }

    [TestCase(false, true)]
    [TestCase(false, false)]
    [TestCase(true, true)]
    [TestCase(true, false)]
    public void Fallback_NarrowHeaderClipsLargeIconBeforeClose(bool rightToLeft, bool enabled)
    {
        var theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var metrics = BootstrapTabControlRenderLogic.ResolveMetrics(theme.Metrics, 96, 0);
        var palette = BootstrapTabControlRenderLogic.ResolvePalette(theme.Colors,
            BootstrapVariant.Primary, BootstrapTabStyle.Underline, false, enabled, false);
        var bounds = new Rectangle(0, 0, 60, 32);
        var close = new Rectangle(40, 6, 16, 20);
        using var icon = new Bitmap(48, 24);
        using (var graphics = Graphics.FromImage(icon)) graphics.Clear(Color.Blue);
        using var font = new Font("Segoe UI", 9f);
        using var withIcon = new Bitmap(60, 32);
        using var withoutIcon = new Bitmap(60, 32);
        using (var graphics = Graphics.FromImage(withIcon))
            BootstrapTabHeaderPainter.DrawHeaderFallback(
                new DrawItemEventArgs(graphics, font, bounds, 0, DrawItemState.None),
                TabAlignment.Bottom, rightToLeft, metrics, palette, string.Empty, icon,
                close, enabled, false, false);
        using (var graphics = Graphics.FromImage(withoutIcon))
            BootstrapTabHeaderPainter.DrawHeaderFallback(
                new DrawItemEventArgs(graphics, font, bounds, 0, DrawItemState.None),
                TabAlignment.Bottom, rightToLeft, metrics, palette, string.Empty, null,
                close, enabled, false, false);

        var iconPixels = 0;
        var closeDifferences = 0;
        for (var y = 0; y < bounds.Height; y++)
        for (var x = 0; x < bounds.Width; x++)
        {
            if (withIcon.GetPixel(x, y).ToArgb() == withoutIcon.GetPixel(x, y).ToArgb()) continue;
            if (close.Contains(x, y)) closeDifferences++;
            else iconPixels++;
        }
        Assert.That(iconPixels, Is.GreaterThan(0), "Image must remain visible in available header content.");
        Assert.That(closeDifferences, Is.Zero, "Image must not alter any close target pixel.");
    }

    private sealed class ProbeTabs : BootstrapTabs
    {
        internal System.Collections.Generic.List<string> Order { get; } = new System.Collections.Generic.List<string>();
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            Order.Add("before");
            base.OnDrawItem(e);
            Order.Add("after");
            using var brush = new SolidBrush(Color.Magenta);
            e.Graphics.FillRectangle(brush, e.Bounds.Left + 2, e.Bounds.Top + 2, 3, 3);
        }
    }

    [DllImport("user32.dll", EntryPoint = "SendMessageW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);
    [DllImport("gdi32.dll", ExactSpelling = true)]
    private static extern int SaveDC(IntPtr hdc);
    [DllImport("gdi32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RestoreDC(IntPtr hdc, int saved);
    [DllImport("gdi32.dll", ExactSpelling = true)]
    private static extern int IntersectClipRect(IntPtr hdc, int left, int top, int right, int bottom);
    [DllImport("gdi32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetViewportOrgEx(IntPtr hdc, int x, int y, out Point previous);
}
