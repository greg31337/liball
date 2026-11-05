namespace iso20022;

/// <summary>Identifies a location in a source artifact or message.</summary>
public class SourceLocation
{
    /// <summary>The source name or URI when known.</summary>
    public string? SourceName;
    /// <summary>Whether line and column information is available.</summary>
    public bool HasLineInformation;
    /// <summary>The one-based line number when available.</summary>
    public int LineNumber;
    /// <summary>The one-based column number when available.</summary>
    public int ColumnNumber;
    /// <summary>Whether a character offset is available.</summary>
    public bool HasCharacterOffset;
    /// <summary>The zero-based character offset when available.</summary>
    public long CharacterOffset;
}
