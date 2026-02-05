using System;

namespace Knotty;

/// <summary>
/// Interface for exposing Effect stream to Views.
/// Used for auto-DI scenarios where View subscribes to DataContext's Effects.
/// </summary>
public interface IEffectSource
{
    /// <summary>
    /// Observable stream of effects emitted by the Store.
    /// </summary>
    IObservable<IEffect> Effects { get; }
}
