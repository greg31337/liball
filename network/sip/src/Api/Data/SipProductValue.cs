namespace sip;

/// <summary>
/// Holds one product or comment from a Server or User-Agent header field.
/// </summary>
public class SipProductValue
{
    /// <summary>Indicates that the value is a comment rather than a product.</summary>
    public bool IsComment;

    /// <summary>The product name, or null for a comment.</summary>
    public string? Name;

    /// <summary>The optional product version.</summary>
    public string? Version;

    /// <summary>The decoded comment text, or null for a product.</summary>
    public string? Comment;
}
