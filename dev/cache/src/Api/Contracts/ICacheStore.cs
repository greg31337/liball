namespace cache;

/// <summary>
/// Defines the minimal read and write operations needed by cache-backed components.
/// </summary>
/// <remarks>
/// This smaller contract is useful for components such as <see cref="Memoizer"/> that do not need loading, policy, maintenance, or lifecycle operations.
/// </remarks>
public interface ICacheStore
{
    /// <summary>Stores a key and value using cache-level defaults.</summary>
    /// <param name="key">The non-null key used for lookup.</param>
    /// <param name="value">The value to store.</param>
    /// <returns>A result describing the completed or rejected write.</returns>
    CacheWriteResult Write(object? key, object? value);

    /// <summary>Gets a live entry and records an access.</summary>
    /// <param name="key">The key to look up.</param>
    /// <returns>A snapshot describing the hit, miss, or expired entry.</returns>
    CacheGetResult Read(object? key);
}
