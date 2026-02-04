using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Knotty.Core.Commands;

/// <summary>
/// 비동기 Command를 위한 인터페이스입니다.
/// </summary>
public interface IAsyncCommand : ICommand, INotifyPropertyChanged
{
    Task ExecuteAsync(object? parameter);
    bool IsExecuting { get; }
}

/// <summary>
/// Intent를 Dispatch하는 기본 Command입니다.
/// </summary>
/// <typeparam name="TIntent">Intent 타입</typeparam>
public class IntentCommand<TIntent> : ICommand
{
    private readonly Action<TIntent> _dispatch;
    private readonly TIntent _intent;
    private readonly Func<bool>? _canExecute;

    public IntentCommand(Action<TIntent> dispatch, TIntent intent, Func<bool>? canExecute = null)
    {
        _dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
        _intent = intent;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _dispatch(_intent);

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// 파라미터를 받아서 Intent로 변환하는 Command입니다.
/// </summary>
/// <typeparam name="TIntent">Intent 타입</typeparam>
/// <typeparam name="TParameter">CommandParameter 타입</typeparam>
public class IntentCommand<TIntent, TParameter> : ICommand
{
    private readonly Action<TIntent> _dispatch;
    private readonly Func<TParameter, TIntent> _intentFactory;
    private readonly Func<TParameter, bool>? _canExecute;

    public IntentCommand(Action<TIntent> dispatch, Func<TParameter, TIntent> intentFactory, Func<TParameter, bool>? canExecute = null)
    {
        _dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
        _intentFactory = intentFactory ?? throw new ArgumentNullException(nameof(intentFactory));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        if (_canExecute == null) return true;
        if (parameter is TParameter p) return _canExecute(p);
        return true;
    }

    public void Execute(object? parameter)
    {
        if (parameter is TParameter p)
        {
            var intent = _intentFactory(p);
            _dispatch(intent);
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// Intent를 비동기로 Dispatch하고 완료를 기다리는 Command입니다.
/// 실행 중에는 CanExecute가 false가 됩니다.
/// </summary>
/// <typeparam name="TIntent">Intent 타입</typeparam>
public class AsyncIntentCommand<TIntent> : IAsyncCommand
{
    private readonly Func<TIntent, Task> _dispatchAsync;
    private readonly TIntent _intent;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;

    public bool IsExecuting
    {
        get => _isExecuting;
        private set
        {
            if (_isExecuting == value) return;
            _isExecuting = value;
            OnPropertyChanged();
            RaiseCanExecuteChanged();
        }
    }

    public AsyncIntentCommand(Func<TIntent, Task> dispatchAsync, TIntent intent, Func<bool>? canExecute = null)
    {
        _dispatchAsync = dispatchAsync ?? throw new ArgumentNullException(nameof(dispatchAsync));
        _intent = intent;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public bool CanExecute(object? parameter)
    {
        if (IsExecuting) return false;
        return _canExecute?.Invoke() ?? true;
    }

    public async void Execute(object? parameter) => await ExecuteAsync(parameter);

    public async Task ExecuteAsync(object? parameter)
    {
        if (!CanExecute(parameter)) return;

        try
        {
            IsExecuting = true;
            await _dispatchAsync(_intent);
        }
        finally
        {
            IsExecuting = false;
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// 파라미터를 받아서 Intent로 변환하고 비동기로 Dispatch하는 Command입니다.
/// </summary>
/// <typeparam name="TIntent">Intent 타입</typeparam>
/// <typeparam name="TParameter">CommandParameter 타입</typeparam>
public class AsyncIntentCommand<TIntent, TParameter> : IAsyncCommand
{
    private readonly Func<TIntent, Task> _dispatchAsync;
    private readonly Func<TParameter, TIntent> _intentFactory;
    private readonly Func<TParameter, bool>? _canExecute;
    private bool _isExecuting;

    public bool IsExecuting
    {
        get => _isExecuting;
        private set
        {
            if (_isExecuting == value) return;
            _isExecuting = value;
            OnPropertyChanged();
            RaiseCanExecuteChanged();
        }
    }

    public AsyncIntentCommand(Func<TIntent, Task> dispatchAsync, Func<TParameter, TIntent> intentFactory, Func<TParameter, bool>? canExecute = null)
    {
        _dispatchAsync = dispatchAsync ?? throw new ArgumentNullException(nameof(dispatchAsync));
        _intentFactory = intentFactory ?? throw new ArgumentNullException(nameof(intentFactory));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public bool CanExecute(object? parameter)
    {
        if (IsExecuting) return false;
        if (_canExecute == null) return true;
        if (parameter is TParameter p) return _canExecute(p);
        return true;
    }

    public async void Execute(object? parameter) => await ExecuteAsync(parameter);

    public async Task ExecuteAsync(object? parameter)
    {
        if (!CanExecute(parameter)) return;

        if (parameter is TParameter p)
        {
            try
            {
                IsExecuting = true;
                var intent = _intentFactory(p);
                await _dispatchAsync(intent);
            }
            finally
            {
                IsExecuting = false;
            }
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
