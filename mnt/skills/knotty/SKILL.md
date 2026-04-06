---
name: knotty
description: >
  C# MVI state management framework. Use when working with KnottyStore,
  KnottyBus, IEffect, IntentHandlingStrategy, or "using Knotty;".
triggers:
  - KnottyStore
  - KnottyBus
  - IEffect
  - IntentHandlingStrategy
  - Knotty MVI
  - "using Knotty;"
---

# Knotty Framework — Router

## Decision Tree

```
무엇을 만들고 있나?
│
├─ Store 신규 작성                    → Knotty.md
├─ DI 등록 / 플랫폼 bootstrapping    → Knotty.Setup.md
├─ View에 Store 바인딩               → Knotty.Binding.md
├─ XAML Command 생성 / 바인딩        → Knotty.Command.md
├─ 네비게이션 / Toast / Dialog       → Knotty.Effect.md
├─ 검색 debounce / 취소 / 동시 처리  → Knotty.IntentHandling.md
├─ 에러 처리 / HasErrors / 롤백      → Knotty.ErrorHandling.md
├─ Store 간 통신 (Bus)               → Knotty.Bus.md
└─ 디버깅 / 로그                     → Knotty.Debugger.md
```

## Golden Path — 기능 하나 추가할 때 순서

```
1. State record 정의
       public record MyState(int Count = 0);

2. Intent record 정의
       public abstract record MyIntent
       {
           public record Increment : MyIntent;
       }

3. Store 구현
       public partial class MyStore : KnottyStore<MyState, MyIntent>
       {
           public MyStore() : base(new MyState()) { }
           protected override async Task HandleIntent(MyIntent intent, CancellationToken ct) { ... }
       }

4. DI 등록  →  Knotty.Setup.md 참고 (플랫폼마다 다름)

5. View에서 DataContext 설정 + Effect 구독  →  Knotty.Binding.md 참고
```

## Quick Reference

| 주제 | 파일 |
|------|------|
| State / Intent / Store 시그니처 | [Knotty.md](./Knotty.md) |
| DI 등록 (MAUI / WPF / Avalonia) | [Knotty.Setup.md](./Knotty.Setup.md) |
| View 바인딩 패턴 | [Knotty.Binding.md](./Knotty.Binding.md) |
| Command 생성 / Source Generator | [Knotty.Command.md](./Knotty.Command.md) |
| Side Effect (navigation, toast) | [Knotty.Effect.md](./Knotty.Effect.md) |
| 동시성 전략 (debounce, cancel) | [Knotty.IntentHandling.md](./Knotty.IntentHandling.md) |
| 에러 처리 패턴 | [Knotty.ErrorHandling.md](./Knotty.ErrorHandling.md) |
| Store 간 통신 | [Knotty.Bus.md](./Knotty.Bus.md) |
| 디버깅 | [Knotty.Debugger.md](./Knotty.Debugger.md) |

## Rules

### Always
- `using Knotty;` 하나로 모든 타입 접근 가능. 다른 Knotty 하위 namespace 불필요.
- Source Generator 쓸 때는 Store 클래스에 `partial` 필수
- `HandleIntent` 내부 모든 `await`에 `ct` 전달
- View에서 Effect 구독 시 `Unloaded`/`OnNavigatedFrom`에서 반드시 `.Dispose()`

### Never
- `[ObservableProperty]`, `[RelayCommand]` — CommunityToolkit과 혼용 금지
- `State.SomeProperty = value` — 직접 mutation. 반드시 `State = State with { ... }`
- State record 안에 `bool ShowToast`, `string NavigateTo` 같은 일회성 플래그
- `State` 필드에 `IEffect` 인스턴스 저장
- `HandleIntent` 안에서 `_ = SomeAsync()` fire-and-forget (ct 전파 끊김)

## Common Mistakes (에이전트가 자주 저지르는 실수)

| 실수 | 올바른 방법 |
|------|------------|
| `[ObservableProperty] public int Count { ... }` | State record 필드로 관리, Store가 INPC 자동 구현 |
| `State.Count++` 직접 mutation | `State = State with { Count = State.Count + 1 }` |
| `await Task.Delay(1000)` — ct 없이 | `await Task.Delay(1000, ct)` |
| Store에 `partial` 없이 `[IntentCommand]` 사용 | `public partial class MyStore : KnottyStore<...>` |
| Effect를 `bool IsDialogOpen`으로 State에 넣음 | `EmitEffect(new MyEffect.ShowDialog(...))` |
| `KnottyBus.Subscribe` 없이 Bus 메시지 수신 기대 | 생성자에서 `SubscribeToBus()` 명시적 호출 |
