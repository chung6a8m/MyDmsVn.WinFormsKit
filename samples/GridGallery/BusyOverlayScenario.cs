using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.BootstrapSourceGrid.Demo;

// Proof-only route in the exact manifested gallery executable; production never captures content.
internal static class BusyOverlayScenario
{
    private static readonly Stopwatch Deadline = new Stopwatch();
    internal static int Run(string directory, string themeName, bool fallback)
    {
        Directory.CreateDirectory(directory);
        using var log = new StreamWriter(Path.Combine(directory, "result.txt"), false, new UTF8Encoding(false)) { AutoFlush = true };
        var original = BootstrapThemeManager.CurrentTheme;
        Deadline.Restart();
        try
        {
            var mode = (BootstrapThemeMode)Enum.Parse(typeof(BootstrapThemeMode), themeName);
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode);
            using var form = new BusyOverlayDemoForm();
            // This exact-EXE desktop proof needs foreground input even when its
            // NUnit parent is background. Raise only this proof window and give
            // it real pointer activation; never change foreground-lock policy.
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(100, 100);
            form.Show(); form.Activate(); form.StopClock();
            Pump(100);
            EnsureForeground(form);
            log.WriteLine($"Activation HWND={form.Handle}; foreground={GetForegroundWindow()}; nativeVisible={IsWindowVisible(form.Handle)}; bounds={form.Bounds}");
            var foregroundTitle = new StringBuilder(256); GetWindowText(GetForegroundWindow(), foregroundTitle, 256);
            log.WriteLine("Foreground title=" + foregroundTitle);
            using (var activation = new Bitmap(form.ClientSize.Width, form.ClientSize.Height))
            {
                using (var graphics = Graphics.FromImage(activation)) graphics.CopyFromScreen(form.PointToScreen(Point.Empty), Point.Empty, activation.Size);
                activation.Save(Path.Combine(directory, "activation.png"), ImageFormat.Png);
            }
            Wait(() => GetForegroundWindow() == form.Handle, "foreground");
            log.WriteLine($"EXE={Process.GetCurrentProcess().MainModule!.FileName}; OS={Environment.OSVersion}; CLR={Environment.Version}; x64={Environment.Is64BitProcess}; Theme={mode}; DPI={GetDpiForWindow(form.Handle)}; PMv2={AreDpiAwarenessContextsEqual(GetWindowDpiAwarenessContext(form.Handle), new IntPtr(-4))}");
            var module = GetModuleHandle(null);
            var resource = FindResource(module, new IntPtr(1), new IntPtr(24));
            Require(resource != IntPtr.Zero, "Embedded EXE manifest missing");
            var manifestBytes = new byte[checked((int)SizeofResource(module, resource))];
            Marshal.Copy(LockResource(LoadResource(module, resource)), manifestBytes, 0, manifestBytes.Length);
            var manifest = Encoding.UTF8.GetString(manifestBytes);
            Require(manifest.Contains("Microsoft.Windows.Common-Controls"), "Embedded common-controls dependency missing");
            File.WriteAllText(Path.Combine(directory, "embedded.manifest"), manifest, new UTF8Encoding(false));
            var hasSupportedOs = manifest.Contains("8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a");
            Require(hasSupportedOs != fallback, "Unexpected embedded supportedOS declaration");
            log.WriteLine($"PASS embedded manifest supportedOS={hasSupportedOs}; commonControls={manifest.Contains("Microsoft.Windows.Common-Controls")}; executable capability verified by strict actual RenderMode below");
            // The deliberately unsupported negative EXE can also lose net48's
            // App.config DPI opt-in through OS-version compatibility virtualization.
            // It proves fallback only; the positive executable must be PMv2.
            Require(fallback || AreDpiAwarenessContextsEqual(GetWindowDpiAwarenessContext(form.Handle), new IntPtr(-4)), "Positive gallery is not PerMonitorV2");
            var grid = form.Grid;
            // A native engine popup is genuinely open before the caller cancels it.
            var comboPosition = new SourceGrid.Position(1, 2);
            Require(grid.Selection.Focus(comboPosition, true), "focus combo");
            var comboContext = new SourceGrid.CellContext(grid, comboPosition, grid[comboPosition]);
            comboContext.StartEdit(); form.ChoiceEditor.Control.DroppedDown = true; Pump(100);
            Require(form.ChoiceEditor.Control.DroppedDown && comboContext.IsEditing(), "native popup did not open");
            Require(form.StartOperation(10000, "Đang cập nhật SourceGrid…"), "begin rejected");
            Require(!comboContext.IsEditing() && !form.ChoiceEditor.Control.DroppedDown, "Caller failed to close editor/popup before busy");
            Require(!grid.Enabled && !form.TryApply(), "Pending caller admission");
            Wait(() => form.Host.RenderMode != BootstrapBusyOverlayRenderMode.None, "presentation");
            Require(form.Host.RenderMode == (fallback ? BootstrapBusyOverlayRenderMode.OpaqueFallback : BootstrapBusyOverlayRenderMode.NativeLayered), "RenderMode mismatch");
            VerifyUiaAdmission(form, log);
            EnsureForeground(form);
            Require(GetForegroundWindow() == form.Handle, "Keyboard target lost foreground");
            SendKeys.SendWait("^s"); Pump(50);
            Require(form.AppliedCount == 0, "Busy keyboard command admitted");
            var backdrop = form.Host.Controls.Cast<Control>().Single(c => c.GetType().Name == "BootstrapBusyLayeredBackdrop");
            var card = form.Host.Controls.Cast<Control>().Single(c => c != form.Host.Content && c != backdrop);
            var cancel = card.Controls.OfType<Button>().Single();
            var handle = backdrop.Handle;
            log.WriteLine($"PASS popup/edit closed; pending/visible admission; Mode={form.Host.RenderMode}; BackdropHWND={handle}; GridHWND={grid.Handle}");
            foreach (var alpha in new[] { 0, 35, 100 })
            {
                form.OpacitySlider.Value = alpha;
                var selected = grid.Selection.ActivePosition;
                var scroll = grid.CustomScrollPosition;
                var value = grid[1, 1].Value;
                var target = grid.PositionToRectangle(new SourceGrid.Position(3, 1));
                Click(grid, new Point(target.Left + 15, target.Top + 10));
                Mouse(grid.PointToScreen(new Point(100, 100)), 0x800, unchecked((uint)-120));
                Click(grid.VScrollBar, new Point(8, grid.VScrollBar.Height - 8));
                Require(grid.Selection.ActivePosition == selected && grid.CustomScrollPosition == scroll && Equals(grid[1, 1].Value, value), "Grid selection/edit/scroll changed under overlay");
                Require(!form.TextEditor.IsEditing && !form.ChoiceEditor.IsEditing && !form.TryApply(), "Visible edit/admission");
                using (var capture = Capture(form, card)) capture.Save(Path.Combine(directory, "alpha-" + alpha + ".png"), ImageFormat.Png);
                // Outside sibling theme/opacity controls and the real native Cancel remain usable.
                var oldTheme = BootstrapThemeManager.CurrentTheme.Mode;
                var themeButton = form.Controls.Find(oldTheme == BootstrapThemeMode.Light ? "busyDark" : "busyLight", true).Single();
                Click(themeButton);
                Require(BootstrapThemeManager.CurrentTheme.Mode != oldTheme, "Outside theme button blocked");
                Click(cancel);
                Require(form.OperationCount == 0 && grid.Enabled, "Native Cancel did not finish caller operation");
                Require(form.StartOperation(10000, "Đang cập nhật SourceGrid…"), "restart");
                Wait(() => form.Host.RenderMode != BootstrapBusyOverlayRenderMode.None, "reshow");
                log.WriteLine($"PASS SendInput alpha={alpha}; selection/edit/scroll blocked; Cancel/outside usable; caller admission enabled only after release");
            }
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode);
            form.OpacitySlider.Value = 35;
            handle = backdrop.Handle;
            var marker = grid.PositionToRectangle(new SourceGrid.Position(1, 0));
            marker = new Rectangle(form.PointToClient(grid.PointToScreen(marker.Location)), marker.Size);
            Require(!marker.IntersectsWith(new Rectangle(form.PointToClient(card.PointToScreen(Point.Empty)), card.Size)), "Marker intersects foreground");
            using (var before = Capture(form, card))
            {
                before.Save(Path.Combine(directory, "live-before.png"), ImageFormat.Png);
                form.AdvanceContent();
                using var after = Capture(form, card);
                after.Save(Path.Combine(directory, "live-after.png"), ImageFormat.Png);
                var pixels = Changes(before, after, marker);
                Require(fallback || pixels > 20, "SourceGrid marker did not visibly change beneath same live overlay");
                Require(backdrop.Handle == handle && backdrop.Visible && form.OperationCount == 1, "Live overlay token/HWND changed");
                log.WriteLine($"PASS same-token-live; Marker={grid[1, 0].Value}; ChangedPixels={pixels}; HWND={handle}; Visible={backdrop.Visible}; Progress={form.Progress.Value}; LivePixelGate={!fallback}");
            }
            form.Host.Visible = false; Require(form.Host.RenderMode == BootstrapBusyOverlayRenderMode.None, "hide");
            form.Host.Visible = true; Wait(() => form.Host.RenderMode != BootstrapBusyOverlayRenderMode.None, "restore");
            Require(form.OperationCount == 1, "hide/show ended token");
            form.CancelAll();
            var textPosition = new SourceGrid.Position(1, 1);
            Require(grid.Selection.Focus(textPosition, true), "reedit focus");
            var textContext = new SourceGrid.CellContext(grid, textPosition, grid[textPosition]);
            textContext.StartEdit(); Require(textContext.IsEditing(), "reedit failed");
            form.TextEditor.BootstrapControl.Text = "Sau busy";
            Require(textContext.EndEdit(false) && Equals(grid[1, 1].Value, "Sau busy"), "reedit commit");
            log.WriteLine("PASS hide/show same operation; edit and commit after release");
            form.Close();
            log.WriteLine($"PASS cleanup; disposed={form.IsDisposed}; elapsedMs={Deadline.ElapsedMilliseconds}; physical125/hardware/Owner=NOT RUN");
            return 0;
        }
        catch (Exception error) { log.WriteLine("FAIL " + error); return 1; }
        finally { BootstrapThemeManager.CurrentTheme = original; }
    }
    private static void Check() { if (Deadline.ElapsedMilliseconds > 45000) throw new TimeoutException("SourceGrid scenario 45s deadline"); }
    private static void EnsureForeground(Form form)
    {
        if (GetForegroundWindow() == form.Handle) return;
        try
        {
            form.TopMost = true;
            ClickAt(new Point(form.Left + form.Width / 2, form.Top + 10));
            Wait(() => GetForegroundWindow() == form.Handle, "titlebar activation");
        }
        finally { form.TopMost = false; }
    }
    private static void Wait(Func<bool> condition, string name)
    {
        var watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < 3000) { Check(); Application.DoEvents(); if (condition()) return; Thread.Sleep(5); }
        throw new TimeoutException(name);
    }
    private static void Pump(int milliseconds)
    {
        var watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < milliseconds) { Check(); Application.DoEvents(); Thread.Sleep(5); }
    }
    private static Bitmap Capture(BusyOverlayDemoForm form, Control card)
    {
        // Require stable desktop frames excluding only the animated opaque card.
        long previous = 0; var stable = 0; var watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < 3000)
        {
            Pump(35); Require(DwmFlush() == 0, "DwmFlush");
            var sample = new Bitmap(form.ClientSize.Width, form.ClientSize.Height);
            using (var graphics = Graphics.FromImage(sample)) graphics.CopyFromScreen(form.PointToScreen(Point.Empty), Point.Empty, sample.Size);
            var mask = new Rectangle(form.PointToClient(card.PointToScreen(Point.Empty)), card.Size);
            long hash = 17;
            for (var y = 0; y < sample.Height; y += 13)
            for (var x = 0; x < sample.Width; x += 13)
                if (!mask.Contains(x, y)) hash = unchecked(hash * 31 + sample.GetPixel(x, y).ToArgb());
            stable = hash == previous ? stable + 1 : 0; previous = hash;
            if (stable >= 5 && watch.ElapsedMilliseconds >= 300) return sample;
            sample.Dispose();
        }
        throw new TimeoutException("Desktop did not settle");
    }
    private static int Changes(Bitmap first, Bitmap second, Rectangle area)
    {
        var count = 0;
        for (var y = area.Top + 4; y < area.Bottom - 4; y++)
        for (var x = area.Left + 4; x < area.Right - 4; x++)
            if (first.GetPixel(x, y) != second.GetPixel(x, y)) count++;
        return count;
    }
    private static void Click(Control control, Point? point = null)
    {
        var screen = control.PointToScreen(point ?? new Point(control.Width / 2, control.Height / 2));
        ClickAt(screen);
    }
    private static void ClickAt(Point screen)
    {
        var previous = Cursor.Position;
        try
        {
            Cursor.Position = screen;
            var input = new[] { new Input { Mouse = new MouseInput { Flags = 2 } }, new Input { Mouse = new MouseInput { Flags = 4 } } };
            Require(SendInput(2, input, Marshal.SizeOf(typeof(Input))) == 2, "SendInput click failed");
            Pump(100);
        }
        finally { Cursor.Position = previous; }
    }
    private static void Mouse(Point point, uint flags, uint data)
    {
        var previous = Cursor.Position;
        try
        {
            Cursor.Position = point;
            var input = new[] { new Input { Mouse = new MouseInput { Flags = flags, Data = data } } };
            Require(SendInput(1, input, Marshal.SizeOf(typeof(Input))) == 1, "SendInput failed"); Pump(100);
        }
        finally { Cursor.Position = previous; }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void VerifyUiaAdmission(BusyOverlayDemoForm form, TextWriter log)
    {
        var button = form.Controls.Find("busyApply", true).Single();
        var script = "$ErrorActionPreference='Stop'; Add-Type -AssemblyName UIAutomationClient; " +
            "$element=[System.Windows.Automation.AutomationElement]::FromHandle([IntPtr]" + button.Handle.ToInt64() + "); " +
            "if ($element.Current.IsEnabled) { throw 'Busy command UIA IsEnabled is true' }; " +
            "$pattern=$null; if ($element.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern,[ref]$pattern)) { " +
            "try { $pattern.Invoke(); 'UIA disabled command: Invoke returned; business guard checked by host process' } catch [System.Windows.Automation.ElementNotEnabledException] { 'UIA disabled command: provider rejected Invoke' } " +
            "} else { 'UIA disabled command: InvokePattern unavailable' }";
        using var process = Process.Start(new ProcessStartInfo("powershell.exe", "-NoProfile -NonInteractive -EncodedCommand " + Convert.ToBase64String(Encoding.Unicode.GetBytes(script)))
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true })!;
        try
        {
            Wait(() => process.HasExited, "UIA caller admission");
            log.WriteLine(process.StandardOutput.ReadToEnd());
            Require(process.ExitCode == 0, process.StandardError.ReadToEnd());
            Require(form.AppliedCount == 0, "UIA command changed business state");
            log.WriteLine("PASS UIA IsEnabled=false; provider admission inspected; business state unchanged");
        }
        finally { if (!process.HasExited) { process.Kill(); process.WaitForExit(5000); } }
    }
    [StructLayout(LayoutKind.Sequential)] private struct Input { internal uint Type; internal MouseInput Mouse; }
    [StructLayout(LayoutKind.Sequential)] private struct MouseInput { internal int X, Y; internal uint Data, Flags, Time; internal UIntPtr Extra; }
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int count);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern IntPtr GetWindowDpiAwarenessContext(IntPtr window);
    [DllImport("user32.dll")] private static extern bool AreDpiAwarenessContextsEqual(IntPtr first, IntPtr second);
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, Input[] inputs, int size);
    [DllImport("dwmapi.dll")] private static extern int DwmFlush();
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string? name);
    [DllImport("kernel32.dll")] private static extern IntPtr FindResource(IntPtr module, IntPtr name, IntPtr type);
    [DllImport("kernel32.dll")] private static extern IntPtr LoadResource(IntPtr module, IntPtr resource);
    [DllImport("kernel32.dll")] private static extern IntPtr LockResource(IntPtr resource);
    [DllImport("kernel32.dll")] private static extern uint SizeofResource(IntPtr module, IntPtr resource);
}
