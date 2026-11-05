namespace asn1;

/// <summary>
/// Describes a lexical token classification.
/// </summary>
public class AsnToken
{
    /// <summary>
    /// Contains the token kind.
    /// </summary>
    public AsnTokenKind Kind;

    /// <summary>
    /// Contains the exact token text.
    /// </summary>
    public string Text = "";

    /// <summary>
    /// Contains the token source span when available.
    /// </summary>
    public AsnSourceSpan? Span;
}
