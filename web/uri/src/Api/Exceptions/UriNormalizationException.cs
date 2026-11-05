namespace uri;

/// <summary>
/// Represents a failed URI normalization operation.
/// </summary>
public class UriNormalizationException : System.Exception
{
    /// <summary>The diagnostic describing the normalization failure.</summary>
    public UriDiagnostic Diagnostic;

    /// <summary>Initializes an exception from a URI normalization diagnostic.</summary>
    /// <param name="diagnostic">The diagnostic describing the failure.</param>
    public UriNormalizationException(UriDiagnostic diagnostic)
        : base(diagnostic.Message)
    {
        Diagnostic = diagnostic;
    }
}
