# Getting Started

## Installation

```bash
dotnet add package Knotty
```

The NuGet package includes the Source Generator (analyzer) — no separate install needed.

**Supported targets**: `netstandard2.0` · `net6.0` · `net8.0`

---

## Core Flow

```
User input
    ↓
Command.Execute() or store.Dispatch(intent)
    ↓
KnottyStore.HandleIntent(intent, ct)     ← all business logic goes here
    ↓
State = State with { ... }               ← PropertyChanged fires automatically
    ↓
View bindings update
    ↓
EmitEffect(effect)  [optional]           ← navigation, toast, etc.
```

---

## Minimum Implementation

### 1. Define State

```csharp
// Immutable record. Holds all UI state.
public record CounterState(int Count = 0, string Message = "Ready");
```

### 2. Define Intent

```csharp
// All actions the user or system can request.
public abstract record CounterIntent
{
    public record Increment            : CounterIntent;
    public record IncrementBy(int N)   : CounterIntent;
    public record Reset                : CounterIntent;
}
```

### 3. Implement Store

```csharp
using Knotty;

public partial class CounterStore : KnottyStore<CounterState, CounterIntent>
{
    public CounterStore() : base(new CounterState()) { }

    // Source Generator auto-creates IncrementCommand, ResetCommand
    [IntentCommand]
    private readonly CounterIntent.Increment _increment = new();

    [AsyncIntentCommand(CanExecute = nameof(CanReset))]
    private readonly CounterIntent.Reset _reset = new();

    protected override async Task HandleIntent(CounterIntent intent, CancellationToken ct)
    {
        switch (intent)
        {
            case CounterIntent.Increment:
                State = State with { Count = State.Count + 1 };
                break;

            case CounterIntent.IncrementBy by:
                State = State with { Count = State.Count + by.N };
                break;

            case CounterIntent.Reset:
                await Task.Delay(500, ct);       // always pass ct
                State = State with { Count = 0, Message = "Reset!" };
                break;
        }
    }

    private bool CanReset() => !IsLoading;
}
```

### 4. Connect to View (WPF example)

```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new CounterStore();
    }
}
```

```xml
<TextBlock Text="{Binding State.Count}" />
<TextBlock Text="{Binding State.Message}" />

<Button Command="{Binding IncrementCommand}" Content="+" />
<Button Command="{Binding ResetCommand}"     Content="Reset" />

<ProgressBar Visibility="{Binding IsLoading, Converter={StaticResource BoolToVis}}" />
```

---

## What You Get for Free

Everything below works with zero additional code:

| Feature | Condition |
|---------|-----------|
| `IsLoading = true` | When `HandleIntent` starts |
| `IsLoading = false` | When `HandleIntent` finishes (including exceptions) |
| `PropertyChanged` | When `State = State with { ... }` |
| `HasErrors = true` | When an exception is thrown in `HandleIntent` |
| `ClearAllErrors()` | Automatically on the next intent |

---

## Next Steps

- Platform-specific DI setup → [Platform Setup](en/platform-setup)
- XAML binding patterns → [View Binding](en/view-binding)
- Concurrency control (debounce, cancel) → [Intent Handling](en/intent-handling)
