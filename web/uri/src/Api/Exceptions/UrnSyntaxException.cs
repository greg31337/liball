namespace uri;

/// <summary>
/// Represents a URN that does not match the selected RFC 8141 validation policy.
/// </summary>
public class UrnSyntaxException : System.Exception
{
    /// <summary>The diagnostic describing the syntax failure.</summary>
    public UriDiagnostic Diagnostic;

    /// <summary>Initializes an exception from a URN syntax diagnostic.</summary>
    /// <param name="diagnostic">The diagnostic describing the failure.</param>
    public UrnSyntaxException(UriDiagnostic diagnostic)
        : base(diagnostic.Message)
    {
        Diagnostic = diagnostic;
    }
}
