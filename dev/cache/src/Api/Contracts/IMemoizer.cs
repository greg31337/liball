namespace cache;

/// <summary>
/// Defines a single-argument memoized function.
/// </summary>
public interface IMemoizer
{
    /// <summary>Returns a cached result or evaluates and stores the function result.</summary>
    /// <param name="argument">The non-null function argument and cache key.</param>
    /// <returns>The cached or newly evaluated function result.</returns>
    object? Invoke(object argument);
}
