using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Demo;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Demo;

[TestFixture, Apartment(ApartmentState.STA)]
public sealed class BusyOverlayDemoFormTests
{
    [Test]
    public void LiveTimerNestedCompletionCancelHideShowAndDispose()
    {
        var form = new BusyOverlayDemoForm();
        form.Show();
        var host = All(form).OfType<BootstrapBusyOverlayHost>().Single();
        var buttons = All(form).OfType<Button>().Where(b => b.AccessibleName?.StartsWith("Busy ") == true).ToDictionary(b => b.AccessibleName!);
        var counter = All(host.Content).OfType<Label>().Single();
        var timer = (System.Windows.Forms.Timer)typeof(BusyOverlayDemoForm).GetField("_clock", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(form)!;
        var tick = typeof(BusyOverlayDemoForm).GetMethod("OnTick", BindingFlags.NonPublic | BindingFlags.Instance)!;
        void Advance(int count) { for (var i = 0; i < count; i++) tick.Invoke(form, new object?[] { null, EventArgs.Empty }); }
        var editor = All(host.Content).OfType<TextBox>().Single();
        Assert.That(timer.Enabled, Is.True);
        buttons["Busy nested new first"].PerformClick();
        Assert.That(editor.Enabled, Is.False, "Caller admission begins before visual delay.");
        Advance(20);
        Assert.That(editor.Enabled, Is.False, "Completing newest token must preserve older operation.");
        Assert.That(counter.Text, Is.EqualTo("Counter 20"));
        buttons["Busy visibility"].PerformClick();
        Assert.That(host.Visible, Is.False);
        buttons["Busy visibility"].PerformClick();
        Assert.That(host.Visible, Is.True);
        Advance(25);
        Assert.That(editor.Enabled, Is.True);
        buttons["Busy nested old first"].PerformClick();
        Advance(20);
        Assert.That(editor.Enabled, Is.False);
        buttons["Busy cancel"].PerformClick();
        Assert.That(editor.Enabled, Is.True);
        buttons["Busy fast"].PerformClick();
        Advance(1);
        Assert.That(host.RenderMode, Is.EqualTo(BootstrapBusyOverlayRenderMode.None));
        buttons["Busy long"].PerformClick();
        var wait = System.Diagnostics.Stopwatch.StartNew();
        var initial = counter.Text;
        while ((counter.Text == initial || host.RenderMode == BootstrapBusyOverlayRenderMode.None) && wait.ElapsedMilliseconds < 2000)
        {
            Application.DoEvents();
            Thread.Sleep(5);
        }
        Assert.That(counter.Text, Is.Not.EqualTo(initial), "Real content timer continues while busy.");
        Assert.That(host.RenderMode, Is.Not.EqualTo(BootstrapBusyOverlayRenderMode.None));
        All(host).OfType<Button>().Single(b => b.Text == "Cancel").PerformClick();
        Assert.That(editor.Enabled, Is.True, "Foreground cancel releases caller-owned operation.");
        buttons["Busy long"].PerformClick();
        form.Dispose();
        Assert.That(host.IsDisposed, Is.True);
        Assert.That(host.Content.IsDisposed, Is.True);
        Assert.That(timer.Enabled, Is.False);
    }
    [Test]
    public void RouteCreatesOwnedContentAndOutsideControls()
    {
        using var main = new MainForm();
        var sidebar = All(main).OfType<BootstrapSidebar>().Single();
        var route = sidebar.Items.SingleOrDefault(i => i.Text == "Loading / Busy Overlay");
        Assert.That(route, Is.Not.Null);
        sidebar.SelectedItem = route;
        var host = All(main).OfType<BootstrapBusyOverlayHost>().Single();
        var legacy = All(main).OfType<BootstrapBusyHost>().Single();
        Assert.That(host.Content, Is.Not.SameAs(legacy.Content));
        Assert.That(All(host.Content).OfType<TextBox>().Any(), Is.True);
        Assert.That(All(host.Content).OfType<ListView>().Any(), Is.True);
        var slider = All(main).OfType<TrackBar>().Single();
        Assert.That(host.Contains(slider), Is.False);
        Assert.That(slider.Value, Is.EqualTo(35));
        slider.Value = 0;
        Assert.That(host.BackdropOpacity, Is.Zero);
        slider.Value = 100;
        Assert.That(host.BackdropOpacity, Is.EqualTo(1));
    }
    private static IEnumerable<Control> All(Control root)
    {
        foreach (Control c in root.Controls) { yield return c; foreach (var n in All(c)) yield return n; }
    }
}
