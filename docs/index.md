---
title: Home
nav_order: 1
---

# Knotty

**C# MVI framework for MAUI, WPF, and Avalonia.**

Knotty replaces MVVM boilerplate with a simple three-piece architecture:
immutable **State**, explicit **Intent**, and a single **Store** that handles all logic.

```bash
dotnet add package Knotty
```

---

## Why Knotty?

**MVVM의 문제**: `INotifyPropertyChanged`, `ObservableCollection`, `RelayCommand`, ViewModel 상속, 수동 `OnPropertyChanged`... 비즈니스 로직보다 인프라 코드가 더 많아진다.

**Knotty의 접근**: 상태는 record 하나, 행동은 Intent, 로직은 `HandleIntent` 하나. `IsLoading`, 에러 처리, INPC 모두 프레임워크가 처리.

---

## 30초 예제

```csharp
using Knotty;

public record CounterState(int Count = 0);

public abstract record CounterIntent
{
    public record Increment : CounterIntent;
    public record Reset    : CounterIntent;
}

public partial class CounterStore : KnottyStore<CounterState, CounterIntent>
{
    public CounterStore() : base(new CounterState()) { }

    [IntentCommand]
    private readonly CounterIntent.Increment _increment = new();

    [AsyncIntentCommand]
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
}
```

```xml
<TextBlock Text="{Binding State.Count}" />
<Button Command="{Binding IncrementCommand}" Content="+" />
<Button Command="{Binding ResetCommand}"     Content="Reset" />
<ProgressBar Visibility="{Binding IsLoading, ...}" />
```

---

## 문서 구성

| 챕터 | 내용 |
|------|------|
| [Getting Started](./getting-started) | 설치, 최소 예제, 핵심 흐름 |
| [Core Concepts](./core-concepts) | State · Intent · Store 설계 원칙 |
| [Platform Setup](./platform-setup) | WPF · MAUI · Avalonia bootstrapping |
| [View Binding](./view-binding) | DataContext, XAML 바인딩, Effect 구독 |
| [Commands](./commands) | Source Generator, `[IntentCommand]`, 파라미터 |
| [Intent Handling](./intent-handling) | Block · Queue · Debounce · CancelPrevious · Parallel |
| [Effects](./effects) | 네비게이션 · Toast · Dialog 패턴 |
| [Error Handling](./error-handling) | 예외 처리, 취소 복원, HasErrors |
| [KnottyBus](./knottybus) | Store 간 통신 |
| [Debugging](./debugging) | KnottyDebugger, 로그 |
| [API Reference](./api-reference) | 전체 public API 시그니처 |
