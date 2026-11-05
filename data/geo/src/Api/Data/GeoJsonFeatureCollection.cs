using System.Collections.Generic;
using System.Text.Json;

namespace geo;

/// <summary>Stores an ordered GeoJSON FeatureCollection and its retained metadata.</summary>
public class GeoJsonFeatureCollection
{
    /// <summary>The ordered feature values, or an empty array when the collection is empty.</summary>
    public GeoJsonFeature[] Features = [];

    /// <summary>The optional collection bounding box, or an empty array when absent.</summary>
    public double[] BoundingBox = [];

    /// <summary>The arbitrary foreign members retained from the FeatureCollection object.</summary>
    public Dictionary<string, JsonElement> ForeignMembers = new Dictionary<string, JsonElement>();

    internal void CloneJsonValues()
    {
        Features = (GeoJsonFeature[])Features.Clone();
        foreach (GeoJsonFeature feature in Features)
        {
            feature.CloneJsonValues();
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
