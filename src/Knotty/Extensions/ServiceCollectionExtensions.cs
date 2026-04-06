#if NET6_0_OR_GREATER
using System;
using Microsoft.Extensions.DependencyInjection;

namespace Knotty;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// KnottyStore를 Singleton으로 DI 컨테이너에 등록합니다.
    /// </summary>
    /// <example>
    /// <code>
    /// // MauiProgram.cs 또는 Program.cs
    /// services.AddKnottyStore&lt;CounterStore&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddKnottyStore<TStore>(
        this IServiceCollection services)
        where TStore : class, IDisposable
    {
        services.AddSingleton<TStore> ();
        return services;
    }

    /// <summary>
    /// KnottyStore를 팩토리를 통해 Singleton으로 DI 컨테이너에 등록합니다.
    /// Store 생성자에 추가 의존성이 필요할 때 사용합니다.
    /// </summary>
    /// <example>
    /// <code>
    /// services.AddKnottyStore&lt;CounterStore&gt;(sp =>
    ///     new CounterStore(sp.GetRequiredService&lt;IMyService&gt;()));
    /// </code>
    /// </example>
    public static IServiceCollection AddKnottyStore<TStore>(
        this IServiceCollection services,
        Func<IServiceProvider, TStore> factory)
        where TStore : class, IDisposable
    {
        services.AddSingleton (factory);
        return services;
    }
}
#endif
