using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Knotty;
using Knotty.Tests.TestHelpers;
using Xunit;

namespace Knotty.Tests;

public class DisposeTests
{
    [Fact]
    public void Dispose_CalledTwice_ShouldNotThrow()
    {
        // Arrange
        var store = new TestStore();

        // Act
        store.Dispose();
        var act = () => store.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task Dispose_ShouldCancelOngoingCancelPreviousWork()
    {
        // Arrange
        var store = new CancelPreviousDisposeStore();
        var cancelled = false;

        // Act
        var task = store.DispatchAsync(new CancelPreviousDisposeIntent.LongRunning());
        await Task.Delay(50); // 작업이 시작될 때까지 대기

        store.Dispose(); // CTS를 취소해야 함

        // 취소 신호를 전파하기 위한 짧은 대기
        await Task.Delay(50);
        cancelled = store.WasCancelled;

        // Assert
        cancelled.Should().BeTrue();
    }

    public record CancelPreviousDisposeState(int Counter = 0);

    public abstract record CancelPreviousDisposeIntent
    {
        public record LongRunning : CancelPreviousDisposeIntent;
    }

    public class CancelPreviousDisposeStore : KnottyStore<CancelPreviousDisposeState, CancelPreviousDisposeIntent>
    {
        public bool WasCancelled { get; private set; }

        public CancelPreviousDisposeStore() : base(new CancelPreviousDisposeState()) { }

        protected override IntentHandlingStrategy GetStrategy(CancelPreviousDisposeIntent intent)
            => IntentHandlingStrategy.CancelPrevious;

        protected override async Task HandleIntent(CancelPreviousDisposeIntent intent, CancellationToken ct = default)
        {
            try
            {
                await Task.Delay(500, ct);
            }
            catch (OperationCanceledException)
            {
                WasCancelled = true;
                throw;
            }
        }
    }

    [Fact]
    public async Task OnDispatchError_ShouldBeCalledForFrameworkLevelExceptions()
    {
        // Arrange
        var store = new BrokenStrategyStore();

        // Act
        store.Dispatch(new BrokenStrategyIntent.Trigger());
        await Task.Delay(50);

        // Assert
        store.DispatchErrorCalled.Should().BeTrue();
    }

    public record BrokenStrategyState();

    public abstract record BrokenStrategyIntent
    {
        public record Trigger : BrokenStrategyIntent;
    }

    public class BrokenStrategyStore : KnottyStore<BrokenStrategyState, BrokenStrategyIntent>
    {
        public bool DispatchErrorCalled { get; private set; }

        public BrokenStrategyStore() : base(new BrokenStrategyState()) { }

        protected override IntentHandlingStrategy GetStrategy(BrokenStrategyIntent intent)
            => throw new InvalidOperationException("Strategy failed!");

        protected override Task HandleIntent(BrokenStrategyIntent intent, CancellationToken ct = default)
            => Task.CompletedTask;

        protected override void OnDispatchError(BrokenStrategyIntent intent, Exception ex)
            => DispatchErrorCalled = true;
    }
}
