using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapSourceGridHandleLifecycleTests
{
    [OneTimeSetUp]
    public void ConfigureWinForms()
    {
        WinFormsTestGuard.Configure();
    }

    [Test]
    public void WorkerThemeChangeBeforeFirstHandleIsAppliedOnOwningThreadAfterHandleCreation()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        var light = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

        try
        {
            BootstrapThemeManager.CurrentTheme = light;
            using (var grid = new TrackingBootstrapSourceGrid())
            {
                var owningThreadId = Thread.CurrentThread.ManagedThreadId;
                var snapshotBefore = grid.CurrentThemeSnapshot;
                Exception? workerException = null;
                grid.ResetThemeApplicationCount();

                var worker = new Thread(() =>
                {
                    try
                    {
                        BootstrapThemeManager.CurrentTheme = dark;
                    }
                    catch (Exception exception)
                    {
                        workerException = exception;
                    }
                });
                worker.Start();

                Assert.That(worker.Join(TimeSpan.FromSeconds(5)), Is.True, "Theme worker did not finish.");
                Assert.That(workerException, Is.Null);
                Assert.That(grid.IsHandleCreated, Is.False);
                Assert.That(grid.ThemeApplicationCount, Is.Zero);
                Assert.That(grid.CurrentThemeSnapshot, Is.SameAs(snapshotBefore));

                grid.CreateControl();

                Assert.That(grid.IsHandleCreated, Is.True);
                Assert.That(grid.ThemeApplicationCount, Is.EqualTo(1));
                Assert.That(grid.LastThemeApplicationThreadId, Is.EqualTo(owningThreadId));
                Assert.That(grid.CurrentThemeSnapshot, Is.Not.SameAs(snapshotBefore));
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [Test]
    public void WorkerThemeChangeDuringHandleRecreationCannotBeLost()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        var light = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

        try
        {
            BootstrapThemeManager.CurrentTheme = light;
            using (var form = new Form())
            using (var grid = new ThrowingPostBootstrapSourceGrid())
            {
                form.Controls.Add(grid);
                form.Show();
                _ = grid.Handle;
                var owningThreadId = Thread.CurrentThread.ManagedThreadId;
                Exception? workerException = null;
                grid.ResetThemeApplicationCount();

                var worker = new Thread(() =>
                {
                    try
                    {
                        BootstrapThemeManager.CurrentTheme = dark;
                    }
                    catch (Exception exception)
                    {
                        workerException = exception;
                    }
                });
                worker.Start();

                Assert.That(
                    grid.PostAttempted.Wait(TimeSpan.FromSeconds(5)),
                    Is.True,
                    "Theme worker did not reach the cross-thread post.");
                grid.RecreateGridHandle();
                grid.AllowPostFailure.Set();

                Assert.That(worker.Join(TimeSpan.FromSeconds(5)), Is.True, "Theme worker did not finish.");
                Assert.That(workerException, Is.Null);
                Assert.That(grid.CurrentThemeSnapshot.SelectionBackColor, Is.EqualTo(dark.Colors.Primary));
                Assert.That(grid.ThemeApplicationCount, Is.EqualTo(1));
                Assert.That(grid.LastThemeApplicationThreadId, Is.EqualTo(owningThreadId));
                form.Close();
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [Test]
    public void RepeatedHandleRecreationKeepsOneThemeCallbackAndUsableGrid()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        var light = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

        try
        {
            BootstrapThemeManager.CurrentTheme = light;
            using (var form = new RecreatingForm())
            using (var grid = new TrackingBootstrapSourceGrid())
            {
                grid.Redim(2, 2);
                grid[0, 0] = new SourceGrid.Cells.Cell("before");
                form.Controls.Add(grid);
                form.Show();
                _ = grid.Handle;
                var childControlCount = grid.Controls.Count;
                grid.ResetThemeApplicationCount();

                for (var iteration = 0; iteration < 5; iteration++)
                {
                    form.RecreateHostHandle();
                    grid.RecreateGridHandle();
                    _ = grid.Handle;
                    BootstrapThemeManager.CurrentTheme = (iteration & 1) == 0 ? dark : light;

                    Assert.That(grid.ThemeApplicationCount, Is.EqualTo(iteration + 1));
                    Assert.That(grid.Controls.Count, Is.EqualTo(childControlCount));
                    Assert.That(grid[0, 0].Value, Is.EqualTo("before"));
                    grid[0, 1] = new SourceGrid.Cells.Cell(iteration);
                    Assert.That(grid[0, 1].Value, Is.EqualTo(iteration));
                }

                form.Close();
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [Test]
    public void DisposeAfterHandleCreationReleasesOwnedStateAndStopsThemeCallbacks()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        var light = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

        try
        {
            BootstrapThemeManager.CurrentTheme = light;
            var grid = new BootstrapSourceGridControl();
            grid.Redim(1, 1);
            grid[0, 0] = new SourceGrid.Cells.Cell("value");
            _ = grid.GetCell(0, 0).View;
            grid.CreateControl();
            BootstrapThemeManager.CurrentTheme = dark;
            var snapshotBeforeDisposal = grid.CurrentThemeSnapshot;

            Assert.That(grid.IsThemeSubscribed, Is.True);
            Assert.That(grid.OwnedThemeFont, Is.Not.Null);

            grid.Dispose();

            Assert.That(grid.IsThemeSubscribed, Is.False);
            Assert.That(grid.OwnedThemeFont, Is.Null);
            BootstrapThemeManager.CurrentTheme = light;
            Assert.That(grid.CurrentThemeSnapshot, Is.SameAs(snapshotBeforeDisposal));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [Test]
    public void SharedIntegrationViewsDoNotRetainDeclaredDisposableResources()
    {
        using (var grid = new BootstrapSourceGridControl())
        {
            grid.Redim(2, 2);
            grid[0, 0] = new SourceGrid.Cells.ColumnHeader("column");
            grid[1, 0] = new SourceGrid.Cells.RowHeader("row");
            grid[1, 1] = new SourceGrid.Cells.Cell("cell");

            AssertHasNoDeclaredDisposableFields(grid.GetCell(0, 0).View);
            AssertHasNoDeclaredDisposableFields(grid.GetCell(1, 0).View);
            AssertHasNoDeclaredDisposableFields(grid.GetCell(1, 1).View);
        }
    }

    private static void AssertHasNoDeclaredDisposableFields(object view)
    {
        var fields = view.GetType().GetFields(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
        foreach (var field in fields)
        {
            Assert.That(
                field.GetValue(view),
                Is.Not.InstanceOf<IDisposable>(),
                $"{view.GetType().Name}.{field.Name} must not retain a disposable resource.");
        }
    }

    private sealed class RecreatingForm : Form
    {
        internal void RecreateHostHandle()
        {
            RecreateHandle();
        }
    }

    private class TrackingBootstrapSourceGrid : BootstrapSourceGridControl
    {
        internal int ThemeApplicationCount { get; private set; }

        internal int? LastThemeApplicationThreadId { get; private set; }

        internal void ResetThemeApplicationCount()
        {
            ThemeApplicationCount = 0;
            LastThemeApplicationThreadId = null;
        }

        internal void RecreateGridHandle()
        {
            RecreateHandle();
        }

        internal override void ApplyBootstrapTheme()
        {
            ThemeApplicationCount++;
            LastThemeApplicationThreadId = Thread.CurrentThread.ManagedThreadId;
            base.ApplyBootstrapTheme();
        }
    }

    private sealed class ThrowingPostBootstrapSourceGrid : TrackingBootstrapSourceGrid
    {
        internal ManualResetEventSlim PostAttempted { get; } = new ManualResetEventSlim();

        internal ManualResetEventSlim AllowPostFailure { get; } = new ManualResetEventSlim();

        internal override void PostThemeChange(Action callback)
        {
            PostAttempted.Set();
            if (!AllowPostFailure.Wait(TimeSpan.FromSeconds(5)))
            {
                throw new TimeoutException("Handle recreation did not complete.");
            }

            throw new InvalidOperationException("Simulated BeginInvoke failure during handle recreation.");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                PostAttempted.Dispose();
                AllowPostFailure.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
