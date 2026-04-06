using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Knotty;
using Xunit;

namespace Knotty.Tests;

public class DebounceAndParallelTests
{
    // --- Debounce Helpers ---

    public record DebounceState(int Counter = 0);

    public abstract record DebounceIntent
    {
        public record Tick : DebounceIntent;
    }

    public class DebounceStore : KnottyStore<DebounceState, DebounceIntent>
    {
        private readonly TimeSpan _delay;

        public DebounceStore(TimeSpan? delay = null) : base(new DebounceState())
        {
            _delay = delay ?? TimeSpan.FromMilliseconds(100);
        }

        protected override IntentHandlingStrategy GetStrategy(DebounceIntent intent)
            => IntentHandlingStrategy.Debounce;

        protected override TimeSpan GetDebounceDelay(DebounceIntent intent)
            => _delay;

        protected override Task HandleIntent(DebounceIntent intent, CancellationToken ct = default)
        {
            State = State with { Counter = State.Counter + 1 };
            return Task.CompletedTask;
        }
    }

    // --- Debounce Tests ---

    [Fact]
    public async Task DebounceStrategy_MultipleRapidIntents_ShouldExecuteOnlyLast()
    {
        // Arrange
        var store = new DebounceStore(TimeSpan.FromMilliseconds(100));

        // Act - 5개를 빠르게 보냄
        for (int i = 0; i < 5; i++)
        {
            store.Dispatch(new DebounceIntent.Tick());
            await Task.Delay(20);
        }

        // Debounce 타이머가 만료될 때까지 대기
        await Task.Delay(200);

        // Assert - 마지막 하나만 실행
        store.State.Counter.Should().Be(1);
    }

    [Fact]
    public async Task DebounceStrategy_SingleIntent_ShouldExecuteAfterDelay()
    {
        // Arrange
        var store = new DebounceStore(TimeSpan.FromMilliseconds(80));

        // Act
        store.Dispatch(new DebounceIntent.Tick());

        // 딜레이 전에는 아직 실행 안 됨
        await Task.Delay(40);
        store.State.Counter.Should().Be(0);

        // 딜레이 후에는 실행됨
        await Task.Delay(120);
        store.State.Counter.Should().Be(1);
    }

    [Fact]
    public async Task DebounceStrategy_Dispose_ShouldCancelPendingTimer()
    {
        // Arrange
        var store = new DebounceStore(TimeSpan.FromMilliseconds(200));

        // Act
        store.Dispatch(new DebounceIntent.Tick());
        await Task.Delay(50); // 타이머 도달 전에 dispose
        store.Dispose();
        await Task.Delay(300); // 원래라면 실행됐을 시간

        // Assert - dispose로 타이머가 취소됐으므로 실행 안 됨
        store.State.Counter.Should().Be(0);
    }

    // --- Parallel Helpers ---

    public record ParallelState(int ConcurrentPeak = 0);

    public abstract record ParallelIntent
    {
        public record SlowWork(int Delay = 100) : ParallelIntent;
    }

    public class ParallelStore : KnottyStore<ParallelState, ParallelIntent>
    {
        private int _inFlight;
        public int PeakConcurrency { get; private set; }

        public ParallelStore() : base(new ParallelState()) { }

        protected override IntentHandlingStrategy GetStrategy(ParallelIntent intent)
            => IntentHandlingStrategy.Parallel;

        protected override async Task HandleIntent(ParallelIntent intent, CancellationToken ct = default)
        {
            if (intent is ParallelIntent.SlowWork slow)
            {
                var current = System.Threading.Interlocked.Increment(ref _inFlight);
                if (current > PeakConcurrency) PeakConcurrency = current;
                await Task.Delay(slow.Delay, ct);
                System.Threading.Interlocked.Decrement(ref _inFlight);
            }
        }
    }

    // --- Parallel Tests ---

    [Fact]
    public async Task ParallelStrategy_MultipleIntents_ShouldRunConcurrently()
    {
        // Arrange
        var store = new ParallelStore();

        // Act - 3개를 동시에 dispatch (Block이면 1개씩 실행됨)
        store.Dispatch(new ParallelIntent.SlowWork(100));
        store.Dispatch(new ParallelIntent.SlowWork(100));
        store.Dispatch(new ParallelIntent.SlowWork(100));

        await Task.Delay(50); // 모두 실행 중일 때
        var peak = store.PeakConcurrency;

        await Task.Delay(200); // 완료 대기

        // Assert - 동시에 여러 개 실행됨을 확인
        peak.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task ParallelStrategy_ShouldNotToggleIsLoading()
    {
        // Arrange
        var store = new ParallelStore();

        // Act
        store.Dispatch(new ParallelIntent.SlowWork(100));
        await Task.Delay(20); // intent가 실행 중일 때

        // Assert - Parallel은 IsLoading을 건드리지 않음
        store.IsLoading.Should().BeFalse();
    }
}
