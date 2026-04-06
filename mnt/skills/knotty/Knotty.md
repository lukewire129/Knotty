# Knotty — State / Intent / Store

## Namespace

```csharp
using Knotty;  // 모든 Knotty 타입 포함
```

## KnottyStore 시그니처

```csharp
public abstract partial class KnottyStore<TState, TIntent>
    : INotifyPropertyChanged, INotifyPropertyChanging,
      INotifyDataErrorInfo, IEffectSource, IDisposable
    where TState : class   // record 권장 — KNOT001 analyzer 경고
```

### 생성자

```csharp
protected KnottyStore(TState initialState)
// initialState가 null이면 ArgumentNullException
```

### State 프로퍼티

```csharp
public TState State { get; protected set; }
```

- `State = State with { ... }` 대입 시 **자동으로** `PropertyChanging` → `PropertyChanged` 발생
- 같은 참조값으로 대입하면 이벤트 발생 안 함 (reference equality 비교)
- UI 바인딩은 `{Binding State.Count}` 형태로 접근

### IsLoading

```csharp
public bool IsLoading { get; }  // private set
```

- `ExecuteIntent` 진입 시 자동 `true`, 종료 시 자동 `false`
- **Parallel 전략 제외** — `ExecuteIntentParallel`은 IsLoading 건드리지 않음
- Block 전략: `IsLoading == true`이면 새 intent 무시

### Dispatch

```csharp
public void Dispatch(TIntent intent)          // fire-and-forget, 예외는 OnDispatchError로
public async Task DispatchAsync(TIntent intent) // await 가능, 완료까지 대기
```

### HandleIntent

```csharp
protected abstract Task HandleIntent(TIntent intent, CancellationToken ct = default);
```

- `ct`는 `CancelPrevious` 전략 시 실제로 취소됨
- **모든 `await`에 `ct` 전달 필수** — 빼먹으면 취소 신호 전파 안 됨

### 에러 훅

```csharp
protected virtual void OnHandleError(Exception ex) { }         // intent 실행 중 예외
protected virtual void OnDispatchError(TIntent intent, Exception ex) { } // GetStrategy 등 프레임워크 레벨 예외
```

### Effect

```csharp
protected void EmitEffect(IEffect effect);              // Store에서 방출
public IObservable<IEffect> Effects { get; }            // View에서 구독
protected virtual void OnEffect(IEffect effect) { }    // Store 내부에서 처리 (선택)
```

## State 정의 패턴

```csharp
// 기본
public record CounterState(int Count = 0, string Message = "");

// 컬렉션 포함
public record TodoState(
    IReadOnlyList<TodoItem> Items,
    string Filter = "All"
);

// 중첩 record
public record OrderState(
    OrderInfo? CurrentOrder,
    IReadOnlyList<OrderItem> Cart,
    CheckoutStep Step = CheckoutStep.Cart
);
```

> TState는 반드시 record 사용. mutable class 사용 시 `KNOT001` 경고 발생.
> record 프로퍼티에 `{ get; set; }` 있으면 `KNOT002` 경고 발생.

## Intent 정의 패턴

```csharp
public abstract record CounterIntent
{
    public record Increment : CounterIntent;
    public record Decrement : CounterIntent;
    public record IncrementBy(int Value) : CounterIntent;
    public record Reset : CounterIntent;
}
```

- 최상위 abstract record → nested record 패턴 (C# discriminated union 관용구)
- 데이터 없으면 `record` (파라미터 없이), 데이터 있으면 primary constructor 사용

## Store 구현 패턴

```csharp
public partial class CounterStore : KnottyStore<CounterState, CounterIntent>
{
    public CounterStore() : base(new CounterState()) { }

    protected override async Task HandleIntent(CounterIntent intent, CancellationToken ct = default)
    {
        switch (intent)
        {
            case CounterIntent.Increment:
                State = State with { Count = State.Count + 1 };
                break;

            case CounterIntent.IncrementBy by:
                State = State with { Count = State.Count + by.Value };
                break;

            case CounterIntent.Reset:
                // 비동기 예시 — ct 반드시 전달
                await Task.Delay(500, ct);
                State = State with { Count = 0 };
                break;
        }
    }
}
```

## State 업데이트 패턴

```csharp
// 단일 프로퍼티
State = State with { Count = State.Count + 1 };

// 복수 프로퍼티
State = State with { Count = 0, Message = "Reset!" };

// 컬렉션 — 추가
State = State with { Items = [..State.Items, newItem] };

// 컬렉션 — 제거
State = State with { Items = State.Items.Where(x => x.Id != id).ToList() };

// 컬렉션 — 수정
State = State with
{
    Items = State.Items.Select(x => x.Id == id ? x with { Done = !x.Done } : x).ToList()
};
```

## .NET Standard 2.0 / .NET Framework 호환 폴리필

record의 `init` 키워드 사용 시 필요:

```csharp
namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit { }
}
```
