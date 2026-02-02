using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Knotty.Core;

public abstract partial class KnottyStore<TState, TIntent>
    : INotifyPropertyChanged, INotifyPropertyChanging, INotifyDataErrorInfo, IDisposable
    where TState : class
{
    private TState _state;
    private bool _isLoading; // 이름 변경: IsProcessing -> IsLoading
    private readonly IDisposable _busToken;
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

    // UI에서 ProgressBar나 비활성화 바인딩에 쓰기 딱 좋은 이름
    public bool IsLoading
    {
        get => _isLoading;
        private set { _isLoading = value; OnPropertyChanged (); }
    }

    protected KnottyStore(TState initialState)
    {
        State = initialState ?? throw new ArgumentNullException (nameof (initialState));
        _busToken = KnottyBus.Subscribe<TIntent> (this, Dispatch);
    }

    public void Dispatch(TIntent intent) => _ = DispatchAsync (intent);

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
                _ = ExecuteIntent (intent);  // Fire and forget
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

    public virtual void Dispose() => _busToken?.Dispose ();
}