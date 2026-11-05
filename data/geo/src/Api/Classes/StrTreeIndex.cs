namespace geo;

/// <summary>Provides a packed immutable Sort-Tile-Recursive spatial index.</summary>
/// <remarks>Insert, remove, update, and clear operations are permitted only before an explicit build or the first query.</remarks>
public class StrTreeIndex : ISpatialIndex
{
    internal StrTreeBuilder Builder;

    /// <summary>Creates an unbuilt packed spatial index.</summary>
    /// <param name="nodeCapacity">The maximum number of entries in each tree node.</param>
    /// <exception cref="SpatialIndexException">The node capacity is outside the supported range.</exception>
    public StrTreeIndex(int nodeCapacity)
    {
        Builder = new StrTreeBuilder(nodeCapacity);
    }

    /// <summary>Builds and freezes the packed index; repeated calls have no effect.</summary>
    public void BuildIndex()
    {
        Builder.BuildTree();
    }

    /// <summary>Adds an owning item snapshot before the index is built.</summary>
    /// <param name="item">The item to insert, whose identifier must be unique.</param>
    /// <exception cref="SpatialIndexException">The index is built, or the item, identifier, or envelope is invalid.</exception>
    /// <exception cref="InvalidGeometryException">The optional geometry is invalid.</exception>
    public void InsertItem(SpatialIndexItem item)
    {
        Builder.AddItem(item);
    }

    /// <summary>Removes an identified item before the index is built.</summary>
    /// <param name="itemId">The identifier to remove.</param>
    /// <returns>True when the item existed and was removed.</returns>
    /// <exception cref="SpatialIndexException">The index has already been built.</exception>
    public bool RemoveItem(int itemId)
    {
        return Builder.RemoveItem(itemId);
    }

    /// <summary>Replaces an identified item before the index is built while preserving its insertion order.</summary>
    /// <param name="item">The replacement item with the existing identifier.</param>
    /// <returns>True when the item existed and was replaced.</returns>
    /// <exception cref="SpatialIndexException">The index is built, or the replacement is invalid.</exception>
    /// <exception cref="InvalidGeometryException">The optional geometry is invalid.</exception>
    public bool UpdateItem(SpatialIndexItem item)
    {
        return Builder.UpdateItem(item);
    }

    /// <summary>Finds envelope intersections and builds the index when necessary.</summary>
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

    /// <summary>Gets owning snapshots of all items and builds the index when necessary.</summary>
    /// <returns>Items in stable insertion order.</returns>
    public SpatialIndexItem[] EnumerateItems()
    {
        return Builder.EnumerateItems();
    }

    /// <summary>Counts indexed items without building the index.</summary>
    /// <returns>The item count.</returns>
    public int CountItems()
    {
        return Builder.CountItems();
    }

    /// <summary>Removes every item before the index is built.</summary>
    /// <exception cref="SpatialIndexException">The index has already been built.</exception>
    public void ClearIndex()
    {
        Builder.ClearItems();
    }
}
