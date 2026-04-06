# Knotty

**C# MVI framework for MAUI, WPF, and Avalonia.**

Knotty replaces MVVM boilerplate with a simple three-piece architecture:
immutable **State**, explicit **Intent**, and a single **Store** that handles all logic.

```bash
dotnet add package Knotty
```

---

## Why Knotty?

**The MVVM problem**: `INotifyPropertyChanged`, `ObservableCollection`, `RelayCommand`, ViewModel inheritance, manual `OnPropertyChanged`... more infrastructure code than business logic.

**Knotty's approach**: One record for state, Intents for actions, `HandleIntent` for all logic. `IsLoading`, error handling, and INPC are all managed by the framework.

---

## 30-Second Example

```csharp
using Knotty;

public record CounterState(int Count = 0);

public abstract record CounterIntent
{
    public record Increment : CounterIntent;
    public record Reset    : CounterIntent;
}

public partial class CounterStore : KnottyStore<CounterState, CounterIntent>
{
    public CounterStore() : base(new CounterState()) { }

    [IntentCommand]
    private readonly CounterIntent.Increment _increment = new();

    [AsyncIntentCommand]
    private readonly CounterIntent.Reset _reset = new();

    protected override async Task HandleIntent(CounterIntent intent, CancellationToken ct)
    {
        switch (intent)
        {
            case CounterIntent.Increment:
                State = State with { Count = State.Count + 1 };
                break;
            case CounterIntent.Reset:
                await Task.Delay(500, ct);
                State = State with { Count = 0 };
                break;
        }
    }
}
```

```xml
<TextBlock Text="{Binding State.Count}" />
<Button Command="{Binding IncrementCommand}" Content="+" />
<Button Command="{Binding ResetCommand}"     Content="Reset" />
<ProgressBar Visibility="{Binding IsLoading, ...}" />
```

---

## Documentation

| Chapter | Contents |
|---------|----------|
| [Getting Started](/en/getting-started) | Installation, minimal example, core flow |
| [Core Concepts](/en/core-concepts) | State · Intent · Store design principles |
| [Platform Setup](/en/platform-setup) | WPF · MAUI · Avalonia bootstrapping |
| [View Binding](/en/view-binding) | DataContext, XAML bindings, Effect subscription |
| [Commands](/en/commands) | Source Generator, `[IntentCommand]`, parameters |
| [Intent Handling](/en/intent-handling) | Block · Queue · Debounce · CancelPrevious · Parallel |
| [Effects](/en/effects) | Navigation · Toast · Dialog patterns |
| [Error Handling](/en/error-handling) | Exception handling, cancellation restore, HasErrors |
| [KnottyBus](/en/knottybus) | Store-to-Store communication |
| [Debugging](/en/debugging) | KnottyDebugger, logs |
| [API Reference](/en/api-reference) | Full public API signatures |
