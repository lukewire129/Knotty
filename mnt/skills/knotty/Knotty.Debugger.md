# SKILL: Knotty Debugger

## Overview

`KnottyDebugger` provides built-in logging and debugging for Intent dispatching and State changes.

## Namespace

```csharp
using Knotty.Core;
```

## Enable Debugging

```csharp
// Enable in App startup (Debug mode only)
#if DEBUG
KnottyDebugger.IsEnabled = true;
#endif
```

## Features

### Automatic Logging

When enabled, Knotty automatically logs:
- Intent dispatches
- State changes

```
[Knotty] TodoStore <- Add
[Knotty] TodoStore <- Toggle
[Knotty] TodoStore <- Delete
```

### Log Entry Structure

```csharp
public record LogEntry(
    DateTime Timestamp,
    string StoreType,      // "TodoStore"
    string IntentType,     // "Add", "Toggle", etc.
    object? Intent,        // The actual Intent object
    object? OldState,      // State before change
    object? NewState       // State after change
);
```

## API Reference

### Properties

```csharp
// Enable/disable logging
KnottyDebugger.IsEnabled = true;

// Access all logs
IReadOnlyList<LogEntry> logs = KnottyDebugger.Logs;
```

### Methods

```csharp
// Clear all logs
KnottyDebugger.Clear();

// Export to JSON file (.NET 5.0+)
await KnottyDebugger.ExportToFileAsync("knotty-logs.json");
```

## Usage Examples

### Debug Window

```csharp
public partial class DebugWindow : Window
{
    public DebugWindow()
    {
        InitializeComponent();
        DataContext = KnottyDebugger.Logs;
    }
}
```

```xml
<ListBox ItemsSource="{Binding}">
    <ListBox.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="{Binding Timestamp, StringFormat=HH:mm:ss}" Margin="0,0,10,0"/>
                <TextBlock Text="{Binding StoreType}" FontWeight="Bold" Margin="0,0,5,0"/>
                <TextBlock Text="←" Margin="0,0,5,0"/>
                <TextBlock Text="{Binding IntentType}" Foreground="Blue"/>
            </StackPanel>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

### Export Logs on Error

```csharp
protected override void OnHandleError(Exception ex)
{
    #if DEBUG
    // Export logs when error occurs
    _ = KnottyDebugger.ExportToFileAsync($"error-{DateTime.Now:yyyyMMdd-HHmmss}.json");
    #endif
}
```

## Agent Instructions

- ✅ Enable `KnottyDebugger.IsEnabled` only in DEBUG mode
- ✅ Use `ExportToFileAsync` for debugging complex issues
- ✅ Clear logs periodically to avoid memory growth
- ❌ DON'T enable in production builds
