---
title: Effects
nav_order: 8
---

# Effects

Effect는 State에 저장하지 않는 **일회성 부수 효과**. 네비게이션, Toast, Dialog가 대표적.

## State vs Effect

| | State | Effect |
|---|---|---|
| 성격 | 지속적 UI 데이터 | 일회성 이벤트 |
| 저장 | Store에 유지 | 발행 즉시 소멸 |
| 재렌더 | PropertyChanged 발생 | 발생 안 함 |
| 예시 | `Items`, `IsLoggedIn` | `NavigateTo`, `ShowToast` |

---

## Effect 정의

```csharp
// 마커 인터페이스 상속
public abstract record AppEffect : IEffect
{
    public record ShowToast(string Message, ToastType Type = ToastType.Info) : AppEffect;
    public record NavigateTo(string Route)                                   : AppEffect;
    public record ShowDialog(string Title, string Message)                   : AppEffect;
    public record CloseWindow                                                : AppEffect;
}

public enum ToastType { Info, Success, Warning, Error }
```

---

## Store에서 방출

```csharp
protected override async Task HandleIntent(OrderIntent intent, CancellationToken ct)
{
    switch (intent)
    {
        case OrderIntent.Submit:
            try
            {
                await _service.SubmitAsync(State.Order, ct);
                State = State with { IsSubmitted = true };
                EmitEffect(new AppEffect.ShowToast("주문 완료!", ToastType.Success));
                EmitEffect(new AppEffect.NavigateTo("/orders"));
            }
            catch (Exception ex)
            {
                EmitEffect(new AppEffect.ShowToast(ex.Message, ToastType.Error));
                throw;
            }
            break;
    }
}
```

---

## View에서 구독

```csharp
// 타입별 구독 (권장)
_effects = store.Effects.Subscribe<AppEffect>(effect =>
{
    switch (effect)
    {
        case AppEffect.ShowToast toast:
            ToastService.Show(toast.Message, toast.Type);
            break;
        case AppEffect.NavigateTo nav:
            NavigationService.Navigate(nav.Route);
            break;
        case AppEffect.ShowDialog dialog:
            MessageBox.Show(dialog.Message, dialog.Title);
            break;
        case AppEffect.CloseWindow:
            this.Close();
            break;
    }
});

// 특정 타입만 구독
_effects = store.Effects.Subscribe<AppEffect.ShowToast>(toast =>
    ToastService.Show(toast.Message));
```

### Dispose 필수

```csharp
// WPF
Unloaded += (_, _) => _effects?.Dispose();

// MAUI
protected override void OnDisappearing() => _effects?.Dispose();

// Avalonia
Closed += (_, _) => _effects?.Dispose();
```

---

## Store 내부에서 직접 처리 (OnEffect)

View 없이 Store 내에서 Effect를 처리하고 싶을 때:

```csharp
public class MyStore : KnottyStore<MyState, MyIntent>
{
    protected override void OnEffect(IEffect effect)
    {
        if (effect is AppEffect.ShowToast toast)
            Logger.Info(toast.Message); // 로깅 등
    }
}
```

`EmitEffect()` 호출 시 `OnEffect()`도 동시에 호출됨.

---

## 자주 쓰는 Effect 패턴

### Toast / Snackbar

```csharp
public abstract record ToastEffect : IEffect
{
    public record Info(string Message)    : ToastEffect;
    public record Success(string Message) : ToastEffect;
    public record Error(string Message)   : ToastEffect;
}

// Store
EmitEffect(new ToastEffect.Success("저장되었습니다."));
```

### 네비게이션

```csharp
public abstract record NavEffect : IEffect
{
    public record Push(string Route)   : NavEffect;
    public record Pop                  : NavEffect;
    public record PopToRoot            : NavEffect;
}

// Store
EmitEffect(new NavEffect.Push("/settings"));
```

### 확인 Dialog (콜백 패턴)

```csharp
public record ConfirmEffect(string Title, string Message, Action OnConfirm) : IEffect;

// View
case ConfirmEffect confirm:
    var result = MessageBox.Show(confirm.Message, confirm.Title,
        MessageBoxButton.OKCancel);
    if (result == MessageBoxResult.OK)
        confirm.OnConfirm();
    break;
```

---

## 금지 패턴

```csharp
// 절대 하지 말 것: Effect를 State에 저장
public record BadState(
    int Count,
    bool ShouldNavigate,    // ← 안됨
    string? ToastMessage    // ← 안됨
);

// 올바른 방법
EmitEffect(new NavEffect.Push("/next"));
EmitEffect(new ToastEffect.Info("완료"));
```
