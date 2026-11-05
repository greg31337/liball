namespace geo;

/// <summary>Stores the heterogeneous components of a geometry collection payload.</summary>
public class GeometryCollection
{
    /// <summary>The geometry components, or an empty array when the collection is empty.</summary>
    public Geometry[] Geometries = [];
}
