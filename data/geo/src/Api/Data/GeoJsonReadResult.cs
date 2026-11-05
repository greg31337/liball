namespace geo;

/// <summary>Stores the non-throwing result of reading a GeoJSON document.</summary>
public class GeoJsonReadResult
{
    /// <summary>Whether parsing completed successfully.</summary>
    public bool Success;

    /// <summary>The parsed GeoJSON document, or null when parsing failed.</summary>
    public GeoJsonDocument? Document;

    /// <summary>The parsing failure, or null when parsing succeeded.</summary>
    public ParseError? Error;

    /// <summary>The number of input characters or bytes consumed.</summary>
    public int Consumed;
}
