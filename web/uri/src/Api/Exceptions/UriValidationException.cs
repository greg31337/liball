namespace uri;

/// <summary>
/// Represents a URI reference that does not satisfy selected validation requirements.
/// </summary>
public class UriValidationException : System.Exception
{
    /// <summary>The diagnostic describing the validation failure.</summary>
    public UriDiagnostic Diagnostic;

    /// <summary>
    /// Initializes an exception from a URI validation diagnostic.
    /// </summary>
    /// <param name="diagnostic">The diagnostic describing the failure.</param>
    public UriValidationException(UriDiagnostic diagnostic)
        : base(diagnostic.Message)
    {
        Diagnostic = diagnostic;
    }
}
