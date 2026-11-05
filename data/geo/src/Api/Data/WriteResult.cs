namespace geo;

/// <summary>Stores the non-throwing result of writing into caller-provided storage.</summary>
public class WriteResult
{
    /// <summary>Whether the value was written successfully.</summary>
    public bool Success;

    /// <summary>The number of characters or bytes required for the complete value.</summary>
    public int RequiredSize;

    /// <summary>The number of characters or bytes actually written.</summary>
    public int WrittenSize;

    /// <summary>The writing failure, or null when writing succeeded.</summary>
    public ParseError? Error;
}
