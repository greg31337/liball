namespace uri;

/// <summary>
/// Describes the exception-less outcome of parsing and validating a URN.
/// </summary>
public class UrnParseResult
{
    /// <summary>Whether the complete input is a valid URN.</summary>
    public bool Success;

    /// <summary>The parsed URN, or an empty URN after failure.</summary>
    public Urn Urn;

    /// <summary>The first fatal diagnostic, or <c>null</c> after success.</summary>
    public UriDiagnostic? Diagnostic;

    /// <summary>Initializes a URN parsing result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="urn">The parsed URN outcome.</param>
    /// <param name="diagnostic">The first fatal diagnostic.</param>
    public UrnParseResult(
        bool success,
        Urn urn,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        Urn = urn;
        Diagnostic = diagnostic;
    }
}
