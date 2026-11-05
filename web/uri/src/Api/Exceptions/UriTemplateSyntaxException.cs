namespace uri;

/// <summary>
/// Represents a URI Template that does not match the RFC 6570 grammar.
/// </summary>
public class UriTemplateSyntaxException : System.Exception
{
    /// <summary>The diagnostic describing the syntax failure.</summary>
    public UriDiagnostic Diagnostic;

    /// <summary>Initializes an exception from a URI Template syntax diagnostic.</summary>
    /// <param name="diagnostic">The diagnostic describing the failure.</param>
    public UriTemplateSyntaxException(UriDiagnostic diagnostic)
        : base(diagnostic.Message)
    {
        Diagnostic = diagnostic;
    }
}
