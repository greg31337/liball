namespace version;

/// <summary>
/// Represents a nuget package metadata.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class NuGetPackageMetadata
{
    /// <summary>
    /// Stores the <c>identity</c> value.
    /// </summary>
    public NuGetPackageVersionIdentity identity;
    /// <summary>
    /// Stores the <c>title</c> value.
    /// </summary>
    public string title;
    /// <summary>
    /// Stores the <c>authors</c> value.
    /// </summary>
    public string authors;
    /// <summary>
    /// Stores the <c>owners</c> value.
    /// </summary>
    public string owners;
    /// <summary>
    /// Stores the <c>description</c> value.
    /// </summary>
    public string description;
    /// <summary>
    /// Stores the <c>tags</c> value.
    /// </summary>
    public string tags;
    /// <summary>
    /// Stores the <c>projectUrl</c> value.
    /// </summary>
    public string projectUrl;
    /// <summary>
    /// Stores the <c>licenseUrl</c> value.
    /// </summary>
    public string licenseUrl;
    /// <summary>
    /// Stores the <c>minClientVersion</c> value.
    /// </summary>
    public string minClientVersion;
    /// <summary>
    /// Stores the <c>requireLicenseAcceptance</c> value.
    /// </summary>
    public bool requireLicenseAcceptance;
    /// <summary>
    /// Stores the <c>developmentDependency</c> value.
    /// </summary>
    public bool developmentDependency;
    /// <summary>
    /// Stores the <c>dependencyGroups</c> value.
    /// </summary>
    public NuGetDependencyGroup[] dependencyGroups;

    /// <summary>
    /// Initializes a new NuGet Package Metadata instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="identityValue">The identity value used by the operation.</param>
    /// <param name="titleValue">The title value used by the operation.</param>
    /// <param name="authorsValue">The authors value used by the operation.</param>
    /// <param name="ownersValue">The owners value used by the operation.</param>
    /// <param name="descriptionValue">The description value used by the operation.</param>
    /// <param name="tagsValue">The tags value used by the operation.</param>
    /// <param name="projectUrlValue">The project url value used by the operation.</param>
    /// <param name="licenseUrlValue">The license url value used by the operation.</param>
    /// <param name="minClientVersionValue">The min client version value used by the operation.</param>
    /// <param name="requireLicenseAcceptanceValue">Whether to enable require license acceptance value.</param>
    /// <param name="developmentDependencyValue">Whether to enable development dependency value.</param>
    /// <param name="dependencyGroupValues">The dependency group values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public NuGetPackageMetadata(
        NuGetPackageVersionIdentity identityValue,
        string titleValue,
        string authorsValue,
        string ownersValue,
        string descriptionValue,
        string tagsValue,
        string projectUrlValue,
        string licenseUrlValue,
        string minClientVersionValue,
        bool requireLicenseAcceptanceValue,
        bool developmentDependencyValue,
        NuGetDependencyGroup[] dependencyGroupValues)
    {
        System.ArgumentNullException.ThrowIfNull(identityValue);
        System.ArgumentNullException.ThrowIfNull(titleValue);
        System.ArgumentNullException.ThrowIfNull(authorsValue);
        System.ArgumentNullException.ThrowIfNull(ownersValue);
        System.ArgumentNullException.ThrowIfNull(descriptionValue);
        System.ArgumentNullException.ThrowIfNull(tagsValue);
        System.ArgumentNullException.ThrowIfNull(projectUrlValue);
        System.ArgumentNullException.ThrowIfNull(licenseUrlValue);
        System.ArgumentNullException.ThrowIfNull(minClientVersionValue);
        System.ArgumentNullException.ThrowIfNull(dependencyGroupValues);

        identity = identityValue;
        title = titleValue;
        authors = authorsValue;
        owners = ownersValue;
        description = descriptionValue;
        tags = tagsValue;
        projectUrl = projectUrlValue;
        licenseUrl = licenseUrlValue;
        minClientVersion = minClientVersionValue;
        requireLicenseAcceptance = requireLicenseAcceptanceValue;
        developmentDependency = developmentDependencyValue;
        dependencyGroups = CopyDependencyGroups(dependencyGroupValues);
    }

    /// <summary>
    /// Gets an identity.
    /// </summary>
    /// <returns>The resulting nuget package version identity.</returns>
    internal NuGetPackageVersionIdentity GetIdentity()
    {
        return identity;
    }

    /// <summary>
    /// Gets a title.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetTitle()
    {
        return title;
    }

    /// <summary>
    /// Reports whether this value has title.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasTitle()
    {
        return title.Length > 0;
    }

    /// <summary>
    /// Gets an authors.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetAuthors()
    {
        return authors;
    }

    /// <summary>
    /// Gets an owners.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetOwners()
    {
        return owners;
    }

    /// <summary>
    /// Gets a description.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetDescription()
    {
        return description;
    }

    /// <summary>
    /// Gets a tags.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetTags()
    {
        return tags;
    }

    /// <summary>
    /// Gets a project url.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetProjectUrl()
    {
        return projectUrl;
    }

    /// <summary>
    /// Gets a license url.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetLicenseUrl()
    {
        return licenseUrl;
    }

    /// <summary>
    /// Gets a min client version.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetMinClientVersion()
    {
        return minClientVersion;
    }

    /// <summary>
    /// Reports whether package installation requires explicit license acceptance.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool RequiresLicenseAcceptance()
    {
        return requireLicenseAcceptance;
    }

    /// <summary>
    /// Reports whether development dependency.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsDevelopmentDependency()
    {
        return developmentDependency;
    }

    /// <summary>
    /// Gets a dependency group count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetDependencyGroupCount()
    {
        return dependencyGroups.Length;
    }

    /// <summary>
    /// Gets a dependency group.
    /// </summary>
    /// <param name="dependencyGroupIndex">The zero-based dependency group index.</param>
    /// <returns>The resulting nuget dependency group.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal NuGetDependencyGroup GetDependencyGroup(int dependencyGroupIndex)
    {
        if (dependencyGroupIndex < 0 || dependencyGroupIndex >= dependencyGroups.Length)
        {
            throw new System.ArgumentOutOfRangeException("dependencyGroupIndex");
        }

        return dependencyGroups[dependencyGroupIndex];
    }

    /// <summary>
    /// Gets a dependency groups.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal NuGetDependencyGroup[] GetDependencyGroups()
    {
        return CopyDependencyGroups(dependencyGroups);
    }

    internal static NuGetDependencyGroup[] CopyDependencyGroups(NuGetDependencyGroup[] dependencyGroupValues)
    {
        NuGetDependencyGroup[] copy = new NuGetDependencyGroup[dependencyGroupValues.Length];
        int index = 0;

        while (index < dependencyGroupValues.Length)
        {
            if (dependencyGroupValues[index] == null)
            {
                throw new System.ArgumentException("NuGet dependency group arrays cannot contain null values.");
            }

            copy[index] = dependencyGroupValues[index];
            index = index + 1;
        }

        return copy;
    }
}
