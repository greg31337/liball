namespace geo;

/// <summary>Creates edited, canonical, and collected geometry values without mutating caller-owned inputs.</summary>
public interface IGeometryEditor
{
    /// <summary>Creates an independent deep copy of a geometry.</summary>
    /// <param name="geometry">The geometry to copy.</param>
    /// <returns>The owning geometry copy.</returns>
    Geometry CloneGeometry(Geometry geometry);

    /// <summary>Creates a deterministic canonical representation of a geometry.</summary>
    /// <param name="geometry">The geometry to normalize.</param>
    /// <returns>The normalized geometry.</returns>
    Geometry Normalize(Geometry geometry);

    /// <summary>Reverses coordinate traversal while preserving geometry structure.</summary>
    /// <param name="geometry">The geometry to reverse.</param>
    /// <returns>The reversed geometry.</returns>
    Geometry Reverse(Geometry geometry);

    /// <summary>Corrects polygon shells and holes to opposite requested orientations.</summary>
    /// <param name="geometry">The geometry whose rings are corrected.</param>
    /// <param name="shellOrientation">The required shell orientation.</param>
    /// <returns>The orientation-corrected geometry.</returns>
    Geometry OrientRings(Geometry geometry, RingOrientation shellOrientation);

    /// <summary>Applies a two-dimensional affine transformation while preserving extra ordinates.</summary>
    /// <param name="geometry">The geometry to transform.</param>
    /// <param name="transform">The affine transformation coefficients.</param>
    /// <returns>The transformed geometry.</returns>
    Geometry ApplyAffineTransform(Geometry geometry, AffineTransform transform);

    /// <summary>Replaces spatial reference metadata without changing coordinates.</summary>
    /// <param name="geometry">The geometry whose metadata is replaced.</param>
    /// <param name="spatialReferenceId">The replacement spatial reference identifier.</param>
    /// <returns>The geometry with replaced metadata.</returns>
    Geometry ReplaceSpatialReferenceId(Geometry geometry, int spatialReferenceId);

    /// <summary>Converts coordinate layout, using supplied ordinates when a dimension is added.</summary>
    /// <param name="geometry">The geometry to convert.</param>
    /// <param name="coordinateLayout">The target coordinate layout.</param>
    /// <param name="addedZ">The Z ordinate used when adding Z.</param>
    /// <param name="addedM">The M ordinate used when adding M.</param>
    /// <returns>The converted geometry.</returns>
    Geometry ConvertCoordinateLayout(Geometry geometry, CoordinateLayout coordinateLayout, double addedZ, double addedM);

    /// <summary>Snaps planar ordinates to a precision grid.</summary>
    /// <param name="geometry">The geometry to snap.</param>
    /// <param name="precisionModel">The target precision model.</param>
    /// <returns>The snapped geometry.</returns>
    Geometry SnapToGrid(Geometry geometry, PrecisionModel precisionModel);

    /// <summary>Removes consecutive repeated planar positions from coordinate sequences.</summary>
    /// <param name="geometry">The geometry to clean.</param>
    /// <returns>The cleaned geometry.</returns>
    Geometry RemoveRepeatedPoints(Geometry geometry);

    /// <summary>Copies non-empty leaf geometries into a flat geometry collection.</summary>
    /// <param name="geometry">The geometry to flatten.</param>
    /// <returns>The flat geometry collection.</returns>
    Geometry Flatten(Geometry geometry);

    /// <summary>Copies compatible geometries into the narrowest valid result.</summary>
    /// <param name="geometries">The geometries to combine.</param>
    /// <returns>The combined geometry.</returns>
    Geometry Combine(Geometry[] geometries);

    /// <summary>Extracts non-empty leaves having an exact geometry kind.</summary>
    /// <param name="geometry">The geometry to search.</param>
    /// <param name="geometryType">The geometry kind to extract.</param>
    /// <returns>The narrowest matching geometry or a canonical empty value.</returns>
    Geometry ExtractByType(Geometry geometry, GeometryType geometryType);

    /// <summary>Extracts non-empty leaves having an exact topological dimension.</summary>
    /// <param name="geometry">The geometry to search.</param>
    /// <param name="dimension">The topological dimension to extract.</param>
    /// <returns>The narrowest matching geometry or a canonical empty collection.</returns>
    Geometry ExtractByDimension(Geometry geometry, TopologicalDimension dimension);
}
