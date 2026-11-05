namespace version;

/// <summary>
/// Represents a nuget floating version.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class NuGetFloatingVersion
{
    /// <summary>
    /// Stores the <c>valid</c> value.
    /// </summary>
    public bool valid;
    /// <summary>
    /// Stores the <c>numericPrecision</c> value.
    /// </summary>
    public int numericPrecision;
    /// <summary>
    /// Stores the <c>major</c> value.
    /// </summary>
    public string major;
    /// <summary>
    /// Stores the <c>minor</c> value.
    /// </summary>
    public string minor;
    /// <summary>
    /// Stores the <c>patch</c> value.
    /// </summary>
    public string patch;
    /// <summary>
    /// Stores the <c>includePrerelease</c> value.
    /// </summary>
    public bool includePrerelease;
    /// <summary>
    /// Stores the <c>prereleaseAny</c> value.
    /// </summary>
    public bool prereleaseAny;
    /// <summary>
    /// Stores the <c>prereleasePrefix</c> value.
    /// </summary>
    public string[] prereleasePrefix;

    /// <summary>
    /// Initializes a new NuGet Floating Version instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="validValue">Whether to enable valid value.</param>
    /// <param name="numericPrecisionValue">The numeric precision value used by the operation.</param>
    /// <param name="majorValue">The major value used by the operation.</param>
    /// <param name="minorValue">The minor value used by the operation.</param>
    /// <param name="patchValue">The patch value used by the operation.</param>
    /// <param name="includePrereleaseValue">Whether to enable include prerelease value.</param>
    /// <param name="prereleaseAnyValue">Whether to enable prerelease any value.</param>
    /// <param name="prereleasePrefixValue">The prerelease prefix value to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public NuGetFloatingVersion(
        bool validValue,
        int numericPrecisionValue,
        string majorValue,
        string minorValue,
        string patchValue,
        bool includePrereleaseValue,
        bool prereleaseAnyValue,
        string[] prereleasePrefixValue)
    {
        System.ArgumentNullException.ThrowIfNull(majorValue);
        System.ArgumentNullException.ThrowIfNull(minorValue);
        System.ArgumentNullException.ThrowIfNull(patchValue);
        System.ArgumentNullException.ThrowIfNull(prereleasePrefixValue);

        valid = validValue;
        numericPrecision = numericPrecisionValue;
        major = majorValue;
        minor = minorValue;
        patch = patchValue;
        includePrerelease = includePrereleaseValue;
        prereleaseAny = prereleaseAnyValue;
        prereleasePrefix = CopyStrings(prereleasePrefixValue);
    }

    /// <summary>
    /// Parses version text using this API's ecosystem rules.
    /// </summary>
    /// <remarks>
    /// The complete input is interpreted according to the dialect represented by this API.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting nuget floating version.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal static NuGetFloatingVersion Parse(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        if (!ContainsStar(text) || ContainsCharacter(text, '+') || ContainsWhitespace(text))
        {
            return CreateInvalid();
        }

        int hyphenIndex = FindHyphenIndex(text);
        string coreText = text;
        string prereleaseText = "";
        bool hasPrerelease = false;

        if (hyphenIndex >= 0)
        {
            hasPrerelease = true;
            coreText = text.Substring(0, hyphenIndex);
            prereleaseText = text.Substring(hyphenIndex + 1);
        }

        NuGetFloatingVersion corePattern = ParseCorePattern(coreText);
        if (!corePattern.IsValid())
        {
            return CreateInvalid();
        }

        if (!hasPrerelease)
        {
            return corePattern;
        }

        if (prereleaseText.Length == 0)
        {
            return CreateInvalid();
        }

        if (prereleaseText == "*")
        {
            return new NuGetFloatingVersion(
                true,
                corePattern.GetNumericPrecision(),
                corePattern.GetMajor(),
                corePattern.GetMinor(),
                corePattern.GetPatch(),
                true,
                true,
                new string[0]);
        }

        string[] prefix = ParsePrereleasePrefix(prereleaseText);
        if (prefix.Length == 0)
        {
            return CreateInvalid();
        }

        if (corePattern.GetNumericPrecision() != 3)
        {
            return CreateInvalid();
        }

        return new NuGetFloatingVersion(
            true,
            corePattern.GetNumericPrecision(),
            corePattern.GetMajor(),
            corePattern.GetMinor(),
            corePattern.GetPatch(),
            true,
            false,
            prefix);
    }

    /// <summary>
    /// Reports whether the represented input is valid.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsValid()
    {
        return valid;
    }

    /// <summary>
    /// Gets a numeric precision.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetNumericPrecision()
    {
        return numericPrecision;
    }

    /// <summary>
    /// Gets a major.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetMajor()
    {
        return major;
    }

    /// <summary>
    /// Gets a minor.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetMinor()
    {
        return minor;
    }

    /// <summary>
    /// Gets a patch.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetPatch()
    {
        return patch;
    }

    /// <summary>
    /// Checks whether this value contains version.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool ContainsVersion(UniversalVersion version)
    {
        return ContainsVersionWithPrereleaseOverride(version, false);
    }

    /// <summary>
    /// Checks whether this value contains version.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <param name="includePrereleaseOverride">Whether to enable include prerelease override.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool ContainsVersionWithPrereleaseOverride(UniversalVersion version, bool includePrereleaseOverride)
    {
        System.ArgumentNullException.ThrowIfNull(version);

        if (!MatchesNumericPattern(version))
        {
            return false;
        }

        if (!version.IsPrerelease())
        {
            return true;
        }

        if (!includePrerelease && !includePrereleaseOverride)
        {
            return false;
        }

        if (prereleaseAny || prereleasePrefix.Length == 0 || includePrereleaseOverride)
        {
            return true;
        }

        return MatchesPrereleasePrefix(version);
    }

    /// <summary>
    /// Selects a highest matching version from the supplied candidates.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>The resulting universal version.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    internal UniversalVersion? SelectHighestMatchingVersion(UniversalVersion[] candidates)
    {
        System.ArgumentNullException.ThrowIfNull(candidates);

        UniversalVersion? selectedVersion = null;
        int index = 0;

        while (index < candidates.Length)
        {
            if (candidates[index] == null)
            {
                throw new System.ArgumentException("Candidate versions cannot contain null values.");
            }

            if (ContainsVersion(candidates[index]))
            {
                if (selectedVersion == null || candidates[index].CompareToVersion(selectedVersion, VersionComparisonMode.Precedence) > 0)
                {
                    selectedVersion = candidates[index];
                }
            }

            index = index + 1;
        }

        return selectedVersion;
    }

    /// <summary>
    /// Selects a highest matching text from the supplied candidates.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="candidateTexts">The candidate texts to process.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    internal string SelectHighestMatchingText(string[] candidateTexts)
    {
        System.ArgumentNullException.ThrowIfNull(candidateTexts);

        UniversalVersion? selectedVersion = null;
        string selectedText = "";
        int index = 0;

        while (index < candidateTexts.Length)
        {
            if (candidateTexts[index] == null)
            {
                throw new System.ArgumentException("Candidate version text cannot contain null values.");
            }

            VersionParseResult candidateResult = NuGetVersionParser.Parse(candidateTexts[index]);
            if (candidateResult.IsSuccess() && ContainsVersion(candidateResult.GetVersion()))
            {
                if (selectedVersion == null || candidateResult.GetVersion().CompareToVersion(selectedVersion, VersionComparisonMode.Precedence) > 0)
                {
                    selectedVersion = candidateResult.GetVersion();
                    selectedText = candidateTexts[index];
                }
            }

            index = index + 1;
        }

        return selectedText;
    }

    /// <summary>
    /// Formats this value as comparator set.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <returns>The resulting version comparator set.</returns>
    internal VersionComparatorSet ToComparatorSet()
    {
        if (numericPrecision == 0)
        {
            return VersionComparatorSet.CreateSingle(
                VersionComparator.CreateGreaterThanOrEqual(CreateNuGetVersion("0.0.0"), VersionComparisonMode.Precedence));
        }

        if (numericPrecision == 1)
        {
            return CreateRangeSet(major + ".0.0", IncrementNumericText(major) + ".0.0");
        }

        if (numericPrecision == 2)
        {
            return CreateRangeSet(major + "." + minor + ".0", major + "." + IncrementNumericText(minor) + ".0");
        }

        return CreateRangeSet(major + "." + minor + "." + patch + "-0", major + "." + minor + "." + patch);
    }

    internal bool MatchesNumericPattern(UniversalVersion version)
    {
        if (numericPrecision == 0)
        {
            return true;
        }

        if (!NumericTextEquals(version.GetNumericComponent(0), major))
        {
            return false;
        }

        if (numericPrecision == 1)
        {
            return true;
        }

        if (!NumericTextEquals(version.GetNumericComponent(1), minor))
        {
            return false;
        }

        if (numericPrecision == 2)
        {
            return true;
        }

        return NumericTextEquals(version.GetNumericComponent(2), patch);
    }

    internal bool MatchesPrereleasePrefix(UniversalVersion version)
    {
        if (version.GetPrereleaseIdentifierCount() <= prereleasePrefix.Length)
        {
            return false;
        }

        int index = 0;
        while (index < prereleasePrefix.Length)
        {
            if (System.String.Compare(
                version.GetPrereleaseIdentifier(index).GetText(),
                prereleasePrefix[index],
                System.StringComparison.OrdinalIgnoreCase) != 0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal VersionComparatorSet CreateRangeSet(string lowerText, string upperText)
    {
        VersionComparator[] comparators = new VersionComparator[2];
        comparators[0] = VersionComparator.CreateGreaterThanOrEqual(CreateNuGetVersion(lowerText), VersionComparisonMode.Precedence);
        comparators[1] = VersionComparator.CreateLessThan(CreateNuGetVersion(upperText), VersionComparisonMode.Precedence);
        return new VersionComparatorSet(comparators);
    }

    internal static NuGetFloatingVersion ParseCorePattern(string text)
    {
        string[] components = SplitComponents(text);
        if (components.Length == 1 && components[0] == "*")
        {
            return new NuGetFloatingVersion(true, 0, "0", "0", "0", false, false, new string[0]);
        }

        if (components.Length == 2 && components[1] == "*" && IsValidNumericComponent(components[0]))
        {
            return new NuGetFloatingVersion(true, 1, components[0], "0", "0", false, false, new string[0]);
        }

        if (components.Length == 3
            && components[2] == "*"
            && IsValidNumericComponent(components[0])
            && IsValidNumericComponent(components[1]))
        {
            return new NuGetFloatingVersion(true, 2, components[0], components[1], "0", false, false, new string[0]);
        }

        if (components.Length == 3
            && IsValidNumericComponent(components[0])
            && IsValidNumericComponent(components[1])
            && IsValidNumericComponent(components[2]))
        {
            return new NuGetFloatingVersion(true, 3, components[0], components[1], components[2], false, false, new string[0]);
        }

        return CreateInvalid();
    }

    internal static string[] ParsePrereleasePrefix(string text)
    {
        if (text.Length < 3)
        {
            return new string[0];
        }

        if (text[text.Length - 1] != '*' || text[text.Length - 2] != '.')
        {
            return new string[0];
        }

        string prefixText = text.Substring(0, text.Length - 2);
        string[] components = SplitComponents(prefixText);
        int index = 0;

        while (index < components.Length)
        {
            if (!IsValidPrereleaseComponent(components[index]))
            {
                return new string[0];
            }

            index = index + 1;
        }

        return components;
    }

    internal static NuGetFloatingVersion CreateInvalid()
    {
        return new NuGetFloatingVersion(false, 0, "0", "0", "0", false, false, new string[0]);
    }

    internal static UniversalVersion CreateNuGetVersion(string text)
    {
        VersionParseResult result = NuGetVersionParser.Parse(text);
        if (result.IsFailure())
        {
            throw new System.InvalidOperationException("Internal NuGet floating version desugaring produced an invalid version.");
        }

        return result.GetVersion();
    }

    internal static string[] SplitComponents(string text)
    {
        System.Collections.Generic.List<string> components = new System.Collections.Generic.List<string>();
        int startIndex = 0;
        int index = 0;

        if (text.Length == 0)
        {
            return new string[0];
        }

        while (index <= text.Length)
        {
            if (index == text.Length || text[index] == '.')
            {
                components.Add(text.Substring(startIndex, index - startIndex));
                startIndex = index + 1;
            }

            index = index + 1;
        }

        return components.ToArray();
    }

    internal static bool IsValidNumericComponent(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < text.Length)
        {
            if (!VersionChar.IsDigit(text[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsValidPrereleaseComponent(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < text.Length)
        {
            if (!VersionChar.IsAlphaNumeric(text[index]) && !VersionChar.IsHyphen(text[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool ContainsStar(string text)
    {
        int index = 0;

        while (index < text.Length)
        {
            if (text[index] == '*')
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool ContainsWhitespace(string text)
    {
        int index = 0;

        while (index < text.Length)
        {
            if (VersionChar.IsWhitespace(text[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool ContainsCharacter(string text, char value)
    {
        int index = 0;

        while (index < text.Length)
        {
            if (text[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static int FindHyphenIndex(string text)
    {
        int index = 0;

        while (index < text.Length)
        {
            if (text[index] == '-')
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool NumericTextEquals(string left, string right)
    {
        return VersionFormatter.NormalizeNumericText(left) == VersionFormatter.NormalizeNumericText(right);
    }

    internal static string IncrementNumericText(string text)
    {
        string normalized = VersionFormatter.NormalizeNumericText(text);
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int carry = 1;
        int index = normalized.Length - 1;

        while (index >= 0)
        {
            int digit = (normalized[index] - '0') + carry;
            if (digit == 10)
            {
                builder.Insert(0, '0');
                carry = 1;
            }
            else
            {
                builder.Insert(0, (char)('0' + digit));
                carry = 0;
            }

            index = index - 1;
        }

        if (carry == 1)
        {
            builder.Insert(0, '1');
        }

        return builder.ToString();
    }

    internal string[] CopyStrings(string[] source)
    {
        string[] copy = new string[source.Length];
        int index = 0;

        while (index < source.Length)
        {
            if (source[index] == null)
            {
                throw new System.ArgumentException("Prerelease prefix cannot contain null values.");
            }

            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }
}
