using System;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;
[TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
public sealed class BootstrapBusyOverlayNativeTests
{
    [Test]
    public void Cancel_IsKeyboardAndUiaInvokable_AndMayDisposeReentrantly()
    {
        using var form = new Form { ClientSize = new Size(500, 300) };
        using var host = new BootstrapBusyOverlayHost(new TextBox()) { Dock = DockStyle.Fill };
        form.Controls.Add(host); Activate(form);
        var calls = 0;
        using var token = host.Begin(new BusyPresentation("busy", () => calls++));
        PumpUntil(() => host.RenderMode != BootstrapBusyOverlayRenderMode.None);
        var cancel = host.Controls.Cast<Control>().SelectMany(c => c.Controls.Cast<Control>()).OfType<Button>().Single();
        host.Content.Focus(); SendKeys.SendWait("{TAB}"); PumpUntil(() => cancel.Focused);
        SendKeys.SendWait("+{TAB}"); PumpUntil(() => host.Content.Focused);
        SendKeys.SendWait("{TAB}"); SendKeys.SendWait(" "); PumpUntil(() => calls == 1);
        SendKeys.SendWait("{ENTER}"); PumpUntil(() => calls == 2);
        InvokeUia(cancel); PumpUntil(() => calls == 3);
        using var newest = host.Begin(new BusyPresentation("dispose", host.Dispose));
        InvokeUia(cancel); PumpUntil(() => host.IsDisposed);
        Assert.That(host.InputFilterInstalled, Is.False);
    }

    [TestCase(false), TestCase(true)]
    public void CallerKeyboardAndUiaAdmission_AppliesDuringPendingAndVisible(bool visible)
    {
        using var form = new Form { ClientSize = new Size(500, 300) };
        var content = new Panel(); var action = new Button { Text = "Command" }; var popup = new ComboBox { Top = 50 };
        popup.Items.Add("one"); content.Controls.AddRange(new Control[] { action, popup });
        var delay = new HeldDelay();
        using var host = new BootstrapBusyOverlayHost(content, delay) { Dock = DockStyle.Fill };
        form.Controls.Add(host); Activate(form);
        var commands = 0; var attempts = 0; var admissionClosed = false;
        action.Click += (_, _) => { attempts++; if (!admissionClosed) commands++; };
        popup.DroppedDown = true; Assert.That(popup.DroppedDown, Is.True);
        popup.DroppedDown = false; // Caller closes transient native popup before Begin.
        admissionClosed = true;
        using var token = host.Begin(new BusyPresentation("busy"));
        if (visible) { delay.Fire(); PumpUntil(() => host.RenderMode != BootstrapBusyOverlayRenderMode.None); }
        else Assert.That(host.RenderMode, Is.EqualTo(BootstrapBusyOverlayRenderMode.None));
        Application.DoEvents(); action.Focus(); Assert.That(action.Focused, Is.True); SendKeys.SendWait(" "); PumpUntil(() => attempts == 1);
        InvokeUia(action); PumpUntil(() => attempts == 2);
        Assert.That(attempts, Is.EqualTo(2), "Host must not silently filter keyboard/UIA commands");
        Assert.That(commands, Is.Zero); Assert.That(popup.DroppedDown, Is.False);
        Assert.That(host.RenderMode == BootstrapBusyOverlayRenderMode.None, Is.EqualTo(!visible));
        token.Dispose(); admissionClosed = false; InvokeUia(action); PumpUntil(() => commands == 1);
    }

    [TestCase(0), TestCase(.35), TestCase(1)]
    public void ShowBetweenRealMouseDownAndUp_DoesNotClickContent(double opacity)
    {
        using var form = new Form { ClientSize = new Size(500, 300) };
        var content = new Panel(); var action = new Button { Bounds = new Rectangle(5, 5, 100, 30) }; content.Controls.Add(action);
        var delay = new HeldDelay(); using var host = new BootstrapBusyOverlayHost(content, delay) { Dock = DockStyle.Fill, BackdropOpacity = opacity };
        form.Controls.Add(host); Activate(form);
        IDisposable? token = null; var clicks = 0; action.Click += (_, _) => clicks++;
        action.MouseDown += (_, _) => { token = host.Begin(new BusyPresentation("busy")); delay.Fire(); };
        var position = Cursor.Position;
        try
        {
            Cursor.Position = action.PointToScreen(new Point(10, 10));
            SendMouse(2); PumpUntil(() => host.RenderMode != BootstrapBusyOverlayRenderMode.None);
            Assert.That(action.Capture, Is.False); SendMouse(4);
            var until = Stopwatch.StartNew(); while (until.ElapsedMilliseconds < 100) { Application.DoEvents(); Thread.Sleep(2); }
            Assert.That(clicks, Is.Zero);
        }
        finally { SendMouse(4); var cleanup = Stopwatch.StartNew(); while (cleanup.ElapsedMilliseconds < 100) { Application.DoEvents(); Thread.Sleep(2); } token?.Dispose(); Cursor.Position = position; }
    }

    private sealed class HeldDelay : MyDmsVn.Bootstrap5WinFormUI.Controls.Internal.IBootstrapBusyDelay
    {
        private Action? pending;
        public void Schedule(TimeSpan delay, Action action) => pending = action;
        public void Cancel() => pending = null;
        public void Dispose() => pending = null;
        internal void Fire() { var action = pending; pending = null; action?.Invoke(); }
    }
    private static void SendMouse(uint flags)
    {
        var inputs = new[] { new Input { Mouse = new MouseInput { Flags = flags } } };
        Assert.That(SendInput(1, inputs, Marshal.SizeOf<Input>()), Is.EqualTo(1));
    }
    [StructLayout(LayoutKind.Sequential)] private struct Input { internal uint Type; internal MouseInput Mouse; }
    [StructLayout(LayoutKind.Sequential)] private struct MouseInput { internal int X; internal int Y; internal uint Data; internal uint Flags; internal uint Time; internal UIntPtr Extra; }
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint SendInput(uint count, Input[] inputs, int size);

    [Test]
    public void QueuedMouseFilter_LeavesKeyboardAndOutsideNonclientWheelUntouched()
    {
        using var form = new Form(); using var content = new Panel(); using var child = new TextBox(); using var outside = new TextBox();
        content.Controls.Add(child); form.Controls.AddRange(new Control[] { content, outside }); form.Show();
        using var filter = new MyDmsVn.Bootstrap5WinFormUI.Controls.Internal.BootstrapBusyOverlayInputFilter(content); filter.Install(); filter.Install();
        foreach (var message in new[] { 0x200, 0x201, 0x202, 0x203, 0x20a, 0x20e, 0xa0, 0xa1, 0xad })
        {
            var inside = Message.Create(child.Handle, message, IntPtr.Zero, IntPtr.Zero);
            var external = Message.Create(outside.Handle, message, IntPtr.Zero, IntPtr.Zero);
            Assert.That(filter.PreFilterMessage(ref inside), Is.True);
            Assert.That(filter.PreFilterMessage(ref external), Is.False);
        }
        var key = Message.Create(child.Handle, 0x100, IntPtr.Zero, IntPtr.Zero);
        Assert.That(filter.PreFilterMessage(ref key), Is.False);
        filter.Dispose(); var mouse = Message.Create(child.Handle, 0x201, IntPtr.Zero, IntPtr.Zero);
        Assert.That(filter.PreFilterMessage(ref mouse), Is.False);
    }

    private static void PumpUntil(Func<bool> condition)
    {
        var watch = Stopwatch.StartNew();
        while (!condition() && watch.ElapsedMilliseconds < 5000) { Application.DoEvents(); Thread.Sleep(5); }
        Assert.That(condition(), Is.True, "Native condition timed out after 5 seconds");
    }

    internal static void Activate(Form form)
    {
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(100, 100);
        form.Show(); form.BringToFront(); form.Activate(); Application.DoEvents();
        if (GetForegroundWindow() != form.Handle)
        {
            // Windows may deny programmatic activation after another process was foreground.
            // Establish the prerequisite with actual input, before installing busy handlers.
            var position = Cursor.Position;
            try
            {
                // Only the test fixture is raised for its activation click; remove before Begin.
                form.TopMost = true;
                Cursor.Position = new Point(form.Left + form.Width / 2, form.Top + 10);
                SendMouse(2); SendMouse(4); PumpUntil(() => GetForegroundWindow() == form.Handle);
            }
            finally { form.TopMost = false; Cursor.Position = position; }
        }
        Assert.That(GetForegroundWindow(), Is.EqualTo(form.Handle));
    }

    private static void InvokeUia(Control control)
    {
        // An out-of-process UIA client avoids deadlocking this STA provider.
        var command = MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.BusyOverlayUiaClient.Command(control.Handle);
        var encoded = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(command));
        using var process = Process.Start(new ProcessStartInfo("powershell.exe", "-NoProfile -NonInteractive -EncodedCommand " + encoded)
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true })!;
        try { PumpUntil(() => process.HasExited); TestContext.WriteLine(process.StandardOutput.ReadToEnd()); Assert.That(process.ExitCode, Is.Zero, process.StandardError.ReadToEnd()); }
        finally { if (!process.HasExited) { process.Kill(); process.WaitForExit(5000); } }
    }
    [TestCase(0), TestCase(.35), TestCase(1)]
    public void ZeroOpacity_BlocksQueuedContentMouseButAllowsCancelAndOutsideSibling(double opacity)
    {
        TestContext.WriteLine($"BUS-03 queued input process bits={IntPtr.Size * 8}");
        using var form = new Form { ClientSize = new Size(700, 400) };
        var content = new MouseProbe();
        using var host = new BootstrapBusyOverlayHost(content) { Bounds = new Rectangle(0, 0, 500, 350), BackdropOpacity = opacity };
        using var outside = new MouseProbe { Bounds = new Rectangle(550, 0, 100, 100) };
        form.Controls.AddRange(new Control[] { host, outside }); form.Show();
        var cancels = 0;
        using var token = host.Begin(new BusyPresentation("Busy", () => cancels++));
        var until = DateTime.UtcNow.AddSeconds(3);
        while (host.RenderMode == BootstrapBusyOverlayRenderMode.None && DateTime.UtcNow < until) { Application.DoEvents(); Thread.Sleep(5); }
        Assert.That(host.RenderMode, Is.Not.EqualTo(BootstrapBusyOverlayRenderMode.None));
        PostMessage(content.Handle, 0x202, IntPtr.Zero, IntPtr.Zero);
        PostMessage(outside.Handle, 0x202, IntPtr.Zero, IntPtr.Zero);
        Application.DoEvents();
        Assert.That(content.MouseUps, Is.Zero, "Content must reject queued mouse even at alpha zero");
        Assert.That(outside.MouseUps, Is.EqualTo(1));
        foreach (Control child in host.Controls) foreach (Control cardChild in child.Controls) if (cardChild is Button button) button.PerformClick();
        Assert.That(cancels, Is.EqualTo(1));
        token.Dispose();
        PostMessage(content.Handle, 0x202, IntPtr.Zero, IntPtr.Zero); Application.DoEvents();
        Assert.That(content.MouseUps, Is.EqualTo(1), "Hide must remove the filter");
    }
    private sealed class MouseProbe : Panel { internal int MouseUps; protected override void OnMouseUp(MouseEventArgs e) { MouseUps++; base.OnMouseUp(e); } }
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);
}
