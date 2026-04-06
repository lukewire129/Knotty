# SKILL: Knotty Bus

## Overview

`KnottyBus` is a built-in Event Bus for cross-Store communication.

## Namespace

```csharp
using Knotty;
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

Bus 구독은 **opt-in**. 수신 받고 싶은 Store 생성자에서 `SubscribeToBus()`를 명시적으로 호출해야 함.
호출하지 않으면 `KnottyBus.Publish()`를 보내도 해당 Store에 도달하지 않음.

```csharp
public class SettingsStore : KnottyStore<SettingsState, SettingsIntent>
{
    public SettingsStore() : base(new SettingsState())
    {
        // Bus에서 GlobalIntent를 수신하려면 명시적으로 구독
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

> `SubscribeToBus()`는 Store의 own TIntent를 Bus에 연결하는 헬퍼.
> 다른 타입의 Intent를 받으려면 `KnottyBus.Subscribe<TOtherIntent>(this, handler)` 직접 호출.

### 3. Publish Global Intent

```csharp
// From any Store or service
KnottyBus.Publish(new GlobalIntent.UserLoggedIn("user123"));
KnottyBus.Publish(new GlobalIntent.ThemeChanged(true));

// Send는 deprecated — Publish 사용
// KnottyBus.Send(...)  ← [Obsolete]
```

## API Reference

### Subscribe

```csharp
// Subscribe to specific Intent type
IDisposable token = KnottyBus.Subscribe<TIntent>(object subscriber, Action<TIntent> handler);
```

### Publish

```csharp
// 등록된 모든 구독자에게 broadcast. record 상속 구조 탐색 포함.
KnottyBus.Publish<TIntent>(TIntent intent);
```

### Unsubscribe

```csharp
// Option 1: Subscribe 반환값 Dispose
IDisposable token = KnottyBus.Subscribe<GlobalIntent>(this, handler);
token.Dispose();

// Option 2: Store.Dispose() 시 SubscribeToBus() 토큰 자동 해제
public override void Dispose()
{
    base.Dispose();
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
                KnottyBus.Publish(new GlobalIntent.UserLoggedIn(user.Id));
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

- ✅ `KnottyBus.Publish()` — broadcast. `Send()`는 deprecated.
- ✅ Bus 수신은 opt-in: `KnottyBus.Subscribe<T>(this, handler)` 생성자에서 명시적 호출
- ✅ 구독 토큰 또는 `base.Dispose()`로 반드시 해제
- ❌ Store 간 직접 참조 금지 — Bus로 통신
- ❌ 같은 TIntent 타입의 Store 여럿을 Bus로 연결 시 모두 반응함 — 의도 확인 필요
