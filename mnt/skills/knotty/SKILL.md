SKILL: Knotty Framework (C# MVI) 0. Environment & Setup
Library: Knotty (NuGet Package)

Namespace: using Knotty.Core; (Only one namespace required)

Installation: dotnet add package Knotty

Compatibility: .NET Standard 2.0+

C# Version: C# 9.0+ recommended (for record & with).

Note: If using older C# versions, replace record with class and manually implement immutability.

1. Overview
   Knotty is an AI-First MVI (Model-View-Intent) framework. It enforces a single source of truth and unidirectional data flow, specifically optimized for LLM code generation by eliminating implicit side effects.

Predictability: State is updated only via explicit Intents.

AI-Friendly: No magic code-behind; logic is centralized and declarative.

2. Structural Constraints (Strict)
   [State] - The Immutable Data
   Type: Use record (C# 9.0+) for automatic immutability.

Update: Use with expressions to return a new state instance.

Collections: Use List<T> or IEnumerable<T> but treat them as immutable (always replace the reference).

[Intent] - The Explicit Action
Type: abstract record (or abstract class with inheritance).

Represents "What happened", not "How to do it".

Example: AddTodo, RemoveTodo, ToggleFilter.

[Store] - The Logic Hub
Inherit KnottyStore<TState, TIntent>.

HandleIntent: The ONLY place for business logic and state transitions.

Automation: IsLoading (bool) toggles automatically during async execution.

State Update: State = State with { ... }; (or new instance for older C#).

3. Implementation Patterns
   Standard Store Boilerplate
   C#

using Knotty.Core;

public record MyState(int Count);
public abstract record MyIntent {
public record Increment : MyIntent;
}

public class MyStore : KnottyStore<MyState, MyIntent> {
public MyStore() : base(new MyState(0)) { }

    protected override async Task HandleIntent(MyIntent intent) {
        State = intent switch {
            MyIntent.Increment => State with { Count = State.Count + 1 },
            _ => State
        };
    }

}
Collection Updates (The LINQ Way)
Add: State = State with { Items = State.Items.Append(newItem).ToList() };

Delete: State = State with { Items = State.Items.Where(x => x.Id != id).ToList() };

4. Cross-Store Communication
   Use KnottyBus.Instance.Send(GlobalIntent) for broadcasting.

Stores subscribe in constructor: KnottyBus.Instance.Subscribe<GlobalIntent.Event>(this, action);

5. Agent Instructions
   IMPORT: Always use using Knotty.Core;.

DON'T use CommunityToolkit.Mvvm attributes.

DON'T mutate State properties directly.

DO leverage IsLoading for UI state (e.g., IsEnabled="{Binding !IsLoading}").

C# Compatibility: Ensure the project is configured for C# 9.0+ to use record and with even on .NET Standard 2.0.
