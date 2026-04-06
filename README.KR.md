# Knotty

**MAUI, WPF, Avalonia를 위한 C# MVI 프레임워크.**
MVVM 보일러플레이트를 불변 State, 명시적 Intent, 자동 로딩/에러 처리로 대체합니다.

```bash
dotnet add package Knotty
```

```csharp
using Knotty;

// 1. State
public record CounterState(int Count = 0);

// 2. Intent
public abstract record CounterIntent
{
    public record Increment : CounterIntent;
    public record Reset    : CounterIntent;
}

// 3. Store — IsLoading, 에러 처리, INPC 모두 자동
public partial class CounterStore : KnottyStore<CounterState, CounterIntent>
{
    public CounterStore() : base(new CounterState()) { }

    [IntentCommand]
    private readonly CounterIntent.Increment _increment = new();

    [AsyncIntentCommand(CanExecute = nameof(CanReset))]
    private readonly CounterIntent.Reset _reset = new();

    protected override async Task HandleIntent(CounterIntent intent, CancellationToken ct)
    {
        switch (intent)
        {
            case CounterIntent.Increment:
                State = State with { Count = State.Count + 1 };
                break;
            case CounterIntent.Reset:
                await Task.Delay(500, ct);
                State = State with { Count = 0 };
                break;
        }
    }

    private bool CanReset() => !IsLoading;
}
```

```xml
<TextBlock Text="{Binding State.Count}" />
<Button Command="{Binding IncrementCommand}" Content="+" />
<Button Command="{Binding ResetCommand}"     Content="초기화" />
<ProgressBar Visibility="{Binding IsLoading, Converter={StaticResource BoolToVis}}" />
```

## 주요 기능

| | |
|---|---|
| **자동 IsLoading** | `HandleIntent` 실행 중 자동 토글 |
| **Source Generator** | `[IntentCommand]`로 `ICommand` 자동 생성 |
| **Intent 처리 전략** | Block · Queue · Debounce · CancelPrevious · Parallel |
| **Effect** | State를 오염시키지 않는 일회성 부수 효과 (네비게이션, 토스트) |
| **KnottyBus** | Store 간 이벤트 브로드캐스트 |
| **에러 처리** | `INotifyDataErrorInfo` 내장, 예외 자동 캡처 |
| **DI 지원** | net6.0+에서 `services.AddKnottyStore<MyStore>()` |

## 문서

**→ [Knotty 문서](https://lukewire129.github.io/knotty)**

## 라이선스

MIT
