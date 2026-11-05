namespace geo;

/// <summary>Indexes integer-identified envelopes with optional geometry refinement and payloads.</summary>
public interface ISpatialIndex
{
    /// <summary>Prepares the index for queries.</summary>
    void BuildIndex();

    /// <summary>Adds an owning snapshot of an item with a unique identifier.</summary>
    /// <param name="item">The item to insert.</param>
    void InsertItem(SpatialIndexItem item);

    /// <summary>Removes the item having an identifier.</summary>
    /// <param name="itemId">The identifier to remove.</param>
    /// <returns>True when an item was removed.</returns>
    bool RemoveItem(int itemId);

    /// <summary>Replaces the item having the same identifier with a new snapshot.</summary>
    /// <param name="item">The replacement item.</param>
    /// <returns>True when an item was replaced.</returns>
    bool UpdateItem(SpatialIndexItem item);

    /// <summary>Finds items whose envelopes intersect a query envelope.</summary>
    /// <param name="query">The envelope and result-limit criteria.</param>
    /// <returns>Owning item snapshots in stable insertion order.</returns>
    SpatialIndexItem[] QueryEnvelope(SpatialIndexQuery query);

    /// <summary>Finds envelope candidates with optional explicit geometry refinement.</summary>
    /// <param name="query">The envelope, optional geometry, refinement, and result-limit criteria.</param>
    /// <returns>Owning item snapshots in stable insertion order.</returns>
    SpatialIndexItem[] QueryGeometry(SpatialIndexQuery query);

    /// <summary>Finds the nearest eligible item.</summary>
    /// <param name="query">The envelope, optional geometry refinement, and distance criteria.</param>
    /// <returns>An owning item snapshot, or null when no item is eligible.</returns>
    SpatialIndexItem? QueryNearest(SpatialIndexQuery query);

    /// <summary>Finds the nearest eligible items up to the requested result limit.</summary>
    /// <param name="query">The envelope, optional geometry refinement, count, and distance criteria.</param>
    /// <returns>Owning item snapshots ordered by distance and insertion order.</returns>
    SpatialIndexItem[] QueryKNearest(SpatialIndexQuery query);

    /// <summary>Finds eligible items within the requested maximum distance.</summary>
    /// <param name="query">The envelope, optional geometry refinement, count, and distance criteria.</param>
    /// <returns>Owning item snapshots ordered by distance and insertion order.</returns>
    SpatialIndexItem[] QueryWithinDistance(SpatialIndexQuery query);

    /// <summary>Gets owning snapshots of every indexed item.</summary>
    /// <returns>Items in stable insertion order.</returns>
    SpatialIndexItem[] EnumerateItems();

    /// <summary>Counts indexed items.</summary>
    /// <returns>The item count.</returns>
    int CountItems();

    /// <summary>Removes every item according to the implementation's mutation lifecycle.</summary>
    void ClearIndex();
}
