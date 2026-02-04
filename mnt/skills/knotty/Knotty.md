# SKILL: Knotty Basic

## Overview

Knotty is an AI-First MVI (Model-View-Intent) framework for C# (.NET Standard 2.0+).

## Installation

```bash
dotnet add package Knotty
```

## Namespace

```csharp
using Knotty.Core;
```

## Core Concepts

### 1. State (Immutable Data)

```csharp
public record TodoState(List<Todo> Items, string Filter = "");
```

- Use `record` for automatic immutability
- Update with `with` expressions

### 2. Intent (Explicit Action)

```csharp
public abstract record TodoIntent
{
    public record Add(string Text) : TodoIntent;
    public record Toggle(Guid Id) : TodoIntent;
    public record Delete(Guid Id) : TodoIntent;
}
```

- Represents "What happened", not "How to do it"

### 3. Store (Logic Hub)

```csharp
public class TodoStore : KnottyStore<TodoState, TodoIntent>
{
    public TodoStore() : base(new TodoState(new List<Todo>())) { }

    protected override async Task HandleIntent(TodoIntent intent, CancellationToken ct)
    {
        switch (intent)
        {
            case TodoIntent.Add add:
                var newItem = new Todo(Guid.NewGuid(), add.Text);
                State = State with { Items = State.Items.Append(newItem).ToList() };
                break;

            case TodoIntent.Toggle toggle:
                State = State with
                {
                    Items = State.Items.Select(x =>
                        x.Id == toggle.Id ? x with { Done = !x.Done } : x).ToList()
                };
                break;

            case TodoIntent.Delete delete:
                State = State with
                {
                    Items = State.Items.Where(x => x.Id != delete.Id).ToList()
                };
                break;
        }
    }
}
```

## Built-in Features

### IsLoading

Automatically `true` while `HandleIntent` is running:

```csharp
// In XAML
<Button Content="Save" IsEnabled="{Binding IsLoading, Converter={StaticResource InverseBool}}" />
<ProgressBar Visibility="{Binding IsLoading, Converter={StaticResource BoolToVisibility}}" />
```

### INotifyPropertyChanged

Store automatically implements:
- `INotifyPropertyChanged`
- `INotifyPropertyChanging`
- `INotifyDataErrorInfo`

### State Updates

```csharp
// Single property
State = State with { Count = State.Count + 1 };

// Multiple properties
State = State with { Count = 0, Message = "Reset!" };

// Collection - Add
State = State with { Items = State.Items.Append(newItem).ToList() };

// Collection - Remove
State = State with { Items = State.Items.Where(x => x.Id != id).ToList() };

// Collection - Update
State = State with 
{ 
    Items = State.Items.Select(x => x.Id == id ? x with { Name = "New" } : x).ToList() 
};
```

## Agent Instructions

- ✅ Always use `using Knotty.Core;`
- ✅ Use `record` for State and Intent
- ✅ Update State only via `with` expressions
- ❌ DON'T mutate State properties directly
- ❌ DON'T use CommunityToolkit.Mvvm attributes
