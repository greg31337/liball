namespace uri;

/// <summary>
/// Represents a failed URI resolution or relativization operation.
/// </summary>
public class UriResolutionException : System.Exception
{
    /// <summary>The diagnostic describing the resolution failure.</summary>
    public UriDiagnostic Diagnostic;

    /// <summary>Initializes an exception from a URI resolution diagnostic.</summary>
    /// <param name="diagnostic">The diagnostic describing the failure.</param>
    public UriResolutionException(UriDiagnostic diagnostic)
        : base(diagnostic.Message)
    {
        Diagnostic = diagnostic;
    }
}
