# Knotty

**Knotty**는 .NET Standard 2.0 이상을 지원하는 **AI 우선주의 MVI(Model-View-Intent)** 프레임워크입니다.

**MAUI, WPF, Avalonia**와 같은 현대적인 UI 프레임워크를 위해 설계되었으며, 기존 MVVM의 복잡함을 예측 가능하고 불변성이 보장되는 AI 친화적 아키텍처로 대체합니다.

### 🚀 왜 Knotty인가요?

- **AI 최적화** : 명시적인 State(상태)와 Intent(의도) 구조를 사용하여 GitHub Copilot, Cursor와 같은 AI 에이전트가 부수 효과(Side effects) 없이 코드를 정확하게 생성하고 유지보수할 수 있습니다.

- **예측 가능한 상태** : 불변 record 타입을 사용합니다. 어떤 프로퍼티 설정자가 버그를 일으켰는지 더 이상 추적할 필요가 없습니다.

- **보일러플레이트 제거** : IsLoading 자동 관리, 강력한 예외 처리, Command용 Source Generator가 내장되어 있어 반복적인 코드를 줄여줍니다.

- **가볍고 강력함** : .NET Standard 2.0을 타겟팅하여 거의 모든 .NET 환경(MAUI, WPF, WinUI 등)에서 즉시 사용할 수 있습니다.

### 📦 설치 방법

```bash
dotnet add package Knotty
```

### 🛠 핵심 개념

**1. State (상태 - 단일 진실 공급원)**

UI 상태를 하나의 불변 `record`로 정의합니다.

```csharp
public record TodoState(List<Todo> Items, string Filter = "");
```

**2. Intent (의도 - 사용자 행동)**

사용자가 수행할 수 있는 행동을 중첩 record를 통해 명확히 정의합니다.

```csharp
public abstract record TodoIntent
{
    public record Add(string Text) : TodoIntent;
    public record Toggle(Guid Id) : TodoIntent;
}
```

**3. Store (스토어 - 비즈니스 로직)**

모든 로직을 한곳에서 관리합니다. 비동기 작업 중에는 `IsLoading`이 자동으로 `true`가 됩니다.

```csharp
using Knotty.Core;

public class TodoStore : KnottyStore<TodoState, TodoIntent>
{
    protected override async Task HandleIntent(TodoIntent intent, CancellationToken ct)
    {
        switch (intent)
        {
            case TodoIntent.Add add:
                var newItem = await _api.CreateAsync(add.Text, ct);
                State = State with { Items = State.Items.Append(newItem).ToList() };
                break;
        }
    }
}
```

### ✨ 주요 기능

| 기능 | 설명 |
|------|------|
| **자동 로딩 관리** | `HandleIntent` 실행 중 `IsLoading`이 자동으로 토글됩니다 |
| **KnottyBus** | Store 간 통신을 위한 내장 이벤트 버스 |
| **에러 핸들링** | `INotifyDataErrorInfo` 구현. `HandleIntent` 내 예외가 캡처됩니다 |
| **Command Generator** | `[IntentCommand]` 어트리뷰트로 `ICommand` 프로퍼티 자동 생성 |
| **Intent 처리 전략** | Block, Queue, Debounce, CancelPrevious, Parallel |
| **Effects** | State를 오염시키지 않는 일회성 부수 효과 (네비게이션, 토스트 등) |

### 🔧 Command Generator (Source Generator)

```csharp
using Knotty.Core.Attributes;

public partial class CounterStore : KnottyStore<CounterState, CounterIntent>
{
    [IntentCommand]
    private readonly CounterIntent.Increment _increment = new();

    [AsyncIntentCommand(CanExecute = nameof(CanReset))]
    private readonly CounterIntent.Reset _reset = new();

    // 파라미터 사용 (XAML CommandParameter)
    [IntentCommand]
    private CounterIntent.IncrementBy CreateIncrementBy(string value) => new(int.Parse(value));

    private bool CanReset() => !IsLoading;
}
```

생성되는 코드:
```csharp
public ICommand IncrementCommand => ...;
public IAsyncCommand ResetCommand => ...;
public ICommand IncrementByCommand => ...;
```

### 📚 문서

자세한 문서는 skill 파일들을 참조하세요:

| 주제 | 파일 |
|------|------|
| 기본 사용법 | [mnt/skills/knotty/Knotty.md](mnt/skills/knotty/Knotty.md) |
| KnottyBus | [mnt/skills/knotty/Knotty.Bus.md](mnt/skills/knotty/Knotty.Bus.md) |
| Debugger | [mnt/skills/knotty/Knotty.Debugger.md](mnt/skills/knotty/Knotty.Debugger.md) |
| Command | [mnt/skills/knotty/Knotty.Command.md](mnt/skills/knotty/Knotty.Command.md) |
| Effect | [mnt/skills/knotty/Knotty.Effect.md](mnt/skills/knotty/Knotty.Effect.md) |
| Intent 처리 | [mnt/skills/knotty/Knotty.IntentHandling.md](mnt/skills/knotty/Knotty.IntentHandling.md) |

### 💡 .NET Standard 2.0 / Framework 사용자를 위한 팁

Knotty는 C#의 `record`와 `with` 구문을 사용할 때 가장 강력합니다. .NET Framework 4.7.2 이상이나 .NET Standard 2.0과 같은 구형 환경을 사용 중이라면, 아래 두 단계를 통해 최신 문법을 활성화할 수 있습니다.

**1단계:** 프로젝트 파일(`.csproj`) 수정 - C# 언어 버전을 **9.0** 이상으로 설정합니다.

```xml
<PropertyGroup>
  <LangVersion>9.0</LangVersion>
</PropertyGroup>
```

**2단계: 호환성 코드(Polyfill) 추가** 프로젝트 아무 곳에나(예: `Compatibility.cs`) 아래 코드를 복사해서 넣어주세요. 이 코드는 구형 환경에서도 컴파일러가 `record` 기능을 인식할 수 있게 돕는 역할을 합니다.

```csharp
namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit { }
}
```

### 📄 라이선스

MIT License. 자유롭게 사용하고 기여해 주세요!
