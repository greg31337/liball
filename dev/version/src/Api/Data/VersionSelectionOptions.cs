namespace version;

/// <summary>
/// Configures version selection behavior.
/// </summary>
/// <remarks>
/// Configure behavior through the public fields before passing the value to an operation.
/// </remarks>
public class VersionSelectionOptions
{
    /// <summary>
    /// Stores the <c>prereleasePolicy</c> value.
    /// </summary>
    public PrereleasePolicy prereleasePolicy;

    /// <summary>
    /// Initializes a new Version Selection Options instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    public VersionSelectionOptions()
    {
        prereleasePolicy = PrereleasePolicy.Default;
    }

    /// <summary>
    /// Creates a default.
    /// </summary>
    /// <returns>The resulting version selection options.</returns>
    internal static VersionSelectionOptions CreateDefault()
    {
        return new VersionSelectionOptions();
    }

    /// <summary>
    /// Creates a with prerelease policy.
    /// </summary>
    /// <param name="policy">The policy used by the operation.</param>
    /// <returns>The resulting version selection options.</returns>
    internal static VersionSelectionOptions CreateWithPrereleasePolicy(PrereleasePolicy policy)
    {
        VersionSelectionOptions options = new VersionSelectionOptions();
        options.SetPrereleasePolicy(policy);
        return options;
    }

    /// <summary>
    /// Gets a prerelease policy.
    /// </summary>
    /// <returns>The resulting prerelease policy.</returns>
    internal PrereleasePolicy GetPrereleasePolicy()
    {
        return prereleasePolicy;
    }

    /// <summary>
    /// Updates the prerelease policy setting.
    /// </summary>
    /// <param name="policy">The policy used by the operation.</param>
    internal void SetPrereleasePolicy(PrereleasePolicy policy)
    {
        ValidatePrereleasePolicy(policy);
        prereleasePolicy = policy;
    }

    internal static void ValidatePrereleasePolicy(PrereleasePolicy policy)
    {
        if (policy == PrereleasePolicy.Default)
        {
            return;
        }

        if (policy == PrereleasePolicy.Include)
        {
            return;
        }

        if (policy == PrereleasePolicy.Exclude)
        {
            return;
        }

        if (policy == PrereleasePolicy.ExplicitOnly)
        {
            return;
        }

        if (policy == PrereleasePolicy.AutoIfNoStableCandidate)
        {
            return;
        }

        throw new System.ArgumentOutOfRangeException("policy");
    }
}
