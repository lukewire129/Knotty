# Knotty

**C# MVI framework for MAUI, WPF, and Avalonia.**
Replaces MVVM boilerplate with immutable State, explicit Intent, and automatic loading/error handling.

```bash
dotnet add package Knotty
```

```csharp
using Knotty;

// 1. State
public record CounterState(int Count = 0);

// 2. Intent
public abstract record CounterIntent
{
    public record Increment : CounterIntent;
    public record Reset    : CounterIntent;
}

// 3. Store — IsLoading, error handling, INPC all automatic
public partial class CounterStore : KnottyStore<CounterState, CounterIntent>
{
    public CounterStore() : base(new CounterState()) { }

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
            case CounterIntent.Reset:
                await Task.Delay(500, ct);
                State = State with { Count = 0 };
                break;
        }
    }

    private bool CanReset() => !IsLoading;
}
```

```xml
<TextBlock Text="{Binding State.Count}" />
<Button Command="{Binding IncrementCommand}" Content="+" />
<Button Command="{Binding ResetCommand}"     Content="Reset" />
<ProgressBar Visibility="{Binding IsLoading, Converter={StaticResource BoolToVis}}" />
```

## Features

| | |
|---|---|
| **Auto IsLoading** | Toggles automatically while `HandleIntent` runs |
| **Source Generator** | `[IntentCommand]` generates `ICommand` properties |
| **Intent Strategies** | Block · Queue · Debounce · CancelPrevious · Parallel |
| **Effects** | One-time side effects (navigation, toast) separate from State |
| **KnottyBus** | Cross-store event broadcast |
| **Error Handling** | `INotifyDataErrorInfo` built-in, exceptions auto-captured |
| **DI Ready** | `services.AddKnottyStore<MyStore>()` for net6.0+ |

## Documentation

**→ [knotty docs](https://lukewire129.github.io/knotty)**

## License

MIT
