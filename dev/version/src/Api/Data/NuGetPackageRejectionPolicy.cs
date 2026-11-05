namespace version;

/// <summary>
/// Represents a nuget package rejection policy.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class NuGetPackageRejectionPolicy
{
    /// <summary>
    /// Stores the <c>rejectUnavailablePackages</c> value.
    /// </summary>
    public bool rejectUnavailablePackages;
    /// <summary>
    /// Stores the <c>rejectUnlistedPackages</c> value.
    /// </summary>
    public bool rejectUnlistedPackages;
    /// <summary>
    /// Stores the <c>rejectDeprecatedPackages</c> value.
    /// </summary>
    public bool rejectDeprecatedPackages;
    /// <summary>
    /// Stores the <c>rejectVulnerablePackages</c> value.
    /// </summary>
    public bool rejectVulnerablePackages;
    /// <summary>
    /// Stores the <c>minimumRejectedVulnerabilitySeverity</c> value.
    /// </summary>
    public NuGetPackageVulnerabilitySeverity minimumRejectedVulnerabilitySeverity;

    /// <summary>
    /// Initializes a new NuGet Package Rejection Policy instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    public NuGetPackageRejectionPolicy()
    {
        rejectUnavailablePackages = true;
        rejectUnlistedPackages = false;
        rejectDeprecatedPackages = false;
        rejectVulnerablePackages = false;
        minimumRejectedVulnerabilitySeverity = NuGetPackageVulnerabilitySeverity.Low;
    }

    /// <summary>
    /// Creates a default.
    /// </summary>
    /// <returns>The resulting nuget package rejection policy.</returns>
    internal static NuGetPackageRejectionPolicy CreateDefault()
    {
        return new NuGetPackageRejectionPolicy();
    }

    /// <summary>
    /// Creates a strict.
    /// </summary>
    /// <returns>The resulting nuget package rejection policy.</returns>
    internal static NuGetPackageRejectionPolicy CreateStrict()
    {
        NuGetPackageRejectionPolicy policy = new NuGetPackageRejectionPolicy();
        policy.SetRejectUnlistedPackages(true);
        policy.SetRejectDeprecatedPackages(true);
        policy.SetRejectVulnerablePackages(true);
        policy.SetMinimumRejectedVulnerabilitySeverity(NuGetPackageVulnerabilitySeverity.Low);
        return policy;
    }

    /// <summary>
    /// Reports whether unavailable packages are rejected by this policy.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool ShouldRejectUnavailablePackages()
    {
        return rejectUnavailablePackages;
    }

    /// <summary>
    /// Updates the reject unavailable packages setting.
    /// </summary>
    /// <param name="rejectUnavailablePackagesValue">Whether to enable reject unavailable packages value.</param>
    internal void SetRejectUnavailablePackages(bool rejectUnavailablePackagesValue)
    {
        rejectUnavailablePackages = rejectUnavailablePackagesValue;
    }

    /// <summary>
    /// Reports whether unlisted packages are rejected by this policy.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool ShouldRejectUnlistedPackages()
    {
        return rejectUnlistedPackages;
    }

    /// <summary>
    /// Updates the reject unlisted packages setting.
    /// </summary>
    /// <param name="rejectUnlistedPackagesValue">Whether to enable reject unlisted packages value.</param>
    internal void SetRejectUnlistedPackages(bool rejectUnlistedPackagesValue)
    {
        rejectUnlistedPackages = rejectUnlistedPackagesValue;
    }

    /// <summary>
    /// Reports whether deprecated packages are rejected by this policy.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool ShouldRejectDeprecatedPackages()
    {
        return rejectDeprecatedPackages;
    }

    /// <summary>
    /// Updates the reject deprecated packages setting.
    /// </summary>
    /// <param name="rejectDeprecatedPackagesValue">Whether to enable reject deprecated packages value.</param>
    internal void SetRejectDeprecatedPackages(bool rejectDeprecatedPackagesValue)
    {
        rejectDeprecatedPackages = rejectDeprecatedPackagesValue;
    }

    /// <summary>
    /// Reports whether vulnerable packages are rejected by this policy.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool ShouldRejectVulnerablePackages()
    {
        return rejectVulnerablePackages;
    }

    /// <summary>
    /// Updates the reject vulnerable packages setting.
    /// </summary>
    /// <param name="rejectVulnerablePackagesValue">Whether to enable reject vulnerable packages value.</param>
    internal void SetRejectVulnerablePackages(bool rejectVulnerablePackagesValue)
    {
        rejectVulnerablePackages = rejectVulnerablePackagesValue;
    }

    /// <summary>
    /// Gets a minimum rejected vulnerability severity.
    /// </summary>
    /// <returns>The resulting nuget package vulnerability severity.</returns>
    internal NuGetPackageVulnerabilitySeverity GetMinimumRejectedVulnerabilitySeverity()
    {
        return minimumRejectedVulnerabilitySeverity;
    }

    /// <summary>
    /// Updates the minimum rejected vulnerability severity setting.
    /// </summary>
    /// <param name="severity">The severity used by the operation.</param>
    internal void SetMinimumRejectedVulnerabilitySeverity(NuGetPackageVulnerabilitySeverity severity)
    {
        ValidateSeverity(severity);
        minimumRejectedVulnerabilitySeverity = severity;
    }

    /// <summary>
    /// Reports whether the supplied package metadata is rejected by this policy.
    /// </summary>
    /// <param name="packageMetadata">The package metadata used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool ShouldReject(NuGetSourcePackageMetadata packageMetadata)
    {
        System.ArgumentNullException.ThrowIfNull(packageMetadata);

        if (rejectUnavailablePackages && packageMetadata.GetAvailability() != NuGetPackageAvailability.Available)
        {
            return true;
        }

        if (rejectUnlistedPackages && packageMetadata.GetListedState() == NuGetListedState.Unlisted)
        {
            return true;
        }

        if (rejectDeprecatedPackages && packageMetadata.GetDeprecationMetadata().IsDeprecated())
        {
            return true;
        }

        if (rejectVulnerablePackages && HasRejectedVulnerability(packageMetadata))
        {
            return true;
        }

        return false;
    }

    internal bool HasRejectedVulnerability(NuGetSourcePackageMetadata packageMetadata)
    {
        int index = 0;

        while (index < packageMetadata.GetVulnerabilityCount())
        {
            if (SeverityRank(packageMetadata.GetVulnerability(index).GetSeverity()) >= SeverityRank(minimumRejectedVulnerabilitySeverity))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static int SeverityRank(NuGetPackageVulnerabilitySeverity severity)
    {
        ValidateSeverity(severity);

        if (severity == NuGetPackageVulnerabilitySeverity.Unknown)
        {
            return 0;
        }

        if (severity == NuGetPackageVulnerabilitySeverity.Low)
        {
            return 1;
        }

        if (severity == NuGetPackageVulnerabilitySeverity.Moderate)
        {
            return 2;
        }

        if (severity == NuGetPackageVulnerabilitySeverity.High)
        {
            return 3;
        }

        return 4;
    }

    internal static void ValidateSeverity(NuGetPackageVulnerabilitySeverity severity)
    {
        if (severity == NuGetPackageVulnerabilitySeverity.Unknown)
        {
            return;
        }

        if (severity == NuGetPackageVulnerabilitySeverity.Low)
        {
            return;
        }

        if (severity == NuGetPackageVulnerabilitySeverity.Moderate)
        {
            return;
        }

        if (severity == NuGetPackageVulnerabilitySeverity.High)
        {
            return;
        }

        if (severity == NuGetPackageVulnerabilitySeverity.Critical)
        {
            return;
        }

        throw new System.ArgumentOutOfRangeException("severity");
    }
}
