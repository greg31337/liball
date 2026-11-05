namespace version;

/// <summary>
/// Represents a parsed version together with its ecosystem dialect and formatting details.
/// </summary>
/// <remarks>
/// Numeric, prerelease, and build components are retained separately in public data fields.
/// </remarks>
public class UniversalVersion
{
    /// <summary>
    /// Stores the <c>originalText</c> value.
    /// </summary>
    public string originalText;
    /// <summary>
    /// Stores the <c>dialect</c> value.
    /// </summary>
    public VersionDialect dialect;
    /// <summary>
    /// Stores the <c>numericComponents</c> value.
    /// </summary>
    public string[] numericComponents;
    /// <summary>
    /// Stores the <c>prereleaseIdentifiers</c> value.
    /// </summary>
    public VersionIdentifier[] prereleaseIdentifiers;
    /// <summary>
    /// Stores the <c>buildIdentifiers</c> value.
    /// </summary>
    public VersionIdentifier[] buildIdentifiers;
    /// <summary>
    /// Stores the <c>missingComponentCount</c> value.
    /// </summary>
    public int missingComponentCount;
    /// <summary>
    /// Stores the <c>leadingVPrefix</c> value.
    /// </summary>
    public bool leadingVPrefix;
    /// <summary>
    /// Stores the <c>revisionComponentPresent</c> value.
    /// </summary>
    public bool revisionComponentPresent;
    /// <summary>
    /// Stores the <c>revisionComponent</c> value.
    /// </summary>
    public string revisionComponent;
    /// <summary>
    /// Stores the <c>pythonVersion</c> value.
    /// </summary>
    public PythonVersion? pythonVersion;

    /// <summary>
    /// Initializes a new Universal Version instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="originalTextValue">The original text value to process.</param>
    /// <param name="dialectValue">The dialect value used by the operation.</param>
    /// <param name="numericComponentValues">The numeric component values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="prereleaseIdentifierValues">The prerelease identifier values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="buildIdentifierValues">The build identifier values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="missingComponentCountValue">The missing component count value used by the operation.</param>
    /// <param name="leadingVPrefixValue">Whether to enable leading v prefix value.</param>
    /// <param name="revisionComponentPresentValue">Whether to enable revision component present value.</param>
    /// <param name="revisionComponentValue">The revision component value used by the operation.</param>
    /// <param name="pythonVersionValue">The python version value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    public UniversalVersion(
        string originalTextValue,
        VersionDialect dialectValue,
        string[] numericComponentValues,
        VersionIdentifier[] prereleaseIdentifierValues,
        VersionIdentifier[] buildIdentifierValues,
        int missingComponentCountValue,
        bool leadingVPrefixValue,
        bool revisionComponentPresentValue,
        string revisionComponentValue,
        PythonVersion? pythonVersionValue)
    {
        System.ArgumentNullException.ThrowIfNull(originalTextValue);
        System.ArgumentNullException.ThrowIfNull(numericComponentValues);
        System.ArgumentNullException.ThrowIfNull(prereleaseIdentifierValues);
        System.ArgumentNullException.ThrowIfNull(buildIdentifierValues);
        System.ArgumentNullException.ThrowIfNull(revisionComponentValue);

        if (numericComponentValues.Length == 0)
        {
            throw new System.ArgumentException("At least one numeric component is required.");
        }

        if (missingComponentCountValue < 0)
        {
            throw new System.ArgumentOutOfRangeException("missingComponentCountValue");
        }

        if (revisionComponentPresentValue && revisionComponentValue.Length == 0)
        {
            throw new System.ArgumentException("Revision component cannot be empty when present.");
        }

        if (revisionComponentPresentValue && !ContainsOnlyDigits(revisionComponentValue))
        {
            throw new System.ArgumentException("Revision component must contain only digits.");
        }

        if (pythonVersionValue != null && dialectValue != VersionDialect.Python)
        {
            throw new System.ArgumentException("Python version metadata can only be attached to Python universal versions.");
        }

        originalText = originalTextValue;
        dialect = dialectValue;
        numericComponents = CopyNumericComponents(numericComponentValues);
        prereleaseIdentifiers = CopyIdentifiers(prereleaseIdentifierValues);
        buildIdentifiers = CopyIdentifiers(buildIdentifierValues);
        missingComponentCount = missingComponentCountValue;
        leadingVPrefix = leadingVPrefixValue;
        revisionComponentPresent = revisionComponentPresentValue;
        revisionComponent = revisionComponentValue;
        pythonVersion = pythonVersionValue;
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
    /// Gets a dialect.
    /// </summary>
    /// <returns>The resulting version dialect.</returns>
    internal VersionDialect GetDialect()
    {
        return dialect;
    }

    /// <summary>
    /// Gets a numeric component count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetNumericComponentCount()
    {
        return numericComponents.Length;
    }

    /// <summary>
    /// Gets a numeric component.
    /// </summary>
    /// <param name="componentIndex">The zero-based component index.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal string GetNumericComponent(int componentIndex)
    {
        if (componentIndex < 0 || componentIndex >= numericComponents.Length)
        {
            throw new System.ArgumentOutOfRangeException("componentIndex");
        }

        return numericComponents[componentIndex];
    }

    /// <summary>
    /// Gets a numeric components.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal string[] GetNumericComponents()
    {
        return CopyStringArray(numericComponents);
    }

    /// <summary>
    /// Gets a prerelease identifier count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetPrereleaseIdentifierCount()
    {
        return prereleaseIdentifiers.Length;
    }

    /// <summary>
    /// Gets a prerelease identifier.
    /// </summary>
    /// <param name="identifierIndex">The zero-based identifier index.</param>
    /// <returns>The resulting version identifier.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal VersionIdentifier GetPrereleaseIdentifier(int identifierIndex)
    {
        if (identifierIndex < 0 || identifierIndex >= prereleaseIdentifiers.Length)
        {
            throw new System.ArgumentOutOfRangeException("identifierIndex");
        }

        return prereleaseIdentifiers[identifierIndex];
    }

    /// <summary>
    /// Gets a prerelease identifiers.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal VersionIdentifier[] GetPrereleaseIdentifiers()
    {
        return CopyIdentifiers(prereleaseIdentifiers);
    }

    /// <summary>
    /// Gets a build identifier count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetBuildIdentifierCount()
    {
        return buildIdentifiers.Length;
    }

    /// <summary>
    /// Gets a build identifier.
    /// </summary>
    /// <param name="identifierIndex">The zero-based identifier index.</param>
    /// <returns>The resulting version identifier.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal VersionIdentifier GetBuildIdentifier(int identifierIndex)
    {
        if (identifierIndex < 0 || identifierIndex >= buildIdentifiers.Length)
        {
            throw new System.ArgumentOutOfRangeException("identifierIndex");
        }

        return buildIdentifiers[identifierIndex];
    }

    /// <summary>
    /// Gets a build identifiers.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal VersionIdentifier[] GetBuildIdentifiers()
    {
        return CopyIdentifiers(buildIdentifiers);
    }

    /// <summary>
    /// Reports whether the version is a prerelease.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsPrerelease()
    {
        return prereleaseIdentifiers.Length > 0;
    }

    /// <summary>
    /// Reports whether this value has build metadata.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasBuildMetadata()
    {
        return buildIdentifiers.Length > 0;
    }

    /// <summary>
    /// Gets a missing component count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetMissingComponentCount()
    {
        return missingComponentCount;
    }

    /// <summary>
    /// Reports whether this value has leading v prefix.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasLeadingVPrefix()
    {
        return leadingVPrefix;
    }

    /// <summary>
    /// Reports whether this value has revision component.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasRevisionComponent()
    {
        return revisionComponentPresent;
    }

    /// <summary>
    /// Gets a revision component.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetRevisionComponent()
    {
        return revisionComponent;
    }

    /// <summary>
    /// Reports whether this value has python version.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasPythonVersion()
    {
        return pythonVersion != null;
    }

    /// <summary>
    /// Gets a python version.
    /// </summary>
    /// <returns>The resulting python version.</returns>
    /// <exception cref="System.InvalidOperationException">The requested value is unavailable because the operation did not complete successfully.</exception>
    internal PythonVersion GetPythonVersion()
    {
        if (pythonVersion == null)
        {
            throw new System.InvalidOperationException("Universal version does not contain Python version metadata.");
        }

        return pythonVersion;
    }

    /// <summary>
    /// Formats this value as original string.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <returns>The resulting text.</returns>
    internal string ToOriginalString()
    {
        return VersionFormatter.FormatOriginal(this);
    }

    /// <summary>
    /// Formats this value as normalized string.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <returns>The resulting text.</returns>
    internal string ToNormalizedString()
    {
        return VersionFormatter.FormatNormalized(this);
    }

    /// <summary>
    /// Formats this value as canonical string.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <returns>The resulting text.</returns>
    internal string ToCanonicalString()
    {
        return VersionFormatter.FormatCanonical(this);
    }

    /// <summary>
    /// Formats this value as display string.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <returns>The resulting text.</returns>
    internal string ToDisplayString()
    {
        return VersionFormatter.FormatDisplay(this);
    }

    /// <summary>
    /// Compares two versions using to version rules.
    /// </summary>
    /// <remarks>
    /// A negative value orders the left version first, zero means equal under the selected comparison mode, and a positive value orders the right version first.
    /// </remarks>
    /// <param name="other">The other used by the operation.</param>
    /// <param name="mode">The mode used by the operation.</param>
    /// <returns>A negative number, zero, or a positive number when the left version is less than, equal to, or greater than the right version.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal int CompareToVersion(UniversalVersion other, VersionComparisonMode mode)
    {
        System.ArgumentNullException.ThrowIfNull(other);
        return VersionDialectStrategies.GetForComparison(this, other).Compare(this, other, mode);
    }

    /// <summary>
    /// Compares two versions using to version rules.
    /// </summary>
    /// <remarks>
    /// A negative value orders the left version first, zero means equal under the selected comparison mode, and a positive value orders the right version first.
    /// </remarks>
    /// <param name="other">The other used by the operation.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A negative number, zero, or a positive number when the left version is less than, equal to, or greater than the right version.</returns>
    internal int CompareToVersionWithOptions(UniversalVersion other, VersionComparisonOptions options)
    {
        return VersionOptionDispatcher.CompareVersions(this, other, options);
    }

    /// <summary>
    /// Parses version text using this API's ecosystem rules.
    /// </summary>
    /// <remarks>
    /// The complete input is interpreted according to the dialect represented by this API.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The resulting universal version.</returns>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    internal static UniversalVersion Parse(string text, VersionParseOptions options)
    {
        VersionParseResult result = VersionOptionDispatcher.Parse(text, options);
        if (result.IsFailure())
        {
            throw new System.ArgumentException("Invalid version text.", "text");
        }

        return result.GetVersion();
    }

    /// <summary>
    /// Attempts to parse version text using the supplied options.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    internal static VersionParseResult TryParse(string text, VersionParseOptions options)
    {
        return VersionOptionDispatcher.Parse(text, options);
    }

    /// <summary>
    /// Compares two versions using the selected ecosystem precedence rules.
    /// </summary>
    /// <remarks>
    /// A negative value orders the left version first, zero means equal under the selected comparison mode, and a positive value orders the right version first.
    /// </remarks>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A negative number, zero, or a positive number when the left version is less than, equal to, or greater than the right version.</returns>
    internal static int Compare(string leftText, string rightText, VersionComparisonOptions options)
    {
        return VersionOptionDispatcher.Compare(leftText, rightText, options);
    }

    /// <summary>
    /// Reports whether both versions have equal precedence, ignoring identity-only details such as build metadata.
    /// </summary>
    /// <param name="other">The other used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool EqualsByPrecedence(UniversalVersion other)
    {
        System.ArgumentNullException.ThrowIfNull(other);

        return SemVerVersionComparer.EqualsByPrecedence(this, other);
    }

    /// <summary>
    /// Reports whether both versions have the same complete identity, including build metadata.
    /// </summary>
    /// <param name="other">The other used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool EqualsByIdentity(UniversalVersion other)
    {
        System.ArgumentNullException.ThrowIfNull(other);

        return SemVerVersionComparer.EqualsByIdentity(this, other);
    }

    /// <summary>
    /// Reports whether both versions have the same dialect-normalized package identity.
    /// </summary>
    /// <param name="other">The other used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool EqualsByNormalizedIdentity(UniversalVersion other)
    {
        System.ArgumentNullException.ThrowIfNull(other);

        return SemVerVersionComparer.EqualsByNormalizedIdentity(this, other);
    }

    internal static string[] CopyNumericComponents(string[] components)
    {
        string[] copy = CopyStringArray(components);
        int index = 0;

        while (index < copy.Length)
        {
            if (copy[index] == null)
            {
                throw new System.ArgumentException("Numeric components cannot contain null values.");
            }

            if (copy[index].Length == 0)
            {
                throw new System.ArgumentException("Numeric components cannot contain empty values.");
            }

            if (!ContainsOnlyDigits(copy[index]))
            {
                throw new System.ArgumentException("Numeric components must contain only digits.");
            }

            index = index + 1;
        }

        return copy;
    }

    internal static VersionIdentifier[] CopyIdentifiers(VersionIdentifier[] identifiers)
    {
        VersionIdentifier[] copy = new VersionIdentifier[identifiers.Length];
        int index = 0;

        while (index < identifiers.Length)
        {
            if (identifiers[index] == null)
            {
                throw new System.ArgumentException("Identifiers cannot contain null values.");
            }

            copy[index] = identifiers[index];
            index = index + 1;
        }

        return copy;
    }

    internal static string[] CopyStringArray(string[] values)
    {
        string[] copy = new string[values.Length];
        int index = 0;

        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }

    internal static bool ContainsOnlyDigits(string value)
    {
        int index = 0;

        while (index < value.Length)
        {
            if (!VersionChar.IsDigit(value[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
