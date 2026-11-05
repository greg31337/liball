namespace sdp;

/// <summary>
/// Identifies a stable path to a field, attribute, or repeated value in an SDP description.
/// </summary>
public class SdpSourcePath
{
    /// <summary>Identifies whether the path belongs to session or media scope.</summary>
    public SdpScope Scope = SdpScope.Unknown;

    /// <summary>Contains the zero-based media index, or <c>-1</c> for session scope.</summary>
    public int MediaIndex = -1;

    /// <summary>Contains the media identifier when one is available.</summary>
    public string MediaId = string.Empty;

    /// <summary>Contains the field discriminator, or the null character when unavailable.</summary>
    public char FieldName = '\0';

    /// <summary>Contains the attribute name when the path identifies an attribute.</summary>
    public string AttributeName = string.Empty;

    /// <summary>Contains the zero-based occurrence index for a repeated field or attribute.</summary>
    public int OccurrenceIndex = -1;

    /// <summary>Contains the zero-based index of a nested value, or <c>-1</c> when unavailable.</summary>
    public int ValueIndex = -1;
}
