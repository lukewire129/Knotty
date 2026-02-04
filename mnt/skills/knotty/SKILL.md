# SKILL: Knotty Framework (C# MVI)

## Overview

Knotty is an AI-First MVI (Model-View-Intent) framework for C# (.NET Standard 2.0+).
It enforces a single source of truth and unidirectional data flow, specifically optimized for LLM code generation.

## Quick Reference

| Feature | Skill File |
|---------|-----------|
| Basic Store, State, Intent | [Knotty.md](./Knotty.md) |
| Cross-Store Communication | [Knotty.Bus.md](./Knotty.Bus.md) |
| Debugging & Logging | [Knotty.Debugger.md](./Knotty.Debugger.md) |
| Command Binding (XAML) | [Knotty.Command.md](./Knotty.Command.md) |
| Side Effects (Navigation, Toast) | [Knotty.Effect.md](./Knotty.Effect.md) |
| Concurrent Intent Handling | [Knotty.IntentHandling.md](./Knotty.IntentHandling.md) |

## Installation

```bash
dotnet add package Knotty
```

## Namespace

```csharp
using Knotty.Core;
```

## Minimal Example

```csharp
// State
public record CounterState(int Count);

// Intent
public abstract record CounterIntent
{
    public record Increment : CounterIntent;
    public record Decrement : CounterIntent;
}

// Store
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

1. **State is Immutable** - Use `record` and `with` expressions
2. **Intent is Explicit** - All actions go through Intents
3. **Store is Single Source** - All logic in `HandleIntent`
4. **No Side Effects in State** - Use `IEffect` for navigation/toasts

## Agent Instructions

### DO ??
- Use `record` for State and Intent
- Use `with` expressions for State updates
- Use `partial class` for Source Generator
- Pass `CancellationToken` to async operations
- Use `IEffect` for one-time side effects

### DON'T ??
- Mutate State properties directly
- Use CommunityToolkit.Mvvm attributes
- Put navigation/toast flags in State
- Ignore `CancellationToken` in async handlers

## C# Compatibility

For .NET Standard 2.0 / .NET Framework, add this polyfill:

```csharp
namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit { }
}
```

