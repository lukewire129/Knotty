---
title: Core Concepts
nav_order: 3
---

# Core Concepts

## State

State는 UI가 렌더링에 필요한 모든 데이터의 단일 스냅샷.

```csharp
public record CounterState(int Count = 0, string Message = "Ready");
```

**설계 원칙**

- 반드시 `record` 사용 (`class` 사용 시 `KNOT001` analyzer 경고)
- 모든 프로퍼티는 init-only (`{ get; set; }` 있으면 `KNOT002` 경고)
- 네비게이션 목적지, 토스트 메시지 같은 일회성 데이터는 `IEffect`로 처리
- 컬렉션은 `IReadOnlyList<T>` 권장

**업데이트**

```csharp
// 단일 프로퍼티
State = State with { Count = State.Count + 1 };

// 복수 프로퍼티
State = State with { Count = 0, Message = "Reset!" };

// 컬렉션 추가
State = State with { Items = [..State.Items, newItem] };

// 컬렉션 항목 수정
State = State with
{
    Items = State.Items
        .Select(x => x.Id == id ? x with { Done = !x.Done } : x)
        .ToList()
};
```

`State = ...` 대입 시 `PropertyChanging` → `PropertyChanged` 자동 발생.
같은 참조를 대입하면 이벤트 발생 안 함 (reference equality 비교).

---

## Intent

Intent는 사용자 또는 시스템이 요청할 수 있는 행동의 열거형.

```csharp
public abstract record CounterIntent
{
    public record Increment            : CounterIntent;  // 데이터 없음
    public record IncrementBy(int N)   : CounterIntent;  // 데이터 있음
    public record LoadData(string Url) : CounterIntent;  // 비동기 작업
    public record Reset                : CounterIntent;
}
```

**설계 원칙**

- 최상위 `abstract record` + 중첩 `record` 패턴 (C# discriminated union 관용구)
- Intent 이름은 명령형 동사 (`Load`, `Submit`, `Reset`)
- 파라미터가 없으면 `public record Increment : CounterIntent;` (빈 생성자 불필요)
- UI 이벤트를 Intent로 1:1 매핑하지 말 것 — 의미있는 단위로 묶기

---

## Store

Store는 State와 Intent의 연결자. 비즈니스 로직의 단일 진입점.

```csharp
public abstract partial class KnottyStore<TState, TIntent>
    where TState : class
```

**핵심 메서드**

```csharp
// 구현 필수
protected abstract Task HandleIntent(TIntent intent, CancellationToken ct = default);

// 선택적 override
protected virtual IntentHandlingStrategy GetStrategy(TIntent intent);
protected virtual TimeSpan GetDebounceDelay(TIntent intent);
protected virtual void OnHandleError(Exception ex);
protected virtual void OnDispatchError(TIntent intent, Exception ex);
protected virtual void OnEffect(IEffect effect);
```

**Dispatch**

```csharp
store.Dispatch(intent);            // fire-and-forget. 예외는 OnDispatchError로.
await store.DispatchAsync(intent); // 완료까지 대기. 직접 await 가능.
```

**자동 관리 항목**

| 항목 | 타이밍 |
|------|--------|
| `IsLoading = true` | `HandleIntent` 진입 직전 |
| `ClearAllErrors()` | `HandleIntent` 진입 직전 |
| `IsLoading = false` | `HandleIntent` 종료 후 (finally) |
| `AddError(key, msg)` | 예외 발생 시. key = Intent 타입 이름 |
| `OnHandleError(ex)` | 예외 발생 시 callback |

---

## 세 요소의 관계

```
┌─────────────────────────────────────────┐
│                  View                    │
│  {Binding State.Count}                  │
│  Command="{Binding IncrementCommand}"   │
└───────────┬─────────────────────────────┘
            │ Dispatch(intent)
            ▼
┌─────────────────────────────────────────┐
│                  Store                   │
│  HandleIntent(intent, ct)               │
│    → State = State with { ... }         │
│    → EmitEffect(effect)   [선택]        │
└───────────┬─────────────────────────────┘
            │ State PropertyChanged
            ▼
┌─────────────────────────────────────────┐
│               View (갱신)               │
└─────────────────────────────────────────┘
```

단방향 데이터 흐름 — View는 State를 읽기만 하고, Store를 통해서만 변경 요청.
