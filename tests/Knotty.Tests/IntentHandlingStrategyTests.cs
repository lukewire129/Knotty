using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Knotty.Core;
using Xunit;

namespace Knotty.Tests;

public class IntentHandlingStrategyTests
{
    public record StrategyTestState(int Counter = 0);

    public abstract record StrategyIntent
    {
        public record Slow(int Delay = 200) : StrategyIntent;
        public record Quick : StrategyIntent;
    }

    public class BlockStore : KnottyStore<StrategyTestState, StrategyIntent>
    {
        public BlockStore() : base(new StrategyTestState()) { }

        protected override IntentHandlingStrategy GetStrategy(StrategyIntent intent)
            => IntentHandlingStrategy.Block;

        protected override async Task HandleIntent(StrategyIntent intent, CancellationToken ct = default)
        {
            if (intent is StrategyIntent.Slow slow)
            {
                await Task.Delay(slow.Delay, ct);
                State = State with { Counter = State.Counter + 1 };
            }
        }
    }

    public class QueueStore : KnottyStore<StrategyTestState, StrategyIntent>
    {
        public QueueStore() : base(new StrategyTestState()) { }

        protected override IntentHandlingStrategy GetStrategy(StrategyIntent intent)
            => IntentHandlingStrategy.Queue;

        protected override async Task HandleIntent(StrategyIntent intent, CancellationToken ct = default)
        {
            if (intent is StrategyIntent.Slow slow)
            {
                await Task.Delay(slow.Delay, ct);
                State = State with { Counter = State.Counter + 1 };
            }
        }
    }

    public class CancelPreviousStore : KnottyStore<StrategyTestState, StrategyIntent>
    {
        public CancelPreviousStore() : base(new StrategyTestState()) { }

        protected override IntentHandlingStrategy GetStrategy(StrategyIntent intent)
            => IntentHandlingStrategy.CancelPrevious;

        protected override async Task HandleIntent(StrategyIntent intent, CancellationToken ct = default)
        {
            if (intent is StrategyIntent.Slow slow)
            {
                await Task.Delay(slow.Delay, ct);
                State = State with { Counter = State.Counter + 1 };
            }
        }
    }

    [Fact]
    public async Task BlockStrategy_DuringLoading_ShouldIgnoreNewIntents()
    {
        // Arrange
        var store = new BlockStore();

        // Act
        var task1 = store.DispatchAsync(new StrategyIntent.Slow(200));
        var task2 = store.DispatchAsync(new StrategyIntent.Slow(200)); // Should be blocked

        await task1;
        await task2;

        // Assert
        store.State.Counter.Should().Be(1); // Only first one executed
    }

    [Fact]
    public async Task QueueStrategy_ShouldProcessAllIntents()
    {
        // Arrange
        var store = new QueueStore();

        // Act
        var task1 = store.DispatchAsync(new StrategyIntent.Slow(50));
        var task2 = store.DispatchAsync(new StrategyIntent.Slow(50));
        var task3 = store.DispatchAsync(new StrategyIntent.Slow(50));

        await task1;
        await Task.Delay(200); // Wait for queue to process

        // Assert
        store.State.Counter.Should().Be(3); // All three executed
    }

    [Fact]
    public async Task CancelPreviousStrategy_ShouldCancelPreviousIntent()
    {
        // Arrange
        var store = new CancelPreviousStore();

        // Act
        var task1 = store.DispatchAsync(new StrategyIntent.Slow(200));
        await Task.Delay(50); // Let first one start
        var task2 = store.DispatchAsync(new StrategyIntent.Slow(200)); // Should cancel first

        await task2;
        await Task.Delay(100);

        // Assert
        store.State.Counter.Should().Be(1); // Only second one completed
    }
}
