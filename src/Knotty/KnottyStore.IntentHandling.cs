#if NETSTANDARD2_0 || NET462
using Knotty.Core.Extensions;
#endif
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Knotty.Core;

public abstract partial class KnottyStore<TState, TIntent>
    : INotifyPropertyChanged, INotifyPropertyChanging, INotifyDataErrorInfo, IDisposable
    where TState : class
{
    private readonly Queue<TIntent> _intentQueue = new ();
    private CancellationTokenSource? _currentCts;
    private Dictionary<Type, DateTime> _lastIntentTime = new ();

    // Intent별로 전략 정의
    protected virtual IntentHandlingStrategy GetStrategy(TIntent intent)
        => IntentHandlingStrategy.Block;  // 기본값

    // Debounce 시간 (기본 300ms)
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
            // 취소됨 - 로그만 남기고 에러 처리 안 함
            Debug.WriteLine ($"[Knotty] Intent {intent.GetType ().Name} cancelled");
        }
        catch (Exception ex)
        {
            AddError ("Store", ex.Message);
            OnHandleError (ex);
        }
        finally
        {
            IsLoading = false;
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
        var intentType = intent.GetType ();
        _lastIntentTime[intentType] = DateTime.Now;

        var delay = GetDebounceDelay (intent);
        await Task.Delay (delay);

        // 아직도 이게 마지막 Intent인가?
        if (_lastIntentTime[intentType].Add (delay) <= DateTime.Now)
        {
            await ExecuteIntent (intent);
        }
    }
}