namespace uri;

/// <summary>
/// Defines options shared by URI validation operations.
/// </summary>
public class UriValidationOptions
{
    /// <summary>The processing limits for the operation.</summary>
    public OperationLimits Limits;

    /// <summary>Whether a non-empty scheme is required.</summary>
    public bool RequireScheme;

    /// <summary>Whether an authority delimiter and authority are required.</summary>
    public bool RequireAuthority;

    /// <summary>Whether a non-empty authority host is required.</summary>
    public bool RequireHost;

    /// <summary>Whether a non-empty path is required.</summary>
    public bool RequirePath;

    /// <summary>Whether a query delimiter is required.</summary>
    public bool RequireQuery;

    /// <summary>Whether a fragment delimiter is required.</summary>
    public bool RequireFragment;

    /// <summary>Whether the reference must match an absolute URI form.</summary>
    public bool RequireAbsoluteUri;

    /// <summary>Initializes URI validation options with default processing limits.</summary>
    public UriValidationOptions()
    {
        Limits = new OperationLimits();
        RequireScheme = false;
        RequireAuthority = false;
        RequireHost = false;
        RequirePath = false;
        RequireQuery = false;
        RequireFragment = false;
        RequireAbsoluteUri = false;
    }
}
