using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;

namespace Solitons.Caching;

public interface IInMemoryCache
{
    public static IInMemoryCache Create() => new InMemoryCache();

    [DebuggerStepThrough]
    public sealed T GetOrAdd<T>(object key, Func<T> factory) where T : class 
        => GetOrAdd(key, factory, Observable.Empty<Unit>());

    T GetOrAdd<T>(object key, Func<T> valueFactory, IObservable<Unit> invalidation) where T : class;
}


public interface IInMemoryCacheProxy : IInMemoryCache
{
    private new static IInMemoryCache Create() => new InMemoryCache();

    protected IInMemoryCache InternalCache { get; }

}

sealed class InMemoryCache : IInMemoryCache
{
    private readonly ConcurrentDictionary<object, object> _cache = new();

    public T GetOrAdd<T>(
        object key, 
        Func<T> valueFactory, 
        IObservable<Unit> invalidation) where T : class
    {
        invalidation.Subscribe(_ => _cache.TryRemove(key, out var _));
        var result = _cache.GetOrAdd(key, valueFactory) as T;
        if (result == null)
        {
            result = valueFactory();
            _cache.TryAdd(key, result);
            return result;
        }
        return result;
    }
}