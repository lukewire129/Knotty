# View Binding

## Core Rule

The Store itself implements `INotifyPropertyChanged` — **no ViewModel wrapper needed**.
Assign the Store directly to `DataContext` (WPF/Avalonia) or `BindingContext` (MAUI).

---

## XAML Binding Paths

```xml
<!-- State sub-properties — "State." prefix is required -->
<TextBlock Text="{Binding State.Count}" />
<TextBlock Text="{Binding State.Message}" />
<ListBox ItemsSource="{Binding State.Items}" />

<!-- Store direct properties — no "State." prefix -->
<ProgressBar Visibility="{Binding IsLoading, Converter={StaticResource BoolToVis}}" />
<Button IsEnabled="{Binding IsLoading, Converter={StaticResource InverseBool}}" />

<!-- Commands -->
<Button Command="{Binding IncrementCommand}" Content="+" />
<Button Command="{Binding IncrementByCommand}" CommandParameter="5" Content="+5" />

<!-- AsyncCommand's IsExecuting -->
<Button Command="{Binding ResetCommand}"
        IsEnabled="{Binding ResetCommand.IsExecuting, Converter={StaticResource InverseBool}}" />
```

> `{Binding Count}` — binding without `State.` prefix won't work. Changes to `State.Count` don't send notifications to `Count` directly.

---

## Effect Subscription

Effects are one-shot events emitted from the Store (navigation, toasts, etc.).
The View subscribes to `store.Effects` and **must** Dispose when the View is destroyed.

### WPF

```csharp
public partial class MainWindow : Window
{
    private IDisposable? _effects;

    public MainWindow()
    {
        InitializeComponent();
        var store = new CounterStore();
        DataContext = store;

        _effects = store.Effects.Subscribe<CounterEffect>(OnEffect);
        Unloaded += (_, _) => _effects?.Dispose();
    }

    private void OnEffect(CounterEffect effect)
    {
        switch (effect)
        {
            case CounterEffect.ShowToast toast:
                MessageBox.Show(toast.Message);
                break;
            case CounterEffect.NavigateTo nav:
                // NavigationService.Navigate(nav.Route);
                break;
        }
    }
}
```

### MAUI

```csharp
public partial class CounterPage : ContentPage
{
    private IDisposable? _effects;

    public CounterPage(CounterStore store)
    {
        InitializeComponent();
        BindingContext = store;
        _effects = store.Effects.Subscribe<CounterEffect>(OnEffect);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _effects?.Dispose();
    }

    private void OnEffect(CounterEffect effect)
    {
        if (effect is CounterEffect.ShowToast toast)
            MainThread.BeginInvokeOnMainThread(async () =>
                await DisplayAlert("Notice", toast.Message, "OK"));
    }
}
```

### Avalonia

```csharp
public partial class MainWindow : Window
{
    private IDisposable? _effects;

    public MainWindow(CounterStore store)
    {
        InitializeComponent();
        DataContext = store;
        _effects = store.Effects.Subscribe<CounterEffect>(OnEffect);
        Closed += (_, _) => _effects?.Dispose();
    }
}
```

---

## DataContext Injected Externally (DI / Prism)

```csharp
public partial class MainWindow : Window
{
    private IDisposable? _effects;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, e) =>
        {
            _effects?.Dispose();
            if (e.NewValue is IEffectSource source)
                _effects = source.Effects.Subscribe<MyEffect>(OnEffect);
        };
        Unloaded += (_, _) => _effects?.Dispose();
    }
}
```

---

## IsLoading Patterns

```xml
<!-- WPF: disable button -->
<Button Command="{Binding SaveCommand}"
        IsEnabled="{Binding IsLoading, Converter={StaticResource InverseBool}}"
        Content="Save" />

<!-- WPF: loading overlay -->
<Grid>
    <ContentPresenter Content="{Binding}" />
    <Border Background="#80000000"
            Visibility="{Binding IsLoading, Converter={StaticResource BoolToVis}}">
        <ProgressBar IsIndeterminate="True" Width="200" />
    </Border>
</Grid>

<!-- MAUI -->
<ActivityIndicator IsRunning="{Binding IsLoading}" IsVisible="{Binding IsLoading}" />
```

---

## Checklist

- [ ] State binding paths include `State.` prefix
- [ ] Effect subscriptions are `.Dispose()`'d when the View is destroyed
- [ ] WPF/Avalonia use `DataContext`, MAUI uses `BindingContext`
- [ ] No separate ViewModel class — use the Store directly
