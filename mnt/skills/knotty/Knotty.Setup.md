# Knotty — DI 등록 & 플랫폼 Bootstrapping

## 설치

```bash
dotnet add package Knotty
```

## DI Extension (net6.0+)

```csharp
using Knotty;

// 기본 — 인수 없는 생성자
services.AddKnottyStore<CounterStore>();

// 팩토리 — 의존성 주입이 필요한 경우
services.AddKnottyStore<SearchStore>(sp =>
    new SearchStore(sp.GetRequiredService<ISearchService>()));
```

> `AddKnottyStore`는 Singleton으로 등록함. 대부분의 Store는 앱 생명주기와 동일하게 유지.

---

## 플랫폼별 Bootstrap

### WPF

```csharp
// App.xaml.cs
public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var services = new ServiceCollection();
        services.AddKnottyStore<CounterStore>();
        // 의존성 있는 경우:
        // services.AddSingleton<IMyService, MyService>();
        // services.AddKnottyStore<SearchStore>(sp => new SearchStore(sp.GetRequiredService<IMyService>()));
        Services = services.BuildServiceProvider();
    }
}
```

```csharp
// MainWindow.xaml.cs — 직접 생성 (DI 없이)
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new CounterStore();
    }
}

// MainWindow.xaml.cs — DI에서 주입
public partial class MainWindow : Window
{
    public MainWindow(CounterStore store)
    {
        InitializeComponent();
        DataContext = store;
    }
}
```

```xml
<!-- App.xaml — MainWindow를 DI로 생성하는 경우 -->
<Application StartupUri="">  <!-- StartupUri 제거 -->
```
```csharp
// App.xaml.cs
var mainWindow = Services.GetRequiredService<MainWindow>();
mainWindow.Show();
```

---

### MAUI

```csharp
// MauiProgram.cs
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // Store 등록
        builder.Services.AddKnottyStore<CounterStore>();

        // Page도 함께 등록 (BindingContext 주입 방식)
        builder.Services.AddTransient<CounterPage>();

        return builder.Build();
    }
}
```

```csharp
// CounterPage.xaml.cs — 생성자 주입
public partial class CounterPage : ContentPage
{
    public CounterPage(CounterStore store)
    {
        InitializeComponent();
        BindingContext = store;
    }
}
```

> MAUI에서는 `DataContext` 대신 `BindingContext` 사용.

---

### Avalonia

```csharp
// App.axaml.cs
public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        services.AddKnottyStore<CounterStore>();
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<CounterStore>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
```

> Avalonia의 `ICommand`는 `System.Windows.Input.ICommand`와 호환됨. Knotty Command는 그대로 동작.

---

## DI 없이 직접 생성 (소규모 앱)

```csharp
// WPF: code-behind에서 직접
var store = new CounterStore();
DataContext = store;

// MAUI: code-behind에서 직접
var store = new CounterStore();
BindingContext = store;
```

---

## Store Dispose

Store는 `IDisposable`을 구현. 앱 종료 또는 Window 닫힐 때 해제:

```csharp
// WPF
Closed += (s, e) => (DataContext as IDisposable)?.Dispose();

// MAUI — Page 소멸 시
protected override void OnDisappearing()
{
    base.OnDisappearing();
    (BindingContext as IDisposable)?.Dispose();
}
```

DI Singleton으로 등록한 경우 `ServiceProvider.Dispose()` 시 자동 해제됨 (net6+ `IAsyncDisposable` 지원 시).
