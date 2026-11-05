namespace version;

/// <summary>
/// Represents a nuget dependency.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class NuGetDependency
{
    /// <summary>
    /// Stores the <c>packageId</c> value.
    /// </summary>
    public NuGetPackageId packageId;
    /// <summary>
    /// Stores the <c>versionRange</c> value.
    /// </summary>
    public VersionRange versionRange;
    /// <summary>
    /// Stores the <c>versionRangeText</c> value.
    /// </summary>
    public string versionRangeText;

    /// <summary>
    /// Initializes a new NuGet Dependency instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="packageIdValue">The package id value used by the operation.</param>
    /// <param name="versionRangeValue">The version range value used by the operation.</param>
    /// <param name="versionRangeTextValue">The version range text value to process.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public NuGetDependency(NuGetPackageId packageIdValue, VersionRange versionRangeValue, string versionRangeTextValue)
    {
        System.ArgumentNullException.ThrowIfNull(packageIdValue);
        System.ArgumentNullException.ThrowIfNull(versionRangeValue);
        System.ArgumentNullException.ThrowIfNull(versionRangeTextValue);

        packageId = packageIdValue;
        versionRange = versionRangeValue;
        versionRangeText = versionRangeTextValue;
    }

    /// <summary>
    /// Creates an any version.
    /// </summary>
    /// <param name="packageIdText">The package id text to process.</param>
    /// <returns>The resulting nuget dependency.</returns>
    internal static NuGetDependency CreateAnyVersion(string packageIdText)
    {
        return new NuGetDependency(new NuGetPackageId(packageIdText), VersionRange.CreateUniversal(), "");
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="packageIdText">The package id text to process.</param>
    /// <param name="versionRangeText">The version range text to process.</param>
    /// <returns>The resulting nuget dependency.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    internal static NuGetDependency Create(string packageIdText, string versionRangeText)
    {
        System.ArgumentNullException.ThrowIfNull(versionRangeText);

        VersionRangeParseResult rangeResult = NuGetRangeParser.Parse(versionRangeText);
        if (rangeResult.IsFailure())
        {
            throw new System.ArgumentException("Invalid NuGet dependency version range.");
        }

        return new NuGetDependency(new NuGetPackageId(packageIdText), rangeResult.GetRange(), versionRangeText);
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
    /// Gets a version range.
    /// </summary>
    /// <returns>The resulting version range.</returns>
    internal VersionRange GetVersionRange()
    {
        return versionRange;
    }

    /// <summary>
    /// Gets a version range text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetVersionRangeText()
    {
        return versionRangeText;
    }

    /// <summary>
    /// Reports whether version text.
    /// </summary>
    /// <param name="versionText">The version text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool AllowsVersionText(string versionText)
    {
        return versionRange.ContainsText(versionText, VersionDialect.NuGet);
    }
}
