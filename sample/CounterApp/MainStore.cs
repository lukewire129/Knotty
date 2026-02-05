using Knotty;
using Knotty.Core;
using Knotty.Core.Attributes;

namespace CounterApp;

public record CounterState(int Count, string Message);

// Effect 정의 - 일회성 부수 효과
public abstract record CounterEffect : IEffect
{
    public record Milestone(int Count) : CounterEffect;  // 10 단위 달성 시
}

// 의도는 행동을 정의합니다.
public abstract record CounterIntent
{
    public record Increment : CounterIntent;
    public record Decrement : CounterIntent;
    public record IncrementBy(int Value) : CounterIntent;  // 파라미터 있는 Intent
    public record Reset : CounterIntent;
}

public partial class MainStore : KnottyStore<CounterState, CounterIntent>
{
    // 필드 기반 Command (파라미터 없음)
    [IntentCommand]
    private readonly CounterIntent.Increment _increment = new();

    [IntentCommand]
    private readonly CounterIntent.Decrement _decrement = new();

    // 메서드 기반 Command (파라미터 있음) - XAML의 CommandParameter는 string으로 전달됨
    [IntentCommand]
    private CounterIntent.IncrementBy CreateIncrementBy(string value) => new(int.Parse(value));

    [AsyncIntentCommand(CanExecute = nameof(CanReset))]
    private readonly CounterIntent.Reset _reset = new();

    private bool CanReset() => !IsLoading;

    public MainStore() : base (new CounterState (0, "준비 완료!")) { }

    protected override async Task HandleIntent(CounterIntent intent, CancellationToken ct)
    {
        var prevCount = State.Count;

        switch (intent)
        {
            case CounterIntent.Increment:
                State = State with { Count = State.Count + 1 };
                CheckMilestone(prevCount, State.Count);
                break;

            case CounterIntent.Decrement:
                State = State with { Count = State.Count - 1 };
                break;

            case CounterIntent.IncrementBy(var value):
                State = State with { Count = State.Count + value };
                CheckMilestone(prevCount, State.Count);
                break;

            case CounterIntent.Reset:
                State = State with { Message = "초기화 중..." };
                await Task.Delay (1000); // 비동기 작업 시뮬레이션
                State = State with { Message = "초기화 완료!", Count = 0 };
                break;
        }
    }

    // 10 단위를 처음 넘었을 때만 Effect 발생
    // 예: 9→10 ✓, 9→11 ✓, 10→11 ✗, 19→20 ✓, 8→22 ✓
    private void CheckMilestone(int prevCount, int newCount)
    {
        if (newCount > 0 && prevCount / 10 < newCount / 10)
        {
            // 새로 도달한 10 단위 (8→22면 20)
            var milestone = (newCount / 10) * 10;
            EmitEffect(new CounterEffect.Milestone(newCount));
        }
    }
}