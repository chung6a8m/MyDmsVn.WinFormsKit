using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
public sealed class BootstrapBusyOverlayHostTests
{
    [Test]
    public void Construction_validates_without_taking_failed_content_and_is_handle_free()
    {
        Assert.Throws<ArgumentNullException>((Action)(() => new BootstrapBusyOverlayHost(null!)));
        using var parent = new Panel();
        var child = new TextBox();
        parent.Controls.Add(child);
        Assert.Throws<ArgumentException>((Action)(() => new BootstrapBusyOverlayHost(child)));
        Assert.That(child.IsDisposed, Is.False);
        var content = new TextBox();
        var delay = new ManualDelay();
        var host = new BootstrapBusyOverlayHost(content, delay);
        Assert.That(host.Content, Is.SameAs(content));
        Assert.That(content.Dock, Is.EqualTo(DockStyle.Fill));
        Assert.That(All(host).All(c => !c.IsHandleCreated), Is.True);
        Assert.That(delay.Schedules, Is.Zero);
        Assert.That(All(host).OfType<BootstrapSpinner>().Single().Spinning, Is.False);
        Assert.That(host.BackdropOpacity, Is.EqualTo(.35));
        Assert.That(host.RenderMode, Is.EqualTo(BootstrapBusyOverlayRenderMode.None));
        host.Dispose();
        Assert.That(content.IsDisposed, Is.True);
        Assert.That(delay.Disposals, Is.EqualTo(1));
    }

    [TestCase(-.01), TestCase(1.01), TestCase(double.NaN), TestCase(double.PositiveInfinity), TestCase(double.NegativeInfinity)]
    public void OpacityRejectsInvalidValuesBeforeMutation(double value)
    {
        using var host = new BootstrapBusyOverlayHost(new Panel());
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => host.BackdropOpacity = value));
        Assert.That(host.BackdropOpacity, Is.EqualTo(.35));
    }

    [TestCase(0, 0), TestCase(.35, 89), TestCase(1, 255), TestCase(.5, 128)]
    public void Opacity_alpha_rounds_away_from_zero(double value, int expected)
    {
        using var host = new BootstrapBusyOverlayHost(new Panel());
        host.BackdropOpacity = value;
        Assert.That(BootstrapBusyOverlayHost.ToAlpha(value), Is.EqualTo(expected));
    }

    [Test]
    public void Token_lifecycle_cancel_latest_and_stale_delay_are_guarded()
    {
        var delay = new ManualDelay();
        var shown = new List<string>();
        var hidden = 0;
        using var controller = new BootstrapBusyOverlayController(delay, () => true, p => shown.Add(p.Message), () => hidden++);
        var fast = controller.Begin(new BusyPresentation("fast"));
        var stale = delay.Pending!;
        fast.Dispose();
        using var old = controller.Begin(new BusyPresentation("old"));
        stale();
        Assert.That(shown, Is.Empty);
        delay.Fire();
        var calls = 0;
        var newest = controller.Begin(new BusyPresentation("new", () => calls++));
        controller.RequestCancel();
        Assert.That(calls, Is.EqualTo(1));
        Assert.That(shown.Last(), Is.EqualTo("new"));
        newest.Dispose();
        newest.Dispose();
        Assert.That(shown.Last(), Is.EqualTo("old"));
        using var last = controller.Begin(new BusyPresentation("last"));
        old.Dispose();
        Assert.That(shown.Last(), Is.EqualTo("last"));
        Assert.That(delay.LastDelay, Is.EqualTo(TimeSpan.FromMilliseconds(150)));
    }

    [Test]
    public void Mode_event_commits_state_and_reentrant_hide_cannot_resurrect_presentation()
    {
        using var form = new Form();
        var delay = new ManualDelay();
        using var host = new BootstrapBusyOverlayHost(new TextBox(), delay) { Dock = DockStyle.Fill };
        form.Controls.Add(host);
        form.Show();
        var modes = new List<BootstrapBusyOverlayRenderMode>();
        host.RenderModeChanged += (_, _) => { modes.Add(host.RenderMode); if (host.RenderMode != BootstrapBusyOverlayRenderMode.None) host.Hide(); };
        using var token = host.Begin(new BusyPresentation("busy"));
        Assert.That(host.RenderMode, Is.EqualTo(BootstrapBusyOverlayRenderMode.None));
        delay.Fire();
        Assert.That(modes, Is.EqualTo(new[] { BootstrapBusyOverlayRenderMode.OpaqueFallback, BootstrapBusyOverlayRenderMode.None }));
        Assert.That(host.RenderMode, Is.EqualTo(BootstrapBusyOverlayRenderMode.None));
    }

    [Test]
    public void Invalid_begin_disposal_and_cross_thread_release_preserve_ownership()
    {
        var host = new BootstrapBusyOverlayHost(new Panel());
        Assert.Throws<ArgumentNullException>((Action)(() => host.Begin(null!)));
        var token = host.Begin(new BusyPresentation("busy"));
        Exception? error = null;
        var worker = new Thread(() => { try { token.Dispose(); } catch (Exception ex) { error = ex; } });
        worker.Start(); worker.Join();
        Assert.That(error, Is.TypeOf<InvalidOperationException>());
        token.Dispose();
        host.Dispose();
        token.Dispose();
        Assert.Throws<ObjectDisposedException>((Action)(() => host.Begin(new BusyPresentation("late"))));
    }

    [Test]
    public void Visible_updates_do_not_restart_delay_and_dispose_during_mode_event_stays_hidden()
    {
        using var form = new Form();
        var delay = new ManualDelay();
        var host = new BootstrapBusyOverlayHost(new Panel(), delay) { Dock = DockStyle.Fill };
        form.Controls.Add(host); form.Show();
        using var token = host.Begin(new BusyPresentation("first"));
        delay.Fire();
        var schedules = delay.Schedules;
        host.BackdropOpacity = .35;
        using var next = host.Begin(new BusyPresentation("second"));
        Assert.That(delay.Schedules, Is.EqualTo(schedules));
        host.Hide(); host.Show();
        host.RenderModeChanged += (_, _) => { if (host.RenderMode != BootstrapBusyOverlayRenderMode.None) host.Dispose(); };
        delay.Fire();
        Assert.That(host.IsDisposed, Is.True);
        Assert.That(host.RenderMode, Is.EqualTo(BootstrapBusyOverlayRenderMode.None));
    }

    [Test]
    public void Mode_event_reentrant_begin_uses_newest_live_presentation()
    {
        using var form = new Form();
        var delay = new ManualDelay();
        using var host = new BootstrapBusyOverlayHost(new TextBox(), delay) { Dock = DockStyle.Fill };
        form.Controls.Add(host); form.Show();
        IDisposable? nested = null;
        var events = 0;
        host.RenderModeChanged += (_, _) => { events++; if (host.RenderMode != BootstrapBusyOverlayRenderMode.None) nested = host.Begin(new BusyPresentation("nested")); };
        using var first = host.Begin(new BusyPresentation("first"));
        delay.Fire();
        Assert.That(host.Controls.Cast<Control>().SelectMany(All).OfType<Label>().Single().Text, Is.EqualTo("nested"));
        Assert.That(events, Is.EqualTo(1));
        nested!.Dispose();
        Assert.That(host.Controls.Cast<Control>().SelectMany(All).OfType<Label>().Single().Text, Is.EqualTo("first"));
    }

    [Test]
    public void Hidden_pending_callback_cannot_show_after_new_generation_and_handle_recreation_reschedules()
    {
        using var form = new Form();
        var delay = new ManualDelay();
        using var host = new BootstrapBusyOverlayHost(new Panel(), delay) { Dock = DockStyle.Fill };
        form.Controls.Add(host); form.Show();
        using var token = host.Begin(new BusyPresentation("busy"));
        var stale = delay.Pending!;
        host.Hide(); host.Show();
        stale();
        Assert.That(host.RenderMode, Is.EqualTo(BootstrapBusyOverlayRenderMode.None));
        delay.Fire();
        Assert.That(host.RenderMode, Is.EqualTo(BootstrapBusyOverlayRenderMode.OpaqueFallback));
        typeof(Control).GetMethod("RecreateHandle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(host, null);
        Assert.That(host.RenderMode, Is.EqualTo(BootstrapBusyOverlayRenderMode.None));
        delay.Fire();
        Assert.That(host.RenderMode, Is.EqualTo(BootstrapBusyOverlayRenderMode.OpaqueFallback));
    }

    [Test]
    public void Mutations_require_owning_thread_even_before_handle_creation()
    {
        using var host = new BootstrapBusyOverlayHost(new Panel());
        var errors = new List<Exception>();
        var worker = new Thread(() =>
        {
            try { host.Begin(new BusyPresentation("wrong")); } catch (Exception ex) { errors.Add(ex); }
            try { host.BackdropOpacity = .5; } catch (Exception ex) { errors.Add(ex); }
        });
        worker.Start(); worker.Join();
        Assert.That(errors.Count, Is.EqualTo(2));
        Assert.That(errors.All(e => e is InvalidOperationException), Is.True);
        Assert.That(host.BackdropOpacity, Is.EqualTo(.35));
    }

    [Test]
    public void Same_opacity_does_not_invalidate_and_show_does_not_steal_focus()
    {
        using var form = new Form();
        var delay = new ManualDelay();
        var content = new TextBox();
        using var host = new BootstrapBusyOverlayHost(content, delay) { Dock = DockStyle.Fill };
        form.Controls.Add(host); form.Show(); content.Focus();
        TestContext.WriteLine($"BUS-02 actual host DPI={host.DeviceDpi}; process64={Environment.Is64BitProcess}");
        var invalidations = 0;
        foreach (Control child in host.Controls) child.Invalidated += (_, _) => invalidations++;
        host.BackdropOpacity = .35;
        Assert.That(invalidations, Is.Zero);
        using var token = host.Begin(new BusyPresentation("busy"));
        delay.Fire();
        Assert.That(content.Focused, Is.True);
    }
    private static IEnumerable<Control> All(Control root) => new[] { root }.Concat(root.Controls.Cast<Control>().SelectMany(All));
    private sealed class ManualDelay : IBootstrapBusyDelay
    {
        public Action? Pending;
        public int Schedules, Disposals;
        public TimeSpan LastDelay;
        public void Schedule(TimeSpan delay, Action action) { Schedules++; LastDelay = delay; Pending = action; }
        public void Cancel() => Pending = null;
        public void Fire() { var action = Pending; Pending = null; action?.Invoke(); }
        public void Dispose() { Disposals++; Pending = null; }
    }
}
