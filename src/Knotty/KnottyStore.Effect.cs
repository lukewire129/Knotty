using System;
using System.ComponentModel;
using Knotty.Internal;

namespace Knotty.Core;

public abstract partial class KnottyStore<TState, TIntent>
    : INotifyPropertyChanged, INotifyPropertyChanging, INotifyDataErrorInfo, IDisposable, IEffectSource
    where TState : class
{
    private readonly EffectSubject _effectSubject = new();

    /// <summary>
    /// Observable stream of effects for View subscription.
    /// Used in auto-DI scenarios: ((IEffectSource)DataContext).Effects.Subscribe(...)
    /// </summary>
    public IObservable<IEffect> Effects => _effectSubject;

    /// <summary>
    /// Emit an effect to subscribers.
    /// </summary>
    protected void EmitEffect(IEffect effect)
    {
        _effectSubject.OnNext(effect);
        OnEffect(effect);
    }

    /// <summary>
    /// Override to handle effects directly in Store (optional).
    /// </summary>
    protected virtual void OnEffect(IEffect effect) { }
}