using System;
using System.Collections.Generic;

namespace Knotty.Internal;

/// <summary>
/// Simple Subject implementation for Effect stream without System.Reactive dependency.
/// </summary>
internal sealed class EffectSubject : IObservable<IEffect>
{
    private readonly object _lock = new();
    private readonly List<IObserver<IEffect>> _observers = new();

    public IDisposable Subscribe(IObserver<IEffect> observer)
    {
        lock (_lock)
        {
            _observers.Add(observer);
        }
        return new Unsubscriber(this, observer);
    }

    public void OnNext(IEffect effect)
    {
        IObserver<IEffect>[] snapshot;
        lock (_lock)
        {
            snapshot = _observers.ToArray();
        }

        foreach (var observer in snapshot)
        {
            observer.OnNext(effect);
        }
    }

    public void OnCompleted()
    {
        IObserver<IEffect>[] snapshot;
        lock (_lock)
        {
            snapshot = _observers.ToArray();
            _observers.Clear();
        }

        foreach (var observer in snapshot)
        {
            observer.OnCompleted();
        }
    }

    private void Remove(IObserver<IEffect> observer)
    {
        lock (_lock)
        {
            _observers.Remove(observer);
        }
    }

    private sealed class Unsubscriber : IDisposable
    {
        private readonly EffectSubject _subject;
        private readonly IObserver<IEffect> _observer;

        public Unsubscriber(EffectSubject subject, IObserver<IEffect> observer)
        {
            _subject = subject;
            _observer = observer;
        }

        public void Dispose() => _subject.Remove(_observer);
    }
}
