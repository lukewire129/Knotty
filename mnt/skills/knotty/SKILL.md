---
name: knotty
description: "Build C# UI applications using the Knotty MVI (Model-View-Intent) framework for .NET Standard 2.0+. Use when creating stores with immutable state and explicit intents in MAUI, WPF, or Avalonia projects, implementing cross-store communication via KnottyBus, adding XAML command bindings with source generators, handling side effects like navigation and toasts, or configuring concurrent intent handling strategies."
---

# Knotty Framework

Knotty is an AI-first MVI (Model-View-Intent) framework for C# (.NET Standard 2.0+). It enforces a single source of truth and unidirectional data flow, replacing traditional MVVM with immutable state and explicit intents.

## Quick Reference

| Feature | Reference |
|---------|-----------|
| Basic Store, State, Intent | [Knotty.md](./Knotty.md) |
| Cross-Store Communication | [Knotty.Bus.md](./Knotty.Bus.md) |
| Debugging & Logging | [Knotty.Debugger.md](./Knotty.Debugger.md) |
| Command Binding (XAML) | [Knotty.Command.md](./Knotty.Command.md) |
| Side Effects (Navigation, Toast) | [Knotty.Effect.md](./Knotty.Effect.md) |
| Concurrent Intent Handling | [Knotty.IntentHandling.md](./Knotty.IntentHandling.md) |

## Workflow

1. **Define State** as an immutable `record` representing the UI truth
2. **Define Intents** as nested `abstract record` subtypes for each user action
3. **Create a Store** extending `KnottyStore<TState, TIntent>` with `HandleIntent` logic
4. **Bind to XAML** using `[IntentCommand]` source generator or manual `Command()` helpers
5. **Add side effects** via `EmitEffect()` for navigation, toasts, and dialogs (see [Knotty.Effect.md](./Knotty.Effect.md))
6. **Configure concurrency** by overriding `GetStrategy()` per intent type (see [Knotty.IntentHandling.md](./Knotty.IntentHandling.md))

## Installation

```bash
dotnet add package Knotty
```

```csharp
using Knotty.Core;
```

## Minimal Example

```csharp
// 1. State — immutable record
public record CounterState(int Count);

// 2. Intent — what the user can do
public abstract record CounterIntent
{
    public record Increment : CounterIntent;
    public record Decrement : CounterIntent;
}

// 3. Store — handles all logic
public class CounterStore : KnottyStore<CounterState, CounterIntent>
{
    public CounterStore() : base(new CounterState(0)) { }

    protected override async Task HandleIntent(CounterIntent intent, CancellationToken ct)
    {
        State = intent switch
        {
            CounterIntent.Increment => State with { Count = State.Count + 1 },
            CounterIntent.Decrement => State with { Count = State.Count - 1 },
            _ => State
        };
    }
}
```

## Key Principles

- **State is immutable** — use `record` types and `with` expressions for updates
- **Intents are explicit** — all user actions flow through typed intent records
- **Store is the single source of logic** — all business logic lives in `HandleIntent`
- **Side effects stay out of state** — use `IEffect` for navigation, toasts, and dialogs

## Rules

- Always use `record` for State and Intent definitions
- Always update State via `with` expressions, never mutate properties directly
- Always pass `CancellationToken` to async operations inside `HandleIntent`
- Always use `partial class` when using the `[IntentCommand]` source generator
- Always use `IEffect` for one-time side effects like navigation and toasts
- Never use `CommunityToolkit.Mvvm` attributes (`[ObservableProperty]`, `[RelayCommand]`) — use Knotty's own patterns
- Never put navigation or toast flags in State — emit them as effects instead
- Never ignore `CancellationToken` in async handlers, especially with `CancelPrevious` strategy

## C# Compatibility

For .NET Standard 2.0 / .NET Framework targets, add this polyfill:

```csharp
namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit { }
}
```
