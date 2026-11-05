namespace uri;

/// <summary>
/// Describes the exception-less outcome of parsing an IRI reference.
/// </summary>
public class IriParseResult
{
    /// <summary>Whether the complete input matched the requested IRI production.</summary>
    public bool Success;

    /// <summary>The parsed IRI reference, or an empty reference after failure.</summary>
    public IriReference Reference;

    /// <summary>The first fatal diagnostic, or <c>null</c> after successful parsing.</summary>
    public UriDiagnostic? Diagnostic;

    /// <summary>Initializes an IRI parsing result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="reference">The parsed IRI reference outcome.</param>
    /// <param name="diagnostic">The first fatal diagnostic.</param>
    public IriParseResult(
        bool success,
        IriReference reference,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        Reference = reference;
        Diagnostic = diagnostic;
    }
}
