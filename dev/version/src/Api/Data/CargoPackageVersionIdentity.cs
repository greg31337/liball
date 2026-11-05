namespace version;

/// <summary>
/// Represents a cargo package version identity.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class CargoPackageVersionIdentity
{
    /// <summary>
    /// Stores the <c>packageName</c> value.
    /// </summary>
    public CargoPackageName packageName;
    /// <summary>
    /// Stores the <c>version</c> value.
    /// </summary>
    public UniversalVersion version;

    /// <summary>
    /// Initializes a new Cargo Package Version Identity instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="packageNameValue">The package name value used by the operation.</param>
    /// <param name="versionValue">The version value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    public CargoPackageVersionIdentity(CargoPackageName packageNameValue, UniversalVersion versionValue)
    {
        System.ArgumentNullException.ThrowIfNull(packageNameValue);
        System.ArgumentNullException.ThrowIfNull(versionValue);

        if (versionValue.GetDialect() != VersionDialect.Cargo)
        {
            throw new System.ArgumentException("Cargo package version identity requires a Cargo version.");
        }

        packageName = packageNameValue;
        version = versionValue;
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="packageNameText">The package name text to process.</param>
    /// <param name="versionText">The version text to process.</param>
    /// <returns>The resulting cargo package version identity.</returns>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    internal static CargoPackageVersionIdentity Create(string packageNameText, string versionText)
    {
        VersionParseResult result = CargoVersions.Parse(versionText);
        if (result.IsFailure())
        {
            throw new System.ArgumentException("Invalid Cargo package version.");
        }

        return new CargoPackageVersionIdentity(new CargoPackageName(packageNameText), result.GetVersion());
    }

    /// <summary>
    /// Gets a package name.
    /// </summary>
    /// <returns>The resulting cargo package name.</returns>
    internal CargoPackageName GetPackageName()
    {
        return packageName;
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
    /// Gets a version text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetVersionText()
    {
        return version.GetOriginalText();
    }

    /// <summary>
    /// Gets an identity text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetIdentityText()
    {
        return packageName.GetValue() + " " + version.GetOriginalText();
    }

    /// <summary>
    /// Reports whether this value has same identity.
    /// </summary>
    /// <param name="other">The other used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool HasSameIdentity(CargoPackageVersionIdentity other)
    {
        System.ArgumentNullException.ThrowIfNull(other);

        return packageName.EqualsName(other.GetPackageName())
            && CargoVersions.Compare(version.GetOriginalText(), other.GetVersionText()) == 0;
    }
}
