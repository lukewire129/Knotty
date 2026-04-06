---
title: Intent Handling
nav_order: 7
---

# Intent Handling Strategies

동시에 여러 Intent가 들어올 때 어떻게 처리할지 Intent별로 설정.

## 전략 목록

```csharp
public enum IntentHandlingStrategy
{
    Block,          // 처리 중이면 무시 (기본값)
    Queue,          // 순서대로 처리
    Debounce,       // 일정 시간 후 마지막 것만 처리
    CancelPrevious, // 이전 작업 취소 후 새 것 처리
    Parallel        // 동시 처리 (IsLoading 건드리지 않음)
}
```

---

## Block (기본값)

처리 중인 Intent가 있으면 새 Intent 무시.

```
사용자: [클릭] [클릭] [클릭]
처리:   [ 1번 ]  (무시)  (무시)
```

**적합한 상황**: 폼 제출, 결제 버튼 — 중복 실행 방지

```csharp
// 기본값이므로 override 불필요
// 명시적으로 쓸 경우:
protected override IntentHandlingStrategy GetStrategy(MyIntent intent)
    => IntentHandlingStrategy.Block;
```

---

## Queue

모든 Intent를 큐에 넣어 순서대로 처리.

```
사용자: [클릭] [클릭] [클릭]
처리:   [ 1번 ] → [ 2번 ] → [ 3번 ]
```

**적합한 상황**: 메시지 전송, 순서 보장이 필요한 작업

```csharp
protected override IntentHandlingStrategy GetStrategy(MyIntent intent)
    => IntentHandlingStrategy.Queue;
```

---

## Debounce

연속 입력이 멈춘 후 일정 시간 대기, 마지막 Intent만 처리.

```
사용자: [a] [ab] [abc] [abcd] ... (멈춤)
처리:                              [abcd]
```

**적합한 상황**: 검색어 입력, 자동저장, 필터링

```csharp
protected override IntentHandlingStrategy GetStrategy(SearchIntent intent)
{
    return intent switch
    {
        SearchIntent.UpdateQuery => IntentHandlingStrategy.Debounce,
        _ => IntentHandlingStrategy.Block
    };
}

protected override TimeSpan GetDebounceDelay(SearchIntent intent)
{
    return intent switch
    {
        SearchIntent.UpdateQuery => TimeSpan.FromMilliseconds(400),
        _ => TimeSpan.FromMilliseconds(300) // 기본값
    };
}
```

---

## CancelPrevious

새 Intent가 오면 실행 중인 작업을 취소하고 새 것 시작.

```
사용자: [검색A] -------- [검색B]
처리:   [검색A... 취소] → [검색B]
```

**적합한 상황**: 검색 API 호출, 최신 결과만 필요한 작업

```csharp
protected override IntentHandlingStrategy GetStrategy(SearchIntent intent)
{
    return intent switch
    {
        SearchIntent.Search => IntentHandlingStrategy.CancelPrevious,
        _ => IntentHandlingStrategy.Block
    };
}
```

**HandleIntent에서 ct 반드시 사용**

```csharp
protected override async Task HandleIntent(SearchIntent intent, CancellationToken ct)
{
    case SearchIntent.Search search:
        var results = await _api.SearchAsync(search.Query, ct); // ct 전달 필수
        ct.ThrowIfCancellationRequested();                       // 취소 확인
        State = State with { Results = results };
        break;
}
```

**취소 시 State 복원 패턴**

```csharp
case SearchIntent.Reset:
    var prev = State;
    State = State with { Message = "초기화 중..." };
    try
    {
        await Task.Delay(3000, ct);
        State = State with { Count = 0, Message = "완료" };
    }
    catch (OperationCanceledException)
    {
        State = prev; // 취소 시 원래 State로 복원
        throw;        // 반드시 rethrow
    }
    break;
```

> `OperationCanceledException`을 잡고 rethrow하지 않으면 `IsLoading` 복원에 문제 생길 수 있음.

---

## Parallel

IsLoading을 건드리지 않고 모든 Intent를 동시에 처리.

```
사용자: [클릭] [클릭] [클릭]
처리:   [ 1번 ]
        [ 2번 ]  (동시 실행)
        [ 3번 ]
```

**적합한 상황**: 독립적인 백그라운드 작업, 로깅

```csharp
protected override IntentHandlingStrategy GetStrategy(LogIntent intent)
    => IntentHandlingStrategy.Parallel;
```

> Parallel은 `IsLoading`을 `true`로 바꾸지 않음. 같은 State를 동시에 수정하면 race condition 발생 가능.

---

## Intent별 혼합 전략

```csharp
protected override IntentHandlingStrategy GetStrategy(SearchIntent intent)
{
    return intent switch
    {
        SearchIntent.UpdateQuery => IntentHandlingStrategy.Debounce,
        SearchIntent.Search      => IntentHandlingStrategy.CancelPrevious,
        SearchIntent.ApplyFilter => IntentHandlingStrategy.Queue,
        SearchIntent.LogView     => IntentHandlingStrategy.Parallel,
        _                        => IntentHandlingStrategy.Block
    };
}
```

---

## 전략 선택 가이드

| 상황 | 전략 |
|------|------|
| 버튼 중복 클릭 방지 | `Block` |
| 순서 보장 필요 | `Queue` |
| 텍스트 입력 후 처리 | `Debounce` |
| 검색 API (항상 최신 결과) | `CancelPrevious` |
| 독립적 백그라운드 작업 | `Parallel` |
