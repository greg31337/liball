namespace version;

/// <summary>
/// Provides public Python and PEP 440 version, specifier, selection, and range operations.
/// </summary>
/// <remarks>
/// The service evaluates only caller-supplied text and finite candidate arrays.
/// </remarks>
public class PythonOperations : IPythonOperations
{
    /// <summary>
    /// Initializes a stateless Python operations service.
    /// </summary>
    public PythonOperations()
    {
    }

    /// <summary>
    /// Parses a PEP 440 version without throwing for ordinary invalid input.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the parsed version or diagnostics.</returns>
    public VersionParseResult ParseVersion(string text)
    {
        return PythonVersionParser.Parse(text);
    }

    /// <summary>
    /// Parses and normalizes PEP 440 version text.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The normalized PEP 440 version text.</returns>
    public string NormalizeVersion(string text)
    {
        return PythonVersionFormatter.FormatNormalized(
            PythonVersionParser.ParseVersion(text));
    }

    /// <summary>
    /// Formats a parsed Python version in canonical PEP 440 form.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The canonical version text.</returns>
    public string FormatCanonicalVersion(UniversalVersion version)
    {
        ValidatePythonVersion(version);
        return VersionFormatter.FormatCanonical(version);
    }

    /// <summary>
    /// Formats a parsed Python version for display, preserving its original spelling when available.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The display version text.</returns>
    public string FormatDisplayVersion(UniversalVersion version)
    {
        ValidatePythonVersion(version);
        return VersionFormatter.FormatDisplay(version);
    }

    /// <summary>
    /// Compares two PEP 440 versions.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>A negative number, zero, or a positive number.</returns>
    public int CompareVersions(string leftText, string rightText)
    {
        return PythonVersionComparer.Compare(leftText, rightText);
    }

    /// <summary>
    /// Parses a PEP 440 specifier set without throwing for ordinary invalid input.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the parsed specifier range or diagnostics.</returns>
    public VersionRangeParseResult ParseSpecifier(string text)
    {
        return PythonSpecifierParser.Parse(text);
    }

    /// <summary>
    /// Reports whether version text satisfies a parsed specifier and prerelease policy.
    /// </summary>
    /// <param name="specifier">The parsed specifier range.</param>
    /// <param name="versionText">The version text to process.</param>
    /// <param name="options">The options that control prerelease admission.</param>
    /// <returns><c>true</c> when the version satisfies the specifier; otherwise, <c>false</c>.</returns>
    public bool SpecifierAllowsVersion(
        VersionRange specifier,
        string versionText,
        VersionSelectionOptions options)
    {
        System.ArgumentNullException.ThrowIfNull(specifier);
        ValidateSelectionOptions(options);

        VersionParseResult result = PythonVersionParser.Parse(versionText);
        if (result.IsFailure())
        {
            return false;
        }

        return VersionSelector.Matches(
            specifier,
            result.GetVersion(),
            options.GetPrereleasePolicy());
    }

    /// <summary>
    /// Filters finite Python version candidates using a specifier and prerelease policy.
    /// </summary>
    /// <param name="specifier">The parsed specifier range.</param>
    /// <param name="versionTexts">The version texts to examine.</param>
    /// <param name="options">The options that control prerelease admission.</param>
    /// <returns>A new array containing accepted original candidate texts in input order.</returns>
    public string[] FilterCandidates(
        VersionRange specifier,
        string[] versionTexts,
        VersionSelectionOptions options)
    {
        System.ArgumentNullException.ThrowIfNull(specifier);
        ValidateSelectionOptions(options);

        UniversalVersion[] candidates = ParseCandidates(versionTexts);
        UniversalVersion[] filtered = VersionSelector.Filter(
            specifier,
            candidates,
            options);

        return GetOriginalTexts(filtered);
    }

    /// <summary>
    /// Selects the highest finite Python version candidate using a specifier and prerelease policy.
    /// </summary>
    /// <param name="specifier">The parsed specifier range.</param>
    /// <param name="versionTexts">The version texts to examine.</param>
    /// <param name="options">The options that control prerelease admission.</param>
    /// <returns>The original selected candidate text, or null when no candidate matches.</returns>
    public string? SelectHighestCandidate(
        VersionRange specifier,
        string[] versionTexts,
        VersionSelectionOptions options)
    {
        System.ArgumentNullException.ThrowIfNull(specifier);
        ValidateSelectionOptions(options);

        UniversalVersion? selected = VersionSelector.MaxSatisfying(
            specifier,
            ParseCandidates(versionTexts),
            options);
        if (selected == null)
        {
            return null;
        }

        return selected.GetOriginalText();
    }

    /// <summary>
    /// Reports whether every version accepted by the left specifier is accepted by the right specifier.
    /// </summary>
    /// <param name="left">The left specifier range.</param>
    /// <param name="right">The right specifier range.</param>
    /// <returns><c>true</c> when the left range is a subset of the right range; otherwise, <c>false</c>.</returns>
    public bool IsSpecifierSubset(VersionRange left, VersionRange right)
    {
        return VersionRangeAlgebra.IsSubset(left, right);
    }

    /// <summary>
    /// Reports whether every version accepted by the right specifier is accepted by the left specifier.
    /// </summary>
    /// <param name="left">The left specifier range.</param>
    /// <param name="right">The right specifier range.</param>
    /// <returns><c>true</c> when the left range is a superset of the right range; otherwise, <c>false</c>.</returns>
    public bool IsSpecifierSuperset(VersionRange left, VersionRange right)
    {
        return VersionRangeAlgebra.IsSuperset(left, right);
    }

    /// <summary>
    /// Reports whether two specifier ranges accept no version in common.
    /// </summary>
    /// <param name="left">The left specifier range.</param>
    /// <param name="right">The right specifier range.</param>
    /// <returns><c>true</c> when the ranges are disjoint; otherwise, <c>false</c>.</returns>
    public bool AreSpecifiersDisjoint(VersionRange left, VersionRange right)
    {
        return VersionRangeAlgebra.IsDisjoint(left, right);
    }

    internal void ValidatePythonVersion(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);

        if (version.GetDialect() != VersionDialect.Python || !version.HasPythonVersion())
        {
            throw new System.ArgumentException("Version must use Python semantics.", "version");
        }
    }

    internal void ValidateSelectionOptions(VersionSelectionOptions options)
    {
        System.ArgumentNullException.ThrowIfNull(options);
        VersionSelectionOptions.ValidatePrereleasePolicy(options.GetPrereleasePolicy());
    }

    internal UniversalVersion[] ParseCandidates(string[] versionTexts)
    {
        System.ArgumentNullException.ThrowIfNull(versionTexts);

        System.Collections.Generic.List<UniversalVersion> candidates = new System.Collections.Generic.List<UniversalVersion>();
        int index = 0;
        while (index < versionTexts.Length)
        {
            if (versionTexts[index] == null)
            {
                throw new System.ArgumentException(
                    "Version arrays cannot contain null values.",
                    "versionTexts");
            }

            VersionParseResult result = PythonVersionParser.Parse(versionTexts[index]);
            if (result.IsSuccess())
            {
                candidates.Add(result.GetVersion());
            }

            index = index + 1;
        }

        return candidates.ToArray();
    }

    internal string[] GetOriginalTexts(UniversalVersion[] versions)
    {
        string[] texts = new string[versions.Length];
        int index = 0;
        while (index < versions.Length)
        {
            texts[index] = versions[index].GetOriginalText();
            index = index + 1;
        }

        return texts;
    }
}
