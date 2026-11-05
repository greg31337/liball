namespace uri;

/// <summary>
/// Represents Web Link field input that does not match the selected grammar.
/// </summary>
public class WebLinkSyntaxException : System.Exception
{
    /// <summary>The diagnostic describing the syntax failure.</summary>
    public UriDiagnostic Diagnostic;

    /// <summary>Initializes an exception from a Web Link syntax diagnostic.</summary>
    /// <param name="diagnostic">The diagnostic describing the failure.</param>
    public WebLinkSyntaxException(UriDiagnostic diagnostic)
        : base(diagnostic.Message)
    {
        Diagnostic = diagnostic;
    }
}
