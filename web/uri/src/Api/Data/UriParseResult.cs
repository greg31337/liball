namespace uri;

/// <summary>
/// Describes the exception-less outcome of parsing a URI reference.
/// </summary>
public class UriParseResult
{
    /// <summary>Whether the complete input matched the requested URI production.</summary>
    public bool Success;

    /// <summary>The parsed reference, or an empty reference after failure.</summary>
    public UriReference Reference;

    /// <summary>The first fatal diagnostic, or <c>null</c> after successful parsing.</summary>
    public UriDiagnostic? Diagnostic;

    /// <summary>
    /// Initializes a URI parse result.
    /// </summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="reference">The parsed reference outcome.</param>
    /// <param name="diagnostic">The first fatal diagnostic.</param>
    public UriParseResult(bool success, UriReference reference, UriDiagnostic? diagnostic)
    {
        Success = success;
        Reference = reference;
        Diagnostic = diagnostic;
    }
}
