# Knotty — 에러 처리

## 기본 동작

`HandleIntent`에서 예외 발생 시 Knotty가 자동으로 처리:

1. `OperationCanceledException` → 로그만 남김, `HasErrors` false 유지, `IsLoading` false로 복원
2. 그 외 `Exception` → `AddError(intentTypeName, ex.Message)` 호출, `OnHandleError(ex)` 호출, `IsLoading` false로 복원
3. **State는 롤백되지 않음** — 예외 발생 전까지 변경된 State가 그대로 유지됨

> 에러 key는 Intent 타입 이름 (예: `"Reset"`, `"LoadData"`).

## INotifyDataErrorInfo API

```csharp
// 에러 존재 여부
bool HasErrors { get; }

// 특정 key의 에러 목록 (IEnumerable<string>으로 캐스팅)
var errors = store.GetErrors("Reset")?.Cast<string>().ToList();

// 에러 발생/해제 이벤트
event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
```

## 에러 처리 패턴

### 기본 — OnHandleError override

```csharp
public class OrderStore : KnottyStore<OrderState, OrderIntent>
{
    protected override void OnHandleError(Exception ex)
    {
        // 로깅, Sentry, AppCenter 등 연결
        Logger.Error(ex, "OrderStore intent failed");
    }
}
```

### 에러를 State에 반영하기

```csharp
protected override async Task HandleIntent(OrderIntent intent, CancellationToken ct)
{
    switch (intent)
    {
        case OrderIntent.Submit:
            try
            {
                await _service.SubmitAsync(State.Order, ct);
                State = State with { IsSubmitted = true, ErrorMessage = null };
            }
            catch (HttpRequestException ex)
            {
                // 네트워크 에러 — State에 메시지 반영 후 rethrow하지 않음
                State = State with { ErrorMessage = ex.Message };
                // rethrow 하지 않으면 HasErrors에는 기록되지 않음 (State로만 관리)
            }
            break;
    }
}
```

### 에러를 Effect로 전달하기

```csharp
protected override async Task HandleIntent(OrderIntent intent, CancellationToken ct)
{
    switch (intent)
    {
        case OrderIntent.Submit:
            try
            {
                await _service.SubmitAsync(State.Order, ct);
                EmitEffect(new OrderEffect.SubmitSuccess());
            }
            catch (Exception ex)
            {
                EmitEffect(new OrderEffect.ShowError(ex.Message));
                throw; // HasErrors에도 기록하려면 rethrow
            }
            break;
    }
}
```

## CancelPrevious — 취소 시 State 복원

취소는 자동으로 처리되지 않음. **수동으로 catch해서 이전 State로 복원해야 함.**

```csharp
case OrderIntent.Reset:
    var previousState = State;
    State = State with { Message = "초기화 중..." };
    try
    {
        await Task.Delay(3000, ct);
        State = State with { Count = 0, Message = "완료" };
    }
    catch (OperationCanceledException)
    {
        // 취소 시 State 복원
        State = previousState;
        throw; // 반드시 rethrow — 프레임워크가 OperationCanceledException을 기대함
    }
    break;
```

> `OperationCanceledException`을 잡고 rethrow하지 않으면 `IsLoading`이 정상 복원되지 않을 수 있음.

## UI에서 에러 표시

### WPF — Validation 스타일과 연결

```xml
<!-- HasErrors 바인딩 -->
<TextBlock Text="오류가 발생했습니다."
           Visibility="{Binding HasErrors, Converter={StaticResource BoolToVisibility}}"
           Foreground="Red" />
```

### WPF — 특정 Intent 에러 표시

```csharp
// code-behind
store.ErrorsChanged += (_, e) =>
{
    var errors = store.GetErrors(e.PropertyName)?.Cast<string>().ToList();
    ErrorText.Text = errors?.FirstOrDefault() ?? "";
};
```

### Effect로 에러 표시 (권장)

State나 INotifyDataErrorInfo보다 Effect로 에러 Toast를 보내는 게 더 단순:

```csharp
// Store
EmitEffect(new AppEffect.ShowToast("서버 오류가 발생했습니다.", ToastType.Error));

// View
case AppEffect.ShowToast toast when toast.Type == ToastType.Error:
    ErrorSnackbar.Show(toast.Message);
    break;
```

## 에러 처리 선택 기준

| 상황 | 방법 |
|------|------|
| 앱 전체 에러 로깅 | `OnHandleError` override |
| UI에 인라인 에러 텍스트 표시 | State에 `ErrorMessage` 필드 추가 |
| Toast / Snackbar 알림 | `IEffect` 방출 |
| 폼 Validation 에러 | `INotifyDataErrorInfo` (기본 동작) |
| 취소 시 State 복원 | `OperationCanceledException` catch + rethrow |

## 절대 하지 말 것

```csharp
// 취소 예외를 무시하면 IsLoading이 잘못 복원될 수 있음
catch (OperationCanceledException) { } // rethrow 빠짐 — 위험

// await 없이 예외를 삼키면 ct 체인이 끊김
_ = Task.Run(() => DoSomethingAsync()); // 절대 금지
```
