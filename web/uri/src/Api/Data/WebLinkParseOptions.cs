namespace uri;

/// <summary>
/// Defines options shared by Web Link parsing operations.
/// </summary>
public class WebLinkParseOptions
{
    /// <summary>The processing limits for the operation.</summary>
    public OperationLimits Limits;

    /// <summary>The strict grammar or advisory recovery algorithm to apply.</summary>
    public WebLinkParseMode Mode;

    /// <summary>Initializes strict Web Link parsing with default processing limits.</summary>
    public WebLinkParseOptions()
    {
        Limits = new OperationLimits();
        Mode = WebLinkParseMode.Strict;
    }
}
