---
title: API Reference
nav_order: 12
---

# API Reference

전체 public API 시그니처. 상세 사용법은 각 챕터 참고.

---

## KnottyStore\<TState, TIntent\>

```csharp
namespace Knotty;

public abstract partial class KnottyStore<TState, TIntent>
    : INotifyPropertyChanged, INotifyPropertyChanging,
      INotifyDataErrorInfo, IEffectSource, IDisposable
    where TState : class
```

### Properties

```csharp
// 현재 State. protected setter — HandleIntent 내에서만 변경.
public TState State { get; protected set; }

// 비동기 Intent 처리 중 true. Block/Queue/CancelPrevious/Debounce 전략에서 자동 관리.
// Parallel 전략은 IsLoading을 건드리지 않음.
public bool IsLoading { get; }

// INotifyDataErrorInfo — Intent 처리 중 예외 발생 시 true.
public bool HasErrors { get; }

// Effect 스트림 (IObservable<IEffect>). View에서 구독.
public IObservable<IEffect> Effects { get; }
```

### Constructor

```csharp
protected KnottyStore(TState initialState);
// initialState == null → ArgumentNullException
```

### Public Methods

```csharp
// Fire-and-forget Dispatch. 예외는 OnDispatchError로 전달.
public void Dispatch(TIntent intent);

// await 가능한 Dispatch. 예외가 호출자에게 전파됨.
public async Task DispatchAsync(TIntent intent);
```

### Protected — Override Points

```csharp
// 필수 구현. intent를 처리하고 State를 업데이트.
protected abstract Task HandleIntent(TIntent intent, CancellationToken ct = default);

// Intent별 처리 전략 반환. 기본값: Block.
protected virtual IntentHandlingStrategy GetStrategy(TIntent intent);

// Debounce 대기 시간 반환. 기본값: 300ms.
protected virtual TimeSpan GetDebounceDelay(TIntent intent);

// HandleIntent에서 예외 발생 시 호출.
protected virtual void OnHandleError(Exception ex);

// GetStrategy/라우팅 단계에서 예외 발생 시 호출.
// Dispatch()의 fire-and-forget 예외를 여기서 처리.
protected virtual void OnDispatchError(TIntent intent, Exception ex);

// EmitEffect() 호출 시 동시에 호출됨. Store 내부에서 Effect 처리 시 사용.
protected virtual void OnEffect(IEffect effect);
```

### Protected — Helpers

```csharp
// KnottyBus에서 이 Store로 broadcast intent를 수신하도록 opt-in 등록.
protected IDisposable SubscribeToBus();

// Effect를 구독자에게 방출하고 OnEffect()를 호출.
protected void EmitEffect(IEffect effect);

// Command 팩토리 메서드
protected ICommand      Command(TIntent intent, Func<bool>? canExecute = null);
protected ICommand      Command<TParam>(Func<TParam, TIntent> factory, Func<TParam, bool>? canExecute = null);
protected IAsyncCommand AsyncCommand(TIntent intent, Func<bool>? canExecute = null);
protected IAsyncCommand AsyncCommand<TParam>(Func<TParam, TIntent> factory, Func<TParam, bool>? canExecute = null);

// INotifyDataErrorInfo 보조
protected void AddError(string propertyName, string error);
protected void ClearAllErrors();
```

### INotifyDataErrorInfo

```csharp
// key = Intent 타입 이름 (예: "Submit", "Load")
public IEnumerable? GetErrors(string propertyName);
public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
```

### Dispose

```csharp
// BusToken, EffectSubject, CTS, DebounceTimer, IntentQueue 정리.
// 멱등성 보장 — 여러 번 호출해도 안전.
public virtual void Dispose();
```

---

## IntentHandlingStrategy

```csharp
namespace Knotty;

public enum IntentHandlingStrategy
{
    Block,          // 처리 중이면 새 Intent 무시 (기본값)
    Queue,          // 순서대로 처리
    Debounce,       // 마지막 Intent만 처리 (타이머 기반)
    CancelPrevious, // 이전 작업 취소 후 새 것 처리
    Parallel        // IsLoading 건드리지 않고 동시 처리
}
```

---

## KnottyBus

```csharp
namespace Knotty;

public static class KnottyBus
```

```csharp
// recipient가 GC에 수거되면 자동 해제. IDisposable로 명시적 해제 권장.
public static IDisposable Subscribe<TIntent>(object recipient, Action<TIntent> handler);

// 등록된 모든 구독자에게 broadcast. record 상속 계층 전체에 전달.
public static void Publish<TIntent>(TIntent intent);

[Obsolete("Use Publish instead.")]
public static void Send<TIntent>(TIntent intent);
```

**동작**:
- `Publish`는 `intent.GetType()`부터 `BaseType`을 따라 올라가며 모든 구독자에게 전달.
- WeakReference 기반 — 수거된 recipient는 자동 제거.
- `Subscribe`는 opt-in. Store 생성자에서 `SubscribeToBus()`를 명시 호출해야 Bus 수신.

---

## Effect System

### IEffect

```csharp
namespace Knotty;

public interface IEffect;  // 마커 인터페이스
```

### IEffectSource

```csharp
namespace Knotty;

public interface IEffectSource
{
    IObservable<IEffect> Effects { get; }
}
```

### EffectExtensions

```csharp
namespace Knotty;

public static class EffectExtensions
{
    // TEffect 타입만 필터링하여 구독
    public static IDisposable Subscribe<TEffect>(
        this IObservable<IEffect> source,
        Action<TEffect> handler)
        where TEffect : IEffect;

    // 모든 Effect 구독
    public static IDisposable Subscribe(
        this IObservable<IEffect> source,
        Action<IEffect> handler);
}
```

---

## Commands

### IAsyncCommand

```csharp
namespace Knotty;

public interface IAsyncCommand : ICommand, INotifyPropertyChanged
{
    Task ExecuteAsync(object? parameter);
    bool IsExecuting { get; }
}
```

`IsExecuting == true`이면 `CanExecute()`가 자동으로 `false` 반환.

### Attributes (Source Generator)

```csharp
namespace Knotty;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
public sealed class IntentCommandAttribute : Attribute
{
    public string? CommandName { get; set; }   // 생성할 프로퍼티 이름 (옵션)
    public string? CanExecute  { get; set; }   // CanExecute 메서드/프로퍼티 이름 (옵션)
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
public sealed class AsyncIntentCommandAttribute : Attribute
{
    public string? CommandName { get; set; }
    public string? CanExecute  { get; set; }
}
```

**이름 생성 규칙**: 접두어 `_`, `Create`, `Get`, `Make` 제거 → PascalCase + `Command` 접미어.

---

## DI Extension (net6.0+)

```csharp
namespace Knotty;

public static class ServiceCollectionExtensions
{
    // Store를 Singleton으로 등록
    public static IServiceCollection AddKnottyStore<TStore>(
        this IServiceCollection services)
        where TStore : class, IDisposable;

    // 팩토리로 Singleton 등록
    public static IServiceCollection AddKnottyStore<TStore>(
        this IServiceCollection services,
        Func<IServiceProvider, TStore> factory)
        where TStore : class, IDisposable;
}
```

> `netstandard2.0` 타겟에서는 컴파일 제외됨.

---

## KnottyDebugger

```csharp
namespace Knotty;

public static class KnottyDebugger
{
    // 활성화 여부 (기본값 false)
    public static bool IsEnabled { get; set; }

    // 기록된 로그 전체
    public static IReadOnlyList<LogEntry> Logs { get; }

    // 로그 초기화
    public static void Clear();

    // JSON 파일로 내보내기 (net5.0+)
    public static async Task ExportToFileAsync(string path);
}
```

```csharp
// net5.0+: record
// netstandard2.0: class (동일 멤버)
public record LogEntry(
    DateTime Timestamp,
    string   StoreType,
    string   IntentType,   // Intent 타입 이름 또는 "StateChanged"
    object?  Intent,
    object?  OldState,
    object?  NewState
);
```

---

## Analyzer Diagnostics

Source Generator 패키지(`Knotty`)에 포함된 Roslyn Analyzer.

| 코드 | 수준 | 조건 | 메시지 |
|------|------|------|--------|
| `KNOT001` | Warning | `TState`가 record가 아닌 경우 | `TState '{TypeName}' is not a record. Knotty requires immutable state — use a record type.` |
| `KNOT002` | Warning | record의 property에 mutable setter가 있는 경우 | `Property '{PropName}' on state type '{TypeName}' has a setter. State properties should be init-only.` |

```csharp
// KNOT001 발생
public class CounterStore : KnottyStore<CounterState, CounterIntent>  // ← class, not record
{ }

// KNOT002 발생
public record OrderState
{
    public List<Item> Items { get; set; }  // ← { get; set; } 대신 { get; init; } 사용
}
```
