namespace asn1;

/// <summary>
/// Describes a source location related to a diagnostic.
/// </summary>
public class AsnDiagnosticRelatedLocation
{
    /// <summary>
    /// Contains the explanation of the relationship.
    /// </summary>
    public string Message = "";

    /// <summary>
    /// Contains the related source span.
    /// </summary>
    public AsnSourceSpan Span = new AsnSourceSpan();
}
