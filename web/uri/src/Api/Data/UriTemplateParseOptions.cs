namespace uri;

/// <summary>
/// Defines options shared by URI Template parsing operations.
/// </summary>
public class UriTemplateParseOptions
{
    /// <summary>The processing limits for the operation.</summary>
    public OperationLimits Limits;

    /// <summary>Initializes URI Template parsing options with default processing limits.</summary>
    public UriTemplateParseOptions()
    {
        Limits = new OperationLimits();
    }
}
