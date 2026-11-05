namespace version;

/// <summary>
/// Represents a nuget source package metadata.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class NuGetSourcePackageMetadata
{
    /// <summary>
    /// Stores the <c>source</c> value.
    /// </summary>
    public NuGetSourceIdentity source;
    /// <summary>
    /// Stores the <c>packageMetadata</c> value.
    /// </summary>
    public NuGetPackageMetadata packageMetadata;
    /// <summary>
    /// Stores the <c>listedState</c> value.
    /// </summary>
    public NuGetListedState listedState;
    /// <summary>
    /// Stores the <c>availability</c> value.
    /// </summary>
    public NuGetPackageAvailability availability;
    /// <summary>
    /// Stores the <c>deprecationMetadata</c> value.
    /// </summary>
    public NuGetPackageDeprecationMetadata deprecationMetadata;
    /// <summary>
    /// Stores the <c>vulnerabilities</c> value.
    /// </summary>
    public NuGetPackageVulnerabilityMetadata[] vulnerabilities;

    /// <summary>
    /// Initializes a new NuGet Source Package Metadata instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="sourceValue">The source value used by the operation.</param>
    /// <param name="packageMetadataValue">The package metadata value used by the operation.</param>
    /// <param name="listedStateValue">The listed state value used by the operation.</param>
    /// <param name="availabilityValue">The availability value used by the operation.</param>
    /// <param name="deprecationMetadataValue">The deprecation metadata value used by the operation.</param>
    /// <param name="vulnerabilityValues">The vulnerability values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public NuGetSourcePackageMetadata(
        NuGetSourceIdentity sourceValue,
        NuGetPackageMetadata packageMetadataValue,
        NuGetListedState listedStateValue,
        NuGetPackageAvailability availabilityValue,
        NuGetPackageDeprecationMetadata deprecationMetadataValue,
        NuGetPackageVulnerabilityMetadata[] vulnerabilityValues)
    {
        System.ArgumentNullException.ThrowIfNull(sourceValue);
        System.ArgumentNullException.ThrowIfNull(packageMetadataValue);
        System.ArgumentNullException.ThrowIfNull(deprecationMetadataValue);
        System.ArgumentNullException.ThrowIfNull(vulnerabilityValues);
        ValidateListedState(listedStateValue);
        ValidateAvailability(availabilityValue);

        source = sourceValue;
        packageMetadata = packageMetadataValue;
        listedState = listedStateValue;
        availability = availabilityValue;
        deprecationMetadata = deprecationMetadataValue;
        vulnerabilities = CopyVulnerabilities(vulnerabilityValues);
    }

    /// <summary>
    /// Creates an available.
    /// </summary>
    /// <param name="source">The source used by the operation.</param>
    /// <param name="packageMetadata">The package metadata used by the operation.</param>
    /// <param name="listedState">The listed state used by the operation.</param>
    /// <returns>The resulting nuget source package metadata.</returns>
    internal static NuGetSourcePackageMetadata CreateAvailable(
        NuGetSourceIdentity source,
        NuGetPackageMetadata packageMetadata,
        NuGetListedState listedState)
    {
        return new NuGetSourcePackageMetadata(
            source,
            packageMetadata,
            listedState,
            NuGetPackageAvailability.Available,
            NuGetPackageDeprecationMetadata.CreateNotDeprecated(),
            new NuGetPackageVulnerabilityMetadata[0]);
    }

    /// <summary>
    /// Gets a source.
    /// </summary>
    /// <returns>The resulting nuget source identity.</returns>
    internal NuGetSourceIdentity GetSource()
    {
        return source;
    }

    /// <summary>
    /// Gets a package metadata.
    /// </summary>
    /// <returns>The resulting nuget package metadata.</returns>
    internal NuGetPackageMetadata GetPackageMetadata()
    {
        return packageMetadata;
    }

    /// <summary>
    /// Gets an identity.
    /// </summary>
    /// <returns>The resulting nuget package version identity.</returns>
    internal NuGetPackageVersionIdentity GetIdentity()
    {
        return packageMetadata.GetIdentity();
    }

    /// <summary>
    /// Gets a listed state.
    /// </summary>
    /// <returns>The resulting nuget listed state.</returns>
    internal NuGetListedState GetListedState()
    {
        return listedState;
    }

    /// <summary>
    /// Reports whether listed.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsListed()
    {
        return listedState == NuGetListedState.Listed;
    }

    /// <summary>
    /// Gets an availability.
    /// </summary>
    /// <returns>The resulting nuget package availability.</returns>
    internal NuGetPackageAvailability GetAvailability()
    {
        return availability;
    }

    /// <summary>
    /// Reports whether available.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsAvailable()
    {
        return availability == NuGetPackageAvailability.Available;
    }

    /// <summary>
    /// Gets a deprecation metadata.
    /// </summary>
    /// <returns>The resulting nuget package deprecation metadata.</returns>
    internal NuGetPackageDeprecationMetadata GetDeprecationMetadata()
    {
        return deprecationMetadata;
    }

    /// <summary>
    /// Gets a vulnerability count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetVulnerabilityCount()
    {
        return vulnerabilities.Length;
    }

    /// <summary>
    /// Reports whether this value has vulnerabilities.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasVulnerabilities()
    {
        return vulnerabilities.Length > 0;
    }

    /// <summary>
    /// Gets a vulnerability.
    /// </summary>
    /// <param name="vulnerabilityIndex">The zero-based vulnerability index.</param>
    /// <returns>The resulting nuget package vulnerability metadata.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal NuGetPackageVulnerabilityMetadata GetVulnerability(int vulnerabilityIndex)
    {
        if (vulnerabilityIndex < 0 || vulnerabilityIndex >= vulnerabilities.Length)
        {
            throw new System.ArgumentOutOfRangeException("vulnerabilityIndex");
        }

        return vulnerabilities[vulnerabilityIndex];
    }

    /// <summary>
    /// Gets a vulnerabilities.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal NuGetPackageVulnerabilityMetadata[] GetVulnerabilities()
    {
        return CopyVulnerabilities(vulnerabilities);
    }

    /// <summary>
    /// Reports whether rejected by.
    /// </summary>
    /// <param name="policy">The policy used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool IsRejectedBy(NuGetPackageRejectionPolicy policy)
    {
        System.ArgumentNullException.ThrowIfNull(policy);

        return policy.ShouldReject(this);
    }

    internal static NuGetPackageVulnerabilityMetadata[] CopyVulnerabilities(NuGetPackageVulnerabilityMetadata[] vulnerabilityValues)
    {
        NuGetPackageVulnerabilityMetadata[] copy = new NuGetPackageVulnerabilityMetadata[vulnerabilityValues.Length];
        int index = 0;

        while (index < vulnerabilityValues.Length)
        {
            if (vulnerabilityValues[index] == null)
            {
                throw new System.ArgumentException("NuGet vulnerability arrays cannot contain null values.");
            }

            copy[index] = vulnerabilityValues[index];
            index = index + 1;
        }

        return copy;
    }

    internal static void ValidateListedState(NuGetListedState listedStateValue)
    {
        if (listedStateValue == NuGetListedState.Unknown)
        {
            return;
        }

        if (listedStateValue == NuGetListedState.Listed)
        {
            return;
        }

        if (listedStateValue == NuGetListedState.Unlisted)
        {
            return;
        }

        throw new System.ArgumentOutOfRangeException("listedStateValue");
    }

    internal static void ValidateAvailability(NuGetPackageAvailability availabilityValue)
    {
        if (availabilityValue == NuGetPackageAvailability.Unknown)
        {
            return;
        }

        if (availabilityValue == NuGetPackageAvailability.Available)
        {
            return;
        }

        if (availabilityValue == NuGetPackageAvailability.Unavailable)
        {
            return;
        }

        throw new System.ArgumentOutOfRangeException("availabilityValue");
    }
}
