# SKILL: Knotty Command

## Overview

Knotty provides `IntentCommand` and `AsyncIntentCommand` for XAML binding, with Source Generator support for automatic Command generation.

## Namespace

```csharp
using Knotty.Core;
using Knotty.Core.Commands;
using Knotty.Core.Attributes;  // For Source Generator
```

## Manual Command Creation

### Basic Command (No Parameter)

```csharp
public class CounterStore : KnottyStore<CounterState, CounterIntent>
{
    public ICommand IncrementCommand { get; }
    public ICommand DecrementCommand { get; }
    public IAsyncCommand ResetCommand { get; }

    public CounterStore() : base(new CounterState(0))
    {
        IncrementCommand = Command(new CounterIntent.Increment());
        DecrementCommand = Command(new CounterIntent.Decrement());
        ResetCommand = AsyncCommand(new CounterIntent.Reset(), () => !IsLoading);
    }
}
```

### Parameterized Command

```csharp
public ICommand IncrementByCommand { get; }

public CounterStore() : base(new CounterState(0))
{
    // CommandParameter (string from XAML) → Intent
    IncrementByCommand = Command<string>(value => new CounterIntent.IncrementBy(int.Parse(value)));
}
```

```xml
<Button Command="{Binding IncrementByCommand}" CommandParameter="5" Content="+5" />
```

## Source Generator (Recommended)

### Setup

Add Knotty.Generators analyzer reference:

```xml
<ItemGroup>
  <ProjectReference Include="path\to\Knotty.csproj" />
  <Analyzer Include="path\to\Knotty.Generators.dll" />
</ItemGroup>
```

### Field-Based Command (No Parameter)

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

**Generated Code:**

```csharp
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

### Method-Based Command (With Parameter)

```csharp
public partial class CounterStore : KnottyStore<CounterState, CounterIntent>
{
    // XAML CommandParameter is always string
    [IntentCommand]
    private CounterIntent.IncrementBy CreateIncrementBy(string value) => new(int.Parse(value));

    [AsyncIntentCommand]
    private CounterIntent.LoadData CreateLoadData(string url) => new(url);
}
```

**Generated Code:**

```csharp
partial class CounterStore
{
    private ICommand? _incrementByCommand;
    public ICommand IncrementByCommand => _incrementByCommand ??= Command<string>(CreateIncrementBy);

    private IAsyncCommand? _loadDataCommand;
    public IAsyncCommand LoadDataCommand => _loadDataCommand ??= AsyncCommand<string>(CreateLoadData);
}
```

### Naming Rules

| Member Name | Generated Command Name |
|-------------|----------------------|
| `_increment` | `IncrementCommand` |
| `_reset` | `ResetCommand` |
| `CreateIncrementBy` | `IncrementByCommand` |
| `GetLoadData` | `LoadDataCommand` |
| `MakeSearch` | `SearchCommand` |

### Custom Command Name

```csharp
[IntentCommand(CommandName = "AddItemCommand")]
private readonly CounterIntent.Increment _increment = new();
```

## IAsyncCommand Features

### IsExecuting Property

```csharp
public interface IAsyncCommand : ICommand, INotifyPropertyChanged
{
    Task ExecuteAsync(object? parameter);
    bool IsExecuting { get; }
}
```

### XAML Binding

```xml
<!-- Disable button while executing -->
<Button Command="{Binding ResetCommand}" 
        Content="Reset"
        IsEnabled="{Binding ResetCommand.IsExecuting, Converter={StaticResource InverseBool}}" />

<!-- Show loading indicator -->
<ProgressBar Visibility="{Binding ResetCommand.IsExecuting, Converter={StaticResource BoolToVis}}" />
```

## API Reference

### Store Helper Methods

```csharp
// Sync Command (fixed Intent)
protected ICommand Command(TIntent intent, Func<bool>? canExecute = null);

// Sync Command (with parameter)
protected ICommand Command<TParameter>(Func<TParameter, TIntent> factory, Func<TParameter, bool>? canExecute = null);

// Async Command (fixed Intent)
protected IAsyncCommand AsyncCommand(TIntent intent, Func<bool>? canExecute = null);

// Async Command (with parameter)
protected IAsyncCommand AsyncCommand<TParameter>(Func<TParameter, TIntent> factory, Func<TParameter, bool>? canExecute = null);
```

## Agent Instructions

- ✅ Use `partial class` for Source Generator
- ✅ Use method-based for parameterized Commands
- ✅ XAML CommandParameter is always `string` - convert in method
- ✅ Use `IAsyncCommand` for async operations
- ❌ DON'T use CommunityToolkit.Mvvm `[RelayCommand]`
- ❌ DON'T mix manual and generated Commands for same Intent
