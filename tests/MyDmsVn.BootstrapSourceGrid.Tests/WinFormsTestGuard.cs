using System;
using System.Windows.Forms;

namespace MyDmsVn.BootstrapSourceGrid.Tests;

internal static class WinFormsTestGuard
{
    private static readonly object SyncRoot = new object();
    private static bool _configured;

    internal static bool IsConfigured
    {
        get
        {
            lock (SyncRoot)
            {
                return _configured;
            }
        }
    }

    public static void Configure()
    {
        lock (SyncRoot)
        {
            if (_configured)
            {
                return;
            }

            Application.SetUnhandledExceptionMode(
                UnhandledExceptionMode.ThrowException,
                threadScope: false);
            _configured = true;
        }
    }

    internal static UserExceptionCapture CaptureUserExceptions(SourceGrid.GridVirtual grid)
    {
        Configure();
        return new UserExceptionCapture(grid);
    }

    internal sealed class UserExceptionCapture : IDisposable
    {
        private SourceGrid.GridVirtual? _grid;

        internal UserExceptionCapture(SourceGrid.GridVirtual grid)
        {
            _grid = grid;
            grid.UserException += OnUserException;
        }

        internal Exception? Exception { get; private set; }

        internal int Count { get; private set; }

        public void Dispose()
        {
            var grid = _grid;
            _grid = null;
            if (grid is not null)
            {
                grid.UserException -= OnUserException;
            }
        }

        private void OnUserException(object sender, SourceGrid.ExceptionEventArgs e)
        {
            Count++;
            Exception = e.Exception;
            e.Handled = true;
        }
    }
}
