using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Compatibility;
namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
internal sealed class BootstrapBusyLayeredBackdrop : Control
{
    private readonly IBootstrapBusyLayeredWindow _native;
    private bool _layered;
    private bool _failed;
    internal int NativeError { get; private set; }
    internal BootstrapBusyLayeredBackdrop(IBootstrapBusyLayeredWindow native) { _native = native; SetStyle(ControlStyles.Opaque, true); TabStop = false; }
    internal BootstrapBusyOverlayRenderMode Configure(byte alpha, Color tint, Color fallback)
    {
        var layered = !_failed && _native.IsSupported;
        if (_layered != layered) { _layered = layered; if (IsHandleCreated) RecreateHandle(); }
        BackColor = Color.FromArgb(255, layered ? tint : fallback);
        if (!IsHandleCreated) CreateControl();
        if (layered && !_native.SetAlpha(Handle, alpha, out var error))
        {
            NativeError = error; _failed = true; _layered = false;
            RecreateHandle(); BackColor = Color.FromArgb(255, fallback); layered = false;
        }
        Invalidate();
        return layered ? BootstrapBusyOverlayRenderMode.NativeLayered : BootstrapBusyOverlayRenderMode.OpaqueFallback;
    }
    protected override CreateParams CreateParams { get { var value = base.CreateParams; value.Style |= 0x04000000; if (_layered) value.ExStyle |= 0x80000; return value; } }
    protected override void OnPaint(PaintEventArgs e) => e.Graphics.Clear(BackColor);
}
