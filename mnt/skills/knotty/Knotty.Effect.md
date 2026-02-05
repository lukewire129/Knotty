# SKILL: Knotty Effect

## Overview

`IEffect` provides a way to trigger one-time side effects (like navigation, dialogs, toasts) without polluting State.

## Namespace

```csharp
using Knotty;       // IEffect
using Knotty.Core;  // KnottyStore
```

## Concept

**State** = What to display (persistent, bindable)
**Effect** = What to do once (navigation, toast, dialog)

Effects are NOT stored in State because:
- They're one-time actions
- They don't represent UI state
- They shouldn't trigger re-renders

## Usage

### 1. Define Effects

```csharp
public abstract record AppEffect : IEffect
{
    public record ShowToast(string Message, ToastType Type = ToastType.Info) : AppEffect;
    public record NavigateTo(string Route) : AppEffect;
    public record ShowDialog(string Title, string Message) : AppEffect;
    public record CloseWindow : AppEffect;
}

public enum ToastType { Info, Success, Warning, Error }
```

### 2. Emit Effect from Store

```csharp
public class OrderStore : KnottyStore<OrderState, OrderIntent>
{
    protected override async Task HandleIntent(OrderIntent intent, CancellationToken ct)
    {
        switch (intent)
        {
            case OrderIntent.Submit:
                try
                {
                    await _orderService.SubmitAsync(State.Order);
                    State = State with { IsSubmitted = true };
                    
                    // Emit side effects
                    EmitEffect(new AppEffect.ShowToast("Order submitted!", ToastType.Success));
                    EmitEffect(new AppEffect.NavigateTo("/orders"));
                }
                catch (Exception ex)
                {
                    EmitEffect(new AppEffect.ShowToast(ex.Message, ToastType.Error));
                }
                break;
        }
    }
}
```

### 3. Handle Effect in View

**Option A: IEffectSource (Recommended for Auto-DI)**

For auto-DI scenarios (Prism, DryIoc, etc.) where DataContext is set automatically:

```csharp
public partial class OrderWindow : Window
{
    private IDisposable? _effectSubscription;

    public OrderWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Unloaded += OnUnloaded;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // Unsubscribe from old DataContext
        _effectSubscription?.Dispose();

        // Subscribe to new DataContext if it supports Effects
        if (e.NewValue is IEffectSource source)
        {
            _effectSubscription = source.Effects.Subscribe<AppEffect>(HandleEffect);
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _effectSubscription?.Dispose();
    }

    private void HandleEffect(AppEffect effect)
    {
        switch (effect)
        {
            case AppEffect.ShowToast toast:
                ShowToastNotification(toast.Message, toast.Type);
                break;

            case AppEffect.NavigateTo nav:
                NavigationService.Navigate(nav.Route);
                break;

            case AppEffect.ShowDialog dialog:
                MessageBox.Show(dialog.Message, dialog.Title);
                break;

            case AppEffect.CloseWindow:
                this.Close();
                break;
        }
    }
}
```

**Option B: Direct Store Reference**

For manual Store creation:

```csharp
public partial class OrderWindow : Window
{
    private readonly OrderStore _store;
    private readonly IDisposable _effectSubscription;

    public OrderWindow()
    {
        InitializeComponent();
        _store = new OrderStore();
        DataContext = _store;

        // Subscribe to effects
        _effectSubscription = _store.Effects.Subscribe<AppEffect>(HandleEffect);

        Unloaded += (s, e) => _effectSubscription.Dispose();
    }

    private void HandleEffect(AppEffect effect)
    {
        // Same switch as above
    }
}
```

**Option C: OnEffect Override (No subscription needed)**

Handle effects directly in Store subclass:

```csharp
public class OrderStore : KnottyStore<OrderState, OrderIntent>
{
    public event Action<IEffect>? EffectEmitted;

    protected override void OnEffect(IEffect effect)
    {
        EffectEmitted?.Invoke(effect);
    }
}
```

## API Reference

### IEffect Interface

```csharp
public interface IEffect;
```

Marker interface for all effects.

### IEffectSource Interface

```csharp
public interface IEffectSource
{
    IObservable<IEffect> Effects { get; }
}
```

Exposes Effect stream for View subscription. `KnottyStore` implements this interface.

### EffectExtensions

```csharp
// Subscribe to specific effect type
source.Effects.Subscribe<AppEffect>(effect => { ... });

// Subscribe to all effects
source.Effects.Subscribe(effect => { ... });
```

### Store Methods

```csharp
// Observable stream of effects (IEffectSource)
public IObservable<IEffect> Effects { get; }

// Emit an effect
protected void EmitEffect(IEffect effect);

// Override to handle effects directly in Store (optional)
protected virtual void OnEffect(IEffect effect) { }
```

## Patterns

### Toast Notifications

```csharp
public abstract record ToastEffect : IEffect
{
    public record Info(string Message) : ToastEffect;
    public record Success(string Message) : ToastEffect;
    public record Warning(string Message) : ToastEffect;
    public record Error(string Message) : ToastEffect;
}

// Usage
EmitEffect(new ToastEffect.Success("Saved successfully!"));
```

### Navigation

```csharp
public abstract record NavigationEffect : IEffect
{
    public record GoTo(string Route) : NavigationEffect;
    public record GoBack : NavigationEffect;
    public record GoHome : NavigationEffect;
}

// Usage
EmitEffect(new NavigationEffect.GoTo("/settings"));
EmitEffect(new NavigationEffect.GoBack());
```

### Dialogs

```csharp
public abstract record DialogEffect : IEffect
{
    public record Confirm(string Title, string Message, Action OnConfirm) : DialogEffect;
    public record Alert(string Title, string Message) : DialogEffect;
}
```

## Agent Instructions

- ✅ Use `IEffect` for one-time side effects
- ✅ Effects are NOT stored in State
- ✅ Handle effects in View layer (code-behind or behavior)
- ❌ DON'T put navigation/toast flags in State
- ❌ DON'T use Effects for data that should persist
