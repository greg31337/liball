namespace uri;

/// <summary>
/// Defines options shared by URI Template expansion operations.
/// </summary>
public class UriTemplateExpansionOptions
{
    /// <summary>The processing limits for the operation.</summary>
    public OperationLimits Limits;

    /// <summary>Initializes URI Template expansion options with default processing limits.</summary>
    public UriTemplateExpansionOptions()
    {
        Limits = new OperationLimits();
    }
}
