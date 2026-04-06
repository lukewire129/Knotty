---
title: KnottyBus
nav_order: 10
---

# KnottyBus

Store 간 직접 참조 없이 Intent를 broadcast하는 이벤트 버스.

## 언제 쓰나

- 로그인 후 여러 Store에 동시에 알려야 할 때
- 테마/언어 변경처럼 앱 전역에 영향을 주는 이벤트
- Store가 서로를 모르는 상태에서 통신이 필요할 때

---

## 기본 사용

### 1. Global Intent 정의

```csharp
public abstract record GlobalIntent
{
    public record UserLoggedIn(string UserId)   : GlobalIntent;
    public record ThemeChanged(bool IsDark)     : GlobalIntent;
    public record LanguageChanged(string Culture) : GlobalIntent;
}
```

### 2. Store에서 구독 (opt-in)

Bus 구독은 **opt-in** — 생성자에서 명시적으로 `KnottyBus.Subscribe`를 호출해야 함.
호출하지 않으면 `KnottyBus.Publish()` 메시지가 해당 Store에 도달하지 않음.

```csharp
public class SettingsStore : KnottyStore<SettingsState, SettingsIntent>
{
    private readonly IDisposable _busToken;

    public SettingsStore() : base(new SettingsState())
    {
        // GlobalIntent 수신 등록
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

### 3. Intent Publish

```csharp
// 어느 Store, 서비스에서든 호출 가능
KnottyBus.Publish(new GlobalIntent.UserLoggedIn("user123"));
KnottyBus.Publish(new GlobalIntent.ThemeChanged(isDark: true));
```

---

## Store-to-Store 통신 예시

```csharp
// AuthStore — 로그인 후 알림
public class AuthStore : KnottyStore<AuthState, AuthIntent>
{
    protected override async Task HandleIntent(AuthIntent intent, CancellationToken ct)
    {
        case AuthIntent.Login login:
            var user = await _service.LoginAsync(login.Email, login.Password, ct);
            State = State with { User = user, IsLoggedIn = true };

            // 다른 Store들에 알림
            KnottyBus.Publish(new GlobalIntent.UserLoggedIn(user.Id));
            break;
    }
}

// CartStore — 로그인 후 장바구니 로드
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
// 구독 — IDisposable 반환. Dispose() 시 자동 해제.
IDisposable token = KnottyBus.Subscribe<TIntent>(object recipient, Action<TIntent> handler);

// Broadcast — record 상속 구조 탐색 (부모 타입 구독자에게도 전달)
KnottyBus.Publish<TIntent>(TIntent intent);

// [Obsolete] Send → Publish로 교체됨
// KnottyBus.Send(intent);
```

**WeakReference 기반** — `recipient`가 GC에 수거되면 구독이 자동 해제됨.
단, 명시적 `Dispose()`를 권장 (GC 타이밍 의존 지양).

---

## 주의사항

| 항목 | 내용 |
|------|------|
| 자동 구독 없음 | Store 생성만으로 Bus 수신 안 됨. `Subscribe` 명시 필요. |
| 같은 TIntent 복수 Store | 모두 반응함. 의도 확인 필요. |
| Store 간 직접 참조 | `_otherStore.Dispatch(...)` 금지 — Bus 사용 |
| 구독 해제 | `Dispose()` 또는 token `.Dispose()` 필수 |
