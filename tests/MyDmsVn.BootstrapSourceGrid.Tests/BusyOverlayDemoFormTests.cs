using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.BootstrapSourceGrid.Demo;
using NUnit.Framework;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
public sealed class BusyOverlayDemoFormTests
{
    [TestCase(false), TestCase(true)]
    public void DisposalReleasesCallerOwnedNativeEditorComponentAndControl(bool attached)
    {
        var form = new BusyOverlayDemoForm();
        var editor = form.ChoiceEditor;
        var disposed = 0;
        editor.Disposed += (_, _) => disposed++;
        if (attached) { form.Show(); Application.DoEvents(); StartEdit(form, 2).EndEdit(true); }
        form.Dispose(); form.Dispose();
        Assert.That(editor.Control.IsDisposed, Is.True);
        Assert.That(disposed, Is.EqualTo(1));
    }

    [TestCase("Light"), TestCase("Dark")]
    public void ExactManifestedGalleryExeProvesLiveSourceGridAndInput(string theme)
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "MyDmsVn.WinFormsKit.sln"))) root = root.Parent;
        Assert.That(root, Is.Not.Null);
#if NET8_0_OR_GREATER
        const string target = "net8.0-windows";
#else
        const string target = "net48";
#endif
        var configuration = new DirectoryInfo(TestContext.CurrentContext.TestDirectory).Parent!.Name;
        var exe = Path.Combine(root!.FullName, "samples", "GridGallery", "bin", configuration, target, "MyDmsVn.BootstrapSourceGrid.Demo.exe");
        var output = Path.Combine(root.FullName, "artifacts", "verification", "sourcegrid-busy", target + "-" + theme);
        Directory.CreateDirectory(output);
        using var process = Process.Start(new ProcessStartInfo(exe, "--scenario sourcegrid-busy-live \"" + output + "\" " + theme) { UseShellExecute = true, WindowStyle = ProcessWindowStyle.Normal })!;
        try
        {
            Assert.That(process.WaitForExit(60000), Is.True, "Exact gallery EXE exceeded 60s");
            var log = File.ReadAllText(Path.Combine(output, "result.txt"));
            TestContext.Out.WriteLine(log);
            Assert.That(process.ExitCode, Is.Zero, log);
            Assert.That(log, Does.Contain("Mode=NativeLayered"));
            Assert.That(log, Does.Contain("PASS same-token-live"));
            Assert.That(log, Does.Contain("PASS SendInput alpha=100"));
            Assert.That(log, Does.Contain("PMv2=True"));
        }
        finally { if (!process.HasExited) { process.Kill(); process.WaitForExit(5000); } }
    }

    [Test]
    public void ConstructorOwnsSeparateGridAndSharedEditorsWithoutHandles()
    {
        using var first = new BusyOverlayDemoForm();
        using var second = new BusyOverlayDemoForm();
        Assert.That(first.IsHandleCreated, Is.False);
        Assert.That(first.Grid.IsHandleCreated, Is.False);
        Assert.That(first.Host.Content, Is.SameAs(first.Grid.Parent));
        Assert.That(first.Grid, Is.Not.SameAs(second.Grid));
        Assert.That(first.Grid[1, 1].Editor, Is.SameAs(first.Grid[39, 1].Editor));
        Assert.That(first.Grid[1, 1].Editor, Is.Not.SameAs(second.Grid[1, 1].Editor));
        Assert.That(first.OpacitySlider.Parent, Is.Not.SameAs(first.Host.Content));
    }

    [Test]
    public void BeginCancelsNativeEditAndClosesPopupBeforePendingAdmission()
    {
        using var form = Show();
        var context = StartEdit(form, 2);
        form.ChoiceEditor.Control.DroppedDown = true;
        Application.DoEvents();
        Assert.That(form.ChoiceEditor.Control.DroppedDown, Is.True);
        Assert.That(form.StartOperation(1000, "test"), Is.True);
        Assert.That(form.ChoiceEditor.Control.DroppedDown, Is.False);
        Assert.That(context.IsEditing(), Is.False);
        Assert.That(form.Grid.Enabled, Is.False);
        Assert.That(form.Grid.AccessibilityObject.State.HasFlag(AccessibleStates.Unavailable), Is.True);
        Assert.That(form.TryApply(), Is.False);
        Assert.That(form.AppliedCount, Is.Zero);
        Assert.That(form.Host.RenderMode, Is.EqualTo(BootstrapBusyOverlayRenderMode.None));
        Pump(200);
        Assert.That(form.Host.RenderMode, Is.Not.EqualTo(BootstrapBusyOverlayRenderMode.None));
        Assert.That(form.TryApply(), Is.False);
        form.CancelAll();
        Assert.That(form.Grid.Enabled, Is.True);
        Assert.That(form.TryApply(), Is.True);
        StartEdit(form, 1).EndEdit(true);
    }

    [Test]
    public void NestedReleaseKeepsAdmissionClosedUntilLastOperation()
    {
        using var form = Show();
        form.StartOperation(1, "old");
        form.StartOperation(1000, "new");
        form.AdvanceContent();
        Assert.That(form.OperationCount, Is.EqualTo(1));
        Assert.That(form.Grid.Enabled, Is.False);
        form.CancelNewest();
        Assert.That(form.OperationCount, Is.Zero);
        Assert.That(form.Grid.Enabled, Is.True);
    }

    [Test]
    public void LiveTickChangesCellsAndProgressWithoutEndingBusy()
    {
        using var form = Show();
        form.StartOperation(1000, "live");
        var before = form.Grid[1, 0].Value;
        form.AdvanceContent();
        Assert.That(form.Grid[1, 0].Value, Is.Not.EqualTo(before));
        Assert.That(form.Progress.Value, Is.GreaterThan(0));
        Assert.That(form.OperationCount, Is.EqualTo(1));
        form.OpacitySlider.Value = 0;
        Assert.That(form.Host.BackdropOpacity, Is.Zero);
        form.OpacitySlider.Value = 100;
        Assert.That(form.Host.BackdropOpacity, Is.EqualTo(1));
    }

    [Test]
    public void HideShowRetainsOperationAndDisposalReleasesEditors()
    {
        var form = Show();
        var editor = form.TextEditor.Control;
        form.StartOperation(1000, "live");
        Pump(200);
        form.Host.Visible = false;
        Assert.That(form.Host.RenderMode, Is.EqualTo(BootstrapBusyOverlayRenderMode.None));
        form.Host.Visible = true;
        Pump(200);
        Assert.That(form.OperationCount, Is.EqualTo(1));
        form.Dispose();
        Assert.That(editor.IsDisposed, Is.True);
        Assert.That(form.Host.IsDisposed, Is.True);
    }

    private static BusyOverlayDemoForm Show()
    {
        var form = new BusyOverlayDemoForm();
        form.Show(); Application.DoEvents(); return form;
    }
    private static SourceGrid.CellContext StartEdit(BusyOverlayDemoForm form, int column)
    {
        var position = new SourceGrid.Position(1, column);
        Assert.That(form.Grid.Selection.Focus(position, true), Is.True);
        var context = new SourceGrid.CellContext(form.Grid, position, form.Grid[position]);
        context.StartEdit(); Application.DoEvents();
        Assert.That(context.IsEditing(), Is.True);
        return context;
    }
    private static void Pump(int milliseconds)
    {
        var watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < milliseconds) { Application.DoEvents(); Thread.Sleep(5); }
    }
}
