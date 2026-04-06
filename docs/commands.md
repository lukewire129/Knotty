---
title: Commands
nav_order: 6
---

# Commands & Source Generator

## Source Generator (권장)

`dotnet add package Knotty` 시 Source Generator가 자동 포함됨.
Store 클래스에 `partial` 키워드와 `[IntentCommand]` / `[AsyncIntentCommand]` 어트리뷰트만 붙이면 `ICommand` 프로퍼티가 자동 생성됨.

### Field 기반 — 파라미터 없는 Command

```csharp
public partial class CounterStore : KnottyStore<CounterState, CounterIntent>
{
    [IntentCommand]
    private readonly CounterIntent.Increment _increment = new();

    [IntentCommand]
    private readonly CounterIntent.Decrement _decrement = new();

    [AsyncIntentCommand(CanExecute = nameof(CanReset))]
    private readonly CounterIntent.Reset _reset = new();

    private bool CanReset() => !IsLoading;
}
```

**생성 결과**

```csharp
// 자동 생성 (수정 불필요)
partial class CounterStore
{
    private ICommand? _incrementCommand;
    public ICommand IncrementCommand => _incrementCommand ??= Command(_increment);

    private ICommand? _decrementCommand;
    public ICommand DecrementCommand => _decrementCommand ??= Command(_decrement);

    private IAsyncCommand? _resetCommand;
    public IAsyncCommand ResetCommand => _resetCommand ??= AsyncCommand(_reset, CanReset);
}
```

### Method 기반 — XAML CommandParameter 있는 Command

XAML `CommandParameter`는 항상 `string`. 메서드에서 타입 변환.

```csharp
public partial class CounterStore : KnottyStore<CounterState, CounterIntent>
{
    // CommandParameter(string) → IncrementBy(int)
    [IntentCommand]
    private CounterIntent.IncrementBy CreateIncrementBy(string value)
        => new(int.Parse(value));

    [AsyncIntentCommand]
    private CounterIntent.LoadData CreateLoadData(string url)
        => new(url);
}
```

```xml
<Button Command="{Binding IncrementByCommand}" CommandParameter="5" Content="+5" />
<Button Command="{Binding LoadDataCommand}" CommandParameter="https://api.example.com/data" />
```

### 이름 생성 규칙

| 멤버 이름 | 생성되는 Command 이름 |
|-----------|----------------------|
| `_increment` | `IncrementCommand` |
| `_reset` | `ResetCommand` |
| `CreateIncrementBy` | `IncrementByCommand` |
| `GetSearchResults` | `SearchResultsCommand` |
| `MakeFilter` | `FilterCommand` |

앞의 `_`, `Create`, `Get`, `Make` 접두어 제거 후 `Command` 접미어 추가.

### CommandName으로 이름 지정

```csharp
[IntentCommand(CommandName = "AddItemCommand")]
private readonly CounterIntent.Increment _inc = new();
// → public ICommand AddItemCommand
```

---

## 수동 생성 (Source Generator 없이)

### 기본 Command

```csharp
public class CounterStore : KnottyStore<CounterState, CounterIntent>
{
    public ICommand IncrementCommand { get; }
    public IAsyncCommand ResetCommand { get; }

    public CounterStore() : base(new CounterState(0))
    {
        IncrementCommand = Command(new CounterIntent.Increment());
        ResetCommand = AsyncCommand(new CounterIntent.Reset(), () => !IsLoading);
    }
}
```

### 파라미터 Command

```csharp
public ICommand IncrementByCommand { get; }

public CounterStore() : base(new CounterState(0))
{
    IncrementByCommand = Command<string>(
        value => new CounterIntent.IncrementBy(int.Parse(value)));
}
```

---

## IAsyncCommand

`AsyncCommand` / `[AsyncIntentCommand]`로 생성한 Command는 `IAsyncCommand`를 구현.

```csharp
public interface IAsyncCommand : ICommand, INotifyPropertyChanged
{
    Task ExecuteAsync(object? parameter);
    bool IsExecuting { get; }
}
```

`IsExecuting`이 `true`이면 `CanExecute()`가 자동으로 `false` 반환.

```xml
<!-- 실행 중 버튼 비활성화 -->
<Button Command="{Binding ResetCommand}"
        IsEnabled="{Binding ResetCommand.IsExecuting, Converter={StaticResource InverseBool}}" />

<!-- 실행 중 스피너 표시 -->
<ProgressBar Visibility="{Binding ResetCommand.IsExecuting, Converter={StaticResource BoolToVis}}" />
```

---

## 헬퍼 메서드 시그니처

```csharp
// Store 내부에서 사용 가능한 protected 메서드
protected ICommand      Command(TIntent intent, Func<bool>? canExecute = null);
protected ICommand      Command<TParam>(Func<TParam, TIntent> factory, Func<TParam, bool>? canExecute = null);
protected IAsyncCommand AsyncCommand(TIntent intent, Func<bool>? canExecute = null);
protected IAsyncCommand AsyncCommand<TParam>(Func<TParam, TIntent> factory, Func<TParam, bool>? canExecute = null);
```

---

## 주의사항

- Source Generator 사용 시 Store 클래스에 `partial` **필수**
- `[RelayCommand]` (CommunityToolkit.Mvvm) 혼용 금지
- 하나의 Intent에 Manual과 Generator 방식 혼용 금지
