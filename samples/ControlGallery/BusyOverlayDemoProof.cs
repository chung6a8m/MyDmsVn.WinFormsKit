using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

// Optional bounded executable diagnostic; runs inside the actual manifested apphost.
internal static class BusyOverlayDemoProof
{
    internal static void Run(string output)
    {
        using var form = new BusyOverlayDemoForm();
        using var timer = new Timer { Interval = 900 };
        IDisposable? token = null;
        form.Shown += (_, _) =>
        {
            var host = Descendants(form).OfType<BootstrapBusyOverlayHost>().Single();
            token = host.Begin(new BusyPresentation("Đang kiểm tra live…"));
            timer.Start();
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            try
            {
                var module = GetModuleHandle(null);
                var resource = FindResource(module, (IntPtr)1, (IntPtr)24);
                if (resource == IntPtr.Zero) throw new InvalidOperationException("Missing embedded manifest.");
                var manifest = Marshal.PtrToStringAnsi(LockResource(LoadResource(module, resource)), checked((int)SizeofResource(module, resource)));
                QueryActCtxW(8, module, IntPtr.Zero, 6, IntPtr.Zero, UIntPtr.Zero, out var size);
                if (size.ToUInt64() < 4 || size.ToUInt64() > 65536) throw new InvalidOperationException("Invalid compatibility context size.");
                var buffer = Marshal.AllocHGlobal(checked((int)size.ToUInt64()));
                try
                {
                    var queried = QueryActCtxW(8, module, IntPtr.Zero, 6, buffer, size, out var written);
                    if (!queried || written.ToUInt64() < 4 || written.ToUInt64() > size.ToUInt64()) throw new InvalidOperationException("Compatibility context query failed.");
                    var host = Descendants(form).OfType<BootstrapBusyOverlayHost>().Single();
                    File.WriteAllText(output, "EmbeddedManifest=" + manifest + Environment.NewLine + "QueryActCtx(EXE HMODULE)=" + queried + "; bytes=" + written + "; elements=" + Marshal.ReadInt32(buffer) + Environment.NewLine + "ContextHex=" + BitConverter.ToString(Copy(buffer, checked((int)written.ToUInt64()))) + Environment.NewLine + "DPI=" + form.DeviceDpi + "; PerMonitorV2=" + AreDpiAwarenessContextsEqual(GetWindowDpiAwarenessContext(form.Handle), (IntPtr)(-4)) + "; RenderMode=" + host.RenderMode + "; Bits=" + (IntPtr.Size * 8));
                    if (host.RenderMode != BootstrapBusyOverlayRenderMode.NativeLayered || !AreDpiAwarenessContextsEqual(GetWindowDpiAwarenessContext(form.Handle), (IntPtr)(-4))) Environment.ExitCode = 1;
                }
                finally { Marshal.FreeHGlobal(buffer); }
            }
            finally { token?.Dispose(); form.Close(); }
        };
        Application.Run(form);
    }
    private static byte[] Copy(IntPtr buffer, int count) { var bytes = new byte[count]; Marshal.Copy(buffer, bytes, 0, count); return bytes; }
    private static System.Collections.Generic.IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls) { yield return child; foreach (var nested in Descendants(child)) yield return nested; }
    }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string? name);
    [DllImport("kernel32.dll")] private static extern IntPtr FindResource(IntPtr module, IntPtr name, IntPtr type);
    [DllImport("kernel32.dll")] private static extern IntPtr LoadResource(IntPtr module, IntPtr resource);
    [DllImport("kernel32.dll")] private static extern IntPtr LockResource(IntPtr resource);
    [DllImport("kernel32.dll")] private static extern uint SizeofResource(IntPtr module, IntPtr resource);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool QueryActCtxW(uint flags, IntPtr context, IntPtr sub, uint info, IntPtr buffer, UIntPtr size, out UIntPtr required);
    [DllImport("user32.dll")] private static extern IntPtr GetWindowDpiAwarenessContext(IntPtr window);
    [DllImport("user32.dll")] private static extern bool AreDpiAwarenessContextsEqual(IntPtr first, IntPtr second);
}
