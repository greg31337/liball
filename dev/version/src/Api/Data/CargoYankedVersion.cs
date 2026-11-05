namespace version;

/// <summary>
/// Represents a cargo yanked version.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class CargoYankedVersion
{
    /// <summary>
    /// Stores the <c>identity</c> value.
    /// </summary>
    public CargoPackageVersionIdentity identity;
    /// <summary>
    /// Stores the <c>reason</c> value.
    /// </summary>
    public string reason;

    /// <summary>
    /// Initializes a new Cargo Yanked Version instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="identityValue">The identity value used by the operation.</param>
    /// <param name="reasonValue">The reason value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public CargoYankedVersion(CargoPackageVersionIdentity identityValue, string reasonValue)
    {
        System.ArgumentNullException.ThrowIfNull(identityValue);

        identity = identityValue;

        if (reasonValue == null)
        {
            reason = "";
        }
        else
        {
            reason = reasonValue;
        }
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="packageNameText">The package name text to process.</param>
    /// <param name="versionText">The version text to process.</param>
    /// <param name="reason">The reason used by the operation.</param>
    /// <returns>The resulting cargo yanked version.</returns>
    internal static CargoYankedVersion Create(string packageNameText, string versionText, string reason)
    {
        return new CargoYankedVersion(CargoPackageVersionIdentity.Create(packageNameText, versionText), reason);
    }

    /// <summary>
    /// Gets an identity.
    /// </summary>
    /// <returns>The resulting cargo package version identity.</returns>
    internal CargoPackageVersionIdentity GetIdentity()
    {
        return identity;
    }

    /// <summary>
    /// Gets a package name.
    /// </summary>
    /// <returns>The resulting cargo package name.</returns>
    internal CargoPackageName GetPackageName()
    {
        return identity.GetPackageName();
    }

    /// <summary>
    /// Gets a version text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetVersionText()
    {
        return identity.GetVersionText();
    }

    /// <summary>
    /// Reports whether this value has reason.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasReason()
    {
        return reason.Length > 0;
    }

    /// <summary>
    /// Gets a reason.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetReason()
    {
        return reason;
    }
}
