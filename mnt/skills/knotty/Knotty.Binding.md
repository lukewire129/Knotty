# Knotty — View Binding 패턴

## 핵심 원칙

- Store 자체가 `INotifyPropertyChanged`를 구현 → **ViewModel wrapper 불필요**
- `DataContext`(WPF/Avalonia) 또는 `BindingContext`(MAUI)에 Store 직접 할당
- State 프로퍼티는 `{Binding State.Count}` 형태로 접근 (중간에 `State.` 필요)
- `IsLoading`은 Store 직접 프로퍼티 → `{Binding IsLoading}`

---

## WPF

### DataContext 설정

```csharp
// code-behind
public MainWindow()
{
    InitializeComponent();
    DataContext = new CounterStore();
    // 또는 DI: DataContext = App.Services.GetRequiredService<CounterStore>();
}
```

### XAML 바인딩

```xml
<!-- State 프로퍼티: 반드시 State. 접두어 필요 -->
<TextBlock Text="{Binding State.Count}" />
<TextBlock Text="{Binding State.Message}" />

<!-- Store 직접 프로퍼티 -->
<ProgressBar Visibility="{Binding IsLoading, Converter={StaticResource BoolToVisibility}}" />
<Button IsEnabled="{Binding IsLoading, Converter={StaticResource InverseBool}}" />

<!-- Command 바인딩 -->
<Button Command="{Binding IncrementCommand}" Content="+" />
<Button Command="{Binding IncrementByCommand}" CommandParameter="5" Content="+5" />
```

### Effect 구독

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
        }
    }
}
```

---

## MAUI

### BindingContext 설정

```csharp
// ContentPage code-behind
public CounterPage(CounterStore store)
{
    InitializeComponent();
    BindingContext = store;
}
```

### XAML 바인딩

```xml
<!-- WPF와 동일하게 State. 접두어 필요 -->
<Label Text="{Binding State.Count}" />
<ActivityIndicator IsRunning="{Binding IsLoading}" />

<!-- Command -->
<Button Command="{Binding IncrementCommand}" Text="+" />
<Button Command="{Binding IncrementByCommand}" CommandParameter="5" Text="+5" />
```

### Effect 구독

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
            DisplayAlert("알림", toast.Message, "확인");
    }
}
```

---

## Avalonia

### DataContext 설정

```csharp
// Window 생성 시
new MainWindow { DataContext = store }

// code-behind에서
this.DataContext = store;
```

### AXAML 바인딩

```xml
<!-- Avalonia는 WPF와 동일한 바인딩 문법 -->
<TextBlock Text="{Binding State.Count}" />
<ProgressBar IsIndeterminate="{Binding IsLoading}" />
<Button Command="{Binding IncrementCommand}" Content="+" />
```

### Effect 구독

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

## 공통 주의사항

| 항목 | 내용 |
|------|------|
| 바인딩 경로 | `{Binding State.Count}` — `State.` 빠지면 바인딩 실패 (에러 없이 빈값) |
| ViewModel 필요 여부 | 불필요. Store가 INPC 직접 구현 |
| ICommand 구현 | 불필요. `Command()` / `AsyncCommand()` / Source Generator 사용 |
| Effect 구독 해제 | View 소멸 시 필수. 빼먹으면 View가 GC 안 됨 |
| DataContext vs BindingContext | WPF/Avalonia = DataContext, MAUI = BindingContext |

---

## DataContextChanged 패턴 (DI/Prism 환경)

DataContext가 외부에서 주입되는 경우:

```csharp
// WPF
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
```
