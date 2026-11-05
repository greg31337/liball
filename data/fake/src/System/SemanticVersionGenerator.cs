using System;
using System.Globalization;

namespace fake;

internal static class SemanticVersionGenerator
{
    const int GeneratedComponentMaximum = 999;

    internal static string Generate(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateGeneration(context, true, true);
        bool includePreRelease = random.Bool();
        bool includeBuildMetadata = random.Bool();
        return GenerateUnchecked(
            random,
            includePreRelease,
            includeBuildMetadata);
    }

    internal static string Generate(
        GenerationContext context,
        FakeRandom random,
        bool includePreRelease,
        bool includeBuildMetadata)
    {
        ValidateGeneration(
            context,
            includePreRelease,
            includeBuildMetadata);
        return GenerateUnchecked(
            random,
            includePreRelease,
            includeBuildMetadata);
    }

    internal static string Generate(
        GenerationContext context,
        int major,
        int minor,
        int patch,
        string preRelease,
        string buildMetadata)
    {
        Guard.NonNegative(major, "major");
        Guard.NonNegative(minor, "minor");
        Guard.NonNegative(patch, "patch");
        Guard.NotNull(preRelease, "preRelease");
        Guard.NotNull(buildMetadata, "buildMetadata");
        Guard.Count(
            preRelease.Length,
            context.CapturedOptions.MaximumCount,
            "preRelease");
        Guard.Count(
            buildMetadata.Length,
            context.CapturedOptions.MaximumCount,
            "buildMetadata");
        if (preRelease.Length > 0 && !ValidIdentifiers(
            preRelease,
            0,
            preRelease.Length,
            true,
            context.CapturedOptions.MaximumCount))
        {
            throw new ArgumentException(
                "preRelease must contain valid dot-separated Semantic Versioning identifiers.",
                "preRelease");
        }
        if (buildMetadata.Length > 0 && !ValidIdentifiers(
            buildMetadata,
            0,
            buildMetadata.Length,
            false,
            context.CapturedOptions.MaximumCount))
        {
            throw new ArgumentException(
                "buildMetadata must contain valid dot-separated Semantic Versioning identifiers.",
                "buildMetadata");
        }
        string majorText = major.ToString(CultureInfo.InvariantCulture);
        string minorText = minor.ToString(CultureInfo.InvariantCulture);
        string patchText = patch.ToString(CultureInfo.InvariantCulture);
        int length = majorText.Length + minorText.Length +
            patchText.Length + 2;
        if (preRelease.Length > 0)
        {
            length = length + 1 + preRelease.Length;
        }
        if (buildMetadata.Length > 0)
        {
            length = length + 1 + buildMetadata.Length;
        }
        Guard.OutputSize(
            length,
            context.CapturedOptions.MaximumOutputSize,
            "semanticVersion");
        string value = majorText + "." + minorText + "." + patchText;
        if (preRelease.Length > 0)
        {
            value = value + "-" + preRelease;
        }
        if (buildMetadata.Length > 0)
        {
            value = value + "+" + buildMetadata;
        }
        return value;
    }

    internal static string PreRelease(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateData(context);
        int maximum = MaximumLength(SemanticVersionData.PreReleases);
        Guard.OutputSize(
            maximum,
            context.CapturedOptions.MaximumOutputSize,
            "semVerPreRelease");
        return SemanticVersionData.PreReleases[
            random.IntFromMaximumExclusive(SemanticVersionData.PreReleases.Length)];
    }

    internal static string BuildMetadata(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateData(context);
        int maximum = MaximumLength(SemanticVersionData.BuildMetadata);
        Guard.OutputSize(
            maximum,
            context.CapturedOptions.MaximumOutputSize,
            "semVerBuildMetadata");
        return SemanticVersionData.BuildMetadata[
            random.IntFromMaximumExclusive(SemanticVersionData.BuildMetadata.Length)];
    }

    internal static string Invalid(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateData(context);
        int maximum = MaximumLength(SemanticVersionData.InvalidVersions);
        Guard.OutputSize(
            maximum,
            context.CapturedOptions.MaximumOutputSize,
            "invalidSemVer");
        return SemanticVersionData.InvalidVersions[
            random.IntFromMaximumExclusive(SemanticVersionData.InvalidVersions.Length)];
    }

    internal static string Invalid(
        GenerationContext context,
        SemVerInvalidKind kind)
    {
        ValidateInvalidKind(kind);
        ValidateData(context);
        string value = SemanticVersionData.InvalidVersions[(int)kind];
        Guard.OutputSize(
            value.Length,
            context.CapturedOptions.MaximumOutputSize,
            "invalidSemVer");
        return value;
    }

    internal static bool Validate(
        GenerationContext context,
        string value)
    {
        Guard.NotNull(value, "value");
        return Components(
            value,
            context.CapturedOptions.MaximumCount) != null;
    }

    internal static SemanticVersion Parse(
        GenerationContext context,
        string value)
    {
        Guard.NotNull(value, "value");
        SemanticVersion? result = Components(
            value,
            context.CapturedOptions.MaximumCount);
        if (result == null)
        {
            throw new ArgumentException(
                "value must conform to Semantic Versioning 2.0.0.",
                "value");
        }
        Guard.OutputSize(
            RecordSize(result),
            context.CapturedOptions.MaximumOutputSize,
            "semanticVersion");
        return result;
    }

    internal static int Compare(
        GenerationContext context,
        string left,
        string right)
    {
        Guard.NotNull(left, "left");
        Guard.NotNull(right, "right");
        SemanticVersion? leftVersion = Components(
            left,
            context.CapturedOptions.MaximumCount);
        if (leftVersion == null)
        {
            throw new ArgumentException(
                "left must conform to Semantic Versioning 2.0.0.",
                "left");
        }
        SemanticVersion? rightVersion = Components(
            right,
            context.CapturedOptions.MaximumCount);
        if (rightVersion == null)
        {
            throw new ArgumentException(
                "right must conform to Semantic Versioning 2.0.0.",
                "right");
        }
        int comparison = CompareNumeric(leftVersion.Major, rightVersion.Major);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = CompareNumeric(leftVersion.Minor, rightVersion.Minor);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = CompareNumeric(leftVersion.Patch, rightVersion.Patch);
        if (comparison != 0)
        {
            return comparison;
        }
        if (!leftVersion.HasPreRelease && !rightVersion.HasPreRelease)
        {
            return 0;
        }
        if (!leftVersion.HasPreRelease)
        {
            return 1;
        }
        if (!rightVersion.HasPreRelease)
        {
            return -1;
        }
        return ComparePreRelease(
            leftVersion.PreRelease,
            rightVersion.PreRelease);
    }

    static string GenerateUnchecked(
        FakeRandom random,
        bool includePreRelease,
        bool includeBuildMetadata)
    {
        string result = random.IntFromMinimumAndMaximumAndMaximumInclusive(
            0,
            GeneratedComponentMaximum,
            true).ToString(CultureInfo.InvariantCulture) + "." +
            random.IntFromMinimumAndMaximumAndMaximumInclusive(
                0,
                GeneratedComponentMaximum,
                true).ToString(CultureInfo.InvariantCulture) + "." +
            random.IntFromMinimumAndMaximumAndMaximumInclusive(
                0,
                GeneratedComponentMaximum,
                true).ToString(CultureInfo.InvariantCulture);
        if (includePreRelease)
        {
            result = result + "-" + SemanticVersionData.PreReleases[
                random.IntFromMaximumExclusive(SemanticVersionData.PreReleases.Length)];
        }
        if (includeBuildMetadata)
        {
            result = result + "+" + SemanticVersionData.BuildMetadata[
                random.IntFromMaximumExclusive(SemanticVersionData.BuildMetadata.Length)];
        }
        return result;
    }

    static void ValidateGeneration(
        GenerationContext context,
        bool includePreRelease,
        bool includeBuildMetadata)
    {
        ValidateData(context);
        int maximum = 11;
        if (includePreRelease)
        {
            maximum = maximum + 1 + MaximumLength(
                SemanticVersionData.PreReleases);
        }
        if (includeBuildMetadata)
        {
            maximum = maximum + 1 + MaximumLength(
                SemanticVersionData.BuildMetadata);
        }
        Guard.OutputSize(
            maximum,
            context.CapturedOptions.MaximumOutputSize,
            "semanticVersion");
    }

    static void ValidateData(GenerationContext context)
    {
        Guard.Count(
            SemanticVersionData.PreReleases.Length,
            context.CapturedOptions.MaximumCount,
            "semVerPreReleaseData");
        Guard.Count(
            SemanticVersionData.BuildMetadata.Length,
            context.CapturedOptions.MaximumCount,
            "semVerBuildMetadataData");
        Guard.Count(
            SemanticVersionData.InvalidVersions.Length,
            context.CapturedOptions.MaximumCount,
            "invalidSemVerData");
        int index = 0;
        while (index < SemanticVersionData.PreReleases.Length)
        {
            string value = SemanticVersionData.PreReleases[index];
            if (!ValidIdentifiers(
                value,
                0,
                value.Length,
                true,
                context.CapturedOptions.MaximumCount))
            {
                throw FakeException.Create(
                    "The project-authored Semantic Versioning pre-release table is malformed.");
            }
            index = index + 1;
        }
        index = 0;
        while (index < SemanticVersionData.BuildMetadata.Length)
        {
            string value = SemanticVersionData.BuildMetadata[index];
            if (!ValidIdentifiers(
                value,
                0,
                value.Length,
                false,
                context.CapturedOptions.MaximumCount))
            {
                throw FakeException.Create(
                    "The project-authored Semantic Versioning build table is malformed.");
            }
            index = index + 1;
        }
        index = 0;
        while (index < SemanticVersionData.InvalidVersions.Length)
        {
            if (Components(
                SemanticVersionData.InvalidVersions[index],
                FakeLimits.DefaultMaximumCount) != null)
            {
                throw FakeException.Create(
                    "The project-authored invalid Semantic Versioning table contains a valid value.");
            }
            index = index + 1;
        }
    }

    static SemanticVersion? Components(string value, int maximumCount)
    {
        Guard.Count(value.Length, maximumCount, "value");
        if (value.Length == 0)
        {
            return null;
        }
        int plus = value.IndexOf('+');
        if (plus >= 0 && value.IndexOf('+', plus + 1) >= 0)
        {
            return null;
        }
        int hyphen = value.IndexOf('-');
        if (hyphen >= 0 && plus >= 0 && hyphen > plus)
        {
            hyphen = -1;
        }
        int coreEnd = value.Length;
        if (hyphen >= 0)
        {
            coreEnd = hyphen;
        }
        else if (plus >= 0)
        {
            coreEnd = plus;
        }
        int firstDot = Find(value, '.', 0, coreEnd);
        if (firstDot <= 0)
        {
            return null;
        }
        int secondDot = Find(value, '.', firstDot + 1, coreEnd);
        if (secondDot <= firstDot + 1 || secondDot >= coreEnd - 1)
        {
            return null;
        }
        if (Find(value, '.', secondDot + 1, coreEnd) >= 0)
        {
            return null;
        }
        if (!ValidNumeric(value, 0, firstDot) ||
            !ValidNumeric(value, firstDot + 1, secondDot) ||
            !ValidNumeric(value, secondDot + 1, coreEnd))
        {
            return null;
        }
        int preReleaseStart = -1;
        int preReleaseEnd = -1;
        if (hyphen >= 0)
        {
            preReleaseStart = hyphen + 1;
            preReleaseEnd = plus >= 0 ? plus : value.Length;
            if (!ValidIdentifiers(
                value,
                preReleaseStart,
                preReleaseEnd,
                true,
                maximumCount))
            {
                return null;
            }
        }
        int buildStart = -1;
        if (plus >= 0)
        {
            buildStart = plus + 1;
            if (!ValidIdentifiers(
                value,
                buildStart,
                value.Length,
                false,
                maximumCount))
            {
                return null;
            }
        }
        SemanticVersion result = new SemanticVersion();
        result.Value = value;
        result.Major = value.Substring(0, firstDot);
        result.Minor = value.Substring(
            firstDot + 1,
            secondDot - firstDot - 1);
        result.Patch = value.Substring(
            secondDot + 1,
            coreEnd - secondDot - 1);
        result.HasPreRelease = preReleaseStart >= 0;
        result.PreRelease = result.HasPreRelease ? value.Substring(
            preReleaseStart,
            preReleaseEnd - preReleaseStart) : string.Empty;
        result.HasBuildMetadata = buildStart >= 0;
        result.BuildMetadata = result.HasBuildMetadata ? value.Substring(
            buildStart) : string.Empty;
        result.SpecificationVersion = SemanticVersionData.SpecificationVersion;
        return result;
    }

    static bool ValidNumeric(
        string value,
        int start,
        int end)
    {
        if (start >= end)
        {
            return false;
        }
        if (end - start > 1 && value[start] == '0')
        {
            return false;
        }
        int index = start;
        while (index < end)
        {
            if (!AsciiDigit(value[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool ValidIdentifiers(
        string value,
        int start,
        int end,
        bool rejectNumericLeadingZero,
        int maximumCount)
    {
        if (start >= end)
        {
            return false;
        }
        int identifierStart = start;
        int identifierCount = 1;
        int index = start;
        while (index <= end)
        {
            if (index == end || value[index] == '.')
            {
                if (identifierStart == index)
                {
                    return false;
                }
                if (rejectNumericLeadingZero &&
                    index - identifierStart > 1 &&
                    value[identifierStart] == '0' &&
                    AllDigits(value, identifierStart, index))
                {
                    return false;
                }
                if (index < end)
                {
                    identifierCount = identifierCount + 1;
                    identifierStart = index + 1;
                }
            }
            else if (!IdentifierCharacter(value[index]))
            {
                return false;
            }
            index = index + 1;
        }
        Guard.Count(identifierCount, maximumCount, "identifierCount");
        return true;
    }

    static int ComparePreRelease(string left, string right)
    {
        string[] leftIdentifiers = left.Split('.');
        string[] rightIdentifiers = right.Split('.');
        int count = leftIdentifiers.Length < rightIdentifiers.Length ?
            leftIdentifiers.Length : rightIdentifiers.Length;
        int index = 0;
        while (index < count)
        {
            string leftIdentifier = leftIdentifiers[index];
            string rightIdentifier = rightIdentifiers[index];
            bool leftNumeric = AllDigits(
                leftIdentifier,
                0,
                leftIdentifier.Length);
            bool rightNumeric = AllDigits(
                rightIdentifier,
                0,
                rightIdentifier.Length);
            int comparison = 0;
            if (leftNumeric && rightNumeric)
            {
                comparison = CompareNumeric(
                    leftIdentifier,
                    rightIdentifier);
            }
            else if (leftNumeric)
            {
                comparison = -1;
            }
            else if (rightNumeric)
            {
                comparison = 1;
            }
            else
            {
                comparison = string.CompareOrdinal(
                    leftIdentifier,
                    rightIdentifier);
            }
            if (comparison < 0)
            {
                return -1;
            }
            if (comparison > 0)
            {
                return 1;
            }
            index = index + 1;
        }
        if (leftIdentifiers.Length < rightIdentifiers.Length)
        {
            return -1;
        }
        if (leftIdentifiers.Length > rightIdentifiers.Length)
        {
            return 1;
        }
        return 0;
    }

    static int CompareNumeric(string left, string right)
    {
        if (left.Length < right.Length)
        {
            return -1;
        }
        if (left.Length > right.Length)
        {
            return 1;
        }
        int comparison = string.CompareOrdinal(left, right);
        if (comparison < 0)
        {
            return -1;
        }
        if (comparison > 0)
        {
            return 1;
        }
        return 0;
    }

    static bool AllDigits(string value, int start, int end)
    {
        int index = start;
        while (index < end)
        {
            if (!AsciiDigit(value[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool IdentifierCharacter(char value)
    {
        return AsciiDigit(value) ||
            value >= 'A' && value <= 'Z' ||
            value >= 'a' && value <= 'z' ||
            value == '-';
    }

    static bool AsciiDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    static int Find(
        string value,
        char target,
        int start,
        int end)
    {
        int index = start;
        while (index < end)
        {
            if (value[index] == target)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static void ValidateInvalidKind(SemVerInvalidKind kind)
    {
        switch (kind)
        {
            case SemVerInvalidKind.LeadingZero:
            case SemVerInvalidKind.MissingComponent:
            case SemVerInvalidKind.EmptyIdentifier:
            case SemVerInvalidKind.InvalidCharacter:
            case SemVerInvalidKind.NumericPreReleaseLeadingZero:
            case SemVerInvalidKind.ExtraCoreComponent:
                return;
            default:
                throw new ArgumentOutOfRangeException(
                    "kind",
                    kind,
                    "kind is not a supported Semantic Versioning invalidity category.");
        }
    }

    static int MaximumLength(string[] values)
    {
        int result = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].Length > result)
            {
                result = values[index].Length;
            }
            index = index + 1;
        }
        return result;
    }

    static int RecordSize(SemanticVersion value)
    {
        return value.Value.Length + value.Major.Length +
            value.Minor.Length + value.Patch.Length +
            value.PreRelease.Length + value.BuildMetadata.Length +
            value.SpecificationVersion.Length;
    }
}
