namespace geo;

/// <summary>Specifies RFC 7946 GeoJSON formatting behavior.</summary>
public class GeoJsonWriteOptions
{
    /// <summary>Whether strict RFC 7946 output requirements are enforced.</summary>
    public bool Strict = true;

    /// <summary>The maximum number of fractional digits written for a coordinate.</summary>
    public int CoordinatePrecision = 15;

    /// <summary>Whether an available bounding box is emitted.</summary>
    public bool IncludeBoundingBox;

    /// <summary>Whether retained foreign members are emitted.</summary>
    public bool IncludeForeignMembers = true;

    /// <summary>Whether polygon rings are emitted using right-hand-rule orientation.</summary>
    public bool NormalizeRingOrientation = true;

    /// <summary>Whether the JSON output uses indentation.</summary>
    public bool Indented;
}
