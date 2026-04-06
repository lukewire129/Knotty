# Intent Handling Strategies

Configure how concurrent Intents are handled, per Intent type.

## Strategy List

```csharp
public enum IntentHandlingStrategy
{
    Block,          // Ignore new intents while one is processing (default)
    Queue,          // Process in order
    Debounce,       // Wait for input to stop, then process the last one
    CancelPrevious, // Cancel current work and start the new intent
    Parallel        // Process simultaneously (does not touch IsLoading)
}
```

---

## Block (default)

Ignores new Intents while one is already processing.

```
User:    [click] [click] [click]
Process: [ 1st ]  (skip)  (skip)
```

**Best for**: form submissions, payment buttons — prevents duplicate execution

```csharp
// This is the default — no override needed
// Explicit usage:
protected override IntentHandlingStrategy GetStrategy(MyIntent intent)
    => IntentHandlingStrategy.Block;
```

---

## Queue

Enqueues all Intents and processes them in order.

```
User:    [click] [click] [click]
Process: [ 1st ] → [ 2nd ] → [ 3rd ]
```

**Best for**: message sending, operations that require ordering

```csharp
protected override IntentHandlingStrategy GetStrategy(MyIntent intent)
    => IntentHandlingStrategy.Queue;
```

---

## Debounce

Waits for rapid input to stop, then processes only the last Intent.

```
User:    [a] [ab] [abc] [abcd] ... (pause)
Process:                             [abcd]
```

**Best for**: search input, auto-save, filtering

```csharp
protected override IntentHandlingStrategy GetStrategy(SearchIntent intent)
{
    return intent switch
    {
        SearchIntent.UpdateQuery => IntentHandlingStrategy.Debounce,
        _ => IntentHandlingStrategy.Block
    };
}

protected override TimeSpan GetDebounceDelay(SearchIntent intent)
{
    return intent switch
    {
        SearchIntent.UpdateQuery => TimeSpan.FromMilliseconds(400),
        _ => TimeSpan.FromMilliseconds(300) // default
    };
}
```

---

## CancelPrevious

Cancels the running operation when a new Intent arrives, then starts the new one.

```
User:    [searchA] -------- [searchB]
Process: [searchA... cancel] → [searchB]
```

**Best for**: search API calls, operations where only the latest result matters

```csharp
protected override IntentHandlingStrategy GetStrategy(SearchIntent intent)
{
    return intent switch
    {
        SearchIntent.Search => IntentHandlingStrategy.CancelPrevious,
        _ => IntentHandlingStrategy.Block
    };
}
```

**Always pass `ct` in HandleIntent**

```csharp
protected override async Task HandleIntent(SearchIntent intent, CancellationToken ct)
{
    case SearchIntent.Search search:
        var results = await _api.SearchAsync(search.Query, ct); // pass ct
        ct.ThrowIfCancellationRequested();                       // check cancellation
        State = State with { Results = results };
        break;
}
```

**State restore on cancellation**

```csharp
case SearchIntent.Reset:
    var prev = State;
    State = State with { Message = "Resetting..." };
    try
    {
        await Task.Delay(3000, ct);
        State = State with { Count = 0, Message = "Done" };
    }
    catch (OperationCanceledException)
    {
        State = prev; // restore original state on cancellation
        throw;        // must rethrow
    }
    break;
```

> Swallowing `OperationCanceledException` without rethrowing may prevent `IsLoading` from being restored.

---

## Parallel

Processes all Intents simultaneously without touching `IsLoading`.

```
User:    [click] [click] [click]
Process: [ 1st ]
         [ 2nd ]  (concurrent)
         [ 3rd ]
```

**Best for**: independent background tasks, logging

```csharp
protected override IntentHandlingStrategy GetStrategy(LogIntent intent)
    => IntentHandlingStrategy.Parallel;
```

> Parallel does **not** set `IsLoading = true`. Concurrent State mutations from multiple Parallel intents can cause race conditions.

---

## Mixed Strategies per Intent

```csharp
protected override IntentHandlingStrategy GetStrategy(SearchIntent intent)
{
    return intent switch
    {
        SearchIntent.UpdateQuery => IntentHandlingStrategy.Debounce,
        SearchIntent.Search      => IntentHandlingStrategy.CancelPrevious,
        SearchIntent.ApplyFilter => IntentHandlingStrategy.Queue,
        SearchIntent.LogView     => IntentHandlingStrategy.Parallel,
        _                        => IntentHandlingStrategy.Block
    };
}
```

---

## Strategy Selection Guide

| Situation | Strategy |
|-----------|----------|
| Prevent duplicate button clicks | `Block` |
| Ordering must be preserved | `Queue` |
| Process after text input stops | `Debounce` |
| Search API (always want latest result) | `CancelPrevious` |
| Independent background tasks | `Parallel` |
