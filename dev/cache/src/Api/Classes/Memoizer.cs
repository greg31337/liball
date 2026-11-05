using System;

namespace cache;

/// <summary>
/// Memoizes a single-argument function by storing results in a supplied <see cref="ICacheStore"/>.
/// </summary>
/// <remarks>
/// The argument itself is the cache key. Existing cache entries take precedence over function evaluation, so sharing the cache with unrelated writes requires compatible keys and values.
/// </remarks>
public class Memoizer : IMemoizer
{
    internal ICacheStore _cache;
    internal Func<object, object?> _function;

    /// <summary>Creates a memoizer backed by an existing cache.</summary>
    /// <param name="cache">The cache used to read and store function results.</param>
    /// <param name="function">The synchronous function evaluated on a cache miss.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>cache</c> or <c>function</c> is null.</exception>
    public Memoizer(ICacheStore cache, Func<object, object?> function)
    {
        if (cache == null)
        {
            throw new ArgumentNullException("cache");
        }

        if (function == null)
        {
            throw new ArgumentNullException("function");
        }

        _cache = cache;
        _function = function;
    }

    /// <summary>Returns a cached result or evaluates and stores the function result.</summary>
    /// <remarks>Function and cache callback exceptions propagate to the caller. The memoizer does not provide single-flight coordination; use an appropriately configured cache loader when that behavior is required.</remarks>
    /// <param name="argument">The non-null function argument and cache key.</param>
    /// <returns>The cached or newly evaluated function result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>argument</c> is null.</exception>
    public object? Invoke(object argument)
    {
        if (argument == null)
        {
            throw new ArgumentNullException("argument");
        }

        CacheGetResult cached = _cache.Read(argument);
        if (cached.Found)
        {
            return cached.Value;
        }

        object? value = _function(argument);
        _cache.Write(argument, value);
        return value;
    }
}
