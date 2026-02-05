using System;

namespace Knotty;

/// <summary>
/// Extension methods for subscribing to Effect streams.
/// </summary>
public static class EffectExtensions
{
    /// <summary>
    /// Subscribe to effects of a specific type.
    /// </summary>
    /// <typeparam name="TEffect">The effect type to filter and handle.</typeparam>
    /// <param name="source">The effect observable source.</param>
    /// <param name="handler">Action to handle the effect.</param>
    /// <returns>Disposable to unsubscribe.</returns>
    public static IDisposable Subscribe<TEffect>(this IObservable<IEffect> source, Action<TEffect> handler)
        where TEffect : IEffect
    {
        return source.Subscribe(new FilteredEffectObserver<TEffect>(handler));
    }

    /// <summary>
    /// Subscribe to all effects.
    /// </summary>
    /// <param name="source">The effect observable source.</param>
    /// <param name="handler">Action to handle any effect.</param>
    /// <returns>Disposable to unsubscribe.</returns>
    public static IDisposable Subscribe(this IObservable<IEffect> source, Action<IEffect> handler)
    {
        return source.Subscribe(new EffectObserver(handler));
    }

    private sealed class FilteredEffectObserver<TEffect> : IObserver<IEffect>
        where TEffect : IEffect
    {
        private readonly Action<TEffect> _handler;

        public FilteredEffectObserver(Action<TEffect> handler) => _handler = handler;

        public void OnNext(IEffect value)
        {
            if (value is TEffect effect)
            {
                _handler(effect);
            }
        }

        public void OnError(Exception error) { }
        public void OnCompleted() { }
    }

    private sealed class EffectObserver : IObserver<IEffect>
    {
        private readonly Action<IEffect> _handler;

        public EffectObserver(Action<IEffect> handler) => _handler = handler;

        public void OnNext(IEffect value) => _handler(value);
        public void OnError(Exception error) { }
        public void OnCompleted() { }
    }
}
