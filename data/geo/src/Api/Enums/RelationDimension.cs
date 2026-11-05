namespace geo;

/// <summary>Identifies the dimension stored in a DE-9IM matrix cell.</summary>
public enum RelationDimension
{
    /// <summary>The intersection is empty.</summary>
    False = -1,

    /// <summary>The intersection contains points.</summary>
    Point = 0,

    /// <summary>The intersection contains curves.</summary>
    Curve = 1,

    /// <summary>The intersection contains surfaces.</summary>
    Surface = 2
}
