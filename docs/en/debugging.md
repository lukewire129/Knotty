# Debugging

## KnottyDebugger

`KnottyDebugger` is a static class that records Intent dispatches and State changes with timestamps.
It is **disabled by default** — you must enable it explicitly at startup.

```csharp
// App startup (App.xaml.cs, MauiProgram.cs, Program.cs, etc.)
KnottyDebugger.IsEnabled = true;
```

---

## Log Entry Structure

```csharp
public record LogEntry(
    DateTime Timestamp,   // when the entry was recorded
    string   StoreType,   // Store class name
    string   IntentType,  // Intent type name, or "StateChanged"
    object?  Intent,      // Intent instance (null for StateChanged)
    object?  OldState,    // previous State (null for Intent entries)
    object?  NewState     // new State (null for Intent entries)
);
```

Two types of events are recorded:

| IntentType | Recorded when | Intent | OldState / NewState |
|-----------|--------------|--------|---------------------|
| Intent type name | Before entering `HandleIntent` | Intent instance | null |
| `"StateChanged"` | When the `State` setter is called | null | before/after State |

---

## Reading Logs

```csharp
// All logs
IReadOnlyList<KnottyDebugger.LogEntry> logs = KnottyDebugger.Logs;

// Intents only
var intentLogs = logs.Where(e => e.IntentType != "StateChanged");

// Specific Store only
var orderLogs = logs.Where(e => e.StoreType == nameof(OrderStore));

// Last 5 entries
var recent = logs.TakeLast(5);
```

---

## Export to File (net5.0+)

```csharp
// Export as JSON (async)
await KnottyDebugger.ExportToFileAsync("debug_log.json");
```

Sample output:
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

> `ExportToFileAsync` is only available on net5.0+. It is excluded from netstandard2.0 builds.

---

## Clearing Logs

```csharp
KnottyDebugger.Clear();
```

Use this between test cases or on screen transitions to reset the log.

---

## Debug.WriteLine Output

With `IsEnabled = true`, every dispatched Intent is written to the debugger output window:

```
[Knotty] CounterStore <- Increment
[Knotty] OrderStore <- Submit
[Knotty] Intent Search cancelled
[Knotty] Unhandled error dispatching Reset: Object reference not set...
```

Check the Debug Output window in your IDE (Visual Studio: View → Output → Debug).

---

## OnDispatchError Hook

Exceptions thrown in `GetStrategy()` overrides or during routing don't go through `OnHandleError` because they occur outside `HandleIntent`.
Override `OnDispatchError` to catch these:

```csharp
public class OrderStore : KnottyStore<OrderState, OrderIntent>
{
    protected override void OnDispatchError(OrderIntent intent, Exception ex)
    {
        // Sentry, AppCenter, custom logger, etc.
        Logger.Fatal(ex, $"[Knotty] Framework error on {intent?.GetType().Name}");
    }
}
```

Difference between `OnHandleError` and `OnDispatchError`:

| Method | Called when | Catch location |
|--------|-------------|---------------|
| `OnHandleError` | Exception inside `HandleIntent` | Inside `ExecuteIntent` |
| `OnDispatchError` | Exception in `GetStrategy`, routing | Inside `DispatchInternalAsync` |

---

## Using Logs in Tests

```csharp
[Fact]
public async Task Submit_ShouldDispatchIntentAndChangeState()
{
    KnottyDebugger.IsEnabled = true;
    KnottyDebugger.Clear();

    var store = new OrderStore();
    store.Dispatch(new OrderIntent.Submit());
    await Task.Delay(50); // wait for async processing

    var intent = KnottyDebugger.Logs.FirstOrDefault(e => e.IntentType == "Submit");
    Assert.NotNull(intent);

    var stateChange = KnottyDebugger.Logs.FirstOrDefault(e => e.IntentType == "StateChanged");
    Assert.NotNull(stateChange);
}
```

---

## Notes

| Item | Details |
|------|---------|
| Disabled by default | `IsEnabled = false`. Be careful not to enable in production builds. |
| Memory | Logs accumulate indefinitely. Call `Clear()` periodically for long-running apps. |
| Thread safety | No lock on `_logs` — concurrent writes from multiple threads can cause issues. |
| netstandard2.0 | `ExportToFileAsync` not available. `LogEntry` is a `class` instead of a `record`. |
