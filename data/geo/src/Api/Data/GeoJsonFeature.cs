using System.Collections.Generic;
using System.Text.Json;

namespace geo;

/// <summary>Stores a GeoJSON Feature including optional geometry, identity, properties, and metadata.</summary>
public class GeoJsonFeature
{
    /// <summary>The feature geometry, or null for a null GeoJSON geometry.</summary>
    public GeoJsonGeometry? Geometry;

    /// <summary>The string or number identifier, or an undefined value when absent.</summary>
    public JsonElement Id;

    /// <summary>The arbitrary properties value, including JSON null.</summary>
    public JsonElement Properties;

    /// <summary>The optional feature bounding box, or an empty array when absent.</summary>
    public double[] BoundingBox = [];

    /// <summary>The arbitrary foreign members retained from the Feature object.</summary>
    public Dictionary<string, JsonElement> ForeignMembers = new Dictionary<string, JsonElement>();

    internal void CloneJsonValues()
    {
        if (Id.ValueKind != JsonValueKind.Undefined)
        {
            Id = Id.Clone();
        }

        if (Properties.ValueKind != JsonValueKind.Undefined)
        {
            Properties = Properties.Clone();
        }

        BoundingBox = (double[])BoundingBox.Clone();
        Dictionary<string, JsonElement> clonedMembers = new Dictionary<string, JsonElement>(ForeignMembers.Count);
        foreach (KeyValuePair<string, JsonElement> member in ForeignMembers)
        {
            clonedMembers.Add(member.Key, member.Value.Clone());
        }

        ForeignMembers = clonedMembers;
        if (Geometry != null)
        {
            Geometry.CloneJsonValues();
        }
    }
}
