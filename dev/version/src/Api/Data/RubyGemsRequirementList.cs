namespace version;

/// <summary>
/// Represents a rubygems requirement list.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class RubyGemsRequirementList
{
    /// <summary>
    /// Stores the <c>requirementTexts</c> value.
    /// </summary>
    public string[] requirementTexts;
    /// <summary>
    /// Stores the <c>normalizedRequirementTexts</c> value.
    /// </summary>
    public string[] normalizedRequirementTexts;
    /// <summary>
    /// Stores the <c>ranges</c> value.
    /// </summary>
    public VersionRange[] ranges;

    /// <summary>
    /// Initializes a new RubyGems Requirement List instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="requirementTextValues">The requirement text values to process.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public RubyGemsRequirementList(string?[] requirementTextValues)
    {
        System.ArgumentNullException.ThrowIfNull(requirementTextValues);

        requirementTexts = NormalizeInputRequirements(requirementTextValues);
        if (requirementTexts.Length == 0)
        {
            requirementTexts = new string[] { ">= 0" };
        }

        normalizedRequirementTexts = CreateNormalizedRequirementTexts(requirementTexts);
        ranges = ParseRanges(requirementTexts);
    }

    /// <summary>
    /// Creates a default.
    /// </summary>
    /// <returns>The resulting rubygems requirement list.</returns>
    internal static RubyGemsRequirementList CreateDefault()
    {
        return new RubyGemsRequirementList(new string[0]);
    }

    /// <summary>
    /// Creates a default prerelease.
    /// </summary>
    /// <returns>The resulting rubygems requirement list.</returns>
    internal static RubyGemsRequirementList CreateDefaultPrerelease()
    {
        return new RubyGemsRequirementList(new string[] { ">= 0.a" });
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="requirementText">The requirement text to process.</param>
    /// <returns>The resulting rubygems requirement list.</returns>
    internal static RubyGemsRequirementList Create(string requirementText)
    {
        return new RubyGemsRequirementList(new string[] { requirementText });
    }

    /// <summary>
    /// Creates a from nullable text.
    /// </summary>
    /// <param name="requirementText">The requirement text to process.</param>
    /// <returns>The resulting rubygems requirement list.</returns>
    internal static RubyGemsRequirementList CreateFromNullableText(string? requirementText)
    {
        if (requirementText == null)
        {
            return CreateDefault();
        }

        return Create(requirementText);
    }

    /// <summary>
    /// Creates a from version text.
    /// </summary>
    /// <param name="versionText">The version text to process.</param>
    /// <returns>The resulting rubygems requirement list.</returns>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    internal static RubyGemsRequirementList CreateFromVersionText(string versionText)
    {
        VersionParseResult result = RubyGemsVersions.Parse(versionText);
        if (result.IsFailure())
        {
            throw new System.ArgumentException("Invalid RubyGems requirement version.");
        }

        return CreateFromVersion(result.GetVersion());
    }

    /// <summary>
    /// Creates a from version.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting rubygems requirement list.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    internal static RubyGemsRequirementList CreateFromVersion(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);

        if (version.GetDialect() != VersionDialect.RubyGems)
        {
            throw new System.ArgumentException("RubyGems requirement version must use the RubyGems dialect.");
        }

        return new RubyGemsRequirementList(new string[] { "= " + version.GetOriginalText() });
    }

    /// <summary>
    /// Creates a from arrays.
    /// </summary>
    /// <param name="requirementArrays">The requirement arrays to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>The resulting rubygems requirement list.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal static RubyGemsRequirementList CreateFromArrays(string?[]?[] requirementArrays)
    {
        System.ArgumentNullException.ThrowIfNull(requirementArrays);

        System.Collections.Generic.List<string> values = new System.Collections.Generic.List<string>();
        int outerIndex = 0;

        while (outerIndex < requirementArrays.Length)
        {
            string?[]? requirementArray = requirementArrays[outerIndex];
            if (requirementArray != null)
            {
                int innerIndex = 0;
                while (innerIndex < requirementArray.Length)
                {
                    string? requirementText = requirementArray[innerIndex];
                    if (requirementText != null)
                    {
                        values.Add(requirementText);
                    }

                    innerIndex = innerIndex + 1;
                }
            }

            outerIndex = outerIndex + 1;
        }

        return new RubyGemsRequirementList(values.ToArray());
    }

    /// <summary>
    /// Gets a requirement count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetRequirementCount()
    {
        return requirementTexts.Length;
    }

    /// <summary>
    /// Gets a requirement text.
    /// </summary>
    /// <param name="requirementIndex">The zero-based requirement index.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal string GetRequirementText(int requirementIndex)
    {
        if (requirementIndex < 0 || requirementIndex >= requirementTexts.Length)
        {
            throw new System.ArgumentOutOfRangeException("requirementIndex");
        }

        return requirementTexts[requirementIndex];
    }

    /// <summary>
    /// Gets a requirement texts.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal string[] GetRequirementTexts()
    {
        return CopyStrings(requirementTexts);
    }

    /// <summary>
    /// Gets a normalized requirement text.
    /// </summary>
    /// <param name="requirementIndex">The zero-based requirement index.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal string GetNormalizedRequirementText(int requirementIndex)
    {
        if (requirementIndex < 0 || requirementIndex >= normalizedRequirementTexts.Length)
        {
            throw new System.ArgumentOutOfRangeException("requirementIndex");
        }

        return normalizedRequirementTexts[requirementIndex];
    }

    /// <summary>
    /// Gets a normalized requirement texts.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal string[] GetNormalizedRequirementTexts()
    {
        return CopyStrings(normalizedRequirementTexts);
    }

    /// <summary>
    /// Gets a range.
    /// </summary>
    /// <param name="requirementIndex">The zero-based requirement index.</param>
    /// <returns>The resulting version range.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal VersionRange GetRange(int requirementIndex)
    {
        if (requirementIndex < 0 || requirementIndex >= ranges.Length)
        {
            throw new System.ArgumentOutOfRangeException("requirementIndex");
        }

        return ranges[requirementIndex];
    }

    /// <summary>
    /// Formats this value as requirement text.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <returns>The resulting text.</returns>
    internal string ToRequirementText()
    {
        return JoinRequirements(requirementTexts);
    }

    /// <summary>
    /// Formats this value as normalized requirement text.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <returns>The resulting text.</returns>
    internal string ToNormalizedRequirementText()
    {
        return JoinRequirements(normalizedRequirementTexts);
    }

    /// <summary>
    /// Reports whether default.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsDefault()
    {
        return normalizedRequirementTexts.Length == 1 && normalizedRequirementTexts[0] == ">= 0";
    }

    /// <summary>
    /// Reports whether default prerelease.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsDefaultPrerelease()
    {
        return normalizedRequirementTexts.Length == 1 && normalizedRequirementTexts[0] == ">= 0.a";
    }

    /// <summary>
    /// Reports whether exact.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsExact()
    {
        return normalizedRequirementTexts.Length == 1 && StartsWithText(normalizedRequirementTexts[0], "= ");
    }

    /// <summary>
    /// Reports whether specific.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsSpecific()
    {
        if (normalizedRequirementTexts.Length > 1)
        {
            return true;
        }

        if (StartsWithText(normalizedRequirementTexts[0], "> "))
        {
            return false;
        }

        if (StartsWithText(normalizedRequirementTexts[0], ">= "))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Reports whether version text.
    /// </summary>
    /// <param name="versionText">The version text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool AllowsVersionText(string versionText)
    {
        VersionParseResult result = RubyGemsVersions.Parse(versionText);
        if (result.IsFailure())
        {
            return false;
        }

        return AllowsVersion(result.GetVersion());
    }

    /// <summary>
    /// Reports whether version.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    internal bool AllowsVersion(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);

        if (version.GetDialect() != VersionDialect.RubyGems)
        {
            throw new System.ArgumentException("RubyGems requirements can only match RubyGems versions.");
        }

        if (IsDefault() && version.IsPrerelease())
        {
            return false;
        }

        int index = 0;
        while (index < ranges.Length)
        {
            if (!ranges[index].ContainsVersion(version))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    /// <summary>
    /// Reports whether this value has prerelease requirement.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasPrereleaseRequirement()
    {
        int index = 0;

        while (index < requirementTexts.Length)
        {
            if (ContainsLetter(requirementTexts[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static string[] NormalizeInputRequirements(string?[] values)
    {
        System.Collections.Generic.List<string> requirements = new System.Collections.Generic.List<string>();
        int index = 0;

        while (index < values.Length)
        {
            string? value = values[index];
            if (value != null)
            {
                AddSplitRequirements(requirements, value);
            }

            index = index + 1;
        }

        return requirements.ToArray();
    }

    internal static void AddSplitRequirements(System.Collections.Generic.List<string> requirements, string text)
    {
        string[] parts = SplitRequirements(text);
        int index = 0;

        while (index < parts.Length)
        {
            string requirement = TrimText(parts[index]);
            if (requirement.Length == 0)
            {
                throw new System.ArgumentException("RubyGems requirements cannot contain empty values.");
            }

            string normalized = NormalizeRequirementText(requirement);
            if (!ContainsText(requirements, normalized))
            {
                requirements.Add(requirement);
            }

            index = index + 1;
        }
    }

    internal static string[] CreateNormalizedRequirementTexts(string[] values)
    {
        string[] normalized = new string[values.Length];
        int index = 0;

        while (index < values.Length)
        {
            normalized[index] = NormalizeRequirementText(values[index]);
            index = index + 1;
        }

        return normalized;
    }

    internal static VersionRange[] ParseRanges(string[] requirementTextValues)
    {
        VersionRange[] rangeValues = new VersionRange[requirementTextValues.Length];
        int index = 0;

        while (index < requirementTextValues.Length)
        {
            VersionRangeParseResult result = RubyGemsRangeParser.Parse(requirementTextValues[index]);
            if (result.IsFailure())
            {
                throw new System.ArgumentException("Invalid RubyGems requirement.");
            }

            rangeValues[index] = result.GetRange();
            index = index + 1;
        }

        return rangeValues;
    }

    internal static string NormalizeRequirementText(string text)
    {
        string trimmed = TrimText(text);
        string rangeOperator = ReadOperator(trimmed);
        string versionText = TrimText(trimmed.Substring(rangeOperator.Length));

        if (rangeOperator.Length == 0)
        {
            rangeOperator = "=";
            versionText = trimmed;
        }

        return rangeOperator + " " + versionText;
    }

    internal static string ReadOperator(string text)
    {
        if (StartsWithText(text, "~>"))
        {
            return "~>";
        }

        if (StartsWithText(text, ">="))
        {
            return ">=";
        }

        if (StartsWithText(text, "<="))
        {
            return "<=";
        }

        if (StartsWithText(text, "!="))
        {
            return "!=";
        }

        if (StartsWithText(text, ">"))
        {
            return ">";
        }

        if (StartsWithText(text, "<"))
        {
            return "<";
        }

        if (StartsWithText(text, "="))
        {
            return "=";
        }

        return "";
    }

    internal static string[] SplitRequirements(string text)
    {
        System.Collections.Generic.List<string> parts = new System.Collections.Generic.List<string>();
        int startIndex = 0;
        int index = 0;

        while (index < text.Length)
        {
            if (text[index] == ',')
            {
                parts.Add(text.Substring(startIndex, index - startIndex));
                startIndex = index + 1;
            }

            index = index + 1;
        }

        parts.Add(text.Substring(startIndex));
        return parts.ToArray();
    }

    internal static string TrimText(string text)
    {
        int startIndex = 0;
        int endIndex = text.Length;

        while (startIndex < endIndex && VersionChar.IsWhitespace(text[startIndex]))
        {
            startIndex = startIndex + 1;
        }

        while (endIndex > startIndex && VersionChar.IsWhitespace(text[endIndex - 1]))
        {
            endIndex = endIndex - 1;
        }

        return text.Substring(startIndex, endIndex - startIndex);
    }

    internal static bool ContainsText(System.Collections.Generic.List<string> values, string normalizedRequirement)
    {
        int index = 0;

        while (index < values.Count)
        {
            if (NormalizeRequirementText(values[index]) == normalizedRequirement)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static string[] CopyStrings(string[] values)
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

    internal static string JoinRequirements(string[] values)
    {
        string text = "";
        int index = 0;

        while (index < values.Length)
        {
            if (text.Length > 0)
            {
                text = text + ", ";
            }

            text = text + values[index];
            index = index + 1;
        }

        return text;
    }

    internal static bool ContainsLetter(string text)
    {
        int index = 0;

        while (index < text.Length)
        {
            if (VersionChar.IsLetter(text[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool StartsWithText(string text, string valueText)
    {
        if (text.Length < valueText.Length)
        {
            return false;
        }

        return text.Substring(0, valueText.Length) == valueText;
    }
}
