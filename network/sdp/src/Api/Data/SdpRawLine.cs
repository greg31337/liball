namespace sdp;

/// <summary>
/// Retains an original SDP line for lossless output or parse recovery.
/// </summary>
public class SdpRawLine
{
    /// <summary>Contains the field discriminator, or the null character when it could not be read.</summary>
    public char FieldName = '\0';

    /// <summary>Contains the complete line text without its line ending.</summary>
    public string Text = string.Empty;

    /// <summary>Contains the text following the field discriminator and equals sign.</summary>
    public string Value = string.Empty;

    /// <summary>Identifies the original line ending.</summary>
    public SdpLineEnding LineEnding = SdpLineEnding.Unknown;

    /// <summary>Indicates that the line was retained after malformed-input recovery.</summary>
    public bool IsMalformed = false;

    /// <summary>Identifies the line's starting position in the input.</summary>
    public SdpSourceLocation Location = new SdpSourceLocation();
}
