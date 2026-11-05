namespace geo;

/// <summary>Identifies the traversal direction of a ring.</summary>
public enum RingOrientation
{
    /// <summary>No orientation, as for an empty or degenerate ring.</summary>
    Undefined = 0,

    /// <summary>Clockwise traversal.</summary>
    Clockwise = 1,

    /// <summary>Counter-clockwise traversal.</summary>
    CounterClockwise = 2
}
