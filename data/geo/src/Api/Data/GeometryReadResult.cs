namespace geo;

/// <summary>Stores the non-throwing result of reading a geometry.</summary>
public class GeometryReadResult
{
    /// <summary>Whether parsing completed successfully.</summary>
    public bool Success;

    /// <summary>The parsed geometry, or null when parsing failed.</summary>
    public Geometry? Geometry;

    /// <summary>The parsing failure, or null when parsing succeeded.</summary>
    public ParseError? Error;

    /// <summary>The number of input characters or bytes consumed.</summary>
    public int Consumed;
}
