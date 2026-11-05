namespace uri;

/// <summary>
/// Represents a URI input that does not match the requested RFC 3986 production.
/// </summary>
public class UriSyntaxException : System.Exception
{
    /// <summary>The diagnostic describing the syntax failure.</summary>
    public UriDiagnostic Diagnostic;

    /// <summary>
    /// Initializes an exception from a URI syntax diagnostic.
    /// </summary>
    /// <param name="diagnostic">The diagnostic describing the failure.</param>
    public UriSyntaxException(UriDiagnostic diagnostic)
        : base(diagnostic.Message)
    {
        Diagnostic = diagnostic;
    }
}
