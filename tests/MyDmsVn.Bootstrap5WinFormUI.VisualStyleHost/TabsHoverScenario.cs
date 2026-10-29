using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;

namespace MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost;

// This is a diagnostic host, deliberately separate from the production painter.
internal static class TabsHoverScenario
{
    private const int WmPaint = 0x000F;
    private const int WmPrintClient = 0x0318;
    private const int WmEraseBkgnd = 0x0014;
    private static readonly Stopwatch Clock = new Stopwatch();
    private static readonly List<string> Trace = new List<string>();

    internal static int Run(string outputDirectory)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory)) throw new ArgumentException("An output directory is required.", nameof(outputDirectory));
        Directory.CreateDirectory(outputDirectory);
        Clock.Restart();
        var previousCursor = Cursor.Position;
        var result = "PROBE BLOCKED";
        var suppliedPaint = false;
        var printClient = false;
        var drawOutsidePaint = 0;
        var invariant = false;
        try
        {
            using var images = new ImageList { ImageSize = new Size(16, 16) };
            using var image = new Bitmap(16, 16);
            using (var graphics = Graphics.FromImage(image))
            {
                graphics.Clear(Color.RoyalBlue);
                images.Images.Add(image);
            }
            using var form = new Form
            {
                Text = "Tabs native paint proof",
                ClientSize = new Size(900, 620),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(80, 80),
                ShowInTaskbar = false
            };
            using var native = new ProbeTabControl { Bounds = new Rectangle(25, 25, 390, 170), HotTrack = true };
            using var hot = new BootstrapTabControl { Bounds = new Rectangle(460, 25, 390, 170), HotTrack = true };
            using var cold = new BootstrapTabControl { Bounds = new Rectangle(25, 240, 390, 170), HotTrack = false };
            using var paintProbe = new ProbeTabControl { Bounds = new Rectangle(460, 240, 390, 170), HotTrack = true, DrawMode = TabDrawMode.OwnerDrawFixed };
            foreach (TabControl tabs in new TabControl[] { native, hot, cold, paintProbe })
            {
                tabs.Font = form.Font;
                tabs.ImageList = images;
                tabs.ShowToolTips = true;
                Populate(tabs);
                form.Controls.Add(tabs);
            }
            hot.SetTabClosable(hot.TabPages[1], true);
            cold.SetTabClosable(cold.TabPages[1], true);
            form.Show();
            Pump();
            Write("environment", Environment.OSVersion + "; " + IntPtr.Size * 8 + " bit; runtime " + Environment.Version + "; DeviceDpi=" + form.DeviceDpi + "; nativeDpi=" + GetDpiForWindow(form.Handle));
            Write("overflow-child", "probe=" + FindWindowEx(paintProbe.Handle, IntPtr.Zero, "msctls_updown32", null));
            foreach (TabControl tabs in new TabControl[] { native, hot, cold, paintProbe })
                Snapshot(tabs, "before");
            var hotBefore = Signature(hot);
            var coldBefore = Signature(cold);
            Capture(form, Path.Combine(outputDirectory, "before.png"));

            // Cursor.Position sends actual desktop pointer movement, not an artificial MouseMove event.
            foreach (TabControl tabs in new TabControl[] { native, hot, cold, paintProbe })
            {
                for (var index = 0; index < 3; index++)
                {
                    var rect = tabs.GetTabRect(index);
                    Cursor.Position = tabs.PointToScreen(new Point(rect.Left + Math.Min(10, rect.Width / 2), rect.Top + rect.Height / 2));
                    Pump();
                    Write("hover", tabs.GetType().Name + "; index=" + index + "; handle=" + tabs.Handle + "; rect=" + rect);
                }
                Snapshot(tabs, "after");
            }
            Capture(form, Path.Combine(outputDirectory, "after.png"));
            invariant = hotBefore == Signature(hot) && coldBefore == Signature(cold);
            Write("hover-invariant", invariant ? "PASS" : "FAIL");

            using (var surface = new Bitmap(paintProbe.Width + 40, paintProbe.Height + 40, PixelFormat.Format32bppArgb))
            using (var graphics = Graphics.FromImage(surface))
            {
                graphics.Clear(Color.Fuchsia);
                var hdc = graphics.GetHdc();
                try
                {
                    paintProbe.StartSuppliedPaint();
                    // Saved DC origin and clip exercise a target whose origin is not the screen.
                    var saved = SaveDC(hdc);
                    SetViewportOrgEx(hdc, 17, 11, out _);
                    IntersectClipRect(hdc, 17, 11, paintProbe.Width + 17, paintProbe.Height + 11);
                    SendMessage(paintProbe.Handle, WmPaint, hdc, IntPtr.Zero);
                    paintProbe.EndSuppliedPaint();
                    suppliedPaint = paintProbe.SuppliedDrawCount > 0;
                    Write("supplied-paint", "draw=" + paintProbe.SuppliedDrawCount + "; outside=" + paintProbe.OutsidePaintDrawCount + "; depth=" + paintProbe.MaxPaintDepth);
                    SendMessage(paintProbe.Handle, WmPrintClient, hdc, (IntPtr)0x00000004);
                    printClient = paintProbe.PrintDrawCount > 0;
                    RestoreDC(hdc, saved);
                }
                finally { graphics.ReleaseHdc(hdc); }
                surface.Save(Path.Combine(outputDirectory, "supplied-paint.png"));
            }
            drawOutsidePaint = paintProbe.OutsidePaintDrawCount;
            Capture(form, Path.Combine(outputDirectory, "final.png"));
            result = suppliedPaint && printClient && drawOutsidePaint == 0 && invariant ? "PROBE PASS" : "PROBE BLOCKED";
            Write("gate", "suppliedPaint=" + suppliedPaint + "; printClientDispatched=" + printClient + "; outsidePaintDraw=" + drawOutsidePaint + "; hoverInvariant=" + invariant);
        }
        finally
        {
            Cursor.Position = previousCursor;
            File.WriteAllLines(Path.Combine(outputDirectory, "trace.txt"), Trace, new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(outputDirectory, "metadata.json"),
                "{\"candidate\":\"" + Escape(RunGitHead()) + "\",\"os\":\"" + Escape(Environment.OSVersion.ToString()) +
                "\",\"bitness\":" + (IntPtr.Size * 8) + ",\"runtime\":\"" + Escape(Environment.Version.ToString()) +
                "\",\"probeResult\":\"" + result + "\",\"fullGate\":\"OPEN\",\"flicker\":\"NOT REPRODUCED\",\"elapsedMs\":" + Clock.ElapsedMilliseconds + "}", new UTF8Encoding(false));
        }
        return result == "PROBE PASS" ? 0 : 2;
    }

    private static string RunGitHead()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("git", "rev-parse HEAD")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            });
            if (process == null || !process.WaitForExit(2000) || process.ExitCode != 0) return "unknown";
            return process.StandardOutput.ReadToEnd().Trim();
        }
        catch { return "unknown"; }
    }
    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    private static void Write(string kind, string value) => Trace.Add(Clock.ElapsedMilliseconds + "\t" + kind + "\t" + value);
    private static void Pump()
    {
        if (Clock.Elapsed > TimeSpan.FromSeconds(29)) throw new TimeoutException("Tabs hover scenario exceeded 29 seconds.");
        for (var i = 0; i < 3; i++) Application.DoEvents();
    }
    private static void Populate(TabControl tabs)
    {
        var names = new[] { "Trang đầu", "Đóng được", "Dữ liệu có ảnh", "Một tiêu đề tiếng Việt khá dài", "Overflow 5", "Overflow 6", "Overflow 7", "Overflow 8", "Overflow 9", "Overflow 10" };
        for (var i = 0; i < names.Length; i++)
            tabs.TabPages.Add(new TabPage(names[i]) { ImageIndex = i == 2 ? 0 : -1, ToolTipText = names[i] });
    }
    private static void Snapshot(TabControl tabs, string stage)
    {
        var rects = new StringBuilder();
        for (var i = 0; i < tabs.TabCount; i++) rects.Append(i).Append(':').Append(tabs.GetTabRect(i)).Append(';');
        Write("snapshot", stage + "; " + tabs.GetType().Name + "; handle=" + tabs.Handle + "; selected=" + tabs.SelectedIndex + "; font=" + tabs.Font + "; itemSize=" + tabs.ItemSize + "; rects=" + rects);
    }
    private static string Signature(TabControl tabs)
    {
        var value = tabs.Handle + ";" + tabs.SelectedIndex + ";" + tabs.Font.Name + ";" + tabs.Font.Size + ";" + tabs.ItemSize;
        for (var i = 0; i < tabs.TabCount; i++) value += ";" + tabs.GetTabRect(i);
        return value;
    }
    private static void Capture(Form form, string path)
    {
        using var bitmap = new Bitmap(form.Width, form.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(form.Location, Point.Empty, bitmap.Size);
        bitmap.Save(path, ImageFormat.Png);
    }

    private sealed class ProbeTabControl : TabControl
    {
        private int _paintDepth;
        private int _printDepth;
        private bool _supplied;
        internal int SuppliedDrawCount { get; private set; }
        internal int PrintDrawCount { get; private set; }
        internal int OutsidePaintDrawCount { get; private set; }
        internal int MaxPaintDepth { get; private set; }
        internal void StartSuppliedPaint() { _supplied = true; SuppliedDrawCount = 0; }
        internal void EndSuppliedPaint() => _supplied = false;
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmPaint)
            {
                _paintDepth++;
                MaxPaintDepth = Math.Max(MaxPaintDepth, _paintDepth);
                Write("WM_PAINT", "depth=" + _paintDepth + "; supplied=" + (m.WParam != IntPtr.Zero) + "; handle=" + Handle);
                try { base.WndProc(ref m); }
                finally { _paintDepth--; }
                return;
            }
            if (m.Msg == WmEraseBkgnd) Write("WM_ERASEBKGND", "handle=" + Handle);
            if (m.Msg == WmPrintClient)
            {
                _printDepth++;
                Write("WM_PRINTCLIENT", "depth=" + _printDepth + "; handle=" + Handle);
                try { base.WndProc(ref m); }
                finally { _printDepth--; }
                return;
            }
            base.WndProc(ref m);
        }
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            Write("DrawItem", "index=" + e.Index + "; bounds=" + e.Bounds + "; paintDepth=" + _paintDepth + "; printDepth=" + _printDepth + "; supplied=" + _supplied + "; hdc=" + e.Graphics.GetHdc());
            // GetHdc must be balanced before the normal event pipeline continues.
            e.Graphics.ReleaseHdc();
            if (_supplied) SuppliedDrawCount++;
            if (_printDepth > 0) PrintDrawCount++;
            if (_paintDepth == 0 && _printDepth == 0) OutsidePaintDrawCount++;
            using (var brush = new SolidBrush(Color.Navy)) e.Graphics.FillRectangle(brush, e.Bounds);
            TextRenderer.DrawText(e.Graphics, TabPages[e.Index].Text, Font, e.Bounds, Color.White, TextFormatFlags.EndEllipsis);
            base.OnDrawItem(e);
        }
    }

    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    [DllImport("gdi32.dll")] private static extern int SaveDC(IntPtr hdc);
    [DllImport("gdi32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool RestoreDC(IntPtr hdc, int savedDc);
    [DllImport("gdi32.dll")] private static extern int IntersectClipRect(IntPtr hdc, int left, int top, int right, int bottom);
    [DllImport("gdi32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetViewportOrgEx(IntPtr hdc, int x, int y, out Point previous);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr hWnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string className, string? windowName);
}
