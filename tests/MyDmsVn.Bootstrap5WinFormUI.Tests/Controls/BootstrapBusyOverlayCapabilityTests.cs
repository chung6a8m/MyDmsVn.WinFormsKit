using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using MyDmsVn.Bootstrap5WinFormUI.Compatibility;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;
[TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
public sealed class BootstrapBusyOverlayCapabilityTests
{
    [TestCase("1f676c76-80e1-4239-95bb-83d0f6d0da78"), TestCase("4a2f28e3-53b9-4441-ba9c-d69d4a4a6e38"), TestCase("8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a")]
    public void CompatibilityParserAcceptsDocumentedOsDeclarations(string id)
    {
        var bytes = Record(id);
        Assert.That(BootstrapBusyLayeredWindow.ParseCompatibility(bytes, 8, 32), Is.True);
        bytes[24] = 2;
        Assert.That(BootstrapBusyLayeredWindow.ParseCompatibility(bytes, 8, 32), Is.False);
    }
    [Test]
    public void CompatibilityParserRejectsMissingDeclarationBoundsCountAndAlignment()
    {
        Assert.That(BootstrapBusyLayeredWindow.ParseCompatibility(new byte[4], 8, 32), Is.False);
        Assert.That(BootstrapBusyLayeredWindow.ParseCompatibility(Record(Guid.Empty.ToString()), 8, 32), Is.False);
        var bytes = Record("8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a");
        foreach (var length in new[] { 0, 3, 7, 8, 24, 39 }) { var truncated = new byte[length]; Array.Copy(bytes, truncated, length); Assert.That(BootstrapBusyLayeredWindow.ParseCompatibility(truncated, 8, 32), Is.False); }
        Assert.That(BootstrapBusyLayeredWindow.ParseCompatibility(bytes, 4, 32), Is.False);
        Assert.That(BootstrapBusyLayeredWindow.ParseCompatibility(bytes, 5, 32), Is.False);
        Assert.That(BootstrapBusyLayeredWindow.ParseCompatibility(bytes, 8, 24), Is.False);
        Array.Copy(BitConverter.GetBytes(uint.MaxValue), bytes, 4);
        Assert.That(BootstrapBusyLayeredWindow.ParseCompatibility(bytes, 8, 32), Is.False);
    }
    [TestCase(false, false), TestCase(true, true)]
    public void MissingCapabilityOrNativeAlphaFailureFallsBackWithoutLosingTokensOrCancel(bool supported, bool fail)
    {
        using var form = new Form { ClientSize = new Size(500, 350) };
        var native = new FakeNative { Supported = supported, Fail = fail };
        var delay = new Delay();
        using var host = new BootstrapBusyOverlayHost(new TextBox(), delay, native) { Dock = DockStyle.Fill };
        form.Controls.Add(host); form.Show(); var calls = 0;
        using var token = host.Begin(new BusyPresentation("Busy", () => calls++)); delay.Fire();
        Assert.That(host.RenderMode, Is.EqualTo(BootstrapBusyOverlayRenderMode.OpaqueFallback));
        Assert.That(host.InputFilterInstalled, Is.True);
        foreach (Control child in host.Controls) foreach (Control item in child.Controls) if (item is Button button) button.PerformClick();
        Assert.That(calls, Is.EqualTo(1));
        var attempts = native.Attempts; host.BackdropOpacity = .5;
        Assert.That(native.Attempts, Is.EqualTo(attempts), "Failed host must not retry indefinitely");
        token.Dispose(); Assert.That(host.RenderMode, Is.EqualTo(BootstrapBusyOverlayRenderMode.None)); Assert.That(host.InputFilterInstalled, Is.False);
    }
    [Test]
    public void LateNativeFailureChangesModeButConsumerCallbackExceptionIsNotNativeFailure()
    {
        using var form = new Form { ClientSize = new Size(500, 350) }; var native = new FakeNative(); var delay = new Delay();
        using var host = new BootstrapBusyOverlayHost(new Panel(), delay, native) { Dock = DockStyle.Fill };
        form.Controls.Add(host); form.Show(); using var token = host.Begin(new BusyPresentation("Busy", () => throw new InvalidOperationException("consumer"))); delay.Fire();
        Assert.That(host.RenderMode, Is.EqualTo(BootstrapBusyOverlayRenderMode.NativeLayered));
        foreach (Control child in host.Controls) foreach (Control item in child.Controls) if (item is Button button) Assert.Throws<InvalidOperationException>((Action)(() => button.PerformClick()));
        Assert.That(host.RenderMode, Is.EqualTo(BootstrapBusyOverlayRenderMode.NativeLayered));
        native.Fail = true; host.BackdropOpacity = .5;
        Assert.That(host.RenderMode, Is.EqualTo(BootstrapBusyOverlayRenderMode.OpaqueFallback)); Assert.That(host.InputFilterInstalled, Is.True);
    }
    [TestCase(5, 40, false, 40), TestCase(122, 3, false, 3), TestCase(122, 65537, false, 65537), TestCase(122, 40, false, 40), TestCase(122, 40, true, 41)]
    public void CompatibilityQueryErrorsAndInvalidSizeProtocolReturnUnsupported(int firstError, int size, bool secondSuccess, int written)
    {
        var calls = 0;
        var supported = BootstrapBusyLayeredWindow.ReadCompatibility((IntPtr memory, UIntPtr capacity, out UIntPtr required, out int error) =>
        {
            calls++; required = (UIntPtr)(calls == 1 ? size : written); error = calls == 1 ? firstError : 13;
            return calls > 1 && secondSuccess;
        });
        Assert.That(supported, Is.False); Assert.That(calls, Is.InRange(1, 2));
    }
    [TestCase(6, 1, false), TestCase(6, 2, true), TestCase(6, 3, true), TestCase(10, 0, true)]
    public void ChildLayeredOsGateUsesNumericVersionAlongsideManifestAndNativeSetup(int major, int minor, bool supported)
    {
        Assert.That(BootstrapBusyLayeredWindow.IsChildLayeredOs(new Version(major, minor)), Is.EqualTo(supported));
    }
    [Test]
    public void ForegroundSurfaceRemainsOpaqueWithAlphaThemeTokens()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            var c = original.Colors;
            var colors = new BootstrapThemeColors(c.Primary, c.Secondary, c.Success, c.Danger, c.Warning, c.Info, c.Light, Color.FromArgb(20, c.Dark), c.Body, Color.FromArgb(20, c.Surface), Color.FromArgb(20, c.SurfaceSecondary), c.Border, c.Text, c.MutedText, c.Disabled, c.Focus, c.Hover, c.Active);
            BootstrapThemeManager.CurrentTheme = new BootstrapTheme(original.Mode, colors, original.Metrics, original.Typography);
            using var host = new BootstrapBusyOverlayHost(new Panel());
            foreach (Control child in host.Controls) if (child is Panel && child != host.Content) Assert.That(child.BackColor.A, Is.EqualTo(255));
            Assert.That(host.IsHandleCreated, Is.False);
        }
        finally { BootstrapThemeManager.CurrentTheme = original; }
    }
    [Test]
    public void ThemeResizeAndOpacityKeepContentHandleAndLiveToken()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            using var form = new Form { ClientSize = new Size(600, 400) };
            var content = new TextBox { Text = "unchanged", SelectionStart = 2 };
            var delay = new Delay();
            using var host = new BootstrapBusyOverlayHost(content, delay, new FakeNative { Supported = false }) { Size = new Size(500, 350) };
            form.Controls.Add(host); form.Show(); content.Focus(); var handle = content.Handle;
            using var token = host.Begin(new BusyPresentation("Busy")); delay.Fire();
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
            host.Size = new Size(150, 40); host.BackdropOpacity = .5;
            Assert.That(content.Handle, Is.EqualTo(handle)); Assert.That(content.SelectionStart, Is.EqualTo(2)); Assert.That(content.Focused, Is.True);
            Assert.That(delay.Schedules, Is.EqualTo(1)); Assert.That(host.RenderMode, Is.EqualTo(BootstrapBusyOverlayRenderMode.OpaqueFallback));
            token.Dispose(); Assert.That(host.RenderMode, Is.EqualTo(BootstrapBusyOverlayRenderMode.None));
        }
        finally { BootstrapThemeManager.CurrentTheme = original; }
    }
    [Test]
    public void ThemeRefreshAppliesNativeAlphaExactlyOnce()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            using var form = new Form { ClientSize = new Size(500, 350) }; var native = new FakeNative(); var delay = new Delay();
            using var host = new BootstrapBusyOverlayHost(new Panel(), delay, native) { Dock = DockStyle.Fill };
            form.Controls.Add(host); form.Show(); using var token = host.Begin(new BusyPresentation("Busy")); delay.Fire();
            var before = native.Attempts;
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
            Assert.That(native.Attempts - before, Is.EqualTo(1));
        }
        finally { BootstrapThemeManager.CurrentTheme = original; }
    }
    private static byte[] Record(string id) { var bytes = new byte[40]; bytes[0] = 1; Array.Copy(new Guid(id).ToByteArray(), 0, bytes, 8, 16); bytes[24] = 1; return bytes; }
    private sealed class FakeNative : IBootstrapBusyLayeredWindow { internal bool Supported = true, Fail; internal int Attempts; public bool IsSupported => Supported; public bool SetAlpha(IntPtr hwnd, byte alpha, out int error) { Attempts++; error = Fail ? 87 : 0; return !Fail; } }
    private sealed class Delay : IBootstrapBusyDelay { private Action? _action; internal int Schedules; public void Schedule(TimeSpan delay, Action action) { Schedules++; _action = action; } public void Cancel() => _action = null; public void Dispose() => Cancel(); internal void Fire() { var action = _action; _action = null; action?.Invoke(); } }
}
