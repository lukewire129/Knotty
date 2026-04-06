---
title: Getting Started
nav_order: 2
---

# Getting Started

## 설치

```bash
dotnet add package Knotty
```

NuGet 패키지에 Source Generator(analyzer)가 포함되어 있어 별도 설치 불필요.

**지원 타겟 프레임워크**: `netstandard2.0` · `net6.0` · `net8.0`

---

## 핵심 흐름

```
사용자 입력
    ↓
Command.Execute() 또는 store.Dispatch(intent)
    ↓
KnottyStore.HandleIntent(intent, ct)     ← 여기에 비즈니스 로직 전부
    ↓
State = State with { ... }               ← PropertyChanged 자동 발생
    ↓
View 바인딩 자동 갱신
    ↓
EmitEffect(effect)  [선택]               ← 네비게이션, 토스트 등
```

---

## 최소 구현 순서

### 1. State 정의

```csharp
// 불변 record. 모든 UI 상태를 담음.
public record CounterState(int Count = 0, string Message = "Ready");
```

### 2. Intent 정의

```csharp
// 사용자가 할 수 있는 모든 행동의 목록.
public abstract record CounterIntent
{
    public record Increment            : CounterIntent;
    public record IncrementBy(int N)   : CounterIntent;
    public record Reset                : CounterIntent;
}
```

### 3. Store 구현

```csharp
using Knotty;

public partial class CounterStore : KnottyStore<CounterState, CounterIntent>
{
    public CounterStore() : base(new CounterState()) { }

    // Source Generator가 IncrementCommand, ResetCommand 자동 생성
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

            case CounterIntent.IncrementBy by:
                State = State with { Count = State.Count + by.N };
                break;

            case CounterIntent.Reset:
                await Task.Delay(500, ct);       // ct 반드시 전달
                State = State with { Count = 0, Message = "Reset!" };
                break;
        }
    }

    private bool CanReset() => !IsLoading;
}
```

### 4. View 연결 (WPF 예시)

```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new CounterStore();
    }
}
```

```xml
<TextBlock Text="{Binding State.Count}" />
<TextBlock Text="{Binding State.Message}" />

<Button Command="{Binding IncrementCommand}" Content="+" />
<Button Command="{Binding ResetCommand}"     Content="Reset" />

<ProgressBar Visibility="{Binding IsLoading, Converter={StaticResource BoolToVis}}" />
```

---

## 자동으로 제공되는 것

Store를 만들면 추가 코드 없이 다음이 동작함:

| 기능 | 조건 |
|------|------|
| `IsLoading = true` | `HandleIntent` 진입 시 |
| `IsLoading = false` | `HandleIntent` 종료 시 (예외 포함) |
| `PropertyChanged` | `State = State with { ... }` 시 |
| `HasErrors = true` | `HandleIntent`에서 예외 발생 시 |
| `ClearAllErrors()` | 다음 intent 진입 시 자동 초기화 |

---

## 다음 단계

- 플랫폼별 DI 설정 → [Platform Setup](./platform-setup)
- XAML 바인딩 패턴 상세 → [View Binding](./view-binding)
- 동시성 제어 (debounce, cancel) → [Intent Handling](./intent-handling)
