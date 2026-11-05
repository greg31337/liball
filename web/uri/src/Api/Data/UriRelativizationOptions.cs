namespace uri;

/// <summary>
/// Defines options shared by URI relativization operations.
/// </summary>
public class UriRelativizationOptions
{
    /// <summary>The processing limits for the operation.</summary>
    public OperationLimits Limits;

    /// <summary>The preferred output reference form.</summary>
    public UriRelativizationMode Mode;

    /// <summary>The comparison policy used to verify the resolution round trip.</summary>
    public UriComparisonLevel ComparisonLevel;

    /// <summary>The optional explicit profile used by scheme-profile comparison.</summary>
    public UriSchemeProfile? Profile;

    /// <summary>Initializes relative-path, exact relativization with default processing limits.</summary>
    public UriRelativizationOptions()
    {
        Limits = new OperationLimits();
        Mode = UriRelativizationMode.RelativePath;
        ComparisonLevel = UriComparisonLevel.Exact;
        Profile = null;
    }
}
