using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal delegate void BootstrapTabMessageDispatch(ref Message message);

/// <summary>Owns the HDCs used for one native tab paint pass.</summary>
internal sealed class BootstrapTabNativePaint : IDisposable
{
    private const int WmPaint = 0x000f;
    private const int WmPrint = 0x0317;
    private const int WmPrintClient = 0x0318;
    private const int PrfClient = 0x0004;
    private const int Srccopy = 0x00cc0020;
    private readonly TabControl _owner;
    private int _compositionDepth;
    private bool _disposed;

    internal BootstrapTabNativePaint(TabControl owner) => _owner = owner ?? throw new ArgumentNullException(nameof(owner));

    internal bool IsComposing => _compositionDepth > 0;

    /// <summary>Returns false for unrelated or nested messages so the caller dispatches them normally.</summary>
    internal bool TryPaint(ref Message message, BootstrapTabMessageDispatch dispatch,
        Action<Graphics, Rectangle> completeChrome)
    {
        if (dispatch is null) throw new ArgumentNullException(nameof(dispatch));
        if (completeChrome is null) throw new ArgumentNullException(nameof(completeChrome));
        if (_disposed || IsComposing) return false;
        if (message.Msg != WmPaint && message.Msg != WmPrint && message.Msg != WmPrintClient) return false;

        if (message.Msg == WmPaint && message.WParam == IntPtr.Zero)
            return PaintScreen(ref message, dispatch, completeChrome);

        _compositionDepth++;
        try
        {
            // Native WM_PRINT can synchronously send WM_PRINTCLIENT. The nested call goes
            // through the facade's base dispatch; only this outer pass completes chrome.
            dispatch(ref message);
            var drawsClient = message.Msg != WmPrint || (message.LParam.ToInt64() & PrfClient) != 0;
            if (drawsClient && message.WParam != IntPtr.Zero)
            {
                using var graphics = Graphics.FromHdc(message.WParam);
                completeChrome(graphics, _owner.ClientRectangle);
            }
            return true;
        }
        finally { _compositionDepth--; }
    }

    private bool PaintScreen(ref Message message, BootstrapTabMessageDispatch dispatch,
        Action<Graphics, Rectangle> completeChrome)
    {
        var paint = new NativePaintStruct { Reserved = new byte[32] };
        var screenDc = BeginPaint(_owner.Handle, ref paint);
        if (screenDc == IntPtr.Zero) return false;
        try
        {
            var dirty = Rectangle.Intersect(_owner.ClientRectangle,
                Rectangle.FromLTRB(paint.Paint.Left, paint.Paint.Top, paint.Paint.Right, paint.Paint.Bottom));
            if (dirty.IsEmpty)
            {
                message.Result = IntPtr.Zero;
                return true;
            }

            var width = Math.Max(1, _owner.ClientSize.Width);
            var height = Math.Max(1, _owner.ClientSize.Height);
            var memoryDc = CreateCompatibleDC(screenDc);
            if (memoryDc == IntPtr.Zero)
            {
                DispatchToScreen(ref message, screenDc, dispatch);
                return true;
            }
            try
            {
                var bitmap = CreateCompatibleBitmap(screenDc, width, height);
                if (bitmap == IntPtr.Zero)
                {
                    DispatchToScreen(ref message, screenDc, dispatch);
                    return true;
                }
                try
                {
                    var oldBitmap = SelectObject(memoryDc, bitmap);
                    if (oldBitmap == IntPtr.Zero || oldBitmap == new IntPtr(-1))
                    {
                        DispatchToScreen(ref message, screenDc, dispatch);
                        return true;
                    }
                    try
                    {
                        // Native dispatch can leave page/child pixels untouched. Preserve the
                        // current client pixels before copying the complete dirty rect back.
                        if (!BitBlt(memoryDc, dirty.Left, dirty.Top, dirty.Width, dirty.Height,
                            screenDc, dirty.Left, dirty.Top, Srccopy))
                        {
                            DispatchToScreen(ref message, screenDc, dispatch);
                            return true;
                        }
                        var saved = SaveDC(memoryDc);
                        try
                        {
                            IntersectClipRect(memoryDc, dirty.Left, dirty.Top, dirty.Right, dirty.Bottom);
                            var supplied = Message.Create(_owner.Handle, WmPaint, memoryDc, IntPtr.Zero);
                            _compositionDepth++;
                            try
                            {
                                dispatch(ref supplied);
                                using var graphics = Graphics.FromHdc(memoryDc);
                                completeChrome(graphics, dirty);
                            }
                            finally { _compositionDepth--; }
                            message.Result = supplied.Result;
                            BitBlt(screenDc, dirty.Left, dirty.Top, dirty.Width, dirty.Height,
                                memoryDc, dirty.Left, dirty.Top, Srccopy);
                        }
                        finally { if (saved != 0) RestoreDC(memoryDc, saved); }
                    }
                    finally { SelectObject(memoryDc, oldBitmap); }
                }
                finally { DeleteObject(bitmap); }
            }
            finally { DeleteDC(memoryDc); }
            return true;
        }
        finally { EndPaint(_owner.Handle, ref paint); }
    }

    private void DispatchToScreen(ref Message message, IntPtr screenDc, BootstrapTabMessageDispatch dispatch)
    {
        var supplied = Message.Create(_owner.Handle, WmPaint, screenDc, IntPtr.Zero);
        _compositionDepth++;
        try { dispatch(ref supplied); }
        finally { _compositionDepth--; }
        message.Result = supplied.Result;
    }

    public void Dispose() => _disposed = true;

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { internal int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePaintStruct
    {
        internal IntPtr Hdc;
        [MarshalAs(UnmanagedType.Bool)] internal bool Erase;
        internal NativeRect Paint;
        [MarshalAs(UnmanagedType.Bool)] internal bool Restore;
        [MarshalAs(UnmanagedType.Bool)] internal bool IncUpdate;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] internal byte[] Reserved;
    }

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern IntPtr BeginPaint(IntPtr hWnd, ref NativePaintStruct paint);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EndPaint(IntPtr hWnd, ref NativePaintStruct paint);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    private static extern IntPtr CreateCompatibleDC(IntPtr dc);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr dc, int width, int height);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    private static extern IntPtr SelectObject(IntPtr dc, IntPtr image);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr image);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteDC(IntPtr dc);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    private static extern int SaveDC(IntPtr dc);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RestoreDC(IntPtr dc, int saved);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    private static extern int IntersectClipRect(IntPtr dc, int left, int top, int right, int bottom);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BitBlt(IntPtr destination, int x, int y, int width, int height,
        IntPtr source, int sourceX, int sourceY, int operation);
}
