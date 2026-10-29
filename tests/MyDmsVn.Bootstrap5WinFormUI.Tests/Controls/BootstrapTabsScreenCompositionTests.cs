using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapTabsScreenCompositionTests
{
    [Test]
    public void FullDirtyScreenPaintPreservesSelectedPagePixels()
    {
        using var host = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(72, 72),
            ClientSize = new Size(420, 260),
            TopMost = true
        };
        using var tabs = new BootstrapTabs { Bounds = new Rectangle(18, 18, 360, 190) };
        var page = new TabPage("Trang màu") { BackColor = Color.Cyan, UseVisualStyleBackColor = false };
        tabs.TabPages.Add(page);
        host.Controls.Add(tabs);
        host.Show();
        host.Activate();
        Application.DoEvents();

        var original = SamplePage(page);
        Assert.That(original, Is.Not.EqualTo(-1));

        InvalidateRect(tabs.Handle, IntPtr.Zero, false);
        UpdateWindow(tabs.Handle);
        Application.DoEvents();
        Assert.That(SamplePage(page), Is.EqualTo(original));
    }

    private static int SamplePage(Control page)
    {
        var dc = GetDC(page.Handle);
        try { return GetPixel(dc, 20, 30); }
        finally { ReleaseDC(page.Handle, dc); }
    }

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool InvalidateRect(IntPtr window, IntPtr rectangle, [MarshalAs(UnmanagedType.Bool)] bool erase);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UpdateWindow(IntPtr window);

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern IntPtr GetDC(IntPtr window);

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern int ReleaseDC(IntPtr window, IntPtr dc);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    private static extern int GetPixel(IntPtr dc, int x, int y);
}
