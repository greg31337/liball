namespace uri;

/// <summary>
/// Represents a failed percent-encoding, percent-validation, or percent-normalization operation.
/// </summary>
public class PercentEncodingException : System.Exception
{
    /// <summary>The diagnostic describing the operation failure.</summary>
    public UriDiagnostic Diagnostic;

    /// <summary>
    /// Initializes an exception from an operation diagnostic.
    /// </summary>
    /// <param name="diagnostic">The diagnostic describing the failure.</param>
    public PercentEncodingException(UriDiagnostic diagnostic)
        : base(diagnostic.Message)
    {
        Diagnostic = diagnostic;
    }
}
