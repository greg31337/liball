namespace uri;

/// <summary>
/// Defines options shared by URI parsing operations.
/// </summary>
public class UriParseOptions
{
    /// <summary>The processing limits for the operation.</summary>
    public OperationLimits Limits;

    /// <summary>Initializes URI parsing options with default processing limits.</summary>
    public UriParseOptions()
    {
        Limits = new OperationLimits();
    }
}
