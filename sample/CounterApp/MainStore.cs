using Knotty.Core;

namespace CounterApp;
public record MainState(int Count, string Message);
// 의도는 행동을 정의합니다.
public abstract record MainIntent
{
    public record Increment : MainIntent;
    public record Decrement : MainIntent;
    public record ResetAsync : MainIntent;
}

public class MainStore : KnottyStore<MainState, MainIntent>
{
    public MainStore() : base (new MainState (0, "준비 완료!")) { }

    protected override async Task HandleIntent(MainIntent intent)
    {
        switch (intent)
        {
            case MainIntent.Increment:
                State = State with { Count = State.Count + 1 };
                break;

            case MainIntent.Decrement:
                State = State with { Count = State.Count - 1 };
                break;

            case MainIntent.ResetAsync:
                State = State with { Message = "초기화 중..." };
                await Task.Delay (1000); // 비동기 작업 시뮬레이션
                State = new MainState (0, "초기화 완료!");
                break;
        }
    }
}