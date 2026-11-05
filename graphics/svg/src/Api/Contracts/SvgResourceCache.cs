namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgResourceCache"/>.</summary>
public interface ISvgResourceCache
{
    /// <summary>Gets an independent snapshot by normalized initial or final URI.</summary>
    /// <param name="normalizedUri">The normalized URI, with an optional fragment.</param>
    /// <returns>A hit containing an independent entry snapshot, or a miss.</returns>
    SvgResourceCacheLookup Get(string normalizedUri);
    /// <summary>Adds or replaces a successful or failed acquisition snapshot.</summary>
    /// <param name="normalizedUri">The normalized initial URI used as the cache identity.</param>
    /// <param name="result">The completed acquisition result to snapshot.</param>
    /// <returns><c>true</c> when the entry fit within the configured capacities.</returns>
    bool Add(string normalizedUri, SvgResourceAcquisitionResult result);
    /// <summary>Removes an entry addressed by its normalized initial or final URI.</summary>
    /// <param name="normalizedUri">The normalized URI, with an optional fragment.</param>
    /// <returns><c>true</c> when an entry was removed.</returns>
    bool Remove(string normalizedUri);
    /// <summary>Clears every entry while retaining cumulative counters.</summary>
    /// <returns>The removed entry count.</returns>
    int Clear();
    /// <summary>Returns an independent capacity and activity snapshot.</summary>
    /// <returns>The current cache statistics.</returns>
    SvgResourceCacheStatistics GetStatistics();
}
