using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Compatibility;
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
        using var host = new BootstrapBusyOverlayHost(new TextBox(), delay, new UnsupportedNative()) { Dock = DockStyle.Fill };
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
        using var host = new BootstrapBusyOverlayHost(new Panel(), delay, new UnsupportedNative()) { Dock = DockStyle.Fill };
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
    [Test]
    public void Dispose_preserves_consumer_exception_but_disposes_owned_controls_and_delay_once()
    {
        using var form = new Form();
        var delay = new ManualDelay();
        var content = new TextBox();
        var host = new BootstrapBusyOverlayHost(content, delay) { Dock = DockStyle.Fill };
        form.Controls.Add(host); form.Show();
        using var token = host.Begin(new BusyPresentation("busy"));
        delay.Fire();
        var owned = All(host).ToArray();
        var expected = new InvalidOperationException("consumer event");
        host.RenderModeChanged += (_, _) => { if (host.RenderMode == BootstrapBusyOverlayRenderMode.None) throw expected; };

        var actual = Assert.Throws<InvalidOperationException>((Action)(() => host.Dispose()));

        Assert.That(actual, Is.SameAs(expected));
        Assert.Multiple((Action)(() =>
        {
            Assert.That(owned.All(c => c.IsDisposed), Is.True);
            Assert.That(content.IsDisposed, Is.True);
            Assert.That(delay.Disposals, Is.EqualTo(1));
        }));
        host.Dispose();
        Assert.That(delay.Disposals, Is.EqualTo(1));
    }
    [TestCase(0, 250), TestCase(400, 0), TestCase(0, 0)]
    public void Zero_size_Begin_then_positive_client_schedules_once_without_new_token(int width, int height)
    {
        using var form = new Form { ClientSize = new Size(500, 350) };
        var delay = new ManualDelay();
        using var host = new BootstrapBusyOverlayHost(new Panel(), delay, new UnsupportedNative()) { ClientSize = new Size(width, height) };
        form.Controls.Add(host); form.Show(); host.CreateControl();
        Assert.That(host.IsHandleCreated, Is.True);
        using var token = host.Begin(new BusyPresentation("busy"));
        Assert.That(delay.Schedules, Is.Zero); AssertPresentation(host, false);
        host.ClientSize = new Size(400, 250);
        Assert.That(delay.Schedules, Is.EqualTo(1)); Assert.That(delay.LastDelay, Is.EqualTo(TimeSpan.FromMilliseconds(150))); AssertPresentation(host, false);
        host.ClientSize = new Size(420, 260);
        Assert.That(delay.Schedules, Is.EqualTo(1), "positive to positive must preserve pending delay");
        delay.Fire(); AssertPresentation(host, true);
        host.ClientSize = new Size(430, 270);
        Assert.That(delay.Schedules, Is.EqualTo(1), "positive to positive must keep visible operation"); AssertPresentation(host, true);
    }

    [TestCase(false), TestCase(true)]
    public void Zero_size_cancels_pending_or_visible_presentation_and_stale_delay_cannot_return(bool alreadyVisible)
    {
        using var form = new Form { ClientSize = new Size(500, 350) };
        var delay = new ManualDelay();
        using var host = new BootstrapBusyOverlayHost(new Panel(), delay, new UnsupportedNative()) { ClientSize = new Size(400, 250) };
        form.Controls.Add(host); form.Show(); using var token = host.Begin(new BusyPresentation("busy"));
        var stale = delay.Pending!;
        if (alreadyVisible) delay.Fire();
        host.ClientSize = new Size(0, 250);
        AssertPresentation(host, false); Assert.That((object?)delay.Pending, Is.Null);
        stale(); AssertPresentation(host, false);
        host.ClientSize = new Size(400, 250);
        Assert.That(delay.Schedules, Is.EqualTo(2)); Assert.That(delay.LastDelay, Is.EqualTo(TimeSpan.FromMilliseconds(150)));
        stale(); AssertPresentation(host, false); Assert.That((object?)delay.Pending, Is.Not.Null);
        delay.Fire(); AssertPresentation(host, true);
        token.Dispose(); AssertPresentation(host, false);
    }

    [TestCase(false), TestCase(true)]
    public void Zero_size_mode_callback_can_regrow_and_Begin_without_stale_delay_resurrection(bool setClientSize)
    {
        using var form = new Form { ClientSize = new Size(500, 350) }; var delay = new ManualDelay();
        using var host = new BootstrapBusyOverlayHost(new Panel(), delay, new UnsupportedNative()) { ClientSize = new Size(400, 250) };
        form.Controls.Add(host); form.Show(); using var old = host.Begin(new BusyPresentation("old")); var stale = delay.Pending!; delay.Fire();
        Action<Size> resize = size => { if (setClientSize) host.ClientSize = size; else host.Size = size; };
        IDisposable? newest = null; var reentered = false;
        host.RenderModeChanged += (_, _) =>
        {
            if (host.RenderMode != BootstrapBusyOverlayRenderMode.None || reentered) return;
            reentered = true; resize(new Size(400, 250)); newest = host.Begin(new BusyPresentation("newest"));
        };
        resize(new Size(0, 250)); Assert.That(reentered, Is.True); AssertPresentation(host, false);
        // WinForms SetClientSizeCore can restore the outer setter's dimensions
        // after the callback. Eligibility must follow the resulting client size.
        var outerSetterRestoredZero = host.ClientSize.Width == 0;
        if (outerSetterRestoredZero)
        {
            Assert.That((object?)delay.Pending, Is.Null);
            resize(new Size(400, 250));
        }
        Assert.That(delay.Schedules, Is.EqualTo(outerSetterRestoredZero ? 3 : 2));
        Assert.That((object?)delay.Pending, Is.Not.Null);
        stale(); AssertPresentation(host, false); delay.Fire(); AssertPresentation(host, true);
        Assert.That(All(host).OfType<Label>().Single().Text, Is.EqualTo("newest"));
        old.Dispose(); AssertPresentation(host, true); newest!.Dispose(); AssertPresentation(host, false);
    }
    [TestCase(false), TestCase(true)]
    public void Zero_size_during_Present_layout_cannot_resurrect_old_generation(bool regrow)
    {
        using var form = new Form { ClientSize = new Size(500, 350) }; var delay = new ManualDelay();
        using var host = new BootstrapBusyOverlayHost(new Panel(), delay, new UnsupportedNative()) { ClientSize = new Size(400, 250) };
        form.Controls.Add(host); form.Show(); using var token = host.Begin(new BusyPresentation("busy")); var armed = true;
        host.Layout += (_, _) =>
        {
            if (!armed) return;
            armed = false; host.ClientSize = new Size(0, 250);
            if (regrow) host.ClientSize = new Size(400, 250);
        };
        delay.Fire(); Assert.That(armed, Is.False); AssertPresentation(host, false);
        Assert.That(delay.Schedules, Is.EqualTo(regrow ? 2 : 1));
        if (regrow) { delay.Fire(); AssertPresentation(host, true); }
    }


    [TestCase(false), TestCase(true)]
    public void Lifecycle_ContentDisposal_RemovesPresentationAndPending(bool visible)
    {
        using var form = new Form(); var delay = new ManualDelay();
        using var host = new BootstrapBusyOverlayHost(new TextBox(), delay, new UnsupportedNative()) { Dock = DockStyle.Fill };
        form.Controls.Add(host); form.Show();
        using var token = host.Begin(new BusyPresentation("busy")); var stale = delay.Pending!;
        if (visible) delay.Fire();
        host.Content.Dispose(); AssertPresentation(host, false);
        Assert.That((object?)delay.Pending, Is.Null); stale(); AssertPresentation(host, false);
    }

    [Test]
    public void Lifecycle_MinimizeAndAncestorReparent_TracksCurrentOwnerOnly()
    {
        using var first = new Form(); using var second = new Form();
        using var ancestor = new Panel { Dock = DockStyle.Fill }; var delay = new ManualDelay();
        using var host = new BootstrapBusyOverlayHost(new TextBox(), delay, new UnsupportedNative()) { Dock = DockStyle.Fill };
        ancestor.Controls.Add(host); first.Controls.Add(ancestor); first.Show(); second.Show();
        using var token = host.Begin(new BusyPresentation("busy")); delay.Fire(); AssertPresentation(host, true);
        first.WindowState = FormWindowState.Minimized; Application.DoEvents(); AssertPresentation(host, false);
        first.WindowState = FormWindowState.Normal; Application.DoEvents();
        Assert.That(delay.LastDelay, Is.EqualTo(TimeSpan.FromMilliseconds(150))); delay.Fire(); AssertPresentation(host, true);
        second.Controls.Add(ancestor); Application.DoEvents(); delay.Fire(); AssertPresentation(host, true);
        first.WindowState = FormWindowState.Minimized; Application.DoEvents(); AssertPresentation(host, true);
        second.WindowState = FormWindowState.Minimized; Application.DoEvents(); AssertPresentation(host, false);
    }

    [TestCase(false), TestCase(true)]
    public void Lifecycle_Show_ReleasesOnlyContentCapture(bool outsideCapture)
    {
        using var form = new Form(); var delay = new ManualDelay();
        using var host = new BootstrapBusyOverlayHost(new TextBox(), delay, new UnsupportedNative()) { Bounds = new Rectangle(0, 0, 200, 200) };
        using var outside = new TextBox { Left = 210 }; form.Controls.AddRange(new Control[] { host, outside }); form.Show();
        var captured = outsideCapture ? outside : host.Content; captured.Capture = true;
        Assert.That(captured.Capture, Is.True);
        using var token = host.Begin(new BusyPresentation("busy")); delay.Fire();
        Assert.That(captured.Capture, Is.EqualTo(outsideCapture)); captured.Capture = false;
    }

    [TestCase(false), TestCase(true)]
    public void Lifecycle_CancelDisappears_RestoresOriginalDescendantOnlyFromCard(bool moveOutside)
    {
        using var form = new Form { ClientSize = new Size(500, 300) }; var delay = new ManualDelay();
        var content = new Panel(); var first = new TextBox { Top = 0 }; var remembered = new TextBox { Top = 30 };
        content.Controls.AddRange(new Control[] { first, remembered });
        using var host = new BootstrapBusyOverlayHost(content, delay, new UnsupportedNative()) { Bounds = new Rectangle(0, 0, 350, 280) };
        using var outside = new TextBox { Left = 360 }; form.Controls.AddRange(new Control[] { host, outside }); form.Show(); form.Activate(); remembered.Focus();
        using var token = host.Begin(new BusyPresentation("busy", () => { })); delay.Fire();
        Assert.That(remembered.Focused, Is.True); var cancel = All(host).OfType<Button>().Single(); cancel.Focus();
        Assert.That(cancel.Focused, Is.True); if (moveOutside) outside.Focus();
        using var noCancel = host.Begin(new BusyPresentation("no cancel"));
        Assert.That(moveOutside ? outside.Focused : remembered.Focused, Is.True);
    }

    [Test]
    public void Lifecycle_ContentHandleLoss_CancelsUntilRecreation()
    {
        using var form = new Form(); var delay = new ManualDelay();
        using var host = new BootstrapBusyOverlayHost(new TextBox(), delay, new UnsupportedNative()) { Dock = DockStyle.Fill };
        form.Controls.Add(host); form.Show(); using var token = host.Begin(new BusyPresentation("busy")); delay.Fire();
        typeof(Control).GetMethod("DestroyHandle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(host.Content, null);
        AssertPresentation(host, false);
        Assert.That(host.Content.IsHandleCreated, Is.False);
        _ = host.Content.Handle; Assert.That(delay.LastDelay, Is.EqualTo(TimeSpan.FromMilliseconds(150)));
        delay.Fire(); AssertPresentation(host, true);
    }

    [Test]
    public void Lifecycle_PresentationCallbackFailure_RemovesFilterAndSpinner()
    {
        using var form = new Form(); var delay = new ManualDelay();
        using var host = new BootstrapBusyOverlayHost(new TextBox(), delay, new UnsupportedNative()) { Dock = DockStyle.Fill };
        form.Controls.Add(host); form.Show(); using var token = host.Begin(new BusyPresentation("busy")); delay.Fire();
        var expected = new InvalidOperationException("consumer TextChanged");
        All(host).OfType<Label>().Single().TextChanged += (_, _) => throw expected;
        Assert.That(Assert.Throws<InvalidOperationException>((Action)(() => host.Begin(new BusyPresentation("new")))), Is.SameAs(expected));
        AssertPresentation(host, false);
    }

    [Test]
    public void Lifecycle_CaptureLossCallbackCanDisposeWithoutStaleFilter()
    {
        using var form = new Form(); var delay = new ManualDelay();
        var host = new BootstrapBusyOverlayHost(new TextBox(), delay, new UnsupportedNative()) { Dock = DockStyle.Fill };
        form.Controls.Add(host); form.Show(); host.Content.Capture = true;
        host.Content.MouseCaptureChanged += (_, _) => { if (!host.Content.Capture) host.Dispose(); };
        using var token = host.Begin(new BusyPresentation("busy")); delay.Fire();
        Assert.That(host.IsDisposed, Is.True); Assert.That(host.InputFilterInstalled, Is.False);
        Assert.That(delay.Disposals, Is.EqualTo(1));
    }

    [Test]
    public void Lifecycle_DpiNotificationPreservesOperationAndDelay()
    {
        using var form = new Form(); var delay = new ManualDelay();
        using var host = new BootstrapBusyOverlayHost(new TextBox(), delay, new UnsupportedNative()) { Dock = DockStyle.Fill };
        form.Controls.Add(host); form.Show(); using var token = host.Begin(new BusyPresentation("busy")); delay.Fire();
        var handle = host.Handle;
        for (var i = 0; i < 10; i++)
            typeof(Control).GetMethod("OnDpiChangedAfterParent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(host, new object[] { EventArgs.Empty });
        Assert.That(host.Handle, Is.EqualTo(handle)); Assert.That(delay.Schedules, Is.EqualTo(1)); AssertPresentation(host, true);
        TestContext.WriteLine($"Synthetic DPI lifecycle only; actual={host.DeviceDpi}; physical mixed-DPI NOT RUN here");
    }

    [Test]
    public void Lifecycle_ResourceSoak100Cycles_ReleasesOwnedWindowsFiltersDelaysAndSubscriptions()
    {
        using var form = new Form { ClientSize = new Size(500, 300) }; form.Show();
        Action cycle = () =>
        {
            var delay = new ManualDelay(); var host = new BootstrapBusyOverlayHost(new TextBox(), delay, new UnsupportedNative()) { Dock = DockStyle.Fill };
            form.Controls.Add(host); using var token = host.Begin(new BusyPresentation("busy", () => { })); delay.Fire(); AssertPresentation(host, true);
            typeof(Control).GetMethod("RecreateHandle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(host, null);
            AssertPresentation(host, false); delay.Fire(); AssertPresentation(host, true);
            var owned = All(host).ToArray(); var handles = owned.Where(c => c.IsHandleCreated).Select(c => c.Handle).ToArray();
            token.Dispose(); AssertPresentation(host, false); host.Dispose();
            Assert.That(host.InputFilterInstalled, Is.False); Assert.That(delay.Disposals, Is.EqualTo(1)); Assert.That((object?)delay.Pending, Is.Null);
            Assert.That(owned.All(c => c.IsDisposed && !c.IsHandleCreated), Is.True); Assert.That(handles.Any(IsWindow), Is.False);
            var ancestors = (System.Collections.ICollection)typeof(BootstrapBusyOverlayHost).GetField("_ancestors", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(host)!;
            Assert.That(ancestors.Count, Is.Zero);
            Assert.That(typeof(BootstrapBusyOverlayHost).GetField("_owner", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(host), Is.Null);
            Application.DoEvents();
        };
        for (var i = 0; i < 10; i++) cycle();
        var process = System.Diagnostics.Process.GetCurrentProcess();
        var initialUser = GetGuiResources(process.Handle, 1); var initialGdi = GetGuiResources(process.Handle, 0);
        TestContext.WriteLine($"warm10 USER={initialUser} GDI={initialGdi}");
        for (var batch = 0; batch < 4; batch++)
        {
            for (var i = 0; i < 25; i++) cycle();
            var user = GetGuiResources(process.Handle, 1); var gdi = GetGuiResources(process.Handle, 0);
            TestContext.WriteLine($"cycle={(batch + 1) * 25} USER={user} GDI={gdi}");
            Assert.That(user, Is.LessThanOrEqualTo(initialUser), "USER growth after warmup");
            Assert.That(gdi, Is.LessThanOrEqualTo(initialGdi), "GDI growth after warmup");
        }
    }
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern int GetGuiResources(IntPtr process, int flags);
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool IsWindow(IntPtr handle);

    [Test]
    public void Lifecycle_IndependentHostsAndOwnerDeactivation_PreserveTheirOperations()
    {
        using var form = new Form { ClientSize = new Size(600, 300) }; using var other = new Form();
        var aDelay = new ManualDelay(); var bDelay = new ManualDelay();
        using var a = new BootstrapBusyOverlayHost(new TextBox(), aDelay, new UnsupportedNative()) { Bounds = new Rectangle(0, 0, 290, 280) };
        using var b = new BootstrapBusyOverlayHost(new TextBox(), bDelay, new UnsupportedNative()) { Bounds = new Rectangle(300, 0, 290, 280) };
        form.Controls.AddRange(new Control[] { a, b }); form.Show();
        using var aToken = a.Begin(new BusyPresentation("a")); using var bToken = b.Begin(new BusyPresentation("b")); aDelay.Fire(); bDelay.Fire();
        other.Show(); other.Activate(); Application.DoEvents(); AssertPresentation(a, true); AssertPresentation(b, true);
        aToken.Dispose(); AssertPresentation(a, false); AssertPresentation(b, true);
        form.Dispose(); Assert.That(b.InputFilterInstalled, Is.False); Assert.That(bDelay.Disposals, Is.EqualTo(1));
    }

    [Test]
    public void Lifecycle_LastTokenHidesFocusedCancel_WithoutTakingForegroundFromAnotherOwner()
    {
        using var form = new Form { ClientSize = new Size(500, 300) }; using var other = new Form(); var delay = new ManualDelay();
        var content = new Panel(); var first = new TextBox(); var remembered = new TextBox { Top = 35 }; content.Controls.AddRange(new Control[] { first, remembered });
        using var host = new BootstrapBusyOverlayHost(content, delay, new UnsupportedNative()) { Dock = DockStyle.Fill };
        form.Controls.Add(host); BootstrapBusyOverlayNativeTests.Activate(form); remembered.Focus();
        var token = host.Begin(new BusyPresentation("busy", () => { })); delay.Fire(); All(host).OfType<Button>().Single().Focus(); token.Dispose();
        Assert.That(remembered.Focused, Is.True);
        using var next = host.Begin(new BusyPresentation("again", () => { })); delay.Fire(); All(host).OfType<Button>().Single().Focus();
        BootstrapBusyOverlayNativeTests.Activate(other); var foreground = GetForegroundWindow();
        next.Dispose(); Assert.That(GetForegroundWindow(), Is.EqualTo(foreground)); AssertPresentation(host, false);
    }
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();

    [Test]
    public void Lifecycle_FocusRestorationReentrantShow_CannotHideTheNewPresentation()
    {
        using var form = new Form(); var delay = new ManualDelay(); var content = new TextBox();
        using var host = new BootstrapBusyOverlayHost(content, delay, new UnsupportedNative()) { Dock = DockStyle.Fill };
        form.Controls.Add(host); BootstrapBusyOverlayNativeTests.Activate(form); content.Focus();
        var token = host.Begin(new BusyPresentation("old", () => { })); delay.Fire(); All(host).OfType<Button>().Single().Focus();
        IDisposable? next = null;
        content.Enter += (_, _) => { if (next is null) { next = host.Begin(new BusyPresentation("new")); delay.Fire(); } };
        token.Dispose();
        try { AssertPresentation(host, true); Assert.That(All(host).OfType<Label>().Single().Text, Is.EqualTo("new")); }
        finally { next?.Dispose(); }
    }

    [TestCase("card", false), TestCase("backdrop", false), TestCase("card", true), TestCase("backdrop", true)]
    public void HideBoundary_ReentrantPresentationSurvivesOldHide(string boundary, bool throws)
    {
        using var form = new Form(); var delay = new ManualDelay();
        using var host = new BootstrapBusyOverlayHost(new TextBox(), delay, new UnsupportedNative()) { Dock = DockStyle.Fill };
        form.Controls.Add(host); form.Show();
        var token = host.Begin(new BusyPresentation("old", () => { })); delay.Fire();
        var card = All(host).OfType<Button>().Single().Parent!;
        var backdrop = host.Controls.Cast<Control>().Single(c => c != host.Content && c != card);
        var watched = boundary == "card" ? card : backdrop;
        var expected = new InvalidOperationException("original hide event"); var armed = true; IDisposable? newer = null;
        watched.VisibleChanged += (_, _) =>
        {
            if (!armed || watched.Visible) return;
            armed = false; newer = host.Begin(new BusyPresentation("new", () => { })); delay.Fire();
            if (throws) throw expected;
        };
        try
        {
            if (throws) Assert.That(Assert.Throws<InvalidOperationException>((Action)token.Dispose), Is.SameAs(expected));
            else token.Dispose();
            Assert.That(armed, Is.False); AssertPresentation(host, true);
            Assert.That(card.Visible, Is.True); Assert.That(backdrop.Visible, Is.True);
            Assert.That(All(host).OfType<Label>().Single().Text, Is.EqualTo("new"));
        }
        finally { newer?.Dispose(); token.Dispose(); }
    }

    [TestCase("card"), TestCase("backdrop"), TestCase("focus")]
    public void HideBoundary_ThrowingCallbacksFinishCleanupAndKeepOriginalException(string boundary)
    {
        using var form = new Form(); var delay = new ManualDelay(); var content = new TextBox();
        using var host = new BootstrapBusyOverlayHost(content, delay, new UnsupportedNative()) { Dock = DockStyle.Fill };
        form.Controls.Add(host); BootstrapBusyOverlayNativeTests.Activate(form); content.Focus();
        var token = host.Begin(new BusyPresentation("old", () => { })); delay.Fire();
        var cancel = All(host).OfType<Button>().Single(); var card = cancel.Parent!;
        var backdrop = host.Controls.Cast<Control>().Single(c => c != host.Content && c != card);
        cancel.Focus();
        var expected = new InvalidOperationException("first callback"); var secondary = new InvalidOperationException("later cleanup callback");
        var armed = true;
        EventHandler first = (_, _) => { if (armed) { armed = false; throw expected; } };
        if (boundary == "card") card.VisibleChanged += (_, e) => { if (!card.Visible) first(card, e); };
        if (boundary == "backdrop") backdrop.VisibleChanged += (_, e) => { if (!backdrop.Visible) first(backdrop, e); };
        if (boundary == "focus")
        {
            content.Enabled = false; cancel.TabStop = false;
            var outside = new ThrowingSelectControl { Failure = expected }; form.Controls.Add(outside);
        }
        if (boundary != "backdrop") backdrop.VisibleChanged += (_, _) => { if (!backdrop.Visible) throw secondary; };
        Assert.That(Assert.Throws<InvalidOperationException>((Action)token.Dispose), Is.SameAs(expected));
        AssertPresentation(host, false); Assert.That(card.Visible, Is.False); Assert.That(backdrop.Visible, Is.False);
        Assert.That((object?)delay.Pending, Is.Null); token.Dispose();
    }

    [Test]
    public void HideBoundary_PresentationFailureKeepsOriginalWhenCleanupAlsoThrows()
    {
        using var form = new Form(); var delay = new ManualDelay();
        using var host = new BootstrapBusyOverlayHost(new TextBox(), delay, new UnsupportedNative()) { Dock = DockStyle.Fill };
        form.Controls.Add(host); form.Show(); using var token = host.Begin(new BusyPresentation("old")); delay.Fire();
        var card = All(host).OfType<Button>().Single().Parent!;
        var expected = new InvalidOperationException("presentation callback");
        All(host).OfType<Label>().Single().TextChanged += (_, _) => throw expected;
        card.VisibleChanged += (_, _) => { if (!card.Visible) throw new InvalidOperationException("cleanup callback"); };
        Assert.That(Assert.Throws<InvalidOperationException>((Action)(() => host.Begin(new BusyPresentation("new")))), Is.SameAs(expected));
        AssertPresentation(host, false);
        Assert.That(host.Controls.Cast<Control>().Where(c => c != host.Content).All(c => !c.Visible), Is.True);
    }

    [Test]
    public void Review_EmbeddedForm_RestoresFocusAndTracksOuterOwnerAfterReparent()
    {
        using var outer = new Form(); using var second = new Form();
        using var embedded = new Form { TopLevel = false, Dock = DockStyle.Fill };
        var content = new Panel(); var first = new TextBox(); var remembered = new TextBox { Top = 35 };
        content.Controls.AddRange(new Control[] { first, remembered }); var delay = new ManualDelay();
        using var host = new BootstrapBusyOverlayHost(content, delay, new UnsupportedNative()) { Dock = DockStyle.Fill };
        embedded.Controls.Add(host); outer.Controls.Add(embedded); embedded.Show();
        BootstrapBusyOverlayNativeTests.Activate(outer); remembered.Focus();
        var token = host.Begin(new BusyPresentation("busy", () => { })); delay.Fire();
        All(host).OfType<Button>().Single().Focus(); token.Dispose();
        Assert.That(remembered.Focused, Is.True, "restore remembered descendant through embedded Form");
        using var next = host.Begin(new BusyPresentation("again")); delay.Fire();
        outer.WindowState = FormWindowState.Minimized; Application.DoEvents(); AssertPresentation(host, false);
        outer.WindowState = FormWindowState.Normal; Application.DoEvents(); delay.Fire(); AssertPresentation(host, true);
        second.Controls.Add(embedded); second.Show(); Application.DoEvents(); delay.Fire(); AssertPresentation(host, true);
        outer.WindowState = FormWindowState.Minimized; Application.DoEvents(); AssertPresentation(host, true);
        second.WindowState = FormWindowState.Minimized; Application.DoEvents(); AssertPresentation(host, false);
    }

    [TestCase(false, "release"), TestCase(false, "hide"), TestCase(false, "new")]
    [TestCase(true, "release"), TestCase(true, "hide"), TestCase(true, "new")]
    public void Review_BackdropUpdate_LayoutCannotCommitStaleMode(bool theme, string action)
    {
        using var form = new Form(); var delay = new ManualDelay();
        using var host = new BootstrapBusyOverlayHost(new Panel(), delay, new UnsupportedNative()) { Dock = DockStyle.Fill };
        form.Controls.Add(host); form.Show(); var token = host.Begin(new BusyPresentation("old")); delay.Fire();
        var oldTheme = MyDmsVn.Bootstrap5WinFormUI.Theme.BootstrapThemeManager.CurrentTheme;
        IDisposable? newer = null; var armed = true;
        host.Layout += (_, e) =>
        {
            // Theme also lays out before UpdateBackdrop; select its z-order callback.
            if (!armed || e.AffectedProperty != "ChildIndex") return;
            armed = false;
            if (action == "hide") host.Hide();
            else { token.Dispose(); if (action == "new") newer = host.Begin(new BusyPresentation("new")); }
        };
        try
        {
            if (theme) MyDmsVn.Bootstrap5WinFormUI.Theme.BootstrapThemeManager.CurrentTheme = MyDmsVn.Bootstrap5WinFormUI.Theme.BootstrapTheme.CreateDefault(MyDmsVn.Bootstrap5WinFormUI.Theme.BootstrapThemeMode.Dark);
            else host.BackdropOpacity = .5;
            Assert.That(armed, Is.False); AssertPresentation(host, false);
            Assert.That(host.Controls.Cast<Control>().Where(c => c != host.Content).All(c => !c.Visible), Is.True);
            if (action == "new") { delay.Fire(); AssertPresentation(host, true); Assert.That(All(host).OfType<Label>().Single().Text, Is.EqualTo("new")); }
        }
        finally { newer?.Dispose(); token.Dispose(); MyDmsVn.Bootstrap5WinFormUI.Theme.BootstrapThemeManager.CurrentTheme = oldTheme; }
    }

    [Test]
    public void Review_LongMessage_RealViewportScrollsVerticallyOnlyAndCancelWorks()
    {
        using var form = new Form { ClientSize = new Size(400, 220) }; var delay = new ManualDelay(); var cancelled = 0;
        using var host = new BootstrapBusyOverlayHost(new Panel(), delay, new UnsupportedNative()) { Dock = DockStyle.Fill };
        form.Controls.Add(host); form.Show();
        using var token = host.Begin(new BusyPresentation(string.Join(" ", Enumerable.Repeat("Thông báo xử lý dữ liệu dài cần đọc đầy đủ.", 100)), () => cancelled++)); delay.Fire(); Application.DoEvents();
        var label = All(host).OfType<Label>().Single(); var viewport = (Panel)label.Parent!;
        Assert.That(viewport.VerticalScroll.Visible, Is.True);
        Assert.That(viewport.HorizontalScroll.Visible, Is.False);
        Assert.That(label.Width, Is.LessThanOrEqualTo(viewport.ClientSize.Width));
        viewport.ScrollControlIntoView(label); viewport.AutoScrollPosition = new Point(0, label.Height);
        Assert.That(viewport.AutoScrollPosition.Y, Is.LessThan(0));
        var cancel = All(host).OfType<Button>().Single(); Assert.That(cancel.CanSelect, Is.True); cancel.PerformClick(); Assert.That(cancelled, Is.EqualTo(1));
    }

    [TestCase(400), TestCase(320)]
    public void Review_Round2_LongToFittingMessageClearsOldScrollbarAndMatchesFreshHost(int width)
    {
        using var font = new Font("Segoe UI", 9f);
        using var form = new Form { ClientSize = new Size(width, 220), Font = font }; var delay = new ManualDelay();
        using var host = new BootstrapBusyOverlayHost(new Panel(), delay, new UnsupportedNative()) { Dock = DockStyle.Fill };
        form.Controls.Add(host); form.Show();
        var flags = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.TextBoxControl;
        string? fitting = null;
        for (var count = 1; count < 100; count++)
        {
            var text = string.Join(" ", Enumerable.Repeat("i", count));
            var metrics = BootstrapBusyOverlayLayoutLogic.Calculate(host.ClientSize, host.Font, text, "Cancel", true,
                host.DeviceDpi, MyDmsVn.Bootstrap5WinFormUI.Theme.BootstrapThemeManager.CurrentTheme.Metrics);
            var narrowHeight = TextRenderer.MeasureText(text, host.Font,
                new Size(metrics.Viewport.Width - SystemInformation.VerticalScrollBarWidth, int.MaxValue), flags).Height;
            if (metrics.Message.Height <= metrics.Viewport.Height && narrowHeight > metrics.Viewport.Height)
            { fitting = text; break; }
        }
        Assert.That(fitting, Is.Not.Null, "message must fit at full width and wrap an extra line at stale scrollbar width");
        using var freshForm = new Form { ClientSize = form.ClientSize, Font = font }; var freshDelay = new ManualDelay();
        using var fresh = new BootstrapBusyOverlayHost(new Panel(), freshDelay, new UnsupportedNative()) { Dock = DockStyle.Fill };
        freshForm.Controls.Add(fresh); freshForm.Show();
        using var freshToken = fresh.Begin(new BusyPresentation(fitting!, () => { })); freshDelay.Fire();
        var freshLabel = All(fresh).OfType<Label>().Single(); var freshViewport = (Panel)freshLabel.Parent!;
        using var longToken = host.Begin(new BusyPresentation(string.Join(" ", Enumerable.Repeat("long message", 100)), () => { })); delay.Fire();
        var label = All(host).OfType<Label>().Single(); var viewport = (Panel)label.Parent!;
        Assert.That(viewport.VerticalScroll.Visible, Is.True);
        viewport.AutoScrollPosition = new Point(0, label.Height);
        using (host.Begin(new BusyPresentation(fitting!, () => { })))
        {
            for (var repeat = 0; repeat < 3; repeat++)
            {
                host.PerformLayout();
                Assert.That(viewport.VerticalScroll.Visible, Is.False, "stale extent must not preserve vertical scroll");
                Assert.That(viewport.HorizontalScroll.Visible, Is.False);
                Assert.That(viewport.AutoScrollPosition, Is.EqualTo(Point.Empty));
                Assert.That(label.Bounds, Is.EqualTo(freshLabel.Bounds));
                Assert.That(viewport.Bounds, Is.EqualTo(freshViewport.Bounds));
                Assert.That(All(host).OfType<Button>().Single().CanSelect, Is.True);
            }
        }
        Assert.That(viewport.VerticalScroll.Visible, Is.True, "releasing newest token restores long presentation");
        Assert.That(viewport.HorizontalScroll.Visible, Is.False);
    }

    private sealed class ThrowingSelectControl : TextBox
    {
        internal Exception? Failure;
        protected override void Select(bool directed, bool forward) { var error = Failure; Failure = null; if (error is not null) throw error; base.Select(directed, forward); }
    }

    private static void AssertPresentation(BootstrapBusyOverlayHost host, bool visible)
    {
        Assert.That(host.RenderMode, Is.EqualTo(visible ? BootstrapBusyOverlayRenderMode.OpaqueFallback : BootstrapBusyOverlayRenderMode.None));
        Assert.That(host.InputFilterInstalled, Is.EqualTo(visible));
        Assert.That(All(host).OfType<BootstrapSpinner>().Single().Spinning, Is.EqualTo(visible));
    }
    private static IEnumerable<Control> All(Control root) => new[] { root }.Concat(root.Controls.Cast<Control>().SelectMany(All));
    private sealed class UnsupportedNative : IBootstrapBusyLayeredWindow { public bool IsSupported => false; public bool SetAlpha(IntPtr hwnd, byte alpha, out int error) { error = 0; throw new InvalidOperationException("Unsupported native path must not be called"); } }
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
