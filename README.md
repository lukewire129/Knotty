# Knotty

**Knotty** is an AI-first MVI (Model-View-Intent) framework for C# (.NET Standard 2.0+).

Designed specifically for modern UI frameworks like **MAUI, WPF, and Avalonia,** Knotty replaces the complexity of traditional MVVM with a predictable, immutable, and AI-friendly architecture.

### ?? Why Knotty?

- **AI-Optimized**: Explicit State and Intent structures make it easy for AI agents (GitHub Copilot, Cursor, etc.) to generate and maintain code without side effects.

- **Predictable State**: Uses immutable record types. No more chasing down which property setter triggered a bug.

- **Boilerplate-Free**: Automatic IsLoading management, built-in error handling, and Source Generator for Commands.

- **Lightweight**: Target .NET Standard 2.0, making it compatible with almost all .NET environments.

### ?? Installation

```bash
dotnet add package Knotty
```

### ?? Core Concepts

**1. State (The Truth)**

Define your UI state as a single, immutable record.

```csharp
public record TodoState(List<Todo> Items, string Filter = "");
```

**2. Intent (The Action)**

Define what the user can do using discriminated unions (nested records).

```csharp
public abstract record TodoIntent
{
    public record Add(string Text) : TodoIntent;
    public record Toggle(Guid Id) : TodoIntent;
}
```

**3. Store (The Logic)**

Handle all business logic in one place. `IsLoading` is managed automatically for async tasks.

```csharp
using Knotty.Core;

public class TodoStore : KnottyStore<TodoState, TodoIntent>
{
    protected override async Task HandleIntent(TodoIntent intent, CancellationToken ct)
    {
        switch (intent)
        {
            case TodoIntent.Add add:
                var newItem = await _api.CreateAsync(add.Text, ct);
                State = State with { Items = State.Items.Append(newItem).ToList() };
                break;
        }
    }
}
```

### ? Key Features

| Feature | Description |
|---------|-------------|
| **Automatic Loading** | `IsLoading` toggles automatically while `HandleIntent` is running |
| **KnottyBus** | Built-in Event Bus for cross-Store communication |
| **Error Handling** | Implements `INotifyDataErrorInfo`. Exceptions in `HandleIntent` are captured |
| **Command Generator** | `[IntentCommand]` attribute auto-generates `ICommand` properties |
| **Intent Handling Strategies** | Block, Queue, Debounce, CancelPrevious, Parallel |
| **Effects** | One-time side effects (navigation, toast) without polluting State |

### ?? Command Generator (Source Generator)

```csharp
using Knotty.Core.Attributes;

public partial class CounterStore : KnottyStore<CounterState, CounterIntent>
{
    [IntentCommand]
    private readonly CounterIntent.Increment _increment = new();

    [AsyncIntentCommand(CanExecute = nameof(CanReset))]
    private readonly CounterIntent.Reset _reset = new();

    // With parameter (XAML CommandParameter)
    [IntentCommand]
    private CounterIntent.IncrementBy CreateIncrementBy(string value) => new(int.Parse(value));

    private bool CanReset() => !IsLoading;
}
```

Generated:
```csharp
public ICommand IncrementCommand => ...;
public IAsyncCommand ResetCommand => ...;
public ICommand IncrementByCommand => ...;
```

### ?? Documentation

For detailed documentation, see the skill files:

| Topic | File |
|-------|------|
| Basic Usage | [mnt/skills/knotty/Knotty.md](mnt/skills/knotty/Knotty.md) |
| KnottyBus | [mnt/skills/knotty/Knotty.Bus.md](mnt/skills/knotty/Knotty.Bus.md) |
| Debugger | [mnt/skills/knotty/Knotty.Debugger.md](mnt/skills/knotty/Knotty.Debugger.md) |
| Command | [mnt/skills/knotty/Knotty.Command.md](mnt/skills/knotty/Knotty.Command.md) |
| Effect | [mnt/skills/knotty/Knotty.Effect.md](mnt/skills/knotty/Knotty.Effect.md) |
| Intent Handling | [mnt/skills/knotty/Knotty.IntentHandling.md](mnt/skills/knotty/Knotty.IntentHandling.md) |

### ?? Tips for .NET Standard 2.0 / Framework Users

Knotty works best with C# `record` and `with` expressions. If you are targeting older frameworks (like .NET Framework 4.7.2+ or .NET Standard 2.0), follow these two simple steps to enable modern C# features:

**Step 1: Update your** `.csproj` Set the C# language version to **9.0** or higher.

```xml
<PropertyGroup>
  <LangVersion>9.0</LangVersion>
</PropertyGroup>
```

**Step 2: Add the Compatibility Polyfill** Add the following code anywhere in your project (e.g., `Compatibility.cs`). This "tricks" the compiler into allowing `record` features on older platforms.

```csharp
namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit { }
}
```

### ?? License

MIT License. Feel free to use and contribute!