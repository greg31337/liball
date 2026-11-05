namespace version;

/// <summary>
/// Represents a nuget target framework.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class NuGetTargetFramework
{
    /// <summary>
    /// Stores the <c>originalText</c> value.
    /// </summary>
    public string originalText;
    /// <summary>
    /// Stores the <c>normalizedText</c> value.
    /// </summary>
    public string normalizedText;
    /// <summary>
    /// Stores the <c>kind</c> value.
    /// </summary>
    public NuGetTargetFrameworkKind kind;
    /// <summary>
    /// Stores the <c>versionText</c> value.
    /// </summary>
    public string versionText;
    /// <summary>
    /// Stores the <c>platformText</c> value.
    /// </summary>
    public string platformText;
    /// <summary>
    /// Stores the <c>platformVersionText</c> value.
    /// </summary>
    public string platformVersionText;
    /// <summary>
    /// Stores the <c>profileText</c> value.
    /// </summary>
    public string profileText;
    /// <summary>
    /// Stores the <c>portableFrameworks</c> value.
    /// </summary>
    public string[] portableFrameworks;

    /// <summary>
    /// Initializes a new NuGet Target Framework instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="originalTextValue">The original text value to process.</param>
    /// <param name="normalizedTextValue">The normalized text value to process.</param>
    /// <param name="kindValue">The kind value used by the operation.</param>
    /// <param name="versionTextValue">The version text value to process.</param>
    /// <param name="platformTextValue">The platform text value to process.</param>
    /// <param name="platformVersionTextValue">The platform version text value to process.</param>
    /// <param name="profileTextValue">The profile text value to process.</param>
    /// <param name="portableFrameworkValues">The portable framework values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public NuGetTargetFramework(
        string originalTextValue,
        string normalizedTextValue,
        NuGetTargetFrameworkKind kindValue,
        string versionTextValue,
        string platformTextValue,
        string platformVersionTextValue,
        string profileTextValue,
        string[] portableFrameworkValues)
    {
        System.ArgumentNullException.ThrowIfNull(originalTextValue);
        System.ArgumentNullException.ThrowIfNull(normalizedTextValue);
        System.ArgumentNullException.ThrowIfNull(versionTextValue);
        System.ArgumentNullException.ThrowIfNull(platformTextValue);
        System.ArgumentNullException.ThrowIfNull(platformVersionTextValue);
        System.ArgumentNullException.ThrowIfNull(profileTextValue);
        System.ArgumentNullException.ThrowIfNull(portableFrameworkValues);
        ValidateKind(kindValue);

        originalText = originalTextValue;
        normalizedText = normalizedTextValue;
        kind = kindValue;
        versionText = versionTextValue;
        platformText = platformTextValue;
        platformVersionText = platformVersionTextValue;
        profileText = profileTextValue;
        portableFrameworks = CopyStrings(portableFrameworkValues);
    }

    /// <summary>
    /// Gets an original text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetOriginalText()
    {
        return originalText;
    }

    /// <summary>
    /// Gets a normalized text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetNormalizedText()
    {
        return normalizedText;
    }

    /// <summary>
    /// Gets a kind.
    /// </summary>
    /// <returns>The resulting nuget target framework kind.</returns>
    internal NuGetTargetFrameworkKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Gets a version text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetVersionText()
    {
        return versionText;
    }

    /// <summary>
    /// Reports whether this value has platform.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasPlatform()
    {
        return platformText.Length > 0;
    }

    /// <summary>
    /// Gets a platform text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetPlatformText()
    {
        return platformText;
    }

    /// <summary>
    /// Gets a platform version text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetPlatformVersionText()
    {
        return platformVersionText;
    }

    /// <summary>
    /// Reports whether this value has platform version.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasPlatformVersion()
    {
        return platformVersionText.Length > 0;
    }

    /// <summary>
    /// Reports whether portable.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsPortable()
    {
        return kind == NuGetTargetFrameworkKind.Portable;
    }

    /// <summary>
    /// Gets a profile text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetProfileText()
    {
        return profileText;
    }

    /// <summary>
    /// Gets a portable framework count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetPortableFrameworkCount()
    {
        return portableFrameworks.Length;
    }

    /// <summary>
    /// Gets a portable framework.
    /// </summary>
    /// <param name="frameworkIndex">The zero-based framework index.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal string GetPortableFramework(int frameworkIndex)
    {
        if (frameworkIndex < 0 || frameworkIndex >= portableFrameworks.Length)
        {
            throw new System.ArgumentOutOfRangeException("frameworkIndex");
        }

        return portableFrameworks[frameworkIndex];
    }

    /// <summary>
    /// Gets a portable frameworks.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal string[] GetPortableFrameworks()
    {
        return CopyStrings(portableFrameworks);
    }

    internal static string[] CopyStrings(string[] values)
    {
        string[] copy = new string[values.Length];
        int index = 0;

        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new System.ArgumentException("Target framework arrays cannot contain null values.");
            }

            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }

    internal static void ValidateKind(NuGetTargetFrameworkKind kindValue)
    {
        if (kindValue == NuGetTargetFrameworkKind.Unsupported)
        {
            return;
        }

        if (kindValue == NuGetTargetFrameworkKind.NetFramework)
        {
            return;
        }

        if (kindValue == NuGetTargetFrameworkKind.NetCoreApp)
        {
            return;
        }

        if (kindValue == NuGetTargetFrameworkKind.NetStandard)
        {
            return;
        }

        if (kindValue == NuGetTargetFrameworkKind.Net)
        {
            return;
        }

        if (kindValue == NuGetTargetFrameworkKind.Portable)
        {
            return;
        }

        if (kindValue == NuGetTargetFrameworkKind.Windows)
        {
            return;
        }

        if (kindValue == NuGetTargetFrameworkKind.WindowsPhone)
        {
            return;
        }

        if (kindValue == NuGetTargetFrameworkKind.Silverlight)
        {
            return;
        }

        if (kindValue == NuGetTargetFrameworkKind.Uap)
        {
            return;
        }

        throw new System.ArgumentOutOfRangeException("kindValue");
    }
}
