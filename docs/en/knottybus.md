# KnottyBus

An event bus that broadcasts Intents without direct Store-to-Store references.

## When to Use

- Notifying multiple Stores simultaneously after a login event
- App-wide events like theme or language changes
- When Stores need to communicate without knowing about each other

---

## Basic Usage

### 1. Define a Global Intent

```csharp
public abstract record GlobalIntent
{
    public record UserLoggedIn(string UserId)    : GlobalIntent;
    public record ThemeChanged(bool IsDark)      : GlobalIntent;
    public record LanguageChanged(string Culture) : GlobalIntent;
}
```

### 2. Subscribe in a Store (opt-in)

Bus subscription is **opt-in** — you must explicitly call `KnottyBus.Subscribe` in the constructor.
If you don't, `KnottyBus.Publish()` messages won't reach that Store.

```csharp
public class SettingsStore : KnottyStore<SettingsState, SettingsIntent>
{
    private readonly IDisposable _busToken;

    public SettingsStore() : base(new SettingsState())
    {
        // Register to receive GlobalIntents
        _busToken = KnottyBus.Subscribe<GlobalIntent>(this, OnGlobalIntent);
    }

    private void OnGlobalIntent(GlobalIntent intent)
    {
        switch (intent)
        {
            case GlobalIntent.ThemeChanged theme:
                State = State with { IsDarkMode = theme.IsDark };
                break;
            case GlobalIntent.LanguageChanged lang:
                State = State with { Culture = lang.Culture };
                break;
        }
    }

    public override void Dispose()
    {
        _busToken.Dispose();
        base.Dispose();
    }
}
```

### 3. Publish an Intent

```csharp
// Can be called from any Store or service
KnottyBus.Publish(new GlobalIntent.UserLoggedIn("user123"));
KnottyBus.Publish(new GlobalIntent.ThemeChanged(isDark: true));
```

---

## Store-to-Store Communication Example

```csharp
// AuthStore — notifies after login
public class AuthStore : KnottyStore<AuthState, AuthIntent>
{
    protected override async Task HandleIntent(AuthIntent intent, CancellationToken ct)
    {
        case AuthIntent.Login login:
            var user = await _service.LoginAsync(login.Email, login.Password, ct);
            State = State with { User = user, IsLoggedIn = true };

            // Notify other Stores
            KnottyBus.Publish(new GlobalIntent.UserLoggedIn(user.Id));
            break;
    }
}

// CartStore — loads cart after login
public class CartStore : KnottyStore<CartState, CartIntent>
{
    public CartStore() : base(new CartState())
    {
        KnottyBus.Subscribe<GlobalIntent>(this, intent =>
        {
            if (intent is GlobalIntent.UserLoggedIn loggedIn)
                Dispatch(new CartIntent.Load(loggedIn.UserId));
        });
    }
}
```

---

## API

```csharp
// Subscribe — returns IDisposable. Automatically released on Dispose().
IDisposable token = KnottyBus.Subscribe<TIntent>(object recipient, Action<TIntent> handler);

// Broadcast — walks the record inheritance hierarchy (delivers to base type subscribers too)
KnottyBus.Publish<TIntent>(TIntent intent);

// [Obsolete] Send → replaced by Publish
// KnottyBus.Send(intent);
```

**WeakReference-based** — if `recipient` is garbage collected, the subscription is automatically removed.
Explicit `Dispose()` is still recommended (avoid relying on GC timing).

---

## Notes

| Item | Details |
|------|---------|
| No auto-subscription | Just creating a Store does not subscribe it to the Bus. Call `Subscribe` explicitly. |
| Multiple Stores with same TIntent | All will react. Confirm this is intentional. |
| Direct Store references | `_otherStore.Dispatch(...)` is forbidden — use the Bus |
| Unsubscribe | Must call `Dispose()` on the Store or the token |
