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
    public void ClosableCaption_IsCenteredInRemainingContent()
    {
        using var host = new Form { ClientSize = new Size(360, 180) };
        using var tabs = new DrawProbeTabs { Bounds = new Rectangle(0, 0, 320, 150), Fill = true, TabStyle = BootstrapTabStyle.Underline };
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

        var ink = BootstrapThemeManager.CurrentTheme.Colors.Primary;
        var minX = bitmap.Width;
        var maxX = -1;
        for (var y = header.Top + 3; y < header.Bottom - 5; y++)
        for (var x = header.Left + 8; x < close.Left - 4; x++)
        {
            var pixel = bitmap.GetPixel(x, y);
            if (Math.Abs(pixel.R - ink.R) > 32 || Math.Abs(pixel.G - ink.G) > 32 || Math.Abs(pixel.B - ink.B) > 32) continue;
            minX = Math.Min(minX, x);
            maxX = Math.Max(maxX, x);
        }

        Assert.That(maxX, Is.GreaterThan(minX), "Caption pixels must be visible.");
        var expectedCenter = (header.Left + 12 + close.Left - 4) / 2d;
        Assert.That((minX + maxX) / 2d, Is.EqualTo(expectedCenter).Within(2),
            $"header={header}, close={close}, ink={minX}..{maxX}");
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
