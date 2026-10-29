using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;
[TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
public sealed class BootstrapBusyOverlayNativeTests
{
    [TestCase(0), TestCase(.35), TestCase(1)]
    public void ZeroOpacity_BlocksQueuedContentMouseButAllowsCancelAndOutsideSibling(double opacity)
    {
        TestContext.WriteLine($"BUS-03 queued input process bits={IntPtr.Size * 8}");
        using var form = new Form { ClientSize = new Size(700, 400) };
        var content = new MouseProbe();
        using var host = new BootstrapBusyOverlayHost(content) { Bounds = new Rectangle(0, 0, 500, 350), BackdropOpacity = opacity };
        using var outside = new MouseProbe { Bounds = new Rectangle(550, 0, 100, 100) };
        form.Controls.AddRange(new Control[] { host, outside }); form.Show();
        var cancels = 0;
        using var token = host.Begin(new BusyPresentation("Busy", () => cancels++));
        var until = DateTime.UtcNow.AddSeconds(3);
        while (host.RenderMode == BootstrapBusyOverlayRenderMode.None && DateTime.UtcNow < until) { Application.DoEvents(); Thread.Sleep(5); }
        Assert.That(host.RenderMode, Is.Not.EqualTo(BootstrapBusyOverlayRenderMode.None));
        PostMessage(content.Handle, 0x202, IntPtr.Zero, IntPtr.Zero);
        PostMessage(outside.Handle, 0x202, IntPtr.Zero, IntPtr.Zero);
        Application.DoEvents();
        Assert.That(content.MouseUps, Is.Zero, "Content must reject queued mouse even at alpha zero");
        Assert.That(outside.MouseUps, Is.EqualTo(1));
        foreach (Control child in host.Controls) foreach (Control cardChild in child.Controls) if (cardChild is Button button) button.PerformClick();
        Assert.That(cancels, Is.EqualTo(1));
        token.Dispose();
        PostMessage(content.Handle, 0x202, IntPtr.Zero, IntPtr.Zero); Application.DoEvents();
        Assert.That(content.MouseUps, Is.EqualTo(1), "Hide must remove the filter");
    }
    private sealed class MouseProbe : Panel { internal int MouseUps; protected override void OnMouseUp(MouseEventArgs e) { MouseUps++; base.OnMouseUp(e); } }
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);
}
