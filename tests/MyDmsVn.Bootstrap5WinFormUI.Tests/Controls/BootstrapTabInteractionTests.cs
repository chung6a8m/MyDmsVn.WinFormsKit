using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapTabInteractionTests
{
    private const int WmMouseMove = 0x0200;
    private const int TcsHotTrack = 0x0040;
    private const int GwlStyle = -16;

    [Test]
    public void CustomHeaders_HotTrackTrue_DoesNotEnableNativeHotTrackStyle()
    {
        using var form = new Form { ClientSize = new Size(420, 230) };
        using var tabs = new BootstrapTabControl { Dock = DockStyle.Fill, HotTrack = true };
        tabs.TabPages.Add("One");
        tabs.TabPages.Add("Two");
        form.Controls.Add(tabs);
        form.Show();

        Assert.That(GetWindowLong(tabs.Handle, GwlStyle) & TcsHotTrack, Is.Zero);
        Assert.That(tabs.HotTrack, Is.True);
    }

    [Test]
    public void NormalDrawMode_RetainsNativeHotTrackStyle()
    {
        using var form = new Form { ClientSize = new Size(420, 230) };
        using var tabs = new BootstrapTabControl { Dock = DockStyle.Fill, HotTrack = true, DrawMode = TabDrawMode.Normal };
        tabs.TabPages.Add("One");
        form.Controls.Add(tabs);
        form.Show();

        Assert.That(GetWindowLong(tabs.Handle, GwlStyle) & TcsHotTrack, Is.EqualTo(TcsHotTrack));
    }

    [Test]
    public void HotTrackFalse_DoesNotRequestCustomHoverRepaint()
    {
        using var form = new Form { ClientSize = new Size(420, 230) };
        using var tabs = new BootstrapTabControl { Dock = DockStyle.Fill, HotTrack = false };
        tabs.TabPages.Add("One");
        tabs.TabPages.Add("Two");
        form.Controls.Add(tabs);
        form.Show();
        Application.DoEvents();
        tabs.Update();

        var header = tabs.GetTabRect(1);
        var point = new Point(header.Left + header.Width / 2, header.Top + header.Height / 2);
        SendMessage(tabs.Handle, WmMouseMove, IntPtr.Zero, (IntPtr)((point.Y << 16) | (point.X & 0xffff)));

        Assert.That(GetUpdateRect(tabs.Handle, out _, false), Is.False);
    }

    [Test]
    public void HoverWithinSameHeader_DoesNotInvalidateAgain()
    {
        using var form = new Form { ClientSize = new Size(520, 230) };
        using var tabs = CreateTabs();
        form.Controls.Add(tabs);
        form.Show();
        using var interaction = new BootstrapTabInteractionController(tabs, () => true);
        var header = tabs.GetTabRect(1);
        interaction.MouseMoved(new Point(header.Left + 5, header.Top + 5));
        ValidateRect(tabs.Handle, IntPtr.Zero);

        interaction.MouseMoved(new Point(header.Right - 5, header.Top + 5));

        Assert.That(interaction.HoveredIndex, Is.EqualTo(1));
        Assert.That(GetUpdateRect(tabs.Handle, out _, false), Is.False);
    }

    [Test]
    public void HoverTransition_ChangesOnlyExpectedOldNewHeaderRegions()
    {
        using var form = new Form { ClientSize = new Size(520, 230) };
        using var tabs = CreateTabs();
        form.Controls.Add(tabs);
        form.Show();
        using var interaction = new BootstrapTabInteractionController(tabs, () => true);
        var oldHeader = tabs.GetTabRect(1);
        var nextHeader = tabs.GetTabRect(2);
        interaction.MouseMoved(Center(oldHeader));
        ValidateRect(tabs.Handle, IntPtr.Zero);

        interaction.MouseMoved(Center(nextHeader));

        Assert.That(interaction.HoveredIndex, Is.EqualTo(2));
        Assert.That(GetUpdateRect(tabs.Handle, out var dirty, false), Is.True);
        var expected = Rectangle.Union(oldHeader, nextHeader);
        Assert.That(dirty.ToRectangle(), Is.EqualTo(expected));
    }

    [Test]
    public void Hover_DoesNotChangeFontItemSizeRectsSelectionOrHandle()
    {
        using var form = new Form { ClientSize = new Size(520, 230) };
        using var tabs = CreateTabs();
        form.Controls.Add(tabs);
        form.Show();
        var font = tabs.Font;
        var itemSize = tabs.ItemSize;
        var selected = tabs.SelectedIndex;
        var handle = tabs.Handle;
        var bounds = new[] { tabs.GetTabRect(0), tabs.GetTabRect(1), tabs.GetTabRect(2) };

        SendMove(tabs, Center(bounds[1]));
        SendMove(tabs, Center(bounds[2]));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(tabs.Font, Is.SameAs(font));
            Assert.That(tabs.ItemSize, Is.EqualTo(itemSize));
            Assert.That(tabs.SelectedIndex, Is.EqualTo(selected));
            Assert.That(tabs.Handle, Is.EqualTo(handle));
            Assert.That(new[] { tabs.GetTabRect(0), tabs.GetTabRect(1), tabs.GetTabRect(2) }, Is.EqualTo(bounds));
        }));
    }

    [Test]
    public void StationaryPointer_RefreshesAfterResizePageMutationAndHandleRecreation()
    {
        using var form = new Form { ClientSize = new Size(520, 230) };
        using var tabs = new HandleProbeTabs { Dock = DockStyle.Fill, HotTrack = true };
        tabs.TabPages.Add("One");
        tabs.TabPages.Add("Two");
        tabs.TabPages.Add("Three");
        form.Controls.Add(tabs);
        form.Show();
        var previousPointer = Cursor.Position;
        try
        {
            Cursor.Position = tabs.PointToScreen(Center(tabs.GetTabRect(1)));
            SendMove(tabs, tabs.PointToClient(Cursor.Position));
            AssertHoveredMatchesPointer(tabs);

            tabs.TabPages[0].Text = "A much longer caption that changes all fixed header widths";
            AssertHoveredMatchesPointer(tabs);

            form.ClientSize = new Size(380, 230);
            AssertHoveredMatchesPointer(tabs);

            tabs.TabPages.Add("Four");
            AssertHoveredMatchesPointer(tabs);
            tabs.TabPages.RemoveAt(0);
            AssertHoveredMatchesPointer(tabs);

            tabs.RecreateNativeHandle();
            AssertHoveredMatchesPointer(tabs);
        }
        finally
        {
            Cursor.Position = previousPointer;
        }
    }

    [Test]
    public void NoEraseHeaderInvalidation_ABTrace()
    {
        using var form = new Form { ClientSize = new Size(520, 230) };
        using var tabs = new EraseProbeTabs { Dock = DockStyle.Fill, HotTrack = true };
        tabs.TabPages.Add("One");
        tabs.TabPages.Add("Two");
        form.Controls.Add(tabs);
        form.Show();
        using var interaction = new BootstrapTabInteractionController(tabs, () => true);
        ValidateRect(tabs.Handle, IntPtr.Zero);
        tabs.EraseCount = 0;

        InvalidateRect(tabs.Handle, IntPtr.Zero, true);
        GetUpdateRect(tabs.Handle, out _, true);
        var eraseRequested = tabs.EraseCount;

        ValidateRect(tabs.Handle, IntPtr.Zero);
        tabs.EraseCount = 0;
        interaction.MouseMoved(Center(tabs.GetTabRect(1)));
        GetUpdateRect(tabs.Handle, out _, true);
        var noEraseRequested = tabs.EraseCount;
        TestContext.WriteLine($"erase=true WM_ERASEBKGND={eraseRequested}; header erase=false WM_ERASEBKGND={noEraseRequested}");

        Assert.That(eraseRequested, Is.GreaterThan(0));
        Assert.That(noEraseRequested, Is.Zero);
    }

    private static void AssertHoveredMatchesPointer(BootstrapTabControl tabs)
    {
        var clientPoint = tabs.PointToClient(Cursor.Position);
        var expected = -1;
        for (var index = 0; index < tabs.TabCount; index++)
        {
            if (!tabs.GetTabRect(index).Contains(clientPoint)) continue;
            expected = index;
            break;
        }
        var field = typeof(BootstrapTabControl).GetField("_interaction", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        var interaction = (BootstrapTabInteractionController)field!.GetValue(tabs)!;
        Assert.That(interaction.HoveredIndex, Is.EqualTo(expected));
    }

    private sealed class HandleProbeTabs : BootstrapTabControl
    {
        internal void RecreateNativeHandle() => RecreateHandle();
    }

    private sealed class EraseProbeTabs : BootstrapTabControl
    {
        internal int EraseCount { get; set; }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x0014) EraseCount++;
            base.WndProc(ref message);
        }
    }

    private static BootstrapTabControl CreateTabs()
    {
        var tabs = new BootstrapTabControl { Dock = DockStyle.Fill, HotTrack = true };
        tabs.TabPages.Add("One");
        tabs.TabPages.Add("Two");
        tabs.TabPages.Add("Three");
        return tabs;
    }

    private static Point Center(Rectangle bounds) => new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);

    private static void SendMove(TabControl tabs, Point point) =>
        SendMessage(tabs.Handle, WmMouseMove, IntPtr.Zero, (IntPtr)((point.Y << 16) | (point.X & 0xffff)));

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;
        internal Rectangle ToRectangle() => Rectangle.FromLTRB(Left, Top, Right, Bottom);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr window, int index);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetUpdateRect(IntPtr window, out NativeRectangle rectangle, [MarshalAs(UnmanagedType.Bool)] bool erase);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ValidateRect(IntPtr window, IntPtr rectangle);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool InvalidateRect(IntPtr window, IntPtr rectangle, [MarshalAs(UnmanagedType.Bool)] bool erase);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);
}
