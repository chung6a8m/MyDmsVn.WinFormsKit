using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal sealed class BootstrapTabInteractionController : IDisposable
{
    private const int TcsHotTrack = 0x0040;
    private readonly TabControl _owner;
    private readonly Func<bool> _usesCustomHeaders;
    private Rectangle _hoveredBounds;
    private bool _disposed;

    internal BootstrapTabInteractionController(TabControl owner, Func<bool> usesCustomHeaders)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _usesCustomHeaders = usesCustomHeaders ?? throw new ArgumentNullException(nameof(usesCustomHeaders));
    }

    internal int HoveredIndex { get; private set; } = -1;

    internal int FilterNativeStyle(int nativeStyle) => _usesCustomHeaders() ? nativeStyle & ~TcsHotTrack : nativeStyle;

    internal bool IsOverflowChild(IntPtr window) =>
        window != IntPtr.Zero && _owner.IsHandleCreated &&
        FindWindowEx(_owner.Handle, IntPtr.Zero, "msctls_updown32", null) == window;

    internal void MouseMoved(Point clientPoint)
    {
        if (_disposed) return;
        var next = HitTest(clientPoint);
        var nextBounds = Bounds(next);
        if (HoveredIndex == next && _hoveredBounds == nextBounds) return;

        var oldIndex = HoveredIndex;
        var oldBounds = _hoveredBounds;
        HoveredIndex = next;
        _hoveredBounds = nextBounds;
        if (!HasHoverVisual()) return;
        InvalidateVisual(oldIndex, oldBounds);
        InvalidateVisual(next, nextBounds);
    }

    internal void MouseLeft() => MouseMoved(new Point(int.MinValue, int.MinValue));

    internal void RefreshFromPointer()
    {
        if (_disposed || !_owner.IsHandleCreated) return;
        var screenPoint = Cursor.Position;
        var pointerWindow = WindowFromPoint(screenPoint);
        if (IsOverflowChild(pointerWindow) ||
            (pointerWindow != _owner.Handle && !IsChild(_owner.Handle, pointerWindow)))
        {
            MouseLeft();
            return;
        }
        MouseMoved(_owner.PointToClient(screenPoint));
    }

    internal void Reset()
    {
        HoveredIndex = -1;
        _hoveredBounds = Rectangle.Empty;
    }

    public void Dispose()
    {
        Reset();
        _disposed = true;
    }

    private int HitTest(Point point)
    {
        for (var index = 0; index < _owner.TabCount; index++)
        {
            if (_owner.GetTabRect(index).Contains(point)) return index;
        }
        return -1;
    }

    private Rectangle Bounds(int index) => index >= 0 && index < _owner.TabCount ? _owner.GetTabRect(index) : Rectangle.Empty;

    private bool HasHoverVisual() => _usesCustomHeaders() && _owner.HotTrack && _owner.Enabled;

    private void InvalidateVisual(int index, Rectangle bounds)
    {
        if (index < 0 || index >= _owner.TabCount || bounds.IsEmpty || index == _owner.SelectedIndex ||
            !_owner.TabPages[index].Enabled || !_owner.IsHandleCreated) return;
        var clipped = Rectangle.Intersect(bounds, _owner.ClientRectangle);
        if (clipped.IsEmpty) return;
        var native = new NativeRectangle(clipped);
        InvalidateRect(_owner.Handle, ref native, false);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;

        internal NativeRectangle(Rectangle bounds)
        {
            Left = bounds.Left;
            Top = bounds.Top;
            Right = bounds.Right;
            Bottom = bounds.Bottom;
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool InvalidateRect(IntPtr window, ref NativeRectangle rectangle, [MarshalAs(UnmanagedType.Bool)] bool erase);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string className, string? windowName);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(Point point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsChild(IntPtr parent, IntPtr window);
}
