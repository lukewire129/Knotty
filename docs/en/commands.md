# Commands & Source Generator

## Source Generator (recommended)

`dotnet add package Knotty` includes the Source Generator automatically.
Add `partial` to the Store class and attach `[IntentCommand]` / `[AsyncIntentCommand]` attributes — `ICommand` properties are generated for you.

### Field-based — Commands with no parameter

```csharp
public partial class CounterStore : KnottyStore<CounterState, CounterIntent>
{
    [IntentCommand]
    private readonly CounterIntent.Increment _increment = new();

    [IntentCommand]
    private readonly CounterIntent.Decrement _decrement = new();

    [AsyncIntentCommand(CanExecute = nameof(CanReset))]
    private readonly CounterIntent.Reset _reset = new();

    private bool CanReset() => !IsLoading;
}
```

**Generated output**

```csharp
// Auto-generated (do not edit)
partial class CounterStore
{
    private ICommand? _incrementCommand;
    public ICommand IncrementCommand => _incrementCommand ??= Command(_increment);

    private ICommand? _decrementCommand;
    public ICommand DecrementCommand => _decrementCommand ??= Command(_decrement);

    private IAsyncCommand? _resetCommand;
    public IAsyncCommand ResetCommand => _resetCommand ??= AsyncCommand(_reset, CanReset);
}
```

### Method-based — Commands with XAML CommandParameter

XAML `CommandParameter` is always a `string`. Convert types inside the method.

```csharp
public partial class CounterStore : KnottyStore<CounterState, CounterIntent>
{
    // CommandParameter(string) → IncrementBy(int)
    [IntentCommand]
    private CounterIntent.IncrementBy CreateIncrementBy(string value)
        => new(int.Parse(value));

    [AsyncIntentCommand]
    private CounterIntent.LoadData CreateLoadData(string url)
        => new(url);
}
```

```xml
<Button Command="{Binding IncrementByCommand}" CommandParameter="5" Content="+5" />
<Button Command="{Binding LoadDataCommand}" CommandParameter="https://api.example.com/data" />
```

### Name Generation Rules

| Member name | Generated command name |
|-------------|----------------------|
| `_increment` | `IncrementCommand` |
| `_reset` | `ResetCommand` |
| `CreateIncrementBy` | `IncrementByCommand` |
| `GetSearchResults` | `SearchResultsCommand` |
| `MakeFilter` | `FilterCommand` |

Strips leading `_`, `Create`, `Get`, `Make` prefixes, then appends `Command`.

### Custom Name with CommandName

```csharp
[IntentCommand(CommandName = "AddItemCommand")]
private readonly CounterIntent.Increment _inc = new();
// → public ICommand AddItemCommand
```

---

## Manual Creation (without Source Generator)

### Basic Command

```csharp
public class CounterStore : KnottyStore<CounterState, CounterIntent>
{
    public ICommand IncrementCommand { get; }
    public IAsyncCommand ResetCommand { get; }

    public CounterStore() : base(new CounterState(0))
    {
        IncrementCommand = Command(new CounterIntent.Increment());
        ResetCommand = AsyncCommand(new CounterIntent.Reset(), () => !IsLoading);
    }
}
```

### Parameterized Command

```csharp
public ICommand IncrementByCommand { get; }

public CounterStore() : base(new CounterState(0))
{
    IncrementByCommand = Command<string>(
        value => new CounterIntent.IncrementBy(int.Parse(value)));
}
```

---

## IAsyncCommand

Commands created with `AsyncCommand` / `[AsyncIntentCommand]` implement `IAsyncCommand`.

```csharp
public interface IAsyncCommand : ICommand, INotifyPropertyChanged
{
    Task ExecuteAsync(object? parameter);
    bool IsExecuting { get; }
}
```

When `IsExecuting` is `true`, `CanExecute()` automatically returns `false`.

```xml
<!-- Disable button while executing -->
<Button Command="{Binding ResetCommand}"
        IsEnabled="{Binding ResetCommand.IsExecuting, Converter={StaticResource InverseBool}}" />

<!-- Show spinner while executing -->
<ProgressBar Visibility="{Binding ResetCommand.IsExecuting, Converter={StaticResource BoolToVis}}" />
```

---

## Helper Method Signatures

```csharp
// Available as protected methods inside the Store
protected ICommand      Command(TIntent intent, Func<bool>? canExecute = null);
protected ICommand      Command<TParam>(Func<TParam, TIntent> factory, Func<TParam, bool>? canExecute = null);
protected IAsyncCommand AsyncCommand(TIntent intent, Func<bool>? canExecute = null);
protected IAsyncCommand AsyncCommand<TParam>(Func<TParam, TIntent> factory, Func<TParam, bool>? canExecute = null);
```

---

## Notes

- `partial` keyword on the Store class is **required** when using the Source Generator
- Do not mix `[RelayCommand]` (CommunityToolkit.Mvvm) with Knotty commands
- Do not use both manual and generator approaches for the same Intent
