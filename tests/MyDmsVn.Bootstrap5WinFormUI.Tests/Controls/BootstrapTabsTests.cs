using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapTabsTests
{
    [Test]
    public void BootstrapTabs_HasDirectNativeBaseTypeAndExactDefaults()
    {
        using var tabs = new BootstrapTabs();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(typeof(BootstrapTabs).BaseType, Is.EqualTo(typeof(TabControl)));
            Assert.That(tabs.IsHandleCreated, Is.False);
            Assert.That(tabs.TabStyle, Is.EqualTo(BootstrapTabStyle.Underline));
            Assert.That(tabs.Variant, Is.EqualTo(BootstrapVariant.Primary));
            Assert.That(tabs.Fill, Is.False);
            Assert.That(tabs.BorderRadius, Is.Zero);
            Assert.That(tabs.HotTrack, Is.False);
            Assert.That(tabs.DrawMode, Is.EqualTo(TabDrawMode.OwnerDrawFixed));
            Assert.That(tabs.SizeMode, Is.EqualTo(TabSizeMode.Fixed));
            Assert.That(tabs.Alignment, Is.EqualTo(TabAlignment.Top));
            Assert.That(tabs.Multiline, Is.False);
        }));

        Assert.That(typeof(BootstrapTabs).GetCustomAttribute<DefaultEventAttribute>()?.Name,
            Is.EqualTo(nameof(TabControl.SelectedIndexChanged)));
        Assert.That(typeof(BootstrapTabs).GetProperty(nameof(BootstrapTabs.TabStyle))!
            .GetCustomAttribute<DefaultValueAttribute>()?.Value, Is.EqualTo(BootstrapTabStyle.Underline));
        Assert.That(typeof(BootstrapTabs).GetProperty(nameof(BootstrapTabs.BorderRadius))!
            .GetCustomAttribute<DefaultValueAttribute>()?.Value, Is.Zero);
    }

    [Test]
    public void NativePagesAndCallerResourcesRemainOwnedByConsumer()
    {
        using var font = new Font("Segoe UI", 11f);
        using var images = new ImageList();
        var tabs = new BootstrapTabs { Font = font, ImageList = images };
        var page = new TabPage("Trang dữ liệu");
        tabs.TabPages.Add(page);
        tabs.SetTabClosable(page, true);
        TabCloseRequestedEventArgs? request = null;
        tabs.TabCloseRequested += (_, args) => request = args;

        Assert.That(tabs.RequestTabClose(page), Is.True);
        Assert.That(request?.Page, Is.SameAs(page));
        Assert.That(tabs.TabPages.Contains(page), Is.True);
        tabs.Dispose();
        Assert.That(font.Size, Is.EqualTo(11f));
        Assert.That(images.Images.Count, Is.Zero);
    }

    [Test]
    public void PublicDeclaredSurfaceMatchesPlannedFacade()
    {
        var publicDeclared = typeof(BootstrapTabs)
            .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(member => member.MemberType is MemberTypes.Constructor or MemberTypes.Event or MemberTypes.Property ||
                member is MethodInfo method && !method.IsSpecialName)
            .Select(member => member.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.That(publicDeclared, Is.EqualTo(new[]
        {
            ".ctor", "BorderRadius", "Fill", "IsTabClosable", "RequestTabClose",
            "SetTabClosable", "TabCloseRequested", "TabStyle", "Variant"
        }));
    }

    [Test]
    public void CloseRequestRejectsUnownedAndDisabledPagesWithoutSelectingTarget()
    {
        using var tabs = new BootstrapTabs();
        using var unowned = new TabPage("Ngoài control");
        var first = new TabPage("Có thể đóng");
        var second = new TabPage("Đang chọn");
        tabs.TabPages.Add(first);
        tabs.TabPages.Add(second);
        tabs.SelectedTab = second;
        tabs.SetTabClosable(first, true);
        var requests = 0;
        tabs.TabCloseRequested += (_, _) => requests++;

        Assert.Throws<ArgumentException>((Action)(() => tabs.SetTabClosable(unowned, true)));
        Assert.That(tabs.RequestTabClose(unowned), Is.False);
        first.Enabled = false;
        Assert.That(tabs.RequestTabClose(first), Is.False);
        first.Enabled = true;
        Assert.That(tabs.RequestTabClose(first), Is.True);
        Assert.That(requests, Is.EqualTo(1));
        Assert.That(tabs.SelectedTab, Is.SameAs(second));
    }

    [Test]
    public void NativeFallbackRoundtripPreservesCallerPropertiesAndPages()
    {
        using var tabs = new BootstrapTabs();
        var page = new TabPage("Một trang");
        tabs.TabPages.Add(page);
        tabs.Alignment = TabAlignment.Bottom;
        tabs.Appearance = TabAppearance.Buttons;
        tabs.Multiline = true;
        tabs.RightToLeft = RightToLeft.Yes;
        tabs.RightToLeftLayout = true;

        Assert.That(tabs.Alignment, Is.EqualTo(TabAlignment.Bottom));
        Assert.That(tabs.Appearance, Is.EqualTo(TabAppearance.Buttons));
        Assert.That(tabs.Multiline, Is.True);
        Assert.That(tabs.RightToLeft, Is.EqualTo(RightToLeft.Yes));
        Assert.That(tabs.RightToLeftLayout, Is.True);

        tabs.Alignment = TabAlignment.Top;
        tabs.Appearance = TabAppearance.Normal;
        tabs.Multiline = false;
        tabs.RightToLeftLayout = false;
        tabs.RightToLeft = RightToLeft.No;
        Assert.That(tabs.TabPages[0], Is.SameAs(page));
        Assert.That(tabs.DrawMode, Is.EqualTo(TabDrawMode.OwnerDrawFixed));
    }

    [Test]
    public void ImageMutationResizesBeforeDrawItemWithoutChangingSizeInsidePainter()
    {
        using var host = new Form { ClientSize = new Size(350, 180) };
        using var images = new ImageList { ImageSize = new Size(16, 16) };
        using var image = new Bitmap(16, 16);
        images.Images.Add(image);
        using var tabs = new ItemSizeProbeTabs { Dock = DockStyle.Fill, ImageList = images };
        var page = new TabPage("Đo");
        tabs.TabPages.Add(page);
        host.Controls.Add(tabs);
        host.Show();
        Application.DoEvents();
        var before = tabs.ItemSize.Width;

        page.ImageIndex = 0;
        tabs.Invalidate();
        tabs.Update();

        Assert.That(tabs.ItemSize.Width, Is.GreaterThan(before));
        Assert.That(tabs.DrawCount, Is.GreaterThan(0));
        Assert.That(tabs.SizeChangedInsideDraw, Is.False);
    }

    [Test]
    public void SetTabClosableResyncsHoverAfterHeadersShiftUnderStationaryPointer()
    {
        using var host = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(75, 75),
            ClientSize = new Size(380, 180),
            TopMost = true
        };
        using var tabs = new BootstrapTabs { Dock = DockStyle.Fill, HotTrack = true };
        var first = new TabPage("Một");
        tabs.TabPages.Add(first);
        tabs.TabPages.Add(new TabPage("Hai"));
        host.Controls.Add(tabs);
        host.Show();
        Application.DoEvents();

        var second = tabs.GetTabRect(1);
        var point = new Point(second.Left + 5, second.Top + second.Height / 2);
        Cursor.Position = tabs.PointToScreen(point);
        Assert.That(WindowFromPoint(Cursor.Position), Is.EqualTo(tabs.Handle));
        SendMessage(tabs.Handle, 0x0200, IntPtr.Zero, (IntPtr)((point.Y << 16) | (point.X & 0xffff)));
        var interaction = (BootstrapTabInteractionController)typeof(BootstrapTabs)
            .GetField("_interaction", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(tabs)!;
        Assert.That(interaction.HoveredIndex, Is.EqualTo(1));

        tabs.SetTabClosable(first, true);

        Assert.That(tabs.GetTabRect(0).Contains(point), Is.True);
        Assert.That(interaction.HoveredIndex, Is.Zero);
    }

    private sealed class ItemSizeProbeTabs : BootstrapTabs
    {
        internal int DrawCount { get; private set; }
        internal bool SizeChangedInsideDraw { get; private set; }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            var before = ItemSize;
            base.OnDrawItem(e);
            SizeChangedInsideDraw |= ItemSize != before;
            DrawCount++;
        }
    }

    [DllImport("user32.dll", EntryPoint = "SendMessageW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern IntPtr WindowFromPoint(Point point);
}
