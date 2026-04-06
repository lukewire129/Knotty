---
title: Debugging
nav_order: 11
---

# Debugging

## KnottyDebugger

`KnottyDebugger`는 Intent 수신과 State 변경을 타임스탬프 단위로 기록하는 정적 클래스.
기본값은 **비활성화** — 앱 시작 시 명시적으로 켜야 함.

```csharp
// 앱 시작점 (App.xaml.cs, MauiProgram.cs, Program.cs 등)
KnottyDebugger.IsEnabled = true;
```

---

## 로그 항목 구조

```csharp
public record LogEntry(
    DateTime Timestamp,   // 기록 시각
    string   StoreType,   // Store 클래스 이름
    string   IntentType,  // Intent 타입 이름 또는 "StateChanged"
    object?  Intent,      // Intent 인스턴스 (StateChanged 시 null)
    object?  OldState,    // 이전 State (Intent 수신 시 null)
    object?  NewState     // 새 State (Intent 수신 시 null)
);
```

두 가지 이벤트가 기록됨:

| IntentType | 기록 시점 | Intent | OldState / NewState |
|-----------|----------|--------|-------------------|
| Intent 타입 이름 | `HandleIntent` 진입 전 | Intent 인스턴스 | null |
| `"StateChanged"` | `State` setter 호출 시 | null | 이전/현재 State |

---

## 로그 읽기

```csharp
// 전체 로그
IReadOnlyList<KnottyDebugger.LogEntry> logs = KnottyDebugger.Logs;

// Intent만 필터
var intentLogs = logs.Where(e => e.IntentType != "StateChanged");

// 특정 Store만
var orderLogs = logs.Where(e => e.StoreType == nameof(OrderStore));

// 마지막 5개
var recent = logs.TakeLast(5);
```

---

## 파일로 내보내기 (net5.0+)

```csharp
// JSON으로 내보내기 (비동기)
await KnottyDebugger.ExportToFileAsync("debug_log.json");
```

출력 예시:
```json
[
  {
    "Timestamp": "2026-04-07T10:23:01.123",
    "StoreType": "CounterStore",
    "IntentType": "Increment",
    "Intent": {},
    "OldState": null,
    "NewState": null
  },
  {
    "Timestamp": "2026-04-07T10:23:01.125",
    "StoreType": "CounterStore",
    "IntentType": "StateChanged",
    "Intent": null,
    "OldState": { "Count": 0 },
    "NewState": { "Count": 1 }
  }
]
```

> `ExportToFileAsync`는 net5.0 이상에서만 사용 가능. netstandard2.0 타겟에서는 컴파일 제외됨.

---

## 로그 초기화

```csharp
KnottyDebugger.Clear();
```

테스트 케이스 사이, 또는 화면 전환 시 로그를 리셋할 때 사용.

---

## Debug.WriteLine 출력

`IsEnabled = true` 상태에서 Intent가 Dispatch되면 디버거 출력창에 자동 기록:

```
[Knotty] CounterStore <- Increment
[Knotty] OrderStore <- Submit
[Knotty] Intent Search cancelled
[Knotty] Unhandled error dispatching Reset: Object reference not set...
```

IDE의 Debug Output 창(Visual Studio: 보기 → 출력 → 디버그)에서 확인.

---

## OnDispatchError 훅

`GetStrategy()` override나 라우팅 단계에서 예외가 발생하면 `HandleIntent` 바깥이므로 `OnHandleError`가 호출되지 않음.
이 경우를 잡으려면 `OnDispatchError`를 override.

```csharp
public class OrderStore : KnottyStore<OrderState, OrderIntent>
{
    protected override void OnDispatchError(OrderIntent intent, Exception ex)
    {
        // Sentry, AppCenter, 커스텀 로거 등
        Logger.Fatal(ex, $"[Knotty] Framework error on {intent?.GetType().Name}");
    }
}
```

`OnHandleError`와의 차이:

| 메서드 | 호출 시점 | 호출 조건 |
|--------|----------|----------|
| `OnHandleError` | `HandleIntent` 내 예외 | `ExecuteIntent` 내부에서 catch |
| `OnDispatchError` | `GetStrategy`, 라우팅 오류 | `DispatchInternalAsync`에서 catch |

---

## 테스트에서 로그 활용

```csharp
[Fact]
public async Task Submit_ShouldDispatchIntentAndChangeState()
{
    KnottyDebugger.IsEnabled = true;
    KnottyDebugger.Clear();

    var store = new OrderStore();
    store.Dispatch(new OrderIntent.Submit());
    await Task.Delay(50); // 비동기 처리 대기

    var intent = KnottyDebugger.Logs.FirstOrDefault(e => e.IntentType == "Submit");
    Assert.NotNull(intent);

    var stateChange = KnottyDebugger.Logs.FirstOrDefault(e => e.IntentType == "StateChanged");
    Assert.NotNull(stateChange);
}
```

---

## 주의사항

| 항목 | 내용 |
|------|------|
| 기본값 비활성화 | `IsEnabled = false`. 프로덕션 빌드에서 켜지 않도록 주의. |
| 메모리 | 로그가 무제한 축적됨. 장시간 실행 시 `Clear()` 주기적 호출 권장. |
| 스레드 안전 | `_logs`에 lock 없음 — 멀티스레드 환경에서 동시 기록 시 주의. |
| netstandard2.0 | `ExportToFileAsync` 미제공. `LogEntry`는 record 대신 class로 정의됨. |
