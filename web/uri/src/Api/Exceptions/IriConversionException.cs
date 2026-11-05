namespace uri;

/// <summary>
/// Represents a failed IRI conversion or Unicode-normalization operation.
/// </summary>
public class IriConversionException : System.Exception
{
    /// <summary>The diagnostic describing the operation failure.</summary>
    public UriDiagnostic Diagnostic;

    /// <summary>Initializes an exception from an IRI operation diagnostic.</summary>
    /// <param name="diagnostic">The diagnostic describing the failure.</param>
    public IriConversionException(UriDiagnostic diagnostic)
        : base(diagnostic.Message)
    {
        Diagnostic = diagnostic;
    }
}
