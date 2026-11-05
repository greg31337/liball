namespace version;

/// <summary>
/// Represents a go module path.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class GoModulePath
{
    /// <summary>
    /// Stores the <c>value</c> value.
    /// </summary>
    public string value;
    /// <summary>
    /// Stores the <c>majorVersionSuffix</c> value.
    /// </summary>
    public string majorVersionSuffix;
    /// <summary>
    /// Stores the <c>majorVersionSuffixSpecified</c> value.
    /// </summary>
    public bool majorVersionSuffixSpecified;
    /// <summary>
    /// Stores the <c>gopkgInPath</c> value.
    /// </summary>
    public bool gopkgInPath;

    /// <summary>
    /// Initializes a new Go Module Path instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="valueText">The value text to process.</param>
    /// <param name="majorVersionSuffixText">The major version suffix text to process.</param>
    /// <param name="gopkgInPathValue">Whether to enable gopkg in path value.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public GoModulePath(string valueText, string majorVersionSuffixText, bool gopkgInPathValue)
    {
        System.ArgumentNullException.ThrowIfNull(valueText);
        System.ArgumentNullException.ThrowIfNull(majorVersionSuffixText);

        value = valueText;
        majorVersionSuffix = majorVersionSuffixText;
        majorVersionSuffixSpecified = majorVersionSuffixText.Length > 0;
        gopkgInPath = gopkgInPathValue;
    }

    /// <summary>
    /// Gets a value.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetValue()
    {
        return value;
    }

    /// <summary>
    /// Reports whether this value has major version suffix.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasMajorVersionSuffix()
    {
        return majorVersionSuffixSpecified;
    }

    /// <summary>
    /// Gets a major version suffix.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetMajorVersionSuffix()
    {
        return majorVersionSuffix;
    }

    /// <summary>
    /// Reports whether gopkg in path.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsGopkgInPath()
    {
        return gopkgInPath;
    }

    /// <summary>
    /// Reports whether this module path matches the supplied path text.
    /// </summary>
    /// <param name="other">The other used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool MatchesPath(GoModulePath other)
    {
        System.ArgumentNullException.ThrowIfNull(other);

        return value == other.GetValue();
    }
}
