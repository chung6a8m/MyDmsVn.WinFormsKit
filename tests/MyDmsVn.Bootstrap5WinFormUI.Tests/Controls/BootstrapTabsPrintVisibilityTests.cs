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
public sealed class BootstrapTabsPrintVisibilityTests
{
    private const int WmPrint = 0x0317;
    private const int PrfClientAndCheckVisible = 0x0005;

    [Test]
    public void PrintWithCheckVisibleLeavesSuppliedDcUntouchedWhenTabIsHidden()
    {
        using var host = new Form { ClientSize = new Size(350, 220) };
        using var tabs = new BootstrapTabs { Dock = DockStyle.Fill };
        tabs.TabPages.Add(new TabPage("Trang in"));
        host.Controls.Add(tabs);
        host.Show();
        Application.DoEvents();
        var handle = tabs.Handle;

        using (var visibleBitmap = PrintToBitmap(handle, tabs.ClientSize))
            Assert.That(CountChangedPixels(visibleBitmap), Is.GreaterThan(0), "Visible control must draw for flags 5.");

        tabs.Hide();
        Assert.That(tabs.IsHandleCreated, Is.True);
        Assert.That(tabs.Handle, Is.EqualTo(handle));
        using var hiddenBitmap = PrintToBitmap(handle, tabs.ClientSize);
        Assert.That(CountChangedPixels(hiddenBitmap), Is.Zero, "PRF_CHECKVISIBLE must suppress all drawing to the caller HDC.");
    }

    private static Bitmap PrintToBitmap(IntPtr handle, Size size)
    {
        var bitmap = new Bitmap(size.Width, size.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Fuchsia);
        var hdc = graphics.GetHdc();
        try { SendMessage(handle, WmPrint, hdc, (IntPtr)PrfClientAndCheckVisible); }
        finally { graphics.ReleaseHdc(hdc); }
        return bitmap;
    }

    private static int CountChangedPixels(Bitmap bitmap)
    {
        var changed = 0;
        for (var y = 0; y < bitmap.Height; y++)
            for (var x = 0; x < bitmap.Width; x++)
                if (bitmap.GetPixel(x, y).ToArgb() != Color.Fuchsia.ToArgb()) changed++;
        return changed;
    }

    [DllImport("user32.dll", EntryPoint = "SendMessageW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
