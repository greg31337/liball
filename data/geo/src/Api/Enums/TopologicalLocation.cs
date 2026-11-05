namespace geo;

/// <summary>Identifies a point location relative to a geometry.</summary>
public enum TopologicalLocation
{
    /// <summary>The point lies outside the geometry.</summary>
    Exterior = 0,

    /// <summary>The point lies on the geometry boundary.</summary>
    Boundary = 1,

    /// <summary>The point lies in the geometry interior.</summary>
    Interior = 2
}
