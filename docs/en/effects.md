# Effects

An Effect is a **one-shot side effect** that is not stored in State. Navigation, Toast notifications, and Dialogs are the most common examples.

## State vs Effect

| | State | Effect |
|---|---|---|
| Nature | Persistent UI data | One-shot event |
| Storage | Kept in the Store | Consumed on emission |
| Re-render | Fires PropertyChanged | Does not |
| Examples | `Items`, `IsLoggedIn` | `NavigateTo`, `ShowToast` |

---

## Defining Effects

```csharp
// Inherit from the marker interface
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

## Emitting from the Store

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
                EmitEffect(new AppEffect.ShowToast("Order placed!", ToastType.Success));
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

## Subscribing in the View

```csharp
// Subscribe by type (recommended)
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

// Subscribe to a specific type only
_effects = store.Effects.Subscribe<AppEffect.ShowToast>(toast =>
    ToastService.Show(toast.Message));
```

### Always Dispose

```csharp
// WPF
Unloaded += (_, _) => _effects?.Dispose();

// MAUI
protected override void OnDisappearing() => _effects?.Dispose();

// Avalonia
Closed += (_, _) => _effects?.Dispose();
```

---

## Handling Effects Inside the Store (OnEffect)

When you want to handle an Effect directly in the Store without a View:

```csharp
public class MyStore : KnottyStore<MyState, MyIntent>
{
    protected override void OnEffect(IEffect effect)
    {
        if (effect is AppEffect.ShowToast toast)
            Logger.Info(toast.Message); // logging, etc.
    }
}
```

`OnEffect()` is called simultaneously with `EmitEffect()`.

---

## Common Effect Patterns

### Toast / Snackbar

```csharp
public abstract record ToastEffect : IEffect
{
    public record Info(string Message)    : ToastEffect;
    public record Success(string Message) : ToastEffect;
    public record Error(string Message)   : ToastEffect;
}

// Store
EmitEffect(new ToastEffect.Success("Saved successfully."));
```

### Navigation

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

### Confirmation Dialog (callback pattern)

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

## Anti-Patterns

```csharp
// Never do this: storing Effects in State
public record BadState(
    int Count,
    bool ShouldNavigate,    // ← wrong
    string? ToastMessage    // ← wrong
);

// Do this instead
EmitEffect(new NavEffect.Push("/next"));
EmitEffect(new ToastEffect.Info("Done"));
```
