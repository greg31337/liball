using System.Collections.Generic;
using System.Text.Json;

namespace geo;

/// <summary>Stores exactly one classified GeoJSON root object and its retained metadata.</summary>
public class GeoJsonDocument
{
    /// <summary>The root GeoJSON object classification.</summary>
    public GeoJsonObjectType ObjectType;

    /// <summary>The geometry when the root is a geometry object.</summary>
    public GeoJsonGeometry? Geometry;

    /// <summary>The feature when the root is a Feature object.</summary>
    public GeoJsonFeature? Feature;

    /// <summary>The feature collection when the root is a FeatureCollection object.</summary>
    public GeoJsonFeatureCollection? FeatureCollection;

    /// <summary>The optional root bounding box, or an empty array when absent.</summary>
    public double[] BoundingBox = [];

    /// <summary>The arbitrary foreign members retained from the root object.</summary>
    public Dictionary<string, JsonElement> ForeignMembers = new Dictionary<string, JsonElement>();

    internal void CloneJsonValues()
    {
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

        if (Feature != null)
        {
            Feature.CloneJsonValues();
        }

        if (FeatureCollection != null)
        {
            FeatureCollection.CloneJsonValues();
        }
    }
}
