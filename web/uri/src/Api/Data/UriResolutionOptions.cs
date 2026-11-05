namespace uri;

/// <summary>
/// Defines options shared by URI resolution operations.
/// </summary>
public class UriResolutionOptions
{
    /// <summary>The processing limits for the operation.</summary>
    public OperationLimits Limits;

    /// <summary>The strict or identical-scheme-compatible resolution behavior.</summary>
    public UriResolutionMode Mode;

    /// <summary>Initializes strict resolution with default processing limits.</summary>
    public UriResolutionOptions()
    {
        Limits = new OperationLimits();
        Mode = UriResolutionMode.Strict;
    }
}
