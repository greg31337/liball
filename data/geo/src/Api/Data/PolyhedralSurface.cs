namespace geo;

/// <summary>Stores the polygon patches of a polyhedral surface payload.</summary>
public class PolyhedralSurface
{
    /// <summary>The polygon patches, or an empty array when the surface is empty.</summary>
    public Polygon[] Patches = [];
}
