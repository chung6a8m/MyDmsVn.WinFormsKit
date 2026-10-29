using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost;

// A bounded, visible native-pointer sequence intended for an external screen recorder.
internal static class TabsTemporalScenario
{
    internal static int Run(string outputDirectory, string themeName)
    {
        if (!Enum.TryParse(themeName, true, out BootstrapThemeMode mode) ||
            (mode != BootstrapThemeMode.Light && mode != BootstrapThemeMode.Dark))
            throw new ArgumentException("Expected Light or Dark.", nameof(themeName));
        Directory.CreateDirectory(outputDirectory);
        var oldTheme = BootstrapThemeManager.CurrentTheme;
        var oldCursor = Cursor.Position;
        var trace = new StringBuilder();
        var clock = Stopwatch.StartNew();
        var passed = true;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode);
            using var form = new Form
            {
                Text = "BootstrapTabs temporal " + mode,
                ClientSize = new Size(700, 420),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(80, 80),
                ShowInTaskbar = false,
                TopMost = true
            };
            using var tabs = new BootstrapTabs
            {
                Bounds = new Rectangle(30, 30, 520, 220),
                HotTrack = true,
                ShowToolTips = true
            };
            for (var i = 0; i < 11; i++)
                tabs.TabPages.Add(new TabPage("Tab " + i + " long caption") { ToolTipText = "Tooltip " + i });
            tabs.SelectedIndex = 0;
            tabs.SetTabClosable(tabs.TabPages[1], true);
            form.Controls.Add(tabs);
            form.Show();
            form.Activate();
            PumpFor(100);
            var handle = tabs.Handle;
            var selected = tabs.SelectedTab;
            var firstRect = tabs.GetTabRect(0);
            var font = tabs.Font;
            var itemSize = tabs.ItemSize;
            var moves = 0;
            tabs.MouseMove += (_, _) => moves++;
            trace.AppendLine("candidate=" + GitHead() + "; mode=" + mode + "; OS=" + Environment.OSVersion +
                "; runtime=" + Environment.Version + "; bitness=" + (IntPtr.Size * 8) +
                "; deviceDpi=" + tabs.DeviceDpi + "; nativeDpi=" + GetDpiForWindow(handle) +
                "; monitor=" + Screen.FromHandle(handle).DeviceName + "; hwnd=" + handle);
            File.WriteAllText(Path.Combine(outputDirectory, "ready.txt"), form.Text + "; hwnd=" + form.Handle, new UTF8Encoding(false));
            PumpFor(1800); // Recorder attaches after the window becomes visible.
            for (var cycle = 0; cycle < 5; cycle++)
            {
                foreach (var index in new[] { 0, 1, 0 })
                {
                    var rect = tabs.GetTabRect(index);
                    Cursor.Position = tabs.PointToScreen(new Point(rect.Left + Math.Min(10, rect.Width / 2), rect.Top + rect.Height / 2));
                    PumpFor(220);
                    var target = WindowFromPoint(Cursor.Position);
                    var hit = target == handle;
                    passed &= hit;
                    trace.AppendLine(clock.ElapsedMilliseconds + "ms; cycle=" + cycle + "; index=" + index +
                        "; target=" + target + "; expected=" + handle + "; hit=" + hit +
                        (hit ? "" : "; targetRoot=" + GetAncestor(target, 2) +
                            "; foreground=" + GetForegroundWindow()));
                }
            }
            PumpFor(1000);
            passed &= moves >= 15 && tabs.Handle == handle && ReferenceEquals(tabs.Font, font) &&
                tabs.ItemSize == itemSize && tabs.GetTabRect(0) == firstRect && tabs.SelectedTab == selected;
            trace.AppendLine("result=" + (passed ? "PASS" : "FAIL") + "; mouseMoves=" + moves +
                "; hwndStable=" + (tabs.Handle == handle) + "; elapsedMs=" + clock.ElapsedMilliseconds);
            return passed ? 0 : 2;
        }
        finally
        {
            Cursor.Position = oldCursor;
            BootstrapThemeManager.CurrentTheme = oldTheme;
            File.WriteAllText(Path.Combine(outputDirectory, "trace.txt"), trace.ToString(), new UTF8Encoding(false));
        }
    }

    private static void PumpFor(int milliseconds)
    {
        var until = Stopwatch.StartNew();
        while (until.ElapsedMilliseconds < milliseconds)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
    }

    private static string GitHead()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("git", "rev-parse HEAD")
            { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true });
            if (process == null || !process.WaitForExit(2000) || process.ExitCode != 0) return "unknown";
            return process.StandardOutput.ReadToEnd().Trim();
        }
        catch { return "unknown"; }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(Point point);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr window);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr window, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
}
