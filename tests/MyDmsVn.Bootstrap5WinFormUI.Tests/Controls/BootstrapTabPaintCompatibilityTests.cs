using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapTabPaintCompatibilityTests
{
    private BootstrapTheme? _originalTheme;

    [SetUp]
    public void SaveTheme() => _originalTheme = BootstrapThemeManager.CurrentTheme;

    [TearDown]
    public void RestoreTheme() => BootstrapThemeManager.CurrentTheme = _originalTheme!;

    [Test]
    public void DerivedOnDrawItem_AndUserHandlers_PreserveOrderingGraphicsAndOutput()
    {
        using var tabs = new DrawProbeTabs();
        tabs.TabPages.Add(new TabPage("Trang thử"));
        using var bitmap = new Bitmap(140, 40);
        using var graphics = Graphics.FromImage(bitmap);
        tabs.DrawItem += (_, args) =>
        {
            tabs.Order.Add("user");
            Assert.That(args.Graphics, Is.SameAs(graphics));
            Assert.That(args.Index, Is.Zero);
            Assert.That(args.Bounds, Is.EqualTo(new Rectangle(5, 3, 120, 32)));
        };

        tabs.DrawForTest(new DrawItemEventArgs(graphics, tabs.Font, new Rectangle(5, 3, 120, 32), 0, DrawItemState.None));

        Assert.That(tabs.Order, Is.EqualTo(new[] { "before", "user", "after" }));
        Assert.That(bitmap.GetPixel(8, 7).ToArgb(), Is.EqualTo(Color.Magenta.ToArgb()));
    }

    [Test]
    public void PointerClose_CallbackMayRemovePageSynchronouslyWithoutSelectingIt()
    {
        using var host = new Form { ClientSize = new Size(500, 230) };
        using var tabs = new BootstrapTabControl { Dock = DockStyle.Fill };
        var first = new TabPage("Đóng ngay");
        var second = new TabPage("Giữ chọn");
        tabs.TabPages.Add(first);
        tabs.TabPages.Add(second);
        tabs.SetTabClosable(first, true);
        tabs.SelectedTab = second;
        host.Controls.Add(tabs);
        host.Show();
        var requests = 0;
        tabs.TabCloseRequested += (_, args) =>
        {
            requests++;
            tabs.TabPages.Remove(args.Page);
        };
        var rect = tabs.GetTabRect(0);
        var point = new Point(rect.Right - 13, rect.Top + rect.Height / 2);
        var location = (IntPtr)((point.Y << 16) | (point.X & 0xffff));

        SendMessage(tabs.Handle, 0x0201, (IntPtr)1, location);
        SendMessage(tabs.Handle, 0x0202, IntPtr.Zero, location);

        Assert.That(requests, Is.EqualTo(1));
        Assert.That(tabs.SelectedTab, Is.SameAs(second));
        Assert.That(tabs.TabPages.Contains(first), Is.False);
        Assert.That(first.IsDisposed, Is.False);
        first.Dispose();
    }

    [Test]
    public void CloseController_RejectsDisabledAndUnownedPagesAndPreservesConsumerMenu()
    {
        using var consumerMenu = new ContextMenuStrip();
        var tabs = new BootstrapTabControl { ContextMenuStrip = consumerMenu };
        var owned = new TabPage("Owned");
        using var unowned = new TabPage("Other");
        tabs.TabPages.Add(owned);
        tabs.SetTabClosable(owned, true);
        Assert.Throws<ArgumentException>((Action)(() => tabs.SetTabClosable(unowned, true)));
        owned.Enabled = false;
        Assert.That(tabs.RequestTabClose(owned), Is.False);
        Assert.That(tabs.RequestTabClose(unowned), Is.False);

        tabs.Dispose();

        Assert.That(consumerMenu.IsDisposed, Is.False);
    }

    [Test]
    public void RemovingPageClearsStaleCloseMenuTarget()
    {
        using var tabs = new BootstrapTabControl();
        var first = new TabPage("Xóa ngay");
        var second = new TabPage("Giữ lại");
        tabs.TabPages.Add(first);
        tabs.TabPages.Add(second);
        tabs.SetTabClosable(first, true);
        tabs.SelectedTab = first;
        var menu = tabs.ContextMenuStrip!;
        var command = (ToolStripMenuItem)menu.Items[0];
        Assert.That(command.Enabled, Is.True);
        var requests = 0;
        tabs.TabCloseRequested += (_, _) => requests++;

        tabs.TabPages.Remove(first);
        command.PerformClick();

        Assert.That(command.Enabled, Is.False);
        Assert.That(command.AccessibleName, Is.EqualTo("Close tab Giữ lại"));
        Assert.That(requests, Is.Zero);
        first.Dispose();
    }

    [TestCase(BootstrapTabStyle.Tabs, BootstrapVariant.Primary, "Tiếng Việt", false, true)]
    [TestCase(BootstrapTabStyle.Pills, BootstrapVariant.Success, "", true, true)]
    [TestCase(BootstrapTabStyle.Underline, BootstrapVariant.Danger, "Tiêu đề tiếng Việt rất dài cần ellipsis", true, true)]
    [TestCase(BootstrapTabStyle.Tabs, BootstrapVariant.Info, "Có ảnh", true, false)]
    public void CaptionLayoutKeepsTextAndImageInsideAvailableContent(
        BootstrapTabStyle style, BootstrapVariant variant, string caption, bool withImage, bool closable)
    {
        var metrics = BootstrapTabControlRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 96, -1);
        var header = new Rectangle(7, 4, 160, 32);
        var close = closable ? new Rectangle(143, 12, 16, 16) : Rectangle.Empty;
        var imageSize = withImage ? new Size(16, 16) : Size.Empty;
        using var font = new Font("Segoe UI", 9f);
        var textWidth = string.IsNullOrEmpty(caption) ? 0 : TextRenderer.MeasureText(
            caption, font, new Size(int.MaxValue, 32),
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding).Width;
        var layout = BootstrapTabControlRenderLogic.CalculateLayout(
            header, style, metrics, textWidth, imageSize, withImage, close);
        var colors = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light).Colors;
        var palette = BootstrapTabControlRenderLogic.ResolvePalette(
            colors,
            variant, style, selected: true, enabled: true, hovered: false);

        Assert.That(layout.ContentBounds.Right, Is.LessThanOrEqualTo(closable ? close.Left - 4 : header.Right));
        Assert.That(layout.TextBounds.Left, Is.GreaterThanOrEqualTo(layout.ContentBounds.Left));
        Assert.That(layout.TextBounds.Right, Is.LessThanOrEqualTo(layout.ContentBounds.Right));
        if (withImage)
        {
            Assert.That(layout.ImageBounds.Left, Is.GreaterThanOrEqualTo(layout.ContentBounds.Left));
            Assert.That(layout.ImageBounds.Right, Is.LessThanOrEqualTo(layout.ContentBounds.Right));
        }
        var expectedAccent = variant switch
        {
            BootstrapVariant.Primary => colors.Primary,
            BootstrapVariant.Success => colors.Success,
            BootstrapVariant.Danger => colors.Danger,
            BootstrapVariant.Info => colors.Info,
            _ => throw new InvalidOperationException()
        };
        Assert.That(palette.Accent, Is.EqualTo(expectedAccent));
    }

    [TestCase(BootstrapThemeMode.Light)]
    [TestCase(BootstrapThemeMode.Dark)]
    public void ClosableCaption_IsCenteredInRemainingContent(BootstrapThemeMode mode)
    {
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode);
        using var host = new Form { ClientSize = new Size(360, 180) };
        using var tabs = new CaptionDrawTabs { Bounds = new Rectangle(0, 0, 320, 150), Fill = true, TabStyle = BootstrapTabStyle.Underline };
        tabs.TabPages.Add(new TabPage("X"));
        tabs.TabPages.Add(new TabPage("Other"));
        tabs.SetTabClosable(tabs.TabPages[0], true);
        host.Controls.Add(tabs);
        host.Show();
        var header = tabs.GetTabRect(0);
        var close = tabs.CloseBounds(0);
        using var bitmap = new Bitmap(tabs.Width, tabs.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(BootstrapThemeManager.CurrentTheme.Colors.Surface);

        tabs.DrawForTest(new DrawItemEventArgs(graphics, tabs.Font, header, 0, DrawItemState.Selected));

        var background = BootstrapThemeManager.CurrentTheme.Colors.Surface;
        var metrics = BootstrapTabControlRenderLogic.ResolveMetrics(BootstrapThemeManager.CurrentTheme.Metrics, tabs.DeviceDpi, -1);
        var content = BootstrapTabControlRenderLogic.CalculateLayout(header, tabs.TabStyle, metrics, 0, Size.Empty, false, close).ContentBounds;
        var minX = bitmap.Width;
        var maxX = -1;
        for (var y = header.Top + 3; y < header.Bottom - 5; y++)
        for (var x = content.Left; x < content.Right; x++)
        {
            var pixel = bitmap.GetPixel(x, y);
            // ClearType may produce only subpixel coverage for a thin glyph on Dark.
            // Detect painted pixels against the known surface, rather than requiring solid foreground.
            if (pixel.ToArgb() == background.ToArgb()) continue;
            minX = Math.Min(minX, x);
            maxX = Math.Max(maxX, x);
        }

        Assert.That(maxX, Is.GreaterThan(minX), "Caption pixels must be visible.");
        var expectedCenter = (content.Left + content.Right) / 2d;
        Assert.That((minX + maxX) / 2d, Is.EqualTo(expectedCenter).Within(2),
            $"header={header}, close={close}, ink={minX}..{maxX}");
    }

    [TestCase(BootstrapThemeMode.Light, false)]
    [TestCase(BootstrapThemeMode.Light, true)]
    [TestCase(BootstrapThemeMode.Dark, false)]
    [TestCase(BootstrapThemeMode.Dark, true)]
    public void ItalicCaption_PreservesOverhangingInkWithinHeader(BootstrapThemeMode mode, bool selected)
    {
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode);
        using var font = new Font("Times New Roman", 16f, FontStyle.Italic);
        using var tabs = new CaptionDrawTabs { Font = font, TabStyle = BootstrapTabStyle.Underline };
        const string caption = "f";
        const TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
        tabs.TabPages.Add(new TabPage(caption));
        tabs.SelectedIndex = selected ? 0 : -1;
        var bounds = new Rectangle(0, 0, 120, 40);
        var metrics = BootstrapTabControlRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 96, -1);
        var layout = BootstrapTabControlRenderLogic.CalculateLayout(bounds, BootstrapTabStyle.Underline,
            metrics, 0, Size.Empty, false);
        var theme = BootstrapThemeManager.CurrentTheme;
        var palette = BootstrapTabControlRenderLogic.ResolvePalette(theme.Colors, tabs.Variant, tabs.TabStyle,
            selected, enabled: true, hovered: false);
        var background = palette.Background;
        var foreground = palette.Foreground;
        Assert.That(tabs.SelectedIndex, Is.EqualTo(selected ? 0 : -1));

        using var actual = new Bitmap(bounds.Width, bounds.Height);
        using (var graphics = Graphics.FromImage(actual))
        {
            graphics.Clear(background);
            tabs.DrawForTest(new DrawItemEventArgs(graphics, font, bounds, 0, DrawItemState.None));
        }

        using var reference = new Bitmap(bounds.Width, bounds.Height);
        using (var graphics = Graphics.FromImage(reference))
        {
            graphics.Clear(background);
            TextRenderer.DrawText(graphics, caption, font, layout.ContentBounds, foreground, flags);
        }

        var actualInk = CountForegroundPixels(actual, layout.ContentBounds, foreground, background);
        var referenceInk = CountForegroundPixels(reference, layout.ContentBounds, foreground, background);
        TestContext.WriteLine($"actual={actualInk}, reference={referenceInk}, content={layout.ContentBounds}, selected={selected}, theme={theme.Mode}");
        Assert.That(referenceInk, Is.GreaterThan(0), "The reference must contain observable caption ink.");
        Assert.That(actualInk, Is.GreaterThanOrEqualTo(referenceInk - 1),
            "The native tab painter cropped italic caption ink compared with a generously bounded rendering.");
    }

    [Test]
    public void PaddedItalicCaption_ClearsFocusRingAt125Percent()
    {
        using var font = new Font("Times New Roman", 16f, FontStyle.Italic);
        var metrics = BootstrapTabControlRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 120, -1);
        var header = new Rectangle(0, 0, 160, 50);
        var textWidth = TextRenderer.MeasureText("f", font, new Size(int.MaxValue, header.Height),
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis).Width;
        var layout = BootstrapTabControlRenderLogic.CalculateLayout(
            header, BootstrapTabStyle.Tabs, metrics, textWidth, Size.Empty, false);

        Assert.That(layout.TextBounds.Left, Is.GreaterThan(layout.FocusBounds.Left + metrics.FocusBorderWidth));
        Assert.That(layout.TextBounds.Right, Is.LessThan(layout.FocusBounds.Right - metrics.FocusBorderWidth));
    }

    private static int CountForegroundPixels(Bitmap bitmap, Rectangle bounds, Color foreground, Color background)
    {
        var count = 0;
        for (var y = bounds.Top; y < bounds.Bottom; y++)
        for (var x = bounds.Left; x < bounds.Right; x++)
        {
            var pixel = bitmap.GetPixel(x, y);
            var toForeground = ColorDistanceSquared(pixel, foreground);
            var toBackground = ColorDistanceSquared(pixel, background);
            if (toForeground < toBackground) count++;
        }
        return count;
    }

    private static int ColorDistanceSquared(Color left, Color right)
    {
        var red = left.R - right.R;
        var green = left.G - right.G;
        var blue = left.B - right.B;
        return red * red + green * green + blue * blue;
    }

    private sealed class CaptionDrawTabs : BootstrapTabControl
    {
        internal void DrawForTest(DrawItemEventArgs args) => OnDrawItem(args);
        internal Rectangle CloseBounds(int index) => GetTabCloseButtonBounds(index);
    }

    private sealed class DrawProbeTabs : BootstrapTabControl
    {
        internal List<string> Order { get; } = new();

        internal void DrawForTest(DrawItemEventArgs args) => OnDrawItem(args);
        internal Rectangle CloseBounds(int index) => GetTabCloseButtonBounds(index);

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            Order.Add("before");
            base.OnDrawItem(e);
            using var brush = new SolidBrush(Color.Magenta);
            e.Graphics.FillRectangle(brush, new Rectangle(e.Bounds.Left + 1, e.Bounds.Top + 1, 10, 10));
            Order.Add("after");
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam);
}
