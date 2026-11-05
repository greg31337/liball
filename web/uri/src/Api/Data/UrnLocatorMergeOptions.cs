namespace uri;

/// <summary>
/// Defines options shared by URN locator merge operations.
/// </summary>
public class UrnLocatorMergeOptions
{
    /// <summary>The processing limits for the operation.</summary>
    public OperationLimits Limits;

    /// <summary>The policy used when both the locator and URN have a query.</summary>
    public UrnLocatorCollisionMode QueryCollisionMode;

    /// <summary>The policy used when both the locator and URN have a fragment.</summary>
    public UrnLocatorCollisionMode FragmentCollisionMode;

    /// <summary>Initializes locator merging with collision rejection.</summary>
    public UrnLocatorMergeOptions()
    {
        Limits = new OperationLimits();
        QueryCollisionMode = UrnLocatorCollisionMode.Reject;
        FragmentCollisionMode = UrnLocatorCollisionMode.Reject;
    }
}
