using Knotty.Core;

namespace CounterApp;
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

    protected override async Task HandleIntent(CounterIntent intent)
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