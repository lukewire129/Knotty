---
title: Platform Setup
nav_order: 4
---

# Platform Setup

## 설치

```bash
dotnet add package Knotty
```

`net6.0+`에서는 `AddKnottyStore<T>()` DI extension 자동 포함.
`netstandard2.0`에서는 수동으로 Store 인스턴스 생성.

---

## WPF

### DI 사용 (권장)

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
        // 의존성 주입 예시
        // services.AddSingleton<IApiService, ApiService>();
        // services.AddKnottyStore<SearchStore>(sp =>
        //     new SearchStore(sp.GetRequiredService<IApiService>()));

        Services = services.BuildServiceProvider();

        var window = new MainWindow(Services.GetRequiredService<CounterStore>());
        window.Show();
    }
}
```

```xml
<!-- App.xaml — StartupUri 제거 -->
<Application x:Class="MyApp.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources />
</Application>
```

```csharp
// MainWindow.xaml.cs
public partial class MainWindow : Window
{
    public MainWindow(CounterStore store)
    {
        InitializeComponent();
        DataContext = store;
    }
}
```

### DI 없이 (소규모 앱)

```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new CounterStore();
    }
}
```

### Dispose

```csharp
public partial class MainWindow : Window
{
    protected override void OnClosed(EventArgs e)
    {
        (DataContext as IDisposable)?.Dispose();
        base.OnClosed(e);
    }
}
```

---

## MAUI

```csharp
// MauiProgram.cs
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // Store Singleton 등록
        builder.Services.AddKnottyStore<CounterStore>();

        // Page를 Transient으로 등록 (생성자 주입 활성화)
        builder.Services.AddTransient<CounterPage>();

        return builder.Build();
    }
}
```

```csharp
// CounterPage.xaml.cs
// BindingContext = DataContext (WPF) 에 해당하는 MAUI 용어
public partial class CounterPage : ContentPage
{
    public CounterPage(CounterStore store)
    {
        InitializeComponent();
        BindingContext = store;
    }
}
```

```xml
<!-- CounterPage.xaml -->
<Label Text="{Binding State.Count}" />
<Button Command="{Binding IncrementCommand}" Text="+" />
<ActivityIndicator IsRunning="{Binding IsLoading}" />
```

### Shell 네비게이션과 함께

```csharp
// AppShell.xaml.cs
Routing.RegisterRoute(nameof(CounterPage), typeof(CounterPage));

// 다른 Page에서 이동
await Shell.Current.GoToAsync(nameof(CounterPage));
```

### Dispose (MAUI)

```csharp
protected override void OnDisappearing()
{
    base.OnDisappearing();
    (BindingContext as IDisposable)?.Dispose();
}
```

---

## Avalonia

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

```xml
<!-- MainWindow.axaml -->
<TextBlock Text="{Binding State.Count}" />
<Button Command="{Binding IncrementCommand}" Content="+" />
<ProgressBar IsIndeterminate="{Binding IsLoading}" />
```

> Avalonia의 `ICommand`는 `System.Windows.Input.ICommand`와 동일 인터페이스. Knotty Command 그대로 동작.

---

## 플랫폼별 차이 요약

| 항목 | WPF | MAUI | Avalonia |
|------|-----|------|----------|
| DI 진입점 | `App.xaml.cs OnStartup` | `MauiProgram.cs` | `App.axaml.cs OnFrameworkInitializationCompleted` |
| DataContext 프로퍼티 이름 | `DataContext` | `BindingContext` | `DataContext` |
| 바인딩 문법 | `{Binding State.Count}` | `{Binding State.Count}` | `{Binding State.Count}` |
| Page 등록 | 불필요 | `AddTransient<TPage>()` | 불필요 |
| Dispose 타이밍 | `OnClosed` | `OnDisappearing` | `Closed` 이벤트 |
