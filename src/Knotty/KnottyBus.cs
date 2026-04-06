using System;
using System.Collections.Generic;
using System.Linq;

namespace Knotty;
public static class KnottyBus
{
    private class Subscription
    {
        public WeakReference Target { get; set; } = null!;
        public Action<object> Action { get; set; } = null!;
        public Type IntentType { get; set; } = null!;
    }

    private static readonly Dictionary<Type, List<Subscription>> _subs = new Dictionary<Type, List<Subscription>> ();

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
                _subs[typeof (TIntent)] = new List<Subscription> ();
            _subs[typeof (TIntent)].Add (sub);
        }
        return new Unsubscriber (sub);
    }

    /// <summary>
    /// 등록된 모든 구독자에게 intent를 broadcast합니다.
    /// record 상속 구조를 지원하여 부모 타입의 구독자에게도 전달됩니다.
    /// </summary>
    public static void Publish<TIntent>(TIntent intent)
    {
        if (intent == null)
            return;

        // 현재 타입부터 부모 타입까지 훑습니다 (record 상속 대응)
        var currentType = intent.GetType ();

        lock (_subs)
        {
            while (currentType != null)
            {
                if (_subs.TryGetValue (currentType, out var list))
                {
                    list.RemoveAll (s => !s.Target.IsAlive);
                    // 리스트의 복사본을 만들어 실행 (동시성 안전)
                    var activeSubs = list.ToList ();
                    activeSubs.ForEach (s => s.Action (intent));
                }
                currentType = currentType.BaseType;
            }
        }
    }

    /// <inheritdoc cref="Publish{TIntent}"/>
    [Obsolete ("Use Publish instead.")]
    public static void Send<TIntent>(TIntent intent) => Publish (intent);

    private class Unsubscriber : IDisposable
    {
        private readonly Subscription _sub;
        public Unsubscriber(Subscription sub) => _sub = sub;
        public void Dispose() { lock (_subs) { if (_subs.TryGetValue (_sub.IntentType, out var list)) list.Remove (_sub); } }
    }
}