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

// BUS-01 intentionally has no production host dependency. Desktop capture is proof only.
internal static class BusyOverlayScenario
{
    private static readonly Color Tint = Color.FromArgb(24, 32, 40);
    private static readonly Color[] Patches = { Color.Lime, Color.Red, Color.Blue, Color.Yellow };
    private static readonly Guid Windows10 = new("8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a");

    internal static int Run(string directory, bool fallback)
    {
        Directory.CreateDirectory(directory);
        using var log = new StreamWriter(Path.Combine(directory, "result.txt"), false, new UTF8Encoding(false)) { AutoFlush = true };
        try
        {
            log.WriteLine($"Candidate={Environment.GetEnvironmentVariable("BUS_CANDIDATE")}; Runtime={RuntimeInformation.FrameworkDescription}; Bitness={IntPtr.Size * 8}; OS={Environment.OSVersion}; Interactive={Environment.UserInteractive}");
            var capability = QueryExecutable(log);
            Require(capability != fallback, "Executable capability does not match requested scenario.");
            using var form = new Form { Text = "BUS-01 native child-layer proof", StartPosition = FormStartPosition.Manual, Location = new Point(80, 80), ClientSize = new Size(800, 580) };
            using var tabs = new TabControl { Bounds = new Rectangle(10, 10, 650, 500) };
            var page = new TabPage("Probe");
            tabs.TabPages.Add(page);
            tabs.TabPages.Add(new TabPage("Other"));
            using var scroll = new Panel { Bounds = new Rectangle(12, 12, 590, 410), AutoScroll = true };
            using var host = new Panel { Bounds = new Rectangle(20, 20, 530, 340) };
            using var content = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            using var edit = new TextBox { Bounds = new Rectangle(20, 20, 220, 26), Text = "Live 0" };
            using var tree = new TreeView { Bounds = new Rectangle(20, 60, 220, 90) };
            tree.Nodes.Add("Live 0");
            using var progress = new ProgressBar { Bounds = new Rectangle(20, 165, 220, 24), Value = 10 };
            using var patch = new Panel { Bounds = new Rectangle(20, 210, 200, 70), BackColor = Patches[0] };
            using var action = new Button { Text = "Content action", Bounds = new Rectangle(270, 230, 180, 35) };
            content.Controls.AddRange(new Control[] { edit, tree, progress, patch, action });
            using var backdrop = new Backdrop(!fallback) { Bounds = host.ClientRectangle, BackColor = fallback ? Color.Silver : Tint, Visible = false };
            using var card = new Panel { Bounds = new Rectangle(275, 45, 210, 145), BackColor = Color.White, Visible = false };
            using var cancel = new Button { Text = "Cancel", Bounds = new Rectangle(35, 75, 135, 35) };
            card.Controls.Add(new Label { Text = "Busy — opaque card", AutoSize = true, Location = new Point(10, 15) });
            using var spinner = new BootstrapSpinner { Location = new Point(90, 40) };
            card.Controls.Add(spinner);
            card.Controls.Add(cancel);
            host.Controls.AddRange(new Control[] { content, backdrop, card });
            content.SendToBack(); backdrop.BringToFront(); card.BringToFront();
            scroll.Controls.Add(host);
            page.Controls.Add(scroll);
            form.Controls.Add(tabs);
            using var outside = new Button { Text = "Outside", Bounds = new Rectangle(675, 70, 100, 40) };
            form.Controls.Add(outside);
            var contentClicks = 0; var cancelClicks = 0; var outsideClicks = 0;
            action.Click += (_, _) => contentClicks++;
            cancel.Click += (_, _) => cancelClicks++;
            outside.Click += (_, _) => outsideClicks++;
            form.Show(); form.Activate();
            Wait(() => form.Visible && GetForegroundWindow() == form.Handle, "foreground activation");
            Paint(form);
            using (var settled = Capture(form, card)) settled.Save(Path.Combine(directory, "initial.png"), ImageFormat.Png);
            Click(action); Require(contentClicks == 1, "Uncovered Content action did not receive SendInput."); contentClicks = 0;
            var printHooks = new List<PrintHook>();
            Hook(content, printHooks, log);
            try
            {
                var filter = new MouseFilter(content);
                Application.AddMessageFilter(filter);
                try
                {
                    var showPrintBefore = PrintCount(printHooks);
                    if (!fallback) Require(SetLayeredWindowAttributes(backdrop.Handle, 0, 89, 2), "Initial alpha setup failed.");
                    backdrop.Visible = true; card.Visible = true;
                    content.SendToBack(); backdrop.BringToFront(); card.BringToFront(); Paint(form);
                    using (var shown = Capture(form, card)) AssertPatch(shown, form, patch, fallback ? Color.Silver : Blend(Patches[0], 89), "show");
                    Require(UnexpectedPrintCount(printHooks) == 0, "Overlay show invoked non-native Content print.");
                    log.WriteLine($"PASS overlay show explicit Content print=0; incidental native background delta={PrintCount(printHooks) - showPrintBefore}");
                    log.WriteLine($"DPI={GetDpiForWindow(form.Handle)}; Monitors={Screen.AllScreens.Length}; Generation=1; Mode={(fallback ? "OpaqueFallback probe" : "NativeLayered probe")}");
                    LogWindow(log, "host", host); LogWindow(log, "content", content); LogWindow(log, "backdrop", backdrop); LogWindow(log, "card", card);
                    Require((ClassStyle(backdrop.Handle) & 0x60) == 0, "Backdrop class owns a DC.");
                    Require((WindowStyle(backdrop.Handle, -20) & 0x80000) == (fallback ? 0 : 0x80000), "Incorrect layered style.");
                    Require((WindowStyle(backdrop.Handle, -20) & 0x20) == 0, "Unexpected WS_EX_TRANSPARENT.");
                    Require(GetParent(backdrop.Handle) == host.Handle && GetParent(card.Handle) == host.Handle && GetParent(content.Handle) == host.Handle, "Not a sibling child hierarchy.");
                    Require(GetWindow(card.Handle, 2) == backdrop.Handle && GetWindow(backdrop.Handle, 2) == content.Handle, "Incorrect physical HWND z-order.");
                    var overlayHandle = backdrop.Handle;
                    foreach (var alpha in new byte[] { 0, 89, 255 })
                    {
                        var printBefore = PrintCount(printHooks);
                        if (!fallback) Require(SetLayeredWindowAttributes(backdrop.Handle, 0, alpha, 2), $"SetLayeredWindowAttributes failed: {Marshal.GetLastWin32Error()}");
                        Paint(form);
                        using (var capture = Capture(form, card))
                        {
                            capture.Save(Path.Combine(directory, $"alpha-{alpha}.png"), ImageFormat.Png);
                            AssertPatch(capture, form, patch, fallback ? Color.Silver : Blend(Patches[0], alpha), $"alpha {alpha}");
                            AssertPoint(capture, form, card.PointToScreen(new Point(5, 130)), Color.White, "opaque card");
                        }
                        Require(PrintCount(printHooks) == printBefore, "Overlay alpha update invoked Content print.");
                        Click(action); Click(cancel); Click(outside);
                        if (!fallback && alpha == 0) Require(filter.Blocked >= 2, "Alpha-zero queued mouse did not exercise the scoped filter.");
                        Require(contentClicks == 0 && cancelClicks == Array.IndexOf(new byte[] { 0, 89, 255 }, alpha) + 1 && outsideClicks == cancelClicks, "Queued input routing failed.");
                        log.WriteLine($"PASS alpha={alpha}; ContentClicks={contentClicks}; CancelClicks={cancelClicks}; OutsideClicks={outsideClicks}; FilterBlocked={filter.Blocked}");
                    }
                    if (!fallback) Require(SetLayeredWindowAttributes(backdrop.Handle, 0, 89, 2), "Cannot restore alpha.");
                    Bitmap? previousLive = null;
                    try
                    {
                        for (var i = 0; i < 4; i++)
                        {
                            edit.Text = $"Live {i}"; tree.Nodes[0].Text = $"Live {i}"; progress.Value = 10 + i * 25; patch.BackColor = Patches[i];
                            Paint(form);
                            using var capture = Capture(form, card);
                            capture.Save(Path.Combine(directory, $"live-{i}.png"), ImageFormat.Png);
                            AssertPatch(capture, form, patch, fallback ? Color.Silver : Blend(Patches[i], 89), $"live {i}");
                            if (!fallback && previousLive != null)
                            {
                                var editPixels = Changes(previousLive, capture, form, edit);
                                var treePixels = Changes(previousLive, capture, form, tree);
                                var progressPixels = Changes(previousLive, capture, form, progress);
                                Require(editPixels > 2, "Native TextBox pixels did not update under tint.");
                                Require(treePixels > 2, "Native TreeView pixels did not update under tint.");
                                Require(progressPixels > 50, "Native progress pixels did not update under tint.");
                                log.WriteLine($"PASS live region changes edit={editPixels}; tree={treePixels}; progress={progressPixels}; card/spinner excluded");
                            }
                            previousLive?.Dispose(); previousLive = (Bitmap)capture.Clone();
                            Require(backdrop.Visible && backdrop.Handle == overlayHandle, "Overlay changed during live generation 1.");
                            log.WriteLine($"PASS live-{i}; Text={edit.Text}; Tree={tree.Nodes[0].Text}; Progress={progress.Value}; HWND={overlayHandle}; Visible={backdrop.Visible}");
                        }
                    }
                    finally { previousLive?.Dispose(); }
                    log.WriteLine($"PASS overlay alpha update Content print delta=0; incidental native content print total={PrintCount(printHooks)} (native themed editor/background redraw; see trace)");
                    Require(UnexpectedPrintCount(printHooks) == 0, "Unexpected Content print path.");
                    // A higher sibling must cover all three owned children.
                    using (var higher = new Panel { Bounds = new Rectangle(300, 100, 100, 160), BackColor = Color.Magenta })
                    {
                        page.Controls.Add(higher); higher.BringToFront(); Paint(form);
                        using var capture = Capture(form, card);
                        AssertPoint(capture, form, higher.PointToScreen(new Point(20, 20)), Color.Magenta, "higher sibling");
                        capture.Save(Path.Combine(directory, "higher-sibling.png"), ImageFormat.Png);
                    }
                    log.WriteLine("PASS higher overlapping sibling");
                    var outsideClip = scroll.PointToScreen(new Point(30, scroll.Height + 4));
                    Color clipSentinel;
                    using (var beforeClip = Capture(form, card)) { var p = form.PointToClient(outsideClip); clipSentinel = beforeClip.GetPixel(p.X, p.Y); }
                    host.Height = 650; backdrop.Bounds = host.ClientRectangle;
                    scroll.AutoScrollPosition = new Point(0, 110); Paint(form);
                    using (var capture = Capture(form, card))
                    {
                        capture.Save(Path.Combine(directory, "ancestor-scroll.png"), ImageFormat.Png);
                        AssertPoint(capture, form, outsideClip, clipSentinel, "ancestor clipping sentinel");
                    }
                    Require(WindowFromPoint(outsideClip) != backdrop.Handle, "Backdrop escaped ancestor clipping.");
                    log.WriteLine("PASS ancestor scroll/clip hit-test; capture retained");
                    scroll.AutoScrollPosition = Point.Empty; host.Height = 340; backdrop.Bounds = host.ClientRectangle;
                    tabs.SelectedIndex = 1; Paint(form); Require(!backdrop.Visible, "Backdrop remains visible on hidden page.");
                    tabs.SelectedIndex = 0; Paint(form); Require(backdrop.Visible, "Backdrop did not restore with page.");
                    log.WriteLine("PASS TabPage hide/show");
                    using (var modal = new Form { Text = "Bounded owned modal", StartPosition = FormStartPosition.CenterParent, ClientSize = new Size(260, 100), BackColor = Color.Coral })
                    using (var timer = new Timer { Interval = 750 })
                    {
                        var closed = false; Exception? modalError = null;
                        timer.Tick += (_, _) =>
                        {
                            timer.Stop();
                            try { using var capture = CaptureNow(form); AssertPoint(capture, form, modal.PointToScreen(new Point(100, 50)), Color.Coral, "owned modal above overlay"); capture.Save(Path.Combine(directory, "owned-modal.png")); }
                            catch (Exception error) { modalError = error; }
                            finally { closed = true; modal.Close(); }
                        };
                        timer.Start(); modal.ShowDialog(form); timer.Stop(); Require(closed, "Modal auto-close did not execute.");
                        if (modalError != null) throw modalError;
                    }
                    log.WriteLine("PASS owned modal with bounded auto-close");
                    form.WindowState = FormWindowState.Minimized; Wait(() => IsIconic(form.Handle), "minimize");
                    form.WindowState = FormWindowState.Normal; form.Activate(); Wait(() => !IsIconic(form.Handle) && GetForegroundWindow() == form.Handle, "restore");
                    Paint(form);
                    using (var capture = Capture(form, card)) AssertPatch(capture, form, patch, fallback ? Color.Silver : Blend(Patches[3], 89), "restore");
                    log.WriteLine("PASS minimize/restore composition");
                    var dpis = new HashSet<uint>();
                    foreach (var screen in Screen.AllScreens)
                    {
                        form.Location = new Point(screen.WorkingArea.Left + 40, screen.WorkingArea.Top + 40); Paint(form);
                        var dpi = GetDpiForWindow(form.Handle); dpis.Add(dpi); log.WriteLine($"Monitor={screen.DeviceName}; DPI={dpi}");
                        using var moved = Capture(form, card);
                        AssertPatch(moved, form, patch, fallback ? Color.Silver : Blend(Patches[3], 89), "monitor move composition");
                        AssertPoint(moved, form, card.PointToScreen(new Point(5, 130)), Color.White, "monitor move opaque card");
                    }
                    log.WriteLine(dpis.Count > 1 ? "PASS mixed-DPI move" : "NOT RUN mixed-DPI: no distinct physical monitor DPI available");
                    Require(UnexpectedPrintCount(printHooks) == 0, "Unexpected print path during lifecycle proof.");
                    log.WriteLine($"PASS scenarios; WM_PRINT={PrintCount(printHooks)}; FilterBlocked={filter.Blocked}; NativeLiveGate={(fallback ? "NOT APPLICABLE" : "PASS")}");
                }
                finally { Application.RemoveMessageFilter(filter); }
            }
            finally { foreach (var hook in printHooks) hook.ReleaseHandle(); }
            return 0;
        }
        catch (Exception error) { log.WriteLine("BLOCKED " + error); return 1; }
    }

    private static bool QueryExecutable(TextWriter log)
    {
        var module = GetModuleHandle(null);
        var resource = FindResource(module, (IntPtr)1, (IntPtr)24);
        Require(resource != IntPtr.Zero, "No embedded executable manifest resource 1.");
        var manifest = Marshal.PtrToStringAnsi(LockResource(LoadResource(module, resource)), checked((int)SizeofResource(module, resource))) ?? "";
        log.WriteLine("EmbeddedManifest=" + manifest.Replace("\r", "").Replace("\n", ""));
        // HMODULE selects the executable context explicitly, never the active visual-style thread context.
        QueryActCtxW(8, module, IntPtr.Zero, 6, IntPtr.Zero, UIntPtr.Zero, out var size);
        var error = Marshal.GetLastWin32Error();
        Require(error == 122 && size.ToUInt64() >= 4 && size.ToUInt64() <= 65536, $"QueryActCtx size protocol error={error}, size={size}");
        var buffer = Marshal.AllocHGlobal(checked((int)size.ToUInt64()));
        try
        {
            Require(QueryActCtxW(8, module, IntPtr.Zero, 6, buffer, size, out var written), $"QueryActCtx error={Marshal.GetLastWin32Error()}");
            var count = Marshal.ReadInt32(buffer);
            var offset = Marshal.OffsetOf<CompatibilityInformation>(nameof(CompatibilityInformation.First)).ToInt32();
            var stride = Marshal.SizeOf<CompatibilityElement>();
            log.WriteLine($"QueryActCtx(EXE HMODULE) bytes={written}; count={count}; offset={offset}; stride={stride}");
            Require(count >= 0 && (ulong)offset + (ulong)count * (ulong)stride <= written.ToUInt64(), "Compatibility elements outside native buffer.");
            var supported = false;
            for (var i = 0; i < count; i++)
            {
                var entry = Marshal.PtrToStructure<CompatibilityElement>(IntPtr.Add(buffer, offset + i * stride));
                log.WriteLine($"Compatibility[{i}]={entry.Id}; Type={entry.Type}; MaxVersion={entry.MaxVersion}");
                supported |= entry.Type == 1 && (entry.Id == Windows10 || entry.Id == new Guid("4a2f28e3-53b9-4441-ba9c-d69d4a4a6e38") || entry.Id == new Guid("1f676c76-80e1-4239-95bb-83d0f6d0da78"));
            }
            Require(supported == manifest.Contains("supportedOS"), "Embedded manifest and compatibility context disagree.");
            return supported;
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private static void Hook(Control control, List<PrintHook> hooks, TextWriter log)
    {
        var hook = new PrintHook(control.GetType().Name, log); hook.AssignHandle(control.Handle); hooks.Add(hook);
        foreach (Control child in control.Controls) Hook(child, hooks, log);
    }
    private static int PrintCount(List<PrintHook> hooks) { var count = 0; foreach (var hook in hooks) count += hook.Count; return count; }
    private static int UnexpectedPrintCount(List<PrintHook> hooks) { var count = 0; foreach (var hook in hooks) count += hook.Unexpected; return count; }
    private static void LogWindow(TextWriter log, string name, Control control) => log.WriteLine($"{name}: HWND={control.Handle}; Parent={GetParent(control.Handle)}; Style=0x{WindowStyle(control.Handle, -16):X}; ExStyle=0x{WindowStyle(control.Handle, -20):X}; ClassStyle=0x{ClassStyle(control.Handle):X}; Visible={control.Visible}");
    private static long WindowStyle(IntPtr window, int index) => IntPtr.Size == 8 ? GetWindowLongPtr(window, index).ToInt64() : GetWindowLong(window, index);
    private static uint ClassStyle(IntPtr window) => IntPtr.Size == 8 ? unchecked((uint)GetClassLongPtr(window, -26).ToUInt64()) : GetClassLong(window, -26);
    private static void Paint(Control control)
    {
        control.Update();
        var watch = Stopwatch.StartNew(); var stable = 0;
        while (watch.ElapsedMilliseconds < 2000)
        {
            Application.DoEvents();
            Require(DwmFlush() == 0, "DwmFlush failed.");
            if (GetQueueStatus(0x20) == 0) { if (++stable >= 3) return; } else stable = 0;
        }
        throw new TimeoutException("Paint/composition did not settle in 2 seconds.");
    }
    private static void Wait(Func<bool> condition, string name)
    {
        var watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < 3000) { Application.DoEvents(); if (condition()) return; System.Threading.Thread.Sleep(5); }
        throw new TimeoutException(name);
    }
    private static Bitmap Capture(Form form, Control? animationMask = null)
    {
        // Show/restore animations can outlive an empty WinForms paint queue. Require
        // desktop pixels to settle across composition frames, with a finite deadline.
        var watch = Stopwatch.StartNew(); var previous = 0L; var stable = 0;
        while (watch.ElapsedMilliseconds < 3000)
        {
            Application.DoEvents(); Require(DwmFlush() == 0, "Capture DwmFlush failed.");
            using var sample = CaptureNow(form);
            var mask = animationMask == null ? Rectangle.Empty : new Rectangle(form.PointToClient(animationMask.PointToScreen(Point.Empty)), animationMask.Size);
            long hash = 17;
            for (var y = 0; y < sample.Height; y += 13)
            for (var x = 0; x < sample.Width; x += 13)
                if (!mask.Contains(x, y)) hash = unchecked(hash * 31 + sample.GetPixel(x, y).ToArgb());
            stable = previous == hash ? stable + 1 : 0; previous = hash;
            if (stable >= 5 && watch.ElapsedMilliseconds >= 300) return CaptureNow(form);
            System.Threading.Thread.Sleep(30);
        }
        throw new TimeoutException("Desktop composition did not stabilize in 3 seconds.");
    }
    private static Bitmap CaptureNow(Form form)
    {
        var bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(form.PointToScreen(Point.Empty), Point.Empty, bitmap.Size, CopyPixelOperation.SourceCopy);
        return bitmap;
    }
    private static Color Blend(Color color, byte alpha) => Color.FromArgb((color.R * (255 - alpha) + Tint.R * alpha + 127) / 255, (color.G * (255 - alpha) + Tint.G * alpha + 127) / 255, (color.B * (255 - alpha) + Tint.B * alpha + 127) / 255);
    private static int Changes(Bitmap first, Bitmap second, Form form, Control control)
    {
        var origin = form.PointToClient(control.PointToScreen(Point.Empty)); var count = 0;
        // Exclude border/caret slot and all foreground/card pixels.
        for (var y = origin.Y + 4; y < origin.Y + control.Height - 4; y++)
        for (var x = origin.X + 4; x < origin.X + control.Width - 8; x++)
            if (first.GetPixel(x, y).ToArgb() != second.GetPixel(x, y).ToArgb()) count++;
        return count;
    }
    private static void AssertPatch(Bitmap image, Form form, Control patch, Color color, string name)
    {
        // Only this fixed flat patch is sampled; foreground/spinner and all other regions are excluded.
        for (var y = 10; y < 50; y += 10)
        for (var x = 10; x < 150; x += 10) AssertPoint(image, form, patch.PointToScreen(new Point(x, y)), color, name);
    }
    private static void AssertPoint(Bitmap image, Form form, Point screen, Color expected, string name)
    {
        var point = form.PointToClient(screen); var actual = image.GetPixel(point.X, point.Y);
        Require(Math.Abs(actual.R - expected.R) <= 3 && Math.Abs(actual.G - expected.G) <= 3 && Math.Abs(actual.B - expected.B) <= 3, $"{name}: pixel {point}: expected={expected}, actual={actual}");
    }
    private static void Click(Control control)
    {
        var position = Cursor.Position;
        try
        {
            Cursor.Position = control.PointToScreen(new Point(control.Width / 2, control.Height / 2));
            var input = new[] { new Input { Type = 0, Mouse = new MouseInput { Flags = 2 } }, new Input { Type = 0, Mouse = new MouseInput { Flags = 4 } } };
            Require(SendInput(2, input, Marshal.SizeOf<Input>()) == 2, $"SendInput error={Marshal.GetLastWin32Error()}");
            var watch = Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < 100) { Application.DoEvents(); System.Threading.Thread.Sleep(2); }
        }
        finally { Cursor.Position = position; }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class Backdrop : Control
    {
        private readonly bool layered;
        internal Backdrop(bool layered) { this.layered = layered; SetStyle(ControlStyles.Opaque, true); }
        protected override CreateParams CreateParams { get { var value = base.CreateParams; value.Style |= 0x04000000; if (layered) value.ExStyle |= 0x80000; return value; } }
        protected override void OnPaint(PaintEventArgs e) => e.Graphics.Clear(BackColor);
    }
    private sealed class PrintHook : NativeWindow
    {
        internal int Count;
        internal int Unexpected;
        private readonly string name;
        private readonly TextWriter log;
        internal PrintHook(string name, TextWriter log) { this.name = name; this.log = log; }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x317 || m.Msg == 0x318)
            {
                Count++; var stack = Environment.StackTrace;
                // Windows themed children ask their parent for a background during
                // ordinary redraw too. Preserve these counts/traces; never report total zero.
                if (m.Msg != 0x318 || (!stack.Contains("TextBox.WndProc") && !stack.Contains("DrawThemeParentBackground"))) Unexpected++;
                log.WriteLine($"PRINT {name} HWND={m.HWnd} Msg=0x{m.Msg:X} Flags=0x{m.LParam.ToInt64():X} Stack={stack}");
            }
            base.WndProc(ref m);
        }
    }
    private sealed class MouseFilter : IMessageFilter
    {
        private readonly Control content;
        internal int Blocked;
        internal MouseFilter(Control content) { this.content = content; }
        public bool PreFilterMessage(ref Message m)
        {
            var mouse = (m.Msg >= 0x200 && m.Msg <= 0x20E) || (m.Msg >= 0xA0 && m.Msg <= 0xAD);
            if (!mouse || !content.IsHandleCreated || (m.HWnd != content.Handle && !IsChild(content.Handle, m.HWnd))) return false;
            Blocked++; return true;
        }
    }
    [StructLayout(LayoutKind.Sequential)] private struct CompatibilityElement { internal Guid Id; internal int Type; internal ulong MaxVersion; }
    [StructLayout(LayoutKind.Sequential)] private struct CompatibilityInformation { internal uint Count; internal CompatibilityElement First; }
    [StructLayout(LayoutKind.Sequential)] private struct Input { internal uint Type; internal MouseInput Mouse; }
    [StructLayout(LayoutKind.Sequential)] private struct MouseInput { internal int X; internal int Y; internal uint Data; internal uint Flags; internal uint Time; internal UIntPtr Extra; }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string? name);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool QueryActCtxW(uint flags, IntPtr context, IntPtr sub, uint info, IntPtr buffer, UIntPtr size, out UIntPtr required);
    [DllImport("kernel32.dll")] private static extern IntPtr FindResource(IntPtr module, IntPtr name, IntPtr type);
    [DllImport("kernel32.dll")] private static extern IntPtr LoadResource(IntPtr module, IntPtr resource);
    [DllImport("kernel32.dll")] private static extern IntPtr LockResource(IntPtr resource);
    [DllImport("kernel32.dll")] private static extern uint SizeofResource(IntPtr module, IntPtr resource);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetLayeredWindowAttributes(IntPtr window, uint key, byte alpha, uint flags);
    [DllImport("user32.dll")] private static extern IntPtr GetParent(IntPtr window);
    [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr window, uint command);
    [DllImport("user32.dll")] private static extern bool IsChild(IntPtr parent, IntPtr child);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(Point point);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern int GetWindowLong(IntPtr window, int index);
    [DllImport("user32.dll", EntryPoint = "GetClassLongPtrW")] private static extern UIntPtr GetClassLongPtr(IntPtr window, int index);
    [DllImport("user32.dll", EntryPoint = "GetClassLongW")] private static extern uint GetClassLong(IntPtr window, int index);
    [DllImport("user32.dll")] private static extern uint GetQueueStatus(uint flags);
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, Input[] inputs, int size);
    [DllImport("dwmapi.dll")] private static extern int DwmFlush();
}
