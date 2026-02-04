using System;

namespace Knotty.Core.Attributes;

/// <summary>
/// Intent 필드 또는 Intent 생성 메서드에 적용하면 동기 Command 프로퍼티를 자동 생성합니다.
/// </summary>
/// <example>
/// <code>
/// // 필드 기반 (파라미터 없음)
/// [IntentCommand]
/// private readonly CounterIntent.Increment _increment = new();
/// // 생성됨: public ICommand IncrementCommand => Command(_increment);
/// 
/// // 메서드 기반 (파라미터 있음)
/// [IntentCommand]
/// private CounterIntent.IncrementBy CreateIncrementBy(int value) => new(value);
/// // 생성됨: public ICommand IncrementByCommand => Command&lt;int&gt;(CreateIncrementBy);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class IntentCommandAttribute : Attribute
{
    /// <summary>
    /// 생성될 Command 프로퍼티 이름을 지정합니다.
    /// 지정하지 않으면 필드명/메서드명에서 자동 생성됩니다.
    /// </summary>
    public string? CommandName { get; set; }

    /// <summary>
    /// CanExecute 조건을 반환하는 메서드 또는 프로퍼티 이름입니다.
    /// </summary>
    public string? CanExecute { get; set; }
}

/// <summary>
/// Intent 필드 또는 Intent 생성 메서드에 적용하면 비동기 Command 프로퍼티를 자동 생성합니다.
/// 실행 중에는 자동으로 CanExecute가 false가 됩니다.
/// </summary>
/// <example>
/// <code>
/// // 필드 기반 (파라미터 없음)
/// [AsyncIntentCommand]
/// private readonly CounterIntent.Reset _reset = new();
/// // 생성됨: public IAsyncCommand ResetCommand => AsyncCommand(_reset);
/// 
/// // 메서드 기반 (파라미터 있음)
/// [AsyncIntentCommand]
/// private CounterIntent.LoadData CreateLoadData(string url) => new(url);
/// // 생성됨: public IAsyncCommand LoadDataCommand => AsyncCommand&lt;string&gt;(CreateLoadData);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class AsyncIntentCommandAttribute : Attribute
{
    /// <summary>
    /// 생성될 Command 프로퍼티 이름을 지정합니다.
    /// 지정하지 않으면 필드명/메서드명에서 자동 생성됩니다.
    /// </summary>
    public string? CommandName { get; set; }

    /// <summary>
    /// CanExecute 조건을 반환하는 메서드 또는 프로퍼티 이름입니다.
    /// </summary>
    public string? CanExecute { get; set; }
}
