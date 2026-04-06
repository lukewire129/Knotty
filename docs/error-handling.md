---
title: Error Handling
nav_order: 9
---

# Error Handling

## 기본 동작

`HandleIntent`에서 예외 발생 시 Knotty가 자동 처리:

```
예외 발생
  ├─ OperationCanceledException → Debug 로그만. HasErrors = false. IsLoading = false.
  └─ 그 외 Exception
       ├─ AddError(intentTypeName, ex.Message)   HasErrors = true
       ├─ OnHandleError(ex)                       → override 가능
       └─ IsLoading = false
```

**State는 롤백되지 않음** — 예외 전까지 변경된 State 유지.

---

## OnHandleError — 에러 로깅

```csharp
public class OrderStore : KnottyStore<OrderState, OrderIntent>
{
    protected override void OnHandleError(Exception ex)
    {
        // Sentry, AppCenter, NLog 등 연결
        Sentry.CaptureException(ex);
    }
}
```

## OnDispatchError — 프레임워크 레벨 에러

`GetStrategy()` override 오류 등 `HandleIntent` 외부에서 발생하는 예외:

```csharp
protected override void OnDispatchError(MyIntent intent, Exception ex)
{
    Logger.Fatal(ex, $"Dispatch failed for {intent.GetType().Name}");
}
```

---

## HasErrors / GetErrors

```csharp
// 에러 존재 여부
bool hasError = store.HasErrors;

// 특정 Intent의 에러 메시지 목록 (key = Intent 타입 이름)
var msgs = store.GetErrors("Reset")?.Cast<string>().ToList();

// 에러 변경 이벤트
store.ErrorsChanged += (_, e) =>
{
    var errors = store.GetErrors(e.PropertyName)?.Cast<string>();
};
```

### XAML 바인딩

```xml
<TextBlock Text="오류가 발생했습니다."
           Foreground="Red"
           Visibility="{Binding HasErrors, Converter={StaticResource BoolToVis}}" />
```

---

## 에러 처리 전략별 패턴

### 패턴 1 — State에 에러 메시지 저장

UI 인라인 표시에 적합. `HasErrors`와 독립적으로 관리.

```csharp
protected override async Task HandleIntent(OrderIntent intent, CancellationToken ct)
{
    case OrderIntent.Submit:
        try
        {
            await _api.SubmitAsync(State.Order, ct);
            State = State with { ErrorMessage = null, IsSubmitted = true };
        }
        catch (HttpRequestException ex)
        {
            State = State with { ErrorMessage = $"서버 오류: {ex.Message}" };
            // throw하지 않으면 HasErrors에는 기록 안 됨
        }
        break;
}
```

```xml
<TextBlock Text="{Binding State.ErrorMessage}"
           Visibility="{Binding State.ErrorMessage, Converter={StaticResource NullToVis}}" />
```

### 패턴 2 — Effect로 Toast 표시 (권장)

```csharp
catch (Exception ex)
{
    EmitEffect(new AppEffect.ShowToast(ex.Message, ToastType.Error));
    throw; // HasErrors에도 기록하려면 rethrow
}
```

### 패턴 3 — 예외 전파 차단

에러를 조용히 처리하고 Store가 정상 동작 유지:

```csharp
catch (Exception ex)
{
    State = State with { LastError = ex.Message };
    // throw 없음 → HasErrors = false, 다음 intent 정상 처리
}
```

---

## CancelPrevious — 취소 시 State 복원

취소는 자동 롤백 없음. 직접 처리해야 함.

```csharp
case OrderIntent.Reset:
    var snapshot = State;                          // 롤백용 스냅샷
    State = State with { Message = "초기화 중..." };

    try
    {
        await Task.Delay(3000, ct);
        State = State with { Count = 0, Message = "완료" };
    }
    catch (OperationCanceledException)
    {
        State = snapshot;   // 취소 시 원래 State 복원
        throw;              // 반드시 rethrow — 프레임워크가 이를 기대함
    }
    break;
```

> rethrow 없이 `OperationCanceledException`을 삼키면 `IsLoading`이 `false`로 복원되지 않을 수 있음.

---

## 에러 처리 선택 기준

| 요구사항 | 방법 |
|----------|------|
| 앱 전체 에러 로깅 (Sentry 등) | `OnHandleError` override |
| UI 인라인 에러 텍스트 | State에 `string? ErrorMessage` |
| Toast / Snackbar 알림 | `EmitEffect(new ToastEffect.Error(...))` |
| Validation 에러 (WPF 바인딩) | `INotifyDataErrorInfo` 기본 동작 (`HasErrors`) |
| 취소 시 UI 원복 | `OperationCanceledException` catch + State 스냅샷 복원 |
