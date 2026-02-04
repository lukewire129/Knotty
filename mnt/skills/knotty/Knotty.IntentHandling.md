# SKILL: Knotty Intent Handling

## Overview

Knotty provides multiple strategies for handling concurrent Intent dispatches.

## Namespace

```csharp
using Knotty.Core;
```

## Strategies

```csharp
public enum IntentHandlingStrategy
{
    Block,           // Ignore if already processing (default)
    Queue,           // Queue and process in order
    Debounce,        // Wait and process only the last one
    CancelPrevious,  // Cancel current and start new
    Parallel         // Process simultaneously
}
```

### Block (Default)

Ignores new Intent if one is already being processed.

```
User clicks: [Click] [Click] [Click]
Processed:   [  1  ]  (ignored) (ignored)
```

**Best for:** Single submit actions, preventing double-clicks

### Queue

Queues all Intents and processes them in order.

```
User clicks: [Click] [Click] [Click]
Processed:   [  1  ] [  2  ] [  3  ]
```

**Best for:** Message sending, sequential operations

### Debounce

Waits for a pause, then processes only the last Intent.

```
User types:  [a] [ab] [abc] [abcd] ... (pause)
Processed:                              [abcd]
```

**Best for:** Search input, auto-save, filtering

### CancelPrevious

Cancels the current operation and starts the new one.

```
User clicks: [Click]    [Click]
Processed:   [  1... cancelled] [  2  ]
```

**Best for:** Search API calls, navigation, loading latest data

### Parallel

Processes all Intents simultaneously (no `IsLoading` blocking).

```
User clicks: [Click] [Click] [Click]
Processed:   [  1  ]
             [  2  ]
             [  3  ]
```

**Best for:** Independent background tasks, logging

## Usage

### Override GetStrategy

```csharp
public class SearchStore : KnottyStore<SearchState, SearchIntent>
{
    protected override IntentHandlingStrategy GetStrategy(SearchIntent intent)
    {
        return intent switch
        {
            // Debounce search input
            SearchIntent.UpdateQuery => IntentHandlingStrategy.Debounce,
            
            // Cancel previous search when new one starts
            SearchIntent.Search => IntentHandlingStrategy.CancelPrevious,
            
            // Queue filter operations
            SearchIntent.ApplyFilter => IntentHandlingStrategy.Queue,
            
            // Default: Block
            _ => IntentHandlingStrategy.Block
        };
    }
}
```

### Custom Debounce Delay

```csharp
protected override TimeSpan GetDebounceDelay(SearchIntent intent)
{
    return intent switch
    {
        SearchIntent.UpdateQuery => TimeSpan.FromMilliseconds(500),  // Wait 500ms
        SearchIntent.AutoSave => TimeSpan.FromSeconds(2),            // Wait 2s
        _ => TimeSpan.FromMilliseconds(300)                          // Default 300ms
    };
}
```

### CancellationToken in HandleIntent

```csharp
protected override async Task HandleIntent(SearchIntent intent, CancellationToken ct)
{
    switch (intent)
    {
        case SearchIntent.Search search:
            // Pass CancellationToken to async operations
            var results = await _searchService.SearchAsync(search.Query, ct);
            
            // Check cancellation before updating state
            ct.ThrowIfCancellationRequested();
            
            State = State with { Results = results };
            break;
    }
}
```

## Complete Example

```csharp
public record SearchState(
    string Query = "",
    List<SearchResult> Results = null!,
    bool IsSearching = false
);

public abstract record SearchIntent
{
    public record UpdateQuery(string Query) : SearchIntent;
    public record Search : SearchIntent;
    public record Clear : SearchIntent;
}

public class SearchStore : KnottyStore<SearchState, SearchIntent>
{
    private readonly ISearchService _searchService;

    public SearchStore(ISearchService searchService) 
        : base(new SearchState(Results: new List<SearchResult>()))
    {
        _searchService = searchService;
    }

    protected override IntentHandlingStrategy GetStrategy(SearchIntent intent)
    {
        return intent switch
        {
            SearchIntent.UpdateQuery => IntentHandlingStrategy.Debounce,
            SearchIntent.Search => IntentHandlingStrategy.CancelPrevious,
            _ => IntentHandlingStrategy.Block
        };
    }

    protected override TimeSpan GetDebounceDelay(SearchIntent intent)
    {
        return intent switch
        {
            SearchIntent.UpdateQuery => TimeSpan.FromMilliseconds(400),
            _ => base.GetDebounceDelay(intent)
        };
    }

    protected override async Task HandleIntent(SearchIntent intent, CancellationToken ct)
    {
        switch (intent)
        {
            case SearchIntent.UpdateQuery update:
                State = State with { Query = update.Query };
                // Auto-trigger search after debounce
                Dispatch(new SearchIntent.Search());
                break;

            case SearchIntent.Search:
                if (string.IsNullOrWhiteSpace(State.Query))
                {
                    State = State with { Results = new List<SearchResult>() };
                    return;
                }

                var results = await _searchService.SearchAsync(State.Query, ct);
                ct.ThrowIfCancellationRequested();
                State = State with { Results = results };
                break;

            case SearchIntent.Clear:
                State = State with { Query = "", Results = new List<SearchResult>() };
                break;
        }
    }
}
```

## Agent Instructions

- ✅ Override `GetStrategy()` for Intent-specific handling
- ✅ Use `Debounce` for search/filter input
- ✅ Use `CancelPrevious` for API calls that should show latest only
- ✅ Pass `CancellationToken` to async operations
- ✅ Check `ct.ThrowIfCancellationRequested()` before state updates
- ❌ DON'T use `Parallel` for operations that modify same State
- ❌ DON'T ignore `CancellationToken` in `CancelPrevious` strategy
