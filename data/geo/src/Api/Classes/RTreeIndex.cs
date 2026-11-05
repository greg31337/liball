namespace geo;

/// <summary>Provides a continuously maintained mutable quadratic-split R-tree.</summary>
public class RTreeIndex : ISpatialIndex
{
    internal RTreeBuilder Builder;

    /// <summary>Creates an empty mutable spatial index.</summary>
    /// <param name="nodeCapacity">The maximum number of entries in each tree node.</param>
    /// <exception cref="SpatialIndexException">The node capacity is outside the supported range.</exception>
    public RTreeIndex(int nodeCapacity)
    {
        Builder = new RTreeBuilder(nodeCapacity);
    }

    /// <summary>Confirms readiness of the continuously maintained index.</summary>
    public void BuildIndex()
    {
        Builder.CountItems();
    }

    /// <summary>Adds an owning item snapshot.</summary>
    /// <param name="item">The item to insert, whose identifier must be unique.</param>
    /// <exception cref="SpatialIndexException">The item, identifier, or envelope is invalid.</exception>
    /// <exception cref="InvalidGeometryException">The optional geometry is invalid.</exception>
    public void InsertItem(SpatialIndexItem item)
    {
        Builder.InsertItem(item);
    }

    /// <summary>Removes an identified item.</summary>
    /// <param name="itemId">The identifier to remove.</param>
    /// <returns>True when the item existed and was removed.</returns>
    public bool RemoveItem(int itemId)
    {
        return Builder.RemoveItem(itemId);
    }

    /// <summary>Replaces an identified item while preserving its insertion order.</summary>
    /// <param name="item">The replacement item with the existing identifier.</param>
    /// <returns>True when the item existed and was replaced.</returns>
    /// <exception cref="SpatialIndexException">The replacement item or envelope is invalid.</exception>
    /// <exception cref="InvalidGeometryException">The optional geometry is invalid.</exception>
    public bool UpdateItem(SpatialIndexItem item)
    {
        return Builder.UpdateItem(item);
    }

    /// <summary>Finds items whose envelopes intersect the query envelope.</summary>
    /// <param name="query">The envelope and result-limit criteria.</param>
    /// <returns>Owning item snapshots in stable insertion order.</returns>
    public SpatialIndexItem[] QueryEnvelope(SpatialIndexQuery query)
    {
        return Builder.QueryEnvelope(query);
    }

    /// <summary>Finds envelope candidates with optional explicit geometry refinement.</summary>
    /// <param name="query">The envelope, optional geometry, refinement, and result-limit criteria.</param>
    /// <returns>Owning item snapshots in stable insertion order.</returns>
    public SpatialIndexItem[] QueryGeometry(SpatialIndexQuery query)
    {
        return Builder.QueryGeometry(query);
    }

    /// <summary>Finds the nearest eligible item.</summary>
    /// <param name="query">The envelope, optional geometry refinement, and distance criteria.</param>
    /// <returns>An owning item snapshot, or null when none is eligible.</returns>
    public SpatialIndexItem? QueryNearest(SpatialIndexQuery query)
    {
        return Builder.QueryNearest(query);
    }

    /// <summary>Finds the nearest eligible items up to the requested limit.</summary>
    /// <param name="query">The envelope, optional geometry refinement, count, and distance criteria.</param>
    /// <returns>Owning item snapshots ordered by distance and insertion order.</returns>
    public SpatialIndexItem[] QueryKNearest(SpatialIndexQuery query)
    {
        return Builder.QueryKNearest(query);
    }

    /// <summary>Finds eligible items within the requested maximum distance.</summary>
    /// <param name="query">The envelope, optional geometry refinement, count, and distance criteria.</param>
    /// <returns>Owning item snapshots ordered by distance and insertion order.</returns>
    public SpatialIndexItem[] QueryWithinDistance(
        SpatialIndexQuery query)
    {
        return Builder.QueryWithinDistance(query);
    }

    /// <summary>Gets owning snapshots of every indexed item.</summary>
    /// <returns>Items in stable insertion order.</returns>
    public SpatialIndexItem[] EnumerateItems()
    {
        return Builder.EnumerateItems();
    }

    /// <summary>Counts indexed items.</summary>
    /// <returns>The item count.</returns>
    public int CountItems()
    {
        return Builder.CountItems();
    }

    /// <summary>Removes every item and leaves the index ready for reuse.</summary>
    public void ClearIndex()
    {
        Builder.ClearItems();
    }
}
