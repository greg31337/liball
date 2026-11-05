namespace geo;

/// <summary>Stores the triangular patches of a triangulated irregular network payload.</summary>
public class Tin
{
    /// <summary>The triangle patches, or an empty array when the TIN is empty.</summary>
    public Triangle[] Triangles = [];
}
