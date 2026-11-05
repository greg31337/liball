namespace geo;

/// <summary>Inspects geometry values and returns owning coordinate, component, and envelope results.</summary>
public class GeometryInspector : IGeometryInspector
{
    /// <summary>Creates a geometry inspector.</summary>
    public GeometryInspector()
    {
    }

    /// <summary>Gets the geometry kind after validating its selected payload.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>The geometry kind.</returns>
    public GeometryType GetGeometryType(Geometry geometry)
    {
        return GeometryInspection.GetGeometryTypeValue(geometry);
    }

    /// <summary>Gets the highest non-empty topological dimension.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>The topological dimension.</returns>
    public TopologicalDimension GetTopologicalDimension(Geometry geometry)
    {
        return GeometryInspection.GetTopologicalDimensionValue(geometry);
    }

    /// <summary>Gets the coordinate layout.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>The coordinate layout.</returns>
    public CoordinateLayout GetCoordinateLayout(Geometry geometry)
    {
        return GeometryInspection.GetCoordinateLayoutValue(geometry);
    }

    /// <summary>Gets the spatial reference identifier.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>The spatial reference identifier.</returns>
    public int GetSpatialReferenceId(Geometry geometry)
    {
        return GeometryInspection.GetSpatialReferenceIdValue(geometry);
    }

    /// <summary>Determines whether the geometry has no coordinates.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>True when the geometry is empty.</returns>
    public bool IsEmpty(Geometry geometry)
    {
        return GeometryInspection.IsEmptyValue(geometry);
    }

    /// <summary>Determines whether the geometry has a non-empty closed point set or curve sequence.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>True when the geometry is closed.</returns>
    public bool IsClosed(Geometry geometry)
    {
        return GeometryInspection.IsClosedValue(geometry);
    }

    /// <summary>Determines whether the geometry is a structurally valid non-empty ring.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>True when the geometry is a ring.</returns>
    public bool IsRing(Geometry geometry)
    {
        return GeometryInspection.IsRingValue(geometry);
    }

    /// <summary>Determines whether the geometry is an axis-aligned rectangle polygon.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>True when the geometry is a rectangle.</returns>
    public bool IsRectangle(Geometry geometry)
    {
        return GeometryInspection.IsRectangleValue(geometry);
    }

    /// <summary>Counts coordinates recursively, including ring closure coordinates.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>The coordinate count.</returns>
    public int CountCoordinates(Geometry geometry)
    {
        return GeometryInspection.CountCoordinateValues(geometry);
    }

    /// <summary>Counts direct components, or one for an atomic geometry.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>The direct component count.</returns>
    public int CountComponents(Geometry geometry)
    {
        return GeometryInspection.CountComponentValues(geometry);
    }

    /// <summary>Gets an owning copy of a coordinate by flattened traversal index.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <param name="pointIndex">The zero-based coordinate index.</param>
    /// <returns>The coordinate copy.</returns>
    public Coordinate GetPointAt(Geometry geometry, int pointIndex)
    {
        return GeometryInspection.GetPointValueAt(geometry, pointIndex);
    }

    /// <summary>Gets an owning copy of the first coordinate, or null for an empty geometry.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>The first coordinate copy.</returns>
    public Coordinate? GetStartPoint(Geometry geometry)
    {
        return GeometryInspection.GetStartPointValue(geometry);
    }

    /// <summary>Gets an owning copy of the last coordinate, or null for an empty geometry.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>The last coordinate copy.</returns>
    public Coordinate? GetEndPoint(Geometry geometry)
    {
        return GeometryInspection.GetEndPointValue(geometry);
    }

    /// <summary>Gets an owning copy of a direct component by index.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <param name="componentIndex">The zero-based component index.</param>
    /// <returns>The component copy.</returns>
    public Geometry GetComponentAt(Geometry geometry, int componentIndex)
    {
        return GeometryInspection.GetComponentValueAt(geometry, componentIndex);
    }

    /// <summary>Gets the planar envelope as an owning value.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>The geometry envelope.</returns>
    public Envelope GetEnvelope(Geometry geometry)
    {
        return EnvelopeAlgorithms.CreateGeometryEnvelope(geometry);
    }

    /// <summary>Constructs the OGC boundary, including exposed surface edges, using a selected line endpoint rule.</summary>
    /// <param name="geometry">The geometry whose boundary is required.</param>
    /// <param name="boundaryNodeRule">The endpoint valence rule for linear geometries.</param>
    /// <returns>An owning boundary geometry.</returns>
    /// <exception cref="InvalidGeometryException">The geometry is an unsupported non-empty GeometryCollection or is structurally invalid.</exception>
    public Geometry GetBoundary(Geometry geometry, BoundaryNodeRule boundaryNodeRule)
    {
        return GeometryBoundary.CreateBoundaryValue(geometry, boundaryNodeRule);
    }

    /// <summary>Extracts owning coordinate copies in traversal order.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>The coordinate copies.</returns>
    public Coordinate[] ExtractCoordinates(Geometry geometry)
    {
        return GeometryInspection.ExtractCoordinateCopies(geometry);
    }

    /// <summary>Extracts the first owning copy of each distinct coordinate in traversal order.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>The unique coordinate copies.</returns>
    public Coordinate[] ExtractUniqueCoordinates(Geometry geometry)
    {
        return GeometryInspection.ExtractUniqueCoordinateCopies(geometry);
    }
}
