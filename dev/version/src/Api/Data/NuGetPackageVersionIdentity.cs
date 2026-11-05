namespace version;

/// <summary>
/// Represents a nuget package version identity.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class NuGetPackageVersionIdentity
{
    /// <summary>
    /// Stores the <c>packageId</c> value.
    /// </summary>
    public NuGetPackageId packageId;
    /// <summary>
    /// Stores the <c>version</c> value.
    /// </summary>
    public UniversalVersion version;
    /// <summary>
    /// Stores the <c>normalizedVersionText</c> value.
    /// </summary>
    public string normalizedVersionText;

    /// <summary>
    /// Initializes a new NuGet Package Version Identity instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="packageIdValue">The package id value used by the operation.</param>
    /// <param name="versionValue">The version value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    public NuGetPackageVersionIdentity(NuGetPackageId packageIdValue, UniversalVersion versionValue)
    {
        System.ArgumentNullException.ThrowIfNull(packageIdValue);
        System.ArgumentNullException.ThrowIfNull(versionValue);

        if (versionValue.GetDialect() != VersionDialect.NuGet)
        {
            throw new System.ArgumentException("NuGet package version identity requires a NuGet version.");
        }

        packageId = packageIdValue;
        version = versionValue;
        normalizedVersionText = NuGetVersionFormatter.Normalize(versionValue);
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="packageIdText">The package id text to process.</param>
    /// <param name="versionText">The version text to process.</param>
    /// <returns>The resulting nuget package version identity.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    internal static NuGetPackageVersionIdentity Create(string packageIdText, string versionText)
    {
        System.ArgumentNullException.ThrowIfNull(versionText);

        VersionParseResult result = NuGetVersionParser.Parse(versionText);
        if (result.IsFailure())
        {
            throw new System.ArgumentException("Invalid NuGet package version.");
        }

        return new NuGetPackageVersionIdentity(new NuGetPackageId(packageIdText), result.GetVersion());
    }

    /// <summary>
    /// Gets a package id.
    /// </summary>
    /// <returns>The resulting nuget package id.</returns>
    internal NuGetPackageId GetPackageId()
    {
        return packageId;
    }

    /// <summary>
    /// Gets a version.
    /// </summary>
    /// <returns>The resulting universal version.</returns>
    internal UniversalVersion GetVersion()
    {
        return version;
    }

    /// <summary>
    /// Gets a normalized version text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetNormalizedVersionText()
    {
        return normalizedVersionText;
    }

    /// <summary>
    /// Gets an identity text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetIdentityText()
    {
        return packageId.GetNormalizedValue() + " " + normalizedVersionText;
    }

    /// <summary>
    /// Reports whether this value has same identity.
    /// </summary>
    /// <param name="other">The other used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool HasSameIdentity(NuGetPackageVersionIdentity other)
    {
        System.ArgumentNullException.ThrowIfNull(other);

        return packageId.EqualsId(other.GetPackageId())
            && normalizedVersionText == other.GetNormalizedVersionText();
    }
}
