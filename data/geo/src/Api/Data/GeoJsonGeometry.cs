using System.Collections.Generic;
using System.Text.Json;

namespace geo;

/// <summary>Stores a GeoJSON geometry, bounding box, and foreign members.</summary>
public class GeoJsonGeometry
{
    /// <summary>The represented geometry kind.</summary>
    public GeometryType GeometryType = GeometryType.Point;

    /// <summary>The represented geometry value.</summary>
    public Geometry? Value;

    /// <summary>The child geometry objects when this value is a GeometryCollection.</summary>
    public GeoJsonGeometry[] Geometries = [];

    /// <summary>The optional GeoJSON bounding box, or an empty array when absent.</summary>
    public double[] BoundingBox = [];

    /// <summary>The arbitrary foreign members retained from the GeoJSON object.</summary>
    public Dictionary<string, JsonElement> ForeignMembers = new Dictionary<string, JsonElement>();

    internal void CloneJsonValues()
    {
        Geometries = (GeoJsonGeometry[])Geometries.Clone();
        foreach (GeoJsonGeometry geometry in Geometries)
        {
            geometry.CloneJsonValues();
        }

        BoundingBox = (double[])BoundingBox.Clone();
        Dictionary<string, JsonElement> clonedMembers = new Dictionary<string, JsonElement>(ForeignMembers.Count);
        foreach (KeyValuePair<string, JsonElement> member in ForeignMembers)
        {
            clonedMembers.Add(member.Key, member.Value.Clone());
        }

        ForeignMembers = clonedMembers;
    }
}
