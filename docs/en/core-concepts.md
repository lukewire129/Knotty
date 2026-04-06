# Core Concepts

## State

State is a single snapshot of all the data the UI needs to render.

```csharp
public record CounterState(int Count = 0, string Message = "Ready");
```

**Design rules**

- Must be a `record` (using `class` triggers `KNOT001` analyzer warning)
- All properties must be init-only (`{ get; set; }` triggers `KNOT002`)
- One-shot data like navigation targets or toast messages → use `IEffect` instead
- Collections: prefer `IReadOnlyList<T>`

**Updating**

```csharp
// Single property
State = State with { Count = State.Count + 1 };

// Multiple properties
State = State with { Count = 0, Message = "Reset!" };

// Add to collection
State = State with { Items = [..State.Items, newItem] };

// Modify collection item
State = State with
{
    Items = State.Items
        .Select(x => x.Id == id ? x with { Done = !x.Done } : x)
        .ToList()
};
```

Assigning `State = ...` automatically fires `PropertyChanging` → `PropertyChanged`.
Assigning the same reference does nothing (reference equality check).

---

## Intent

Intent is an enumeration of every action the user or system can request.

```csharp
public abstract record CounterIntent
{
    public record Increment            : CounterIntent;  // no data
    public record IncrementBy(int N)   : CounterIntent;  // with data
    public record LoadData(string Url) : CounterIntent;  // async operation
    public record Reset                : CounterIntent;
}
```

**Design rules**

- Top-level `abstract record` + nested `record` pattern (C# discriminated union idiom)
- Intent names are imperative verbs (`Load`, `Submit`, `Reset`)
- No data? → `public record Increment : CounterIntent;` (no empty constructor needed)
- Don't map UI events 1:1 to intents — group them into meaningful units

---

## Store

Store is the connector between State and Intent. The single entry point for all business logic.

```csharp
public abstract partial class KnottyStore<TState, TIntent>
    where TState : class
```

**Key methods**

```csharp
// Required
protected abstract Task HandleIntent(TIntent intent, CancellationToken ct = default);

// Optional overrides
protected virtual IntentHandlingStrategy GetStrategy(TIntent intent);
protected virtual TimeSpan GetDebounceDelay(TIntent intent);
protected virtual void OnHandleError(Exception ex);
protected virtual void OnDispatchError(TIntent intent, Exception ex);
protected virtual void OnEffect(IEffect effect);
```

**Dispatch**

```csharp
store.Dispatch(intent);            // fire-and-forget. exceptions go to OnDispatchError.
await store.DispatchAsync(intent); // waits for completion. exceptions propagate.
```

**Automatic management**

| Item | Timing |
|------|--------|
| `IsLoading = true` | Immediately before `HandleIntent` |
| `ClearAllErrors()` | Immediately before `HandleIntent` |
| `IsLoading = false` | After `HandleIntent` (finally block) |
| `AddError(key, msg)` | On exception. key = Intent type name |
| `OnHandleError(ex)` | On exception callback |

---

## How the Three Pieces Relate

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
│    → EmitEffect(effect)   [optional]    │
└───────────┬─────────────────────────────┘
            │ State PropertyChanged
            ▼
┌─────────────────────────────────────────┐
│               View (updated)            │
└─────────────────────────────────────────┘
```

Unidirectional data flow — the View only reads State and requests changes through the Store.
