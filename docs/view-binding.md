---
title: View Binding
nav_order: 5
---

# View Binding

## 핵심 규칙

Store 자체가 `INotifyPropertyChanged`를 구현하므로 **ViewModel wrapper가 필요 없다**.
`DataContext`(WPF/Avalonia) 또는 `BindingContext`(MAUI)에 Store를 직접 할당.

---

## XAML 바인딩 경로

```xml
<!-- State 하위 프로퍼티 — "State." 접두어 필수 -->
<TextBlock Text="{Binding State.Count}" />
<TextBlock Text="{Binding State.Message}" />
<ListBox ItemsSource="{Binding State.Items}" />

<!-- Store 직접 프로퍼티 — "State." 없이 -->
<ProgressBar Visibility="{Binding IsLoading, Converter={StaticResource BoolToVis}}" />
<Button IsEnabled="{Binding IsLoading, Converter={StaticResource InverseBool}}" />

<!-- Command -->
<Button Command="{Binding IncrementCommand}" Content="+" />
<Button Command="{Binding IncrementByCommand}" CommandParameter="5" Content="+5" />

<!-- AsyncCommand의 IsExecuting -->
<Button Command="{Binding ResetCommand}"
        IsEnabled="{Binding ResetCommand.IsExecuting, Converter={StaticResource InverseBool}}" />
```

> `{Binding Count}` — State 없이 직접 바인딩하면 동작 안 함. `State.Count`가 변경되어도 `Count`로는 알림이 오지 않음.

---

## Effect 구독

Effect는 Store에서 방출하는 일회성 이벤트 (네비게이션, 토스트 등).
View가 `store.Effects` Observable을 구독하고, View 소멸 시 반드시 Dispose.

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
                await DisplayAlert("알림", toast.Message, "확인"));
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

## DataContext가 외부에서 주입되는 경우 (DI / Prism)

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

## IsLoading 활용 패턴

```xml
<!-- WPF: 버튼 비활성화 -->
<Button Command="{Binding SaveCommand}"
        IsEnabled="{Binding IsLoading, Converter={StaticResource InverseBool}}"
        Content="저장" />

<!-- WPF: 로딩 오버레이 -->
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

## 체크리스트

- [ ] State 바인딩 경로에 `State.` 붙임
- [ ] Effect 구독 후 View 소멸 시 `.Dispose()` 호출
- [ ] WPF/Avalonia는 `DataContext`, MAUI는 `BindingContext`
- [ ] ViewModel class 별도 작성 안 함 (Store 직접 사용)
