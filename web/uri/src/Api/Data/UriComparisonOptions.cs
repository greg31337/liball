namespace uri;

/// <summary>
/// Defines options shared by URI comparison operations.
/// </summary>
public class UriComparisonOptions
{
    /// <summary>The processing limits for the operation.</summary>
    public OperationLimits Limits;

    /// <summary>The optional explicit profile used by profile comparison.</summary>
    public UriSchemeProfile? Profile;

    /// <summary>Initializes URI comparison options with default processing limits.</summary>
    public UriComparisonOptions()
    {
        Limits = new OperationLimits();
        Profile = null;
    }
}
