using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost;

// Native, bounded diagnostic. Each theme gets a fresh HWND; this does not change display scale.
internal static class TabsMatrixScenario
{
    private const int WmPrintClient = 0x0318;
    private const int WmLButtonDown = 0x0201;
    private const int WmLButtonUp = 0x0202;
    private static readonly Stopwatch Clock = new Stopwatch();
    private static readonly StringBuilder Trace = new StringBuilder();

    internal static int Run(string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        Clock.Restart();
        var savedTheme = BootstrapThemeManager.CurrentTheme;
        var savedCursor = Cursor.Position;
        var passed = true;
        try
        {
            Write("environment", "OS=" + Environment.OSVersion + "; runtime=" + Environment.Version +
                "; processBitness=" + (Environment.Is64BitProcess ? "64" : "32") +
                "; monitors=" + Screen.AllScreens.Length + "; systemDpi=" + GetDpiForSystem());
            foreach (var mode in new[] { BootstrapThemeMode.Light, BootstrapThemeMode.Dark })
                passed &= RunMode(mode, outputDirectory);
        }
        catch (Exception ex)
        {
            passed = false;
            Write("exception", ex.ToString());
        }
        finally
        {
            Cursor.Position = savedCursor;
            BootstrapThemeManager.CurrentTheme = savedTheme;
            Write("result", passed ? "PASS" : "FAIL");
            File.WriteAllText(Path.Combine(outputDirectory, "tabs-matrix.txt"), Trace.ToString(), new UTF8Encoding(false));
        }
        return passed ? 0 : 1;
    }

    private static bool RunMode(BootstrapThemeMode mode, string outputDirectory)
    {
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode);
        using var form = new Form
        {
            Text = "BootstrapTabs matrix " + mode,
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
        var closePage = tabs.TabPages[1];
        var closeCount = 0;
        var closeReason = "none";
        tabs.TabCloseRequested += (_, e) =>
        {
            closeCount++;
            closeReason = e.Reason.ToString();
            if (e.Page == closePage) tabs.TabPages.Remove(e.Page);
        };
        var moveCount = 0;
        tabs.MouseMove += (_, _) => moveCount++;
        form.Controls.Add(tabs);
        form.Show();
        form.Activate();
        Pump();
        var handle = tabs.Handle;
        var font = tabs.Font;
        var itemSize = tabs.ItemSize;
        var selected = tabs.SelectedTab;
        var rect = tabs.GetTabRect(0);
        Write(mode + "-dpi", "formDevice=" + form.DeviceDpi + "; tabsDevice=" + tabs.DeviceDpi +
            "; formHwnd=" + GetDpiForWindow(form.Handle) + "; tabsHwnd=" + GetDpiForWindow(tabs.Handle) +
            "; handle=" + tabs.Handle + "; monitor=" + Screen.FromHandle(tabs.Handle).DeviceName);

        var allTargets = true;
        foreach (var index in new[] { 0, 1, 0 })
        {
            var header = tabs.GetTabRect(index);
            Cursor.Position = tabs.PointToScreen(new Point(header.Left + Math.Min(10, header.Width / 2), header.Top + header.Height / 2));
            Thread.Sleep(25);
            Pump();
            var target = WindowFromPoint(Cursor.Position);
            allTargets &= target == tabs.Handle;
            Write(mode + "-hover", "index=" + index + "; expectedHwnd=" + tabs.Handle + "; targetHwnd=" + target);
        }
        var hover = allTargets && moveCount >= 3 && tabs.Handle == handle && ReferenceEquals(tabs.Font, font) &&
            tabs.ItemSize == itemSize && tabs.GetTabRect(0) == rect && tabs.SelectedTab == selected;
        Write(mode + "-hover-result", "pass=" + hover + "; moves=" + moveCount + "; targets=" + allTargets);
        Capture(form, Path.Combine(outputDirectory, mode + "-hover.png"));

        // Click the exposed close affordance through the real tab HWND.
        var closeRect = tabs.GetTabRect(1);
        var closePoint = new Point(closeRect.Right - 13, closeRect.Top + closeRect.Height / 2);
        var closeParam = (IntPtr)((closePoint.Y << 16) | (closePoint.X & 0xffff));
        SendMessage(tabs.Handle, WmLButtonDown, (IntPtr)1, closeParam);
        SendMessage(tabs.Handle, WmLButtonUp, IntPtr.Zero, closeParam);
        Pump();
        var close = closeCount == 1 && closeReason == "Pointer" && !tabs.TabPages.Contains(closePage) && tabs.SelectedTab == selected;
        Write(mode + "-close", "pass=" + close + "; requests=" + closeCount + "; reason=" + closeReason +
            "; remaining=" + tabs.TabCount);
        if (!tabs.TabPages.Contains(closePage)) closePage.Dispose();

        var overflowChild = FindWindowEx(tabs.Handle, IntPtr.Zero, "msctls_updown32", null);
        var overflow = overflowChild != IntPtr.Zero;
        if (overflow)
        {
            var before = tabs.GetTabRect(0);
            GetWindowRect(overflowChild, out var arrow);
            Cursor.Position = new Point(arrow.Right - 8, (arrow.Top + arrow.Bottom) / 2);
            Thread.Sleep(25);
            Pump();
            var arrowTarget = WindowFromPoint(Cursor.Position);
            MouseEvent(0x0002, 0, 0, 0, UIntPtr.Zero);
            MouseEvent(0x0004, 0, 0, 0, UIntPtr.Zero);
            Pump();
            var after = tabs.GetTabRect(0);
            overflow = arrowTarget == overflowChild && before != after;
            Write(mode + "-overflow", "pass=" + overflow + "; child=" + overflowChild + "; target=" + arrowTarget +
                "; before=" + before + "; after=" + after);
        }
        else Write(mode + "-overflow", "FAIL: no native updown child");

        using var print = new Bitmap(tabs.Width, tabs.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(print))
        {
            graphics.Clear(Color.Fuchsia);
            var hdc = graphics.GetHdc();
            try { SendMessage(tabs.Handle, WmPrintClient, hdc, (IntPtr)4); }
            finally { graphics.ReleaseHdc(hdc); }
        }
        // Overflow scrolled the first header out of the client bounds; inspect the next visible header.
        var visibleHeader = Rectangle.Intersect(tabs.GetTabRect(1), tabs.ClientRectangle);
        var printPixels = CountNotColor(print, Color.Fuchsia, visibleHeader);
        var printPass = printPixels > 12;
        Write(mode + "-printclient", "pass=" + printPass + "; headerPixels=" + printPixels);
        print.Save(Path.Combine(outputDirectory, mode + "-printclient.png"));

        tabs.Alignment = TabAlignment.Bottom;
        tabs.RightToLeft = RightToLeft.Yes;
        tabs.Appearance = TabAppearance.Buttons;
        tabs.Multiline = true;
        Pump();
        var fallback = tabs.Alignment == TabAlignment.Bottom && tabs.RightToLeft == RightToLeft.Yes &&
            tabs.Appearance == TabAppearance.Buttons && tabs.Multiline && tabs.SelectedTab == selected;
        Capture(form, Path.Combine(outputDirectory, mode + "-fallback.png"));
        tabs.Alignment = TabAlignment.Top;
        tabs.RightToLeft = RightToLeft.No;
        tabs.Appearance = TabAppearance.Normal;
        tabs.Multiline = false;
        Pump();
        fallback &= tabs.Alignment == TabAlignment.Top && tabs.RightToLeft == RightToLeft.No &&
            tabs.Appearance == TabAppearance.Normal && !tabs.Multiline && tabs.SelectedTab == selected;
        Write(mode + "-fallback", "pass=" + fallback + "; tabs=" + tabs.TabCount + "; selected=" + tabs.SelectedIndex);
        var pass = hover && close && overflow && printPass && fallback;
        Write(mode + "-result", pass ? "PASS" : "FAIL");
        form.Close();
        Pump();
        return pass;
    }

    private static void Capture(Form form, string path)
    {
        using var bitmap = new Bitmap(form.Width, form.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(form.Location, Point.Empty, form.Size);
        bitmap.Save(path);
    }

    private static int CountNotColor(Bitmap bitmap, Color color, Rectangle bounds)
    {
        var count = 0;
        foreach (var y in new[] { bounds.Top + 3, bounds.Top + bounds.Height / 2, bounds.Bottom - 3 })
            for (var x = bounds.Left + 3; x < bounds.Right - 3; x += 3)
                if (x >= 0 && y >= 0 && x < bitmap.Width && y < bitmap.Height && bitmap.GetPixel(x, y).ToArgb() != color.ToArgb()) count++;
        return count;
    }

    private static void Pump()
    {
        if (Clock.Elapsed > TimeSpan.FromSeconds(29)) throw new TimeoutException("tabs-matrix exceeded 29 seconds");
        Application.DoEvents();
    }

    private static void Write(string label, string value) => Trace.AppendLine(label + ": " + value);

    [StructLayout(LayoutKind.Sequential)] private struct Rect { internal int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] private static extern uint GetDpiForSystem();
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(Point point);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string className, string? windowName);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out Rect rect);
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll", EntryPoint = "mouse_event")] private static extern void MouseEvent(uint flags, uint x, uint y, uint data, UIntPtr extraInfo);
}
