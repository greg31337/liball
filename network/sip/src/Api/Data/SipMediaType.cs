namespace sip;

/// <summary>
/// Holds a media type, subtype, and ordered parameters.
/// </summary>
public class SipMediaType
{
    /// <summary>The top-level media type token.</summary>
    public string? Type;

    /// <summary>The media subtype token.</summary>
    public string? Subtype;

    /// <summary>The ordered media type parameters.</summary>
    public SipUriParameter[] Parameters = new SipUriParameter[0];

    /// <summary>The Accept quality value in thousandths, or negative one when absent.</summary>
    public int QualityThousandths = -1;

    /// <summary>The original media type text when preservation was requested.</summary>
    public string? OriginalText;
}
