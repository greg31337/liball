namespace xml;

/// <summary>
/// Describes a half-open range in an XML source.
/// </summary>
public class SourceRange
{
    /// <summary>
    /// The inclusive start location.
    /// </summary>
    public SourceLocation? Start;
    /// <summary>
    /// The exclusive end location.
    /// </summary>
    public SourceLocation? End;
}

