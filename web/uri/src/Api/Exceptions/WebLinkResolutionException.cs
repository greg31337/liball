namespace uri;

/// <summary>
/// Represents a failed Web Link target or context resolution operation.
/// </summary>
public class WebLinkResolutionException : System.Exception
{
    /// <summary>The diagnostic describing the resolution failure.</summary>
    public UriDiagnostic Diagnostic;

    /// <summary>Initializes an exception from a Web Link resolution diagnostic.</summary>
    /// <param name="diagnostic">The diagnostic describing the failure.</param>
    public WebLinkResolutionException(UriDiagnostic diagnostic)
        : base(diagnostic.Message)
    {
        Diagnostic = diagnostic;
    }
}
