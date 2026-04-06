# Platform Setup

## Installation

```bash
dotnet add package Knotty
```

`net6.0+` includes the `AddKnottyStore<T>()` DI extension automatically.
`netstandard2.0` requires manual Store instantiation.

---

## WPF

### With DI (recommended)

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
        // With dependencies:
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
<!-- App.xaml — remove StartupUri -->
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

### Without DI (small apps)

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

        // Register Store as Singleton
        builder.Services.AddKnottyStore<CounterStore>();

        // Register Page as Transient (enables constructor injection)
        builder.Services.AddTransient<CounterPage>();

        return builder.Build();
    }
}
```

```csharp
// CounterPage.xaml.cs
// BindingContext = MAUI's equivalent of WPF's DataContext
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

### With Shell Navigation

```csharp
// AppShell.xaml.cs
Routing.RegisterRoute(nameof(CounterPage), typeof(CounterPage));

// Navigate from another page
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

> Avalonia's `ICommand` is the same interface as `System.Windows.Input.ICommand`. Knotty Commands work as-is.

---

## Platform Differences

| | WPF | MAUI | Avalonia |
|---|-----|------|----------|
| DI entry point | `App.xaml.cs OnStartup` | `MauiProgram.cs` | `App.axaml.cs OnFrameworkInitializationCompleted` |
| Binding context property | `DataContext` | `BindingContext` | `DataContext` |
| Binding syntax | `{Binding State.Count}` | `{Binding State.Count}` | `{Binding State.Count}` |
| Page registration | Not needed | `AddTransient<TPage>()` | Not needed |
| Dispose timing | `OnClosed` | `OnDisappearing` | `Closed` event |
