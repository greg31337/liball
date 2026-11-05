namespace sdp;

/// <summary>
/// Identifies a position in character or UTF-8 SDP input.
/// </summary>
public class SdpSourceLocation
{
    /// <summary>Contains the zero-based character offset, or <c>-1</c> when unavailable.</summary>
    public int CharacterOffset = -1;

    /// <summary>Contains the zero-based byte offset, or <c>-1</c> when unavailable.</summary>
    public int ByteOffset = -1;

    /// <summary>Contains the one-based line number, or zero when unavailable.</summary>
    public int LineNumber = 0;

    /// <summary>Contains the one-based column number, or zero when unavailable.</summary>
    public int ColumnNumber = 0;
}
