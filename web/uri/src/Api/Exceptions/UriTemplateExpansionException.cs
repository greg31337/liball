namespace uri;

/// <summary>
/// Represents a failed URI Template expansion operation.
/// </summary>
public class UriTemplateExpansionException : System.Exception
{
    /// <summary>The diagnostic describing the expansion failure.</summary>
    public UriDiagnostic Diagnostic;

    /// <summary>Initializes an exception from a URI Template expansion diagnostic.</summary>
    /// <param name="diagnostic">The diagnostic describing the failure.</param>
    public UriTemplateExpansionException(UriDiagnostic diagnostic)
        : base(diagnostic.Message)
    {
        Diagnostic = diagnostic;
    }
}
