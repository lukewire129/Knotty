using Knotty.Core;

namespace DebounceApp;
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
        return intent is CounterIntent.Reset 
            ? IntentHandlingStrategy.Debounce 
            : base.GetStrategy(intent);
    }

    protected override TimeSpan GetDebounceDelay(CounterIntent intent)
    {
        return TimeSpan.FromSeconds(1);
    }

    protected override async Task HandleIntent(CounterIntent intent, CancellationToken ct)
    {
        switch (intent)
        {
            case CounterIntent.Increment:
                State = State with { Count = State.Count + 1 };
                break;

            case CounterIntent.Decrement:
                State = State with { Count = State.Count - 1 };
                break;

            case CounterIntent.Reset:
                State = State with { Message = "초기화 중..." };
                await Task.Delay (1000); // 비동기 작업 시뮬레이션
                State = State with { Message = "초기화 완료!", Count = 0 };
                break;
        }
    }
}