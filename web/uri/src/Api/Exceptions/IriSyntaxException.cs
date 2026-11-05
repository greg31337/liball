namespace uri;

/// <summary>
/// Represents an IRI input that does not match the requested RFC 3987 production.
/// </summary>
public class IriSyntaxException : System.Exception
{
    /// <summary>The diagnostic describing the syntax failure.</summary>
    public UriDiagnostic Diagnostic;

    /// <summary>Initializes an exception from an IRI syntax diagnostic.</summary>
    /// <param name="diagnostic">The diagnostic describing the failure.</param>
    public IriSyntaxException(UriDiagnostic diagnostic)
        : base(diagnostic.Message)
    {
        Diagnostic = diagnostic;
    }
}
