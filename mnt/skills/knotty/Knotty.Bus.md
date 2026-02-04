# SKILL: Knotty Bus

## Overview

`KnottyBus` is a built-in Event Bus for cross-Store communication.

## Namespace

```csharp
using Knotty.Core;
```

## Usage

### 1. Define Global Intent

```csharp
public abstract record GlobalIntent
{
    public record UserLoggedIn(string UserId) : GlobalIntent;
    public record ThemeChanged(bool IsDark) : GlobalIntent;
    public record LanguageChanged(string Culture) : GlobalIntent;
}
```

### 2. Subscribe in Store

```csharp
public class SettingsStore : KnottyStore<SettingsState, SettingsIntent>
{
    public SettingsStore() : base(new SettingsState())
    {
        // Subscribe to global events
        KnottyBus.Subscribe<GlobalIntent>(this, OnGlobalIntent);
    }

    private void OnGlobalIntent(GlobalIntent intent)
    {
        switch (intent)
        {
            case GlobalIntent.ThemeChanged theme:
                State = State with { IsDarkMode = theme.IsDark };
                break;
        }
    }
}
```

### 3. Send Global Intent

```csharp
// From any Store or ViewModel
KnottyBus.Send(new GlobalIntent.UserLoggedIn("user123"));
KnottyBus.Send(new GlobalIntent.ThemeChanged(true));
```

## API Reference

### Subscribe

```csharp
// Subscribe to specific Intent type
IDisposable token = KnottyBus.Subscribe<TIntent>(object subscriber, Action<TIntent> handler);
```

### Send

```csharp
// Broadcast Intent to all subscribers
KnottyBus.Send<TIntent>(TIntent intent);
```

### Unsubscribe

```csharp
// Option 1: Dispose the token
token.Dispose();

// Option 2: Store automatically unsubscribes on Dispose()
public override void Dispose()
{
    base.Dispose(); // Unsubscribes from Bus
}
```

## Patterns

### Store-to-Store Communication

```csharp
// AuthStore - sends event after login
public class AuthStore : KnottyStore<AuthState, AuthIntent>
{
    protected override async Task HandleIntent(AuthIntent intent, CancellationToken ct)
    {
        switch (intent)
        {
            case AuthIntent.Login login:
                var user = await _authService.LoginAsync(login.Email, login.Password);
                State = State with { User = user, IsLoggedIn = true };
                
                // Notify other stores
                KnottyBus.Send(new GlobalIntent.UserLoggedIn(user.Id));
                break;
        }
    }
}

// CartStore - reacts to login
public class CartStore : KnottyStore<CartState, CartIntent>
{
    public CartStore() : base(new CartState())
    {
        KnottyBus.Subscribe<GlobalIntent>(this, intent =>
        {
            if (intent is GlobalIntent.UserLoggedIn loggedIn)
            {
                // Load user's cart after login
                Dispatch(new CartIntent.LoadCart(loggedIn.UserId));
            }
        });
    }
}
```

## Agent Instructions

- ✅ Use `KnottyBus` for cross-Store communication
- ✅ Subscribe in constructor
- ✅ Store auto-unsubscribes on `Dispose()`
- ❌ DON'T use static events or singletons for communication
- ❌ DON'T directly reference other Stores
