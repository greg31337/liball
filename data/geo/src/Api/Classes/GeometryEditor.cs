namespace geo;

/// <summary>Edits geometry values through independent owning results and never mutates caller inputs.</summary>
public class GeometryEditor : IGeometryEditor
{
    internal GeometryFactoryOptions Options;

    /// <summary>Creates a geometry editor that snapshots configuration for results without source metadata.</summary>
    /// <param name="options">The default geometry configuration.</param>
    public GeometryEditor(GeometryFactoryOptions options)
    {
        Options = GeometryBuilder.CopyAndValidateOptions(options);
    }

    /// <summary>Creates an independent deep copy of a geometry.</summary>
    /// <param name="geometry">The geometry to copy.</param>
    /// <returns>The owning geometry copy.</returns>
    public Geometry CloneGeometry(Geometry geometry)
    {
        return GeometryNormalization.CloneGeometryValue(geometry);
    }

    /// <summary>Creates a deterministic canonical representation of a geometry.</summary>
    /// <param name="geometry">The geometry to normalize.</param>
    /// <returns>The normalized geometry.</returns>
    public Geometry Normalize(Geometry geometry)
    {
        return GeometryNormalization.NormalizeGeometryValue(geometry);
    }

    /// <summary>Reverses coordinate traversal while preserving geometry structure.</summary>
    /// <param name="geometry">The geometry to reverse.</param>
    /// <returns>The reversed geometry.</returns>
    public Geometry Reverse(Geometry geometry)
    {
        return GeometryTransforms.ReverseGeometryValue(geometry);
    }

    /// <summary>Corrects polygon shells and holes to opposite requested orientations.</summary>
    /// <param name="geometry">The geometry whose rings are corrected.</param>
    /// <param name="shellOrientation">The required shell orientation.</param>
    /// <returns>The orientation-corrected geometry.</returns>
    public Geometry OrientRings(Geometry geometry, RingOrientation shellOrientation)
    {
        return GeometryTransforms.OrientRingValues(geometry, shellOrientation);
    }

    /// <summary>Applies a two-dimensional affine transformation while preserving extra ordinates.</summary>
    /// <param name="geometry">The geometry to transform.</param>
    /// <param name="transform">The affine transformation coefficients.</param>
    /// <returns>The transformed geometry.</returns>
    public Geometry ApplyAffineTransform(Geometry geometry, AffineTransform transform)
    {
        return GeometryTransforms.ApplyAffineTransformValue(geometry, transform);
    }

    /// <summary>Replaces spatial reference metadata without changing coordinates.</summary>
    /// <param name="geometry">The geometry whose metadata is replaced.</param>
    /// <param name="spatialReferenceId">The replacement spatial reference identifier.</param>
    /// <returns>The geometry with replaced metadata.</returns>
    public Geometry ReplaceSpatialReferenceId(Geometry geometry, int spatialReferenceId)
    {
        return GeometryTransforms.ReplaceSpatialReferenceValue(geometry, spatialReferenceId);
    }

    /// <summary>Converts coordinate layout, using supplied ordinates when a dimension is added.</summary>
    /// <param name="geometry">The geometry to convert.</param>
    /// <param name="coordinateLayout">The target coordinate layout.</param>
    /// <param name="addedZ">The Z ordinate used when adding Z.</param>
    /// <param name="addedM">The M ordinate used when adding M.</param>
    /// <returns>The converted geometry.</returns>
    public Geometry ConvertCoordinateLayout(Geometry geometry, CoordinateLayout coordinateLayout, double addedZ, double addedM)
    {
        return GeometryTransforms.ConvertCoordinateLayoutValue(geometry, coordinateLayout, addedZ, addedM);
    }

    /// <summary>Snaps planar ordinates to a precision grid.</summary>
    /// <param name="geometry">The geometry to snap.</param>
    /// <param name="precisionModel">The target precision model.</param>
    /// <returns>The snapped geometry.</returns>
    public Geometry SnapToGrid(Geometry geometry, PrecisionModel precisionModel)
    {
        return GeometryTransforms.SnapGeometryToGridValue(geometry, precisionModel);
    }

    /// <summary>Removes consecutive repeated planar positions from coordinate sequences.</summary>
    /// <param name="geometry">The geometry to clean.</param>
    /// <returns>The cleaned geometry.</returns>
    public Geometry RemoveRepeatedPoints(Geometry geometry)
    {
        return GeometryTransforms.RemoveRepeatedCoordinatesValue(geometry);
    }

    /// <summary>Copies non-empty leaf geometries into a flat geometry collection.</summary>
    /// <param name="geometry">The geometry to flatten.</param>
    /// <returns>The flat geometry collection.</returns>
    public Geometry Flatten(Geometry geometry)
    {
        return GeometryCollections.FlattenGeometryValue(geometry);
    }

    /// <summary>Copies compatible geometries into the narrowest valid result.</summary>
    /// <param name="geometries">The geometries to combine.</param>
    /// <returns>The combined geometry.</returns>
    public Geometry Combine(Geometry[] geometries)
    {
        return GeometryCollections.CombineGeometryValues(geometries, Options);
    }

    /// <summary>Extracts non-empty leaves having an exact geometry kind.</summary>
    /// <param name="geometry">The geometry to search.</param>
    /// <param name="geometryType">The geometry kind to extract.</param>
    /// <returns>The narrowest matching geometry or a canonical empty value.</returns>
    public Geometry ExtractByType(Geometry geometry, GeometryType geometryType)
    {
        return GeometryCollections.ExtractGeometryTypeValue(geometry, geometryType);
    }

    /// <summary>Extracts non-empty leaves having an exact topological dimension.</summary>
    /// <param name="geometry">The geometry to search.</param>
    /// <param name="dimension">The topological dimension to extract.</param>
    /// <returns>The narrowest matching geometry or a canonical empty collection.</returns>
    public Geometry ExtractByDimension(Geometry geometry, TopologicalDimension dimension)
    {
        return GeometryCollections.ExtractTopologicalDimensionValue(geometry, dimension);
    }
}
