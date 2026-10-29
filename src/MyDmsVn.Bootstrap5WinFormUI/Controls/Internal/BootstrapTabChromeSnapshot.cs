using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

/// <summary>Geometry captured for one native paint pass; no HWND is retained.</summary>
internal sealed class BootstrapTabChromeSnapshot
{
    internal BootstrapTabChromeSnapshot(Rectangle clientBounds, Rectangle pageDisplayBounds,
        Rectangle headerPaintBounds, Rectangle[] headerItemBounds,
        Rectangle[] nativeChildExclusions, long layoutGeneration)
    {
        ClientBounds = clientBounds;
        PageDisplayBounds = pageDisplayBounds;
        HeaderPaintBounds = headerPaintBounds;
        HeaderItemBounds = headerItemBounds;
        NativeChildExclusions = nativeChildExclusions;
        LayoutGeneration = layoutGeneration;
    }

    internal Rectangle ClientBounds { get; }
    internal Rectangle PageDisplayBounds { get; }
    internal Rectangle HeaderPaintBounds { get; }
    internal IReadOnlyList<Rectangle> HeaderItemBounds { get; }
    internal IReadOnlyList<Rectangle> NativeChildExclusions { get; }
    internal long LayoutGeneration { get; }

    internal static BootstrapTabChromeSnapshot Capture(TabControl owner,
        IReadOnlyDictionary<int, Rectangle> actualDrawBounds, long layoutGeneration)
    {
        if (owner is null) throw new ArgumentNullException(nameof(owner));
        if (actualDrawBounds is null) throw new ArgumentNullException(nameof(actualDrawBounds));
        var client = owner.ClientRectangle;
        var page = Rectangle.Intersect(client, owner.DisplayRectangle);
        // Top compact mode only. The facade excludes other native layouts.
        var headerBottom = page.IsEmpty ? client.Bottom : Math.Max(client.Top, Math.Min(client.Bottom, page.Top));
        var header = Rectangle.FromLTRB(client.Left, client.Top, client.Right, headerBottom);
        var items = new Rectangle[owner.TabCount];
        for (var index = 0; index < items.Length; index++)
        {
            if (actualDrawBounds.TryGetValue(index, out var bounds))
            {
                items[index] = bounds;
                continue;
            }
            bounds = owner.GetTabRect(index);
            if (index == owner.SelectedIndex)
            {
                // Native selected DrawItem.Bounds extends GetTabRect by two pixels at 96 DPI.
                // If a clipped pass does not dispatch that item, keep chrome off its overhang.
                var overhang = Math.Max(2, (owner.DeviceDpi + 47) / 48);
                bounds.Inflate(overhang, overhang);
            }
            items[index] = bounds;
        }
        return new BootstrapTabChromeSnapshot(client, page, header, items, CaptureChildBounds(owner), layoutGeneration);
    }

    private static Rectangle[] CaptureChildBounds(TabControl owner)
    {
        if (!owner.IsHandleCreated) return Array.Empty<Rectangle>();
        var bounds = new List<Rectangle>();
        for (var child = GetWindow(owner.Handle, 5); child != IntPtr.Zero; child = GetWindow(child, 2))
        {
            if (!IsWindowVisible(child) || !GetWindowRect(child, out var native)) continue;
            var topLeft = owner.PointToClient(new Point(native.Left, native.Top));
            var bottomRight = owner.PointToClient(new Point(native.Right, native.Bottom));
            var rectangle = Rectangle.Intersect(owner.ClientRectangle,
                Rectangle.FromLTRB(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y));
            if (!rectangle.IsEmpty) bounds.Add(rectangle);
        }
        return bounds.ToArray();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { internal int Left, Top, Right, Bottom; }

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern IntPtr GetWindow(IntPtr hWnd, uint command);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect rect);
}
