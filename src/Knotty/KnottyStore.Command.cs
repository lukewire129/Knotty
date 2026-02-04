using System;
using System.Windows.Input;
using Knotty.Core.Commands;

namespace Knotty.Core;

/// <summary>
/// KnottyStore에 Command 생성 헬퍼 메서드를 추가합니다.
/// </summary>
public abstract partial class KnottyStore<TState, TIntent>
{
    /// <summary>
    /// 고정된 Intent를 Dispatch하는 Command를 생성합니다.
    /// </summary>
    /// <param name="intent">Dispatch할 Intent</param>
    /// <param name="canExecute">실행 가능 여부를 판단하는 함수 (선택)</param>
    /// <returns>ICommand 인스턴스</returns>
    protected ICommand Command(TIntent intent, Func<bool> canExecute = null)
        => new IntentCommand<TIntent>(Dispatch, intent, canExecute);

    /// <summary>
    /// CommandParameter를 Intent로 변환하여 Dispatch하는 Command를 생성합니다.
    /// </summary>
    /// <typeparam name="TParameter">CommandParameter 타입</typeparam>
    /// <param name="intentFactory">파라미터를 Intent로 변환하는 함수</param>
    /// <param name="canExecute">실행 가능 여부를 판단하는 함수 (선택)</param>
    /// <returns>ICommand 인스턴스</returns>
    protected ICommand Command<TParameter>(Func<TParameter, TIntent> intentFactory, Func<TParameter, bool> canExecute = null)
        => new IntentCommand<TIntent, TParameter>(Dispatch, intentFactory, canExecute);

    /// <summary>
    /// 고정된 Intent를 비동기로 Dispatch하는 Command를 생성합니다.
    /// 실행 중에는 자동으로 CanExecute가 false가 됩니다.
    /// </summary>
    /// <param name="intent">Dispatch할 Intent</param>
    /// <param name="canExecute">실행 가능 여부를 판단하는 함수 (선택)</param>
    /// <returns>IAsyncCommand 인스턴스</returns>
    protected IAsyncCommand AsyncCommand(TIntent intent, Func<bool> canExecute = null)
        => new AsyncIntentCommand<TIntent>(DispatchAsync, intent, canExecute);

    /// <summary>
    /// CommandParameter를 Intent로 변환하여 비동기로 Dispatch하는 Command를 생성합니다.
    /// 실행 중에는 자동으로 CanExecute가 false가 됩니다.
    /// </summary>
    /// <typeparam name="TParameter">CommandParameter 타입</typeparam>
    /// <param name="intentFactory">파라미터를 Intent로 변환하는 함수</param>
    /// <param name="canExecute">실행 가능 여부를 판단하는 함수 (선택)</param>
    /// <returns>IAsyncCommand 인스턴스</returns>
    protected IAsyncCommand AsyncCommand<TParameter>(Func<TParameter, TIntent> intentFactory, Func<TParameter, bool> canExecute = null)
        => new AsyncIntentCommand<TIntent, TParameter>(DispatchAsync, intentFactory, canExecute);
}
