# Knotty

**Knotty**는 .NET Standard 2.0 이상을 지원하는 **AI 우선주의 MVI(Model-View-Intent)** 프레임워크입니다.

**MAUI, WPF, Avalonia**와 같은 현대적인 UI 프레임워크를 위해 설계되었으며, 기존 MVVM의 복잡함을 예측 가능하고 불변성이 보장되는 AI 친화적 아키텍처로 대체합니다.

### 🚀 왜 Knotty인가요?

- **AI 최적화** : 명시적인 State(상태)와 Intent(의도) 구조를 사용하여 GitHub Copilot, Cursor와 같은 AI 에이전트가 부수 효과(Side effects) 없이 코드를 정확하게 생성하고 유지보수할 수 있습니다.

- **예측 가능한 상태** : 불변 `record` 타입을 사용합니다. 어떤 프로퍼티 설정자가 버그를 일으켰는지 더 이상 추적할 필요가 없습니다.

- **보일러플레이트 제거** : `IsLoading` 자동 관리와 강력한 예외 처리가 내장되어 있어 반복적인 코드를 줄여줍니다.

- **가볍고 강력함** : .NET Standard 2.0을 타겟팅하여 거의 모든 .NET 환경(MAUI, WPF, WinUI 등)에서 즉시 사용할 수 있습니다.

### 📦 설치 방법

```Bash
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
public abstract record TodoIntent {
public record Add(string Text) : TodoIntent;
public record Toggle(Guid Id) : TodoIntent;
}
```

**3. Store (스토어 - 비즈니스 로직)**

모든 로직을 한곳에서 관리합니다. 비동기 작업 중에는 `IsLoading`이 자동으로 `true`가 됩니다.

```csharp
using Knotty.Core;

public class TodoStore : KnottyStore<TodoState, TodoIntent> {
public TodoStore() : base(new TodoState(new())) { }

    protected override async Task HandleIntent(TodoIntent intent) {
        switch (intent) {
            case TodoIntent.Add add:
                // 비동기 작업 시 IsLoading이 자동으로 관리됨
                var newItem = await _api.CreateAsync(add.Text);
                State = State with { Items = State.Items.Append(newItem).ToList() };
                break;
        }
    }
}
```

### ✨ 주요 기능

- **자동 로딩 관리** : `HandleIntent`가 실행되는 동안 `IsLoading` 프로퍼티가 자동으로 On/Off 됩니다.

- **KnottyBus** : Store 간 통신을 위한 초경량 이벤트 버스가 내장되어 있습니다.

- **에러 핸들링** : `INotifyDataErrorInfo`를 자동 구현합니다. `HandleIntent` 내의 모든 예외는 안전하게 캡처됩니다.

- **LINQ 친화적** : 상태 전환 시 LINQ를 사용하여 간결하고 안전하게 데이터를 처리하도록 설계되었습니다.

### 💡 .NET Standard 2.0 사용 팁

Knotty는 C#의 `record`와 `with` 구문을 사용할 때 가장 강력합니다. .NET Framework 4.7.2 이상이나 .NET Standard 2.0과 같은 구형 환경을 사용 중이라면, 아래 두 단계를 통해 최신 문법을 활성화할 수 있습니다.

1단계: 프로젝트 파일(`.csproj`) 수정 C# 언어 버전을 **9.0** 이상으로 설정합니다.

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
