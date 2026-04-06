# Error Handling

## Default Behavior

When `HandleIntent` throws, Knotty handles it automatically:

```
Exception thrown
  ├─ OperationCanceledException → Debug log only. HasErrors = false. IsLoading = false.
  └─ Any other Exception
       ├─ AddError(intentTypeName, ex.Message)   HasErrors = true
       ├─ OnHandleError(ex)                       → overridable
       └─ IsLoading = false
```

**State is NOT rolled back** — any State changes made before the exception are preserved.

---

## OnHandleError — Error Logging

```csharp
public class OrderStore : KnottyStore<OrderState, OrderIntent>
{
    protected override void OnHandleError(Exception ex)
    {
        // Connect Sentry, AppCenter, NLog, etc.
        Sentry.CaptureException(ex);
    }
}
```

## OnDispatchError — Framework-Level Errors

Exceptions thrown outside `HandleIntent` — such as errors in `GetStrategy()` overrides:

```csharp
protected override void OnDispatchError(MyIntent intent, Exception ex)
{
    Logger.Fatal(ex, $"Dispatch failed for {intent.GetType().Name}");
}
```

---

## HasErrors / GetErrors

```csharp
// Check if errors exist
bool hasError = store.HasErrors;

// Error messages for a specific Intent (key = Intent type name)
var msgs = store.GetErrors("Reset")?.Cast<string>().ToList();

// React to error changes
store.ErrorsChanged += (_, e) =>
{
    var errors = store.GetErrors(e.PropertyName)?.Cast<string>();
};
```

### XAML Binding

```xml
<TextBlock Text="An error occurred."
           Foreground="Red"
           Visibility="{Binding HasErrors, Converter={StaticResource BoolToVis}}" />
```

---

## Error Handling Patterns

### Pattern 1 — Store error message in State

Good for inline UI display. Managed independently from `HasErrors`.

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
            State = State with { ErrorMessage = $"Server error: {ex.Message}" };
            // Not rethrowing → HasErrors stays false
        }
        break;
}
```

```xml
<TextBlock Text="{Binding State.ErrorMessage}"
           Visibility="{Binding State.ErrorMessage, Converter={StaticResource NullToVis}}" />
```

### Pattern 2 — Toast via Effect (recommended)

```csharp
catch (Exception ex)
{
    EmitEffect(new AppEffect.ShowToast(ex.Message, ToastType.Error));
    throw; // rethrow if you also want HasErrors recorded
}
```

### Pattern 3 — Suppress exception propagation

Handle the error silently and keep the Store in a normal state:

```csharp
catch (Exception ex)
{
    State = State with { LastError = ex.Message };
    // no throw → HasErrors = false, next intent processes normally
}
```

---

## CancelPrevious — Restoring State on Cancellation

Cancellation does not roll back automatically. You must handle it explicitly.

```csharp
case OrderIntent.Reset:
    var snapshot = State;                          // snapshot for rollback
    State = State with { Message = "Resetting..." };

    try
    {
        await Task.Delay(3000, ct);
        State = State with { Count = 0, Message = "Done" };
    }
    catch (OperationCanceledException)
    {
        State = snapshot;   // restore on cancellation
        throw;              // must rethrow — the framework expects it
    }
    break;
```

> Swallowing `OperationCanceledException` without rethrowing may prevent `IsLoading` from being restored to `false`.

---

## Choosing an Error Strategy

| Requirement | Approach |
|-------------|----------|
| App-wide error logging (Sentry, etc.) | Override `OnHandleError` |
| Inline error text in UI | `string? ErrorMessage` in State |
| Toast / Snackbar notification | `EmitEffect(new ToastEffect.Error(...))` |
| Validation errors (WPF binding) | Default `INotifyDataErrorInfo` behavior (`HasErrors`) |
| Restore UI on cancellation | `OperationCanceledException` catch + State snapshot restore |
