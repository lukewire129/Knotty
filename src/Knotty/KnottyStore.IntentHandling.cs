#if NETSTANDARD2_0 || NET462
using Knotty.Extensions;
#endif
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Knotty;

public abstract partial class KnottyStore<TState, TIntent>
    : INotifyPropertyChanged, INotifyPropertyChanging, INotifyDataErrorInfo, IDisposable
    where TState : class
{
    private readonly Queue<TIntent> _intentQueue = new ();
    private CancellationTokenSource? _currentCts;
    private CancellationTokenSource? _debounceCts;

    /// <summary>Intent별 처리 전략을 반환합니다. 기본값은 Block입니다.</summary>
    protected virtual IntentHandlingStrategy GetStrategy(TIntent intent)
        => IntentHandlingStrategy.Block;

    /// <summary>Debounce 대기 시간을 반환합니다. 기본값은 300ms입니다.</summary>
    protected virtual TimeSpan GetDebounceDelay(TIntent intent)
        => TimeSpan.FromMilliseconds (300);

    private async Task ExecuteIntent(TIntent intent, CancellationToken ct = default)
    {
        try
        {
            IsLoading = true;
            ClearAllErrors ();
            await HandleIntent (intent, ct);
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine ($"[Knotty] Intent {intent?.GetType ().Name} cancelled");
        }
        catch (Exception ex)
        {
            // 에러 key를 Intent 타입 이름으로 기록해 어떤 Intent에서 발생했는지 추적 가능
            var errorKey = intent?.GetType ().Name ?? "Store";
            AddError (errorKey, ex.Message);
            OnHandleError (ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Parallel 전략 전용 실행 경로. IsLoading을 건드리지 않아
    /// 동시에 여러 Parallel intent가 실행되어도 IsLoading이 불안정해지지 않습니다.
    /// </summary>
    private async Task ExecuteIntentParallel(TIntent intent, CancellationToken ct = default)
    {
        try
        {
            ClearAllErrors ();
            await HandleIntent (intent, ct);
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine ($"[Knotty] Intent {intent?.GetType ().Name} cancelled");
        }
        catch (Exception ex)
        {
            var errorKey = intent?.GetType ().Name ?? "Store";
            AddError (errorKey, ex.Message);
            OnHandleError (ex);
        }
    }

    private async Task ProcessQueue()
    {
        while (_intentQueue.TryDequeue (out var intent))
        {
            await ExecuteIntent (intent);
        }
    }

    private async Task ProcessDebounced(TIntent intent)
    {
        // 이전 debounce 타이머 취소 — CancellationTokenSource 기반으로 DateTime 없이 정확하게 처리
        _debounceCts?.Cancel ();
        _debounceCts?.Dispose ();
        _debounceCts = new CancellationTokenSource ();
        var token = _debounceCts.Token;

        try
        {
            await Task.Delay (GetDebounceDelay (intent), token);
            // 여기 도달 = 이 intent가 마지막 (취소 안 됨)
            await ExecuteIntent (intent);
        }
        catch (OperationCanceledException)
        {
            // 새 intent가 들어와서 이전 대기가 취소됨. 정상 동작.
        }
    }
}