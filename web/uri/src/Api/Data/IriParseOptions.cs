namespace uri;

/// <summary>
/// Defines options shared by IRI parsing operations.
/// </summary>
public class IriParseOptions
{
    /// <summary>The processing limits for the operation.</summary>
    public OperationLimits Limits;

    /// <summary>Initializes IRI parsing options with default processing limits.</summary>
    public IriParseOptions()
    {
        Limits = new OperationLimits();
    }
}
