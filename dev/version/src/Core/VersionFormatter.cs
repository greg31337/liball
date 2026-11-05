namespace version;

/// <summary>
/// Represents a version formatter.
/// </summary>
/// <remarks>
/// Values are exposed through ordinary methods because the library intentionally avoids properties and external dependencies.
/// </remarks>
internal static class VersionFormatter
{
    /// <summary>
    /// Formats the supplied value as original text.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public static string FormatOriginal(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);

        return version.GetOriginalText();
    }

    /// <summary>
    /// Formats the supplied value as normalized text.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public static string FormatNormalized(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);
        return VersionDialectStrategies.Get(version.GetDialect()).FormatNormalized(version);
    }

    /// <summary>
    /// Formats the supplied value as canonical text.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public static string FormatCanonical(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);
        return VersionDialectStrategies.Get(version.GetDialect()).FormatCanonical(version);
    }

    /// <summary>
    /// Formats the supplied value as display text.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public static string FormatDisplay(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);

        if (version.GetOriginalText().Length > 0)
        {
            return version.GetOriginalText();
        }

        return FormatCanonical(version);
    }

    /// <summary>
    /// Joins normalized numeric components with dot separators.
    /// </summary>
    /// <param name="components">The components to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    public static string JoinNumericComponents(string[] components)
    {
        System.ArgumentNullException.ThrowIfNull(components);

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;

        while (index < components.Length)
        {
            if (components[index] == null)
            {
                throw new System.ArgumentException("Numeric components cannot contain null values.");
            }

            if (index > 0)
            {
                builder.Append('.');
            }

            builder.Append(NormalizeNumericText(components[index]));
            index = index + 1;
        }

        return builder.ToString();
    }

    /// <summary>
    /// Joins version identifiers with dot separators after normalizing numeric text.
    /// </summary>
    /// <param name="identifiers">The identifiers to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public static string JoinIdentifiers(VersionIdentifier[] identifiers)
    {
        System.ArgumentNullException.ThrowIfNull(identifiers);

        return JoinIdentifiersWithNormalization(identifiers, true);
    }

    /// <summary>
    /// Removes redundant leading zeroes while retaining a single zero for an all-zero value.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    public static string NormalizeNumericText(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0)
        {
            throw new System.ArgumentException("Numeric text cannot be empty.");
        }

        int index = 0;

        while (index < text.Length - 1 && text[index] == '0')
        {
            index = index + 1;
        }

        return text.Substring(index);
    }

    internal static string FormatCore(UniversalVersion version, bool includeLeadingVPrefix, bool includeBuildMetadata)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();

        if (includeLeadingVPrefix)
        {
            builder.Append('v');
        }

        builder.Append(JoinNumericComponents(version.GetNumericComponents()));

        if (version.HasRevisionComponent())
        {
            builder.Append('.');
            builder.Append(NormalizeNumericText(version.GetRevisionComponent()));
        }

        if (version.IsPrerelease())
        {
            builder.Append('-');
            builder.Append(JoinIdentifiersWithNormalization(version.GetPrereleaseIdentifiers(), true));
        }

        if (includeBuildMetadata && version.HasBuildMetadata())
        {
            builder.Append('+');
            builder.Append(JoinIdentifiersWithNormalization(version.GetBuildIdentifiers(), true));
        }

        return builder.ToString();
    }

    internal static string JoinIdentifiersWithNormalization(VersionIdentifier[] identifiers, bool normalizeNumeric)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;

        while (index < identifiers.Length)
        {
            if (identifiers[index] == null)
            {
                throw new System.ArgumentException("Identifiers cannot contain null values.");
            }

            if (index > 0)
            {
                builder.Append('.');
            }

            if (normalizeNumeric && identifiers[index].IsNumeric())
            {
                builder.Append(NormalizeNumericText(identifiers[index].GetText()));
            }
            else
            {
                builder.Append(identifiers[index].GetText());
            }

            index = index + 1;
        }

        return builder.ToString();
    }
}
