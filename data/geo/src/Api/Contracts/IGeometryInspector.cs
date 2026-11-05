namespace geo;

/// <summary>Inspects geometry metadata, structure, coordinates, components, and envelopes without mutating inputs.</summary>
public interface IGeometryInspector
{
    /// <summary>Gets the geometry kind after validating its selected payload.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>The geometry kind.</returns>
    GeometryType GetGeometryType(Geometry geometry);

    /// <summary>Gets the highest non-empty topological dimension.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>The topological dimension.</returns>
    TopologicalDimension GetTopologicalDimension(Geometry geometry);

    /// <summary>Gets the coordinate layout.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>The coordinate layout.</returns>
    CoordinateLayout GetCoordinateLayout(Geometry geometry);

    /// <summary>Gets the spatial reference identifier.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>The spatial reference identifier.</returns>
    int GetSpatialReferenceId(Geometry geometry);

    /// <summary>Determines whether the geometry has no coordinates.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>True when the geometry is empty.</returns>
    bool IsEmpty(Geometry geometry);

    /// <summary>Determines whether the geometry has a non-empty closed point set or curve sequence.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>True when the geometry is closed.</returns>
    bool IsClosed(Geometry geometry);

    /// <summary>Determines whether the geometry is a structurally valid non-empty ring.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>True when the geometry is a ring.</returns>
    bool IsRing(Geometry geometry);

    /// <summary>Determines whether the geometry is an axis-aligned rectangle polygon.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>True when the geometry is a rectangle.</returns>
    bool IsRectangle(Geometry geometry);

    /// <summary>Counts coordinates recursively, including ring closure coordinates.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>The coordinate count.</returns>
    int CountCoordinates(Geometry geometry);

    /// <summary>Counts direct components, or one for an atomic geometry.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>The direct component count.</returns>
    int CountComponents(Geometry geometry);

    /// <summary>Gets an owning copy of a coordinate by flattened traversal index.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <param name="pointIndex">The zero-based coordinate index.</param>
    /// <returns>The coordinate copy.</returns>
    Coordinate GetPointAt(Geometry geometry, int pointIndex);

    /// <summary>Gets an owning copy of the first coordinate, or null for an empty geometry.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>The first coordinate copy.</returns>
    Coordinate? GetStartPoint(Geometry geometry);

    /// <summary>Gets an owning copy of the last coordinate, or null for an empty geometry.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>The last coordinate copy.</returns>
    Coordinate? GetEndPoint(Geometry geometry);

    /// <summary>Gets an owning copy of a direct component by index.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <param name="componentIndex">The zero-based component index.</param>
    /// <returns>The component copy.</returns>
    Geometry GetComponentAt(Geometry geometry, int componentIndex);

    /// <summary>Gets the planar envelope as an owning value.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>The geometry envelope.</returns>
    Envelope GetEnvelope(Geometry geometry);

    /// <summary>Constructs the OGC boundary, including exposed surface edges, using a selected line endpoint rule.</summary>
    /// <param name="geometry">The geometry whose boundary is required.</param>
    /// <param name="boundaryNodeRule">The endpoint valence rule for linear geometries.</param>
    /// <returns>An owning boundary geometry.</returns>
    /// <exception cref="InvalidGeometryException">The geometry is an unsupported non-empty GeometryCollection or is structurally invalid.</exception>
    Geometry GetBoundary(Geometry geometry, BoundaryNodeRule boundaryNodeRule);

    /// <summary>Extracts owning coordinate copies in traversal order.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>The coordinate copies.</returns>
    Coordinate[] ExtractCoordinates(Geometry geometry);

    /// <summary>Extracts the first owning copy of each distinct coordinate in traversal order.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>The unique coordinate copies.</returns>
    Coordinate[] ExtractUniqueCoordinates(Geometry geometry);
}
