using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Knotty;
using Xunit;

namespace Knotty.Tests;

public class EffectTests
{
    // --- Helpers ---

    public record EffectState(int Counter = 0);

    public abstract record EffectIntent
    {
        public record Increment : EffectIntent;
        public record EmitMilestone(int Value) : EffectIntent;
    }

    public abstract record TestEffect : IEffect
    {
        public record Milestone(int Value) : TestEffect;
        public record Completed : TestEffect;
    }

    public class EffectStore : KnottyStore<EffectState, EffectIntent>
    {
        public EffectStore() : base(new EffectState()) { }

        protected override Task HandleIntent(EffectIntent intent, CancellationToken ct = default)
        {
            switch (intent)
            {
                case EffectIntent.Increment:
                    State = State with { Counter = State.Counter + 1 };
                    break;
                case EffectIntent.EmitMilestone e:
                    EmitEffect(new TestEffect.Milestone(e.Value));
                    break;
            }
            return Task.CompletedTask;
        }
    }

    // --- Tests ---

    [Fact]
    public async Task EmitEffect_ShouldDeliverToTypedSubscriber()
    {
        // Arrange
        var store = new EffectStore();
        TestEffect.Milestone? received = null;
        store.Effects.Subscribe<TestEffect.Milestone>(e => received = e);

        // Act
        await store.DispatchAsync(new EffectIntent.EmitMilestone(42));

        // Assert
        received.Should().NotBeNull();
        received!.Value.Should().Be(42);
    }

    [Fact]
    public async Task Subscribe_WithAllEffects_ShouldReceiveAnyEffect()
    {
        // Arrange
        var store = new EffectStore();
        var receivedEffects = new List<IEffect>();
        store.Effects.Subscribe(e => receivedEffects.Add(e));

        // Act
        await store.DispatchAsync(new EffectIntent.EmitMilestone(1));
        await store.DispatchAsync(new EffectIntent.EmitMilestone(2));

        // Assert
        receivedEffects.Should().HaveCount(2);
    }

    [Fact]
    public async Task Subscribe_TypeFiltered_ShouldIgnoreOtherEffectTypes()
    {
        // Arrange
        var store = new EffectStore();
        int completedCount = 0;
        // Completed 타입만 구독
        store.Effects.Subscribe<TestEffect.Completed>(_ => completedCount++);

        // Act
        await store.DispatchAsync(new EffectIntent.EmitMilestone(1)); // Milestone 방출, Completed 아님

        // Assert
        completedCount.Should().Be(0);
    }

    [Fact]
    public void EffectSubscription_Dispose_ShouldStopReceiving()
    {
        // Arrange
        var store = new EffectStore();
        int count = 0;
        var subscription = store.Effects.Subscribe<TestEffect.Milestone>(_ => count++);

        // Act
        store.Dispatch(new EffectIntent.EmitMilestone(1));
        subscription.Dispose();
        store.Dispatch(new EffectIntent.EmitMilestone(2)); // 구독 해제 후

        // Allow sync dispatch to complete
        Task.Delay(50).Wait();

        // Assert
        count.Should().Be(1); // 첫 번째만
    }

    [Fact]
    public void Dispose_ShouldCallOnCompleted_OnEffectSubject()
    {
        // Arrange
        var store = new EffectStore();
        bool completed = false;
        store.Effects.Subscribe(new CompletionObserver(() => completed = true));

        // Act
        store.Dispose();

        // Assert
        completed.Should().BeTrue();
    }

    private class CompletionObserver : IObserver<IEffect>
    {
        private readonly Action _onCompleted;
        public CompletionObserver(Action onCompleted) => _onCompleted = onCompleted;
        public void OnNext(IEffect value) { }
        public void OnError(Exception error) { }
        public void OnCompleted() => _onCompleted();
    }
}
