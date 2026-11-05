namespace geo;

/// <summary>Stores envelope, geometry-refinement, result-count, and distance query criteria.</summary>
public class SpatialIndexQuery
{
    /// <summary>The query envelope.</summary>
    public Envelope Envelope = new Envelope();

    /// <summary>The optional geometry used to refine envelope candidates.</summary>
    public Geometry? Geometry;

    /// <summary>Whether geometry refinement is required.</summary>
    public bool RefineWithGeometry;

    /// <summary>The maximum number of returned items, or zero for no count limit.</summary>
    public int MaximumResults;

    /// <summary>The maximum accepted distance, or positive infinity for no distance limit.</summary>
    public double MaximumDistance = double.PositiveInfinity;
}
