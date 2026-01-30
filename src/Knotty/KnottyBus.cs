using System;
using System.Collections.Generic;

namespace Knotty.Core;

public static class KnottyBus
{
    private class Subscription
    {
        public WeakReference Target { get; set; }
        public Action<object> Action { get; set; }
        public Type IntentType { get; set; }
    }

    private static readonly Dictionary<Type, List<Subscription>> _subs = new ();

    public static IDisposable Subscribe<TIntent>(object recipient, Action<TIntent> action)
    {
        var sub = new Subscription
        {
            Target = new WeakReference (recipient),
            Action = obj => action ((TIntent)obj),
            IntentType = typeof (TIntent)
        };
        lock (_subs)
        {
            if (!_subs.ContainsKey (typeof (TIntent)))
                _subs[typeof (TIntent)] = new ();
            _subs[typeof (TIntent)].Add (sub);
        }
        return new Unsubscriber (sub);
    }

    public static void Send<TIntent>(TIntent intent)
    {
        InternalSend (intent);
    }

    private static void InternalSend<TIntent>(TIntent intent)
    {
        lock (_subs)
        {
            if (_subs.TryGetValue (typeof (TIntent), out var list))
            {
                list.RemoveAll (s => !s.Target.IsAlive);
                list.ForEach (s => s.Action (intent));
            }
        }
    }

    private class Unsubscriber : IDisposable
    {
        private readonly Subscription _sub;
        public Unsubscriber(Subscription sub) => _sub = sub;
        public void Dispose() { lock (_subs) { if (_subs.TryGetValue (_sub.IntentType, out var list)) list.Remove (_sub); } }
    }
}}
