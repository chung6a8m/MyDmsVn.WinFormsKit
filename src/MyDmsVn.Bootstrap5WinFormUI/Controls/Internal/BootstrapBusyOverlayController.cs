using System;
using System.Collections.Generic;
using System.Threading;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal sealed class BootstrapBusyOverlayController : IDisposable
{
    private readonly IBootstrapBusyDelay _delay;
    private readonly Func<bool> _canPresent;
    private readonly Action<BusyPresentation> _present;
    private readonly Action _hide;
    private readonly int _thread = Thread.CurrentThread.ManagedThreadId;
    private readonly List<Token> _tokens = new();
    private bool _disposed, _pending, _visible;
    private long _generation;

    internal BootstrapBusyOverlayController(IBootstrapBusyDelay delay, Func<bool> canPresent, Action<BusyPresentation> present, Action hide)
    {
        _delay = delay ?? throw new ArgumentNullException(nameof(delay));
        _canPresent = canPresent ?? throw new ArgumentNullException(nameof(canPresent));
        _present = present ?? throw new ArgumentNullException(nameof(present));
        _hide = hide ?? throw new ArgumentNullException(nameof(hide));
    }

    internal IDisposable Begin(BusyPresentation presentation)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BootstrapBusyOverlayController));
        AssertThread();
        if (presentation is null) throw new ArgumentNullException(nameof(presentation));
        var token = new Token(this, presentation);
        _tokens.Add(token);
        RefreshVisibility();
        return token;
    }

    internal void RefreshVisibility()
    {
        if (_disposed) return;
        AssertThread();
        if (_tokens.Count == 0 || !_canPresent())
        {
            Hide();
            return;
        }
        if (_visible)
        {
            _present(_tokens[_tokens.Count - 1].Presentation);
            return;
        }
        if (_pending) return;
        _pending = true;
        var generation = ++_generation;
        _delay.Schedule(TimeSpan.FromMilliseconds(150), () => Show(generation));
    }

    internal void RequestCancel()
    {
        if (_disposed) return;
        AssertThread();
        if (_visible && _tokens.Count > 0)
            _tokens[_tokens.Count - 1].Presentation.CancelRequested?.Invoke();
    }

    public void Dispose()
    {
        if (_disposed) return;
        AssertThread();
        _disposed = true;
        _tokens.Clear();
        Hide();
        _delay.Dispose();
    }

    private void Show(long generation)
    {
        if (_disposed || !_pending || generation != _generation) return;
        AssertThread();
        _pending = false;
        if (_tokens.Count == 0 || !_canPresent()) { Hide(); return; }
        _visible = true; // Commit before calling code which may reenter Hide/Begin/Dispose.
        _present(_tokens[_tokens.Count - 1].Presentation);
    }

    private void Hide()
    {
        ++_generation;
        var pending = _pending;
        _pending = false;
        _visible = false;
        if (pending) _delay.Cancel();
        _hide();
    }

    private void Release(Token token)
    {
        if (_disposed) return;
        AssertThread();
        if (_tokens.Remove(token)) RefreshVisibility();
    }

    private void AssertThread()
    {
        if (Thread.CurrentThread.ManagedThreadId != _thread)
            throw new InvalidOperationException("Busy overlay must be managed on the owning UI thread.");
    }

    private sealed class Token : IDisposable
    {
        private BootstrapBusyOverlayController? _owner;
        internal BusyPresentation Presentation { get; }
        internal Token(BootstrapBusyOverlayController owner, BusyPresentation presentation) { _owner = owner; Presentation = presentation; }
        public void Dispose()
        {
            var owner = _owner;
            if (owner is null) return;
            owner.Release(this);
            _owner = null;
        }
    }
}
