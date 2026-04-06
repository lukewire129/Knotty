using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Knotty;

public abstract partial class KnottyStore<TState, TIntent>
    : INotifyPropertyChanged, INotifyPropertyChanging, INotifyDataErrorInfo, IDisposable
    where TState : class
{
    private TState _state;
    private bool _isLoading;
    private bool _disposed;
    private IDisposable? _busToken;
    private readonly Dictionary<string, List<string>> _errors = new ();

    public TState State
    {
        get => _state;
        protected set
        {
            if (_state == value)
                return;
            OnPropertyChanging ();
            _state = value;
            OnPropertyChanged ();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set { _isLoading = value; OnPropertyChanged (); }
    }

    protected KnottyStore(TState initialState)
    {
        State = initialState ?? throw new ArgumentNullException (nameof (initialState));
        // Bus 구독은 opt-in: SubscribeToBus() 호출 시 활성화
    }

    /// <summary>
    /// KnottyBus에서 이 Store가 broadcast intent를 수신하도록 등록합니다.
    /// 같은 TIntent를 쓰는 Store가 여럿일 때 의도치 않은 cross-dispatch를 방지하기 위해 opt-in입니다.
    /// </summary>
    protected IDisposable SubscribeToBus()
    {
        _busToken = KnottyBus.Subscribe<TIntent> (this, Dispatch);
        return _busToken;
    }

    public void Dispatch(TIntent intent) => _ = DispatchInternalAsync (intent);

    private async Task DispatchInternalAsync(TIntent intent)
    {
        try
        {
            await DispatchAsync (intent);
        }
        catch (Exception ex)
        {
            // ExecuteIntent 내부에서 잡힌 예외는 여기까지 오지 않음.
            // 여기 오는 것: GetStrategy, switch 라우팅 등 프레임워크 레벨 예외.
            OnDispatchError (intent, ex);
        }
    }

    protected virtual void OnDispatchError(TIntent intent, Exception ex)
    {
        Debug.WriteLine ($"[Knotty] Unhandled error dispatching {intent?.GetType ().Name}: {ex.Message}");
    }

    public async Task DispatchAsync(TIntent intent)
    {
        var strategy = GetStrategy (intent);

        switch (strategy)
        {
            case IntentHandlingStrategy.Block:
                if (IsLoading)
                    return;
                await ExecuteIntent (intent);
                break;

            case IntentHandlingStrategy.Queue:
                _intentQueue.Enqueue (intent);
                if (!IsLoading)
                    await ProcessQueue ();
                break;

            case IntentHandlingStrategy.Debounce:
                await ProcessDebounced (intent);
                break;

            case IntentHandlingStrategy.CancelPrevious:
                if (_currentCts != null)
                {
                    _currentCts.Cancel ();
                    _currentCts.Dispose ();
                }

                // 새로운 CancellationTokenSource 생성
                _currentCts = new CancellationTokenSource ();

                try
                {
                    await ExecuteIntent (intent, _currentCts.Token);
                }
                finally
                {
                    // 완료되면 정리
                    if (_currentCts != null)
                    {
                        _currentCts.Dispose ();
                        _currentCts = null;
                    }
                }
                break;

            case IntentHandlingStrategy.Parallel:
                _ = ExecuteIntentParallel (intent);  // IsLoading 건드리지 않는 별도 경로
                break;
        }
    }

    protected abstract Task HandleIntent(TIntent intent, CancellationToken ct = default);
    protected virtual void OnHandleError(Exception ex) { }

    // --- 에러 처리 및 변경 알림 로직은 동일 ---
    #region INotifyDataErrorInfo & PropertyChange 구현
    public bool HasErrors => _errors.Any ();
    public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
    public IEnumerable GetErrors(string propertyName)
    {
        // .NET Standard 2.0에서는 안전하게 TryGetValue를 씁니다.
        if (_errors.TryGetValue (propertyName ?? string.Empty, out var errors))
        {
            return errors;
        }
        return null;
    }

    protected void AddError(string propertyName, string error)
    {
        if (!_errors.ContainsKey (propertyName))
            _errors[propertyName] = new List<string> ();
        _errors[propertyName].Add (error);
        ErrorsChanged?.Invoke (this, new DataErrorsChangedEventArgs (propertyName));
    }

    protected void ClearAllErrors()
    {
        var propertyNames = _errors.Keys.ToList ();
        _errors.Clear ();
        foreach (var name in propertyNames)
            ErrorsChanged?.Invoke (this, new DataErrorsChangedEventArgs (name));
    }

    public event PropertyChangedEventHandler PropertyChanged;
    public event PropertyChangingEventHandler PropertyChanging;
    protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (name));
    protected void OnPropertyChanging([CallerMemberName] string name = null) => PropertyChanging?.Invoke (this, new PropertyChangingEventArgs (name));
    #endregion

    public virtual void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _busToken?.Dispose ();
        _effectSubject.OnCompleted ();   // 구독자에게 Store 종료 알림
        _currentCts?.Cancel ();          // 진행 중인 CancelPrevious 작업 취소
        _currentCts?.Dispose ();
        _currentCts = null;
        _debounceCts?.Cancel ();         // 진행 중인 Debounce 타이머 취소
        _debounceCts?.Dispose ();
        _debounceCts = null;
        _intentQueue.Clear ();
    }
}