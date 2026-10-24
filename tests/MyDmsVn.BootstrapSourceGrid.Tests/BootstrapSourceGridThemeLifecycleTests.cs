using System;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;
using BootstrapSourceGridControl = MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapSourceGridThemeLifecycleTests
{
    [OneTimeSetUp]
    public void ConfigureWinForms()
    {
        WinFormsTestGuard.Configure();
    }

    [Test]
    public void CurrentSnapshotMatchesThemeAtConstruction()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        var theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

        try
        {
            BootstrapThemeManager.CurrentTheme = theme;

            using (var grid = new BootstrapSourceGridControl())
            {
                Assert.That(grid.CurrentThemeSnapshot.CellBackColor, Is.EqualTo(theme.Colors.Surface));
                Assert.That(grid.CurrentThemeSnapshot.CellForeColor, Is.EqualTo(theme.Colors.Text));
                Assert.That(grid.BackColor, Is.EqualTo(theme.Colors.Surface));
                Assert.That(grid.ForeColor, Is.EqualTo(theme.Colors.Text));
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void ThemeChangedUpdatesSnapshotWithoutCreatingHandle()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        var light = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

        try
        {
            BootstrapThemeManager.CurrentTheme = light;
            using (var grid = new BootstrapSourceGridControl())
            {
                var previousSnapshot = grid.CurrentThemeSnapshot;

                BootstrapThemeManager.CurrentTheme = dark;

                Assert.That(grid.CurrentThemeSnapshot, Is.Not.SameAs(previousSnapshot));
                Assert.That(grid.CurrentThemeSnapshot.CellBackColor, Is.EqualTo(dark.Colors.Surface));
                Assert.That(grid.CurrentThemeSnapshot.CellForeColor, Is.EqualTo(dark.Colors.Text));
                Assert.That(grid.IsHandleCreated, Is.False);
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void DisposedGridStopsReactingToThemeChanges()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        var light = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

        try
        {
            BootstrapThemeManager.CurrentTheme = light;
            var grid = new BootstrapSourceGridControl();
            var snapshotBeforeDisposal = grid.CurrentThemeSnapshot;
            grid.Dispose();

            BootstrapThemeManager.CurrentTheme = dark;

            Assert.That(grid.CurrentThemeSnapshot, Is.SameAs(snapshotBeforeDisposal));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void MultipleThemeSwitchesRemainStable()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        var light = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

        try
        {
            BootstrapThemeManager.CurrentTheme = light;
            using (var grid = new BootstrapSourceGridControl())
            {
                BootstrapThemeManager.CurrentTheme = dark;
                Assert.That(grid.CurrentThemeSnapshot.SelectionBackColor, Is.EqualTo(dark.Colors.Primary));

                BootstrapThemeManager.CurrentTheme = light;
                Assert.That(grid.CurrentThemeSnapshot.SelectionBackColor, Is.EqualTo(light.Colors.Primary));
                Assert.That(grid.BackColor, Is.EqualTo(light.Colors.Surface));
                Assert.That(grid.ForeColor, Is.EqualTo(light.Colors.Text));
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void ThemeChangedFromWorkerThreadIsMarshaledToCreatedHandle()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        var light = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

        try
        {
            BootstrapThemeManager.CurrentTheme = light;

            using (var grid = new ThreadTrackingBootstrapSourceGrid())
            {
                grid.CreateControl();
                Assert.That(grid.IsHandleCreated, Is.True);
                var uiThreadId = Thread.CurrentThread.ManagedThreadId;
                grid.ResetThemeApplicationThread();

                Exception? workerException = null;
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
                Assert.That(worker.Join(TimeSpan.FromSeconds(5)), Is.True);

                var deadline = DateTime.UtcNow.AddSeconds(5);
                while (grid.LastThemeApplicationThreadId == 0 &&
                    DateTime.UtcNow < deadline)
                {
                    Application.DoEvents();
                    Thread.Sleep(10);
                }

                Assert.That(workerException, Is.Null);
                Assert.That(grid.LastThemeApplicationThreadId, Is.EqualTo(uiThreadId));
                Assert.That(grid.CurrentThemeSnapshot.CellBackColor, Is.EqualTo(dark.Colors.Surface));
                Assert.That(grid.BackColor, Is.EqualTo(dark.Colors.Surface));
                Assert.That(grid.ForeColor, Is.EqualTo(dark.Colors.Text));
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    private sealed class ThreadTrackingBootstrapSourceGrid : BootstrapSourceGridControl
    {
        internal int LastThemeApplicationThreadId { get; private set; }

        internal void ResetThemeApplicationThread()
        {
            LastThemeApplicationThreadId = 0;
        }

        internal override void ApplyBootstrapTheme()
        {
            LastThemeApplicationThreadId = Thread.CurrentThread.ManagedThreadId;
            base.ApplyBootstrapTheme();
        }
    }
}
