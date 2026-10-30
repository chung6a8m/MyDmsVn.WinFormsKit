using System;
using System.Runtime.InteropServices;
namespace MyDmsVn.Bootstrap5WinFormUI.Compatibility;
internal interface IBootstrapBusyLayeredWindow
{
    bool IsSupported { get; }
    bool SetAlpha(IntPtr window, byte alpha, out int error);
}
internal sealed class BootstrapBusyLayeredWindow : IBootstrapBusyLayeredWindow
{
    internal static readonly BootstrapBusyLayeredWindow Instance = new();
    private static readonly Lazy<bool> Capability = new(QueryExecutable);
    public bool IsSupported => IsChildLayeredOs(Environment.OSVersion.Version) && Capability.Value;
    internal static bool IsChildLayeredOs(Version version) => version.Major > 6 || (version.Major == 6 && version.Minor >= 2);
    public bool SetAlpha(IntPtr window, byte alpha, out int error)
    {
        var success = SetLayeredWindowAttributes(window, 0, alpha, 2);
        error = success ? 0 : Marshal.GetLastWin32Error();
        return success;
    }
    // QUERY_ACTCTX_FLAG_ACTCTX_IS_HMODULE selects the process executable, not
    // the visual-style activation context on the calling thread.
    private static bool QueryExecutable()
    {
        var module = GetModuleHandle(null);
        if (module == IntPtr.Zero) return false;
        return ReadCompatibility((IntPtr buffer, UIntPtr capacity, out UIntPtr required, out int error) =>
        {
            var result = QueryActCtxW(8, module, IntPtr.Zero, 6, buffer, capacity, out required);
            error = Marshal.GetLastWin32Error(); return result;
        });
    }
    internal delegate bool CompatibilityQuery(IntPtr buffer, UIntPtr capacity, out UIntPtr required, out int error);
    internal static bool ReadCompatibility(CompatibilityQuery query)
    {
        var success = query(IntPtr.Zero, UIntPtr.Zero, out var size, out var error);
        if (success || error != 122 || size.ToUInt64() < 4 || size.ToUInt64() > 65536) return false;
        var length = (int)size.ToUInt64();
        var memory = Marshal.AllocHGlobal(length);
        try
        {
            if (!query(memory, size, out var written, out _) || written.ToUInt64() > (ulong)length || written.ToUInt64() < 4) return false;
            var bytes = new byte[(int)written.ToUInt64()]; Marshal.Copy(memory, bytes, 0, bytes.Length);
            return ParseCompatibility(bytes, Marshal.OffsetOf<CompatibilityInformation>(nameof(CompatibilityInformation.First)).ToInt32(), Marshal.SizeOf<CompatibilityElement>());
        }
        finally { Marshal.FreeHGlobal(memory); }
    }
    internal static bool ParseCompatibility(byte[] bytes, int offset, int stride)
    {
        // Native structure has an aligned array of GUID/type/ULONGLONG records.
        if (bytes.Length < 4 || offset < 4 || offset % 8 != 0 || stride < 32 || stride % 8 != 0) return false;
        var count = BitConverter.ToUInt32(bytes, 0);
        if ((ulong)offset + (ulong)count * (ulong)stride > (ulong)bytes.Length) return false;
        for (uint i = 0; i < count; i++)
        {
            var start = checked(offset + (int)i * stride);
            var idBytes = new byte[16]; Array.Copy(bytes, start, idBytes, 0, 16);
            var id = new Guid(idBytes);
            if (BitConverter.ToInt32(bytes, start + 16) == 1 &&
                (id == new Guid("1f676c76-80e1-4239-95bb-83d0f6d0da78") ||
                 id == new Guid("4a2f28e3-53b9-4441-ba9c-d69d4a4a6e38") ||
                 id == new Guid("8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a"))) return true;
        }
        return false;
    }
    [StructLayout(LayoutKind.Sequential)] private struct CompatibilityElement { internal Guid Id; internal int Type; internal ulong MaxVersion; }
    [StructLayout(LayoutKind.Sequential)] private struct CompatibilityInformation { internal uint Count; internal CompatibilityElement First; }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string? name);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool QueryActCtxW(uint flags, IntPtr context, IntPtr sub, uint info, IntPtr buffer, UIntPtr size, out UIntPtr required);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetLayeredWindowAttributes(IntPtr window, uint color, byte alpha, uint flags);
}
