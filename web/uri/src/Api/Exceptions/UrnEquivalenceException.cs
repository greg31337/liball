namespace uri;

/// <summary>
/// Represents a failed URN normalization or equivalence operation.
/// </summary>
public class UrnEquivalenceException : System.Exception
{
    /// <summary>The diagnostic describing the operation failure.</summary>
    public UriDiagnostic Diagnostic;

    /// <summary>Initializes an exception from a URN equivalence diagnostic.</summary>
    /// <param name="diagnostic">The diagnostic describing the failure.</param>
    public UrnEquivalenceException(UriDiagnostic diagnostic)
        : base(diagnostic.Message)
    {
        Diagnostic = diagnostic;
    }
}
