using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
internal sealed class BootstrapBusyOverlayInputFilter : IMessageFilter, IDisposable
{
    private readonly Control _content;
    private bool _installed;
    internal int Blocked { get; private set; }
    internal bool Installed => _installed;
    internal BootstrapBusyOverlayInputFilter(Control content) => _content = content;
    internal void Install()
    {
        if (_installed) return;
        Application.AddMessageFilter(this);
        _installed = true;
        // Capture loss may call consumer code and hide/dispose the host reentrantly.
        // Register first so that cleanup can remove this exact registration.
        if (!_content.IsHandleCreated || _content.IsDisposed) return;
        var capture = GetCapture();
        var root = _content.Handle;
        if (capture == root || IsChild(root, capture)) ReleaseCapture();
    }
    public void Dispose() { if (!_installed) return; Application.RemoveMessageFilter(this); _installed = false; }
    public bool PreFilterMessage(ref Message m)
    {
        if (!_installed || !_content.IsHandleCreated || _content.IsDisposed) return false;
        if (!((m.Msg >= 0x200 && m.Msg <= 0x20e) || (m.Msg >= 0xa0 && m.Msg <= 0xad))) return false;
        var root = _content.Handle; // guarded: never creates a handle
        if (m.HWnd != root && !IsChild(root, m.HWnd)) return false;
        Blocked++; return true;
    }
    [DllImport("user32.dll")] private static extern bool IsChild(IntPtr parent, IntPtr child);
    [DllImport("user32.dll")] private static extern IntPtr GetCapture();
    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
}
