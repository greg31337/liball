namespace uri;

/// <summary>
/// Defines options shared by URI normalization operations.
/// </summary>
public class UriNormalizationOptions
{
    /// <summary>The processing limits for the operation.</summary>
    public OperationLimits Limits;

    /// <summary>The optional scheme profile used by profile-aware normalization.</summary>
    public UriSchemeProfile? Profile;

    /// <summary>Initializes URI normalization options with default processing limits.</summary>
    public UriNormalizationOptions()
    {
        Limits = new OperationLimits();
        Profile = null;
    }
}
