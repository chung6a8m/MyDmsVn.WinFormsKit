using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;

namespace MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost;

internal static class ComboBoxAccessibilityScenario
{
    internal static int Run(string outputDirectory, bool primeProvider)
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        Directory.CreateDirectory(outputDirectory);
        using var form = new Form { ShowInTaskbar = false, AutoScaleMode = AutoScaleMode.None };
        using var combo = new QueryDuringRecreationCombo { IntegralHeight = false, Size = new Size(220, 120) };
        Require(!combo.IsHandleCreated, "Construction must be handle-free.");
        combo.Items.AddRange(new object[] { "Alpha", "Beta" });
        combo.SelectedIndex = 1;
        form.Controls.Add(combo);
        form.Show();
        Application.DoEvents();
        if (primeProvider) VerifyStableList(combo);
        for (var iteration = 0; iteration < 3; iteration++)
        {
            combo.DropDownStyle = ComboBoxStyle.Simple;
            Application.DoEvents();
            Require(combo.Height == 120, "Simple caller height changed.");
            VerifyStableList(combo);
            combo.DropDownStyle = ComboBoxStyle.DropDown;
            Application.DoEvents();
            VerifyStableList(combo);
        }
        Require(combo.TransientQueries == (IntPtr.Size == 8 ? 9 : 6), "Expected MSAA and UIA queries on each retiring dropdown list.");
        Require(combo.SelectedIndex == 1, "Selection changed during recreation.");
        File.WriteAllLines(Path.Combine(outputDirectory, "combo-accessibility.txt"), new[]
        {
            "transient.queries=" + combo.TransientQueries,
            "stable.queries=" + (primeProvider ? 7 : 6), "selection.index=" + combo.SelectedIndex,
            "oracle.no-crash=pass", "oracle.stable-native-accessibility=pass", "oracle.simple-height=pass"
        });
        return 0;
    }

    private static void VerifyStableList(ComboBox combo)
    {
        var info = GetInfo(combo.Handle);
        var id = new Guid("618736E0-3C3D-11CF-810C-00AA00389B71");
        var result = AccessibleObjectFromWindow(info.List, 0xFFFFFFFC, ref id, out var accessible);
        try
        {
            Require(result == 0 && accessible is not null, "Stable native list must expose IAccessible.");
            Require(combo.AccessibilityObject.Role == AccessibleRole.ComboBox, "Preserve native ComboBox role.");
        }
        finally { if (accessible is not null && Marshal.IsComObject(accessible)) Marshal.ReleaseComObject(accessible); }
    }

    private static ComboInfo GetInfo(IntPtr window)
    {
        var info = new ComboInfo { Size = Marshal.SizeOf(typeof(ComboInfo)) };
        Require(GetComboBoxInfo(window, ref info) && info.List != IntPtr.Zero, "Native list HWND missing.");
        return info;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class QueryDuringRecreationCombo : BootstrapComboBox
    {
        internal int TransientQueries { get; private set; }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0002 && RecreatingHandle && DropDownStyle == ComboBoxStyle.Simple)
            {
                var info = GetInfo(m.HWnd);
                // An accessibility client can query the old dropdown while its owner already
                // reports Simple. This is a real native message to a still-live child HWND.
                var objectIds = IntPtr.Size == 8
                    ? new[] { new IntPtr(0xFFFFFFFCL), new IntPtr(-4), new IntPtr(-25) }
                    : new[] { new IntPtr(-4), new IntPtr(-25) };
                foreach (var objectId in objectIds)
                {
                    TransientQueries++;
                    Require(SendMessage(info.List, 0x003D, IntPtr.Zero, objectId) == IntPtr.Zero,
                        "Retiring list must not expose a stale provider.");
                }
            }
            base.WndProc(ref m);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct ComboInfo
    {
        public int Size;
        public NativeRect ItemBounds, ButtonBounds;
        public int ButtonState;
        public IntPtr Combo, Edit, List;
    }
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetComboBoxInfo(IntPtr window, ref ComboInfo info);
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);
    [DllImport("oleacc.dll")]
    private static extern int AccessibleObjectFromWindow(IntPtr window, uint objectId, ref Guid interfaceId,
        [MarshalAs(UnmanagedType.Interface)] out object? accessible);
}
