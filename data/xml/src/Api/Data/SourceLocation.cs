namespace xml;

/// <summary>
/// Describes a position in an XML source.
/// </summary>
public class SourceLocation
{
    /// <summary>
    /// The source URI, or null when no URI is known.
    /// </summary>
    public string? SourceUri;
    /// <summary>
    /// The zero-based normalized character offset.
    /// </summary>
    public long CharacterOffset;
    /// <summary>
    /// The zero-based raw byte offset, or a negative value when unavailable.
    /// </summary>
    public long ByteOffset;
    /// <summary>
    /// The one-based line number.
    /// </summary>
    public int Line;
    /// <summary>
    /// The one-based column number.
    /// </summary>
    public int Column;
}

