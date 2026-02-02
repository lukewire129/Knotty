using Knotty.Core;
using System.Diagnostics;

namespace QueueApp;
public record CounterState(int Count, string Message);
// 의도는 행동을 정의합니다.
public abstract record CounterIntent
{
    public record Increment : CounterIntent;
    public record Decrement : CounterIntent;
    public record Reset : CounterIntent;
}

public class MainStore : KnottyStore<CounterState, CounterIntent>
{
    public MainStore() : base (new CounterState (0, "준비 완료!")) { }

    protected override IntentHandlingStrategy GetStrategy(CounterIntent intent)
    {
        return intent switch
        {
            CounterIntent.Reset => IntentHandlingStrategy.CancelPrevious,
            CounterIntent.Increment => IntentHandlingStrategy.Parallel,  // ← 변경
            CounterIntent.Decrement => IntentHandlingStrategy.Parallel,  // ← 변경
            _ => IntentHandlingStrategy.Block
        };
    }

    protected override async Task HandleIntent(CounterIntent intent, CancellationToken ct)
    {
        switch (intent)
        {
            case CounterIntent.Increment:
                State = State with { Count = State.Count + 1 };
                Debug.WriteLine (State.Count);
                break;

            case CounterIntent.Decrement:
                State = State with { Count = State.Count - 1 };
                break;

            case CounterIntent.Reset:
                State = State with { Message = "초기화 중..." };
                await Task.Delay (3000); // 비동기 작업 시뮬레이션
                State = State with { Message = "초기화 완료!", Count = 0 };
                Debug.WriteLine (State.Count);
                await Task.Delay (3000); // 비동기 작업 시뮬레이션
                break;
        }
    }
}