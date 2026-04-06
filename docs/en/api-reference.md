# API Reference

Full public API signatures. See individual chapters for detailed usage.

---

## KnottyStore\<TState, TIntent\>

```csharp
namespace Knotty;

public abstract partial class KnottyStore<TState, TIntent>
    : INotifyPropertyChanged, INotifyPropertyChanging,
      INotifyDataErrorInfo, IEffectSource, IDisposable
    where TState : class
```

### Properties

```csharp
// Current State. protected setter — only modify inside HandleIntent.
public TState State { get; protected set; }

// True while an async Intent is being processed.
// Managed automatically for Block/Queue/CancelPrevious/Debounce strategies.
// Parallel strategy does NOT touch IsLoading.
public bool IsLoading { get; }

// INotifyDataErrorInfo — true when an unhandled exception occurred.
public bool HasErrors { get; }

// Effect stream (IObservable<IEffect>). Subscribe in the View.
public IObservable<IEffect> Effects { get; }
```

### Constructor

```csharp
protected KnottyStore(TState initialState);
// initialState == null → ArgumentNullException
```

### Public Methods

```csharp
// Fire-and-forget Dispatch. Exceptions are routed to OnDispatchError.
public void Dispatch(TIntent intent);

// Awaitable Dispatch. Exceptions propagate to the caller.
public async Task DispatchAsync(TIntent intent);
```

### Protected — Override Points

```csharp
// Required. Handle the intent and update State.
protected abstract Task HandleIntent(TIntent intent, CancellationToken ct = default);

// Returns the handling strategy for an intent. Default: Block.
protected virtual IntentHandlingStrategy GetStrategy(TIntent intent);

// Returns the debounce delay. Default: 300ms.
protected virtual TimeSpan GetDebounceDelay(TIntent intent);

// Called when HandleIntent throws an exception.
protected virtual void OnHandleError(Exception ex);

// Called when GetStrategy/routing throws an exception.
// Catches exceptions from the Dispatch() fire-and-forget path.
protected virtual void OnDispatchError(TIntent intent, Exception ex);

// Called simultaneously with EmitEffect(). Handle effects inside the Store.
protected virtual void OnEffect(IEffect effect);
```

### Protected — Helpers

```csharp
// Opt-in registration to receive broadcast intents from KnottyBus.
protected IDisposable SubscribeToBus();

// Emit an effect to subscribers and call OnEffect().
protected void EmitEffect(IEffect effect);

// Command factory methods
protected ICommand      Command(TIntent intent, Func<bool>? canExecute = null);
protected ICommand      Command<TParam>(Func<TParam, TIntent> factory, Func<TParam, bool>? canExecute = null);
protected IAsyncCommand AsyncCommand(TIntent intent, Func<bool>? canExecute = null);
protected IAsyncCommand AsyncCommand<TParam>(Func<TParam, TIntent> factory, Func<TParam, bool>? canExecute = null);

// INotifyDataErrorInfo helpers
protected void AddError(string propertyName, string error);
protected void ClearAllErrors();
```

### INotifyDataErrorInfo

```csharp
// key = Intent type name (e.g., "Submit", "Load")
public IEnumerable? GetErrors(string propertyName);
public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
```

### Dispose

```csharp
// Cleans up BusToken, EffectSubject, CTS, DebounceTimer, IntentQueue.
// Idempotent — safe to call multiple times.
public virtual void Dispose();
```

---

## IntentHandlingStrategy

```csharp
namespace Knotty;

public enum IntentHandlingStrategy
{
    Block,          // Ignore new intents while processing (default)
    Queue,          // Process in order
    Debounce,       // Process only the last intent after a delay
    CancelPrevious, // Cancel current work, start new intent
    Parallel        // Process concurrently without touching IsLoading
}
```

---

## KnottyBus

```csharp
namespace Knotty;

public static class KnottyBus
```

```csharp
// Recipient is held as WeakReference — auto-removed on GC.
// Explicit IDisposable.Dispose() is still recommended.
public static IDisposable Subscribe<TIntent>(object recipient, Action<TIntent> handler);

// Broadcasts to all subscribers. Walks the full record inheritance hierarchy.
public static void Publish<TIntent>(TIntent intent);

[Obsolete("Use Publish instead.")]
public static void Send<TIntent>(TIntent intent);
```

**Behavior**:
- `Publish` walks from `intent.GetType()` up through `BaseType`, delivering to all matching subscribers.
- WeakReference-based — dead recipients are purged automatically.
- Subscription is opt-in. Call `SubscribeToBus()` in the Store constructor to receive Bus messages.

---

## Effect System

### IEffect

```csharp
namespace Knotty;

public interface IEffect;  // marker interface
```

### IEffectSource

```csharp
namespace Knotty;

public interface IEffectSource
{
    IObservable<IEffect> Effects { get; }
}
```

### EffectExtensions

```csharp
namespace Knotty;

public static class EffectExtensions
{
    // Subscribe to only TEffect type
    public static IDisposable Subscribe<TEffect>(
        this IObservable<IEffect> source,
        Action<TEffect> handler)
        where TEffect : IEffect;

    // Subscribe to all Effects
    public static IDisposable Subscribe(
        this IObservable<IEffect> source,
        Action<IEffect> handler);
}
```

---

## Commands

### IAsyncCommand

```csharp
namespace Knotty;

public interface IAsyncCommand : ICommand, INotifyPropertyChanged
{
    Task ExecuteAsync(object? parameter);
    bool IsExecuting { get; }
}
```

When `IsExecuting == true`, `CanExecute()` automatically returns `false`.

### Attributes (Source Generator)

```csharp
namespace Knotty;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
public sealed class IntentCommandAttribute : Attribute
{
    public string? CommandName { get; set; }   // optional: override generated property name
    public string? CanExecute  { get; set; }   // optional: method/property name for CanExecute
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
public sealed class AsyncIntentCommandAttribute : Attribute
{
    public string? CommandName { get; set; }
    public string? CanExecute  { get; set; }
}
```

**Name generation**: strips leading `_`, `Create`, `Get`, `Make` → PascalCase + `Command` suffix.

---

## DI Extension (net6.0+)

```csharp
namespace Knotty;

public static class ServiceCollectionExtensions
{
    // Register Store as Singleton
    public static IServiceCollection AddKnottyStore<TStore>(
        this IServiceCollection services)
        where TStore : class, IDisposable;

    // Register Store as Singleton with a factory
    public static IServiceCollection AddKnottyStore<TStore>(
        this IServiceCollection services,
        Func<IServiceProvider, TStore> factory)
        where TStore : class, IDisposable;
}
```

> Excluded from `netstandard2.0` builds.

---

## KnottyDebugger

```csharp
namespace Knotty;

public static class KnottyDebugger
{
    public static bool IsEnabled { get; set; }          // default: false
    public static IReadOnlyList<LogEntry> Logs { get; }
    public static void Clear();
    public static async Task ExportToFileAsync(string path); // net5.0+ only
}
```

```csharp
// net5.0+: record  |  netstandard2.0: class (same members)
public record LogEntry(
    DateTime Timestamp,
    string   StoreType,
    string   IntentType,   // Intent type name or "StateChanged"
    object?  Intent,
    object?  OldState,
    object?  NewState
);
```

---

## Analyzer Diagnostics

Included in the `Knotty` NuGet package as a Roslyn Analyzer.

| Code | Level | Condition | Message |
|------|-------|-----------|---------|
| `KNOT001` | Warning | `TState` is not a record | `TState '{TypeName}' is not a record. Knotty requires immutable state — use a record type.` |
| `KNOT002` | Warning | Record property has a mutable setter | `Property '{PropName}' on state type '{TypeName}' has a setter. State properties should be init-only.` |

```csharp
// Triggers KNOT001
public class CounterStore : KnottyStore<CounterState, CounterIntent>  // CounterState is a class
{ }

// Triggers KNOT002
public record OrderState
{
    public List<Item> Items { get; set; }  // use { get; init; } instead
}
```
