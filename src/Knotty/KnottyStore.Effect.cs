using System;
using System.ComponentModel;

namespace Knotty.Core;

public abstract partial class KnottyStore<TState, TIntent>
    : INotifyPropertyChanged, INotifyPropertyChanging, INotifyDataErrorInfo, IDisposable
    where TState : class
{
    // IEffect로 받기
    protected void EmitEffect(IEffect effect)
    {
        OnEffect (effect);
    }

    // View에서 override
    protected virtual void OnEffect(IEffect effect) { }
}